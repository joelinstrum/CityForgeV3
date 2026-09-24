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

        [Test]
        public void DenseCanopySeasonChangeKeepsBatchWorkInBoundedSlices()
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
                        FloraId = index % 2 == 0
                            ? "forest-deciduous-compact"
                            : "forest-deciduous-large",
                        NormalizedX = .04f + index % 24 * .04f,
                        NormalizedZ = .04f + index / 24 * .058f
                    });
                var world = owner.AddComponent<DistrictWorldController>();
                world.RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);
                int BatchCount() => owner.GetComponentsInChildren<MeshRenderer>()
                    .Count(renderer => renderer.name == "Flora batch");
                var summerBatches = BatchCount();
                district.Labor.SeasonIndex = 1;
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
                var autumnBatches = BatchCount();
                Assert.That(slices, Is.GreaterThanOrEqualTo(96));
                Assert.That(summerBatches, Is.GreaterThan(0));
                Assert.That(autumnBatches, Is.GreaterThan(0));
                sliceTimes.Sort();
                var p95 = sliceTimes[(int)(sliceTimes.Count * .95f)];
                TestContext.WriteLine("384 deciduous clusters: summer batches=" +
                    summerBatches + ", autumn batches=" + autumnBatches +
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
