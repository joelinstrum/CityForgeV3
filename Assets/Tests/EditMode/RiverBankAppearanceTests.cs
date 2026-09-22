using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class RiverBankAppearanceTests
    {
        [Test]
        public void InsertingCollinearSamplesDoesNotChangeBendAtExistingPoints()
        {
            var coarse = new[] { new Vector2(-100, 0), Vector2.zero, new Vector2(80, 60) };
            var dense = new List<Vector2>();
            for (int segment = 0; segment < 2; segment++)
                for (int i = 0; i < 20; i++) dense.Add(Vector2.Lerp(coarse[segment], coarse[segment + 1], i / 20f));
            dense.Add(coarse[2]);
            var a = new RiverBankAppearance(coarse, 64);
            var b = new RiverBankAppearance(dense, 64);
            Assert.That(a.Bend[1], Is.GreaterThan(.1f));
            Assert.That(b.Bend[20], Is.EqualTo(a.Bend[1]).Within(.0001f));
        }

        [Test]
        public void ReversingPointOrderSwapsBankSidesWithoutChangingPhysicalBend()
        {
            var points = new List<Vector2> { new(-100, 0), new(0, 0), new(80, 60), new(120, 140) };
            var forward = new RiverBankAppearance(points, 64);
            points.Reverse();
            var reversed = new RiverBankAppearance(points, 64);
            for (int i = 0; i < points.Count; i++)
                Assert.That(reversed.Bend[^(i + 1)], Is.EqualTo(-forward.Bend[i]).Within(.0001f));
        }

        [Test]
        public void StraightAndDegeneratePathsStayNeutralAndFinite()
        {
            foreach (var points in new[] {
                new[] { Vector2.zero },
                new[] { Vector2.zero, Vector2.zero, Vector2.zero },
                new[] { Vector2.zero, Vector2.zero, Vector2.right * 50, Vector2.right * 100 } })
                foreach (float bend in new RiverBankAppearance(points, 128).Bend)
                    Assert.That(bend, Is.EqualTo(0f));
        }

        [Test]
        public void MirroringBendSwapsGravelAndEarthAndBoundsHairpins()
        {
            var points = new[] { new Vector2(-100, 0), Vector2.zero, new Vector2(-90, 1) };
            var a = new RiverBankAppearance(points, 200);
            for (int i = 0; i < points.Length; i++) points[i].y *= -1;
            var b = new RiverBankAppearance(points, 200);
            Assert.That(a.Bend[1], Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
            Assert.That(b.Bend[1], Is.EqualTo(-a.Bend[1]).Within(.0001f));
        }

        [Test]
        public void ClippingAndJunctionSubtractionRetainInterpolatedSignedBankWeights()
        {
            var mesh = new Mesh();
            try
            {
                mesh.vertices = new[] { new Vector3(-2, 0, -2), new Vector3(2, 0, -2), new Vector3(2, 0, 2), new Vector3(-2, 0, 2) };
                mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                mesh.uv2 = new[] { new Vector2(-1, -2), new Vector2(1, -2), new Vector2(1, 2), new Vector2(-1, 2) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                RiverMeshUnion.ClipToRect(mesh, new Rect(-1, -1, 2, 2));
                RiverMeshUnion.Subtract(mesh, new() { new(new(-.25f, -.25f), new(.25f, -.25f), new(.25f, .25f), new(-.25f, .25f)) }, null);
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    Assert.That(mesh.uv2[i].x, Is.EqualTo(mesh.vertices[i].x * .5f).Within(.00001f));
                    Assert.That(mesh.uv2[i].y, Is.EqualTo(mesh.vertices[i].z).Within(.00001f));
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void JunctionSubtractionFadesTributaryAlphaTowardOwner()
        {
            var mesh = new Mesh();
            try
            {
                mesh.vertices = new[]
                {
                    new Vector3(-2, 0, -1), new Vector3(2, 0, -1),
                    new Vector3(2, 0, 1), new Vector3(-2, 0, 1)
                };
                mesh.uv = new[] { Vector2.zero, Vector2.right,
                    Vector2.one, Vector2.up };
                mesh.uv2 = new[] { Vector2.right, Vector2.right,
                    Vector2.right, Vector2.right };
                mesh.colors = new[] { Color.white, Color.white,
                    Color.white, Color.white };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                RiverMeshUnion.Subtract(mesh, new()
                {
                    new(new(-.25f, -2), new(.25f, -2),
                        new(.25f, 2), new(-.25f, 2))
                }, null, 1f);

                Assert.That(mesh.colors.Any(color => color.a < .001f),
                    Is.True, "The clipped confluence edge must be transparent.");
                Assert.That(mesh.colors.Any(color => color.a > .999f),
                    Is.True, "Water beyond the fade must remain opaque.");
                Assert.That(mesh.colors.All(color => color.r > .999f),
                    Is.True, "Junction alpha must not overwrite depth red.");
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void HillMeadowAssetsSupportContinuousTerrainBlending()
        {
            var texture=Resources.Load<Texture2D>("CityForgeV3/Terrain/HillsV01/crest-meadow-4x4");
            Assert.That(texture,Is.Not.Null);
            Assert.That(texture.wrapMode,Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(texture.mipmapCount,Is.GreaterThan(1));
            var shader=Shader.Find("CityForgeV3/MeadowGroundSurface");
            Assert.That(UnityEditor.ShaderUtil.ShaderHasError(shader),Is.False);
            var material=new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_HillTex"),Is.True);
                Assert.That(material.HasProperty("_HillHeight"),Is.True);
            }
            finally{Object.DestroyImmediate(material);}
        }

        [Test]
        public void BankMaterialsLoadWithHorizontalRepeatAndVerticalClamp()
        {
            foreach (var name in new[] { "BanksV1/grass-pebbles",
                         "BanksV1/inside-gravel", "BanksV1/outside-earth",
                         "BanksV2/shoreline", "BanksV2/shoreline-gravel",
                         "BanksV3/open-gravel", "BanksV4/shoreline-light",
                         "BanksV4/open-gravel-light",
                         "BanksV4/submerged-gravel-light",
                         "BanksV5Wide/shoreline-wide-muted",
                         "BanksV5Wide/open-gravel-wide-muted",
                         "BanksV5Wide/submerged-gravel-wide-muted",
                         "BanksV6Varied/bank-01-neutral",
                         "BanksV6Varied/bank-02-bars",
                         "BanksV6Varied/bank-03-open",
                         "BanksV6Varied/bank-04-cobbles",
                         "BanksV6Varied/submerged-neutral" })
            {
                var texture = Resources.Load<Texture2D>("CityForgeV3/Water/River/" + name);
                Assert.That(texture, Is.Not.Null, name);
                Assert.That(texture.wrapModeU, Is.EqualTo(TextureWrapMode.Repeat));
                Assert.That(texture.wrapModeV, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(texture.mipmapCount, Is.GreaterThan(1));
            }
            var shader = Shader.Find("CityForgeV3/RiverBankSurface");
            Assert.That(shader, Is.Not.Null);
            Assert.That(UnityEditor.ShaderUtil.ShaderHasError(shader), Is.False);
        }

        [Test]
        public void WideBankSelectionPreservesV04ThroughMediumWidths()
        {
            var medium = new RiverBankAppearance(
                new[] { Vector2.zero, Vector2.right }, 76f);
            var major = new RiverBankAppearance(
                new[] { Vector2.zero, Vector2.right }, 144f);

            Assert.That(RiverBankAppearance.UsesWideRiverBank(76f), Is.False);
            Assert.That(medium.ShorelineResource,
                Is.EqualTo(RiverBankAppearance.ShorelineResourceRoot +
                    "shoreline-light"));
            Assert.That(RiverBankAppearance.UsesWideRiverBank(144f), Is.True);
            Assert.That(major.ShorelineResource,
                Is.EqualTo(RiverBankAppearance.WideShorelineResourceRoot +
                    "bank-01-neutral"));
            Assert.That(major.OpenGravelTextureResource,
                Is.EqualTo(RiverBankAppearance.WideOpenGravelResource));
            Assert.That(major.SubmergedGravelTextureResource,
                Is.EqualTo(RiverBankAppearance.WideSubmergedGravelResource));
            Assert.That(major.BankVariantThreeTextureResource,
                Is.EqualTo(RiverBankAppearance.WideBankVariantThreeResource));
            Assert.That(major.BankVariantFourTextureResource,
                Is.EqualTo(RiverBankAppearance.WideBankVariantFourResource));
            Assert.That(major.BankVariantCount, Is.EqualTo(4));
            Assert.That(medium.BankVariantCount, Is.EqualTo(2));
            Assert.That(medium.OuterBlendMeters, Is.Zero);
            Assert.That(medium.OuterFadeEnd,
                Is.EqualTo(RiverBankAppearance.DefaultOuterFadeEnd));
            Assert.That(major.OuterBlendMeters,
                Is.EqualTo(RiverBankAppearance.WideOuterBlendMeters));
            Assert.That(major.OuterFadeEnd,
                Is.EqualTo(RiverBankAppearance.DefaultOuterFadeEnd +
                    RiverBankAppearance.WideOuterBlendMeters / 16f)
                    .Within(.00001f));
            Assert.That(medium.OuterFadeNoise, Is.Zero);
            Assert.That(medium.TerrainBlendStrength, Is.Zero);
            Assert.That(major.OuterFadeNoise,
                Is.EqualTo(RiverBankAppearance.WideOuterFadeNoise));
            Assert.That(major.TerrainBlendStrength,
                Is.EqualTo(RiverBankAppearance.WideTerrainBlendStrength));
            Assert.That(medium.SubmergedBedBrightness,
                Is.EqualTo(RiverBankAppearance.DefaultSubmergedBedBrightness));
            Assert.That(medium.SubmergedBlendStart,
                Is.EqualTo(RiverBankAppearance.DefaultSubmergedBlendStart));
            Assert.That(medium.SubmergedBlendEnd,
                Is.EqualTo(RiverBankAppearance.DefaultSubmergedBlendEnd));
            Assert.That(major.SubmergedBedBrightness,
                Is.EqualTo(RiverBankAppearance.WideSubmergedBedBrightness));
            Assert.That(major.SubmergedBlendStart,
                Is.EqualTo(RiverBankAppearance.WideSubmergedBlendStart));
            Assert.That(major.SubmergedBlendEnd,
                Is.EqualTo(RiverBankAppearance.WideSubmergedBlendEnd));
        }

        [Test]
        public void LightV04BanksAreTheActiveRuntimeResources()
        {
            Assert.That(RiverBankAppearance.ShorelineResourceRoot,
                Does.EndWith("/BanksV4/"));
            Assert.That(Resources.Load<Texture2D>(
                RiverBankAppearance.ShorelineResourceRoot +
                "shoreline-light"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                RiverBankAppearance.OpenGravelResource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                RiverBankAppearance.SubmergedGravelResource), Is.Not.Null);
        }

        [Test]
        public void RiverBankMaterialBindsAllThreeLightV04Textures()
        {
            var district = new RegionCityTile { Width = 4, Height = 4 };
            district.Rivers.Add(new PlacedDistrictRiver
            {
                InstanceId = "light-v04-bank-test",
                Direction = DistrictRiverDirection.WestToEast,
                Depth = DistrictRiverDepth.Deep,
                WidthMeters = 46f,
                Points = new List<DistrictRiverPoint>
                {
                    new(0f, .5f), new(1f, .5f)
                }
            });
            var host = new GameObject("Light V04 bank material test");
            try
            {
                var world = host.AddComponent<DistrictWorldController>();
                world.RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);
                var material = host.GetComponentsInChildren<MeshRenderer>()
                    .Select(renderer => renderer.sharedMaterial)
                    .First(candidate => candidate.shader.name ==
                        "CityForgeV3/RiverBankSurface");

                Assert.That(material.mainTexture, Is.SameAs(Resources.Load<Texture2D>(
                    RiverBankAppearance.ShorelineResourceRoot +
                    "shoreline-light")));
                Assert.That(material.GetTexture("_EarthTex"),
                    Is.SameAs(Resources.Load<Texture2D>(
                        RiverBankAppearance.OpenGravelResource)));
                Assert.That(material.GetTexture("_GravelTex"),
                    Is.SameAs(Resources.Load<Texture2D>(
                        RiverBankAppearance.SubmergedGravelResource)));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MajorRiverMaterialBindsFourVariedBanksAndNeutralBed()
        {
            var district = new RegionCityTile { Width = 4, Height = 4 };
            district.Rivers.Add(new PlacedDistrictRiver
            {
                InstanceId = "muted-wide-bank-test",
                Direction = DistrictRiverDirection.WestToEast,
                Depth = DistrictRiverDepth.Deep,
                WidthMeters = 144f,
                Points = new List<DistrictRiverPoint>
                {
                    new(0f, .5f), new(1f, .5f)
                }
            });
            var host = new GameObject("Muted wide bank material test");
            try
            {
                var world = host.AddComponent<DistrictWorldController>();
                world.RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);
                var material = host.GetComponentsInChildren<MeshRenderer>()
                    .Select(renderer => renderer.sharedMaterial)
                    .First(candidate => candidate.shader.name ==
                        "CityForgeV3/RiverBankSurface");

                Assert.That(material.mainTexture, Is.SameAs(Resources.Load<Texture2D>(
                    RiverBankAppearance.WideShorelineResourceRoot +
                    "bank-01-neutral")));
                Assert.That(material.GetTexture("_EarthTex"),
                    Is.SameAs(Resources.Load<Texture2D>(
                        RiverBankAppearance.WideOpenGravelResource)));
                Assert.That(material.GetTexture("_GravelTex"),
                    Is.SameAs(Resources.Load<Texture2D>(
                        RiverBankAppearance.WideSubmergedGravelResource)));
                Assert.That(material.GetTexture("_BankTex2"),
                    Is.SameAs(Resources.Load<Texture2D>(
                        RiverBankAppearance.WideBankVariantThreeResource)));
                Assert.That(material.GetTexture("_BankTex3"),
                    Is.SameAs(Resources.Load<Texture2D>(
                        RiverBankAppearance.WideBankVariantFourResource)));
                Assert.That(material.GetFloat("_BankVariantCount"),
                    Is.EqualTo(4f));
                Assert.That(material.GetFloat("_BankPatternOffset"),
                    Is.GreaterThanOrEqualTo(0f));
                Assert.That(material.GetFloat("_OuterFadeEnd"),
                    Is.EqualTo(RiverBankAppearance.DefaultOuterFadeEnd +
                        RiverBankAppearance.WideOuterBlendMeters / 16f)
                        .Within(.00001f));
                Assert.That(material.GetFloat("_OuterFadeNoise"),
                    Is.EqualTo(RiverBankAppearance.WideOuterFadeNoise));
                Assert.That(material.GetFloat("_TerrainBlendStrength"),
                    Is.EqualTo(RiverBankAppearance.WideTerrainBlendStrength));
                Assert.That(material.GetFloat("_SubmergedBedBrightness"),
                    Is.EqualTo(RiverBankAppearance.WideSubmergedBedBrightness));
                Assert.That(material.GetFloat("_SubmergedBlendStart"),
                    Is.EqualTo(RiverBankAppearance.WideSubmergedBlendStart));
                Assert.That(material.GetFloat("_SubmergedBlendEnd"),
                    Is.EqualTo(RiverBankAppearance.WideSubmergedBlendEnd));
                Assert.That(material.GetFloat("_TerrainWorldSize"),
                    Is.EqualTo(DistrictWorldController.
                        DistrictGrassTextureWorldSizeMeters));
                Assert.That(material.GetTexture("_TerrainTex"),
                    Is.SameAs(Resources.Load<Texture2D>(
                        DistrictWorldController.DistrictGrassResource)));

                var waterRenderer = host.GetComponentsInChildren<MeshRenderer>()
                    .First(renderer => renderer.sharedMaterial.shader.name ==
                        "CityForgeV3/RiverWaterSurface");
                var waterMaterial = waterRenderer.sharedMaterial;
                Assert.That(waterMaterial.GetFloat("_WaterVisible"),
                    Is.EqualTo(1f));
                Assert.That(waterMaterial.GetFloat("_EdgeOpacity"),
                    Is.EqualTo(RiverBankAppearance.WideWaterEdgeOpacity));
                Assert.That(waterMaterial.GetFloat("_DeepWaterStart"),
                    Is.EqualTo(RiverBankAppearance.WideDeepWaterStart));
                Assert.That(waterMaterial.GetFloat("_DepthBlendSoftness"),
                    Is.EqualTo(RiverBankAppearance.WideDepthBlendSoftness));
                Assert.That(waterMaterial.GetFloat("_SubmergedOpacity"),
                    Is.EqualTo(RiverBankAppearance.WideSubmergedWaterOpacity));
                Assert.That(waterMaterial.GetFloat("_EdgeFeatherMeters"),
                    Is.EqualTo(RiverBankAppearance.WideWaterEdgeFeatherMeters));
                Assert.That(waterMaterial.GetFloat("_WaterHalfWidth"),
                    Is.GreaterThan(1f));
                Assert.That(RiverBankAppearance.VisualWaterHalfWidth(
                        60f, 78f, 144f),
                    Is.EqualTo(72f));
                Assert.That(RiverBankAppearance.VisualWaterHalfWidth(
                        30f, 40f, 76f),
                    Is.EqualTo(30f));

                var grassEdge = host.GetComponentsInChildren<MeshFilter>()
                    .First(filter => filter.name.Contains("Grass Edge"));
                var outerDistance = grassEdge.sharedMesh.vertices
                    .Max(vertex => Mathf.Abs(vertex.z));
                var expectedBankEdge = 144f * 1.08f * .5f;
                Assert.That(outerDistance,
                    Is.EqualTo(expectedBankEdge +
                        RiverBankAppearance.WideOuterBlendMeters)
                    .Within(.01f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
