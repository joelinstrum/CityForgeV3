using UnityEngine;

namespace CityForgeV3.World
{
    public enum SeasonPreset
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public static class SeasonLighting
    {
        public static string Label(SeasonPreset preset) =>
            preset.ToString().ToUpperInvariant();

        // Season chooses artwork and weather, not a second lighting palette.
        // Time-of-day and environment controls remain the sole light/color grade.
        public static Color GroundColor(SeasonPreset preset, Color baseline) =>
            baseline;

        public static Color FloraTint(SeasonPreset preset) => Color.white;

        public static Color BuildingTint(SeasonPreset preset) => Color.white;

        public static Color Multiply(Color left, Color right) => new(
            left.r * right.r,
            left.g * right.g,
            left.b * right.b,
            left.a * right.a);
    }
}
