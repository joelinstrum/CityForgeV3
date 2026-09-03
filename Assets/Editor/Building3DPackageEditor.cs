using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForgeV3.Buildings3D;
using UnityEditor;
using UnityEngine;

namespace CityForge.Editor
{
    [CustomEditor(typeof(Building3DPackage))]
    public sealed class Building3DPackageEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            var report = Building3DPackageValidator.Validate(
                (Building3DPackage)target);
            EditorGUILayout.LabelField("Package Validation", EditorStyles.boldLabel);
            if (report.Count == 0)
                EditorGUILayout.HelpBox("Package is internally consistent.",
                    MessageType.Info);
            else
                foreach (var issue in report)
                    EditorGUILayout.HelpBox(issue.Message, issue.Severity);
        }
    }

    public readonly struct Building3DValidationIssue
    {
        public readonly MessageType Severity;
        public readonly string Message;

        public Building3DValidationIssue(MessageType severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    public static class Building3DPackageValidator
    {
        public static List<Building3DValidationIssue> Validate(
            Building3DPackage package)
        {
            var issues = new List<Building3DValidationIssue>();
            if (package == null) return issues;
            if (package.SchemaVersion != Building3DPackage.CurrentSchemaVersion)
                issues.Add(Error($"Unsupported schema version {package.SchemaVersion}."));
            if (string.IsNullOrWhiteSpace(package.AssetId))
                issues.Add(Error("Stable asset ID is missing."));
            if (package.AuthoredScale.x <= 0f || package.AuthoredScale.y <= 0f ||
                package.AuthoredScale.z <= 0f)
                issues.Add(Error("Authored scale must be positive on every axis."));

            var entries = package.Representations ?? new List<Building3DRepresentation>();
            foreach (Building3DLevel level in System.Enum.GetValues(typeof(Building3DLevel)))
                if (!entries.Any(entry => entry != null && entry.Level == level &&
                    entry.VisualPrefab != null))
                    issues.Add(Warning($"{level} authored representation is missing."));

            var populated = entries.Where(entry => entry?.VisualPrefab != null).ToList();
            for (var index = 1; index < populated.Count; index++)
                if (populated[index].ScreenRelativeHeight >=
                    populated[index - 1].ScreenRelativeHeight)
                    issues.Add(Error("LOD screen-relative heights must descend in package order."));

            Bounds? reference = null;
            HashSet<string> referenceMaterials = null;
            foreach (var entry in populated)
            {
                var metrics = Measure(entry.VisualPrefab);
                if (!metrics.HasBounds)
                {
                    issues.Add(Error($"{entry.Level} contains no renderers."));
                    continue;
                }
                // The billboard is an image-space representation with sprite
                // materials and card bounds; comparing either against source
                // geometry produces guaranteed false errors. Its view count
                // and assignment are validated independently at runtime/tests.
                if (entry.Level == Building3DLevel.LOD5Billboard)
                    continue;
                var normalizedBounds = ScaleBounds(metrics.Bounds,
                    entry.LocalPosition, entry.LocalScale);
                if (reference == null)
                {
                    reference = normalizedBounds;
                    referenceMaterials = metrics.MaterialNames;
                }
                else
                {
                    var sizeDelta = NormalizedDelta(reference.Value.size,
                        normalizedBounds.size);
                    if (sizeDelta > package.BoundsTolerance)
                        issues.Add(Error($"{entry.Level} bounds differ by {sizeDelta:P1}; " +
                            $"allowed tolerance is {package.BoundsTolerance:P1}."));
                    var centerDelta = Vector3.Distance(reference.Value.center,
                        normalizedBounds.center) / Mathf.Max(0.001f,
                        reference.Value.size.magnitude);
                    if (centerDelta > package.BoundsTolerance)
                        issues.Add(Error($"{entry.Level} pivot/bounds center differs by " +
                            $"{centerDelta:P1}."));
                    if (!referenceMaterials.SetEquals(metrics.MaterialNames))
                        issues.Add(Warning($"{entry.Level} material bindings differ from the first populated LOD."));
                }
            }
            return issues;
        }

        private static Bounds ScaleBounds(Bounds bounds, Vector3 position,
            Vector3 scale)
        {
            var absoluteScale = new Vector3(Mathf.Abs(scale.x),
                Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return new Bounds(position + Vector3.Scale(bounds.center, scale),
                Vector3.Scale(bounds.size, absoluteScale));
        }

        public static Building3DMetrics Measure(GameObject prefab)
        {
            var result = new Building3DMetrics();
            if (prefab == null) return result;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!result.HasBounds) { result.Bounds = renderer.bounds; result.HasBounds = true; }
                else result.Bounds.Encapsulate(renderer.bounds);
                result.RendererCount++;
                foreach (var material in renderer.sharedMaterials)
                    if (material != null) result.MaterialNames.Add(material.name);
                if (renderer is SkinnedMeshRenderer skinned)
                    AddMesh(skinned.sharedMesh, result);
                else if (renderer.TryGetComponent<MeshFilter>(out var filter))
                    AddMesh(filter.sharedMesh, result);
            }
            return result;
        }

        private static void AddMesh(Mesh mesh, Building3DMetrics result)
        {
            if (mesh == null) return;
            result.Vertices += mesh.vertexCount;
            result.Submeshes += mesh.subMeshCount;
            for (var index = 0; index < mesh.subMeshCount; index++)
                result.Triangles += (int)mesh.GetIndexCount(index) / 3;
        }

        private static float NormalizedDelta(Vector3 a, Vector3 b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x) / Mathf.Max(0.001f, a.x),
                Mathf.Abs(a.y - b.y) / Mathf.Max(0.001f, a.y),
                Mathf.Abs(a.z - b.z) / Mathf.Max(0.001f, a.z));

        private static Building3DValidationIssue Error(string message) =>
            new(MessageType.Error, message);
        private static Building3DValidationIssue Warning(string message) =>
            new(MessageType.Warning, message);
    }

    public sealed class Building3DMetrics
    {
        public bool HasBounds;
        public Bounds Bounds;
        public int Triangles;
        public int Vertices;
        public int RendererCount;
        public int Submeshes;
        public readonly HashSet<string> MaterialNames = new();
    }

    public static class BrownstoneBuilding3DPilotBuilder
    {
        public const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/BrownstoneProduction";
        public const string PackagePath = Root + "/BrownstoneProduction.asset";
        public const string PrefabPath = Root + "/Prefabs/BrownstoneProduction.prefab";
        private const string SourceModel =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/BrownstoneBuilding22k/brownstone-building-22k.fbx";

        [MenuItem("City Forge/3D Buildings/Create Brownstone Production Pilot")]
        public static void CreatePilotAssets()
        {
            EnsureFolder(Root);
            foreach (var folder in new[] { "Source", "LOD0", "LOD1", "LOD2",
                "LOD3", "Impostor", "Materials", "Textures", "Prefabs" })
                EnsureFolder(Root + "/" + folder);

            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(PackagePath);
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, PackagePath);
            }
            package.AssetId = "brownstone-production-v01";
            package.SourceProvenance =
                "Original Brownstone Building 22K FBX, preserved in place; no automatic reduction.";
            package.AuthoredScale = Vector3.one;
            package.PivotOffset = Vector3.zero;
            package.FrontYawDegrees = 90f;
            package.FootprintMeters = new Vector2(8.1f, 12.8f);
            package.UseCrossFade = true;
            package.CrossFadeWidth = 0.12f;
            package.KeepShadowMeshWithImpostor = true;
            package.Representations = new List<Building3DRepresentation>
            {
                new()
                {
                    Level = Building3DLevel.LOD2,
                    ScreenRelativeHeight = 0.12f,
                    VisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModel),
                    // The existing lot-level mesh-shadow system remains authoritative
                    // for this partial pilot. An independently authored cheaper caster
                    // belongs here once supplied; never duplicate the beauty mesh by
                    // pretending it is a shadow optimization.
                    ShadowPrefab = null,
                    TargetTriangleBudget = 20000,
                    Provenance = "Existing artist/source-supplied 22K pilot mesh; registered without modification."
                }
            };
            EditorUtility.SetDirty(package);

            var root = new GameObject("Brownstone Production V01");
            root.AddComponent<Building3DPackageInstance>().Configure(package);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created brownstone production LOD pilot. Missing authored levels remain validation warnings.");
        }

        public static void CreatePilotAssetsBatch()
        {
            CreatePilotAssets();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    public static class ArtMuseumBuilding3DPilotBuilder
    {
        public const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/ArtMuseumProduction";
        public const string PackagePath = Root + "/ArtMuseumProduction.asset";
        public const string PrefabPath = Root + "/Prefabs/ArtMuseumProduction.prefab";
        private static readonly string[] Models =
        {
            Root + "/LOD0/tripo_convert_62faad61-a4c2-4b5d-8e98-6e37c2503d3c.fbx",
            Root + "/LOD1/tripo_convert_329f9094-04e2-4822-a64b-684c00a2a566.fbx",
            Root + "/LOD2/tripo_convert_d916d3be-e4d5-4d8b-8960-a6b84cb68088.fbx",
            Root + "/LOD3/tripo_convert_97ec4b46-cb7c-406e-9521-06d153d423cb.fbx"
        };

        [MenuItem("City Forge/3D Buildings/Create Art Museum Production Pilot")]
        public static void CreatePilotAssets()
        {
            var visualModels = Models.Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .ToArray();
            if (visualModels.Any(model => model == null))
                throw new FileNotFoundException(
                    "All four imported Art Museum FBX assets must finish importing first.");

            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(PackagePath);
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, PackagePath);
            }
            package.AssetId = "art-museum-production-v01";
            package.SourceProvenance =
                "Four user-supplied Tripo FBX LOD archives imported unchanged from Source/; SHA-256 hashes recorded in documentation.";
            // Tripo exported this museum in normalized ~1 m bounds. CityForge
            // lots use real-world metres; 40x yields an approximately
            // 28 x 39 m footprint and 32 m architectural height.
            package.AuthoredScale = Vector3.one * 40f;
            package.PivotOffset = Vector3.zero;
            package.FrontYawDegrees = 90f;
            package.FootprintMeters = new Vector2(28.4f, 38.8f);
            package.BoundsTolerance = 0.05f;
            package.UseCrossFade = true;
            package.CrossFadeWidth = 0.12f;
            package.KeepShadowMeshWithImpostor = true;
            var thresholds = new[] { 0.60f, 0.30f, 0.12f, 0.035f };
            var budgets = new[] { 250000, 80000, 20000, 5000 };
            var materials = new Material[4];
            for (var index = 0; index < materials.Length; index++)
                materials[index] = CreateOrUpdateMuseumMaterial(index);
            package.Representations = new List<Building3DRepresentation>();
            for (var index = 0; index < visualModels.Length; index++)
                package.Representations.Add(new Building3DRepresentation
                {
                    Level = (Building3DLevel)index,
                    ScreenRelativeHeight = thresholds[index],
                    VisualPrefab = visualModels[index],
                    OverrideMaterial = materials[index],
                    ShadowPrefab = visualModels[index < 2 ? 2 : 3],
                    LocalScale = Vector3.one,
                    TargetTriangleBudget = budgets[index],
                    Provenance = $"User-supplied ArtMuseum-LOD{index}.zip; imported unchanged."
                });
            EditorUtility.SetDirty(package);

            var root = new GameObject("Art Museum Production V01");
            root.AddComponent<Building3DPackageInstance>().Configure(package);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var representation in package.Representations)
            {
                var metrics = Building3DPackageValidator.Measure(
                    representation.VisualPrefab);
                var mesh = representation.VisualPrefab
                    .GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
                Debug.Log($"Art Museum {representation.Level}: " +
                    $"{metrics.Triangles:N0} triangles, {metrics.Vertices:N0} vertices, " +
                    $"{metrics.RendererCount} renderers, {metrics.Submeshes} submeshes, " +
                    $"bounds {metrics.Bounds.size}, center {metrics.Bounds.center}, " +
                    $"UV0 {mesh?.uv?.Length ?? 0}, UV1 {mesh?.uv2?.Length ?? 0}.");
            }
            foreach (var issue in Building3DPackageValidator.Validate(package))
                Debug.Log($"Art Museum validation {issue.Severity}: {issue.Message}");
        }

        private static Material CreateOrUpdateMuseumMaterial(int level)
        {
            var folder = $"{Root}/LOD{level}";
            var baseColor = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .First(path => path.ToLowerInvariant().Contains("basecolor"));
            var normal = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .First(path => path.ToLowerInvariant().Contains("normal"));
            var materialPath = $"{Root}/Materials/ArtMuseum-LOD{level}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            if (shader == null)
                throw new MissingReferenceException(
                    "The CityForge texture-preserving building shader is required.");
            if (material == null)
            {
                material = new Material(shader) { name = $"Art Museum LOD{level}" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_MainTex",
                AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            material.SetTexture("_BumpMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 0.45f);
            // The supplied metallic textures average only 3% metallic. Unity's
            // embedded FBX material interpreted the Tripo channels incorrectly,
            // producing the black chrome appearance seen in Game view. Stone is
            // dielectric; keep metallic at zero and use restrained smoothness.
            material.SetTexture("_MetallicGlossMap", null);
            material.DisableKeyword("_METALLICGLOSSMAP");
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_GlossMapScale", 0.18f);
            // Keep the supplied atlas visibly legible under the fixed CityForge
            // lighting. This is local to the museum material: beige masonry,
            // blue-gray roofs, windows, and small accent colors remain distinct.
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Contrast", 1f);
            material.SetFloat("_Saturation", 1f);
            material.SetFloat("_AmbientFill", 1f);
            material.SetFloat("_AlbedoBoost", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }

    public static class IvyTownhouseWhiteBuilding3DBuilder
    {
        public const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/IvyTownhouseWhiteProduction";
        public const string PackagePath = Root + "/IvyTownhouseWhiteProduction.asset";
        public const string PrefabPath = Root + "/Prefabs/IvyTownhouseWhiteProduction.prefab";
        private const string LightingModel =
            Root + "/Lighting/IvyTownhouseWhite_WindowLighting.fbx";
        private const string BillboardPrefab =
            Root + "/LOD5/IvyTownhouseWhiteEightAngle.prefab";
        private static readonly string[] Models =
        {
            Root + "/LOD0/tripo_convert_2f4ab5a9-cd46-46da-a934-cc67c3d8554d.fbx",
            Root + "/LOD1/tripo_convert_4f48809e-b281-49d8-8d68-0622291ec34c.fbx",
            Root + "/LOD2/tripo_convert_ba319826-54ff-4db4-9f19-711a95fc2db7.fbx",
            Root + "/LOD3/tripo_convert_0a55ac9a-ff6d-4f3d-81f1-a7339d41ef58.fbx",
            Root + "/LOD4/CF_Building_IvyTownhouseWhite_01_LOD4_v01.fbx"
        };

        [InitializeOnLoadMethod]
        private static void QueueSixLevelEvaluationBuild()
        {
            EditorApplication.delayCall += () =>
            {
                var current = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                    PackagePath);
                if (current != null && current.SchemaVersion >= 2 &&
                    current.Representations?.Count >= 6) return;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Models[4]) == null)
                    return;
                CreatePackage();
                Debug.Log("Ivy Townhouse six-level evaluation package rebuilt.");
            };
        }

        [MenuItem("City Forge/3D Buildings/Create Ivy Townhouse White Package")]
        public static void CreatePackage()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CreateOrUpdateEightAngleBillboard();
            var visualModels = Models.Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .ToArray();
            var billboard = AssetDatabase.LoadAssetAtPath<GameObject>(BillboardPrefab);
            var lightingModel = AssetDatabase.LoadAssetAtPath<GameObject>(LightingModel);
            if (visualModels.Any(model => model == null) || billboard == null ||
                lightingModel == null)
                throw new FileNotFoundException(
                    "The five townhouse mesh LODs, billboard, and shared window overlay must import first.");

            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(PackagePath);
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, PackagePath);
            }
            package.AssetId = "ivy-townhouse-white-production-v01";
            package.SchemaVersion = Building3DPackage.CurrentSchemaVersion;
            package.SourceProvenance =
                "Four user-supplied IvyTownhouse LOD archives, a versioned 3,321-triangle LOD4 derivative, eight neutral 45-degree LOD5 views, and one 64-triangle window-light overlay.";
            // LOD0 imports at 0.73 m high. The reviewed Blender derivative is
            // calibrated to a 12 m townhouse, giving a uniform 16.44x runtime
            // scale. The proof panes were authored in that reviewed metre
            // space, so their one-time overlay receives the inverse 0.06x
            // local correction under the same package root.
            package.AuthoredScale = Vector3.one * 16.44f;
            package.PivotOffset = Vector3.zero;
            package.FrontYawDegrees = 90f;
            package.FootprintMeters = new Vector2(6.9f, 16.1f);
            package.BoundsTolerance = 0.08f;
            package.UseCrossFade = true;
            package.CrossFadeWidth = 0.12f;
            package.KeepShadowMeshWithImpostor = true;
            package.NightLightingPrefab = lightingModel;
            package.NightLightingMaterial = CreateOrUpdateNightMaterial();
            package.NightLightingLocalPosition = Vector3.zero;
            package.NightLightingLocalEulerAngles = Vector3.zero;
            package.NightLightingLocalScale = Vector3.one * 0.06f;

            var thresholds = new[] { 0.60f, 0.30f, 0.12f, 0.035f, 0.012f };
            var budgets = new[] { 250000, 80000, 20000, 5000, 3500 };
            package.Representations = new List<Building3DRepresentation>();
            for (var index = 0; index < visualModels.Length; index++)
                package.Representations.Add(new Building3DRepresentation
                {
                    Level = (Building3DLevel)index,
                    ScreenRelativeHeight = thresholds[index],
                    VisualPrefab = visualModels[index],
                    OverrideMaterial = CreateOrUpdateBuildingMaterial(
                        index < 4 ? index : 0),
                    ShadowPrefab = visualModels[index < 2 ? 2 :
                        index < 4 ? 3 : 4],
                    LocalScale = index == 4 ? Vector3.one * 0.06f : Vector3.one,
                    TargetTriangleBudget = budgets[index],
                    Provenance = index < 4
                        ? $"User-supplied IvyTownhouse-LOD{index}.zip; imported unchanged."
                        : "Versioned LOD0 derivative; 3,321 triangles with source UV and material identity preserved."
                });
            package.Representations.Add(new Building3DRepresentation
            {
                Level = Building3DLevel.LOD5Billboard,
                ScreenRelativeHeight = 0.002f,
                VisualPrefab = billboard,
                ShadowPrefab = visualModels[4],
                LocalScale = Vector3.one,
                TargetTriangleBudget = 16,
                BillboardAngleCount = 8,
                BillboardYawOffset = 0f,
                Provenance = "Eight transparent 512px evaluation views rendered every 45 degrees from the normalized LOD0 source."
            });
            EditorUtility.SetDirty(package);

            var root = new GameObject("Ivy Townhouse White Production V01");
            root.AddComponent<Building3DPackageInstance>().Configure(package);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var representation in package.Representations)
            {
                var metrics = Building3DPackageValidator.Measure(
                    representation.VisualPrefab);
                Debug.Log($"Ivy Townhouse {representation.Level}: " +
                    $"{metrics.Triangles:N0} triangles, bounds {metrics.Bounds.size}, " +
                    $"center {metrics.Bounds.center}.");
            }
        }

        private static void CreateOrUpdateEightAngleBillboard()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BillboardPrefab);
            if (existing != null) AssetDatabase.DeleteAsset(BillboardPrefab);
            var root = new GameObject("Ivy Townhouse LOD5 Eight Angle");
            try
            {
                const float canvasMeters = 18.5f;
                const float packageScale = 16.44f;
                // The render camera is centered on z=6m. Its 28m radius and
                // 5m rise project the foundation origin 5.906m below center.
                var localCanvas = canvasMeters / packageScale;
                var localCenterHeight = 5.906f / packageScale;
                for (var index = 0; index < 8; index++)
                {
                    var degrees = index * 45;
                    var texturePath = $"{Root}/LOD5/ivy-townhouse-lod5-angle-" +
                        $"{index}-{degrees:000}-v01.png";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (texture == null)
                        throw new FileNotFoundException(texturePath);
                    var materialPath = $"{Root}/LOD5/IvyTownhouse-LOD5-" +
                        $"{degrees:000}.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        material = new Material(Shader.Find("Sprites/Default"))
                        {
                            name = $"Ivy Townhouse LOD5 {degrees:000}"
                        };
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                    material.mainTexture = texture;
                    EditorUtility.SetDirty(material);

                    var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    card.name = $"Angle {index} ({degrees:000})";
                    Object.DestroyImmediate(card.GetComponent<Collider>());
                    card.transform.SetParent(root.transform, false);
                    card.transform.localPosition = new Vector3(0f,
                        localCenterHeight, 0f);
                    card.transform.localScale = Vector3.one * localCanvas;
                    card.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
                PrefabUtility.SaveAsPrefabAsset(root, BillboardPrefab);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }

        private static Material CreateOrUpdateBuildingMaterial(int level)
        {
            var folder = $"{Root}/LOD{level}";
            var texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var baseColor = texturePaths.First(path =>
                path.ToLowerInvariant().Contains("basecolor"));
            var normal = texturePaths.First(path =>
                path.ToLowerInvariant().Contains("normal"));
            var materialPath = $"{Root}/Materials/IvyTownhouseWhite-LOD{level}.mat";
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = $"Ivy Townhouse White LOD{level}" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 0.45f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_GlossMapScale", 0.16f);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Contrast", 1f);
            material.SetFloat("_Saturation", 1f);
            material.SetFloat("_AmbientFill", 1f);
            material.SetFloat("_AlbedoBoost", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateOrUpdateNightMaterial()
        {
            var path = Root + "/Materials/IvyTownhouseWhite-WindowEmission.mat";
            var shader = Shader.Find("CityForgeV3/BuildingWindowEmission");
            if (shader == null)
                throw new MissingReferenceException("Building window emission shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "Ivy Townhouse Warm Windows" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_EmissionColor", new Color(1f, 0.38f, 0.08f, 1f));
            material.SetFloat("_EmissionStrength", 2.5f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }

    public static class PlymouthStoreBuilding3DBuilder
    {
        public const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/PlymouthStoreProduction";
        public const string PackagePath = Root + "/PlymouthStoreProduction.asset";
        public const string PrefabPath = Root + "/Prefabs/PlymouthStoreProduction.prefab";

        // Legacy independently-authored package builder retained only for
        // source-history readability; it is intentionally not exposed.
        public static void CreatePackage()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var models = Enumerable.Range(0, 4).Select(level =>
            {
                var guids = AssetDatabase.FindAssets("t:Model", new[]
                    { $"{Root}/LOD{level}" });
                if (guids.Length != 1)
                    throw new FileNotFoundException(
                        $"Plymouth Store LOD{level} requires exactly one FBX model.");
                return AssetDatabase.LoadAssetAtPath<GameObject>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }).ToArray();

            var metricsByLevel = models.Select(Building3DPackageValidator.Measure)
                .ToArray();
            var lod0Metrics = metricsByLevel[0];
            var scale = 12f / Mathf.Max(0.01f, lod0Metrics.Bounds.size.y);
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(PackagePath);
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, PackagePath);
            }
            package.AssetId = "plymouth-store-production-v01";
            package.SourceProvenance =
                "Four user-supplied PlymouthStore LOD archives. Night Blender source and eight-view night billboards are preserved for the lighting/impostor pass.";
            package.AuthoredScale = Vector3.one * scale;
            package.PivotOffset = Vector3.zero;
            package.FrontYawDegrees = 90f;
            package.FootprintMeters = new Vector2(
                lod0Metrics.Bounds.size.x * scale,
                lod0Metrics.Bounds.size.z * scale);
            package.BoundsTolerance = 0.08f;
            // These independently exported FBXs are stable when forced one at
            // a time, but their generated materials do not implement Unity's
            // LOD crossfade contract. A discrete switch prevents the third
            // zoom from showing a misleading blended/tipped intermediate.
            package.UseCrossFade = false;
            package.CrossFadeWidth = 0f;
            package.KeepShadowMeshWithImpostor = true;
            package.NightLightingPrefab = null;
            package.NightLightingMaterial = null;

            var thresholds = new[] { 0.60f, 0.30f, 0.12f, 0.035f };
            var budgets = new[] { 250000, 80000, 20000, 5000 };
            package.Representations = new List<Building3DRepresentation>();
            for (var index = 0; index < models.Length; index++)
                package.Representations.Add(new Building3DRepresentation
                {
                    Level = (Building3DLevel)index,
                    ScreenRelativeHeight = thresholds[index],
                    VisualPrefab = models[index],
                    OverrideMaterial = CreateOrUpdateMaterial(index),
                    // LOD0 is in Tripo source units while LOD1-3 were exported
                    // in metres. Normalize every representation to LOD0 before
                    // the shared package scale is applied.
                    ShadowPrefab = null,
                    // The runtime package root is rotated -90 degrees around
                    // X, so a source-space Z half-turn is the world-facing
                    // yaw correction for the two reversed exports.
                    LocalEulerAngles = index >= 2
                        ? new Vector3(0f, 0f, 180f)
                        : Vector3.zero,
                    LocalScale = Vector3.one *
                        (lod0Metrics.Bounds.size.y /
                         Mathf.Max(0.01f, metricsByLevel[index].Bounds.size.y)),
                    TargetTriangleBudget = budgets[index],
                    Provenance =
                        $"User-supplied PlymouthStore-LOD{index}.zip; imported unchanged."
                });
            EditorUtility.SetDirty(package);

            var root = new GameObject("Plymouth Store Production V01");
            root.AddComponent<Building3DPackageInstance>().Configure(package);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            for (var index = 0; index < models.Length; index++)
            {
                var metrics = metricsByLevel[index];
                var localScale = package.Representations[index].LocalScale.x;
                Debug.Log($"Plymouth Store LOD{index}: {metrics.Triangles:N0} " +
                          $"triangles, bounds {metrics.Bounds.size}, " +
                          $"center {metrics.Bounds.center}, " +
                          $"local scale {localScale:F4}, package scale {scale:F3}.");
            }
            foreach (var issue in Building3DPackageValidator.Validate(package))
                Debug.Log($"Plymouth package validation: {issue.Message}");
        }

        private static Material CreateOrUpdateMaterial(int level)
        {
            var folder = $"{Root}/LOD{level}";
            var texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var baseColor = texturePaths.First(path =>
                path.ToLowerInvariant().Contains("basecolor"));
            var normal = texturePaths.First(path =>
                path.ToLowerInvariant().Contains("normal"));
            var materialPath = $"{Root}/Materials/PlymouthStore-LOD{level}.mat";
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = $"Plymouth Store LOD{level}" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_MainTex",
                AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            material.SetTexture("_BumpMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 0.45f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_GlossMapScale", 0.16f);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Contrast", 1f);
            material.SetFloat("_Saturation", 1f);
            material.SetFloat("_AmbientFill", 1f);
            material.SetFloat("_AlbedoBoost", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }

    public static class PlymouthStoreComparisonBuilding3DBuilder
    {
        public const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/PlymouthStoreProduction";
        public const string PackagePath = Root + "/PlymouthStoreComparisonV01.asset";
        public const string PrefabPath = Root + "/Prefabs/PlymouthStoreComparisonV01.prefab";

        [MenuItem("City Forge/3D Buildings/Create Plymouth Store Package")]
        public static void CreatePackage()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Prefabs");
            var models = Enumerable.Range(0, 4).Select(level =>
            {
                var guids = AssetDatabase.FindAssets("t:Model", new[] { $"{Root}/LOD{level}" });
                if (guids.Length != 1)
                    throw new FileNotFoundException(
                        $"Plymouth comparison LOD{level} requires exactly one FBX model.");
                return AssetDatabase.LoadAssetAtPath<GameObject>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }).ToArray();
            var metrics = models.Select(Building3DPackageValidator.Measure).ToArray();
            var lod0 = metrics[0];
            var scale = 12f / Mathf.Max(0.01f, lod0.Bounds.size.y);
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(PackagePath);
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, PackagePath);
            }
            package.AssetId = "plymouth-store-v01";
            package.SourceProvenance =
                "Single immutable Tripo LOD0; LOD1-3 generated in one Blender scene at 60%, 30%, and 12.5%, with shared origin, transforms, materials, and exporter.";
            package.AuthoredScale = Vector3.one * scale;
            package.PivotOffset = Vector3.zero;
            package.FrontYawDegrees = 90f;
            package.FootprintMeters = new Vector2(
                lod0.Bounds.size.x * scale, lod0.Bounds.size.z * scale);
            package.BoundsTolerance = 0.01f;
            package.UseCrossFade = false;
            package.CrossFadeWidth = 0f;
            package.KeepShadowMeshWithImpostor = true;
            package.NightLightingPrefab = null;
            package.NightLightingMaterial = null;
            var thresholds = new[] { 0.60f, 0.30f, 0.12f, 0.035f };
            var budgets = new[] { 87701, 52619, 26309, 10962 };
            package.Representations = new List<Building3DRepresentation>();
            for (var index = 0; index < models.Length; index++)
                package.Representations.Add(new Building3DRepresentation
                {
                    Level = (Building3DLevel)index,
                    ScreenRelativeHeight = thresholds[index],
                    VisualPrefab = models[index],
                    OverrideMaterial = CreateOrUpdateMaterial(index),
                    ShadowPrefab = null,
                    LocalPosition = Vector3.zero,
                    LocalEulerAngles = Vector3.zero,
                    LocalScale = Vector3.one,
                    TargetTriangleBudget = budgets[index],
                    Provenance = "Derived from the same centered canonical LOD0 by the comparison pipeline."
                });
            EditorUtility.SetDirty(package);
            var root = new GameObject("Plymouth Store Comparison V01");
            root.AddComponent<Building3DPackageInstance>().Configure(package);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            for (var index = 0; index < metrics.Length; index++)
                Debug.Log($"Plymouth comparison LOD{index}: {metrics[index].Triangles:N0} " +
                          $"triangles, bounds {metrics[index].Bounds.size}, " +
                          $"center {metrics[index].Bounds.center}.");
            foreach (var issue in Building3DPackageValidator.Validate(package))
                Debug.Log($"Plymouth comparison validation: {issue.Message}");
        }

        private static Material CreateOrUpdateMaterial(int level)
        {
            var folder = $"{Root}/LOD{level}";
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var baseColor = paths.First(path => path.ToLowerInvariant().Contains("basecolor"));
            var normal = paths.First(path => path.ToLowerInvariant().Contains("normal"));
            var materialPath = $"{Root}/Materials/PlymouthStoreComparison-LOD{level}.mat";
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = $"Plymouth Store Comparison LOD{level}" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 0.45f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_GlossMapScale", 0.16f);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Contrast", 1f);
            material.SetFloat("_Saturation", 1f);
            material.SetFloat("_AmbientFill", 1f);
            material.SetFloat("_AlbedoBoost", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(path) && !string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, name);
        }
    }

    public static class GildedAgeMansionBuilding3DBuilder
    {
        public const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/GildedAgeMansionProduction";
        public const string PackagePath = Root + "/GildedAgeMansionV01.asset";
        public const string PrefabPath = Root + "/Prefabs/GildedAgeMansionV01.prefab";
        private const string LightingModel =
            Root + "/NightLighting/GildedAgeMansion_WindowLighting.fbx";
        private const string EmissionMask =
            Root + "/NightLighting/GildedAgeMansion_WindowEmissionMask.png";
        private const string WindowAnchors =
            Root + "/Source/window-light-anchors.json";

        [System.Serializable]
        private sealed class WindowAnchorFile
        {
            public WindowAnchorRecord[] anchors;
        }

        [System.Serializable]
        private sealed class WindowAnchorRecord
        {
            public string id;
            public float[] position;
            public float[] outwardNormal;
        }

        [MenuItem("City Forge/3D Buildings/Create Gilded Age Mansion Package")]
        public static void CreatePackage()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Prefabs");
            var models = Enumerable.Range(0, 4).Select(level =>
            {
                var guids = AssetDatabase.FindAssets("t:Model", new[] { $"{Root}/LOD{level}" });
                if (guids.Length != 1)
                    throw new FileNotFoundException(
                        $"Gilded Age Mansion LOD{level} requires exactly one FBX model.");
                return AssetDatabase.LoadAssetAtPath<GameObject>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }).ToArray();
            var metrics = models.Select(Building3DPackageValidator.Measure).ToArray();
            var lod0 = metrics[0];
            // Blender exports metres through FBX centimetres. Unity's model
            // asset reports metre-sized editor bounds, but the nested prefab
            // mesh is authored at 1/100 scale. Preserve the pipeline's shared
            // 100x package conversion (the same convention used by Plymouth).
            const float scale = 100f;
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(PackagePath);
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, PackagePath);
            }
            package.AssetId = "gilded-age-mansion-v01";
            package.SourceProvenance =
                "Immutable Tripo LOD0; LOD1-3 generated together at 60%, 30%, and 12.5%. Window discovery candidates are preserved for the reviewed night-lighting pass.";
            package.AuthoredScale = Vector3.one * scale;
            package.PivotOffset = Vector3.zero;
            package.FrontYawDegrees = 90f;
            package.FootprintMeters = new Vector2(
                lod0.Bounds.size.x, lod0.Bounds.size.z);
            package.BoundsTolerance = 0.01f;
            package.UseCrossFade = false;
            package.CrossFadeWidth = 0f;
            package.KeepShadowMeshWithImpostor = true;
            // The extracted-triangle prototype produced façade and roof
            // artifacts. Night lighting now comes from BuildingNightLighting,
            // an artist-authored emission mask, and manually placed anchors.
            package.NightLightingPrefab = null;
            package.NightLightingMaterial = null;
            package.NightLightingLocalPosition = Vector3.zero;
            package.NightLightingLocalEulerAngles = Vector3.zero;
            package.NightLightingLocalScale = Vector3.one;
            var thresholds = new[] { 0.60f, 0.30f, 0.12f, 0.035f };
            var budgets = new[] { 180655, 108393, 54196, 22580 };
            package.Representations = new List<Building3DRepresentation>();
            for (var index = 0; index < models.Length; index++)
                package.Representations.Add(new Building3DRepresentation
                {
                    Level = (Building3DLevel)index,
                    ScreenRelativeHeight = thresholds[index],
                    VisualPrefab = models[index],
                    OverrideMaterial = CreateOrUpdateMaterial(index),
                    ShadowPrefab = null,
                    LocalPosition = Vector3.zero,
                    LocalEulerAngles = Vector3.zero,
                    LocalScale = Vector3.one,
                    TargetTriangleBudget = budgets[index],
                    Provenance = "Derived from the same centered canonical LOD0."
                });
            EditorUtility.SetDirty(package);
            var editingExistingPrefab = File.Exists(PrefabPath);
            var root = editingExistingPrefab
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject("Gilded Age Mansion V01");
            try
            {
                if (root.GetComponent<BuildingNightLighting>() == null)
                    root.AddComponent<BuildingNightLighting>();
                ConfigureNightLighting(root);
                var instance = root.GetComponent<Building3DPackageInstance>();
                if (instance == null)
                    instance = root.AddComponent<Building3DPackageInstance>();
                instance.Configure(package);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                if (editingExistingPrefab)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            foreach (var issue in Building3DPackageValidator.Validate(package))
                Debug.Log($"Gilded Age Mansion validation: {issue.Message}");
        }

        private static void ConfigureNightLighting(GameObject root)
        {
            var maskImporter = AssetImporter.GetAtPath(EmissionMask) as TextureImporter;
            if (maskImporter != null && maskImporter.sRGBTexture)
            {
                maskImporter.sRGBTexture = false;
                maskImporter.SaveAndReimport();
            }
            var controller = root.GetComponent<BuildingNightLighting>();
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionMask);
            if (mask == null)
                throw new FileNotFoundException(
                    "The reviewed mansion window-emission mask must import first.");
            controller.ConfigureEmissionMask(mask);

            var source = JsonUtility.FromJson<WindowAnchorFile>(
                File.ReadAllText(WindowAnchors));
            var windowTransforms = new List<Transform>();
            foreach (var record in source.anchors)
            {
                var anchor = FindOrCreateAnchor(root.transform,
                    $"WindowLight_{record.id}");
                var blenderPosition = new Vector3(
                    record.position[0], record.position[1], record.position[2]);
                var blenderNormal = new Vector3(
                    record.outwardNormal[0], record.outwardNormal[1],
                    record.outwardNormal[2]);
                var unityPosition = new Vector3(
                    blenderPosition.x, blenderPosition.z, -blenderPosition.y);
                var unityNormal = new Vector3(
                    blenderNormal.x, blenderNormal.z, -blenderNormal.y).normalized;
                anchor.localPosition = unityPosition + unityNormal * 0.16f;
                windowTransforms.Add(anchor);
            }

            // The source atlas has two real wall sconces beside the main
            // entrance. These are reviewed authoring coordinates, not runtime
            // geometry detection.
            var lampTransforms = new List<Transform>
            {
                ConfigureAnchor(root.transform, "ExteriorLamp_001",
                    new Vector3(-1.65f, 3.25f, 6.28f)),
                ConfigureAnchor(root.transform, "ExteriorLamp_002",
                    new Vector3(1.65f, 3.25f, 6.28f))
            };
            controller.ConfigureAnchors(windowTransforms, lampTransforms);
            controller.ConfigureTuning(2.5f, 2.5f, 3f, 4f, 5f);
            EditorUtility.SetDirty(controller);
        }

        private static Transform FindOrCreateAnchor(Transform root, string name)
        {
            var existing = root.Find(name);
            if (existing != null) return existing;
            var anchor = new GameObject(name).transform;
            anchor.SetParent(root, false);
            return anchor;
        }

        private static Transform ConfigureAnchor(Transform root, string name,
            Vector3 localPosition)
        {
            var anchor = FindOrCreateAnchor(root, name);
            anchor.localPosition = localPosition;
            return anchor;
        }

        private static Material CreateOrUpdateMaterial(int level)
        {
            var folder = $"{Root}/LOD{level}";
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var baseColor = paths.First(path => path.ToLowerInvariant().Contains("basecolor"));
            var normal = paths.First(path => path.ToLowerInvariant().Contains("normal"));
            var materialPath = $"{Root}/Materials/GildedAgeMansion-LOD{level}.mat";
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = $"Gilded Age Mansion LOD{level}" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 0.45f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_GlossMapScale", 0.16f);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Contrast", 1f);
            material.SetFloat("_Saturation", 1f);
            material.SetFloat("_AmbientFill", 1f);
            material.SetFloat("_AlbedoBoost", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateOrUpdateNightMaterial()
        {
            var path = Root + "/Materials/GildedAgeMansion-WindowEmission.mat";
            var texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/LOD0" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var baseColorPath = texturePaths.FirstOrDefault(candidate =>
                candidate.ToLowerInvariant().Contains("basecolor"));
            if (string.IsNullOrEmpty(baseColorPath))
                throw new FileNotFoundException(
                    "The Gilded Age Mansion base-color texture is required for detailed window lighting.");
            var shader = Shader.Find("CityForgeV3/BuildingWindowEmission");
            if (shader == null)
                throw new MissingReferenceException(
                    "Building window emission shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader)
                    { name = "Gilded Age Mansion Warm Windows" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_MainTex",
                AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath));
            material.SetColor("_EmissionColor", new Color(1f, 0.68f, 0.34f, 1f));
            material.SetFloat("_EmissionStrength", 1.65f);
            material.SetFloat("_ArtworkInfluence", 0.92f);
            material.SetFloat("_WarmPixelThreshold", 0.08f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(path) && !string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
