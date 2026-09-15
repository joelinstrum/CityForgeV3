#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
public static class CharacterThumbnailBuilder
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public const string DirectoryPath="Assets/CityForgeV3/Resources/CityForgeV3/UI/CharacterThumbnails";
    public static readonly string[] Ids={LotWorldController.VictorianGentlemanCharacterId,LotWorldController.HooliganCharacterId,LotWorldController.HistoricPolicemanCharacterId,LotWorldController.MusketmanCharacterId,LotWorldController.FarmerCharacterId,LotWorldController.KingKongCharacterId,LotWorldController.BearAnimalId,LotWorldController.HorseAnimalId,LotWorldController.CavalryPropId,LotWorldController.TrapperPropId,LotWorldController.CarriagePropId,LotWorldController.HorseCarriagePropId,LotWorldController.HorseLumberWagonPropId,LotWorldController.HorseCoveredWagonPropId,LotWorldController.HorseFoodWagonPropId};
    [MenuItem("City Forge/Characters/Render Library Thumbnails")]
    static void Build()
    {
        BuildLibrary(Ids, DirectoryPath, "QA/CharacterThumbnails", true);
    }
    public static void BuildLibrary(string[] ids, string directoryPath, string reportDirectory, bool characterPortraits, Func<string,Transform> factory = null)
    {
        if(!EditorApplication.isPlaying){Debug.LogError("Enter Play mode before rendering runtime character portraits.");return;}
        var world=Object.FindFirstObjectByType<LotWorldController>(FindObjectsInactive.Include);
        if(world==null)throw new Exception("Open a lot before rendering character thumbnails.");
        Directory.CreateDirectory(directoryPath);Directory.CreateDirectory(reportDirectory);var report="";
        for(var i=0;i<ids.Length;i++)
        {
            var id=ids[i];var preview=new PreviewRenderUtility();Texture2D pixels=null;
            try
            {
                var root=factory != null ? factory(id) : (Transform)typeof(LotWorldController).GetMethod("CreatePropPresentation",F).Invoke(world,new object[]{id,"Thumbnail "+id,1f});
                if(root==null)throw new Exception("No presentation for "+id);
                preview.AddSingleGO(root.gameObject);
                foreach(var player in root.GetComponentsInChildren<ThreeDimensionalCharacterAnimator>())
                {
                    var clips=(Dictionary<string,AnimationClip>)typeof(ThreeDimensionalCharacterAnimator).GetField("_clips",F).GetValue(player);
                    var animator=player.GetComponentInChildren<Animator>();
                    if(animator!=null&&clips.TryGetValue("idle",out var clip))clip.SampleAnimation(animator.gameObject,clip.length*.23f);
                }
                var team=root.GetComponent<HorseCarriageController>();if(team!=null)typeof(HorseCarriageController).GetMethod("LateUpdate",F).Invoke(team,null);
                var bounds=new Bounds();var first=true;
                foreach(var r in root.GetComponentsInChildren<Renderer>())
                {
                    if(r.shadowCastingMode==ShadowCastingMode.ShadowsOnly||r.sharedMaterials.Any(m=>m!=null&&m.shader!=null&&m.shader.name.Contains("Shadow"))) {r.enabled=false;continue;}
                    if(r is LineRenderer)continue;
                    if(!r.enabled)continue;
                    if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);
                }
                if(first)throw new Exception("No visible renderers for "+id);
                var portrait=characterPortraits && i<4;
                var center=bounds.center;
                var camera=preview.camera;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                var direction=(portrait?new Vector3(.45f,.16f,2f):new Vector3(1.45f,.8f,1.8f)).normalized;
                if(portrait)center.y=bounds.min.y+bounds.size.y*.73f;
                camera.transform.SetPositionAndRotation(center+direction*Mathf.Max(10,bounds.size.magnitude*3),Quaternion.LookRotation(-direction));
                var extentX=0f;var extentY=0f;
                for(var x=-1;x<=1;x+=2)for(var y=-1;y<=1;y+=2)for(var z=-1;z<=1;z+=2)
                {
                    var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                    if(portrait)corner.y=y<0?bounds.min.y+bounds.size.y*.42f:bounds.max.y;
                    var point=camera.transform.InverseTransformPoint(corner);
                    extentX=Mathf.Max(extentX,Mathf.Abs(point.x));extentY=Mathf.Max(extentY,Mathf.Abs(point.y));
                }
                camera.orthographicSize=Mathf.Max(extentY,extentX/(512f/384f))*1.10f;camera.nearClipPlane=.01f;camera.farClipPlane=1000;
                preview.ambientColor=new Color(.42f,.42f,.42f);
                preview.lights[0].intensity=1.25f;preview.lights[0].color=new Color(1f,.95f,.87f);preview.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);
                preview.lights[1].intensity=.85f;preview.lights[1].color=new Color(.84f,.90f,1f);preview.lights[1].transform.rotation=Quaternion.Euler(30,145,0);
                preview.BeginPreview(new Rect(0,0,512,384),GUIStyle.none);preview.Render();var texture=preview.EndPreview();
                var old=RenderTexture.active;
                try{RenderTexture.active=(RenderTexture)texture;pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply();}
                finally{RenderTexture.active=old;}
                File.WriteAllBytes(directoryPath+"/"+id+".png",pixels.EncodeToPNG());report+=$"{id}: {pixels.width}x{pixels.height} portrait={portrait} bounds={bounds.size}\n";
            }
            finally{if(pixels!=null)Object.DestroyImmediate(pixels);preview.Cleanup();}
        }
        AssetDatabase.Refresh();
        foreach(var id in ids)
        {
            var path=directoryPath+"/"+id+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        File.WriteAllText(reportDirectory+"/rendered.txt",report);Debug.Log("Rendered "+ids.Length+" character library thumbnails.");
    }
}
#endif
