#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CityForgeV3.World;
using CityForgeV3.UI;
[InitializeOnLoad] public static class CragPilotReview
{
 static CragPilotReview(){EditorApplication.update+=Poll;}
 [MenuItem("City Forge/QA/Crag Pilot/Build and focus")]static void BuildMenu()=>Run("build");
 static void Poll(){const string path="/tmp/cityforge-crag-pilot-command.txt";if(EditorApplication.isCompiling||!File.Exists(path))return;var c=File.ReadAllText(path).Trim();File.Delete(path);Run(c);}
 static void Run(string c){try{
 var world=UnityEngine.Object.FindObjectsByType<DistrictWorldController>(FindObjectsSortMode.None).First(w=>w.gameObject.activeInHierarchy);
 var pilot=world.GetComponentInChildren<DistrictCragPilot>(true);
 if(c=="build")pilot=DistrictCragPilot.Create(world);
 if(pilot==null)throw new Exception("Build pilot first");
 if(c=="build"||c=="focus"){
 var app=UnityEngine.Object.FindFirstObjectByType<CityForgeApp>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;
 typeof(CityForgeApp).GetField("_terraformPanOffset",flags).SetValue(app,pilot.Center);
 typeof(CityForgeApp).GetField("_districtEdgePanDirection",flags).SetValue(app,Vector2Int.zero);
 typeof(CityForgeApp).GetField("_terraformZoomLevel",flags).SetValue(app,DistrictZoomLevel.LOD1);
 world.SetZoom(DistrictZoomLevel.LOD1);world.SetPan(pilot.Center);world.SetTimeOfDay(TimeOfDayPreset.Noon);
 }
 if(c=="off")pilot.gameObject.SetActive(false);if(c=="on")pilot.gameObject.SetActive(true);
 if(c=="check"){
 if(pilot.RockCount<20||pilot.ExposedHeight<2)throw new Exception("Pilot lacks relief");
 foreach(var filter in pilot.GetComponentsInChildren<MeshFilter>(true)){
 var mesh=filter.sharedMesh;if(mesh==null||mesh.vertexCount==0)throw new Exception("Missing geometry");
 var verts=mesh.vertices;var norms=mesh.normals;
 for(int i=0;i<verts.Length;i++)if(float.IsNaN(verts[i].x)||norms[i].sqrMagnitude<.9f)throw new Exception("Invalid or inward face");
 var renderer=filter.GetComponent<MeshRenderer>();if(ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader))throw new Exception("Shader failed");
 }
 }
 File.WriteAllText("/tmp/cityforge-crag-pilot-result.txt","PASS "+c+" rocks="+pilot.RockCount+" center="+pilot.Center+" exposedMax="+pilot.ExposedHeight);
 }catch(Exception e){File.WriteAllText("/tmp/cityforge-crag-pilot-result.txt",e.ToString());}}
}
#endif
