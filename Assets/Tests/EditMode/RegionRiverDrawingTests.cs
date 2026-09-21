using System.Collections.Generic;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
public class RegionRiverDrawingTests
{
    static RegionPikeStroke Stroke()
    {
        var stroke=new RegionPikeStroke(4,2);stroke.Add(new(.2f,.5f));stroke.Add(new(3.8f,.6f),true);return stroke;
    }
    [Test] public void RiverSizesHaveExpectedWidthsAndDepthsAndCopyTheirDraft()
    {
        var stroke=Stroke();var major=RegionRiverDrawing.Create(stroke,RegionRiverSize.Large);var small=RegionRiverDrawing.Create(stroke,RegionRiverSize.Small);
        Assert.AreEqual(DistrictRiverDepth.Deep,major.Depth);Assert.AreEqual(DistrictRiverDepth.Shallow,small.Depth);
        var widest=RegionRiverDrawing.Create(stroke,RegionRiverSize.Major);
        Assert.AreEqual(128,widest.WidthMeters);Assert.AreEqual(major.WidthMeters*2,widest.WidthMeters);
        Assert.AreEqual(DistrictRiverDepth.Deep,widest.Depth);Assert.AreEqual(18,small.WidthMeters);
        Assert.Greater(major.WidthMeters,small.WidthMeters);Assert.True(major.HandDrawn);
        stroke.Points.Clear();Assert.AreEqual(2,major.Points.Count);
        Assert.IsNull(RegionRiverDrawing.Create(stroke,RegionRiverSize.Large));
    }
    [Test] public void AuthoringMarkersDistinguishMajorMediumAndStreamWithoutChangingSavedWidths()
    {
        Assert.AreEqual(28f,RegionRiverDrawing.MarkerWidthPoints(RegionRiverSize.Major));
        Assert.AreEqual(7f,RegionRiverDrawing.MarkerWidthPoints(RegionRiverSize.Large));
        Assert.AreEqual(2f,RegionRiverDrawing.MarkerWidthPoints(RegionRiverSize.Small));
        Assert.AreEqual("MAJOR",RegionRiverDrawing.DisplayName(RegionRiverSize.Major));
        Assert.AreEqual("MEDIUM",RegionRiverDrawing.DisplayName(RegionRiverSize.Large));
        Assert.AreEqual("STREAM",RegionRiverDrawing.DisplayName(RegionRiverSize.Small));
        Assert.AreEqual(128f,RegionRiverDrawing.WidthMeters(RegionRiverSize.Major));
        Assert.AreEqual(64f,RegionRiverDrawing.WidthMeters(RegionRiverSize.Large));
        Assert.AreEqual(18f,RegionRiverDrawing.WidthMeters(RegionRiverSize.Small));
    }
    [Test] public void RiverInputRejectsJitterAndBacktrackingButKeepsBroadBends()
    {
        var stroke=new RegionPikeStroke(12,8);
        void Add(float x,float y) => RegionRiverDrawing.AddPoint(stroke,RegionRiverSize.Major,new(x,y));
        Add(1,1);Add(2,1);Add(2.02f,1.01f);Add(1.8f,1.05f);
        Assert.AreEqual(2,stroke.Points.Count,"Jitter and a backward stroke should not widen the channel");
        Add(3,1.3f);Add(4,2);Add(4.5f,3);
        Assert.AreEqual(5,stroke.Points.Count,"Broad bends should remain drawable");
        var preview=RegionRiverDrawing.Smooth(stroke.Points);
        var path=RegionRiverDrawing.Create(stroke,RegionRiverSize.Major);
        Assert.AreEqual(preview.Count,path.Points.Count);
        for(int i=0;i<preview.Count;i++)Assert.AreEqual(preview[i],new Vector2(path.Points[i].X,path.Points[i].Z));
        Assert.AreEqual(stroke.Points[0],preview[0]);
        Assert.AreEqual(stroke.Points[stroke.Points.Count-1],preview[preview.Count-1]);
        Assert.AreEqual(128,path.WidthMeters);
    }
    [Test] public void RiverInputRejectsSelfCrossingEvenWithSparsePointerSamples()
    {
        var stroke=new RegionPikeStroke(12,8);
        // A broad looping approach is allowed until it would cross its own channel.
        foreach(var point in new[]{new Vector2(1,2),new Vector2(3,2),new Vector2(4,3),new Vector2(4,4),new Vector2(3,5),new Vector2(2,5),new Vector2(1.5f,4)})
            RegionRiverDrawing.AddPoint(stroke,RegionRiverSize.Major,point);
        int count=stroke.Points.Count;
        Assert.AreEqual(7,count);
        RegionRiverDrawing.AddPoint(stroke,RegionRiverSize.Major,new Vector2(1.5f,1),true);
        Assert.AreEqual(count,stroke.Points.Count,"A loop crossing the previous channel should be ignored");
    }
    [Test] public void ConnectingStrokeSnapsBothEndsAndKeepsSelectedWidth()
    {
        var region=new RegionSaveData { Width=4,Height=4,Tiles=new(){new(){X=0,Y=0,Width=4,Height=4,
            Rivers=new(){new(){WidthMeters=64,Points=new(){new(0,.25f),new(1,.25f)}},
                new(){WidthMeters=128,Points=new(){new(0,.75f),new(1,.75f)}}}}}};
        var stroke=new RegionPikeStroke(4,4);stroke.Add(new(2,1.04f));stroke.Add(new(2,2));stroke.Add(new(2,2.95f),true);
        var preview=RegionRiverDrawing.Preview(stroke,RegionRiverSize.Small,region);
        var path=RegionRiverDrawing.Create(stroke,RegionRiverSize.Small,region);
        Assert.AreEqual(new Vector2(2,1),preview[0]);Assert.AreEqual(new Vector2(2,3),preview[preview.Count-1]);
        Assert.AreEqual(18,path.WidthMeters);Assert.AreEqual(preview.Count,path.Points.Count);
        for(int i=0;i<preview.Count;i++)Assert.AreEqual(preview[i],new Vector2(path.Points[i].X,path.Points[i].Z));
        Assert.AreEqual(new Vector2(2,2),RegionRiverDrawing.SnapToRiver(new(2,2),RegionRiverSize.Small,region),"Distant rivers must not attract the pencil");
        Assert.AreEqual(1.04f,stroke.Points[0].y,"Preview must not mutate the raw stroke");
    }
    [Test] public void DrawingCrossesDistrictBoundaryWithoutGapsAndSurvivesRegenerationAndReload()
    {
        var region=new RegionSaveData{Width=4,Height=2,Tiles=new(){new(){TileId="a",X=0,Y=0,Width=2,Height=2},new(){TileId="b",X=2,Y=0,Width=2,Height=2}}};
        var manual=RegionRiverDrawing.Create(Stroke(),RegionRiverSize.Major);
        RegionRiverGenerator.Apply(region,new(){manual});
        var a=region.Tiles[0].Rivers[0];var b=region.Tiles[1].Rivers[0];
        Assert.AreEqual(1,a.Points[a.Points.Count-1].X,.00001f);Assert.AreEqual(0,b.Points[0].X,.00001f);
        Assert.AreEqual(a.Points[a.Points.Count-1].Z,b.Points[0].Z,.00001f);
        RegionRiverGenerator.Apply(region,new List<RegionRiverPath>());
        Assert.AreEqual(manual.Id,region.RiverPaths[0].Id);Assert.AreEqual(1,region.Tiles[0].Rivers.Count);
        RegionRiverGenerator.Apply(region,region.RiverPaths);
        Assert.AreEqual(1,region.Tiles[0].Rivers.Count,"Duplicate manual section");
        var loaded=JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(region));
        Assert.AreEqual(128,loaded.Tiles[1].Rivers[0].WidthMeters);
        Assert.True(loaded.RiverPaths[0].HandDrawn);Assert.AreEqual(DistrictRiverDepth.Deep,loaded.Tiles[1].Rivers[0].Depth);
    }
}
