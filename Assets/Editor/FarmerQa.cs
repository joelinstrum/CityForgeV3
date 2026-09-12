#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
public static class FarmerQa
{
    const string Resource="CityForgeV3/Props/Characters/FarmerV01/Farmer_Animated_v01";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    [MenuItem("City Forge/QA/Farmer/Check Imported Clips")]
    static void Check()
    {
        Directory.CreateDirectory("QA/Farmer");
        var source=Resources.Load<GameObject>(Resource);if(source==null)throw new Exception("Farmer model missing");
        var model=Object.Instantiate(source);model.SetActive(false);var report="";
        try
        {
            var bones=model.GetComponentsInChildren<Transform>(true);
            var hoe=bones.First(t=>t.name=="Hoe");var foot=bones.First(t=>t.name=="Foot.L");
            var clips=Resources.LoadAll<AnimationClip>(Resource).Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            if(clips.Length!=3)throw new Exception("Expected three clips, got "+clips.Length);
            foreach(var clip in clips)
            {
                clip.SampleAnimation(model,0);var h=hoe.localPosition;var p=foot.localPosition;var hmotion=0f;var pmotion=0f;
                var initialRotation=hoe.localRotation;var angle=0f;
                for(var i=1;i<=60;i++){
                    clip.SampleAnimation(model,clip.length*i/60f);
                    hmotion=Mathf.Max(hmotion,Vector3.Distance(h,hoe.localPosition));pmotion=Mathf.Max(pmotion,Vector3.Distance(p,foot.localPosition));angle=Mathf.Max(angle,Quaternion.Angle(initialRotation,hoe.localRotation));
                }
                var loop=Mathf.Max(Vector3.Distance(h,hoe.localPosition),Vector3.Distance(p,foot.localPosition));
                if(loop>.002f)throw new Exception("Loop seam in "+clip.name);
                if(clip.name.Contains("Walk")&&pmotion<.03f)throw new Exception("Walk foot motion missing");
                if(clip.name.Contains("Hoe")&&(hmotion<.03f||angle<3||pmotion>.002f))throw new Exception("Hoe must move tool while feet remain planted");
                report+=$"{clip.name} seconds={clip.length} footMotion={pmotion} toolMotion={hmotion} toolAngle={angle} loopError={loop}\n";
            }
            if(BusinessAsUsualCharacterScript.AllowsTranslation("hoe"))throw new Exception("Hoe must not translate");
            File.WriteAllText("QA/Farmer/clips.txt",report+"stationaryHoe=True\n");
        }
        finally{Object.DestroyImmediate(model);}
    }
    [MenuItem("City Forge/QA/Farmer/Open Preview")]
    static void Open()
    {
        if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.OpenFarmerQa();
    }
    [MenuItem("City Forge/QA/Farmer/Render Portrait")]
    static void Portrait()=>CharacterThumbnailBuilder.BuildLibrary(new[]{LotWorldController.FarmerCharacterId},CharacterThumbnailBuilder.DirectoryPath,"QA/Farmer",false);
    [MenuItem("City Forge/QA/Farmer/Show Character Library")]
    static void Library(){var app=Object.FindFirstObjectByType<CityForgeApp>();typeof(CityForgeApp).GetMethod("OpenCharactersModal",F)?.Invoke(app,null);}
}
#endif
