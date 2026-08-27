using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class ArtMuseumPackageTests
    {
        private const string PackagePath = "CityForgeV3/Buildings/ArtMuseumTripoV01/building-package";

        [Test]
        public void ReviewPackageLoadsAsLargeCivicMuseum()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.Id, Is.EqualTo("cityforge.v3.civic.art_museum_tripo_01"));
            Assert.That(package.SchemaVersion, Is.EqualTo(HybridBuildingPackage.SourceDerivedIntakeSchema));
            Assert.That(package.Category, Is.EqualTo("Civic"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.WidthMeters, Is.EqualTo(20.226227f).Within(0.01f));
            Assert.That(package.DepthMeters, Is.EqualTo(27.691572f).Within(0.01f));
            Assert.That(package.HeightMeters, Is.EqualTo(24f).Within(0.01f));
            Assert.That(package.UsesMeshProjectedShadow, Is.True);
        }

        [Test]
        public void ProxyRetainsSculpturalRooflineAndMainPavilion()
        {
            var prefab = Resources.Load<GameObject>("CityForgeV3/Buildings/ArtMuseumTripoV01/semantic-primitive-v01");
            Assert.That(prefab, Is.Not.Null);
            var generated = prefab.GetComponentsInChildren<MeshFilter>(true)
                .First(filter => filter.name == "CF_PROXY_BUILDING_GENERATED");
            Assert.That(generated.sharedMesh.vertexCount, Is.GreaterThan(5000));
            Assert.That(generated.sharedMesh.bounds.size.y, Is.GreaterThan(23.5f));
            Assert.That(generated.sharedMesh.bounds.size.z, Is.GreaterThan(27f));
        }
    }
}
