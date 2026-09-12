using UnityEngine;
using UnityEditor;
using CityForgeV3.UI;
public static class SaltboxOriginalComparisonQa
{
    [MenuItem("City Forge/QA/Saltbox/Open Cottage Comparison")]
    static void Open()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Enter Play Mode first"); return; }
        CityForgeV3.World.BuildingContentCatalog.InvalidateCache();
        Object.FindFirstObjectByType<CityForgeApp>()?.OpenSaltboxOriginalComparisonQa();
    }
}
