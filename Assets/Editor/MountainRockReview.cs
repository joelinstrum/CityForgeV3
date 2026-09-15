#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CityForgeV3.World;
using CityForgeV3.UI;

public static class MountainRockReview
{
    const string Command="/tmp/cityforge-mountain-rock-command.txt";
    const string Result="/tmp/cityforge-mountain-rock-result.txt";
    static MountainRockReview(){EditorApplication.update+=Poll;}
    static DistrictWorldController World=>UnityEngine.Object.FindObjectsByType<DistrictWorldController>(FindObjectsSortMode.None).First(w=>w.gameObject.activeInHierarchy);
    static Material Ground=>World.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("District Ground")).sharedMaterial;
    [MenuItem("City Forge/QA/Mountain Rock/On")]static void On()=>Ground.SetFloat("_RockEnabled",1);
    [MenuItem("City Forge/QA/Mountain Rock/Off")]static void Off()=>Ground.SetFloat("_RockEnabled",0);
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Command))return;
        string c=File.ReadAllText(Command).Trim();File.Delete(Command);
        try
        {
            var app=UnityEngine.Object.FindFirstObjectByType<CityForgeApp>();
            if(c=="import"){
                foreach(var name in new[]{"bedrock-v01","shale-v01"}){
                    var p="Assets/CityForgeV3/Resources/CityForgeV3/Terrain/MountainRockV01/"+name+".png";
                    var t=(TextureImporter)AssetImporter.GetAtPath(p);t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Mirror;t.filterMode=FilterMode.Trilinear;t.anisoLevel=4;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
                }
            }
            else if(c=="open")app.OpenLittleRiverBendHillsQa();
            else if(c=="reload")app.CheckMineQa();
            else if(c=="wide")app.OpenLittleRiverBendHillsQa();
            else if(c=="close")World.WorldCamera.orthographicSize=30;
            else if(c=="on")On();
            else if(c=="off")Off();
            else if(c=="check"){
                if(!Ground.shader.name.StartsWith("CityForgeV3/MountainGroundSurface"))throw new Exception("Wrong shader "+Ground.shader.name);
                if(ShaderUtil.ShaderHasError(Ground.shader))throw new Exception("Mountain shader compilation error");
                foreach(var name in new[]{"_BedrockTex","_ShaleTex"}){var t=Ground.GetTexture(name) as Texture2D;if(t==null||t.wrapMode!=TextureWrapMode.Mirror||t.mipmapCount<2)throw new Exception("Missing or misconfigured "+name);}
                var terrain=World.GetComponentsInChildren<MeshCollider>().First(m=>m.sharedMesh.name=="District Elevation").sharedMesh;
                var normals=terrain.normals;int rocky=normals.Count(n=>new Vector2(n.x,n.z).magnitude/Mathf.Max(n.y,.05f)>.85f);
                if(rocky==0)throw new Exception("No steep faces");
                File.WriteAllText(Result,"OK check shader=true textureBindings=true mirror=true mipmaps=true rockyVertices="+rocky+" total="+normals.Length);return;
            }
            else throw new Exception("Unknown command "+c);
            File.WriteAllText(Result,"OK "+c);
        }catch(Exception e){File.WriteAllText(Result,e.ToString());Debug.LogException(e);}
    }
}
#endif
