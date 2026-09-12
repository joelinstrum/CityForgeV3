using CityForgeV3.World;
using NUnit.Framework;
using System.Collections.Generic;
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
            Assert.That(LateralRange(result.River, true), Is.GreaterThan(0.01f));
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
            Assert.That(LateralRange(result.River, false), Is.GreaterThan(0.005f),
                "Even minimum curvature must not produce a road-straight river.");
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
            Assert.That(restored.Rivers[0].Points, Has.Count.EqualTo(33));
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
                world.Build(district);

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
    }
}
