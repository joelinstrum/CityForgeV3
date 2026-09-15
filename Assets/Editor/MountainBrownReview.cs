#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CityForgeV3.World;
public static class MountainBrownReview
{
    static MountainBrownReview(){EditorApplication.update+=Poll;}
    static void Poll(){const string file="/tmp/cityforge-mountain-brown-command.txt";if(EditorApplication.isCompiling||!File.Exists(file))return;var c=File.ReadAllText(file).Trim();File.Delete(file);try{
        string result="OK "+c;
        if(c=="import"){
            var ti=(TextureImporter)AssetImporter.GetAtPath("Assets/CityForgeV3/Resources/CityForgeV3/Terrain/MountainBrownV01/brown-scree-v01.png");
            ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Mirror;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
        }else{
            var world=UnityEngine.Object.FindObjectsByType<DistrictWorldController>(FindObjectsSortMode.None).First(w=>w.gameObject.activeInHierarchy);
            var ground=world.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("District Ground")).sharedMaterial;
            if(c=="noon")world.SetTimeOfDay(TimeOfDayPreset.Noon);
            else if(c=="off")ground.SetFloat("_BrownStrength",0);
            else if(c=="on")ground.SetFloat("_BrownStrength",1);
            else if(c=="check"){
                var t=ground.GetTexture("_BrownTex") as Texture2D;
                if(ground.shader.name!="CityForgeV3/MountainGroundSurfaceV04"||ShaderUtil.ShaderHasError(ground.shader)||t==null||t.mipmapCount<2||t.wrapMode!=TextureWrapMode.Mirror)throw new Exception("Brown material validation failed");
                result="PASS shader=true bound=true mipmaps=true mirrored=true texture="+t.name+" size="+t.width+"x"+t.height;
            }
        }
        File.WriteAllText("/tmp/cityforge-mountain-brown-result.txt",result);
    }catch(Exception e){File.WriteAllText("/tmp/cityforge-mountain-brown-result.txt",e.ToString());}}
}
#endif
