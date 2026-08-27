using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class BrooklynTownhouseRowTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/BrooklynTownhouseRowTripoV01/building-package";

        [Test]
        public void TreeCleanedPackageLoadsInResidentialCatalog()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.residential.brooklyn_townhouse_row_tripo_01"));
            Assert.That(package.ShortDisplayName, Is.EqualTo("Brooklyn Row"));
            Assert.That(package.Category, Is.EqualTo("Residential"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.WidthMeters, Is.EqualTo(21.16144f).Within(0.001f));
            Assert.That(package.DepthMeters, Is.EqualTo(8.635713f).Within(0.001f));
            Assert.That(package.HeightMeters, Is.EqualTo(11.429688f).Within(0.001f));
            Assert.That(package.OccupancyWidth, Is.EqualTo(3));
            Assert.That(package.OccupancyDepth, Is.EqualTo(1));
            Assert.That(BuildingCatalog.ForUseCategory(
                    BuildingUseCategory.Residential, null)
                .Any(entry => entry.Id == package.Id), Is.True);
            Assert.That(Resources.Load<GameObject>(package.PrimitiveResourcePath),
                Is.Not.Null);
        }
    }
}
