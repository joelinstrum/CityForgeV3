using UnityEngine;

namespace CityForgeV3.World
{
    // Five saved identities remain compatible; they share two approved palettes.
    // A cluster is scenery. Separate fir records retain the lumber contract.
    public static class ForestClusterCatalog
    {
        public const int VariantCount = 5;
        public const float PixelsPerUnit = 50f;
        public const float ClearanceMeters = 16f;
        public static readonly Vector2 Pivot = new(.5f, .027f);
        public static string Id(int variant) => "forest-cluster-0" + (variant + 1);
        public static bool IsCluster(string id) => id == "forest-cluster-01" ||
            id == "forest-cluster-02" || id == "forest-cluster-03" ||
            id == "forest-cluster-04" || id == "forest-cluster-05";
        public static bool IsTexture(string name) => name != null &&
            IsCluster(FloraTreeRepairs.Identity(name)) &&
            (name.EndsWith("-summer") || name.EndsWith("-autumn") || name.EndsWith("-winter"));
        public static SeasonPreset SeasonForIndex(int index) => (Mathf.Max(0, index) % 4) switch
        {
            1 => SeasonPreset.Autumn, 2 => SeasonPreset.Winter,
            3 => SeasonPreset.Spring, _ => SeasonPreset.Summer
        };
        public static string ResourcePath(string id, SeasonPreset season = SeasonPreset.Summer)
        {
            if (!IsCluster(id)) return null;
            string palette = (id[16] - '1') % 2 == 0 ? "01" : "02";
            string suffix = season == SeasonPreset.Autumn ? "autumn" :
                season == SeasonPreset.Winter ? "winter" : "summer";
            return "CityForgeV3/Flora/ForestClustersRealisticV01/forest-cluster-" + palette + "-" + suffix;
        }
    }
}
