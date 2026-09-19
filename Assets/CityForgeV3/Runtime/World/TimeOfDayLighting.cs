using UnityEngine;

namespace CityForgeV3.World
{
    public enum TimeOfDayPreset
    {
        Morning,
        Noon,
        Afternoon,
        Evening,
        Night
    }

    public static class DistrictDayCycle
    {
        public const float MorningSeconds = 60f;
        public const float NoonSeconds = 300f;
        public const float AfternoonSeconds = 60f;
        public const float EveningSeconds = 10f;
        public const float NightSeconds = 30f;
        public const float TotalCycleSeconds = MorningSeconds + NoonSeconds +
            AfternoonSeconds + EveningSeconds + NightSeconds;

        public static float Duration(TimeOfDayPreset preset) => preset switch
        {
            TimeOfDayPreset.Morning => MorningSeconds,
            TimeOfDayPreset.Noon => NoonSeconds,
            TimeOfDayPreset.Afternoon => AfternoonSeconds,
            TimeOfDayPreset.Evening => EveningSeconds,
            TimeOfDayPreset.Night => NightSeconds,
            _ => NoonSeconds
        };

        public static TimeOfDayPreset Next(TimeOfDayPreset preset) =>
            preset switch
            {
                TimeOfDayPreset.Morning => TimeOfDayPreset.Noon,
                TimeOfDayPreset.Noon => TimeOfDayPreset.Afternoon,
                TimeOfDayPreset.Afternoon => TimeOfDayPreset.Evening,
                TimeOfDayPreset.Evening => TimeOfDayPreset.Night,
                _ => TimeOfDayPreset.Morning
            };

        public static void Set(RegionCityTile district, TimeOfDayPreset preset)
        {
            if (district == null) return;
            district.TimeOfDay = preset;
            district.TimeOfDaySeconds = 0f;
        }

        public static bool Advance(RegionCityTile district, float seconds)
        {
            if (district == null || !district.Founded || seconds <= 0f ||
                !float.IsFinite(seconds)) return false;
            district.TimeOfDaySeconds = Mathf.Max(0f,
                district.TimeOfDaySeconds) + seconds;
            district.TimeOfDaySeconds %= TotalCycleSeconds;
            var changed = false;
            while (district.TimeOfDaySeconds >= Duration(district.TimeOfDay))
            {
                district.TimeOfDaySeconds -= Duration(district.TimeOfDay);
                district.TimeOfDay = Next(district.TimeOfDay);
                changed = true;
            }
            return changed;
        }
    }

    public readonly struct TimeOfDayLightingSpec
    {
        public TimeOfDayLightingSpec(
            TimeOfDayPreset preset,
            string label,
            float sunElevation,
            float sunAzimuth,
            float sunIntensity,
            Color sunColor,
            Color ambientColor,
            Color groundColor,
            Color backgroundColor,
            Color screenTint,
            Color neutralArtworkTint)
        {
            Preset = preset;
            Label = label;
            SunElevation = sunElevation;
            SunAzimuth = sunAzimuth;
            SunIntensity = sunIntensity;
            SunColor = sunColor;
            AmbientColor = ambientColor;
            GroundColor = groundColor;
            BackgroundColor = backgroundColor;
            ScreenTint = screenTint;
            NeutralArtworkTint = neutralArtworkTint;
        }

        public TimeOfDayPreset Preset { get; }
        public string Label { get; }
        public float SunElevation { get; }
        public float SunAzimuth { get; }
        public float SunIntensity { get; }
        public Color SunColor { get; }
        public Color AmbientColor { get; }
        public Color GroundColor { get; }
        public Color BackgroundColor { get; }
        public Color ScreenTint { get; }
        public Color NeutralArtworkTint { get; }
    }

    public static class TimeOfDayLighting
    {
        private static readonly TimeOfDayLightingSpec[] Specs =
        {
            new(
                TimeOfDayPreset.Morning,
                "MORNING",
                24f,
                90f,
                0.78f,
                new Color(1f, 0.73f, 0.48f),
                new Color(0.31f, 0.35f, 0.43f),
                new Color(0.24f, 0.32f, 0.25f),
                new Color(0.12f, 0.20f, 0.29f),
                new Color(0.96f, 0.55f, 0.25f, 0.035f),
                new Color(0.96f, 0.79f, 0.65f)),
            new(
                TimeOfDayPreset.Noon,
                "NOON",
                70f,
                180f,
                0.92f,
                new Color(1f, 0.985f, 0.95f),
                new Color(0.32f, 0.34f, 0.37f),
                new Color(0.40f, 0.56f, 0.235f),
                new Color(0.055f, 0.16f, 0.205f),
                new Color(0.88f, 0.94f, 1f, 0.008f),
                new Color(0.965f, 0.965f, 0.955f)),
            new(
                TimeOfDayPreset.Afternoon,
                "AFTERNOON",
                34f,
                270f,
                0.82f,
                new Color(1f, 0.94f, 0.84f),
                new Color(0.35f, 0.355f, 0.38f),
                new Color(0.385f, 0.50f, 0.22f),
                new Color(0.085f, 0.155f, 0.20f),
                Color.clear,
                Color.white),
            new(
                TimeOfDayPreset.Evening,
                "EVENING",
                8f,
                272f,
                0.19f,
                new Color(0.82f, 0.47f, 0.33f),
                new Color(0.09f, 0.105f, 0.15f),
                new Color(0.09f, 0.12f, 0.105f),
                new Color(0.0175f, 0.030f, 0.060f),
                new Color(0.08f, 0.15f, 0.34f, 0.15f),
                new Color(0.23f, 0.26f, 0.34f)),
            new(
                TimeOfDayPreset.Night,
                "NIGHT",
                -18f,
                318f,
                0.05f,
                new Color(0.48f, 0.61f, 0.86f),
                new Color(0.056f, 0.073f, 0.123f),
                new Color(0.062f, 0.090f, 0.084f),
                new Color(0.0045f, 0.010f, 0.028f),
                new Color(0.04f, 0.08f, 0.22f, 0.23f),
                new Color(0.157f, 0.190f, 0.291f))
        };

        public static TimeOfDayLightingSpec For(TimeOfDayPreset preset) =>
            Specs[(int)preset];

        public static Quaternion SunRotation(TimeOfDayPreset preset)
        {
            var spec = For(preset);
            // Unity's directional-light forward vector is the direction the
            // rays travel. CityForge's accepted east/west presentation is the
            // fixed isometric camera's visible horizontal axis: morning rays
            // travel left from the visible east, while afternoon rays travel
            // right from the visible west. Keep those two authored views
            // camera-readable; the remaining presets use compass conversion.
            var presentationYaw = preset switch
            {
                TimeOfDayPreset.Morning => 315f,
                // Higher southwest key: short shadows travel toward +X/+Z.
                TimeOfDayPreset.Noon => 45f,
                TimeOfDayPreset.Afternoon => 135f,
                _ => spec.SunAzimuth + 90f
            };
            return Quaternion.Euler(
                spec.SunElevation, presentationYaw, 0f);
        }
    }
}
