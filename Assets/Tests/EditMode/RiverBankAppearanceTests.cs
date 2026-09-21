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
                         "BanksV5Wide/submerged-gravel-wide-muted" })
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
                    "shoreline-wide-muted"));
            Assert.That(major.OpenGravelTextureResource,
                Is.EqualTo(RiverBankAppearance.WideOpenGravelResource));
            Assert.That(major.SubmergedGravelTextureResource,
                Is.EqualTo(RiverBankAppearance.WideSubmergedGravelResource));
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
        public void MajorRiverMaterialBindsAllThreeMutedWideTextures()
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
                    "shoreline-wide-muted")));
                Assert.That(material.GetTexture("_EarthTex"),
                    Is.SameAs(Resources.Load<Texture2D>(
                        RiverBankAppearance.WideOpenGravelResource)));
                Assert.That(material.GetTexture("_GravelTex"),
                    Is.SameAs(Resources.Load<Texture2D>(
                        RiverBankAppearance.WideSubmergedGravelResource)));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
