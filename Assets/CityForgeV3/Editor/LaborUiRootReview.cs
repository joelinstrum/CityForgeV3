#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using CityForgeV3.UI;
public static class LaborUiRootReview
{
 static LaborUiRootReview(){EditorApplication.update+=Poll;}
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Poll(){const string command="/tmp/cityforge-labor-ui-root-check.txt";if(EditorApplication.isCompiling||!File.Exists(command))return;File.Delete(command);
 try{
 var app=UnityEngine.Object.FindFirstObjectByType<CityForgeApp>();if(app==null)throw new Exception("No live app");
 var type=typeof(CityForgeApp);var root=type.GetField("_root",Flags);var document=type.GetField("_document",Flags);var paused=type.GetField("_districtSimulationPaused",Flags);var timer=type.GetField("_laborUiTimer",Flags);
 var savedRoot=root.GetValue(app);var savedDoc=document.GetValue(app);var savedPaused=paused.GetValue(app);var savedTimer=timer.GetValue(app);
 try{
 paused.SetValue(app,true);timer.SetValue(app,1f);root.SetValue(app,null);document.SetValue(app,null);
 type.GetMethod("TickDistrictLabor",Flags).Invoke(app,null);
 if(!(bool)type.GetMethod("EnsureUiRoot",Flags).Invoke(app,null))throw new Exception("UI rebind returned false");
 if(!ReferenceEquals(root.GetValue(app),app.GetComponent<UIDocument>().rootVisualElement))throw new Exception("Wrong root rebound");
 type.GetMethod("TickDistrictLabor",Flags).Invoke(app,null);
 }finally{root.SetValue(app,savedRoot??app.GetComponent<UIDocument>().rootVisualElement);document.SetValue(app,savedDoc??app.GetComponent<UIDocument>());paused.SetValue(app,savedPaused);timer.SetValue(app,savedTimer);}
 var fixture=new GameObject("UI root absent regression");fixture.SetActive(false);
 try{var empty=fixture.AddComponent<CityForgeApp>();if((bool)type.GetMethod("EnsureUiRoot",Flags).Invoke(empty,null))throw new Exception("Unexpected root in document-less fixture");type.GetMethod("Update",Flags).Invoke(empty,null);}finally{UnityEngine.Object.DestroyImmediate(fixture);}
 File.WriteAllText("/tmp/cityforge-labor-ui-root-result.txt","PASS: null-root labor tick; hot-reload root/document recovery; restored UI labor refresh; no-document Update safely returns.");
 }catch(Exception e){File.WriteAllText("/tmp/cityforge-labor-ui-root-result.txt",e.ToString());}}
}
#endif
