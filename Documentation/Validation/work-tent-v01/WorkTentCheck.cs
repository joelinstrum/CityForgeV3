using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CityForgeV3.World;
public static class WorkTentCheck {
 public static void Run() {
 string dir="Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/WorkTentV01";
 foreach(var path in Directory.GetFiles(dir,"*",SearchOption.AllDirectories)) {
 var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
 if(path.EndsWith("_2.png")){importer.textureType=TextureImporterType.NormalMap;importer.sRGBTexture=false;importer.SaveAndReimport();}
 }
 BuildingContentCatalog.InvalidateCache();var entry=BuildingContentCatalog.Find("work-tent-v01");if(entry==null)throw new Exception("No catalog entry");
 var source=BuildingContentCatalog.LoadModel(entry);if(source==null)throw new Exception("No model");
 var root=UnityEngine.Object.Instantiate(source);root.transform.rotation=Quaternion.Euler(entry.pitchDegrees,entry.baseYawDegrees,0);
 var host=new GameObject("Fixture factory");host.SetActive(false);var factory=host.AddComponent<LotWorldController>();
 typeof(LotWorldController).GetMethod("ApplyBuildingContentContract",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(factory,new object[]{root.transform,entry});
 var rs=root.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 if(Mathf.Abs(b.size.y-3.2f)>.02f)throw new Exception("Wrong height "+b.size);
 foreach(var r in rs)foreach(var m in r.sharedMaterials)if(m==null||m.mainTexture==null)throw new Exception("Missing albedo");
 var camera=new GameObject("Fixture camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.5f;camera.transform.position=b.center+new Vector3(8,6,-8);camera.transform.LookAt(b.center);camera.backgroundColor=new Color(.16f,.21f,.25f);camera.clearFlags=CameraClearFlags.SolidColor;
 var sun=new GameObject("Fixture sun").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(45,-35,0);sun.intensity=1.2f;RenderSettings.ambientLight=Color.gray;
 var rt=new RenderTexture(640,640,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(640,640,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();File.WriteAllBytes("work-tent-unity.png",tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;
 File.WriteAllText("work-tent-check.txt","PASS: catalog lookup, original FBX loads, all material slots have albedo, shared building contract height="+b.size.y+", bounds="+b.size+". Unity render generated. No player saves.");
 UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(sun.gameObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
 }
}
