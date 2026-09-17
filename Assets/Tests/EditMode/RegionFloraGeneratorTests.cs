using System;
using System.IO;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class RegionFloraGeneratorTests
{
    static RegionCityTile District() => new() { TileId = "forest-test", Name = "Forest test", Width = 1, Height = 1 };
    [Test] public void OldSavesDefaultToTemperateWithoutGeneratingTrees()
    {
        var region = JsonUtility.FromJson<RegionSaveData>("{\"RegionId\":\"old\",\"Tiles\":[{\"TileId\":\"old-district\"}]}");
        RegionClimateRules.Apply(region);
        Assert.AreEqual(RegionClimate.Temperate, region.Terrain.Climate);
        Assert.AreEqual(RegionTreeCoverage.None, region.Terrain.TreeCoverage);
        Assert.IsEmpty(region.Tiles[0].Flora);
    }
    [TestCase(RegionClimate.Temperate)] [TestCase(RegionClimate.Mediterranean)] [TestCase(RegionClimate.Tropical)]
    public void SeededCoverageIsRepeatableDenserWhenWoodedAndUsesClimateTrees(RegionClimate climate)
    {
        var d = District();
        var sparse = RegionFloraGenerator.Generate(d, climate, RegionTreeCoverage.Sparse, 83);
        var wooded = RegionFloraGenerator.Generate(d, climate, RegionTreeCoverage.Wooded, 83);
        Assert.Greater(sparse.Count, 2); Assert.Greater(wooded.Count, sparse.Count * 5);
        Assert.True(wooded.All(t => RegionClimateRules.AllowsTree(climate, t.FloraId)));
        Assert.True(wooded.All(t => t.GeneratedByRegion && t.NormalizedX > 0 && t.NormalizedX < 1 && t.NormalizedZ > 0 && t.NormalizedZ < 1));
        CollectionAssert.AreEqual(wooded.Select(JsonUtility.ToJson), RegionFloraGenerator.Generate(d, climate, RegionTreeCoverage.Wooded, 83).Select(JsonUtility.ToJson));
        Assert.AreNotEqual(wooded[0].InstanceId, RegionFloraGenerator.Generate(d, climate, RegionTreeCoverage.Wooded, 84)[0].InstanceId);
        if (climate != RegionClimate.Tropical) Assert.True(wooded.Any(DistrictTreeHarvest.CanFell));
    }
    [Test] public void HeavyTriplesMediumDensityAndPreservesSavedCoverageValues()
    {
        Assert.AreEqual(1, (int)RegionTreeCoverage.Sparse);
        Assert.AreEqual(2, (int)RegionTreeCoverage.Wooded);
        var d = District(); d.Width = 4; d.Height = 4;
        var medium = RegionFloraGenerator.Generate(d, RegionClimate.Temperate, RegionTreeCoverage.Wooded, 193);
        var heavy = RegionFloraGenerator.Generate(d, RegionClimate.Temperate, RegionTreeCoverage.Heavy, 193);
        Assert.That((float)heavy.Count / medium.Count, Is.InRange(2.8f, 3.2f));
        Assert.True(heavy.Any(DistrictTreeHarvest.CanFell));
        CollectionAssert.AreEqual(heavy.Select(JsonUtility.ToJson), RegionFloraGenerator.Generate(d, RegionClimate.Temperate, RegionTreeCoverage.Heavy, 193).Select(JsonUtility.ToJson));
        d.TreeCoverage = RegionTreeCoverage.Heavy; d.Flora = heavy;
        var saved = JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
        Assert.AreEqual(RegionTreeCoverage.Heavy, saved.TreeCoverage);
        Assert.AreEqual(heavy.Count, saved.Flora.Count);
        Assert.Throws<InvalidOperationException>(() => RegionFloraGenerator.Generate(d, RegionClimate.Desert, RegionTreeCoverage.Heavy, 193));
    }
    [Test] public void DesertRejectsForestAndWarmClimatesNeverUseSnowyArtwork()
    {
        Assert.Throws<InvalidOperationException>(() => RegionFloraGenerator.Generate(District(), RegionClimate.Desert, RegionTreeCoverage.Sparse, 1));
        Assert.Throws<InvalidOperationException>(() => RegionFloraGenerator.Generate(District(), RegionClimate.Desert, RegionTreeCoverage.Wooded, 1));
        Assert.True(RegionClimateRules.AllowsSnow(RegionClimate.Temperate));
        foreach (var climate in new[] { RegionClimate.Desert, RegionClimate.Tropical, RegionClimate.Mediterranean })
        {
            Assert.False(RegionClimateRules.AllowsSnow(climate));
            Assert.AreEqual("fraser-fir-large", RegionClimateRules.PresentationTree(climate, "fraser-fir-snowy"));
        }
        Assert.False(RegionClimateRules.AllowsTree(RegionClimate.Temperate, "date-palm"));
        Assert.True(RegionClimateRules.AllowsTree(RegionClimate.Mediterranean, "date-palm"));
        Assert.True(RegionClimateRules.AllowsTree(RegionClimate.Mediterranean, "maple"));
    }
    [Test] public void RegenerationRetainsPlantedAndHarvestedTreesAndDoesNotDuplicateIds()
    {
        var d = District(); var manual = new PlacedDistrictFlora { InstanceId = "manual", NormalizedX = .1f, NormalizedZ = .1f };
        d.Flora.Add(manual); d.Flora = RegionFloraGenerator.Generate(d, RegionClimate.Temperate, RegionTreeCoverage.Wooded, 17);
        var harvested = d.Flora.First(DistrictTreeHarvest.CanFell); DistrictTreeHarvest.Fell(harvested, 2);
        var before = JsonUtility.ToJson(d);
        var replaced = RegionFloraGenerator.Generate(d, RegionClimate.Temperate, RegionTreeCoverage.Wooded, 17);
        Assert.AreEqual(before, JsonUtility.ToJson(d), "Planning must not mutate the district");
        Assert.Contains(manual, replaced); Assert.Contains(harvested, replaced);
        Assert.AreEqual(replaced.Count, replaced.Select(t => t.InstanceId).Distinct().Count());
        Assert.AreEqual(2, RegionFloraGenerator.Generate(d, RegionClimate.Temperate, RegionTreeCoverage.None, 18).Count);
    }
    [Test] public void GenerationAvoidsRoadWaterLotsAndIndustrialBuildings()
    {
        var d = District();
        for (int z = 0; z < 64; z++) d.Roads.Add(new PlacedRoadPiece { GridX = 32, GridZ = z });
        d.Rivers.Add(new PlacedDistrictRiver { WidthMeters = 24, Points = new() { new() { X = 0, Z = .8f }, new() { X = 1, Z = .8f } } });
        var lot = new LotSaveData { LotWidthCells = 8, LotDepthCells = 6 };
        var placed = new PlacedDistrictLot { InstanceId = "lot", LotId = "fixture", GridX = 8, GridZ = 8 }; d.Lots.Add(placed);
        var quarry = new DistrictStoneSite { Built = true, NormalizedX = .7f, NormalizedZ = .2f }; d.StoneSites.Add(quarry);
        var works = new DistrictBrickworksSite { NormalizedX = .7f, NormalizedZ = .5f, Yaw = 90 }; d.Brickworks.Add(works);
        var trees = RegionFloraGenerator.Generate(d, RegionClimate.Temperate, RegionTreeCoverage.Wooded, 49, _ => lot);
        Assert.Greater(trees.Count, 25);
        var lotCenter = DistrictWorldController.DistrictLotCenterMeters(d, placed, lot);
        foreach (var tree in trees)
        {
            var p = DistrictLabor.TreePoint(d, tree);
            Assert.Greater(Mathf.Abs(p.x - 5), 9);
            Assert.Greater(Mathf.Abs(p.y - (.8f - .5f) * 640), 19);
            Assert.False(new Rect(lotCenter.x - 40, lotCenter.y - 30, 80, 60).Contains(p));
            Assert.Greater(Vector2.Distance(p, DistrictQuarry.Point(d, quarry)), 26);
            Assert.False(DistrictBrickworks.Contains(d, works, p, 4));
            if (ForestClusterCatalog.IsCluster(tree.FloraId))
            {
                float radius = ForestClusterCatalog.ClearanceMeters * tree.Scale;
                Assert.Greater(Mathf.Abs(p.x - 5), 9 + radius);
                Assert.Greater(Mathf.Abs(p.y - (.8f - .5f) * 640), 19 + radius);
                Assert.GreaterOrEqual(tree.NormalizedX * 640 - radius, 5);
                Assert.Less(tree.NormalizedX * 640 + radius, 635);
            }
        }
    }
    [Test] public void MixedForestUsesAllClustersAndSeparateHarvestableFirsWithFewerRecords()
    {
        var d = District(); d.Width = d.Height = 2;
        var trees = RegionFloraGenerator.Generate(d, RegionClimate.Temperate, RegionTreeCoverage.Wooded, 192);
        var clusters = trees.Where(t => ForestClusterCatalog.IsCluster(t.FloraId)).ToArray();
        Assert.AreEqual(5, clusters.Select(t => t.FloraId).Distinct().Count());
        Assert.Greater(clusters.Length, trees.Count * .65f);
        var firs = trees.Where(DistrictTreeHarvest.CanFell).ToArray();
        Assert.Greater(firs.Length, trees.Count * .12f);
        Assert.Less(firs.Length, trees.Count * .35f);
        Assert.Less(trees.Count, 750, "Old 24m wooded spacing produced about 2200 records here");
        foreach (var tree in clusters) Assert.False(DistrictTreeHarvest.Fell(tree, 0));
        var fir = firs[0]; Assert.True(DistrictTreeHarvest.Fell(fir, 0));
        Assert.AreEqual(DistrictTreeHarvest.PrototypeWoodYield, DistrictTreeHarvest.TakeWood(fir, 10000));
        Assert.AreEqual(DistrictTreeHarvestState.Stump, fir.HarvestState);
        var before = JsonUtility.ToJson(d); var undo = new DistrictUndoHistory(); undo.Reset(before);
        d.Flora = trees; undo.Commit(JsonUtility.ToJson(d));
        var copy = JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
        CollectionAssert.AreEqual(trees.Select(t => t.FloraId), copy.Flora.Select(t => t.FloraId));
        var index = DistrictHarvestIndex.For(copy);
        int count = copy.Flora.Count;
        Assert.True(index.RemoveFlora(clusters[0].InstanceId));
        Assert.AreEqual(count - 1, copy.Flora.Count);
        Assert.Null(index.Find(clusters[0].InstanceId));
        Assert.NotNull(index.Find(firs[1].InstanceId), "Clearing one cluster must retain the separate lumber fir");
        Assert.True(undo.TryUndo(out var restored)); Assert.AreEqual(before, restored);
        foreach (var id in clusters.Select(t => t.FloraId).Distinct())
        {
            var texture = Resources.Load<Texture2D>(LotWorldController.ResolveFloraResourcePath(id, SeasonPreset.Summer));
            Assert.NotNull(texture, id); Assert.Greater(texture.width, 1000);
            Assert.AreEqual(ForestClusterCatalog.Pivot, LotWorldController.FloraPivot(texture.name));
        }
    }
    [Test] public void IncompleteAndFailedGenerationLeaveRegionAndHarvestIndexIntact()
    {
        var d = District(); d.Flora.Add(new PlacedDistrictFlora { InstanceId = "keep", FloraId = "cilician-fir" });
        var region = new RegionSaveData { Tiles = new() { d, new RegionCityTile { TileId = "other", Width = 1, Height = 1 } } };
        var original = JsonUtility.ToJson(region); var index = DistrictHarvestIndex.For(d);
        var job = new RegionFloraGeneration(region, RegionTreeCoverage.Wooded, 2); job.Step();
        Assert.AreEqual(original, JsonUtility.ToJson(region));
        Assert.Throws<InvalidOperationException>(() => job.Commit(_ => Assert.Fail("Must not save an incomplete plan")));
        job.Step(); Assert.True(job.Ready);
        Assert.Throws<IOException>(() => job.Commit(_ => throw new IOException("save fixture")));
        Assert.AreEqual(original, JsonUtility.ToJson(region)); Assert.AreSame(index, DistrictHarvestIndex.For(d));
        job.Commit(_ => {});
        Assert.AreNotSame(index, DistrictHarvestIndex.For(d));
        Assert.AreEqual(d.Flora.Count, d.Flora.Count(t => DistrictHarvestIndex.For(d).Find(t.InstanceId) != null));
    }
    [Test] public void SavedClimateCoverageAndTreesRoundTripAndAtomicSaveReplacesExistingFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "flora-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            var region = new RegionSaveData { RegionId = "test", Tiles = new() { District() } };
            region.Terrain.Climate = RegionClimate.Mediterranean;
            region.Terrain.Streams = RegionWaterAmount.Many;
            RegionSaveStore.Save(region, root);
            var job = new RegionFloraGeneration(region, RegionTreeCoverage.Wooded, 19); job.Step();
            job.Commit(data => RegionSaveStore.Save(data, root));
            var copy = RegionSaveStore.Load("test", root);
            Assert.AreEqual(RegionClimate.Mediterranean, copy.Tiles[0].Climate);
            Assert.AreEqual(RegionTreeCoverage.Wooded, copy.Terrain.TreeCoverage);
            Assert.AreEqual(RegionWaterAmount.Many, copy.Terrain.Streams);
            Assert.AreEqual(region.Tiles[0].Flora.Count, copy.Tiles[0].Flora.Count);
            Assert.AreEqual(JsonUtility.ToJson(region), JsonUtility.ToJson(copy));
            Assert.AreEqual(1, Directory.GetFiles(root).Length);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
