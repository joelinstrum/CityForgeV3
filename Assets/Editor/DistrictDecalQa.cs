#if UNITY_EDITOR
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;

public static class DistrictDecalQa
{
    [MenuItem("City Forge/Flora/Load Saved District Decal Preview")]
    private static void Load()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.OpenSavedDistrictDecalQa();
    }
    [MenuItem("City Forge/Flora/Check Saved District Decal Reload")]
    private static void Check()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.CheckDistrictDecalReloadQa();
    }
    [MenuItem("City Forge/Flora/Default District Decals On")]
    private static void On() => Toggle(true);
    [MenuItem("City Forge/Flora/Default District Decals Off")]
    private static void Off() => Toggle(false);
    private static void Toggle(bool visible)
    {
        if (!EditorApplication.isPlaying) return;
        foreach (var decals in Object.FindObjectsByType<DistrictGroundDecals>(FindObjectsSortMode.None))
            decals.PresentationEnabled = visible;
    }
}
#endif
