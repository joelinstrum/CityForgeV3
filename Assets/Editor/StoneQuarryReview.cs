#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using CityForgeV3.UI;using UnityEditor.TestTools.TestRunner.Api;
[InitializeOnLoad] public static class StoneQuarryReview
{
 const string B="Assets/CityForgeV3/Resources/CityForgeV3/Industry/StoneQuarryV01";
 const string E="/Users/joelinstrum/dev/CityForgeMCP/artifacts/buildings/stone-quarry/v01/";
 static TestRunnerApi runner;
 static StoneQuarryReview(){EditorApplication.update+=Poll;}
 static void Poll(){var path="/tmp/cityforge-quarry-command.txt";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(path))return;var c=File.ReadAllText(path).Trim();File.Delete(path);try{
 if(c.StartsWith("crane-"))EditorApplication.isPaused=false;
 if(c=="workers-inspect")
 {
  var model=Resources.Load<GameObject>(CityForgeV3.World.DistrictWorldController.AxemanResource);
  var lines=model.GetComponentsInChildren<Transform>(true).Select(t=>t.name+" local="+t.localPosition+" rot="+t.localEulerAngles).ToList();
  foreach(var r in model.GetComponentsInChildren<Renderer>(true))lines.Add("RENDERER "+r.name+" bounds="+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m.name)));
  foreach(var clip in Resources.LoadAll<AnimationClip>(CityForgeV3.World.DistrictWorldController.AxemanResource))lines.Add("CLIP "+clip.name+" length="+clip.length);
  File.WriteAllLines(E+"worker-source.txt",lines);
 }
 else if(c=="crane-import")
 {
  AssetDatabase.Refresh();
  var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/CityForgeV3/Resources/CityForgeV3/Industry/StoneQuarryCraneV02/QuarryBase.fbx");
  importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.isReadable=true;importer.SaveAndReimport();
 }
 else if(c=="build")Build();
 else if(c=="art")ImportArtwork();
 else if(c=="tests"){runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Results());runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,groupNames=new[]{".*(DistrictIndustryRotationTests|DistrictBrickworksTests|DistrictQuarryTests|QuarryCraneTests|DistrictNoticeTests|DistrictWildlifeTests|DistrictLaborTests|DistrictTimberTests|DistrictWoodResourceTests|DistrictCoalMineTests|DistrictNaturalResourcesTests).*"}}));}
 else UnityEngine.Object.FindFirstObjectByType<CityForgeApp>().QuarryQa(c);
 File.WriteAllText("/tmp/cityforge-quarry-result.txt","OK "+c);
 }catch(Exception e){File.WriteAllText("/tmp/cityforge-quarry-result.txt",e.ToString());Debug.LogException(e);}}
 static void ImportArtwork()
 {
  const string stone="Assets/CityForgeV3/Resources/CityForgeV3/NaturalResources/StoneV01/stones-for-quarry.png";
  AssetDatabase.Refresh();
  var importer=(TextureImporter)AssetImporter.GetAtPath(stone);
  importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;
  importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.sRGBTexture=true;
  importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  var preview=new PreviewRenderUtility();
  try
  {
   var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("CityForgeV3/Industry/StoneQuarryV01/StoneQuarry"));
   preview.AddSingleGO(go);var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
   foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   preview.camera.orthographic=true;preview.camera.orthographicSize=Mathf.Max(bounds.extents.y,bounds.extents.x*.65f)+4;
   preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=300;
   preview.camera.transform.position=bounds.center+new Vector3(36,28,40);preview.camera.transform.LookAt(bounds.center);
   preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(30/255f,43/255f,51/255f,1);
   preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(45,35,0);preview.lights[1].intensity=.5f;
   preview.BeginStaticPreview(new Rect(0,0,512,320));preview.Render();var image=preview.EndStaticPreview();
   File.WriteAllBytes(B+"/MenuThumbnailV01.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
  }
  finally{preview.Cleanup();}
  AssetDatabase.Refresh();var thumb=(TextureImporter)AssetImporter.GetAtPath(B+"/MenuThumbnailV01.png");
  thumb.textureType=TextureImporterType.Default;thumb.alphaIsTransparency=true;thumb.mipmapEnabled=false;
  thumb.textureCompression=TextureImporterCompression.Uncompressed;thumb.SaveAndReimport();
 }
 static void Build()
 {
 if(Application.isPlaying)throw new Exception("Build in edit mode");
 var importer=(ModelImporter)AssetImporter.GetAtPath(B+"/Model.fbx");importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.isReadable=true;importer.SaveAndReimport();
 foreach(var f in new[]{"BaseColor.jpg","Normal.jpg"}){var t=(TextureImporter)AssetImporter.GetAtPath(B+"/"+f);t.textureType=f=="Normal.jpg"?TextureImporterType.NormalMap:TextureImporterType.Default;t.sRGBTexture=f!="Normal.jpg";t.maxTextureSize=2048;t.anisoLevel=8;t.SaveAndReimport();}
 var mat=AssetDatabase.LoadAssetAtPath<Material>(B+"/StoneQuarry.mat");if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,B+"/StoneQuarry.mat");}
 mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(B+"/BaseColor.jpg");mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(B+"/Normal.jpg"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Metallic",0);mat.SetFloat("_Glossiness",.1f);mat.SetFloat("_SpecularHighlights",0);mat.SetFloat("_GlossyReflections",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
 var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(B+"/Model.fbx"));model.name="Stone Quarry";
 try{foreach(var ren in model.GetComponentsInChildren<Renderer>())ren.sharedMaterials=ren.sharedMaterials.Select(_=>mat).ToArray();
 var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var ren in renderers)bounds.Encapsulate(ren.bounds);
 if(Mathf.Abs(bounds.size.y-6)>.05f||bounds.min.y<-.01f)throw new Exception("Quarry axis/scale/ground failed: "+bounds);
 PrefabUtility.SaveAsPrefabAsset(model,B+"/StoneQuarry.prefab");AssetDatabase.SaveAssets();File.WriteAllText(E+"unity-import.txt","PASS bounds="+bounds+" triangles="+model.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3)+" textured renderers="+renderers.Length);
 }finally{UnityEngine.Object.DestroyImmediate(model);}
 }
 class Results:ICallbacks{public void RunStarted(ITestAdaptor t){}public void TestStarted(ITestAdaptor t){}public void TestFinished(ITestResultAdaptor r){}public void RunFinished(ITestResultAdaptor r){TestRunnerApi.SaveResultToFile(r,E+"tests.xml");UnityEngine.Object.DestroyImmediate(runner);}}
}
#endif
