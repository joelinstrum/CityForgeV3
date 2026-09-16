using System.IO;
using NUnit.Framework;
using CityForgeV3.World;
using UnityEngine;
using System.Linq;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class DeterministicSelectionTests
    {
        [Test]
        public void SelectedObjectBlocksCompetingHoverDiscovery()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var start = source.IndexOf(
                "public LotObjectSelectionKind UpdateObjectHoverFromPanel(",
                System.StringComparison.Ordinal);
            var end = source.IndexOf("public void ClearObjectHover()", start,
                System.StringComparison.Ordinal);
            var method = source.Substring(start, end - start);

            StringAssert.Contains(
                "ActiveObjectSelection != LotObjectSelectionKind.None", method);
            StringAssert.Contains("return ActiveObjectSelection;", method);
        }

        [Test]
        public void BuildingClicksAreNotBlockedByThePreviousSelection()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            var call = source.IndexOf("_lotWorld.BeginBuilding3DDragFromPanel(",
                System.StringComparison.Ordinal);
            var start = source.LastIndexOf("if (evt.button == 0 && !ShouldPrioritizeToolPlacement(",
                call, System.StringComparison.Ordinal);
            var guard = source.Substring(start, call - start);
            StringAssert.DoesNotContain("ActiveObjectSelection", guard);
            StringAssert.Contains("ShouldPrioritizeToolPlacement", guard);
        }

        [Test]
        public void ExistingObjectHitTestPrioritizesTheSelectedFloraOrProp()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.cs");
            var start = source.IndexOf(
                "public LotObjectSelectionKind BeginExistingObjectManipulationFromPanel(",
                System.StringComparison.Ordinal);
            var end = source.IndexOf(
                "public LotObjectSelectionKind UpdateObjectHoverFromPanel(", start,
                System.StringComparison.Ordinal);
            var method = source.Substring(start, end - start);

            StringAssert.Contains("floraIndex == SelectedFloraIndex", method);
            StringAssert.Contains("propIndex == SelectedPropIndex", method);
            StringAssert.Contains("maySelectBuildingProp", method);
        }

        [Test]
        public void BarnAndNativeBuildingCanBeSelectedInSuccessionWithoutEscape()
        {
            var owner = new GameObject("Click selection fixture");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Click selection", LotType.Residential, 6, 6);
                world.AddExperimentalBuilding3D("new-england-farmhouse-v02", 12, 12, 0);
                world.PlacePropForQa(LotWorldController.NewEnglandBarnPropId, -12, -12);
                world.DeselectBuilding3D();
                world.SetPropEditorContext(true);
                var camera = owner.GetComponentInChildren<Camera>();
                var size = new Vector2(camera.pixelWidth, camera.pixelHeight);
                Vector2 Panel(Vector3 point)
                {
                    var pixel = camera.WorldToScreenPoint(point);
                    return new Vector2(pixel.x, size.y - pixel.y);
                }
                var barn = owner.GetComponentsInChildren<Transform>().First(x => x.name == "New England Barn Model");
                var barnPoint = Panel(barn.GetComponentInChildren<Renderer>().bounds.center);
                Assert.IsTrue(world.BeginPropDragFromPanel("", barnPoint, size));
                world.EndPropDrag();
                Assert.AreEqual(LotObjectSelectionKind.Prop, world.ActiveObjectSelection);
                Assert.IsTrue(world.BeginBuilding3DDragFromPanel(Panel(new Vector3(12, 4, 12)), size));
                world.EndBuilding3DDrag();
                Assert.AreEqual(0, world.SelectedBuilding3DIndex);
                Assert.AreEqual(-1, world.SelectedPropIndex);
                var highlight = owner.GetComponentsInChildren<Transform>(true).First(x => x.name == "Selected Prop Highlight");
                Assert.IsFalse(highlight.gameObject.activeSelf);
                // The viewport releases the native selection before the shared
                // prop hit path, without invoking DeselectAll/Escape.
                world.DeselectBuilding3D();
                Assert.AreEqual(LotObjectSelectionKind.Prop,
                    world.BeginExistingObjectManipulationFromPanel(barnPoint, size));
                world.EndPropDrag();
                Assert.AreEqual(0, world.SelectedPropIndex);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void BuildingSelectionUsesMeshWithoutBoundsOrFooterFallback()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Buildings3D.cs");
            StringAssert.Contains("BuildMeshSelectionSilhouette", source);
            StringAssert.Contains("BuildingSelectionGeometryRoot", source);
            StringAssert.Contains("IsBuildingSelectionBeautyRenderer", source);
            StringAssert.Contains("TryRaycastBuildingBeautyMesh", source);
            StringAssert.Contains("Representations/LOD0", source);
            StringAssert.DoesNotContain("bounds.IntersectRay(ray", source);
            StringAssert.DoesNotContain("BuildGroundFootprintSelectionOutline", source);
            var shader = File.ReadAllText(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/MeshSelectionOutline.shader");
            StringAssert.Contains("Cull Front", shader);
            StringAssert.Contains("_OutlineWidth", shader);
        }

        [Test]
        public void BuildingPropsAndEffectsUseTheSameBeautyMeshHitTest()
        {
            var buildingProps = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.BuildingProps.cs");
            var effects = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Effects.cs");
            StringAssert.Contains("TryRaycastBuildingBeautyMesh", buildingProps);
            StringAssert.Contains("TryRaycastBuildingBeautyMesh", effects);
        }
    }
}
