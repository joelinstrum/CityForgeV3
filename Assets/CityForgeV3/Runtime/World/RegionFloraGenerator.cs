using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // Pure spatial generation. No scene objects, rendering, navigation searches or forest scans per candidate.
    public static class RegionFloraGenerator
    {
        public static bool Retain(PlacedDistrictFlora tree) => tree != null &&
            (!tree.GeneratedByRegion || tree.HarvestState != DistrictTreeHarvestState.Standing || tree.WoodCredited);

        // Explicit district-wide edit: one scan, then indexed removals. Stones
        // retain their records and presentations; no terrain generation involved.
        public static List<string> ClearTrees(RegionCityTile district)
        {
            var ids = new List<string>();
            foreach (var flora in district.Flora ?? new())
                if (flora != null && !StoneFloraCatalog.IsStone(flora.FloraId)) ids.Add(flora.InstanceId);
            if (ids.Count == 0) return ids;
            var index = DistrictHarvestIndex.For(district);
            foreach (var id in ids) index.RemoveFlora(id);
            var removed = new HashSet<string>(ids);
            district.LotNudges?.RemoveAll(n => n.Kind == DistrictSelectionKind.Flora && removed.Contains(n.Id));
            district.TreeCoverage = RegionTreeCoverage.None;
            return ids;
        }

        public static List<PlacedDistrictFlora> Generate(RegionCityTile district, RegionClimate climate,
            RegionTreeCoverage coverage, int seed, Func<string, LotSaveData> readLot = null,
            ForestFamilyMix familyMix = null)
        {
            if (district == null) throw new ArgumentNullException(nameof(district));
            if (coverage != RegionTreeCoverage.None && !RegionClimateRules.AllowsForest(climate))
                throw new InvalidOperationException("Tree coverage is unavailable in Desert climate.");
            var result = new List<PlacedDistrictFlora>();
            foreach (var tree in district.Flora ?? new()) if (Retain(tree)) result.Add(tree);
            if (coverage == RegionTreeCoverage.None) return result;
            familyMix ??= new ForestFamilyMix();
            if (familyMix.Total <= 0)
                throw new InvalidOperationException("At least one forest family percentage must be greater than zero.");
            var retainedIds = new HashSet<string>();
            foreach (var tree in result) retainedIds.Add(tree.InstanceId);
            var mask = new PlantingMask(district, readLot ?? LotContentCatalog.Read);
            foreach (var tree in result)
            {
                float radius = ForestClusterCatalog.IsCluster(tree.FloraId)
                    ? ForestClusterCatalog.ClearanceMeters(tree.FloraId) * Mathf.Clamp(tree.Scale, .65f, 1.45f) : 8;
                mask.Block(new Vector2(tree.NormalizedX * mask.Width, tree.NormalizedZ * mask.Depth), radius, radius);
            }
            uint hash = unchecked((uint)seed);
            foreach (char c in district.TileId ?? "") hash = unchecked((hash ^ c) * 16777619);
            var random = new System.Random(unchecked((int)hash));
            float spacing = coverage == RegionTreeCoverage.Sparse ? 64 : 24;
            // Four times fewer candidate records; each mixed cluster depicts five
            // trees. One in five placements remains an individually harvestable fir.
            spacing *= 2;
            // Area density is inverse spacing squared; retain existing Medium exactly.
            if (coverage == RegionTreeCoverage.Heavy) spacing /= Mathf.Sqrt(3f);
            float chance = coverage == RegionTreeCoverage.Sparse ? .35f : .78f;
            var elevation = new DistrictElevation(district, 10f);
            int index = 0;
            for (float z = spacing / 2; z < mask.Depth - spacing / 4; z += spacing)
            for (float x = spacing / 2; x < mask.Width - spacing / 4; x += spacing)
            {
                var point = new Vector2(x + ((float)random.NextDouble() - .5f) * spacing * .6f,
                    z + ((float)random.NextDouble() - .5f) * spacing * .6f);
                var id = $"region-flora-{seed:x8}-{district.TileId}-{index++}";
                if (random.NextDouble() > chance || retainedIds.Contains(id)) continue;
                // Harvestable firs remain separate records so existing timber
                // routing and labor behavior are unchanged. Percentages choose
                // the dominant family of the scenery billboards.
                bool harvestable = random.Next(5) == 0;
                string floraId;
                if (harvestable) floraId = "cilician-fir";
                else
                {
                    string family = ChooseFamily(random, familyMix);
                    bool large = IsLargeFlatFootprint(elevation, point, mask.Width, mask.Depth);
                    floraId = ForestClusterCatalog.Id(family, large);
                }
                float scale = .86f + (float)random.NextDouble() * .3f;
                float clearance = ForestClusterCatalog.IsCluster(floraId) ? ForestClusterCatalog.ClearanceMeters(floraId) * scale : 0;
                if (mask.Blocked(point, clearance))
                {
                    // A flat candidate may still be near a road, river or lot.
                    // Retain it as the smaller one-billboard cluster when that
                    // bounded footprint fits instead of searching elsewhere.
                    if (!ForestClusterCatalog.IsLarge(floraId)) continue;
                    floraId = ForestClusterCatalog.Id(ChooseFamilyFromId(floraId), false);
                    clearance = ForestClusterCatalog.ClearanceMeters(floraId) * scale;
                    if (mask.Blocked(point, clearance)) continue;
                }
                result.Add(new PlacedDistrictFlora
                {
                    InstanceId = id,
                    GroupId = $"region-flora-{seed:x8}", GeneratedByRegion = true,
                    FloraId = floraId,
                    NormalizedX = point.x / mask.Width, NormalizedZ = point.y / mask.Depth,
                    Scale = scale,
                    RotationEighthTurns = random.Next(8)
                });
            }
            return result;
        }

        static string ChooseFamily(System.Random random, ForestFamilyMix mix)
        {
            int deciduous = Math.Max(0, mix.Deciduous);
            int mountain = Math.Max(0, mix.Mountain);
            int roll = random.Next(mix.Total);
            if (roll < deciduous) return FloraFamilies.Deciduous;
            if (roll < deciduous + mountain) return FloraFamilies.Mountain;
            return FloraFamilies.Tropical;
        }

        static string ChooseFamilyFromId(string id) => id.StartsWith("forest-mountain-")
            ? FloraFamilies.Mountain : id.StartsWith("forest-tropical-")
            ? FloraFamilies.Tropical : FloraFamilies.Deciduous;

        // Five constant-time elevation samples decide whether a candidate has
        // enough level ground for a nine-tree composition. This is generation-
        // time work only and never scans district objects or runs per frame.
        static bool IsLargeFlatFootprint(DistrictElevation elevation, Vector2 point,
            float width, float depth)
        {
            float x = point.x - width * .5f, z = point.y - depth * .5f;
            const float offset = 12f;
            float center = elevation.Sample(x, z);
            float min = center, max = center;
            void Include(float h) { min = Mathf.Min(min, h); max = Mathf.Max(max, h); }
            Include(elevation.Sample(x - offset, z));
            Include(elevation.Sample(x + offset, z));
            Include(elevation.Sample(x, z - offset));
            Include(elevation.Sample(x, z + offset));
            return max - min <= 1.25f;
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
            public bool Blocked(Vector2 p, float radius)
            {
                if (p.x - radius < 5 || p.y - radius < 5 || p.x + radius >= Width - 5 || p.y + radius >= Depth - 5) return true;
                // Bounded local footprint query (at most 6 x 6 cells for generated
                // clusters), never a scan through district flora or constraints.
                int minX = Mathf.FloorToInt((p.x - radius) / Cell), maxX = Mathf.FloorToInt((p.x + radius) / Cell);
                int minZ = Mathf.FloorToInt((p.y - radius) / Cell), maxZ = Mathf.FloorToInt((p.y + radius) / Cell);
                for (int z = minZ; z <= maxZ; z++)
                    for (int x = minX; x <= maxX; x++)
                        if (occupied[z * columns + x]) return true;
                return false;
            }
        }
    }

    // One district per step; the UI can cancel without ever mutating the saved region.
    public sealed class RegionFloraGeneration
    {
        readonly RegionSaveData region;
        readonly RegionTerrainSettings settings;
        readonly List<RegionCityTile> districts;
        readonly bool regional;
        readonly List<List<PlacedDistrictFlora>> generated = new();
        readonly Dictionary<string, LotSaveData> lots = new();
        public int Completed => generated.Count;
        public int TreeCount { get; private set; }
        public bool Ready => Completed == districts.Count;
        public RegionFloraGeneration(RegionSaveData region, RegionTreeCoverage coverage, int seed,
            RegionCityTile district = null, ForestFamilyMix familyMix = null)
        {
            this.region = region ?? throw new ArgumentNullException(nameof(region));
            if (district != null && !region.Tiles.Contains(district))
                throw new ArgumentException("The district must belong to this region.", nameof(district));
            regional = district == null;
            districts = regional ? new List<RegionCityTile>(region.Tiles) : new List<RegionCityTile> { district };
            settings = (region.Terrain ?? new()).Copy(); settings.TreeCoverage = coverage; settings.FloraSeed = seed;
            settings.ForestMix = (familyMix ?? district?.ForestMix ?? settings.ForestMix ?? new ForestFamilyMix()).Copy();
            if (coverage != RegionTreeCoverage.None && !RegionClimateRules.AllowsForest(settings.Climate))
                throw new InvalidOperationException("Tree coverage is unavailable in Desert climate.");
        }
        LotSaveData ReadLot(string id)
        {
            if (!lots.TryGetValue(id, out var lot)) lots[id] = lot = LotContentCatalog.Read(id);
            return lot;
        }
        public void Step()
        {
            if (Ready) return;
            var trees = RegionFloraGenerator.Generate(districts[Completed], settings.Climate,
                settings.TreeCoverage, settings.FloraSeed, ReadLot, settings.ForestMix);
            TreeCount += trees.Count; generated.Add(trees);
        }
        public void Commit(Action<RegionSaveData> save)
        {
            if (!Ready) throw new InvalidOperationException("Tree generation is not complete.");
            var oldSettings = region.Terrain; var oldModified = region.ModifiedUtc;
            var previous = new List<List<PlacedDistrictFlora>>();
            var previousCoverage = new List<RegionTreeCoverage>(); var previousSeeds = new List<int>();
            var previousMixes = new List<ForestFamilyMix>();
            foreach (var tile in districts)
            {
                previous.Add(tile.Flora); previousCoverage.Add(tile.TreeCoverage);
                previousSeeds.Add(tile.FloraSeed); previousMixes.Add(tile.ForestMix);
            }
            try
            {
                if (regional) region.Terrain = settings;
                for (int i = 0; i < districts.Count; i++)
                {
                    districts[i].Flora = generated[i];
                    districts[i].TreeCoverage = settings.TreeCoverage; districts[i].FloraSeed = settings.FloraSeed;
                    districts[i].ForestMix = settings.ForestMix.Copy();
                }
                save(region);
            }
            catch
            {
                region.Terrain = oldSettings; region.ModifiedUtc = oldModified;
                for (int i = 0; i < districts.Count; i++)
                {
                    districts[i].Flora = previous[i];
                    districts[i].TreeCoverage = previousCoverage[i]; districts[i].FloraSeed = previousSeeds[i];
                    districts[i].ForestMix = previousMixes[i];
                }
                throw;
            }
            foreach (var tile in districts) DistrictHarvestIndex.Invalidate(tile);
        }
    }
}
