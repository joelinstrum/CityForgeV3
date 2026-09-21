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
            var alongCells = vertical ? district.Height : district.Width;
            var crossCells = vertical ? district.Width : district.Height;
            var crossSize = DistrictScale.SizeMeters(
                crossCells);
            var width = depth == DistrictRiverDepth.Shallow ? 72f : 46f;
            var center = Mathf.Lerp(0.28f, 0.72f, (float)random.NextDouble());
            var centerMeters = (center - .5f) * crossSize;
            var baseCrossCell = Mathf.RoundToInt(centerMeters /
                DistrictScale.CellSizeMeters);
            var minimumCrossCell = Mathf.CeilToInt(-crossCells * .44f);
            var maximumCrossCell = Mathf.FloorToInt(crossCells * .44f);
            baseCrossCell = Mathf.Clamp(baseCrossCell, minimumCrossCell,
                maximumCrossCell);
            var river = new PlacedDistrictRiver
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Direction = direction,
                Depth = depth,
                Curvature = 0f,
                WidthMeters = width
            };
            var logical = new List<DistrictRiverPoint>();
            void AddPoint(int alongCell, int crossCell)
            {
                var along = alongCell / (float)alongCells;
                var cross = crossCell / (float)crossCells + .5f;
                var point = vertical
                    ? new DistrictRiverPoint(cross, along)
                    : new DistrictRiverPoint(along, cross);
                if (logical.Count == 0 ||
                    Mathf.Abs(logical[^1].X - point.X) > .000001f ||
                    Mathf.Abs(logical[^1].Z - point.Z) > .000001f)
                    logical.Add(point);
            }

            // V02 retains exact grid-axis segments, but varies the forward
            // runs and cross-axis steps to form a seeded stair-step channel.
            var maximumOffset = Mathf.Max(1,
                Mathf.RoundToInt(crossCells * .22f));
            var lowerCross = Mathf.Max(minimumCrossCell,
                baseCrossCell - maximumOffset);
            var upperCross = Mathf.Min(maximumCrossCell,
                baseCrossCell + maximumOffset);
            var currentAlong = 0;
            var currentCross = baseCrossCell;
            var previousRun = 0;
            AddPoint(currentAlong, currentCross);
            while (alongCells - currentAlong > 1)
            {
                var remaining = alongCells - currentAlong;
                var maximumRun = Mathf.Min(remaining - 1, Mathf.Max(2,
                    Mathf.RoundToInt(alongCells * .38f)));
                var run = random.Next(1, maximumRun + 1);
                if (maximumRun > 1 && run == previousRun)
                    run = run % maximumRun + 1;
                previousRun = run;
                currentAlong += run;
                AddPoint(currentAlong, currentCross);
                if (lowerCross == upperCross) continue;
                var targetCross = random.Next(lowerCross, upperCross + 1);
                if (targetCross == currentCross)
                    targetCross = targetCross == upperCross
                        ? lowerCross : targetCross + 1;
                currentCross = targetCross;
                AddPoint(currentAlong, currentCross);
            }
            if (currentCross != baseCrossCell)
                AddPoint(currentAlong, baseCrossCell);
            AddPoint(alongCells, baseCrossCell);
            if (reversed) logical.Reverse();
            river.Points.AddRange(logical);
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
