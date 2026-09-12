using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private sealed class AnimalOrder
        {
            public List<Vector2> Path;
            public int Next;
            public Vector2 Destination;
            public bool Arrived;
        }
        private readonly Dictionary<string, AnimalOrder> _animalOrders = new();
        private readonly List<string> _expiredAnimalOrders = new();
        private LineRenderer _animalOrderArrow;
        private float _animalArrowUntil;
        public bool SelectedPropIsAnimal => SelectedPropIndex >= 0 && SelectedPropIndex < PropCount &&
            IsThreeDimensionalAnimal(_session.Data.Props[SelectedPropIndex].PropId);
        public string SelectedAnimalName => SelectedPropIsCarriageTeam ? SelectedHorseWagonName : SelectedPropIsAnimal && IsMountedRider(_session.Data.Props[SelectedPropIndex].PropId) ? "Horse and rider" : SelectedPropIsAnimal && IsHorse(_session.Data.Props[SelectedPropIndex].PropId) ? "Horse" : "Bear";

        public bool TryAnimalDestinationFromPanel(Vector2 panelPosition, Vector2 panelSize, out string status)
        {
            status = "";
            if (!SelectedPropCanWalk || ActiveObjectSelection != LotObjectSelectionKind.Prop ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            var pixel = PanelToCameraPixel(panelPosition, panelSize, new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            // Existing objects keep their normal selection priority.
            if (PropIndexAtCameraPixel(pixel) >= 0 || FloraIndexAtCameraPixel(pixel) >= 0 ||
                FindBuildingHitIndex(pixel, new Vector2(point.x, point.z)) >= 0 ||
                BuildingPropPresentationIndexAtCameraPixel(pixel) >= 0) return false;
            var accepted = CommandSelectedAnimal(new Vector2(point.x, point.z));
            status = accepted ? SelectedAnimalName + " destination set" : "No clear walking route to that spot";
            return true;
        }

        public bool CommandSelectedAnimal(Vector2 destination)
        {
            if (SelectedPropIsCarriageTeam) return CommandSelectedCarriage(destination);
            if (!SelectedPropIsAnimal) return false;
            var prop = _session.Data.Props[SelectedPropIndex];
            if (string.IsNullOrEmpty(prop.InstanceId)) prop.InstanceId = System.Guid.NewGuid().ToString("N");
            var from = new Vector2(prop.PositionX, prop.PositionZ);
            RebuildBearObstacles();
            var path = FindAnimalRoute(from, destination);
            ShowAnimalDestinationArrow(from, destination, path != null);
            if (path == null) return false;
            _animalOrders[prop.InstanceId] = new AnimalOrder { Path = path, Destination = destination };
            _propDragActive = false;
            return true;
        }

        private bool HasAnimalOrder(string id) => !string.IsNullOrEmpty(id) && _animalOrders.ContainsKey(id);

        private static bool SegmentCrossesRect(Vector2 from, Vector2 to, Rect rect)
        {
            var delta = to - from; var enter = 0f; var exit = 1f;
            for (var axis = 0; axis < 2; axis++)
            {
                var origin = from[axis]; var direction = delta[axis];
                var min = axis == 0 ? rect.xMin : rect.yMin;
                var max = axis == 0 ? rect.xMax : rect.yMax;
                if (Mathf.Abs(direction) < 0.000001f)
                { if (origin < min || origin > max) return false; continue; }
                var a = (min-origin)/direction; var b = (max-origin)/direction;
                enter = Mathf.Max(enter, Mathf.Min(a,b)); exit = Mathf.Min(exit, Mathf.Max(a,b));
                if (enter > exit) return false;
            }
            return true;
        }

        private bool AnimalSegmentClear(Vector2 from, Vector2 to)
        {
            // Continuous intersection avoids cutting across a thin obstacle
            // corner between terrain samples. Include room for the whole horse.
            const float clearance = 1.55f;
            if (Mathf.Abs(to.x) > LotWidthMeters * 0.5f-clearance ||
                Mathf.Abs(to.y) > LotDepthMeters * 0.5f-clearance) return false;
            foreach (var obstacle in _bearObstacles)
            {
                var bounds = Rect.MinMaxRect(obstacle.xMin-clearance, obstacle.yMin-clearance,
                    obstacle.xMax+clearance, obstacle.yMax+clearance);
                if (!SegmentCrossesRect(from,to,bounds)) continue;
                if (!bounds.Contains(from) || Vector2.Dot(to-from,from-bounds.center) <= 0f) return false;
            }
            var steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from,to)/0.2f));
            var previous = from;
            for (var i=1;i<=steps;i++)
            {
                var p = Vector2.Lerp(from,to,(float)i/steps);
                if (Mathf.Abs(SampleTerrainHeight(p.x,p.y)-SampleTerrainHeight(previous.x,previous.y)) >
                    Vector2.Distance(previous,p)*0.75f+0.01f) return false;
                previous = p;
            }
            return true;
        }

        private List<Vector2> FindAnimalRoute(Vector2 from, Vector2 goal)
        {
            if (!AnimalSegmentClear(goal, goal)) return null;
            if (AnimalSegmentClear(from, goal)) return new List<Vector2> { goal };
            const float cell = 0.75f;
            var open = new List<Vector2Int> { Vector2Int.zero };
            var closed = new HashSet<Vector2Int>();
            var costs = new Dictionary<Vector2Int, float> { [Vector2Int.zero] = 0f };
            var parents = new Dictionary<Vector2Int, Vector2Int>();
            for (var iteration = 0; iteration < 6000 && open.Count > 0; iteration++)
            {
                var best = 0; var score = float.PositiveInfinity;
                for (var i = 0; i < open.Count; i++)
                {
                    var f = costs[open[i]] + Vector2.Distance(from + (Vector2)open[i] * cell, goal);
                    if (f < score) { score = f; best = i; }
                }
                var node = open[best]; open.RemoveAt(best); closed.Add(node);
                var pos = from + (Vector2)node * cell;
                if (Vector2.Distance(pos, goal) < cell * 1.5f && AnimalSegmentClear(pos, goal))
                {
                    var route = new List<Vector2> { goal, pos };
                    while (parents.TryGetValue(node, out var parent))
                    { node = parent; route.Add(from + (Vector2)node * cell); }
                    route.Reverse();
                    // Collapse grid corners whenever a clear straight segment exists.
                    var smooth = new List<Vector2>(); var anchor = from; var cursor = 1;
                    while (cursor < route.Count)
                    {
                        var far = cursor;
                        while (far + 1 < route.Count && AnimalSegmentClear(anchor, route[far + 1])) far++;
                        smooth.Add(route[far]); anchor = route[far]; cursor = far + 1;
                    }
                    return smooth;
                }
                for (var x = -1; x <= 1; x++) for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    var next = node + new Vector2Int(x, y);
                    if (closed.Contains(next)) continue;
                    var target = from + (Vector2)next * cell;
                    if (!AnimalSegmentClear(pos, target)) continue;
                    var g = costs[node] + Vector2.Distance(pos, target);
                    if (costs.TryGetValue(next, out var old) && old <= g) continue;
                    costs[next] = g; parents[next] = node;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            return null;
        }

        private void UpdateAnimalOrders()
        {
            if (_animalOrderArrow != null)
            {
                var alpha = Mathf.Clamp01((_animalArrowUntil - Time.time) / 0.5f);
                _animalOrderArrow.enabled = alpha > 0f;
                var color = _animalOrderArrow.startColor; color.a = alpha;
                _animalOrderArrow.startColor = _animalOrderArrow.endColor = color;
            }
            var props = _session?.Data?.Props;
            if (props == null) { _animalOrders.Clear(); return; }
            if (_animalOrders.Count == 0) return;
            if (Time.time >= _bearObstacleRefresh)
            { RebuildBearObstacles(); _bearObstacleRefresh = Time.time + 0.5f; }
            _expiredAnimalOrders.Clear();
            foreach (var id in _animalOrders.Keys)
                if (!props.Exists(p => p.InstanceId == id)) _expiredAnimalOrders.Add(id);
            foreach (var id in _expiredAnimalOrders) _animalOrders.Remove(id);
            for (var i = 0; i < props.Count && i < _propPresentations.Count; i++)
            {
                var prop = props[i];
                if (string.IsNullOrEmpty(prop.InstanceId) || !_animalOrders.TryGetValue(prop.InstanceId, out var order)) continue;
                var root = _propPresentations[i];
                if (root == null || !root.gameObject.activeInHierarchy || _propDragActive && i == SelectedPropIndex) continue;
                var pos = new Vector2(prop.PositionX, prop.PositionZ);
                if (!IsHorse(prop.PropId) && BearThreatScore(pos, false, out _, out _) > 0f)
                { _animalOrders.Remove(prop.InstanceId); continue; }
                var player = root.GetComponent<ThreeDimensionalCharacterAnimator>();
                if (player == null) continue;
                if (order.Arrived)
                { player.Play("idle"); prop.AnimationState = "idle"; prop.MovementX = prop.MovementZ = 0; continue; }
                while (order.Next < order.Path.Count && Vector2.Distance(pos, order.Path[order.Next]) < 0.001f) order.Next++;
                if (order.Next >= order.Path.Count)
                { order.Arrived = true; player.Play("idle"); prop.AnimationState = "idle"; prop.MovementX = prop.MovementZ = 0; NotifyStateChanged(); continue; }
                var target = order.Path[order.Next];
                var direction = (target - pos).normalized;
                var speed = IsHorse(prop.PropId) ? HorseGaitController.WalkMetersPerSecond : BearBehavior.WalkSpeed;
                if (!player.Play("walk")) continue;
                prop.AnimationState = "walk";
                player.SetPlaybackSpeed(IsHorse(prop.PropId) ? 1f : speed / 0.145f);
                var rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));
                root.localRotation = Quaternion.RotateTowards(root.localRotation, rotation, 50f * Mathf.Min(Time.deltaTime, 0.05f));
                prop.MovementX = prop.MovementZ = 0;
                if (Quaternion.Angle(root.localRotation, rotation) > 12f) continue;
                var next = Vector2.MoveTowards(pos, target, speed * Mathf.Min(Time.deltaTime, 0.05f));
                if (!AnimalSegmentClear(pos, next))
                {
                    var route = FindAnimalRoute(pos, order.Destination);
                    if (route == null) { order.Arrived = true; player.Play("idle"); prop.AnimationState = "idle"; }
                    else { order.Path = route; order.Next = 0; }
                    continue;
                }
                prop.PositionX = next.x; prop.PositionZ = next.y;
                prop.MovementX = direction.x; prop.MovementZ = direction.y;
                root.localPosition = new Vector3(next.x, CharacterGroundY(prop.PropId) + SampleTerrainHeight(next.x, next.y), next.y);
                if (i == SelectedPropIndex) ApplyPropSelection();
            }
        }

        private void ShowAnimalDestinationArrow(Vector2 from, Vector2 target, bool valid)
        {
            if (_animalOrderArrow == null)
            {
                var go = new GameObject("Animal Destination Arrow"); go.transform.SetParent(_propRoot, false);
                _animalOrderArrow = go.AddComponent<LineRenderer>();
                _animalOrderArrow.useWorldSpace = false; _animalOrderArrow.positionCount = 5;
                _animalOrderArrow.widthMultiplier = 0.075f;
                _animalOrderArrow.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                go.AddComponent<CharacterShadowMaterialOwner>().Add(_animalOrderArrow.sharedMaterial);
                _animalOrderArrow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _animalOrderArrow.receiveShadows = false;
            }
            var direction = (target - from).normalized;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;
            var side = new Vector2(-direction.y, direction.x);
            var points = new[] { target - direction * 0.9f, target, target - direction * 0.3f + side * 0.22f,
                target, target - direction * 0.3f - side * 0.22f };
            for (var i = 0; i < points.Length; i++)
                _animalOrderArrow.SetPosition(i, new Vector3(points[i].x, SampleTerrainHeight(points[i].x, points[i].y) + 0.12f, points[i].y));
            _animalOrderArrow.startColor = _animalOrderArrow.endColor = valid ? new Color(0.4f, 1f, 0.7f) : new Color(1f, 0.4f, 0.25f);
            _animalOrderArrow.enabled = true; _animalArrowUntil = Time.time + 1.6f;
        }
    }
}
