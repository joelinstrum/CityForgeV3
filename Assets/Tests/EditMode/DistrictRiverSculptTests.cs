using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
public class DistrictRiverSculptTests
{
    [Test] public void BankJoinPreservesWidthWithUnevenPointSpacing()
    {
        var points = new[]{new Vector2(-100,0),Vector2.zero,new Vector2(1,1)};
        var offset = DistrictWorldController.RiverSectionOffset(points,1);
        Assert.AreEqual(1,Vector2.Dot(offset,Vector2.up),.00001f);
        Assert.AreEqual(1,Vector2.Dot(offset,new Vector2(-1,1).normalized),.00001f);
        var resampled = new[]{new Vector2(-1,0),Vector2.zero,new Vector2(100,100)};
        Assert.Less((offset-DistrictWorldController.RiverSectionOffset(resampled,1)).magnitude,.00001f);
    }
    [Test] public void BankJoinHandlesEndpointsDuplicatesAndHairpinsWithoutSpikes()
    {
        var points = new[]{Vector2.zero,Vector2.right,Vector2.zero};
        for(int i=0;i<points.Length;i++)
        {
            var offset=DistrictWorldController.RiverSectionOffset(points,i);
            Assert.IsFalse(float.IsNaN(offset.x));Assert.LessOrEqual(offset.magnitude,2.001f);
        }
        Assert.AreEqual(Vector2.up,DistrictWorldController.RiverSectionOffset(new[]{Vector2.zero,Vector2.zero,Vector2.right},1));
    }
    [Test] public void RepairRemovesShortReversalsAndPreservesRiverContract()
    {
        var district=District();var river=district.Rivers[0];river.WidthMeters=128;river.Depth=DistrictRiverDepth.Deep;
        river.Points=new(){new(0,.5f),new(.35f,.5f),new(.39f,.52f),new(.36f,.5f),new(.5f,.5f),new(.53f,.60f),new(.8f,.65f),new(1,.65f)};
        var other=new PlacedDistrictRiver{Points=new(){new(.1f,.1f),new(.9f,.1f)}};district.Rivers.Add(other);
        var repaired=DistrictRiverSculpt.Repair(district,river);
        Assert.AreNotSame(district.Rivers,repaired);Assert.AreSame(other,repaired[1]);
        Assert.AreEqual(river.InstanceId,repaired[0].InstanceId);Assert.AreEqual(river.WidthMeters,repaired[0].WidthMeters);Assert.AreEqual(river.Depth,repaired[0].Depth);
        Assert.AreEqual(river.Points[0].X,repaired[0].Points[0].X);Assert.AreEqual(river.Points[0].Z,repaired[0].Points[0].Z);
        Assert.AreEqual(river.Points.Last().X,repaired[0].Points.Last().X);Assert.AreEqual(river.Points.Last().Z,repaired[0].Points.Last().Z);
        var points=repaired[0].Points.Select(p=>new Vector2(p.X,p.Z)*1280).ToList();
        for(int i=1;i<points.Count-1;i++)
        {
            var a=points[i]-points[i-1];var b=points[i+1]-points[i];
            float radius=Mathf.Min(a.magnitude,b.magnitude)/Mathf.Max(.00001f,2*Mathf.Tan(Vector2.Angle(a,b)*Mathf.Deg2Rad*.5f));
            Assert.Greater(radius,river.WidthMeters*.54f,"Bank would fold at point "+i);
        }
        Assert.AreEqual(8,river.Points.Count,"Original geometry must remain available to undo");
    }
    [Test] public void RepairLeavesHealthyStraightRiverUnchanged()
    {
        var district=District();Assert.AreSame(district.Rivers,DistrictRiverSculpt.Repair(district,district.Rivers[0]));
    }
    [Test] public void ShapeWidthOverrideChangesOnlySelectedRiverAndPreservesDepth()
    {
        var district=District();var selected=district.Rivers[0];selected.Depth=DistrictRiverDepth.Deep;
        var other=new PlacedDistrictRiver{WidthMeters=18,Points=new(){new(0,.1f),new(1,.1f)}};district.Rivers.Add(other);
        var result=DistrictRiverSculpt.Redraw(district,selected,new[]{new Vector2(.3f,.5f),new Vector2(.5f,.55f),new Vector2(.7f,.5f)},100);
        Assert.AreEqual(100,result[0].WidthMeters);Assert.AreEqual(DistrictRiverDepth.Deep,result[0].Depth);
        Assert.AreSame(other,result[1]);Assert.AreEqual(64,selected.WidthMeters);
    }
    [Test] public void RepairAllSmoothsGentleJaggednessAndKeepsBorderTangents()
    {
        var d=District();var river=d.Rivers[0];river.WidthMeters=128;
        river.Points=new(){new(0,.5f),new(.1f,.5f),new(.2f,.51f),new(.3f,.49f),new(.4f,.51f),new(.5f,.49f),new(.6f,.51f),new(.8f,.5f),new(1,.5f)};
        var other=new PlacedDistrictRiver{WidthMeters=64,Points=river.Points.Select(p=>new DistrictRiverPoint(p.X,p.Z-.3f)).ToList()};d.Rivers.Add(other);
        var result=DistrictRiverSculpt.RepairAll(d);
        Assert.AreNotSame(river,result[0]);Assert.AreNotSame(other,result[1]);
        foreach(var repaired in result)
        {
            var original=d.Rivers[result.IndexOf(repaired)];
            Assert.AreEqual(original.Points[0].Z,repaired.Points[0].Z);
            Assert.AreEqual(original.Points.Last().Z,repaired.Points.Last().Z);
            Assert.AreEqual(repaired.Points[0].Z,repaired.Points[1].Z,.00001f);
            Assert.AreEqual(repaired.Points.Last().Z,repaired.Points[repaired.Points.Count-2].Z,.00001f);
        }
        Assert.Less(result[0].Points.Max(p=>Mathf.Abs(p.Z-.5f)),.01f);
    }
    [Test] public void RebuildCombinesContinuationAndDiscardsInternalBankRemnant()
    {
        var d=District();var main=d.Rivers[0];main.WidthMeters=128;
        main.Points=new(){new(0,.5f),new(.5f,.51f),new(.95f,.5f)};
        d.Rivers.Add(new(){WidthMeters=128,Depth=main.Depth,Points=new(){new(.94f,.51f),new(1,.5f)}});
        d.Rivers.Add(new(){WidthMeters=128,Depth=main.Depth,Points=new(){new(.3f,.51f),new(.4f,.52f)}});
        var rebuilt=DistrictRiverSculpt.RepairAll(d);
        Assert.AreEqual(1,rebuilt.Count);Assert.AreEqual(0,rebuilt[0].Points[0].X);Assert.AreEqual(1,rebuilt[0].Points.Last().X);
        Assert.AreEqual(128,rebuilt[0].WidthMeters);Assert.AreEqual(3,d.Rivers.Count);
        Assert.Greater(rebuilt[0].Points.Count,main.Points.Count);
    }
    [Test] public void RebuildRetainsSeparateBorderCrossingsAndDifferentDepths()
    {
        var d=District();var main=d.Rivers[0];
        d.Rivers.Add(new(){WidthMeters=main.WidthMeters,Depth=main.Depth,Points=new(){new(0,.51f),new(.2f,.51f)}});
        d.Rivers.Add(new(){WidthMeters=main.WidthMeters,Depth=main.Depth==DistrictRiverDepth.Deep?DistrictRiverDepth.Shallow:DistrictRiverDepth.Deep,Points=new(){new(.3f,.5f),new(.4f,.5f)}});
        Assert.AreEqual(3,DistrictRiverSculpt.RepairAll(d).Count);
    }
    [Test] public void ShapeThenRepairJoinsOffsetCity051ReachesIntoOneBorderToBorderChannel()
    {
        var d=new RegionCityTile{Width=4,Height=2,Rivers=new(){
            new(){WidthMeters=128,Depth=DistrictRiverDepth.Deep,Points=new(){new(.763589f,1),new(.75f,.7f),new(.74f,.4f),new(.735341f,.311276f)}},
            new(){WidthMeters=128,Depth=DistrictRiverDepth.Deep,Points=new(){new(.677721f,0),new(.68f,.2f),new(.679426f,.374502f)}}}};
        d.Rivers=DistrictRiverSculpt.Redraw(d,d.Rivers[0],new[]{new Vector2(.75f,.7f),new Vector2(.77f,.55f),new Vector2(.74f,.4f)});
        var repaired=DistrictRiverSculpt.RepairAll(d);
        Assert.AreEqual(1,repaired.Count,"Both old terminal ends must be replaced by one centerline");
        Assert.AreEqual(1,repaired[0].Points[0].Z);Assert.AreEqual(0,repaired[0].Points.Last().Z);
        Assert.AreEqual(128,repaired[0].WidthMeters);
        d.Rivers=repaired;
        d.Rivers=DistrictRiverSculpt.Redraw(d,d.Rivers[0],new[]{new Vector2(.74f,.7f),new Vector2(.76f,.5f),new Vector2(.69f,.3f)});
        Assert.AreEqual(1,DistrictRiverSculpt.RepairAll(d).Count,"Repeated Shape/Repair must not leave fragments");
    }
    [Test] public void RiverClippingAndJunctionCutsPreserveInterpolatedFlowDirections()
    {
        var mesh=new Mesh();
        try
        {
            mesh.vertices=new[]{new Vector3(-2,0,0),new Vector3(2,0,0),new Vector3(0,0,2)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up};
            mesh.uv2=mesh.vertices.Select(v=>new Vector2(v.x,v.z)).ToArray();
            mesh.triangles=new[]{0,1,2};
            RiverMeshUnion.ClipToRect(mesh,new Rect(-1,-1,2,2));
            RiverMeshUnion.Subtract(mesh,new(){new RiverMeshUnion.Quad(new(-.2f,-.2f),new(.2f,-.2f),new(.2f,.2f),new(-.2f,.2f))},null);
            Assert.Greater(mesh.vertexCount,0);Assert.AreEqual(mesh.vertexCount,mesh.uv2.Length);
            for(int i=0;i<mesh.vertexCount;i++)Assert.Less(Vector2.Distance(mesh.uv2[i],new Vector2(mesh.vertices[i].x,mesh.vertices[i].z)),.00001f);
        }
        finally{Object.DestroyImmediate(mesh);}
    }
    static RegionCityTile District()=>new(){TileId="a",Width=2,Height=2,Rivers=new(){new(){WidthMeters=64,Points=new(){new(0,.5f),new(.5f,.5f),new(1,.5f)}}}};
    [Test] public void ShapeMovesInteriorPreservingBorderCrossingsAndWidth()
    {
        var district=District();var edited=DistrictRiverSculpt.Edit(district,new[]{new Vector2(.5f,.5f),new Vector2(.5f,.6f)},240,RiverSculptMode.Shape);
        Assert.Greater(edited[0].Points.Max(p=>p.Z),.58f);
        Assert.AreEqual(.5f,edited[0].Points[0].Z);Assert.AreEqual(.5f,edited[0].Points.Last().Z);
        Assert.AreEqual(64,edited[0].WidthMeters);Assert.AreEqual(.5f,district.Rivers[0].Points[1].Z);
    }
    [Test] public void EraserSplitsChannelAndLocalEditsSurviveRegenerationAndSerialization()
    {
        var district=District();district.Rivers[0].RegionRiverId="original";
        district.Rivers=DistrictRiverSculpt.Edit(district,new[]{new Vector2(.5f,.5f)},100,RiverSculptMode.Erase);
        Assert.AreEqual(2,district.Rivers.Count);
        Assert.AreEqual(.5f-100f/1280,district.Rivers[0].Points.Last().X,.0001f);
        Assert.AreEqual(.5f+100f/1280,district.Rivers[1].Points[0].X,.0001f);
        district.RiversEditedLocally=true;
        var region=new RegionSaveData{Tiles=new(){district},RiverPaths=new(){new(){Id="original",Points=new(){new(0,1),new(2,1)}}}};
        RegionRiverGenerator.Apply(region,region.RiverPaths);
        var loaded=JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(region));
        Assert.IsTrue(loaded.Tiles[0].RiversEditedLocally);Assert.AreEqual(2,loaded.Tiles[0].Rivers.Count);
    }
    [Test] public void SofteningReducesSharpBendWithoutMovingBorders()
    {
        var district=District();district.Rivers[0].Points=new(){new(0,.5f),new(.45f,.5f),new(.5f,.65f),new(.55f,.5f),new(1,.5f)};
        var edited=DistrictRiverSculpt.Edit(district,new[]{new Vector2(.5f,.65f)},240,RiverSculptMode.Soften);
        Assert.Less(edited[0].Points.Max(p=>p.Z),.65f);Assert.AreEqual(.5f,edited[0].Points[0].Z);
    }
    [Test] public void ShapeCanSnapAnInteriorEndToAnotherChannel()
    {
        var district=District();district.Rivers[0].Points=new(){new(0,.5f),new(.45f,.5f)};
        district.Rivers.Add(new(){WidthMeters=64,Points=new(){new(.5f,.2f),new(.5f,.8f)}});
        var edited=DistrictRiverSculpt.Edit(district,new[]{new Vector2(.45f,.5f),new Vector2(.49f,.5f)},160,RiverSculptMode.Shape);
        Assert.AreEqual(.5f,edited[0].Points.Last().X,.00001f);
        Assert.AreEqual(.5f,edited[0].Points.Last().Z,.00001f);
        Assert.AreSame(district.Rivers[1],edited[1],"Shaping should not displace the target river");
    }
    [Test] public void RedrawReplacesSelectedReachAtLockedWidthWithoutChangingOtherRivers()
    {
        var d=District();var selected=d.Rivers[0];var other=new PlacedDistrictRiver{WidthMeters=18,Points=new(){new(0,.8f),new(1,.8f)}};d.Rivers.Add(other);
        var result=DistrictRiverSculpt.Redraw(d,selected,new[]{new Vector2(.2f,.5f),new Vector2(.4f,.62f),new Vector2(.6f,.62f),new Vector2(.8f,.5f)});
        Assert.AreEqual(2,result.Count);Assert.AreEqual(64,result[0].WidthMeters);Assert.AreEqual(selected.Depth,result[0].Depth);
        Assert.Greater(result[0].Points.Max(p=>p.Z),.58f);Assert.AreSame(other,result[1]);
        Assert.AreEqual(0,result[0].Points[0].X);Assert.AreEqual(1,result[0].Points.Last().X);
        Assert.AreEqual(3,selected.Points.Count,"Draft must not modify original river");
        var reverse=DistrictRiverSculpt.Redraw(d,selected,new[]{new Vector2(.8f,.5f),new Vector2(.6f,.62f),new Vector2(.4f,.62f),new Vector2(.2f,.5f)});
        CollectionAssert.AreEqual(result[0].Points.Select(p=>new Vector2(p.X,p.Z)),reverse[0].Points.Select(p=>new Vector2(p.X,p.Z)));
        Assert.AreSame(d.Rivers,DistrictRiverSculpt.Redraw(d,selected,new[]{new Vector2(.5f,.5f)}));
    }
    [Test] public void IndexedWaterQueriesMatchFullScanAtCellBoundariesAndAfterRepeatedQueries()
    {
        var points=new List<Vector2>();for(int i=0;i<100;i++)points.Add(new Vector2(-640+i*13,Mathf.Sin(i*.15f)*140));
        var type=typeof(DistrictWorldController).GetNestedType("RuntimeRiverSurface",System.Reflection.BindingFlags.NonPublic);
        var instance=System.Activator.CreateInstance(type,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic,null,
            new object[]{points,64f,56f,50f,-.2f,.184f,1f,true},null);
        var query=type.GetMethod("FindClosest");
        for(int pass=0;pass<2;pass++)for(int x=-700;x<=700;x+=16)for(int z=-220;z<=220;z+=32)
        {
            var point=new Vector2(x,z);float best=float.PositiveInfinity,along=0,total=0;
            for(int i=1;i<points.Count;i++)
            {
                var delta=points[i]-points[i-1];float length=delta.magnitude;
                float t=Mathf.Clamp01(Vector2.Dot(point-points[i-1],delta)/delta.sqrMagnitude);
                float distance=Vector2.Distance(point,points[i-1]+delta*t);
                if(distance<best){best=distance;along=total+length*t;}total+=length;
            }
            var hit=query.Invoke(instance,new object[]{point,0f});var result=hit.GetType();
            float actual=(float)result.GetField("AbsoluteLateral").GetValue(hit);
            if(best<=64){Assert.AreEqual(best,actual,.001f);Assert.AreEqual(along,(float)result.GetField("DistanceAlong").GetValue(hit),.001f);}
            else Assert.Greater(actual,64,"Dry ground must not become a river");
        }
    }
    [Test] public void ClipCutsDiagonalRiverFlushToDistrictEdgeAndPreservesUvs()
    {
        var mesh=new Mesh();
        try
        {
            mesh.vertices=new[]{new Vector3(-2,0,-.4f),new Vector3(2,0,.4f),new Vector3(2,0,1),new Vector3(-2,0,.2f)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};
            RiverMeshUnion.ClipToRect(mesh,new Rect(-1,-1,2,2));
            Assert.IsTrue(mesh.vertices.All(v=>v.x>=-1.0001f && v.x<=1.0001f && v.z>=-1.0001f && v.z<=1.0001f));
            Assert.GreaterOrEqual(mesh.vertices.Count(v=>Mathf.Abs(v.x-1)<.0001f),2);
            Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);Assert.Greater(mesh.triangles.Length,0);
        }
        finally{Object.DestroyImmediate(mesh);}
    }
}
