#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CityForgeV3.Buildings3D;
using CityForgeV3.World;
using CityForgeV3.UI;
public static class DryGoodsV06Builder
{
 const string Root="Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/DryGoodsV06";
 const string Command="/tmp/cityforge-drygoods-v06-command.txt";
 const string Report="/Users/joelinstrum/dev/CityForgeMCP/artifacts/buildings/dry-goods/v06-siding-sun/runtime-report.json";
 [Serializable] public class Maps { public string Color,Normal,Roughness; }
 [Serializable] public class MeshInfo { public string name,kind; public Maps textures; public int triangles; }
 [Serializable] public class Room { public string id,renderer; public bool lit; }
 [Serializable] public class Anchor { public string id,room; public float[] position; }
 [Serializable] public class Manifest { public MeshInfo[] meshes; public Room[] rooms; public Anchor[] lights; }
  static void Register(){EditorApplication.update+=Poll;}
 static void Poll()
 {
  if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Command))return;
  var cmd=File.ReadAllText(Command).Trim();File.Delete(Command);
  try {
   switch(cmd){case "build":Build();break;case "tests":Tests();break;case "play":EditorApplication.isPaused=false;EditorApplication.isPlaying=true;break;case "stop":EditorApplication.isPlaying=false;break;
    case "load":UnityEngine.Object.FindFirstObjectByType<CityForgeApp>()?.OpenDryGoodsQa();break;
    case "night":World()?.SetTimeOfDay(TimeOfDayPreset.Night);break;case "day":World()?.SetTimeOfDay(TimeOfDayPreset.Noon);break;case "evening":World()?.SetTimeOfDay(TimeOfDayPreset.Evening);break;
    case "rotate":World()?.RotateSelectedBuilding3D(1);break;case "door":World()?.ToggleSelectedBuildingDoor();break;
    case "lights":File.WriteAllLines(Report.Replace(".json","-lights.txt"),UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).Select(l=>l.name+" enabled="+l.enabled+" active="+l.gameObject.activeInHierarchy+" intensity="+l.intensity+" ray="+l.transform.forward+" mask="+l.cullingMask));break;case "district":UnityEngine.Object.FindFirstObjectByType<CityForgeApp>()?.OpenDryGoodsDistrictQa();break;case "district-night":UnityEngine.Object.FindFirstObjectByType<CityForgeApp>()?.SetDryGoodsDistrictQaNight(true);break;case "district-afternoon":UnityEngine.Object.FindFirstObjectByType<CityForgeApp>()?.SetDryGoodsDistrictQaNight(false);break;case "hosted":Hosted();break;case "select":World()?.SelectBuilding3DForQa(0);break;case "focus":Focus();break;case "afternoon":World()?.SetTimeOfDay(TimeOfDayPreset.Afternoon);break;case "closeview":World()?.SetQaOrthographicSize(17f);break;case "deselect":World()?.DeselectBuilding3D();break;case "validate":Validate();break;case "dump":Dump();break;
   }
   File.WriteAllText("/tmp/cityforge-drygoods-v06-result.txt",cmd+": OK");
  }catch(Exception e){Debug.LogException(e);File.WriteAllText("/tmp/cityforge-drygoods-v06-result.txt",cmd+": "+e);}
 }
 static LotWorldController World()=>UnityEngine.Object.FindFirstObjectByType<LotWorldController>();
 [MenuItem("City Forge/3D Buildings/Create Dry Goods Package v06")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play before building");
  Directory.CreateDirectory(Root+"/Meshes");AssetDatabase.Refresh();
  var import=AssetImporter.GetAtPath(Root+"/Source/DryGoodsV06.fbx") as ModelImporter;
  import.materialImportMode=ModelImporterMaterialImportMode.None;import.animationType=ModelImporterAnimationType.Generic;import.importAnimation=true;import.globalScale=1;import.SaveAndReimport();
  foreach(var file in Directory.GetFiles(Root+"/Textures","*.png")){
   var ti=(TextureImporter)AssetImporter.GetAtPath(file);if(ti==null)continue;
   ti.maxTextureSize=file.Contains("Roof_")||file.Contains("Shell_")?4096:2048;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.mipmapEnabled=true;
   if(file.EndsWith("_Normal.png")){ti.textureType=TextureImporterType.NormalMap;ti.sRGBTexture=false;}
   else {ti.textureType=TextureImporterType.Default;ti.sRGBTexture=file.EndsWith("_Color.png");}
   ti.SaveAndReimport();
  }
  var cfg=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/export-manifest.json"));
  var visual=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Source/DryGoodsV06.fbx"));visual.name="DryGoodsVisual";
  try {
   foreach(var anim in visual.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(anim);
   var renderers=visual.GetComponentsInChildren<Renderer>(true);
   foreach(var info in cfg.meshes){
    var renderer=renderers.Single(x=>x.name==info.name);var path=Root+"/Materials/"+info.name+".mat";
    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
    mat.color=Color.white;mat.SetFloat("_Metallic",0);mat.SetFloat("_Glossiness",.15f);mat.SetFloat("_SpecularHighlights",0);mat.SetFloat("_GlossyReflections",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
    if(info.kind=="opaque"){
     mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+info.textures.Color);
     mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+info.textures.Normal));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",1);
     mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+info.textures.Roughness.Replace("_Roughness","_MetallicSmoothness")));mat.EnableKeyword("_METALLICGLOSSMAP");mat.SetFloat("_GlossMapScale",1);
    }else if(info.kind=="glass"){
     mat.color=new Color(.85f,.9f,.9f,.12f);mat.SetFloat("_Mode",3);mat.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);mat.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");mat.renderQueue=3000;
    }else {
     mat.color=new Color(.025f,.035f,.035f,1);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(2.5f,1.175f,.45f));mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }
    renderer.sharedMaterials=new[]{mat};EditorUtility.SetDirty(mat);
   }
   // Convert FBX centimeter/axis transforms into native Unity-space mesh data.
   foreach(var renderer in renderers){
    var mf=renderer.GetComponent<MeshFilter>();var original=mf.sharedMesh;var mesh=UnityEngine.Object.Instantiate(original);mesh.name=renderer.name;
    var matrix=visual.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
    mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();mesh.normals=mesh.normals.Select(n=>normalMatrix.MultiplyVector(n).normalized).ToArray();
    mesh.tangents=mesh.tangents.Select(t=>{var v=matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w*(matrix.determinant<0?-1:1));}).ToArray();mesh.RecalculateBounds();
    var meshPath=Root+"/Meshes/"+renderer.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(old==null){AssetDatabase.CreateAsset(mesh,meshPath);old=mesh;}else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);}
    mf.sharedMesh=old;renderer.transform.SetParent(visual.transform,false);renderer.transform.localPosition=Vector3.zero;renderer.transform.localRotation=Quaternion.identity;renderer.transform.localScale=Vector3.one;
   }
   var oldHinge=visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name=="DG_GameDoorHinge");if(oldHinge!=null)UnityEngine.Object.DestroyImmediate(oldHinge.gameObject);
   var nativeHinge=new GameObject("DG_GameDoorHinge").transform;nativeHinge.SetParent(visual.transform,false);nativeHinge.localPosition=new Vector3(-.615f,.495f,2.86f);
   foreach(var renderer in renderers.Where(x=>x.name=="DG_Door" || x.name=="DG_DoorGlass"))renderer.transform.SetParent(nativeHinge,true);
   var lighting=new GameObject("RoomLighting");lighting.transform.SetParent(visual.transform,false);
   foreach(var room in cfg.rooms){
    var node=new GameObject("Room_"+room.id);node.transform.SetParent(lighting.transform,false);
    var source=cfg.lights.FirstOrDefault(x=>x.room==room.id);
    if(source!=null)node.transform.localPosition=new Vector3(source.position[0],source.position[1],source.position[2]);
    var ctl=node.AddComponent<BuildingNightLighting>();ctl.ConfigurePane(renderers.Single(x=>x.name==room.renderer),0,new Color(1,.47f,.18f));
    ctl.ConfigureAnchors(source!=null?new[]{node.transform}:Array.Empty<Transform>(),null);ctl.ConfigureTuning(2.5f,.08f,1.1f,0,0);ctl.ConfigureOccupancy(1,false);ctl.SetRoomLit(room.lit);ctl.SetNightAmount(0);
   }
   foreach(var anchor in cfg.lights.Where(x=>x.room=="storefront")){
    var node=new GameObject(anchor.id);node.transform.SetParent(lighting.transform,false);node.transform.localPosition=new Vector3(anchor.position[0],anchor.position[1],anchor.position[2]);
    var ctl=node.AddComponent<BuildingNightLighting>();if(anchor.id.Contains("Lantern"))ctl.ConfigurePane(renderers.Single(x=>x.name=="DG_LanternGlow"),0,new Color(1,.47f,.18f));
    ctl.ConfigurePerPixel(true);ctl.ConfigureAnchors(new[]{node.transform},null);ctl.ConfigureTuning(2.5f,8f,anchor.id.Contains("Display")?2.5f:2.8f,0,0);ctl.ConfigureOccupancy(1,false);ctl.SetRoomLit(true);ctl.SetNightAmount(0);
   }
   var hinge=visual.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="DG_GameDoorHinge");visual.AddComponent<BuildingDoorController>().Configure(hinge,-90);
   var metrics=CityForge.Editor.Building3DPackageValidator.Measure(visual);
   var prefab=PrefabUtility.SaveAsPrefabAsset(visual,Root+"/Prefabs/DryGoodsVisual.prefab");
   var package=AssetDatabase.LoadAssetAtPath<Building3DPackage>(Root+"/DryGoodsV06.asset");if(package==null){package=ScriptableObject.CreateInstance<Building3DPackage>();AssetDatabase.CreateAsset(package,Root+"/DryGoodsV06.asset");}
   package.AssetId="dry-goods-v05";package.SourceProvenance="Joel-approved Dry Goods v04; baked procedural materials, independent upper lights and hinged door.";package.AuthoredScale=Vector3.one;package.FrontYawDegrees=90;package.UseCrossFade=false;package.FootprintMeters=new Vector2(7.8f,6);
   package.Representations=new List<Building3DRepresentation>{new Building3DRepresentation{Level=Building3DLevel.LOD0,ScreenRelativeHeight=.001f,VisualPrefab=prefab,LocalPosition=Vector3.zero,LocalScale=Vector3.one,TargetTriangleBudget=metrics.Triangles,Provenance="Geometry consolidated to 29 renderers; original foundation anchor retained."}};EditorUtility.SetDirty(package);
   var root=new GameObject("Dry Goods");try{root.AddComponent<Building3DPackageInstance>().Configure(package);PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/DryGoodsV06.prefab");}finally{UnityEngine.Object.DestroyImmediate(root);}
   AssetDatabase.SaveAssets();BuildingContentCatalog.InvalidateCache();
   File.WriteAllText(Report,JsonUtility.ToJson(new BuildReport{meshCount=renderers.Length,triangles=metrics.Triangles,height=metrics.Bounds.size.y,footprint=package.FootprintMeters.ToString()},true));
  }finally{UnityEngine.Object.DestroyImmediate(visual);}
 }
 static UnityEditor.TestTools.TestRunner.Api.TestRunnerApi runner;
 static void Tests(){runner=ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();runner.RegisterCallbacks(new TestResults());runner.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter{testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,groupNames=new[]{"^CityForgeV3.Tests.EditMode.BuildingDoorControllerTests","^CityForgeV3.Tests.EditMode.DistrictAfternoonLightingTests"}}));}
 class TestResults:UnityEditor.TestTools.TestRunner.Api.ICallbacks{
  public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor t){}public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor t){}public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor r){}
  public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor r){UnityEditor.TestTools.TestRunner.Api.TestRunnerApi.SaveResultToFile(r,"/Users/joelinstrum/dev/CityForgeMCP/artifacts/buildings/dry-goods/v06-siding-sun/revision-tests.xml");UnityEngine.Object.DestroyImmediate(runner);}
 }
 [Serializable] class BuildReport{public int meshCount,triangles;public float height;public string footprint;}
 static Building3DPackageInstance Target(){var roots=(List<GameObject>)typeof(LotWorldController).GetField("_experimentalBuilding3DVisibleRoots",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(World());return roots.Select(x=>x.GetComponent<Building3DPackageInstance>()).Single(x=>x!=null && x.Package.AssetId=="dry-goods-v05");}
 static void Hosted(){var w=World();const System.Reflection.BindingFlags f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var camera=(Camera)typeof(LotWorldController).GetField("_camera",f).GetValue(w);var sun=(Light)typeof(LotWorldController).GetField("_sun",f).GetValue(w);w.ConfigureAsDistrictHosted(camera,sun,w.ZoomLevel);w.SetTimeOfDay(TimeOfDayPreset.Afternoon);var ray=(Vector3)typeof(LotWorldController).GetMethod("ProjectedObjectShadowRay",f).Invoke(w,null);File.WriteAllText(Report.Replace(".json","-sun.txt"),"angle="+Vector3.Angle(ray,sun.transform.forward)+" outgoingRay="+ray+" actualSun="+sun.transform.forward);}
 static void Focus(){var c=(Camera)typeof(LotWorldController).GetField("_camera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(World());c.transform.position=new Vector3(0,6,0)-c.transform.forward*50;c.orthographicSize=10;}
 static void Dump(){var r=Target();var lines=new List<string>();foreach(var t in r.GetComponentsInChildren<Transform>(true))if(t.GetComponent<Renderer>()!=null || t.GetComponent<Light>()!=null || t.name.Contains("Visual"))lines.Add(t.name+" world="+t.position+" local="+t.localPosition+" rot="+t.localEulerAngles+" scale="+t.lossyScale+(t.GetComponent<Light>() is Light l && l != null ? " energy="+l.intensity+" enabled="+l.enabled : ""));File.WriteAllLines(Report.Replace(".json","-transforms.txt"),lines);}
 static void Validate(){
  var root=Target();var rooms=root.GetComponentsInChildren<BuildingNightLighting>(true);if(rooms.Length!=25)throw new Exception("Expected 21 sash and 4 storefront controls");
  root.SetNightAmount(0);if(rooms.Sum(x=>x.ActiveRuntimeLightCount)!=0)throw new Exception("Day lights remain on");root.SetNightAmount(1);if(rooms.Sum(x=>x.ActiveRuntimeLightCount)!=20)throw new Exception("Expected 16 upper and 4 storefront lights");
  var states=rooms.Select(x=>x.RoomLit).ToArray();for(int i=0;i<rooms.Length;i++){
   for(int j=0;j<rooms.Length;j++)rooms[j].SetRoomLit(i==j);
   if(rooms.Sum(x=>x.ActiveRuntimeLightCount)>1)throw new Exception("Room light isolation failed");
  }for(int i=0;i<rooms.Length;i++)rooms[i].SetRoomLit(states[i]);
  var door=root.GetComponentInChildren<BuildingDoorController>(true);door.SetOpen(false,true);door.SetOpen(true);door.Advance(.5f);if(Mathf.Abs(door.OpenAmount-.5f)>.001f)throw new Exception("Door timing");door.Advance(.5f);if(door.OpenAmount!=1)throw new Exception("Door open");door.SetOpen(false);door.Advance(1);if(door.OpenAmount!=0)throw new Exception("Door close");
  World().SetTimeOfDay(TimeOfDayPreset.Night);World().SelectBuilding3DForQa(0);World().ToggleSelectedBuildingDoor();
  var initial=Target().transform.localRotation;for(int turn=0;turn<8;turn++){if(!World().RotateSelectedBuilding3D(1))throw new Exception("Rotation failed");var current=Target();if(!current.GetComponentInChildren<BuildingDoorController>().IsOpen)throw new Exception("Door state lost after rebuild");if(current.GetComponentsInChildren<BuildingNightLighting>().Sum(x=>x.ActiveRuntimeLightCount)!=20)throw new Exception("Night state lost after rebuild");}
  if(Quaternion.Angle(initial,Target().transform.localRotation)>.01f)throw new Exception("Full rotation drift");World().ToggleSelectedBuildingDoor();
  World().ToggleSelectedBuildingDoor();var session=(LotEditorSession)typeof(LotWorldController).GetField("_session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(World());var saveRoot=Path.Combine(Path.GetDirectoryName(Report),"qa-lot");LotSaveStore.Save(session,Array.Empty<string>(),saveRoot);if(!World().LoadLot(session.Data.LotId,saveRoot))throw new Exception("QA save reload failed");if(!Target().GetComponentInChildren<BuildingDoorController>().IsOpen)throw new Exception("Door state lost on disk reload");
  File.WriteAllText(Report.Replace(".json","-checks.txt"),"25 controls; 20 active night lights; all off in day; independent light isolation; one-second door open/close; all 8 rotations preserve door and lights and return to original orientation; disk save/reload restores open door passed.");World()?.SetTimeOfDay(TimeOfDayPreset.Night);
 }
}
#endif
