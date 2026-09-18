using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public static class DistrictRoadPlacementModel
    {
        public const string DirtFamily = "Dirt Road";
        public const string PikeDirtFamily = "Pike Dirt Road";
        public const string AntiqueBrickFamily = "Antique Brick Road";
        public const int AntiqueBrickCostPerTile = 25;

        private static readonly RoadPiecePort[] CardinalPorts =
        {
            RoadPiecePort.North, RoadPiecePort.East,
            RoadPiecePort.South, RoadPiecePort.West
        };

        private static readonly RoadPiecePort[] DiagonalPorts =
        {
            RoadPiecePort.NorthEast, RoadPiecePort.SouthEast,
            RoadPiecePort.SouthWest, RoadPiecePort.NorthWest
        };

        public static string PackageId(string family) =>
            family == AntiqueBrickFamily
                ? RoadPiecePackageCatalog.TwoLaneSidewalkId
                : family == PikeDirtFamily
                    ? RoadPiecePackageCatalog.NationalPikeDirtId
                    : RoadPiecePackageCatalog.DirtRoadId;

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
            => new EditSession(roads).TryPlace(x, z, columns, rows, family,
                ref treasury);

        // The district editor owns one session for a drag. Its spatial index is
        // built once and maintained on each edit; normal placement never scans
        // the district to discover neighbors or repair unrelated roads.
        public sealed class EditSession
        {
            private readonly List<PlacedRoadPiece> _roads;
            private readonly Dictionary<Vector2Int, PlacedRoadPiece> _byCell = new();
            private int _knownCount;

            public EditSession(List<PlacedRoadPiece> roads)
            {
                _roads = roads;
                if (roads == null) return;
                foreach (var road in roads)
                    if (road != null)
                        _byCell[new Vector2Int(road.GridX, road.GridZ)] = road;
                _knownCount = roads.Count;
            }

            public bool Owns(List<PlacedRoadPiece> roads) =>
                ReferenceEquals(_roads, roads) && roads != null &&
                roads.Count == _knownCount;

            public PlacedRoadPiece At(int x, int z) =>
                _byCell.TryGetValue(new Vector2Int(x, z), out var road)
                    ? road : null;

            public bool TryPlace(int x, int z, int columns, int rows,
                string family, ref int treasury)
            {
                if (_roads == null || x < 0 || z < 0 || x >= columns || z >= rows)
                    return false;
                var packageId = PackageId(family);
                var existing = At(x, z);
                if (existing != null && existing.PackageId == packageId) return false;
                var cost = CostPerTile(family);
                if (cost > treasury) return false;
                treasury -= cost;
                if (existing == null)
                {
                    existing = new PlacedRoadPiece
                    {
                        Id = $"district-road-{System.Guid.NewGuid():N}",
                        GridX = x, GridZ = z
                    };
                    _roads.Add(existing);
                    _byCell.Add(new Vector2Int(x, z), existing);
                    _knownCount = _roads.Count;
                }
                else
                {
                    // Replacing a diagonal brick tile drops its links on both
                    // ends; the adjacent pieces then repair locally.
                    ClearDiagonalLinks(existing);
                }
                existing.PackageId = packageId;
                existing.RoadMaterialId = family == AntiqueBrickFamily
                    ? "antique-brick" : "dirt";
                existing.SidewalkMaterialId = family == AntiqueBrickFamily
                    ? "antique-brick" : "";
                existing.MarkingStyle = RoadMarkingStyle.NoLines;
                existing.LaneMarkingStyle = RoadLaneMarkingStyle.NoLines;
                existing.CenterMarkingStyle = RoadCenterMarkingStyle.NoLines;
                RepairAround(x, z);
                return true;
            }

            public bool TryConnectDiagonal(Vector2Int from, Vector2Int to)
            {
                var dx = to.x - from.x;
                var dz = to.y - from.y;
                if (Mathf.Abs(dx) != 1 || Mathf.Abs(dz) != 1) return false;
                var first = At(from.x, from.y);
                var second = At(to.x, to.y);
                if (first?.RoadMaterialId != "antique-brick" ||
                    second?.RoadMaterialId != "antique-brick" ||
                    first.PackageId != RoadPiecePackageCatalog.TwoLaneSidewalkId ||
                    second.PackageId != RoadPiecePackageCatalog.TwoLaneSidewalkId)
                    return false;
                var port = DiagonalPort(dx, dz);
                var opposite = Opposite(port);
                var firstBit = 1 << (int)port;
                var secondBit = 1 << (int)opposite;
                if ((first.DistrictDiagonalConnections & firstBit) != 0 &&
                    (second.DistrictDiagonalConnections & secondBit) != 0)
                    return false;
                first.DistrictDiagonalConnections |= firstBit;
                second.DistrictDiagonalConnections |= secondBit;
                RepairAt(first);
                RepairAt(second);
                return true;
            }

            public bool TryDelete(int x, int z)
            {
                var road = At(x, z);
                if (road == null) return false;
                ClearDiagonalLinks(road);
                _roads.Remove(road);
                _byCell.Remove(new Vector2Int(x, z));
                _knownCount = _roads.Count;
                RepairAround(x, z);
                return true;
            }

            public int Connections(PlacedRoadPiece road)
            {
                if (road == null) return 0;
                var mask = road.DistrictDiagonalConnections;
                foreach (var port in CardinalPorts)
                {
                    var step = Step(port);
                    if (At(road.GridX + step.x, road.GridZ + step.y) != null)
                        mask |= 1 << (int)port;
                }
                return mask;
            }

            public void RepairAll()
            {
                foreach (var road in _roads)
                    if (road != null) RepairAt(road);
            }

            private void ClearDiagonalLinks(PlacedRoadPiece road)
            {
                foreach (var port in DiagonalPorts)
                {
                    var bit = 1 << (int)port;
                    if ((road.DistrictDiagonalConnections & bit) == 0) continue;
                    var step = Step(port);
                    var other = At(road.GridX + step.x, road.GridZ + step.y);
                    if (other == null) continue;
                    other.DistrictDiagonalConnections &= ~(1 << (int)Opposite(port));
                    RepairAt(other);
                }
                road.DistrictDiagonalConnections = 0;
            }

            private void RepairAround(int x, int z)
            {
                for (var dz = -1; dz <= 1; dz++)
                    for (var dx = -1; dx <= 1; dx++)
                        RepairAt(At(x + dx, z + dz));
            }

            private void RepairAt(PlacedRoadPiece road)
            {
                if (road == null) return;
                // An old save may have a missing diagonal neighbor. Clear its
                // orphaned bit without changing any saved cardinal road.
                foreach (var port in DiagonalPorts)
                {
                    var bit = 1 << (int)port;
                    if ((road.DistrictDiagonalConnections & bit) == 0) continue;
                    var step = Step(port);
                    var other = At(road.GridX + step.x, road.GridZ + step.y);
                    if (other == null ||
                        (other.DistrictDiagonalConnections &
                         (1 << (int)Opposite(port))) == 0)
                        road.DistrictDiagonalConnections &= ~bit;
                }
                if (road.DistrictDiagonalConnections != 0)
                {
                    // District diagonal road presentation uses the explicit
                    // port mask to draw curved joins of any cardinal shape.
                    road.Topology = RoadPieceTopology.Diagonal;
                    road.RotationQuarterTurns = 0;
                    return;
                }
                var desired = new List<RoadPiecePort>();
                foreach (var port in CardinalPorts)
                {
                    var step = Step(port);
                    if (At(road.GridX + step.x, road.GridZ + step.y) != null)
                        desired.Add(port);
                }
                if (desired.Count == 0) desired.Add(RoadPiecePort.North);
                var package = RoadPiecePackageCatalog.Resolve(road.PackageId);
                if (!RoadPlacementModel.TryFindTopologyForPorts(package, desired,
                        out var topology, out var turns)) return;
                road.Topology = topology;
                road.RotationQuarterTurns = turns;
            }
        }

        public static void Repair(List<PlacedRoadPiece> roads)
        {
            if (roads != null) new EditSession(roads).RepairAll();
        }

        public static bool TryDelete(List<PlacedRoadPiece> roads, int x, int z)
            => roads != null && new EditSession(roads).TryDelete(x, z);

        public static Vector2Int Step(RoadPiecePort port) => port switch
        {
            RoadPiecePort.North => Vector2Int.up,
            RoadPiecePort.East => Vector2Int.right,
            RoadPiecePort.South => Vector2Int.down,
            RoadPiecePort.West => Vector2Int.left,
            RoadPiecePort.NorthEast => new Vector2Int(1, 1),
            RoadPiecePort.SouthEast => new Vector2Int(1, -1),
            RoadPiecePort.SouthWest => new Vector2Int(-1, -1),
            _ => new Vector2Int(-1, 1)
        };

        public static RoadPiecePort DiagonalPort(int dx, int dz) =>
            dx > 0 ? (dz > 0 ? RoadPiecePort.NorthEast : RoadPiecePort.SouthEast)
                : (dz > 0 ? RoadPiecePort.NorthWest : RoadPiecePort.SouthWest);

        public static RoadPiecePort Opposite(RoadPiecePort port) =>
            (int)port < 4
                ? (RoadPiecePort)(((int)port + 2) % 4)
                : (RoadPiecePort)(4 + (((int)port - 4 + 2) % 4));

        public static List<Vector2Int> OctileRoute(Vector2Int from,
            Vector2Int to)
        {
            var route = new List<Vector2Int> { from };
            var delta = to - from;
            var steps = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            for (var i = 1; i <= steps; i++)
            {
                var cell = new Vector2Int(
                    from.x + Mathf.RoundToInt((float)i * delta.x / steps),
                    from.y + Mathf.RoundToInt((float)i * delta.y / steps));
                if (cell != route[route.Count - 1]) route.Add(cell);
            }
            return route;
        }
    }
}
