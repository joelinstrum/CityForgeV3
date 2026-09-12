using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed class DistrictRiverGenerationResult
    {
        public PlacedDistrictRiver River;
        public readonly List<string> IntersectedLotInstanceIds = new();
    }

    public static class DistrictRiverGenerator
    {
        private const int CandidateCount = 24;
        private const int PointCount = 33;

        public static DistrictRiverGenerationResult Generate(
            RegionCityTile district, DistrictRiverDirection direction,
            float curvature, DistrictRiverDepth depth, int seed)
        {
            if (district == null) return null;
            curvature = Mathf.Clamp01(curvature);
            DistrictRiverGenerationResult best = null;
            var bestScore = float.PositiveInfinity;
            var random = new System.Random(seed);
            for (var candidate = 0; candidate < CandidateCount; candidate++)
            {
                var river = BuildCandidate(district, direction, curvature,
                    depth, random);
                var result = new DistrictRiverGenerationResult { River = river };
                var score = ScoreCandidate(district, river,
                    result.IntersectedLotInstanceIds);
                if (score >= bestScore) continue;
                best = result;
                bestScore = score;
                if (result.IntersectedLotInstanceIds.Count == 0 &&
                    score < 25f) break;
            }
            return best;
        }

        private static PlacedDistrictRiver BuildCandidate(
            RegionCityTile district, DistrictRiverDirection direction,
            float curvature, DistrictRiverDepth depth, System.Random random)
        {
            var vertical = direction is DistrictRiverDirection.SouthToNorth or
                DistrictRiverDirection.NorthToSouth;
            var reversed = direction is DistrictRiverDirection.NorthToSouth or
                DistrictRiverDirection.EastToWest;
            var crossSize = DistrictScale.SizeMeters(
                vertical ? district.Width : district.Height);
            var width = depth == DistrictRiverDepth.Shallow ? 72f : 46f;
            var minimumAmplitude = crossSize * 0.025f;
            var amplitude = Mathf.Lerp(minimumAmplitude,
                crossSize * 0.20f, curvature);
            var center = Mathf.Lerp(0.28f, 0.72f, (float)random.NextDouble());
            var phase = (float)random.NextDouble() * Mathf.PI * 2f;
            var harmonic = Mathf.Lerp(0.55f, 1.25f,
                (float)random.NextDouble());
            var river = new PlacedDistrictRiver
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Direction = direction,
                Depth = depth,
                Curvature = curvature,
                WidthMeters = width
            };
            for (var index = 0; index < PointCount; index++)
            {
                var t = index / (float)(PointCount - 1);
                var envelope = Mathf.Sin(Mathf.PI * t);
                var lateralMeters = amplitude * envelope *
                    (0.72f * Mathf.Sin(t * Mathf.PI * 2f + phase) +
                     0.28f * Mathf.Sin(t * Mathf.PI * 4f * harmonic - phase));
                var lateral = Mathf.Clamp(center + lateralMeters / crossSize,
                    0.06f, 0.94f);
                // Keep the procedural channel tied to the district's authored
                // 10 m grid. The polyline still curves between samples, but its
                // control points no longer drift at arbitrary sub-grid angles.
                var lateralFromCenter = (lateral - .5f) * crossSize;
                lateralFromCenter = Mathf.Round(lateralFromCenter /
                    DistrictScale.CellSizeMeters) * DistrictScale.CellSizeMeters;
                lateral = Mathf.Clamp(lateralFromCenter / crossSize + .5f,
                    0.06f, 0.94f);
                var along = reversed ? 1f - t : t;
                river.Points.Add(vertical
                    ? new DistrictRiverPoint(lateral, along)
                    : new DistrictRiverPoint(along, lateral));
            }
            return river;
        }

        private static float ScoreCandidate(RegionCityTile district,
            PlacedDistrictRiver river, List<string> intersections)
        {
            var score = 0f;
            var districtWidth = DistrictScale.SizeMeters(district.Width);
            var districtDepth = DistrictScale.SizeMeters(district.Height);
            foreach (var lotPlacement in district.Lots ??
                     new List<PlacedDistrictLot>())
            {
                if (lotPlacement == null) continue;
                var lot = LotSaveStore.Read(lotPlacement.LotId);
                if (lot == null) continue;
                var spanX = DistrictScale.GridSpanForMeters(
                    lot.LotWidthCells * LotMetricScale.MajorGridMeters);
                var spanZ = DistrictScale.GridSpanForMeters(
                    lot.LotDepthCells * LotMetricScale.MajorGridMeters);
                var rect = new Rect(
                    -districtWidth * 0.5f + lotPlacement.GridX *
                    DistrictScale.CellSizeMeters,
                    -districtDepth * 0.5f + lotPlacement.GridZ *
                    DistrictScale.CellSizeMeters,
                    spanX * DistrictScale.CellSizeMeters,
                    spanZ * DistrictScale.CellSizeMeters);
                var clearance = river.WidthMeters * 0.62f;
                rect.xMin -= clearance;
                rect.xMax += clearance;
                rect.yMin -= clearance;
                rect.yMax += clearance;
                if (!PolylineIntersectsRect(river.Points, rect,
                        districtWidth, districtDepth)) continue;
                intersections.Add(lotPlacement.InstanceId);
                score += 10000f;
            }
            for (var index = 1; index < river.Points.Count - 1; index++)
            {
                var a = ToMeters(river.Points[index - 1], districtWidth,
                    districtDepth);
                var b = ToMeters(river.Points[index], districtWidth,
                    districtDepth);
                var c = ToMeters(river.Points[index + 1], districtWidth,
                    districtDepth);
                score += Mathf.Abs(Vector2.SignedAngle(b - a, c - b)) * 0.02f;
            }
            return score;
        }

        private static bool PolylineIntersectsRect(
            IReadOnlyList<DistrictRiverPoint> points, Rect rect,
            float width, float depth)
        {
            for (var index = 0; index < points.Count; index++)
            {
                var point = ToMeters(points[index], width, depth);
                if (rect.Contains(point)) return true;
                if (index == 0) continue;
                var prior = ToMeters(points[index - 1], width, depth);
                const int probes = 8;
                for (var probe = 1; probe < probes; probe++)
                    if (rect.Contains(Vector2.Lerp(prior, point,
                            probe / (float)probes))) return true;
            }
            return false;
        }

        private static Vector2 ToMeters(DistrictRiverPoint point,
            float width, float depth) => new(
            (point.X - 0.5f) * width, (point.Z - 0.5f) * depth);
    }
}
