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

        public static DistrictRiverGenerationResult Generate(
            RegionCityTile district, DistrictRiverDirection direction,
            float curvature, DistrictRiverDepth depth, int seed)
        {
            if (district == null) return null;
            DistrictRiverGenerationResult best = null;
            var bestScore = float.PositiveInfinity;
            var random = new System.Random(seed);
            for (var candidate = 0; candidate < CandidateCount; candidate++)
            {
                var river = BuildCandidate(district, direction, depth, random);
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
            DistrictRiverDepth depth, System.Random random)
        {
            var vertical = direction is DistrictRiverDirection.SouthToNorth or
                DistrictRiverDirection.NorthToSouth;
            var reversed = direction is DistrictRiverDirection.NorthToSouth or
                DistrictRiverDirection.EastToWest;
            var crossSize = DistrictScale.SizeMeters(
                vertical ? district.Width : district.Height);
            var width = depth == DistrictRiverDepth.Shallow ? 72f : 46f;
            var center = Mathf.Lerp(0.28f, 0.72f, (float)random.NextDouble());
            // The first cardinal prototype is an exact grid-axis line. Keep
            // its cross-axis origin on the authored 10 m district lattice.
            var centerMeters = (center - .5f) * crossSize;
            centerMeters = Mathf.Round(centerMeters /
                DistrictScale.CellSizeMeters) * DistrictScale.CellSizeMeters;
            center = Mathf.Clamp(centerMeters / crossSize + .5f, .06f, .94f);
            var river = new PlacedDistrictRiver
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Direction = direction,
                Depth = depth,
                Curvature = 0f,
                WidthMeters = width
            };
            var start = reversed ? 1f : 0f;
            var end = 1f - start;
            river.Points.Add(vertical
                ? new DistrictRiverPoint(center, start)
                : new DistrictRiverPoint(start, center));
            river.Points.Add(vertical
                ? new DistrictRiverPoint(center, end)
                : new DistrictRiverPoint(end, center));
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
