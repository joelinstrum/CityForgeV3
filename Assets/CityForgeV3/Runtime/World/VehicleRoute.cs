using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    /// <summary>
    /// Distance-based closed vehicle route derived from the cycle core of an
    /// undirected circulation graph. Branches and boundary exits are excluded
    /// from the pilot loop; they remain available to future spawn/despawn logic.
    /// </summary>
    public sealed class VehicleRoute
    {
        public const float HeadingLookAroundMeters = 2f;
        private readonly List<Vector2> _points;
        private readonly List<float> _segmentStarts = new();

        public IReadOnlyList<Vector2> Points => _points;
        public float TotalLengthMeters { get; }
        public bool IsClosed => _points.Count >= 3;
        public bool Clockwise { get; }

        private VehicleRoute(List<Vector2> points, bool clockwise)
        {
            _points = points;
            Clockwise = clockwise;
            var distance = 0f;
            for (var index = 0; index < _points.Count; index++)
            {
                _segmentStarts.Add(distance);
                distance += Vector2.Distance(_points[index], _points[(index + 1) % _points.Count]);
            }
            TotalLengthMeters = distance;
        }

        public static VehicleRoute FromNetwork(
            CirculationNetwork network,
            float rightLaneOffsetMeters = 1.05f,
            int smoothingPasses = 2,
            bool clockwise = true)
        {
            var cycle = FindCycleCore(network);
            if (cycle.Count < 3) return null;
            var points = new List<Vector2>();
            foreach (var nodeId in cycle)
                points.Add(network.FindNode(nodeId).PositionMeters);

            // Each direction receives its own right-hand lane. Direction is a
            // property of the lane graph, not an ad-hoc vehicle preference.
            if (clockwise && SignedArea(points) > 0f) points.Reverse();
            if (!clockwise && SignedArea(points) < 0f) points.Reverse();
            points = OffsetClosedPolyline(points, rightLaneOffsetMeters);
            for (var pass = 0; pass < Mathf.Max(0, smoothingPasses); pass++)
                points = ChaikinClosed(points);
            return new VehicleRoute(points, clockwise);
        }

        public void Sample(float distanceMeters, out Vector2 point, out Vector2 direction)
        {
            if (_points.Count == 0 || TotalLengthMeters <= 0.001f)
            {
                point = Vector2.zero;
                direction = Vector2.up;
                return;
            }
            var wrapped = Mathf.Repeat(distanceMeters, TotalLengthMeters);
            var segmentIndex = _points.Count - 1;
            for (var index = 0; index < _points.Count; index++)
            {
                var nextStart = index == _points.Count - 1
                    ? TotalLengthMeters
                    : _segmentStarts[index + 1];
                if (wrapped < nextStart)
                {
                    segmentIndex = index;
                    break;
                }
            }
            var start = _points[segmentIndex];
            var end = _points[(segmentIndex + 1) % _points.Count];
            var length = Vector2.Distance(start, end);
            var progress = length <= 0.001f
                ? 0f
                : (wrapped - _segmentStarts[segmentIndex]) / length;
            point = Vector2.Lerp(start, end, Mathf.Clamp01(progress));
            direction = (end - start).normalized;
        }

        public float SteeringDegrees(float distanceMeters, float lookAheadMeters = 0.75f)
        {
            var before = SmoothedDirection(distanceMeters - lookAheadMeters);
            var after = SmoothedDirection(distanceMeters + lookAheadMeters);
            return Mathf.Clamp(Vector2.SignedAngle(before, after) * 1.35f, -24f, 24f);
        }

        public Vector2 SmoothedDirection(float distanceMeters,
            float lookAroundMeters = HeadingLookAroundMeters)
        {
            Sample(distanceMeters - lookAroundMeters, out var before, out _);
            Sample(distanceMeters + lookAroundMeters, out var after, out _);
            var direction = (after - before).normalized;
            if (direction.sqrMagnitude > 0.001f) return direction;
            Sample(distanceMeters, out _, out direction);
            return direction;
        }

        private static List<string> FindCycleCore(CirculationNetwork network)
        {
            var adjacency = new Dictionary<string, HashSet<string>>();
            foreach (var node in network?.Nodes ?? new List<CirculationNode>())
                if (node != null) adjacency[node.Id] = new HashSet<string>();
            foreach (var segment in network?.Segments ?? new List<CirculationSegment>())
            {
                if (segment == null || !adjacency.ContainsKey(segment.StartNodeId) ||
                    !adjacency.ContainsKey(segment.EndNodeId)) continue;
                adjacency[segment.StartNodeId].Add(segment.EndNodeId);
                adjacency[segment.EndNodeId].Add(segment.StartNodeId);
            }

            var active = new HashSet<string>(adjacency.Keys);
            var leaves = new Queue<string>();
            foreach (var pair in adjacency)
                if (pair.Value.Count < 2) leaves.Enqueue(pair.Key);
            while (leaves.Count > 0)
            {
                var removed = leaves.Dequeue();
                if (!active.Remove(removed)) continue;
                foreach (var neighbor in adjacency[removed])
                    if (active.Contains(neighbor) && ActiveDegree(neighbor, adjacency, active) < 2)
                        leaves.Enqueue(neighbor);
            }
            if (active.Count < 3) return new List<string>();

            var ordered = new List<string>(active);
            ordered.Sort(StringComparer.Ordinal);
            var start = ordered[0];
            var cycle = new List<string>();
            string previous = null;
            var current = start;
            for (var safety = 0; safety <= active.Count; safety++)
            {
                cycle.Add(current);
                var candidates = new List<string>();
                foreach (var neighbor in adjacency[current])
                    if (active.Contains(neighbor) && neighbor != previous) candidates.Add(neighbor);
                candidates.Sort(StringComparer.Ordinal);
                if (candidates.Count == 0) return new List<string>();
                var next = candidates.Find(candidate => candidate == start && cycle.Count >= 3);
                next ??= candidates.Find(candidate => !cycle.Contains(candidate));
                if (next == null) return new List<string>();
                if (next == start) return cycle;
                previous = current;
                current = next;
            }
            return new List<string>();
        }

        private static int ActiveDegree(string id, Dictionary<string, HashSet<string>> adjacency,
            HashSet<string> active)
        {
            var count = 0;
            foreach (var neighbor in adjacency[id]) if (active.Contains(neighbor)) count++;
            return count;
        }

        private static float SignedArea(List<Vector2> points)
        {
            var area = 0f;
            for (var index = 0; index < points.Count; index++)
            {
                var current = points[index];
                var next = points[(index + 1) % points.Count];
                area += current.x * next.y - next.x * current.y;
            }
            return area * 0.5f;
        }

        private static List<Vector2> OffsetClosedPolyline(List<Vector2> points, float offset)
        {
            var result = new List<Vector2>();
            for (var index = 0; index < points.Count; index++)
            {
                var previous = points[(index - 1 + points.Count) % points.Count];
                var current = points[index];
                var next = points[(index + 1) % points.Count];
                var incoming = (current - previous).normalized;
                var outgoing = (next - current).normalized;
                var rightIncoming = new Vector2(incoming.y, -incoming.x);
                var rightOutgoing = new Vector2(outgoing.y, -outgoing.x);
                var bisector = (rightIncoming + rightOutgoing).normalized;
                if (bisector.sqrMagnitude < 0.001f) bisector = rightIncoming;
                var denominator = Mathf.Max(0.35f, Vector2.Dot(bisector, rightIncoming));
                result.Add(current + bisector * (offset / denominator));
            }
            return result;
        }

        private static List<Vector2> ChaikinClosed(List<Vector2> points)
        {
            var result = new List<Vector2>(points.Count * 2);
            for (var index = 0; index < points.Count; index++)
            {
                var current = points[index];
                var next = points[(index + 1) % points.Count];
                result.Add(Vector2.Lerp(current, next, 0.25f));
                result.Add(Vector2.Lerp(current, next, 0.75f));
            }
            return result;
        }
    }
}
