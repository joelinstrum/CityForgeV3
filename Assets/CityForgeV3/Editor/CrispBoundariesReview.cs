#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CityForgeV3.World;
public static class CrispBoundariesReview
{
 static CrispBoundariesReview(){EditorApplication.update+=Poll;}
 static void Poll(){const string file="/tmp/cityforge-crisp-boundaries-command.txt";if(EditorApplication.isCompiling||!File.Exists(file))return;var c=File.ReadAllText(file).Trim();File.Delete(file);try{
 const string asset="Assets/CityForgeV3/Resources/CityForgeV3/Terrain/QuietSilverV01/quiet-silver-v01.png";
 if(c=="import") {var ti=(TextureImporter)AssetImporter.GetAtPath(asset);ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Mirror;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();}
 else{
 var w=UnityEngine.Object.FindObjectsByType<DistrictWorldController>(FindObjectsSortMode.None).First(x=>x.gameObject.activeInHierarchy);
 var ground=w.GetComponentsInChildren<MeshRenderer>().First(x=>x.name.StartsWith("District Ground")).sharedMaterial;
 var pilot=w.GetComponentInChildren<DistrictCragPilot>();if(pilot==null)throw new Exception("No crag pilot");
 var t=Resources.Load<Texture2D>("CityForgeV3/Terrain/QuietSilverV01/quiet-silver-v01");
 if(t==null||t.mipmapCount<2||t.wrapMode!=TextureWrapMode.Mirror||ground.GetTexture("_BedrockTex")!=t)throw new Exception("Texture import/binding failed");
 foreach(var renderer in pilot.GetComponentsInChildren<MeshRenderer>())if(renderer.sharedMaterial.mainTexture!=t||ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader))throw new Exception("Crag material failed");
 if(ground.shader.name!="CityForgeV3/MountainGroundSurfaceV10"||ShaderUtil.ShaderHasError(Shader.Find("CityForgeV3/HillGroundOverlayForestV02"))||ShaderUtil.ShaderHasError(ground.shader))throw new Exception("Terrain shader failed");
 }
 File.WriteAllText("/tmp/cityforge-crisp-boundaries-result.txt","PASS "+c+" quiet silver source, terrain and crag bindings, mips, mirror, shaders");
 }catch(Exception e){File.WriteAllText("/tmp/cityforge-crisp-boundaries-result.txt",e.ToString());}}
}
#endif
