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
        public string DoorTransformName = "";
        public string HostElevation = "Front";
        public float VisibleWidthMeters = 2.4f;
        public float VisibleHeightMeters = 1.9f;
        public float ProjectionDepthMeters = 0.18f;
        public float ForegroundDepthMeters = 0.35f;
        public float ModelNativeWidthMeters = 1f;
        public float ModelPitchDegrees;
        public float ModelYawDegrees;
        public float ModelRollDegrees;
        public float SwingAmplitudeDegrees;
        public float SwingPeriodSeconds;
        public float DoorOpenAngleDegrees = 72f;
        public bool NightLighting;
    }

    public static class BuildingPropCatalog
    {
        public const string AleHouseSignId = "ale-house-hanging-sign-v01";
        public const string TeaShopStorefrontId = "tea-shop-storefront-v01";

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
                    ModelYawDegrees = 186f,
                    SwingAmplitudeDegrees = 2.5f,
                    SwingPeriodSeconds = 6f
                },
                new()
                {
                    Id = TeaShopStorefrontId,
                    Revision = "v01",
                    DisplayName = "Tea & Spices Storefront",
                    Family = "Storefronts",
                    PreviewResourcePath =
                        "CityForgeV3/BuildingProps/Storefronts/TeaShopV01/catalog-preview",
                    ModelResourcePath =
                        "CityForgeV3/BuildingProps/Storefronts/TeaShopV01/Models/TeaShopStorefrontV01",
                    BaseColorResourcePath =
                        "CityForgeV3/BuildingProps/Storefronts/TeaShopV01/Textures/base-color",
                    NormalResourcePath =
                        "CityForgeV3/BuildingProps/Storefronts/TeaShopV01/Textures/normal",
                    MetallicResourcePath =
                        "CityForgeV3/BuildingProps/Storefronts/TeaShopV01/Textures/metallic",
                    HostElevation = "Front",
                    VisibleWidthMeters = 5.2f,
                    VisibleHeightMeters = 3.67f,
                    ProjectionDepthMeters = 0.12f,
                    ForegroundDepthMeters = 0.22f,
                    // The intact Tripo FBX is authored in centimetre-scale
                    // units. Its renderer is about 0.0113 Unity metres wide
                    // at import scale, so use that measured width to resolve
                    // the requested 5.2-metre storefront presentation.
                    ModelNativeWidthMeters = 0.0098f,
                    // This FBX stores its vertical artwork on local Z.
                    ModelPitchDegrees = -90f,
                    // Face the detailed, authored storefront front outward
                    // from its host facade. The imported FBX faces the
                    // opposite direction from the simple hanging sign.
                    ModelYawDegrees = 90f,
                    // Flipping both imported pitch and yaw turns the artwork
                    // within its own facade plane while keeping its detailed
                    // front pointed away from the host wall.
                    ModelRollDegrees = 0f,
                    // Keep the untouched textured mesh as the production
                    // storefront. The first automatic face-based door split
                    // visibly damaged the facade and is intentionally not
                    // exposed until a clean component separation is authored.
                    DoorTransformName = "",
                    DoorOpenAngleDegrees = 72f,
                    NightLighting = true
                }
            };

        public static BuildingPropDefinition Find(string id)
        {
            foreach (var item in Items)
                if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            return null;
        }

        public static int ResolveFacingPreset(int hostQuarterTurns,
            float propRotationDegrees)
        {
            var propPreset = UnityEngine.Mathf.RoundToInt(propRotationDegrees / 45f);
            return ((propPreset % 8) + 8) % 8;
        }

        public static void RotateWithBuilding(PlacedBuilding building, int direction)
        {
            if (building?.Attachments == null) return;
            foreach (var attachment in building.Attachments)
            {
                if (attachment == null) continue;
                var preset = UnityEngine.Mathf.RoundToInt(
                    attachment.RotationDegrees / 45f);
                attachment.RotationDegrees =
                    ((preset + direction * 2) % 8 + 8) % 8 * 45f;
            }
        }

        public static float ResolveYawDegrees(BuildingPropDefinition definition,
            int hostQuarterTurns, float propRotationDegrees)
        {
            return UnityEngine.Mathf.Repeat(definition.ModelYawDegrees +
                ResolveFacingPreset(hostQuarterTurns, propRotationDegrees) * 45f,
                360f);
        }
    }
}
