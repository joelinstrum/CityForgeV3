using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class LawOfficeTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/LawOfficeTripoV01/building-package";

        [Test]
        public void ReviewPackageLoadsInCommercialCatalog()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.commercial.law_office_tripo_01"));
            Assert.That(package.ShortDisplayName, Is.EqualTo("Law Office"));
            Assert.That(package.Category, Is.EqualTo("Commercial"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.UsesPersistedArtworkPivot, Is.True);
            Assert.That(BuildingCatalog.ForUseCategory(
                    BuildingUseCategory.Commercial, null)
                .Any(entry => entry.Id == package.Id), Is.True);
            Assert.That(Resources.Load<GameObject>(package.PrimitiveResourcePath),
                Is.Not.Null);
            for (var index = 0; index < package.FacingCount; index++)
            {
                var facing = package.Facing(index);
                Assert.That(Resources.Load<Texture2D>(facing.NeutralResourcePath),
                    Is.Not.Null, facing.Id);
                Assert.That(Resources.Load<Texture2D>(facing.NightOverlayResourcePath),
                    Is.Not.Null, facing.Id);
            }
        }
    }
}
