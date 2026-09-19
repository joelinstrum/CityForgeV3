using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        // User-facing stops count from one: first inherits the old second
        // stop's texture density; second inherits the old third stop's density.
        public static float DistrictGrassWorldSizeForZoom(DistrictZoomLevel level)
        {
            if (level != DistrictZoomLevel.LOD0)
                return DistrictGrassTextureWorldSizeMeters;
            return DistrictGrassTextureWorldSizeMeters *
                OrthographicSize(DistrictZoomLevel.LOD1, 1, 1, 1) /
                OrthographicSize(DistrictZoomLevel.LOD2, 1, 1, 1);
        }

        public static bool DistrictGrassUsesSmoothFiltering(DistrictZoomLevel level) =>
            level >= DistrictZoomLevel.LOD2;

        private void ApplyDistrictGrassZoomScale()
        {
            if (_terrainDistrict?.Hills?.Mountains == true) return;
            var material = _groundRenderer?.sharedMaterial;
            if (material == null) return;
            float metres = DistrictGrassWorldSizeForZoom(_zoomLevel);
            var scale = new Vector2(_widthMeters / metres, _depthMeters / metres);
            if (material.mainTextureScale != scale) material.mainTextureScale = scale;
            float distant = DistrictGrassUsesSmoothFiltering(_zoomLevel) ? 1f : 0f;
            if (material.HasProperty("_DistantMeadow") && material.GetFloat("_DistantMeadow") != distant)
                material.SetFloat("_DistantMeadow", distant);
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
                material.SetFloat("_GrassHueShift", .035f); // Reversible forest-green colour study; original artwork unchanged.
                var hillGrass=Resources.Load<Texture2D>("CityForgeV3/Terrain/HillsV01/crest-meadow-4x4");
                bool hills=(_terrainDistrict?.Hills?.HeightMeters ?? 0)>0 && hillGrass!=null;
                material.SetTexture("_HillTex",hillGrass);
                material.SetFloat("_MeadowPatchStrength", 1f);
                if (hillGrass != null) material.EnableKeyword("MEADOW_PATCHES");
                else material.DisableKeyword("MEADOW_PATCHES");
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
    }
}
