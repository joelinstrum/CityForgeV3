#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CityForgeV3.World;
using CityForgeV3.UI;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
public static class PlaneUkQa
{
 [MenuItem("City Forge/Flora/Open Tree Comparison")]
 static void Open(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.OpenPlaneUkQa();}
 [MenuItem("City Forge/Flora/Show 3D Library")]
 static void Library(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.ShowPlaneUkFloraLibrary();}
 [MenuItem("City Forge/Flora/Check Library Planting")]
 static void Plant(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.CheckPlaneUkLibraryQa();}
 [MenuItem("City Forge/Flora/Check Tree LOD")]
 static void Check()
 {
  var world=Object.FindFirstObjectByType<LotWorldController>();
  if(world==null||world.Session.Data.Flora.All(f=>!PlaneUkFloraPresentation.IsTree(f.FloraId)))throw new Exception("Open comparison first");
  var report="";var initial=world.ZoomLevel;
  try{
   foreach(var level in new[]{LotZoomLevel.Detail,LotZoomLevel.Inspection,LotZoomLevel.CloseUp}){
    world.SetZoomLevel(level);
    var trees=Object.FindObjectsByType<PlaneUkFloraPresentation>(FindObjectsSortMode.None);
    foreach(var t in trees){if(t.IsMeshVisible!=PlaneUkFloraPresentation.UsesMesh(level))throw new Exception("Incorrect LOD "+level);}
    report+=$"zoom={(int)level+1} placedTrees={trees.Length} meshVisible={trees.Count(t=>t.IsMeshVisible)}\n";
   }
  }finally{world.SetZoomLevel(initial);world.SetQaOrthographicSize(16f);}
  Directory.CreateDirectory("QA/PlaneUK");File.WriteAllText("QA/PlaneUK/lod.txt",report);
 }
}
#endif
