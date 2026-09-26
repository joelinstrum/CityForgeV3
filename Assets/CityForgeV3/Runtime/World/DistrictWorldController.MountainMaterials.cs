using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        // Terrain artwork is physically anchored to the district. Camera zoom
        // must not resize it or reveal a different texture coordinate system.
        public static float DistrictGrassWorldSizeForZoom(DistrictZoomLevel level)
            => DistrictGrassTextureWorldSizeMeters;

        public static bool DistrictGrassUsesSmoothFiltering(DistrictZoomLevel level) =>
            level >= DistrictZoomLevel.LOD3;

        // Zoom 3 keeps Zoom 2's unfiltered texture and base brightness, with
        // separate world-anchored grain to retain detail at its farther camera.
        // Farther zooms retain their calibrated filtering.
        public static float DistrictGrassFilteringStrengthForZoom(
            DistrictZoomLevel level) => level switch
        {
            DistrictZoomLevel.LOD2 => 0f,
            DistrictZoomLevel.LOD3 => .3f,
            DistrictZoomLevel.LOD4 => .2f,
            DistrictZoomLevel.LOD5Billboard => .15f,
            _ => 0f
        };

        public static float DistrictGrassNoiseStrengthForZoom(
            DistrictZoomLevel level) => level switch
        {
            DistrictZoomLevel.LOD2 => .25f,
            DistrictZoomLevel.LOD3 => .6f,
            DistrictZoomLevel.LOD4 => .8f,
            DistrictZoomLevel.LOD5Billboard => 1f,
            _ => 0f
        };

        public static float DistrictGrassBrightnessForZoom(
            DistrictZoomLevel level) => level switch
        {
            DistrictZoomLevel.LOD2 => 1f,
            DistrictZoomLevel.LOD3 => .94f,
            DistrictZoomLevel.LOD4 => .91f,
            DistrictZoomLevel.LOD5Billboard => .89f,
            _ => 1f
        };

        // Player-facing Zoom 3's closer camera needs smaller world-space grain
        // to keep the stipple near the apparent size seen at Zoom 4.
        public static float DistrictGrassGrainFrequencyForZoom(
            DistrictZoomLevel level) =>
            level == DistrictZoomLevel.LOD2 ? 1.82f : 1f;

        private void ApplyDistrictGrassZoomScale()
        {
            if (_terrainDistrict?.Hills?.Mountains == true) return;
            var material = _groundRenderer?.sharedMaterial;
            if (material == null) return;
            if (GrassMapping == GrassMappingMode.DistrictWide &&
                material.GetFloat("_DistrictWideGrass") > 0.5f)
            {
                if (material.mainTextureScale != Vector2.one)
                    material.mainTextureScale = Vector2.one;
                return;
            }
            float metres = DistrictGrassWorldSizeForZoom(_zoomLevel);
            var scale = new Vector2(_widthMeters / metres, _depthMeters / metres);
            if (material.mainTextureScale != scale) material.mainTextureScale = scale;
            float distant = DistrictGrassFilteringStrengthForZoom(_zoomLevel);
            if (material.HasProperty("_DistantMeadow") && material.GetFloat("_DistantMeadow") != distant)
                material.SetFloat("_DistantMeadow", distant);
            float noise = (_terrainDistrict?.Hills?.HeightMeters ?? 0)>0
                ? 0f : DistrictGrassNoiseStrengthForZoom(_zoomLevel);
            if (material.HasProperty("_FarGrassNoise") &&
                material.GetFloat("_FarGrassNoise") != noise)
                material.SetFloat("_FarGrassNoise", noise);
            float brightness = DistrictGrassBrightnessForZoom(_zoomLevel);
            if (material.HasProperty("_FarGrassBrightness") &&
                material.GetFloat("_FarGrassBrightness") != brightness)
                material.SetFloat("_FarGrassBrightness", brightness);
            float grainFrequency = DistrictGrassGrainFrequencyForZoom(_zoomLevel);
            if (material.HasProperty("_FarGrassGrainFrequency") &&
                material.GetFloat("_FarGrassGrainFrequency") != grainFrequency)
                material.SetFloat("_FarGrassGrainFrequency", grainFrequency);
            // Preserve the authored grass texture's fine detail at Zoom 3
            // instead of reconstructing it with strong procedural noise.
            float detailMipScale = _zoomLevel == DistrictZoomLevel.LOD2 ? .5f : 1f;
            if (material.HasProperty("_GrassDetailMipScale") &&
                material.GetFloat("_GrassDetailMipScale") != detailMipScale)
                material.SetFloat("_GrassDetailMipScale", detailMipScale);
        }

        private void ConfigureMountainGroundMaterial()
        {
            var material = _groundRenderer?.sharedMaterial;
            if (material == null) return;
            bool mountains = _terrainDistrict?.Hills?.Mountains == true;
            var shader = Shader.Find(mountains ? "CityForgeV3/MountainGroundSurfaceV10" : "CityForgeV3/MeadowGroundSurface");
            if (shader == null) return;
            material.shader = shader;
            bool districtWide = GrassMapping == GrassMappingMode.DistrictWide;
            var grass = Resources.Load<Texture2D>(districtWide
                ? DistrictWideGrassResource
                : mountains ? DefaultGrassResource : DistrictGrassResource);
            if (grass == null && districtWide)
            {
                districtWide = false;
                grass = Resources.Load<Texture2D>(mountains ? DefaultGrassResource : DistrictGrassResource);
            }
            if (grass != null) material.mainTexture = grass;
            float grassMetres = mountains ? GrassTextureWorldSizeMeters : DistrictGrassWorldSizeForZoom(_zoomLevel);
            material.mainTextureScale = districtWide ? Vector2.one
                : new Vector2(_widthMeters / grassMetres, _depthMeters / grassMetres);
            material.mainTextureOffset = Vector2.zero;
            material.SetFloat("_DistrictWideGrass", districtWide ? 1f : 0f);
            if (!mountains)
            {
                material.SetFloat("_TextureWorldSize",
                    DistrictGrassTextureWorldSizeMeters);
                // The macro texture now owns broad color variation. Preserve
                // its authored palette and avoid a second dry-patch system.
                material.SetFloat("_GrassHueShift", 0f);
                bool hills=(_terrainDistrict?.Hills?.HeightMeters ?? 0)>0;
                material.SetFloat("_RollingHillDarkSlopeLift",RollingHillDarkSlopeLift);
                material.SetFloat("_RollingHillDeepShadeLift",RollingHillDeepShadeLift);
                // The color lift uses actual world Y relative to the relief
                // control; keep this independent of horizontal field tuning.
                float reliefHeight=Mathf.Clamp(_terrainDistrict?.Hills?.HeightMeters ?? 0,0,240);
                float reliefScale=_terrainDistrict?.Hills?.VerticalReliefScale ?? 1f;
                if(reliefScale<=0)reliefScale=1f;
                material.SetFloat("_MeadowReliefHeight",reliefHeight*
                    Mathf.Clamp(reliefScale,.25f,4f)*DistrictElevation.RollingHillVerticalScale);
                material.SetFloat("_MeadowPatchStrength", 0f);
                material.DisableKeyword("MEADOW_PATCHES");
                material.SetFloat("_HillHeight",Mathf.Clamp(_terrainDistrict?.Hills?.HeightMeters ?? 0,1,60));
                if(hills) material.EnableKeyword("HILL_MEADOW"); else material.DisableKeyword("HILL_MEADOW");
                ApplyDistrictGrassZoomScale();
                return;
            }
            material.DisableKeyword("HILL_MEADOW");
            material.DisableKeyword("MEADOW_PATCHES");
            var rock = Resources.Load<Texture2D>("CityForgeV3/Terrain/QuietSilverV01/quiet-silver-v01");
            var shale = Resources.Load<Texture2D>("CityForgeV3/Terrain/AlpineRockV01/scree-v01");
            var brown = Resources.Load<Texture2D>("CityForgeV3/Terrain/MountainBrownV01/brown-scree-v01");
            material.SetTexture("_BrownTex", brown);
            material.SetFloat("_BrownStrength", brown != null ? 1 : 0);
            material.SetTexture("_BedrockTex", rock);
            material.SetTexture("_ShaleTex", shale);
            material.SetFloat("_RockEnabled", rock != null && shale != null ? 1 : 0);
        }

        // Fraction of the darkest slope lighting deficit restored toward level
        // grass. The higher value also offsets darker normals from the 1.3x Y
        // exaggeration; it leaves neutral terrain and highlights unchanged.
        public const float RollingHillDarkSlopeLift = .5f;
        // Additional reduction of the deepest remaining slope darkness.
        public const float RollingHillDeepShadeLift = .225f;
    }
}
