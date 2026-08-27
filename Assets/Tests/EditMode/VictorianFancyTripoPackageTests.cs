using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class VictorianFancyTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/VictorianFancyTripoV01/building-package";

        [Test]
        public void ReviewPackageLoadsWithRegisteredEveningOverlays()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);

            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.residential.red_victorian_tripo_01"));
            Assert.That(package.ShortDisplayName, Is.EqualTo("Red Victorian"));
            Assert.That(package.Category, Is.EqualTo("Residential"));
            Assert.That(package.Subcategory, Is.EqualTo("Mid-Wealth"));
            Assert.That(package.ReviewStatus, Does.Contain("REVIEW"));
            Assert.That(package.WidthMeters, Is.EqualTo(5.838204f).Within(0.001f));
            Assert.That(package.DepthMeters, Is.EqualTo(9.690476f).Within(0.001f));
            Assert.That(package.HeightMeters, Is.EqualTo(12.281418f).Within(0.001f));
            Assert.That(package.PixelsPerMeter, Is.EqualTo(48f));
            Assert.That(package.UsesPersistedArtworkPivot, Is.True);
            Assert.That(package.FacingCount, Is.EqualTo(4));

            var catalogEntry = BuildingCatalog.ForUseCategory(
                    BuildingUseCategory.Residential, "Mid-Wealth")
                .Single(entry => entry.Id == package.Id);
            Assert.That(catalogEntry.ShortName, Is.EqualTo("Red Victorian"));

            Assert.That(Resources.Load<GameObject>(package.PrimitiveResourcePath),
                Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(package.PlanResourcePath),
                Is.Not.Null);

            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                var neutral = Resources.Load<Texture2D>(facing.NeutralResourcePath);
                var night = Resources.Load<Texture2D>(facing.NightOverlayResourcePath);
                Assert.That(neutral, Is.Not.Null, facing.Id);
                Assert.That(night, Is.Not.Null, facing.Id);
                Assert.That(neutral.width, Is.EqualTo(1024), facing.Id);
                Assert.That(neutral.height, Is.EqualTo(1024), facing.Id);
                Assert.That(night.width, Is.EqualTo(1024), facing.Id);
                Assert.That(night.height, Is.EqualTo(1024), facing.Id);
            }
        }
    }
}
