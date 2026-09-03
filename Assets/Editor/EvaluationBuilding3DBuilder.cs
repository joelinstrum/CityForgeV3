using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForgeV3.Buildings3D;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Editor
{
    public static class EvaluationBuilding3DBuilder
    {
        private readonly struct Spec
        {
            public Spec(string folder, string id, string model, float height)
            {
                Folder = folder; Id = id; Model = model; Height = height;
            }
            public string Folder { get; }
            public string Id { get; }
            public string Model { get; }
            public float Height { get; }
        }

        private const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/Evaluation";
        private static readonly Spec[] Specs =
        {
            new("NYBrownstoneLight", "ny-brownstone-light-eval-v01",
                "tripo_convert_1ac370be-d12f-4034-ab34-1ac2ffa914a6.fbx", 15f),
            new("NYBrownstoneBay", "ny-brownstone-bay-eval-v01",
                "tripo_convert_aa29ae56-77e0-4e65-a59a-53c6c90c5c27.fbx", 15f),
            new("NYFancyTownhouse", "ny-fancy-townhouse-eval-v01",
                "tripo_convert_4ea85563-44f0-4e48-ae53-fa8feac21a8a.fbx", 15f),
            new("NYBrownstone", "ny-brownstone-eval-v01",
                "tripo_convert_ddea9fd4-f35e-4817-ad0a-69fc445bd252.fbx", 15f),
            new("BrooklynTownhomeRow", "brooklyn-townhome-row-eval-v01",
                "tripo_convert_66614bbe-e6a7-4205-8a19-931eef4fd955.fbx", 15f),
            new("NorwalkClockTower", "norwalk-clock-tower-eval-v01",
                "tripo_convert_36826576-a2ca-44a4-a1bd-3d9754105385.fbx", 48f),
        };

        [InitializeOnLoadMethod]
        private static void QueueBuild()
        {
            EditorApplication.update -= TryBuildWhenReady;
            EditorApplication.update += TryBuildWhenReady;
        }

        private static void TryBuildWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                AssetDatabase.LoadAssetAtPath<GameObject>(Specs[0].ModelPath()) == null)
                return;
            EditorApplication.update -= TryBuildWhenReady;
            foreach (var spec in Specs)
            {
                var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                    spec.PackagePath());
                if (AssetDatabase.LoadAssetAtPath<GameObject>(
                        spec.PrefabPath()) == null ||
                    spec.UsesLocalBrownstoneMaterial() &&
                    AssetDatabase.LoadAssetAtPath<Material>(
                        spec.MaterialPath()) == null ||
                    spec.Folder == "NorwalkClockTower" &&
                    (package?.Representations?.Count ?? 0) < 5)
                    Build(spec);
            }
        }

        [MenuItem("City Forge/3D Buildings/Rebuild Evaluation Building Packages")]
        public static void RebuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var spec in Specs) Build(spec);
        }

        private static void Build(Spec spec)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath());
            if (model == null) throw new FileNotFoundException(spec.ModelPath());
            var metrics = CityForge.Editor.Building3DPackageValidator.Measure(model);
            if (metrics.Bounds.size.y <= 0.0001f)
                throw new InvalidDataException($"No measurable bounds: {spec.Id}");
            var scale = spec.Height / metrics.Bounds.size.y;
            var material = spec.UsesLocalBrownstoneMaterial()
                ? CreateOrUpdateMaterial(spec) : null;

            EnsureFolder(spec.RootPath() + "/Prefabs");
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                spec.PackagePath());
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, spec.PackagePath());
            }
            package.SchemaVersion = Building3DPackage.CurrentSchemaVersion;
            package.AssetId = spec.Id;
            package.SourceProvenance = spec.Folder == "NorwalkClockTower"
                ? "Norwalk Juvenile Courthouse, Norwalk, Ohio; user-supplied immutable Tripo archive. Original FBX preserved unchanged as LOD0; lower levels are derived separately."
                : "User-supplied immutable Tripo archive; LOD0-only evaluation package.";
            package.AuthoredScale = Vector3.one * scale;
            package.PivotOffset = Vector3.zero;
            package.FrontYawDegrees = 90f;
            package.FootprintMeters = new Vector2(
                metrics.Bounds.size.x * scale, metrics.Bounds.size.z * scale);
            package.BoundsTolerance = 0.1f;
            package.UseCrossFade = false;
            package.Representations = new List<Building3DRepresentation>
            {
                new()
                {
                    Level = Building3DLevel.LOD0,
                    ScreenRelativeHeight = spec.Folder == "NorwalkClockTower"
                        ? 0.32f : 0.001f,
                    VisualPrefab = model,
                    OverrideMaterial = material,
                    LocalPosition = new Vector3(-metrics.Bounds.center.x,
                        -metrics.Bounds.min.y, -metrics.Bounds.center.z),
                    LocalScale = Vector3.one,
                    TargetTriangleBudget = metrics.Triangles,
                    Provenance = "Supplied LOD0 imported unchanged; measured and ground-registered for evaluation."
                }
            };
            if (spec.Folder == "NorwalkClockTower")
            {
                var thresholds = new[] { 0.18f, 0.09f, 0.035f, 0.001f };
                var ratios = new[] { 0.50f, 0.25f, 0.10f, 0.04f };
                for (var index = 0; index < 4; index++)
                {
                    var level = index + 1;
                    var lodPath = $"{spec.RootPath()}/LOD{level}/" +
                        $"NorwalkCourthouse_LOD{level}.fbx";
                    var lodModel = AssetDatabase.LoadAssetAtPath<GameObject>(lodPath);
                    if (lodModel == null)
                        throw new FileNotFoundException(lodPath);
                    var lodMetrics = CityForge.Editor.Building3DPackageValidator
                        .Measure(lodModel);
                    package.Representations.Add(new Building3DRepresentation
                    {
                        Level = (Building3DLevel)level,
                        ScreenRelativeHeight = thresholds[index],
                        VisualPrefab = lodModel,
                        OverrideMaterial = material,
                        LocalPosition = new Vector3(-lodMetrics.Bounds.center.x,
                            -lodMetrics.Bounds.min.y, -lodMetrics.Bounds.center.z),
                        LocalScale = Vector3.one,
                        TargetTriangleBudget = Mathf.RoundToInt(
                            metrics.Triangles * ratios[index]),
                        Provenance = $"Derived from immutable supplied LOD0 at " +
                            $"{ratios[index]:P0} triangle ratio; LOD0 unchanged."
                    });
                }
            }
            EditorUtility.SetDirty(package);

            var root = new GameObject(spec.Id);
            try
            {
                root.AddComponent<Building3DPackageInstance>().Configure(package);
                PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath());
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log($"Evaluation building ready: {spec.Id}, " +
                $"{metrics.Triangles:N0} triangles, {spec.Height:N1}m high.");
        }

        private static Material CreateOrUpdateMaterial(Spec spec)
        {
            EnsureFolder(spec.RootPath() + "/Materials");
            var texturePaths = AssetDatabase.FindAssets("t:Texture2D",
                    new[] { spec.RootPath() + "/Source" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var baseColorPath = texturePaths.First(path =>
                path.IndexOf("basecolor", StringComparison.OrdinalIgnoreCase) >= 0);
            var normalPath = texturePaths.First(path =>
                path.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0);
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            if (shader == null)
                throw new MissingReferenceException(
                    "CityForge building-local PBR shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                spec.MaterialPath());
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = spec.Folder + " Local Brownstone"
                };
                AssetDatabase.CreateAsset(material, spec.MaterialPath());
            }
            material.shader = shader;
            material.SetTexture("_MainTex",
                AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath));
            material.SetTexture("_BumpMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            material.EnableKeyword("_NORMALMAP");
            var isNorwalk = spec.Folder == "NorwalkClockTower";
            material.SetFloat("_BumpScale", isNorwalk ? 0.85f : 0.5f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_GlossMapScale", isNorwalk ? 0.18f : 0.14f);
            material.SetColor("_Color", Color.white);
            // Recover restrained authored brick chroma locally. The source
            // atlases average only 17-20% saturation, so this remains a
            // historic brownstone grade rather than a scene-wide color push.
            material.SetFloat("_Contrast", isNorwalk ? 1.04f : 1.18f);
            material.SetFloat("_Saturation", isNorwalk ? 1.08f : 1.72f);
            material.SetFloat("_Vibrance", isNorwalk ? 0.06f : 0.18f);
            material.SetFloat("_AmbientFill", 1f);
            material.SetFloat("_AlbedoBoost", isNorwalk ? 1f : 0.96f);
            material.SetFloat("_EnvironmentDim", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string RootPath(this Spec spec) => $"{Root}/{spec.Folder}";
        private static string ModelPath(this Spec spec) =>
            $"{spec.RootPath()}/Source/{spec.Model}";
        private static string PackagePath(this Spec spec) =>
            $"{spec.RootPath()}/{spec.Folder}Evaluation.asset";
        private static string PrefabPath(this Spec spec) =>
            $"{spec.RootPath()}/Prefabs/{spec.Folder}Evaluation.prefab";
        private static string MaterialPath(this Spec spec) =>
            $"{spec.RootPath()}/Materials/{spec.Folder}LocalBrownstone.mat";
        private static bool UsesLocalBrownstoneMaterial(this Spec spec) =>
            spec.Folder.StartsWith("NY", StringComparison.Ordinal) ||
            spec.Folder == "NorwalkClockTower";
    }
}
