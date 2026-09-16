using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class NationalPikePlacementTests
{
    static RegionSaveData Region() => new RegionSaveData
    {
        Width=2,Height=1,Tiles=new List<RegionCityTile>
        {
            new RegionCityTile{TileId="a",X=0,Y=0,Width=1,Height=1},
            new RegionCityTile{TileId="b",X=1,Y=0,Width=1,Height=1}
        }
    };
    static RegionTransportRoute Route(params Vector2[] points) => new RegionTransportRoute
    {Id="test-pike",Name="Test Pike",Points=new List<Vector2>(points)};

    [Test] public void DiagonalStrokeHasOnlyCardinalStepsAndEndsAtDestination()
    {
        foreach(var end in new[]{new Vector2(1.9f,.9f),new Vector2(.05f,.05f),new Vector2(.5f,.95f)})
        {
            var cells=NationalPikePlacement.Cells(new[]{new Vector2(.5f,.5f),end}).ToList();
            Assert.AreEqual(Vector2Int.FloorToInt(end*64),cells.Last());
            for(int i=1;i<cells.Count;i++)Assert.AreEqual(1,Mathf.Abs(cells[i].x-cells[i-1].x)+Mathf.Abs(cells[i].y-cells[i-1].y));
        }
    }
    [Test] public void CrossDistrictRoadIsContinuousAndPreparationDoesNotMutateLiveData()
    {
        var region=Region();var route=Route(new(.1f,.5f),new(1.9f,.5f));
        var edits=NationalPikePlacement.Build(region,route);
        Assert.AreEqual(2,edits.Count);Assert.IsEmpty(region.Tiles[0].Roads);
        Assert.IsEmpty(region.Tiles[1].Roads);
        foreach(var edit in edits)edit.Key.Roads=edit.Value;
        foreach(var tile in region.Tiles)
        {
            var edge=tile.Roads.Single(r=>r.GridX==(tile.X==0?63:0));
            Assert.AreEqual(RoadPieceTopology.Straight,edge.Topology);
            Assert.AreEqual(RoadPiecePackageCatalog.NationalPikeDirtId,edge.PackageId);
        }
        var reloaded=JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(region));
        Assert.AreEqual("test-pike",reloaded.Tiles[1].Roads[0].NationalPikeId);
    }
    [Test] public void RiverCrossingStopsAtBankAndResumesOnOtherSide()
    {
        var region=Region();region.Tiles[0].Rivers.Add(new PlacedDistrictRiver
        {WidthMeters=40,Points=new(){new(.5f,0),new(.5f,1)}});
        var route=Route(new(.1f,.5f),new(.9f,.5f));
        var roads=NationalPikePlacement.Build(region,route)[region.Tiles[0]];
        Assert.IsTrue(roads.Any(r=>r.GridX<25));Assert.IsTrue(roads.Any(r=>r.GridX>38));
        var mask=new NationalPikePlacement.WaterMask(region);
        Assert.IsTrue(roads.All(r=>!mask.Contains(new Vector2((r.GridX+.5f)*10,(r.GridZ+.5f)*10),7.1f)));
        var dry=NationalPikePlacement.DryMapSections(region,route,72);
        Assert.AreEqual(2,dry.Count);Assert.Less(dry[0].Last().x,.5f);Assert.Greater(dry[1][0].x,.5f);
    }
    [Test] public void JoiningExistingRoadCreatesJunctionWithoutOverwritingMaterialOrOriginal()
    {
        var region=Region();var tile=region.Tiles[0];
        for(int x=20;x<=40;x++)tile.Roads.Add(new PlacedRoadPiece
        {GridX=x,GridZ=32,PackageId=RoadPiecePackageCatalog.DirtRoadId,Topology=RoadPieceTopology.Straight,RoadMaterialId="dirt"});
        var old=tile.Roads.Single(r=>r.GridX==32);
        var roads=NationalPikePlacement.Build(region,Route(new(32.5f/64,.2f),new(32.5f/64,32.5f/64)))[tile];
        var junction=roads.Single(r=>r.GridX==32&&r.GridZ==32);
        Assert.AreEqual(RoadPieceTopology.TJunction,junction.Topology);
        Assert.AreEqual(RoadPiecePackageCatalog.DirtRoadId,junction.PackageId);
        Assert.AreEqual(RoadPieceTopology.Straight,old.Topology);
    }
    [TestCase("founders","dirt")][TestCase("industrial","dirt")]
    [TestCase("discovery","early-concrete")][TestCase("modern","blacktop")]
    public void EraSurfaceContract(string era,string surface) => Assert.AreEqual(surface,NationalPikePlacement.SurfaceForEra(era));

    [Test] public void DirtPackageHasFourRequestedArtPiecesAndCompilingShader()
    {
        var package=RoadPiecePackageCatalog.Resolve(RoadPiecePackageCatalog.NationalPikeDirtId);
        Assert.AreEqual(8,package.RoadWidthMeters);
        foreach(var topology in new[]{RoadPieceTopology.Straight,RoadPieceTopology.Corner,RoadPieceTopology.TJunction,RoadPieceTopology.FourWay})
            Assert.IsNotNull(Resources.Load<Texture2D>(package.Piece(topology).ResourcePath));
        var shader=Shader.Find("CityForgeV3/ShadowReceivingRoadOverlay");
        Assert.IsNotNull(shader);Assert.False(ShaderUtil.ShaderHasError(shader));
    }
}
