#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForgeV3.World;
using UnityEngine;
using Object=UnityEngine.Object;
public static class FixedStoneBridgeQa
{
    [Serializable] sealed class Module {public Vector3[] vertices;public int[] triangles;}
    [Serializable] sealed class Package {public float fixedLength;public Module[] modules;}
    public static void Run()
    {
        string output="/tmp/cityforge-fixed-bridge-qa";Directory.CreateDirectory(output);
        string report="";
        foreach(float width in new[]{24f,42f,64f})
        {
            var d=new RegionCityTile{Width=1,Height=1,TileId="fixed-bridge-qa"};
            d.Rivers.Add(new(){InstanceId="river",WidthMeters=width,Depth=DistrictRiverDepth.Deep,Points=new(){new(.5f,.1f),new(.5f,.9f)}});
            var host=new GameObject("Fixed bridge QA");var world=host.AddComponent<DistrictWorldController>();world.Build(d);
            if((world.WorldCamera.depthTextureMode&DepthTextureMode.Depth)==0)
                throw new Exception("River depth transparency camera texture is disabled");
            if(!DistrictBridgePlanner.TryPlan(d,new(27,32),Vector2Int.right,world.SampleBridgeSurface,_=>false,out var proposal,out var reason))throw new Exception(reason);
            bool shortFits=false,longFits=false;
            foreach(var style in DistrictBridgeCatalog.Styles.Where(s=>s.Id.StartsWith("stone-")))
            {
                bool fits=world.TryFitBridgeStyle(d,proposal,style.Id,_=>false,out var b,out reason);
                report+=$"{width} m river / {style.Id}: {(fits?"available":reason)}\n";
                if(style.Id=="stone-original")shortFits=fits;else longFits=fits;
                if(!fits)continue;
                world.AddDistrictBridge(d,b);
                var asset=JsonUtility.FromJson<Package>(Resources.Load<TextAsset>(style.Resource+"/modules").text);
                var body=host.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Bridge span").sharedMesh;
                var vertices=body.vertices;
                if(vertices.Length!=asset.modules[0].vertices.Length || !body.triangles.SequenceEqual(asset.modules[0].triangles))throw new Exception("Whole mesh topology changed");
                for(int i=0;i<vertices.Length;i++)
                    if(Vector3.Distance(vertices[i],asset.modules[0].vertices[i]+Vector3.forward*b.NearApproach)>.0001f)
                        throw new Exception("Original bridge vertex was deformed");
                if(Mathf.Abs(body.bounds.size.z-asset.fixedLength)>.001f)throw new Exception("Fixed bridge was stretched");
                var a=DistrictBridgePlanner.Center(d,b.Start);var z=DistrictBridgePlanner.Center(d,b.End);var center=(a+z)*.5f;
                foreach(float joint in new[]{b.NearApproach,Vector2.Distance(a,z)-b.FarApproach})
                    if(Mathf.Abs(world.TravelElevation(a+Vector2.right*(joint-.001f))-world.TravelElevation(a+Vector2.right*(joint+.001f)))>.02f)
                        throw new Exception("Whole-model approach travel discontinuity");
                var camera=world.WorldCamera;camera.orthographic=true;camera.orthographicSize=40;
                camera.transform.position=new Vector3(center.x+70,50,center.y-85);camera.transform.LookAt(new Vector3(center.x,1,center.y));
                var target=new RenderTexture(1500,1000,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(output,$"{style.Id}-{width}.png"),image.EncodeToPNG());Object.DestroyImmediate(image);
                camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(target);
                world.RemoveDistrictBridge(d,b);
                report+=$"  Original topology and all {vertices.Length} vertices preserved; length {asset.fixedLength:F2} m; approaches {b.NearApproach:F2}/{b.FarApproach:F2} m.\n";
            }
            Debug.Log(report);
            if(width==24 && (!shortFits||!longFits))throw new Exception("Both models should fit narrow river");
            if(width==42 && (shortFits||!longFits))throw new Exception("Only long model should fit medium river");
            if(width==64 && (shortFits||longFits))throw new Exception("Neither fixed model should fit wide river");
            // Verify the actual modal excludes models that fail the bank-fit check.
            var appHost=new GameObject("Fixed chooser QA");appHost.SetActive(false);var app=appHost.AddComponent<CityForgeV3.UI.CityForgeApp>();
            var root=new UnityEngine.UIElements.VisualElement();var region=new RegionSaveData();region.Tiles.Add(d);
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            void Field(string name,object value)=>app.GetType().GetField(name,flags).SetValue(app,value);
            Field("_root",root);Field("_openRegion",region);Field("_selectedRegionTileId",d.TileId);Field("_districtWorld",world);
            proposal.StyleId=shortFits?"stone-original":longFits?"stone-long":"covered-wood";
            app.GetType().GetMethod("ComposeDistrictBridgeModal",flags).Invoke(app,new object[]{proposal});
            var labels=UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(root).ToList();
            if(labels.Any(l=>l.text=="STONE ARCH BRIDGE")!=shortFits || labels.Any(l=>l.text=="LONG STONE ARCH BRIDGE")!=longFits)throw new Exception("Chooser offers a model that does not fit");
            if(!shortFits && !labels.Any(l=>l.name=="bridge-unavailable-reasons" && l.text.Contains("Stone Arch Bridge") && l.text.Contains("fixed 34")))
                throw new Exception("Hidden original model has no fit explanation");
            if(!longFits && !labels.Any(l=>l.name=="bridge-unavailable-reasons" && l.text.Contains("Long Stone Arch Bridge") && l.text.Contains("fixed 50")))
                throw new Exception("Hidden long model has no fit explanation");
            if(shortFits||longFits)
            {
                Field("_currentScreen",Enum.Parse(app.GetType().GetField("_currentScreen",flags).FieldType,"DistrictTerraform"));
                app.GetType().GetMethod("EnsureDistrictUndo",flags).Invoke(app,new object[]{d});
                if(!world.TryFitBridgeStyle(d,proposal,proposal.StyleId,_=>false,out var build,out _))throw new Exception("Selected fixed model lost its fit");
                app.GetType().GetMethod("BuildDistrictBridge",flags).Invoke(app,new object[]{d,build});
                if(d.Bridges.Count!=1||!d.Bridges[0].FixedModel)throw new Exception("Fixed model build transaction failed");
                string saved=JsonUtility.ToJson(d);var undo=(DistrictUndoHistory)app.GetType().GetField("_districtUndo",flags).GetValue(app);
                if(!undo.TryUndo(out var before))throw new Exception("Fixed model build was not undoable");
                JsonUtility.FromJsonOverwrite(before,d);world.Build(d);
                if(d.Bridges.Count!=0)throw new Exception("Fixed model undo failed");
                JsonUtility.FromJsonOverwrite(saved,d);world.Build(d);
                if(!d.Bridges[0].FixedModel || host.GetComponentsInChildren<MeshFilter>().Count(f=>f.name=="Bridge span")!=1)throw new Exception("Fixed model reload failed");
                report+="  Build, in-memory undo, and serialized reload passed.\n";
                if(width==42)
                {
                    var prototype=d.Bridges[0];
                    for(int row=8;row<=56;row+=2)
                    {
                        if(row==prototype.Start.y)continue;
                        var copy=JsonUtility.FromJson<PlacedDistrictBridge>(JsonUtility.ToJson(prototype));copy.Id="fixed-dense-"+row;
                        copy.Start.y=row;copy.End.y=row;d.Bridges.Add(copy);world.AddDistrictBridge(d,copy);
                    }
                    var camera=world.WorldCamera;camera.orthographic=true;camera.orthographicSize=250;
                    camera.transform.position=new Vector3(350,350,-350);camera.transform.LookAt(Vector3.zero);
                    var target=new RenderTexture(1500,1000,24);camera.targetTexture=target;camera.Render();
                    GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
                    var timer=new System.Diagnostics.Stopwatch();var times=new double[30];
                    for(int i=0;i<times.Length;i++){timer.Restart();camera.Render();timer.Stop();times[i]=timer.Elapsed.TotalMilliseconds;}
                    Array.Sort(times);
                    long heapBefore=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();timer.Restart();
                    for(int i=0;i<100000;i++)world.TravelElevation(new Vector2((i%90)-45,(i%25)*20-235));
                    timer.Stop();
                    report+=$"  25 long bridges: {host.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Bridge span"||f.name=="Approaches"||f.name=="Graded earth approaches").Count()} renderers; camera CPU submission median {times[15]:F2} ms / max {times[29]:F2} ms; 100,000 travel queries {timer.Elapsed.TotalMilliseconds:F2} ms / heap delta {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-heapBefore} bytes. GPU/full frame time unverified.\n";
                    camera.targetTexture=null;Object.DestroyImmediate(target);
                }
            }
            Object.DestroyImmediate(appHost);Object.DestroyImmediate(host);
        }
        File.WriteAllText(Path.Combine(output,"report.txt"),report);Debug.Log(report);
    }
}
#endif
