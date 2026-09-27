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

        // A small amount of district color keeps distant views varied without
        // replacing the grass appearance that works at Zoom 1 and 2.
        public static float DistrictGrassMapStrengthForZoom(DistrictZoomLevel level) => level switch
        {
            DistrictZoomLevel.LOD2 => .15f,
            DistrictZoomLevel.LOD3 => .20f,
            DistrictZoomLevel.LOD4 => .25f,
            DistrictZoomLevel.LOD5Billboard => .25f,
            _ => 0f
        };

        // The same world-anchored soil sits over the distant district grass
        // maps. Lower strength keeps wide hill faces from turning solid brown.
        public static float DistrictSoilRevealForZoom(DistrictZoomLevel level) => level switch
        {
            DistrictZoomLevel.LOD0 or DistrictZoomLevel.LOD1 => .9f,
            DistrictZoomLevel.LOD2 or DistrictZoomLevel.LOD3 => .75f,
            _ => .65f
        };

        private Texture2D DistrictGrassMapForZoom(DistrictZoomLevel level)
        {
            if (!_useDistrictZoomGrassMaps || level < DistrictZoomLevel.LOD2)
                return null;
            if (!_districtZoomGrassMapsLoaded)
            {
                const string root = "CityForgeV3/Terrain/DistrictZoomGrassV01/";
                _zoom3GrassMap = Resources.Load<Texture2D>(root + "district-grass-zoom-3");
                _zoom4GrassMap = Resources.Load<Texture2D>(root + "district-grass-zoom-4");
                _zoom5GrassMap = Resources.Load<Texture2D>(root + "district-grass-zoom-5");
                _districtZoomGrassMapsLoaded = true;
            }
            return level switch
            {
                DistrictZoomLevel.LOD2 => _zoom3GrassMap,
                DistrictZoomLevel.LOD3 => _zoom4GrassMap,
                DistrictZoomLevel.LOD4 => _zoom5GrassMap,
                DistrictZoomLevel.LOD5Billboard => _zoom5GrassMap,
                _ => null
            };
        }

        private void ApplyDistrictGrassZoomScale()
        {
            if (_terrainDistrict?.Hills?.Mountains == true) return;
            var material = _groundRenderer?.sharedMaterial;
            if (material == null) return;
            var districtMap = DistrictGrassMapForZoom(_zoomLevel);
            if (districtMap != null)
            {
                material.SetTexture("_DistrictMapTex", districtMap);
                material.SetFloat("_DistrictMapStrength", DistrictGrassMapStrengthForZoom(_zoomLevel));
                material.SetFloat("_DistrictMapMipBias", 2.5f);
                material.EnableKeyword("DISTRICT_GRASS_MAP");
            }
            else
            {
                material.DisableKeyword("DISTRICT_GRASS_MAP");
            }
            float metres = DistrictGrassWorldSizeForZoom(_zoomLevel);
            var scale = new Vector2(_widthMeters / metres, _depthMeters / metres);
            if (material.mainTextureScale != scale) material.mainTextureScale = scale;
            float distant = DistrictGrassFilteringStrengthForZoom(_zoomLevel);
            if (material.HasProperty("_DistantMeadow") && material.GetFloat("_DistantMeadow") != distant)
                material.SetFloat("_DistantMeadow", distant);
            float noise = districtMap != null || (_terrainDistrict?.Hills?.HeightMeters ?? 0)>0
                ? 0f : DistrictGrassNoiseStrengthForZoom(_zoomLevel);
            if (material.HasProperty("_FarGrassNoise") &&
                material.GetFloat("_FarGrassNoise") != noise)
                material.SetFloat("_FarGrassNoise", noise);
            float brightness = districtMap != null ? 1f : DistrictGrassBrightnessForZoom(_zoomLevel);
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
            float soil = (_terrainDistrict?.Hills?.HeightMeters ?? 0) > 0
                ? DistrictSoilRevealForZoom(_zoomLevel) : 0f;
            if (material.HasProperty("_SoilRevealStrength") &&
                material.GetFloat("_SoilRevealStrength") != soil)
                material.SetFloat("_SoilRevealStrength", soil);
        }

        private void ConfigureMountainGroundMaterial()
        {
            var material = _groundRenderer?.sharedMaterial;
            if (material == null) return;
            bool mountains = _terrainDistrict?.Hills?.Mountains == true;
            var shader = Shader.Find(mountains ? "CityForgeV3/MountainGroundSurfaceV10" : "CityForgeV3/MeadowGroundSurface");
            if (shader == null) return;
            material.shader = shader;
            // Mountain materials use a separately calibrated 5m grass source,
            // including triplanar slope sampling; retain that artwork contract.
            var grass = Resources.Load<Texture2D>(mountains ? DefaultGrassResource : DistrictGrassResource);
            if (grass != null) material.mainTexture = grass;
            float grassMetres = mountains ? GrassTextureWorldSizeMeters : DistrictGrassWorldSizeForZoom(_zoomLevel);
            material.mainTextureScale = new Vector2(_widthMeters / grassMetres, _depthMeters / grassMetres);
            if (!mountains)
            {
                material.SetFloat("_TextureWorldSize",
                    DistrictGrassTextureWorldSizeMeters);
                // The macro texture now owns broad color variation. Preserve
                // its authored palette and avoid a second dry-patch system.
                material.SetFloat("_GrassHueShift", 0f);
                bool hills=(_terrainDistrict?.Hills?.HeightMeters ?? 0)>0;
                if (hills)
                {
                    var relief = _terrainDistrict.Hills;
                    var vertical = relief.VerticalReliefScale > 0
                        ? Mathf.Clamp(relief.VerticalReliefScale, .25f, 4f) : 1f;
                    material.SetFloat("_SoilPeakHeight", Mathf.Max(1f,
                        relief.HeightMeters * DistrictElevation.RollingHillVerticalScale * vertical));
                }
                material.SetFloat("_RollingHillDarkSlopeLift",RollingHillDarkSlopeLift);
                material.SetFloat("_RollingHillDeepShadeLift",RollingHillDeepShadeLift);
                material.SetFloat("_MeadowPatchStrength", 0f);
                if (hills)
                    material.SetTexture("_SoilTex", Resources.Load<Texture2D>(
                        "CityForgeV3/Terrain/MountainBrownV01/brown-scree-v01"));
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
