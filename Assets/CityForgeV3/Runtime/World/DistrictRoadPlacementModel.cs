using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public static class DistrictRoadPlacementModel
    {
        sealed class TopologyStamp { public int Value; }
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<List<PlacedRoadPiece>,TopologyStamp> TopologyStamps=new();
        static void MarkTopologyChanged(List<PlacedRoadPiece> roads)
        { if(roads!=null)TopologyStamps.GetValue(roads,_=>new TopologyStamp()).Value++; }
        public static void InvalidateNetwork(List<PlacedRoadPiece> roads) => MarkTopologyChanged(roads);
        sealed class NetworkStamp
        {
            public List<PlacedRoadPiece> Roads;
            public int Count=-1, Topology=-1, Bridge=-1, Key;
        }
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RegionCityTile,NetworkStamp> NetworkStamps=new();
        // A maintained generation, not a hash: distinct topology/bridge edits cannot cancel each other out.
        public static int NetworkKey(RegionCityTile district)
        {
            var roads=district.Roads;var stamp=NetworkStamps.GetValue(district,_=>new NetworkStamp());
            int topology=roads==null?0:TopologyStamps.GetValue(roads,_=>new TopologyStamp()).Value;
            int count=roads?.Count??0;
            if(!ReferenceEquals(stamp.Roads,roads)||stamp.Count!=count||stamp.Topology!=topology||stamp.Bridge!=district.BridgeRevision)
            {
                stamp.Roads=roads;stamp.Count=count;stamp.Topology=topology;stamp.Bridge=district.BridgeRevision;
                unchecked{stamp.Key++;}
            }
            return stamp.Key;
        }


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
                MarkTopologyChanged(_roads);
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
                MarkTopologyChanged(_roads);
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
                MarkTopologyChanged(_roads);
                return true;
            }

            // Smooth only a short staircase created by this drag. Older roads
            // and branch tiles are never removed, and the check touches at
            // most five path cells and their immediate neighbors.
            public bool TrySmoothAntiqueBrickStaircase(List<Vector2Int> path,
                HashSet<Vector2Int> addedThisStroke, ref int treasury,
                System.Func<Vector2Int, Vector2Int, bool> canConnect,
                out Vector2Int[] changed)
            {
                changed = null;
                if (path == null || path.Count < 3 || addedThisStroke == null)
                    return false;
                var last = path.Count - 1;
                var start = -1;
                if (last >= 4)
                {
                    var a = path[last - 4];
                    var b = path[last - 3];
                    var c = path[last - 2];
                    var d = path[last - 1];
                    var e = path[last];
                    var first = b - a;
                    var second = c - b;
                    if (first == d - c && second == e - d &&
                        IsCardinal(first) && IsCardinal(second) &&
                        first.x != second.x && first.y != second.y)
                        start = last - 4;
                }
                if (start < 0 && last >= 3)
                {
                    var heading = path[last - 2] - path[last - 3];
                    var first = path[last - 1] - path[last - 2];
                    var second = path[last] - path[last - 1];
                    if (Mathf.Abs(heading.x) == 1 &&
                        Mathf.Abs(heading.y) == 1 &&
                        IsCardinal(first) && IsCardinal(second) &&
                        first + second == heading)
                        start = last - 2;
                }
                if (start < 0) return false;

                var old = path.GetRange(start, path.Count - start);
                var removals = old.Count == 5
                    ? new[] { old[1], old[3] }
                    : new[] { old[1] };
                foreach (var cell in old)
                {
                    var road = At(cell.x, cell.y);
                    if (road == null ||
                        road.PackageId != RoadPiecePackageCatalog.TwoLaneSidewalkId ||
                        road.RoadMaterialId != "antique-brick") return false;
                }
                for (var index = 0; index < removals.Length; index++)
                {
                    var cell = removals[index];
                    var road = At(cell.x, cell.y);
                    if (!addedThisStroke.Contains(cell) ||
                        road.DistrictDiagonalConnections != 0) return false;
                    foreach (var port in CardinalPorts)
                    {
                        var step = Step(port);
                        var neighbor = cell + step;
                        if (At(neighbor.x, neighbor.y) != null &&
                            neighbor != old[index * 2] &&
                            neighbor != old[index * 2 + 2]) return false;
                    }
                }
                for (var index = 0; index < old.Count - 2; index += 2)
                    if (canConnect != null && !canConnect(old[index], old[index + 2]))
                        return false;

                foreach (var cell in removals)
                {
                    TryDelete(cell.x, cell.y);
                    addedThisStroke.Remove(cell);
                    treasury += AntiqueBrickCostPerTile;
                }
                for (var index = 0; index < old.Count - 2; index += 2)
                    TryConnectDiagonal(old[index], old[index + 2]);
                for (var index = last - 1; index > start; index -= 2)
                    path.RemoveAt(index);
                changed = old.ToArray();
                return true;
            }

            private static bool IsCardinal(Vector2Int step) =>
                Mathf.Abs(step.x) + Mathf.Abs(step.y) == 1;

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
                MarkTopologyChanged(_roads);
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
