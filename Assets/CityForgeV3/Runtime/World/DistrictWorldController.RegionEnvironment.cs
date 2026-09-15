using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
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
            if (sun == null) return;
            sun.transform.rotation = TimeOfDayLighting.SunRotation(preset);
            sun.color = preset == TimeOfDayPreset.Morning ? new Color(1f,.985f,.96f) : spec.SunColor;
            sun.intensity = preset switch
            {
                TimeOfDayPreset.Morning => .62f,
                TimeOfDayPreset.Noon => 1.05f,
                TimeOfDayPreset.Afternoon => .50f * 1.35f,
                TimeOfDayPreset.Evening => .14f,
                _ => .035f
            };
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = preset == TimeOfDayPreset.Noon ? .92f : .86f;
        }
    }
}
