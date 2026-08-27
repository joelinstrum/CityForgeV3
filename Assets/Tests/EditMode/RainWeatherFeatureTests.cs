using System.IO;
using NUnit.Framework;
using UnityEngine;
using CityForgeV3.World;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class RainWeatherFeatureTests
    {
        [Test]
        public void EnvironmentPanel_OffersIconOnlyRainToggleBelowTimeOfDay()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");

            StringAssert.Contains("weather-actions", source);
            StringAssert.Contains("rain-toggle", source);
            StringAssert.Contains("☂", source);
            StringAssert.Contains("ToggleRain", source);
        }

        [Test]
        public void Rain_IsSlantedAndSuppressesEveryRuntimeShadowPath()
        {
            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var props = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Props.cs");
            var buildings = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/HybridBuildingPresentation.cs");

            StringAssert.Contains("velocity.x", rain);
            StringAssert.Contains("ParticleSystemRenderMode.Stretch", rain);
            StringAssert.Contains("Behind Buildings", rain);
            StringAssert.Contains("In Front of Buildings", rain);
            StringAssert.Contains("material.renderQueue = foreground ? 4000 : 2990", rain);
            StringAssert.Contains("MinMaxGradient", rain);
            StringAssert.Contains("LightShadows.None", world);
            StringAssert.Contains("!IsRaining", props);
            StringAssert.Contains("!_isRaining", buildings);
        }

        [Test]
        public void Rain_UsesNeutralDropsAndAddsLightFogGrade()
        {
            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");
            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");

            StringAssert.Contains("0.90f, 0.91f, 0.90f", rain);
            StringAssert.Contains("rainFog", app);
            StringAssert.Contains("0.24f * fogIntensity", app);
            StringAssert.Contains("RainVisualIntensity", app);
            StringAssert.Contains("Mathf.Max(80f", rain);
            StringAssert.Contains("0.012f, 0.030f", rain);
            StringAssert.Contains("0.045f, 0.085f", rain);
        }

        [Test]
        public void Rain_BuildsFromSparseDropsToSteadyRainfall()
        {
            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");

            StringAssert.Contains("RainBuildUpDurationSeconds = 5f", rain);
            StringAssert.Contains("RainBuildUpRoutine", rain);
            StringAssert.Contains("Mathf.SmoothStep", rain);
            StringAssert.Contains("Mathf.Lerp(35f, 2700f, progress)", rain);
            StringAssert.Contains("Mathf.Lerp(8f, 1100f, progress)", rain);
        }

        [Test]
        public void Rain_TapersBeforeTheLastDropsAreRemoved()
        {
            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");

            StringAssert.Contains("RainFadeOutDurationSeconds = 4f", rain);
            StringAssert.Contains("RainFadeOutRoutine", rain);
            StringAssert.Contains("Mathf.Lerp(backgroundStartRate, 0f, progress)", rain);
            StringAssert.Contains("Mathf.Lerp(foregroundStartRate, 0f, progress)", rain);
            StringAssert.Contains("WaitForSeconds(3f)", rain);
        }

        [Test]
        public void Rain_DiesOnSolidSurfacesAndFogFollowsStormIntensity()
        {
            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");

            StringAssert.Contains("ParticleSystemCollisionType.World", rain);
            StringAssert.Contains("collision.lifetimeLoss = 1f", rain);
            StringAssert.Contains("RainVisualIntensity = progress", rain);
            StringAssert.Contains("fogStartIntensity, 0f, progress", rain);
        }

        [Test]
        public void Rain_LeavesPersistentGroundProjectedWetStreetReflections()
        {
            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");
            var presentation = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/HybridBuildingPresentation.cs");
            var roadShader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/" +
                "ShadowReceivingRoadOverlay.shader");
            var reflectionShader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/" +
                "WetStreetReflection.shader");

            StringAssert.Contains("RoadWetness = Mathf.Max", rain);
            StringAssert.Contains(
                "GetComponentsInChildren<\n                HybridBuildingPresentation>(true)",
                rain);
            StringAssert.Contains("foreach (var presentation in presentations)",
                rain);
            StringAssert.Contains("ProjectWetReflectionOntoRoad", presentation);
            StringAssert.Contains("ScreenPointToRay", presentation);
            StringAssert.Contains("2f * foundationScreenY - screenCorner.y",
                presentation);
            StringAssert.Contains("Ref 1", roadShader);
            StringAssert.Contains("WriteMask 1", roadShader);
            StringAssert.Contains("Ref 1 ReadMask 253 WriteMask 0 Comp Equal Pass Keep",
                reflectionShader);
            StringAssert.Contains("0.0022 * _RainActive", reflectionShader);
            StringAssert.Contains("lerp(0.18, 0.036, _RainActive)",
                reflectionShader);
        }

        [Test]
        public void SelectingDifferentTimeOfDay_DriesWetStreetReflections()
        {
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");

            StringAssert.Contains("var timeChanged = TimeOfDay != preset", world);
            StringAssert.Contains("if (timeChanged)\n                ClearRoadWetness()",
                world);
            StringAssert.Contains("RoadWetness = 0f", rain);
            StringAssert.Contains("UpdateWetStreetReflections()", rain);
        }

        [Test]
        public void RebuildingMultiBuildingLot_ReappliesWetnessToEveryPresentation()
        {
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var rebuild = world.IndexOf("RebuildOtherBuildingPresentations();",
                System.StringComparison.Ordinal);
            var wetness = world.IndexOf("UpdateWetStreetReflections();", rebuild,
                System.StringComparison.Ordinal);
            Assert.That(rebuild, Is.GreaterThanOrEqualTo(0));
            Assert.That(wetness, Is.GreaterThan(rebuild));
            StringAssert.Contains("ScheduleWetStreetReflectionRefresh();", world);

            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");
            StringAssert.Contains("yield return null", rain);
            StringAssert.Contains("RefreshWetStreetReflectionsAfterRebuild", rain);
        }

        [Test]
        public void BuildingSelection_DoesNotAutomaticallyMoveOrRefitCamera()
        {
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var stateStart = world.IndexOf("private void ApplySessionState()",
                System.StringComparison.Ordinal);
            var stateEnd = world.IndexOf(
                "private void RebuildOtherBuildingPresentations()", stateStart,
                System.StringComparison.Ordinal);
            var stateBody = world.Substring(stateStart, stateEnd - stateStart);
            Assert.That(stateBody, Does.Not.Contain("ApplyProjectedLotFit();"));

            var packageStart = world.IndexOf(
                "private void EnsureBuildingPackage(string buildingId)",
                System.StringComparison.Ordinal);
            var packageEnd = world.IndexOf("private void UpdateFloraShadowSun()",
                packageStart, System.StringComparison.Ordinal);
            var packageBody = world.Substring(packageStart,
                packageEnd - packageStart);
            Assert.That(packageBody, Does.Not.Contain("ApplyCameraFacing();"));
            StringAssert.Contains("var preservedCamera = CaptureCameraFraming()",
                stateBody);
            StringAssert.Contains("RestoreCameraFraming(preservedCamera)",
                stateBody);
        }

        [Test]
        public void ZoomDeleteAndArrowNudge_NeverReframeTheLot()
        {
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var zoomStart = world.IndexOf("public void SetZoomLevel(",
                System.StringComparison.Ordinal);
            var zoomEnd = world.IndexOf("#if UNITY_EDITOR", zoomStart,
                System.StringComparison.Ordinal);
            Assert.That(world.Substring(zoomStart, zoomEnd - zoomStart),
                Does.Not.Contain("ApplyCameraFacing();"));
            var deleteStart = world.IndexOf("public bool DeleteSelectedBuilding()",
                System.StringComparison.Ordinal);
            var deleteEnd = world.IndexOf("public void DeleteSelected()", deleteStart,
                System.StringComparison.Ordinal);
            Assert.That(world.Substring(deleteStart, deleteEnd - deleteStart),
                Does.Not.Contain("ApplyCameraFacing();"));
            StringAssert.Contains("ApplySelectedBuildingPositionOnly();", world);
        }

        [Test]
        public void BuildingSelection_UsesTightVisibleArtwork_NotBillboardBounds()
        {
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var presentation = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/HybridBuildingPresentation.cs");
            StringAssert.Contains("ContainsVisibleArtworkPixel(_camera, pixel)", world);
            StringAssert.Contains("tightSprite.triangles", presentation);
            StringAssert.DoesNotContain("pixel.x >= minimum.x && pixel.x <= maximum.x",
                world);
        }

        [Test]
        public void VictorianGentleman_HasPlayableCharacterControlsAndAllSevenClips()
        {
            var prefab = Resources.Load<GameObject>(
                "CityForgeV3/Props/Characters/VictorianGentlemanV01/VictorianGentlemanAnimatedV01");
            Assert.That(prefab, Is.Not.Null);
            var clips = Resources.LoadAll<AnimationClip>(
                "CityForgeV3/Props/Characters/VictorianGentlemanV01/VictorianGentlemanAnimatedV01");
            foreach (var expected in new[]
                     {
                         "bow", "fold_arms", "idle", "look_around",
                         "run_upstairs", "sit", "walk"
                     })
                Assert.That(System.Array.Exists(clips, clip =>
                        clip.name.IndexOf(expected,
                            System.StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.True, $"Missing character animation: {expected}");

            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            StringAssert.Contains("LotEditorCategory.Characters", app);
            StringAssert.Contains("WalkSelectedCharacter", app);
            StringAssert.Contains("StopSelectedCharacter", app);
        }

        [Test]
        public void LotEditorRefreshAndSave_PreserveExactCameraFraming()
        {
            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            StringAssert.Contains("preserveLotCamera", app);
            StringAssert.Contains("_lotWorld.CaptureCameraFraming()", app);
            StringAssert.Contains("_lotWorld.RestoreCameraFraming(preservedCamera)",
                app);
        }

        [Test]
        public void PropEditing_NeverSchedulesAnAutomaticLotRefit()
        {
            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            StringAssert.DoesNotContain("_lotWorld.RefreshCameraFraming();", app);
            StringAssert.Contains("NudgeSelectedPropByScreenPixels", app);
        }

        [Test]
        public void WetStreetReflections_IncludeProps()
        {
            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");
            var props = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Props.cs");
            StringAssert.Contains("UpdatePropWetStreetReflections();", rain);
            StringAssert.Contains("WetStreetPropReflection", props);
            StringAssert.Contains("_Wetness", props);
            StringAssert.Contains("-_camera.transform.forward", props);
            StringAssert.Contains("renderer.sharedMaterials", props);
            StringAssert.Contains("source.mainTexture", props);
        }

        [Test]
        public void Snow_DrawsInFrontAndLeavesWinterRoadsWet()
        {
            var snow = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Snow.cs");
            var rain = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Rain.cs");
            StringAssert.Contains("Winter Snowfall — In Front of Buildings", snow);
            StringAssert.Contains("renderQueue = foreground ? 4000 : 2990", snow);
            StringAssert.Contains("RoadWetness = Mathf.Max(RoadWetness, _snowAccumulation)",
                snow);
            StringAssert.Contains("Season == SeasonPreset.Winter", rain);
            StringAssert.Contains("SnowAccumulation", rain);
            StringAssert.Contains("ResizeSnowGroundCover();", File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs"));
        }

        [Test]
        public void ArrowKeys_UseCalibratedSinglePixelMovementForBuildingsAndProps()
        {
            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var props = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Props.cs");
            StringAssert.Contains("NudgeSelectedBuildingByScreenPixels", app);
            StringAssert.Contains("displayedPixelScale = 1f", world);
            StringAssert.Contains("TryGroundDeltaForArrowKey", props);
        }

        [Test]
        public void BuildingLibrary_IsLargeScrollableModal()
        {
            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            var styles = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/UI/CityForgeV3.uss");
            StringAssert.Contains("building-library-modal-panel", app);
            StringAssert.Contains("new ScrollView(ScrollViewMode.Vertical)", app);
            StringAssert.Contains("name = \"building-card-scroll\"", app);
            StringAssert.Contains("width: 900px", styles);
            StringAssert.Contains("height: 760px", styles);
        }

        [Test]
        public void Buildings_CanMoveAgainstLandscapingAndWithoutArtificialClearance()
        {
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var occupancyStart = world.IndexOf("private bool CanOccupyBuilding(",
                System.StringComparison.Ordinal);
            var occupancyEnd = world.IndexOf("public bool EndBuildingDrag()",
                occupancyStart, System.StringComparison.Ordinal);
            var occupancy = world.Substring(occupancyStart,
                occupancyEnd - occupancyStart);
            Assert.That(occupancy, Does.Not.Contain("_session.Data.Buildings"));
            Assert.That(occupancy, Does.Not.Contain("_session.Data.Flora"));
        }

        [Test]
        public void CharacterAnimatorPreventsEmbeddedRootDrift()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/ThreeDimensionalCharacterAnimator.cs");
            StringAssert.Contains("private void LateUpdate()", source);
            StringAssert.Contains(
                "_animatedRoot.localPosition = _anchoredLocalPosition", source);
            StringAssert.Contains(
                "_animatedRoot.localRotation = _anchoredLocalRotation", source);
        }

        [Test]
        public void CharacterDiagonalsPollPhysicalArrowKeysEveryFrame()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            StringAssert.Contains("PollPhysicalCharacterArrowKeys();", source);
            StringAssert.Contains("Input.GetKey(KeyCode.LeftArrow)", source);
            StringAssert.Contains("Input.GetKey(KeyCode.RightArrow)", source);
            StringAssert.Contains("Input.GetKey(KeyCode.UpArrow)", source);
            StringAssert.Contains("Input.GetKey(KeyCode.DownArrow)", source);
        }

        [TestCase(0, 1, 0f)]
        [TestCase(1, 0, 90f)]
        [TestCase(0, -1, 180f)]
        [TestCase(-1, 0, 270f)]
        [TestCase(1, 1, 45f)]
        [TestCase(1, -1, 135f)]
        [TestCase(-1, -1, 225f)]
        [TestCase(-1, 1, 315f)]
        public void CharacterArrowsFollowLotCompass(
            int horizontal, int vertical, float expectedHeading)
        {
            var direction = LotWorldController.CharacterDirectionForArrowInput(
                horizontal, vertical);
            var heading = Mathf.Repeat(
                Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 360f);
            Assert.That(heading, Is.EqualTo(expectedHeading).Within(0.01f));
        }

        [Test]
        public void OrnateBench_IsACompletePlaceableThreeDimensionalProp()
        {
            const string root = "CityForgeV3/Props/OrnateBenchV01/";
            Assert.That(Resources.Load<GameObject>(root + "OrnateBenchV01"),
                Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(root + "Textures/base-color"),
                Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(root + "Textures/normal"),
                Is.Not.Null);
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Props.cs");
            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            StringAssert.Contains("ornate-bench-v01", world);
            StringAssert.Contains("NormalizeStaticPropToLength", world);
            StringAssert.Contains("ORNATE BENCH", app);
        }

        [Test]
        public void NearbyBenchTurnsCharacterStopIntoASeatedPose()
        {
            var props = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Props.cs");
            var animator = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/ThreeDimensionalCharacterAnimator.cs");
            StringAssert.Contains("TrySeatCharacterAtNearestBench", props);
            StringAssert.Contains("character.AnimationState = \"sit\"", props);
            StringAssert.Contains("ApplyCharacterAnimation(SelectedPropIndex, \"sit\")",
                props);
            StringAssert.Contains("IsLoopingState(_state)", animator);
            StringAssert.Contains("_playable.SetSpeed(0d)", animator);
        }

        [Test]
        public void CharacterAnimationStateSurvivesSelectionRebuilds()
        {
            var props = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Props.cs");
            StringAssert.Contains(
                "ApplyCharacterAnimation(_propPresentations.Count - 1", props);
            StringAssert.Contains("prop.AnimationState", props);
        }

        [Test]
        public void EveryBuildingUsesItsSemanticPrimitiveForVisualOcclusion()
        {
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var presentation = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/HybridBuildingPresentation.cs");
            StringAssert.Contains(
                "renderer.enabled = IsPerceptualBuildingOccluder(renderer);",
                world);
            StringAssert.Contains(
                "return objectName.Contains(\"WALL\") || objectName.Contains(\"ROOF\");",
                world);
            StringAssert.Contains(
                "_otherBuildingDepthOccluders.Add(\n                    CreateOtherBuildingDepthOccluder",
                world);
            StringAssert.Contains("renderQueue = 2430", world);
            StringAssert.DoesNotContain("Artwork Silhouette Depth Mask", presentation);
            StringAssert.DoesNotContain("BuildingArtworkDepthMask", presentation);
        }

        [Test]
        public void FloraUsesPhysicalDepthAndHostSpecificFrontFacadePriority()
        {
            var world = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var frontShader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/FrontFacadeLitShadowReceivingSprite.shader");
            var depthShader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/BuildingDepthOccluder.shader");
            StringAssert.Contains("renderer.sharedMaterial = inFrontApron", world);
            StringAssert.Contains("? FloraHostFrontRecoveryMaterial(hostStencilReference)", world);
            StringAssert.Contains(": FloraLitShadowReceiverMaterial();", world);
            StringAssert.Contains("CompareFunction.LessEqual", world);
            StringAssert.Contains("TryResolveVisibleBuildingFrontHost", world);
            StringAssert.Contains("TryBuildingOcclusionStencilReference", world);
            StringAssert.Contains("EntranceFacingDegrees", world);
            StringAssert.DoesNotContain("_ViewDepthBiasMeters", world);
            StringAssert.DoesNotContain("IsBeyondNearestBuildingFront", world);
            Assert.That(world, Does.Not.Contain("_StencilComp"));
            StringAssert.Contains("_BuildingHostStencilRef", frontShader);
            StringAssert.Contains("Ref 0", frontShader);
            StringAssert.Contains("WriteMask 252", frontShader);
            StringAssert.Contains("ReadMask 252", frontShader);
            StringAssert.Contains("ZTest Greater", frontShader);
            StringAssert.Contains("_BuildingHostStencilRef", depthShader);
            StringAssert.Contains("_BuildingHostStencilWriteMask", depthShader);
        }
    }
}
