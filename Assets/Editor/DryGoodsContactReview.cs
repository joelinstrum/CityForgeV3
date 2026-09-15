using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEngine;using CityForgeV3.Buildings3D;
[InitializeOnLoad] public static class DryGoodsContactReview {
 const string Root="Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/DryGoodsV09";
 const string Report="/Users/joelinstrum/dev/CityForgeMCP/artifacts/buildings/dry-goods/v09-contact-detail/";
 static Dictionary<Renderer,Material[]> original=new();static List<Material> copies=new();
 static DryGoodsContactReview(){EditorApplication.update+=Poll;}
 static void Restore(){foreach(var p in original)if(p.Key!=null)p.Key.sharedMaterials=p.Value;original.Clear();foreach(var m in copies)if(m!=null)UnityEngine.Object.DestroyImmediate(m);copies.Clear();}
 static void Poll(){var p="/tmp/cityforge-contact-command.txt";if(!File.Exists(p))return;var c=File.ReadAllText(p).Trim();File.Delete(p);try{
 if(c=="build"){Build();}
 else if(c=="import"){AssetDatabase.Refresh();foreach(var file in Directory.GetFiles(Root+"/Textures","*.png")){var ti=(TextureImporter)AssetImporter.GetAtPath(file);ti.sRGBTexture=false;ti.maxTextureSize=file.Contains("Shell")?4096:2048;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.mipmapEnabled=true;ti.SaveAndReimport();}}
 else {Restore();var pkg=UnityEngine.Object.FindObjectsByType<Building3DPackageInstance>(FindObjectsSortMode.None).First(x=>x.Package!=null&&x.Package.AssetId=="dry-goods-v05");var report=new List<string>();
 foreach(var r in pkg.GetComponentsInChildren<Renderer>()){var mats=r.sharedMaterials;report.Add("SEEN "+r.name+" shader="+(mats.Length>0?mats[0].shader.name:"none")+" enabled="+r.enabled);if(mats.Length==0||mats[0].shader.name!="Standard")continue;var key=r.name.Split('.')[0].Replace("DG_","");var map=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+key+"_Occlusion.png");if(map==null)continue;
 var m=new Material(mats[0]);copies.Add(m);original[r]=mats;r.sharedMaterial=m;
 report.Add(r.name+" color="+m.color+" albedo="+AssetDatabase.GetAssetPath(m.mainTexture)+" specular="+m.GetFloat("_SpecularHighlights")+" reflections="+m.GetFloat("_GlossyReflections"));
 if(c=="ao"){m.SetTexture("_OcclusionMap",map);m.SetFloat("_OcclusionStrength",.7f);}else if(c=="neutral"){m.shader=Shader.Find("Unlit/Texture");}
 }
 File.WriteAllLines(Report+c+"-materials.txt",report);
 }
 File.WriteAllText("/tmp/cityforge-contact-result.txt","OK "+c);
 }catch(Exception e){File.WriteAllText("/tmp/cityforge-contact-result.txt",e.ToString());}}
 static void Build(){
 if(EditorApplication.isPlaying)throw new Exception("Stop play before saving derivative");
 var old="Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/DryGoodsV06";
 Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
 var visual=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(old+"/Prefabs/DryGoodsVisual.prefab"));
 try {
 foreach(var r in visual.GetComponentsInChildren<Renderer>()){
 var key=r.name.Split('.')[0].Replace("DG_","");var map=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+key+"_Occlusion.png");if(map==null)continue;
 var mat=new Material(r.sharedMaterial);mat.name=r.sharedMaterial.name+"_Contact";mat.SetTexture("_OcclusionMap",map);mat.SetFloat("_OcclusionStrength",.7f);
 var p=Root+"/Materials/"+key+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(p);if(existing==null){AssetDatabase.CreateAsset(mat,p);existing=mat;}else{EditorUtility.CopySerialized(mat,existing);UnityEngine.Object.DestroyImmediate(mat);}r.sharedMaterial=existing;
 }
 var prefab=PrefabUtility.SaveAsPrefabAsset(visual,Root+"/Prefabs/DryGoodsVisual.prefab");
 var package=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Building3DPackage>(old+"/DryGoodsV06.asset"));package.SourceProvenance+="; v09 short-range .18m contact occlusion, original albedo unchanged";package.Representations[0].VisualPrefab=prefab;
 var target=Root+"/DryGoodsV09.asset";var saved=AssetDatabase.LoadAssetAtPath<Building3DPackage>(target);if(saved==null){AssetDatabase.CreateAsset(package,target);saved=package;}else{EditorUtility.CopySerialized(package,saved);UnityEngine.Object.DestroyImmediate(package);}
 var obj=new GameObject("Dry Goods");try{obj.AddComponent<Building3DPackageInstance>().Configure(saved);PrefabUtility.SaveAsPrefabAsset(obj,Root+"/Prefabs/DryGoodsV09.prefab");}finally{UnityEngine.Object.DestroyImmediate(obj);}
 AssetDatabase.SaveAssets();
 }finally{UnityEngine.Object.DestroyImmediate(visual);}
 }

}
