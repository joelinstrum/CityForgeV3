#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public static class PropThumbnailBuilder
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static readonly string[] Ids = {
        "wrought-iron-fence-straight-v01", "wrought-iron-fence-corner-v01",
        LotWorldController.PicketFencePropId, LotWorldController.OldWoodenFencePropId,
        LotWorldController.DecorativeIronGardenPropId, LotWorldController.OrnateIronCornerPropId,
        "three-lantern-lamppost-v01", LotWorldController.SimpleStreetLamppostPropId,
        LotWorldController.OrnateBenchPropId, LotWorldController.WoodenPalisadePropId,
        LotWorldController.MedievalWellPropId, LotWorldController.WoodenPalisadeGatePropId,
        LotWorldController.MedievalTorchPropId, LotWorldController.Hedge3DPropId,
        LotWorldController.PumpkinJackOLanternPropId, LotWorldController.NewEnglandBarnPropId
    };
    [MenuItem("City Forge/Props/Render Library Thumbnails")]
    static void Build() => CharacterThumbnailBuilder.BuildLibrary(Ids,
        "Assets/CityForgeV3/Resources/CityForgeV3/UI/PropThumbnails", "QA/PropThumbnails", false);

    [MenuItem("City Forge/Props/Open Thumbnail Library")]
    static void Open()
    {
        if (!EditorApplication.isPlaying) return;
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        typeof(CityForgeApp).GetMethod("RemoveDocumentModal", F).Invoke(app, null);
        typeof(CityForgeApp).GetMethod("OpenPropsModal", F).Invoke(app, null);
        var root = (VisualElement)typeof(CityForgeApp).GetField("_root", F).GetValue(app);
        root.schedule.Execute(() => {
            var report = ""; var missing = 0;
            foreach (var id in Ids) {
                var image = root.Q<Image>("prop-preview-" + id);
                var button = root.Q<Button>("prop-select-" + id);
                if (image?.image == null || button == null) missing++;
                report += $"{id}: image={image?.image?.name} button={button != null} enabled={button?.enabledSelf}\n";
            }
            var scroll = root.Q<ScrollView>("prop-library-scroll");
            report += $"missing={missing} entries={Ids.Length} viewport={scroll.contentViewport.worldBound} content={scroll.contentContainer.worldBound}\n";
            Directory.CreateDirectory("QA/PropThumbnails");
            File.WriteAllText("QA/PropThumbnails/library.txt", report);
            if (missing != 0) Debug.LogError("Missing prop portraits: " + missing);
        }).StartingIn(500);
    }
    [MenuItem("City Forge/Props/Show Lower Library")]
    static void Lower()
    {
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        var root = (VisualElement)typeof(CityForgeApp).GetField("_root", F).GetValue(app);
        var scroll = root.Q<ScrollView>("prop-library-scroll");
        if (scroll != null) scroll.scrollOffset = new Vector2(0,10000);
    }
}
#endif
