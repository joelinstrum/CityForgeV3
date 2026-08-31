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
        [TestCase(0.00f, BusinessAsUsualAction.Walk)]
        [TestCase(0.74f, BusinessAsUsualAction.Walk)]
        [TestCase(0.75f, BusinessAsUsualAction.Wait)]
        [TestCase(0.79f, BusinessAsUsualAction.Wait)]
        [TestCase(0.80f, BusinessAsUsualAction.FoldArms)]
        [TestCase(0.84f, BusinessAsUsualAction.FoldArms)]
        [TestCase(0.85f, BusinessAsUsualAction.Idle)]
        [TestCase(0.94f, BusinessAsUsualAction.Idle)]
        [TestCase(0.95f, BusinessAsUsualAction.LookAround)]
        [TestCase(1.00f, BusinessAsUsualAction.LookAround)]
        public void BusinessAsUsualScriptHonorsAuthoredWeights(float roll,
            BusinessAsUsualAction expected)
        {
            Assert.That(BusinessAsUsualCharacterScript.Select(roll),
                Is.EqualTo(expected));
        }

        [TestCase(0.00f, 0f, 1f)]
        [TestCase(0.24f, 0f, 1f)]
        [TestCase(0.25f, 1f, 0f)]
        [TestCase(0.50f, 0f, -1f)]
        [TestCase(0.75f, -1f, 0f)]
        [TestCase(1.00f, -1f, 0f)]
        public void BusinessAsUsualWalkingUsesExactLotCardinalHeadings(
            float roll, float expectedX, float expectedZ)
        {
            var direction = BusinessAsUsualCharacterScript.WalkingDirection(roll);
            Assert.That(direction.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(direction.y, Is.EqualTo(expectedZ).Within(0.0001f));
        }

        [Test]
        public void BusinessAsUsualWalkUsesBoundaryDrivenFallbackDuration()
        {
            Assert.That(BusinessAsUsualCharacterScript.Duration(
                BusinessAsUsualAction.Walk, 0f), Is.EqualTo(60f));
            Assert.That(BusinessAsUsualCharacterScript.Duration(
                BusinessAsUsualAction.Walk, 1f), Is.EqualTo(60f));
        }

        [Test]
        public void VictorianGentleman10KExposesCompleteAnimationLibrary()
        {
            const string path =
                "CityForgeV3/Props/Characters/VictorianGentlemanV01/VictorianGentlemanAnimatedV01";
            Assert.That(Resources.Load<GameObject>(path), Is.Not.Null);
            var names = Resources.LoadAll<AnimationClip>(path)
                .Select(clip => clip.name.ToLowerInvariant()).ToArray();
            foreach (var expected in new[]
                     {
                         "agree", "afraid", "bow", "clap", "fall", "flee",
                         "fold_arms", "hit_to_body", "idle", "laugh",
                         "look_around", "sit", "wait", "walk"
                     })
                Assert.That(names.Any(name => name.Contains(expected)), Is.True,
                    expected);
        }

        [Test]
        public void HooliganExposesIdleWalkAndRunAnimationLibrary()
        {
            const string path =
                "CityForgeV3/Props/Characters/HooliganV01/HooliganAnimatedV01";
            Assert.That(Resources.Load<GameObject>(path), Is.Not.Null);
            var names = Resources.LoadAll<AnimationClip>(path)
                .Select(clip => clip.name.ToLowerInvariant()).ToArray();
            foreach (var expected in new[] { "idle", "walk", "run" })
                Assert.That(names.Any(name => name.Contains(expected)), Is.True,
                    expected);
        }

        [Test]
        public void KingKongExposesIdleWalkAndTurnAnimationLibrary()
        {
            const string path =
                "CityForgeV3/Props/Characters/KingKongV01/KingKongV01";
            var model = Resources.Load<GameObject>(path);
            Assert.That(model, Is.Not.Null);
            Assert.That(LotWorldController.IsKingKong(
                LotWorldController.KingKongCharacterId), Is.True);
            Assert.That(LotWorldController.IsThreeDimensionalCharacter(
                LotWorldController.KingKongCharacterId), Is.True);
            var names = Resources.LoadAll<AnimationClip>(path)
                .Select(clip => clip.name.ToLowerInvariant()).ToArray();
            foreach (var expected in new[] { "idle", "walk", "turn" })
                Assert.That(names.Any(name => name.Contains(expected)), Is.True,
                    expected);
        }

        [Test]
        public void ThreeDimensionalEntertainmentLibraryIncludesKingKongEnclosure()
        {
            Assert.That(Resources.Load<GameObject>(
                LotWorldController.KingKongEnclosureBuilding3DResource),
                Is.Not.Null);
            Assert.That(LotWorldController.KingKongEnclosureBuilding3DId,
                Is.EqualTo("king-kong-enclosure-building-v01"));
            Assert.That(LotWorldController.KingKongEnclosureBuildingSizeMeters,
                Is.EqualTo(30f));
            Assert.That(LotWorldController.KingKongEnclosureVisibleBoundsScale,
                Is.EqualTo(0.58f));
            Assert.That(System.Enum.IsDefined(typeof(BuildingUseCategory),
                BuildingUseCategory.Entertainment), Is.True);
        }

        [TestCase(0.00f, BusinessAsUsualAction.Walk)]
        [TestCase(0.14f, BusinessAsUsualAction.Walk)]
        [TestCase(0.15f, BusinessAsUsualAction.Idle)]
        [TestCase(1.00f, BusinessAsUsualAction.Idle)]
        public void HooliganBusinessAsUsualIsMostlyIdle(float roll,
            BusinessAsUsualAction expected)
        {
            Assert.That(BusinessAsUsualCharacterScript.SelectForCharacter(
                LotWorldController.HooliganCharacterId, roll), Is.EqualTo(expected));
        }

        [Test]
        public void HistoricPolicemanExposesInteractionReadyAnimationLibrary()
        {
            const string path =
                "CityForgeV3/Props/Characters/HistoricPolicemanV01/HistoricPolicemanAnimatedV01";
            Assert.That(Resources.Load<GameObject>(path), Is.Not.Null);
            var names = Resources.LoadAll<AnimationClip>(path)
                .Select(clip => clip.name.ToLowerInvariant()).ToArray();
            foreach (var expected in new[]
                     {
                         "idle", "walk", "run", "wait", "look_around",
                         "angry", "hit_to_body", "fall"
                     })
                Assert.That(names.Any(name => name.Contains(expected)), Is.True,
                    expected);
        }

        [TestCase(0.00f, BusinessAsUsualAction.Walk)]
        [TestCase(0.20f, BusinessAsUsualAction.Wait)]
        [TestCase(0.25f, BusinessAsUsualAction.LookAround)]
        [TestCase(0.35f, BusinessAsUsualAction.Idle)]
        [TestCase(1.00f, BusinessAsUsualAction.Idle)]
        public void HistoricPolicemanBusinessAsUsualIsMostlyIdle(float roll,
            BusinessAsUsualAction expected)
        {
            Assert.That(BusinessAsUsualCharacterScript.SelectForCharacter(
                LotWorldController.HistoricPolicemanCharacterId, roll),
                Is.EqualTo(expected));
        }

        [Test]
        public void CharacterScriptsExposeStableSaveIdentifiers()
        {
            Assert.That(CharacterBehaviorScript.Normalize("unknown"), Is.EqualTo(
                CharacterBehaviorScript.BusinessAsUsual));
            Assert.That(CharacterBehaviorScript.IsAvailableFor(
                LotWorldController.HooliganCharacterId,
                CharacterBehaviorScript.HarassPedestrian), Is.True);
            Assert.That(CharacterBehaviorScript.IsAvailableFor(
                LotWorldController.HooliganCharacterId,
                CharacterBehaviorScript.EvadePolice), Is.True);
            Assert.That(CharacterBehaviorScript.IsAvailableFor(
                LotWorldController.HistoricPolicemanCharacterId,
                CharacterBehaviorScript.HarassPedestrian), Is.False);
            Assert.That(CharacterBehaviorScript.IsAvailableFor(
                LotWorldController.HooliganCharacterId,
                CharacterBehaviorScript.FightHooligan), Is.False);
        }

        [Test]
        public void CharacterGroundShadowUsesCanonicalSunDirection()
        {
            var expectedRay = TimeOfDayLighting.SunRotation(
                TimeOfDayPreset.Afternoon) * Vector3.forward;
            var expected = new Vector2(expectedRay.x, expectedRay.z).normalized;
            Assert.That(Vector2.Dot(CharacterGroundShadow.Direction(
                TimeOfDayPreset.Afternoon), expected),
                Is.GreaterThan(0.9999f));
        }

        [Test]
        public void CharacterGroundShadowAcceptsTheActiveWorldSunDirection()
        {
            var southwestRay = Quaternion.Euler(34f, 315f, 0f) *
                Vector3.forward;
            var expected = new Vector2(southwestRay.x, southwestRay.z).normalized;
            Assert.That(Vector2.Dot(
                CharacterGroundShadow.Direction(southwestRay), expected),
                Is.GreaterThan(0.999f));
        }

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
        [TestCase(1, 186f)]
        [TestCase(2, 186f)]
        [TestCase(3, 186f)]
        public void BuildingPropFacing_UsesSavedPropPreset(
            int hostQuarterTurns, float expectedYaw)
        {
            var item = BuildingPropCatalog.Find(BuildingPropCatalog.AleHouseSignId);
            var yaw = BuildingPropCatalog.ResolveYawDegrees(
                item, hostQuarterTurns, 0f);

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

        [TestCase(0, 0, 0)]
        [TestCase(1, 0, 0)]
        [TestCase(2, 0, 0)]
        [TestCase(3, 0, 0)]
        [TestCase(4, 0, 0)]
        [TestCase(0, 45, 1)]
        [TestCase(1, 45, 1)]
        [TestCase(2, 45, 1)]
        [TestCase(3, 45, 1)]
        public void ResolvedPropPreset_DoesNotReapplyHostRotation(
            int buildingQuarterTurns, float propRotationDegrees,
            int expectedPreset)
        {
            Assert.That(BuildingPropCatalog.ResolveFacingPreset(
                buildingQuarterTurns, propRotationDegrees),
                Is.EqualTo(expectedPreset));
        }

        [Test]
        public void BuildingTurn_UsesTheSameTwoPresetsAsTwoRPresses()
        {
            var building = new PlacedBuilding();
            building.Attachments.Add(new PlacedBuildingProp
                { RotationDegrees = 0f });

            BuildingPropCatalog.RotateWithBuilding(building, 1);
            Assert.That(building.Attachments[0].RotationDegrees, Is.EqualTo(90f));

            BuildingPropCatalog.RotateWithBuilding(building, -1);
            Assert.That(building.Attachments[0].RotationDegrees, Is.EqualTo(0f));
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
        public void LotEditorStartsWithoutAnExpandedToolAndUsesTheCarRoadIcon()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            StringAssert.Contains(
                "_lotEditorCategory = LotEditorCategory.Main", source);
            StringAssert.Contains(
                "private bool _lotEditorCategoryExpanded;", source);
            StringAssert.Contains("private bool _hasOpenLot;", source);
            StringAssert.Contains("_lotWorld.SetVisible(false);", source);
            StringAssert.DoesNotContain("_lotWorld.NewEmptyLot();", source);
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
        public void DeleteAndBackspaceRouteThroughActiveObjectSelection()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            StringAssert.Contains("DeleteActiveSelection()", source);
            StringAssert.Contains("case LotObjectSelectionKind.BuildingProp:", source);
            StringAssert.Contains("case LotObjectSelectionKind.Prop:", source);
            StringAssert.Contains("case LotObjectSelectionKind.Flora:", source);
            StringAssert.Contains("case LotObjectSelectionKind.Building:", source);
            StringAssert.Contains("DeleteSelectedBuildingProp()", source);
            StringAssert.Contains("DeleteSelectedFlora()", source);
            StringAssert.Contains("DeleteBuilding();", source);
            StringAssert.Contains("_lotWorld.DeleteSelectedBuilding()", source);
        }

        [Test]
        public void DeleteSelectedBuildingPropRemovesOnlyTheActiveAttachment()
        {
            var root = new GameObject("Selected Building Prop Delete Test");
            try
            {
                LogAssert.ignoreFailingMessages = true;
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Delete Building Props", LotType.Residential, 4, 4);
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                var building = world.Session.Data.Buildings[0];
                building.Attachments.Add(new PlacedBuildingProp
                {
                    InstanceId = "keep",
                    ComponentId = BuildingPropCatalog.AleHouseSignId
                });
                building.Attachments.Add(new PlacedBuildingProp
                {
                    InstanceId = "delete",
                    ComponentId = BuildingPropCatalog.AleHouseSignId
                });
                typeof(LotWorldController).GetField(
                    "_selectedBuildingPropBuildingIndex",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(world, 0);
                typeof(LotWorldController).GetField(
                    "_selectedBuildingPropAttachmentIndex",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(world, 1);

                Assert.That(world.DeleteSelectedBuildingProp(), Is.True);
                Assert.That(building.Attachments.Count, Is.EqualTo(1));
                Assert.That(building.Attachments[0].InstanceId, Is.EqualTo("keep"));
                Assert.That(world.ActiveObjectSelection,
                    Is.EqualTo(LotObjectSelectionKind.None));
                Assert.That(world.DeleteSelectedBuildingProp(), Is.False);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Object.DestroyImmediate(root);
            }
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
                LogAssert.ignoreFailingMessages = true;
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
                var applyCountAfterFocusEntry = world.SessionStateApplyCountForQa;
                var liveMoveCountBeforeDrag = world.BuildingFocusLiveMoveCountForQa;
                var cameraBeforeDrag = world.CaptureCameraFraming();
                Assert.That(world.BuildingFocusFreezeActive, Is.True);
                Assert.That(world.DragBuildingFromPanel(
                    PanelPoint(new Vector3(3f, 0f, 2f)), panelSize), Is.True);
                Assert.That(world.DragBuildingFromPanel(
                    PanelPoint(new Vector3(4f, 0f, 3f)), panelSize), Is.True);
                Assert.That(world.EndBuildingDrag(), Is.True);
                Assert.That(world.BuildingCell, Is.Not.EqualTo(Vector2Int.zero));
                Assert.That(world.BuildingPresentationPosition(
                        world.SelectedBuildingIndex),
                    Is.EqualTo(new Vector3(
                        world.Session.Data.CellX, 0f, world.Session.Data.CellZ)));
                Assert.That(world.IsSelected, Is.True);
                Assert.That(world.BuildingFocusFreezeActive, Is.True,
                    "Pointer-up keeps the focused context frozen.");
                Assert.That(world.SessionStateApplyCountForQa,
                    Is.EqualTo(applyCountAfterFocusEntry),
                    "Pointer moves and pointer-up may not rebuild the lot.");
                Assert.That(world.BuildingFocusLiveMoveCountForQa,
                    Is.EqualTo(liveMoveCountBeforeDrag + 2));
                var cameraAfterDrag = world.CaptureCameraFraming();
                Assert.That(cameraAfterDrag.Position,
                    Is.EqualTo(cameraBeforeDrag.Position));
                Assert.That(Quaternion.Angle(
                    cameraAfterDrag.Rotation, cameraBeforeDrag.Rotation),
                    Is.LessThan(0.001f));
                Assert.That(cameraAfterDrag.OrthographicSize,
                    Is.EqualTo(cameraBeforeDrag.OrthographicSize).Within(0.000001f));

                world.DeselectAll();
                Assert.That(world.BuildingFocusFreezeActive, Is.False);
                Assert.That(world.SessionStateApplyCountForQa,
                    Is.EqualTo(applyCountAfterFocusEntry + 1),
                    "Focus exit performs exactly one full reconciliation.");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
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
        public void BuildingFocusFreezeUsesAVisibleSpotlightAndDefersReconciliation()
        {
            var app = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            var world = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "CityForgeV3/Runtime/World/LotWorldController.cs"));

            StringAssert.Contains("building-focus-freeze-overlay", app);
            StringAssert.Contains("Building position applied • context restored", app);
            StringAssert.Contains("_lotWorld.DeselectAll();", app);
            StringAssert.Contains("public bool BuildingFocusFreezeActive", world);

            var dragStart = world.IndexOf("public bool DragBuildingFromPanel(",
                System.StringComparison.Ordinal);
            var dragEnd = world.IndexOf("private bool MoveBuildingTo(", dragStart,
                System.StringComparison.Ordinal);
            var drag = world.Substring(dragStart, dragEnd - dragStart);
            Assert.That(drag, Does.Contain("ApplySelectedBuildingPositionOnly();"));
            Assert.That(drag, Does.Not.Contain("ApplySessionState();"));
            Assert.That(drag, Does.Not.Contain("NotifyStateChanged();"));

            var releaseStart = world.IndexOf("public bool EndBuildingDrag()",
                System.StringComparison.Ordinal);
            var releaseEnd = world.IndexOf("private bool TryLotPointFromPanel(",
                releaseStart, System.StringComparison.Ordinal);
            var release = world.Substring(releaseStart, releaseEnd - releaseStart);
            Assert.That(release, Does.Not.Contain("ApplySessionState();"));
            Assert.That(release, Does.Not.Contain("NotifyStateChanged();"));
        }

        [Test]
        public void BuildingFocusSpotlightUsesCachedTightFacingBoundsOnly()
        {
            var presentation = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "CityForgeV3/Runtime/World/HybridBuildingPresentation.cs"));
            var buildingProps = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "CityForgeV3/Runtime/World/LotWorldController.BuildingProps.cs"));

            StringAssert.Contains("CacheRegistrationLocalBounds(",
                presentation);
            StringAssert.Contains("var vertices = registrationSprite.vertices",
                presentation);
            StringAssert.Contains("TryGetVisibleArtworkScreenBounds(",
                presentation);
            StringAssert.Contains("_neutralRegistrationLocalBounds",
                presentation);
            StringAssert.Contains("_fullNightRegistrationLocalBounds",
                presentation);
            StringAssert.Contains(
                "_renderer.sprite == _fullNightSprites[_facing]",
                presentation);

            var focusStart = buildingProps.IndexOf(
                "public bool TryGetSelectedBuildingPanelBounds(",
                System.StringComparison.Ordinal);
            var focusEnd = buildingProps.IndexOf(
                "private static void ApplyBuildingPropMaterials(",
                focusStart, System.StringComparison.Ordinal);
            var focusBounds = buildingProps.Substring(
                focusStart, focusEnd - focusStart);
            Assert.That(focusBounds,
                Does.Contain("presentation.TryGetVisibleArtworkScreenBounds("));
            Assert.That(focusBounds,
                Does.Contain("TryBuildingArtworkScreenBounds(").And
                    .Contain("hasTightBounds"),
                "Full-rectangle bounds remain only as a safe fallback.");

            var sharedStart = buildingProps.IndexOf(
                "private bool TryBuildingArtworkScreenBounds(",
                System.StringComparison.Ordinal);
            var sharedEnd = buildingProps.IndexOf(
                "public bool TryGetSelectedBuildingPanelBounds(",
                sharedStart, System.StringComparison.Ordinal);
            var sharedBounds = buildingProps.Substring(
                sharedStart, sharedEnd - sharedStart);
            Assert.That(sharedBounds, Does.Contain("var bounds = renderer.bounds;"),
                "Attachment placement and broad hover bounds stay unchanged.");
        }

        [Test]
        public void EveryActiveBuildingFacingProvidesAlphaTightSpotlightGeometry()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var packageCount = 0;
            var facingCount = 0;
            foreach (var package in HybridBuildingPackageRegistry.All)
            {
                packageCount++;
                for (var facingIndex = 0;
                     facingIndex < package.FacingCount;
                     facingIndex++)
                {
                    var facing = package.Facing(facingIndex);
                    var texture = Resources.Load<Texture2D>(
                        facing.ApprovedResourcePath);
                    Assert.That(texture, Is.Not.Null,
                        $"{package.Id} / {facing.Id}");
                    var tight = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        facing.UnityPivot,
                        package.PixelsPerMeter,
                        0,
                        SpriteMeshType.Tight);
                    try
                    {
                        var vertices = tight.vertices;
                        Assert.That(vertices, Is.Not.Empty,
                            $"{package.Id} / {facing.Id}");
                        var minimum = vertices[0];
                        var maximum = vertices[0];
                        for (var vertexIndex = 1;
                             vertexIndex < vertices.Length;
                             vertexIndex++)
                        {
                            minimum = Vector2.Min(minimum, vertices[vertexIndex]);
                            maximum = Vector2.Max(maximum, vertices[vertexIndex]);
                        }

                        var tightSize = maximum - minimum;
                        var fullSize = new Vector2(
                            texture.width / package.PixelsPerMeter,
                            texture.height / package.PixelsPerMeter);
                        Assert.That(tightSize.x,
                            Is.LessThanOrEqualTo(fullSize.x + 0.001f),
                            $"{package.Id} / {facing.Id}");
                        Assert.That(tightSize.y,
                            Is.LessThanOrEqualTo(fullSize.y + 0.001f),
                            $"{package.Id} / {facing.Id}");
                        Assert.That(tightSize.x < fullSize.x - 0.001f ||
                                    tightSize.y < fullSize.y - 0.001f,
                            Is.True,
                            $"{package.Id} / {facing.Id} retained its full canvas");
                    }
                    finally
                    {
                        Object.DestroyImmediate(tight);
                    }
                    facingCount++;
                }
            }

            Assert.That(packageCount, Is.GreaterThanOrEqualTo(32));
            Assert.That(facingCount, Is.GreaterThanOrEqualTo(128));
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
            var entry = BuildingCatalog.Find(
                "cityforge.v3.residential.new_england_three_bay_01");
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
        public void TopDownViewIsTemporaryAndDoesNotMutateLotData()
        {
            var root = new GameObject("Top Down View Test");
            try
            {
                LogAssert.ignoreFailingMessages = true;
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Top Down Test", LotType.Residential, 4, 4);
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                world.SetInspectionMode(BuildingInspectionMode.Hybrid);
                var savedData = world.Session.Serialize();
                var worldZScreenBefore = new Vector2(
                    Vector3.Dot(Vector3.forward, Camera.main.transform.right),
                    Vector3.Dot(Vector3.forward, Camera.main.transform.up)).normalized;

                world.ToggleTopDownView();

                Assert.That(world.TopDownViewEnabled, Is.True);
                Assert.That(Mathf.DeltaAngle(world.CameraPitchDegrees, 90f),
                    Is.EqualTo(0f).Within(0.001f));
                Assert.That(world.InspectionMode,
                    Is.EqualTo(BuildingInspectionMode.Primitive));
                var worldZScreenAfter = new Vector2(
                    Vector3.Dot(Vector3.forward, Camera.main.transform.right),
                    Vector3.Dot(Vector3.forward, Camera.main.transform.up)).normalized;
                Assert.That(Vector2.Angle(worldZScreenAfter,
                    LotWorldController.SnapTopDownScreenDirection(
                        worldZScreenBefore)), Is.LessThan(0.001f));
                Assert.That(world.Session.Serialize(), Is.EqualTo(savedData));
                world.SetBuildingEditorContext(true, false);
                Assert.That(world.InspectionMode,
                    Is.EqualTo(BuildingInspectionMode.Primitive));

                world.ToggleTopDownView();

                Assert.That(world.TopDownViewEnabled, Is.False);
                Assert.That(world.InspectionMode,
                    Is.EqualTo(BuildingInspectionMode.Hybrid));
                Assert.That(world.Session.Serialize(), Is.EqualTo(savedData));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SelectedTopDownBuildingShowsItsAuthoredEntranceArrow()
        {
            var root = new GameObject("Authored Building Front Arrow Test");
            try
            {
                LogAssert.ignoreFailingMessages = true;
                const string buildingId =
                    "cityforge.v3.residential.ny_brownstone_tripo_01";
                var package = HybridBuildingPackageRegistry.Load(
                    BuildingCatalog.Find(buildingId).PackageResourcePath);
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Front Arrow Test", LotType.Residential, 5, 5);
                Assert.That(world.PlaceBuildingAtCenter(buildingId), Is.True);
                world.SetBuildingEditorContext(true, false);
                var camera = root.GetComponentInChildren<Camera>();
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var screen = camera.WorldToScreenPoint(Vector3.zero);
                var panelPoint = new Vector2(screen.x, panelSize.y - screen.y);
                Assert.That(world.BeginBuildingDragFromPanel(
                    panelPoint, panelSize), Is.True);

                Assert.That(world.SelectedBuildingFrontMarkerVisible, Is.False);
                world.ToggleTopDownView();

                Assert.That(world.SelectedBuildingFrontMarkerVisible, Is.True);
                Assert.That(Vector3.Angle(
                    world.SelectedBuildingFrontDirection,
                    LotWorldController.AuthoredBuildingFrontDirection(
                        package, 0)), Is.LessThan(0.001f));

                world.RotateSelected(1);
                Assert.That(Vector3.Angle(
                    world.SelectedBuildingFrontDirection,
                    LotWorldController.AuthoredBuildingFrontDirection(
                        package, 1)), Is.LessThan(0.001f));
                Assert.That(world.SelectedBuildingFrontMarkerVisible, Is.True);

                world.DeselectAll();
                Assert.That(world.SelectedBuildingFrontMarkerVisible, Is.False);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NyStreetTreeUsesOnlyTheBoundedVisibleAuthoredFrontApron()
        {
            const string buildingId =
                "cityforge.v3.residential.ny_brownstone_tripo_01";
            var package = HybridBuildingPackageRegistry.Load(
                BuildingCatalog.Find(buildingId).PackageResourcePath);
            var building = new Vector3(-20f, 0f, 5f);
            var front = LotWorldController.AuthoredBuildingFrontDirection(
                package, 0);
            var towardCamera = front;
            var savedStreetTree = new Vector3(
                -20.435188f, 0f, -6.456524f);

            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                savedStreetTree, building, package, 0, towardCamera), Is.True,
                "The unambiguous NY Residential tree is 1.20 m beyond the " +
                "authored entrance edge and must remain visible.");

            var side = building + Vector3.right *
                (package.WidthMeters * 0.5f + 0.75f);
            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                side, building, package, 0, towardCamera), Is.False,
                "The front exception may not leak around a side wall.");

            var rear = building - front *
                (package.DepthMeters * 0.5f + 0.5f);
            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                rear, building, package, 0, towardCamera), Is.False,
                "The back of the building keeps ordinary depth occlusion.");

            var fartherIntoStreet = building + front *
                (package.DepthMeters * 0.5f + 12f);
            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                fartherIntoStreet, building, package, 0, towardCamera), Is.True,
                "Camera-front classification comes from the ground anchor and " +
                "footprint support plane, not an arbitrary facade apron depth.");

            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                savedStreetTree, building, package, 0, -front), Is.False,
                "A facade facing away from the camera may not promote flora.");
        }

        [Test]
        public void AuthoredFrontApronRotatesWithEveryPlacedBuildingQuarterTurn()
        {
            const string buildingId =
                "cityforge.v3.residential.ny_brownstone_bay_windows_tripo_01";
            var package = HybridBuildingPackageRegistry.Load(
                BuildingCatalog.Find(buildingId).PackageResourcePath);
            var building = new Vector3(3f, 0f, -4f);
            for (var turn = 0; turn < 4; turn++)
            {
                var front = LotWorldController.AuthoredBuildingFrontDirection(
                    package, turn);
                var point = building + front *
                    (package.DepthMeters * 0.5f + 1f);
                Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                    point, building, package, turn, front), Is.True,
                    $"quarter turn {turn}");
            }
        }

        [Test]
        public void FrontApronVisibilityUsesOneParallelOrthographicDirection()
        {
            const string buildingId =
                "cityforge.v3.residential.ny_fancy_townhouse_tripo_01";
            var package = HybridBuildingPackageRegistry.Load(
                BuildingCatalog.Find(buildingId).PackageResourcePath);
            var building = new Vector3(-6f, 0f, 1f);
            var towardCamera = new Vector3(0.5664f, 0f, -0.8241f);
            var savedTree = new Vector3(-8.542774f, 0f, -4.711983f);

            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                savedTree, building, package, 0, towardCamera), Is.True);
        }

        [Test]
        public void FacadeCenterTreeUsesNearestBuildingSurface()
        {
            const string buildingId =
                "cityforge.v3.residential.ny_brownstone_tripo_01";
            var package = HybridBuildingPackageRegistry.Load(
                BuildingCatalog.Find(buildingId).PackageResourcePath);
            var building = Vector3.zero;
            var towardCamera = new Vector3(0.5664f, 0f, -0.8241f).normalized;
            var facadeCenterTree = new Vector3(
                0f, 0f, -package.DepthMeters * 0.5f - 1f);

            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                facadeCenterTree, building, package, 0, towardCamera), Is.True,
                "The closest facade surface is behind the tree relative to " +
                "the camera, so that building may recover covered pixels.");
            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                Vector3.zero, building, package, 0, towardCamera), Is.False,
                "An anchor deep inside the footprint remains physically hidden.");
        }

        [Test]
        public void TreeJustInsideVisibleFacadeSeamUsesShallowRecoveryBand()
        {
            const string buildingId =
                "cityforge.v3.residential.ny_brownstone_tripo_01";
            var package = HybridBuildingPackageRegistry.Load(
                BuildingCatalog.Find(buildingId).PackageResourcePath);
            var towardCamera = new Vector3(0f, 0f, -1f);
            var justInsideFacade = new Vector3(
                0f, 0f, -package.DepthMeters * 0.5f + 0.5f);
            var deepInside = new Vector3(
                0f, 0f, -package.DepthMeters * 0.5f + 2f);

            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                justInsideFacade, Vector3.zero, package, 0, towardCamera),
                Is.True,
                "A half-meter facade registration overlap must not make a " +
                "sidewalk tree disappear.");
            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                deepInside, Vector3.zero, package, 0, towardCamera), Is.False,
                "The tolerance must not expose trees deep inside a building.");
        }

        [Test]
        public void LateralFacadeTreeUsesNearestSurfaceWhenGroundRayMissesFootprint()
        {
            const string buildingId =
                "cityforge.v3.residential.ny_brownstone_tripo_01";
            var package = HybridBuildingPackageRegistry.Load(
                BuildingCatalog.Find(buildingId).PackageResourcePath);
            var towardCamera = new Vector3(0.5664f, 0f, -0.8241f).normalized;
            var lateralFacadeTree = new Vector3(
                package.WidthMeters * 0.5f + 0.75f,
                0f,
                package.DepthMeters * 0.5f - 0.1f);

            Assert.That(LotWorldController.IsInVisibleBuildingFrontApron(
                lateralFacadeTree, Vector3.zero, package, 0, towardCamera),
                Is.True,
                "A wide tree beside the facade must recover pixels covered " +
                "by the host building even when its trunk ray misses the " +
                "rectangular footprint near the corner.");
        }

        [Test]
        public void FloraSpritePickingRejectsTransparentPixels()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = Enumerable.Repeat(Color.clear, 16).ToArray();
            pixels[2 + 2 * 4] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f), 1f);
            var root = new GameObject("Alpha Flora Pick Test");
            var cameraObject = new GameObject("Alpha Flora Pick Camera");
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 2f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.transform.rotation = Quaternion.identity;
                renderer.transform.rotation = Quaternion.identity;

                var opaquePixel = camera.WorldToScreenPoint(
                    new Vector3(0.5f, 0.5f, 0f));
                var transparentPixel = camera.WorldToScreenPoint(
                    new Vector3(-0.5f, -0.5f, 0f));
                Assert.That(LotWorldController.SpriteRendererContainsCameraPixel(
                    renderer, camera, opaquePixel), Is.True);
                Assert.That(LotWorldController.SpriteRendererContainsCameraPixel(
                    renderer, camera, transparentPixel), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void LotEditorTopBarExposesTopDownToggleInEveryCategory()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");

            StringAssert.Contains("TOP DOWN [T]", source);
            StringAssert.Contains("viewActions.Add", source);
            StringAssert.Contains("ToggleTopDownView", source);
        }

        [Test]
        [TestCase(1f, 0.2f, 1f, 0f)]
        [TestCase(-0.9f, 0.1f, -1f, 0f)]
        [TestCase(0.2f, 1f, 0f, 1f)]
        [TestCase(0.1f, -0.8f, 0f, -1f)]
        public void TopDownRotationSnapsNearestVisibleGridAxisLevel(
            float screenX, float screenY, float expectedX, float expectedY)
        {
            var expectedWorldZScreen = new Vector2(expectedX, expectedY);
            var rotation = LotWorldController.ResolveTopDownRotation(
                new Vector2(screenX, screenY));

            Assert.That(Vector3.Angle(rotation * Vector3.forward,
                Vector3.down), Is.LessThan(0.001f));
            var actualWorldZScreen = new Vector2(
                Vector3.Dot(Vector3.forward, rotation * Vector3.right),
                Vector3.Dot(Vector3.forward, rotation * Vector3.up)).normalized;
            Assert.That(Vector2.Angle(actualWorldZScreen,
                expectedWorldZScreen), Is.LessThan(0.001f));
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
        public void HybridBuildingArtworkBindsItsOwnDepthStencilId()
        {
            var cameraObject = new GameObject("Stencil Presentation Camera");
            var presentationObject = new GameObject("Stencil Presentation");
            try
            {
                var presentation =
                    presentationObject.AddComponent<HybridBuildingPresentation>();
                presentation.Build(cameraObject.AddComponent<Camera>(),
                    HybridBuildingPackageRegistry.NewEnglandHouse);
                var reference =
                    LotWorldController.BuildingDepthOcclusionStencilReference(3);
                presentation.SetHostBuildingStencilReference(reference);

                Assert.That(presentation.HostBuildingStencilReference,
                    Is.EqualTo(reference));
                var renderer = presentationObject
                    .GetComponentInChildren<SpriteRenderer>();
                Assert.That(renderer.sharedMaterial.GetFloat(
                    "_BuildingHostStencilRef"), Is.EqualTo(reference));
            }
            finally
            {
                Object.DestroyImmediate(presentationObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ExperimentalThreeDimensionalBuildingLibraryIsAvailable()
        {
            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            Assert.That(Resources.Load<GameObject>(
                LotWorldController.BrownstoneBuilding22kResource), Is.Not.Null);
            StringAssert.Contains("3D BUILDING LIBRARY", app);
            StringAssert.Contains("CREATE 3D BUILDINGS TEST LOT", app);
            StringAssert.Contains("21,743 TRIANGLES", app);
        }

        [Test]
        public void ExperimentalBrownstoneCastsMorningAndAfternoonShadowsOnAllReceivers()
        {
            var root = new GameObject("Brownstone Receiver Shadow Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.CreateExperimental3DBuildingsLot();

                Assert.That(world.ExperimentalBuilding3DCount, Is.EqualTo(3));
                Assert.That(Resources.Load<GameObject>(
                    LotWorldController.LowPolyBrownstoneV01Resource),
                    Is.Not.Null);
                var lotSurface = root.GetComponentsInChildren<Renderer>(true)
                    .Single(renderer => renderer.gameObject.name == "Lot Surface");
                Assert.That(lotSurface.receiveShadows, Is.True);
                Assert.That(lotSurface.sharedMaterial.shader.name,
                    Is.EqualTo("CityForgeV3/Experimental3DGroundReceiver"),
                    "The all-3D lot must use the textured native mesh-shadow receiver.");
                world.SetBaseTexture("grass-lush");
                Assert.That(lotSurface.sharedMaterial.mainTexture, Is.Not.Null,
                    "Switching to the 3D shadow receiver must preserve the selected lush-grass texture.");
                Assert.That(lotSurface.sharedMaterial.color,
                    Is.EqualTo(Color.white),
                    "Authored lush grass must not inherit a time-of-day or environment color tint.");
                Assert.That(lotSurface.sharedMaterial.GetColor("_DisplayMatch"),
                    Is.EqualTo(new Color(0.75f, 0.80f, 0.75f, 1f)),
                    "The Game-view grass must retain the measured chooser-preview display calibration.");
                Assert.That(lotSurface.sharedMaterial.renderQueue, Is.EqualTo(2000),
                    "Grass must remain opaque beneath the isolated transparent building shadows.");
                Assert.That(QualitySettings.shadowDistance,
                    Is.GreaterThanOrEqualTo(150f));
                Assert.That(QualitySettings.shadowCascades, Is.EqualTo(4));
                Assert.That(world.ExperimentalBuilding3DFloraShadowCasterCount,
                    Is.GreaterThan(0));
                var visibleBuildings = root.GetComponentsInChildren<Transform>(true)
                    .Where(transform =>
                        transform.name == "3D Building — Brownstone 22K")
                    .ToArray();
                Assert.That(visibleBuildings.Length, Is.EqualTo(2));
                var brownstoneMaterials = visibleBuildings
                    .SelectMany(transform =>
                        transform.GetComponentsInChildren<Renderer>(true))
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .ToArray();
                Assert.That(brownstoneMaterials, Is.Not.Empty);
                Assert.That(brownstoneMaterials.All(material =>
                        material.shader.name ==
                        "CityForgeV3/Experimental3DBuildingPBR"),
                    Is.True,
                    "The Tripo brownstone must use the local linearized PBR response instead of Gamma-space Standard shading.");
                Assert.That(brownstoneMaterials.All(material =>
                        material.GetFloat("_Contrast") > 1.25f &&
                        material.GetFloat("_Saturation") > 1.20f),
                    Is.True,
                    "The Gamma-space pilot must preserve charcoal and green chroma without lowering brownstone midtones.");
                var lowPolyBrownstone = root
                    .GetComponentsInChildren<Transform>(true)
                    .Single(transform => transform.name ==
                        "3D Building — Low-Poly Brownstone V01");
                Assert.That(Quaternion.Angle(
                        lowPolyBrownstone.localRotation,
                        Quaternion.Euler(-90f, 180f, 0f)),
                    Is.LessThan(0.01f),
                    "The comparison brownstone must stand upright with its front aligned to the other façades.");
                Assert.That(visibleBuildings.SelectMany(transform =>
                        transform.GetComponentsInChildren<Renderer>(true))
                    .All(renderer => renderer.receiveShadows &&
                        renderer.shadowCastingMode == ShadowCastingMode.Off),
                    Is.True,
                    "The beauty meshes receive shadows while their single hidden mesh copies cast, preventing doubles.");
                var nativeGroundCasters = root.GetComponentsInChildren<Transform>(true)
                    .Where(transform => transform.name.Contains(
                        "Native Ground Shadow Caster"))
                    .ToArray();
                Assert.That(nativeGroundCasters.Length, Is.EqualTo(3));
                Assert.That(nativeGroundCasters.SelectMany(transform =>
                        transform.GetComponentsInChildren<Renderer>(true))
                    .All(renderer => renderer.gameObject.layer == 0 &&
                        renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly),
                    Is.True,
                    "Each 3D building must have exactly one mesh-detail caster visible to the main sun.");
                var projectedGroundShadows = root
                    .GetComponentsInChildren<Transform>(true)
                    .Where(transform => transform.name.StartsWith(
                        "3D Building Ground Shadow —"))
                    .ToArray();
                Assert.That(projectedGroundShadows.Length, Is.EqualTo(3));
                Assert.That(Shader.Find(
                    "CityForgeV3/ProjectedBuildingMeshShadow"), Is.Not.Null);
                Assert.That(projectedGroundShadows.All(transform =>
                        transform.GetComponentsInChildren<MeshFilter>(true)
                            .Any(filter => filter.sharedMesh != null) &&
                        transform.GetComponentsInChildren<MeshRenderer>(true)
                            .All(renderer =>
                                renderer.sharedMaterial.shader.name ==
                                    "CityForgeV3/ProjectedBuildingMeshShadow" &&
                                renderer.sharedMaterial.renderQueue == 3001 &&
                                renderer.shadowCastingMode ==
                                    ShadowCastingMode.Off)),
                    Is.True,
                    "Each real 3D building needs a projected copy of its actual render meshes so stairs and façade details remain in the ground silhouette.");
                var casterRoots = root.GetComponentsInChildren<Transform>(true)
                    .Where(transform => transform.name.Contains(
                        "Flora/Prop Shadow Caster"))
                    .ToArray();
                var caster = root.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => casterRoots.Any(casterRoot =>
                        renderer.transform.IsChildOf(casterRoot)))
                    .ToArray();
                Assert.That(caster, Is.Not.Empty);
                Assert.That(caster.All(renderer =>
                    renderer.gameObject.layer == 31), Is.True);
                Assert.That(caster.All(renderer => renderer.shadowCastingMode ==
                    ShadowCastingMode.ShadowsOnly), Is.True);

                world.SetTimeOfDay(TimeOfDayPreset.Morning);
                var sun = root.GetComponentsInChildren<Light>(true)
                    .Single(light => light.gameObject.name == "Time of Day Sun");
                Assert.That(sun.cullingMask & (1 << 31), Is.Zero,
                    "The beauty sun must not render the private layer-31 ShadowsOnly duplicate or buildings cast twice.");
                var floraShadowSun = root.GetComponentsInChildren<Light>(true)
                    .Single(light => light.gameObject.name == "Flora Shadow Alignment Sun");
                Assert.That(floraShadowSun.cullingMask, Is.EqualTo(1 << 31),
                    "Only the isolated flora/billboard light may render the ShadowsOnly duplicate.");
                var morningRay = sun.transform.forward;
                Assert.That(morningRay.z, Is.LessThan(-0.5f),
                    "Morning rays must travel west from an eastern sun.");
                Assert.That(Mathf.Asin(-morningRay.y) * Mathf.Rad2Deg,
                    Is.EqualTo(48f).Within(0.1f),
                    "The 3D pilot uses a higher morning sun to avoid oversized native shadows.");
                Assert.That(sun.intensity, Is.EqualTo(0.62f).Within(0.001f));
                Assert.That(RenderSettings.ambientMode,
                    Is.EqualTo(UnityEngine.Rendering.AmbientMode.Skybox));
                Assert.That(RenderSettings.ambientIntensity,
                    Is.EqualTo(0.92f).Within(0.001f));
                Assert.That(RenderSettings.skybox, Is.Not.Null);
                Assert.That(RenderSettings.skybox.name,
                    Does.Contain("Tripo Studio IBL"));
                Assert.That(lotSurface.sharedMaterial.color.g,
                    Is.EqualTo(1f).Within(0.01f),
                    "Authored grass must remain neutrally tinted instead of inheriting the time-of-day olive calibration.");
                world.SetTimeOfDay(TimeOfDayPreset.Noon);
                Assert.That(sun.intensity, Is.EqualTo(0.55f).Within(0.001f));
                Assert.That(projectedGroundShadows.All(transform =>
                        transform.gameObject.activeSelf), Is.True,
                    "A freshly loaded noon 3D lot needs restrained contact shadows because its exact-color grass receiver is unlit.");
                Assert.That(lotSurface.sharedMaterial.color.g,
                    Is.EqualTo(1f).Within(0.01f),
                    "Noon must preserve the authored lush-grass texture color.");
                Assert.That(root.GetComponentsInChildren<Transform>(true)
                    .Any(transform => transform.name.Contains(
                        "Directional Ground Shadow")), Is.False,
                    "Real 3D buildings must rely on their mesh silhouette rather than a bounds-derived ground hull.");
                world.SetTimeOfDay(TimeOfDayPreset.Afternoon);
                var afternoonRay = sun.transform.forward;
                Assert.That(afternoonRay.z, Is.GreaterThan(0.5f),
                    "Afternoon rays must travel east from a western sun.");
                Assert.That(Vector2.Dot(
                        new Vector2(morningRay.x, morningRay.z).normalized,
                        new Vector2(afternoonRay.x, afternoonRay.z).normalized),
                    Is.LessThan(-0.99f),
                    "Morning and afternoon brownstone shadows must project to opposite sides.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ExperimentalBuildingRebuildImmediatelyPopulatesSavedLotShadows()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "CityForgeV3/Runtime/World/LotWorldController.Buildings3D.cs"));
            var rebuildStart = source.IndexOf(
                "private void RebuildExperimentalBuilding3DPresentations()",
                System.StringComparison.Ordinal);
            var rebuildEnd = source.IndexOf(
                "private void BuildExperimentalBuilding3DProjectedGroundShadow(",
                rebuildStart, System.StringComparison.Ordinal);
            var rebuild = source.Substring(rebuildStart,
                rebuildEnd - rebuildStart);

            StringAssert.Contains(
                "UpdateExperimentalBuilding3DProjectedGroundShadows();",
                rebuild,
                "The normal LoadLot ordering rebuilds runtime objects after lighting; the rebuild must populate shadow meshes itself.");
        }

        [Test]
        public void PlayerBuildKeepsExperimentalBuildingMeshesReadableForGroundShadows()
        {
            var brownstoneMeta = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Buildings3D/BrownstoneBuilding22k/brownstone-building-22k.fbx.meta"));
            var lowPolyMeta = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Buildings3D/LowPolyBrownstoneV01/LowPolyBrownstone.fbx.meta"));
            var shadowSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "CityForgeV3/Runtime/World/LotWorldController.Buildings3D.cs"));

            StringAssert.Contains("isReadable: 1", brownstoneMeta);
            StringAssert.Contains("isReadable: 1", lowPolyMeta);
            StringAssert.Contains("if (!filter.sharedMesh.isReadable)",
                shadowSource,
                "Standalone players need a conservative fallback rather than silently losing every ground shadow.");
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
            var entry = BuildingCatalog.Find(
                "cityforge.v3.residential.new_england_three_bay_01");
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

        [Test]
        public void SeasonsHaveDeterministicNonAccumulatingPresentationContracts()
        {
            var baseline = new Color(0.4f, 0.6f, 0.3f, 1f);

            Assert.That(SeasonLighting.Label(SeasonPreset.Spring),
                Is.EqualTo("SPRING"));
            Assert.That(SeasonLighting.GroundColor(
                SeasonPreset.Summer, baseline), Is.EqualTo(baseline));
            Assert.That(SeasonLighting.BuildingTint(SeasonPreset.Summer),
                Is.EqualTo(Color.white));
            Assert.That(SeasonLighting.FloraTint(SeasonPreset.Autumn).r,
                Is.GreaterThan(SeasonLighting.FloraTint(
                    SeasonPreset.Autumn).g));
            var winterFlora = SeasonLighting.FloraTint(SeasonPreset.Winter);
            Assert.That(winterFlora.r, Is.GreaterThanOrEqualTo(0.90f));
            Assert.That(winterFlora.b, Is.GreaterThan(winterFlora.r),
                "Winter should preserve authored bark color with only a restrained cool cast.");
            Assert.That(SeasonLighting.GroundColor(
                    SeasonPreset.Winter, baseline).b,
                Is.GreaterThan(baseline.b));
        }

        [TestCase(TimeOfDayPreset.Morning, "MORNING", 24f, 90f)]
        [TestCase(TimeOfDayPreset.Noon, "NOON", 70f, 180f)]
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
            Assert.That(rayDirection.z, Is.GreaterThan(0f),
                "A western afternoon sun must cast its rays toward the east.");
            Assert.That(Mathf.Abs(rayDirection.x), Is.LessThan(0.001f),
                "Due-west afternoon light must not introduce a north/south component.");
            Assert.That(Vector3.Dot(Vector3.forward, -rayDirection),
                Is.LessThan(0f),
                "An east-facing wall must face away from the afternoon sun.");
        }

        [Test]
        public void NativeAfternoonSunUsesCanonicalCharacterShadowDirection()
        {
            var root = new GameObject("Native Character Shadow Direction Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetTimeOfDay(TimeOfDayPreset.Afternoon);
                var sun = root.GetComponentsInChildren<Light>()
                    .First(light => light.name == "Time of Day Sun");
                var actualRay = sun.transform.rotation * Vector3.forward;
                var expectedRay = TimeOfDayLighting.SunRotation(
                    TimeOfDayPreset.Afternoon) * Vector3.forward;
                Assert.That(Vector3.Angle(actualRay, expectedRay),
                    Is.LessThan(0.001f));
                Assert.That(Mathf.Abs(actualRay.x), Is.LessThan(0.001f));
                Assert.That(actualRay.z, Is.GreaterThan(0.999f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
            Assert.That(horizontal.y, Is.LessThan(-0.999f),
                "A due-east morning sun must cast its shadow due west.");
            Assert.That(Mathf.Abs(horizontal.x), Is.LessThan(0.001f),
                "Morning building shadows must not drift north or south.");
        }

        [Test]
        public void NoonSunFromSouthCastsReadableShadowsNorthward()
        {
            var rotation = TimeOfDayLighting.SunRotation(
                TimeOfDayPreset.Noon);
            var rayDirection = rotation * Vector3.forward;
            var horizontal = new Vector2(
                rayDirection.x, rayDirection.z).normalized;

            Assert.That(rayDirection.y, Is.LessThan(-0.9f),
                "Noon sunlight must remain high and travel downward.");
            Assert.That(horizontal.x, Is.LessThan(-0.999f),
                "A southern noon sun must cast its shadows northward.");
            Assert.That(Mathf.Abs(horizontal.y), Is.LessThan(0.001f),
                "Due-south noon light must not drift east or west.");
            Assert.That(Mathf.Abs(rayDirection.x), Is.GreaterThan(0.3f),
                "Noon needs enough horizontal travel to produce a readable shadow.");
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
        public void ShadowReceiverPreservesLowAngleDirectionalContrast()
        {
            var path = System.IO.Path.Combine(
                Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Shaders/ShadowReceivingLotSurface.shader");
            var source = System.IO.File.ReadAllText(path);
            StringAssert.Contains(
                "_AmbientFloor, 1.0h, diffuse * shadow", source);
            StringAssert.DoesNotContain(
                "max(_AmbientFloor, diffuse * shadow)", source);
        }

        [Test]
        public void ExperimentalBuildingsUseMeshShadowsWithoutBoundsGroundHull()
        {
            var runtime = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Buildings3D.cs");
            StringAssert.DoesNotContain(
                "BuildExperimentalBuilding3DGroundShadow", runtime);
            StringAssert.DoesNotContain(
                "Bounds-Derived Directional Ground Shadow", runtime);
            StringAssert.Contains(
                "renderer.shadowCastingMode = ShadowCastingMode.On", runtime);
            StringAssert.Contains(
                "renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly", runtime);
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

            Assert.That(noon.SunElevation, Is.EqualTo(70f));
            Assert.That(noon.SunAzimuth, Is.EqualTo(180f));
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

            Assert.That(evening, Is.EqualTo(new Color(0.66f, 0.69f, 0.76f)));
            Assert.That(evening.grayscale, Is.LessThan(afternoon.grayscale));
            Assert.That(afternoon, Is.EqualTo(Color.white),
                "The approved v30 afternoon exposure must not change.");
        }

        [Test]
        public void HybridNightBaseRemainsReadableBehindWindowOverlays()
        {
            var night = HybridBuildingPresentation.NeutralBaseTintFor(
                TimeOfDayPreset.Night,
                false);

            Assert.That(night, Is.EqualTo(new Color(0.38f, 0.43f, 0.54f)));
            Assert.That(night.grayscale, Is.GreaterThan(0.40f));
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

        [TestCase(BuildingInspectionMode.Artwork, 1f)]
        [TestCase(BuildingInspectionMode.Hybrid, 0.20f)]
        [TestCase(BuildingInspectionMode.Primitive, 1f)]
        public void InspectionModeOwnsArtworkOpacity(
            BuildingInspectionMode mode,
            float expected)
        {
            Assert.That(
                BuildingInspectionPolicy.ArtworkOpacity(mode, 1f),
                Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void BuildingEditorRedrawPreservesExplicitOverlayMode()
        {
            var root = new GameObject("Building Overlay Redraw Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetBuildingEditorContext(true, false);
                world.SetInspectionMode(BuildingInspectionMode.Hybrid);

                world.SetBuildingEditorContext(true, false);

                Assert.That(world.InspectionMode,
                    Is.EqualTo(BuildingInspectionMode.Hybrid));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
        public void CatalogBuildingFollowsPointerAsASeventyFivePercentPreviewUntilCommitted()
        {
            var root = new GameObject("Building Placement Preview Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Placement Preview", LotType.Mixed, 5, 5);
                world.SetBuildingEditorContext(true, false);

                Assert.That(world.BeginBuildingPlacementAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                Assert.That(world.BuildingPlacementPreviewActive, Is.True);
                Assert.That(world.IsSelected, Is.True);
                Assert.That(world.ActiveObjectSelection,
                    Is.EqualTo(LotObjectSelectionKind.Building));
                Assert.That(world.SelectedBuildingOpacity,
                    Is.EqualTo(LotWorldController.BuildingPlacementPreviewOpacity)
                        .Within(0.001f));

                Assert.That(world.EndBuildingDrag(), Is.True);
                Assert.That(world.BuildingPlacementPreviewActive, Is.False);
                Assert.That(world.SelectedBuildingOpacity, Is.EqualTo(1f).Within(0.001f));
                Assert.That(world.IsSelected, Is.True,
                    "The first click commits placement but keeps normal building selection.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Production3DBuildingBeginsAsASeventyFivePercentPlacementPreview()
        {
            var root = new GameObject("3D Building Placement Preview Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("3D Placement Preview", LotType.Mixed, 6, 6);

                Assert.That(world.BeginExperimentalBuilding3DPlacement(
                    LotWorldController.PlymouthStoreProductionId), Is.True);
                Assert.That(world.ExperimentalBuilding3DCount, Is.EqualTo(1));
                Assert.That(world.Building3DPlacementPreviewActive, Is.True);
                Assert.That(LotWorldController.Building3DPlacementPreviewOpacity,
                    Is.EqualTo(0.75f));

                world.EndBuilding3DDrag();
                Assert.That(world.Building3DPlacementPreviewActive, Is.False,
                    "The first placement click must restore authored materials.");
                Assert.That(world.SelectedBuilding3DIndex, Is.EqualTo(0),
                    "Committed placement remains selected for normal editing.");
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
        public void BuildingPropAttachment_RoundTripsHostLocalPosition()
        {
            var session = new LotEditorSession();
            session.AddBuilding(BuildingCatalog.ColonialGovernmentHouseId, -4, 0);
            session.Data.Buildings[0].Attachments.Add(new PlacedBuildingProp
            {
                ComponentId = BuildingPropCatalog.AleHouseSignId,
                HasHostLocalPosition = true,
                HostLocalX = 3.25f,
                HostLocalY = 4.75f,
                HostLocalZ = 2.1f
            });

            var restored = new LotEditorSession();
            restored.Restore(session.Serialize());
            var attachment = restored.Data.Buildings[0].Attachments[0];
            Assert.That(attachment.HasHostLocalPosition, Is.True);
            Assert.That(attachment.HostLocalX, Is.EqualTo(3.25f));
            Assert.That(attachment.HostLocalY, Is.EqualTo(4.75f));
            Assert.That(attachment.HostLocalZ, Is.EqualTo(2.1f));
        }

        [Test]
        public void BuildingPropAttachment_FourHostTurnsReturnExactWorldPoint()
        {
            var building = new PlacedBuilding
            {
                CellX = 3,
                CellZ = -2
            };
            var local = new Vector3(4.2f, 5.1f, 5.58f);
            var initial = LotWorldController.ResolveHostLocalWorldPosition(
                building, local);

            building.RotationQuarterTurns = 1;
            Assert.That(Vector3.Distance(
                LotWorldController.ResolveHostLocalWorldPosition(building, local),
                new Vector3(8.58f, 5.1f, -6.2f)), Is.LessThan(0.0001f));
            building.RotationQuarterTurns = 4;
            Assert.That(Vector3.Distance(
                LotWorldController.ResolveHostLocalWorldPosition(building, local),
                initial), Is.LessThan(0.0001f));
            building.RotationQuarterTurns = -4;
            Assert.That(Vector3.Distance(
                LotWorldController.ResolveHostLocalWorldPosition(building, local),
                initial), Is.LessThan(0.0001f));
        }

        [Test]
        public void BuildingPropPixelResolvesNearestPrimitiveFacade()
        {
            var ray = new Ray(new Vector3(-12f, 4f, 12f),
                new Vector3(1f, 0f, -1f).normalized);

            Assert.That(LotWorldController.TryResolvePrimitiveFacadeLocalPosition(
                ray, Vector3.zero, Quaternion.identity, 10f, 8f, 9f, 0.2f,
                out var local, out var elevation), Is.True);
            Assert.That(elevation, Is.EqualTo("Left"));
            Assert.That(local.x, Is.EqualTo(-5.2f).Within(0.001f));
            Assert.That(local.y, Is.EqualTo(4f).Within(0.001f));
            Assert.That(local.z, Is.EqualTo(5.2f).Within(0.001f));
        }

        [Test]
        public void PrimitiveFacadeAnchorRotatesAroundHostWithoutDrift()
        {
            var building = new PlacedBuilding { CellX = 2, CellZ = -3 };
            var local = new Vector3(-5.2f, 4f, 2.7f);
            var initial = LotWorldController.ResolveHostLocalWorldPosition(
                building, local);

            building.RotationQuarterTurns = 1;
            var clockwise = LotWorldController.ResolveHostLocalWorldPosition(
                building, local);
            Assert.That(Vector3.Distance(clockwise,
                new Vector3(4.7f, 4f, 2.2f)), Is.LessThan(0.0001f));
            building.RotationQuarterTurns = 4;
            Assert.That(Vector3.Distance(
                LotWorldController.ResolveHostLocalWorldPosition(building, local),
                initial), Is.LessThan(0.0001f));
        }

        [Test]
        public void LegacyTreeArtworkLoadsAndFloraPlacementsRoundTrip()
        {
            foreach (var id in new[]
                     {
                         "maple", "ashe", "oak", "date-palm",
                         "narrow-street-tree", "street-tree-3d",
                         "vendor-red-maple", "vendor-red-maple-young",
                         "vendor-balsam-fir-broad", "vendor-balsam-fir-tall",
                         "vendor-balsam-fir-classic", "vendor-hickory",
                         "vendor-willow", "vendor-cypress-oak",
                         "vendor-cypress-oak-wide", "vendor-oregon-ash",
                         "vendor-oregon-ash-wide", "vendor-spruce-narrow",
                         "vendor-spruce-classic", "vendor-spruce-wide",
                         "small-hedge", "medium-hedge",
                         "long-hedge"
                     })
                Assert.That(Resources.Load<Texture2D>(
                    $"CityForgeV3/Flora/LegacyTreesV01/{id}-summer"),
                    Is.Not.Null, id);

            foreach (var season in new[] { "spring", "summer", "autumn", "winter" })
            {
                Assert.That(Resources.Load<Texture2D>(
                    $"CityForgeV3/Flora/LegacyTreesV01/date-palm-{season}"),
                    Is.Not.Null, season);
                Assert.That(Resources.Load<Texture2D>(
                    $"CityForgeV3/Flora/LegacyTreesV01/narrow-street-tree-{season}"),
                    Is.Not.Null, season);
                Assert.That(Resources.Load<Texture2D>(
                    $"CityForgeV3/Flora/LegacyTreesV01/street-tree-3d-{season}"),
                    Is.Not.Null, season);
                foreach (var vendorId in new[]
                         {
                             "vendor-red-maple", "vendor-red-maple-young",
                             "vendor-balsam-fir-broad", "vendor-balsam-fir-tall",
                             "vendor-balsam-fir-classic", "vendor-hickory",
                             "vendor-willow", "vendor-cypress-oak",
                             "vendor-cypress-oak-wide", "vendor-oregon-ash",
                             "vendor-oregon-ash-wide", "vendor-spruce-narrow",
                             "vendor-spruce-classic", "vendor-spruce-wide"
                         })
                    Assert.That(Resources.Load<Texture2D>(
                        $"CityForgeV3/Flora/LegacyTreesV01/{vendorId}-{season}"),
                        Is.Not.Null, $"{vendorId}-{season}");
                Assert.That(Resources.Load<Texture2D>(
                    $"CityForgeV3/Flora/LegacyTreesV01/small-hedge-{season}"),
                    Is.Not.Null, season);
                Assert.That(Resources.Load<Texture2D>(
                    $"CityForgeV3/Flora/LegacyTreesV01/medium-hedge-{season}"),
                    Is.Not.Null, season);
                Assert.That(Resources.Load<Texture2D>(
                    $"CityForgeV3/Flora/LegacyTreesV01/long-hedge-{season}"),
                    Is.Not.Null, season);
            }

            var source = new LotEditorSession();
            source.Data.Flora.Add(new PlacedFlora
            {
                InstanceId = "tree-1",
                FloraId = "maple",
                PositionX = 3.5f,
                PositionZ = -2.25f,
                SinkDepthMeters = 0.75f
            });
            var restored = new LotEditorSession();
            restored.Restore(source.Serialize());
            Assert.That(restored.Data.Flora.Count, Is.EqualTo(1));
            Assert.That(restored.Data.Flora[0].FloraId, Is.EqualTo("maple"));
            Assert.That(restored.Data.Flora[0].PositionX, Is.EqualTo(3.5f));
            Assert.That(restored.Data.Flora[0].PositionZ, Is.EqualTo(-2.25f));
            Assert.That(restored.Data.Flora[0].SinkDepthMeters,
                Is.EqualTo(0.75f));
        }

        [Test]
        public void SelectedFloraCanSinkButCannotRiseAboveGroundLevel()
        {
            var root = new GameObject("Flora Sink Depth Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.SetFloraEditorContext(true);
                var camera = root.GetComponentInChildren<Camera>();
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var pixel = camera.WorldToScreenPoint(Vector3.zero);
                var panelPoint = new Vector2(pixel.x, panelSize.y - pixel.y);

                Assert.That(world.BeginFloraDragFromPanel("maple", panelPoint,
                    panelSize), Is.True);
                Assert.That(world.EndFloraDrag(), Is.True);
                Assert.That(world.AdjustSelectedFloraSink(false), Is.False,
                    "H must not raise a grounded tree into the air.");
                Assert.That(world.AdjustSelectedFloraSink(true), Is.True);
                Assert.That(world.SelectedFloraSinkDepth, Is.EqualTo(0.25f));
                var shadow = root.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.name ==
                        "Flora Shadow — Canopy");
                var shadowProperties = new MaterialPropertyBlock();
                shadow.GetPropertyBlock(shadowProperties);
                Assert.That(shadowProperties.GetFloat("_SinkCompensation"),
                    Is.EqualTo(0.25f),
                    "Sinking the artwork must not lower its ground-shadow anchor.");
                Assert.That(world.AdjustSelectedFloraSink(false), Is.True);
                Assert.That(world.SelectedFloraSinkDepth, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
        public void NewFloraAndFenceMayBePlacedAtOccludedGroundPoint()
        {
            var root = new GameObject("Church Edge Placement Assistance Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Church Edge Assistance", LotType.Residential, 4, 4);
                Assert.That(world.PlaceBuildingAtCenter(
                    "cityforge.v3.civics.culture.new_england_church_tripo_01"),
                    Is.True);

                var camera = root.GetComponentInChildren<Camera>();
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var centerPixel = camera.WorldToScreenPoint(Vector3.zero);
                var centerPanelPoint = new Vector2(
                    centerPixel.x, panelSize.y - centerPixel.y);

                world.SetFloraEditorContext(true);
                Assert.That(world.BeginFloraDragFromPanel(
                    "maple", centerPanelPoint, panelSize), Is.True,
                    "A tree may be placed at an occluded ground point.");
                world.EndFloraDrag();
                var tree = world.Session.Data.Flora[0];
                Assert.That(world.CanPlaceFloraAt(
                    new Vector2(tree.PositionX, tree.PositionZ)), Is.True);
                Assert.That(new Vector2(tree.PositionX, tree.PositionZ).magnitude,
                    Is.LessThan(0.01f));

                world.SetPropEditorContext(true);
                world.SetPropPlacementPreview("wrought-iron-fence-straight-v01");
                Assert.That(world.BeginPropDragFromPanel(
                    "wrought-iron-fence-straight-v01", centerPanelPoint, panelSize),
                    Is.True,
                    "An ordinary prop may be placed at an occluded ground point.");
                world.EndPropDrag();
                var fence = world.Session.Data.Props[0];
                Assert.That(new Vector2(fence.PositionX, fence.PositionZ).magnitude,
                    Is.LessThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void VisibleNorthwestTreeHitBeatsChurchBillboardFallback()
        {
            var root = new GameObject("Church Northwest Tree Selection Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Church Tree Selection", LotType.Residential, 4, 4);
                Assert.That(world.PlaceBuildingAtCenter(
                    "cityforge.v3.civics.culture.new_england_church_tripo_01"),
                    Is.True);
                Assert.That(world.PlaceFloraForQa("maple", -8f, 10.5f), Is.True);
                world.SetFloraEditorContext(true);

                var camera = root.GetComponentInChildren<Camera>();
                var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
                var tree = Find(root.transform, "Flora — maple")
                    .GetComponent<SpriteRenderer>();
                var treePixel = camera.WorldToScreenPoint(tree.bounds.center);
                var treePanelPoint = new Vector2(
                    treePixel.x, panelSize.y - treePixel.y);

                Assert.That(world.UpdateObjectHoverFromPanel(
                    treePanelPoint, panelSize), Is.EqualTo(LotObjectSelectionKind.Flora),
                    "Visible tree artwork must hover as flora even inside the church billboard's broad screen bounds.");
                Assert.That(world.BeginExistingObjectManipulationFromPanel(
                    treePanelPoint, panelSize), Is.EqualTo(LotObjectSelectionKind.Flora));
                Assert.That(world.SelectedFloraIndex, Is.EqualTo(0));
                Assert.That(world.ActiveObjectSelection,
                    Is.EqualTo(LotObjectSelectionKind.Flora));
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
        public void FloraMayShareBuildingFootprintsAndUsesCameraDepthSorting()
        {
            var root = new GameObject("Flora Occupancy and Depth Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                Assert.That(world.PlaceBuildingAtCenter(
                    BuildingCatalog.ColonialGovernmentHouseId), Is.True);
                Assert.That(world.CanPlaceFloraAt(Vector2.zero), Is.True,
                    "A tree anchor may occupy a building footprint even when hidden.");

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
                var tree = cast.transform.parent.GetComponent<SpriteRenderer>();
                Assert.That(System.Array.FindAll(
                    root.GetComponentsInChildren<SpriteRenderer>(),
                    renderer => renderer.name.StartsWith("Flora Shadow")).Length,
                    Is.EqualTo(1));
                Assert.That(cast.sharedMaterial.shader.name,
                    Is.EqualTo("CityForgeV3/ProjectedFloraShadow"));
                Assert.That(cast.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(cast.transform.localRotation,
                    Is.EqualTo(Quaternion.identity));
                Assert.That(cast.sortingOrder, Is.EqualTo(tree.sortingOrder - 1));
                var properties = new MaterialPropertyBlock();
                cast.GetPropertyBlock(properties);
                Assert.That(properties.GetColor("_Color").a,
                    Is.EqualTo(0.315f).Within(0.001f));
                Assert.That(properties.GetVector("_SunRay").y, Is.LessThan(0f));
                Assert.That(properties.GetFloat("_GroundY"),
                    Is.EqualTo(0.024f).Within(0.001f));

                world.SetTimeOfDay(TimeOfDayPreset.Morning);
                cast.GetPropertyBlock(properties);
                Assert.That(properties.GetColor("_Color").a,
                    Is.EqualTo(0.14f).Within(0.001f));

                world.SetTimeOfDay(TimeOfDayPreset.Night);
                Assert.That(cast.enabled, Is.False,
                    "Night must not retain a directional tree shadow.");
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
        public void WhitePicketFenceIsAPlaceablePersistentThreeDimensionalProp()
        {
            Assert.That(Resources.Load<GameObject>(
                "CityForgeV3/Props/PicketFenceV01/CF_Prop_PicketFence_Straight_v01"),
                Is.Not.Null);
            var root = new GameObject("White Picket Fence Prop Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.Session.Data.Props.Add(new PlacedProp
                {
                    InstanceId = "picket-fence-test",
                    PropId = LotWorldController.PicketFencePropId,
                    PositionX = 0f,
                    PositionZ = 0f
                });
                world.SetInspectionMode(BuildingInspectionMode.Artwork);
                world.SetPropEditorContext(true);
                var fence = Find(root.transform,
                    $"Prop — {LotWorldController.PicketFencePropId}");
                Assert.That(fence, Is.Not.Null);
                var renderer = fence.GetComponentInChildren<MeshRenderer>();
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.bounds.size.x, Is.EqualTo(2.4f).Within(0.1f));
                Assert.That(renderer.bounds.size.y, Is.EqualTo(1.26f).Within(0.12f));
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Standard"));
                Assert.That(renderer.sharedMaterial.mainTexture, Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_BumpMap"), Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_MetallicGlossMap"), Is.Not.Null);

                var restored = new LotEditorSession();
                restored.Restore(world.Session.Serialize());
                Assert.That(restored.Data.Props[0].PropId,
                    Is.EqualTo(LotWorldController.PicketFencePropId));
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
                Assert.That(renderer.sharedMaterial.GetFloat("_GlossMapScale"),
                    Is.EqualTo(0.64f));

                var lights = lamppost.GetComponentsInChildren<Light>(true);
                Assert.That(lights.Length, Is.EqualTo(3));
                var lightPool = Find(lamppost, "CF Runtime Lantern Light Pool");
                Assert.That(lightPool, Is.Null);
                world.SetTimeOfDay(TimeOfDayPreset.Noon);
                foreach (var light in lights) Assert.That(light.enabled, Is.False);
                Assert.That(renderer.sharedMaterial.GetColor("_EmissionColor"),
                    Is.EqualTo(Color.black));
                world.SetTimeOfDay(TimeOfDayPreset.Evening);
                foreach (var light in lights) Assert.That(light.enabled, Is.True);
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
        public void SimpleStreetLamppostIsOptimizedScaledAndNightLit()
        {
            Assert.That(Resources.Load<GameObject>(
                "CityForgeV3/Props/SimpleStreetLamppostV01/SimpleStreetLamppostV01"),
                Is.Not.Null);
            var root = new GameObject("Simple Street Lamppost Prop Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.Session.Data.Props.Add(new PlacedProp
                {
                    InstanceId = "simple-street-lamp-test",
                    PropId = LotWorldController.SimpleStreetLamppostPropId,
                    PositionX = 0f,
                    PositionZ = 0f
                });
                world.SetInspectionMode(BuildingInspectionMode.Artwork);
                world.SetPropEditorContext(false);
                var lamppost = Find(root.transform,
                    $"Prop — {LotWorldController.SimpleStreetLamppostPropId}");
                Assert.That(lamppost, Is.Not.Null);
                var renderer = lamppost.GetComponentInChildren<MeshRenderer>();
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.bounds.size.y, Is.EqualTo(3.6f).Within(0.15f));
                Assert.That(renderer.sharedMaterial.mainTexture, Is.Not.Null);
                Assert.That(renderer.sharedMaterial.mainTexture.name,
                    Is.EqualTo("base-color-dark"));
                Assert.That(renderer.sharedMaterial.GetTexture("_BumpMap"), Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_MetallicGlossMap"), Is.Not.Null);
                Assert.That(renderer.sharedMaterial.GetTexture("_EmissionMap"), Is.Not.Null);
                Assert.That(renderer.sharedMaterial.color.r, Is.EqualTo(0.3f).Within(0.001f));
                Assert.That(renderer.sharedMaterial.color.g, Is.EqualTo(0.32f).Within(0.001f));
                Assert.That(renderer.sharedMaterial.color.b, Is.EqualTo(0.34f).Within(0.001f));

                var lights = lamppost.GetComponentsInChildren<Light>(true);
                Assert.That(lights.Length, Is.EqualTo(1));
                Assert.That(lights[0].type, Is.EqualTo(LightType.Point));
                Assert.That(lights[0].range, Is.EqualTo(5.5f));
                Assert.That(lights[0].intensity, Is.EqualTo(1.75f));
                Assert.That(lights[0].shadows, Is.EqualTo(LightShadows.None));
                Assert.That(lights[0].bounceIntensity, Is.EqualTo(0f));
                var lightPool = Find(lamppost, "CF Runtime Lantern Light Pool");
                Assert.That(lightPool, Is.Null);
                world.SetTimeOfDay(TimeOfDayPreset.Noon);
                Assert.That(lights[0].enabled, Is.False);
                Assert.That(renderer.sharedMaterial.GetColor("_EmissionColor"),
                    Is.EqualTo(Color.black));
                world.SetTimeOfDay(TimeOfDayPreset.Night);
                Assert.That(lights[0].enabled, Is.True);
                Assert.That(renderer.sharedMaterial.GetColor("_EmissionColor").maxColorComponent,
                    Is.GreaterThan(1f));
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
            StringAssert.Contains("Blend SrcAlpha OneMinusSrcAlpha", source,
                "Antialiased cutout edges must blend instead of becoming an opaque dark stroke.");
            StringAssert.Contains("ZWrite On", source,
                "Edge blending must preserve the established flora depth contract.");
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
            StringAssert.Contains("ZTest LEqual", source);
            StringAssert.Contains("ZTest Greater", source);
            StringAssert.Contains("_BuildingHostStencilRef", source);
            StringAssert.Contains("ReadMask 252", source);
            StringAssert.Contains("Comp Equal", source);
            StringAssert.Contains("clip(color.a - _Cutoff)", source);
            StringAssert.DoesNotContain("ZTest Always", source);
            StringAssert.DoesNotContain("Queue\"=\"Transparent", source);
        }

        [Test]
        public void CommittedBuildingPropsRespectHostPrimitiveDepth()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/AlwaysVisibleBuildingProp.shader");

            StringAssert.Contains("ZWrite On", source);
            StringAssert.Contains("ZTest [_ZTest]", source);
            StringAssert.DoesNotContain("ZTest Always", source);
            StringAssert.Contains("CompareFunction", source);
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
        public void FrontFloraUsesOnlyItsAuthoredHostFacadeOverride()
        {
            var floraShader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/LitShadowReceivingSprite.shader");
            var frontFloraShader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/FrontFacadeLitShadowReceivingSprite.shader");
            var depthShader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/BuildingDepthOccluder.shader");
            var worldSource = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var propSource = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Props.cs");

            StringAssert.Contains("ZTest [_ZTest]", floraShader);
            StringAssert.Contains("Ref 0", floraShader);
            StringAssert.Contains("WriteMask 252", floraShader);
            StringAssert.Contains("ZFail Keep", floraShader);
            StringAssert.Contains("ZTest [_ZTest]", frontFloraShader);
            StringAssert.Contains("_BuildingHostStencilRef", frontFloraShader);
            StringAssert.Contains("Ref 0", frontFloraShader);
            StringAssert.Contains("WriteMask 252", frontFloraShader);
            StringAssert.Contains("ReadMask 252", frontFloraShader);
            StringAssert.Contains("ZTest Greater", frontFloraShader);
            StringAssert.Contains("ZWrite Off", frontFloraShader);
            StringAssert.Contains("_BuildingHostStencilRef", depthShader);
            StringAssert.Contains("_BuildingHostStencilWriteMask", depthShader);
            StringAssert.Contains("Pass Replace", depthShader);
            StringAssert.Contains("FloraLitShadowReceiverMaterial", worldSource);
            StringAssert.Contains("renderQueue = 3001", worldSource);
            StringAssert.Contains("CreatePropDepthPrepass", propSource);
            StringAssert.Contains("Committed Prop Depth Prepass", propSource);
            StringAssert.Contains("renderQueue = 2435", propSource);
            StringAssert.Contains("BuildingCameraFrontToleranceMeters", worldSource);
            StringAssert.Contains("classify the closest point on the footprint", worldSource);
            StringAssert.Contains("FloraHostFrontRecoveryMaterial", worldSource);
            StringAssert.Contains("TryResolveVisibleBuildingFrontHosts", worldSource);
            StringAssert.Contains("_BuildingHostStencilRef4", worldSource);
            StringAssert.Contains("_BuildingHostStencilRef4", frontFloraShader);
            StringAssert.Contains("Cull [_Cull]", frontFloraShader);
            StringAssert.Contains("TryBuildingOcclusionStencilReference", worldSource);
            StringAssert.Contains("_BuildingHostStencilRef", worldSource);
            StringAssert.Contains("PropFrontRecoveryCameraSync", propSource);
            StringAssert.Contains("CameraClearFlags.Depth", propSource);
            StringAssert.Contains("renderer.gameObject.layer = PropFrontRecoveryLayer", propSource);
            StringAssert.DoesNotContain("FrontFacadeMeshPropRecovery", propSource);
            StringAssert.DoesNotContain("CompareFunction.Always", worldSource);
            StringAssert.Contains("material.renderQueue = 2455", propSource);
            StringAssert.DoesNotContain(
                "IsOnNearestBuildingCameraFacingSide", worldSource);
            StringAssert.DoesNotContain(
                "ApplyFrontPropPresentationPriority", propSource);
            StringAssert.Contains("_buildingPackage.ShadowDirectionOffsetDegrees", propSource);
        }

        [Test]
        public void PropMovementRefreshesBuildingFrontRecoveryAtCommit()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Props.cs");
            var endStart = source.IndexOf("public bool EndPropDrag()",
                System.StringComparison.Ordinal);
            var rotateStart = source.IndexOf("public bool RotateSelectedProp(",
                endStart, System.StringComparison.Ordinal);
            var endDrag = source.Substring(endStart, rotateStart - endStart);
            StringAssert.Contains("RebuildPropPresentations();", endDrag);

            var nudgeStart = source.IndexOf("public bool NudgeSelectedPropByScreenPixels(",
                System.StringComparison.Ordinal);
            var walkStart = source.IndexOf("public bool WalkSelectedCharacter(",
                nudgeStart, System.StringComparison.Ordinal);
            var nudge = source.Substring(nudgeStart, walkStart - nudgeStart);
            StringAssert.Contains("RebuildPropPresentations();", nudge);
        }

        [Test]
        public void BuildingFacadeStencilIdsReserveRoadAndShadowBits()
        {
            Assert.That(LotWorldController.TryBuildingOcclusionStencilReference(
                0, out var first), Is.True);
            Assert.That(first, Is.EqualTo(4));
            Assert.That(LotWorldController.TryBuildingOcclusionStencilReference(
                61, out var last), Is.True);
            Assert.That(last, Is.EqualTo(248));
            Assert.That(LotWorldController.TryBuildingOcclusionStencilReference(
                62, out _), Is.False);
            Assert.That(LotWorldController.BuildingDepthOcclusionStencilReference(
                62), Is.EqualTo(252));

            var road = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/ShadowReceivingRoadOverlay.shader");
            var reflection = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/WetStreetReflection.shader");
            var shadow = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/ProjectedBuildingMeshShadow.shader");
            StringAssert.Contains("ReadMask 1", road);
            StringAssert.Contains("WriteMask 1", road);
            StringAssert.Contains("ReadMask 253", reflection);
            StringAssert.Contains("WriteMask 0", reflection);
            StringAssert.Contains("Ref 2", shadow);
            StringAssert.Contains("ReadMask 2", shadow);
            StringAssert.Contains("WriteMask 2", shadow);
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
        public void AssetShortcutsAreNotConnectedToEditorInput()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            Assert.That(source, Does.Not.Contain("TryFindByShortcut"));
            Assert.That(source, Does.Not.Contain("entry.Shortcut"));
        }

        [Test]
        public void BuildingPropSelectionSurvivesBuildingsPanelRefresh()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "CityForgeV3/Runtime/World/LotWorldController.BuildingProps.cs"));
            Assert.That(source, Does.Contain(
                "ActiveObjectSelection == LotObjectSelectionKind.BuildingProp"));
            Assert.That(source, Does.Contain(
                "ApplyBuildingPropHover(_selectedBuildingPropPresentationIndex)"));
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
        public void DeletingMajorColumnRemovesItsContentsAndClosesTheGap()
        {
            var root = new GameObject("Delete Major Column Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Three Columns", LotType.Residential, 3, 3);
                world.Session.Data.Flora.Add(new PlacedFlora
                    { InstanceId = "west", PositionX = -10f });
                world.Session.Data.Flora.Add(new PlacedFlora
                    { InstanceId = "middle", PositionX = 0f });
                world.Session.Data.Flora.Add(new PlacedFlora
                    { InstanceId = "east", PositionX = 10f });
                world.Session.Data.OverlayTextures.Add(new PlacedOverlayTexture
                    { InstanceId = "middle-overlay", CellX = 1, CellZ = 0 });

                Assert.That(world.ShowMajorStripDeletionPreview(1, true), Is.True);
                var preview = root.transform.Find(
                    "Major Row Column Deletion Preview");
                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.localPosition.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(preview.localScale.x, Is.EqualTo(10f).Within(0.001f));
                Assert.That(preview.localScale.z, Is.EqualTo(30f).Within(0.001f));

                Assert.That(world.DeleteMajorColumn(1), Is.True);
                Assert.That(world.LotWidthCells, Is.EqualTo(2));
                Assert.That(world.LotDepthCells, Is.EqualTo(3));
                Assert.That(world.Session.Data.Flora.Count, Is.EqualTo(2));
                Assert.That(world.Session.Data.Flora[0].PositionX,
                    Is.EqualTo(-5f).Within(0.001f));
                Assert.That(world.Session.Data.Flora[1].PositionX,
                    Is.EqualTo(5f).Within(0.001f));
                Assert.That(world.Session.Data.OverlayTextures, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DeletingStripKeepsSurvivingBuildingAsTheSinglePrimaryPresentation()
        {
            var root = new GameObject("Delete Strip Building State Test");
            LogAssert.ignoreFailingMessages = true;
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.ConfigureLot("Boston Pub Shape", LotType.Residential, 4, 4);
                world.Session.AddBuilding(
                    BuildingCatalog.ColonialGovernmentHouseId, 0, 5);
                var building = world.Session.Data.Buildings[0];
                building.Attachments.Add(new PlacedBuildingProp
                {
                    ComponentId = BuildingPropCatalog.AleHouseSignId,
                    HasHostLocalPosition = true,
                    HostLocalX = 3.25f,
                    HostLocalY = 4.75f,
                    HostLocalZ = 2.1f
                });
                world.Session.Data.OverlayTextures.Add(new PlacedOverlayTexture
                    { InstanceId = "surviving-overlay", CellX = 2, CellZ = 2 });
                var instanceId = building.InstanceId;

                Assert.That(world.DeleteMajorRow(0), Is.True);

                Assert.That(world.LotWidthCells, Is.EqualTo(4));
                Assert.That(world.LotDepthCells, Is.EqualTo(3));
                Assert.That(world.BuildingCount, Is.EqualTo(1));
                Assert.That(world.SelectedBuildingIndex, Is.EqualTo(0));
                Assert.That(world.IsSelected, Is.False);
                Assert.That(world.Session.Data.BuildingId,
                    Is.EqualTo(world.Session.Data.Buildings[0].BuildingId));
                Assert.That(world.Session.Data.CellX,
                    Is.EqualTo(world.Session.Data.Buildings[0].CellX));
                Assert.That(world.Session.Data.CellZ,
                    Is.EqualTo(world.Session.Data.Buildings[0].CellZ));
                Assert.That(world.Session.Data.Buildings[0].InstanceId,
                    Is.EqualTo(instanceId));
                Assert.That(world.Session.Data.Buildings[0].Attachments.Count,
                    Is.EqualTo(1));
                Assert.That(world.Session.Data.OverlayTextures.Single().CellZ,
                    Is.EqualTo(1));
                Assert.That(world.SelectBuildingAtLotPoint(Vector2.zero), Is.True);
                Assert.That(world.SelectedBuildingIndex, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void RowAndColumnLabelsReduceTheExpectedLotDimension()
        {
            var root = new GameObject("Delete Strip Dimension Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Four By Four", LotType.Residential, 4, 4);

                Assert.That(world.DeleteMajorRow(2), Is.True);
                Assert.That(world.LotWidthCells, Is.EqualTo(4));
                Assert.That(world.LotDepthCells, Is.EqualTo(3));

                world.ConfigureLot("Two By Four", LotType.Residential, 2, 4);
                Assert.That(world.DeleteMajorColumn(0), Is.True);
                Assert.That(world.LotWidthCells, Is.EqualTo(1));
                Assert.That(world.LotDepthCells, Is.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EmptyLotRightClickOffersRowAndColumnDeletion()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath,
                "CityForgeV3/Runtime/UI/CityForgeApp.cs"));

            StringAssert.Contains("↔  DELETE ROW", source);
            StringAssert.Contains("↕  DELETE COLUMN", source);
            StringAssert.Contains("TryMajorCellFromPanel", source);
            StringAssert.Contains("LotObjectSelectionKind.None", source);
            StringAssert.Contains("() => DeleteLotStrip(cell, false)", source);
            StringAssert.Contains("() => DeleteLotStrip(cell, true)", source);
            StringAssert.Contains("lot-context-delete", source);
            StringAssert.Contains("StopImmediatePropagation", source);
            StringAssert.Contains("TrickleDown.TrickleDown", source);
            StringAssert.Contains("deleteRow.worldBound.Contains(evt.position)", source);
            StringAssert.Contains("deleteColumn.worldBound.Contains(evt.position)", source);
            StringAssert.Contains("deleteRow.schedule.Execute(deleteRow.Focus)", source);
            StringAssert.Contains("_hoveredLotStripDeleteAction", source);
            StringAssert.Contains("Input.GetMouseButtonDown(0)", source);
            StringAssert.DoesNotContain(
                "_lotContextMenu.RegisterCallback<PointerDownEvent>", source);
            StringAssert.DoesNotContain(
                "_lotContextMenu.RegisterCallback<PointerUpEvent>", source);
            StringAssert.DoesNotContain(
                "_lotContextMenu.RegisterCallback<ClickEvent>", source);
            StringAssert.Contains("RegisterCallback<PointerEnterEvent>", source);
            StringAssert.Contains("ShowMajorStripDeletionPreview", source);
            StringAssert.Contains(
                "ShowMajorStripDeletionPreview(cell.x, true)", source);
            StringAssert.Contains(
                "ShowMajorStripDeletionPreview(cell.y, false)", source);
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
            StringAssert.Contains("WarnAboutUnsavedLotChanges = false", source);
            StringAssert.Contains("if (!WarnAboutUnsavedLotChanges", source);
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
        public void LoadLotBrowserConstrainsEntriesToAnAlwaysScrollableModalBody()
        {
            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            var styles = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/UI/CityForgeV3.uss");

            StringAssert.Contains("new ScrollView(ScrollViewMode.Vertical)", app);
            StringAssert.Contains("name = \"lot-save-list\"", app);
            StringAssert.Contains(
                "verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible", app);
            StringAssert.Contains(
                "horizontalScrollerVisibility = ScrollerVisibility.Hidden", app);
            StringAssert.Contains("load-lot-modal-panel", app);
            StringAssert.Contains(".document-modal-panel.load-lot-modal-panel", styles);
            StringAssert.Contains("height: 420px", styles);
            StringAssert.Contains("contentViewport.style.overflow = Overflow.Hidden", app);
            StringAssert.Contains(".lot-save-list .unity-scroll-view__content-viewport", styles);
            StringAssert.Contains("overflow: hidden", styles);
        }

        [Test]
        public void StreetcarSourceMeshCannotCastDisconnectedOffscreenShadows()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Streetcar.cs");
            var rebuild = source.Substring(source.IndexOf(
                "private void RebuildStreetcarVehicles()",
                System.StringComparison.Ordinal));

            StringAssert.Contains("renderer.shadowCastingMode = ShadowCastingMode.Off", rebuild);
            StringAssert.Contains("renderer.receiveShadows = false", rebuild);
        }

        [Test]
        public void StreetVehicleProjectionIsDepthOccludedAndFootprintShaped()
        {
            var shader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/VehicleContactShadow.shader");

            StringAssert.Contains("ZTest LEqual", shader,
                "Vehicle shadows must disappear behind cars and buildings.");
            StringAssert.DoesNotContain("ZTest Always", shader,
                "Always-on-top shadows cut through vehicles and buildings.");
            Assert.That(StreetVehicleGroundShadow.MaximumDirectionalTailMeters,
                Is.LessThanOrEqualTo(1.65f),
                "A vehicle shadow should retain its footprint instead of becoming a long ray.");
            Assert.That(StreetVehicleGroundShadow.FootprintWidthScale,
                Is.GreaterThanOrEqualTo(1f),
                "The projection must remain at least as broad as the vehicle footprint.");
        }

        [Test]
        public void NewLotDropdownsStyleTheirVisibleInputTextAndArrow()
        {
            var app = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            var styles = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/UI/CityForgeV3.uss");

            StringAssert.Contains("formatSelectedValueCallback", app);
            StringAssert.Contains("choice}   ▼", app);
            StringAssert.Contains(
                ".cf-choice-display .unity-base-popup-field__input", styles);
            StringAssert.Contains(
                ".cf-choice-display .unity-base-popup-field__text", styles);
            StringAssert.Contains(
                ".cf-choice-display .unity-base-popup-field__arrow", styles);
            StringAssert.Contains("color: rgb(248, 241, 217)", styles);
            StringAssert.Contains(
                "-unity-background-image-tint-color: rgb(230, 188, 91)", styles);
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
        public void CirculationAndRoadEditorHelpersNeverCastWorldShadows()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var cubeFactory = source.Substring(
                source.IndexOf("private static GameObject Cube(",
                    System.StringComparison.Ordinal));

            StringAssert.Contains("bool castShadows = false", cubeFactory);
            StringAssert.Contains(
                "UnityEngine.Rendering.ShadowCastingMode.Off", cubeFactory);
            StringAssert.Contains("renderer.receiveShadows = castShadows", cubeFactory);
            StringAssert.Contains(
                "new Vector3(size, size, size), color, true", source);
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
                Assert.That(presentation.DirectionalShadow, Is.Not.Null,
                    "Moving road vehicles need a late directional street shadow.");
                var directionalRenderer = Find(
                    presentation.DirectionalShadow.transform,
                    "CF Directional Street Vehicle Shadow")
                    ?.GetComponent<Renderer>();
                Assert.That(directionalRenderer, Is.Not.Null);
                Assert.That(directionalRenderer.sharedMaterial.renderQueue,
                    Is.EqualTo(StreetVehicleGroundShadow.RenderQueue));
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
        public void RollsRoyceTestVehicleLoadsAtAuthoredRoadScale()
        {
            var package = VehicleTypePackage.LoadRollsRoyce1926();
            Assert.That(package.Id, Is.EqualTo("rolls-royce-1926"));
            Assert.That(package.Validate(), Is.Empty);
            Assert.That(package.LengthMeters, Is.EqualTo(4.7f));
            var root = new GameObject("Rolls-Royce Presentation Test");
            try
            {
                var presentation = VehicleRuntimePresentation.Create(
                    root.transform, TestVehicleModel.RollsRoyce1926);
                Assert.That(presentation.VehicleModel,
                    Is.EqualTo(TestVehicleModel.RollsRoyce1926));
                Assert.That(presentation.VisualRoot.name,
                    Is.EqualTo("1926 Rolls-Royce Visual"));
                var renderers = presentation.VisualRoot
                    .GetComponentsInChildren<Renderer>();
                Assert.That(renderers.Length, Is.GreaterThan(0));
                var bounds = renderers[0].bounds;
                for (var index = 1; index < renderers.Length; index++)
                    bounds.Encapsulate(renderers[index].bounds);
                Assert.That(Mathf.Max(bounds.size.x, bounds.size.z),
                    Is.EqualTo(package.LengthMeters).Within(0.05f));
                Assert.That(bounds.size.y, Is.InRange(1.2f, 2.4f),
                    "The imported car must be upright rather than scaled from a side-on pose.");
                var names = string.Join(", ", System.Array.ConvertAll(
                    presentation.GetComponentsInChildren<Transform>(true),
                    item => item.name));
                StringAssert.DoesNotContain("Cube", names);
                Assert.That(presentation.GetComponentsInChildren<Camera>(true), Is.Empty);
                Assert.That(presentation.GetComponentsInChildren<Light>(true), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
            Assert.That(package.RoadWidthMeters, Is.EqualTo(9.5f));
            Assert.That(package.Pieces.Count, Is.EqualTo(7));
            Assert.That(package.Validate(), Is.Empty);
            Assert.That(package.Piece(RoadPieceTopology.Straight).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.TJunction).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.TJunction).ArtworkStatus,
                Is.EqualTo("authored-2026-08-30-classic-brick"));
            Assert.That(package.Piece(RoadPieceTopology.FourWay).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.Corner).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.Endpoint).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.StraightToDiagonal).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.Diagonal).HasArtwork, Is.True);
            Assert.That(package.Piece(RoadPieceTopology.Corner).ArtworkStatus, Is.EqualTo("authored-2026-07-28"));
            Assert.That(package.Piece(RoadPieceTopology.Endpoint).ArtworkStatus,
                Is.EqualTo("authored-2026-07-28-straight-cap"));
            Assert.That(package.Piece(RoadPieceTopology.StraightToDiagonal).ArtworkStatus,
                Is.EqualTo("authored-2026-08-30-classic-brick-alternating"));
            Assert.That(package.Piece(RoadPieceTopology.Diagonal).ArtworkStatus,
                Is.EqualTo("authored-2026-08-30-classic-brick-alternating"));
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
                foreach (var topology in RoadPiecePackage.RequiredCoreTopologies)
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
                Assert.That(world.SpawnTestVehicle(
                    TestVehicleModel.RollsRoyce1926), Is.True);
                // The library is intentionally uncapped: repeated clicks are
                // allowed to build a dense traffic stress test.
                for (var index = 0; index < 10; index++)
                    Assert.That(world.SpawnTestVehicle(
                        (VehiclePaintVariant)(index % 4)), Is.True);
                Assert.That(world.TestVehicleCount, Is.EqualTo(13));
                var travelers = root.GetComponentsInChildren<VehicleRuntimePresentation>();
                Assert.That(System.Array.Exists(travelers,
                    vehicle => vehicle.gameObject.name.Contains("Red") &&
                               vehicle.PaintVariant == VehiclePaintVariant.Red), Is.True);
                Assert.That(System.Array.Exists(travelers,
                    vehicle => vehicle.gameObject.name.Contains("Blue") &&
                               vehicle.PaintVariant == VehiclePaintVariant.Blue), Is.True);
                Assert.That(System.Array.Exists(travelers,
                    vehicle => vehicle.gameObject.name.Contains("Rolls-Royce") &&
                               vehicle.VehicleModel == TestVehicleModel.RollsRoyce1926), Is.True);

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
        [TestCase(LotZoomLevel.Close, 1, LotZoomLevel.Lot)]
        [TestCase(LotZoomLevel.Lot, -1, LotZoomLevel.Close)]
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

        [TestCase(LotZoomLevel.Lot, 20, 44f)]
        [TestCase(LotZoomLevel.Lot, 30, 44f)]
        [TestCase(LotZoomLevel.Lot, 40, 44f)]
        [TestCase(LotZoomLevel.Neighborhood, 40, 220f)]
        [TestCase(LotZoomLevel.Detail, 40, 11.4375f)]
        [TestCase(LotZoomLevel.Detail, 80, 11.4375f)]
        [TestCase(LotZoomLevel.Close, 80, 22f)]
        [TestCase(LotZoomLevel.Lot, 80, 44f)]
        [TestCase(LotZoomLevel.Wide, 80, 88f)]
        [TestCase(LotZoomLevel.Neighborhood, 80, 220f)]
        public void CameraFitScalesWithExpandedLots(
            LotZoomLevel level, int lotSizeMeters, float expectedSize)
        {
            Assert.That(
                LotWorldController.OrthographicSizeForLot(level, lotSizeMeters),
                Is.EqualTo(expectedSize).Within(0.001f));
        }

        [TestCase(LotZoomLevel.Detail, true)]
        [TestCase(LotZoomLevel.Close, true)]
        [TestCase(LotZoomLevel.Lot, true)]
        [TestCase(LotZoomLevel.Wide, false)]
        [TestCase(LotZoomLevel.Far, false)]
        [TestCase(LotZoomLevel.Neighborhood, false)]
        public void ThreeDimensionalCharactersUseTheThreeClosestZoomBands(
            LotZoomLevel level, bool expected)
        {
            Assert.That(LotWorldController.ShowsThreeDimensionalCharacters(level),
                Is.EqualTo(expected));
        }

        [TestCase(false, true, true, false, 1, 1)]
        [TestCase(true, false, true, false, -1, 1)]
        [TestCase(false, true, false, true, 1, -1)]
        [TestCase(true, false, false, true, -1, -1)]
        [TestCase(false, true, false, false, 1, 0)]
        public void CharacterArrowKeysResolveToEightDirections(
            bool left, bool right, bool up, bool down, int expectedX, int expectedY)
        {
            Assert.That(CityForgeV3.UI.CityForgeApp.EightWayCharacterDirection(
                    left, right, up, down),
                Is.EqualTo(new Vector2Int(expectedX, expectedY)));
        }

        [TestCase(0f, 1f, 20f)]
        [TestCase(1f, 1f, 65f)]
        [TestCase(1f, 0f, 110f)]
        [TestCase(1f, -1f, 155f)]
        public void CharacterMovementSnapsToLotAlignedEightWayHeadings(
            float x, float y, float expectedHeading)
        {
            var direction = LotWorldController.SnapCharacterDirectionToLotAxes(
                new Vector2(x, y));
            var heading = Mathf.Repeat(
                Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 360f);
            Assert.That(heading, Is.EqualTo(expectedHeading).Within(0.01f));
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

            var floraShadow = Shader.Find("CityForgeV3/ProjectedFloraShadow");
            Assert.That(floraShadow, Is.Not.Null);
            Assert.That(floraShadow.passCount, Is.EqualTo(2),
                "Flora needs a normal ground pass and a road-stencil pass.");
            Assert.That(floraShadow.renderQueue, Is.GreaterThan(3002),
                "Projected flora must draw after opaque brick road artwork.");
        }

        [Test]
        public void BuildingConstructionCreatesDirtAndAdvancesOneStoryAtATime()
        {
            var root = new GameObject("Construction Sequence Test");
            var finished = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finished.transform.SetParent(root.transform, false);
            finished.name = "Finished Building";
            finished.transform.localScale = new Vector3(8f, 9.6f, 6f);
            try
            {
                var sequence = root.AddComponent<BuildingConstructionSequence>();
                sequence.Begin(finished, 8f, 6f, 9.6f);
                Assert.That(finished.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(Find(root.transform, "Excavated Dirt Footprint"),
                    Is.Not.Null);
                Assert.That(sequence.StoryCount, Is.EqualTo(3));
                Assert.That(sequence.CompletedStories, Is.Zero);

                sequence.AdvanceOneStageForQa();
                Assert.That(sequence.CompletedStories, Is.EqualTo(1));
                Assert.That(sequence.RevealedBuildingStories, Is.Zero,
                    "The frame must lead the finished facade by one floor.");
                Assert.That(Find(root.transform,
                    "Front Diagonal 1"), Is.Not.Null,
                    "Simulate Build should reveal the shared frame one story at a time.");

                sequence.AdvanceOneStageForQa();
                Assert.That(sequence.RevealedBuildingStories, Is.EqualTo(1));
                Assert.That(Find(root.transform,
                    "Front Panel Wall — Story 1 Bay 1 Sill"), Is.Not.Null,
                    "The preceding story should gain panel walls with real window gaps.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuildingShaderSupportsFloorByFloorConstructionReveal()
        {
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_ConstructionRevealHeight"),
                    Is.True,
                    "A combined building mesh needs a world-height reveal mask.");
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void BuildingShaderExposesDaytimeColorGradeControls()
        {
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_Contrast"), Is.True);
                Assert.That(material.HasProperty("_Saturation"), Is.True);
                Assert.That(material.HasProperty("_Vibrance"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ConstructionFrameMatchesFootprintAndFullBuildingHeight()
        {
            var root = new GameObject("Construction Frame Test");
            try
            {
                var frame = root.AddComponent<BuildingConstructionFramePreview>();
                frame.Build(8f, 6f, 9.6f);
                var frontRail = Find(root.transform, "Front Story Rail 3");
                Assert.That(frontRail, Is.Not.Null,
                    "A three-story structure needs a full-height top rail.");
                Assert.That(frontRail.localPosition.y,
                    Is.EqualTo(9.64f).Within(0.01f));
                Assert.That(frontRail.localScale.x,
                    Is.EqualTo(6.4f).Within(0.01f),
                    "Construction footprints should be 20% narrower than the mesh bounds.");
                Assert.That(Find(root.transform, "Front Full-Height Post 1"),
                    Is.Not.Null);
                Assert.That(Find(root.transform, "Front Diagonal 3"),
                    Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ConstructionPreviewCancelsPackageScaleAndUsesVisibleCentre()
        {
            var root = new GameObject("Scaled Building Root");
            root.transform.position = new Vector3(5f, 0f, -4f);
            root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            root.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            try
            {
                var originMethod = typeof(LotWorldController).GetMethod(
                    "ConstructionLocalOrigin",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var scaleMethod = typeof(LotWorldController).GetMethod(
                    "ConstructionScaleCompensation",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var bounds = new Bounds(new Vector3(7f, 6f, -1f),
                    new Vector3(10f, 12f, 8f));
                var origin = (Vector3)originMethod.Invoke(null,
                    new object[] { root.transform, bounds });
                var compensation = (Vector3)scaleMethod.Invoke(null,
                    new object[] { root.transform });
                var preview = new GameObject("Preview").transform;
                preview.SetParent(root.transform, false);
                preview.localPosition = origin;
                preview.localScale = compensation;

                Assert.That(preview.position.x,
                    Is.EqualTo(bounds.center.x).Within(0.001f));
                Assert.That(preview.position.y,
                    Is.EqualTo(bounds.min.y).Within(0.001f));
                Assert.That(preview.position.z,
                    Is.EqualTo(bounds.center.z).Within(0.001f));
                Assert.That(preview.lossyScale.x, Is.EqualTo(1f).Within(0.001f));
                Assert.That(preview.lossyScale.y, Is.EqualTo(1f).Within(0.001f));
                Assert.That(preview.lossyScale.z, Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
                out var topology, out var turns), Is.True);
            Assert.That(topology, Is.EqualTo(RoadPieceTopology.Endpoint));
            Assert.That(turns, Is.EqualTo(0));

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
        public void BrickRoadStraightTransitionAndDiagonalFormAValidChain()
        {
            var package = RoadPiecePackage.Load();
            var transition = package.Piece(RoadPieceTopology.StraightToDiagonal);
            var diagonal = package.Piece(RoadPieceTopology.Diagonal);
            Assert.That(transition.RotatedPorts(1),
                Is.EquivalentTo(new[] { RoadPiecePort.East, RoadPiecePort.South }));
            Assert.That(diagonal.RotatedPorts(1),
                Is.EquivalentTo(new[] { RoadPiecePort.North, RoadPiecePort.West }));

            var pieces = new System.Collections.Generic.List<PlacedRoadPiece>
            {
                new PlacedRoadPiece
                {
                    Id = "straight",
                    PackageId = package.Id,
                    Topology = RoadPieceTopology.Straight,
                    GridX = 0,
                    GridZ = 1
                },
                new PlacedRoadPiece
                {
                    Id = "transition",
                    PackageId = package.Id,
                    Topology = RoadPieceTopology.StraightToDiagonal,
                    GridX = 0,
                    GridZ = 0
                },
                new PlacedRoadPiece
                {
                    Id = "diagonal",
                    PackageId = package.Id,
                    Topology = RoadPieceTopology.Diagonal,
                    GridX = 1,
                    GridZ = 0
                },
                new PlacedRoadPiece
                {
                    Id = "diagonal-alternate",
                    PackageId = package.Id,
                    Topology = RoadPieceTopology.Diagonal,
                    GridX = 1,
                    GridZ = -1,
                    RotationQuarterTurns = 2
                }
            };

            Assert.That(RoadPlacementModel.Validate(pieces, package, 40), Is.Empty);
            Assert.That(RoadPlacementModel.ResolveAlternatingDiagonalRotation(
                pieces, 1, 0, package, 0), Is.EqualTo(0));
            Assert.That(RoadPlacementModel.ResolveAlternatingDiagonalRotation(
                pieces, 1, -1, package, 0), Is.EqualTo(2));
            var network = RoadPlacementModel.BuildVehicleNetwork(pieces, package, 40);
            Assert.That(network.Nodes.Count, Is.EqualTo(6));
            Assert.That(network.Segments.Count, Is.EqualTo(5));
        }

        [Test]
        public void PlannedRoadRouteIsContinuousAndReachesItsEndpoint()
        {
            var route = RoadPlacementModel.BuildPlannedRoadRoute(
                new Vector2Int(0, 0), new Vector2Int(3, 3));

            Assert.That(route.Count, Is.EqualTo(7));
            Assert.That(route[0], Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(route[^1], Is.EqualTo(new Vector2Int(3, 3)));
            for (var index = 1; index < route.Count; index++)
            {
                var delta = route[index] - route[index - 1];
                Assert.That(Mathf.Abs(delta.x) + Mathf.Abs(delta.y), Is.EqualTo(1));
            }
        }

        [Test]
        public void PlannedRoadRouteResolvesEndpointsTurnsAndStraightRuns()
        {
            var package = RoadPiecePackage.Load();
            var staircase = RoadPlacementModel.BuildPlannedRoadRoute(
                new Vector2Int(0, 0), new Vector2Int(2, 2));

            Assert.That(RoadPlacementModel.TryResolvePlannedRoutePiece(
                staircase, 0, package, out var startTopology, out _), Is.True);
            Assert.That(startTopology, Is.EqualTo(RoadPieceTopology.Diagonal));
            Assert.That(RoadPlacementModel.TryResolvePlannedRoutePiece(
                staircase, 1, package, out var turnTopology, out var turnRotation), Is.True);
            Assert.That(turnTopology,
                Is.EqualTo(RoadPieceTopology.StraightToDiagonal));
            Assert.That(turnRotation, Is.EqualTo(0));
            Assert.That(RoadPlacementModel.TryResolvePlannedRoutePiece(
                staircase, 2, package, out var alternateTurnTopology, out _), Is.True);
            Assert.That(alternateTurnTopology,
                Is.EqualTo(RoadPieceTopology.Diagonal));
            Assert.That(RoadPlacementModel.TryResolveComplementaryDiagonalFiller(
                staircase, 0, out var complementaryCell, out var complementaryRotation), Is.True);
            Assert.That(complementaryCell, Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(complementaryRotation, Is.EqualTo(2));

            var descending = RoadPlacementModel.BuildPlannedRoadRoute(
                new Vector2Int(0, 2), new Vector2Int(2, 0));
            Assert.That(RoadPlacementModel.TryResolvePlannedRoutePiece(
                descending, 1, package, out _, out var descendingTurnRotation), Is.True);
            Assert.That(descendingTurnRotation, Is.EqualTo(3));
            Assert.That(RoadPlacementModel.TryResolveComplementaryDiagonalFiller(
                descending, 0, out var descendingComplement, out var descendingComplementRotation),
                Is.True);
            Assert.That(descendingComplement, Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(descendingComplementRotation, Is.EqualTo(1));

            Assert.That(RoadPlacementModel.TryResolveDiagonalTransition(
                RoadPiecePort.NorthEast, RoadPiecePort.South, package,
                out var rightTransition, out var rightTransitionRotation), Is.True);
            Assert.That(rightTransition, Is.EqualTo(RoadPieceTopology.DiagonalTransitionRight));
            Assert.That(rightTransitionRotation, Is.EqualTo(0));
            Assert.That(RoadPlacementModel.TryResolveDiagonalTransition(
                RoadPiecePort.NorthEast, RoadPiecePort.West, package,
                out var leftTransition, out var leftTransitionRotation), Is.True);
            Assert.That(leftTransition, Is.EqualTo(RoadPieceTopology.DiagonalTransitionLeft));
            Assert.That(leftTransitionRotation, Is.EqualTo(0));
            Assert.That(RoadPlacementModel.CardinalApproachForDiagonal(
                RoadPiecePort.SouthWest, true), Is.EqualTo(RoadPiecePort.East));
            Assert.That(RoadPlacementModel.CardinalApproachForDiagonal(
                RoadPiecePort.SouthWest, false), Is.EqualTo(RoadPiecePort.North));
            Assert.That(RoadPlacementModel.TryResolveTJunction(
                new[] { RoadPiecePort.East, RoadPiecePort.West },
                RoadPiecePort.South, package, out var tTurns), Is.True);
            Assert.That(tTurns, Is.EqualTo(0));
            Assert.That(RoadPlacementModel.TryResolveTJunction(
                new[] { RoadPiecePort.North, RoadPiecePort.South },
                RoadPiecePort.West, package, out var rotatedTTurns), Is.True);
            Assert.That(rotatedTTurns, Is.EqualTo(1));
            Assert.That(RoadPlacementModel.OppositeCardinalPort(
                RoadPiecePort.North), Is.EqualTo(RoadPiecePort.South));
            Assert.That(RoadPlacementModel.TryResolveDiagonalTJunction(
                RoadPiecePort.NorthEast,
                new[] { RoadPiecePort.North, RoadPiecePort.South }, package,
                out var diagonalTJunction, out var diagonalTTurns), Is.True);
            Assert.That(diagonalTJunction,
                Is.EqualTo(RoadPieceTopology.DiagonalTJunctionRight));
            Assert.That(diagonalTTurns, Is.EqualTo(0));
            Assert.That(RoadPlacementModel.TryResolveDiagonalTJunction(
                RoadPiecePort.SouthWest,
                new[] { RoadPiecePort.East, RoadPiecePort.West }, package,
                out _, out _), Is.True);

            var straight = RoadPlacementModel.BuildPlannedRoadRoute(
                new Vector2Int(0, 0), new Vector2Int(3, 0));
            Assert.That(RoadPlacementModel.TryResolvePlannedRoutePiece(
                straight, 1, package, out var straightTopology, out _), Is.True);
            Assert.That(straightTopology, Is.EqualTo(RoadPieceTopology.Straight));
        }

        [Test]
        public void RoadPlannerUsesRoadOnlySKeyStartPreviewAndClickCommit()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath,
                "CityForgeV3/Runtime/UI/CityForgeApp.cs"));
            StringAssert.Contains("evt.keyCode == KeyCode.S", source);
            StringAssert.Contains("_lotEditorCategory == LotEditorCategory.Roads", source);
            StringAssert.Contains("BeginRoadRoutePlan", source);
            StringAssert.Contains("UpdateRoadRoutePlanPreviewFromPanel", source);
            StringAssert.Contains("CommitRoadRoutePlanFromPanel", source);
            StringAssert.DoesNotContain("KeyCode.Backslash", source);
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
        public void LushGrassHasFourSeasonArtworkAndDeterministicFallbacks()
        {
            var lush = LotWorldController.ResolveBaseTexture("grass-lush");
            Assert.That(lush, Is.Not.Null);
            foreach (var season in new[]
                     {
                         SeasonPreset.Spring, SeasonPreset.Summer,
                         SeasonPreset.Autumn, SeasonPreset.Winter
                     })
            {
                Assert.That(lush.HasResourceForSeason(season), Is.True);
                Assert.That(Resources.Load<Texture2D>(lush.ResolveResourcePath(season)),
                    Is.Not.Null, $"Missing lush texture for {season}");
            }

            var summerOnly = new LotWorldController.LotTextureOption(
                "summer-only", "Summer Only", "legacy/path",
                summerResourcePath: "season/summer");
            Assert.That(summerOnly.ResolveResourcePath(SeasonPreset.Winter),
                Is.EqualTo("season/summer"));

            var legacyOnly = new LotWorldController.LotTextureOption(
                "legacy", "Legacy", "legacy/path");
            Assert.That(legacyOnly.ResolveResourcePath(SeasonPreset.Autumn),
                Is.EqualTo("legacy/path"));
        }

        [Test]
        public void AsheAndMapleUseWinterArtworkWithSummerFallback()
        {
            foreach (var floraId in new[] { "ashe", "maple", "oak" })
            {
                var winterPath = LotWorldController.ResolveFloraResourcePath(
                    floraId, SeasonPreset.Winter);
                Assert.That(winterPath, Does.EndWith("-winter"));
                Assert.That(Resources.Load<Texture2D>(winterPath), Is.Not.Null);

                var autumnPath = LotWorldController.ResolveFloraResourcePath(
                    floraId, SeasonPreset.Autumn);
                Assert.That(autumnPath, Does.EndWith("-summer"),
                    "Missing seasonal tree art should use the approved summer sprite.");
            }
        }

        [Test]
        public void WinterSeasonRefreshesActiveFloraPreviewAndStrengthensItsShadow()
        {
            var root = new GameObject("Winter Flora Presentation Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                world.ConfigureLot("Winter Flora", LotType.Residential, 4, 4);
                world.SetFloraPlacementPreview("ashe");

                var previewField = typeof(LotWorldController).GetField(
                    "_floraPreview", BindingFlags.Instance | BindingFlags.NonPublic);
                var preview = (SpriteRenderer)previewField.GetValue(world);
                Assert.That(preview.sprite.texture.name, Does.EndWith("-summer"));

                world.SetSeason(SeasonPreset.Winter);
                Assert.That(preview.sprite.texture.name, Does.EndWith("-winter"),
                    "The cursor and planted tree must resolve the same seasonal art.");

                Assert.That(world.PlaceFloraForQa("ashe", -15f, -15f), Is.True);
                var shadow = root.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.name == "Flora Shadow — Canopy");
                Assert.That(shadow.sprite.texture.name, Does.EndWith("-winter"));
                var properties = new MaterialPropertyBlock();
                shadow.GetPropertyBlock(properties);
                Assert.That(properties.GetColor("_Color").a,
                    Is.GreaterThan(0.18f),
                    "Sparse winter branches need a legible projected shadow.");

                world.SetTimeOfDay(TimeOfDayPreset.Afternoon);
                var afternoonRay = Quaternion.Euler(0f,
                    world.BuildingShadowDirectionOffsetDegrees, 0f) *
                    (TimeOfDayLighting.SunRotation(TimeOfDayPreset.Afternoon) *
                     Vector3.forward);
                shadow.GetPropertyBlock(properties);
                Assert.That(Vector3.Angle(
                        properties.GetVector("_SunRay"), afternoonRay),
                    Is.LessThan(0.001f),
                    "Afternoon winter trees must share the building shadow ray.");
                Assert.That(properties.GetFloat("_ProjectionScale"),
                    Is.GreaterThan(0f));

                world.SetTimeOfDay(TimeOfDayPreset.Morning);
                var morningRay = Quaternion.Euler(0f,
                    world.BuildingShadowDirectionOffsetDegrees, 0f) *
                    (TimeOfDayLighting.SunRotation(TimeOfDayPreset.Morning) *
                     Vector3.forward);
                shadow.GetPropertyBlock(properties);
                Assert.That(Vector3.Angle(
                        properties.GetVector("_SunRay"), morningRay),
                    Is.LessThan(0.001f),
                    "Morning winter trees must share the building shadow ray.");
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
            Assert.That(LotWorldController.ShadowLengthScale(TimeOfDayPreset.Noon), Is.EqualTo(0.65f));
            Assert.That(LotWorldController.ShadowLengthScale(TimeOfDayPreset.Afternoon), Is.EqualTo(0.50f));
            Assert.That(LotWorldController.ShadowLengthScale(TimeOfDayPreset.Evening), Is.EqualTo(0.32f));
            Assert.That(LotWorldController.BuildingShadowLengthScale(TimeOfDayPreset.Morning), Is.EqualTo(0.90f));
            Assert.That(LotWorldController.BuildingShadowLengthScale(TimeOfDayPreset.Noon), Is.EqualTo(0.65f));
            Assert.That(LotWorldController.BuildingShadowLengthScale(TimeOfDayPreset.Afternoon), Is.EqualTo(1.15f));
            Assert.That(LotWorldController.BuildingShadowLengthScale(TimeOfDayPreset.Evening), Is.EqualTo(0.40f));
            Assert.That(
                LotWorldController.BuildingShadowOpacityMultiplier(
                    TimeOfDayPreset.Morning),
                Is.EqualTo(LotWorldController.PropShadowOpacityMultiplier(
                    TimeOfDayPreset.Morning) * 1.8125f));
            Assert.That(
                LotWorldController.BuildingShadowOpacityMultiplier(
                    TimeOfDayPreset.Afternoon),
                Is.EqualTo(LotWorldController.PropShadowOpacityMultiplier(
                    TimeOfDayPreset.Afternoon) * 3.0f));
            foreach (var preset in new[]
                     {
                         TimeOfDayPreset.Noon,
                         TimeOfDayPreset.Evening
                     })
                Assert.That(
                    LotWorldController.BuildingShadowOpacityMultiplier(preset),
                    Is.EqualTo(LotWorldController.PropShadowOpacityMultiplier(preset) * 1.45f),
                    $"Building shadows must retain the calibrated baseline at {preset}.");
        }

        [Test]
        public void NeighboringBuildingsCreateAPrimitiveDerivedMorningGapShadow()
        {
            Assert.That(
                LotWorldController.BuildingGapShadowColor(
                    TimeOfDayPreset.Morning).a,
                Is.EqualTo(0.34f));
            Assert.That(
                LotWorldController.BuildingGapShadowColor(
                    TimeOfDayPreset.Noon).a,
                Is.LessThan(0.34f));
            Assert.That(
                LotWorldController.BuildingGapShadowColor(
                    TimeOfDayPreset.Night).a,
                Is.Zero);

            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            StringAssert.Contains("package.DepthMeters : package.WidthMeters", source);
            StringAssert.Contains("package.WidthMeters : package.DepthMeters", source);
            StringAssert.Contains("gapMax - gapMin <= 3.5f", source);
            StringAssert.Contains("Primitive Neighbor Gap Shadow", source);
            StringAssert.Contains("UpdateBuildingGapShadowAppearance();", source);

            var root = new GameObject("Neighbor Building Gap Shadow Test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.Build();
                typeof(LotWorldController).GetMethod(
                        "CreateBuildingGapShadow",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(world, new object[]
                    {
                        new Vector2(-1f, -4f), new Vector2(1f, 4f)
                    });
                world.SetTimeOfDay(TimeOfDayPreset.Morning);

                var gapShadows = root.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(renderer => renderer.gameObject.name ==
                        "Primitive Neighbor Gap Shadow").ToArray();
                Assert.That(gapShadows.Length, Is.EqualTo(1));
                Assert.That(gapShadows[0].sharedMaterial.color.a,
                    Is.EqualTo(0.34f));
                Assert.That(gapShadows[0].shadowCastingMode,
                    Is.EqualTo(ShadowCastingMode.Off));
                Assert.That(gapShadows[0].receiveShadows, Is.False);

                world.SetTimeOfDay(TimeOfDayPreset.Night);
                Assert.That(gapShadows[0].enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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

            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            StringAssert.Contains("0.252f", source,
                "Noon flora shadows should be 40% darker than the 0.18 baseline.");
            Assert.That(LotWorldController.BuildingGapShadowColor(
                TimeOfDayPreset.Noon).a, Is.EqualTo(0.28f));
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

        [Test]
        public void OakVariation_IsStableAndUsesAllFourProfiles()
        {
            var profiles = Enumerable.Range(0, 128)
                .Select(index => LotWorldController.StableFloraVariationProfile(
                    $"saved-oak-{index}"))
                .ToArray();

            Assert.That(profiles, Is.EqualTo(Enumerable.Range(0, 128)
                .Select(index => LotWorldController.StableFloraVariationProfile(
                    $"saved-oak-{index}"))));
            Assert.That(profiles.Distinct().OrderBy(value => value),
                Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [TestCase(0, "oak")]
        [TestCase(1, "oak")]
        [TestCase(2, "oak-b")]
        [TestCase(3, "oak-b")]
        public void OakVariation_ChoosesMatchingSpeedTreeFamily(
            int profile, string expectedFloraId)
        {
            Assert.That(LotWorldController.ResolveFloraPresentationId(
                "oak", profile), Is.EqualTo(expectedFloraId));
            Assert.That(Resources.Load<Texture2D>(
                LotWorldController.ResolveFloraResourcePath(
                    expectedFloraId, SeasonPreset.Summer)), Is.Not.Null);
        }

        [TestCase(0, SeasonPreset.Winter, "evergreen-snow")]
        [TestCase(1, SeasonPreset.Winter, "evergreen-b-snow")]
        [TestCase(2, SeasonPreset.Winter, "evergreen-snow")]
        [TestCase(3, SeasonPreset.Summer, "evergreen-b")]
        [TestCase(3, SeasonPreset.Winter, "evergreen-b-snow")]
        public void EvergreenVariation_UsesSnowOnSomeWinterTrees(
            int profile, SeasonPreset season, string expectedFloraId)
        {
            Assert.That(LotWorldController.ResolveFloraPresentationId(
                "evergreen", profile, season), Is.EqualTo(expectedFloraId));
            Assert.That(Resources.Load<Texture2D>(
                LotWorldController.ResolveFloraResourcePath(
                    expectedFloraId, season)), Is.Not.Null);
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
