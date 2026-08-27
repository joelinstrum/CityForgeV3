using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class NineteenthCenturyRetailShopsPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/NineteenthCenturyRetailShopsTripoV01/building-package";

        [Test]
        public void PackageMatchesPlymouthHeightAndUsesV2ShadowContract()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.SchemaVersion, Is.EqualTo(
                HybridBuildingPackage.SourceDerivedIntakeSchema));
            Assert.That(package.HeightMeters, Is.EqualTo(10.36f).Within(0.01f));
            Assert.That(package.WidthMeters, Is.EqualTo(29.268133f).Within(0.01f));
            Assert.That(package.DepthMeters, Is.EqualTo(7.725878f).Within(0.01f));
            Assert.That(package.OccupancyWidth, Is.EqualTo(3));
            Assert.That(package.OccupancyDepth, Is.EqualTo(1));
            Assert.That(package.FrontFacingQuarterTurns, Is.EqualTo(0));
            Assert.That(package.UsesMeshProjectedShadow, Is.True);
            Assert.That(package.RequiredPrimitiveObjects,
                Does.Contain("CF_PROXY_BUILDING_GENERATED"));
        }

        [Test]
        public void ProxyRetainsCompleteFiveShopSilhouette()
        {
            var prefab = Resources.Load<GameObject>(
                "CityForgeV3/Buildings/NineteenthCenturyRetailShopsTripoV01/semantic-primitive-v01");
            Assert.That(prefab, Is.Not.Null);
            var generated = prefab.GetComponentsInChildren<MeshFilter>(true)
                .First(filter => filter.name == "CF_PROXY_BUILDING_GENERATED");
            Assert.That(generated.sharedMesh.vertexCount, Is.GreaterThan(3000));
            Assert.That(generated.sharedMesh.bounds.size.x, Is.GreaterThan(28f));
            Assert.That(generated.sharedMesh.bounds.size.y, Is.GreaterThan(10f));
        }
    }
}
