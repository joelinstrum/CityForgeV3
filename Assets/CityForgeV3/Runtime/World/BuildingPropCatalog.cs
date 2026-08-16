using System;
using System.Collections.Generic;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class BuildingPropDefinition
    {
        public string Id = "";
        public string Revision = "";
        public string DisplayName = "";
        public string Family = "";
        public string PreviewResourcePath = "";
        public string ModelResourcePath = "";
        public string BaseColorResourcePath = "";
        public string NormalResourcePath = "";
        public string MetallicResourcePath = "";
        public string SwingTransformName = "";
        public string HostElevation = "Front";
        public float VisibleWidthMeters = 2.4f;
        public float VisibleHeightMeters = 1.9f;
        public float ProjectionDepthMeters = 0.18f;
        public float ForegroundDepthMeters = 0.35f;
        public float ModelNativeWidthMeters = 1f;
        public float ModelYawDegrees;
        public float SwingAmplitudeDegrees;
        public float SwingPeriodSeconds;
    }

    public static class BuildingPropCatalog
    {
        public const string AleHouseSignId = "ale-house-hanging-sign-v01";

        public static readonly IReadOnlyList<BuildingPropDefinition> Items =
            new List<BuildingPropDefinition>
            {
                new()
                {
                    Id = AleHouseSignId,
                    Revision = "v01",
                    DisplayName = "Ale House Hanging Sign",
                    Family = "Wooden Signs",
                    PreviewResourcePath =
                        "CityForgeV3/BuildingProps/WoodenSignsV01/ale-house-preview",
                    ModelResourcePath =
                        "CityForgeV3/BuildingProps/WoodenSignsV01/Models/ale-house-animated-v01",
                    BaseColorResourcePath =
                        "CityForgeV3/BuildingProps/WoodenSignsV01/Textures/pub_sign_3d_model_basecolor",
                    NormalResourcePath =
                        "CityForgeV3/BuildingProps/WoodenSignsV01/Textures/pub_sign_3d_model_normal",
                    MetallicResourcePath =
                        "CityForgeV3/BuildingProps/WoodenSignsV01/Textures/pub_sign_3d_model_metallic",
                    SwingTransformName = "CF_SIGN_SWING",
                    HostElevation = "Front",
                    VisibleWidthMeters = 2.4f,
                    VisibleHeightMeters = 1.9f,
                    ProjectionDepthMeters = 0.18f,
                    ForegroundDepthMeters = 0.35f,
                    ModelNativeWidthMeters = 0.9823304f,
                    ModelYawDegrees = 210f,
                    SwingAmplitudeDegrees = 2.5f,
                    SwingPeriodSeconds = 6f
                }
            };

        public static BuildingPropDefinition Find(string id)
        {
            foreach (var item in Items)
                if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            return null;
        }
    }
}
