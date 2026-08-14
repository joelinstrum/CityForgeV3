using System;
using System.IO;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Editor
{
    internal static class ArtDecoCornerPlacementQaMenu
    {
        private const string BuildingId =
            "cityforge.base.building.commercial.art_deco_corner_building_01";
        private const string PackagePath =
            "CityForgeV3/Buildings/ArtDecoCornerV03/building-package";
        private const string ResultPath = "/tmp/cityforge-art-deco-corner-placement-qa.txt";
        private const string ScreenshotPath = "/tmp/cityforge-art-deco-corner-placement-qa.png";
        private const string TriggerPath = "/tmp/cityforge-run-art-deco-corner-placement-qa";

        [InitializeOnLoadMethod]
        private static void RegisterTriggeredRun()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(TriggerPath)) return;
                File.Delete(TriggerPath);
                RunPlacementRegression();
            };
        }

        [MenuItem("City Forge/QA/Run Art Deco Corner Placement Regression")]
        private static void RunPlacementRegression()
        {
            var root = new GameObject("Art Deco Corner Placement QA");
            try
            {
                var package = HybridBuildingPackageRegistry.Load(PackagePath);
                Require(package != null, "Art Deco Corner package did not load.");
                Require(package.FacingCount == 4, "Package does not have four facings.");
                for (var index = 0; index < package.FacingCount; index++)
                {
                    var facing = package.Facing(index);
                    Require(Resources.Load<Texture2D>(facing.ApprovedResourcePath) != null,
                        $"Missing approved artwork for {facing.Id}.");
                    Require(Resources.Load<Texture2D>(facing.NeutralResourcePath) != null,
                        $"Missing neutral artwork for {facing.Id}.");
                    Require(Resources.Load<Texture2D>(facing.NightOverlayResourcePath) != null,
                        $"Missing night artwork for {facing.Id}.");
                }

                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Art Deco Corner QA", LotType.Commercial, 5, 5);
                Require(world.PlaceBuildingAtCenter(BuildingId), "Building could not be placed.");
                world.SetBuildingEditorContext(true, false);
                world.SetInspectionMode(BuildingInspectionMode.Artwork);

                var presentations = root.GetComponentsInChildren<HybridBuildingPresentation>(true);
                HybridBuildingPresentation activePresentation = null;
                foreach (var candidate in presentations)
                {
                    if (candidate.Visible)
                    {
                        activePresentation = candidate;
                        break;
                    }
                }
                var artwork = activePresentation == null
                    ? null
                    : Find(activePresentation.transform, "Directional Render")
                        ?.GetComponent<SpriteRenderer>();
                Require(artwork != null,
                    $"No visible building presentation after placement (count={presentations.Length}, " +
                    $"hasBuilding={world.HasBuilding}, inspection={world.InspectionMode}).");
                Require(artwork.sprite != null,
                    "Directional Render exists but its sprite is absent immediately after placement.");
                Require(artwork.enabled,
                    $"Directional Render exists with a sprite but is disabled immediately after placement " +
                    $"(hasBuilding={world.HasBuilding}, inspection={world.InspectionMode}, " +
                    $"active={artwork.gameObject.activeInHierarchy}).");

                var camera = root.GetComponentInChildren<Camera>(true);
                Require(camera != null, "Lot camera was not created.");
                for (var facingIndex = 0; facingIndex < 4; facingIndex++)
                {
                    activePresentation.AlignToCamera();
                    var origin = camera.WorldToScreenPoint(artwork.transform.position);
                    var screenUp = camera.WorldToScreenPoint(
                        artwork.transform.position + artwork.transform.up);
                    var delta = screenUp - origin;
                    Require(delta.y > 0f,
                        $"Facing {facingIndex} rendered vertically inverted ({delta}).");
                    Require(Mathf.Abs(delta.x) <= delta.y * 0.02f,
                        $"Facing {facingIndex} rendered with screen roll ({delta}).");
                    world.Rotate(1);
                }

                CaptureCamera(camera, ScreenshotPath);

                world.SetInspectionMode(BuildingInspectionMode.Primitive);
                world.SetBuildingEditorContext(true, false);
                Require(world.SelectBuildingAtLotPoint(Vector2.zero),
                    "Building could not be selected after placement.");
                world.NudgeSelected(1, 0);

                Require(world.InspectionMode == BuildingInspectionMode.Artwork,
                    "Moving the building left the world in proxy inspection mode.");
                Require(artwork.sprite != null && artwork.enabled,
                    "Moving the building removed or disabled its artwork.");
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name.StartsWith("CF_PROXY_", StringComparison.Ordinal))
                    {
                        Require(!renderer.enabled,
                            $"{renderer.gameObject.name} became visible after moving the building.");
                    }
                }

                File.WriteAllText(ResultPath,
                    "PASS\nArt Deco Corner V04 loaded four complete screen-upright facings and night overlays, " +
                    "rendered through the windowed Unity camera, placed and moved with artwork, " +
                    "and kept every semantic proxy hidden.\n");
                Debug.Log("CF_QA_ART_DECO_CORNER_PASS");
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

        private static void CaptureCamera(Camera camera, string path)
        {
            const int width = 1600;
            const int height = 1000;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var priorTarget = camera.targetTexture;
            var priorActive = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = priorTarget;
                RenderTexture.active = priorActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
