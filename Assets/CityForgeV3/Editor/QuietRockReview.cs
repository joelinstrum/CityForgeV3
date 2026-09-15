#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CityForgeV3.World;
public static class QuietRockReview
{
    static QuietRockReview(){EditorApplication.update+=Poll;}
    static void Poll(){const string file="/tmp/cityforge-quiet-rock-command.txt";if(EditorApplication.isCompiling||!File.Exists(file))return;var c=File.ReadAllText(file).Trim();File.Delete(file);try{
        string result="OK "+c;
        if(c=="import"){
            foreach(var name in new[]{"bedrock-v01","scree-v01"}){
                var ti=(TextureImporter)AssetImporter.GetAtPath("Assets/CityForgeV3/Resources/CityForgeV3/Terrain/AlpineRockV01/"+name+".png");
                ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Mirror;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
            }
        }else{
            var world=UnityEngine.Object.FindObjectsByType<DistrictWorldController>(FindObjectsSortMode.None).First(w=>w.gameObject.activeInHierarchy);
            var ground=world.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("District Ground")).sharedMaterial;
            if(c=="noon")world.SetTimeOfDay(TimeOfDayPreset.Noon);
            else if(c=="mid")world.WorldCamera.orthographicSize=115f;
            else if(c=="off")ground.shader=Shader.Find("CityForgeV3/MountainGroundSurfaceV08");
            else if(c=="on")ground.shader=Shader.Find("CityForgeV3/MountainGroundSurfaceV09");
            else if(c=="check"){
                var t=ground.GetTexture("_BedrockTex") as Texture2D;
                var scree=ground.GetTexture("_ShaleTex") as Texture2D;
                if(t==null||scree==null||t.name!="bedrock-v01"||scree.name!="scree-v01"||scree.mipmapCount<2||scree.wrapMode!=TextureWrapMode.Mirror)throw new Exception("Alpine texture binding failed");
                if(ground.shader.name!="CityForgeV3/MountainGroundSurfaceV09"||ShaderUtil.ShaderHasError(ground.shader)||t==null||t.mipmapCount<2||t.wrapMode!=TextureWrapMode.Mirror)throw new Exception("Grass material validation failed");
                var overlayShader=Shader.Find("CityForgeV3/HillGroundOverlayForestV01"); if(overlayShader==null||ShaderUtil.ShaderHasError(overlayShader))throw new Exception("Forest overlay shader failed");
                result="PASS forestOverlay=true shader=true bound=true mipmaps=true mirrored=true texture="+t.name+" size="+t.width+"x"+t.height;
            }
        }
        File.WriteAllText("/tmp/cityforge-quiet-rock-result.txt",result);
    }catch(Exception e){File.WriteAllText("/tmp/cityforge-quiet-rock-result.txt",e.ToString());}}
}
#endif
