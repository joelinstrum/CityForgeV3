#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using CityForgeV3.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
public static class CharacterThumbnailQa
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    [MenuItem("City Forge/Characters/Open Portrait Library")]
    static void Open()
    {
        if(!EditorApplication.isPlaying)return;
        typeof(CarriageDriveQa).GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        var app=Object.FindFirstObjectByType<CityForgeApp>();
        typeof(CityForgeApp).GetMethod("OpenCharactersModal",F).Invoke(app,null);
        var root=(VisualElement)typeof(CityForgeApp).GetField("_root",F).GetValue(app);
        root.schedule.Execute(()=>
        {
            var report="";var missing=0;
            foreach(var id in CharacterThumbnailBuilder.Ids)
            {
                var image=root.Q<Image>("character-preview-"+id);var button=root.Q<Button>("character-select-"+id);
                var ok=image?.image!=null&&button!=null&&button.enabledSelf;if(!ok)missing++;
                report+=$"{id}: image={image?.image?.name} button={button!=null} valid={ok}\n";
            }
            var modal=root.Q<VisualElement>("document-modal");var scroll=root.Q<ScrollView>("character-library-scroll");
            report+=$"missing={missing} entries={CharacterThumbnailBuilder.Ids.Length} modal={modal.worldBound} scroll={scroll.worldBound} content={scroll.contentContainer.worldBound}\n";
            Directory.CreateDirectory("QA/CharacterThumbnails");File.WriteAllText("QA/CharacterThumbnails/library.txt",report);
        }).StartingIn(500);
    }
    [MenuItem("City Forge/Characters/Show Vehicle Portraits")]
    static void Vehicles()
    {
        var app=Object.FindFirstObjectByType<CityForgeApp>();var root=(VisualElement)typeof(CityForgeApp).GetField("_root",F).GetValue(app);
        var scroll=root.Q<ScrollView>("character-library-scroll");if(scroll!=null)scroll.scrollOffset=new Vector2(0,10000);
    }
    [MenuItem("City Forge/Characters/Check Portrait Selection")]
    static void Select()
    {
        var app=Object.FindFirstObjectByType<CityForgeApp>();var root=(VisualElement)typeof(CityForgeApp).GetField("_root",F).GetValue(app);
        var id=CityForgeV3.World.LotWorldController.CavalryPropId;
        var button=root.Q<Button>("character-select-"+id);if(button==null)return;
        typeof(Clickable).GetMethod("Invoke",F).Invoke(button.clickable,new object[]{null});
        var selected=(string)typeof(CityForgeApp).GetField("_placementPropId",F).GetValue(app);
        File.WriteAllText("QA/CharacterThumbnails/selection.txt",$"selected={selected} correct={selected==id} modalClosed={root.Q<VisualElement>("document-modal")==null}\n");
        // Clear the preview and reopen the library; no object is placed or saved.
        typeof(CityForgeApp).GetField("_placementPropId",F).SetValue(app,"");
        var world=Object.FindFirstObjectByType<CityForgeV3.World.LotWorldController>();world.SetPropPlacementPreview("");
        typeof(CityForgeApp).GetMethod("OpenCharactersModal",F).Invoke(app,null);
    }
}
#endif
