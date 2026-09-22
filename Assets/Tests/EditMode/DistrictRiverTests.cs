using CityForgeV3.World;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class DistrictRiverTests
    {
        [TestCase(DistrictRiverDirection.SouthToNorth, 0f, 1f)]
        [TestCase(DistrictRiverDirection.NorthToSouth, 1f, 0f)]
        public void VerticalRiverCrossesOppositeDistrictEdges(
            DistrictRiverDirection direction, float start, float end)
        {
            var result = DistrictRiverGenerator.Generate(
                new RegionCityTile { Width = 4, Height = 4 }, direction,
                0.5f, DistrictRiverDepth.Shallow, 1785);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.River.Points[0].Z, Is.EqualTo(start).Within(0.001f));
            Assert.That(result.River.Points[^1].Z, Is.EqualTo(end).Within(0.001f));
            Assert.That(result.River.Points.Count, Is.GreaterThan(4));
            Assert.That(LateralRange(result.River, true), Is.GreaterThan(0f));
            Assert.That(result.River.Curvature, Is.EqualTo(0f));
            AssertRoundedStairs(result.River);
        }

        [TestCase(DistrictRiverDirection.WestToEast, 0f, 1f)]
        [TestCase(DistrictRiverDirection.EastToWest, 1f, 0f)]
        public void HorizontalRiverCrossesOppositeDistrictEdges(
            DistrictRiverDirection direction, float start, float end)
        {
            var result = DistrictRiverGenerator.Generate(
                new RegionCityTile { Width = 4, Height = 4 }, direction,
                0f, DistrictRiverDepth.Deep, 1785);

            Assert.That(result.River.Points[0].X,
                Is.EqualTo(start).Within(0.001f));
            Assert.That(result.River.Points[^1].X,
                Is.EqualTo(end).Within(0.001f));
            Assert.That(result.River.Points.Count, Is.GreaterThan(4));
            Assert.That(LateralRange(result.River, false), Is.GreaterThan(0f));
            Assert.That(result.River.Curvature, Is.EqualTo(0f));
            AssertRoundedStairs(result.River);
        }

        [Test]
        public void RiverDataSurvivesRegionSerialization()
        {
            var district = new RegionCityTile { Width = 4, Height = 4 };
            district.Rivers.Add(DistrictRiverGenerator.Generate(district,
                DistrictRiverDirection.EastToWest, 0.73f,
                DistrictRiverDepth.Deep, 42).River);

            var restored = JsonUtility.FromJson<RegionCityTile>(
                JsonUtility.ToJson(district));

            Assert.That(restored.Rivers, Has.Count.EqualTo(1));
            Assert.That(restored.Rivers[0].Depth, Is.EqualTo(DistrictRiverDepth.Deep));
            Assert.That(restored.Rivers[0].Direction,
                Is.EqualTo(DistrictRiverDirection.EastToWest));
            Assert.That(restored.Rivers[0].Points.Count,
                Is.EqualTo(district.Rivers[0].Points.Count));
            Assert.That(restored.Rivers[0].Points.Count, Is.GreaterThan(4));
        }

        [Test]
        public void StairRunsUseSeededLongAndShortGridSpans()
        {
            var river = DistrictRiverGenerator.Generate(
                new RegionCityTile { Width = 20, Height = 12 },
                DistrictRiverDirection.WestToEast, .5f,
                DistrictRiverDepth.Deep, 1785).River;
            var runs = river.Points.Zip(river.Points.Skip(1), (a, b) =>
                    Mathf.Abs(a.Z - b.Z) < .000001f
                        ? Mathf.RoundToInt(Mathf.Abs(a.X - b.X) * 20f) : 0)
                .Where(length => length > 0).ToArray();

            Assert.That(runs.Length, Is.GreaterThan(2));
            Assert.That(runs.Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void DistrictFloraSurvivesRegionSerialization()
        {
            var district = new RegionCityTile { Width = 4, Height = 4 };
            district.Flora.Add(new PlacedDistrictFlora
            {
                InstanceId = "tree-1",
                GroupId = "grove-1",
                FloraId = "silver-maple-a",
                NormalizedX = .32f,
                NormalizedZ = .71f,
                Scale = 1.08f
            });

            var restored = JsonUtility.FromJson<RegionCityTile>(
                JsonUtility.ToJson(district));

            Assert.That(restored.Flora, Has.Count.EqualTo(1));
            Assert.That(restored.Flora[0].FloraId,
                Is.EqualTo("silver-maple-a"));
            Assert.That(restored.Flora[0].GroupId, Is.EqualTo("grove-1"));
            Assert.That(restored.Flora[0].NormalizedX,
                Is.EqualTo(.32f).Within(.001f));
            Assert.That(restored.Flora[0].Scale,
                Is.EqualTo(1.08f).Within(.001f));
        }

        [Test]
        public void ShallowRiverIsBroaderThanDeepRiver()
        {
            var district = new RegionCityTile { Width = 4, Height = 4 };
            var shallow = DistrictRiverGenerator.Generate(district,
                DistrictRiverDirection.WestToEast, 0.5f,
                DistrictRiverDepth.Shallow, 7).River;
            var deep = DistrictRiverGenerator.Generate(district,
                DistrictRiverDirection.WestToEast, 0.5f,
                DistrictRiverDepth.Deep, 7).River;

            Assert.That(shallow.WidthMeters, Is.GreaterThan(deep.WidthMeters));
        }

        [Test]
        public void WaterSelectionFindsAndMovesOnlyTheRiver()
        {
            var district = new RegionCityTile { Width = 4, Height = 4 };
            var river = new PlacedDistrictRiver
            {
                InstanceId = "editable-river",
                Direction = DistrictRiverDirection.WestToEast,
                WidthMeters = 46f,
                Points = new List<DistrictRiverPoint>
                {
                    new(0f, .4f),
                    new(.5f, .45f),
                    new(1f, .4f)
                }
            };
            district.Rivers.Add(river);

            Assert.That(DistrictRiverEditing.FindAt(district,
                new Vector2(.5f, .45f)), Is.SameAs(river));
            Assert.That(DistrictRiverEditing.FindAt(district,
                new Vector2(.5f, .9f)), Is.Null);
            Assert.That(DistrictRiverEditing.Move(river,
                new Vector2(0f, .1f)), Is.True);
            Assert.That(river.Points[1].Z, Is.EqualTo(.55f).Within(.001f));
            Assert.That(river.Points[0].X, Is.EqualTo(0f).Within(.001f),
                "Moving a spanning river must retain its map-edge connection.");
        }

        [Test]
        public void RiverBedTextureIsAvailableToRuntimeRendering()
        {
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverBedResource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverBedBorderResource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverEdge01Resource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverShallow02Resource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverTransition03Resource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverMiddle04Resource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverGrassToDirtResource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverBedDirtResource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverWaterTextureResource), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                DistrictWorldController.RiverWhitecapTextureResource), Is.Not.Null);
            StringAssert.Contains("/RiverBlueV01/",
                DistrictWorldController.RiverWaterTextureResource);
            StringAssert.Contains("/RiverBlueV01/",
                DistrictWorldController.RiverWhitecapTextureResource);
        }

        [Test]
        public void BothCardinalAxesUseTheBlueV02SurfaceCalibration()
        {
            var district = new RegionCityTile { Width = 4, Height = 4 };
            district.Rivers.Add(new PlacedDistrictRiver
            {
                InstanceId = "blue-v02-west-east",
                Direction = DistrictRiverDirection.WestToEast,
                Depth = DistrictRiverDepth.Deep,
                WidthMeters = 46f,
                Points = new List<DistrictRiverPoint>
                {
                    new(0f, .3f), new(1f, .3f)
                }
            });
            district.Rivers.Add(new PlacedDistrictRiver
            {
                InstanceId = "blue-v02-north-south",
                Direction = DistrictRiverDirection.NorthToSouth,
                Depth = DistrictRiverDepth.Shallow,
                WidthMeters = 18f,
                Points = new List<DistrictRiverPoint>
                {
                    new(.7f, 1f), new(.7f, 0f)
                }
            });
            var host = new GameObject("Blue V02 cardinal river test");
            try
            {
                var world = host.AddComponent<DistrictWorldController>();
                Assert.That(world.WaterTextureTiling, Is.EqualTo(30f));
                Assert.That(world.DeepWaterStrength, Is.EqualTo(.42f));
                Assert.That(world.WaterBrightness, Is.EqualTo(1.16f));
                Assert.That(world.WaterEdgeOpacity, Is.EqualTo(.28f));
                Assert.That(world.WaterEdgeFadeWidth, Is.EqualTo(.14f));
                Assert.That(world.DepthBlendSoftness, Is.EqualTo(.34f));
                world.RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);

                var expectedBase = Resources.Load<Texture2D>(
                    DistrictWorldController.RiverWaterTextureResource);
                var expectedCrests = Resources.Load<Texture2D>(
                    DistrictWorldController.RiverWhitecapTextureResource);
                var surfaces = host.GetComponentsInChildren<MeshRenderer>()
                    .Where(renderer => renderer.name.StartsWith("River Water"))
                    .ToArray();

                Assert.That(surfaces, Has.Length.EqualTo(2));
                foreach (var surface in surfaces)
                {
                    var material = surface.sharedMaterial;
                    Assert.That(material.shader.name,
                        Is.EqualTo("CityForgeV3/RiverWaterSurface"));
                    Assert.That(material.mainTexture, Is.SameAs(expectedBase));
                    Assert.That(material.GetTexture("_WhitecapTex"),
                        Is.SameAs(expectedCrests));
                    Assert.That(material.GetFloat("_DeepWaterStrength"),
                        Is.EqualTo(.42f).Within(.0001f));
                    Assert.That(material.GetFloat("_WhitecapStrength"),
                        Is.EqualTo(.44f).Within(.0001f));
                    Assert.That(material.GetFloat("_WhitecapCoverage"),
                        Is.EqualTo(.72f).Within(.0001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void WiderRiverOwnsJunctionAndTributaryFadesRegardlessOfOrder()
        {
            var district = new RegionCityTile { Width = 4, Height = 4 };
            district.Rivers.Add(new PlacedDistrictRiver
            {
                InstanceId = "small-first",
                Direction = DistrictRiverDirection.NorthToSouth,
                Depth = DistrictRiverDepth.Shallow,
                WidthMeters = 18f,
                Points = new List<DistrictRiverPoint>
                {
                    new(.5f, 1f), new(.5f, 0f)
                }
            });
            district.Rivers.Add(new PlacedDistrictRiver
            {
                InstanceId = "major-second",
                Direction = DistrictRiverDirection.WestToEast,
                Depth = DistrictRiverDepth.Deep,
                WidthMeters = 54f,
                Points = new List<DistrictRiverPoint>
                {
                    new(0f, .5f), new(1f, .5f)
                }
            });
            var host = new GameObject("Width-owned river junction test");
            try
            {
                var world = host.AddComponent<DistrictWorldController>();
                world.RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);
                var waters = host.GetComponentsInChildren<MeshFilter>()
                    .Where(filter => filter.name.StartsWith("River Water — "))
                    .ToArray();
                var small = waters.Single(filter =>
                    filter.name.EndsWith("small-first"));
                var major = waters.Single(filter =>
                    filter.name.EndsWith("major-second"));

                Assert.That(small.sharedMesh.colors.Any(color => color.a < .99f),
                    Is.True, "The smaller river must fade into the major.");
                Assert.That(major.sharedMesh.colors.All(color => color.a > .999f),
                    Is.True, "The widest river must retain junction ownership.");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void RiverSurfaceSamplerReturnsBedWaterAndDownstreamDirection()
        {
            var district = new RegionCityTile { Width = 4, Height = 4 };
            district.Rivers.Add(new PlacedDistrictRiver
            {
                InstanceId = "surface-sampler-test",
                Direction = DistrictRiverDirection.WestToEast,
                Depth = DistrictRiverDepth.Deep,
                WidthMeters = 46f,
                Points = new List<DistrictRiverPoint>
                {
                    new(0f, .5f),
                    new(1f, .5f)
                }
            });
            var host = new GameObject("District river sampler test");
            try
            {
                var world = host.AddComponent<DistrictWorldController>();
                world.RebuildEntireDistrict(district, DistrictBulkRebuildReason.TestFixture);

                var center = world.SampleRiverSurface(Vector3.zero);
                Assert.That(center.HasValue, Is.True);
                Assert.That(center.Value.InsideChannel, Is.True);
                Assert.That(center.Value.UnderWater, Is.True);
                Assert.That(center.Value.BedElevation,
                    Is.LessThan(center.Value.WaterElevation));
                Assert.That(center.Value.WaterDepth, Is.GreaterThan(3f),
                    "Deep rivers should provide roughly ten feet of water " +
                    "over the broad center bed.");
                Assert.That(Vector3.Dot(center.Value.DownstreamDirection,
                    Vector3.right), Is.GreaterThan(.99f));

                Assert.That(world.SampleRiverSurface(
                    new Vector3(0f, 0f, 100f)), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static float LateralRange(PlacedDistrictRiver river,
            bool vertical)
        {
            var values = new List<float>();
            foreach (var point in river.Points)
                values.Add(vertical ? point.X : point.Z);
            return Mathf.Max(values.ToArray()) - Mathf.Min(values.ToArray());
        }

        private static void AssertRoundedStairs(PlacedDistrictRiver river)
        {
            var horizontal = false;
            var vertical = false;
            var curved = false;
            var hasPrior = false;
            var prior = Vector2.zero;
            foreach (var pair in river.Points.Zip(river.Points.Skip(1),
                         (a, b) => (a, b)))
            {
                var x = Mathf.Abs(pair.a.X - pair.b.X);
                var z = Mathf.Abs(pair.a.Z - pair.b.Z);
                var delta = new Vector2(pair.b.X - pair.a.X,
                    pair.b.Z - pair.a.Z);
                Assert.That(delta.sqrMagnitude, Is.GreaterThan(.000000001f));
                horizontal |= x > .000001f && z < .000001f;
                vertical |= z > .000001f && x < .000001f;
                curved |= x > .000001f && z > .000001f;
                if (hasPrior)
                    Assert.That(Vector2.Angle(prior, delta), Is.LessThan(50f),
                        "Rounded stairs must not retain a hard 90-degree turn.");
                prior = delta;
                hasPrior = true;
            }
            Assert.That(horizontal && vertical && curved, Is.True,
                "A rounded stair river needs straight runs, steps, and curves.");
        }
    }
}
