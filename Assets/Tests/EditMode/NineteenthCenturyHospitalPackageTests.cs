using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class NineteenthCenturyHospitalPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/NineteenthCenturyHospitalTripoV01/building-package";

        [Test]
        public void ReviewPackageUsesInstitutionalScaleAndChurchShadowContract()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.Id, Is.EqualTo("cityforge.v3.civic.nineteenth_century_hospital_tripo_01"));
            Assert.That(package.SchemaVersion, Is.EqualTo(HybridBuildingPackage.SourceDerivedIntakeSchema));
            Assert.That(package.Category, Is.EqualTo("Civic"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.WidthMeters, Is.EqualTo(26.043871f).Within(0.01f));
            Assert.That(package.DepthMeters, Is.EqualTo(22.592205f).Within(0.01f));
            Assert.That(package.HeightMeters, Is.EqualTo(24f).Within(0.01f));
            Assert.That(package.CanvasWidth, Is.EqualTo(1280));
            Assert.That(package.CanvasHeight, Is.EqualTo(1280));
            Assert.That(package.UsesMeshProjectedShadow, Is.True);
            Assert.That(package.RequiredPrimitiveObjects, Does.Contain("CF_PROXY_BUILDING_GENERATED"));
        }

        [Test]
        public void ProxyRetainsDomedHospitalSilhouette()
        {
            var prefab = Resources.Load<GameObject>(
                "CityForgeV3/Buildings/NineteenthCenturyHospitalTripoV01/semantic-primitive-v01");
            Assert.That(prefab, Is.Not.Null);
            var generated = prefab.GetComponentsInChildren<MeshFilter>(true)
                .First(filter => filter.name == "CF_PROXY_BUILDING_GENERATED");
            Assert.That(generated.sharedMesh.vertexCount, Is.GreaterThan(5000));
            Assert.That(generated.sharedMesh.bounds.size.y, Is.GreaterThan(23.5f));

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.That(filter.sharedMesh, Is.Not.Null, filter.name);
                Assert.That(filter.sharedMesh.isReadable, Is.True,
                    $"{filter.name} must remain CPU-readable for tight lot-editor footprints.");
            }
        }

        [Test]
        public void SourceDerivedFacingsUseVersionedSafeCanvas()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            var facing = package.Facing(3);
            Assert.That(facing.Id, Is.EqualTo("front-left"));
            Assert.That(facing.NeutralResourcePath,
                Does.EndWith("front-left-neutral-v03"));
            Assert.That(facing.NightOverlayResourcePath,
                Does.EndWith("front-left-night-v03"));
            Assert.That(facing.SourcePivotTopOrigin.y,
                Is.EqualTo(0.742987275f).Within(0.000001f));

            var neutral = Resources.Load<Texture2D>(facing.NeutralResourcePath);
            var night = Resources.Load<Texture2D>(facing.NightOverlayResourcePath);
            Assert.That(neutral, Is.Not.Null);
            Assert.That(night, Is.Not.Null);
            Assert.That(neutral.width, Is.EqualTo(1280));
            Assert.That(neutral.height, Is.EqualTo(1280));
            Assert.That(night.width, Is.EqualTo(neutral.width));
            Assert.That(night.height, Is.EqualTo(neutral.height));

            for (var index = 0; index < package.FacingCount; index++)
            {
                var registeredFacing = package.Facing(index);
                var registeredNeutral = Resources.Load<Texture2D>(
                    registeredFacing.NeutralResourcePath);
                var registeredNight = Resources.Load<Texture2D>(
                    registeredFacing.NightOverlayResourcePath);
                Assert.That(registeredNeutral.width, Is.EqualTo(1280));
                Assert.That(registeredNeutral.height, Is.EqualTo(1280));
                Assert.That(registeredNight.width, Is.EqualTo(1280));
                Assert.That(registeredNight.height, Is.EqualTo(1280));
            }
        }
    }
}
