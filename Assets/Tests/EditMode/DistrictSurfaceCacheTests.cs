using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
public class DistrictSurfaceCacheTests
{
    [Test] public void DirtyMatrixCoalescesOverlappingChangesAndDrainsOnce()
    {
        var grid=new DistrictDirtyGrid(new Rect(-256,-256,512,512),128);
        grid.Mark(new Rect(-200,-200,20,20));grid.Mark(new Rect(-190,-190,20,20));
        var cells=grid.Consume();Assert.AreEqual(1,cells.Count);Assert.AreEqual(new Vector2Int(0,0),cells[0]);Assert.IsEmpty(grid.Consume());
        grid.Mark(new Rect(1000,1000,5,5));Assert.IsEmpty(grid.Consume());
        grid.Mark(new Rect(250,250,20,20));Assert.AreEqual(new Vector2Int(3,3),grid.Consume().Single());
    }
    static RegionCityTile District()=>new(){Width=2,Height=2,TileId="cache",Hills=new(){HeightMeters=45,Coverage=.8f},
        Rivers=new(){new(){WidthMeters=64,Points=new(){new(.3f,.4f),new(.5f,.5f),new(.7f,.4f)}}}};
    [Test] public void ValueSnapshotsDetectInPlaceEditsAndIgnoreUnrelatedState()
    {
        var d=District();var cache=new DistrictSurfaceCache();Assert.IsTrue(cache.Update(d).Full);int revision=cache.Revision;
        d.Treasury++;Assert.IsFalse(cache.Update(d).Any);Assert.AreEqual(revision,cache.Revision);
        d.Rivers[0].Points[1].Z=.6f;var changed=cache.Update(d);Assert.IsTrue(changed.Any);Assert.IsFalse(changed.Full);
        Assert.AreEqual(revision+1,cache.Revision);Assert.IsFalse(cache.Update(d).Any);
        d.Roads.Add(new(){GridX=3,GridZ=4});Assert.IsTrue(cache.Update(d).Any);
        d.Roads.Clear();Assert.IsTrue(cache.Update(d).Any);
        d.Hills.Seed++;Assert.IsTrue(cache.Update(d).Full);
    }
    [Test] public void IncrementalRiverAndRoadHeightsEqualFullRebuild()
    {
        var d=District();var cache=new DistrictSurfaceCache();cache.Update(d);var elevation=new DistrictElevation(d);
        d.Rivers[0].Points[1].Z=.55f;var changes=cache.Update(d);
        elevation.RefreshLocal(d,changes.Areas);var rebuilt=new DistrictElevation(d);
        CollectionAssert.AreEqual(rebuilt.Heights,elevation.Heights);
        Assert.Less(elevation.LastUpdatedSampleCount,elevation.Heights.Length);
        d.Roads.Add(new(){GridX=50,GridZ=60});changes=cache.Update(d);elevation.RefreshLocal(d,changes.Areas);
        CollectionAssert.AreEqual(new DistrictElevation(d).Heights,elevation.Heights);
        d.Roads.Clear();changes=cache.Update(d);elevation.RefreshLocal(d,changes.Areas);
        CollectionAssert.AreEqual(new DistrictElevation(d).Heights,elevation.Heights);
        d.Rivers.Clear();changes=cache.Update(d);elevation.RefreshLocal(d,changes.Areas);
        CollectionAssert.AreEqual(new DistrictElevation(d).Heights,elevation.Heights);
    }
}
