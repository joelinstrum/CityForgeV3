using System;
using System.IO;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Editor
{
    internal static class EmpirePlacementQaMenu
    {
        private const string BuildingId =
            "cityforge.base.building.commercial.art_deco_empire_tower_01";
        private const string PackagePath =
            "CityForgeV3/Buildings/ArtDecoEmpireV31/building-package";
        private const string ResultPath = "/tmp/cityforge-empire-placement-qa.txt";
        private const string TriggerPath = "/tmp/cityforge-run-empire-placement-qa";

        [InitializeOnLoadMethod]
        private static void RegisterTriggeredRun()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(TriggerPath)) return;
                File.Delete(TriggerPath);
                RunEmpirePlacementRegression();
            };
        }

        [MenuItem("City Forge/QA/Run Empire Placement Regression")]
        private static void RunEmpirePlacementRegression()
        {
            var root = new GameObject("Empire Placement QA");
            try
            {
                var package = HybridBuildingPackageRegistry.Load(PackagePath);
                Require(package != null, "Empire package did not load.");
                Require(package.FacingCount == 4, "Empire package does not have four facings.");
                for (var index = 0; index < package.FacingCount; index++)
                {
                    var facing = package.Facing(index);
                    Require(Resources.Load<Texture2D>(facing.ApprovedResourcePath) != null,
                        $"Missing approved artwork for {facing.Id}.");
                    Require(Resources.Load<Texture2D>(facing.NeutralResourcePath) != null,
                        $"Missing neutral artwork for {facing.Id}.");
                }

                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Empire QA", LotType.Commercial, 5, 5);
                Require(world.PlaceBuildingAtCenter(BuildingId), "Empire could not be placed.");
                world.SetBuildingEditorContext(true, false);

                var artwork = Find(root.transform, "Directional Render")
                    ?.GetComponent<SpriteRenderer>();
                Require(artwork != null && artwork.sprite != null && artwork.enabled,
                    "Empire artwork is absent immediately after placement.");

                // Reproduce the exact regression: diagnostic proxy, return to Buildings,
                // select the placed tower, then move it.
                world.SetInspectionMode(BuildingInspectionMode.Primitive);
                world.SetBuildingEditorContext(true, false);
                Require(world.SelectBuildingAtLotPoint(Vector2.zero),
                    "Empire could not be selected after placement.");
                world.NudgeSelected(1, 0);

                Require(world.InspectionMode == BuildingInspectionMode.Artwork,
                    "Moving Empire left the world in proxy inspection mode.");
                Require(artwork.sprite != null && artwork.enabled,
                    "Moving Empire removed or disabled its artwork.");
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name.StartsWith("CF_PROXY_", StringComparison.Ordinal))
                    {
                        Require(!renderer.enabled,
                            $"{renderer.gameObject.name} became visible after moving Empire.");
                    }
                }

                File.WriteAllText(ResultPath,
                    "PASS\nEmpire V31 loaded four complete facings, placed with artwork, " +
                    "survived selection and movement, and kept every semantic proxy hidden.\n");
                Debug.Log("CF_QA_EMPIRE_PASS");
            }
            catch (Exception exception)
            {
                File.WriteAllText(ResultPath, $"FAIL\n{exception}\n");
                Debug.LogException(exception);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }

            return null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
