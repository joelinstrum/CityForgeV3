using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
public class RegionPikeStrokeTests
{
    [Test] public void ClickAndTinyDragCannotCreateRoad()
    {
        var stroke=new RegionPikeStroke(12,8);stroke.Add(new(2,3));stroke.Add(new(2.01f,3),true);
        Assert.False(stroke.IsValid);Assert.IsNull(stroke.CreateRoute("Pike"));
    }
    [Test] public void DrawnRoadClampsToRegionAndPersistsNameAndIndependentGeometry()
    {
        var stroke=new RegionPikeStroke(12,8);stroke.Add(new(-2,3));stroke.Add(new(5,4));stroke.Add(new(20,9),true);
        Assert.IsNull(stroke.CreateRoute("  "));
        var route=stroke.CreateRoute("  National Road  ");
        Assert.AreEqual("National Road",route.Name);Assert.AreEqual(new Vector2(0,3),route.Points[0]);Assert.AreEqual(new Vector2(12,8),route.Points[2]);
        stroke.Points.Clear();Assert.AreEqual(3,route.Points.Count);
        var region=new RegionSaveData();region.TransportRoutes.Add(route);
        var loaded=JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(region));
        Assert.AreEqual(route.Name,loaded.TransportRoutes[0].Name);Assert.AreEqual(route.Id,loaded.TransportRoutes[0].Id);
        CollectionAssert.AreEqual(route.Points,loaded.TransportRoutes[0].Points);
    }
    [Test] public void LongStrokeRemainsBoundedAndKeepsEndpoints()
    {
        var stroke=new RegionPikeStroke(12,8);stroke.Add(Vector2.zero);
        for(int i=0;i<10000;i++)stroke.Add(i%2==0?new Vector2(3,2):new Vector2(4,3));
        stroke.Add(new Vector2(12,8),true);
        Assert.LessOrEqual(stroke.Points.Count,4096);Assert.AreEqual(Vector2.zero,stroke.Points[0]);Assert.AreEqual(new Vector2(12,8),stroke.Points[stroke.Points.Count-1]);
    }
}
