using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public static class DistrictRoadPlacementModel
    {
        public const string DirtFamily = "Dirt Road";
        public const string AntiqueBrickFamily = "Antique Brick Road";
        public const int AntiqueBrickCostPerTile = 25;

        private static readonly RoadPiecePort[] CardinalPorts =
        {
            RoadPiecePort.North, RoadPiecePort.East,
            RoadPiecePort.South, RoadPiecePort.West
        };

        public static string PackageId(string family) =>
            family == AntiqueBrickFamily
                ? RoadPiecePackageCatalog.TwoLaneSidewalkId
                : RoadPiecePackageCatalog.NationalPikeDirtId;

        public static int CostPerTile(string family) =>
            family == AntiqueBrickFamily ? AntiqueBrickCostPerTile : 0;

        public static bool IsFloraPositionClear(RegionCityTile district,
            Vector2 normalized, float clearanceMeters = 3f)
        {
            if (district == null) return false;
            var width = DistrictScale.SizeMeters(district.Width);
            var depth = DistrictScale.SizeMeters(district.Height);
            var point = new Vector2((Mathf.Clamp01(normalized.x) - .5f) * width,
                (Mathf.Clamp01(normalized.y) - .5f) * depth);
            var halfCell = DistrictScale.CellSizeMeters * .5f;
            var clearance = Mathf.Max(0f, clearanceMeters);
            foreach (var road in district.Roads ?? new List<PlacedRoadPiece>())
            {
                if (road == null) continue;
                var center = new Vector2(
                    -width * .5f + (road.GridX + .5f) * DistrictScale.CellSizeMeters,
                    -depth * .5f + (road.GridZ + .5f) * DistrictScale.CellSizeMeters);
                var dx = Mathf.Max(Mathf.Abs(point.x - center.x) - halfCell, 0f);
                var dz = Mathf.Max(Mathf.Abs(point.y - center.y) - halfCell, 0f);
                if (dx * dx + dz * dz <= clearance * clearance) return false;
            }
            return true;
        }

        public static bool TryPlace(List<PlacedRoadPiece> roads, int x, int z,
            int columns, int rows, string family, ref int treasury)
        {
            if (roads == null || x < 0 || z < 0 || x >= columns || z >= rows)
                return false;
            var packageId = PackageId(family);
            var existing = RoadPlacementModel.FindAt(roads, x, z);
            if (existing != null && existing.PackageId == packageId) return false;
            var cost = CostPerTile(family);
            if (cost > treasury) return false;
            treasury -= cost;
            if (existing == null)
            {
                existing = new PlacedRoadPiece
                {
                    Id = $"district-road-{System.Guid.NewGuid():N}",
                    GridX = x,
                    GridZ = z
                };
                roads.Add(existing);
            }
            existing.PackageId = packageId;
            existing.RoadMaterialId = family == AntiqueBrickFamily
                ? "antique-brick" : "dirt";
            existing.SidewalkMaterialId = family == AntiqueBrickFamily
                ? "antique-brick" : "";
            existing.MarkingStyle = RoadMarkingStyle.NoLines;
            existing.LaneMarkingStyle = RoadLaneMarkingStyle.NoLines;
            existing.CenterMarkingStyle = RoadCenterMarkingStyle.NoLines;
            Repair(roads);
            return true;
        }

        public static void Repair(List<PlacedRoadPiece> roads)
        {
            if (roads == null) return;
            var occupied = new HashSet<Vector2Int>();
            foreach (var road in roads)
                if (road != null) occupied.Add(new Vector2Int(road.GridX, road.GridZ));
            foreach (var road in roads)
            {
                if (road == null) continue;
                var desired = new List<RoadPiecePort>();
                foreach (var port in CardinalPorts)
                {
                    var neighbor = port switch
                    {
                        RoadPiecePort.North => new Vector2Int(road.GridX, road.GridZ + 1),
                        RoadPiecePort.East => new Vector2Int(road.GridX + 1, road.GridZ),
                        RoadPiecePort.South => new Vector2Int(road.GridX, road.GridZ - 1),
                        _ => new Vector2Int(road.GridX - 1, road.GridZ)
                    };
                    if (occupied.Contains(neighbor))
                        desired.Add(port);
                }
                if (desired.Count == 0) desired.Add(RoadPiecePort.North);
                var package = RoadPiecePackageCatalog.Resolve(road.PackageId);
                if (!RoadPlacementModel.TryFindTopologyForPorts(package, desired,
                        out var topology, out var turns)) continue;
                road.Topology = topology;
                road.RotationQuarterTurns = turns;
            }
        }

        public static bool TryDelete(List<PlacedRoadPiece> roads, int x, int z)
        {
            var road = RoadPlacementModel.FindAt(roads, x, z);
            if (road == null) return false;
            roads.Remove(road);
            Repair(roads);
            return true;
        }
    }
}
