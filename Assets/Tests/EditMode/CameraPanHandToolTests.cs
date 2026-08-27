using System.IO;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class CameraPanHandToolTests
    {
        [Test]
        public void LotToolbar_OffersSelectableCameraPanHandBeforeNewAction()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            var hand = source.IndexOf("camera-pan-hand");
            var newAction = source.IndexOf("CfButton.Create(\"NEW\"");

            Assert.That(hand, Is.GreaterThanOrEqualTo(0));
            Assert.That(newAction, Is.GreaterThan(hand));
            StringAssert.Contains("✋", source);
            StringAssert.Contains("_cameraPanPointerDown", source);
            StringAssert.Contains(
                "pointerDelta = pointerPosition - _cameraPanLastPosition", source);
            StringAssert.Contains("PanCameraViewport(\n                    pointerDelta", source);
            StringAssert.Contains("CameraPanCursorTexture()", source);
            StringAssert.Contains("UnityEngine.Cursor.SetCursor", source);
            StringAssert.Contains("SetCameraPanTool(false)", source);
            StringAssert.Contains(
                "SetCameraPanInteraction(_cameraPanToolActive)", source);
        }

        [Test]
        public void CameraPan_UsesOrthographicWorldScaleAndClampsToLot()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");

            StringAssert.Contains(
                "PanCameraViewport(Vector2 screenDelta, Vector2 viewportSize)",
                source);
            StringAssert.Contains("_camera.orthographicSize", source);
            StringAssert.Contains("-LotWidthMeters * 0.5f", source);
            StringAssert.Contains("-LotDepthMeters * 0.5f", source);
            StringAssert.Contains("_cameraPanInteractionActive", source);
            StringAssert.Contains("PanCameraInScreenPlane(", source);
            StringAssert.Contains("_camera.transform.up", source);
            StringAssert.Contains(
                "if (_cameraPanWorld.sqrMagnitude <= 0.0001f)", source);
            StringAssert.Contains(
                "if (_cameraPanInteractionActive)", source);
        }

        [TestCase(1, 0, 1, 0)]
        [TestCase(-1, 0, -1, 0)]
        [TestCase(0, 1, 0, 1)]
        [TestCase(0, -1, 0, -1)]
        public void ArrowPan_MovesLotOnScreenInPrintedArrowDirection(
            int horizontal, int vertical, int expectedX, int expectedY)
        {
            var root = new GameObject("Arrow Direction QA");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                var camera = root.GetComponentInChildren<Camera>(true);
                Assert.That(camera, Is.Not.Null);
                var before = camera.WorldToScreenPoint(Vector3.zero);

                world.PanCameraViewport(horizontal, vertical);

                var after = camera.WorldToScreenPoint(Vector3.zero);
                if (expectedX != 0)
                    Assert.That(Mathf.Sign(after.x - before.x), Is.EqualTo(expectedX));
                if (expectedY != 0)
                    Assert.That(Mathf.Sign(after.y - before.y), Is.EqualTo(expectedY));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LotBuildingArtwork_CannotBeRemovedByOcclusionCulling()
        {
            var root = new GameObject("Building Pan Visibility QA");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.PlaceGovernmentHouseAtCenter();
                var camera = root.GetComponentInChildren<Camera>(true);
                Assert.That(camera.useOcclusionCulling, Is.False);
                var presentation = root.GetComponentInChildren<
                    HybridBuildingPresentation>(true);
                Assert.That(presentation, Is.Not.Null);
                foreach (var renderer in presentation.GetComponentsInChildren<
                             SpriteRenderer>(true))
                    Assert.That(renderer.allowOcclusionWhenDynamic, Is.False,
                        renderer.name);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RepeatedVerticalPan_DoesNotChangeBuildingCameraDepth()
        {
            var root = new GameObject("Vertical Pan Depth QA");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotSizeMeters(60);
                world.PlaceGovernmentHouseAtCenter();
                var camera = root.GetComponentInChildren<Camera>(true);
                var presentation = root.GetComponentInChildren<
                    HybridBuildingPresentation>(true);
                Assert.That(presentation.TryGetArtworkRenderer(out var renderer),
                    Is.True);
                var beforeDepth = camera.transform.InverseTransformPoint(
                    renderer.bounds.center).z;

                for (var step = 0; step < 12; step++)
                    world.PanCameraViewport(0, 1);

                var afterDepth = camera.transform.InverseTransformPoint(
                    renderer.bounds.center).z;
                Assert.That(afterDepth, Is.EqualTo(beforeDepth).Within(0.001f));
                Assert.That(afterDepth, Is.GreaterThan(camera.nearClipPlane));
                Assert.That(afterDepth, Is.LessThan(camera.farClipPlane));
                Assert.That(renderer.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
