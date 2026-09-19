using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    public static class DistrictLotRequirements
    {
        public static bool Quote(RegionCityTile district, LotSaveData lot, string era,
            int x, int z, int width, int depth, Func<int,int,bool> road,
            Func<Vector2,bool> water, Vector2 shoreOffset, out long[] resources, out string reason)
        {
            resources = new long[ResourceIds.Length]; reason = "";
            if (district == null || lot == null) { reason = "Lot unavailable."; return false; }
            int cost = LotEconomy.CalculatePlopCost(lot);
            if (district.Treasury < cost) { reason = $"Requires ${cost:N0} to place."; return false; }
            var stats = lot.Stats;
            if (!string.IsNullOrWhiteSpace(stats?.MinimumEraId) &&
                LotEraCatalog.IndexOf(era) < LotEraCatalog.IndexOf(stats.MinimumEraId))
            { reason = "Requires " + LotEraCatalog.DisplayName(stats.MinimumEraId) + "."; return false; }
            var population = district.Population?.Population ?? 0;
            var minimumPopulation = Math.Max(0, stats?.MinimumPopulation ?? 0);
            if (population < minimumPopulation)
            { reason = $"Requires population {minimumPopulation:N0} (current: {population:N0})."; return false; }
            var education = district.Population?.Education ?? 0f;
            var minimumEducation = Math.Clamp(stats?.MinimumEducationScore ?? 0, 0, 100);
            if (education < minimumEducation)
            { reason = $"Requires education score {minimumEducation}/100 (current: {education:0.#}/100)."; return false; }
            foreach (var item in LotEconomy.ConstructionRequirements(lot))
            {
                int resource = ResourceIndex(item.Key);
                if (resource < 0 || !float.IsFinite(item.Value)) { reason = "Unsupported construction resource: " + item.Key; return false; }
                resources[resource] += (long)Math.Ceiling(Math.Max(0, item.Value));
            }
            for (int i = 0; i < resources.Length; i++)
            {
                if (resources[i] <= 0) continue;
                var available = ResourceAmount(district, i);
                if (resources[i] > available)
                { reason = $"Requires {resources[i]:N0} t of {ResourceIds[i]} (available: {available:N0} t)."; return false; }
            }
            bool hasRoad = false, hasWater = false;
            if (stats?.RequiresRoad == true || stats?.RequiresWaterfront == true)
            {
                float metresX = DistrictScale.SizeMeters(district.Width), metresZ = DistrictScale.SizeMeters(district.Height);
                void Check(int cx, int cz)
                {
                    if (stats.RequiresRoad && road != null && road(cx,cz)) hasRoad = true;
                    if (stats.RequiresWaterfront && water != null)
                    {
                        var n = new Vector2(((cx+.5f)*DistrictScale.CellSizeMeters+shoreOffset.x)/metresX,
                            ((cz+.5f)*DistrictScale.CellSizeMeters+shoreOffset.y)/metresZ);
                        if (n.x >= 0 && n.x <= 1 && n.y >= 0 && n.y <= 1 && water(n)) hasWater = true;
                    }
                }
                for (int xx = x; xx < x + width; xx++) { Check(xx,z-1); Check(xx,z+depth); }
                for (int zz = z; zz < z + depth; zz++) { Check(x-1,zz); Check(x+width,zz); }
                if (stats.RequiresRoad && !hasRoad) { reason = "Requires a road along the lot boundary."; return false; }
                if (stats.RequiresWaterfront && !hasWater) { reason = "Requires river frontage along the lot boundary."; return false; }
            }
            return true;
        }
        public static void Consume(RegionCityTile district, long[] resources)
        {
            if (district == null || resources == null) return;
            for (int i = 0; i < Math.Min(ResourceIds.Length, resources.Length); i++)
                if (resources[i] > 0) AddResource(district, i, -resources[i]);
        }

        // Fixed stockpile slots keep placement checks bounded regardless of
        // district size. Wood retains its existing Labor.Wood save location.
        private static readonly string[] ResourceIds =
        { "food", "lumber", "stone", "bricks", "coal", "iron ore",
          "gold", "oil", "jewels", "cloth" };

        private static int ResourceIndex(string id) => id?.ToLowerInvariant() switch
        {
            "food" => 0, "wood" or "lumber" => 1, "stone" => 2,
            "brick" or "bricks" => 3, "coal" => 4,
            "iron-ore" or "iron ore" or "ironore" => 5,
            "gold" => 6, "oil" => 7, "jewel" or "jewels" => 8,
            "cloth" => 9, _ => -1
        };

        private static int ResourceAmount(RegionCityTile district, int index)
        {
            if (index < 4) return DistrictLotSimulation.ResourceAmount(district, index);
            var stock = district.ResourceInventory ??= new DistrictResourceInventory();
            return index switch
            {
                4 => stock.Coal, 5 => stock.IronOre, 6 => stock.Gold,
                7 => stock.Oil, 8 => stock.Jewels, 9 => stock.Cloth,
                _ => 0
            };
        }

        private static void AddResource(RegionCityTile district, int index,
            long delta)
        {
            if (index < 4)
            {
                DistrictLotSimulation.AddResource(district, index, delta);
                return;
            }
            var stock = district.ResourceInventory ??= new DistrictResourceInventory();
            var value = DistrictLotSimulation.Clamp((long)ResourceAmount(district, index) + delta);
            switch (index)
            {
                case 4: stock.Coal = value; break;
                case 5: stock.IronOre = value; break;
                case 6: stock.Gold = value; break;
                case 7: stock.Oil = value; break;
                case 8: stock.Jewels = value; break;
                case 9: stock.Cloth = value; break;
            }
        }
    }
}
