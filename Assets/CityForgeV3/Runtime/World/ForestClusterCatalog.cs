using UnityEngine;

namespace CityForgeV3.World
{
    // Legacy identities remain compatible. New identities encode dominant
    // family and terrain footprint while each is still one scenery billboard.
    // Separate fir records retain the lumber contract.
    public static class ForestClusterCatalog
    {
        public const int VariantCount = 5;
        public const float CompactPixelsPerUnit = 50f;
        public const float LargePixelsPerUnit = 36f;
        public const float CompactClearanceMeters = 16f;
        public const float LargeClearanceMeters = 23f;
        public static readonly Vector2 Pivot = new(.5f, .027f);
        public static string Id(int variant) => "forest-cluster-0" + (variant + 1);
        public static string Id(string family, bool large) => "forest-" +
            (family == FloraFamilies.Mountain ? "mountain" :
             family == FloraFamilies.Tropical ? "tropical" : "deciduous") +
            (large ? "-large" : "-compact");
        public static bool IsLarge(string id) => id != null && id.EndsWith("-large");
        public static float ClearanceMeters(string id) => IsLarge(id)
            ? LargeClearanceMeters : CompactClearanceMeters;
        public static float PixelsPerUnit(string id) => IsLarge(id)
            ? LargePixelsPerUnit : CompactPixelsPerUnit;
        public static bool IsCluster(string id) => id == "forest-cluster-01" ||
            id == "forest-cluster-02" || id == "forest-cluster-03" ||
            id == "forest-cluster-04" || id == "forest-cluster-05" ||
            id == "forest-deciduous-compact" || id == "forest-mountain-compact" ||
            id == "forest-tropical-compact" || id == "forest-deciduous-large" ||
            id == "forest-mountain-large" || id == "forest-tropical-large";
        public static bool IsTexture(string name) => name != null &&
            IsCluster(FloraTreeRepairs.Identity(name)) &&
            (name.EndsWith("-summer") || name.EndsWith("-autumn") || name.EndsWith("-winter"));
        public static bool UsesDepthShadedCutout(string name) => name != null &&
            (name.EndsWith("-summer") || name.EndsWith("-autumn")) &&
            (name.StartsWith("forest-deciduous-") ||
             name.StartsWith("forest-mountain-") ||
             name.StartsWith("forest-tropical-"));
        // The far-view artwork is deliberately used at every zoom in this
        // visual experiment so close-up readability can be judged in play.
        public static string FarCanopyResourcePath(string id,
            SeasonPreset season)
        {
            if (id != "forest-deciduous-compact" &&
                id != "forest-deciduous-large") return null;
            if (season == SeasonPreset.Winter) return null;
            var suffix = season == SeasonPreset.Autumn ? "autumn" : "summer";
            return "CityForgeV3/Flora/ForestCanopyFarV01/" + id + "-" + suffix;
        }
        public static SeasonPreset SeasonForIndex(int index) => (Mathf.Max(0, index) % 4) switch
        {
            1 => SeasonPreset.Autumn, 2 => SeasonPreset.Winter,
            3 => SeasonPreset.Spring, _ => SeasonPreset.Summer
        };
        public static string ResourcePath(string id, SeasonPreset season = SeasonPreset.Summer)
        {
            if (!IsCluster(id)) return null;
            if (id.StartsWith("forest-deciduous-") || id.StartsWith("forest-mountain-") ||
                id.StartsWith("forest-tropical-"))
            {
                string familySuffix = id.StartsWith("forest-tropical-") ? "summer" :
                    season == SeasonPreset.Autumn ? "autumn" :
                    season == SeasonPreset.Winter ? "winter" : "summer";
                // Summer/spring use the accepted V03 depth-staggered art.
                // Autumn/winter use seasonal derivatives of those same
                // silhouettes so trunks never fall back to the old V01 row.
                string collection = familySuffix == "summer"
                    ? "ForestClustersFamilyMixV03"
                    : "ForestClustersFamilyMixV04";
                return "CityForgeV3/Flora/" + collection + "/" + id + "-" + familySuffix;
            }
            string palette = (id[16] - '1') % 2 == 0 ? "01" : "02";
            string suffix = season == SeasonPreset.Autumn ? "autumn" :
                season == SeasonPreset.Winter ? "winter" : "summer";
            return "CityForgeV3/Flora/ForestClustersRealisticV01/forest-cluster-" + palette + "-" + suffix;
        }
    }
}
