using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using CityForgeV3.World;using CityForgeV3.Buildings3D;
 public static class DryGoodsNoonReview {
 const string Root="/Users/joelinstrum/dev/CityForgeMCP/artifacts/buildings/dry-goods/v07-noon-lighting/";
 static DryGoodsNoonReview(){EditorApplication.update+=Poll;}
 static void Poll(){var p="/tmp/cityforge-noon-command.txt";if(!File.Exists(p))return;var c=File.ReadAllText(p).Trim();File.Delete(p);try{
 var d=UnityEngine.Object.FindFirstObjectByType<DistrictWorldController>();
 if(c=="baseline"||c=="candidate"){
 d.SetTimeOfDay(TimeOfDayPreset.Noon);
 if(c=="baseline") {d.WorldSun.intensity=0.55f;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=TimeOfDayLighting.For(TimeOfDayPreset.Noon).AmbientColor;var v=d.WorldSun.transform.forward;d.WorldSun.transform.rotation=Quaternion.LookRotation(new Vector3(-v.x,v.y,-v.z));foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.type==LightType.Directional&&l.transform.parent==null)l.enabled=true;}
 var pkg=d.GetComponentsInChildren<Building3DPackageInstance>().First(x=>x.Package!=null&&x.Package.AssetId=="dry-goods-v05");var b=pkg.GetComponentsInChildren<Renderer>().First().bounds;foreach(var rend in pkg.GetComponentsInChildren<Renderer>())b.Encapsulate(rend.bounds);var cam=d.WorldCamera;var pan=new Vector2(b.center.x,b.center.z)+Vector2.one*(b.center.y/Mathf.Tan(20*Mathf.Deg2Rad)/Mathf.Sqrt(2));var app=UnityEngine.Object.FindFirstObjectByType<CityForgeV3.UI.CityForgeApp>();typeof(CityForgeV3.UI.CityForgeApp).GetField("_terraformPanOffset",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(app,pan);d.SetPan(pan);cam.orthographicSize=9;
 File.WriteAllText(Root+c+"-lighting.txt","Ambient="+RenderSettings.ambientMode+" sky="+RenderSettings.ambientSkyColor+" equator="+RenderSettings.ambientEquatorColor+" ground="+RenderSettings.ambientGroundColor+"\n"+string.Join("\n",UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).Select(l=>l.name+" enabled="+l.enabled+" intensity="+l.intensity+" ray="+l.transform.forward)));
 }
 File.WriteAllText("/tmp/cityforge-noon-result.txt","OK "+c);
 }catch(Exception e){File.WriteAllText("/tmp/cityforge-noon-result.txt",e.ToString());}}
}
