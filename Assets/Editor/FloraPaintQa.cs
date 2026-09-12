#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CityForgeV3.UI;
public static class FloraPaintQa
{
 [MenuItem("City Forge/Flora/Refresh District Flora Pose")]
 static void Pose(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.ReloadFloraPoseQa();}
 [MenuItem("City Forge/Flora/Shadow Comparison On")]
 static void On(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.SetFloraShadowComparisonQa(true);}
 [MenuItem("City Forge/Flora/Shadow Comparison Off")]
 static void Off(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.SetFloraShadowComparisonQa(false);}
 [MenuItem("City Forge/Flora/Check Mountain Paint")]
 static void Run(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.CheckMountainPaintQa();}
}
#endif
