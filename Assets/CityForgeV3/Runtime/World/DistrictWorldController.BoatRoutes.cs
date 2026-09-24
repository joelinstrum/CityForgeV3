using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        private const float MinimumBoatWaterDepth = .05f;
        public void SetLotBehaviorsPaused(bool paused)
        { foreach (var lot in _lots) if (lot != null) lot.BehaviorSimulationPaused = paused; }
        public bool ValidateLotBoatPlacement(RegionCityTile district, PlacedDistrictLot placement, LotSaveData data, out string reason)
        {
            reason = "";
            if (data?.Props == null) return true;
            var boats = data.Props.Where(p => BoatCatalog.Find(p.PropId) != null).ToList();
            if (boats.Count == 0 && !data.HasWaterOrientation) return true;
            if (_content == null) { reason = "River surface is not ready."; return false; }
            var center = DistrictLotCenterMeters(district, placement, data);
            var rotation = Quaternion.Euler(0, placement.RotationQuarterTurns * 90 + HostedLotFacingOffsetDegrees, 0);
            if (data.HasWaterOrientation)
            {
                var landLocal = data.WaterOrientationLand;
                var waterLocal = data.WaterOrientationWater;
                // A movable arrow is a direction handle, not a physical dock.
                // For building/boat lots use the actual authored objects as anchors.
                if (boats.Count > 0 && data.Buildings3D != null && data.Buildings3D.Count > 0)
                {
                    var building = data.Buildings3D[0];
                    landLocal = new Vector3(building.X,0,building.Z);
                }
                if (IsLumberMill(data) && boats.Count > 0 &&
                    !TryAlignLumberMillLotToRiver(center, rotation, landLocal,
                        boats[0], placement))
                { reason = "Place the mill beside river water deep enough for its barge."; return false; }
                center = DistrictLotCenterMeters(district, placement, data);
                Vector3 Point(Vector3 p) => _content.TransformPoint(
                    new Vector3(center.x,0,center.y)+rotation*p);
                var land = Point(landLocal);
                foreach (var delta in new[] { Vector3.zero, Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    var sample = SampleRiverSurface(land + delta);
                    if (sample.HasValue && sample.Value.UnderWater)
                    { reason = "Shoreline required: keep the mill on land and the barge over the river."; return false; }
                }
                if (boats.Count > 0 && data.Buildings3D != null &&
                    data.Buildings3D.Count > 0)
                {
                    waterLocal = new Vector3(boats[0].PositionX, 0,
                        boats[0].PositionZ);
                    var facing = data.WaterOrientationWater-
                        data.WaterOrientationLand; facing.y=0;
                    if (Vector3.Dot(facing.normalized,
                            (waterLocal-landLocal).normalized) <= 0)
                    { reason = "Point the water-facing arrow toward the barge side of the mill."; return false; }
                }
                var water = Point(waterLocal);
                var wet = SampleRiverSurface(water);
                if (!wet.HasValue || !wet.Value.UnderWater)
                { reason = "Shoreline required: point the blue arrow toward river water."; return false; }
            }
            foreach (var boat in boats)
            {
                var definition = BoatCatalog.Find(boat.PropId);
                // Preserve the complete authored Lot composition. Shoreline
                // fitting moves/rotates the Lot as one unit; it must never
                // detach a boat from its saved Lot-local pose.
                var local = new Vector3(center.x, 0, center.y) + rotation *
                    new Vector3(boat.PositionX, 0, boat.PositionZ);
                var turns = placement.HasBoatDockOverride
                    ? placement.BoatDockRotationQuarterTurns
                    : boat.RotationQuarterTurns;
                var heading = rotation * Quaternion.Euler(0, turns * 90, 0);
                var origin = _content.TransformPoint(local);
                if (!BoatFootprintOnRiver(origin, _content.rotation * heading, definition.widthMeters, definition.lengthMeters))
                { reason = "Place the entire barge on navigable river water."; return false; }
                if (data.Behaviors != null && data.Behaviors.Any(b => b.BoatInstanceId == boat.InstanceId) &&
                    FindDownstreamBoatRoute(origin, definition.widthMeters, definition.lengthMeters) == null)
                { reason = "The barge needs a navigable downstream river exit."; return false; }
            }
            return true;
        }

        private static bool IsLumberMill(LotSaveData data) =>
            data?.Buildings3D != null && data.Buildings3D.Any(building =>
                building.AssetId == "lumber-mill-v01");

        private bool TryAlignLumberMillLotToRiver(Vector2 center,
            Quaternion lotRotation, Vector3 landLocal, PlacedProp boat,
            PlacedDistrictLot placement)
        {
            var authored = new Vector3(center.x, 0, center.y) + lotRotation *
                new Vector3(boat.PositionX, 0, boat.PositionZ);
            var query = new Vector2(authored.x, authored.z);
            RuntimeRiverSurface best = null;
            RuntimeRiverSurface.ClosestPoint closest = default;
            var nearest = float.PositiveInfinity;
            const float shorelineSearchRadius = 96f;
            _riverSurfaceIndex.QueryBounds(new Rect(
                query - Vector2.one * shorelineSearchRadius,
                Vector2.one * shorelineSearchRadius * 2f),
                _riverPlacementCandidates);
            foreach (var river in _riverPlacementCandidates)
            {
                var candidate = river.FindClosest(query,
                    shorelineSearchRadius);
                if (candidate.AbsoluteLateral >= nearest) continue;
                best = river;
                closest = candidate;
                nearest = candidate.AbsoluteLateral;
            }
            if (best == null || float.IsInfinity(nearest) ||
                nearest > shorelineSearchRadius)
                return false;
            var normal = new Vector2(-closest.Tangent.y, closest.Tangent.x);
            var landPoint = new Vector3(center.x, 0, center.y) +
                lotRotation * landLocal;
            var landClosest = best.FindClosest(new Vector2(
                landPoint.x, landPoint.z), shorelineSearchRadius);
            // Choose the bank from the authored mill-to-barge relationship,
            // independent of where the placement cursor began. The mill must
            // remain outward from the hull after the complete Lot translates.
            var landFromBoat = new Vector2(landPoint.x - authored.x,
                landPoint.z - authored.z);
            var bankSide = Mathf.Sign(Vector2.Dot(landFromBoat, normal));
            if (Mathf.Approximately(bankSide, 0f))
                bankSide = Mathf.Sign(landClosest.SignedLateral);
            if (Mathf.Approximately(bankSide, 0f)) bankSide = 1f;
            var definition = BoatCatalog.Find(boat.PropId);
            var worldTangent = _content.TransformDirection(new Vector3(
                closest.Tangent.x, 0, closest.Tangent.y));
            var desiredWorldHeading = Quaternion.LookRotation(
                worldTangent).eulerAngles.y;
            var lotWorldHeading = (_content.rotation * lotRotation).eulerAngles.y;
            var localTurns = Mathf.RoundToInt(Mathf.DeltaAngle(0,
                desiredWorldHeading - lotWorldHeading) / 90f);
            var heading = _content.rotation * lotRotation *
                Quaternion.Euler(0, localTurns * 90f, 0);
            var districtHeading = Quaternion.Inverse(_content.rotation) *
                heading * Vector3.forward;
            var districtRight = Quaternion.Inverse(_content.rotation) *
                heading * Vector3.right;
            var lateralHalfExtent = Mathf.Abs(Vector2.Dot(
                    new Vector2(districtHeading.x, districtHeading.z), normal)) *
                definition.lengthMeters * .5f + Mathf.Abs(Vector2.Dot(
                    new Vector2(districtRight.x, districtRight.z), normal)) *
                definition.widthMeters * .5f;
            // The lumber barge is a shallow-draft flatboat. Requiring deep
            // water here pushed it unnecessarily far from its authored dock.
            var navigableHalfWidth = best.NavigableHalfWidth(landClosest,
                MinimumBoatWaterDepth);
            var boatLateral = Mathf.Max(0f, navigableHalfWidth -
                lateralHalfExtent - 1f);
            var centerline = query - normal * closest.SignedLateral;
            var dock = centerline + normal * bankSide * boatLateral;
            var delta = dock - query;
            placement.ShoreOffsetX += delta.x;
            placement.ShoreOffsetZ += delta.y;
            // Keep the authored position, while allowing the hull to face the
            // nearest navigable direction of the river.
            placement.HasBoatDockOverride = true;
            placement.BoatDockRotationQuarterTurns =
                ((localTurns % 4) + 4) % 4;
            placement.BoatDockContractVersion = 3;
            return true;
        }

        public bool BoatFootprintOnRiver(Vector3 origin, Quaternion heading, float width, float length)
        {
            var rows = Mathf.Max(1, Mathf.CeilToInt(length));
            var columns = Mathf.Max(1, Mathf.CeilToInt(width));
            for (var z = 0; z <= rows; z++) for (var x = 0; x <= columns; x++)
            {
                var point = origin + heading * new Vector3(Mathf.Lerp(-width / 2, width / 2, x / (float)columns), 0,
                    Mathf.Lerp(-length / 2, length / 2, z / (float)rows));
                var water = SampleRiverSurface(point);
                if (!water.HasValue || !water.Value.UnderWater ||
                    water.Value.WaterDepth < MinimumBoatWaterDepth) return false;
            }
            return true;
        }
        // Connectivity comes from the authored directional channel, not a preview checkbox.
        public List<Vector3> FindDownstreamBoatRoute(Vector3 origin, float boatWidth, float boatLength)
        {
            if (_content == null) return null;
            var local = _content.InverseTransformPoint(origin);
            var point = new Vector2(local.x, local.z);
            RuntimeRiverSurface best = null; var nearest = float.PositiveInfinity;
            foreach (var river in _riverSurfaces)
            {
                var distance = river.FindClosest(point).AbsoluteLateral;
                if (distance + boatWidth * .5f >= river.WaterHalfWidth || distance >= nearest) continue;
                best = river; nearest = distance;
            }
            if (best == null || best.Points.Count < 2) return null;
            var end = best.Points[best.Points.Count - 1];
            if (Mathf.Abs(end.x) < _widthMeters * .5f - 2 && Mathf.Abs(end.y) < _depthMeters * .5f - 2) return null;
            var segmentIndex = -1; var closest = Vector2.zero; var distanceSquared = float.PositiveInfinity;
            for (var i = 1; i < best.Points.Count; i++)
            {
                var a = best.Points[i - 1]; var delta = best.Points[i] - a;
                if (delta.sqrMagnitude < .0001f) continue;
                var projected = a + delta * Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude);
                if ((projected - point).sqrMagnitude >= distanceSquared) continue;
                distanceSquared = (projected - point).sqrMagnitude; closest = projected; segmentIndex = i;
            }
            if (segmentIndex < 0) return null;
            Vector3 World(Vector2 p) => _content.TransformPoint(new Vector3(p.x, best.WaterElevation - .25f, p.y));
            var route = new List<Vector3> { World(point), World(closest) };
            for (var i = segmentIndex; i < best.Points.Count; i++) route.Add(World(best.Points[i]));
            var length = 0f;
            for (var i = 1; i < route.Count; i++)
            {
                var delta = route[i] - route[i - 1]; length += delta.magnitude;
                // The first leg is the short push-off from the authored dock
                // pose to the river centerline. A hull may intentionally rest
                // on the irregular shoreline there; navigability is enforced
                // on every downstream channel segment after it clears the dock.
                if (i == 1) continue;
                var side = Vector3.Cross(Vector3.up, delta.normalized) * (boatWidth * .5f + .2f);
                var samples = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 2));
                for (var j = 0; j <= samples; j++)
                {
                    var center = Vector3.Lerp(route[i - 1], route[i], j / (float)samples);
                    foreach (var lateral in new[] { -side, Vector3.zero, side })
                    {
                        var water = SampleRiverSurface(center + lateral);
                        if (!water.HasValue || !water.Value.UnderWater ||
                            water.Value.WaterDepth < MinimumBoatWaterDepth) return null;
                    }
                }
            }
            return length > boatLength * 2 ? route : null;
        }
    }
}
