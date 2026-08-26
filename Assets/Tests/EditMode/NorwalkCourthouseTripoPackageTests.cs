using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class NorwalkCourthouseTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/NorwalkCourthouseTripoV01/building-package";

        [Test]
        public void ReviewPackageLoadsInCivicCatalog()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.civic.norwalk_courthouse_tripo_01"));
            Assert.That(package.SchemaVersion, Is.EqualTo(
                HybridBuildingPackage.SourceDerivedIntakeSchema));
            Assert.That(package.ShortDisplayName, Is.EqualTo("Norwalk Courthouse"));
            Assert.That(package.Category, Is.EqualTo("Civic"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.WidthMeters, Is.EqualTo(26.019118f).Within(0.001f));
            Assert.That(package.DepthMeters, Is.EqualTo(25.938388f).Within(0.001f));
            Assert.That(package.HeightMeters, Is.EqualTo(34.075f).Within(0.001f));
            Assert.That(package.OccupancyWidth, Is.EqualTo(3));
            Assert.That(package.OccupancyDepth, Is.EqualTo(3));
            Assert.That(package.PixelsPerMeter, Is.EqualTo(24.208038f).Within(0.001f));
            Assert.That(package.UsesPersistedArtworkPivot, Is.True);
            Assert.That(package.UsesMeshProjectedShadow, Is.True);
            Assert.That(package.FrontFacingQuarterTurns, Is.EqualTo(1));
            CollectionAssert.Contains(package.RequiredPrimitiveObjects,
                "CF_PROXY_BUILDING_GENERATED");
            Assert.That(BuildingCatalog.ForUseCategory(
                    BuildingUseCategory.Civics, null)
                .Any(entry => entry.Id == package.Id), Is.True);
            Assert.That(Resources.Load<GameObject>(package.PrimitiveResourcePath),
                Is.Not.Null);
            var primitive = Resources.Load<GameObject>(package.PrimitiveResourcePath);
            var generated = primitive.GetComponentsInChildren<MeshFilter>(true)
                .First(filter => filter.name == "CF_PROXY_BUILDING_GENERATED");
            Assert.That(generated.sharedMesh.vertexCount, Is.GreaterThan(1000),
                "Towered civic buildings must retain a source-derived silhouette, not a box proxy.");
            Assert.That(generated.sharedMesh.bounds.size.y, Is.GreaterThan(30f),
                "The projected mesh must include the complete clock tower.");
        }

        [Test]
        public void V2IntakeRejectsABoxShadowFallback()
        {
            var source = Resources.Load<TextAsset>(PackagePath);
            Assert.That(source, Is.Not.Null);
            var manifest = JsonUtility.FromJson<HybridBuildingPackageManifest>(
                source.text);
            manifest.shadow.projectionMode = null;

            var issues = HybridBuildingPackage.Validate(manifest);
            Assert.That(issues, Does.Contain(
                "V2 intake requires shadow projectionMode projected-mesh"));

            manifest.shadow.projectionMode = "projected-mesh";
            manifest.primitive.requiredObjects = new[]
            {
                "CF_PROXY_FOUNDATION", "CF_PROXY_WALLS"
            };
            issues = HybridBuildingPackage.Validate(manifest);
            Assert.That(issues, Does.Contain(
                "V2 intake requires CF_PROXY_BUILDING_GENERATED"));
        }
    }
}
