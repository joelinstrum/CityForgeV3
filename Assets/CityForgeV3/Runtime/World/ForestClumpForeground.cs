using UnityEngine;

namespace CityForgeV3.World
{
    // One ordinary tree sits on the camera-facing edge of each new clump.
    // It remains an independent flora record, so existing selection, seasons,
    // batching and close-zoom ground shadows apply without a clump shadow.
    public static class ForestClumpForeground
    {
        public static string TreeId(string clumpId, int variation)
        {
            var slot = Mathf.Abs(variation % 3);
            if (clumpId != null && clumpId.StartsWith("forest-mountain-"))
                return slot == 1 ? "medium-balsam-fir" : "medium-fraser-fir";
            if (clumpId != null && clumpId.StartsWith("forest-tropical-"))
                return slot == 1 ? "la-fan-palm-a-medium" : "date-palm-short";
            return slot == 0 ? "american-elm" :
                slot == 1 ? "mature-oak" : "shagbark-hickory";
        }

        // The district camera looks from negative X and Z. A small lateral
        // variation keeps the foreground tree from forming a repeated row.
        public static Vector2 PointMeters(string clumpId, int variation,
            Vector2 center, float scale, float offsetScale = 1f)
        {
            float forward = (ForestClusterCatalog.IsLarge(clumpId) ? 18f : 13f) * scale * offsetScale;
            float side = ((variation % 3 + 3) % 3 - 1) * 5f * scale * offsetScale;
            const float diagonal = .70710678f;
            return center + new Vector2((-forward + side) * diagonal,
                (-forward - side) * diagonal);
        }
    }
}
