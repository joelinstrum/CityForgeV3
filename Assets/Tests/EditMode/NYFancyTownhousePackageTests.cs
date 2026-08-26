using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class NYFancyTownhousePackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/NYFancyTownhouseTripoV01/building-package";

        [Test]
        public void PackageMatchesNYResidentialScaleAndV2ShadowContract()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.SchemaVersion, Is.EqualTo(
                HybridBuildingPackage.SourceDerivedIntakeSchema));
            Assert.That(package.HeightMeters, Is.EqualTo(15f).Within(0.01f));
            Assert.That(package.WidthMeters, Is.EqualTo(6.992843f).Within(0.01f));
            Assert.That(package.DepthMeters, Is.EqualTo(13.46917f).Within(0.01f));
            Assert.That(package.OccupancyWidth, Is.EqualTo(1));
            Assert.That(package.OccupancyDepth, Is.EqualTo(2));
            Assert.That(package.FrontFacingQuarterTurns, Is.EqualTo(0));
            Assert.That(package.UsesMeshProjectedShadow, Is.True);
            Assert.That(package.RequiredPrimitiveObjects,
                Does.Contain("CF_PROXY_BUILDING_GENERATED"));
        }

        [Test]
        public void ProxyRetainsStoopCorniceAndRoofSilhouette()
        {
            var prefab = Resources.Load<GameObject>(
                "CityForgeV3/Buildings/NYFancyTownhouseTripoV01/semantic-primitive-v01");
            Assert.That(prefab, Is.Not.Null);
            var generated = prefab.GetComponentsInChildren<MeshFilter>(true)
                .First(filter => filter.name == "CF_PROXY_BUILDING_GENERATED");
            Assert.That(generated.sharedMesh.vertexCount, Is.GreaterThan(3500));
            Assert.That(generated.sharedMesh.bounds.size.y, Is.GreaterThan(14.5f));
            Assert.That(generated.sharedMesh.bounds.size.z, Is.GreaterThan(13f));
        }
    }
}
