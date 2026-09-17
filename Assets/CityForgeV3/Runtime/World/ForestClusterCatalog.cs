using UnityEngine;

namespace CityForgeV3.World
{
    // One cluster is one saved/selectable flora item. Its five depicted trees
    // are scenery; separate Cilician fir records retain the lumber contract.
    public static class ForestClusterCatalog
    {
        public const int VariantCount = 5;
        public const float PixelsPerUnit = 50f;
        public const float ClearanceMeters = 16f;
        public static readonly Vector2 Pivot = new(.5f, .065f);
        public static string Id(int variant) => "forest-cluster-0" + (variant + 1);
        public static bool IsCluster(string id) => id == "forest-cluster-01" ||
            id == "forest-cluster-02" || id == "forest-cluster-03" ||
            id == "forest-cluster-04" || id == "forest-cluster-05";
        public static bool IsTexture(string name) => name != null &&
            name.EndsWith("-summer") && IsCluster(name.Substring(0, name.Length - 7));
        // District flora currently renders in summer. Seasonal art studies are
        // not production cutouts yet, so all callers share the ready summer art.
        public static string ResourcePath(string id) =>
            "CityForgeV3/Flora/ForestClustersV01/" + id + "-summer";
    }
}
