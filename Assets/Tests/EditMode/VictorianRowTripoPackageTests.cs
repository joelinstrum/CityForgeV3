using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class VictorianRowTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/VictorianRowTripoV01/building-package";

        [Test]
        public void ReviewPackageLoadsInResidentialMidWealthCatalog()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);

            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.residential.victorian_row_tripo_01"));
            Assert.That(package.ShortDisplayName, Is.EqualTo("Victorian Row"));
            Assert.That(package.Category, Is.EqualTo("Residential"));
            Assert.That(package.Subcategory, Is.EqualTo("Mid-Wealth"));
            Assert.That(package.ReviewStatus, Does.Contain("REVIEW"));
            Assert.That(package.PlacementScale, Is.EqualTo(1f));
            Assert.That(package.OccupancyWidth, Is.EqualTo(1));
            Assert.That(package.WidthMeters, Is.EqualTo(20.5674f).Within(0.001f));
            Assert.That(package.DepthMeters, Is.EqualTo(8.2782f).Within(0.001f));
            Assert.That(package.HeightMeters, Is.EqualTo(11.7726f).Within(0.001f));
            Assert.That(package.PixelsPerMeter, Is.EqualTo(34.761905f).Within(0.0001f));
            Assert.That(package.UsesPersistedArtworkPivot, Is.True);
            Assert.That(package.FacingCount, Is.EqualTo(4));

            var catalogEntry = BuildingCatalog.ForUseCategory(
                    BuildingUseCategory.Residential, "Mid-Wealth")
                .Single(entry => entry.Id == package.Id);
            Assert.That(catalogEntry.ShortName, Is.EqualTo("Victorian Row"));
            Assert.That(catalogEntry.PackageResourcePath, Is.EqualTo(PackagePath));

            Assert.That(Resources.Load<GameObject>(package.PrimitiveResourcePath),
                Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(package.PlanResourcePath),
                Is.Not.Null);
            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                Assert.That(Resources.Load<Texture2D>(facing.NeutralResourcePath),
                    Is.Not.Null, $"Missing neutral artwork for {facing.Id}");
                Assert.That(Resources.Load<Texture2D>(facing.NightOverlayResourcePath),
                    Is.Not.Null, $"Missing review night artwork for {facing.Id}");
            }
        }
    }
}
