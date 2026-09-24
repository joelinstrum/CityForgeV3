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
        public void CanopyHasSeparateSummerAndAutumnArtWithWinterFallback()
        {
            foreach (var id in new[] { "forest-deciduous-compact",
                         "forest-deciduous-large" })
            foreach (var season in new[] { SeasonPreset.Summer,
                         SeasonPreset.Autumn })
            {
                var path = ForestClusterCatalog.FarCanopyResourcePath(
                    id, season);
                var texture = Resources.Load<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(1536));
                Assert.That(texture.height, Is.EqualTo(1024));
            }
            Assert.That(ForestClusterCatalog.FarCanopyResourcePath(
                "forest-deciduous-large", SeasonPreset.Winter), Is.Null);
            Assert.That(ForestClusterCatalog.FarCanopyResourcePath(
                "forest-mountain-large", SeasonPreset.Autumn), Is.Null);
            Assert.That(ForestClusterCatalog.FarCanopyResourcePath(
                "forest-deciduous-large", SeasonPreset.Spring),
                Does.Contain("ForestCanopyGroundedSummerV02"));
            Assert.That(ForestClusterCatalog.FarCanopyResourcePath(
                "forest-deciduous-compact", SeasonPreset.Summer),
                Does.Contain("ForestCanopyGroundedSummerV02"));
            Assert.That(ForestClusterCatalog.FarCanopyResourcePath(
                "forest-deciduous-large", SeasonPreset.Autumn),
                Does.Contain("ForestCanopyFarV01"));
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
            var nearTexture = Resources.Load<Texture2D>(
                ForestClusterCatalog.ResourcePath("forest-deciduous-large",
                    SeasonPreset.Summer));
            var nearSprite = Sprite.Create(nearTexture,
                new Rect(0, 0, nearTexture.width, nearTexture.height),
                ForestClusterCatalog.Pivot,
                ForestClusterCatalog.LargePixelsPerUnit);
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
                    registry.Add(index.ToString(), tree);
                    trees.Add(tree);
                }
                district.Labor.SeasonIndex = 1;
                world.SyncForestSeason(8);
                Assert.That(world.ForestSeasonPending, Is.True);
                var farTexture = Resources.Load<Texture2D>(
                    ForestClusterCatalog.FarCanopyResourcePath(
                        "forest-deciduous-large", SeasonPreset.Autumn));
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
            }
            finally
            {
                Object.DestroyImmediate(nearSprite);
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
                    ForestClusterCatalog.FarCanopyResourcePath(
                        "forest-deciduous-large",
                        ForestClusterCatalog.SeasonForIndex(seasonIndex)));
                Assert.That(tree.sprite.texture, Is.SameAs(selectedTexture));
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
