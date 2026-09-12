using UnityEngine;
using UnityEditor;
using CityForgeV3.UI;
public static class VictorianColorHouseQa
{
    [MenuItem("City Forge/QA/Open Victorian Color House")]
    static void Open()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Enter Play Mode first"); return; }
        CityForgeV3.World.BuildingContentCatalog.InvalidateCache();
        Object.FindFirstObjectByType<CityForgeApp>()?.OpenVictorianColorHouseQa();
    }
}
