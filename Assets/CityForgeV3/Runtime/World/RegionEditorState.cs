using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum DistrictSelectionKind
    {
        Flora,
        Lot,
        Road,
        River,
        Entity
    }

    public readonly struct DistrictSelectionRef
    {
        public readonly DistrictSelectionKind Kind;
        public readonly string Id;

        public DistrictSelectionRef(DistrictSelectionKind kind, string id)
        {
            Kind = kind;
            Id = id ?? "";
        }
    }

    [Serializable]
    public sealed class PlacedDistrictLot
    {
        public bool BehaviorsInitialized;
        public int TimberBundles;
        public List<CityForgeV3.Behaviors.LotBehaviorInstance> Behaviors = new();
        public string InstanceId = "";
        public string LotId = "";
        public int GridX;
        public int GridZ;
        public float ShoreOffsetX;
        public float ShoreOffsetZ;
        public int RotationQuarterTurns;
    }

    public enum DistrictRiverDirection
    {
        SouthToNorth,
        NorthToSouth,
        WestToEast,
        EastToWest
    }

    public enum DistrictRiverDepth
    {
        Shallow,
        Deep
    }

    [Serializable]
    public sealed class DistrictRiverPoint
    {
        public float X;
        public float Z;

        public DistrictRiverPoint() { }

        public DistrictRiverPoint(float x, float z)
        {
            X = x;
            Z = z;
        }
    }

    [Serializable]
    public sealed class PlacedDistrictRiver
    {
        public string RegionRiverId = "";
        public string InstanceId = "";
        public DistrictRiverDirection Direction =
            DistrictRiverDirection.SouthToNorth;
        public DistrictRiverDepth Depth = DistrictRiverDepth.Shallow;
        public float Curvature = 0.5f;
        public float WidthMeters = 64f;
        public List<DistrictRiverPoint> Points = new();
    }

    public static class DistrictRiverEditing
    {
        public static PlacedDistrictRiver FindAt(RegionCityTile district,
            Vector2 normalizedPoint, float brushRadius = 0)
        {
            if (district?.Rivers == null) return null;
            var widthMeters = DistrictScale.SizeMeters(district.Width);
            var depthMeters = DistrictScale.SizeMeters(district.Height);
            PlacedDistrictRiver closest = null;
            var closestDistance = float.PositiveInfinity;
            foreach (var river in district.Rivers)
            {
                if (river?.Points == null || river.Points.Count < 2) continue;
                var hitRadius = river.WidthMeters *
                    (river.Depth == DistrictRiverDepth.Deep ? .54f : .64f);
                hitRadius = Mathf.Max(hitRadius, brushRadius);
                for (var index = 0; index < river.Points.Count - 1; index++)
                {
                    var from = new Vector2(river.Points[index].X * widthMeters,
                        river.Points[index].Z * depthMeters);
                    var to = new Vector2(river.Points[index + 1].X * widthMeters,
                        river.Points[index + 1].Z * depthMeters);
                    var point = new Vector2(normalizedPoint.x * widthMeters,
                        normalizedPoint.y * depthMeters);
                    var segment = to - from;
                    var denominator = segment.sqrMagnitude;
                    var t = denominator <= .0001f ? 0f : Mathf.Clamp01(
                        Vector2.Dot(point - from, segment) / denominator);
                    var distance = Vector2.Distance(point, from + segment * t);
                    if (distance > hitRadius || distance >= closestDistance)
                        continue;
                    closestDistance = distance;
                    closest = river;
                }
            }
            return closest;
        }

        public static bool Move(PlacedDistrictRiver river,
            Vector2 normalizedDelta)
        {
            if (river?.Points == null || river.Points.Count == 0 || !string.IsNullOrEmpty(river.RegionRiverId)) return false;
            var minX = river.Points.Min(point => point.X);
            var maxX = river.Points.Max(point => point.X);
            var minZ = river.Points.Min(point => point.Z);
            var maxZ = river.Points.Max(point => point.Z);
            var delta = new Vector2(
                Mathf.Clamp(normalizedDelta.x, -minX, 1f - maxX),
                Mathf.Clamp(normalizedDelta.y, -minZ, 1f - maxZ));
            if (delta.sqrMagnitude <= .00000001f) return false;
            foreach (var point in river.Points)
            {
                point.X += delta.x;
                point.Z += delta.y;
            }
            return true;
        }
    }

    [Serializable]
    public sealed class PlacedDistrictFlora
    {
        public string InstanceId = "";
        public string GroupId = "";
        public string FloraId = "maple";
        public float NormalizedX = 0.5f;
        public float NormalizedZ = 0.5f;
        public float Scale = 1f;
        public int RotationEighthTurns;
        public DistrictTreeHarvestState HarvestState;
        public int HarvestDirection;
        public int RemainingWood;
        public bool WoodCredited;
    }

    public enum RegionPlaceDesignation
    {
        District,
        Town
    }

    [Serializable]
    public sealed class RegionCityTile
    {
        public string TileId = "";
        public string Name = "";
        public RegionPlaceDesignation Designation =
            RegionPlaceDesignation.District;
        public int X;
        public int Y;
        public int Width = 2;
        public int Height = 2;
        public string LotId = "";
        public bool Founded;
        public string FounderBuildingId = "";
        public string FounderBuildingName = "";
        public int FoundingYear;
        public TimeOfDayPreset TimeOfDay = TimeOfDayPreset.Noon;
        public float FounderNormalizedX = 0.5f;
        public float FounderNormalizedY = 0.5f;
        public List<PlacedDistrictLot> Lots = new();
        public List<PlacedRoadPiece> Roads = new();
        public List<PlacedDistrictRiver> Rivers = new();
        public bool RiversEditedLocally;
        public List<PlacedDistrictFlora> Flora = new();
        public int Treasury = 280000;
        public DistrictLaborState Labor = new();
        public DistrictWildlifeState Wildlife = new();
        public DistrictResourceInventory ResourceInventory = new();
        public DistrictHillSettings Hills = new();
        public RegionBiome Biome = RegionBiome.Grassland;
        public bool StoneDepositsGenerated;
        public List<DistrictStoneSite> StoneSites = new();
        public List<DistrictBrickworksSite> Brickworks = new();
        public string NaturalResourceGenerationKey = "";
        public List<DistrictResourceDeposit> ResourceDeposits = new();
    }

    [Serializable]
    public sealed class RegionSaveData
    {
        public string Schema = "cityforge-v3-region-v1";
        public string RegionId = "";
        public string Name = "Untitled Region";
        public string EraId = LotEraCatalog.DefaultId;
        public int Width = 28;
        public int Height = 20;
        public string ModifiedUtc = "";
        public RegionTerrainSettings Terrain = new();
        public RegionMapLayers MapLayers = new();
        public List<RegionTransportRoute> TransportRoutes = new();
        public int RiverSeed;
        public List<RegionRiverPath> RiverPaths = new();
        public List<RegionCityTile> Tiles = new();
    }

    public sealed class RegionSaveSummary
    {
        public string RegionId;
        public string Name;
        public int Width;
        public int Height;
        public int TileCount;
        public DateTime ModifiedUtc;
    }

    public static class RegionSaveStore
    {
        private const string FolderName = "CityForge/Regions";

        public static string DefaultRoot => Path.Combine(
            Application.persistentDataPath, FolderName);

        public static RegionSaveData Create(string name, int width = 28,
            int height = 20)
        {
            width = Mathf.Max(8, width / 2 * 2);
            height = Mathf.Max(8, height / 2 * 2);
            var data = new RegionSaveData
            {
                RegionId = Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(name)
                    ? "Untitled Region" : name.Trim(),
                Width = width,
                Height = height
            };
            var tileNumber = 1;
            for (var y = 0; y < height;)
            {
                var bandHeight = y % 6 == 0 && y + 4 <= height ? 4 : 2;
                for (var x = 0; x < width;)
                {
                    var wide = (x / 2 + y / 2) % 3 == 0 && x + 4 <= width;
                    var tileWidth = wide ? 4 : 2;
                    data.Tiles.Add(new RegionCityTile
                    {
                        TileId = $"city-{tileNumber:000}",
                        Name = $"City {tileNumber:000}",
                        X = x,
                        Y = y,
                        Width = tileWidth,
                        Height = bandHeight
                    });
                    tileNumber++;
                    x += tileWidth;
                }
                y += bandHeight;
            }
            return data;
        }

        public static void RegenerateTiles(RegionSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Width = Mathf.Max(8, data.Width / 2 * 2);
            data.Height = Mathf.Max(8, data.Height / 2 * 2);
            data.Tiles ??= new List<RegionCityTile>();
            data.Tiles.Clear();
            var random = new System.Random(Guid.NewGuid().GetHashCode());
            var tileNumber = 1;
            for (var y = 0; y < data.Height;)
            {
                var canUseTallBand = y + 4 <= data.Height;
                var bandHeight = canUseTallBand && random.NextDouble() < 0.42
                    ? 4 : 2;
                for (var x = 0; x < data.Width;)
                {
                    var canUseWideTile = x + 4 <= data.Width;
                    var tileWidth = canUseWideTile && random.NextDouble() < 0.46
                        ? 4 : 2;
                    data.Tiles.Add(new RegionCityTile
                    {
                        TileId = $"city-{tileNumber:000}",
                        Name = $"City {tileNumber:000}",
                        X = x,
                        Y = y,
                        Width = tileWidth,
                        Height = bandHeight
                    });
                    tileNumber++;
                    x += tileWidth;
                }
                y += bandHeight;
            }
            if (data.RiverPaths != null && data.RiverPaths.Count > 0)
                RegionRiverGenerator.Apply(data, data.RiverPaths);
        }

        public static string Save(RegionSaveData data, string root = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(data.RegionId))
                data.RegionId = Guid.NewGuid().ToString("N");
            data.ModifiedUtc = DateTime.UtcNow.ToString("O");
            root ??= DefaultRoot;
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, $"{data.RegionId}.json");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            return path;
        }

        public static RegionSaveData Load(string regionId, string root = null)
        {
            if (string.IsNullOrWhiteSpace(regionId)) return null;
            root ??= DefaultRoot;
            var path = Path.Combine(root, $"{regionId}.json");
            return File.Exists(path)
                ? JsonUtility.FromJson<RegionSaveData>(File.ReadAllText(path))
                : null;
        }

        public static bool Delete(string regionId, string root = null)
        {
            // IDs are filenames, never paths supplied by save contents.
            if (string.IsNullOrWhiteSpace(regionId) || regionId == "." || regionId == ".." ||
                regionId.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 ||
                regionId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Invalid region ID.", nameof(regionId));
            var path = Path.Combine(root ?? DefaultRoot, $"{regionId}.json");
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        public static List<RegionSaveSummary> List(string root = null)
        {
            root ??= DefaultRoot;
            if (!Directory.Exists(root)) return new List<RegionSaveSummary>();
            var summaries = new List<RegionSaveSummary>();
            foreach (var path in Directory.GetFiles(root, "*.json"))
            {
                try
                {
                    var data = JsonUtility.FromJson<RegionSaveData>(
                        File.ReadAllText(path));
                    if (data == null || string.IsNullOrWhiteSpace(data.RegionId))
                        continue;
                    DateTime.TryParse(data.ModifiedUtc, out var modified);
                    summaries.Add(new RegionSaveSummary
                    {
                        RegionId = data.RegionId,
                        Name = data.Name,
                        Width = data.Width,
                        Height = data.Height,
                        TileCount = data.Tiles?.Count ?? 0,
                        ModifiedUtc = modified
                    });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Skipping invalid region save {path}: {exception.Message}");
                }
            }
            return summaries.OrderByDescending(summary => summary.ModifiedUtc)
                .ToList();
        }
    }
}
