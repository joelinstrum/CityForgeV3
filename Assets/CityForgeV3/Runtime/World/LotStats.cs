using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    [Serializable]
    public sealed class LotStats
    {
        public string MinimumEraId = "founders";
        public int MinimumPopulation;
        public int MinimumEducationScore;
        public bool RequiresRoad;
        public bool RequiresWaterfront;
        public int Residents;
        public int SeasonalWagePerJob = 150;
        public string Service = "None";
        public int ServiceCapacity;
        public List<ContentResourceAmount> ConstructionResources = new();
        public List<LotBenefit> Benefits = new();
        public List<LotResourceBonus> PlacementBonuses = new();
        public LotStats Copy() => JsonUtility.FromJson<LotStats>(JsonUtility.ToJson(this));
    }
    [Serializable]
    public sealed class LotBenefit
    {
        public string ResourceId = "food";
        public int Amount;
        public string Timing = "Per season";
    }

    [Serializable]
    public sealed class LotResourceBonus
    {
        public string ResourceId = "food";
        public int Amount;
    }

    public static class LotPlacementBonusCatalog
    {
        private static readonly IReadOnlyList<string> Ids = Array.AsReadOnly(
            new[] {
                "lumber", "coal", "stone", "iron-ore", "gold", "oil",
                "food", "jewels", "cloth", "bricks"
            });

        public static IReadOnlyList<string> ResourceIds => Ids;

        public static string DisplayName(string id) => id switch
        {
            "iron-ore" => "Iron Ore",
            "lumber" => "Lumber",
            "bricks" => "Bricks",
            _ => string.IsNullOrWhiteSpace(id) ? "Resource" :
                char.ToUpperInvariant(id[0]) + id.Substring(1)
        };

        public static void Apply(RegionCityTile district, LotSaveData lot)
        {
            if (district == null || lot?.Stats?.PlacementBonuses == null) return;
            var totals = new long[Ids.Count];
            foreach (var bonus in lot.Stats.PlacementBonuses)
            {
                if (bonus == null || bonus.Amount <= 0) continue;
                var index = DistrictLotRequirements.ResourceIndex(
                    bonus.ResourceId);
                if (index >= 0)
                    totals[index] = Math.Min(int.MaxValue,
                        totals[index] + bonus.Amount);
            }
            for (var index = 0; index < totals.Length; index++)
                if (totals[index] > 0)
                    DistrictLotRequirements.AddResource(district, index,
                        totals[index]);
        }
    }
}
