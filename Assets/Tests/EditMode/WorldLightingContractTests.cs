using System.IO;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class WorldLightingContractTests
    {
        [Test]
        public void DistrictPublishesOneSharedEnvironmentForCustomShaders()
        {
            DistrictWorldController.ApplyRegionEnvironment(
                TimeOfDayPreset.Noon, null);
            var noonAmbient = Shader.GetGlobalColor("_CFWorldAmbientColor");
            var noonSun = Shader.GetGlobalColor("_CFWorldSunColor");
            var direction = Shader.GetGlobalVector("_CFWorldLightDirection");

            DistrictWorldController.ApplyRegionEnvironment(
                TimeOfDayPreset.Night, null);
            var nightAmbient = Shader.GetGlobalColor("_CFWorldAmbientColor");
            var nightSun = Shader.GetGlobalColor("_CFWorldSunColor");

            Assert.That(direction.sqrMagnitude, Is.EqualTo(1f).Within(.001f));
            Assert.That(noonAmbient.maxColorComponent,
                Is.GreaterThan(nightAmbient.maxColorComponent));
            Assert.That(noonSun.maxColorComponent,
                Is.GreaterThan(nightSun.maxColorComponent * 10f));
        }

        [Test]
        public void OrdinaryWorldShadersHaveNoPrivateTimeOfDayLightingControls()
        {
            var shaderNames = new[]
            {
                "CityForgeV3/Experimental3DBuildingPBR",
                "CityForgeV3/ShadowReceivingRoadOverlay",
                "CityForgeV3/BridgeApproachBlend",
                "CityForgeV3/RiverWaterSurface",
                "CityForgeV3/RiverBankSurface",
                "CityForgeV3/RiverBedSurface",
                "CityForgeV3/ShadowReceivingLotSurface",
                "CityForgeV3/Experimental3DGroundReceiver",
                "CityForgeV3/MeadowGroundSurface",
                "CityForgeV3/MountainGroundSurfaceV10",
                "CityForgeV3/NaturalGrassGardenPatch",
                "CityForgeV3/LitShadowReceivingSprite",
                "CityForgeV3/AlwaysVisibleBuildingProp",
                "CityForgeV3/SoftGroundDecal",
                "CityForgeV3/AutomataGarmentRecolor",
                "CityForge/Interior Automata",
                "CityForgeV3/LotSurfaceColor",
                "CityForgeV3/WorldColor",
                "CityForgeV3/DistrictSnowCover",
                "CityForge/SnowAccumulation",
                "CityForgeV3/StoneFountainWater"
            };
            foreach (var shaderName in shaderNames)
            {
                var shader = Shader.Find(shaderName);
                Assert.That(shader, Is.Not.Null, shaderName);
                Assert.That(shader.isSupported, Is.True, shaderName);
                var material = new Material(shader);
                try
                {
                    Assert.That(material.HasProperty("_TimeTint"), Is.False,
                        shaderName);
                    Assert.That(material.HasProperty("_AmbientFloor"), Is.False,
                        shaderName);
                    Assert.That(material.HasProperty("_TerrainSunDirection"),
                        Is.False, shaderName);
                    Assert.That(material.HasProperty("_EnvironmentDim"), Is.False,
                        shaderName);
                    Assert.That(material.HasProperty("_DirectionalFloorOverride"),
                        Is.False, shaderName);
                }
                finally
                {
                    Object.DestroyImmediate(material);
                }
            }
        }

        [Test]
        public void NativeBuildingBaseIsLitAndOnlyNightMaskIsEmissive()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Shaders/" +
                "Experimental3DBuildingPBR.shader"));
            StringAssert.Contains("output.Albedo = preserved", source);
            StringAssert.Contains("output.Emission = nightEmission", source);
            StringAssert.DoesNotContain("output.Albedo = fixed3(0, 0, 0)", source);
        }

        [Test]
        public void GardenArtworkUsesTheSharedLitSpriteMaterial()
        {
            var owner = new GameObject("World-lit garden fixture");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                var garden = world.CreatePropPresentation(
                    LotWorldController.FoundationOpenRosesPropId,
                    "World-lit foundation garden", 1f);
                try
                {
                    var sprites = garden.GetComponentsInChildren<SpriteRenderer>(true);
                    Assert.That(sprites, Is.Not.Empty);
                    Assert.That(sprites.All(renderer =>
                        renderer.sharedMaterial.shader.name ==
                        "CityForgeV3/LitShadowReceivingSprite"), Is.True);
                }
                finally
                {
                    if (garden != null) Object.DestroyImmediate(garden.gameObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
