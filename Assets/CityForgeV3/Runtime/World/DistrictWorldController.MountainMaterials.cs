using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        private void ConfigureMountainGroundMaterial()
        {
            var material = _groundRenderer?.sharedMaterial;
            if (material == null) return;
            bool mountains = _terrainDistrict?.Hills?.Mountains == true;
            var shader = Shader.Find(mountains ? "CityForgeV3/MountainGroundSurfaceV10" : "CityForgeV3/ShadowReceivingLotSurface");
            if (shader == null) return;
            material.shader = shader;
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
