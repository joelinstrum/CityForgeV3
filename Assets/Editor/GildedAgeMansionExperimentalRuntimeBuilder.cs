using System.Collections.Generic;
using CityForgeV3.Buildings3D;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Editor
{
    public static class GildedAgeMansionExperimentalRuntimeBuilder
    {
        private const string ProductionRoot =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/GildedAgeMansionProduction";
        private const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/GildedAgeMansionExperimental";
        private const string PackagePath = Root + "/GildedAgeMansionExpV01.asset";
        private const string PrefabPath = Root + "/Prefabs/GildedAgeMansionExpV01.prefab";
        private const string EmissionMaskPath = ProductionRoot +
            "/NightLighting/GildedAgeMansion_WindowEmissionMask.png";

        private readonly struct AuthoredLight
        {
            public AuthoredLight(string name, float x, float y, float z)
            {
                Name = name;
                BlenderPosition = new Vector3(x, y, z);
            }

            public string Name { get; }
            public Vector3 BlenderPosition { get; }
        }

        private static readonly AuthoredLight[] WindowLights =
        {
            new("Area", 6.892226f, 1.681615f, 4.478419f),
            new("CF_WindowLight_Front_01", 6.892226f, 1.681615f, 7.478419f),
            new("CF_WindowLight_Front_02", 6.892226f, 1.681615f, 10.478419f),
            new("CF_WindowLight_Front_03", 6.892226f, -1.268385f, 7.478419f),
            new("CF_WindowLight_Front_04", 6.892226f, -1.268385f, 10.478419f),
            new("CF_WindowLight_Front_05", 7.09f, 0f, 13.2f),
            new("CF_WindowLight_Side_01", 1.48f, -6.892226f, 4.478419f),
            new("CF_WindowLight_Side_02", 4.18f, -6.892226f, 7.478419f),
            new("CF_WindowLight_Side_03", 1.48f, -6.892226f, 10.478419f),
            new("CF_WindowLight_Side_04", -1.2f, -6.892226f, 7.478419f),
            new("CF_WindowLight_Side_05", 4.18f, -6.892226f, 10.478419f),
            new("CF_WindowLight_BayFront_01", 6.9f, -4.7f, 4.478419f),
            new("CF_WindowLight_BayFront_02", 6.9f, -4.7f, 10.478419f),
            new("CF_WindowLight_BayFront_03", 6.48f, -5.39f, 7.478419f),
            new("CF_WindowLight_BayFront_04", 6.48f, -4.01f, 4.478419f),
            new("CF_WindowLight_BayFront_05", 6.48f, -4.01f, 10.478419f),
            new("CF_WindowLight_BaySide_01", -4.7f, -6.9f, 7.478419f),
            new("CF_WindowLight_BaySide_02", -4.7f, -6.9f, 10.478419f),
            new("CF_WindowLight_BaySide_03", -5.39f, -6.48f, 4.478419f),
            new("CF_WindowLight_BaySide_04", -5.39f, -6.48f, 10.478419f),
            new("CF_WindowLight_BaySide_05", -4.01f, -6.48f, 7.478419f),
        };

        private static readonly AuthoredLight[] ExteriorLights =
        {
            new("CF_ExteriorLamp_01", 6.72f, -3.25f, 5.55f),
            new("CF_ExteriorLamp_02", 6.72f, -0.15f, 5.55f),
        };

        [MenuItem("City Forge/3D Buildings/Create Exp. Gilded Age Mansion Package")]
        public static void CreatePackage()
        {
            EnsureFolder(Root);
            EnsureFolder(Root + "/Prefabs");
            var source = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                ProductionRoot + "/GildedAgeMansionV01.asset");
            if (source == null)
                throw new MissingReferenceException("Production Gilded Age Mansion package is missing.");

            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(PackagePath);
            if (package == null)
            {
                package = ScriptableObject.CreateInstance<Building3DPackage>();
                AssetDatabase.CreateAsset(package, PackagePath);
            }
            CopyPackage(source, package);
            package.AssetId = "gilded-age-mansion-exp-v01";
            package.SourceProvenance =
                "Non-overwriting runtime derivative of GildedAgeMansionV01. " +
                "Window and entrance anchors are transported from " +
                "CF_Building_GildedAgeMansion_01_night_lighting_v03.blend; " +
                "realtime Area lights are represented by budget-conscious Unity point spill lights.";
            EditorUtility.SetDirty(package);

            var root = new GameObject("Exp. Gilded Age Mansion V01");
            try
            {
                var controller = root.AddComponent<BuildingNightLighting>();
                controller.ConfigureEmissionMask(
                    AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionMaskPath));
                var windows = CreateAnchors(root.transform, WindowLights);
                var lamps = CreateAnchors(root.transform, ExteriorLights);
                controller.ConfigureAnchors(windows, lamps);
                controller.ConfigureTuning(2.5f, 2.5f, 3f, 4f, 5f);
                controller.ConfigureOccupancy(1f, false);
                root.AddComponent<Building3DPackageInstance>().Configure(package);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created Exp. Gilded Age Mansion: {PrefabPath} with " +
                      $"{WindowLights.Length} window and {ExteriorLights.Length} entrance anchors.");
        }

        private static List<Transform> CreateAnchors(Transform parent,
            IEnumerable<AuthoredLight> lights)
        {
            var result = new List<Transform>();
            foreach (var authored in lights)
            {
                var anchor = new GameObject(authored.Name).transform;
                anchor.SetParent(parent, false);
                var p = authored.BlenderPosition;
                anchor.localPosition = new Vector3(p.x, p.z, -p.y);
                result.Add(anchor);
            }
            return result;
        }

        private static void CopyPackage(Building3DPackage source,
            Building3DPackage destination)
        {
            destination.SchemaVersion = source.SchemaVersion;
            destination.AuthoredScale = source.AuthoredScale;
            destination.PivotOffset = source.PivotOffset;
            destination.FrontYawDegrees = source.FrontYawDegrees;
            destination.FootprintMeters = source.FootprintMeters;
            destination.BoundsTolerance = source.BoundsTolerance;
            destination.UseCrossFade = source.UseCrossFade;
            destination.CrossFadeWidth = source.CrossFadeWidth;
            destination.KeepShadowMeshWithImpostor = source.KeepShadowMeshWithImpostor;
            destination.CollisionPrefab = source.CollisionPrefab;
            destination.NightLightingPrefab = null;
            destination.NightLightingMaterial = null;
            destination.NightLightingLocalPosition = Vector3.zero;
            destination.NightLightingLocalEulerAngles = Vector3.zero;
            destination.NightLightingLocalScale = Vector3.one;
            destination.Representations = new List<Building3DRepresentation>();
            foreach (var item in source.Representations)
                destination.Representations.Add(new Building3DRepresentation
                {
                    Level = item.Level,
                    ScreenRelativeHeight = item.ScreenRelativeHeight,
                    VisualPrefab = item.VisualPrefab,
                    OverrideMaterial = item.OverrideMaterial,
                    ShadowPrefab = item.ShadowPrefab,
                    LocalPosition = item.LocalPosition,
                    LocalEulerAngles = item.LocalEulerAngles,
                    LocalScale = item.LocalScale,
                    TargetTriangleBudget = item.TargetTriangleBudget,
                    Provenance = item.Provenance,
                });
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var split = path.LastIndexOf('/');
            var parent = path[..split];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path[(split + 1)..]);
        }
    }
}
