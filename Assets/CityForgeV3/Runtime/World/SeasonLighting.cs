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

        public static Color GroundColor(SeasonPreset preset, Color baseline) =>
            preset switch
            {
                SeasonPreset.Spring => Multiply(baseline,
                    new Color(0.94f, 1.08f, 0.91f, 1f)),
                SeasonPreset.Autumn => Multiply(baseline,
                    new Color(0.76f, 0.68f, 0.48f, 1f)),
                SeasonPreset.Winter => Color.Lerp(baseline,
                    new Color(0.78f, 0.82f, 0.82f, baseline.a), 0.82f),
                _ => baseline
            };

        public static Color FloraTint(SeasonPreset preset) => preset switch
        {
            SeasonPreset.Spring => new Color(0.82f, 1.08f, 0.78f, 1f),
            SeasonPreset.Autumn => new Color(1.02f, 0.62f, 0.28f, 1f),
            // Winter trees already provide authored leafless bark colors.
            // Preserve that variation and apply only a restrained cool cast.
            SeasonPreset.Winter => new Color(0.90f, 0.93f, 0.96f, 1f),
            _ => Color.white
        };

        public static Color BuildingTint(SeasonPreset preset) => preset switch
        {
            SeasonPreset.Spring => new Color(0.98f, 1.02f, 0.97f, 1f),
            SeasonPreset.Autumn => new Color(1f, 0.91f, 0.78f, 1f),
            SeasonPreset.Winter => new Color(0.90f, 0.95f, 1f, 1f),
            _ => Color.white
        };

        public static Color Multiply(Color left, Color right) => new(
            left.r * right.r,
            left.g * right.g,
            left.b * right.b,
            left.a * right.a);
    }
}
