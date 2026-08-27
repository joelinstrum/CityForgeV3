using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class GildedAgeMansionTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/GildedAgeMansionTripoV01/building-package";

        [Test]
        public void ReviewPackageLoadsInResidentialCatalog()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.residential.gilded_age_mansion_tripo_01"));
            Assert.That(package.ShortDisplayName, Is.EqualTo("Gilded Mansion"));
            Assert.That(package.Category, Is.EqualTo("Residential"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.WidthMeters, Is.EqualTo(26.557548f).Within(0.001f));
            Assert.That(package.DepthMeters, Is.EqualTo(26.914218f).Within(0.001f));
            Assert.That(package.HeightMeters, Is.EqualTo(17.659989f).Within(0.001f));
            Assert.That(package.OccupancyWidth, Is.EqualTo(3));
            Assert.That(package.OccupancyDepth, Is.EqualTo(3));
            Assert.That(package.PixelsPerMeter, Is.EqualTo(21.818182f).Within(0.001f));
            Assert.That(package.UsesPersistedArtworkPivot, Is.True);
            Assert.That(BuildingCatalog.ForUseCategory(
                    BuildingUseCategory.Residential, null)
                .Any(entry => entry.Id == package.Id), Is.True);
            Assert.That(Resources.Load<GameObject>(package.PrimitiveResourcePath),
                Is.Not.Null);
        }
    }
}
