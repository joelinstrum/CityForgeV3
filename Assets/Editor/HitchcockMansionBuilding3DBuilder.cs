using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForgeV3.Buildings3D;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Editor
{
    public static class HitchcockMansionBuilding3DBuilder
    {
        public const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/HitchcockMansionProduction";
        public const string PackagePath = Root + "/HitchcockMansionV01.asset";
        public const string PrefabPath = Root + "/Prefabs/HitchcockMansionV01.prefab";
        private const string BillboardPrefab = Root +
            "/LOD5/HitchcockMansionEightAngle.prefab";
        private const string CollisionPrefab = Root +
            "/Prefabs/HitchcockMansionCollision.prefab";
        private const float TargetHeightMeters = 17f;
        private const float BillboardCanvasLocal = 1.36354f;
        private const float BillboardCenterHeightLocal = 0.3752f;

        private static readonly string[] Models =
        {
            Root + "/Source/tripo_convert_2d190be9-ee5d-4766-b295-7a6beeb3eb9d.fbx",
            Root + "/LOD1/HitchcockMansion_LOD1.fbx",
            Root + "/LOD2/HitchcockMansion_LOD2.fbx",
            Root + "/LOD3/HitchcockMansion_LOD3.fbx",
            Root + "/LOD4/HitchcockMansion_LOD4.fbx"
        };

        [InitializeOnLoadMethod]
        private static void QueueBuild()
        {
            EditorApplication.delayCall += () =>
            {
                var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                    PackagePath);
                if (package?.Representations?.Count >= 6) return;
                if (Models.Any(path =>
                        AssetDatabase.LoadAssetAtPath<GameObject>(path) == null))
                    return;
                Build();
            };
        }

        [MenuItem("City Forge/3D Buildings/Create Hitchcock Mansion Package")]
        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CreateOrUpdateBillboard();
            var models = Models.Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .ToArray();
            var billboard = AssetDatabase.LoadAssetAtPath<GameObject>(BillboardPrefab);
            if (models.Any(model => model == null) || billboard == null)
                throw new FileNotFoundException(
                    "Hitchcock Mansion LOD assets must import before packaging.");

            var metrics = models.Select(
                CityForge.Editor.Building3DPackageValidator.Measure).ToArray();
            var runtimeScale = TargetHeightMeters / metrics[0].Bounds.size.y;
            var material = CreateOrUpdateMaterial();
            var collision = CreateOrUpdateCollision(metrics[0]);
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(PackagePath);
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, PackagePath);
            }
            package.SchemaVersion = Building3DPackage.CurrentSchemaVersion;
            package.AssetId = "hitchcock-mansion-v01";
            package.SourceProvenance =
                "Immutable supplied Tripo FBX retained as LOD0; LOD1-LOD4 are visually reviewed UV-preserving derivatives; LOD5 contains eight neutral 45-degree renders.";
            package.AuthoredScale = Vector3.one * runtimeScale;
            package.PivotOffset = Vector3.zero;
            package.FrontYawDegrees = 90f;
            package.FootprintMeters = new Vector2(
                metrics[0].Bounds.size.x * runtimeScale,
                metrics[0].Bounds.size.z * runtimeScale);
            package.BoundsTolerance = 0.08f;
            package.UseCrossFade = true;
            package.CrossFadeWidth = 0.12f;
            package.KeepShadowMeshWithImpostor = true;
            package.CollisionPrefab = collision;

            var thresholds = new[] { 0.60f, 0.30f, 0.12f, 0.035f, 0.012f };
            package.Representations = new List<Building3DRepresentation>();
            for (var index = 0; index < models.Length; index++)
            {
                package.Representations.Add(new Building3DRepresentation
                {
                    Level = (Building3DLevel)index,
                    ScreenRelativeHeight = thresholds[index],
                    VisualPrefab = models[index],
                    OverrideMaterial = material,
                    ShadowPrefab = models[Mathf.Max(index, 2)],
                    LocalPosition = new Vector3(-metrics[index].Bounds.center.x,
                        -metrics[index].Bounds.min.y, -metrics[index].Bounds.center.z),
                    LocalScale = Vector3.one,
                    TargetTriangleBudget = metrics[index].Triangles,
                    Provenance = index == 0
                        ? "Original user-supplied model; source bytes unchanged."
                        : "UV-preserving derivative accepted after silhouette review."
                });
            }
            package.Representations.Add(new Building3DRepresentation
            {
                Level = Building3DLevel.LOD5Billboard,
                ScreenRelativeHeight = 0.002f,
                VisualPrefab = billboard,
                ShadowPrefab = models[4],
                TargetTriangleBudget = 16,
                BillboardAngleCount = 8,
                BillboardYawOffset = 0f,
                Provenance =
                    "Eight transparent 1024px neutral views at 45-degree intervals; rendered from LOD2 with a shared camera and origin."
            });
            EditorUtility.SetDirty(package);

            var root = new GameObject("Hitchcock Mansion V01");
            try
            {
                root.AddComponent<Building3DPackageInstance>().Configure(package);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            foreach (var issue in CityForge.Editor.Building3DPackageValidator
                         .Validate(package))
                Debug.LogWarning($"Hitchcock Mansion package: {issue.Message}");
            Debug.Log($"Hitchcock Mansion package ready: {metrics[0].Triangles:N0} → " +
                $"{metrics[4].Triangles:N0} triangles, six levels, 8 billboard angles.");
        }

        private static void CreateOrUpdateBillboard()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BillboardPrefab);
            if (existing != null) AssetDatabase.DeleteAsset(BillboardPrefab);
            var root = new GameObject("Hitchcock Mansion LOD5 Eight Angle");
            try
            {
                var renderers = new Renderer[8];
                for (var index = 0; index < 8; index++)
                {
                    var degrees = index * 45;
                    var texturePath = $"{Root}/LOD5/hitchcock-mansion-angle-" +
                        $"{index}-{degrees:000}-v01.png";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (texture == null) throw new FileNotFoundException(texturePath);
                    var materialPath = $"{Root}/LOD5/HitchcockMansion-LOD5-" +
                        $"{degrees:000}.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        material = new Material(Shader.Find("Sprites/Default"))
                        {
                            name = $"Hitchcock Mansion LOD5 {degrees:000}"
                        };
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                    material.mainTexture = texture;
                    EditorUtility.SetDirty(material);
                    var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    card.name = $"Angle {index} ({degrees:000})";
                    UnityEngine.Object.DestroyImmediate(card.GetComponent<Collider>());
                    card.transform.SetParent(root.transform, false);
                    card.transform.localPosition = new Vector3(
                        0f, BillboardCenterHeightLocal, 0f);
                    card.transform.localScale = Vector3.one * BillboardCanvasLocal;
                    renderers[index] = card.GetComponent<MeshRenderer>();
                    renderers[index].sharedMaterial = material;
                }
                root.AddComponent<EightAngleBuildingBillboard>().Configure(renderers, 0f);
                PrefabUtility.SaveAsPrefabAsset(root, BillboardPrefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject CreateOrUpdateCollision(
            CityForge.Editor.Building3DMetrics metrics)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CollisionPrefab);
            if (existing != null) AssetDatabase.DeleteAsset(CollisionPrefab);
            var root = new GameObject("Hitchcock Mansion Simple Collision");
            try
            {
                var collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(metrics.Bounds.center.x,
                    metrics.Bounds.min.y + metrics.Bounds.size.y * 0.5f,
                    metrics.Bounds.center.z);
                collider.size = metrics.Bounds.size;
                return PrefabUtility.SaveAsPrefabAsset(root, CollisionPrefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Material CreateOrUpdateMaterial()
        {
            var texturePaths = AssetDatabase.FindAssets("t:Texture2D",
                    new[] { Root + "/Source" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var baseColor = texturePaths.First(path =>
                path.IndexOf("basecolor", StringComparison.OrdinalIgnoreCase) >= 0);
            var normal = texturePaths.First(path =>
                path.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0);
            var path = Root + "/Materials/HitchcockMansionPBR.mat";
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            if (shader == null) throw new MissingReferenceException(
                "City Forge 3D building PBR shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "Hitchcock Mansion PBR" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_MainTex",
                AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            material.SetTexture("_BumpMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 0.72f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_GlossMapScale", 0.18f);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Contrast", 1f);
            material.SetFloat("_Saturation", 1f);
            material.SetFloat("_Vibrance", 0f);
            material.SetFloat("_AmbientFill", 1f);
            material.SetFloat("_AlbedoBoost", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
