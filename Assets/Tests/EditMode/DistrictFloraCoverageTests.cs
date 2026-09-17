using System;
using System.IO;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class DistrictFloraCoverageTests
{
    static RegionSaveData Region()
    {
        var a = new RegionCityTile { TileId = "a", Name = "A", Width = 1, Height = 1 };
        var b = new RegionCityTile { TileId = "b", Name = "B", Width = 1, Height = 1, TreeCoverage = RegionTreeCoverage.Sparse, FloraSeed = 19 };
        a.Flora.Add(new PlacedDistrictFlora { InstanceId = "manual-a", FloraId = "cilician-fir" });
        b.Flora.Add(new PlacedDistrictFlora { InstanceId = "manual-b", FloraId = "cilician-fir" });
        return new RegionSaveData { RegionId = "scoped-test", Tiles = new() { a, b },
            Terrain = new RegionTerrainSettings { Climate = RegionClimate.Mediterranean, TreeCoverage = RegionTreeCoverage.Sparse, FloraSeed = 71 } };
    }
    [Test] public void DistrictGenerationChangesOnlyItsTargetAndInvalidatesOnlyItsIndex()
    {
        var r = Region(); var a = r.Tiles[0]; var b = r.Tiles[1];
        var neighbor = JsonUtility.ToJson(b); var settings = r.Terrain;
        var firstIndex = DistrictHarvestIndex.For(a); var secondIndex = DistrictHarvestIndex.For(b);
        var job = new RegionFloraGeneration(r, RegionTreeCoverage.Wooded, 83, a);
        job.Step(); Assert.True(job.Ready); job.Commit(_ => {});
        Assert.Greater(a.Flora.Count, 75); Assert.AreEqual(RegionTreeCoverage.Wooded, a.TreeCoverage); Assert.AreEqual(83, a.FloraSeed);
        Assert.AreEqual(neighbor, JsonUtility.ToJson(b)); Assert.AreSame(settings, r.Terrain);
        Assert.AreEqual(71, r.Terrain.FloraSeed); Assert.AreEqual(RegionTreeCoverage.Sparse, r.Terrain.TreeCoverage);
        Assert.AreNotSame(firstIndex, DistrictHarvestIndex.For(a)); Assert.AreSame(secondIndex, DistrictHarvestIndex.For(b));
    }
    [Test] public void FailedDistrictSaveRestoresTreesMetadataAndNeighbor()
    {
        var r = Region(); var before = JsonUtility.ToJson(r); var original = r.Tiles[0].Flora;
        var job = new RegionFloraGeneration(r, RegionTreeCoverage.Wooded, 83, r.Tiles[0]); job.Step();
        Assert.Throws<IOException>(() => job.Commit(_ => throw new IOException("fixture")));
        Assert.AreEqual(before, JsonUtility.ToJson(r)); Assert.AreSame(original, r.Tiles[0].Flora);
    }
    [Test] public void DistrictCoverageSurvivesReloadAndUndoRestoresPreviousCoverage()
    {
        var r = Region(); var a = r.Tiles[0]; var before = JsonUtility.ToJson(a);
        var undo = new DistrictUndoHistory(); undo.Reset(before);
        var job = new RegionFloraGeneration(r, RegionTreeCoverage.Wooded, 83, a); job.Step(); job.Commit(_ => {});
        undo.Commit(JsonUtility.ToJson(a));
        var loaded = JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(r));
        Assert.AreEqual(RegionTreeCoverage.Wooded, loaded.Tiles[0].TreeCoverage); Assert.AreEqual(83, loaded.Tiles[0].FloraSeed);
        Assert.AreEqual(a.Flora.Count, loaded.Tiles[0].Flora.Count);
        Assert.True(undo.TryUndo(out var snapshot)); JsonUtility.FromJsonOverwrite(snapshot, a);
        Assert.AreEqual(before, JsonUtility.ToJson(a)); Assert.AreEqual(RegionTreeCoverage.Sparse, r.Tiles[1].TreeCoverage);
    }
    [Test] public void DistrictScopeRejectsForeignDistrictAndDesertCoverage()
    {
        var r = Region();
        Assert.Throws<ArgumentException>(() => new RegionFloraGeneration(r, RegionTreeCoverage.Wooded, 1, new RegionCityTile()));
        r.Terrain.Climate = RegionClimate.Desert;
        Assert.Throws<InvalidOperationException>(() => new RegionFloraGeneration(r, RegionTreeCoverage.Sparse, 1, r.Tiles[0]));
    }
    [Test] public void RegionalGenerationUpdatesEachDistrictCoverageMetadata()
    {
        var r = Region(); var job = new RegionFloraGeneration(r, RegionTreeCoverage.Wooded, 92);
        while (!job.Ready) job.Step(); job.Commit(_ => {});
        foreach (var d in r.Tiles) { Assert.AreEqual(RegionTreeCoverage.Wooded, d.TreeCoverage); Assert.AreEqual(92, d.FloraSeed); }
        Assert.AreEqual(RegionTreeCoverage.Wooded, r.Terrain.TreeCoverage);
    }
}
