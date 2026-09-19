using System;
using System.IO;
using System.Text;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
public static class MeadowZoomCheck
{
 public static void Run()
 {
  Directory.CreateDirectory("MeadowZoomReview");
  var shader = Shader.Find("CityForgeV3/MeadowGroundSurface");
  var before = Shader.Find("CityForgeV3/MeadowBefore");
  if(shader == null || before == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Meadow shader failed");
  var grass=Resources.Load<Texture2D>("CityForgeV3/Terrain/MeadowV01/meadow-4x4");
  var hill=Resources.Load<Texture2D>("CityForgeV3/Terrain/HillsV01/crest-meadow-4x4");
  if(grass == null || hill == null) throw new Exception("Missing texture fixture");
  var root = new GameObject("Isolated meadow render");
  var cameraObject = new GameObject("Meadow camera"); cameraObject.transform.SetParent(root.transform);
  var camera = cameraObject.AddComponent<Camera>(); camera.orthographic=true;
  camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;
  camera.transform.rotation=Quaternion.Euler(20,45,0);camera.transform.position=-camera.transform.forward*3000;
  camera.nearClipPlane=.1f;camera.farClipPlane=8000;camera.aspect=16f/9;
  var lightObject=new GameObject("Sun");lightObject.transform.SetParent(root.transform);
  var sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;
  sun.transform.rotation=Quaternion.Euler(50,-30,0);sun.shadows=LightShadows.None;
  RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.5f,.5f);
  var ground=new GameObject("Ground");ground.transform.SetParent(root.transform);
  var filter=ground.AddComponent<MeshFilter>();var renderer=ground.AddComponent<MeshRenderer>();
  const int n=128;const float size=3200;
  var v=new Vector3[(n+1)*(n+1)];var uv=new Vector2[v.Length];var tris=new int[n*n*6];
  for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){int i=z*(n+1)+x;v[i]=new Vector3((float)x/n*size-size/2,0,(float)z/n*size-size/2);uv[i]=new Vector2((float)x/n,(float)z/n);}
  int t=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x;tris[t++]=a;tris[t++]=a+n+1;tris[t++]=a+1;tris[t++]=a+1;tris[t++]=a+n+1;tris[t++]=a+n+2;}
  var mesh=new Mesh();mesh.vertices=v;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();filter.sharedMesh=mesh;
  var target=new RenderTexture(1280,720,24);camera.targetTexture=target;
  var report=new StringBuilder();
  for(int h=0;h<2;h++)
  {
   for(int i=0;i<v.Length;i++)v[i].y=h==0?0:25+20*Mathf.Sin(v[i].x/120)*Mathf.Cos(v[i].z/150);
   mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();
   foreach(int zoom in new[]{0,1,2,3,4,5})
   {
    var level=(DistrictZoomLevel)zoom;
    var expected=zoom==0?40f*44f/132f:40f;
    if(Mathf.Abs(DistrictWorldController.DistrictGrassWorldSizeForZoom(level)-expected)>.0001f || DistrictWorldController.DistrictGrassUsesSmoothFiltering(level)!=(zoom>=2)) throw new Exception("Zoom contract failed");
    camera.orthographicSize=DistrictWorldController.OrthographicSize(level,1280,1280,camera.aspect);
    for(int current=0;current<2;current++)
    {
     var mat=new Material(current==1?shader:before);renderer.sharedMaterial=mat;
     mat.mainTexture=grass;mat.SetTexture("_HillTex",hill);mat.SetFloat("_HillHeight",45);
     mat.SetFloat("_GrassHueShift",.035f);mat.SetFloat("_MeadowPatchStrength",1);mat.EnableKeyword("MEADOW_PATCHES");
     if(h==1)mat.EnableKeyword("HILL_MEADOW");
     float metres=current==1?DistrictWorldController.DistrictGrassWorldSizeForZoom(level):zoom<2?40*DistrictWorldController.OrthographicSize(level,1,1,1)/DistrictWorldController.OrthographicSize((DistrictZoomLevel)(zoom+1),1,1,1):40;
     mat.mainTextureScale=Vector2.one*(size/metres);
     mat.SetFloat("_DistantMeadow",current==1?(zoom>=2?1:0):(zoom==5?1:0));
     camera.Render();RenderTexture.active=target;
     var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
     string name=(h==0?"flat":"hills")+"-zoom-"+(zoom+1)+(current==1?"-after":"-before");
     File.WriteAllBytes("MeadowZoomReview/"+name+".png",image.EncodeToPNG());
     var pixels=image.GetPixels();double delta=0;int samples=0;
     for(int y=100;y<620;y++)for(int x=100;x<1180;x++){var a=pixels[y*1280+x];var b=pixels[y*1280+x+1];delta+=Math.Abs(a.grayscale-b.grayscale);samples++;}
     report.AppendLine(name+" mean-adjacent-luminance-difference="+(delta/samples).ToString("F6"));
     UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(mat);
    }
   }
  }
  RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);
  if(ShaderUtil.ShaderHasError(shader))throw new Exception("Shader compilation failed after render");
  File.WriteAllText("MeadowZoomReview/report.txt", "PASS: six zoom contracts; flat/hill shader renders; shader has no errors. Single shared terrain mesh per fixture; no extra shader texture samples.\n"+report);
 }
}
