using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class PlymouthStoreTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/PlymouthStoreTripoV01/building-package";
        private const string MarlowePackagePath =
            "CityForgeV3/Buildings/DownloadsBatchV02/MarloweArtDecoHotelV02/" +
            "building-package";

        [Test]
        public void PackageUsesV2SourceDerivedShadowContract()
        {
            HybridBuildingPackageRegistry.InvalidateCache();
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.SchemaVersion, Is.EqualTo(
                HybridBuildingPackage.SourceDerivedIntakeSchema));
            Assert.That(package.UsesMeshProjectedShadow, Is.True);
            Assert.That(package.RequiredPrimitiveObjects,
                Does.Contain("CF_PROXY_BUILDING_GENERATED"));
        }

        [Test]
        public void ProxyRetainsMeaningfulStorefrontSilhouette()
        {
            var prefab = Resources.Load<GameObject>(
                "CityForgeV3/Buildings/PlymouthStoreTripoV01/semantic-primitive-v01");
            Assert.That(prefab, Is.Not.Null);
            var meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(meshFilters.Any(filter => filter.sharedMesh != null &&
                filter.sharedMesh.vertexCount > 1000), Is.True);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            Assert.That(bounds.size.y, Is.GreaterThan(10.1f));
        }

        [Test]
        public void ScaleAndDefaultEntranceAreCalibrated()
        {
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            Assert.That(package.HeightMeters, Is.EqualTo(10.36f).Within(0.01f));
            Assert.That(package.FrontFacingQuarterTurns, Is.EqualTo(0));
            Assert.That(package.OccupancyWidth, Is.EqualTo(1));
            Assert.That(package.OccupancyDepth, Is.EqualTo(1));
        }

        [Test]
        public void FocusSpotlightUsesAlphaTightBoundsAcrossEveryFacing()
        {
            var root = new GameObject("Plymouth Focus Bounds Test");
            try
            {
                var cameraObject = new GameObject("Focus Bounds Camera");
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 12f;
                camera.pixelRect = new Rect(0f, 0f, 1024f, 1024f);
                cameraObject.transform.position = new Vector3(12f, 10f, -12f);
                cameraObject.transform.LookAt(new Vector3(0f, 4f, 0f));

                var presentationObject = new GameObject("Plymouth Presentation");
                presentationObject.transform.SetParent(root.transform, false);
                var presentation =
                    presentationObject.AddComponent<HybridBuildingPresentation>();
                var package = HybridBuildingPackageRegistry.Load(PackagePath);
                presentation.Build(camera, package);
                presentation.SetVisible(true);

                for (var facing = 0; facing < package.FacingCount; facing++)
                {
                    presentation.ApplyFacing(facing);
                    Assert.That(presentation.TryGetVisibleArtworkScreenBounds(
                        camera, out var tightMinimum, out var tightMaximum),
                        Is.True, package.Facing(facing).Id);
                    Assert.That(presentation.TryGetArtworkRenderer(out var renderer),
                        Is.True, package.Facing(facing).Id);
                    ProjectSpriteBounds(camera, renderer,
                        out var fullMinimum, out var fullMaximum);

                    var tightSize = tightMaximum - tightMinimum;
                    var fullSize = fullMaximum - fullMinimum;
                    Assert.That(tightSize.x, Is.LessThan(fullSize.x * 0.75f),
                        package.Facing(facing).Id);
                    Assert.That(tightSize.y, Is.LessThan(fullSize.y * 0.92f),
                        package.Facing(facing).Id);
                    Assert.That(tightMinimum.x, Is.GreaterThanOrEqualTo(
                        fullMinimum.x - 0.01f), package.Facing(facing).Id);
                    Assert.That(tightMinimum.y, Is.GreaterThanOrEqualTo(
                        fullMinimum.y - 0.01f), package.Facing(facing).Id);
                    Assert.That(tightMaximum.x, Is.LessThanOrEqualTo(
                        fullMaximum.x + 0.01f), package.Facing(facing).Id);
                    Assert.That(tightMaximum.y, Is.LessThanOrEqualTo(
                        fullMaximum.y + 0.01f), package.Facing(facing).Id);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FocusSpotlightTracksTheActuallyDisplayedFullNightArtwork()
        {
            var root = new GameObject("Marlowe Night Focus Bounds Test");
            try
            {
                var cameraObject = new GameObject("Focus Bounds Camera");
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 18f;
                camera.pixelRect = new Rect(0f, 0f, 1024f, 1024f);
                cameraObject.transform.position = new Vector3(12f, 10f, -12f);
                cameraObject.transform.LookAt(new Vector3(0f, 4f, 0f));

                var presentationObject = new GameObject("Marlowe Presentation");
                presentationObject.transform.SetParent(root.transform, false);
                var presentation =
                    presentationObject.AddComponent<HybridBuildingPresentation>();
                var package =
                    HybridBuildingPackageRegistry.Load(MarlowePackagePath);
                presentation.Build(camera, package);
                presentation.SetVisible(true);
                presentation.ApplyFacing(0);

                presentation.SetTimeOfDay(TimeOfDayPreset.Noon);
                Assert.That(presentation.TryGetVisibleArtworkScreenBounds(
                    camera, out var dayMinimum, out var dayMaximum), Is.True);

                presentation.SetTimeOfDay(TimeOfDayPreset.Night);
                Assert.That(presentation.TryGetVisibleArtworkScreenBounds(
                    camera, out var nightMinimum, out var nightMaximum), Is.True);
                Assert.That(presentation.TryGetArtworkRenderer(out var renderer),
                    Is.True);
                ProjectSpriteBounds(camera, renderer,
                    out var fullMinimum, out var fullMaximum);

                var daySize = dayMaximum - dayMinimum;
                var nightSize = nightMaximum - nightMinimum;
                Assert.That(Vector2.Distance(daySize, nightSize),
                    Is.GreaterThan(0.01f),
                    "Marlowe's distinct full-night silhouette must use its own cache.");
                Assert.That(nightMinimum.x,
                    Is.GreaterThanOrEqualTo(fullMinimum.x - 0.01f));
                Assert.That(nightMinimum.y,
                    Is.GreaterThanOrEqualTo(fullMinimum.y - 0.01f));
                Assert.That(nightMaximum.x,
                    Is.LessThanOrEqualTo(fullMaximum.x + 0.01f));
                Assert.That(nightMaximum.y,
                    Is.LessThanOrEqualTo(fullMaximum.y + 0.01f));
                Assert.That(nightSize.x < fullMaximum.x - fullMinimum.x ||
                            nightSize.y < fullMaximum.y - fullMinimum.y,
                    Is.True,
                    "Night focus must exclude at least one transparent canvas margin.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ProjectSpriteBounds(Camera camera,
            SpriteRenderer renderer, out Vector2 minimum, out Vector2 maximum)
        {
            minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var bounds = renderer.sprite.bounds;
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            {
                var local = new Vector3(
                    x < 0 ? bounds.min.x : bounds.max.x,
                    y < 0 ? bounds.min.y : bounds.max.y,
                    0f);
                var screen = camera.WorldToScreenPoint(
                    renderer.transform.TransformPoint(local));
                minimum = Vector2.Min(minimum, screen);
                maximum = Vector2.Max(maximum, screen);
            }
        }
    }
}
