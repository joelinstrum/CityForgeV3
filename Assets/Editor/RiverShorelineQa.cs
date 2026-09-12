#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
public static class RiverShorelineQa
{
    [MenuItem("City Forge/QA/River/Check Gentle Shorelines")]
    static void Check()
    {
        if(!EditorApplication.isPlaying)return;
        var app=Object.FindFirstObjectByType<CityForgeApp>();
        app.OpenGeneratedRiverQa();
        app.GetComponent<UIDocument>().rootVisualElement.schedule.Execute(Validate).StartingIn(500);
    }
    static void Validate()
    {
        var app=Object.FindFirstObjectByType<CityForgeApp>();
        var world=Object.FindFirstObjectByType<DistrictWorldController>();
        var district=(RegionCityTile)app.GetType().GetMethod("FindSelectedRegionTile",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,null);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var width=(float)world.GetType().GetField("_widthMeters",flags).GetValue(world);
        var depth=(float)world.GetType().GetField("_depthMeters",flags).GetValue(world);
        var river=district.Rivers[district.Rivers.Count-1];
        var centers=new List<Vector2>();foreach(var p in river.Points)centers.Add(new Vector2((p.X-.5f)*width,(p.Z-.5f)*depth));
        Mesh water=null;foreach(var f in world.GetComponentsInChildren<MeshFilter>())if(f.name=="River Water — "+river.InstanceId)water=f.sharedMesh;
        if(water==null)throw new Exception("Water mesh missing");
        var oldHalf=river.WidthMeters*(river.Depth==DistrictRiverDepth.Deep?1.08f:1.28f)*.5f;
        // Compare the high-water shore with the full existing bank footprint.
        var vertices=water.vertices;var minLeft=float.MaxValue;var maxLeft=0f;var minRight=float.MaxValue;var maxRight=0f;var maxAsymmetry=0f;var outside=0;
        for(var row=0;row<vertices.Length/5;row++)
        {
            var center=new Vector2(vertices[row*5+2].x,vertices[row*5+2].z);
            var best=float.PositiveInfinity;var segment=0;var t=0f;
            for(var i=0;i<centers.Count-1;i++)
            {var delta=centers[i+1]-centers[i];var u=Mathf.Clamp01(Vector2.Dot(center-centers[i],delta)/Mathf.Max(.0001f,delta.sqrMagnitude));var d=(center-Vector2.Lerp(centers[i],centers[i+1],u)).sqrMagnitude;if(d<best){best=d;segment=i;t=u;}}
            Vector2 Normal(int i){var tangent=(centers[Mathf.Min(centers.Count-1,i+1)]-centers[Mathf.Max(0,i-1)]).normalized;return new Vector2(-tangent.y,tangent.x);}
            var envelope=Vector2.Lerp(Normal(segment),Normal(segment+1),t).magnitude*oldHalf;
            var left=Vector2.Distance(center,new Vector2(vertices[row*5].x,vertices[row*5].z))/envelope;
            var right=Vector2.Distance(center,new Vector2(vertices[row*5+4].x,vertices[row*5+4].z))/envelope;
            minLeft=Mathf.Min(minLeft,left);maxLeft=Mathf.Max(maxLeft,left);minRight=Mathf.Min(minRight,right);maxRight=Mathf.Max(maxRight,right);maxAsymmetry=Mathf.Max(maxAsymmetry,Mathf.Abs(left-right));
            if(left>1.0001f||right>1.0001f||left<.75f||right<.75f)outside++;
        }
        Directory.CreateDirectory("QA/RiverShoreline");
        File.WriteAllText("QA/RiverShoreline/checks.txt",$"waterHeight={world.WaterHeight} waterElevation={vertices[0].y} waterRows={vertices.Length/5} originalRows={centers.Count} leftRange={minLeft}..{maxLeft} rightRange={minRight}..{maxRight} maximumAsymmetry={maxAsymmetry} envelopeViolations={outside}\n");
        if(outside!=0||maxLeft-minLeft<.02f||maxRight-minRight<.02f||maxAsymmetry<.02f)throw new Exception("Shoreline shape contract failed");
        world.SetZoom(DistrictZoomLevel.LOD1);
    }
}
#endif
