using System;
using System.Linq;
using System.Collections.Generic;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
public class RegionRiverNetworkTests
{
    RegionSaveData Region()=>new RegionSaveData{Width=28,Height=20,Tiles=new List<RegionCityTile>{new(){TileId="south",Width=28,Height=10},new(){TileId="north",Y=10,Width=28,Height=10}}};
    [TestCase(1)][TestCase(1785)][TestCase(994)][TestCase(26)]
    public void NetworkUsesStraightCardinalConnectedSegments(int seed)
    {
        var r=Region();var settings=new RegionTerrainSettings{Streams=RegionWaterAmount.Few};
        var paths=RegionRiverGenerator.Generate(r,settings,seed);
        Assert.That(paths.Count,Is.EqualTo(4));
        for(int n=1;n<paths.Count;n++)
        {
            var end=paths[n].Points.Last();
            Assert.That(paths.Take(n).Any(parent=>parent.Points.Any(p=>Mathf.Abs(p.X-end.X)<.0001f&&Mathf.Abs(p.Z-end.Z)<.0001f)),Is.True,"branch must terminate on earlier river");
            Assert.That(paths[n].Points.Count,Is.GreaterThan(1));
        }
        var lengths=paths.Select(p=>p.Points.Zip(p.Points.Skip(1),(a,b)=>Vector2.Distance(new(a.X,a.Z),new(b.X,b.Z))).Sum()).ToArray();
        Assert.That(lengths.Min(),Is.GreaterThan(.05f));
        Assert.That(lengths.Max()-lengths.Min(),Is.GreaterThan(1));
        foreach(var path in paths)
        {
            foreach(var p in path.Points){Assert.That(p.X,Is.InRange(0,28));Assert.That(p.Z,Is.InRange(0,20));}
            foreach(var pair in path.Points.Zip(path.Points.Skip(1),(a,b)=>(a,b)))
                Assert.That(Mathf.Abs(pair.a.X-pair.b.X)<.000001f||
                    Mathf.Abs(pair.a.Z-pair.b.Z)<.000001f,Is.True,
                    "Every generated segment must follow one district-grid axis.");
        }
    }
    [TestCase(RegionRiverFlow.WestToEast,DistrictRiverDirection.WestToEast)]
    [TestCase(RegionRiverFlow.EastToWest,DistrictRiverDirection.EastToWest)]
    [TestCase(RegionRiverFlow.NorthToSouth,DistrictRiverDirection.NorthToSouth)]
    [TestCase(RegionRiverFlow.SouthToNorth,DistrictRiverDirection.SouthToNorth)]
    public void ExplicitDirectionMatchesCoordinatesAndDistrictMetadata(RegionRiverFlow flow,DistrictRiverDirection expected)
    {
        var r=Region();var paths=RegionRiverGenerator.Generate(r,new(){DeepRivers=RegionWaterAmount.Few,Flow=flow},3);
        Assert.That(RegionRiverGenerator.DirectionOf(paths[0]),Is.EqualTo(expected));
        foreach(var path in paths)foreach(var pair in path.Points.Zip(path.Points.Skip(1),(a,b)=>(a,b)))
            Assert.That(Mathf.Abs(pair.a.X-pair.b.X)<.000001f||Mathf.Abs(pair.a.Z-pair.b.Z)<.000001f,Is.True);
        RegionRiverGenerator.Apply(r,paths);
        foreach(var section in r.Tiles.SelectMany(t=>t.Rivers).Where(p=>p.RegionRiverId==paths[0].Id))
        {
            Assert.That(section.Direction,Is.EqualTo(expected));
            foreach(var pair in section.Points.Zip(section.Points.Skip(1),(a,b)=>(a,b)))
                Assert.That(Mathf.Abs(pair.a.X-pair.b.X)<.000001f||Mathf.Abs(pair.a.Z-pair.b.Z)<.000001f,Is.True);
        }
    }
    [Test] public void BorderClippingDoesNotRotateTheRiver()
    {
        var r=Region();var paths=RegionRiverGenerator.Generate(r,new(){DeepRivers=RegionWaterAmount.Many,Flow=RegionRiverFlow.SouthToNorth},42);
        RegionRiverGenerator.Apply(r,paths);
        int crossings=0;
        foreach(var lower in r.Tiles[0].Rivers)foreach(var point in lower.Points.Where(p=>Mathf.Abs(p.Z-1)<.00001f))
        {
            var matches=r.Tiles[1].Rivers.Where(p=>p.RegionRiverId==lower.RegionRiverId).SelectMany(p=>p.Points).Where(p=>Mathf.Abs(p.Z)<.00001f);
            Assert.That(matches.Any(p=>Mathf.Abs(p.X-point.X)<.00001f),Is.True);crossings++;
        }
        Assert.That(crossings,Is.GreaterThan(0));
    }
    [Test] public void EmptyDistrictLightingDoesNotInheritPreviousAmbient()
    {
        var go=new GameObject("environment-test");var light=go.AddComponent<Light>();
        var mode=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;var ambient=RenderSettings.ambientLight;
        try
        {
            RenderSettings.ambientSkyColor=Color.red;
            DistrictWorldController.ApplyRegionEnvironment(TimeOfDayPreset.Noon,light);
            Assert.That(light.intensity,Is.EqualTo(.64f));Assert.That(RenderSettings.ambientSkyColor.r,Is.EqualTo(.42f).Within(.001f));
            var expected=RenderSettings.ambientSkyColor;
            DistrictWorldController.ApplyRegionEnvironment(TimeOfDayPreset.Night,light);
            DistrictWorldController.ApplyRegionEnvironment(TimeOfDayPreset.Noon,light);
            Assert.That(RenderSettings.ambientSkyColor,Is.EqualTo(expected));
        }
        finally{UnityEngine.Object.DestroyImmediate(go);RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=ground;}
    }
}
