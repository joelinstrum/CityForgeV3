using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForgeV3.Buildings3D;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Editor
{
    public static class MixedUseBrickBuilding3DBuilder
    {
        public const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/Evaluation/MixedUseBrick";
        private const string AssetId = "mixed-use-brick-eval-v01";
        private const string SourceModel = Root +
            "/Source/tripo_convert_c4f7afa3-bb8e-499d-9766-3880c50e7e58.fbx";
        private const string PackagePath = Root + "/MixedUseBrickEvaluation.asset";
        private const string PrefabPath = Root +
            "/Prefabs/MixedUseBrickEvaluation.prefab";
        private const string BillboardPrefab = Root +
            "/LOD5/MixedUseBrickEightAngle.prefab";
        private const float TargetHeightMeters = 18f;
        private const float BillboardCanvasLocal = 1.1777f;
        private const float BillboardCenterHeightLocal = 0.36975f;

        private static readonly string[] Models =
        {
            SourceModel,
            Root + "/LOD1/MixedUseBrick_LOD1.fbx",
            Root + "/LOD2/MixedUseBrick_LOD2.fbx",
            Root + "/LOD3/MixedUseBrick_LOD3.fbx",
            Root + "/LOD4/MixedUseBrick_LOD4.fbx"
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

        [MenuItem("City Forge/3D Buildings/Create Mixed Use Brick Evaluation Package")]
        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CreateOrUpdateBillboard();
            var models = Models.Select(
                AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
            var billboard = AssetDatabase.LoadAssetAtPath<GameObject>(
                BillboardPrefab);
            if (models.Any(model => model == null) || billboard == null)
                throw new FileNotFoundException(
                    "Mixed Use Brick LOD assets must import before packaging.");

            var lod0Metrics = CityForge.Editor.Building3DPackageValidator
                .Measure(models[0]);
            var scale = TargetHeightMeters / lod0Metrics.Bounds.size.y;
            var material = CreateOrUpdateMaterial();
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                PackagePath);
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, PackagePath);
            }
            package.SchemaVersion = Building3DPackage.CurrentSchemaVersion;
            package.AssetId = AssetId;
            package.SourceProvenance =
                "User-supplied Tripo mixed-use brick FBX preserved unchanged as LOD0; LOD1-LOD4 are UV-preserving derivatives; LOD5 contains eight neutral 45-degree renders.";
            package.AuthoredScale = Vector3.one * scale;
            package.PivotOffset = Vector3.zero;
            package.FrontYawDegrees = 90f;
            package.FootprintMeters = new Vector2(
                lod0Metrics.Bounds.size.x * scale,
                lod0Metrics.Bounds.size.z * scale);
            package.BoundsTolerance = 0.10f;
            package.UseCrossFade = true;
            package.CrossFadeWidth = 0.12f;
            package.KeepShadowMeshWithImpostor = true;
            var thresholds = new[] { 0.60f, 0.30f, 0.12f, 0.035f, 0.012f };
            package.Representations = new List<Building3DRepresentation>();
            for (var index = 0; index < models.Length; index++)
            {
                var metrics = CityForge.Editor.Building3DPackageValidator
                    .Measure(models[index]);
                package.Representations.Add(new Building3DRepresentation
                {
                    Level = (Building3DLevel)index,
                    ScreenRelativeHeight = thresholds[index],
                    VisualPrefab = models[index],
                    OverrideMaterial = material,
                    ShadowPrefab = models[Mathf.Max(index, 2)],
                    LocalPosition = new Vector3(-metrics.Bounds.center.x,
                        -metrics.Bounds.min.y, -metrics.Bounds.center.z),
                    LocalScale = Vector3.one,
                    TargetTriangleBudget = metrics.Triangles,
                    Provenance = index == 0
                        ? "Supplied textured FBX imported unchanged as the closest-range model."
                        : $"UV-preserving derivative of immutable LOD0 at " +
                          $"{new[] { 0.55f, 0.30f, 0.15f, 0.07f }[index - 1]:P0} ratio."
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
                    "Eight transparent 1024px neutral views rendered every 45 degrees from immutable LOD0."
            });
            EditorUtility.SetDirty(package);

            var root = new GameObject("Mixed Use Brick Evaluation V01");
            try
            {
                root.AddComponent<Building3DPackageInstance>().Configure(package);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log($"Mixed Use Brick package ready: " +
                $"{lod0Metrics.Triangles:N0} LOD0 triangles, " +
                $"{TargetHeightMeters:N1}m tall, six levels.");
        }

        private static void CreateOrUpdateBillboard()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(
                BillboardPrefab);
            if (existing != null) AssetDatabase.DeleteAsset(BillboardPrefab);
            var root = new GameObject("Mixed Use Brick LOD5 Eight Angle");
            try
            {
                var renderers = new Renderer[8];
                for (var index = 0; index < 8; index++)
                {
                    var degrees = index * 45;
                    var texturePath = $"{Root}/LOD5/mixed-use-brick-angle-" +
                        $"{index}-{degrees:000}-v01.png";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        texturePath);
                    if (texture == null) throw new FileNotFoundException(texturePath);
                    var materialPath = $"{Root}/LOD5/MixedUseBrick-LOD5-" +
                        $"{degrees:000}.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(
                        materialPath);
                    if (material == null)
                    {
                        material = new Material(Shader.Find("Sprites/Default"))
                        {
                            name = $"Mixed Use Brick LOD5 {degrees:000}"
                        };
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                    material.mainTexture = texture;
                    EditorUtility.SetDirty(material);
                    var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    card.name = $"Angle {index} ({degrees:000})";
                    UnityEngine.Object.DestroyImmediate(card.GetComponent<Collider>());
                    card.transform.SetParent(root.transform, false);
                    card.transform.localPosition = new Vector3(0f,
                        BillboardCenterHeightLocal, 0f);
                    card.transform.localScale = Vector3.one *
                        BillboardCanvasLocal;
                    renderers[index] = card.GetComponent<MeshRenderer>();
                    renderers[index].sharedMaterial = material;
                }
                root.AddComponent<EightAngleBuildingBillboard>().Configure(
                    renderers, 0f);
                PrefabUtility.SaveAsPrefabAsset(root, BillboardPrefab);
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
            var path = Root + "/Materials/MixedUseBrickPBR.mat";
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            if (shader == null) throw new MissingReferenceException(
                "CityForge 3D building PBR shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "Mixed Use Brick PBR" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_MainTex",
                AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            material.SetTexture("_BumpMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 0.75f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_GlossMapScale", 0.16f);
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
