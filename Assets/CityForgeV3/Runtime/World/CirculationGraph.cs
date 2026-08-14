using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum CirculationMode
    {
        Pedestrian,
        Vehicle
    }

    public enum CirculationDirection
    {
        TwoWay,
        StartToEnd,
        EndToStart
    }

    public enum CirculationNodeKind
    {
        Waypoint,
        LotBoundaryPort,
        BuildingEntrance,
        Crossing
    }

    [Serializable]
    public sealed class CirculationNode
    {
        public string Id = "";
        public Vector2 PositionMeters;
        public CirculationNodeKind Kind;
        public string PortId = "";

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Id) &&
            (Kind == CirculationNodeKind.Waypoint ||
             Kind == CirculationNodeKind.Crossing ||
             !string.IsNullOrWhiteSpace(PortId));
    }

    [Serializable]
    public sealed class CirculationSegment
    {
        public string Id = "";
        public string StartNodeId = "";
        public string EndNodeId = "";
        public CirculationDirection Direction = CirculationDirection.TwoWay;
        public float WidthMeters = 1.5f;
        public float SpeedMetersPerSecond = 1.4f;
    }

    [Serializable]
    public sealed class CirculationNetwork
    {
        public CirculationMode Mode;
        public List<CirculationNode> Nodes = new();
        public List<CirculationSegment> Segments = new();

        public CirculationNode FindNode(string id) =>
            Nodes.Find(node => node != null && node.Id == id);

        public CirculationNode AddNode(
            Vector2 positionMeters,
            CirculationNodeKind kind = CirculationNodeKind.Waypoint,
            string portId = "")
        {
            var node = new CirculationNode
            {
                Id = $"{Mode.ToString().ToLowerInvariant()}-node-{Nodes.Count + 1}",
                PositionMeters = positionMeters,
                Kind = kind,
                PortId = portId ?? ""
            };
            Nodes.Add(node);
            return node;
        }

        public CirculationSegment Connect(
            string startNodeId,
            string endNodeId,
            CirculationDirection direction = CirculationDirection.TwoWay)
        {
            if (startNodeId == endNodeId || FindNode(startNodeId) == null ||
                FindNode(endNodeId) == null)
                return null;
            var existing = Segments.Find(segment => segment != null &&
                ((segment.StartNodeId == startNodeId && segment.EndNodeId == endNodeId) ||
                 (segment.StartNodeId == endNodeId && segment.EndNodeId == startNodeId)));
            if (existing != null) return existing;
            var segment = new CirculationSegment
            {
                Id = $"{Mode.ToString().ToLowerInvariant()}-segment-{Segments.Count + 1}",
                StartNodeId = startNodeId,
                EndNodeId = endNodeId,
                Direction = direction,
                WidthMeters = Mode == CirculationMode.Vehicle ? 3f : 1.5f,
                SpeedMetersPerSecond = Mode == CirculationMode.Vehicle ? 5.5f : 1.4f
            };
            Segments.Add(segment);
            return segment;
        }

        public bool DeleteNode(string id)
        {
            var removed = Nodes.RemoveAll(node => node != null && node.Id == id) > 0;
            if (removed)
                Segments.RemoveAll(segment => segment == null ||
                    segment.StartNodeId == id || segment.EndNodeId == id);
            return removed;
        }

        public List<string> Validate()
        {
            var issues = new List<string>();
            var ids = new HashSet<string>();
            foreach (var node in Nodes)
            {
                if (node == null || !node.IsValid) issues.Add("Invalid circulation node");
                else if (!ids.Add(node.Id)) issues.Add($"Duplicate node {node.Id}");
            }
            foreach (var segment in Segments)
            {
                if (segment == null || string.IsNullOrWhiteSpace(segment.Id))
                    issues.Add("Invalid circulation segment");
                else if (segment.WidthMeters <= 0f || segment.SpeedMetersPerSecond <= 0f)
                    issues.Add($"Invalid dimensions on {segment.Id}");
                else if (segment.StartNodeId == segment.EndNodeId ||
                    FindNode(segment.StartNodeId) == null || FindNode(segment.EndNodeId) == null)
                    issues.Add($"Broken node reference on {segment.Id}");
            }
            return issues;
        }

        public Vector2 SampleFirstSegment(float progress)
        {
            if (Segments.Count == 0) return Nodes.Count == 0 ? Vector2.zero : Nodes[0].PositionMeters;
            var segment = Segments[0];
            var start = FindNode(segment.StartNodeId);
            var end = FindNode(segment.EndNodeId);
            return start == null || end == null
                ? Vector2.zero
                : Vector2.Lerp(start.PositionMeters, end.PositionMeters, Mathf.Clamp01(progress));
        }
    }

    public static class CirculationDefaults
    {
        public static void SeedVerticalSlice(LotSaveData data)
        {
            if (data.PedestrianNetwork.Nodes.Count == 0)
            {
                var entrance = data.PedestrianNetwork.AddNode(
                    new Vector2(0f, 1.5f), CirculationNodeKind.BuildingEntrance, "selected-building-entrance");
                var walk = data.PedestrianNetwork.AddNode(new Vector2(-3f, -1f));
                var boundary = data.PedestrianNetwork.AddNode(
                    new Vector2(-3f, -10f), CirculationNodeKind.LotBoundaryPort, "south-sidewalk");
                data.PedestrianNetwork.Connect(entrance.Id, walk.Id);
                data.PedestrianNetwork.Connect(walk.Id, boundary.Id);
            }
            if (data.VehicleNetwork.Nodes.Count == 0)
            {
                var south = data.VehicleNetwork.AddNode(
                    new Vector2(0f, -10f), CirculationNodeKind.LotBoundaryPort, "south-main");
                var center = data.VehicleNetwork.AddNode(new Vector2(0f, -2f));
                var north = data.VehicleNetwork.AddNode(
                    new Vector2(0f, 5f), CirculationNodeKind.LotBoundaryPort, "north-main");
                data.VehicleNetwork.Connect(south.Id, center.Id);
                data.VehicleNetwork.Connect(center.Id, north.Id);
            }
        }
    }
}
