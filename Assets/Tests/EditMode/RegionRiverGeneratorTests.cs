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
    [Test] public void CountsWidthsDirectionsAndSeedAreDeterministic()
    {
        var r=Region();var settings=new RegionTerrainSettings{DeepRivers=RegionWaterAmount.Few,Streams=RegionWaterAmount.Many};
        var paths=RegionRiverGenerator.Generate(r,settings,42);
        Assert.That(paths.Count,Is.EqualTo(6));
        Assert.That(paths.Count(path=>path.Depth==DistrictRiverDepth.Deep),Is.EqualTo(1));
        Assert.That(paths.Single(path=>path.Depth==DistrictRiverDepth.Deep).WidthMeters,Is.InRange(144f,228f));
        Assert.That(paths.All(path=>RegionRiverGenerator.DirectionOf(path) is
            DistrictRiverDirection.WestToEast or DistrictRiverDirection.NorthToSouth),Is.True);
        Assert.That(paths.Any(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.WestToEast),Is.True);
        Assert.That(paths.Any(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.NorthToSouth),Is.True);
        AssertSeparated(paths.Where(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.WestToEast),true);
        AssertSeparated(paths.Where(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.NorthToSouth),false);
        RegionRiverGenerator.Apply(r,paths);var before=JsonUtility.ToJson(r);
        RegionRiverGenerator.Apply(r,RegionRiverGenerator.Generate(r,settings,42));
        Assert.That(JsonUtility.ToJson(r),Is.EqualTo(before));
        var legacyMany=RegionRiverGenerator.Generate(r,new RegionTerrainSettings{DeepRivers=RegionWaterAmount.Many,Streams=RegionWaterAmount.Few},42);
        Assert.That(legacyMany.Count,Is.EqualTo(3));
        Assert.That(legacyMany.Count(path=>path.Depth==DistrictRiverDepth.Deep),Is.EqualTo(1));
    }
    [Test] public void ThreeRiversUseSeparateIrregularEnvelopes()
    {
        var r=Region();var paths=RegionRiverGenerator.Generate(r,new RegionTerrainSettings{DeepRivers=RegionWaterAmount.Few,Streams=RegionWaterAmount.Few},1785);
        Assert.That(paths.Count,Is.EqualTo(3));
        Assert.That(paths.Any(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.WestToEast),Is.True);
        Assert.That(paths.Any(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.NorthToSouth),Is.True);
        AssertSeparated(paths.Where(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.WestToEast),true);
        AssertSeparated(paths.Where(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.NorthToSouth),false);
    }
    [Test] public void FourSmallRiversUseBothCardinalOrientations()
    {
        var settings=new RegionTerrainSettings{RiverCountsVersion=1,SmallRiverCount=4};
        var paths=RegionRiverGenerator.Generate(Region(),settings,91);
        Assert.That(paths,Has.Count.EqualTo(4));
        Assert.That(paths.All(path=>path.GeneratedSize==GeneratedRegionRiverSize.Small),Is.True);
        Assert.That(paths.All(path=>path.WidthMeters>=24&&path.WidthMeters<=36),Is.True);
        Assert.That(paths.Any(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.WestToEast),Is.True);
        Assert.That(paths.Any(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.NorthToSouth),Is.True);
    }
    [Test] public void ExplicitCountsCreateEveryRequestedRiverSize()
    {
        var settings=new RegionTerrainSettings{RiverCountsVersion=1,
            DeepRivers=RegionWaterAmount.Few,MediumRiverCount=3,
            SmallRiverCount=4,StreamCount=5};
        var paths=RegionRiverGenerator.Generate(Region(),settings,121);
        Assert.That(paths,Has.Count.EqualTo(13));
        Assert.That(paths.Count(path=>path.GeneratedSize==GeneratedRegionRiverSize.Major),Is.EqualTo(1));
        Assert.That(paths.Count(path=>path.GeneratedSize==GeneratedRegionRiverSize.Medium),Is.EqualTo(3));
        Assert.That(paths.Count(path=>path.GeneratedSize==GeneratedRegionRiverSize.Small),Is.EqualTo(4));
        Assert.That(paths.Count(path=>path.GeneratedSize==GeneratedRegionRiverSize.Stream),Is.EqualTo(5));
        Assert.That(paths.Any(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.WestToEast),Is.True);
        Assert.That(paths.Any(path=>RegionRiverGenerator.DirectionOf(path)==DistrictRiverDirection.NorthToSouth),Is.True);
    }
    [Test] public void OccupiedDistrictCentersAreNotPreferredRouteTargets()
    {
        var r=Region();
        r.Tiles[0].Lots.Add(new PlacedDistrictLot{InstanceId="occupied"});
        var paths=RegionRiverGenerator.Generate(r,new RegionTerrainSettings{
            RiverCountsVersion=1,SmallRiverCount=1},19);
        Assert.That(paths.Single().Points.Any(point=>
            Mathf.Abs(point.X-4)<.0001f&&Mathf.Abs(point.Z-2)<.0001f),Is.False);
    }
    [Test] public void SmallRiverStraightRunsAreBoundedWithoutForcedDistrictTurns()
    {
        var r=RegionSaveStore.Create("Meander coverage",28,20);
        var paths=RegionRiverGenerator.Generate(r,new RegionTerrainSettings{
            RiverCountsVersion=1,SmallRiverCount=4},7721);
        var foundStraightDistrict=false;
        foreach(var path in paths)
        foreach(var pair in path.Points.Zip(path.Points.Skip(1),(a,b)=>(a,b)))
        {
            var delta=new Vector2(pair.b.X-pair.a.X,pair.b.Z-pair.a.Z);
            if(Mathf.Abs(delta.x)<.0001f||Mathf.Abs(delta.y)<.0001f)
                Assert.That(delta.magnitude,Is.LessThanOrEqualTo(4.21f));
        }
        foreach(var tile in r.Tiles)
        foreach(var section in RegionRiverGenerator.Sections(tile,paths))
        {
            var horizontal=section.Direction==DistrictRiverDirection.WestToEast;
            var first=section.Points[0];var last=section.Points[^1];
            var full=horizontal?
                first.X<.001f&&last.X>.999f:
                first.Z>.999f&&last.Z<.001f;
            if(!full)continue;
            if(CountTurns(section.Points)==0)foundStraightDistrict=true;
        }
        Assert.That(foundStraightDistrict,Is.True);
    }
    [Test] public void SmallerWatercoursesCreateInteriorHeadsAndConfluences()
    {
        var r=RegionSaveStore.Create("Natural network",28,20);
        var paths=RegionRiverGenerator.Generate(r,new RegionTerrainSettings{
            RiverCountsVersion=1,DeepRivers=RegionWaterAmount.Few,
            MediumRiverCount=3,SmallRiverCount=5,StreamCount=5},7721);
        Assert.That(paths,Has.Count.EqualTo(14));
        var tributaries=paths.Where(path=>!string.IsNullOrEmpty(
            path.TributaryOfRiverId)).ToArray();
        Assert.That(tributaries.Length,Is.GreaterThan(2));
        foreach(var tributary in tributaries)
        {
            var parent=paths.Single(path=>path.Id==tributary.TributaryOfRiverId);
            var end=tributary.Points[^1];
            Assert.That(DistanceToPath(parent,new Vector2(end.X,end.Z)),
                Is.LessThan(.001f));
        }
        Assert.That(paths.Where(path=>path.GeneratedSize is
            GeneratedRegionRiverSize.Small or GeneratedRegionRiverSize.Stream)
            .Any(path=>path.Points[0].X>.001f&&path.Points[0].X<r.Width-.001f&&
                path.Points[0].Z>.001f&&path.Points[0].Z<r.Height-.001f),Is.True);
        for(var later=1;later<paths.Count;later++)
        for(var earlier=0;earlier<later;earlier++)
        foreach(var intersection in Intersections(paths[later],paths[earlier]))
        {
            var end=paths[later].Points[^1];
            Assert.That(paths[later].TributaryOfRiverId,
                Is.EqualTo(paths[earlier].Id));
            Assert.That(Vector2.Distance(intersection,
                new Vector2(end.X,end.Z)),Is.LessThan(.002f),
                "Generated rivers may meet only at the later river's confluence.");
        }
    }
    [Test] public void SpacingIsIrregularAndRiverSizesHaveDifferentCadence()
    {
        var r=RegionSaveStore.Create("Irregular spacing",28,20);
        var settings=new RegionTerrainSettings{RiverCountsVersion=1,
            MediumRiverCount=3,SmallRiverCount=5,StreamCount=5};
        var paths=RegionRiverGenerator.Generate(r,settings,8842);
        var foundIrregularGroup=false;
        foreach(var group in paths.GroupBy(RegionRiverGenerator.DirectionOf))
        {
            var horizontal=group.Key==DistrictRiverDirection.WestToEast;
            var centers=group.Select(path=>path.Points.Average(point=>
                horizontal?point.Z:point.X)).OrderBy(value=>value).ToArray();
            if(centers.Length<3)continue;
            var gaps=centers.Zip(centers.Skip(1),(a,b)=>b-a).ToArray();
            if(gaps.Max()-gaps.Min()>.2f)foundIrregularGroup=true;
        }
        Assert.That(foundIrregularGroup,Is.True);

        var major=RegionRiverGenerator.Generate(r,new RegionTerrainSettings{
            RiverCountsVersion=1,DeepRivers=RegionWaterAmount.Few},92).Single();
        var stream=RegionRiverGenerator.Generate(r,new RegionTerrainSettings{
            RiverCountsVersion=1,StreamCount=1},92).Single();
        Assert.That(major.Points.Count,Is.LessThan(stream.Points.Count),
            "Major rivers should use longer, slower meanders than streams.");
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
        var original=r.RiverPaths.ToArray();var geometry=original.Select(JsonUtility.ToJson).ToArray();
        RegionSaveStore.RegenerateTiles(r);
        CollectionAssert.AreEqual(original,r.RiverPaths);
        CollectionAssert.AreEqual(geometry,r.RiverPaths.Select(JsonUtility.ToJson));
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

    static void AssertSeparated(IEnumerable<RegionRiverPath> paths,bool horizontal)
    {
        var ranges=paths.Select(path=>(Minimum:path.Points.Min(point=>horizontal?point.Z:point.X),Maximum:path.Points.Max(point=>horizontal?point.Z:point.X))).OrderBy(range=>range.Minimum).ToArray();
        for(int index=1;index<ranges.Length;index++)
            Assert.That(ranges[index-1].Maximum,Is.LessThan(ranges[index].Minimum),"Parallel river corridors must not intersect.");
    }
    static int CountTurns(IReadOnlyList<DistrictRiverPoint> points)
    {
        var count=0;
        for(var index=1;index<points.Count-1;index++)
        {
            var incoming=new Vector2(points[index].X-points[index-1].X,
                points[index].Z-points[index-1].Z);
            var outgoing=new Vector2(points[index+1].X-points[index].X,
                points[index+1].Z-points[index].Z);
            if(incoming.sqrMagnitude<1e-10f||outgoing.sqrMagnitude<1e-10f)
                continue;
            if(Vector2.Angle(incoming,outgoing)>2f)count++;
        }
        return count;
    }
    static float DistanceToPath(RegionRiverPath path,Vector2 target)
    {
        var closest=float.PositiveInfinity;
        for(var index=1;index<path.Points.Count;index++)
        {
            var a=new Vector2(path.Points[index-1].X,path.Points[index-1].Z);
            var b=new Vector2(path.Points[index].X,path.Points[index].Z);
            var delta=b-a;
            var t=delta.sqrMagnitude<1e-10f?0:
                Mathf.Clamp01(Vector2.Dot(target-a,delta)/delta.sqrMagnitude);
            closest=Mathf.Min(closest,Vector2.Distance(target,a+delta*t));
        }
        return closest;
    }
    static IEnumerable<Vector2> Intersections(RegionRiverPath first,
        RegionRiverPath second)
    {
        float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        for(var one=1;one<first.Points.Count;one++)
        for(var two=1;two<second.Points.Count;two++)
        {
            var a=new Vector2(first.Points[one-1].X,first.Points[one-1].Z);
            var b=new Vector2(first.Points[one].X,first.Points[one].Z);
            var c=new Vector2(second.Points[two-1].X,second.Points[two-1].Z);
            var d=new Vector2(second.Points[two].X,second.Points[two].Z);
            var ab=b-a;var cd=d-c;var denominator=Cross(ab,cd);
            if(Mathf.Abs(denominator)<1e-6f)continue;
            var t=Cross(c-a,cd)/denominator;
            var u=Cross(c-a,ab)/denominator;
            if(t>=-1e-4f&&t<=1.0001f&&u>=-1e-4f&&u<=1.0001f)
                yield return a+ab*Mathf.Clamp01(t);
        }
    }
}
