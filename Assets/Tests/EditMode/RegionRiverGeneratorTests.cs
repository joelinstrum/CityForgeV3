using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class RegionRiverGeneratorTests
{
    static RegionSaveData Region() => new RegionSaveData { Width=8,Height=8,Tiles=new List<RegionCityTile> {
        new RegionCityTile{TileId="south",X=0,Y=0,Width=8,Height=4},
        new RegionCityTile{TileId="north",X=0,Y=4,Width=8,Height=4}}};
    [Test] public void CountsSeedAndStreamJunctionsAreDeterministic()
    {
        var r=Region();var settings=new RegionTerrainSettings{DeepRivers=RegionWaterAmount.Few,Streams=RegionWaterAmount.Many};
        var paths=RegionRiverGenerator.Generate(r,settings,42);
        Assert.That(paths.Count,Is.EqualTo(12));
        RegionRiverGenerator.Apply(r,paths);var before=JsonUtility.ToJson(r);
        RegionRiverGenerator.Apply(r,RegionRiverGenerator.Generate(r,settings,42));
        Assert.That(JsonUtility.ToJson(r),Is.EqualTo(before));
        foreach(var stream in paths.Skip(2))
        {
            var end=stream.Points.Last();
            Assert.That(paths.Where(main=>main!=stream).Any(main=>main.Points.Any(p=>Mathf.Abs(p.X-end.X)<.00001f&&Mathf.Abs(p.Z-end.Z)<.00001f)),Is.True);
        }
        Assert.That(RegionRiverGenerator.Generate(r,new RegionTerrainSettings{DeepRivers=RegionWaterAmount.Many,Streams=RegionWaterAmount.Few},42).Count,Is.EqualTo(9));
    }
    [Test] public void SharedDistrictBoundaryMatchesAndReloadPreservesPaths()
    {
        var r=Region();var paths=RegionRiverGenerator.Generate(r,new RegionTerrainSettings{DeepRivers=RegionWaterAmount.Many},7);
        RegionRiverGenerator.Apply(r,paths);
        foreach(var tile in r.Tiles)foreach(var section in tile.Rivers)
        {
            Assert.That(section.Points.Count,Is.GreaterThan(1));
            foreach(var point in section.Points)
            {
                Assert.That(point.X,Is.InRange(-.00001f,1.00001f));
                Assert.That(point.Z,Is.InRange(-.00001f,1.00001f));
            }
        }
        var loaded=JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(r));
        Assert.That(JsonUtility.ToJson(loaded),Is.EqualTo(JsonUtility.ToJson(r)));
    }
    [Test] public void RegenerationAndNonePreserveManualRiversAndOtherFeatures()
    {
        var r=Region();var manual=new PlacedDistrictRiver{InstanceId="manual"};r.Tiles[0].Rivers.Add(manual);
        r.Tiles[0].Lots.Add(new PlacedDistrictLot{InstanceId="building"});
        RegionRiverGenerator.Apply(r,RegionRiverGenerator.Generate(r,new RegionTerrainSettings{DeepRivers=RegionWaterAmount.Few},1));
        RegionRiverGenerator.Apply(r,new List<RegionRiverPath>());
        Assert.That(r.Tiles[0].Rivers,Has.Count.EqualTo(1));Assert.That(r.Tiles[0].Rivers[0],Is.SameAs(manual));
        Assert.That(r.Tiles[0].Lots[0].InstanceId,Is.EqualTo("building"));Assert.That(r.Tiles[1].Rivers,Is.Empty);
    }
    [Test] public void ChangingDistrictBordersReclipsTheSameRegionalPaths()
    {
        var r=Region();RegionRiverGenerator.Apply(r,RegionRiverGenerator.Generate(r,new RegionTerrainSettings{DeepRivers=RegionWaterAmount.Few},3));
        var original=r.RiverPaths;
        RegionSaveStore.RegenerateTiles(r);
        Assert.That(r.RiverPaths,Is.SameAs(original));
        foreach(var tile in r.Tiles)
            Assert.That(tile.Rivers.Count,Is.EqualTo(RegionRiverGenerator.Sections(tile,original).Count));
        Assert.That(r.Tiles.Sum(t=>t.Rivers.Count),Is.GreaterThan(0));
    }
    [Test] public void RegionalSectionCannotBeMovedIndependently()
    {
        var river = new PlacedDistrictRiver { RegionRiverId="shared", Points=new List<DistrictRiverPoint>{new(.2f,.2f),new(.3f,.3f)} };
        Assert.That(DistrictRiverEditing.Move(river,new Vector2(.1f,.1f)),Is.False);
        Assert.That(river.Points[0].X,Is.EqualTo(.2f));
        river.RegionRiverId="";
        Assert.That(DistrictRiverEditing.Move(river,new Vector2(.1f,.1f)),Is.True);
    }
    [Test] public void ClipHandlesUnequalTilesReentryAndOutsideSegments()
    {
        var path=new RegionRiverPath{Id="zigzag",Points=new List<DistrictRiverPoint>{new(-1,1),new(2,1),new(4,1),new(2,2),new(-1,2)}};
        var tile=new RegionCityTile{TileId="tile",Width=3,Height=4};
        var sections=RegionRiverGenerator.Sections(tile,new[]{path});
        Assert.That(sections.Count,Is.EqualTo(2));
        foreach(var section in sections)foreach(var p in section.Points){Assert.That(p.X,Is.InRange(0f,1f));Assert.That(p.Z,Is.InRange(0f,1f));}
    }
}
