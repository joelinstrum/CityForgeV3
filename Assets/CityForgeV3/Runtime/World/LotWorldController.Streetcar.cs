using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public enum StreetcarTrackTopology
    {
        Straight,
        Curve
    }

    [Serializable]
    public sealed class PlacedStreetcarTrack
    {
        public string Id = "";
        public int GridX;
        public int GridZ;
        public int RotationQuarterTurns;
        public StreetcarTrackTopology Topology;
    }

    [Serializable]
    public sealed class PlacedStreetcarStop
    {
        public string Id = "";
        public int GridX;
        public int GridZ;
        public int Side = 1;
    }

    public sealed partial class LotWorldController
    {
        private const string StreetcarTrackResourceRoot =
            "CityForgeV3/Railroad/StreetcarTrackV01";
        private const string StreetcarModelResource =
            "CityForgeV3/Vehicles/StreetcarV01/tripo_convert_38aa7374-039c-4057-8b7b-f3342f3ddf60";
        private Transform _streetcarTrackRoot;
        private Transform _streetcarVehicleRoot;
        private Transform _streetcarStopRoot;
        private Transform _streetcarPassengerRoot;
        private Material _streetcarTrackMaterial;
        private readonly List<Vector3> _streetcarRoutePoints = new();
        private readonly List<float> _streetcarRouteDistances = new();
        private readonly List<StreetcarMotionState> _streetcarMotionStates = new();
        private readonly List<StreetcarStopState> _streetcarStopStates = new();
        private readonly List<BoardingPassengerState> _boardingPassengers = new();
        private float _streetcarRouteLength;
        private bool _streetcarRouteClosed;
        private const float StreetcarSpeedMetersPerSecond = 3.2f;
        private const float StreetcarStopDwellSeconds = 3.5f;
        private float _nextStreetcarPassengerArrival;
        private int _streetcarBoardedPassengerCount;

        private sealed class StreetcarMotionState
        {
            public Transform Transform;
            public float Distance;
            public int Direction = 1;
            public float DwellUntil;
            public int LastStopIndex = -1;
        }

        private sealed class StreetcarStopState
        {
            public PlacedStreetcarStop Data;
            public Transform QueueRoot;
            public Vector3 PlatformPosition;
            public float RouteDistance;
            public readonly List<Transform> WaitingPassengers = new();
        }

        private sealed class BoardingPassengerState
        {
            public Transform Transform;
            public Transform Streetcar;
            public Vector3 DoorWorldOffset;
        }

        public StreetcarTrackTopology SelectedStreetcarTrackTopology { get; private set; }
            = StreetcarTrackTopology.Straight;
        public int StreetcarRiderDemand => Mathf.Max(0,
            _session?.Data?.StreetcarRiderDemand ?? 0);
        public int ActiveStreetcarCount => _streetcarVehicleRoot == null
            ? 0 : _streetcarVehicleRoot.childCount;
        public int StreetcarStopCount => _session?.Data?.StreetcarStops?.Count ?? 0;
        public int StreetcarBoardedPassengerCount => _streetcarBoardedPassengerCount;

        private void BuildStreetcarTrackLayer()
        {
            _session.Data.StreetcarTracks ??= new List<PlacedStreetcarTrack>();
            _streetcarTrackRoot = new GameObject("Streetcar Track Overlay").transform;
            _streetcarTrackRoot.SetParent(transform, false);
            _streetcarVehicleRoot = new GameObject("Demand Streetcars").transform;
            _streetcarVehicleRoot.SetParent(transform, false);
            _streetcarStopRoot = new GameObject("Streetcar Stops").transform;
            _streetcarStopRoot.SetParent(transform, false);
            _streetcarPassengerRoot = new GameObject("Streetcar Stop Passengers").transform;
            _streetcarPassengerRoot.SetParent(transform, false);
            RebuildStreetcarTracks();
        }

        public bool PlaceStreetcarStop()
        {
            _session.Data.StreetcarStops ??= new List<PlacedStreetcarStop>();
            if (FindStreetcarTrackAt(RoadCursorCell.x, RoadCursorCell.y) == null)
                return false;
            var existing = _session.Data.StreetcarStops.Find(stop =>
                stop.GridX == RoadCursorCell.x && stop.GridZ == RoadCursorCell.y);
            if (existing == null)
            {
                existing = new PlacedStreetcarStop
                {
                    Id = $"streetcar-stop-{_session.Data.StreetcarStops.Count + 1}",
                    GridX = RoadCursorCell.x,
                    GridZ = RoadCursorCell.y
                };
                _session.Data.StreetcarStops.Add(existing);
            }
            else existing.Side *= -1;
            RebuildStreetcarVehicles();
            NotifyStateChanged();
            return true;
        }

        public bool DeleteStreetcarStop()
        {
            if (_session.Data.StreetcarStops == null) return false;
            var removed = _session.Data.StreetcarStops.RemoveAll(stop =>
                stop.GridX == RoadCursorCell.x && stop.GridZ == RoadCursorCell.y) > 0;
            if (!removed) return false;
            RebuildStreetcarVehicles();
            NotifyStateChanged();
            return true;
        }

        public void SelectStreetcarTrack(StreetcarTrackTopology topology)
        {
            SelectedStreetcarTrackTopology = topology;
            RoadCursorSelected = true;
            ApplyRoadCursor();
            NotifyStateChanged();
        }

        public void RotateStreetcarTrack()
        {
            RoadRotationQuarterTurns = FiveBayHybridContract.WrapFacing(
                RoadRotationQuarterTurns + 1);
            var placed = FindStreetcarTrackAt(RoadCursorCell.x, RoadCursorCell.y);
            if (placed != null)
            {
                placed.RotationQuarterTurns = RoadRotationQuarterTurns;
                RebuildStreetcarTracks();
            }
            NotifyStateChanged();
        }

        public bool PlaceStreetcarTrack()
        {
            // Track is an independent vertical layer but must be supported by
            // a road tile. Overlay textures never block it.
            if (RoadPlacementModel.FindAt(_session.Data.RoadPieces,
                    RoadCursorCell.x, RoadCursorCell.y) == null)
                return false;
            var placed = FindStreetcarTrackAt(RoadCursorCell.x, RoadCursorCell.y);
            if (placed == null)
            {
                placed = new PlacedStreetcarTrack
                {
                    Id = $"streetcar-track-{_session.Data.StreetcarTracks.Count + 1}",
                    GridX = RoadCursorCell.x,
                    GridZ = RoadCursorCell.y
                };
                _session.Data.StreetcarTracks.Add(placed);
            }
            placed.Topology = SelectedStreetcarTrackTopology;
            placed.RotationQuarterTurns = RoadRotationQuarterTurns;
            RepairStreetcarTrackTopologies();
            RebuildStreetcarTracks();
            NotifyStateChanged();
            return true;
        }

        public bool PaintStreetcarTrackStrokeCellFromPanel(
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!SelectRoadCellFromPanel(panelPosition, panelSize, false))
                return false;
            if (RoadPlacementModel.FindAt(_session.Data.RoadPieces,
                    RoadCursorCell.x, RoadCursorCell.y) == null)
                return false;
            var placed = FindStreetcarTrackAt(RoadCursorCell.x, RoadCursorCell.y);
            if (placed == null)
            {
                placed = new PlacedStreetcarTrack
                {
                    Id = $"streetcar-track-{_session.Data.StreetcarTracks.Count + 1}",
                    GridX = RoadCursorCell.x,
                    GridZ = RoadCursorCell.y,
                    Topology = SelectedStreetcarTrackTopology,
                    RotationQuarterTurns = RoadRotationQuarterTurns
                };
                _session.Data.StreetcarTracks.Add(placed);
            }
            RepairStreetcarTrackTopologies();
            RebuildStreetcarTracks();
            NotifyStateChanged();
            return true;
        }

        private void RepairStreetcarTrackTopologies()
        {
            foreach (var track in _session.Data.StreetcarTracks)
            {
                var north = FindStreetcarTrackAt(track.GridX, track.GridZ + 1) != null;
                var east = FindStreetcarTrackAt(track.GridX + 1, track.GridZ) != null;
                var south = FindStreetcarTrackAt(track.GridX, track.GridZ - 1) != null;
                var west = FindStreetcarTrackAt(track.GridX - 1, track.GridZ) != null;
                var count = (north ? 1 : 0) + (east ? 1 : 0) +
                    (south ? 1 : 0) + (west ? 1 : 0);
                if (count == 2 && north && south)
                {
                    track.Topology = StreetcarTrackTopology.Straight;
                    track.RotationQuarterTurns = 0;
                }
                else if (count == 2 && east && west)
                {
                    track.Topology = StreetcarTrackTopology.Straight;
                    track.RotationQuarterTurns = 1;
                }
                else if (count == 2)
                {
                    track.Topology = StreetcarTrackTopology.Curve;
                    // Authored curve ports are South + East at rotation zero.
                    track.RotationQuarterTurns = south && east ? 0 :
                        south && west ? 1 : north && west ? 2 : 3;
                }
                else if (count == 1)
                {
                    track.Topology = StreetcarTrackTopology.Straight;
                    track.RotationQuarterTurns = north || south ? 0 : 1;
                }
            }
        }

        public bool DeleteStreetcarTrack()
        {
            var removed = _session.Data.StreetcarTracks.RemoveAll(track =>
                track.GridX == RoadCursorCell.x && track.GridZ == RoadCursorCell.y) > 0;
            if (!removed) return false;
            _session.Data.StreetcarStops?.RemoveAll(stop =>
                stop.GridX == RoadCursorCell.x && stop.GridZ == RoadCursorCell.y);
            RepairStreetcarTrackTopologies();
            RebuildStreetcarTracks();
            NotifyStateChanged();
            return true;
        }

        public void AdjustStreetcarRiderDemand(int delta)
        {
            _session.Data.StreetcarRiderDemand = Mathf.Max(0,
                _session.Data.StreetcarRiderDemand + delta);
            RebuildStreetcarVehicles();
            NotifyStateChanged();
        }

        private PlacedStreetcarTrack FindStreetcarTrackAt(int gridX, int gridZ) =>
            _session.Data.StreetcarTracks.Find(track =>
                track.GridX == gridX && track.GridZ == gridZ);

        private void RebuildStreetcarTracks()
        {
            if (_streetcarTrackRoot == null) return;
            ClearChildren(_streetcarTrackRoot);
            foreach (var track in _session.Data.StreetcarTracks)
            {
                if (RoadPlacementModel.FindAt(_session.Data.RoadPieces,
                        track.GridX, track.GridZ) == null) continue;
                var texture = Resources.Load<Texture2D>(
                    $"{StreetcarTrackResourceRoot}/{track.Topology.ToString().ToLowerInvariant()}");
                if (texture == null) continue;
                var center = RoadPlacementModel.CellCenterMeters(track.GridX,
                    track.GridZ, LotWidthMeters, LotDepthMeters);
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Streetcar Track {track.Topology} {track.GridX}, {track.GridZ}";
                quad.transform.SetParent(_streetcarTrackRoot, false);
                // Above road artwork and every lot overlay, without occupying
                // either layer's placement slot.
                quad.transform.localPosition = new Vector3(center.x, 0.135f, center.y);
                quad.transform.localRotation = Quaternion.Euler(
                    90f, track.RotationQuarterTurns * 90f, 0f);
                quad.transform.localScale = new Vector3(10f, 10f, 1f);
                quad.GetComponent<Collider>().enabled = false;
                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = StreetcarTrackMaterial();
                renderer.material.mainTexture = texture;
                renderer.sortingOrder = 2100;
            }
            RebuildStreetcarVehicles();
        }

        private Material StreetcarTrackMaterial()
        {
            if (_streetcarTrackMaterial != null) return _streetcarTrackMaterial;
            var shader = Shader.Find("Unlit/Transparent") ??
                Shader.Find("Sprites/Default");
            _streetcarTrackMaterial = new Material(shader)
            {
                name = "CF Streetcar Track Overlay",
                renderQueue = 3100
            };
            _streetcarTrackMaterial.SetInt("_ZWrite", 0);
            return _streetcarTrackMaterial;
        }

        private void RebuildStreetcarVehicles()
        {
            if (_streetcarVehicleRoot == null) return;
            ClearChildren(_streetcarVehicleRoot);
            _streetcarMotionStates.Clear();
            _boardingPassengers.Clear();
            BuildStreetcarRoute();
            RebuildStreetcarStops();
            var prefab = Resources.Load<GameObject>(StreetcarModelResource);
            if (prefab == null || _streetcarRoutePoints.Count < 2 ||
                _streetcarRouteLength <= 0.001f) return;
            var desired = Mathf.Min(_session.Data.StreetcarTracks.Count,
                Mathf.CeilToInt(StreetcarRiderDemand / 40f));
            for (var index = 0; index < desired; index++)
            {
                var streetcar = Instantiate(prefab, _streetcarVehicleRoot);
                streetcar.name = $"Demand Streetcar {index + 1}";
                NormalizeStreetcar(streetcar);
                var motion = new StreetcarMotionState
                {
                    Transform = streetcar.transform,
                    Distance = _streetcarRouteLength * index / Mathf.Max(1, desired)
                };
                _streetcarMotionStates.Add(motion);
                PlaceStreetcarOnRoute(motion, true);
                // The source FBX includes tall trolley hardware. When a car is
                // just outside the orthographic camera, that hidden geometry
                // can project long disconnected bars across the visible lot.
                // Streetcars use their authored presentation without native
                // mesh shadows until a bounded contact-shadow proxy is added.
                foreach (var renderer in streetcar.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                var directionalShadow = streetcar.AddComponent<
                    StreetVehicleGroundShadow>();
                directionalShadow.Initialize(streetcar.transform);
                directionalShadow.SetLighting(
                    ExperimentalBuilding3DSunRotation() * Vector3.forward,
                    !IsRaining && TimeOfDay != TimeOfDayPreset.Night);
            }
        }

        private void UpdateStreetcarShadowLighting()
        {
            if (_streetcarVehicleRoot == null) return;
            var ray = ExperimentalBuilding3DSunRotation() * Vector3.forward;
            var visible = !IsRaining && TimeOfDay != TimeOfDayPreset.Night;
            foreach (var shadow in _streetcarVehicleRoot.GetComponentsInChildren<
                         StreetVehicleGroundShadow>(true))
                shadow.SetLighting(ray, visible);
        }

        private void RebuildStreetcarStops()
        {
            _streetcarStopStates.Clear();
            if (_streetcarStopRoot == null || _streetcarPassengerRoot == null) return;
            ClearChildren(_streetcarStopRoot);
            ClearChildren(_streetcarPassengerRoot);
            foreach (var stop in _session.Data.StreetcarStops ?? new List<PlacedStreetcarStop>())
            {
                var track = FindStreetcarTrackAt(stop.GridX, stop.GridZ);
                if (track == null || _streetcarRoutePoints.Count < 2) continue;
                var center2 = RoadPlacementModel.CellCenterMeters(stop.GridX,
                    stop.GridZ, LotWidthMeters, LotDepthMeters);
                var along = track.RotationQuarterTurns % 2 == 0
                    ? Vector3.forward : Vector3.right;
                var side = Vector3.Cross(Vector3.up, along).normalized * stop.Side;
                var platformPosition = new Vector3(center2.x, 0f, center2.y) + side * 3.7f;
                var root = new GameObject($"Streetcar Stop {stop.Id}").transform;
                root.SetParent(_streetcarStopRoot, false);
                root.localPosition = platformPosition;
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Stop Pole";
                pole.transform.SetParent(root, false);
                pole.transform.localPosition = Vector3.up * 1.15f;
                pole.transform.localScale = new Vector3(0.09f, 1.15f, 0.09f);
                pole.GetComponent<Renderer>().material.color = new Color(0.12f, 0.15f, 0.17f);
                var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sign.name = "Streetcar Stop Sign";
                sign.transform.SetParent(root, false);
                sign.transform.localPosition = Vector3.up * 2.15f;
                sign.transform.localScale = new Vector3(0.65f, 0.48f, 0.10f);
                sign.GetComponent<Renderer>().material.color = new Color(0.88f, 0.68f, 0.16f);
                foreach (var collider in root.GetComponentsInChildren<Collider>())
                    collider.enabled = false;

                var nearestIndex = 0;
                var nearest = float.MaxValue;
                var trackCenter = new Vector3(center2.x, 0.22f, center2.y);
                for (var index = 0; index < _streetcarRoutePoints.Count; index++)
                {
                    var distance = (_streetcarRoutePoints[index] - trackCenter).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance;
                    nearestIndex = index;
                }
                _streetcarStopStates.Add(new StreetcarStopState
                {
                    Data = stop,
                    QueueRoot = root,
                    PlatformPosition = platformPosition,
                    RouteDistance = _streetcarRouteDistances[Mathf.Min(nearestIndex,
                        _streetcarRouteDistances.Count - 1)]
                });
            }
            _nextStreetcarPassengerArrival = Time.time + 0.5f;
        }

        private void BuildStreetcarRoute()
        {
            _streetcarRoutePoints.Clear();
            _streetcarRouteDistances.Clear();
            _streetcarRouteLength = 0f;
            _streetcarRouteClosed = false;
            if (_session?.Data?.StreetcarTracks == null ||
                _session.Data.StreetcarTracks.Count < 2) return;

            var tracks = new Dictionary<Vector2Int, PlacedStreetcarTrack>();
            foreach (var track in _session.Data.StreetcarTracks)
                tracks[new Vector2Int(track.GridX, track.GridZ)] = track;

            var directions = new[]
            {
                Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
            };
            var start = default(Vector2Int);
            var hasStart = false;
            foreach (var cell in tracks.Keys)
            {
                if (!hasStart)
                {
                    start = cell;
                    hasStart = true;
                }
                if (ConnectedStreetcarNeighbors(cell, tracks, directions).Count == 1)
                {
                    start = cell;
                    break;
                }
            }
            if (!hasStart) return;

            var ordered = new List<Vector2Int>();
            var previous = new Vector2Int(int.MinValue, int.MinValue);
            var current = start;
            while (ordered.Count <= tracks.Count)
            {
                ordered.Add(current);
                var neighbors = ConnectedStreetcarNeighbors(current, tracks, directions);
                Vector2Int? next = null;
                foreach (var neighbor in neighbors)
                {
                    if (neighbor == previous) continue;
                    next = neighbor;
                    break;
                }
                if (!next.HasValue) break;
                if (next.Value == start)
                {
                    _streetcarRouteClosed = ordered.Count == tracks.Count;
                    break;
                }
                previous = current;
                current = next.Value;
            }

            if (_streetcarRouteClosed)
            {
                for (var index = 0; index < ordered.Count; index++)
                {
                    var cell = ordered[index];
                    var prior = ordered[(index - 1 + ordered.Count) % ordered.Count];
                    var next = ordered[(index + 1) % ordered.Count];
                    AppendStreetcarTilePath(cell, prior - cell, next - cell);
                }
            }
            else
            {
                // Until stops are introduced, an incomplete line shuttles
                // smoothly between tile centers instead of leaving the track.
                foreach (var cell in ordered)
                {
                    var center = RoadPlacementModel.CellCenterMeters(cell.x,
                        cell.y, LotWidthMeters, LotDepthMeters);
                    AppendStreetcarRoutePoint(new Vector3(center.x, 0.22f, center.y));
                }
            }

            _streetcarRouteDistances.Clear();
            _streetcarRouteDistances.Add(0f);
            for (var index = 1; index < _streetcarRoutePoints.Count; index++)
            {
                _streetcarRouteLength += Vector3.Distance(
                    _streetcarRoutePoints[index - 1], _streetcarRoutePoints[index]);
                _streetcarRouteDistances.Add(_streetcarRouteLength);
            }
        }

        private static List<Vector2Int> ConnectedStreetcarNeighbors(
            Vector2Int cell,
            Dictionary<Vector2Int, PlacedStreetcarTrack> tracks,
            IReadOnlyList<Vector2Int> directions)
        {
            var neighbors = new List<Vector2Int>(2);
            foreach (var direction in directions)
            {
                var candidate = cell + direction;
                if (tracks.ContainsKey(candidate)) neighbors.Add(candidate);
            }
            return neighbors;
        }

        private void AppendStreetcarTilePath(Vector2Int cell,
            Vector2Int entryDirection, Vector2Int exitDirection)
        {
            var center2 = RoadPlacementModel.CellCenterMeters(cell.x, cell.y,
                LotWidthMeters, LotDepthMeters);
            var center = new Vector3(center2.x, 0.22f, center2.y);
            const float halfTile = 5f;
            var entry = center + new Vector3(entryDirection.x, 0f,
                entryDirection.y) * halfTile;
            var exit = center + new Vector3(exitDirection.x, 0f,
                exitDirection.y) * halfTile;

            if (entryDirection + exitDirection == Vector2Int.zero)
            {
                for (var step = 0; step <= 4; step++)
                    AppendStreetcarRoutePoint(Vector3.Lerp(entry, exit, step / 4f));
                return;
            }

            var corner = center + new Vector3(
                entryDirection.x + exitDirection.x, 0f,
                entryDirection.y + exitDirection.y) * halfTile;
            var startVector = entry - corner;
            var endVector = exit - corner;
            var startAngle = Mathf.Atan2(startVector.z, startVector.x) * Mathf.Rad2Deg;
            var endAngle = Mathf.Atan2(endVector.z, endVector.x) * Mathf.Rad2Deg;
            var sweep = Mathf.DeltaAngle(startAngle, endAngle);
            const int curveSteps = 12;
            for (var step = 0; step <= curveSteps; step++)
            {
                var angle = (startAngle + sweep * step / curveSteps) * Mathf.Deg2Rad;
                AppendStreetcarRoutePoint(corner + new Vector3(
                    Mathf.Cos(angle) * halfTile, 0f,
                    Mathf.Sin(angle) * halfTile));
            }
        }

        private void AppendStreetcarRoutePoint(Vector3 point)
        {
            if (_streetcarRoutePoints.Count > 0 &&
                Vector3.SqrMagnitude(_streetcarRoutePoints[^1] - point) < 0.0001f)
                return;
            _streetcarRoutePoints.Add(point);
        }

        private void UpdateStreetcars()
        {
            UpdateStreetcarStopsAndPassengers();
            if (_streetcarMotionStates.Count == 0 ||
                _streetcarRouteLength <= 0.001f) return;
            foreach (var motion in _streetcarMotionStates)
            {
                if (motion.Transform == null) continue;
                if (Time.time < motion.DwellUntil)
                {
                    PlaceStreetcarOnRoute(motion, false);
                    continue;
                }
                var previousDistance = motion.Distance;
                motion.Distance += StreetcarSpeedMetersPerSecond *
                    Time.deltaTime * motion.Direction;
                if (_streetcarRouteClosed)
                    motion.Distance = Mathf.Repeat(motion.Distance, _streetcarRouteLength);
                else if (motion.Distance >= _streetcarRouteLength || motion.Distance <= 0f)
                {
                    motion.Distance = Mathf.Clamp(motion.Distance, 0f, _streetcarRouteLength);
                    motion.Direction *= -1;
                }
                for (var stopIndex = 0; stopIndex < _streetcarStopStates.Count; stopIndex++)
                {
                    if (stopIndex == motion.LastStopIndex) continue;
                    var stopDistance = _streetcarStopStates[stopIndex].RouteDistance;
                    if (!CrossedRouteDistance(previousDistance, motion.Distance,
                            stopDistance, motion.Direction)) continue;
                    motion.Distance = stopDistance;
                    motion.DwellUntil = Time.time + StreetcarStopDwellSeconds;
                    motion.LastStopIndex = stopIndex;
                    BeginBoarding(_streetcarStopStates[stopIndex], motion);
                    break;
                }
                if (motion.LastStopIndex >= 0 &&
                    RouteDistanceApart(motion.Distance,
                        _streetcarStopStates[motion.LastStopIndex].RouteDistance) > 3f)
                    motion.LastStopIndex = -1;
                PlaceStreetcarOnRoute(motion, false);
            }
        }

        private bool CrossedRouteDistance(float before, float after,
            float target, int direction)
        {
            if (direction < 0) return target <= before && target >= after;
            if (!_streetcarRouteClosed || after >= before)
                return target >= before && target <= after;
            return target >= before || target <= after;
        }

        private float RouteDistanceApart(float left, float right)
        {
            var distance = Mathf.Abs(left - right);
            return _streetcarRouteClosed
                ? Mathf.Min(distance, _streetcarRouteLength - distance)
                : distance;
        }

        private void UpdateStreetcarStopsAndPassengers()
        {
            if (_streetcarStopStates.Count > 0 && Time.time >= _nextStreetcarPassengerArrival)
            {
                _nextStreetcarPassengerArrival = Time.time + 2.25f;
                var capacity = Mathf.Clamp(Mathf.CeilToInt(StreetcarRiderDemand / 10f), 1, 8);
                foreach (var stop in _streetcarStopStates)
                    if (stop.WaitingPassengers.Count < capacity)
                        SpawnWaitingPassenger(stop);
            }
            for (var index = _boardingPassengers.Count - 1; index >= 0; index--)
            {
                var passenger = _boardingPassengers[index];
                if (passenger.Transform == null || passenger.Streetcar == null)
                {
                    _boardingPassengers.RemoveAt(index);
                    continue;
                }
                var target = passenger.Streetcar.position + passenger.DoorWorldOffset;
                target.y = 0f;
                var delta = target - passenger.Transform.position;
                if (delta.sqrMagnitude <= 0.20f)
                {
                    Destroy(passenger.Transform.gameObject);
                    _boardingPassengers.RemoveAt(index);
                    _streetcarBoardedPassengerCount++;
                    continue;
                }
                passenger.Transform.rotation = Quaternion.Slerp(
                    passenger.Transform.rotation,
                    Quaternion.LookRotation(delta.normalized, Vector3.up),
                    1f - Mathf.Exp(-10f * Time.deltaTime));
                passenger.Transform.position = Vector3.MoveTowards(
                    passenger.Transform.position, target, 1.7f * Time.deltaTime);
            }
        }

        private void SpawnWaitingPassenger(StreetcarStopState stop)
        {
            var root = CreatePropPresentation(VictorianGentlemanCharacterId,
                $"Waiting Passenger {stop.WaitingPassengers.Count + 1}", 1f);
            if (root == null) return;
            root.SetParent(_streetcarPassengerRoot, false);
            var queueIndex = stop.WaitingPassengers.Count;
            root.position = stop.PlatformPosition + new Vector3(
                (queueIndex % 2) * 0.65f, 0f, (queueIndex / 2) * 0.72f);
            root.GetComponent<ThreeDimensionalCharacterAnimator>()?.Play("wait");
            foreach (var collider in root.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            stop.WaitingPassengers.Add(root);
        }

        private void BeginBoarding(StreetcarStopState stop, StreetcarMotionState motion)
        {
            _nextStreetcarPassengerArrival = Mathf.Max(
                _nextStreetcarPassengerArrival, Time.time + 6f);
            SampleStreetcarRoute(motion.Distance, out _, out var tangent);
            for (var index = 0; index < stop.WaitingPassengers.Count; index++)
            {
                var passenger = stop.WaitingPassengers[index];
                if (passenger == null) continue;
                passenger.GetComponent<ThreeDimensionalCharacterAnimator>()?.Play("walk");
                _boardingPassengers.Add(new BoardingPassengerState
                {
                    Transform = passenger,
                    Streetcar = motion.Transform,
                    // Alternate between the streetcar's front and rear entry
                    // doors instead of converging on the vehicle midpoint.
                    DoorWorldOffset = tangent * (index % 2 == 0 ? 2.35f : -2.35f)
                });
            }
            stop.WaitingPassengers.Clear();
        }

        private void PlaceStreetcarOnRoute(StreetcarMotionState motion, bool immediate)
        {
            SampleStreetcarRoute(motion.Distance, out var position, out var tangent);
            if (!_streetcarRouteClosed && motion.Direction < 0) tangent = -tangent;
            motion.Transform.localPosition = position;
            var yaw = Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg;
            var targetRotation = Quaternion.Euler(-90f, yaw + 90f, 0f);
            motion.Transform.localRotation = immediate
                ? targetRotation
                : Quaternion.Slerp(motion.Transform.localRotation, targetRotation,
                    1f - Mathf.Exp(-9f * Time.deltaTime));
        }

        private void SampleStreetcarRoute(float distance,
            out Vector3 position, out Vector3 tangent)
        {
            distance = Mathf.Clamp(distance, 0f, _streetcarRouteLength);
            var segment = 1;
            while (segment < _streetcarRouteDistances.Count - 1 &&
                _streetcarRouteDistances[segment] < distance)
                segment++;
            var startDistance = _streetcarRouteDistances[segment - 1];
            var endDistance = _streetcarRouteDistances[segment];
            var amount = Mathf.InverseLerp(startDistance, endDistance, distance);
            var start = _streetcarRoutePoints[segment - 1];
            var end = _streetcarRoutePoints[segment];
            position = Vector3.Lerp(start, end, amount);
            tangent = (end - start).normalized;
        }

        private static void NormalizeStreetcar(GameObject streetcar)
        {
            var renderers = streetcar.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            var footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint <= 0.001f) return;
            var scale = 7.5f / footprint;
            streetcar.transform.localScale *= scale;
        }

    }
}
