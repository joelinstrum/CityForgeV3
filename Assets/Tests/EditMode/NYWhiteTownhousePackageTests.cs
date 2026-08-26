using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class NYWhiteTownhousePackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/NYWhiteTownhouseTripoV01/building-package";

        [Test]
        public void PackageUsesThreeStoryScaleAndV2ShadowContract()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.SchemaVersion, Is.EqualTo(
                HybridBuildingPackage.SourceDerivedIntakeSchema));
            Assert.That(package.HeightMeters, Is.EqualTo(13f).Within(0.01f));
            Assert.That(package.WidthMeters, Is.EqualTo(7.544923f).Within(0.01f));
            Assert.That(package.DepthMeters, Is.EqualTo(17.26618f).Within(0.01f));
            Assert.That(package.OccupancyWidth, Is.EqualTo(1));
            Assert.That(package.OccupancyDepth, Is.EqualTo(2));
            Assert.That(package.FrontFacingQuarterTurns, Is.EqualTo(0));
            Assert.That(package.UsesMeshProjectedShadow, Is.True);
            Assert.That(package.RequiredPrimitiveObjects,
                Does.Contain("CF_PROXY_BUILDING_GENERATED"));
        }

        [Test]
        public void ProxyRetainsStoopRoofAndIvySilhouette()
        {
            var prefab = Resources.Load<GameObject>(
                "CityForgeV3/Buildings/NYWhiteTownhouseTripoV01/semantic-primitive-v01");
            Assert.That(prefab, Is.Not.Null);
            var generated = prefab.GetComponentsInChildren<MeshFilter>(true)
                .First(filter => filter.name == "CF_PROXY_BUILDING_GENERATED");
            Assert.That(generated.sharedMesh.vertexCount, Is.GreaterThan(3000));
            Assert.That(generated.sharedMesh.bounds.size.y, Is.GreaterThan(12.5f));
            Assert.That(generated.sharedMesh.bounds.size.z, Is.GreaterThan(16.5f));
        }
    }
}
