using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class FarForestCanopyTests
    {
        [Test]
        public void TrueAngleAtlasHasTwelveMatchingSeasonalTreeSlots()
        {
            foreach (var season in new[] { SeasonPreset.Spring,
                         SeasonPreset.Summer, SeasonPreset.Autumn,
                         SeasonPreset.Winter })
            {
                var path = ForestTrueAngleCluster.ResourcePath(season);
                var texture = Resources.Load<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(1536));
                Assert.That(texture.height, Is.EqualTo(1024));
                Assert.That(ForestTrueAngleCluster.RootSprite(season).texture,
                    Is.SameAs(texture));
            }
            Assert.That(ForestTrueAngleCluster.ResourcePath(SeasonPreset.Spring),
                Is.EqualTo(ForestTrueAngleCluster.ResourcePath(SeasonPreset.Summer)));
            var owner = new GameObject("True angle atlas layout test");
            try
            {
                var cluster = owner.AddComponent<ForestTrueAngleCluster>();
                cluster.Configure("forest-deciduous-large", 0,
                    SeasonPreset.Summer, _ => 0f);
                Assert.That(cluster.PieceCount, Is.EqualTo(7));
                var summerRects = Enumerable.Range(0, cluster.PieceCount)
                    .Select(index => cluster.Piece(index).rect).ToArray();
                cluster.SetSeason(SeasonPreset.Autumn);
                Assert.That(Enumerable.Range(0, cluster.PieceCount)
                    .Select(index => cluster.Piece(index).rect),
                    Is.EqualTo(summerRects));
                cluster.SetSeason(SeasonPreset.Winter);
                Assert.That(Enumerable.Range(0, cluster.PieceCount)
                    .Select(index => cluster.Piece(index).rect),
                    Is.EqualTo(summerRects));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void VariationsKeepOneHandleAndBatchSevenTreeQuads()
        {
            var owner = new GameObject("True angle batch test");
            try
            {
                var sprite = ForestTrueAngleCluster.RootSprite(SeasonPreset.Summer);
                var variants = new List<ForestTrueAngleCluster>();
                var renderers = new List<SpriteRenderer>();
                for (var index = 0; index < 3; index++)
                {
                    var tree = new GameObject("Large clump " + index);
                    tree.transform.SetParent(owner.transform, false);
                    tree.transform.localPosition = new Vector3(index * 40, 0, 0);
                    var cluster = tree.AddComponent<ForestTrueAngleCluster>();
                    cluster.Configure("forest-deciduous-large", index,
                        SeasonPreset.Summer, _ => 0f);
                    var renderer = tree.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    variants.Add(cluster);
                    renderers.Add(renderer);
                }
                Assert.That(variants.Select(cluster => cluster.Piece(0).rect)
                    .Distinct().Count(), Is.EqualTo(3));
                var batches = owner.AddComponent<DistrictFloraBatches>();
                batches.Build(renderers);
                var output = owner.GetComponentsInChildren<MeshRenderer>()
                    .Where(renderer => renderer.name == "Flora batch").ToArray();
                Assert.That(output.Length, Is.EqualTo(1));
                Assert.That(output[0].GetComponent<MeshFilter>().sharedMesh.vertexCount,
                    Is.EqualTo(3 * 7 * 4));
                Assert.That(renderers.All(renderer => renderer.forceRenderingOff),
                    Is.True);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void FirAtlasSuppliesVariedIndividualAndMountainClumpTrees()
        {
            var owner = new GameObject("Fir atlas district test");
            try
            {
                var district = new RegionCityTile
                {
                    TileId = "fir-atlas-test", Width = 1, Height = 1,
                    Founded = true,
                    Labor = new DistrictLaborState { SeasonIndex = 0 }
                };
                foreach (var id in new[] { "forest-mountain-compact",
                             "forest-mountain-large", "cilician-fir",
                             "medium-balsam-fir", "medium-fraser-fir",
                             "medium-blue-spruce" })
                    district.Flora.Add(new PlacedDistrictFlora
                    {
                        InstanceId = id, FloraId = id,
                        NormalizedX = .15f + district.Flora.Count * .13f,
                        NormalizedZ = .5f
                    });
                var world = owner.AddComponent<DistrictWorldController>();
                world.RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);
                var trees = owner.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(renderer => renderer.name.StartsWith("District Flora — "))
                    .ToArray();
                Assert.That(trees.Length, Is.EqualTo(6));
                Assert.That(trees.Select(renderer => renderer.sprite.texture)
                    .Distinct().Count(), Is.EqualTo(1));
                Assert.That(trees.All(renderer => renderer.sprite.texture.name ==
                    "fir-trees"), Is.True);
                Assert.That(trees.Select(renderer =>
                    renderer.GetComponent<ForestTrueAngleCluster>().PieceCount),
                    Is.EquivalentTo(new[] { 4, 7, 1, 1, 1, 1 }));
                Assert.That(trees.Where(renderer => renderer.GetComponent<
                        ForestTrueAngleCluster>().PieceCount == 1)
                    .Select(renderer => renderer.GetComponent<
                        ForestTrueAngleCluster>().Piece(0).rect)
                    .Distinct().Count(), Is.GreaterThan(1));
                district.Labor.SeasonIndex = 2;
                var guard = 0;
                do
                {
                    world.SyncForestSeason(4);
                    Assert.That(++guard, Is.LessThan(10));
                } while (world.ForestSeasonPending);
                Assert.That(trees.All(renderer => renderer.sprite.texture.name ==
                    "fir-trees-winter"), Is.True);
                Assert.That(trees.All(renderer => renderer.GetComponent<
                    ForestTrueAngleCluster>().Season == SeasonPreset.Winter),
                    Is.True);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void FirIndividualSlotsUseMatchingSnowArt()
        {
            var summer = ForestTrueAngleCluster.IndividualSprite(
                "medium-fraser-fir", 2, SeasonPreset.Summer);
            var autumn = ForestTrueAngleCluster.IndividualSprite(
                "medium-fraser-fir", 2, SeasonPreset.Autumn);
            var winter = ForestTrueAngleCluster.IndividualSprite(
                "medium-fraser-fir", 2, SeasonPreset.Winter);
            Assert.That(autumn, Is.SameAs(summer));
            Assert.That(winter.texture.name, Is.EqualTo("fir-trees-winter"));
            Assert.That(winter.rect.x + winter.pivot.x,
                Is.EqualTo(summer.rect.x + summer.pivot.x).Within(.01f),
                "Seasonal crops may differ, but trunk anchors must coincide.");
            Assert.That(ForestTrueAngleCluster.IndividualSprite(
                "medium-fraser-fir", 1, SeasonPreset.Summer).rect,
                Is.Not.EqualTo(summer.rect));
        }

        [Test]
        public void FirIndividualTrunksAlignWithTheirAtlasFootMargins()
        {
            var expectedSummer = new[] { 1f, 47f, 18f, 15f };
            var expectedWinter = new[] { 13f, 29f, 17f, 17f };
            for (var variation = 0; variation < 4; variation++)
            {
                var summer = ForestTrueAngleCluster.IndividualSprite(
                    "cilician-fir", variation, SeasonPreset.Summer);
                var winter = ForestTrueAngleCluster.IndividualSprite(
                    "cilician-fir", variation, SeasonPreset.Winter);
                Assert.That(summer.pivot.y,
                    Is.EqualTo(expectedSummer[variation]).Within(.1f));
                Assert.That(winter.pivot.y,
                    Is.EqualTo(expectedWinter[variation]).Within(.1f));
            }
        }

        [Test]
        public void LotIndividualFirUsesTheSameAtlasWithoutChangingSavedIdentity()
        {
            var owner = new GameObject("Lot fir atlas test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                var load = typeof(LotWorldController).GetMethod(
                    "LoadFloraSprite", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(load, Is.Not.Null);
                var sprite = (Sprite)load.Invoke(world,
                    new object[] { "medium-fraser-fir", 2 });
                Assert.That(sprite, Is.SameAs(ForestTrueAngleCluster.IndividualSprite(
                    "medium-fraser-fir", 2, SeasonPreset.Summer)));
                var alias = (Sprite)load.Invoke(world,
                    new object[] { "evergreen", 1 });
                Assert.That(alias, Is.SameAs(ForestTrueAngleCluster.IndividualSprite(
                    "medium-blue-spruce", 1, SeasonPreset.Summer)));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [TestCase("forest-deciduous-large", 0)]
        [TestCase("forest-mountain-large", 0)]
        [TestCase("forest-mountain-large", 2)]
        public void NoonAtlasShadowsStayTuckedUnderTheirTrees(
            string id, int seasonIndex)
        {
            var owner = new GameObject("Tight noon forest shadow test");
            var mesh = new Mesh();
            try
            {
                var tree = new GameObject("Cluster");
                tree.transform.SetParent(owner.transform, false);
                var cluster = tree.AddComponent<ForestTrueAngleCluster>();
                var season = ForestClusterCatalog.SeasonForIndex(seasonIndex);
                cluster.Configure(id, 0, season, _ => 0f);
                var source = tree.AddComponent<SpriteRenderer>();
                source.sprite = ForestTrueAngleCluster.RootSprite(id, season);
                var shadowObject = new GameObject("Shadow");
                shadowObject.transform.SetParent(tree.transform, false);
                shadowObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var shadow = shadowObject.AddComponent<MeshRenderer>();
                var ray = TimeOfDayLighting.SunRotation(
                    TimeOfDayPreset.Noon) * Vector3.forward;
                Assert.That(ForestClusterShadows.Update(source, shadow, ray,
                    _ => 0f, point => point, .55f), Is.True);
                var behind = new Vector3(ray.x, 0f, ray.z).normalized;
                for (var piece = 0; piece < cluster.PieceCount; piece++)
                {
                    var treeFoot = cluster.WorldOffset(piece);
                    var farthest = mesh.vertices.Skip(piece * 50).Take(41)
                        .Max(vertex => Vector3.Dot(vertex - treeFoot, behind));
                    Assert.That(farthest,
                        Is.LessThan(cluster.TreeWidth * cluster.PieceScale(piece) * .38f),
                        "The noon crown shadow should end beneath the canopy.");
                }
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase("forest-deciduous-compact")]
        [TestCase("forest-deciduous-large")]
        public void NoonDeciduousCrownShadowProjectsBehindTheCameraFacingTrunk(
            string id)
        {
            var owner = new GameObject("Camera-facing noon shadow test");
            var mesh = new Mesh();
            try
            {
                var tree = new GameObject("Cluster");
                tree.transform.SetParent(owner.transform, false);
                var cluster = tree.AddComponent<ForestTrueAngleCluster>();
                cluster.Configure(id, 0, SeasonPreset.Summer, _ => 0f);
                var source = tree.AddComponent<SpriteRenderer>();
                source.sprite = ForestTrueAngleCluster.RootSprite(id,
                    SeasonPreset.Summer);
                var shadowObject = new GameObject("Shadow");
                shadowObject.transform.SetParent(tree.transform, false);
                shadowObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var shadow = shadowObject.AddComponent<MeshRenderer>();
                var ray = TimeOfDayLighting.SunRotation(
                    TimeOfDayPreset.Noon) * Vector3.forward;
                var awayFromCamera = new Vector3(-1f, 0f, 1f).normalized;
                Assert.That(ForestClusterShadows.Update(source, shadow, ray,
                    _ => 0f, point => point, .55f, awayFromCamera), Is.True);
                for (var piece = 0; piece < cluster.PieceCount; piece++)
                {
                    var treeFoot = cluster.WorldOffset(piece);
                    var center = mesh.vertices[piece * 50];
                    Assert.That(Vector3.Dot(center - treeFoot, awayFromCamera),
                        Is.GreaterThan(cluster.TreeWidth * .16f),
                        "The crown mass must land behind the trunk on screen.");
                }
                var centerVertex = mesh.vertices[0];
                var inner = (mesh.vertices[1] - centerVertex).magnitude;
                var outer = (mesh.vertices[21] - centerVertex).magnitude;
                Assert.That(inner / outer, Is.GreaterThan(.93f),
                    "The crown edge should have a narrow, crisp feather.");
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void DistrictNoonDeciduousShadowUsesItsActualCameraDirection()
        {
            var owner = new GameObject("District noon shadow direction test");
            try
            {
                var district = new RegionCityTile
                {
                    TileId = "noon-shadow-direction", Width = 1, Height = 1,
                    Founded = true, TimeOfDay = TimeOfDayPreset.Noon
                };
                district.Flora.Add(new PlacedDistrictFlora
                {
                    InstanceId = "forest", FloraId = "forest-deciduous-compact",
                    NormalizedX = .5f, NormalizedZ = .5f
                });
                var world = owner.AddComponent<DistrictWorldController>();
                world.RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);
                var camera = owner.GetComponentInChildren<Camera>();
                var tree = owner.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.name ==
                        "District Flora — forest-deciduous-compact");
                var shadow = tree.transform.Find("District Flora Shadow");
                var mesh = shadow.GetComponent<MeshFilter>().sharedMesh;
                var away = Vector3.ProjectOnPlane(camera.transform.forward,
                    Vector3.up).normalized;
                var firstTreeFoot = tree.transform.position +
                    tree.GetComponent<ForestTrueAngleCluster>().WorldOffset(0);
                var firstCrownCenter = shadow.TransformPoint(mesh.vertices[0]);
                Assert.That(Vector3.Dot(firstCrownCenter - firstTreeFoot, away),
                    Is.GreaterThan(0f),
                    "Noon crown shadow must project away from the viewing camera.");
                var referenceObject = new GameObject("Former noon projection");
                referenceObject.transform.SetParent(tree.transform, false);
                var referenceMesh = new Mesh();
                referenceObject.AddComponent<MeshFilter>().sharedMesh =
                    referenceMesh;
                var referenceShadow = referenceObject.AddComponent<MeshRenderer>();
                var ray = ForestClusterShadows.BehindCameraRay(
                    TimeOfDayLighting.SunRotation(TimeOfDayPreset.Noon) *
                        Vector3.forward, camera.transform.forward);
                Assert.That(ForestClusterShadows.Update(tree, referenceShadow,
                    ray, _ => firstCrownCenter.y, point => point, .55f,
                    away), Is.True);
                var formerCenter = referenceObject.transform.TransformPoint(
                    referenceMesh.vertices[0]);
                Assert.That(Vector3.Dot(firstCrownCenter - firstTreeFoot, away),
                    Is.GreaterThan(Vector3.Dot(formerCenter - firstTreeFoot,
                        away) + .03f),
                    "Noon clump shadows should extend beyond the former " +
                    "compressed projection.");
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void SeasonSwapOnlyChangesOneBudgetOfExistingClusterHandles()
        {
            var owner = new GameObject("Far canopy zoom test");
            var world = owner.AddComponent<DistrictWorldController>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            void Set(string name, object value) =>
                typeof(DistrictWorldController).GetField(name, flags)
                    .SetValue(world, value);
            var district = new RegionCityTile
            {
                Labor = new DistrictLaborState { SeasonIndex = 0 }
            };
            Set("_content", owner.transform);
            Set("_terrainDistrict", district);
            var nearSprite = ForestTrueAngleCluster.RootSprite(SeasonPreset.Summer);
            var registry = (Dictionary<string, SpriteRenderer>)
                typeof(DistrictWorldController).GetField("_forestClusters",
                    flags).GetValue(world);
            var trees = new List<SpriteRenderer>();
            try
            {
                for (var index = 0; index < 41; index++)
                {
                    var tree = new GameObject("Canopy " + index)
                        .AddComponent<SpriteRenderer>();
                    tree.transform.SetParent(owner.transform, false);
                    tree.sprite = nearSprite;
                    tree.gameObject.AddComponent<ForestTrueAngleCluster>()
                        .Configure("forest-deciduous-large", index,
                            SeasonPreset.Summer, _ => 0f);
                    registry.Add(index.ToString(), tree);
                    trees.Add(tree);
                }
                district.Labor.SeasonIndex = 1;
                world.SyncForestSeason(8);
                Assert.That(world.ForestSeasonPending, Is.True);
                var farTexture = Resources.Load<Texture2D>(
                    ForestTrueAngleCluster.ResourcePath(SeasonPreset.Autumn));
                Assert.That(trees.Count(tree => tree.sprite.texture ==
                    farTexture), Is.EqualTo(8));
                Assert.That(world.ForestSeasonPending, Is.True);
                var guard = 0;
                while (world.ForestSeasonPending)
                {
                    world.SyncForestSeason(8);
                    Assert.That(++guard, Is.LessThan(10));
                }
                Assert.That(trees.All(tree => tree.sprite.texture ==
                    farTexture), Is.True);
                Assert.That(registry.Count, Is.EqualTo(41));
                district.Labor.SeasonIndex = 0;
                guard = 0;
                do
                {
                    world.SyncForestSeason(8);
                    Assert.That(++guard, Is.LessThan(10));
                } while (world.ForestSeasonPending);
                Assert.That(trees.All(tree =>
                    tree.sprite == nearSprite &&
                    tree.GetComponent<ForestTrueAngleCluster>().Season ==
                        SeasonPreset.Summer), Is.True,
                    "Returning to summer must reuse the cached atlas sprites.");
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void DistrictUsesSameCanopyAtFarAndCloseZoom(int seasonIndex)
        {
            var owner = new GameObject("Forest appearance district test");
            try
            {
                var district = new RegionCityTile
                {
                    TileId = "far-canopy-fixture", Width = 1, Height = 1,
                    Founded = true,
                    Labor = new DistrictLaborState { SeasonIndex = seasonIndex }
                };
                district.Flora.Add(new PlacedDistrictFlora
                {
                    InstanceId = "deciduous-test",
                    FloraId = "forest-deciduous-large",
                    NormalizedX = .5f, NormalizedZ = .5f
                });
                var world = owner.AddComponent<DistrictWorldController>();
                world.RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);
                var tree = owner.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.name ==
                        "District Flora — forest-deciduous-large");
                var selectedTexture = Resources.Load<Texture2D>(
                    ForestTrueAngleCluster.ResourcePath(
                        ForestClusterCatalog.SeasonForIndex(seasonIndex)));
                Assert.That(tree.sprite.texture, Is.SameAs(selectedTexture));
                Assert.That(tree.GetComponent<ForestTrueAngleCluster>(), Is.Not.Null);
                Assert.That(tree.GetComponent<ForestTrueAngleCluster>().PieceCount,
                    Is.EqualTo(7));
                if (seasonIndex == 0)
                    Assert.That(tree.sprite.vertices.Length, Is.EqualTo(4),
                        "Atlas cutouts must remain four-vertex quads.");
                world.SetZoom(DistrictZoomLevel.LOD3);
                world.SyncForestSeason();
                Assert.That(tree.sprite.texture, Is.SameAs(selectedTexture));
                world.SetZoom(DistrictZoomLevel.LOD1);
                world.SyncForestSeason();
                Assert.That(tree.sprite.texture, Is.SameAs(selectedTexture));
                Assert.That(world.ForestSeasonPending, Is.False);
                Assert.That(tree.GetComponent<DistrictSelectable>(),
                    Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DenseCanopySeasonChangeKeepsBatchWorkInBoundedSlices(
            bool fir)
        {
            var owner = new GameObject("Dense far canopy test");
            try
            {
                var district = new RegionCityTile
                {
                    TileId = "dense-far-canopy", Width = 1, Height = 1,
                    Founded = true,
                    Labor = new DistrictLaborState { SeasonIndex = 0 }
                };
                for (var index = 0; index < 384; index++)
                    district.Flora.Add(new PlacedDistrictFlora
                    {
                        InstanceId = "dense-canopy-" + index,
                        FloraId = fir
                            ? index % 2 == 0 ? "forest-mountain-compact" :
                                "forest-mountain-large"
                            : index % 2 == 0 ? "forest-deciduous-compact" :
                                "forest-deciduous-large",
                        NormalizedX = .04f + index % 24 * .04f,
                        NormalizedZ = .04f + index / 24 * .058f
                    });
                var world = owner.AddComponent<DistrictWorldController>();
                world.RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);
                int BatchCount() => owner.GetComponentsInChildren<MeshRenderer>()
                    .Count(renderer => renderer.name == "Flora batch");
                var summerBatches = BatchCount();
                district.Labor.SeasonIndex = fir ? 2 : 1;
                var maxMilliseconds = 0d;
                var maxAllocatedBytes = 0L;
                var sliceTimes = new List<double>();
                var slices = 0;
                do
                {
                    var before = System.GC.GetAllocatedBytesForCurrentThread();
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    world.SyncForestSeason();
                    watch.Stop();
                    maxMilliseconds = System.Math.Max(maxMilliseconds,
                        watch.Elapsed.TotalMilliseconds);
                    sliceTimes.Add(watch.Elapsed.TotalMilliseconds);
                    maxAllocatedBytes = System.Math.Max(maxAllocatedBytes,
                        System.GC.GetAllocatedBytesForCurrentThread() - before);
                    Assert.That(++slices, Is.LessThan(100));
                } while (world.ForestSeasonPending);
                var seasonalBatches = BatchCount();
                Assert.That(slices, Is.GreaterThanOrEqualTo(96));
                Assert.That(summerBatches, Is.GreaterThan(0));
                Assert.That(seasonalBatches, Is.GreaterThan(0));
                sliceTimes.Sort();
                var p95 = sliceTimes[(int)(sliceTimes.Count * .95f)];
                TestContext.WriteLine("384 " + (fir ? "fir" : "deciduous") +
                    " clusters: summer batches=" +
                    summerBatches + ", changed-season batches=" +
                    seasonalBatches +
                    ", season slices=" + slices +
                    ", max slice ms=" + maxMilliseconds.ToString("F2") +
                    ", p95 slice ms=" + p95.ToString("F2") +
                    ", max allocated bytes=" + maxAllocatedBytes);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
