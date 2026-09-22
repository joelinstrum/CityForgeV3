using System.IO;
using System.Linq;
using System.Collections.Generic;
using CityForgeV3.Buildings3D;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class WorldLightingContractTests
    {
        [Test]
        public void ImportedMaterialsUseSharedNativeShaderAndCachedCopies()
        {
            var standard = Shader.Find("Standard");
            var shared = Shader.Find(
                "CityForgeV3/Experimental3DBuildingPBR");
            Assert.That(standard, Is.Not.Null);
            Assert.That(shared, Is.Not.Null);

            var texture = new Texture2D(2, 2);
            var source = new Material(standard)
            {
                color = new Color(.42f, .31f, .2f, 1f),
                mainTexture = texture,
                mainTextureScale = new Vector2(1.5f, .75f),
                mainTextureOffset = new Vector2(.1f, .2f)
            };
            var first = new GameObject("First imported building");
            var second = new GameObject("Second imported building");
            first.AddComponent<MeshRenderer>().sharedMaterial = source;
            second.AddComponent<MeshRenderer>().sharedMaterial = source;
            var owned = new List<Material>();
            var cache = new Dictionary<Material, Material>();

            try
            {
                ImportedBuildingMaterials.Prepare(first.transform, owned, cache);
                ImportedBuildingMaterials.Prepare(second.transform, owned, cache);

                var firstPrepared = first.GetComponent<MeshRenderer>()
                    .sharedMaterial;
                var secondPrepared = second.GetComponent<MeshRenderer>()
                    .sharedMaterial;
                Assert.That(firstPrepared, Is.SameAs(secondPrepared));
                Assert.That(firstPrepared, Is.Not.SameAs(source));
                Assert.That(owned, Has.Count.EqualTo(1));
                Assert.That(cache, Has.Count.EqualTo(1));
                Assert.That(firstPrepared.shader, Is.EqualTo(shared));
                Assert.That(firstPrepared.color, Is.EqualTo(source.color));
                Assert.That(firstPrepared.mainTexture, Is.SameAs(texture));
                Assert.That(firstPrepared.mainTextureScale,
                    Is.EqualTo(source.mainTextureScale));
                Assert.That(firstPrepared.mainTextureOffset,
                    Is.EqualTo(source.mainTextureOffset));
                Assert.That(firstPrepared.GetFloat("_GlossMapScale"),
                    Is.EqualTo(.1f).Within(.001f));
                Assert.That(firstPrepared.GetFloat("_Contrast"), Is.EqualTo(1f));
                Assert.That(firstPrepared.GetFloat("_Saturation"), Is.EqualTo(1f));
                Assert.That(firstPrepared.GetFloat("_NightEmissionIntensity"),
                    Is.Zero);
                Assert.That(firstPrepared.enableInstancing, Is.True);
                Assert.That(source.shader, Is.EqualTo(standard),
                    "Preparing runtime copies must not mutate source assets.");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                foreach (var material in owned)
                    Object.DestroyImmediate(material);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ImportedMaterialPreparationPreservesGlassAndNightEmitters()
        {
            var standard = Shader.Find("Standard");
            var glass = new Material(standard) { renderQueue = 3000 };
            glass.SetFloat("_Mode", 3f);
            var emitter = new Material(standard);
            emitter.EnableKeyword("_EMISSION");
            emitter.SetColor("_EmissionColor", Color.yellow);
            var root = new GameObject("Authored Standard surfaces");
            var glassRenderer = root.AddComponent<MeshRenderer>();
            glassRenderer.sharedMaterials = new[] { glass, emitter };
            var owned = new List<Material>();

            try
            {
                ImportedBuildingMaterials.Prepare(root.transform, owned);
                Assert.That(glassRenderer.sharedMaterials[0], Is.SameAs(glass));
                Assert.That(glassRenderer.sharedMaterials[1], Is.SameAs(emitter));
                Assert.That(owned, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(glass);
                Object.DestroyImmediate(emitter);
            }
        }

        [TestCase("CityForgeV3/Buildings3D/LumberMillV03/Prefabs/LumberMillV03")]
        [TestCase("CityForgeV3/Buildings3D/WorkTentV01/Source/" +
            "tripo_convert_d9f9e5b8-49ea-4d0c-b7fd-c97e8321421a")]
        public void CurrentIndustrialBuildingsAcceptSharedNativeMaterialContract(
            string resourcePath)
        {
            var source = Resources.Load<GameObject>(resourcePath);
            Assert.That(source, Is.Not.Null, resourcePath);
            var instance = Object.Instantiate(source);
            var owned = new List<Material>();
            var cache = new Dictionary<Material, Material>();
            try
            {
                var standardBefore = instance.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Count(material => material != null &&
                        material.shader.name == "Standard");
                Assert.That(standardBefore, Is.GreaterThan(0), resourcePath);

                ImportedBuildingMaterials.Prepare(instance.transform, owned, cache);

                var sharedAfter = instance.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Count(material => material != null && material.shader.name ==
                        "CityForgeV3/Experimental3DBuildingPBR");
                Assert.That(sharedAfter, Is.GreaterThan(0), resourcePath);
                Assert.That(owned, Has.Count.EqualTo(cache.Count), resourcePath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
                foreach (var material in owned)
                    Object.DestroyImmediate(material);
            }
        }

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
            Assert.That(Shader.GetGlobalFloat("_CFWorldWhitePoint"),
                Is.EqualTo(DistrictWorldController.WorldWhitePoint).Within(.001f));
            Assert.That(Shader.GetGlobalFloat(
                    "_CFNativeSurfaceIndirectScale"),
                Is.EqualTo(1f).Within(.001f),
                "Night must not lift ordinary native-surface albedo.");
            Assert.That(Shader.GetGlobalFloat(
                    "_CFNativeBuildingNightDimming"),
                Is.EqualTo(.55f).Within(.001f),
                "Night should darken building shells without changing emission.");
            Assert.That(Shader.GetGlobalFloat("_CFGardenSurfaceExposure"),
                Is.EqualTo(1f).Within(.001f),
                "Night must not apply the daylight garden exposure.");
        }

        [Test]
        public void EveryPresetKeepsArtworkWithinTheSharedWhitePoint()
        {
            foreach (TimeOfDayPreset preset in
                     System.Enum.GetValues(typeof(TimeOfDayPreset)))
            {
                var illumination =
                    DistrictWorldController.RegionArtworkIllumination(preset);
                Assert.That(illumination.maxColorComponent,
                    Is.LessThanOrEqualTo(
                        DistrictWorldController.WorldWhitePoint + .001f),
                    preset.ToString());
            }

            var morning = DistrictWorldController.RegionArtworkIllumination(
                TimeOfDayPreset.Morning).grayscale;
            var noon = DistrictWorldController.RegionArtworkIllumination(
                TimeOfDayPreset.Noon).grayscale;
            var afternoon = DistrictWorldController.RegionArtworkIllumination(
                TimeOfDayPreset.Afternoon).grayscale;
            var evening = DistrictWorldController.RegionArtworkIllumination(
                TimeOfDayPreset.Evening).grayscale;
            var night = DistrictWorldController.RegionArtworkIllumination(
                TimeOfDayPreset.Night).grayscale;

            Assert.That(noon, Is.GreaterThan(morning));
            Assert.That(noon, Is.GreaterThan(afternoon));
            Assert.That(Mathf.Abs(morning - afternoon), Is.GreaterThan(.005f));
            Assert.That(afternoon, Is.GreaterThan(evening));
            Assert.That(evening, Is.GreaterThan(night));
        }

        [Test]
        public void HybridArtworkUsesOneDaylightExposureAndGentlerNoonShade()
        {
            Assert.That(DistrictWorldController.HybridArtworkExposureFor(
                TimeOfDayPreset.Morning), Is.EqualTo(1.5f));
            Assert.That(DistrictWorldController.HybridArtworkExposureFor(
                TimeOfDayPreset.Noon), Is.EqualTo(1.5f));
            Assert.That(DistrictWorldController.HybridArtworkExposureFor(
                TimeOfDayPreset.Afternoon), Is.EqualTo(1.5f));
            Assert.That(DistrictWorldController.HybridArtworkExposureFor(
                TimeOfDayPreset.Evening), Is.EqualTo(1f));
            Assert.That(DistrictWorldController.HybridArtworkExposureFor(
                TimeOfDayPreset.Night), Is.EqualTo(1f));
            Assert.That(HybridBuildingPresentation.DirectionalShadeOpacityFor(
                TimeOfDayPreset.Noon), Is.EqualTo(.24f));

            var source = File.ReadAllText(Path.Combine(Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Shaders/" +
                "AlwaysVisibleBuildingSprite.shader"));
            StringAssert.Contains("_HybridBaseLayer", source);
            StringAssert.Contains("CalibrateHybridBase", source);
            StringAssert.Contains("shoulderStart", source);
        }

        [Test]
        public void DistrictNativeSurfacesUseOneNonEmissiveDaylightLift()
        {
            foreach (var preset in new[]
                     {
                         TimeOfDayPreset.Morning,
                         TimeOfDayPreset.Noon,
                         TimeOfDayPreset.Afternoon
                     })
                Assert.That(DistrictWorldController
                        .NativeSurfaceIndirectScaleFor(preset),
                    Is.EqualTo(2.5f), preset.ToString());
            Assert.That(DistrictWorldController.NativeSurfaceIndirectScaleFor(
                TimeOfDayPreset.Evening), Is.EqualTo(1f));
            Assert.That(DistrictWorldController.NativeSurfaceIndirectScaleFor(
                TimeOfDayPreset.Night), Is.EqualTo(1f));
            Assert.That(DistrictWorldController.NativeBuildingNightResponseFor(
                TimeOfDayPreset.Morning), Is.EqualTo(1f));
            Assert.That(DistrictWorldController.NativeBuildingNightResponseFor(
                TimeOfDayPreset.Noon), Is.EqualTo(1f));
            Assert.That(DistrictWorldController.NativeBuildingNightResponseFor(
                TimeOfDayPreset.Afternoon), Is.EqualTo(1f));
            Assert.That(DistrictWorldController.NativeBuildingNightResponseFor(
                TimeOfDayPreset.Evening), Is.EqualTo(.8f));
            Assert.That(DistrictWorldController.NativeBuildingNightResponseFor(
                TimeOfDayPreset.Night), Is.EqualTo(.45f));
            foreach (var preset in new[]
                     {
                         TimeOfDayPreset.Morning,
                         TimeOfDayPreset.Noon,
                         TimeOfDayPreset.Afternoon
                     })
                Assert.That(DistrictWorldController.GardenSurfaceExposureFor(
                    preset), Is.EqualTo(1.3f).Within(.001f),
                    preset.ToString());
            Assert.That(DistrictWorldController.GardenSurfaceExposureFor(
                TimeOfDayPreset.Evening), Is.EqualTo(1f).Within(.001f));
            Assert.That(DistrictWorldController.GardenSurfaceExposureFor(
                TimeOfDayPreset.Night), Is.EqualTo(1f).Within(.001f));

            DistrictWorldController.ApplyRegionEnvironment(
                TimeOfDayPreset.Noon, null);
            Assert.That(Shader.GetGlobalFloat(
                    "_CFNativeSurfaceIndirectScale"),
                Is.EqualTo(2.5f).Within(.001f));
            Assert.That(Shader.GetGlobalFloat("_CFGardenSurfaceExposure"),
                Is.EqualTo(1.3f).Within(.001f));

            var source = File.ReadAllText(Path.Combine(Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Shaders/" +
                "Experimental3DBuildingPBR.shader"));
            StringAssert.Contains("LightingStandardBuilding_GI", source);
            StringAssert.Contains("lighting.indirect.diffuse *=", source);
            StringAssert.Contains("max(1.0h,", source);
            StringAssert.Contains(
                "output.Albedo = preserved * (1.0h - _CFNativeBuildingNightDimming)",
                source);
            StringAssert.Contains("output.Emission = nightEmission", source);
            StringAssert.DoesNotContain(
                "output.Emission = lighting.indirect.diffuse", source);

            var gardenSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Shaders/GardenPropPBR.shader"));
            StringAssert.Contains("LightingStandardGarden_GI", gardenSource);
            StringAssert.Contains("_CFNativeSurfaceIndirectScale", gardenSource);
            StringAssert.Contains("_CFGardenSurfaceExposure", gardenSource);
            StringAssert.Contains("_CFWorldWhitePoint", gardenSource);
            StringAssert.Contains("output.Emission = 0", gardenSource);
        }

        [Test]
        public void SharedArtworkLightingUsesHuePreservingWhitePointBound()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath,
                "CityForgeV3/Resources/CityForgeV3/Shaders/" +
                "CityForgeWorldLighting.cginc"));
            StringAssert.Contains("CityForgeBoundWorldIllumination", source);
            StringAssert.Contains("_CFWorldWhitePoint", source);
            StringAssert.DoesNotContain("saturate(illumination)", source);
        }

        [Test]
        public void NoonArtworkLightingRetainsTextureHighlightHeadroom()
        {
            DistrictWorldController.ApplyRegionEnvironment(
                TimeOfDayPreset.Noon, null);
            var ambient = Shader.GetGlobalColor("_CFWorldAmbientColor");
            var sun = Shader.GetGlobalColor("_CFWorldSunColor");
            var fullLight = ambient + sun;

            Assert.That(fullLight.r, Is.LessThanOrEqualTo(1f));
            Assert.That(fullLight.g, Is.LessThanOrEqualTo(1f));
            Assert.That(fullLight.b, Is.LessThanOrEqualTo(1f));
            Assert.That(fullLight.maxColorComponent, Is.GreaterThan(.9f),
                "Noon should remain bright without clipping source artwork.");
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
            StringAssert.Contains(
                "output.Albedo = preserved * (1.0h - _CFNativeBuildingNightDimming)",
                source);
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
