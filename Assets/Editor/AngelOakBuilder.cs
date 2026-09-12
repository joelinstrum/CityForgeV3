#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
public static class AngelOakBuilder
{
 const string Root="Assets/CityForgeV3/Resources/CityForgeV3/Flora/TreeRepairsV01/";
 [MenuItem("City Forge/Flora/Build Tree Repairs")]
 public static void Build()
 {
  foreach(var path in Directory.GetFiles(Root,"*",SearchOption.AllDirectories))
  {
   if(!path.EndsWith(".png")&&!path.EndsWith(".jpeg"))continue;
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.alphaIsTransparency=true;importer.sRGBTexture=!path.Contains("opacity");importer.maxTextureSize=2048;importer.SaveAndReimport();
  }
  var shader=Shader.Find("CityForgeV3/Flora/Angel Oak Cutout");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Angel shader failed");
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"angel-oak.fbx");var obj=UnityEngine.Object.Instantiate(source);obj.name="AngelOak";
  try{
   foreach(var renderer in obj.GetComponentsInChildren<MeshRenderer>()){
    var mats=renderer.sharedMaterials;
    for(int i=0;i<mats.Length;i++){
     string name="AngelMaterial"+i;var path=Root+name+".mat";
     var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;mat.enableInstancing=true;mat.SetColor("_Color",Color.white);mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"AngelTextures/"+name+"-color.jpeg"));
     var opacity=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"AngelTextures/"+name+"-opacity.jpeg");mat.SetTexture("_OpacityMap",opacity==null?Texture2D.whiteTexture:opacity);mats[i]=mat;EditorUtility.SetDirty(mat);
    }
    renderer.sharedMaterials=mats;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
   }
   PrefabUtility.SaveAsPrefabAsset(obj,Root+"AngelOak.prefab");AssetDatabase.SaveAssets();
  }finally{UnityEngine.Object.DestroyImmediate(obj);}
 }
}
#endif
