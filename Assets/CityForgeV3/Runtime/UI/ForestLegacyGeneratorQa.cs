#if UNITY_EDITOR
// Frozen HEAD baseline used only for isolated before/after forest QA.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // Pure spatial generation. No scene objects, rendering, navigation searches or forest scans per candidate.
    internal static class ForestLegacyGeneratorQa
    {
        static readonly string[] Temperate = { "maple", "oak", "vendor-red-maple", "vendor-oregon-ash", "cilician-fir", "cilician-fir" };
        static readonly string[] Tropical = { "date-palm", "camphor-tree", "eucalyptus-robusta-a", "eucalyptus-robusta-b", "angel-oak-spanish-moss" };
        static readonly string[] Mediterranean = { "maple", "oak", "cilician-fir", "cilician-fir", "date-palm", "camphor-tree", "eucalyptus-robusta-a" };
        public static bool Retain(PlacedDistrictFlora tree) => tree != null &&
            (!tree.GeneratedByRegion || tree.HarvestState != DistrictTreeHarvestState.Standing || tree.WoodCredited);

        public static List<PlacedDistrictFlora> Generate(RegionCityTile district, RegionClimate climate,
            RegionTreeCoverage coverage, int seed, Func<string, LotSaveData> readLot = null)
        {
            if (district == null) throw new ArgumentNullException(nameof(district));
            if (coverage != RegionTreeCoverage.None && !RegionClimateRules.AllowsForest(climate))
                throw new InvalidOperationException("Tree coverage is unavailable in Desert climate.");
            var result = new List<PlacedDistrictFlora>();
            foreach (var tree in district.Flora ?? new()) if (Retain(tree)) result.Add(tree);
            if (coverage == RegionTreeCoverage.None) return result;
            var retainedIds = new HashSet<string>();
            foreach (var tree in result) retainedIds.Add(tree.InstanceId);
            var mask = new PlantingMask(district, readLot ?? LotContentCatalog.Read);
            foreach (var tree in result)
                mask.Block(new Vector2(tree.NormalizedX * mask.Width, tree.NormalizedZ * mask.Depth), 8, 8);
            uint hash = unchecked((uint)seed);
            foreach (char c in district.TileId ?? "") hash = unchecked((hash ^ c) * 16777619);
            var random = new System.Random(unchecked((int)hash));
            var palette = climate == RegionClimate.Tropical ? Tropical : climate == RegionClimate.Mediterranean ? Mediterranean : Temperate;
            float spacing = coverage == RegionTreeCoverage.Sparse ? 64 : 24;
            float chance = coverage == RegionTreeCoverage.Sparse ? .35f : .78f;
            int index = 0;
            for (float z = spacing / 2; z < mask.Depth - spacing / 4; z += spacing)
            for (float x = spacing / 2; x < mask.Width - spacing / 4; x += spacing)
            {
                var point = new Vector2(x + ((float)random.NextDouble() - .5f) * spacing * .6f,
                    z + ((float)random.NextDouble() - .5f) * spacing * .6f);
                var id = $"region-flora-{seed:x8}-{district.TileId}-{index++}";
                if (random.NextDouble() > chance || mask.Blocked(point) || retainedIds.Contains(id)) continue;
                result.Add(new PlacedDistrictFlora
                {
                    InstanceId = id,
                    GroupId = $"region-flora-{seed:x8}", GeneratedByRegion = true,
                    FloraId = palette[random.Next(palette.Length)],
                    NormalizedX = point.x / mask.Width, NormalizedZ = point.y / mask.Depth,
                    Scale = .86f + (float)random.NextDouble() * .3f,
                    RotationEighthTurns = random.Next(8)
                });
            }
            return result;
        }

        // Eight-meter occupancy cells conservatively reserve roads, river banks and
        // developed footprints once. Candidate checks then cost a single bit lookup.
        sealed class PlantingMask
        {
            const float Cell = 8;
            readonly int columns, rows;
            readonly BitArray occupied;
            public readonly float Width, Depth;
            public PlantingMask(RegionCityTile d, Func<string, LotSaveData> readLot)
            {
                Width = DistrictScale.SizeMeters(d.Width); Depth = DistrictScale.SizeMeters(d.Height);
                columns = Mathf.CeilToInt(Width / Cell); rows = Mathf.CeilToInt(Depth / Cell);
                occupied = new BitArray(columns * rows);
                var origin = new Vector2(Width / 2, Depth / 2);
                foreach (var road in d.Roads ?? new())
                    Block(new Vector2((road.GridX + .5f) * DistrictScale.CellSizeMeters,
                        (road.GridZ + .5f) * DistrictScale.CellSizeMeters), 9, 9);
                foreach (var placed in d.Lots ?? new())
                {
                    var lot = readLot(placed.LotId) ?? throw new InvalidOperationException("Cannot check building footprint in " + d.Name + ": " + placed.LotId);
                    float w = lot.LotWidthCells * LotMetricScale.MajorGridMeters, h = lot.LotDepthCells * LotMetricScale.MajorGridMeters;
                    if ((placed.RotationQuarterTurns & 1) != 0) (w, h) = (h, w);
                    Block(DistrictWorldController.DistrictLotCenterMeters(d, placed, lot) + origin, w / 2 + 5, h / 2 + 5);
                }
                if (d.Founded && (d.Lots?.Count ?? 0) == 0 && !string.IsNullOrEmpty(d.LotId))
                {
                    var lot = readLot(d.LotId) ?? throw new InvalidOperationException("Cannot check founder footprint in " + d.Name);
                    float w = lot.LotWidthCells * LotMetricScale.MajorGridMeters, h = lot.LotDepthCells * LotMetricScale.MajorGridMeters;
                    Block(DistrictWorldController.DistrictLotCenterMeters(d, d.FounderNormalizedX, d.FounderNormalizedY, w, h) + origin, w / 2 + 5, h / 2 + 5);
                }
                foreach (var q in d.StoneSites ?? new()) if (q.Built) Block(DistrictQuarry.Point(d, q) + origin, 26, 26);
                foreach (var b in d.Brickworks ?? new())
                {
                    float radians = b.Yaw * Mathf.Deg2Rad;
                    float c = Mathf.Abs(Mathf.Cos(radians)), s = Mathf.Abs(Mathf.Sin(radians));
                    Block(DistrictBrickworks.Point(d, b) + origin,
                        c * DistrictBrickworks.HalfWidth + s * DistrictBrickworks.HalfDepth + 6,
                        s * DistrictBrickworks.HalfWidth + c * DistrictBrickworks.HalfDepth + 6);
                }
                foreach (var mine in d.ResourceDeposits ?? new()) if (mine.MineBuilt)
                    Block(new Vector2(mine.NormalizedX * Width, mine.NormalizedZ * Depth) +
                        DistrictLotNudge.GetOffset(d, DistrictSelectionKind.Entity, "mine:" + mine.Id), 30, 30);
                foreach (var river in d.Rivers ?? new())
                {
                    for (int i = 1; i < river.Points.Count; i++)
                    {
                        var a = new Vector2(river.Points[i - 1].X * Width, river.Points[i - 1].Z * Depth);
                        var b = new Vector2(river.Points[i].X * Width, river.Points[i].Z * Depth);
                        float radius = river.WidthMeters * .8f + 8;
                        var delta = b - a; float length = delta.sqrMagnitude;
                        Each(Rect.MinMaxRect(Mathf.Min(a.x, b.x) - radius, Mathf.Min(a.y, b.y) - radius,
                            Mathf.Max(a.x, b.x) + radius, Mathf.Max(a.y, b.y) + radius), (x, z) =>
                        {
                            var p = new Vector2((x + .5f) * Cell, (z + .5f) * Cell);
                            var nearest = a + delta * (length > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, delta) / length) : 0);
                            if ((p - nearest).sqrMagnitude <= radius * radius) occupied[z * columns + x] = true;
                        });
                    }
                }
            }
            public void Block(Vector2 center, float halfWidth, float halfDepth) =>
                Each(new Rect(center.x - halfWidth, center.y - halfDepth, halfWidth * 2, halfDepth * 2),
                    (x, z) => occupied[z * columns + x] = true);
            void Each(Rect rect, Action<int, int> visit)
            {
                int minX = Mathf.Max(0, Mathf.FloorToInt(rect.xMin / Cell)), maxX = Mathf.Min(columns - 1, Mathf.FloorToInt(rect.xMax / Cell));
                int minZ = Mathf.Max(0, Mathf.FloorToInt(rect.yMin / Cell)), maxZ = Mathf.Min(rows - 1, Mathf.FloorToInt(rect.yMax / Cell));
                for (int z = minZ; z <= maxZ; z++) for (int x = minX; x <= maxX; x++) visit(x, z);
            }
            public bool Blocked(Vector2 p) => p.x < 5 || p.y < 5 || p.x >= Width - 5 || p.y >= Depth - 5 ||
                occupied[Mathf.FloorToInt(p.y / Cell) * columns + Mathf.FloorToInt(p.x / Cell)];
        }
    }

}
#endif
