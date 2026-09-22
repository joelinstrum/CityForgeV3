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
        private static readonly int WorldWhitePointId =
            Shader.PropertyToID("_CFWorldWhitePoint");
        private static readonly int HybridArtworkExposureId =
            Shader.PropertyToID("_CFHybridArtworkExposure");
        private static readonly int NativeSurfaceIndirectScaleId =
            Shader.PropertyToID("_CFNativeSurfaceIndirectScale");
        private static readonly int GardenSurfaceExposureId =
            Shader.PropertyToID("_CFGardenSurfaceExposure");

        public const float WorldWhitePoint = .98f;

        public static float RegionSunIntensity(TimeOfDayPreset preset) => preset switch
        {
            // Morning uses a softer key and stronger fill so lit faces are not
            // blown out while the shaded side remains readable.
            TimeOfDayPreset.Morning => .52f,
            // Ambient plus the full artwork sun must remain below display
            // white. The former 1.05 intensity produced about 1.37 at noon,
            // clipping texture highlights and flattening baked contrast.
            TimeOfDayPreset.Noon => .64f,
            TimeOfDayPreset.Afternoon => .675f,
            TimeOfDayPreset.Evening => .18f,
            _ => .035f
        };

        public static float HybridArtworkExposureFor(
            TimeOfDayPreset preset) => preset switch
        {
            // Directional building renders already contain material response,
            // occlusion, and shade. This is one family-wide display exposure,
            // not a replacement light or a per-building correction.
            TimeOfDayPreset.Morning => 1.50f,
            TimeOfDayPreset.Noon => 1.50f,
            TimeOfDayPreset.Afternoon => 1.50f,
            _ => 1f
        };

        public static float NativeSurfaceIndirectScaleFor(
            TimeOfDayPreset preset) => preset switch
        {
            // A rotated district Lot can present shaded native surfaces to the
            // fixed camera. Strengthen only their indirect diffuse response,
            // leaving direct highlights and albedo intact.
            TimeOfDayPreset.Morning => 2.5f,
            TimeOfDayPreset.Noon => 2.5f,
            TimeOfDayPreset.Afternoon => 2.5f,
            _ => 1f
        };

        public static float GardenSurfaceExposureFor(
            TimeOfDayPreset preset) => preset switch
        {
            // Native garden meshes share one post-light response so their
            // authored pale and saturated colors survive district shade. The
            // display-white shoulder prevents bright surfaces from clipping.
            TimeOfDayPreset.Morning => 1.3f,
            TimeOfDayPreset.Noon => 1.3f,
            TimeOfDayPreset.Afternoon => 1.3f,
            _ => 1f
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
                sunIntensity, sunRotation,
                HybridArtworkExposureFor(preset),
                NativeSurfaceIndirectScaleFor(preset),
                GardenSurfaceExposureFor(preset));
            if (sun == null) return;
            sun.transform.rotation = sunRotation;
            sun.color = sunColor;
            sun.intensity = sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = preset == TimeOfDayPreset.Noon ? .92f : .86f;
        }

        public static void ApplyWorldShaderLighting(Color ambientColor,
            Color sunColor, float sunIntensity, Quaternion sunRotation,
            float hybridArtworkExposure = 1f,
            float nativeSurfaceIndirectScale = 1f,
            float gardenSurfaceExposure = 1f)
        {
            var directionToSun = -(sunRotation * Vector3.forward).normalized;
            Shader.SetGlobalColor(WorldAmbientColorId, ambientColor);
            Shader.SetGlobalColor(WorldSunColorId, sunColor * sunIntensity);
            Shader.SetGlobalVector(WorldLightDirectionId, new Vector4(
                directionToSun.x, directionToSun.y, directionToSun.z, 0f));
            Shader.SetGlobalFloat(WorldWhitePointId, WorldWhitePoint);
            Shader.SetGlobalFloat(HybridArtworkExposureId,
                Mathf.Max(0f, hybridArtworkExposure));
            Shader.SetGlobalFloat(NativeSurfaceIndirectScaleId,
                Mathf.Max(0f, nativeSurfaceIndirectScale));
            Shader.SetGlobalFloat(GardenSurfaceExposureId,
                Mathf.Max(0f, gardenSurfaceExposure));
        }

        public static Color BoundWorldIllumination(Color illumination)
        {
            var peak = Mathf.Max(illumination.r,
                Mathf.Max(illumination.g, illumination.b));
            if (peak <= WorldWhitePoint) return illumination;
            var scale = WorldWhitePoint / peak;
            return new Color(illumination.r * scale,
                illumination.g * scale,
                illumination.b * scale,
                illumination.a);
        }

        public static Color RegionArtworkIllumination(
            TimeOfDayPreset preset)
        {
            var spec = TimeOfDayLighting.For(preset);
            var sunColor = preset == TimeOfDayPreset.Morning
                ? new Color(1f, .985f, .96f)
                : spec.SunColor;
            return BoundWorldIllumination(spec.AmbientColor +
                sunColor * RegionSunIntensity(preset));
        }
    }
}
