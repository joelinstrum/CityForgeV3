using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForgeV3.World
{
    // Routes contain connected, dry district road cells only. No forest shortcuts.
    public sealed class DistrictTimberNavigation
    {
        readonly RegionCityTile district;
        readonly Func<Vector2, bool> water;
        readonly Func<string,LotSaveData> readLot;
        readonly Dictionary<Vector2Int, PlacedRoadPiece> roads;
        readonly float width, depth;
        public DistrictTimberNavigation(RegionCityTile district, Func<Vector2, bool> water, Func<string,LotSaveData> readLot = null)
        {
            this.district = district; this.water = water; this.readLot = readLot ?? LotContentCatalog.Read;
            width = DistrictScale.SizeMeters(district.Width); depth = DistrictScale.SizeMeters(district.Height);
            roads = new();
            foreach (var road in district.Roads ?? new())
                if (road != null) roads[new Vector2Int(road.GridX, road.GridZ)] = road;
        }
        public Vector2 Center(Vector2Int cell) => new((cell.x + .5f) * 10 - width / 2, (cell.y + .5f) * 10 - depth / 2);
        Vector2Int Cell(Vector2 p) => new(Mathf.FloorToInt((p.x + width / 2) / 10), Mathf.FloorToInt((p.y + depth / 2) / 10));
        public bool OnRoad(Vector2 p)
        {
            return roads.ContainsKey(Cell(p)) && !water(new Vector2(p.x / width + .5f, p.y / depth + .5f));
        }
        public bool Segment(Vector2 a, Vector2 b)
        {
            var steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) * 2));
            for (int i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(a, b, (float)i / steps);
                // Width clearance applies to the horse and to both wagon axles.
                if (!OnRoad(p) || !OnRoad(p + Vector2.right * .8f) || !OnRoad(p - Vector2.right * .8f) ||
                    !OnRoad(p + Vector2.up * .8f) || !OnRoad(p - Vector2.up * .8f)) return false;
            }
            return true;
        }
        public Vector2? ParkingNear(Vector2 camp, float maximumDistance = 12)
        {
            foreach (var p in roads.Keys.Select(Center).OrderBy(p => (p - camp).sqrMagnitude))
                if (Vector2.Distance(camp, p) <= maximumDistance && Segment(p, p)) return p;
            return null;
        }
        public float ParkingHeading(Vector2 p)
        {
            var cell = Cell(p);
            foreach (var delta in Directions)
                if (roads.ContainsKey(cell + delta) && Segment(p, Center(cell + delta)))
                    return Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            return 0;
        }
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        Dictionary<Vector2Int, Vector2Int> Search(Vector2 from)
        {
            if (!Segment(from, Center(Cell(from)))) return null;
            var start = Cell(from); var previous = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var queue = new Queue<Vector2Int>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var delta in Directions)
                {
                    var next = current + delta;
                    if (previous.ContainsKey(next) || !roads.ContainsKey(next) || !Segment(Center(current), Center(next))) continue;
                    previous[next] = current; queue.Enqueue(next);
                }
            }
            return previous;
        }
        List<Vector2> Trace(Dictionary<Vector2Int, Vector2Int> previous, Vector2 from, Vector2 to)
        {
            var end = Cell(to);
            if (previous == null || !previous.ContainsKey(end) || !Segment(Center(end), to)) return null;
            var result = new List<Vector2> { to };
            for (var cell = end; ; cell = previous[cell]) { result.Add(Center(cell)); if (previous[cell] == cell) break; }
            result.Reverse();
            // A wagon reloading mid-cell must continue toward the next cell,
            // not reverse to the center it has already passed.
            if (result.Count > 1 && Segment(from, result[1])) result.RemoveAt(0);
            return result;
        }
        public List<Vector2> Route(Vector2 from, Vector2 to) => Trace(Search(from), from, to);
        public List<TimberDestination> Mills(Vector2 from)
        {
            var previous = Search(from); var result = new List<TimberDestination>();
            if (previous == null) return result;
            foreach (var placed in district.Lots ?? new())
            {
                var data = readLot(placed.LotId);
                if (data == null || !(data.Buildings3D?.Any(b => b.AssetId == "lumber-mill-v01") ?? false)) continue;
                var center = DistrictWorldController.DistrictLotCenterMeters(district, placed, data);
                float x = data.LotWidthCells * 10, z = data.LotDepthCells * 10;
                if (placed.RotationQuarterTurns % 2 != 0) (x, z) = (z, x);
                var rect = new Rect(center.x - x / 2, center.y - z / 2, x, z);
                foreach (var cell in previous.Keys)
                {
                    var p = Center(cell);
                    var gap = Vector2.Distance(p, new Vector2(Mathf.Clamp(p.x, rect.xMin, rect.xMax), Mathf.Clamp(p.y, rect.yMin, rect.yMax)));
                    // A road may enter the lot as a driveway. Keep the dry,
                    // connected road checks, but do not exclude interior stops.
                    if (gap > 6) continue;
                    var route = Trace(previous, from, p); if (route == null) continue;
                    float distance = 0; var last = from;
                    foreach (var point in route) { distance += Vector2.Distance(last, point); last = point; }
                    result.Add(new TimberDestination { MillId = placed.InstanceId, Point = p, Route = route, Distance = distance });
                }
            }
            return result.OrderBy(r => r.Distance).ToList();
        }
    }
}
