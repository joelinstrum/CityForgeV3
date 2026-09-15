#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using CityForgeV3.UI;
 public static class BrickworksReview
{
 const string Root="Assets/CityForgeV3/Resources/CityForgeV3/Industry/BrickworksV01/";
 static BrickworksReview(){EditorApplication.update+=Poll;}
 static void Poll()
 {
  const string command="/tmp/cityforge-brickworks-command.txt";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(command))return;
  string c=File.ReadAllText(command).Trim();File.Delete(command);if(c.Length==0)return;
  try
  {
   if(c=="import")Import();
   else if(c=="play")EditorApplication.isPlaying=true;
   else if(c=="stop")EditorApplication.isPlaying=false;
   else {EditorApplication.isPaused=false;UnityEngine.Object.FindFirstObjectByType<CityForgeApp>().BrickworksQa(c);}
   File.WriteAllText("/tmp/cityforge-brickworks-result.txt","OK "+c);
  }
  catch(Exception e){File.WriteAllText("/tmp/cityforge-brickworks-result.txt",e.ToString());Debug.LogException(e);}
 }
 static void Import()
 {
  AssetDatabase.Refresh();var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"Brickworks.fbx");importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.SaveAndReimport();
  var normal=(TextureImporter)AssetImporter.GetAtPath(Root+"Normal.jpg");normal.textureType=TextureImporterType.NormalMap;normal.SaveAndReimport();
  var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"BrickworksMaterial.mat");
  if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,Root+"BrickworksMaterial.mat");}
  material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"BaseColor.jpg");material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Normal.jpg"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_Glossiness",.08f);material.SetFloat("_Metallic",0);AssetDatabase.SaveAssets();
  var preview=new PreviewRenderUtility();
  try
  {
   var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("CityForgeV3/Industry/BrickworksV01/Brickworks"));preview.AddSingleGO(model);var rs=model.GetComponentsInChildren<Renderer>();foreach(var r in rs)r.sharedMaterial=material;
   var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
   preview.camera.orthographic=true;preview.camera.orthographicSize=17;preview.camera.transform.position=bounds.center+new Vector3(28,22,32);preview.camera.transform.LookAt(bounds.center);preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=200;
   preview.camera.backgroundColor=new Color(.1f,.15f,.18f,1);preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.ambientColor=Color.gray;preview.lights[0].intensity=1.5f;preview.lights[0].transform.rotation=Quaternion.Euler(45,30,0);preview.lights[1].intensity=.5f;
   preview.BeginStaticPreview(new Rect(0,0,512,360));preview.Render();var image=preview.EndStaticPreview();File.WriteAllBytes(Root+"MenuThumbnail.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
  }
  finally{preview.Cleanup();}
  AssetDatabase.Refresh();
 }
}
#endif
