using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        // Match the screen-space detail of the next existing camera band.
        // Farther views keep their original world-space texture scale.
        public static float DistrictGrassWorldSizeForZoom(DistrictZoomLevel level)
        {
            if (level != DistrictZoomLevel.LOD0 && level != DistrictZoomLevel.LOD1)
                return DistrictGrassTextureWorldSizeMeters;
            var next = (DistrictZoomLevel)((int)level + 1);
            return DistrictGrassTextureWorldSizeMeters *
                OrthographicSize(level, 1, 1, 1) / OrthographicSize(next, 1, 1, 1);
        }

        private void ApplyDistrictGrassZoomScale()
        {
            if (_terrainDistrict?.Hills?.Mountains == true) return;
            var material = _groundRenderer?.sharedMaterial;
            if (material == null) return;
            float metres = DistrictGrassWorldSizeForZoom(_zoomLevel);
            var scale = new Vector2(_widthMeters / metres, _depthMeters / metres);
            if (material.mainTextureScale != scale) material.mainTextureScale = scale;
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
            if (!mountains) return;
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
