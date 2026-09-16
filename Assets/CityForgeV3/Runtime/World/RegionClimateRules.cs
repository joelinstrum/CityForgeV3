namespace CityForgeV3.World
{
    public static class RegionClimateRules
    {
        public static bool AllowsSnow(RegionClimate climate) => climate == RegionClimate.Temperate;
        public static bool AllowsForest(RegionClimate climate) => climate != RegionClimate.Desert;
        public static bool AllowsTree(RegionClimate climate, string id)
        {
            if (id == "fraser-fir-snowy") return AllowsSnow(climate);
            if (climate == RegionClimate.Desert) return id == "date-palm";
            if (climate == RegionClimate.Tropical) return FloraFamilies.ForTree(id) == FloraFamilies.Tropical;
            return climate == RegionClimate.Mediterranean || FloraFamilies.ForTree(id) != FloraFamilies.Tropical;
        }
        public static string PresentationTree(RegionClimate climate, string id) =>
            !AllowsSnow(climate) && id == "fraser-fir-snowy" ? "fraser-fir-large" : id;
        public static void Apply(RegionSaveData region)
        {
            region.Terrain ??= new();
            foreach (var tile in region.Tiles) tile.Climate = region.Terrain.Climate;
        }
        public static string Description(RegionClimate climate) => climate switch
        {
            RegionClimate.Temperate => "Northeastern US: deciduous trees and firs, with snowy winters.",
            RegionClimate.Desert => "Dry climate. Sparse and Wooded tree generation are unavailable; palms can be planted individually.",
            RegionClimate.Tropical => "Warm throughout the year, with tropical trees and no snow.",
            _ => "Italy or California: temperate and tropical trees, with no snow."
        };
    }
}
