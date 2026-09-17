using System;
using System.Collections.Generic;

namespace CityForgeV3.World
{
    public static class LotEconomy
    {
        public static int CalculatePlopCost(LotSaveData lot)
        {
            if (lot == null) return 0;
            long total = Math.Max(0, lot.BasePlopCost);
            foreach (var placed in lot.Buildings3D ?? new List<PlacedBuilding3D>())
            {
                var building = BuildingContentCatalog.Find(placed?.AssetId);
                if (building != null) total += Math.Max(0, building.plopCost);
            }
            foreach (var placed in lot.Buildings ?? new List<PlacedBuilding>())
            {
                if (placed == null || string.IsNullOrWhiteSpace(placed.BuildingId))
                    continue;
                try
                {
                    total += HybridBuildingPackageRegistry.Load(
                        BuildingCatalog.Find(placed.BuildingId).PackageResourcePath)
                        .PlopCost;
                }
                catch (KeyNotFoundException)
                {
                    // Missing content is reported by the ordinary dependency
                    // path; it must not make cost inspection itself fail.
                }
            }
            return (int)Math.Min(int.MaxValue, total);
        }

        public static IReadOnlyDictionary<string, float> ConstructionRequirements(
            LotSaveData lot)
        {
            var totals = new Dictionary<string, float>(
                StringComparer.OrdinalIgnoreCase);
            if (lot == null) return totals;
            Add(lot.Stats?.ConstructionResources, totals);
            foreach (var placed in lot.Buildings3D ?? new List<PlacedBuilding3D>())
            {
                var building = BuildingContentCatalog.Find(placed?.AssetId);
                foreach (var item in building?.constructionRequirements ??
                             Array.Empty<ContentResourceAmount>())
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.resourceId) ||
                        item.amount <= 0f) continue;
                    totals.TryGetValue(item.resourceId, out var current);
                    totals[item.resourceId] = current + item.amount;
                }
            }
            foreach (var placed in lot.Buildings ?? new List<PlacedBuilding>())
            {
                if (placed == null || string.IsNullOrWhiteSpace(placed.BuildingId))
                    continue;
                try
                {
                    Add(HybridBuildingPackageRegistry.Load(
                        BuildingCatalog.Find(placed.BuildingId).PackageResourcePath)
                        .ConstructionRequirements, totals);
                }
                catch (KeyNotFoundException) { }
            }
            return totals;
        }

        private static void Add(IEnumerable<ContentResourceAmount> items,
            Dictionary<string, float> totals)
        {
            foreach (var item in items ?? Array.Empty<ContentResourceAmount>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.resourceId) ||
                    item.amount <= 0f) continue;
                totals.TryGetValue(item.resourceId, out var current);
                totals[item.resourceId] = current + item.amount;
            }
        }
    }
}
