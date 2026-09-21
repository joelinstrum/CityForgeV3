using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        private static readonly int WorldAmbientColorId =
            Shader.PropertyToID("_CFWorldAmbientColor");
        private static readonly int WorldSunColorId =
            Shader.PropertyToID("_CFWorldSunColor");
        private static readonly int WorldLightDirectionId =
            Shader.PropertyToID("_CFWorldLightDirection");

        public static float RegionSunIntensity(TimeOfDayPreset preset) => preset switch
        {
            TimeOfDayPreset.Morning => .62f,
            // Ambient plus the full artwork sun must remain below display
            // white. The former 1.05 intensity produced about 1.37 at noon,
            // clipping texture highlights and flattening baked contrast.
            TimeOfDayPreset.Noon => .64f,
            TimeOfDayPreset.Afternoon => .675f,
            TimeOfDayPreset.Evening => .14f,
            _ => .035f
        };

        public static void ApplyRegionEnvironment(TimeOfDayPreset preset, Light sun)
        {
            var spec = TimeOfDayLighting.For(preset);
            RenderSettings.ambientMode = preset == TimeOfDayPreset.Noon ? AmbientMode.Trilight : AmbientMode.Flat;
            RenderSettings.ambientLight = spec.AmbientColor;
            if (preset == TimeOfDayPreset.Noon)
            {
                RenderSettings.ambientSkyColor = new Color(.42f,.44f,.47f);
                RenderSettings.ambientEquatorColor = new Color(.30f,.305f,.315f);
                RenderSettings.ambientGroundColor = new Color(.12f,.115f,.105f);
            }
            var sunRotation = TimeOfDayLighting.SunRotation(preset);
            var sunColor = preset == TimeOfDayPreset.Morning
                ? new Color(1f,.985f,.96f) : spec.SunColor;
            var sunIntensity = RegionSunIntensity(preset);
            ApplyWorldShaderLighting(spec.AmbientColor, sunColor,
                sunIntensity, sunRotation);
            if (sun == null) return;
            sun.transform.rotation = sunRotation;
            sun.color = sunColor;
            sun.intensity = sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = preset == TimeOfDayPreset.Noon ? .92f : .86f;
        }

        public static void ApplyWorldShaderLighting(Color ambientColor,
            Color sunColor, float sunIntensity, Quaternion sunRotation)
        {
            var directionToSun = -(sunRotation * Vector3.forward).normalized;
            Shader.SetGlobalColor(WorldAmbientColorId, ambientColor);
            Shader.SetGlobalColor(WorldSunColorId, sunColor * sunIntensity);
            Shader.SetGlobalVector(WorldLightDirectionId, new Vector4(
                directionToSun.x, directionToSun.y, directionToSun.z, 0f));
        }
    }
}
