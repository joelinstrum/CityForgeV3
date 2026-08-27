using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;

public static class ExperimentalThreeDimensionalBuildingQaMenu
{
    [MenuItem("City Forge/QA/Open 3D Buildings Experimental Lot")]
    private static void Open()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning(
                "Enter Play Mode, then run Open 3D Buildings Experimental Lot.");
            return;
        }
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenExperimentalThreeDimensionalBuildingsQa())
            Debug.LogError("The City Forge runtime is not ready for 3D building QA.");
    }

    [MenuItem("City Forge/QA/3D Buildings Lighting/Morning")]
    private static void Morning() => SetTime(TimeOfDayPreset.Morning);

    [MenuItem("City Forge/QA/3D Buildings Lighting/Noon")]
    private static void Noon() => SetTime(TimeOfDayPreset.Noon);

    [MenuItem("City Forge/QA/3D Buildings Lighting/Afternoon")]
    private static void Afternoon() => SetTime(TimeOfDayPreset.Afternoon);

    private static void SetTime(TimeOfDayPreset preset)
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
            if (world.ExperimentalBuilding3DCount > 0)
                world.SetTimeOfDay(preset);
    }
}
