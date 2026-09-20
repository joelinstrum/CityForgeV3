using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class LotModManifest
    {
        public string schema;
        public LotModEntry[] lots;
    }

    [Serializable]
    public sealed class LotModEntry
    {
        public string id;
        public string lotFile;
        public string previewFile;
    }

    [Serializable]
    public sealed class BundledLotManifest
    {
        public string schema;
        public BundledLotEntry[] lots;
    }

    [Serializable]
    public sealed class BundledLotEntry
    {
        public string id;
        public string lotResourcePath;
        public string previewResourcePath;
    }

    /// <summary>
    /// One discovery boundary for bundled, user-authored, and mod Lots.
    /// District code asks by stable lot ID and never needs to know where the
    /// package came from.
    /// </summary>
    public static class LotContentCatalog
    {
        public const string ModManifestName = "lot-content.json";
        private sealed class Source
        {
            public LotSaveSummary Summary;
            public string JsonPath;
            public string PreviewPath;
            public string JsonResourcePath;
            public string PreviewResourcePath;
            public bool UseSaveStore;
        }

        private static Dictionary<string, Source> _byId;
        private static List<LotSaveSummary> _all;

        public static IReadOnlyList<LotSaveSummary> All
        {
            get { EnsureLoaded(); return _all; }
        }

        public static LotSaveData Read(string lotId)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(lotId) ||
                !_byId.TryGetValue(lotId, out var source)) return null;
            try
            {
                return source.UseSaveStore
                    ? LotSaveStore.Read(lotId)
                    : !string.IsNullOrWhiteSpace(source.JsonResourcePath)
                        ? JsonUtility.FromJson<LotSaveData>(
                            Resources.Load<TextAsset>(source.JsonResourcePath)?.text)
                    : JsonUtility.FromJson<LotSaveData>(File.ReadAllText(source.JsonPath));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not load lot content '{lotId}': {exception.Message}");
                return null;
            }
        }

        public static string PreviewPath(string lotId)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(lotId) &&
                   _byId.TryGetValue(lotId, out var source)
                ? source.PreviewPath : null;
        }

        public static Texture2D PreviewTexture(string lotId)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(lotId) &&
                   _byId.TryGetValue(lotId, out var source) &&
                   !string.IsNullOrWhiteSpace(source.PreviewResourcePath)
                ? Resources.Load<Texture2D>(source.PreviewResourcePath) : null;
        }

        public static void InvalidateCache()
        {
            _byId = null;
            _all = null;
        }

        private static void EnsureLoaded()
        {
            if (_byId != null) return;
            _byId = new Dictionary<string, Source>(StringComparer.Ordinal);
            _all = new List<LotSaveSummary>();
            LoadBundledManifest();
            foreach (var summary in LotSaveStore.List())
                Add(summary, summary.Path, LotSaveStore.PreviewPath(summary.LotId),
                    "saved lot", false, true);

            var modsRoot = Path.Combine(Application.persistentDataPath, "Mods");
            if (Directory.Exists(modsRoot))
                foreach (var manifestPath in Directory.GetFiles(modsRoot,
                             ModManifestName, SearchOption.AllDirectories))
                    LoadModManifest(manifestPath);
            _all.Sort((left, right) =>
                string.CompareOrdinal(right.ModifiedUtc, left.ModifiedUtc));
        }

        private static void LoadBundledManifest()
        {
            const string manifestPath = "CityForgeV3/Lots/catalog";
            var asset = Resources.Load<TextAsset>(manifestPath);
            if (asset == null) return;
            try
            {
                var manifest = JsonUtility.FromJson<BundledLotManifest>(asset.text);
                if (manifest?.schema != "cityforge-bundled-lots-v1" ||
                    manifest.lots == null)
                    throw new InvalidOperationException("Unsupported bundled Lot manifest.");
                foreach (var entry in manifest.lots)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id) ||
                        string.IsNullOrWhiteSpace(entry.lotResourcePath))
                        throw new InvalidOperationException(
                            "Each bundled Lot requires id and lotResourcePath.");
                    var lotAsset = Resources.Load<TextAsset>(entry.lotResourcePath);
                    if (lotAsset == null)
                        throw new InvalidOperationException(
                            $"Bundled Lot '{entry.id}' is missing its JSON resource.");
                    var data = JsonUtility.FromJson<LotSaveData>(lotAsset.text);
                    if (data == null || data.LotId != entry.id)
                        throw new InvalidOperationException(
                            $"Bundled Lot '{entry.id}' does not match its payload ID.");
                    var summary = Summary(data, $"resource:{entry.lotResourcePath}");
                    Add(summary, null, null, manifestPath, false, false,
                        entry.lotResourcePath, entry.previewResourcePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Skipping invalid bundled Lot manifest: {exception.Message}");
            }
        }

        private static void LoadModManifest(string manifestPath)
        {
            try
            {
                var manifest = JsonUtility.FromJson<LotModManifest>(
                    File.ReadAllText(manifestPath));
                if (manifest?.schema != "cityforge-lot-content-v1" ||
                    manifest.lots == null)
                    throw new InvalidOperationException("Unsupported lot manifest.");
                var root = Path.GetFullPath(Path.GetDirectoryName(manifestPath));
                foreach (var entry in manifest.lots)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id) ||
                        string.IsNullOrWhiteSpace(entry.lotFile))
                        throw new InvalidOperationException(
                            "Each mod lot requires id and lotFile.");
                    var jsonPath = SafeChildPath(root, entry.lotFile);
                    var previewPath = string.IsNullOrWhiteSpace(entry.previewFile)
                        ? null : SafeChildPath(root, entry.previewFile);
                    var data = JsonUtility.FromJson<LotSaveData>(File.ReadAllText(jsonPath));
                    if (data == null || data.LotId != entry.id)
                        throw new InvalidOperationException(
                            $"Lot '{entry.id}' does not match its save payload ID.");
                    var summary = Summary(data, jsonPath);
                    Add(summary, jsonPath, previewPath, manifestPath, true, false);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Skipping invalid lot mod manifest '{manifestPath}': " +
                               exception.Message);
            }
        }

        private static void Add(LotSaveSummary summary, string jsonPath,
            string previewPath, string sourceName, bool mod, bool useSaveStore,
            string jsonResourcePath = null, string previewResourcePath = null)
        {
            if (summary == null || string.IsNullOrWhiteSpace(summary.LotId)) return;
            if (_byId.ContainsKey(summary.LotId))
            {
                if (mod) Debug.LogError($"Skipping duplicate mod lot id " +
                    $"'{summary.LotId}' from '{sourceName}'.");
                return;
            }
            _byId.Add(summary.LotId, new Source
            {
                Summary = summary,
                JsonPath = jsonPath,
                PreviewPath = previewPath,
                JsonResourcePath = jsonResourcePath,
                PreviewResourcePath = previewResourcePath,
                UseSaveStore = useSaveStore
            });
            _all.Add(summary);
        }

        private static LotSaveSummary Summary(LotSaveData data, string path) => new()
        {
            LotId = data.LotId,
            Name = data.Name,
            LotType = data.LotType,
            LotSizeMeters = data.LotSizeMeters,
            LotWidthCells = data.LotWidthCells,
            LotDepthCells = data.LotDepthCells,
            ModifiedUtc = data.ModifiedUtc,
            Path = path,
            BuildingId = data.Buildings3D != null && data.Buildings3D.Count > 0
                ? data.Buildings3D[0].AssetId
                : data.Buildings != null && data.Buildings.Count > 0
                    ? data.Buildings[0].BuildingId : data.BuildingId,
            PlopCost = LotEconomy.CalculatePlopCost(data),
            RequiredPackageIds = data.RequiredPackageIds ?? new List<string>()
        };

        private static string SafeChildPath(string root, string relative)
        {
            var path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Mod content path escapes its package root: {relative}");
            return path;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => InvalidateCache();
    }
}
