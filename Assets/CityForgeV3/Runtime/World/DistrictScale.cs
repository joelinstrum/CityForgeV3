namespace CityForgeV3.World
{
    using UnityEngine;

    public enum DistrictZoomLevel
    {
        LOD0 = 0,
        LOD1 = 1,
        LOD2 = 2,
        LOD3 = 3,
        LOD4 = 4,
        LOD5Billboard = 5
    }

    /// <summary>
    /// Shared physical scale for region districts. Terrain, roads, zoning and
    /// district rendering must use this contract so authored features retain
    /// consistent proportions across editor modes.
    /// </summary>
    public static class DistrictScale
    {
        public const int CellsPerRegionUnit = 64;
        public const float CellSizeMeters = LotMetricScale.MajorGridMeters;

        public static int Columns(int regionUnits) =>
            regionUnits * CellsPerRegionUnit;

        public static float SizeMeters(int regionUnits) =>
            Columns(regionUnits) * CellSizeMeters;

        public static int GridSpanForMeters(float meters) =>
            Mathf.Max(1, Mathf.CeilToInt(meters / CellSizeMeters));

        public static float SnapFootprintCenter(float normalizedPosition,
            int axisCells, float footprintMeters)
        {
            axisCells = Mathf.Max(1, axisCells);
            var span = Mathf.Min(axisCells, GridSpanForMeters(footprintMeters));
            var requestedCenter = Mathf.Clamp01(normalizedPosition) * axisCells;
            var startCell = Mathf.Clamp(
                Mathf.RoundToInt(requestedCenter - span * 0.5f),
                0, axisCells - span);
            return (startCell + span * 0.5f) / axisCells;
        }
    }

    /// <summary>
    /// Discrete district camera stops. Building representations are selected
    /// by each asset: the schoolhouse uses images at LOD4 and LOD5, while
    /// package LODGroups choose their mesh or billboard by screen size.
    /// </summary>
    public static class DistrictZoom
    {
        public const DistrictZoomLevel DefaultLevel = DistrictZoomLevel.LOD4;

        public static DistrictZoomLevel Step(DistrictZoomLevel current,
            int direction)
        {
            var next = (int)current + direction;
            if (next < (int)DistrictZoomLevel.LOD0)
                next = (int)DistrictZoomLevel.LOD0;
            if (next > (int)DistrictZoomLevel.LOD5Billboard)
                next = (int)DistrictZoomLevel.LOD5Billboard;
            return (DistrictZoomLevel)next;
        }

        public static float Scale(DistrictZoomLevel level) => level switch
        {
            DistrictZoomLevel.LOD0 => 20f,
            DistrictZoomLevel.LOD1 => 7.78f,
            DistrictZoomLevel.LOD2 => 3.03f,
            DistrictZoomLevel.LOD3 => 1.18f,
            DistrictZoomLevel.LOD4 => 1f,
            DistrictZoomLevel.LOD5Billboard => 0.55f,
            _ => 1f
        };

        public static bool UsesBuildingBillboards(DistrictZoomLevel level) =>
            level == DistrictZoomLevel.LOD5Billboard;

        public static int GridInterval(DistrictZoomLevel level) => level switch
        {
            DistrictZoomLevel.LOD0 => 1,
            DistrictZoomLevel.LOD1 => 2,
            DistrictZoomLevel.LOD2 => 4,
            DistrictZoomLevel.LOD3 => 8,
            DistrictZoomLevel.LOD4 => 16,
            DistrictZoomLevel.LOD5Billboard => 32,
            _ => 16
        };

        public static float PanStepMeters(DistrictZoomLevel level) => level switch
        {
            DistrictZoomLevel.LOD0 => LotMetricScale.MinorGridMeters * 2f,
            DistrictZoomLevel.LOD1 => 10f,
            DistrictZoomLevel.LOD2 => 30f,
            DistrictZoomLevel.LOD3 => 70f,
            DistrictZoomLevel.LOD4 => 180f,
            DistrictZoomLevel.LOD5Billboard => 280f,
            _ => 180f
        };

        /// <summary>
        /// Converts the requested on-screen movement of the world into the
        /// isometric camera-target movement that produces it. A positive
        /// horizontal value moves the visible world right; a positive vertical
        /// value moves it up.
        /// </summary>
        public static Vector2 PanOffsetForWorldMotion(int horizontal,
            int vertical, float stepMeters)
        {
            const float diagonal = 0.70710678f;
            var cameraRight = new Vector2(diagonal, -diagonal);
            var cameraScreenUp = new Vector2(diagonal, diagonal);
            return (-horizontal * cameraRight - vertical * cameraScreenUp) *
                   stepMeters;
        }

        public static string DisplayName(DistrictZoomLevel level) =>
            UsesBuildingBillboards(level) ? "LOD5 BILLBOARD" : level.ToString();
    }
}
