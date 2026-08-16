using CityForgeV3.UI;
using CityForgeV3.World;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace CityForgeV3.Tests
{
    public sealed class UiFoundationTests
    {
        [Test]
        public void BuildingPropCatalog_ProvidesAleHousePreviewContract()
        {
            var item = BuildingPropCatalog.Find(BuildingPropCatalog.AleHouseSignId);

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Revision, Is.EqualTo("v01"));
            Assert.That(item.HostElevation, Is.EqualTo("Front"));
            Assert.That(item.PreviewResourcePath,
                Does.Contain("WoodenSignsV01/ale-house-preview"));
            Assert.That(item.ModelResourcePath,
                Does.Contain("Models/ale-house-animated-v01"));
            Assert.That(item.ForegroundDepthMeters,
                Is.GreaterThan(item.ProjectionDepthMeters));
            Assert.That(item.ModelYawDegrees, Is.EqualTo(186f));
            Assert.That(Resources.Load<Texture2D>(item.BaseColorResourcePath),
                Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(item.NormalResourcePath),
                Is.Not.Null);

            var model = Resources.Load<GameObject>(item.ModelResourcePath);
            Assert.That(model, Is.Not.Null);
            Assert.That(System.Array.Exists(
                model.GetComponentsInChildren<Transform>(true),
                transform => transform.name == item.SwingTransformName), Is.True);
        }

        [TestCase(0, 186f)]
        [TestCase(1, 276f)]
        [TestCase(2, 6f)]
        [TestCase(3, 96f)]
        public void BuildingPropFacing_FollowsHostQuarterTurns(
            int hostQuarterTurns, float expectedYaw)
        {
            var item = BuildingPropCatalog.Find(BuildingPropCatalog.AleHouseSignId);
            var yaw = Mathf.Repeat(
                item.ModelYawDegrees + hostQuarterTurns * 90f, 360f);

            Assert.That(yaw, Is.EqualTo(expectedYaw));
        }

        [Test]
        public void BuildingPropRotation_AdvancesThroughEightUsableAngles()
        {
            var attachment = new PlacedBuildingProp();
            for (var step = 1; step <= 8; step++)
            {
                attachment.RotationDegrees = Mathf.Repeat(
                    attachment.RotationDegrees + 45f, 360f);
                Assert.That(attachment.RotationDegrees,
                    Is.EqualTo(step % 8 * 45f));
            }
        }

        [Test]
        public void BuildingPropPreview_UsesRealMeshAndAlwaysVisibleMaterial()
        {
            var item = BuildingPropCatalog.Find(BuildingPropCatalog.AleHouseSignId);
            var prefab = Resources.Load<GameObject>(item.ModelResourcePath);
            var model = Object.Instantiate(prefab);
            try
            {
                Assert.That(model.GetComponentsInChildren<Renderer>(true)
                    .Any(renderer => renderer is not SpriteRenderer), Is.True);
                LogAssert.ignoreFailingMessages = true;
                InvokeBuildingPropMaterialMethod("ApplyBuildingPropMaterials", model, item);
                InvokeBuildingPropMaterialMethod("ApplyBuildingPropPreviewMaterials", model);

                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.That(renderer.sharedMaterial.shader.name,
                        Is.EqualTo("CityForgeV3/BuildingPropPlacementPreview"));
                    Assert.That(renderer.sharedMaterial.renderQueue, Is.EqualTo(5000));
                    Assert.That(renderer.shadowCastingMode,
                        Is.EqualTo(ShadowCastingMode.Off));
                    Assert.That(renderer.receiveShadows, Is.False);
                }
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Object.DestroyImmediate(model);
            }
        }

        [Test]
        public void BuildingPropCommittedMaterial_RendersAfterBuildingArtwork()
        {
            var item = BuildingPropCatalog.Find(BuildingPropCatalog.AleHouseSignId);
            var model = Object.Instantiate(Resources.Load<GameObject>(item.ModelResourcePath));
            try
            {
                LogAssert.ignoreFailingMessages = true;
                InvokeBuildingPropMaterialMethod("ApplyBuildingPropMaterials", model, item);
                Assert.That(model.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .All(material => material.renderQueue == 5000 &&
                        material.shader.name == "CityForgeV3/AlwaysVisibleBuildingProp"),
                    Is.True);
                Assert.That(model.GetComponentsInChildren<Renderer>(true)
                    .All(renderer => renderer.sortingOrder == 2200), Is.True);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Object.DestroyImmediate(model);
            }
        }

        [Test]
        public void BuildingPropCursor_UsesVisibleArtworkAndAllProjectedCorners()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.BuildingProps.cs");

            StringAssert.Contains("FindBuildingArtworkHitIndex", source);
            StringAssert.Contains("candidate.name == \"Directional Render\"", source);
            StringAssert.Contains("for (var x = -1; x <= 1; x += 2)", source);
            StringAssert.Contains("for (var y = -1; y <= 1; y += 2)", source);
            StringAssert.Contains("for (var z = -1; z <= 1; z += 2)", source);
            StringAssert.DoesNotContain("FindBuildingVisualHitIndex", source);
        }

        [Test]
        public void BuildingPropOrientation_IsWorldUprightAndHostRelative()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.BuildingProps.cs");

            StringAssert.Contains("model.rotation = Quaternion.Euler(", source);
            StringAssert.DoesNotContain(
                "model.rotation = _camera.transform.rotation", source);
        }

        [Test]
        public void BuildingProps_RenderThroughDedicatedCameraLayer()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.BuildingProps.cs");
            var controllerSource = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var layers = File.ReadAllText("ProjectSettings/TagManager.asset");

            StringAssert.Contains("BuildingPropOverlay", layers);
            StringAssert.Contains("BuildBuildingPropOverlayPass", source);
            StringAssert.Contains("_camera.cullingMask |=", source);
            StringAssert.DoesNotContain("CameraEvent.AfterEverything", source);
            StringAssert.DoesNotContain("DrawRenderer", source);
            StringAssert.Contains("SetBuildingPropOverlayLayer(model)", source);
            StringAssert.Contains("SetBuildingPropOverlayLayer(root)", source);
            StringAssert.Contains("RebuildBuildingPropOverlayPass", controllerSource);
            Assert.That(File.Exists(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/AlwaysVisibleBuildingProp.shader"),
                Is.True, "The committed-prop shader must be in Resources so player builds retain it.");
            Assert.That(File.Exists(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/BuildingPropPlacementPreview.shader"),
                Is.True, "The live-preview shader must be in Resources so player builds retain it.");
        }

        [Test]
        public void EscapeClearsTheActiveBuildingPropCursor()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");

            StringAssert.Contains("_placementBuildingPropId = \"\";", source);
            StringAssert.Contains("SetBuildingPropPlacementPreview(\"\")", source);
        }

        [Test]
        public void SuccessfulBuildingPropDrop_ReleasesPlacementCursorForSelection()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            StringAssert.Contains("if (placed)", source);
            StringAssert.Contains("_placementBuildingPropId = \"\";", source);
            StringAssert.Contains("Ale House sign attached • drag it to reposition", source);
        }

        private static void InvokeBuildingPropMaterialMethod(string name,
            params object[] arguments)
        {
            var method = typeof(LotWorldController).GetMethod(name,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            method.Invoke(null, arguments);
        }

        [Test]
        public void LotEditorSession_RoundTripsBuildingOwnedAttachment()
        {
            var session = new LotEditorSession();
            session.Place(BuildingCatalog.ColonialGovernmentHouseId, 1, 2);
            session.Data.Buildings[0].Attachments.Add(new PlacedBuildingProp
            {
                InstanceId = "attachment-pilot",
                ComponentId = BuildingPropCatalog.AleHouseSignId,
                Revision = "v01",
                HostElevation = "Front",
                NormalizedX = 0.72f,
                NormalizedY = 0.44f,
                ProjectionDepthMeters = 0.18f,
                Scale = 1f
            });

            var restored = new LotEditorSession();
            restored.Restore(session.Serialize());

            var attachment = restored.Data.Buildings[0].Attachments[0];
            Assert.That(restored.Data.Schema, Is.EqualTo("cityforge-v3-lot-save-v7"));
            Assert.That(attachment.ComponentId,
                Is.EqualTo(BuildingPropCatalog.AleHouseSignId));
            Assert.That(attachment.NormalizedX, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(attachment.NormalizedY, Is.EqualTo(0.44f).Within(0.001f));
        }

        [Test]
        public void DisabledMenuButtonIsNotFocusableOrEnabled()
        {
            var button = CfButton.Create("NEW REGION", null, false);

            Assert.That(button.enabledSelf, Is.False);
            Assert.That(button.focusable, Is.False);
        }

        [Test]
        public void LotEditorButtonUsesSharedPrimaryComponent()
        {
            var button = CfButton.Create("LOT EDITOR", () => { }, true, "primary");

            Assert.That(button.enabledSelf, Is.True);
            Assert.That(button.ClassListContains("cf-button"), Is.True);
            Assert.That(button.ClassListContains("cf-button--primary"), Is.True);
        }

        [TestCase(LotEditorCategory.Main, true, LotEditorCategory.Main, false)]
        [TestCase(LotEditorCategory.Main, false, LotEditorCategory.Main, true)]
        [TestCase(LotEditorCategory.Main, true, LotEditorCategory.Buildings, true)]
        public void ActiveToolIconTogglesItsPanel(
            LotEditorCategory current,
            bool currentlyExpanded,
            LotEditorCategory clicked,
            bool expected)
        {
            Assert.That(
                CityForgeApp.CategoryExpandedAfterClick(
                    current, currentlyExpanded, clicked),
                Is.EqualTo(expected));
        }

        [Test]
        public void LotEditorDefaultsToGeneralAndUsesTheCarRoadIcon()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            StringAssert.Contains(
                "_lotEditorCategory = LotEditorCategory.Main", source);
            StringAssert.Contains(
                "LotEditorCategory.Roads, \"roads-car-v74\", \"Roads\"", source);
        }

        [Test]
        public void DeleteAndBackspaceAreRoadDeletionShortcuts()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            StringAssert.Contains(
                "KeyCode.Delete or KeyCode.Backspace", source);
            StringAssert.Contains(
                "_lotEditorCategory == LotEditorCategory.Roads", source);
            StringAssert.Contains("DeleteRoadPiece();", source);
        }

        [Test]
        public void DeleteAndBackspaceRouteSelectedBuildingsThroughSharedCommand()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            StringAssert.Contains(
                "_lotEditorCategory == LotEditorCategory.Buildings", source);
            StringAssert.Contains("_lotWorld.IsSelected", source);
            StringAssert.Contains("DeleteBuilding();", source);
            StringAssert.Contains("_lotWorld.DeleteSelectedBuilding()", source);
        }

        [Test]
        public void DeleteSelectedBuildingRemovesOnlySelectionAndClearsSelectionState()
        {
            var root = new GameObject("Selected Building Delete Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Delete Buildings", LotType.Residential, 4, 4);
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.NewEnglandHouseId), Is.True);
                var firstId = world.Session.Data.Buildings[0].InstanceId;

                Assert.That(world.DeleteSelectedBuilding(), Is.True);
                Assert.That(world.BuildingCount, Is.EqualTo(1));
                Assert.That(world.Session.Data.Buildings[0].InstanceId, Is.EqualTo(firstId));
                Assert.That(world.IsSelected, Is.False);
                Assert.That(world.ActiveObjectSelection,
                    Is.EqualTo(LotObjectSelectionKind.None));
                Assert.That(world.DeleteSelectedBuilding(), Is.False);
                Assert.That(world.BuildingCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PrimitiveViewUsesReusableSemanticIconButton()
        {
            var button = CfButton.CreateIcon(
                "Primitive View",
                "3D",
                "Primitive View — show the imported 3D building proxy",
                () => { },
                true,
                true);

            Assert.That(button.name, Is.EqualTo("Primitive View"));
            Assert.That(button.text, Is.EqualTo("3D"));
            Assert.That(button.tooltip, Does.Contain("imported 3D building proxy"));
            Assert.That(button.enabledSelf, Is.True);
            Assert.That(button.focusable, Is.True);
            Assert.That(button.ClassListContains("cf-button--icon-selected"), Is.True);
        }

        [Test]
        public void IllustratedDisabledMenuButtonPreservesApprovedArtwork()
        {
            var button = CfImageButton.Create(
                "Open Region",
                "CityForgeV3/Art/MainMenu/open-region",
                null,
                false);

            Assert.That(button.enabledSelf, Is.False);
            Assert.That(button.focusable, Is.False);
            Assert.That(button.ClassListContains("cf-image-button"), Is.True);
            Assert.That(button.ClassListContains("cf-image-button--menu"), Is.True);
        }

        [Test]
        public void QuitControlUsesTheExistingBottomRightIconAsAnEnabledButton()
        {
            var button = CfImageButton.Create(
                "Quit Game",
                "CityForgeV3/Art/MainMenu/stats",
                CityForgeApp.QuitApplication,
                true,
                "utility");

            Assert.That(button.name, Is.EqualTo("Quit Game"));
            Assert.That(button.enabledSelf, Is.True);
            Assert.That(button.focusable, Is.True);
            Assert.That(button.ClassListContains("cf-image-button--utility"), Is.True);
        }

        [Test]
        public void LotSurfaceUsesItsDedicatedNonOccludingShader()
        {
            var shader = Shader.Find("CityForgeV3/LotSurfaceColor");

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.renderQueue, Is.LessThan((int)UnityEngine.Rendering.RenderQueue.Geometry));
        }

        [Test]
        public void HybridContractUsesCenteredMetricFoundation()
        {
            Assert.That(
                FiveBayHybridContract.Schema,
                Is.EqualTo("cityforge-v3-hybrid-building-package-v1"));
            Assert.That(FiveBayHybridContract.FoundationCenter, Is.EqualTo(Vector3.zero));
            Assert.That(FiveBayHybridContract.WidthMeters, Is.EqualTo(10f));
            Assert.That(FiveBayHybridContract.DepthMeters, Is.EqualTo(7f));
            Assert.That(FiveBayHybridContract.PresentationAnchor, Is.EqualTo(Vector3.zero));
        }

        [TestCase(0, 4.9f, 0f, true)]
        [TestCase(0, 0f, 3.6f, false)]
        [TestCase(1, 4.9f, 0f, false)]
        [TestCase(1, 0f, 3.4f, true)]
        public void BuildingDragHitTestUsesTheRotatedFoundationFootprint(
            int rotationQuarterTurns,
            float pointX,
            float pointZ,
            bool expected)
        {
            Assert.That(
                LotWorldController.BuildingFootprintContains(
                    new Vector2(pointX, pointZ),
                    Vector2.zero,
                    FiveBayHybridContract.WidthMeters,
                    FiveBayHybridContract.DepthMeters,
                    rotationQuarterTurns),
                Is.EqualTo(expected));
        }

        [Test]
        public void BuildingDragMovesFromProjectedPanelPointsAndCommitsOnRelease()
        {
            var root = new GameObject("Building Drag Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.PlaceBuildingAtCenter(BuildingCatalog.ColonialGovernmentHouseId);
                var camera = Object.FindFirstObjectByType<Camera>();
                Assert.That(camera, Is.Not.Null);
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);

                Vector2 PanelPoint(Vector3 worldPoint)
                {
                    var screen = camera.WorldToScreenPoint(worldPoint);
                    return new Vector2(screen.x, camera.pixelHeight - screen.y);
                }

                Assert.That(world.BeginBuildingDragFromPanel(
                    PanelPoint(Vector3.zero), panelSize), Is.True);
                Assert.That(world.DragBuildingFromPanel(
                    PanelPoint(new Vector3(3f, 0f, 2f)), panelSize), Is.True);
                Assert.That(world.EndBuildingDrag(), Is.True);
                Assert.That(world.BuildingCell, Is.EqualTo(new Vector2Int(3, 2)));
                Assert.That(world.IsSelected, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuildingHoverShowsBeforeSelectionAndIsSuppressedDuringDrag()
        {
            var root = new GameObject("Building Hover Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.PlaceBuildingAtCenter(BuildingCatalog.ColonialGovernmentHouseId);
                var camera = root.GetComponentInChildren<Camera>();
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var screen = camera.WorldToScreenPoint(Vector3.zero);
                var panelPoint = new Vector2(screen.x, panelSize.y - screen.y);

                Assert.That(world.UpdateObjectHoverFromPanel(panelPoint, panelSize),
                    Is.EqualTo(LotObjectSelectionKind.Building));
                Assert.That(world.HoverObjectIndex, Is.EqualTo(0));
                Assert.That(world.ObjectHoverVisible, Is.True);

                Assert.That(world.BeginBuildingDragFromPanel(panelPoint, panelSize), Is.True);
                Assert.That(world.ObjectHoverVisible, Is.False);
                Assert.That(world.UpdateObjectHoverFromPanel(
                    panelPoint, panelSize, true), Is.EqualTo(LotObjectSelectionKind.None));
                Assert.That(world.EndBuildingDrag(), Is.True);
                Assert.That(world.ObjectHoverVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ViewportClearsHoverOnExitAndBeforePointerSelection()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            StringAssert.Contains("RegisterCallback<PointerLeaveEvent>", source);
            StringAssert.Contains("_lotWorld.ClearObjectHover();", source);
            StringAssert.Contains("UpdateObjectHoverFromPanel", source);
        }

        [Test]
        public void FourRotationsRestoreTheExactFacing()
        {
            var facing = 0;
            for (var turn = 0; turn < 4; turn++)
            {
                facing = FiveBayHybridContract.WrapFacing(facing + 1);
            }

            Assert.That(facing, Is.Zero);
            Assert.That(FiveBayHybridContract.Facing(facing).Id, Is.EqualTo("front-right"));
        }

        [TestCase(0, "front-right", -55.5f)]
        [TestCase(1, "rear-right", 34.5f)]
        [TestCase(2, "rear-left", 124.5f)]
        [TestCase(3, "front-left", -145.5f)]
        public void FacingOrderMatchesLockedCameraViews(int index, string id, float azimuth)
        {
            var facing = FiveBayHybridContract.Facing(index);

            Assert.That(facing.Id, Is.EqualTo(id));
            Assert.That(facing.CameraAzimuthDegrees, Is.EqualTo(azimuth));
        }

        [Test]
        public void CameraUsesTheExactFoundersColonialElevation()
        {
            Assert.That(FiveBayHybridContract.CameraElevationDegrees, Is.EqualTo(30f));
            Assert.That(FiveBayHybridContract.CameraRadiusMeters, Is.EqualTo(26f));
            Assert.That(FiveBayHybridContract.CameraTargetHeightMeters, Is.EqualTo(3.15f));
        }

        [Test]
        public void SourcePivotIsConvertedFromTopOriginForUnity()
        {
            var facing = FiveBayHybridContract.Facing(0);

            Assert.That(facing.SourcePivotTopOrigin.y, Is.EqualTo(0.662379831f));
            Assert.That(facing.UnityPivot.y, Is.EqualTo(0.337620169f).Within(0.000001f));
        }

        [Test]
        public void EveryFacingHasAnImportedTransparentMaster()
        {
            for (var index = 0; index < FiveBayHybridContract.FacingCount; index++)
            {
                var facing = FiveBayHybridContract.Facing(index);
                var texture = Resources.Load<Texture2D>(facing.ResourcePath);

                Assert.That(texture, Is.Not.Null, facing.Id);
                Assert.That(texture.width, Is.EqualTo(2048), facing.Id);
                Assert.That(texture.height, Is.EqualTo(2048), facing.Id);
            }
        }

        [Test]
        public void GovernmentHouseLoadsFromTheReusableBuildingPackage()
        {
            var entry = BuildingCatalog.GovernmentHouse;
            var package = HybridBuildingPackage.Load(entry.PackageResourcePath);

            Assert.That(package.Id, Is.EqualTo(entry.Id));
            Assert.That(package.DisplayName, Is.EqualTo(entry.Name));
            Assert.That(package.PlacementScale, Is.EqualTo(1f));
            Assert.That(package.RotationAnchor, Is.EqualTo(Vector3.zero));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.PrimitiveResourcePath, Is.Not.Empty);
        }

        [Test]
        public void BuildingPackageValidationRejectsIncompleteManifests()
        {
            var issues = HybridBuildingPackage.Validate(
                new HybridBuildingPackageManifest
                {
                    schema = HybridBuildingPackage.Schema,
                    id = "incomplete"
                });

            Assert.That(issues, Is.Not.Empty);
            Assert.That(issues, Does.Contain("spatial is required"));
            Assert.That(issues, Does.Contain("exactly four facings are required"));
        }

        [Test]
        public void NewEnglandHouseIsACompleteIndependentPackage()
        {
            var entry = BuildingCatalog.NewEnglandHouse;
            var package =
                HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);

            Assert.That(BuildingCatalog.All.Count, Is.EqualTo(3));
            Assert.That(package.Id, Is.EqualTo(entry.Id));
            Assert.That(package.WidthMeters, Is.EqualTo(11.8f));
            Assert.That(package.DepthMeters, Is.EqualTo(5.5f));
            Assert.That(package.HeightMeters, Is.EqualTo(8.93f));
            Assert.That(package.FrontFacingQuarterTurns, Is.EqualTo(2));
            Assert.That(package.CanvasWidth, Is.EqualTo(1024));
            Assert.That(package.CanvasHeight, Is.EqualTo(1024));
            Assert.That(package.PixelsPerMeter, Is.EqualTo(66.064516f).Within(0.001f));
            Assert.That(
                package.CanvasHeight / package.PixelsPerMeter,
                Is.EqualTo(15.5f).Within(0.01f),
                "The sprite canvas must retain the approved Blender camera's 15.5 m orthographic scale.");
            Assert.That(package.PlanResourcePath, Is.Not.Empty);
            Assert.That(
                Resources.Load<Texture2D>(package.PlanResourcePath),
                Is.Not.Null);
            Assert.That(
                Resources.Load<GameObject>(package.PrimitiveResourcePath),
                Is.Not.Null);
            var proxy = Resources.Load<GameObject>(package.PrimitiveResourcePath);
            var proxyRenderers = proxy.GetComponentsInChildren<MeshRenderer>(true);
            Assert.That(
                proxyRenderers.Length,
                Is.EqualTo(4),
                "The V3 proxy must not retain detailed source-house meshes.");
            CollectionAssert.AreEquivalent(
                package.RequiredPrimitiveObjects,
                System.Array.ConvertAll(proxyRenderers, renderer => renderer.name));
            var roofFilter = System.Array.Find(
                proxy.GetComponentsInChildren<MeshFilter>(true),
                filter => filter.name == "CF_PROXY_ROOF_GABLE");
            Assert.That(roofFilter, Is.Not.Null);
            var highest = float.NegativeInfinity;
            var ridge = new System.Collections.Generic.List<Vector3>();
            foreach (var vertex in roofFilter.sharedMesh.vertices)
            {
                var point = roofFilter.transform.TransformPoint(vertex);
                if (point.y > highest + 0.001f)
                {
                    highest = point.y;
                    ridge.Clear();
                    ridge.Add(point);
                }
                else if (Mathf.Abs(point.y - highest) < 0.001f)
                {
                    ridge.Add(point);
                }
            }
            Assert.That(ridge.Count, Is.GreaterThanOrEqualTo(2));
            var ridgeMinX = float.PositiveInfinity;
            var ridgeMaxX = float.NegativeInfinity;
            var ridgeMinZ = float.PositiveInfinity;
            var ridgeMaxZ = float.NegativeInfinity;
            foreach (var point in ridge)
            {
                ridgeMinX = Mathf.Min(ridgeMinX, point.x);
                ridgeMaxX = Mathf.Max(ridgeMaxX, point.x);
                ridgeMinZ = Mathf.Min(ridgeMinZ, point.z);
                ridgeMaxZ = Mathf.Max(ridgeMaxZ, point.z);
            }
            Assert.That(ridgeMaxX - ridgeMinX, Is.GreaterThan(11f));
            Assert.That(ridgeMaxZ - ridgeMinZ, Is.LessThan(0.01f));

            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                Assert.That(
                    Resources.Load<Texture2D>(facing.ApprovedResourcePath),
                    Is.Not.Null,
                    facing.Id);
                Assert.That(
                    Resources.Load<Texture2D>(facing.NeutralResourcePath),
                    Is.Not.Null,
                    facing.Id);
                Assert.That(
                    Resources.Load<Texture2D>(facing.NightOverlayResourcePath),
                    Is.Not.Null,
                    facing.Id);
                foreach (var preset in new[]
                {
                    TimeOfDayPreset.Morning,
                    TimeOfDayPreset.Noon,
                    TimeOfDayPreset.Afternoon,
                    TimeOfDayPreset.Evening
                })
                {
                    var shadePath = facing.ShadeResourcePath(preset);
                    var shade = Resources.Load<Texture2D>(shadePath);
                    Assert.That(shadePath, Is.Not.Empty,
                        $"{facing.Id} {preset}");
                    Assert.That(shade, Is.Not.Null,
                        $"{facing.Id} {preset}");
                    Assert.That(shade.width, Is.EqualTo(1024),
                        $"{facing.Id} {preset}");
                    Assert.That(shade.height, Is.EqualTo(1024),
                        $"{facing.Id} {preset}");
                }
                Assert.That(facing.ShadeResourcePath(TimeOfDayPreset.Night),
                    Is.Null, "Night lighting must remain a separate authored overlay.");
                Assert.That(
                    Resources.Load<Texture2D>(facing.WinterResourcePath),
                    Is.Not.Null,
                    facing.Id);
            }
        }

        [Test]
        public void ActiveCatalogDiscoversThreeBayPackageFromManifestData()
        {
            Assert.That(BuildingCatalog.TryFindByShortcut('T', out var entry), Is.True);
            Assert.That(entry.Name, Is.EqualTo("New England Three-Bay House"));
            Assert.That(entry.ReviewStatus, Is.EqualTo("PENDING JOE REVIEW"));
            Assert.That(entry.OccupancyWidth, Is.EqualTo(1));
            Assert.That(entry.OccupancyDepth, Is.EqualTo(1));
            var package = HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
            Assert.That(package.WidthMeters, Is.EqualTo(7.2f));
            Assert.That(package.DepthMeters, Is.EqualTo(5.5f));
            Assert.That(package.RoofRidgeAxis, Is.EqualTo("x"));
            Assert.That(package.EntranceFacingDegrees, Is.EqualTo(180f));
            Assert.That(Resources.Load<GameObject>(package.PrimitiveResourcePath), Is.Not.Null);
            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                Assert.That(Resources.Load<Texture2D>(facing.ApprovedResourcePath), Is.Not.Null, facing.Id);
                Assert.That(Resources.Load<Texture2D>(facing.WinterResourcePath), Is.Not.Null, facing.Id);
            }
        }

        [Test]
        public void PubQaFrontIsVisibleInCommercialCatalogAndLoadsAtFullResolution()
        {
            const string id = "cityforge.qa.building.commercial.pub_qa_front_01";
            var entry = BuildingCatalog.All.FirstOrDefault(candidate => candidate.Id == id);
            Assert.That(entry, Is.Not.Null, "Pub-QA must be visible in the active Buildings catalog.");
            Assert.That(entry.Category, Is.EqualTo("Commercial"));

            var package = HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
            Assert.That(package.Id, Is.EqualTo(id));
            Assert.That(package.CanvasWidth, Is.EqualTo(2048));
            Assert.That(package.CanvasHeight, Is.EqualTo(2048));
            for (var index = 0; index < package.FacingCount; index++)
            {
                var texture = Resources.Load<Texture2D>(package.Facing(index).ApprovedResourcePath);
                Assert.That(texture, Is.Not.Null, package.Facing(index).Id);
                Assert.That(texture.width, Is.EqualTo(2048));
                Assert.That(texture.height, Is.EqualTo(2048));
            }
        }

        [Test]
        public void PubQa20DegreeHasFourLosslessQuarterTurnFacings()
        {
            const string id = "cityforge.qa.building.commercial.pub_qa_20deg_05";
            var entry = BuildingCatalog.All.FirstOrDefault(candidate => candidate.Id == id);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Category, Is.EqualTo("Commercial"));
            var package = HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
            var expectedAngles = new[] { 20f, 110f, 200f, 290f };
            Assert.That(package.FacingCount, Is.EqualTo(4));
            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                Assert.That(facing.CameraAzimuthDegrees, Is.EqualTo(expectedAngles[index]));
                var texture = Resources.Load<Texture2D>(facing.ApprovedResourcePath);
                Assert.That(texture, Is.Not.Null, facing.Id);
                Assert.That(texture.width, Is.EqualTo(2048));
                Assert.That(texture.height, Is.EqualTo(2048));
                Assert.That(facing.MorningShadeResourcePath, Is.Not.Empty);
                Assert.That(facing.NoonShadeResourcePath, Is.Not.Empty);
                Assert.That(facing.AfternoonShadeResourcePath, Is.Not.Empty);
                Assert.That(facing.EveningShadeResourcePath, Is.Not.Empty);
                Assert.That(Resources.Load<Texture2D>(facing.MorningShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.NoonShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.AfternoonShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.EveningShadeResourcePath), Is.Not.Null);
            }
        }

        [Test]
        public void EveryActiveRenderedBuildingHasFourRegisteredNightOverlays()
        {
            foreach (var entry in BuildingCatalog.All)
            {
                var package = HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
                for (var index = 0; index < package.FacingCount; index++)
                {
                    var facing = package.Facing(index);
                    Assert.That(facing.NightOverlayResourcePath, Is.Not.Empty,
                        $"{entry.Name} {facing.Id}");
                    Assert.That(Resources.Load<Texture2D>(facing.NightOverlayResourcePath),
                        Is.Not.Null, $"{entry.Name} {facing.Id}");
                }
            }
        }

        [Test]
        public void NewEnglandHouseCardinalFacingStartsSouthAndRotatesClockwise()
        {
            var package = HybridBuildingPackageRegistry.NewEnglandHouse;
            var expected = new[] { "South", "West", "North", "East", "South" };

            for (var turn = 0; turn < expected.Length; turn++)
            {
                var cardinal = LotEditorSession.CardinalOrientation(
                    package.FrontFacingQuarterTurns + turn);
                Assert.That(cardinal, Is.EqualTo(expected[turn]));
            }
        }

        [Test]
        public void EveryActivePackageUsesTheSameClockwiseCardinalRule()
        {
            foreach (var entry in BuildingCatalog.All)
            {
                var package = HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
                var start = package.FrontFacingQuarterTurns;
                var clockwise = LotEditorSession.CardinalOrientation(start + 1);
                var counterClockwise = LotEditorSession.CardinalOrientation(start - 1);
                if (start == 2)
                {
                    Assert.That(clockwise, Is.EqualTo("West"), entry.Name);
                    Assert.That(counterClockwise, Is.EqualTo("East"), entry.Name);
                }
                else if (start == 0)
                {
                    Assert.That(clockwise, Is.EqualTo("East"), entry.Name);
                    Assert.That(counterClockwise, Is.EqualTo("West"), entry.Name);
                }
            }
        }

        [Test]
        public void SouthAuthoredWoodenPackagesShowFrontAtEastAndRearAtWest()
        {
            foreach (var entry in BuildingCatalog.All)
            {
                if (entry.Category != "Residential") continue;
                var package = HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
                Assert.That(package.FrontFacingQuarterTurns, Is.EqualTo(2), entry.Name);
                Assert.That(package.ArtworkRotationStep, Is.EqualTo(1), entry.Name);
                Assert.That(package.Facing(package.PresentationFacing(0, 3)).Id,
                    Is.EqualTo("front-left"), $"{entry.Name} East must show its front");
                Assert.That(package.Facing(package.PresentationFacing(0, 1)).Id,
                    Is.EqualTo("rear-right"), $"{entry.Name} West must show its rear");
            }
        }

        [Test]
        public void GovernmentHouseRetainsItsExistingArtworkRotationMapping()
        {
            var package = HybridBuildingPackageRegistry.GovernmentHouse;
            Assert.That(package.ArtworkRotationStep, Is.EqualTo(-1));
            Assert.That(package.FrontFacingQuarterTurns, Is.EqualTo(2),
                "The front-door orientation is south in the lot coordinate system.");
            Assert.That(package.PresentationFacing(0, 1), Is.EqualTo(3));
        }

        [Test]
        public void LotEditorDefaultsToNeutralGameLitArtwork()
        {
            var host = new GameObject("Lot World Lighting Default Test");
            try
            {
                var world = host.AddComponent<LotWorldController>();
                Assert.That(world.ArtworkSource, Is.EqualTo(BuildingArtworkSource.NeutralPilot));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void NewEnglandHousePresentationKeepsItsAuthoredWorldScale()
        {
            var package = HybridBuildingPackageRegistry.NewEnglandHouse;
            var cameraObject = new GameObject("Presentation Test Camera");
            var presentationObject = new GameObject("Presentation Test");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                var presentation =
                    presentationObject.AddComponent<HybridBuildingPresentation>();
                presentation.Build(camera, package);
                presentation.SetVisible(true);

                var renderer =
                    presentationObject.GetComponentInChildren<SpriteRenderer>();
                Assert.That(renderer.sprite, Is.Not.Null);
                Assert.That(renderer.sprite.rect.width, Is.EqualTo(1024f));
                Assert.That(renderer.sprite.rect.height, Is.EqualTo(1024f));
                Assert.That(
                    renderer.sprite.bounds.size.x,
                    Is.EqualTo(15.5f).Within(0.01f));
                Assert.That(
                    renderer.sprite.bounds.size.y,
                    Is.EqualTo(15.5f).Within(0.01f));
                Assert.That(presentationObject.transform.lossyScale, Is.EqualTo(Vector3.one));
                Assert.That(presentationObject.transform.position,
                    Is.EqualTo(package.PresentationAnchor),
                    "The logical package anchor must remain unchanged.");
                Assert.That(presentation.VisualPlaneLocalPosition.z,
                    Is.EqualTo(-0.08f).Within(0.001f),
                    "Only the child billboard plane may receive the camera-depth safety offset.");
            }
            finally
            {
                Object.DestroyImmediate(presentationObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void HybridBuildingArtworkUsesCameraScreenUpWhenCameraHasRoll()
        {
            var cameraObject = new GameObject("Rolled Presentation Camera");
            var presentationObject = new GameObject("Upright Presentation");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.rotation = Quaternion.Euler(32f, 45f, 37f);
                var presentation =
                    presentationObject.AddComponent<HybridBuildingPresentation>();
                presentation.Build(camera, HybridBuildingPackageRegistry.NewEnglandHouse);
                presentation.AlignToCamera();

                Assert.That(
                    Vector3.Dot(
                        presentationObject.transform.up,
                        camera.transform.up),
                    Is.GreaterThan(0.999f));
            }
            finally
            {
                Object.DestroyImmediate(presentationObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void NewEnglandHouseUsesDirectionalShadeByDayAndAuthoredLightsAtNight()
        {
            var package = HybridBuildingPackageRegistry.NewEnglandHouse;
            var cameraObject = new GameObject("Shade Test Camera");
            var presentationObject = new GameObject("Shade Test Presentation");
            try
            {
                var presentation = presentationObject.AddComponent<HybridBuildingPresentation>();
                presentation.Build(cameraObject.AddComponent<Camera>(), package);
                presentation.SetArtworkSource(BuildingArtworkSource.NeutralPilot);
                presentation.SetVisible(true);
                presentation.SetTimeOfDay(TimeOfDayPreset.Afternoon);
                Assert.That(presentation.ShadeOverlayShowing, Is.True);
                Assert.That(presentation.NightOverlayShowing, Is.False);

                presentation.SetTimeOfDay(TimeOfDayPreset.Night);
                Assert.That(presentation.ShadeOverlayShowing, Is.False);
                Assert.That(presentation.NightOverlayShowing, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(presentationObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ThreeBayHouseHasRegisteredDirectionalLightingForEveryDayFacing()
        {
            Assert.That(BuildingCatalog.TryFindByShortcut('T', out var entry), Is.True);
            var package = HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
            for (var facingIndex = 0; facingIndex < package.FacingCount; facingIndex++)
            {
                var facing = package.Facing(facingIndex);
                for (var timeIndex = 0; timeIndex < 4; timeIndex++)
                {
                    var preset = (TimeOfDayPreset)timeIndex;
                    var path = facing.ShadeResourcePath(preset);
                    Assert.That(path, Is.Not.Empty, $"{facing.Id} {preset}");
                    Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null,
                        $"{facing.Id} {preset}");
                }
                Assert.That(facing.ShadeResourcePath(TimeOfDayPreset.Night),
                    Is.Null, "Night remains an independent authored light overlay.");
            }
        }

        [Test]
        public void LotSessionSupportsPlaceMoveRotateAndDelete()
        {
            var session = new LotEditorSession();

            session.Place(BuildingCatalog.ColonialGovernmentHouseId, 2, -3);
            Assert.That(session.Data.HasBuilding, Is.True);
            Assert.That(session.Data.CellX, Is.EqualTo(2));
            Assert.That(session.Data.CellZ, Is.EqualTo(-3));

            session.Nudge(1, 2);
            session.Rotate(1);
            Assert.That(session.Data.CellX, Is.EqualTo(3));
            Assert.That(session.Data.CellZ, Is.EqualTo(-1));
            Assert.That(session.Data.RotationQuarterTurns, Is.EqualTo(1));

            session.Delete();
            Assert.That(session.Data.HasBuilding, Is.False);
            Assert.That(session.IsSelected, Is.False);
        }

        [Test]
        public void FourBuildingRotationsPreserveTheExactGridAnchor()
        {
            var session = new LotEditorSession();
            session.Place(BuildingCatalog.ColonialGovernmentHouseId, -2, 4);

            for (var turn = 0; turn < 4; turn++)
            {
                session.Rotate(1);
            }

            Assert.That(session.Data.CellX, Is.EqualTo(-2));
            Assert.That(session.Data.CellZ, Is.EqualTo(4));
            Assert.That(session.Data.RotationQuarterTurns, Is.Zero);
        }

        [Test]
        public void CounterClockwiseBuildingRotationWrapsAndPreservesAnchor()
        {
            var session = new LotEditorSession();
            session.Place(BuildingCatalog.ColonialGovernmentHouseId, 3, -4);

            session.Rotate(-1);

            Assert.That(session.Data.RotationQuarterTurns, Is.EqualTo(3));
            Assert.That(session.Data.CellX, Is.EqualTo(3));
            Assert.That(session.Data.CellZ, Is.EqualTo(-4));
            Assert.That(
                LotEditorSession.CardinalOrientation(session.Data.RotationQuarterTurns),
                Is.EqualTo("West"));
        }

        [TestCase(0, "North")]
        [TestCase(1, "East")]
        [TestCase(2, "South")]
        [TestCase(3, "West")]
        [TestCase(4, "North")]
        [TestCase(-1, "West")]
        public void BuildingRotationHasClearCardinalFeedback(
            int quarterTurns,
            string expected)
        {
            Assert.That(
                LotEditorSession.CardinalOrientation(quarterTurns),
                Is.EqualTo(expected));
        }

        [TestCase(TimeOfDayPreset.Morning, "MORNING", 24f, 90f)]
        [TestCase(TimeOfDayPreset.Noon, "NOON", 68f, 174f)]
        [TestCase(TimeOfDayPreset.Afternoon, "AFTERNOON", 34f, 270f)]
        [TestCase(TimeOfDayPreset.Evening, "EVENING", 8f, 272f)]
        [TestCase(TimeOfDayPreset.Night, "NIGHT", -18f, 318f)]
        public void TimeOfDayPresetsHaveDeterministicSunContracts(
            TimeOfDayPreset preset,
            string label,
            float elevation,
            float azimuth)
        {
            var spec = TimeOfDayLighting.For(preset);

            Assert.That(spec.Preset, Is.EqualTo(preset));
            Assert.That(spec.Label, Is.EqualTo(label));
            Assert.That(spec.SunElevation, Is.EqualTo(elevation));
            Assert.That(spec.SunAzimuth, Is.EqualTo(azimuth));
            Assert.That(spec.SunIntensity, Is.GreaterThan(0f));
            Assert.That(spec.ScreenTint.a, Is.GreaterThan(0f));
        }

        [Test]
        public void AfternoonSunRaysTravelEastwardSoEastWallsAreShaded()
        {
            var rotation = TimeOfDayLighting.SunRotation(
                TimeOfDayPreset.Afternoon);
            var rayDirection = rotation * Vector3.forward;

            Assert.That(rayDirection.y, Is.LessThan(0f),
                "Afternoon sunlight must travel downward.");
            Assert.That(rayDirection.x, Is.GreaterThan(0f),
                "A western afternoon sun must cast its rays toward the east.");
            Assert.That(Mathf.Abs(rayDirection.z), Is.LessThan(0.001f),
                "Due-west afternoon light must not introduce a north/south component.");
            Assert.That(Vector3.Dot(Vector3.right, -rayDirection),
                Is.LessThan(0f),
                "An east-facing wall must face away from the afternoon sun.");
        }

        [Test]
        public void MorningSunRaysTravelDueWestWithoutNorthSouthDrift()
        {
            var rotation = TimeOfDayLighting.SunRotation(
                TimeOfDayPreset.Morning);
            var rayDirection = rotation * Vector3.forward;
            var horizontal = new Vector2(
                rayDirection.x, rayDirection.z).normalized;

            Assert.That(rayDirection.y, Is.LessThan(0f),
                "Morning sunlight must travel downward.");
            Assert.That(horizontal.x, Is.LessThan(-0.999f),
                "A due-east morning sun must cast its shadow due west.");
            Assert.That(Mathf.Abs(horizontal.y), Is.LessThan(0.001f),
                "Morning building shadows must not drift north or south.");
        }

        [Test]
        public void HybridPackageCanRegisterShadowDirectionToItsVisibleCompass()
        {
            var path = System.IO.Path.Combine(
                Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Buildings/PubQa20DegV05/building-package.json");
            var json = System.IO.File.ReadAllText(path);

            StringAssert.Contains("\"directionOffsetDegrees\": -90.0", json,
                "Pub QA 2 must rotate its source-space solar projection onto the visible lot compass.");
        }

        [Test]
        public void ShadowReceiverShaderIsAvailableAndSupported()
        {
            var shader = Shader.Find(
                "CityForgeV3/ShadowReceivingLotSurface");

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
        }

        [Test]
        public void ShadowReceiverDoesNotWriteDepthOverBillboardArtwork()
        {
            var path = System.IO.Path.Combine(
                Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Shaders/ShadowReceivingLotSurface.shader");
            var source = System.IO.File.ReadAllText(path);
            StringAssert.Contains("ZWrite Off", source);
            StringAssert.DoesNotContain("ZWrite On", source);
        }

        [Test]
        public void AfternoonBuildsAVisiblePrimitiveProjectedShadow()
        {
            var root = new GameObject("Shadow Projection Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId);
                world.SetTimeOfDay(TimeOfDayPreset.Afternoon);

                Assert.That(world.ProjectedShadowVisible, Is.True);
                Assert.That(world.ProjectedShadowBounds.size.x,
                    Is.GreaterThan(0.1f));
                Assert.That(world.ProjectedShadowBounds.size.z,
                    Is.GreaterThan(0.1f));
                Assert.That(world.ProjectedShadowVertexCount,
                    Is.InRange(3, 8),
                    "The cast shadow must be one semantic primitive silhouette, not overlapping renderer bounds.");
                Assert.That(world.ProjectedShadowSourceVertexCount,
                    Is.EqualTo(10),
                    "The visible shadow must use the stable four-corner/eave/ridge semantic primitive.");
                Assert.That(world.ProjectedShadowBounds.size.x,
                    Is.LessThan(35f));
                Assert.That(world.ProjectedShadowBounds.size.z,
                    Is.LessThan(35f));
                var direction = world.ProjectedShadowLocalDirection;
                Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
                Assert.That(Mathf.Abs(direction.x), Is.GreaterThan(0.05f),
                    "The primitive shadow must retain the sun ray's local X component.");
                Assert.That(direction.x, Is.GreaterThan(0.999f),
                    "Afternoon building shadows must travel east from the due-west sun.");
                Assert.That(Mathf.Abs(direction.y), Is.LessThan(0.001f),
                    "Afternoon building shadows must not drift north or south.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ColonialCommercialShadowSpansTheAuthoredRoofFootprint()
        {
            var root = new GameObject("Commercial Shadow Footprint Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialCornerPorticoCommercialId);
                world.SetTimeOfDay(TimeOfDayPreset.Afternoon);

                var bounds = world.ProjectedShadowBounds;
                Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(16.8f),
                    "The shadow hull must retain the complete 16.84 m authored roof width.");
                Assert.That(bounds.size.z, Is.GreaterThanOrEqualTo(11.9f),
                    "The shadow hull must retain the complete 12 m authored roof depth, not the 10.5 m placement shell.");
                Assert.That(bounds.max.x, Is.GreaterThan(10f),
                    "The due-west afternoon sun must produce a visible shadow beyond the building's 8.42 m east eave.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LowSunLightingIsSoftAndAfternoonAvoidsTheWrongBakedSpotlight()
        {
            var morning = TimeOfDayLighting.For(TimeOfDayPreset.Morning);
            var afternoon = TimeOfDayLighting.For(TimeOfDayPreset.Afternoon);

            Assert.That(morning.SunIntensity, Is.EqualTo(0.78f));
            Assert.That(afternoon.SunIntensity, Is.EqualTo(0.72f));
            Assert.That(morning.ScreenTint.a, Is.EqualTo(0.035f));
            Assert.That(afternoon.ScreenTint.a, Is.EqualTo(0.045f));
            Assert.That(
                HybridBuildingPresentation.DirectionalShadeOpacityFor(
                    TimeOfDayPreset.Morning), Is.EqualTo(0.55f));
            Assert.That(
                HybridBuildingPresentation.DirectionalShadeOpacityFor(
                    TimeOfDayPreset.Afternoon), Is.EqualTo(0.12f));
        }

        [Test]
        public void EveningIsDarkerAndMoreStronglyGradedThanNoon()
        {
            var noon = TimeOfDayLighting.For(TimeOfDayPreset.Noon);
            var evening = TimeOfDayLighting.For(TimeOfDayPreset.Evening);

            Assert.That(evening.SunIntensity, Is.LessThan(noon.SunIntensity));
            Assert.That(
                evening.BackgroundColor.grayscale,
                Is.LessThan(noon.BackgroundColor.grayscale));
            Assert.That(evening.ScreenTint.a, Is.GreaterThan(noon.ScreenTint.a));
            Assert.That(
                evening.NeutralArtworkTint.grayscale,
                Is.LessThan(noon.NeutralArtworkTint.grayscale));
        }

        [Test]
        public void NoonUsesHighHardSunWithoutWashoutExposure()
        {
            var noon = TimeOfDayLighting.For(TimeOfDayPreset.Noon);

            Assert.That(noon.SunElevation, Is.EqualTo(68f));
            Assert.That(noon.SunIntensity, Is.EqualTo(0.92f));
            Assert.That(noon.AmbientColor.grayscale, Is.LessThan(0.36f));
            Assert.That(noon.ScreenTint.a, Is.EqualTo(0.008f));
            Assert.That(
                HybridBuildingPresentation.DirectionalShadeOpacityFor(
                    TimeOfDayPreset.Noon), Is.EqualTo(0.42f));
        }

        [Test]
        public void EveningDirectionalOverlayRetainsDuskBaseExposure()
        {
            var evening = HybridBuildingPresentation.NeutralBaseTintFor(
                TimeOfDayPreset.Evening,
                true);
            var afternoon = HybridBuildingPresentation.NeutralBaseTintFor(
                TimeOfDayPreset.Afternoon,
                true);

            Assert.That(evening,
                Is.EqualTo(TimeOfDayLighting.For(TimeOfDayPreset.Evening)
                    .NeutralArtworkTint));
            Assert.That(evening.grayscale, Is.LessThan(afternoon.grayscale));
            Assert.That(afternoon, Is.EqualTo(Color.white),
                "The approved v30 afternoon exposure must not change.");
        }

        [Test]
        public void EveningAndNightUseTheApprovedHalfExposureCalibration()
        {
            var evening = TimeOfDayLighting.For(TimeOfDayPreset.Evening);
            var night = TimeOfDayLighting.For(TimeOfDayPreset.Night);

            Assert.That(evening.SunIntensity, Is.EqualTo(0.19f));
            Assert.That(evening.AmbientColor, Is.EqualTo(new Color(0.09f, 0.105f, 0.15f)));
            Assert.That(evening.GroundColor, Is.EqualTo(new Color(0.09f, 0.12f, 0.105f)));
            Assert.That(evening.NeutralArtworkTint, Is.EqualTo(new Color(0.23f, 0.26f, 0.34f)));
            Assert.That(evening.ScreenTint.a, Is.LessThanOrEqualTo(0.15f));
            Assert.That(night.SunIntensity, Is.EqualTo(0.05f));
            Assert.That(night.AmbientColor, Is.EqualTo(new Color(0.05f, 0.065f, 0.11f)));
            Assert.That(night.GroundColor, Is.EqualTo(new Color(0.055f, 0.08f, 0.075f)));
            Assert.That(night.NeutralArtworkTint, Is.EqualTo(new Color(0.14f, 0.17f, 0.26f)));
        }

        [Test]
        public void NightIsDistinctlyDarkerAndCoolerThanEvening()
        {
            var evening = TimeOfDayLighting.For(TimeOfDayPreset.Evening);
            var night = TimeOfDayLighting.For(TimeOfDayPreset.Night);

            Assert.That(night.SunIntensity, Is.LessThan(evening.SunIntensity));
            Assert.That(
                night.AmbientColor.grayscale,
                Is.LessThan(evening.AmbientColor.grayscale));
            Assert.That(
                night.GroundColor.grayscale,
                Is.LessThan(evening.GroundColor.grayscale));
            Assert.That(
                night.NeutralArtworkTint.grayscale,
                Is.LessThan(evening.NeutralArtworkTint.grayscale));
            Assert.That(night.ScreenTint.a, Is.GreaterThan(evening.ScreenTint.a));
            Assert.That(night.SunColor.b, Is.GreaterThan(night.SunColor.r));
        }

        [Test]
        public void NeutralRenderSetUsesEveryApprovedFacingRegistration()
        {
            for (var index = 0;
                 index < FiveBayHybridContract.FacingCount;
                 index++)
            {
                var facing = FiveBayHybridContract.Facing(index);
                var neutral =
                    Resources.Load<Texture2D>(facing.NeutralResourcePath);
                var night =
                    Resources.Load<Texture2D>(facing.NightOverlayResourcePath);

                Assert.That(neutral, Is.Not.Null, facing.Id);
                Assert.That(night, Is.Not.Null, facing.Id);
                Assert.That(neutral.width, Is.EqualTo(2048), facing.Id);
                Assert.That(neutral.height, Is.EqualTo(2048), facing.Id);
                Assert.That(night.width, Is.EqualTo(2048), facing.Id);
                Assert.That(night.height, Is.EqualTo(2048), facing.Id);
                Assert.That(
                    HybridBuildingPresentation.SupportsNeutralPilot(index),
                    Is.True,
                    facing.Id);
            }
        }

        [TestCase(BuildingInspectionMode.Artwork, true, false)]
        [TestCase(BuildingInspectionMode.Hybrid, true, true)]
        [TestCase(BuildingInspectionMode.Primitive, false, true)]
        public void InspectionModesHaveOneExplicitVisibilityPolicy(
            BuildingInspectionMode mode,
            bool showsArtwork,
            bool showsPrimitive)
        {
            Assert.That(
                BuildingInspectionPolicy.ShowsArtwork(mode),
                Is.EqualTo(showsArtwork));
            Assert.That(
                BuildingInspectionPolicy.ShowsPrimitive(mode),
                Is.EqualTo(showsPrimitive));
        }

        [TestCase(BuildingInspectionMode.Artwork, false)]
        [TestCase(BuildingInspectionMode.Hybrid, false)]
        [TestCase(BuildingInspectionMode.Primitive, true)]
        public void FilledFoundationDiagnosticIsExclusiveToPrimitiveView(
            BuildingInspectionMode mode,
            bool expected)
        {
            Assert.That(
                BuildingInspectionPolicy.ShowsFoundationFill(mode),
                Is.EqualTo(expected));
        }

        [Test]
        public void ChoosingABuildingLeavesDiagnosticViewAndShowsArtwork()
        {
            var root = new GameObject("Catalog Artwork Mode Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetInspectionMode(BuildingInspectionMode.Primitive);

                world.SelectCatalogBuilding(
                    BuildingCatalog.ColonialGovernmentHouseId);

                Assert.That(world.InspectionMode,
                    Is.EqualTo(BuildingInspectionMode.Artwork),
                    "Choosing a catalog building must not present its diagnostic proxy as the building.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RestoredBuildingWorkspaceLeavesDiagnosticViewAndShowsArtwork()
        {
            var root = new GameObject("Restored Building Artwork Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                world.SetInspectionMode(BuildingInspectionMode.Primitive);

                world.SetBuildingEditorContext(true, false);

                Assert.That(world.InspectionMode,
                    Is.EqualTo(BuildingInspectionMode.Artwork));
                var artwork = Find(root.transform, "Directional Render")
                    .GetComponent<SpriteRenderer>();
                Assert.That(artwork.enabled, Is.True,
                    "Restoring directly into Buildings must display the rendered artwork.");
                Assert.That(artwork.sprite, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ColonialCommercialV16LoadsAllRuntimeArtwork()
        {
            const string packagePath =
                "CityForgeV3/Buildings/ColonialCornerPorticoCommercialV16/building-package";
            var package = HybridBuildingPackageRegistry.Load(packagePath);

            Assert.That(package.FacingCount, Is.EqualTo(4));
            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                Assert.That(Resources.Load<Texture2D>(facing.ApprovedResourcePath),
                    Is.Not.Null, $"Missing approved artwork for {facing.Id}");
                Assert.That(Resources.Load<Texture2D>(facing.NeutralResourcePath),
                    Is.Not.Null, $"Missing neutral artwork for {facing.Id}");
                Assert.That(Resources.Load<Texture2D>(facing.NightOverlayResourcePath),
                    Is.Not.Null, $"Missing night artwork for {facing.Id}");
                foreach (var preset in new[]
                         {
                             TimeOfDayPreset.Morning,
                             TimeOfDayPreset.Noon,
                             TimeOfDayPreset.Afternoon,
                             TimeOfDayPreset.Evening
                         })
                {
                    Assert.That(Resources.Load<Texture2D>(
                            facing.ShadeResourcePath(preset)),
                        Is.Not.Null,
                        $"Missing {preset} overlay for {facing.Id}");
                }
            }
        }

        [Test]
        public void ColonialCommercialV16BuildsArtworkWithTransformOnlyEntranceAnchor()
        {
            const string buildingId =
                "cityforge.base.building.commercial.colonial_corner_portico_commercial_01";
            var root = new GameObject("Colonial Commercial V16 Presentation Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();

                Assert.That(world.PlaceBuildingAtCenter(buildingId), Is.True);
                world.SetBuildingEditorContext(true, false);

                var artwork = Find(root.transform, "Directional Render")
                    .GetComponent<SpriteRenderer>();
                Assert.That(artwork.sprite, Is.Not.Null);
                Assert.That(artwork.enabled, Is.True);
                Assert.That(world.InspectionMode,
                    Is.EqualTo(BuildingInspectionMode.Artwork));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EmpireV31LoadsAndKeepsArtworkAfterSelectionAndMovement()
        {
            const string buildingId =
                "cityforge.base.building.commercial.art_deco_empire_tower_01";
            const string packagePath =
                "CityForgeV3/Buildings/ArtDecoEmpireV31/building-package";
            var package = HybridBuildingPackageRegistry.Load(packagePath);

            Assert.That(package.FacingCount, Is.EqualTo(4));
            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                Assert.That(Resources.Load<Texture2D>(facing.ApprovedResourcePath),
                    Is.Not.Null, $"Missing Empire artwork for {facing.Id}");
                Assert.That(Resources.Load<Texture2D>(facing.NeutralResourcePath),
                    Is.Not.Null, $"Missing Empire neutral artwork for {facing.Id}");
            }

            var root = new GameObject("Empire V31 Move Smoke Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Empire Test", LotType.Commercial, 5, 5);
                Assert.That(world.PlaceBuildingAtCenter(buildingId), Is.True);
                world.SetBuildingEditorContext(true, false);

                var artwork = Find(root.transform, "Directional Render")
                    .GetComponent<SpriteRenderer>();
                Assert.That(artwork.sprite, Is.Not.Null);
                Assert.That(artwork.enabled, Is.True);
                Assert.That(world.InspectionMode,
                    Is.EqualTo(BuildingInspectionMode.Artwork));

                world.SetInspectionMode(BuildingInspectionMode.Primitive);
                world.SetBuildingEditorContext(true, false);
                Assert.That(world.SelectBuildingAtLotPoint(Vector2.zero), Is.True);
                world.NudgeSelected(1, 0);

                Assert.That(world.InspectionMode,
                    Is.EqualTo(BuildingInspectionMode.Artwork));
                Assert.That(artwork.enabled, Is.True,
                    "Moving Empire must not replace its artwork with the proxy.");
                Assert.That(artwork.sprite, Is.Not.Null);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.gameObject.name.StartsWith("CF_PROXY_")) continue;
                    Assert.That(renderer.enabled, Is.False,
                        $"{renderer.gameObject.name} became visible after moving Empire.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ArtworkModeHidesEveryVisibleProxySurface()
        {
            var root = new GameObject("Artwork Proxy Visibility Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);

                world.SetInspectionMode(BuildingInspectionMode.Artwork);

                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.gameObject.name.StartsWith("CF_PROXY_"))
                        continue;
                    Assert.That(renderer.enabled, Is.False,
                        $"{renderer.gameObject.name} must be disabled in Artwork mode.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HybridWorldHidesFoundationFillButPrimitiveWorldShowsIt()
        {
            var root = new GameObject("Foundation Visibility Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.PlaceBuildingAtCenter(BuildingCatalog.ColonialGovernmentHouseId);
                var foundation = System.Array.Find(
                    root.GetComponentsInChildren<Renderer>(true),
                    renderer => renderer.gameObject.name == "CF_PROXY_FOUNDATION");
                Assert.That(foundation, Is.Not.Null);

                world.SetInspectionMode(BuildingInspectionMode.Hybrid);
                Assert.That(foundation.enabled, Is.False,
                    "Hybrid artwork must not be covered by the cyan foundation fill.");

                world.SetInspectionMode(BuildingInspectionMode.Primitive);
                Assert.That(foundation.enabled, Is.True,
                    "Primitive inspection must retain the foundation diagnostic.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SerializedLotRestoresBuildingAnchorAndOrientationExactly()
        {
            var source = new LotEditorSession();
            source.Place(BuildingCatalog.ColonialGovernmentHouseId, 4, 6);
            source.Rotate(1);
            source.Rotate(1);

            var restored = new LotEditorSession();
            restored.Restore(source.Serialize());

            Assert.That(restored.Data.HasBuilding, Is.True);
            Assert.That(restored.Data.BuildingId,
                Is.EqualTo(BuildingCatalog.ColonialGovernmentHouseId));
            Assert.That(restored.Data.CellX, Is.EqualTo(4));
            Assert.That(restored.Data.CellZ, Is.EqualTo(6));
            Assert.That(restored.Data.RotationQuarterTurns, Is.EqualTo(2));
        }

        [Test]
        public void LotSessionKeepsMultipleBuildingsAndDeletesOnlyTheSelectedInstance()
        {
            var session = new LotEditorSession();
            session.AddBuilding(BuildingCatalog.ColonialGovernmentHouseId, -4, 0);
            session.AddBuilding(BuildingCatalog.NewEnglandHouseId, 5, 1);

            Assert.That(session.Data.Buildings.Count, Is.EqualTo(2));
            Assert.That(session.SelectedBuildingIndex, Is.EqualTo(1));
            session.Move(6, 2);
            Assert.That(session.Data.Buildings[0].CellX, Is.EqualTo(-4));
            Assert.That(session.Data.Buildings[1].CellX, Is.EqualTo(6));

            session.Delete();
            Assert.That(session.Data.Buildings.Count, Is.EqualTo(1));
            Assert.That(session.Data.Buildings[0].BuildingId,
                Is.EqualTo(BuildingCatalog.ColonialGovernmentHouseId));
            Assert.That(session.Data.HasBuilding, Is.True);
        }

        [Test]
        public void LotSessionRotatesOnlyTheSelectedBuildingInstance()
        {
            var session = new LotEditorSession();
            session.AddBuilding(BuildingCatalog.ColonialGovernmentHouseId, -4, 0);
            session.AddBuilding(BuildingCatalog.NewEnglandHouseId, 5, 1);

            session.SelectBuilding(0);
            session.Rotate(1);

            Assert.That(session.Data.Buildings[0].RotationQuarterTurns, Is.EqualTo(1));
            Assert.That(session.Data.Buildings[1].RotationQuarterTurns, Is.EqualTo(0));
        }

        [Test]
        public void LotSessionRotationPreservesAttachmentFacadeCoordinates()
        {
            var session = new LotEditorSession();
            session.AddBuilding(BuildingCatalog.ColonialGovernmentHouseId, -4, 0);
            session.Data.Buildings[0].Attachments.Add(new PlacedBuildingProp
            {
                ComponentId = BuildingPropCatalog.AleHouseSignId,
                NormalizedX = 0.78f,
                NormalizedY = 0.42f
            });
            session.SelectBuilding(0);

            session.Rotate(-1);
            Assert.That(session.Data.Buildings[0].Attachments[0].NormalizedX,
                Is.EqualTo(0.78f).Within(0.0001f));
            Assert.That(session.Data.Buildings[0].Attachments[0].NormalizedY,
                Is.EqualTo(0.42f).Within(0.0001f));

            session.Rotate(-1);
            session.Rotate(-1);
            session.Rotate(-1);
            Assert.That(session.Data.Buildings[0].Attachments[0].NormalizedX,
                Is.EqualTo(0.78f).Within(0.0001f));
            Assert.That(session.Data.Buildings[0].RotationQuarterTurns, Is.Zero);
        }

        [Test]
        public void LegacyTreeArtworkLoadsAndFloraPlacementsRoundTrip()
        {
            foreach (var id in new[] { "maple", "ashe", "oak" })
                Assert.That(Resources.Load<Texture2D>(
                    $"CityForgeV3/Flora/LegacyTreesV01/{id}-summer"),
                    Is.Not.Null, id);

            var source = new LotEditorSession();
            source.Data.Flora.Add(new PlacedFlora
            {
                InstanceId = "tree-1",
                FloraId = "maple",
                PositionX = 3.5f,
                PositionZ = -2.25f
            });
            var restored = new LotEditorSession();
            restored.Restore(source.Serialize());
            Assert.That(restored.Data.Flora.Count, Is.EqualTo(1));
            Assert.That(restored.Data.Flora[0].FloraId, Is.EqualTo("maple"));
            Assert.That(restored.Data.Flora[0].PositionX, Is.EqualTo(3.5f));
            Assert.That(restored.Data.Flora[0].PositionZ, Is.EqualTo(-2.25f));
        }

        [Test]
        public void NewlyPlacedFloraIsSelectedAndCanBeDraggedBeforeRelease()
        {
            var root = new GameObject("Flora Direct Manipulation Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetFloraEditorContext(true);
                var camera = root.GetComponentInChildren<Camera>();
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                Vector2 PanelPoint(Vector3 lotPoint)
                {
                    var pixel = camera.WorldToScreenPoint(lotPoint);
                    return new Vector2(pixel.x, panelSize.y - pixel.y);
                }

                Assert.That(world.BeginFloraDragFromPanel("maple",
                    PanelPoint(Vector3.zero), panelSize), Is.True);
                Assert.That(world.FloraCount, Is.EqualTo(1));
                Assert.That(world.SelectedFloraIndex, Is.EqualTo(0));
                Assert.That(world.DragFloraFromPanel(
                    PanelPoint(new Vector3(3f, 0f, -2f)), panelSize), Is.True);
                Assert.That(world.EndFloraDrag(), Is.True);
                Assert.That(world.Session.Data.Flora[0].PositionX,
                    Is.EqualTo(3f).Within(0.1f));
                Assert.That(world.Session.Data.Flora[0].PositionZ,
                    Is.EqualTo(-2f).Within(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ArmedAuthoringToolsTakePriorityOverGlobalObjectSelection()
        {
            Assert.That(CityForgeApp.ShouldPrioritizeToolPlacement(
                LotEditorCategory.Roads, "", ""), Is.True);
            Assert.That(CityForgeApp.ShouldPrioritizeToolPlacement(
                LotEditorCategory.Flora, "maple", ""), Is.True);
            Assert.That(CityForgeApp.ShouldPrioritizeToolPlacement(
                LotEditorCategory.Flora, "", ""), Is.False,
                "Escape must disarm flora placement and restore object selection.");
            Assert.That(CityForgeApp.ShouldPrioritizeToolPlacement(
                LotEditorCategory.Props, "", "wrought-iron-fence-straight-v01"),
                Is.True);
            Assert.That(CityForgeApp.ShouldPrioritizeToolPlacement(
                LotEditorCategory.Buildings, "", ""), Is.False);
        }

        [Test]
        public void RoadStrokePaintsItsInitialCellWithoutPointerMovement()
        {
            var root = new GameObject("Road Initial Click Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotType(LotType.Neighborhood);
                var camera = root.GetComponentInChildren<Camera>();
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var pixel = camera.WorldToScreenPoint(Vector3.zero);
                var panelPoint = new Vector2(pixel.x, panelSize.y - pixel.y);

                Assert.That(world.PaintRoadStrokeCellFromPanel(panelPoint, panelSize),
                    Is.True);
                Assert.That(RoadPlacementModel.FindAt(
                    world.Session.Data.RoadPieces,
                    world.RoadCursorCell.x,
                    world.RoadCursorCell.y), Is.Not.Null,
                    "The pointer-down cell must be painted even without a move event.");
                world.EndRoadPaintStroke();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FloraAndBuildingsRejectSharedFootprintsAndUseCameraDepthSorting()
        {
            var root = new GameObject("Flora Occupancy and Depth Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                Assert.That(world.CanPlaceFloraAt(Vector2.zero), Is.False,
                    "A tree trunk cannot occupy the selected building footprint.");

                var camera = root.GetComponentInChildren<Camera>();
                var behind = new Vector2(camera.transform.forward.x,
                    camera.transform.forward.z).normalized * 8f;
                Assert.That(world.CanPlaceFloraAt(behind), Is.True);
                world.Session.Data.Flora.Add(new PlacedFlora
                {
                    InstanceId = "behind-building",
                    FloraId = "maple",
                    PositionX = behind.x,
                    PositionZ = behind.y
                });
                world.SetInspectionMode(BuildingInspectionMode.Artwork);
                var tree = Find(root.transform, "Flora — maple")
                    .GetComponent<SpriteRenderer>();
                var building = Find(root.transform, "Directional Render")
                    .GetComponent<SpriteRenderer>();
                Assert.That(tree.sortingOrder, Is.LessThan(building.sortingOrder),
                    "A tree behind the building must be obscured by it.");

                world.NewEmptyLot("Tree Blocks Building", LotType.Residential, 20);
                world.Session.Data.Flora.Add(new PlacedFlora
                {
                    InstanceId = "center-tree",
                    FloraId = "oak",
                    PositionX = 0f,
                    PositionZ = 0f
                });
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                Assert.That(LotWorldController.BuildingFootprintContains(
                    Vector2.zero,
                    new Vector2(world.BuildingCell.x, world.BuildingCell.y),
                    world.BuildingPackage.WidthMeters,
                    world.BuildingPackage.DepthMeters, 0), Is.False,
                    "The placement search must move a building away from an existing tree trunk.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ArmedFloraShowsHalfOpacityCursorPreviewAndDeselectClearsIt()
        {
            var root = new GameObject("Flora Cursor Preview Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetFloraEditorContext(true);
                world.SetFloraPlacementPreview("maple");
                var camera = root.GetComponentInChildren<Camera>();
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var pixel = camera.WorldToScreenPoint(new Vector3(7f, 0f, 7f));
                var panelPoint = new Vector2(pixel.x, panelSize.y - pixel.y);
                Assert.That(world.UpdateFloraPreviewFromPanel(panelPoint, panelSize), Is.True);
                var preview = Find(root.transform, "Flora Placement Preview")
                    .GetComponent<SpriteRenderer>();
                Assert.That(preview.gameObject.activeSelf, Is.True);
                Assert.That(preview.color.a, Is.EqualTo(0.5f).Within(0.001f));

                world.DeselectAll();
                Assert.That(preview.gameObject.activeSelf, Is.False);
                Assert.That(world.SelectedFloraIndex, Is.EqualTo(-1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NightTintAppliesToFloraAndSemanticRoadMaterials()
        {
            var root = new GameObject("Flora and Road Night Tint Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.Session.Data.Flora.Add(new PlacedFlora
                {
                    InstanceId = "night-maple",
                    FloraId = "maple",
                    PositionX = 7f,
                    PositionZ = 7f
                });
                world.SetInspectionMode(BuildingInspectionMode.Artwork);
                world.SetTimeOfDay(TimeOfDayPreset.Night);
                var expected = TimeOfDayLighting.For(
                    TimeOfDayPreset.Night).NeutralArtworkTint;
                var tree = Find(root.transform, "Flora — maple")
                    .GetComponent<SpriteRenderer>();
                Assert.That(tree.color.r, Is.EqualTo(expected.r).Within(0.001f));
                Assert.That(tree.color.g, Is.EqualTo(expected.g).Within(0.001f));
                Assert.That(tree.color.b, Is.EqualTo(expected.b).Within(0.001f));

                var roadRoot = Find(root.transform, "Road Family Artwork");
                var road = roadRoot.GetComponentInChildren<Renderer>();
                Assert.That(road.sharedMaterial.HasProperty("_TimeTint"), Is.True);
                var roadTint = road.sharedMaterial.GetColor("_TimeTint");
                Assert.That(roadTint.r, Is.EqualTo(expected.r).Within(0.001f));
                Assert.That(roadTint.g, Is.EqualTo(expected.g).Within(0.001f));
                Assert.That(roadTint.b, Is.EqualTo(expected.b).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FloraUsesOneNormalizedTimeResponsiveShadow()
        {
            var root = new GameObject("Flora Shadow Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.Session.Data.Flora.Add(new PlacedFlora
                {
                    InstanceId = "shadow-maple",
                    FloraId = "maple",
                    PositionX = 7f,
                    PositionZ = 7f
                });
                world.SetInspectionMode(BuildingInspectionMode.Artwork);
                world.SetTimeOfDay(TimeOfDayPreset.Afternoon);

                var cast = Find(root.transform, "Flora Shadow — Canopy")
                    .GetComponent<SpriteRenderer>();
                var tree = Find(root.transform, "Flora — maple")
                    .GetComponent<SpriteRenderer>();
                Assert.That(System.Array.FindAll(
                    root.GetComponentsInChildren<SpriteRenderer>(),
                    renderer => renderer.name.StartsWith("Flora Shadow")).Length,
                    Is.EqualTo(1));
                Assert.That(cast.color.a, Is.EqualTo(0.45f).Within(0.001f));
                Assert.That(cast.sprite.bounds.size.x * cast.transform.localScale.x,
                    Is.EqualTo(8.1f).Within(0.01f));
                Assert.That(cast.sortingOrder, Is.EqualTo(tree.sortingOrder - 1));

                world.SetTimeOfDay(TimeOfDayPreset.Morning);
                Assert.That(cast.sprite.bounds.size.x * cast.transform.localScale.x,
                    Is.EqualTo(5.375f).Within(0.01f));

                world.SetTimeOfDay(TimeOfDayPreset.Night);
                Assert.That(cast.color.a, Is.EqualTo(0.02f).Within(0.001f));
                Assert.That(cast.sprite.bounds.size.x * cast.transform.localScale.x,
                    Is.EqualTo(0.95f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FloraSpriteRendererUsesNativeLitCutoutShadowReceiver()
        {
            var root = new GameObject("Native Flora Shadow Receiver Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                var flags = System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic;
                typeof(LotWorldController).GetMethod("BuildFloraRoot", flags)
                    ?.Invoke(world, null);
                world.Session.Data.Flora.Add(new PlacedFlora
                {
                    InstanceId = "native-shadow-maple",
                    FloraId = "maple",
                    PositionX = 7f,
                    PositionZ = 7f
                });
                typeof(LotWorldController).GetMethod(
                    "RebuildFloraPresentations", flags)?.Invoke(world, null);

                var tree = Find(root.transform, "Flora — maple")
                    .GetComponent<SpriteRenderer>();
                Assert.That(tree.sharedMaterial.shader.name,
                    Is.EqualTo("CityForgeV3/LitShadowReceivingSprite"));
                Assert.That(tree.sharedMaterial.GetFloat("_Cutoff"),
                    Is.EqualTo(0.02f).Within(0.001f));
                Assert.That(tree.receiveShadows, Is.True);
                Assert.That(tree.shadowCastingMode,
                    Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
                Assert.That(tree.gameObject.layer, Is.EqualTo(31));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FloraShadowLightMatchesPubPackageShadowCompass()
        {
            var root = new GameObject("Flora Shadow Compass Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                var package = HybridBuildingPackageRegistry.Load(
                    "CityForgeV3/Buildings/PubQa20DegV05/building-package");
                var flags = System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic;
                typeof(LotWorldController).GetField("_buildingPackage", flags)
                    ?.SetValue(world, package);
                typeof(LotWorldController).GetMethod("BuildLighting", flags)
                    ?.Invoke(world, null);
                world.SetTimeOfDay(TimeOfDayPreset.Afternoon);

                var lights = root.GetComponentsInChildren<Light>(true);
                var beautySun = System.Array.Find(lights,
                    light => light.name == "Time of Day Sun");
                var floraSun = System.Array.Find(lights,
                    light => light.name == "Flora Shadow Alignment Sun");
                Assert.That(beautySun, Is.Not.Null);
                Assert.That(floraSun, Is.Not.Null);
                Assert.That(beautySun.cullingMask & (1 << 31), Is.Zero);
                Assert.That(floraSun.cullingMask, Is.EqualTo(1 << 31));

                var floraRay = floraSun.transform.rotation * Vector3.forward;
                Assert.That(floraRay.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(floraRay.z, Is.GreaterThan(0.8f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OptimizedWroughtIronFenceIsAPlaceablePersistentThreeDimensionalProp()
        {
            Assert.That(Resources.Load<GameObject>(
                "CityForgeV3/Props/WroughtIronFenceV01/CF_WroughtIronFence_Straight_LOD0_v01"),
                Is.Not.Null);
            var root = new GameObject("Wrought-Iron Fence Prop Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.Session.Data.Props.Add(new PlacedProp
                {
                    InstanceId = "fence-test",
                    PropId = "wrought-iron-fence-straight-v01",
                    PositionX = 0f,
                    PositionZ = -7f
                });
                world.SetInspectionMode(BuildingInspectionMode.Artwork);
                world.SetPropEditorContext(true);
                var fence = Find(root.transform,
                    "Prop — wrought-iron-fence-straight-v01");
                Assert.That(fence, Is.Not.Null);
                Assert.That(fence.GetComponentsInChildren<MeshRenderer>().Length,
                    Is.GreaterThan(0));
                var renderer = fence.GetComponentInChildren<MeshRenderer>();
                var localCenter = fence.InverseTransformPoint(renderer.bounds.center);
                Assert.That(Mathf.Abs(localCenter.x), Is.LessThan(0.15f),
                    "The exported fence mesh must remain centered on its placement outline.");
                Assert.That(Mathf.Abs(localCenter.z), Is.LessThan(0.15f));
                Assert.That(renderer.bounds.size.x, Is.EqualTo(6.38f).Within(0.25f));
                Assert.That(renderer.bounds.size.y, Is.EqualTo(1.50f).Within(0.15f));
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Standard"));
                Assert.That(renderer.sharedMaterial.mainTexture, Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_BumpMap"), Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_MetallicGlossMap"), Is.Not.Null);
                Assert.That(world.PropCount, Is.EqualTo(1));

                var camera = root.GetComponentInChildren<Camera>();
                var before = camera.WorldToScreenPoint(renderer.bounds.center);
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var panelPosition = new Vector2(before.x, camera.pixelHeight - before.y);
                Assert.That(world.BeginPropDragFromPanel("", panelPosition, panelSize), Is.True);
                Assert.That(world.NudgeSelectedPropByScreenPixels(1, 0), Is.True);
                var after = camera.WorldToScreenPoint(renderer.bounds.center);
                Assert.That(after.x - before.x, Is.EqualTo(1f).Within(0.05f));
                Assert.That(after.y - before.y, Is.EqualTo(0f).Within(0.05f));

                var json = world.Session.Serialize();
                var restored = new LotEditorSession();
                restored.Restore(json);
                Assert.That(restored.Data.Props.Count, Is.EqualTo(1));
                Assert.That(restored.Data.Props[0].PropId,
                    Is.EqualTo("wrought-iron-fence-straight-v01"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ThreeLanternLamppostIsTexturedPersistentAndOnlyLitAfterDaytime()
        {
            Assert.That(Resources.Load<GameObject>(
                "CityForgeV3/Props/ThreeLanternLamppostV01/CF_Prop_ThreeLanternLamppost_01_game_v01"),
                Is.Not.Null);
            var root = new GameObject("Three-Lantern Lamppost Prop Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.Session.Data.Props.Add(new PlacedProp
                {
                    InstanceId = "three-lantern-test",
                    PropId = "three-lantern-lamppost-v01",
                    PositionX = 0f,
                    PositionZ = 0f
                });
                world.SetInspectionMode(BuildingInspectionMode.Artwork);
                world.SetPropEditorContext(false);
                var lamppost = Find(root.transform,
                    "Prop — three-lantern-lamppost-v01");
                Assert.That(lamppost, Is.Not.Null);
                var renderer = lamppost.GetComponentInChildren<MeshRenderer>();
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.bounds.size.y, Is.EqualTo(4.5f).Within(0.2f));
                Assert.That(renderer.sharedMaterial.mainTexture, Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_BumpMap"), Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_MetallicGlossMap"), Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_EmissionMap"), Is.Not.Null);

                var lights = lamppost.GetComponentsInChildren<Light>(true);
                Assert.That(lights.Length, Is.EqualTo(3));
                var lightPool = Find(lamppost, "CF Runtime Lantern Light Pool");
                Assert.That(lightPool, Is.Not.Null);
                Assert.That(lightPool.localScale.x,
                    Is.EqualTo(LotWorldController.ThreeLanternLightPoolDiameterMeters));
                world.SetTimeOfDay(TimeOfDayPreset.Noon);
                foreach (var light in lights) Assert.That(light.enabled, Is.False);
                Assert.That(lightPool.GetComponent<MeshRenderer>().enabled, Is.False);
                Assert.That(renderer.sharedMaterial.GetColor("_EmissionColor"),
                    Is.EqualTo(Color.black));
                world.SetTimeOfDay(TimeOfDayPreset.Evening);
                foreach (var light in lights) Assert.That(light.enabled, Is.True);
                Assert.That(lightPool.GetComponent<MeshRenderer>().enabled, Is.True);
                Assert.That(renderer.sharedMaterial.GetColor("_EmissionColor").maxColorComponent,
                    Is.GreaterThan(1f));

                var restored = new LotEditorSession();
                restored.Restore(world.Session.Serialize());
                Assert.That(restored.Data.Props[0].PropId,
                    Is.EqualTo("three-lantern-lamppost-v01"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OptimizedWroughtIronCornerIsCataloguedTexturedAndPersistent()
        {
            Assert.That(Resources.Load<GameObject>(
                "CityForgeV3/Props/WroughtIronFenceV01/CF_WroughtIronFence_Corner_LShape_LOD0_v02"),
                Is.Not.Null);
            var root = new GameObject("Wrought-Iron Corner Prop Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.Session.Data.Props.Add(new PlacedProp
                {
                    InstanceId = "fence-corner-test",
                    PropId = "wrought-iron-fence-corner-v01",
                    PositionX = 0f,
                    PositionZ = 0f
                });
                world.SetInspectionMode(BuildingInspectionMode.Artwork);
                world.SetPropEditorContext(false);
                var corner = Find(root.transform,
                    "Prop — wrought-iron-fence-corner-v01");
                Assert.That(corner, Is.Not.Null);
                var renderer = corner.GetComponentInChildren<MeshRenderer>();
                Assert.That(renderer, Is.Not.Null);
                var localCenter = corner.InverseTransformPoint(renderer.bounds.center);
                Assert.That(Mathf.Abs(localCenter.x), Is.LessThan(1.2f));
                Assert.That(Mathf.Abs(localCenter.z), Is.LessThan(1.2f));
                Assert.That(renderer.bounds.size.x, Is.EqualTo(2.55f).Within(0.2f));
                Assert.That(renderer.bounds.size.z, Is.EqualTo(2.55f).Within(0.2f));
                Assert.That(renderer.bounds.size.y, Is.EqualTo(1.50f).Within(0.15f));
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Standard"));
                Assert.That(renderer.sharedMaterial.mainTexture, Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_BumpMap"), Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_MetallicGlossMap"), Is.Not.Null);

                var camera = root.GetComponentInChildren<Camera>();
                var screen = camera.WorldToScreenPoint(renderer.bounds.center);
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var panelPosition = new Vector2(screen.x, camera.pixelHeight - screen.y);
                var beforeDrag = new Vector2(
                    world.Session.Data.Props[0].PositionX,
                    world.Session.Data.Props[0].PositionZ);
                Assert.That(world.BeginPropDragFromPanel("", panelPosition, panelSize), Is.True,
                    "The corner must remain selectable outside the Props catalog context.");
                Assert.That(world.DragPropFromPanel(
                    panelPosition + new Vector2(20f, 0f), panelSize), Is.True);
                Assert.That(world.EndPropDrag(), Is.True);
                var afterDrag = new Vector2(
                    world.Session.Data.Props[0].PositionX,
                    world.Session.Data.Props[0].PositionZ);
                Assert.That(Vector2.Distance(beforeDrag, afterDrag), Is.GreaterThan(0.01f));

                var restored = new LotEditorSession();
                restored.Restore(world.Session.Serialize());
                Assert.That(restored.Data.Props[0].PropId,
                    Is.EqualTo("wrought-iron-fence-corner-v01"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ArmedFencePlacementCanCreateMultiplePropsWithinOneLotTile()
        {
            var root = new GameObject("Dense Fence Placement Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Dense Props", LotType.Residential, 2, 2);
                world.SetPropEditorContext(true);
                world.SetPropPlacementPreview("wrought-iron-fence-straight-v01");
                var camera = root.GetComponentInChildren<Camera>();
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var preview = Find(root.transform, "Prop Placement Preview");
                Assert.That(preview, Is.Not.Null);
                foreach (var renderer in preview.GetComponentsInChildren<MeshRenderer>(true))
                    Assert.That(renderer.shadowCastingMode,
                        Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));

                Vector2 PanelPoint(float x, float z)
                {
                    var screen = camera.WorldToScreenPoint(new Vector3(x, 0f, z));
                    return new Vector2(screen.x, camera.pixelHeight - screen.y);
                }

                var first = PanelPoint(0f, -1f);
                Assert.That(world.BeginPropDragFromPanel(
                    "wrought-iron-fence-straight-v01", first, panelSize), Is.True);
                world.EndPropDrag();
                var second = PanelPoint(0f, 1f);
                Assert.That(world.BeginPropDragFromPanel(
                    "wrought-iron-fence-straight-v01", second, panelSize), Is.True,
                    "An armed prop must place another instance rather than select the first one's projected bounds.");
                world.EndPropDrag();

                Assert.That(world.PropCount, Is.EqualTo(2));
                Assert.That(Mathf.Abs(world.Session.Data.Props[0].PositionZ -
                    world.Session.Data.Props[1].PositionZ), Is.GreaterThan(1f));
                var committed = Find(root.transform,
                    "Prop — wrought-iron-fence-straight-v01");
                Assert.That(committed, Is.Not.Null);
                foreach (var renderer in committed.GetComponentsInChildren<MeshRenderer>())
                {
                    if (renderer.transform.IsChildOf(Find(committed,
                            "Projected Prop Silhouette"))) continue;
                    Assert.That(renderer.shadowCastingMode,
                        Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
                    Assert.That(renderer.receiveShadows, Is.True);
                }
                var projectedShadow = Find(root.transform, "Projected Prop Silhouette");
                Assert.That(projectedShadow, Is.Not.Null);
                var silhouetteRenderer = projectedShadow.GetComponentInChildren<MeshRenderer>();
                Assert.That(silhouetteRenderer.sharedMaterial.shader.name,
                    Is.EqualTo("CityForgeV3/ProjectedPropShadow"));
                var silhouetteMesh = projectedShadow.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.That(silhouetteMesh.vertexCount, Is.GreaterThan(1000),
                    "The shadow must reuse the detailed 3D mesh, not a rectangular hull.");
                var displacement = silhouetteRenderer.sharedMaterial.GetVector(
                    "_ShadowDisplacement");
                Assert.That(new Vector2(displacement.x, displacement.z).magnitude,
                    Is.LessThanOrEqualTo(8.501f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RotatingStraightPropRotatesSelectionFootprintExactlyOnce()
        {
            var root = new GameObject("Rotated Prop Footprint Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.Session.Data.Props.Add(new PlacedProp
                {
                    InstanceId = "rotating-fence",
                    PropId = "wrought-iron-fence-straight-v01",
                    PositionX = 0f,
                    PositionZ = 0f
                });
                world.SetInspectionMode(BuildingInspectionMode.Artwork);
                world.SetPropEditorContext(true);
                var camera = root.GetComponentInChildren<Camera>();
                var fence = Find(root.transform,
                    "Prop — wrought-iron-fence-straight-v01");
                var screen = camera.WorldToScreenPoint(
                    fence.GetComponentInChildren<MeshRenderer>().bounds.center);
                Assert.That(world.BeginPropDragFromPanel("",
                    new Vector2(screen.x, camera.pixelHeight - screen.y),
                    new Vector2(camera.pixelWidth, camera.pixelHeight)), Is.True);
                world.EndPropDrag();
                var selection = Find(root.transform, "Selected Prop Highlight");
                Bounds SelectionBounds()
                {
                    var renderers = selection.GetComponentsInChildren<Renderer>();
                    var bounds = renderers[0].bounds;
                    for (var index = 1; index < renderers.Length; index++)
                        bounds.Encapsulate(renderers[index].bounds);
                    return bounds;
                }
                var before = SelectionBounds();
                Assert.That(before.size.x, Is.GreaterThan(before.size.z * 5f));
                Assert.That(world.RotateSelectedProp(1), Is.True);
                var after = SelectionBounds();
                Assert.That(after.size.z, Is.GreaterThan(after.size.x * 5f));
                Assert.That(selection.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DirectionalBuildingShadeKeepsWorldTimePresetAcrossRotation()
        {
            Assert.That(HybridBuildingPresentation.ShadePresetForRotation(
                TimeOfDayPreset.Morning, 0), Is.EqualTo(TimeOfDayPreset.Morning));
            Assert.That(HybridBuildingPresentation.ShadePresetForRotation(
                TimeOfDayPreset.Morning, 1), Is.EqualTo(TimeOfDayPreset.Morning));
            Assert.That(HybridBuildingPresentation.ShadePresetForRotation(
                TimeOfDayPreset.Morning, 2), Is.EqualTo(TimeOfDayPreset.Morning));
            Assert.That(HybridBuildingPresentation.ShadePresetForRotation(
                TimeOfDayPreset.Morning, 3), Is.EqualTo(TimeOfDayPreset.Morning));
            Assert.That(HybridBuildingPresentation.ShadePresetForRotation(
                TimeOfDayPreset.Afternoon, 1), Is.EqualTo(TimeOfDayPreset.Afternoon));
            Assert.That(HybridBuildingPresentation.ShadePresetForRotation(
                TimeOfDayPreset.Night, 3), Is.EqualTo(TimeOfDayPreset.Night));
        }

        [Test]
        public void BuildingAndFloraShadersSupportSolidDepthOcclusion()
        {
            Assert.That(Shader.Find("CityForgeV3/BuildingDepthOccluder"), Is.Not.Null);
            Assert.That(Shader.Find("CityForgeV3/AlwaysVisibleBuildingSprite"), Is.Not.Null);
            Assert.That(Shader.Find("CityForgeV3/LitShadowReceivingSprite"), Is.Not.Null);
        }

        [Test]
        public void FloraReceiverSamplesUnityShadowMapAndClipsTransparentPixels()
        {
            var path = Path.Combine(Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Shaders/LitShadowReceivingSprite.shader");
            var source = File.ReadAllText(path);

            StringAssert.Contains("SHADOW_ATTENUATION", source);
            StringAssert.Contains("TRANSFER_SHADOW", source);
            StringAssert.Contains("multi_compile_fwdbase", source);
            StringAssert.Contains("clip(artwork.a - _Cutoff)", source);
            StringAssert.DoesNotContain("LightingShadowOnly", source,
                "The receiver must explicitly sample Unity's shadow map instead of relying on the failed custom surface-lighting callback.");
        }

        [Test]
        public void BuildingDepthOccluderWritesOnlyDepthBeforeFlora()
        {
            var path = Path.Combine(Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Shaders/BuildingDepthOccluder.shader");
            var source = File.ReadAllText(path);

            StringAssert.Contains("ColorMask 0", source);
            StringAssert.Contains("ZWrite On", source);
            StringAssert.Contains("ZTest LEqual", source);
            StringAssert.Contains("Queue\"=\"AlphaTest-10", source);
        }

        [Test]
        public void BuildingArtworkDrawsBetweenProxyDepthAndFlora()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/AlwaysVisibleBuildingSprite.shader");

            StringAssert.Contains("Queue\"=\"AlphaTest-5", source);
            StringAssert.Contains("ZWrite Off", source);
            StringAssert.Contains("ZTest Always", source);
            StringAssert.DoesNotContain("Queue\"=\"Transparent", source);
        }

        [Test]
        public void FloraShadowLightUsesAnIsolatedHighResolutionMap()
        {
            var controllerSource = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");

            StringAssert.Contains("_floraShadowSun.shadowCustomResolution = 4096", controllerSource);
            StringAssert.DoesNotContain("_sun.shadowCustomResolution = 4096", controllerSource);
        }

        [Test]
        public void FrontFloraAndCommittedPropsUseDepthAwarePriority()
        {
            var floraShader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/LitShadowReceivingSprite.shader");
            var worldSource = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var propSource = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Props.cs");

            StringAssert.Contains("ZTest [_ZTest]", floraShader);
            StringAssert.Contains("IsBeyondNearestBuildingFront", worldSource);
            StringAssert.Contains("renderQueue = 3001", worldSource);
            StringAssert.Contains("CreatePropDepthPrepass", propSource);
            StringAssert.Contains("Committed Prop Depth Prepass", propSource);
            StringAssert.Contains("renderQueue = 2435", propSource);
            StringAssert.Contains("_camera.transform.position", worldSource);
            StringAssert.Contains("Mathf.Abs(front.x) * halfWidth", worldSource);
            StringAssert.Contains("CompareFunction.Always", worldSource);
            StringAssert.Contains("material.renderQueue = 2455", propSource);
            StringAssert.Contains("IsOnNearestBuildingCameraFacingSide", propSource);
            StringAssert.Contains("ApplyFrontPropPresentationPriority", propSource);
            StringAssert.Contains("_buildingPackage.ShadowDirectionOffsetDegrees", propSource);
            StringAssert.Contains("material.renderQueue = 3000", propSource);
        }

        [Test]
        public void LotWorldAddsBuildingsUntilFullAndDeletingOneFreesItsSite()
        {
            var root = new GameObject("Multi Building Capacity Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Campus", LotType.Residential, 4, 4);
                var added = 0;
                while (added < 20 && world.PlaceBuildingAtCenter(
                           BuildingCatalog.ColonialGovernmentHouseId)) added++;

                Assert.That(added, Is.GreaterThan(1));
                Assert.That(added, Is.LessThan(20));
                Assert.That(world.BuildingCount, Is.EqualTo(added));
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.False);

                world.DeleteSelected();
                Assert.That(world.BuildingCount, Is.EqualTo(added - 1));
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                Assert.That(world.BuildingCount, Is.EqualTo(added));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuildingsModeCanSwitchAndMoveEitherOfTwoPlacedBuildings()
        {
            var root = new GameObject("Multi Building Selection Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Townhouses", LotType.Residential, 4, 4);
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.NewEnglandHouseId), Is.True);
                world.SetBuildingEditorContext(true, false);

                Assert.That(world.BuildingCount, Is.EqualTo(2));
                for (var index = 0; index < world.BuildingCount; index++)
                {
                    var placed = world.Session.Data.Buildings[index];
                    Assert.That(world.BuildingPresentationPosition(index),
                        Is.EqualTo(new Vector3(placed.CellX, 0f, placed.CellZ)));
                }

                var first = world.Session.Data.Buildings[0];
                var firstX = first.CellX;
                var secondX = world.Session.Data.Buildings[1].CellX;
                Assert.That(world.SelectBuildingAtLotPoint(
                    new Vector2(first.CellX, first.CellZ)), Is.True);
                Assert.That(world.SelectedBuildingIndex, Is.EqualTo(0));
                var moveDirection = firstX < secondX ? -1 : 1;
                world.NudgeSelected(moveDirection, 0);
                Assert.That(world.Session.Data.Buildings[0].CellX,
                    Is.EqualTo(firstX + moveDirection));
                Assert.That(world.Session.Data.Buildings[1].CellX,
                    Is.EqualTo(secondX));

                var second = world.Session.Data.Buildings[1];
                Assert.That(world.SelectBuildingAtLotPoint(
                    new Vector2(second.CellX, second.CellZ)), Is.True);
                Assert.That(world.SelectedBuildingIndex, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TrafficModeFadesEveryBuildingButKeepsBuildingSelectionAvailable()
        {
            var root = new GameObject("Traffic Building Context Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Neighborhood", LotType.Neighborhood, 4, 4);
                world.PlaceBuildingAtCenter(BuildingCatalog.ColonialGovernmentHouseId);
                world.PlaceBuildingAtCenter(BuildingCatalog.NewEnglandHouseId);
                var first = world.Session.Data.Buildings[0];
                var selectedBefore = world.SelectedBuildingIndex;

                world.SetBuildingEditorContext(false, true);
                Assert.That(world.BuildingsSelectable, Is.True);
                Assert.That(world.BuildingContextOpacity, Is.EqualTo(0.32f));
                Assert.That(world.BuildingPresentationOpacity(0), Is.EqualTo(0.32f));
                Assert.That(world.BuildingPresentationOpacity(1), Is.EqualTo(0.32f));
                Assert.That(world.SelectBuildingAtLotPoint(
                    new Vector2(first.CellX, first.CellZ)), Is.True);
                Assert.That(world.SelectedBuildingIndex, Is.EqualTo(0));

                world.SetBuildingEditorContext(true, false);
                Assert.That(world.BuildingPresentationOpacity(0), Is.EqualTo(1f));
                Assert.That(world.BuildingPresentationOpacity(1), Is.EqualTo(1f));
                Assert.That(world.SelectBuildingAtLotPoint(
                    new Vector2(first.CellX, first.CellZ)), Is.True);
                Assert.That(world.SelectedBuildingIndex, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LegacySingletonBuildingSaveMigratesIntoTheBuildingCollection()
        {
            const string legacy = "{\"Schema\":\"cityforge-v3-lot-save-v3\",\"HasBuilding\":true," +
                "\"BuildingId\":\"cityforge.building.government-house.v1\",\"CellX\":3,\"CellZ\":-2}";
            var session = new LotEditorSession();
            session.Restore(legacy);

            Assert.That(session.Data.Schema, Is.EqualTo("cityforge-v3-lot-save-v6"));
            Assert.That(session.Data.Buildings.Count, Is.EqualTo(1));
            Assert.That(session.Data.Buildings[0].CellX, Is.EqualTo(3));
            Assert.That(session.Data.Buildings[0].CellZ, Is.EqualTo(-2));
            Assert.That(session.Data.Buildings[0].InstanceId, Is.Not.Empty);
        }

        [Test]
        public void LotMovementClampsToTheInspectableLotBounds()
        {
            var session = new LotEditorSession();
            session.Place(BuildingCatalog.ColonialGovernmentHouseId, 100, -100);

            Assert.That(session.Data.CellX, Is.EqualTo(session.Data.LotWidthCells * 5));
            Assert.That(session.Data.CellZ, Is.EqualTo(-session.Data.LotDepthCells * 5));
        }

        [Test]
        public void BuildingMovementUsesCurrentRectangularLotAndRotatedFootprintBounds()
        {
            var root = new GameObject("Dynamic Building Bounds Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("4 x 3", LotType.Residential, 4, 3);
                world.PlaceBuildingAtCenter(BuildingCatalog.ColonialGovernmentHouseId);
                for (var index = 0; index < 12; index++) world.NudgeSelected(1, 0);
                Assert.That(world.BuildingCell.x, Is.EqualTo(12));
                for (var index = 0; index < 20; index++) world.NudgeSelected(1, 0);
                Assert.That(world.BuildingCell.x, Is.EqualTo(15));
                for (var index = 0; index < 20; index++) world.NudgeSelected(0, 1);
                Assert.That(world.BuildingCell.y, Is.EqualTo(11));

                world.RotateSelected(1);
                world.NudgeSelected(20, 20);
                Assert.That(world.BuildingCell.x, Is.EqualTo(16));
                Assert.That(world.BuildingCell.y, Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GovernmentHouseLeavesGAvailableForTheGridShortcut()
        {
            Assert.That(BuildingCatalog.GovernmentHouse.Shortcut, Is.EqualTo("C"));
            Assert.That(BuildingCatalog.TryFindByShortcut('G', out _), Is.False);
        }

        [Test]
        public void OwnedChoiceFieldUsesNativeDropdownSelection()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            StringAssert.Contains("new DropdownField(_choices, index)", source);
            StringAssert.Contains("public int index => Mathf.Max(0, _choices.IndexOf(_field.value))", source);
            StringAssert.DoesNotContain("overlayRoot.Add(_menu)", source);
        }

        [Test]
        public void GeneralCellDimensionsUseDirectPersistentButtons()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath,
                "CityForgeV3/Runtime/UI/CityForgeApp.cs"));

            StringAssert.Contains("new CityForgeCellCountField(", source);
            StringAssert.Contains("_pendingLotWidthCells, _pendingLotDepthCells", source);
            StringAssert.Contains("cells => _pendingLotWidthCells = cells", source);
            StringAssert.Contains("cells => _pendingLotDepthCells = cells", source);
            StringAssert.Contains("button.AddToClassList(\"cf-cell-count-option\")", source);
        }

        [Test]
        public void UnsavedDocumentModalConsumesPointerEventsAndDiscardContinuesTheAction()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            StringAssert.Contains("overlay.RegisterCallback<PointerDownEvent>", source);
            StringAssert.Contains("overlay.RegisterCallback<PointerUpEvent>", source);
            StringAssert.Contains("overlay.RegisterCallback<ClickEvent>", source);
            StringAssert.Contains("action?.Invoke();", source);
            StringAssert.Contains("CfButton.Create(\"DISCARD\", () => ContinueDocumentAction(false)", source);
            StringAssert.Contains("CfButton.Create(\"CANCEL\", CancelDocumentAction", source);
        }

        [Test]
        public void CatalogEntryUsesTheGeneratedMediumOccupancy()
        {
            var entry = BuildingCatalog.GovernmentHouse;

            Assert.That(entry.SizeClass, Is.EqualTo("medium"));
            Assert.That(entry.OccupancyWidth, Is.EqualTo(1));
            Assert.That(entry.OccupancyDepth, Is.EqualTo(1));
            Assert.That(entry.PackageResourcePath, Is.Not.Empty);
        }

        [Test]
        public void BlenderAuthoredProxyContainsTheRequiredSemanticParts()
        {
            var proxy = Resources.Load<GameObject>(
                HybridBuildingPackageRegistry.GovernmentHouse
                    .PrimitiveResourcePath);

            Assert.That(proxy, Is.Not.Null);
            Assert.That(Find(proxy.transform, "CF_PROXY_FOUNDATION"), Is.Not.Null);
            Assert.That(Find(proxy.transform, "CF_PROXY_WALLS"), Is.Not.Null);
            var roof = Find(proxy.transform, "CF_PROXY_ROOF_GABLE");
            Assert.That(roof, Is.Not.Null);
            Assert.That(
                roof.GetComponent<MeshFilter>().sharedMesh.triangles.Length,
                Is.EqualTo(24),
                "The roof proxy must remain a triangular prism.");
            Assert.That(Find(proxy.transform, "CF_ANCHOR_ENTRANCE"), Is.Not.Null);
        }

        [Test]
        public void MetricGridSeparatesOneMeterDetailFromTenMeterPlanning()
        {
            Assert.That(LotMetricScale.MinorGridMeters, Is.EqualTo(1f));
            Assert.That(LotMetricScale.MajorGridMeters, Is.EqualTo(10f));
            Assert.That(LotMetricScale.ShowsMinorGrid(LotZoomLevel.Detail), Is.True);
            Assert.That(LotMetricScale.ShowsMinorGrid(LotZoomLevel.Lot), Is.True);
            Assert.That(LotMetricScale.ShowsMinorGrid(LotZoomLevel.Neighborhood), Is.False);
            Assert.That(LotMetricScale.OrthographicSize(LotZoomLevel.Detail),
                Is.LessThan(LotMetricScale.OrthographicSize(LotZoomLevel.Lot)));
            Assert.That(LotMetricScale.OrthographicSize(LotZoomLevel.Lot),
                Is.LessThan(LotMetricScale.OrthographicSize(LotZoomLevel.Neighborhood)));
        }

        [Test]
        public void LotWorldAppliesDualGridVisibilityAtEachZoomLevel()
        {
            var root = new GameObject("Dual Grid Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                Assert.That(world.MinorGridVisible, Is.True);
                Assert.That(world.MajorGridVisible, Is.True);
                world.ToggleGridVisibility();
                Assert.That(world.GridVisible, Is.False);
                Assert.That(world.MinorGridVisible, Is.False);
                Assert.That(world.MajorGridVisible, Is.False);
                world.ToggleGridVisibility();
                Assert.That(world.GridVisible, Is.True);
                Assert.That(world.MinorGridVisible, Is.True);
                Assert.That(world.MajorGridVisible, Is.True);
                world.SetZoomLevel(LotZoomLevel.Neighborhood);
                Assert.That(world.MinorGridVisible, Is.False);
                Assert.That(world.MajorGridVisible, Is.True);
                world.SetZoomLevel(LotZoomLevel.Detail);
                Assert.That(world.MinorGridVisible, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EveryLotTypeHasAValidExplicitRoadAccessContract()
        {
            foreach (LotType lotType in System.Enum.GetValues(typeof(LotType)))
            {
                var contract = LotTypeCatalog.For(lotType);
                Assert.That(contract.Type, Is.EqualTo(lotType));
                Assert.That(contract.IsValid, Is.True, lotType.ToString());
                Assert.That(contract.RoadPorts, Is.Not.Empty, lotType.ToString());
            }
            Assert.That(LotTypeCatalog.For(LotType.Residential).AllowsThroughTraffic, Is.False);
            Assert.That(LotTypeCatalog.For(LotType.Business).AllowsInternalRoads, Is.True);
            Assert.That(LotTypeCatalog.For(LotType.Neighborhood).AllowsThroughTraffic, Is.True);
        }

        [Test]
        public void LotTypePersistsInTheSerializedLotContract()
        {
            var source = new LotEditorSession();
            source.SetLotType(LotType.Neighborhood);
            var restored = new LotEditorSession();
            restored.Restore(source.Serialize());
            Assert.That(restored.Data.LotType, Is.EqualTo(LotType.Neighborhood));
        }

        [Test]
        public void EmptyLotLifecycleAndTrafficTemplateAreExplicitOperations()
        {
            var root = new GameObject("Lot Lifecycle Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("My Empty Lot", LotType.Neighborhood, 80);
                Assert.That(world.CurrentLotName, Is.EqualTo("My Empty Lot"));
                Assert.That(world.PlacedRoadCount, Is.Zero);
                Assert.That(world.HasBuilding, Is.False);
                Assert.That(world.PedestrianNodeCount, Is.Zero);
                Assert.That(world.VehicleLaneCount, Is.Zero);
                Assert.That(world.HasUnsavedChanges, Is.False);

                world.ApplyTrafficTestTemplate();
                Assert.That(world.CurrentLotName, Is.EqualTo("Two-Way Traffic Test"));
                Assert.That(world.PlacedRoadCount, Is.EqualTo(21));
                Assert.That(world.TrafficIntersectionCount, Is.EqualTo(1));
                Assert.That(world.HasUnsavedChanges, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void VersionedLotFilesSaveListAndLoadWithoutEmbeddingAssets()
        {
            var folder = Path.Combine(Path.GetTempPath(),
                $"cityforge-lot-save-{System.Guid.NewGuid():N}");
            try
            {
                var source = new LotEditorSession();
                source.NewLot("Market Square", LotType.Business, 40);
                source.Place(BuildingCatalog.ColonialGovernmentHouseId, 1, 2);
                Assert.That(source.IsDirty, Is.True);
                var path = LotSaveStore.Save(source,
                    new[] { "cityforge.base.road.brick.v1", "ford-model-t-1920s" }, folder);
                Assert.That(File.Exists(path), Is.True);
                Assert.That(source.IsDirty, Is.False);

                var summaries = LotSaveStore.List(folder);
                Assert.That(summaries.Count, Is.EqualTo(1));
                Assert.That(summaries[0].Name, Is.EqualTo("Market Square"));
                Assert.That(summaries[0].LotSizeMeters, Is.EqualTo(40));

                var restored = new LotEditorSession();
                Assert.That(LotSaveStore.Load(restored, "market-square", folder), Is.True);
                Assert.That(restored.Data.Schema, Is.EqualTo("cityforge-v3-lot-save-v6"));
                Assert.That(restored.Data.HasBuilding, Is.True);
                Assert.That(restored.Data.RequiredPackageIds,
                    Is.EquivalentTo(new[] { "cityforge.base.road.brick.v1", "ford-model-t-1920s" }));
                Assert.That(restored.IsDirty, Is.False);
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void SavedLotsCanRenameDuplicateDeleteAndReportMissingPackages()
        {
            var folder = Path.Combine(Path.GetTempPath(),
                $"cityforge-lot-library-{System.Guid.NewGuid():N}");
            try
            {
                var source = new LotEditorSession();
                source.NewLot("Harbor Ward", LotType.Neighborhood, 80);
                LotSaveStore.Save(source, new[] { "road.available", "vehicle.missing" }, folder);

                source.Rename("Harbor District");
                LotSaveStore.Save(source, source.Data.RequiredPackageIds, folder);
                Assert.That(LotSaveStore.List(folder)[0].Name, Is.EqualTo("Harbor District"));

                var copy = LotSaveStore.Duplicate(source.Data.LotId, folder);
                Assert.That(copy, Is.Not.Null);
                Assert.That(copy.LotId, Is.Not.EqualTo(source.Data.LotId));
                Assert.That(copy.Name, Is.EqualTo("Harbor District Copy"));
                Assert.That(LotSaveStore.List(folder).Count, Is.EqualTo(2));

                var missing = LotSaveStore.MissingDependencies(
                    source.Data.LotId, new[] { "road.available" }, folder);
                Assert.That(missing, Is.EqualTo(new[] { "vehicle.missing" }));

                Assert.That(LotSaveStore.Delete(copy.LotId, folder), Is.True);
                Assert.That(LotSaveStore.Delete(copy.LotId, folder), Is.False);
                Assert.That(LotSaveStore.List(folder).Count, Is.EqualTo(1));
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void SaveAsForksStableIdentityAndPreservesOriginalFile()
        {
            var folder = Path.Combine(Path.GetTempPath(),
                $"cityforge-lot-fork-{System.Guid.NewGuid():N}");
            try
            {
                var source = new LotEditorSession();
                source.NewLot("Original", LotType.Business, 30);
                var originalId = source.Data.LotId;
                LotSaveStore.Save(source, System.Array.Empty<string>(), folder);

                source.ForkAs("Original Variant", LotSaveStore.UniqueId("Original Variant", folder));
                LotSaveStore.Save(source, System.Array.Empty<string>(), folder);

                Assert.That(source.Data.LotId, Is.Not.EqualTo(originalId));
                Assert.That(LotSaveStore.Read(originalId, folder).Name, Is.EqualTo("Original"));
                Assert.That(LotSaveStore.Read(source.Data.LotId, folder).Name,
                    Is.EqualTo("Original Variant"));
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void RectangularLotContractPersistsIndependentWidthDepthAndExpandedType()
        {
            var session = new LotEditorSession();
            session.NewLot("Canal Exchange", LotType.Transportation, 20);
            session.SetLotDimensions(5, 6);
            session.SetEra("industrial");
            var restored = new LotEditorSession();
            restored.Restore(session.Serialize());

            Assert.That(restored.Data.LotWidthCells, Is.EqualTo(5));
            Assert.That(restored.Data.LotDepthCells, Is.EqualTo(6));
            Assert.That(restored.Data.LotSizeMeters, Is.EqualTo(60));
            Assert.That(restored.Data.LotType, Is.EqualTo(LotType.Transportation));
            Assert.That(restored.Data.EraId, Is.EqualTo("industrial"));
            Assert.That(LotEraCatalog.DisplayName(restored.Data.EraId),
                Is.EqualTo("Industrial Age"));
            Assert.That(LotTypeCatalog.For(LotType.Industrial).DisplayName,
                Is.EqualTo("INDUSTRIAL LOT"));
            Assert.That(LotTypeCatalog.For(LotType.Mixed).DisplayName,
                Is.EqualTo("MIXED-USE LOT"));
        }

        [Test]
        public void LegacySquareV2SaveMigratesToV3Dimensions()
        {
            const string legacy = "{\"Schema\":\"cityforge-v3-lot-save-v2\",\"LotSizeMeters\":40,\"LotType\":1}";
            var restored = new LotEditorSession();
            restored.Restore(legacy);

            Assert.That(restored.Data.Schema, Is.EqualTo("cityforge-v3-lot-save-v6"));
            Assert.That(restored.Data.LotWidthCells, Is.EqualTo(4));
            Assert.That(restored.Data.LotDepthCells, Is.EqualTo(4));
            Assert.That(restored.Data.LotType, Is.EqualTo(LotType.Commercial));
            Assert.That(restored.Data.EraId, Is.EqualTo(LotEraCatalog.DefaultId));
        }

        [Test]
        public void RectangularRoadCoordinatesClampEachAxisIndependently()
        {
            Assert.That(RoadPlacementModel.WorldToCell(99f, 99f, 50, 60),
                Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(RoadPlacementModel.WorldToCell(-99f, -99f, 50, 60),
                Is.EqualTo(new Vector2Int(-2, -3)));
            Assert.That(RoadPlacementModel.CellCenterMeters(1, 2, 50, 60),
                Is.EqualTo(new Vector2(10f, 25f)));
        }

        [TestCase(30, -15, true)]
        [TestCase(30, -5, true)]
        [TestCase(30, 0, false)]
        [TestCase(40, -20, true)]
        [TestCase(40, -10, true)]
        [TestCase(40, 0, true)]
        [TestCase(50, -25, true)]
        [TestCase(50, -15, true)]
        [TestCase(50, 0, false)]
        [TestCase(50, 25, true)]
        public void MajorGridLinesStartAtLotEdgeForOddAndEvenDimensions(
            int lotSizeMeters, int positionMeters, bool expected)
        {
            Assert.That(LotWorldController.IsMajorGridLine(positionMeters, lotSizeMeters),
                Is.EqualTo(expected));
        }

        [Test]
        public void FiveCellRoadCentersSitBetweenMajorGridBoundaries()
        {
            for (var cell = -2; cell <= 2; cell++)
            {
                var center = RoadPlacementModel.CellCenterMeters(cell, cell, 50, 50);
                Assert.That(center.x, Is.EqualTo(cell * 10f));
                Assert.That(center.y, Is.EqualTo(cell * 10f));
                Assert.That(LotWorldController.IsMajorGridLine((int)center.x, 50), Is.False);
            }
        }

        [Test]
        public void NeighborhoodLotActivatesConnectedRoadAndTrafficSlice()
        {
            var root = new GameObject("Neighborhood Road Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotType(LotType.Neighborhood);
                Assert.That(world.NeighborhoodRoadVisible, Is.True);
                Assert.That(world.PrimaryRoadPort, Is.Not.Null);
                Assert.That(world.PrimaryRoadPort.Id, Is.EqualTo("south-main"));
                Assert.That(world.PrimaryRoadPort.WidthMeters, Is.EqualTo(6f));
                Assert.That(world.PrimaryRoadPort.LaneCount, Is.EqualTo(2));
                Assert.That(Find(root.transform, "Internal Two-Lane Road"), Is.Not.Null);
                Assert.That(Find(root.transform, "Traffic Vehicle"), Is.Null,
                    "Vehicle traffic is now driven by the placed-road graph traveler.");
                world.SetLotType(LotType.Residential);
                Assert.That(world.NeighborhoodRoadVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GovernmentHouseHasCompleteDayAndNightLightingLayers()
        {
            var package = HybridBuildingPackageRegistry.GovernmentHouse;
            for (var facingIndex = 0; facingIndex < package.FacingCount; facingIndex++)
            {
                var facing = package.Facing(facingIndex);
                Assert.That(Resources.Load<Texture2D>(facing.NeutralResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.NightOverlayResourcePath), Is.Not.Null);
                for (var timeIndex = 0; timeIndex < 4; timeIndex++)
                {
                    var preset = (TimeOfDayPreset)timeIndex;
                    var path = facing.ShadeResourcePath(preset);
                    Assert.That(path, Is.Not.Empty, $"{facing.Id} {preset}");
                    var texture = Resources.Load<Texture2D>(path);
                    Assert.That(texture, Is.Not.Null, $"{facing.Id} {preset}");
                    Assert.That(texture.width, Is.EqualTo(2048));
                    Assert.That(texture.height, Is.EqualTo(2048));
                }
            }
        }

        [Test]
        public void SharedCirculationGraphSupportsPedestrianAndVehicleContracts()
        {
            var pedestrian = new CirculationNetwork { Mode = CirculationMode.Pedestrian };
            var a = pedestrian.AddNode(Vector2.zero);
            var b = pedestrian.AddNode(Vector2.right * 4f);
            var segment = pedestrian.Connect(a.Id, b.Id, CirculationDirection.StartToEnd);
            Assert.That(segment, Is.Not.Null);
            Assert.That(segment.WidthMeters, Is.EqualTo(1.5f));
            Assert.That(segment.SpeedMetersPerSecond, Is.EqualTo(1.4f));
            Assert.That(pedestrian.Validate(), Is.Empty);

            var vehicle = new CirculationNetwork { Mode = CirculationMode.Vehicle };
            a = vehicle.AddNode(Vector2.zero);
            b = vehicle.AddNode(Vector2.up * 8f);
            segment = vehicle.Connect(a.Id, b.Id);
            Assert.That(segment.WidthMeters, Is.EqualTo(3f));
            Assert.That(segment.SpeedMetersPerSecond, Is.EqualTo(5.5f));
            Assert.That(vehicle.Validate(), Is.Empty);
        }

        [Test]
        public void CirculationGraphRejectsBrokenReferencesAndDeletesConnectedSegments()
        {
            var network = new CirculationNetwork { Mode = CirculationMode.Pedestrian };
            var a = network.AddNode(Vector2.zero);
            var b = network.AddNode(Vector2.one);
            network.Connect(a.Id, b.Id);
            Assert.That(network.DeleteNode(a.Id), Is.True);
            Assert.That(network.Segments, Is.Empty);
            network.Segments.Add(new CirculationSegment
            {
                Id = "broken",
                StartNodeId = "missing",
                EndNodeId = b.Id
            });
            Assert.That(network.Validate(), Has.Some.Contains("Broken node reference"));
        }

        [Test]
        public void PedestrianAndVehicleNetworksRoundTripWithTheLotSave()
        {
            var source = new LotEditorSession();
            CirculationDefaults.SeedVerticalSlice(source.Data);
            var restored = new LotEditorSession();
            restored.Restore(source.Serialize());
            Assert.That(restored.Data.PedestrianNetwork.Mode, Is.EqualTo(CirculationMode.Pedestrian));
            Assert.That(restored.Data.PedestrianNetwork.Nodes.Count, Is.EqualTo(3));
            Assert.That(restored.Data.PedestrianNetwork.Segments.Count, Is.EqualTo(2));
            Assert.That(restored.Data.VehicleNetwork.Mode, Is.EqualTo(CirculationMode.Vehicle));
            Assert.That(restored.Data.VehicleNetwork.Nodes.Count, Is.EqualTo(3));
            Assert.That(restored.Data.VehicleNetwork.Segments.Count, Is.EqualTo(2));
            Assert.That(restored.Data.PedestrianNetwork.Validate(), Is.Empty);
            Assert.That(restored.Data.VehicleNetwork.Validate(), Is.Empty);
        }

        [Test]
        public void CirculationVerticalSliceBuildsBothNetworksAndTravelers()
        {
            var root = new GameObject("Circulation Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                Assert.That(world.PedestrianNodeCount, Is.EqualTo(3));
                Assert.That(world.PedestrianSegmentCount, Is.EqualTo(2));
                Assert.That(world.VehicleNodeCount, Is.EqualTo(4));
                Assert.That(world.VehicleSegmentCount, Is.EqualTo(4));
                Assert.That(world.VehicleRoutePointCount, Is.EqualTo(16));
                Assert.That(world.VehicleRouteLengthMeters, Is.GreaterThan(20f));
                Assert.That(world.VehicleLaneCount, Is.EqualTo(2));
                Assert.That(world.VehicleDirectedSegmentCount, Is.EqualTo(32));
                Assert.That(world.AverageVehicleSpeedMetersPerSecond, Is.GreaterThan(0f));
                Assert.That(world.MinimumVehicleGapMeters, Is.GreaterThan(6f));
                Assert.That(Find(root.transform, "Pedestrian Traveler"), Is.Not.Null);
                Assert.That(world.VehiclePresentationCount, Is.EqualTo(4));
                Assert.That(Find(root.transform, "Vehicle Traveler — Green"), Is.Not.Null);
                Assert.That(Find(root.transform, "Circulation Cursor"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CirculationCursorIsVisibleOnlyInPathsWorkspace()
        {
            var root = new GameObject("Circulation Context Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetCirculationMode(CirculationMode.Pedestrian);
                var cursor = Find(root.transform, "Circulation Cursor");
                Assert.That(cursor, Is.Not.Null);

                world.SetCirculationEditorContext(true);
                Assert.That(cursor.gameObject.activeSelf, Is.True);

                world.SetCirculationEditorContext(false);
                Assert.That(cursor.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ModelTPilotLoadsWithDriverAndFourAnimationReadyWheels()
        {
            var asset = Resources.Load<GameObject>(VehicleRuntimePresentation.ModelTResourcePath);
            Assert.That(asset, Is.Not.Null);
            var root = new GameObject("Model T Presentation Test");
            try
            {
                var presentation = VehicleRuntimePresentation.Create(root.transform);
                Assert.That(presentation.transform.localScale,
                    Is.EqualTo(Vector3.one * VehicleRuntimePresentation.PresentationScale));
                Assert.That(VehicleRuntimePresentation.PresentationScale,
                    Is.EqualTo(1.28f));
                Assert.That(presentation.VisualRoot, Is.Not.Null);
                var importedNames = string.Join(", ",
                    System.Array.ConvertAll(presentation.GetComponentsInChildren<Transform>(true),
                        item => item.name));
                Assert.That(presentation.RollingWheelCount, Is.EqualTo(4), importedNames);
                Assert.That(presentation.SteeringPivotCount, Is.EqualTo(2), importedNames);
                Assert.That(presentation.FrontAxleBrace, Is.Not.Null,
                    "Separated front wheel meshes need a visible structural axle.");
                Assert.That(VehicleRuntimePresentation.ModelYawOffsetDegrees, Is.EqualTo(-90f),
                    "The Blender +X nose must align with Unity route-forward +Z.");
                Assert.That(Find(presentation.VisualRoot, "CF_ModelT_LOD0_body"), Is.Not.Null);
                Assert.That(presentation.GetComponentsInChildren<Renderer>(true).Length,
                    Is.GreaterThanOrEqualTo(5));
                Assert.That(presentation.HeadlightCount, Is.EqualTo(2));
                Assert.That(presentation.HeadlightsEnabled, Is.False,
                    "Headlights should remain off during daylight presets.");
                Assert.That(presentation.ShadowCastingRendererCount, Is.GreaterThanOrEqualTo(5));
                Assert.That(presentation.ShadowProxyRendererCount, Is.EqualTo(7));
                Assert.That(presentation.ShadowProxyRoot, Is.Not.Null);
                Assert.That(presentation.ContactShadow, Is.Not.Null);
                var contactRenderer = presentation.ContactShadow.GetComponent<Renderer>();
                Assert.That(contactRenderer.sharedMaterial.shader.name,
                    Is.EqualTo("CityForgeV3/VehicleContactShadow"));
                Assert.That(contactRenderer.sharedMaterial.renderQueue, Is.EqualTo(3100),
                    "The contact shadow must render after Geometry+2 road artwork.");
                Assert.That(contactRenderer.sortingOrder,
                    Is.EqualTo(VehicleRuntimePresentation.ContactShadowSortingOrder),
                    "Road multi-pass rendering must not overdraw the vehicle contact decal.");
                Assert.That(contactRenderer.sharedMaterial.GetColor("_Color").a,
                    Is.GreaterThanOrEqualTo(0.4f),
                    "The contact shadow must remain readable on unselected gray roads.");
                foreach (var renderer in presentation.ShadowProxyRoot.GetComponentsInChildren<Renderer>())
                    Assert.That(renderer.shadowCastingMode,
                        Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly));
                Assert.That(presentation.ShadowProxyRoot.localScale.y,
                    Is.EqualTo(VehicleRuntimePresentation.ShadowProxyHeightScale));
                foreach (var renderer in presentation.VisualRoot.GetComponentsInChildren<Renderer>())
                    Assert.That(renderer.shadowCastingMode,
                        Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off),
                        "The detailed open mesh must not duplicate the closed proxy shadow.");
                foreach (var headlight in presentation.GetComponentsInChildren<Light>(true))
                {
                    Assert.That(headlight.type, Is.EqualTo(LightType.Spot));
                    Assert.That(headlight.shadows, Is.EqualTo(LightShadows.Soft));
                }
                presentation.SetTimeOfDay(TimeOfDayPreset.Evening);
                Assert.That(presentation.HeadlightsEnabled, Is.True);
                Assert.That(presentation.GetComponentsInChildren<Light>(true)[0].intensity,
                    Is.EqualTo(2.5f));
                presentation.SetTimeOfDay(TimeOfDayPreset.Morning);
                Assert.That(presentation.HeadlightsEnabled, Is.False);
                var initialWheelRotation = presentation.RollingWheels[0].localRotation;
                presentation.Place(Vector2.zero, Vector2.up);
                presentation.Place(Vector2.up, Vector2.up);
                Assert.That(presentation.RollingWheels[0].localRotation,
                    Is.Not.EqualTo(initialWheelRotation));
                presentation.Place(Vector2.up * 2f, Vector2.up, 24f);
                Assert.That(presentation.AppliedVisualSteeringDegrees, Is.EqualTo(-10f),
                    "Imported pivots must invert the route turn sign after axis correction.");
                for (var index = 0; index < presentation.SteeringPivots.Count; index++)
                {
                    var angle = Quaternion.Angle(
                        Quaternion.identity,
                        presentation.SteeringPivots[index].localRotation);
                    Assert.That(angle, Is.LessThanOrEqualTo(
                        VehicleRuntimePresentation.MaximumVisualSteeringDegrees + 0.1f));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(VehiclePaintVariant.Green)]
        [TestCase(VehiclePaintVariant.Blue)]
        [TestCase(VehiclePaintVariant.Red)]
        [TestCase(VehiclePaintVariant.Yellow)]
        public void ModelTPaintVariantsUseMaskedRealtimeLitBodyMaterial(
            VehiclePaintVariant variant)
        {
            var root = new GameObject($"Model T {variant} Test");
            try
            {
                var presentation = VehicleRuntimePresentation.Create(root.transform, variant);
                Assert.That(presentation.PaintVariant, Is.EqualTo(variant));
                var body = Find(presentation.VisualRoot, "CF_ModelT_LOD0_body");
                Assert.That(body, Is.Not.Null);
                var renderer = body.GetComponent<Renderer>();
                Assert.That(renderer.sharedMaterial.shader.name,
                    Is.EqualTo("CityForgeV3/VehiclePaint"));
                Assert.That(renderer.sharedMaterial.GetColor("_PaintColor"),
                    Is.EqualTo(VehicleRuntimePresentation.PaintColor(variant)));
                Assert.That(renderer.receiveShadows, Is.True);
                Assert.That(renderer.shadowCastingMode,
                    Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off),
                    "The closed proxy, not the open paint mesh, owns the vehicle shadow.");
                Assert.That(presentation.HasBlackRoof,
                    Is.EqualTo(variant is VehiclePaintVariant.Blue or VehiclePaintVariant.Red));
                Assert.That(renderer.sharedMaterial.GetFloat("_BlackRoof"),
                    Is.EqualTo(presentation.HasBlackRoof ? 1f : 0f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void VehicleRouteSamplesAClosedSmoothedRightHandLane()
        {
            var network = new CirculationNetwork { Mode = CirculationMode.Vehicle };
            var southwest = network.AddNode(new Vector2(-5f, -5f));
            var northwest = network.AddNode(new Vector2(-5f, 5f));
            var northeast = network.AddNode(new Vector2(5f, 5f));
            var southeast = network.AddNode(new Vector2(5f, -5f));
            network.Connect(southwest.Id, northwest.Id);
            network.Connect(northwest.Id, northeast.Id);
            network.Connect(northeast.Id, southeast.Id);
            network.Connect(southeast.Id, southwest.Id);

            var route = VehicleRoute.FromNetwork(network);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.IsClosed, Is.True);
            Assert.That(route.Points.Count, Is.EqualTo(16));
            Assert.That(route.TotalLengthMeters, Is.GreaterThan(20f));
            route.Sample(0f, out var start, out var direction);
            route.Sample(route.TotalLengthMeters, out var wrapped, out _);
            Assert.That(Vector2.Distance(start, wrapped), Is.LessThan(0.001f));
            Assert.That(direction.sqrMagnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(Mathf.Abs(route.SteeringDegrees(0f)), Is.GreaterThan(0.1f));
            Assert.That(VehicleRoute.HeadingLookAroundMeters, Is.EqualTo(2f));
            var maximumHeadingStep = 0f;
            var previousHeading = route.SmoothedDirection(0f);
            for (var distance = 0.25f; distance <= route.TotalLengthMeters;
                 distance += 0.25f)
            {
                var nextHeading = route.SmoothedDirection(distance);
                maximumHeadingStep = Mathf.Max(maximumHeadingStep,
                    Mathf.Abs(Vector2.SignedAngle(previousHeading, nextHeading)));
                previousHeading = nextHeading;
            }
            Assert.That(maximumHeadingStep, Is.LessThan(12f),
                "Vehicle heading should rotate progressively through curves.");
        }

        [Test]
        public void RoadTrafficGraphBuildsTwoLegalOpposingLanes()
        {
            var network = new CirculationNetwork { Mode = CirculationMode.Vehicle };
            var southwest = network.AddNode(new Vector2(-5f, -5f));
            var northwest = network.AddNode(new Vector2(-5f, 5f));
            var northeast = network.AddNode(new Vector2(5f, 5f));
            var southeast = network.AddNode(new Vector2(5f, -5f));
            network.Connect(southwest.Id, northwest.Id);
            network.Connect(northwest.Id, northeast.Id);
            network.Connect(northeast.Id, southeast.Id);
            network.Connect(southeast.Id, southwest.Id);

            var graph = RoadTrafficGraph.FromRoadNetwork(network, RoadPiecePackage.Load());
            Assert.That(graph.LaneCount, Is.EqualTo(2));
            Assert.That(graph.DirectedSegmentCount, Is.EqualTo(32));
            Assert.That(graph.SpeedMetersPerSecond, Is.EqualTo(5.5f));
            Assert.That(graph.Routes[0].Clockwise, Is.True);
            Assert.That(graph.Routes[1].Clockwise, Is.False);
            Assert.That(Vector2.Distance(graph.Routes[0].Points[0],
                graph.Routes[1].Points[0]), Is.GreaterThan(1.5f));
        }

        [Test]
        public void FourLaneBoulevardBuildsTwoRoutesPerDirection()
        {
            var network = new CirculationNetwork { Mode = CirculationMode.Vehicle };
            var southwest = network.AddNode(new Vector2(-10f, -10f));
            var northwest = network.AddNode(new Vector2(-10f, 10f));
            var northeast = network.AddNode(new Vector2(10f, 10f));
            var southeast = network.AddNode(new Vector2(10f, -10f));
            network.Connect(southwest.Id, northwest.Id);
            network.Connect(northwest.Id, northeast.Id);
            network.Connect(northeast.Id, southeast.Id);
            network.Connect(southeast.Id, southwest.Id);

            var package = RoadPiecePackageCatalog.Resolve(
                RoadPiecePackageCatalog.DividedBoulevardId);
            var graph = RoadTrafficGraph.FromRoadNetwork(network, package);
            Assert.That(graph.LaneCount, Is.EqualTo(4));
            Assert.That(graph.Routes[0].Clockwise, Is.True);
            Assert.That(graph.Routes[1].Clockwise, Is.False);
            Assert.That(graph.Routes[2].Clockwise, Is.True);
            Assert.That(graph.Routes[3].Clockwise, Is.False);
            Assert.That(Vector2.Distance(graph.Routes[0].Points[0],
                graph.Routes[2].Points[0]), Is.GreaterThan(2f));
        }

        [Test]
        public void RoadTrafficGraphPublishesYieldControlledTIntersectionMovements()
        {
            var network = new CirculationNetwork { Mode = CirculationMode.Vehicle };
            var center = network.AddNode(Vector2.zero);
            var north = network.AddNode(Vector2.up * 10f);
            var south = network.AddNode(Vector2.down * 10f);
            var west = network.AddNode(Vector2.left * 10f);
            var boundary = network.AddNode(Vector2.left * 15f,
                CirculationNodeKind.LotBoundaryPort, "west-access");
            network.Connect(center.Id, north.Id);
            network.Connect(center.Id, south.Id);
            network.Connect(center.Id, west.Id);
            network.Connect(west.Id, boundary.Id);

            var package = RoadPiecePackage.Load();
            var graph = RoadTrafficGraph.FromRoadNetwork(network, package);
            Assert.That(graph.IntersectionCount, Is.EqualTo(1));
            var intersection = graph.Intersections[0];
            Assert.That(intersection.ApproachCount, Is.EqualTo(3));
            Assert.That(intersection.LegalTurnMovementCount, Is.EqualTo(6));
            Assert.That(intersection.MinorApproachControl, Is.EqualTo("yield"));
            Assert.That(intersection.ControlledApproachNodeId, Is.EqualTo(west.Id));
        }

        [Test]
        public void LaneTrafficMaintainsFollowingDistanceAndBrakesForSlowerTraffic()
        {
            var network = new CirculationNetwork { Mode = CirculationMode.Vehicle };
            var southwest = network.AddNode(new Vector2(-10f, -10f));
            var northwest = network.AddNode(new Vector2(-10f, 10f));
            var northeast = network.AddNode(new Vector2(10f, 10f));
            var southeast = network.AddNode(new Vector2(10f, -10f));
            network.Connect(southwest.Id, northwest.Id);
            network.Connect(northwest.Id, northeast.Id);
            network.Connect(northeast.Id, southeast.Id);
            network.Connect(southeast.Id, southwest.Id);
            var graph = RoadTrafficGraph.FromRoadNetwork(network, RoadPiecePackage.Load());
            var vehicleType = VehicleTypePackage.LoadModelT();
            var states = LaneTrafficModel.Seed(2, graph, vehicleType);
            states[0].LaneIndex = 0;
            states[1].LaneIndex = 0;
            states[0].DistanceMeters = 10f;
            states[0].SpeedMetersPerSecond = 5f;
            states[1].DistanceMeters = 17f;
            states[1].SpeedMetersPerSecond = 1f;
            states[1].DesiredSpeedMetersPerSecond = 1f;

            LaneTrafficModel.Step(states, graph, vehicleType, 0.1f);

            Assert.That(states[0].Braking, Is.True);
            Assert.That(states[0].SpeedMetersPerSecond, Is.LessThan(5f));
            Assert.That(states[0].GapAheadMeters, Is.GreaterThanOrEqualTo(0f));
            Assert.That(states[1].Braking, Is.False);
        }

        [Test]
        public void ModelTTrafficBehaviorLoadsFromVersionedJsonPackage()
        {
            var package = VehicleTypePackage.LoadModelT();
            Assert.That(package.Id, Is.EqualTo("ford-model-t-1920s"));
            Assert.That(package.Validate(), Is.Empty);
            Assert.That(package.LengthMeters, Is.EqualTo(3.4f));
            Assert.That(package.MinimumStoppedGapMeters, Is.EqualTo(2f));
            Assert.That(package.FollowingTimeSeconds, Is.EqualTo(0.9f));
            Assert.That(package.AccelerationMetersPerSecondSquared, Is.EqualTo(1.25f));
            Assert.That(package.ComfortableBrakeMetersPerSecondSquared, Is.EqualTo(3.2f));
            Assert.That(package.ModelResourcePath,
                Is.EqualTo(VehicleRuntimePresentation.ModelTResourcePath));
        }

        [Test]
        public void CirculationEditorAddsConnectedMetricNodes()
        {
            var root = new GameObject("Circulation Editing Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetCirculationMode(CirculationMode.Pedestrian);
                var initialNodes = world.PedestrianNodeCount;
                var initialSegments = world.PedestrianSegmentCount;
                world.NudgeCirculationCursor(2, 1);
                world.AddCirculationNode();
                world.NudgeCirculationCursor(1, 0);
                world.AddCirculationNode();
                Assert.That(world.PedestrianNodeCount, Is.EqualTo(initialNodes + 2));
                Assert.That(world.PedestrianSegmentCount, Is.EqualTo(initialSegments + 1));
                Assert.That(world.CirculationCursorMeters, Is.EqualTo(new Vector2(3f, 1f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BrickRoadPackageRegistersTheLatestCoherentAuthoredSet()
        {
            var package = RoadPiecePackage.Load();
            Assert.That(package.Id, Is.EqualTo("cityforge.base.road.brick.v1"));
            Assert.That(package.TileSizeMeters, Is.EqualTo(10f));
            Assert.That(package.RoadWidthMeters, Is.EqualTo(3.8f));
            Assert.That(package.Pieces.Count, Is.EqualTo(5));
            Assert.That(package.Validate(), Is.Empty);
            Assert.That(package.Piece(RoadPieceTopology.Straight).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.TJunction).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.FourWay).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.Corner).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.Endpoint).HasArtwork, Is.False);
            Assert.That(package.Piece(RoadPieceTopology.Corner).ArtworkStatus, Is.EqualTo("authored-2026-07-28"));
            Assert.That(package.Piece(RoadPieceTopology.Endpoint).ArtworkStatus, Is.EqualTo("pending"));
        }

        [Test]
        public void FlatColorRoadFamiliesLoadEveryTopologyAndBehaviorContract()
        {
            var ids = new[]
            {
                RoadPiecePackageCatalog.TwoLaneSidewalkId,
                RoadPiecePackageCatalog.OneWaySidewalkId,
                RoadPiecePackageCatalog.AlleyId,
                RoadPiecePackageCatalog.PedestrianStreetId
            };
            foreach (var id in ids)
            {
                var package = RoadPiecePackageCatalog.Resolve(id);
                Assert.That(package.Id, Is.EqualTo(id));
                Assert.That(package.Pieces.Count, Is.EqualTo(5));
                Assert.That(package.Validate(), Is.Empty, id);
                foreach (RoadPieceTopology topology in System.Enum.GetValues(typeof(RoadPieceTopology)))
                    Assert.That(package.Piece(topology)?.HasArtwork, Is.True,
                        $"{id} {topology}");
            }
            Assert.That(RoadPiecePackageCatalog.Resolve(
                RoadPiecePackageCatalog.TwoLaneSidewalkId).LaneCount, Is.EqualTo(2));
            Assert.That(RoadPiecePackageCatalog.Resolve(
                RoadPiecePackageCatalog.OneWaySidewalkId).TrafficDirection,
                Is.EqualTo("one_way"));
            Assert.That(RoadPiecePackageCatalog.Resolve(
                RoadPiecePackageCatalog.PedestrianStreetId).AllowsVehicles, Is.False);
        }

        [Test]
        public void DividedBoulevardPublishesATwoCellRectangularStraightContract()
        {
            var package = RoadPiecePackageCatalog.Resolve(
                RoadPiecePackageCatalog.DividedBoulevardId);
            Assert.That(package.Validate(), Is.Empty);
            Assert.That(package.ArtworkWidthMeters, Is.EqualTo(20f));
            Assert.That(package.ArtworkLengthMeters, Is.EqualTo(10f));
            Assert.That(package.OccupancyCrossCells, Is.EqualTo(2));
            Assert.That(package.LaneCount, Is.EqualTo(4));
            Assert.That(package.LaneOffsetsMeters, Is.EqualTo(new[] { 3.175f, 6.925f }));
            Assert.That(package.SupportsIndependentMarkings, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.Straight).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.Corner).HasArtwork, Is.False);
        }

        [Test]
        public void WideAvenuePublishesMaterialReadyStraightArtwork()
        {
            var package = RoadPiecePackageCatalog.Resolve(
                RoadPiecePackageCatalog.WideTwoLaneAvenueId);
            Assert.That(package.Validate(), Is.Empty);
            Assert.That(package.ArtworkWidthMeters, Is.EqualTo(20f));
            Assert.That(package.ArtworkLengthMeters, Is.EqualTo(10f));
            Assert.That(package.OccupancyCrossCells, Is.EqualTo(2));
            Assert.That(package.LaneCount, Is.EqualTo(4));
            Assert.That(package.LaneOffsetsMeters, Is.EqualTo(new[] { 2.05f, 6.15f }));
            Assert.That(package.SupportsIndependentMarkings, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.Straight).HasArtwork, Is.True);
        }

        [Test]
        public void WideRoadsPublishEveryIndependentMarkingCombination()
        {
            foreach (var packageId in new[]
                     {
                         RoadPiecePackageCatalog.DividedBoulevardId,
                         RoadPiecePackageCatalog.WideTwoLaneAvenueId
                     })
            {
                var piece = RoadPiecePackageCatalog.Resolve(packageId)
                    .Piece(RoadPieceTopology.Straight);
                foreach (var suffix in new[]
                         {
                             "-lanes-double-center", "-lanes-no-center",
                             "-no-lanes-double-center", "-no-lanes-no-center"
                         })
                    Assert.That(Resources.Load<Texture2D>(piece.ResourcePath + suffix),
                        Is.Not.Null, $"{packageId} {suffix}");
            }
        }

        [Test]
        public void MixedRoadFamiliesPersistPerTileAndLegacyTilesMigrateToBrick()
        {
            var source = new LotEditorSession();
            RoadPlacementModel.PlaceOrReplace(source.Data.RoadPieces,
                RoadPieceTopology.Straight, -1, 0, 0, 20,
                RoadPiecePackageCatalog.AlleyId);
            RoadPlacementModel.PlaceOrReplace(source.Data.RoadPieces,
                RoadPieceTopology.Endpoint, 0, 0, 1, 20,
                RoadPiecePackageCatalog.PedestrianStreetId);
            var restored = new LotEditorSession();
            restored.Restore(source.Serialize());
            Assert.That(restored.Data.RoadPieces[0].PackageId,
                Is.EqualTo(RoadPiecePackageCatalog.AlleyId));
            Assert.That(restored.Data.RoadPieces[1].PackageId,
                Is.EqualTo(RoadPiecePackageCatalog.PedestrianStreetId));

            const string legacy = "{\"RoadPieces\":[{\"Id\":\"legacy\",\"Topology\":0,\"GridX\":-1,\"GridZ\":0}]}";
            restored.Restore(legacy);
            Assert.That(restored.Data.RoadPieces[0].PackageId,
                Is.EqualTo(RoadPiecePackage.LegacyPackageId));
        }

        [Test]
        public void PedestrianStreetTilesDoNotEnterTheVehicleNetwork()
        {
            var pieces = new System.Collections.Generic.List<PlacedRoadPiece>();
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.Straight,
                -1, 0, 1, 20, RoadPiecePackageCatalog.TwoLaneSidewalkId);
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.Straight,
                0, 0, 1, 20, RoadPiecePackageCatalog.PedestrianStreetId);
            var network = RoadPlacementModel.BuildVehicleNetwork(pieces,
                piece => RoadPiecePackageCatalog.Resolve(piece.PackageId), 20, 20);
            Assert.That(network.Nodes.Count, Is.EqualTo(2),
                "Only the vehicle tile center and its exterior boundary port belong in the vehicle graph.");
            Assert.That(network.Nodes.Exists(node => node.PositionMeters.x > 0f), Is.False);
        }

        [Test]
        public void OneWayRoadFamilyPublishesOneDirectedRoute()
        {
            var network = new CirculationNetwork { Mode = CirculationMode.Vehicle };
            var southwest = network.AddNode(new Vector2(-5f, -5f));
            var northwest = network.AddNode(new Vector2(-5f, 5f));
            var northeast = network.AddNode(new Vector2(5f, 5f));
            var southeast = network.AddNode(new Vector2(5f, -5f));
            network.Connect(southwest.Id, northwest.Id);
            network.Connect(northwest.Id, northeast.Id);
            network.Connect(northeast.Id, southeast.Id);
            network.Connect(southeast.Id, southwest.Id);
            var graph = RoadTrafficGraph.FromRoadNetwork(network,
                RoadPiecePackageCatalog.Resolve(RoadPiecePackageCatalog.OneWaySidewalkId));
            Assert.That(graph.LaneCount, Is.EqualTo(1));
            Assert.That(graph.Routes[0].Clockwise, Is.True);
            Assert.That(graph.SpeedMetersPerSecond, Is.EqualTo(7f));
        }

        [Test]
        public void TestVehicleLibrarySpawnsChosenVariantsAndRemovesThem()
        {
            var root = new GameObject("Test Vehicle Library Runtime Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Open Road Vehicle Playground",
                    LotType.Neighborhood, 20);
                var network = world.Session.Data.VehicleNetwork;
                network.Nodes.Clear();
                network.Segments.Clear();
                var west = network.AddNode(new Vector2(-8f, 0f));
                var bend = network.AddNode(new Vector2(0f, 0f));
                var north = network.AddNode(new Vector2(0f, 8f));
                network.Connect(west.Id, bend.Id);
                network.Connect(bend.Id, north.Id);

                Assert.That(world.CanSpawnTestVehicle, Is.True);
                Assert.That(world.SpawnTestVehicle(VehiclePaintVariant.Red), Is.True);
                Assert.That(world.SpawnTestVehicle(VehiclePaintVariant.Blue), Is.True);
                Assert.That(world.TestVehicleCount, Is.EqualTo(2));
                var travelers = root.GetComponentsInChildren<VehicleRuntimePresentation>();
                Assert.That(System.Array.Exists(travelers,
                    vehicle => vehicle.gameObject.name.Contains("Red") &&
                               vehicle.PaintVariant == VehiclePaintVariant.Red), Is.True);
                Assert.That(System.Array.Exists(travelers,
                    vehicle => vehicle.gameObject.name.Contains("Blue") &&
                               vehicle.PaintVariant == VehiclePaintVariant.Blue), Is.True);

                world.RemoveTestVehicles();
                Assert.That(world.TestVehicleCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LotWorldRendersMixedRoadFamiliesAndAdoptsSelectedTileFamily()
        {
            var root = new GameObject("Mixed Road Family Runtime Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Mixed Roads", LotType.Neighborhood, 20);
                world.SelectRoadCellAtWorld(-5f, -5f);
                world.SelectRoadPackage(RoadPiecePackageCatalog.AlleyId);
                world.SelectRoadPiece(RoadPieceTopology.Straight);
                Assert.That(world.PlaceRoadPiece(), Is.True);
                world.SelectRoadCellAtWorld(5f, -5f);
                world.SelectRoadPackage(RoadPiecePackageCatalog.PedestrianStreetId);
                world.SelectRoadPiece(RoadPieceTopology.Straight);
                Assert.That(world.PlaceRoadPiece(), Is.True);
                Assert.That(Find(root.transform, "Alley Straight"), Is.Not.Null);
                Assert.That(Find(root.transform, "Pedestrian Street Straight"), Is.Not.Null);
                world.SelectRoadCellAtWorld(-5f, -5f);
                Assert.That(world.SelectedRoadPackageId,
                    Is.EqualTo(RoadPiecePackageCatalog.AlleyId));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoadPiecePortsRotateWithTheirArtwork()
        {
            var straight = RoadPiecePackage.Load().Piece(RoadPieceTopology.Straight);
            CollectionAssert.AreEquivalent(
                new[] { RoadPiecePort.East, RoadPiecePort.West },
                straight.RotatedPorts(1));
            var tee = RoadPiecePackage.Load().Piece(RoadPieceTopology.TJunction);
            CollectionAssert.AreEquivalent(
                new[] { RoadPiecePort.South, RoadPiecePort.West, RoadPiecePort.North },
                tee.RotatedPorts(1));
        }

        [Test]
        public void NeighborhoodLotShowsAuthoredRoadArtAboveTheSimulationLayer()
        {
            var root = new GameObject("Road Artwork Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                Assert.That(world.RoadArtworkVisible, Is.False);
                Assert.That(world.CirculationDiagnosticsVisible, Is.False);
                world.SetLotType(LotType.Neighborhood);
                Assert.That(world.RoadArtworkVisible, Is.True);
                Assert.That(Find(root.transform, "Colonial Brick Road Corner"), Is.Not.Null);
                Assert.That(
                    Resources.Load<Texture2D>(world.RoadPackage.Piece(RoadPieceTopology.FourWay).ResourcePath),
                    Is.Not.Null,
                    "The registered four-way remains available to the upcoming road-piece placement tool.");
                world.ToggleCirculationDiagnostics();
                Assert.That(world.CirculationDiagnosticsVisible, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlacedRoadPiecesRoundTripWithCellTopologyAndRotation()
        {
            var source = new LotEditorSession();
            RoadPlacementModel.PlaceOrReplace(source.Data.RoadPieces,
                RoadPieceTopology.TJunction, -1, 0, 3);
            var restored = new LotEditorSession();
            restored.Restore(source.Serialize());
            Assert.That(restored.Data.RoadPieces.Count, Is.EqualTo(1));
            Assert.That(restored.Data.RoadPieces[0].Topology, Is.EqualTo(RoadPieceTopology.TJunction));
            Assert.That(restored.Data.RoadPieces[0].GridX, Is.EqualTo(-1));
            Assert.That(restored.Data.RoadPieces[0].GridZ, Is.Zero);
            Assert.That(restored.Data.RoadPieces[0].RotationQuarterTurns, Is.EqualTo(3));
        }

        [Test]
        public void RoadPlacementReplacesAndDeletesTheOccupiedMajorGridCell()
        {
            var pieces = new System.Collections.Generic.List<PlacedRoadPiece>();
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.Straight, -1, -1, 0);
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.FourWay, -1, -1, 2);
            Assert.That(pieces.Count, Is.EqualTo(1));
            Assert.That(pieces[0].Topology, Is.EqualTo(RoadPieceTopology.FourWay));
            Assert.That(pieces[0].RotationQuarterTurns, Is.EqualTo(2));
            Assert.That(RoadPlacementModel.DeleteAt(pieces, -1, -1), Is.True);
            Assert.That(pieces, Is.Empty);
        }

        [Test]
        public void RoadValidationAcceptsMatchedPortsAndReportsUnmatchedInternalPorts()
        {
            var package = RoadPiecePackage.Load();
            var pieces = new System.Collections.Generic.List<PlacedRoadPiece>();
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.Straight, -1, -1, 0);
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.TJunction, -1, 0, 0);
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.Straight, 0, -1, 0);
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.FourWay, 0, 0, 0);
            Assert.That(RoadPlacementModel.Validate(pieces, package), Is.Empty);
            RoadPlacementModel.DeleteAt(pieces, 0, -1);
            Assert.That(RoadPlacementModel.Validate(pieces, package),
                Has.Some.Contains("Unmatched South port at 0,0"));
        }

        [Test]
        public void ConnectedRoadPiecesDeriveVehicleNodesSegmentsAndBoundaryExits()
        {
            var package = RoadPiecePackage.Load();
            var pieces = new System.Collections.Generic.List<PlacedRoadPiece>();
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.Straight, -1, -1, 0);
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.TJunction, -1, 0, 0);
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.Straight, 0, -1, 0);
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.FourWay, 0, 0, 0);
            var network = RoadPlacementModel.BuildVehicleNetwork(pieces, package);
            Assert.That(network.Nodes.Count, Is.EqualTo(9));
            Assert.That(network.Segments.Count, Is.EqualTo(8));
            Assert.That(network.Nodes.FindAll(node => node.Kind == CirculationNodeKind.LotBoundaryPort).Count,
                Is.EqualTo(5));
            Assert.That(network.Validate(), Is.Empty);
        }

        [Test]
        public void RoadEditorMovesRotatesPlacesAndDeletesOnTheMajorGrid()
        {
            var root = new GameObject("Road Editor Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotType(LotType.Neighborhood);
                Assert.That(world.LotSizeMeters, Is.EqualTo(80));
                Assert.That(world.LotMajorCellCount, Is.EqualTo(8));
                Assert.That(world.PlacedRoadCount, Is.EqualTo(21));
                Assert.That(world.RoadValidationIssues, Is.Empty);
                Assert.That(world.TrafficIntersectionCount, Is.EqualTo(1));
                world.NudgeRoadCursor(1, 0);
                world.SelectRoadPiece(RoadPieceTopology.TJunction);
                world.RotateRoadPiece(1);
                Assert.That(world.PlaceRoadPiece(), Is.True);
                Assert.That(world.PlacedRoadCount, Is.EqualTo(22));
                var repaired = RoadPlacementModel.FindAt(
                    world.Session.Data.RoadPieces,
                    world.RoadCursorCell.x,
                    world.RoadCursorCell.y);
                Assert.That(world.RoadRotationQuarterTurns,
                    Is.EqualTo(repaired.RotationQuarterTurns));
                Assert.That(world.DeleteRoadPiece(), Is.True);
                Assert.That(world.PlacedRoadCount, Is.EqualTo(21));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoadPalettePaintsImmediatelyIntoTheHighlightedCell()
        {
            var root = new GameObject("Road Palette Paint Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotType(LotType.Neighborhood);
                world.SetLotSizeMeters(40);
                world.SelectRoadCellAtWorld(-15f, 15f);
                Assert.That(world.PaintRoadPiece(RoadPieceTopology.TJunction), Is.True);
                var painted = RoadPlacementModel.FindAt(
                    world.Session.Data.RoadPieces, -2, 1);
                Assert.That(painted, Is.Not.Null);
                Assert.That(painted.Topology, Is.EqualTo(RoadPieceTopology.TJunction));
                Assert.That(world.PlacedRoadCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AutomaticRoadRepairChoosesEachAvailableAuthoredTopology()
        {
            var package = RoadPiecePackage.Load();
            void AssertCenterShape(
                RoadPieceTopology expected,
                params Vector2Int[] neighbors)
            {
                var pieces = new System.Collections.Generic.List<PlacedRoadPiece>();
                RoadPlacementModel.PlaceOrReplace(
                    pieces, RoadPieceTopology.FourWay, 0, 0, 0, 40);
                foreach (var neighbor in neighbors)
                    RoadPlacementModel.PlaceOrReplace(
                        pieces, RoadPieceTopology.Endpoint,
                        neighbor.x, neighbor.y, 0, 40);
                RoadPlacementModel.RepairConnectedTopologies(pieces, package, 40);
                Assert.That(RoadPlacementModel.FindAt(pieces, 0, 0).Topology,
                    Is.EqualTo(expected));
            }

            // The authored brick family does not yet include a matching end cap,
            // so repair preserves the current piece rather than creating invisible art.
            AssertCenterShape(RoadPieceTopology.FourWay, Vector2Int.up);
            AssertCenterShape(RoadPieceTopology.Straight, Vector2Int.up, Vector2Int.down);
            AssertCenterShape(RoadPieceTopology.Corner, Vector2Int.up, Vector2Int.right);
            AssertCenterShape(RoadPieceTopology.TJunction,
                Vector2Int.up, Vector2Int.right, Vector2Int.down);
            AssertCenterShape(RoadPieceTopology.FourWay,
                Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left);
        }

        [Test]
        public void RoadPaintUndoAndRedoRestoreTheEditedNetwork()
        {
            var root = new GameObject("Road Undo Redo Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotSizeMeters(40);
                world.SelectRoadCellAtWorld(-15f, 15f);
                Assert.That(world.PaintRoadPiece(RoadPieceTopology.Straight), Is.True);
                Assert.That(world.PlacedRoadCount, Is.EqualTo(5));
                Assert.That(world.CanUndoRoadEdit, Is.True);
                Assert.That(world.UndoRoadEdit(), Is.True);
                Assert.That(world.PlacedRoadCount, Is.EqualTo(4));
                Assert.That(world.CanRedoRoadEdit, Is.True);
                Assert.That(world.RedoRoadEdit(), Is.True);
                Assert.That(world.PlacedRoadCount, Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RotateActsOnTheHighlightedPlacedRoadAndSupportsUndo()
        {
            var root = new GameObject("Placed Road Rotation Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SelectRoadCellAtWorld(-5f, -5f);
                var placed = RoadPlacementModel.FindAt(
                    world.Session.Data.RoadPieces, -1, -1);
                var original = placed.RotationQuarterTurns;
                Assert.That(world.RotateRoadPiece(1), Is.True);
                Assert.That(placed.RotationQuarterTurns,
                    Is.EqualTo(FiveBayHybridContract.WrapFacing(original + 1)));
                Assert.That(world.UndoRoadEdit(), Is.True);
                placed = RoadPlacementModel.FindAt(
                    world.Session.Data.RoadPieces, -1, -1);
                Assert.That(placed.RotationQuarterTurns, Is.EqualTo(original));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RotateOnAnEmptyCellChangesOnlyThePlacementPreview()
        {
            var root = new GameObject("Road Preview Rotation Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotSizeMeters(40);
                world.SelectRoadCellAtWorld(-15f, 15f);
                Assert.That(world.RotateRoadPiece(1), Is.False);
                Assert.That(world.RoadRotationQuarterTurns, Is.EqualTo(1));
                Assert.That(RoadPlacementModel.FindAt(
                    world.Session.Data.RoadPieces, -2, 1), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(LotZoomLevel.Detail, -1, LotZoomLevel.Detail)]
        [TestCase(LotZoomLevel.Detail, 1, LotZoomLevel.Close)]
        [TestCase(LotZoomLevel.Close, 1, LotZoomLevel.Near)]
        [TestCase(LotZoomLevel.Near, -1, LotZoomLevel.Close)]
        [TestCase(LotZoomLevel.Near, 1, LotZoomLevel.Lot)]
        [TestCase(LotZoomLevel.Lot, -1, LotZoomLevel.Near)]
        [TestCase(LotZoomLevel.Lot, 1, LotZoomLevel.Wide)]
        [TestCase(LotZoomLevel.Wide, 1, LotZoomLevel.Far)]
        [TestCase(LotZoomLevel.Far, -1, LotZoomLevel.Wide)]
        [TestCase(LotZoomLevel.Far, 1, LotZoomLevel.Neighborhood)]
        [TestCase(LotZoomLevel.Neighborhood, 1, LotZoomLevel.Neighborhood)]
        public void ViewportZoomStepsAndClamps(
            LotZoomLevel current, int direction, LotZoomLevel expected)
        {
            Assert.That(LotWorldController.NextZoomLevel(current, direction),
                Is.EqualTo(expected));
        }

        [TestCase(LotZoomLevel.Lot, 20, 11.5f)]
        [TestCase(LotZoomLevel.Lot, 30, 16.5f)]
        [TestCase(LotZoomLevel.Lot, 40, 22f)]
        [TestCase(LotZoomLevel.Neighborhood, 40, 40f)]
        [TestCase(LotZoomLevel.Detail, 40, 8.5f)]
        [TestCase(LotZoomLevel.Detail, 80, 8.5f)]
        [TestCase(LotZoomLevel.Close, 80, 16.042f)]
        [TestCase(LotZoomLevel.Lot, 80, 26.098f)]
        [TestCase(LotZoomLevel.Wide, 80, 37.83f)]
        [TestCase(LotZoomLevel.Neighborhood, 80, 80f)]
        public void CameraFitScalesWithExpandedLots(
            LotZoomLevel level, int lotSizeMeters, float expectedSize)
        {
            Assert.That(
                LotWorldController.OrthographicSizeForLot(level, lotSizeMeters),
                Is.EqualTo(expectedSize).Within(0.001f));
        }

        [TestCase(20, 2.4f)]
        [TestCase(30, 3.6f)]
        [TestCase(40, 4.8f)]
        [TestCase(80, 9.6f)]
        public void CameraCompositionOffsetsExpandedLotsAwayFromWestPanels(
            int lotSizeMeters, float expectedOffset)
        {
            Assert.That(
                LotWorldController.CameraFramingOffsetMeters(lotSizeMeters),
                Is.EqualTo(expectedOffset).Within(0.001f));
        }

        [Test]
        public void DetailViewUsesFixedRightInspectorClearanceInsteadOfLotScaledOffset()
        {
            Assert.That(LotWorldController.DetailInspectorClearanceMeters, Is.EqualTo(3f));
            Assert.That(LotWorldController.DetailInspectorClearanceMeters,
                Is.LessThan(LotWorldController.CameraFramingOffsetMeters(80)));
        }

        [Test]
        public void EmptySelectionArrowPansLotWithoutChangingIsometricRotation()
        {
            var root = new GameObject("Lot Camera Pan Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                var camera = root.GetComponentInChildren<Camera>(true);
                var initialPosition = camera.transform.position;
                var initialRotation = camera.transform.rotation;
                world.PanCameraViewport(1, 0);
                Assert.That(world.CameraPanWorld.magnitude, Is.EqualTo(5f).Within(0.001f));
                Assert.That(camera.transform.position, Is.Not.EqualTo(initialPosition));
                Assert.That(camera.transform.rotation, Is.EqualTo(initialRotation));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EscapeContractClearsBuildingRoadAndPathSelectionsTogether()
        {
            var root = new GameObject("Deselect All Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.PlaceGovernmentHouseAtCenter();
                world.SelectRoadPiece(RoadPieceTopology.Straight);
                world.SetCirculationMode(CirculationMode.Vehicle);
                Assert.That(world.IsSelected, Is.True);
                Assert.That(world.RoadCursorSelected, Is.True);
                Assert.That(world.CirculationCursorSelected, Is.True);
                world.DeselectAll();
                Assert.That(world.IsSelected, Is.False);
                Assert.That(world.RoadCursorSelected, Is.False);
                Assert.That(world.CirculationCursorSelected, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoadArtworkReceivesVehicleShadowsAndAdditiveHeadlights()
        {
            var shader = Shader.Find("CityForgeV3/ShadowReceivingRoadOverlay");
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.passCount, Is.EqualTo(2));
        }

        [TestCase(20, 100f)]
        [TestCase(40, 140f)]
        [TestCase(80, 280f)]
        public void CameraFarPlaneScalesBeyondExpandedLotCorners(int meters, float expected)
        {
            Assert.That(LotWorldController.FarClipPlaneForLot(meters), Is.EqualTo(expected));
        }

        [TestCase(26f, 20, 26f)]
        [TestCase(26f, 40, 26f)]
        [TestCase(26f, 80, 120f)]
        public void LargeLotsMoveTheCameraBackWithoutChangingItsIsometricAngle(
            float authoredRadius, int meters, float expected)
        {
            Assert.That(LotWorldController.CameraRadiusForLot(authoredRadius, meters),
                Is.EqualTo(expected));
        }

        [Test]
        public void HoveringRoadCellsDoesNotRetargetTheDetailCamera()
        {
            var root = new GameObject("Stable Detail Camera Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotType(LotType.Neighborhood);
                world.SelectRoadCellAtWorld(5f, 5f);
                world.SetZoomLevel(LotZoomLevel.Detail);
                var camera = Find(root.transform, "Lot Camera");
                var position = camera.position;
                var rotation = camera.rotation;

                Assert.That(world.SelectRoadCellAtWorld(-35f, 35f, false), Is.True);
                Assert.That(camera.position, Is.EqualTo(position));
                Assert.That(camera.rotation, Is.EqualTo(rotation));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClickingWorldGridCellsSelectsEachRoadTileForRepeatedDeletion()
        {
            var root = new GameObject("Direct Road Selection Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotType(LotType.Neighborhood);

                Assert.That(world.SelectRoadCellAtWorld(-15f, -25f), Is.True);
                Assert.That(world.RoadCursorCell, Is.EqualTo(new Vector2Int(-2, -3)));
                Assert.That(world.DeleteRoadPiece(), Is.True);
                Assert.That(world.SelectRoadCellAtWorld(-5f, -25f), Is.True);
                Assert.That(world.RoadCursorCell, Is.EqualTo(new Vector2Int(-1, -3)));
                Assert.That(world.DeleteRoadPiece(), Is.True);
                Assert.That(world.PlacedRoadCount, Is.EqualTo(19));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SelectedRoadCellUsesAVisibleFilledHighlightAboveRoadArtwork()
        {
            var root = new GameObject("Road Cell Highlight Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotType(LotType.Neighborhood);
                Assert.That(world.SelectRoadCellAtWorld(5f, 5f), Is.True);
                Assert.That(world.RoadCursorHighlightVisible, Is.True);
                var highlight = Find(root.transform, "Selected Road Cell Highlight");
                Assert.That(highlight, Is.Not.Null);
                Assert.That(highlight.GetComponent<Renderer>().sharedMaterial.color.a,
                    Is.GreaterThanOrEqualTo(0.20f));
                Assert.That(highlight.GetComponent<Renderer>().sharedMaterial.renderQueue,
                    Is.GreaterThan(2002));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SilentPointerStyleRoadSelectionStillActivatesTheOutline()
        {
            var root = new GameObject("Silent Road Selection Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotType(LotType.Neighborhood);
                Assert.That(world.SelectRoadCellAtWorld(-15f, -25f, false), Is.True);
                Assert.That(world.HasSelectedRoadPiece, Is.True);
                Assert.That(world.RoadCursorHighlightVisible, Is.True);
                Assert.That(world.DeleteRoadPiece(), Is.True);
                Assert.That(world.HasSelectedRoadPiece, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TrafficTypeAndDirectionalOutsideConnectorsRoundTripWithTheLot()
        {
            var source = new LotEditorSession();
            source.SetTrafficType(TrafficLotType.SuburbanStreet);
            source.Data.OutsideRoadConnectors.Add(new OutsideRoadConnector
            {
                Id = "west-entry",
                GridX = -1,
                GridZ = 0,
                Edge = RoadPiecePort.West,
                Flow = RoadTrafficFlow.InboundOnly
            });
            var restored = new LotEditorSession();
            restored.Restore(source.Serialize());

            Assert.That(restored.Data.Schema, Is.EqualTo("cityforge-v3-lot-save-v6"));
            Assert.That(restored.Data.TrafficType,
                Is.EqualTo(TrafficLotType.SuburbanStreet));
            Assert.That(restored.Data.OutsideRoadConnectors.Count, Is.EqualTo(1));
            Assert.That(restored.Data.OutsideRoadConnectors[0].Flow,
                Is.EqualTo(RoadTrafficFlow.InboundOnly));
        }

        [Test]
        public void SuburbanTrafficRequiresValidInboundAndOutboundBoundaryConnectors()
        {
            var root = new GameObject("Suburban Connector Traffic Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Suburban Street", LotType.Neighborhood, 4, 4);
                world.Session.Data.RoadPieces.Clear();
                for (var x = -2; x <= 1; x++)
                    RoadPlacementModel.PlaceOrReplace(world.Session.Data.RoadPieces,
                        RoadPieceTopology.Straight, x, 0, 1, 40);
                world.SetTrafficType(TrafficLotType.SuburbanStreet);
                Assert.That(world.TryBeginSuburbanTrip(), Is.False,
                    "Unflagged boundary roads must never spawn unexplained traffic.");

                var west = RoadPlacementModel.CellCenterMeters(-2, 0, 40, 40);
                world.SelectRoadCellAtWorld(west.x, west.y);
                Assert.That(world.SelectedRoadCanConnectOutside, Is.True);
                Assert.That(world.SetSelectedOutsideConnector(
                    RoadTrafficFlow.InboundOnly), Is.True);
                Assert.That(world.TryBeginSuburbanTrip(), Is.False,
                    "A car needs somewhere valid to leave the lot.");

                var east = RoadPlacementModel.CellCenterMeters(1, 0, 40, 40);
                world.SelectRoadCellAtWorld(east.x, east.y);
                Assert.That(world.SetSelectedOutsideConnector(
                    RoadTrafficFlow.OutboundOnly), Is.True);
                Assert.That(world.OutsideConnectorCount, Is.EqualTo(2));
                Assert.That(world.OutsideConnectorMarkerCount, Is.EqualTo(2));
                Assert.That(world.TryBeginSuburbanTrip(), Is.True);
                Assert.That(world.SuburbanVehicleActive, Is.True);
                Assert.That(world.SuburbanTripLengthMeters, Is.GreaterThan(30f));

                Assert.That(world.RemoveSelectedOutsideConnector(), Is.True);
                Assert.That(world.OutsideConnectorCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DraggingStraightBoundaryRoadOutsideCreatesGreenTwoWayConnector()
        {
            var root = new GameObject("Road Drag Outside Connector Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Outside Connection", LotType.Neighborhood, 4, 4);
                world.Session.Data.RoadPieces.Clear();
                world.Session.Data.OutsideRoadConnectors.Clear();
                for (var x = -2; x <= 1; x++)
                    RoadPlacementModel.PlaceOrReplace(world.Session.Data.RoadPieces,
                        RoadPieceTopology.Straight, x, 0, 1, 40, 40,
                        RoadPiecePackageCatalog.TwoLaneSidewalkId);

                var east = RoadPlacementModel.CellCenterMeters(1, 0, 40, 40);
                Assert.That(world.SelectRoadCellAtWorld(east.x, east.y), Is.True);
                Assert.That(world.TryCreateOutsideConnectorFromDrag(15f, 21f), Is.False,
                    "Leaving through a side without the road's exterior port must not connect.");
                Assert.That(world.TryCreateOutsideConnectorFromDrag(21f, 0f), Is.True);
                Assert.That(world.OutsideConnectorCount, Is.EqualTo(1));
                Assert.That(world.OutsideConnectorMarkerCount, Is.EqualTo(1));
                Assert.That(world.Session.Data.OutsideRoadConnectors[0].Flow,
                    Is.EqualTo(RoadTrafficFlow.TwoWay));
                Assert.That(world.Session.Data.OutsideRoadConnectors[0].Edge,
                    Is.EqualTo(RoadPiecePort.East));
                Assert.That(world.TryCreateOutsideConnectorFromDrag(22f, 0f), Is.True);
                Assert.That(world.OutsideConnectorCount, Is.EqualTo(1),
                    "Continuing the same drag must not duplicate the connector.");

                var marker = Find(root.transform, "Outside Connector TwoWay");
                Assert.That(marker, Is.Not.Null);
                var markerColor = marker.GetComponentInChildren<Renderer>().sharedMaterial.color;
                Assert.That(markerColor.g, Is.GreaterThan(markerColor.r));
                Assert.That(markerColor.g, Is.GreaterThan(markerColor.b));

                world.EndRoadPaintStroke();
                Assert.That(world.UndoRoadEdit(), Is.True);
                Assert.That(world.OutsideConnectorCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TrafficProfileCatalogUsesTheThreeInitialLotBehaviors()
        {
            Assert.That(TrafficLotModel.DisplayName(TrafficLotType.None), Is.EqualTo("None"));
            Assert.That(TrafficLotModel.DisplayName(TrafficLotType.SuburbanStreet),
                Is.EqualTo("Suburban Street"));
            Assert.That(TrafficLotModel.ForDisplayName("Parking Lot"),
                Is.EqualTo(TrafficLotType.ParkingLot));
            Assert.That(TrafficLotModel.SuburbanMinimumSpawnSeconds,
                Is.GreaterThanOrEqualTo(8f));
            Assert.That(TrafficLotModel.SuburbanMaximumActiveVehicles,
                Is.LessThanOrEqualTo(2));
        }

        [TestCase(400f, 300f, 800f, 600f, 1920f, 1080f, 960f, 540f)]
        [TestCase(200f, 150f, 400f, 300f, 1920f, 1080f, 960f, 540f)]
        [TestCase(100f, 75f, 400f, 300f, 1920f, 1080f, 480f, 810f)]
        public void PanelPointersNormalizeIntoCameraPixelsAcrossGameViewScales(
            float pointerX, float pointerY,
            float panelWidth, float panelHeight,
            float cameraWidth, float cameraHeight,
            float expectedX, float expectedY)
        {
            var pixel = LotWorldController.PanelToCameraPixel(
                new Vector2(pointerX, pointerY),
                new Vector2(panelWidth, panelHeight),
                new Vector2(cameraWidth, cameraHeight));
            Assert.That(pixel.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(pixel.y, Is.EqualTo(expectedY).Within(0.001f));
        }

        [TestCase(20, -1, 0, 4)]
        [TestCase(30, -1, 1, 9)]
        [TestCase(40, -2, 1, 16)]
        [TestCase(80, -4, 3, 64)]
        public void LotSizePresetsControlMajorGridAndRoadCellBounds(
            int meters, int minimumCell, int maximumCell, int majorCellCount)
        {
            var session = new LotEditorSession();
            session.SetLotSizeMeters(meters);
            Assert.That(session.Data.LotSizeMeters, Is.EqualTo(meters));
            Assert.That(RoadPlacementModel.MinimumCellForLot(meters), Is.EqualTo(minimumCell));
            Assert.That(RoadPlacementModel.MaximumCellForLot(meters), Is.EqualTo(maximumCell));
            Assert.That((meters / 10) * (meters / 10), Is.EqualTo(majorCellCount));

            var restored = new LotEditorSession();
            restored.Restore(session.Serialize());
            Assert.That(restored.Data.LotSizeMeters, Is.EqualTo(meters));
        }

        [Test]
        public void FortyMeterLotSelectsOuterRoadCellsWithoutClampingToLegacyBounds()
        {
            var root = new GameObject("Expanded Lot Selection Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetLotSizeMeters(40);
                Assert.That(world.LotMajorCellCount, Is.EqualTo(4));
                Assert.That(world.SelectRoadCellAtWorld(-15f, 15f), Is.True);
                Assert.That(world.RoadCursorCell, Is.EqualTo(new Vector2Int(-2, 1)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NeighborAwareRoadSuggestionMapsOneThroughFourPorts()
        {
            var package = RoadPiecePackage.Load();
            var pieces = new System.Collections.Generic.List<PlacedRoadPiece>();
            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.Endpoint, -1, 0, 2);
            Assert.That(RoadPlacementModel.TrySuggest(pieces, -1, -1, package,
                out var topology, out var turns), Is.False,
                "A pending end cap must not be offered as a placeable invisible tile.");

            RoadPlacementModel.PlaceOrReplace(pieces, RoadPieceTopology.Endpoint, 0, -1, 3);
            Assert.That(RoadPlacementModel.TrySuggest(pieces, -1, -1, package,
                out topology, out turns), Is.True);
            Assert.That(topology, Is.EqualTo(RoadPieceTopology.Corner));
            Assert.That(turns, Is.EqualTo(2));

            pieces.Clear();
            pieces.Add(new PlacedRoadPiece { Id = "west", Topology = RoadPieceTopology.Endpoint, GridX = -1, GridZ = 0, RotationQuarterTurns = 1 });
            pieces.Add(new PlacedRoadPiece { Id = "east", Topology = RoadPieceTopology.Endpoint, GridX = 1, GridZ = 0, RotationQuarterTurns = 3 });
            Assert.That(RoadPlacementModel.TrySuggest(pieces, 0, 0, package,
                out topology, out turns), Is.True);
            Assert.That(topology, Is.EqualTo(RoadPieceTopology.Straight));
            Assert.That(turns, Is.EqualTo(1));
        }

        [Test]
        public void FourCornerRoadLoopIsValidAndTraversable()
        {
            var root = new GameObject("Road Loop Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                Assert.That(world.RoadValidationIssues, Is.Empty);
                Assert.That(world.PlacedRoadCount, Is.EqualTo(4));
                Assert.That(world.VehicleNodeCount, Is.EqualTo(4));
                Assert.That(world.VehicleSegmentCount, Is.EqualTo(4));
                Assert.That(world.Session.Data.VehicleNetwork.Nodes,
                    Has.None.Matches<CirculationNode>(node => node.Kind == CirculationNodeKind.LotBoundaryPort));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LotEditorNavigationExposesTextureToolCategoriesWithCompactControls()
        {
            CollectionAssert.AreEqual(
                new[] { "Main", "Buildings", "Roads", "Paths", "Flora", "Props", "BaseTextures", "OverlayTextures", "Environment", "View" },
                System.Enum.GetNames(typeof(LotEditorCategory)));

            // Main and Environment intentionally use the owned gear and sun glyphs.
            foreach (var icon in new[]
                     {
                         "buildings", "roads-car-v74", "paths",
                         "flora-tree-v91", "props-lamppost-v91", "base-textures",
                         "overlay-textures", "view"
                     })
            {
                Assert.That(
                    Resources.Load<Texture2D>($"CityForgeV3/UI/LotEditorTools/{icon}"),
                    Is.Not.Null,
                    $"Missing compact Lot Editor category icon: {icon}");
            }
        }

        [Test]
        public void LegacyGrassBasesAndUrbanOverlaysApplyAndPersist()
        {
            Assert.That(LotWorldController.GrassBaseTextures.Count, Is.EqualTo(6));
            foreach (var option in LotWorldController.GrassBaseTextures)
                Assert.That(Resources.Load<Texture2D>(option.ResourcePath), Is.Not.Null,
                    $"Missing legacy grass texture {option.Id}");
            Assert.That(Resources.Load<Texture2D>(
                LotWorldController.BrickWalkwayOverlay.ResourcePath), Is.Not.Null);
            Assert.That(LotWorldController.OverlayTextures.Count, Is.EqualTo(3));
            var sidewalk = LotWorldController.ResolveOverlayTexture("concrete-sidewalk");
            Assert.That(sidewalk.DisplayName, Is.EqualTo("Concrete Sidewalk"));
            Assert.That(Resources.Load<Texture2D>(sidewalk.ResourcePath), Is.Not.Null);
            var fancySidewalk = LotWorldController.ResolveOverlayTexture("fancy-sidewalk");
            Assert.That(fancySidewalk.DisplayName, Is.EqualTo("Fancy Sidewalk"));
            Assert.That(Resources.Load<Texture2D>(fancySidewalk.ResourcePath), Is.Not.Null);

            var root = new GameObject("Lot Texture Persistence Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Grass and Path", LotType.Residential, 3, 3);
                world.SetBaseTexture("grass-lush");
                Assert.That(world.BaseTextureId, Is.EqualTo("grass-lush"));
                Assert.That(world.PlaceOverlayTextureFromPanel("brick-walkway",
                    new Vector2(500f, 500f), new Vector2(1000f, 1000f)), Is.True);
                Assert.That(world.OverlayTextureCount, Is.EqualTo(1));
                var json = world.Session.Serialize();
                var restored = new LotEditorSession();
                restored.Restore(json);
                Assert.That(restored.Data.BaseTextureId, Is.EqualTo("grass-lush"));
                Assert.That(restored.Data.OverlayTextures.Count, Is.EqualTo(1));
                Assert.That(restored.Data.OverlayTextures[0].TextureId,
                    Is.EqualTo("brick-walkway"));
                Assert.That(Find(root.transform, "Overlay — brick-walkway"), Is.Not.Null);
                Assert.That(world.PlaceOverlayTextureFromPanel("concrete-sidewalk",
                    new Vector2(170f, 170f), new Vector2(1000f, 1000f)), Is.True);
                Assert.That(world.Session.Data.OverlayTextures[1].TextureId,
                    Is.EqualTo("concrete-sidewalk"));
                Assert.That(Find(root.transform, "Overlay — concrete-sidewalk"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OverlayModeSelectsDragPaintsWithoutDuplicatesAndDeletesSelectedTile()
        {
            var root = new GameObject("Overlay Paint Editing Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Overlay Paint", LotType.Residential, 3, 3);

                Assert.That(world.BeginOverlayPaintAtCell("brick-walkway", 1, 1), Is.False,
                    "Overlay editing must remain inactive outside the Overlay menu.");
                world.SetOverlayEditorContext(true);
                Assert.That(world.BeginOverlayPaintAtCell("brick-walkway", 1, 1), Is.True);
                Assert.That(world.OverlayTextureCount, Is.EqualTo(1));
                Assert.That(world.SelectedOverlayTextureIndex, Is.EqualTo(0));
                Assert.That(world.PaintOverlayStrokeCell(2, 1), Is.True);
                Assert.That(world.PaintOverlayStrokeCell(2, 1), Is.False);
                Assert.That(world.OverlayTextureCount, Is.EqualTo(2));
                Assert.That(world.SelectedOverlayTextureIndex, Is.EqualTo(1));
                world.EndOverlayPaint();

                Assert.That(world.BeginOverlayPaintAtCell("", 1, 1), Is.True,
                    "Clicking an existing tile should select it without an armed texture.");
                world.EndOverlayPaint();
                Assert.That(world.SelectedOverlayTextureIndex, Is.EqualTo(0));
                Assert.That(world.DeleteSelectedOverlayTexture(), Is.True);
                Assert.That(world.OverlayTextureCount, Is.EqualTo(1));
                Assert.That(world.Session.Data.OverlayTextures[0].CellX, Is.EqualTo(2));

                world.SetOverlayEditorContext(false);
                Assert.That(world.DeleteSelectedOverlayTexture(), Is.False,
                    "Delete must not edit overlays outside the Overlay menu.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimePrimitiveShadowsUseTheCalibratedV39ProjectionContract()
        {
            Assert.That(LotWorldController.ShadowLengthScale(TimeOfDayPreset.Morning), Is.EqualTo(0.35f));
            Assert.That(LotWorldController.ShadowLengthScale(TimeOfDayPreset.Noon), Is.EqualTo(0.45f));
            Assert.That(LotWorldController.ShadowLengthScale(TimeOfDayPreset.Afternoon), Is.EqualTo(0.50f));
            Assert.That(LotWorldController.ShadowLengthScale(TimeOfDayPreset.Evening), Is.EqualTo(0.32f));
            Assert.That(LotWorldController.BuildingShadowLengthScale(TimeOfDayPreset.Morning), Is.EqualTo(0.90f));
            Assert.That(LotWorldController.BuildingShadowLengthScale(TimeOfDayPreset.Noon), Is.EqualTo(0.45f));
            Assert.That(LotWorldController.BuildingShadowLengthScale(TimeOfDayPreset.Afternoon), Is.EqualTo(1.15f));
            Assert.That(LotWorldController.BuildingShadowLengthScale(TimeOfDayPreset.Evening), Is.EqualTo(0.65f));
            foreach (var preset in new[]
                     {
                         TimeOfDayPreset.Morning,
                         TimeOfDayPreset.Noon,
                         TimeOfDayPreset.Afternoon,
                         TimeOfDayPreset.Evening
                     })
                Assert.That(
                    LotWorldController.BuildingShadowOpacityMultiplier(preset),
                    Is.EqualTo(LotWorldController.PropShadowOpacityMultiplier(preset) * 1.45f),
                    $"Building shadows must compensate for the lighter primitive silhouette at {preset}.");
        }

        [Test]
        public void AfternoonPropShadowsHaveASeparateLongerAndStrongerProfile()
        {
            Assert.That(LotWorldController.PropShadowLengthScale(TimeOfDayPreset.Afternoon),
                Is.EqualTo(1f));
            Assert.That(LotWorldController.PropShadowLengthScale(TimeOfDayPreset.Morning),
                Is.EqualTo(LotWorldController.ShadowLengthScale(TimeOfDayPreset.Morning)));
            Assert.That(LotWorldController.PropShadowLengthScale(TimeOfDayPreset.Noon),
                Is.EqualTo(LotWorldController.ShadowLengthScale(TimeOfDayPreset.Noon)));
            Assert.That(LotWorldController.PropShadowOpacityMultiplier(TimeOfDayPreset.Afternoon),
                Is.EqualTo(1.25f));
            Assert.That(LotWorldController.PropShadowOpacityMultiplier(TimeOfDayPreset.Noon),
                Is.EqualTo(0.78f));
        }

        [Test]
        public void AfternoonSunUsesTheV24SoftenedIntensityContract()
        {
            Assert.That(
                TimeOfDayLighting.For(TimeOfDayPreset.Afternoon).SunIntensity,
                Is.EqualTo(0.72f));
        }

        [Test]
        public void GovernmentHouseUsesArchitecturalV38NightOverlays()
        {
            var package = HybridBuildingPackageRegistry.GovernmentHouse;
            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                StringAssert.Contains("ColonialGovernmentHouseNightV38", facing.NightOverlayResourcePath);
                Assert.That(Resources.Load<Texture2D>(facing.NightOverlayResourcePath), Is.Not.Null);
            }
        }

        [Test]
        public void RoadMaterialCatalogGroupsUsableSurfacesByEraAndRole()
        {
            Assert.That(RoadMaterialCatalog.All.Count, Is.EqualTo(7));
            Assert.That(RoadMaterialCatalog.ForEra(RoadMaterialEra.Founders, false).Count,
                Is.EqualTo(4));
            Assert.That(RoadMaterialCatalog.ForEra(RoadMaterialEra.Founders, true).Count,
                Is.EqualTo(2));
            Assert.That(RoadMaterialCatalog.ForEra(RoadMaterialEra.Modern, true), Is.Empty);
            foreach (var definition in RoadMaterialCatalog.All)
                Assert.That(definition.LoadTexture(), Is.Not.Null, definition.ResourcePath);
        }

        [Test]
        public void RoadMaterialSelectionsPersistPerTileAndAreAdoptedOnSelection()
        {
            var root = new GameObject("Road Material Runtime Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Material Roads", LotType.Neighborhood, 20);
                world.SelectRoadPackage(RoadPiecePackageCatalog.TwoLaneSidewalkId);
                world.ApplyRoadMaterials("cobblestone", "brick");
                Assert.That(world.PlaceRoadPiece(), Is.True);
                var piece = world.Session.Data.RoadPieces[0];
                Assert.That(piece.RoadMaterialId, Is.EqualTo("cobblestone"));
                Assert.That(piece.SidewalkMaterialId, Is.EqualTo("brick"));

                world.ApplyRoadMaterials("dirt", "cut-stone");
                Assert.That(piece.RoadMaterialId, Is.EqualTo("dirt"));
                Assert.That(piece.SidewalkMaterialId, Is.EqualTo("cut-stone"));
                world.SelectRoadCellAtWorld(-5f, -5f);
                Assert.That(world.SelectedRoadMaterialId, Is.EqualTo("dirt"));
                Assert.That(world.SelectedSidewalkMaterialId, Is.EqualTo("cut-stone"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SemanticRoadRendererReceivesIndependentRoadAndSidewalkTextures()
        {
            var root = new GameObject("Semantic Road Material Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Semantic Roads", LotType.Neighborhood, 20);
                world.SelectRoadPackage(RoadPiecePackageCatalog.TwoLaneSidewalkId);
                world.ApplyRoadMaterials("brick", "early-concrete");
                Assert.That(world.PlaceRoadPiece(), Is.True);
                var road = Find(root.transform, "Two-Lane Street with Sidewalks Straight");
                Assert.That(road, Is.Not.Null);
                var material = road.GetComponentInChildren<Renderer>().sharedMaterial;
                Assert.That(material.GetFloat("_UseMaterialZones"), Is.EqualTo(1f));
                Assert.That(material.GetTexture("_RoadSurfaceTex").name,
                    Is.EqualTo("brick-realistic"));
                Assert.That(material.GetTexture("_SidewalkSurfaceTex").name,
                    Is.EqualTo("early-concrete"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyToAllRoadsUpdatesExistingTilesAndFuturePlacementBrush()
        {
            var root = new GameObject("Apply All Road Materials Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Apply All", LotType.Neighborhood, 30);
                world.SelectRoadPackage(RoadPiecePackageCatalog.TwoLaneSidewalkId);
                Assert.That(world.PlaceRoadPiece(), Is.True);
                world.NudgeRoadCursor(1, 0);
                Assert.That(world.PlaceRoadPiece(), Is.True);

                Assert.That(world.ApplyRoadMaterials("brick", "cut-stone",
                    RoadMarkingStyle.DoubleLines, true), Is.True);
                foreach (var road in world.Session.Data.RoadPieces)
                {
                    Assert.That(road.RoadMaterialId, Is.EqualTo("brick"));
                    Assert.That(road.SidewalkMaterialId, Is.EqualTo("cut-stone"));
                    Assert.That(road.MarkingStyle, Is.EqualTo(RoadMarkingStyle.DoubleLines));
                }

                world.NudgeRoadCursor(1, 0);
                Assert.That(world.PlaceRoadPiece(), Is.True);
                var newest = RoadPlacementModel.FindAt(world.Session.Data.RoadPieces,
                    world.RoadCursorCell.x, world.RoadCursorCell.y);
                Assert.That(newest.RoadMaterialId, Is.EqualTo("brick"));
                Assert.That(newest.SidewalkMaterialId, Is.EqualTo("cut-stone"));
                Assert.That(newest.MarkingStyle, Is.EqualTo(RoadMarkingStyle.DoubleLines));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EveryRoadFamilyProvidesAllMarkingArtworkVariants()
        {
            foreach (var package in RoadPiecePackageCatalog.Packages)
            {
                if (package.Id == RoadPiecePackage.LegacyPackageId) continue;
                foreach (var piece in package.Pieces)
                {
                    if (!piece.HasArtwork) continue;
                foreach (var suffix in new[] { "-no-lines", "-double-lines", "-single-dotted" })
                    Assert.That(Resources.Load<Texture2D>(piece.ResourcePath + suffix),
                        Is.Not.Null, piece.ResourcePath + suffix);
                }
            }
        }

        [Test]
        public void ExistingRoadVisiblySwitchesToCobblestoneAndNoLinesArtwork()
        {
            var root = new GameObject("Live Cobblestone And No Lines Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Live Palette", LotType.Neighborhood, 20);
                world.SelectRoadPackage(RoadPiecePackageCatalog.TwoLaneSidewalkId);
                Assert.That(world.PlaceRoadPiece(), Is.True);
                Assert.That(world.ApplyRoadMaterials("cobblestone", "brick",
                    RoadMarkingStyle.NoLines), Is.True);

                var road = Find(root.transform, "Two-Lane Street with Sidewalks Straight");
                var renderer = road.GetComponentInChildren<Renderer>();
                Assert.That(renderer.sharedMaterial.mainTexture.name,
                    Is.EqualTo("straight-no-lines"));
                Assert.That(renderer.sharedMaterial.GetTexture("_RoadSurfaceTex").name,
                    Is.EqualTo("cobblestone-gray"));
                Assert.That(renderer.sharedMaterial.GetFloat("_RoadMaterialTiling"),
                    Is.EqualTo(3.333f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LoadingFiveByFiveLotRebuildsWorldAtSavedDimensions()
        {
            var folder = Path.Combine(Application.temporaryCachePath,
                "cityforge-five-by-five-" + System.Guid.NewGuid().ToString("N"));
            var root = new GameObject("Five By Five Load Test");
            try
            {
                var source = new LotEditorSession();
                source.NewLot("Saved Five By Five", LotType.Neighborhood, 50);
                LotSaveStore.Save(source, System.Array.Empty<string>(), folder);

                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Initial Two By Two", LotType.Neighborhood, 20);
                Assert.That(world.LoadLot(source.Data.LotId, folder), Is.True);
                Assert.That(world.LotWidthCells, Is.EqualTo(5));
                Assert.That(world.LotDepthCells, Is.EqualTo(5));
                var ground = Find(root.transform, "Lot Surface");
                Assert.That(ground.localScale.x, Is.EqualTo(5.4f).Within(0.001f));
                Assert.That(ground.localScale.z, Is.EqualTo(5.4f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void ApplyAllCanReplaceCobblestoneWithOtherMaterialsRepeatedly()
        {
            var root = new GameObject("Repeated Apply All Materials Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Repeated Apply All", LotType.Neighborhood, 20);
                world.SelectRoadPackage(RoadPiecePackageCatalog.TwoLaneSidewalkId);
                Assert.That(world.PlaceRoadPiece(), Is.True);

                Assert.That(world.ApplyRoadMaterials("cobblestone", "brick",
                    RoadMarkingStyle.NoLines, true), Is.True);
                Assert.That(world.ApplyRoadMaterials("brick", "antique-brick",
                    RoadMarkingStyle.DoubleLines, true), Is.True);
                var road = Find(root.transform, "Two-Lane Street with Sidewalks Straight");
                var material = road.GetComponentInChildren<Renderer>().sharedMaterial;
                Assert.That(material.GetTexture("_RoadSurfaceTex").name,
                    Is.EqualTo("brick-realistic"));
                Assert.That(material.mainTexture.name, Is.EqualTo("straight-double-lines"));

                Assert.That(world.ApplyRoadMaterials("blacktop", "brick",
                    RoadMarkingStyle.SingleDotted, true), Is.True);
                road = Find(root.transform, "Two-Lane Street with Sidewalks Straight");
                material = road.GetComponentInChildren<Renderer>().sharedMaterial;
                Assert.That(material.GetTexture("_RoadSurfaceTex").name,
                    Is.EqualTo("blacktop-realistic"));
                Assert.That(world.Session.Data.RoadPieces[0].RoadMaterialId,
                    Is.EqualTo("blacktop"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Transform Find(Transform root, string objectName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }

    }
}
