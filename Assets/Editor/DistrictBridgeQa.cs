#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class DistrictBridgeQa
{
    public static void Run()
    {
        string output=Environment.GetEnvironmentVariable("CITYFORGE_BRIDGE_QA")??"/tmp/cityforge-bridge-qa";
        Directory.CreateDirectory(output);
        var d=new RegionCityTile{Width=1,Height=1};
        d.Rivers.Add(new(){InstanceId="bridge-review-river",WidthMeters=30,Depth=DistrictRiverDepth.Deep,
            Points=new(){new(.5f,.1f),new(.5f,.9f)}});
        var go=new GameObject("Bridge review world");var world=go.AddComponent<DistrictWorldController>();world.Build(d);
        var bank=new Vector2Int(29,32);
        if(!DistrictBridgePlanner.TryPlan(d,bank,Vector2Int.right,world.SampleBridgeSurface,_=>false,out var b,out var reason))throw new Exception(reason);
        var roads=new DistrictRoadPlacementModel.EditSession(d.Roads);int money=10000;
        for(int x=b.Start.x-4;x<=b.Start.x;x++)roads.TryPlace(x,b.Start.y,64,64,DistrictRoadPlacementModel.AntiqueBrickFamily,ref money);
        for(int x=b.End.x;x<=b.End.x+4;x++)roads.TryPlace(x,b.End.y,64,64,DistrictRoadPlacementModel.AntiqueBrickFamily,ref money);
        world.RefreshRoadCellsAndNeighbors(d,new[]{b.Start,b.End,b.Start-Vector2Int.right,b.End+Vector2Int.right,b.Start-Vector2Int.right*2,b.End+Vector2Int.right*2,b.Start-Vector2Int.right*3,b.End+Vector2Int.right*3,b.Start-Vector2Int.right*4,b.End+Vector2Int.right*4},roads.At);
        var report=$"Planned {b.Start} -> {b.End}; deck {b.DeckHeight}; near {b.StartHeight}; far {b.EndHeight}\n";
        var camera=world.WorldCamera;
        var a=DistrictBridgePlanner.Center(d,b.Start);var z=DistrictBridgePlanner.Center(d,b.End);var center=(a+z)*.5f;
        camera.orthographic=true;camera.orthographicSize=31;camera.transform.position=new Vector3(center.x+55,48,center.y-65);
        camera.transform.LookAt(new Vector3(center.x,1,center.y));camera.nearClipPlane=.1f;camera.farClipPlane=2000;
        var target=new RenderTexture(1500,1000,24);camera.targetTexture=target;
        foreach(var style in DistrictBridgeCatalog.Styles)
        {
            b.StyleId=style.Id;world.PreviewDistrictBridge(d,b);
            camera.Render();report+=$"{style.Id}: UnityStats draw calls={UnityStats.drawCalls}, batches={UnityStats.batches}, triangles={UnityStats.triangles} (manual Camera.Render)\n";RenderTexture.active=target;var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,style.Id+".png"),image.EncodeToPNG());Object.DestroyImmediate(image);
            world.HideDistrictBridgePreview();GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();var watch=Stopwatch.StartNew();long before=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            world.AddDistrictBridge(d,b);watch.Stop();long allocated=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-before;
            int renderers=go.GetComponentsInChildren<Renderer>().Count(r=>r.name=="Bridge span"||r.name=="Approaches");
            if(world.BridgeAt(center)!=b)throw new Exception("Bridge spatial lookup failed");
            if(Mathf.Abs(world.TravelElevation(center)-b.DeckHeight)>.001f)throw new Exception("Travel surface mismatch");
            report+=$"{style.Id}: warm assembly {watch.Elapsed.TotalMilliseconds:F2} ms, {allocated} managed heap delta bytes, {renderers} renderers\n";
            world.RemoveDistrictBridge(d,b);if(world.BridgeAt(center)!=null)throw new Exception("Removed bridge remains indexed");
        }
        var appHost=new GameObject("Bridge UI transaction review");appHost.SetActive(false);
        var app=appHost.AddComponent<CityForgeV3.UI.CityForgeApp>();var appType=app.GetType();
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        void Field(string name,object value)=>appType.GetField(name,flags).SetValue(app,value);
        void Call(string name,params object[] args)=>appType.GetMethod(name,flags).Invoke(app,args);
        var ui=new UnityEngine.UIElements.VisualElement();var region=new RegionSaveData();region.Tiles.Add(d);d.TileId="bridge-qa";
        Field("_root",ui);Field("_openRegion",region);Field("_selectedRegionTileId",d.TileId);Field("_districtWorld",world);
        Field("_currentScreen",Enum.Parse(appType.GetField("_currentScreen",flags).FieldType,"DistrictTerraform"));
        Call("EnsureDistrictUndo",d);
        string unchanged=JsonUtility.ToJson(d);Call("ComposeDistrictBridgeModal",b);Call("RemoveDocumentModal");
        if(JsonUtility.ToJson(d)!=unchanged)throw new Exception("Cancel changed district");
        if(go.GetComponentsInChildren<Renderer>().Any(r=>r.name=="Bridge span"))throw new Exception("Preview survived cancellation");
        int originalMoney=d.Treasury;Call("BuildDistrictBridge",d,b);
        if(d.Bridges.Count!=1 || d.Treasury!=originalMoney-b.Cost || world.BridgeAt(center)==null)throw new Exception("Build transaction failed");
        if((bool)appType.GetMethod("TryOfferDistrictBridge",flags).Invoke(app,new object[]{d,b.End+Vector2Int.right,b.End}))throw new Exception("Existing bridge endpoint intercepted a connecting road");
        if(!(bool)appType.GetMethod("CanPlaceDistrictRoad",flags).Invoke(app,new object[]{d,b.End.x,b.End.y}))throw new Exception("Existing bridge endpoint rejects connecting roads");
        string built=JsonUtility.ToJson(d);
        var undo=(DistrictUndoHistory)appType.GetField("_districtUndo",flags).GetValue(app);
        if(!undo.TryUndo(out var snapshot))throw new Exception("Build was not undoable");
        JsonUtility.FromJsonOverwrite(snapshot,d);world.Build(d);
        if(d.Bridges.Count!=0 || world.BridgeAt(center)!=null || d.Treasury!=originalMoney)throw new Exception("Undo failed");
        JsonUtility.FromJsonOverwrite(built,d);world.Build(d);
        if(world.BridgeAt(center)==null)throw new Exception("Reload failed");
        report+="UI transaction: cancel leaves state unchanged and removes preview; build charges once; in-memory undo restores treasury and removes bridge; reload rebuilds bridge index. PASS\n";
        Object.DestroyImmediate(appHost);
        for(int row=8;row<=56;row+=2)
        {
            if(row==b.Start.y)continue;
            var copy=JsonUtility.FromJson<PlacedDistrictBridge>(JsonUtility.ToJson(b));copy.Id="dense-"+row;
            copy.Start.y=row;copy.End.y=row;copy.StyleId=row%4==0?"stone":"covered-wood";
            d.Bridges.Add(copy);world.AddDistrictBridge(d,copy);
        }
        camera=world.WorldCamera;camera.orthographic=true;camera.orthographicSize=245;
        camera.transform.position=new Vector3(350,350,-350);camera.transform.LookAt(Vector3.zero);camera.targetTexture=target;
        camera.Render();GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        long denseBefore=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();var frameTimes=new double[30];
        var frameWatch=new Stopwatch();
        for(int i=0;i<frameTimes.Length;i++){frameWatch.Restart();camera.Render();frameWatch.Stop();frameTimes[i]=frameWatch.Elapsed.TotalMilliseconds;}
        Array.Sort(frameTimes);
        report+=$"Dense render: {d.Bridges.Count} bridges, {go.GetComponentsInChildren<Renderer>().Count(r=>r.name=="Bridge span"||r.name=="Approaches")} bridge renderers; 30 synchronous Camera.Render calls median={frameTimes[15]:F2} ms max={frameTimes[29]:F2} ms; heap delta={UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-denseBefore}; UnityStats draw calls={UnityStats.drawCalls} batches={UnityStats.batches}. This is render submission timing, not game frame timing.\n";
        RenderTexture.active=target;var denseImage=new Texture2D(1500,1000,TextureFormat.RGB24,false);denseImage.ReadPixels(new Rect(0,0,1500,1000),0,0);denseImage.Apply();File.WriteAllBytes(Path.Combine(output,"dense-bridges.png"),denseImage.EncodeToPNG());Object.DestroyImmediate(denseImage);
        frameWatch.Restart();denseBefore=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
        for(int i=0;i<100000;i++)world.TravelElevation(new Vector2((i%90)-45,(i%25)*20-235));
        frameWatch.Stop();report+=$"Dense travel: 100,000 indexed height queries, {frameWatch.Elapsed.TotalMilliseconds:F2} ms, heap delta={UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-denseBefore} bytes.\n";
        // Measure the retained edit boundary against a populated district, separate from pointer sampling.
        for(int i=0;i<20000;i++)d.Flora.Add(new(){InstanceId=i.ToString(),FloraId="oak",NormalizedX=(i%128)/128f,NormalizedZ=(i/128)/128f,Scale=1});
        for(int i=0;i<10000;i++)d.Roads.Add(new(){GridX=i%128,GridZ=i/128});
        var method=typeof(CityForgeV3.UI.CityForgeApp).GetMethod("DistrictCompositionKey",BindingFlags.Static|BindingFlags.NonPublic);
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        var timer=Stopwatch.StartNew();long memory=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();method.Invoke(null,new object[]{d});timer.Stop();
        report+=$"Existing edit composition key (20,000 flora, 10,000 roads): {timer.Elapsed.TotalMilliseconds:F2} ms, {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-memory} bytes\n";
        timer.Restart();memory=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();new DistrictRoadDelivery(d);timer.Stop();
        report+=$"Existing navigation construction (10,000 roads): {timer.Elapsed.TotalMilliseconds:F2} ms, {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-memory} bytes\n";
        var cached=DistrictRoadDelivery.For(d);timer.Restart();memory=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
        for(int i=0;i<100000;i++)if(!ReferenceEquals(cached,DistrictRoadDelivery.For(d)))throw new Exception("Network cache changed without edits");
        timer.Stop();report+=$"Cached network (10,000 roads, 100,000 lookups): {timer.Elapsed.TotalMilliseconds:F2} ms, {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-memory} heap delta bytes\n";
        File.WriteAllText(Path.Combine(output,"report.txt"),report);UnityEngine.Debug.Log(report);
        if(camera!=null)camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(target);Object.DestroyImmediate(go);
    }
}
#endif
