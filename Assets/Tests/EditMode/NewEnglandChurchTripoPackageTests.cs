using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class NewEnglandChurchTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/NewEnglandChurchTripoV01/building-package";

        [Test]
        public void ChurchUsesCivicsCultureAndRegisteredSpatialContract()
        {
            var package = HybridBuildingPackageRegistry.Load(PackagePath);

            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.civics.culture.new_england_church_tripo_01"));
            Assert.That(package.Category, Is.EqualTo("Civics"));
            Assert.That(package.Subcategory, Is.EqualTo("Culture"));
            Assert.That(package.SizeClass, Is.EqualTo("civic"));
            Assert.That(package.OccupancyWidth, Is.EqualTo(2));
            Assert.That(package.OccupancyDepth, Is.EqualTo(2));
            Assert.That(package.PlacementScale, Is.EqualTo(1f));
            Assert.That(package.RoofRidgeAxis, Is.EqualTo("z"));
            Assert.That(package.UsesMeshProjectedShadow, Is.True);
            Assert.That(package.UsesPersistedArtworkPivot, Is.True);
            Assert.That(package.RotationAnchor, Is.EqualTo(Vector3.zero));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            CollectionAssert.Contains(package.RequiredPrimitiveObjects,
                "CF_PROXY_BUILDING_GENERATED");

            var entry = BuildingCatalog.Find(package.Id);
            Assert.That(BuildingCatalog.UseCategoryFor(entry),
                Is.EqualTo(BuildingUseCategory.Civics));
            Assert.That(entry.Subcategory, Is.EqualTo("Culture"));
            CollectionAssert.Contains(
                BuildingCatalog.SubcategoriesFor(BuildingUseCategory.Civics),
                "Culture");
            CollectionAssert.Contains(
                BuildingCatalog.ForUseCategory(
                    BuildingUseCategory.Civics, "Culture"), entry);

            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                Assert.That(Resources.Load<Texture2D>(facing.NeutralResourcePath),
                    Is.Not.Null, facing.Id);
                Assert.That(Resources.Load<Texture2D>(facing.NightOverlayResourcePath),
                    Is.Not.Null, facing.Id);
                Assert.That(facing.SourcePivotTopOrigin.x,
                    Is.EqualTo(0.5f).Within(0.0001f), facing.Id);
                Assert.That(facing.SourcePivotTopOrigin.y,
                    Is.EqualTo(0.815586f).Within(0.0001f), facing.Id);
            }

            Assert.That(package.Facing(4).Id, Is.EqualTo(package.Facing(0).Id));
        }
    }
}
