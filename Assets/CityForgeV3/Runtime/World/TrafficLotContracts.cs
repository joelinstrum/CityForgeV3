using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum TrafficLotType
    {
        None,
        SuburbanStreet,
        ParkingLot
    }

    [Serializable]
    public sealed class OutsideRoadConnector
    {
        public string Id = "";
        public int GridX;
        public int GridZ;
        public RoadPiecePort Edge;
        public RoadTrafficFlow Flow = RoadTrafficFlow.TwoWay;

        public string PortId =>
            $"road-{GridX}-{GridZ}-{Edge.ToString().ToLowerInvariant()}";
    }

    public static class TrafficLotModel
    {
        public const float SuburbanMinimumSpawnSeconds = 8f;
        public const float SuburbanMaximumSpawnSeconds = 18f;
        public const int SuburbanMaximumActiveVehicles = 2;

        public static string DisplayName(TrafficLotType type) => type switch
        {
            TrafficLotType.SuburbanStreet => "Suburban Street",
            TrafficLotType.ParkingLot => "Parking Lot",
            _ => "None"
        };

        public static TrafficLotType ForDisplayName(string value) => value switch
        {
            "Suburban Street" => TrafficLotType.SuburbanStreet,
            "Parking Lot" => TrafficLotType.ParkingLot,
            _ => TrafficLotType.None
        };

        public static bool TryGetExteriorPort(
            PlacedRoadPiece piece,
            RoadPiecePackage package,
            int lotWidthMeters,
            int lotDepthMeters,
            out RoadPiecePort edge)
        {
            edge = default;
            if (piece == null || package?.Piece(piece.Topology) == null) return false;
            foreach (var port in package.Piece(piece.Topology).RotatedPorts(
                         piece.RotationQuarterTurns))
            {
                var neighbor = Neighbor(piece.GridX, piece.GridZ, port);
                if (RoadPlacementModel.IsInside(
                        neighbor.x, neighbor.y, lotWidthMeters, lotDepthMeters)) continue;
                edge = port;
                return true;
            }
            return false;
        }

        public static bool IsValidConnector(
            OutsideRoadConnector connector,
            IReadOnlyList<PlacedRoadPiece> pieces,
            RoadPiecePackage package,
            int lotWidthMeters,
            int lotDepthMeters)
        {
            if (connector == null || string.IsNullOrWhiteSpace(connector.Id)) return false;
            PlacedRoadPiece piece = null;
            foreach (var candidate in pieces ?? Array.Empty<PlacedRoadPiece>())
                if (candidate != null && candidate.GridX == connector.GridX &&
                    candidate.GridZ == connector.GridZ) piece = candidate;
            if (piece == null) return false;
            return TryGetExteriorPort(piece, package, lotWidthMeters, lotDepthMeters,
                       out var edge) && edge == connector.Edge;
        }

        public static List<Vector2> FindTrip(
            CirculationNetwork network,
            IReadOnlyList<OutsideRoadConnector> connectors)
        {
            if (network == null || connectors == null || connectors.Count < 2)
                return new List<Vector2>();
            OutsideRoadConnector inbound = null;
            OutsideRoadConnector outbound = null;
            foreach (var connector in connectors)
            {
                if (connector == null) continue;
                if (inbound == null && connector.Flow != RoadTrafficFlow.OutboundOnly)
                    inbound = connector;
                if (connector != inbound && connector.Flow != RoadTrafficFlow.InboundOnly)
                    outbound = connector;
            }
            if (inbound == null || outbound == null)
            {
                foreach (var connector in connectors)
                    if (connector != null && connector != inbound &&
                        connector.Flow != RoadTrafficFlow.InboundOnly) outbound = connector;
            }
            if (inbound == null || outbound == null || inbound == outbound)
                return new List<Vector2>();
            var start = network.Nodes.Find(node => node != null &&
                node.Kind == CirculationNodeKind.LotBoundaryPort &&
                node.PortId == inbound.PortId);
            var finish = network.Nodes.Find(node => node != null &&
                node.Kind == CirculationNodeKind.LotBoundaryPort &&
                node.PortId == outbound.PortId);
            if (start == null || finish == null) return new List<Vector2>();

            var previous = new Dictionary<string, string>();
            var queue = new Queue<string>();
            previous[start.Id] = null;
            queue.Enqueue(start.Id);
            while (queue.Count > 0 && !previous.ContainsKey(finish.Id))
            {
                var current = queue.Dequeue();
                foreach (var segment in network.Segments)
                {
                    if (segment == null) continue;
                    var next = segment.StartNodeId == current ? segment.EndNodeId :
                        segment.EndNodeId == current ? segment.StartNodeId : null;
                    if (next == null || previous.ContainsKey(next)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(finish.Id)) return new List<Vector2>();
            var ids = new List<string>();
            for (var id = finish.Id; id != null; id = previous[id]) ids.Add(id);
            ids.Reverse();
            var result = new List<Vector2>();
            foreach (var id in ids) result.Add(network.FindNode(id).PositionMeters);
            return result;
        }

        public static float TripLength(IReadOnlyList<Vector2> points)
        {
            var result = 0f;
            for (var index = 1; index < (points?.Count ?? 0); index++)
                result += Vector2.Distance(points[index - 1], points[index]);
            return result;
        }

        public static void SampleTrip(IReadOnlyList<Vector2> points, float distance,
            out Vector2 position, out Vector2 direction)
        {
            position = Vector2.zero;
            direction = Vector2.up;
            if (points == null || points.Count < 2) return;
            var remaining = Mathf.Max(0f, distance);
            for (var index = 1; index < points.Count; index++)
            {
                var start = points[index - 1];
                var finish = points[index];
                var length = Vector2.Distance(start, finish);
                direction = length > 0.001f ? (finish - start) / length : direction;
                if (remaining <= length)
                {
                    position = Vector2.Lerp(start, finish,
                        length <= 0.001f ? 0f : remaining / length);
                    return;
                }
                remaining -= length;
            }
            position = points[points.Count - 1];
        }

        private static Vector2Int Neighbor(int x, int z, RoadPiecePort port) => port switch
        {
            RoadPiecePort.North => new Vector2Int(x, z + 1),
            RoadPiecePort.East => new Vector2Int(x + 1, z),
            RoadPiecePort.South => new Vector2Int(x, z - 1),
            RoadPiecePort.West => new Vector2Int(x - 1, z),
            RoadPiecePort.NorthEast => new Vector2Int(x + 1, z + 1),
            RoadPiecePort.SouthEast => new Vector2Int(x + 1, z - 1),
            RoadPiecePort.SouthWest => new Vector2Int(x - 1, z - 1),
            _ => new Vector2Int(x - 1, z + 1)
        };
    }
}
