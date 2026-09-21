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
    public void NetworkUsesRoundedCardinalStairsAndConnectedSegments(int seed)
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
                Assert.That(new Vector2(pair.a.X-pair.b.X,pair.a.Z-pair.b.Z).sqrMagnitude,Is.GreaterThan(.000000001f));
        }
        var trunk=paths[0];
        Assert.That(trunk.Points.Count,Is.GreaterThan(4));
        AssertRoundedTrunk(trunk);
    }
    [TestCase(RegionRiverFlow.WestToEast,DistrictRiverDirection.WestToEast)]
    [TestCase(RegionRiverFlow.EastToWest,DistrictRiverDirection.EastToWest)]
    [TestCase(RegionRiverFlow.NorthToSouth,DistrictRiverDirection.NorthToSouth)]
    [TestCase(RegionRiverFlow.SouthToNorth,DistrictRiverDirection.SouthToNorth)]
    public void ExplicitDirectionMatchesCoordinatesAndDistrictMetadata(RegionRiverFlow flow,DistrictRiverDirection expected)
    {
        var r=Region();var paths=RegionRiverGenerator.Generate(r,new(){DeepRivers=RegionWaterAmount.Few,Flow=flow},3);
        Assert.That(RegionRiverGenerator.DirectionOf(paths[0]),Is.EqualTo(expected));
        AssertRoundedTrunk(paths[0]);
        RegionRiverGenerator.Apply(r,paths);
        foreach(var section in r.Tiles.SelectMany(t=>t.Rivers).Where(p=>p.RegionRiverId==paths[0].Id))
        {
            Assert.That(section.Direction,Is.EqualTo(expected));
            foreach(var pair in section.Points.Zip(section.Points.Skip(1),(a,b)=>(a,b)))
                Assert.That(new Vector2(pair.a.X-pair.b.X,pair.a.Z-pair.b.Z).sqrMagnitude,Is.GreaterThan(.000000001f));
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

    static void AssertRoundedTrunk(RegionRiverPath path)
    {
        bool horizontal=false,vertical=false,curved=false,hasPrior=false;
        var prior=Vector2.zero;
        foreach(var pair in path.Points.Zip(path.Points.Skip(1),(a,b)=>(a,b)))
        {
            var delta=new Vector2(pair.b.X-pair.a.X,pair.b.Z-pair.a.Z);
            var x=Mathf.Abs(delta.x);var z=Mathf.Abs(delta.y);
            horizontal|=x>.000001f&&z<.000001f;
            vertical|=z>.000001f&&x<.000001f;
            curved|=x>.000001f&&z>.000001f;
            if(hasPrior)Assert.That(Vector2.Angle(prior,delta),Is.LessThan(50f));
            prior=delta;hasPrior=true;
        }
        Assert.That(horizontal&&vertical&&curved,Is.True);
    }
}
