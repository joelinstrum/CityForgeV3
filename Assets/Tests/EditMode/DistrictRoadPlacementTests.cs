using System.Collections.Generic;
using CityForgeV3.World;
using NUnit.Framework;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class DistrictRoadPlacementTests
    {
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
    }
}
