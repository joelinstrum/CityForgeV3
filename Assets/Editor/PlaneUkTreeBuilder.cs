#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
public static class PlaneUkTreeBuilder
{
 const string Root="Assets/CityForgeV3/Resources/CityForgeV3/Flora/PlaneUK3DV01/";
 [MenuItem("City Forge/Flora/Build Plane UK Trees")]
 public static void Build()
 {
  var shader=Shader.Find("CityForgeV3/Flora/Plane UK Cutout");
  if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Tree shader missing or failed");
  foreach(var file in Directory.GetFiles(Root+"Textures","*.png")){
   var importer=(TextureImporter)AssetImporter.GetAtPath(file);bool normal=file.Contains("Normal");
   importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!normal;importer.alphaIsTransparency=!normal;importer.maxTextureSize=2048;importer.SaveAndReimport();
  }
  var report="";
  foreach(var variant in new[]{"a","b"})foreach(var season in new[]{"summer","autumn","winter"}){
   var sourcePath=Root+"plane-"+variant+"-"+(season=="winter"?"winter":"summer")+".fbx";
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);if(source==null)throw new Exception(sourcePath);
   var instance=Object.Instantiate(source);instance.name="Plane UK "+variant+" "+season;
   try{
    foreach(var r in instance.GetComponentsInChildren<MeshRenderer>()){
     var mats=r.sharedMaterials;
     for(var i=0;i<mats.Length;i++){
      var name=mats[i]!=null?mats[i].name:"";
      var role=name.Contains("LeafCard")?"Leaf_Card":name.Contains("Leaves")?"Leaves":name.Contains("Bark")?"Bark":"Cap";
      var texture=role=="Cap"?(variant=="a"?"Cap_02":"Cap"):"London_Plane_"+role;
      var color=texture+((role=="Leaves"||role=="Leaf_Card")?(season=="autumn"?"_fall_transp":"_transp"):"");
      var matPath=Root+"Material-"+color+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
      if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}
      mat.shader=shader;mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+color+".png");
      mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+texture+"_Normal.png"));mat.SetColor("_Color",Color.white);mat.enableInstancing=true;
      if(mat.mainTexture==null)throw new Exception("Missing color "+color);
      mats[i]=mat;EditorUtility.SetDirty(mat);
     }
     r.sharedMaterials=mats;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;
    }
    var renderers=instance.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
    if(season=="winter" ? bounds.size.y<9f||bounds.size.y>12.1f : Mathf.Abs(bounds.size.y-12f)>.1f)throw new Exception("Tree scale wrong: "+bounds.size);
    PrefabUtility.SaveAsPrefabAsset(instance,Root+"Tree-"+variant+"-"+season+".prefab");
    var triangles=instance.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);
    report+=$"{variant} {season}: triangles={triangles} height={bounds.size.y} materials={renderers.Sum(r=>r.sharedMaterials.Length)}\n";
   }finally{Object.DestroyImmediate(instance);}
   var png=Root+"plane-"+variant+"-"+season+".png";var t=(TextureImporter)AssetImporter.GetAtPath(png);t.isReadable=true;t.alphaIsTransparency=true;t.sRGBTexture=true;t.mipmapEnabled=true;t.SaveAndReimport();
  }
  AssetDatabase.SaveAssets();Directory.CreateDirectory("QA/PlaneUK");File.WriteAllText("QA/PlaneUK/assets.txt",report);
 }
}
#endif
