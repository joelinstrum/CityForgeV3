using System.IO;
using NUnit.Framework;

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
        public void BuildingHitPriorityIsLimitedToNoSelectionOrBuildingSelection()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
            var call = source.IndexOf("_lotWorld.BeginBuilding3DDragFromPanel(",
                System.StringComparison.Ordinal);
            var guard = source.Substring(call - 700, 700);

            StringAssert.Contains(
                "LotObjectSelectionKind.None", guard);
            StringAssert.Contains(
                "LotObjectSelectionKind.Building", guard);
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
