#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEngine;
using UnityEditor;
public static class FarmCabinQa
{
    [MenuItem("City Forge/QA/Open Founders Farmhouse and Cabin")]
    static void Open()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Enter Play Mode first"); return; }
        BuildingContentCatalog.InvalidateCache();
        var entry = BuildingContentCatalog.Find("founders-farm-cabin-v01");
        var lot = new LotSaveData { EraId = "founders", BasePlopCost = 0,
            Buildings3D = new List<PlacedBuilding3D> { new PlacedBuilding3D { AssetId = entry.id } } };
        var cost = LotEconomy.CalculatePlopCost(lot);
        if (cost != 2500) throw new System.Exception("Farmhouse construction cost should be $2500, got " + cost);
        Directory.CreateDirectory("QA/FarmCabin");
        File.WriteAllText("QA/FarmCabin/cost.txt", $"catalog={entry.id}\nera={lot.EraId}\ncategory={entry.category}\nbuildCost={cost}\n");
        Object.FindFirstObjectByType<CityForgeApp>()?.OpenFarmCabinQa();
    }
}
#endif
