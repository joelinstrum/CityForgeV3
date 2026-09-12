#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
public static class FarmCabinLightingQa
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    [MenuItem("City Forge/QA/Open Farm Cabin Blank Ground Check")]
    static void OpenBlank()
    {
        if (!EditorApplication.isPlaying) return;
        Object.FindFirstObjectByType<CityForgeV3.UI.CityForgeApp>()?.OpenFarmCabinQa(false);
        Inspect();
    }
    [MenuItem("City Forge/QA/Inspect Current Lot Ground and Cabin")]
    static void Inspect()
    {
        if (!EditorApplication.isPlaying) return;
        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world == null) return;
        // Refresh presentation only. Never load a fixture, change the session or save.
        typeof(LotWorldController).GetMethod("ApplyTimeOfDay", F).Invoke(world, null);
        var ground = (Renderer)typeof(LotWorldController).GetField("_groundRenderer", F).GetValue(world);
        var text = new StringBuilder();
        text.AppendLine($"lot={world.CurrentLotName} time={world.TimeOfDay} season={world.Season} baseTexture={world.BaseTextureId}");
        text.AppendLine($"groundShader={ground.sharedMaterial.shader.name} tint={ground.sharedMaterial.color} texture={ground.sharedMaterial.mainTexture?.name}");
        if (string.IsNullOrWhiteSpace(world.BaseTextureId))
        {
            var expected = SeasonLighting.GroundColor(world.Season, TimeOfDayLighting.For(world.TimeOfDay).GroundColor);
            var actual = ground.sharedMaterial.color;
            var distance = Vector4.Distance((Vector4)expected, (Vector4)actual);
            text.AppendLine($"expectedBlankGround={expected} distance={distance}");
            if (distance > .0001f) throw new Exception("Blank-lot ground baseline changed on 3D placement.");
            if (ground.sharedMaterial.HasProperty("_DisplayMatch"))
            {
                var match = ground.sharedMaterial.GetColor("_DisplayMatch");
                text.AppendLine($"displayMatch={match}");
                if (match != Color.white) throw new Exception("Blank-lot ground should have neutral display match.");
            }
        }
        foreach(var renderer in world.GetComponentsInChildren<Renderer>(true))
        foreach(var material in renderer.sharedMaterials)
        {
            if(material == null || !material.name.Contains("29314f7c")) continue;
            text.AppendLine($"house={renderer.name} material={material.name} shader={material.shader.name} color={material.color} albedo={material.mainTexture?.name}");
            foreach(var name in new[]{"_Metallic","_Glossiness","_SpecularHighlights","_GlossyReflections","_BumpScale"})
                if(material.HasProperty(name)) text.AppendLine($"{name}={material.GetFloat(name)}");
            text.AppendLine("keywords="+string.Join(",",material.shaderKeywords));
        }
        Directory.CreateDirectory("QA/FarmCabin");File.WriteAllText("QA/FarmCabin/lighting.txt",text.ToString());
        Debug.Log(text.ToString());
    }
}
#endif
