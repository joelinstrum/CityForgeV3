using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class HitchcockMansionTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/HitchcockMansionTripoV01/building-package";

        [Test]
        public void PackageLoadsAsLargeHistoricResidence()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.residential.hitchcock_mansion_tripo_01"));
            Assert.That(package.SchemaVersion, Is.EqualTo(
                HybridBuildingPackage.SourceDerivedIntakeSchema));
            Assert.That(package.Category, Is.EqualTo("Residential"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.WidthMeters, Is.EqualTo(17.710129f).Within(0.001f));
            Assert.That(package.DepthMeters, Is.EqualTo(18.542833f).Within(0.001f));
            Assert.That(package.HeightMeters, Is.EqualTo(17f).Within(0.001f));
            Assert.That(package.OccupancyWidth, Is.EqualTo(2));
            Assert.That(package.OccupancyDepth, Is.EqualTo(2));
            Assert.That(package.UsesMeshProjectedShadow, Is.True);
            CollectionAssert.Contains(package.RequiredPrimitiveObjects,
                "CF_PROXY_BUILDING_GENERATED");
            var catalog = Resources.Load<TextAsset>(
                "CityForgeV3/Buildings/active-building-catalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.text, Does.Contain(PackagePath));
        }

        [Test]
        public void ProxyRetainsMansardTowerPorchesAndChimneys()
        {
            var primitive = Resources.Load<GameObject>(
                "CityForgeV3/Buildings/HitchcockMansionTripoV01/semantic-primitive-v01");
            Assert.That(primitive, Is.Not.Null);
            var generated = primitive.GetComponentsInChildren<MeshFilter>(true)
                .First(filter => filter.name == "CF_PROXY_BUILDING_GENERATED");
            Assert.That(generated.sharedMesh.vertexCount, Is.GreaterThan(10000));
            Assert.That(generated.sharedMesh.bounds.size.y, Is.GreaterThan(16.5f));
            Assert.That(generated.sharedMesh.bounds.size.x, Is.GreaterThan(17f));
            Assert.That(generated.sharedMesh.bounds.size.z, Is.GreaterThan(16.5f));
        }
    }
}
