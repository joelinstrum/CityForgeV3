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
    public void NetworkUsesNaturalGridOrientedRivers(int seed)
    {
        var r=Region();var settings=new RegionTerrainSettings{Streams=RegionWaterAmount.Few};
        var paths=RegionRiverGenerator.Generate(r,settings,seed);
        Assert.That(paths.Count,Is.EqualTo(2));
        CollectionAssert.AreEquivalent(new[]{DistrictRiverDirection.WestToEast,
            DistrictRiverDirection.NorthToSouth},paths.Select(RegionRiverGenerator.DirectionOf));
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
        AssertNaturalTrunk(trunk);
    }
    [TestCase(RegionRiverFlow.WestToEast)]
    [TestCase(RegionRiverFlow.EastToWest)]
    [TestCase(RegionRiverFlow.NorthToSouth)]
    [TestCase(RegionRiverFlow.SouthToNorth)]
    public void LegacyFlowChoiceDoesNotOverrideFixedDirections(RegionRiverFlow flow)
    {
        var r=Region();var paths=RegionRiverGenerator.Generate(r,new(){DeepRivers=RegionWaterAmount.Few,Streams=RegionWaterAmount.Few,Flow=flow},3);
        Assert.That(paths.Select(RegionRiverGenerator.DirectionOf).Distinct().Count(),Is.EqualTo(2));
        Assert.That(paths.All(path=>RegionRiverGenerator.DirectionOf(path) is
            DistrictRiverDirection.WestToEast or DistrictRiverDirection.NorthToSouth),Is.True);
        Assert.That(paths.Count(path=>path.Depth==DistrictRiverDepth.Deep),Is.EqualTo(1));
        Assert.That(paths[0].WidthMeters,Is.InRange(144f,228f));
        AssertNaturalTrunk(paths[0]);
        RegionRiverGenerator.Apply(r,paths);
        foreach(var path in paths)foreach(var section in r.Tiles.SelectMany(t=>t.Rivers).Where(p=>p.RegionRiverId==path.Id))
        {
            Assert.That(section.Direction,Is.EqualTo(RegionRiverGenerator.DirectionOf(path)));
            foreach(var pair in section.Points.Zip(section.Points.Skip(1),(a,b)=>(a,b)))
                Assert.That(new Vector2(pair.a.X-pair.b.X,pair.a.Z-pair.b.Z).sqrMagnitude,Is.GreaterThan(.000000001f));
        }
    }
    [Test] public void BorderClippingDoesNotRotateTheRiver()
    {
        var r=Region();var paths=RegionRiverGenerator.Generate(r,new(){DeepRivers=RegionWaterAmount.Few,Streams=RegionWaterAmount.Few},42);
        var vertical=paths.Single(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.NorthToSouth);
        RegionRiverGenerator.Apply(r,paths);
        int crossings=0;
        foreach(var lower in r.Tiles[0].Rivers.Where(path=>path.RegionRiverId==vertical.Id))foreach(var point in lower.Points.Where(p=>Mathf.Abs(p.Z-1)<.00001f))
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

    static void AssertNaturalTrunk(RegionRiverPath path)
    {
        bool curved=false,hasPrior=false;
        var changes=0;
        var prior=Vector2.zero;
        foreach(var pair in path.Points.Zip(path.Points.Skip(1),(a,b)=>(a,b)))
        {
            var delta=new Vector2(pair.b.X-pair.a.X,pair.b.Z-pair.a.Z);
            var x=Mathf.Abs(delta.x);var z=Mathf.Abs(delta.y);
            curved|=x>.000001f&&z>.000001f;
            if(hasPrior)
            {
                var angle=Vector2.Angle(prior,delta);
                Assert.That(angle,Is.LessThan(50f));
                if(angle>1f)changes++;
            }
            prior=delta;hasPrior=true;
        }
        Assert.That(curved,Is.True);
        Assert.That(changes,Is.GreaterThanOrEqualTo(4));
    }
}
