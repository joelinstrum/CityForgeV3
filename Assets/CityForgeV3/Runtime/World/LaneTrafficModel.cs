using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class LaneVehicleState
    {
        public int LaneIndex;
        public float DistanceMeters;
        public float SpeedMetersPerSecond;
        public float DesiredSpeedMetersPerSecond;
        public float GapAheadMeters = float.PositiveInfinity;
        public bool Braking;
    }

    /// <summary>
    /// Deterministic local traffic behavior. The lane graph supplies legality;
    /// this model supplies acceleration, following distance, and braking.
    /// </summary>
    public static class LaneTrafficModel
    {
        public static List<LaneVehicleState> Seed(int vehicleCount, RoadTrafficGraph graph,
            VehicleTypePackage vehicleType)
        {
            var states = new List<LaneVehicleState>();
            if (graph == null || graph.LaneCount == 0 || vehicleType == null) return states;
            for (var index = 0; index < vehicleCount; index++)
            {
                var lane = index % graph.LaneCount;
                var sequence = index / graph.LaneCount;
                var countOnLane = Mathf.CeilToInt(vehicleCount / (float)graph.LaneCount);
                var routeLength = graph.Routes[lane].TotalLengthMeters;
                states.Add(new LaneVehicleState
                {
                    LaneIndex = lane,
                    DistanceMeters = routeLength * sequence / countOnLane,
                    SpeedMetersPerSecond = graph.SpeedMetersPerSecond * 0.72f,
                    DesiredSpeedMetersPerSecond = graph.SpeedMetersPerSecond * Mathf.Lerp(
                        vehicleType.MinimumDesiredSpeedFactor,
                        vehicleType.MaximumDesiredSpeedFactor,
                        (index % 3) / 2f)
                });
            }
            UpdateGaps(states, graph, vehicleType);
            return states;
        }

        public static void Step(IReadOnlyList<LaneVehicleState> states,
            RoadTrafficGraph graph, VehicleTypePackage vehicleType, float deltaTime)
        {
            if (states == null || graph == null || vehicleType == null || deltaTime <= 0f) return;
            UpdateGaps(states, graph, vehicleType);
            foreach (var state in states)
            {
                if (state == null || state.LaneIndex < 0 || state.LaneIndex >= graph.LaneCount)
                    continue;
                var safeGap = vehicleType.MinimumStoppedGapMeters +
                    state.SpeedMetersPerSecond * vehicleType.FollowingTimeSeconds;
                var emergencyGap = vehicleType.MinimumStoppedGapMeters +
                    vehicleType.LengthMeters * 0.35f;
                state.Braking = state.GapAheadMeters < safeGap;
                var acceleration = state.Braking
                    ? -vehicleType.ComfortableBrakeMetersPerSecondSquared *
                      Mathf.Clamp01((safeGap - state.GapAheadMeters) /
                                    Mathf.Max(0.1f, safeGap - emergencyGap))
                    : vehicleType.AccelerationMetersPerSecondSquared;
                state.SpeedMetersPerSecond = Mathf.Clamp(
                    state.SpeedMetersPerSecond + acceleration * deltaTime,
                    0f, Mathf.Min(state.DesiredSpeedMetersPerSecond,
                        graph.SpeedMetersPerSecond));
                // Never allow a long or slow frame to carry the follower into
                // the vehicle ahead. GapAheadMeters is already bumper-to-bumper,
                // so preserve the authored stopped gap before integrating.
                if (!float.IsPositiveInfinity(state.GapAheadMeters))
                {
                    var availableTravel = Mathf.Max(0f,
                        state.GapAheadMeters - vehicleType.MinimumStoppedGapMeters);
                    state.SpeedMetersPerSecond = Mathf.Min(
                        state.SpeedMetersPerSecond, availableTravel / deltaTime);
                }
                var routeLength = graph.Routes[state.LaneIndex].TotalLengthMeters;
                state.DistanceMeters = Mathf.Repeat(
                    state.DistanceMeters + state.SpeedMetersPerSecond * deltaTime,
                    routeLength);
            }
            UpdateGaps(states, graph, vehicleType);
        }

        private static void UpdateGaps(IReadOnlyList<LaneVehicleState> states,
            RoadTrafficGraph graph, VehicleTypePackage vehicleType)
        {
            foreach (var state in states)
            {
                if (state == null || state.LaneIndex < 0 || state.LaneIndex >= graph.LaneCount)
                    continue;
                var routeLength = graph.Routes[state.LaneIndex].TotalLengthMeters;
                var nearest = float.PositiveInfinity;
                foreach (var candidate in states)
                {
                    if (candidate == null || ReferenceEquals(candidate, state) ||
                        candidate.LaneIndex != state.LaneIndex) continue;
                    var forward = Mathf.Repeat(
                        candidate.DistanceMeters - state.DistanceMeters, routeLength);
                    if (forward > 0.001f) nearest = Mathf.Min(nearest, forward);
                }
                state.GapAheadMeters = float.IsPositiveInfinity(nearest)
                    ? nearest
                    : Mathf.Max(0f, nearest - vehicleType.LengthMeters);
            }
        }
    }
}
