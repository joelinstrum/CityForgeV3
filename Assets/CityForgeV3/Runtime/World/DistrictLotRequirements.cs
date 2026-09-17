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
            resources = new long[4]; reason = "";
            if (district == null || lot == null) { reason = "Lot unavailable."; return false; }
            int cost = LotEconomy.CalculatePlopCost(lot);
            if (district.Treasury < cost) { reason = $"Requires ${cost:N0} to place."; return false; }
            var stats = lot.Stats;
            if (!string.IsNullOrWhiteSpace(stats?.MinimumEraId) &&
                LotEraCatalog.IndexOf(era) < LotEraCatalog.IndexOf(stats.MinimumEraId))
            { reason = "Requires " + LotEraCatalog.DisplayName(stats.MinimumEraId) + "."; return false; }
            foreach (var item in LotEconomy.ConstructionRequirements(lot))
            {
                int resource = DistrictLotSimulation.ResourceIndex(item.Key);
                if (resource < 0 || !float.IsFinite(item.Value)) { reason = "Unsupported construction resource: " + item.Key; return false; }
                resources[resource] += (long)Math.Ceiling(Math.Max(0, item.Value));
            }
            var names = new[] { "food", "lumber", "stone", "bricks" };
            for (int i = 0; i < 4; i++) if (resources[i] > DistrictLotSimulation.ResourceAmount(district, i))
            { reason = $"Requires {resources[i]:N0} t of {names[i]} (available: {DistrictLotSimulation.ResourceAmount(district, i):N0} t)."; return false; }
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
            for (int i = 0; i < 4; i++) DistrictLotSimulation.AddResource(district, i, -resources[i]);
        }
    }
}
