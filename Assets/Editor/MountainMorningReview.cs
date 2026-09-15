#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CityForgeV3.World;
[InitializeOnLoad] public static class MountainMorningReview
{
    static MountainMorningReview(){EditorApplication.update+=Poll;}
    static void Poll(){const string path="/tmp/cityforge-mountain-morning-command.txt";if(EditorApplication.isCompiling||!File.Exists(path))return;var c=File.ReadAllText(path).Trim();File.Delete(path);try{
        var world=UnityEngine.Object.FindObjectsByType<DistrictWorldController>(FindObjectsSortMode.None).First(w=>w.gameObject.activeInHierarchy);
        var ground=world.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("District Ground")).sharedMaterial;
        string result="OK "+c;
        if(c=="check"){
            var rows=new List<string>();
            foreach(TimeOfDayPreset preset in Enum.GetValues(typeof(TimeOfDayPreset))){
                world.SetTimeOfDay(preset);var sun=world.GetComponentsInChildren<Light>().First(l=>l.name=="District Sun");
                var original=-sun.transform.forward.normalized;var expected=preset==TimeOfDayPreset.Morning?new Vector3(-original.x,original.y,-original.z):original;
                Vector3 actual=ground.GetVector("_TerrainSunDirection");if(Vector3.Distance(actual,expected)>.0001f)throw new Exception("Wrong direction "+preset);
                float screenRight=Vector3.Dot(actual,world.WorldCamera.transform.right);
                if(preset==TimeOfDayPreset.Morning&&screenRight<.1f)throw new Exception("Morning is not from screen right/east "+screenRight);
                rows.Add(preset+" direction="+actual+" screenRight="+screenRight+" exactExpected=true");
            }
            world.SetTimeOfDay(TimeOfDayPreset.Morning);result=string.Join("\n",rows);
        }else{
            world.SetTimeOfDay(TimeOfDayPreset.Morning);
            if(c=="before"){var d=ground.GetVector("_TerrainSunDirection");ground.SetVector("_TerrainSunDirection",new Vector4(-d.x,d.y,-d.z,0));}
        }
        File.WriteAllText("/tmp/cityforge-mountain-morning-result.txt",result);
    }catch(Exception e){File.WriteAllText("/tmp/cityforge-mountain-morning-result.txt",e.ToString());}}
}
#endif
