using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private readonly List<Rect> _bearObstacles = new();
        private float _bearObstacleRefresh;
        private const float BearClearance = 1.05f;

        private void UpdateBears()
        {
            var props = _session?.Data?.Props;
            if (props == null || _propRoot == null) return;
            var hasBears = props.Exists(p => p != null && p.PropId == BearAnimalId);
            if (!hasBears) return;
            if (Time.time >= _bearObstacleRefresh)
            {
                RebuildBearObstacles();
                _bearObstacleRefresh = Time.time + 0.5f;
            }
            for (var i = 0; i < props.Count && i < _propPresentations.Count; i++)
            {
                var bear = props[i];
                if (bear == null || bear.PropId != BearAnimalId || HasAnimalOrder(bear.InstanceId)) continue;
                var root = _propPresentations[i];
                if (root == null || !root.gameObject.activeInHierarchy ||
                    (_propDragActive && i == SelectedPropIndex)) continue;
                var agent = root.GetComponent<BearRoamingAgent>() ??
                    root.gameObject.AddComponent<BearRoamingAgent>();
                var player = root.GetComponent<ThreeDimensionalCharacterAnimator>();
                if (player == null) continue;
                var pos = new Vector2(bear.PositionX, bear.PositionZ);
                var fleeing = agent.Behavior.Mode == BearMode.Retreating;
                var danger = BearThreatScore(pos, fleeing, out var away, out var threatId) > 0f;
                var previous = agent.Behavior.Mode;
                agent.Behavior.Tick(Time.time, danger, Random.value);
                agent.Mode = agent.Behavior.Mode;
                agent.ThreatDetected = danger;
                agent.ThreatId = threatId;
                var desiredState = agent.Behavior.Animation;
                if (!player.Play(desiredState)) continue;
                bear.AnimationState = desiredState;
                var moving = agent.Mode != BearMode.Looking;
                if (moving && (Time.time >= agent.NextSteer || previous != agent.Mode ||
                               agent.Direction.sqrMagnitude < 0.01f))
                {
                    var desired = danger ? away : agent.Direction;
                    if (desired.sqrMagnitude < 0.01f || previous != agent.Mode && !danger)
                        desired = Random.insideUnitCircle.normalized;
                    agent.Direction = FindBearDirection(pos, desired, danger, fleeing);
                    agent.NextSteer = Time.time + 0.3f;
                }
                var direction = agent.Direction;
                var step = agent.Behavior.Speed * Mathf.Min(Time.deltaTime, 0.05f);
                var next = pos + direction * step;
                if (!moving || direction.sqrMagnitude < 0.01f || !BearStepClear(pos, next))
                {
                    bear.MovementX = bear.MovementZ = 0f;
                    // No foot cycling while physically blocked.
                    if (moving) { player.Play("idle"); bear.AnimationState = "idle"; agent.NextSteer = 0f; }
                    player.SetPlaybackSpeed(1f);
                }
                else
                {
                    // Keep the gait running while stepping around to face travel.
                    // Switching to idle here planted every paw during body rotation.
                    player.SetPlaybackSpeed(agent.Behavior.Speed / 0.145f);
                    var targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
                    root.localRotation = Quaternion.RotateTowards(root.localRotation, targetRotation,
                        50f * Mathf.Min(Time.deltaTime, 0.05f));
                    if (Quaternion.Angle(root.localRotation, targetRotation) > 15f)
                    {
                        bear.MovementX = bear.MovementZ = 0f;
                        if (i == SelectedPropIndex) ApplyPropSelection();
                        continue;
                    }
                    bear.MovementX = direction.x; bear.MovementZ = direction.y;
                    bear.PositionX = next.x; bear.PositionZ = next.y;
                    root.localPosition = new Vector3(next.x,
                        CharacterGroundY(bear.PropId) + SampleTerrainHeight(next.x, next.y), next.y);
                    if (i == SelectedPropIndex) ApplyPropSelection();
                }
            }
        }

        private float BearThreatScore(Vector2 pos, bool retreating,
            out Vector2 away, out string id)
        {
            away = Vector2.zero; id = ""; var score = 0f;
            foreach (var person in _session.Data.Props)
            {
                if (person == null || !BearBehavior.FearsPerson(person.PropId)) continue;
                AddThreat(pos, new Vector2(person.PositionX, person.PositionZ),
                    BearBehavior.ThreatRadius(false, retreating), person.PropId, ref score, ref away, ref id);
            }
            foreach (var building in _session.Data.Buildings3D ?? new List<PlacedBuilding3D>())
            {
                if (building == null || !BearBehavior.FearsBuilding(building.AssetId)) continue;
                AddThreat(pos, new Vector2(building.X, building.Z),
                    BearBehavior.ThreatRadius(true, retreating), building.AssetId, ref score, ref away, ref id);
            }
            if (away.sqrMagnitude < 0.001f && score > 0f) away = Vector2.right;
            return score;
        }

        private static void AddThreat(Vector2 pos, Vector2 threat, float radius, string threatId,
            ref float score, ref Vector2 away, ref string id)
        {
            var delta = pos - threat; var distance = delta.magnitude;
            if (distance >= radius) return;
            var weight = 1f - distance / radius;
            score += weight;
            away += (distance > 0.001f ? delta / distance : Vector2.right) * weight;
            id = threatId;
        }

        private Vector2 FindBearDirection(Vector2 pos, Vector2 desired, bool danger, bool retreating)
        {
            desired.Normalize(); var best = Vector2.zero; var bestScore = float.NegativeInfinity;
            var initial = BearThreatScore(pos, retreating, out _, out _);
            for (var j = 0; j < 24; j++)
            {
                var angle = j * Mathf.PI / 12f;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var target = pos + dir * 0.65f;
                if (!BearStepClear(pos, target)) continue;
                var score = Vector2.Dot(desired, dir);
                if (danger)
                    score += (initial - BearThreatScore(target, retreating, out _, out _)) * 80f;
                if (score <= bestScore) continue;
                bestScore = score; best = dir;
            }
            return best;
        }

        private bool BearStepClear(Vector2 from, Vector2 target)
        {
            var halfX = Mathf.Max(0f, LotWidthMeters * 0.5f - BearClearance);
            var halfZ = Mathf.Max(0f, LotDepthMeters * 0.5f - BearClearance);
            if (Mathf.Abs(target.x) > halfX || Mathf.Abs(target.y) > halfZ) return false;
            if (Mathf.Abs(SampleTerrainHeight(target.x, target.y) -
                          SampleTerrainHeight(from.x, from.y)) > Vector2.Distance(from, target) * 0.75f + 0.01f)
                return false;
            foreach (var obstacle in _bearObstacles)
            {
                var expanded = Rect.MinMaxRect(obstacle.xMin - BearClearance,
                    obstacle.yMin - BearClearance, obstacle.xMax + BearClearance,
                    obstacle.yMax + BearClearance);
                if (!expanded.Contains(target)) continue;
                // A bear placed within an obstacle may walk out, but never deeper in.
                if (!expanded.Contains(from) ||
                    Vector2.Distance(target, expanded.center) <= Vector2.Distance(from, expanded.center)) return false;
            }
            return true;
        }

        private void RebuildBearObstacles()
        {
            _bearObstacles.Clear();
            foreach (var root in _experimentalBuilding3DVisibleRoots)
                if (root != null) AddBearObstacle(root);
            for (var i = 0; i < _session.Data.Props.Count && i < _propPresentations.Count; i++)
            {
                var prop = _session.Data.Props[i];
                if (prop == null || IsHorseWagon(prop.PropId) || IsThreeDimensionalCharacter(prop.PropId) || IsThreeDimensionalAnimal(prop.PropId)) continue;
                if (_propPresentations[i] != null) AddBearObstacle(_propPresentations[i].gameObject);
            }
            foreach (var placed in _session.Data.Buildings ?? new List<PlacedBuilding>())
            {
                var entry = BuildingCatalog.Find(placed.BuildingId);
                if (string.IsNullOrWhiteSpace(entry.PackageResourcePath)) continue;
                var package = HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
                if (package == null) continue;
                var fp = new BuildingGroundFootprint(placed, package);
                _bearObstacles.Add(Rect.MinMaxRect(fp.MinX, fp.MinZ, fp.MaxX, fp.MaxZ));
            }
        }

        private void AddBearObstacle(GameObject root)
        {
            var bounds = CombinedRendererBounds(root, out var valid);
            if (!valid) return;
            var min = _propRoot.InverseTransformPoint(bounds.min);
            var max = _propRoot.InverseTransformPoint(bounds.max);
            _bearObstacles.Add(Rect.MinMaxRect(Mathf.Min(min.x,max.x),Mathf.Min(min.z,max.z),
                Mathf.Max(min.x,max.x),Mathf.Max(min.z,max.z)));
        }
    }
}
