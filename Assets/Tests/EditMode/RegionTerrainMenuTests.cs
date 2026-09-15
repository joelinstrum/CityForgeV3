using System;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

public class RegionTerrainMenuTests
{
    UnityEditor.EditorWindow window;
    [TearDown] public void Cleanup() { if(window != null) window.Close(); }
    [Test] public void CheckboxesAreExclusiveWithinTypeAndIndependentAcrossTypes()
    {
        window = ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
        window.Show();
        var root = window.rootVisualElement;
        var deep = RegionWaterAmount.None;
        var streams = RegionWaterAmount.None;
        var method = typeof(CityForgeApp).GetMethod("AddRegionWaterChoices", BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] {root,"deep","Few","Many",deep,(Action<RegionWaterAmount>)(v=>deep=v)});
        method.Invoke(null, new object[] {root,"stream","Few","Many",streams,(Action<RegionWaterAmount>)(v=>streams=v)});
        using (var submit = NavigationSubmitEvent.GetPooled())
        { submit.target = root.Q<Toggle>("deep-few"); root.Q<Toggle>("deep-few").SendEvent(submit); }
        Assert.That(root.Q<Toggle>("deep-few").value, Is.True);
        Assert.That(root.Q<Toggle>("deep-few").Q<Label>("region-checkbox-glyph").text, Is.EqualTo("✓"));
        using (var submit = NavigationSubmitEvent.GetPooled())
        { submit.target = root.Q<Toggle>("deep-many"); root.Q<Toggle>("deep-many").SendEvent(submit); }
        root.Q<Toggle>("stream-few").value = true;
        Assert.That(root.Q<Toggle>("deep-few").value, Is.False);
        Assert.That(deep, Is.EqualTo(RegionWaterAmount.Many));
        Assert.That(root.Q<Toggle>("deep-few").Q<Label>("region-checkbox-glyph").text, Is.Empty);
        Assert.That(root.Q<Toggle>("deep-many").Q<Label>("region-checkbox-glyph").text, Is.EqualTo("✓"));
        Assert.That(streams, Is.EqualTo(RegionWaterAmount.Few));
        root.Q<Toggle>("deep-many").value = false;
        Assert.That(deep, Is.EqualTo(RegionWaterAmount.None));
        Assert.That(streams, Is.EqualTo(RegionWaterAmount.Few));
    }
    [Test] public void RegionOptionsSurviveSerialization()
    {
        var region = RegionSaveStore.Create("Terrain options",8,8);
        region.Terrain.DeepRivers = RegionWaterAmount.Many;
        region.Terrain.Streams = RegionWaterAmount.Few;
        var loaded=JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(region));
        Assert.That(loaded.Terrain.DeepRivers,Is.EqualTo(RegionWaterAmount.Many));
        Assert.That(loaded.Terrain.Streams,Is.EqualTo(RegionWaterAmount.Few));
        Assert.That(loaded.Tiles.Count,Is.EqualTo(region.Tiles.Count));
    }
}
