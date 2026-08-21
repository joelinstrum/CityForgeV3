using System.IO;
using NUnit.Framework;

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
            StringAssert.Contains(
                "groundUp * screenDelta.y * metersPerPixel", source);
            StringAssert.Contains(
                "if (_cameraPanWorld.sqrMagnitude <= 0.0001f)", source);
            StringAssert.Contains(
                "if (_cameraPanInteractionActive)", source);
        }
    }
}
