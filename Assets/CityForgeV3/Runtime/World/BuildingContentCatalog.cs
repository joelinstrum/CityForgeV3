using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class BuildingContentManifest
    {
        public string schema;
        public BuildingContentEntry[] buildings;
    }

    [Serializable]
    public sealed class ContentResourceAmount
    {
        public string resourceId;
        public float amount;
        public string interval;
    }

    [Serializable]
    public sealed class BuildingEffectContract
    {
        public string effectId;
        public float magnitude;
        public float radiusMeters;
        public string target;
    }

    [Serializable]
    public sealed class BuildingConstructionContract
    {
        public string mode = "mesh-height";
        public float secondsPerStage = 1f;
        public string[] stageNames;
        public string[] rendererGroups;
    }

    [Serializable]
    public sealed class BuildingContentEntry
    {
        public string id;
        public string displayName;
        public string category;
        public string subcategory;
        public string description;
        public string modelResourcePath;
        public string thumbnailResourcePath;
        public string provider = "resources";
        public string bundlePath;
        public string modelAssetName;
        public string thumbnailAssetName;
        public bool hideFromLotEditor;
        public bool hideFromDistrictBuilder;
        public float pitchDegrees = -90f;
        public float baseYawDegrees = 90f;
        public string normalizeAxis = "none";
        public float normalizeMeters;
        public string materialMode = "embedded";
        public string textureRoot;
        public float bumpScale = 0.72f;
        public float metallic;
        public float smoothness = 0.18f;
        public string tintHex = "#FFFFFF";
        public string runtimeProfile = "standard";
        public int plopCost;
        public ContentResourceAmount[] constructionRequirements;
        public ContentResourceAmount[] operatingInputs;
        public ContentResourceAmount[] operatingOutputs;
        public BuildingEffectContract[] effects;
        public BuildingConstructionContract construction;

        [NonSerialized] public string sourceId;
        [NonSerialized] public string sourceRoot;
    }

    public interface IBuildingContentProvider
    {
        string Id { get; }
        GameObject LoadModel(BuildingContentEntry entry);
        Texture2D LoadThumbnail(BuildingContentEntry entry);
    }

    internal sealed class ResourcesBuildingContentProvider : IBuildingContentProvider
    {
        public string Id => "resources";
        public GameObject LoadModel(BuildingContentEntry entry) =>
            Resources.Load<GameObject>(entry.modelResourcePath);
        public Texture2D LoadThumbnail(BuildingContentEntry entry) =>
            Resources.Load<Texture2D>(entry.thumbnailResourcePath);
    }

    internal sealed class AssetBundleBuildingContentProvider : IBuildingContentProvider
    {
        private readonly Dictionary<string, AssetBundle> _bundles = new();
        public string Id => "asset-bundle";

        public GameObject LoadModel(BuildingContentEntry entry) =>
            Load<GameObject>(entry, entry.modelAssetName);

        public Texture2D LoadThumbnail(BuildingContentEntry entry) =>
            Load<Texture2D>(entry, entry.thumbnailAssetName);

        private T Load<T>(BuildingContentEntry entry, string assetName)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(entry.sourceRoot) ||
                string.IsNullOrWhiteSpace(entry.bundlePath) ||
                string.IsNullOrWhiteSpace(assetName)) return null;
            var root = Path.GetFullPath(entry.sourceRoot);
            var path = Path.GetFullPath(Path.Combine(root, entry.bundlePath));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Content bundle escapes its mod root: {entry.bundlePath}");
            if (!_bundles.TryGetValue(path, out var bundle) || bundle == null)
            {
                bundle = AssetBundle.LoadFromFile(path);
                if (bundle != null) _bundles[path] = bundle;
            }
            return bundle == null ? null : bundle.LoadAsset<T>(assetName);
        }
    }

    public static class BuildingContentCatalog
    {
        public const string BuiltInManifestResource =
            "CityForgeV3/Content/building-content-catalog";
        public const string ModManifestName = "building-content.json";
        private static readonly Dictionary<string, IBuildingContentProvider>
            Providers = new(StringComparer.OrdinalIgnoreCase)
            {
                ["resources"] = new ResourcesBuildingContentProvider(),
                ["asset-bundle"] = new AssetBundleBuildingContentProvider()
            };
        private static IReadOnlyList<BuildingContentEntry> _all;
        private static Dictionary<string, BuildingContentEntry> _byId;

        public static IReadOnlyList<BuildingContentEntry> All
        {
            get
            {
                EnsureLoaded();
                return _all;
            }
        }

        public static BuildingContentEntry Find(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && _byId.TryGetValue(id, out var entry)
                ? entry : null;
        }

        public static IReadOnlyList<BuildingContentEntry> ForLotEditor(
            BuildingUseCategory category)
        {
            EnsureLoaded();
            var result = new List<BuildingContentEntry>();
            foreach (var entry in _all)
                if (!entry.hideFromLotEditor && Category(entry) == category) result.Add(entry);
            return result;
        }

        public static IReadOnlyList<BuildingContentEntry> ForDistrictBuilder(
            BuildingUseCategory category)
        {
            EnsureLoaded();
            var result = new List<BuildingContentEntry>();
            foreach (var entry in _all)
                if (!entry.hideFromDistrictBuilder && Category(entry) == category) result.Add(entry);
            return result;
        }

        public static GameObject LoadModel(BuildingContentEntry entry) =>
            Provider(entry).LoadModel(entry);
        public static Texture2D LoadThumbnail(BuildingContentEntry entry) =>
            Provider(entry).LoadThumbnail(entry);

        public static BuildingUseCategory Category(BuildingContentEntry entry) =>
            Enum.TryParse(entry?.category, true, out BuildingUseCategory category)
                ? category : BuildingUseCategory.Mixed;

        public static void InvalidateCache()
        {
            _all = null;
            _byId = null;
        }

        private static IBuildingContentProvider Provider(BuildingContentEntry entry)
        {
            var providerId = string.IsNullOrWhiteSpace(entry?.provider)
                ? "resources" : entry.provider;
            if (entry != null && Providers.TryGetValue(providerId, out var provider))
                return provider;
            throw new InvalidOperationException(
                $"Unknown building content provider '{entry?.provider}'.");
        }

        private static void EnsureLoaded()
        {
            if (_all != null) return;
            var entries = new List<BuildingContentEntry>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var builtIn = Resources.Load<TextAsset>(BuiltInManifestResource);
            if (builtIn == null) throw new MissingReferenceException(
                $"Missing building content manifest: {BuiltInManifestResource}");
            AddManifest(builtIn.text, "builtin", null, entries, ids, true);

            var modsRoot = Path.Combine(Application.persistentDataPath, "Mods");
            if (Directory.Exists(modsRoot))
                foreach (var path in Directory.GetFiles(modsRoot, ModManifestName,
                             SearchOption.AllDirectories))
                    try
                    {
                        AddManifest(File.ReadAllText(path), path,
                            Path.GetDirectoryName(path), entries, ids, false);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"Skipping invalid building mod manifest '{path}': " +
                                       exception.Message);
                    }
            _all = entries;
            _byId = new Dictionary<string, BuildingContentEntry>(StringComparer.Ordinal);
            foreach (var entry in entries) _byId.Add(entry.id, entry);
        }

        private static void AddManifest(string json, string sourceId, string sourceRoot,
            List<BuildingContentEntry> entries, HashSet<string> ids, bool failFast)
        {
            var manifest = JsonUtility.FromJson<BuildingContentManifest>(json);
            if (manifest?.schema != "cityforge-building-content-v1" ||
                manifest.buildings == null)
                throw new InvalidOperationException("Unsupported or empty building manifest.");
            foreach (var entry in manifest.buildings)
            {
                try
                {
                    Validate(entry);
                    if (!ids.Add(entry.id)) throw new InvalidOperationException(
                        $"Duplicate building content id '{entry.id}'.");
                    entry.sourceId = sourceId;
                    entry.sourceRoot = sourceRoot;
                    entries.Add(entry);
                }
                catch (Exception exception)
                {
                    if (failFast) throw;
                    Debug.LogError($"Skipping invalid building from '{sourceId}': " +
                                   exception.Message);
                }
            }
        }

        private static void Validate(BuildingContentEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) ||
                string.IsNullOrWhiteSpace(entry.displayName) ||
                string.IsNullOrWhiteSpace(entry.category))
                throw new InvalidOperationException(
                    "Building entries require id, displayName, and category.");
            var providerId = string.IsNullOrWhiteSpace(entry.provider)
                ? "resources" : entry.provider;
            if (!Providers.ContainsKey(providerId))
                throw new InvalidOperationException(
                    $"Unknown provider '{entry.provider}'.");
            if (providerId == "resources" &&
                string.IsNullOrWhiteSpace(entry.modelResourcePath))
                throw new InvalidOperationException(
                    $"Resource building '{entry.id}' has no modelResourcePath.");
            if (entry.normalizeMeters < 0f)
                throw new InvalidOperationException(
                    $"Building '{entry.id}' has a negative normalization size.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => InvalidateCache();
    }
}
