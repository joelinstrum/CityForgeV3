using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    /// <summary>
    /// Traffic-law contract generated from placed road artwork. Roads own the
    /// legal lanes, intersections, controls, and speeds; pathfinding consumes it.
    /// </summary>
    public sealed class RoadTrafficGraph
    {
        public sealed class Intersection
        {
            public string NodeId;
            public Vector2 PositionMeters;
            public int ApproachCount;
            public int LegalTurnMovementCount;
            public string MinorApproachControl;
            public string ControlledApproachNodeId;
        }

        private readonly List<VehicleRoute> _routes = new();
        private readonly List<Intersection> _intersections = new();
        public IReadOnlyList<VehicleRoute> Routes => _routes;
        public IReadOnlyList<Intersection> Intersections => _intersections;
        public int LaneCount => _routes.Count;
        public int IntersectionCount => _intersections.Count;
        public int DirectedSegmentCount { get; private set; }
        public float SpeedMetersPerSecond { get; private set; }

        public static RoadTrafficGraph FromRoadNetwork(CirculationNetwork network,
            RoadPiecePackage roadPackage)
        {
            var graph = new RoadTrafficGraph
            {
                SpeedMetersPerSecond = roadPackage?.SpeedLimitMetersPerSecond ?? 0f
            };
            if (roadPackage?.AllowsVehicles != true) return graph;
            var offsets = roadPackage.LaneOffsetsMeters;
            var twoWay = roadPackage.TrafficDirection == "two_way" &&
                roadPackage.LaneCount >= 2;
            var routesPerDirection = twoWay
                ? Mathf.Max(1, roadPackage.LaneCount / 2)
                : Mathf.Max(1, roadPackage.LaneCount);
            for (var lane = 0; lane < routesPerDirection; lane++)
            {
                var offset = offsets[Mathf.Min(lane, offsets.Count - 1)];
                var clockwise = VehicleRoute.FromNetwork(network, offset, 2, true);
                if (clockwise != null) graph._routes.Add(clockwise);
                if (!twoWay) continue;
                var counterClockwise = VehicleRoute.FromNetwork(network, offset, 2, false);
                if (counterClockwise != null) graph._routes.Add(counterClockwise);
            }
            foreach (var route in graph._routes)
                graph.DirectedSegmentCount += route.Points.Count;
            graph.BuildIntersections(network, roadPackage);
            return graph;
        }

        private void BuildIntersections(CirculationNetwork network, RoadPiecePackage roadPackage)
        {
            if (network == null) return;
            foreach (var node in network.Nodes)
            {
                if (node == null || node.Kind == CirculationNodeKind.LotBoundaryPort) continue;
                var neighbors = new List<CirculationNode>();
                foreach (var segment in network.Segments)
                {
                    if (segment.StartNodeId == node.Id)
                        neighbors.Add(network.FindNode(segment.EndNodeId));
                    else if (segment.EndNodeId == node.Id)
                        neighbors.Add(network.FindNode(segment.StartNodeId));
                }
                neighbors.RemoveAll(candidate => candidate == null);
                if (neighbors.Count < 3) continue;
                var controlled = neighbors.Find(candidate =>
                    IsOneHopFromBoundary(candidate.Id, node.Id, network));
                _intersections.Add(new Intersection
                {
                    NodeId = node.Id,
                    PositionMeters = node.PositionMeters,
                    ApproachCount = neighbors.Count,
                    LegalTurnMovementCount = neighbors.Count * (neighbors.Count - 1),
                    MinorApproachControl = roadPackage?.MinorApproachControl ?? "yield",
                    ControlledApproachNodeId = controlled?.Id ?? ""
                });
            }
        }

        private static bool IsOneHopFromBoundary(string nodeId, string intersectionId,
            CirculationNetwork network)
        {
            foreach (var segment in network.Segments)
            {
                var otherId = segment.StartNodeId == nodeId ? segment.EndNodeId :
                    segment.EndNodeId == nodeId ? segment.StartNodeId : "";
                if (string.IsNullOrEmpty(otherId) || otherId == intersectionId) continue;
                if (network.FindNode(otherId)?.Kind == CirculationNodeKind.LotBoundaryPort)
                    return true;
            }
            return false;
        }
    }
}
