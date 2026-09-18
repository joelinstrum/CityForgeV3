using System.Collections.Generic;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class DistrictRoadPlacementTests
    {
        [TestCase(3, 3)]
        [TestCase(3, -3)]
        [TestCase(-3, 3)]
        [TestCase(-3, -3)]
        public void OctileRouteSupportsAllDiagonalFacings(int dx, int dz)
        {
            var route = DistrictRoadPlacementModel.OctileRoute(
                new Vector2Int(8, 8), new Vector2Int(8 + dx, 8 + dz));
            Assert.That(route.Count, Is.EqualTo(4));
            Assert.That(route[0], Is.EqualTo(new Vector2Int(8, 8)));
            Assert.That(route[3], Is.EqualTo(new Vector2Int(8 + dx, 8 + dz)));
            for (var index = 1; index < route.Count; index++)
                Assert.That(Mathf.Abs(route[index].x - route[index - 1].x),
                    Is.EqualTo(1));
        }

        [Test]
        public void CountryAndPikeRemainDistinctWhenConnectingAndReplacingRoads()
        {
            var roads = new List<PlacedRoadPiece>();
            var treasury = 100;
            DistrictRoadPlacementModel.TryPlace(roads, 1, 1, 8, 8,
                DistrictRoadPlacementModel.DirtFamily, ref treasury);
            DistrictRoadPlacementModel.TryPlace(roads, 2, 1, 8, 8,
                DistrictRoadPlacementModel.PikeDirtFamily, ref treasury);
            Assert.That(roads[0].PackageId, Is.EqualTo(RoadPiecePackageCatalog.DirtRoadId));
            Assert.That(roads[1].PackageId, Is.EqualTo(RoadPiecePackageCatalog.NationalPikeDirtId));
            Assert.That(DistrictRoadPlacementModel.TryPlace(roads, 2, 1, 8, 8,
                DistrictRoadPlacementModel.DirtFamily, ref treasury), Is.True);
            Assert.That(roads.Count, Is.EqualTo(2));
            Assert.That(roads[1].PackageId, Is.EqualTo(RoadPiecePackageCatalog.DirtRoadId));
            Assert.That(treasury, Is.EqualTo(100));
        }

        [Test]
        public void DirtIsFreeAndBrickCostsTwentyFiveOnlyForNewTile()
        {
            var roads = new List<PlacedRoadPiece>();
            var treasury = 100;
            Assert.That(DistrictRoadPlacementModel.TryPlace(roads, 1, 1,
                8, 8, DistrictRoadPlacementModel.DirtFamily, ref treasury), Is.True);
            Assert.That(treasury, Is.EqualTo(100));
            Assert.That(DistrictRoadPlacementModel.TryPlace(roads, 2, 1,
                8, 8, DistrictRoadPlacementModel.AntiqueBrickFamily,
                ref treasury), Is.True);
            Assert.That(treasury, Is.EqualTo(75));
            Assert.That(DistrictRoadPlacementModel.TryPlace(roads, 2, 1,
                8, 8, DistrictRoadPlacementModel.AntiqueBrickFamily,
                ref treasury), Is.False);
            Assert.That(treasury, Is.EqualTo(75));
        }

        [Test]
        public void NeighborConnectionsInferCornerTAndCross()
        {
            var roads = new List<PlacedRoadPiece>();
            var treasury = 1000;
            foreach (var cell in new[] { (2, 2), (2, 3), (3, 2) })
                DistrictRoadPlacementModel.TryPlace(roads, cell.Item1, cell.Item2,
                    8, 8, DistrictRoadPlacementModel.DirtFamily, ref treasury);
            Assert.That(RoadPlacementModel.FindAt(roads, 2, 2).Topology,
                Is.EqualTo(RoadPieceTopology.Corner));

            DistrictRoadPlacementModel.TryPlace(roads, 2, 1, 8, 8,
                DistrictRoadPlacementModel.DirtFamily, ref treasury);
            Assert.That(RoadPlacementModel.FindAt(roads, 2, 2).Topology,
                Is.EqualTo(RoadPieceTopology.TJunction));

            DistrictRoadPlacementModel.TryPlace(roads, 1, 2, 8, 8,
                DistrictRoadPlacementModel.DirtFamily, ref treasury);
            Assert.That(RoadPlacementModel.FindAt(roads, 2, 2).Topology,
                Is.EqualTo(RoadPieceTopology.FourWay));
        }

        [Test]
        public void BrickPlacementStopsWhenTreasuryCannotAffordTile()
        {
            var roads = new List<PlacedRoadPiece>();
            var treasury = 24;
            Assert.That(DistrictRoadPlacementModel.TryPlace(roads, 0, 0,
                4, 4, DistrictRoadPlacementModel.AntiqueBrickFamily,
                ref treasury), Is.False);
            Assert.That(roads, Is.Empty);
            Assert.That(treasury, Is.EqualTo(24));
        }

        [Test]
        public void DeletingRoadRepairsItsFormerNeighbors()
        {
            var roads = new List<PlacedRoadPiece>();
            var treasury = 100;
            for (var x = 1; x <= 3; x++)
                DistrictRoadPlacementModel.TryPlace(roads, x, 2, 8, 8,
                    DistrictRoadPlacementModel.DirtFamily, ref treasury);
            Assert.That(RoadPlacementModel.FindAt(roads, 2, 2).Topology,
                Is.EqualTo(RoadPieceTopology.Straight));
            Assert.That(DistrictRoadPlacementModel.TryDelete(roads, 3, 2),
                Is.True);
            Assert.That(RoadPlacementModel.FindAt(roads, 2, 2).Topology,
                Is.EqualTo(RoadPieceTopology.Endpoint));
        }

        [Test]
        public void AntiqueBrickDragConnectsDiagonalTilesAndRepairsOnlyNearbyRoads()
        {
            var roads = new List<PlacedRoadPiece>();
            var treasury = 1000;
            var session = new DistrictRoadPlacementModel.EditSession(roads);
            var route = DistrictRoadPlacementModel.OctileRoute(
                new Vector2Int(2, 2), new Vector2Int(5, 5));
            Assert.That(route, Is.EqualTo(new[]
            {
                new Vector2Int(2, 2), new Vector2Int(3, 3),
                new Vector2Int(4, 4), new Vector2Int(5, 5)
            }));
            foreach (var cell in route)
                Assert.That(session.TryPlace(cell.x, cell.y, 12, 12,
                    DistrictRoadPlacementModel.AntiqueBrickFamily,
                    ref treasury), Is.True);
            for (var index = 1; index < route.Count; index++)
                Assert.That(session.TryConnectDiagonal(route[index - 1],
                    route[index]), Is.True);
            Assert.That(treasury, Is.EqualTo(900));
            Assert.That(session.At(3, 3).Topology,
                Is.EqualTo(RoadPieceTopology.Diagonal));
            Assert.That(session.Connections(session.At(3, 3)) &
                (1 << (int)RoadPiecePort.NorthEast), Is.Not.Zero);
            Assert.That(session.Connections(session.At(3, 3)) &
                (1 << (int)RoadPiecePort.SouthWest), Is.Not.Zero);
            var restored = JsonUtility.FromJson<RegionCityTile>(
                JsonUtility.ToJson(new RegionCityTile { Roads = roads }));
            Assert.That(restored.Roads[1].DistrictDiagonalConnections,
                Is.EqualTo(session.At(3, 3).DistrictDiagonalConnections));

            Assert.That(session.TryDelete(4, 4), Is.True);
            Assert.That(session.At(3, 3).DistrictDiagonalConnections &
                (1 << (int)RoadPiecePort.NorthEast), Is.Zero);
            Assert.That(session.At(5, 5).DistrictDiagonalConnections, Is.Zero);
        }

        [Test]
        public void AntiqueBrickCanTurnFromCardinalToDiagonalWithoutLinkingTouchingRoads()
        {
            var roads = new List<PlacedRoadPiece>();
            var treasury = 1000;
            var session = new DistrictRoadPlacementModel.EditSession(roads);
            foreach (var cell in new[] { new Vector2Int(3, 2),
                         new Vector2Int(3, 3), new Vector2Int(4, 4),
                         new Vector2Int(5, 5), new Vector2Int(6, 6) })
                session.TryPlace(cell.x, cell.y, 12, 12,
                    DistrictRoadPlacementModel.AntiqueBrickFamily,
                    ref treasury);
            session.TryConnectDiagonal(new Vector2Int(3, 3),
                new Vector2Int(4, 4));
            session.TryConnectDiagonal(new Vector2Int(4, 4),
                new Vector2Int(5, 5));
            var turn = session.At(3, 3);
            Assert.That(session.Connections(turn) &
                (1 << (int)RoadPiecePort.South), Is.Not.Zero);
            Assert.That(session.Connections(turn) &
                (1 << (int)RoadPiecePort.NorthEast), Is.Not.Zero);
            Assert.That(session.At(5, 5).DistrictDiagonalConnections &
                (1 << (int)RoadPiecePort.NorthEast), Is.Zero,
                "Touching road cells must not silently become diagonal links.");
        }

        [Test]
        public void RepeatedStairStepsBecomeDiagonalAndRefundOnlyRemovedTiles()
        {
            var roads = new List<PlacedRoadPiece>();
            var session = new DistrictRoadPlacementModel.EditSession(roads);
            var treasury = 1000;
            var path = new List<Vector2Int>();
            var added = new HashSet<Vector2Int>();
            foreach (var cell in new[] { new Vector2Int(2, 2),
                         new Vector2Int(3, 2), new Vector2Int(3, 3),
                         new Vector2Int(4, 3), new Vector2Int(4, 4) })
            {
                session.TryPlace(cell.x, cell.y, 12, 12,
                    DistrictRoadPlacementModel.AntiqueBrickFamily, ref treasury);
                path.Add(cell);
                added.Add(cell);
            }
            Assert.That(session.TrySmoothAntiqueBrickStaircase(path, added,
                ref treasury, (_, _) => true, out var changed), Is.True);
            Assert.That(changed.Length, Is.EqualTo(5));
            Assert.That(path, Is.EqualTo(new[] { new Vector2Int(2, 2),
                new Vector2Int(3, 3), new Vector2Int(4, 4) }));
            Assert.That(session.At(3, 2), Is.Null);
            Assert.That(session.At(4, 3), Is.Null);
            Assert.That(treasury, Is.EqualTo(925));
            Assert.That(session.At(3, 3).DistrictDiagonalConnections,
                Is.Not.Zero);

            foreach (var cell in new[] { new Vector2Int(5, 4),
                         new Vector2Int(5, 5) })
            {
                session.TryPlace(cell.x, cell.y, 12, 12,
                    DistrictRoadPlacementModel.AntiqueBrickFamily, ref treasury);
                path.Add(cell);
                added.Add(cell);
            }
            Assert.That(session.TrySmoothAntiqueBrickStaircase(path, added,
                ref treasury, (_, _) => true, out _), Is.True);
            Assert.That(path[path.Count - 1], Is.EqualTo(new Vector2Int(5, 5)));
            Assert.That(session.At(5, 4), Is.Null);
            Assert.That(treasury, Is.EqualTo(900));
        }

        [Test]
        public void StairSmoothingPreservesPreexistingRoadAndBranches()
        {
            var roads = new List<PlacedRoadPiece>();
            var session = new DistrictRoadPlacementModel.EditSession(roads);
            var treasury = 1000;
            var path = new List<Vector2Int> { new(2, 2), new(3, 2),
                new(3, 3), new(4, 3), new(4, 4) };
            foreach (var cell in path)
                session.TryPlace(cell.x, cell.y, 12, 12,
                    DistrictRoadPlacementModel.AntiqueBrickFamily, ref treasury);
            var added = new HashSet<Vector2Int>(path);
            added.Remove(new Vector2Int(3, 2));
            Assert.That(session.TrySmoothAntiqueBrickStaircase(path, added,
                ref treasury, (_, _) => true, out _), Is.False);
            Assert.That(session.At(3, 2), Is.Not.Null);

            added.Add(new Vector2Int(3, 2));
            session.TryPlace(3, 1, 12, 12,
                DistrictRoadPlacementModel.AntiqueBrickFamily, ref treasury);
            Assert.That(session.TrySmoothAntiqueBrickStaircase(path, added,
                ref treasury, (_, _) => true, out _), Is.False);
            Assert.That(roads.Count, Is.EqualTo(6));
        }

        [Test]
        public void DeliveryUsesOnlyExplicitDiagonalLinks()
        {
            var district = new RegionCityTile { Width = 1, Height = 1 };
            var session = new DistrictRoadPlacementModel.EditSession(district.Roads);
            var treasury = 1000;
            foreach (var cell in new[] { new Vector2Int(32, 32),
                         new Vector2Int(33, 33), new Vector2Int(34, 34) })
                session.TryPlace(cell.x, cell.y, 64, 64,
                    DistrictRoadPlacementModel.AntiqueBrickFamily,
                    ref treasury);
            Assert.That(new DistrictRoadDelivery(district).Route(
                new Vector2(5, 5), new Rect(25, 25, 0, 0), 5f), Is.Null);
            session.TryConnectDiagonal(new Vector2Int(32, 32),
                new Vector2Int(33, 33));
            session.TryConnectDiagonal(new Vector2Int(33, 33),
                new Vector2Int(34, 34));
            Assert.That(new DistrictRoadDelivery(district).Route(
                new Vector2(5, 5), new Rect(25, 25, 0, 0), 5f),
                Is.EqualTo(new[] { new Vector2(5, 5),
                    new Vector2(15, 15), new Vector2(25, 25) }));
        }

        [Test]
        public void DiagonalBrickPresentationSharesMaterialAndBridgesTileCorners()
        {
            var district = new RegionCityTile { Width = 1, Height = 1 };
            var session = new DistrictRoadPlacementModel.EditSession(district.Roads);
            var treasury = 1000;
            foreach (var cell in new[] { new Vector2Int(32, 32),
                         new Vector2Int(33, 33), new Vector2Int(34, 34) })
                session.TryPlace(cell.x, cell.y, 64, 64,
                    DistrictRoadPlacementModel.AntiqueBrickFamily,
                    ref treasury);
            session.TryConnectDiagonal(new Vector2Int(32, 32),
                new Vector2Int(33, 33));
            session.TryConnectDiagonal(new Vector2Int(33, 33),
                new Vector2Int(34, 34));
            var root = new GameObject("Diagonal Brick Review");
            try
            {
                var world = root.AddComponent<DistrictWorldController>();
                world.Build(district);
                var renderers = System.Array.FindAll(
                    root.GetComponentsInChildren<MeshRenderer>(),
                    renderer => renderer.name == "District Antique Brick Road");
                Assert.That(renderers.Length, Is.EqualTo(3));
                Assert.That(renderers[0].sharedMaterial,
                    Is.SameAs(renderers[1].sharedMaterial));
                Assert.That(renderers[0].sharedMaterial.mainTexture.name,
                    Is.EqualTo("brick-antique"));
                var bounds = renderers[1].GetComponent<MeshFilter>().sharedMesh.bounds;
                Assert.That(bounds.size.x, Is.GreaterThan(10f),
                    "Diagonal strips must reach across tile corners.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StraightBrickUsesPlainBrickSurfaceWithoutAuthoredEdgeArtwork()
        {
            var district = new RegionCityTile { Width = 1, Height = 1 };
            var session = new DistrictRoadPlacementModel.EditSession(district.Roads);
            var treasury = 1000;
            session.TryPlace(32, 32, 64, 64,
                DistrictRoadPlacementModel.AntiqueBrickFamily, ref treasury);
            session.TryPlace(33, 32, 64, 64,
                DistrictRoadPlacementModel.AntiqueBrickFamily, ref treasury);
            var root = new GameObject("Straight Brick Review");
            try
            {
                root.AddComponent<DistrictWorldController>().Build(district);
                var renderers = System.Array.FindAll(
                    root.GetComponentsInChildren<MeshRenderer>(),
                    renderer => renderer.name == "District Antique Brick Road");
                Assert.That(renderers.Length, Is.EqualTo(2));
                Assert.That(renderers[0].sharedMaterial,
                    Is.SameAs(renderers[1].sharedMaterial));
                Assert.That(renderers[0].sharedMaterial.mainTexture.name,
                    Is.EqualTo("brick-antique"));
                Assert.That(renderers[0].sharedMaterial.GetFloat("_UseMaterialZones"),
                    Is.Zero, "The marked topology artwork must not be sampled.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
