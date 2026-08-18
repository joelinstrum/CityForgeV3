using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum LotToolMode
    {
        Select,
        Place,
        Move
    }

    public enum BuildingInspectionMode
    {
        Artwork,
        Hybrid,
        Primitive
    }

    public static class BuildingInspectionPolicy
    {
        public static bool ShowsArtwork(BuildingInspectionMode mode) =>
            mode != BuildingInspectionMode.Primitive;

        public static bool ShowsPrimitive(BuildingInspectionMode mode) =>
            mode != BuildingInspectionMode.Artwork;

        public static bool ShowsFoundationFill(BuildingInspectionMode mode) =>
            mode == BuildingInspectionMode.Primitive;

        public static float ArtworkOpacity(
            BuildingInspectionMode mode,
            float contextOpacity) =>
            mode == BuildingInspectionMode.Hybrid
                ? Mathf.Min(contextOpacity, 0.20f)
                : contextOpacity;
    }

    public static class LotEraCatalog
    {
        public const string DefaultId = "founders";

        public static readonly IReadOnlyList<string> Ids = new[]
        {
            "founders", "industrial", "discovery", "modern"
        };

        public static readonly IReadOnlyList<string> DisplayNames = new[]
        {
            "Founders Era", "Industrial Age", "Age of Discovery", "Modern Age"
        };

        public static string DisplayName(string id)
        {
            var index = IndexOf(id);
            return DisplayNames[index];
        }

        public static string IdForDisplayName(string displayName)
        {
            for (var index = 0; index < DisplayNames.Count; index++)
                if (string.Equals(DisplayNames[index], displayName, StringComparison.OrdinalIgnoreCase))
                    return Ids[index];
            return DefaultId;
        }

        public static int IndexOf(string id)
        {
            for (var index = 0; index < Ids.Count; index++)
                if (string.Equals(Ids[index], id, StringComparison.OrdinalIgnoreCase))
                    return index;
            return 0;
        }
    }

    [Serializable]
    public sealed class PlacedBuilding
    {
        public string InstanceId = "";
        public string BuildingId = "";
        public int CellX;
        public int CellZ;
        public int RotationQuarterTurns;
        public List<PlacedBuildingProp> Attachments = new();
    }

    [Serializable]
    public sealed class PlacedBuildingProp
    {
        public string InstanceId = "";
        public string ComponentId = "";
        public string Revision = "";
        public string HostElevation = "Front";
        public float NormalizedX = 0.5f;
        public float NormalizedY = 0.5f;
        public float ProjectionDepthMeters = 0.18f;
        public float Scale = 1f;
        public float RotationDegrees;
        public bool HasHostLocalPosition;
        public float HostLocalX;
        public float HostLocalY;
        public float HostLocalZ;
    }

    [Serializable]
    public sealed class PlacedFlora
    {
        public string InstanceId = "";
        public string FloraId = "";
        public float PositionX;
        public float PositionZ;
    }

    [Serializable]
    public sealed class PlacedProp
    {
        public string InstanceId = "";
        public string PropId = "";
        public float PositionX;
        public float PositionZ;
        public int RotationQuarterTurns;
    }

    [Serializable]
    public sealed class PlacedOverlayTexture
    {
        public string InstanceId = "";
        public string TextureId = "";
        public int CellX;
        public int CellZ;
        public int RotationQuarterTurns;
    }

    [Serializable]
    public sealed class LotSaveData
    {
        public string Schema = "cityforge-v3-lot-save-v7";
        public string LotId = "untitled-lot";
        public string Name = "Untitled Lot";
        public string CreatedUtc = "";
        public string ModifiedUtc = "";
        public List<string> RequiredPackageIds = new();
        public int LotSizeMeters = 20;
        public int LotWidthCells = 2;
        public int LotDepthCells = 2;
        public LotType LotType = LotType.Residential;
        public string EraId = LotEraCatalog.DefaultId;
        public TrafficLotType TrafficType = TrafficLotType.None;
        public List<OutsideRoadConnector> OutsideRoadConnectors = new();
        public bool HasBuilding;
        public string BuildingId = "";
        public int CellX;
        public int CellZ;
        public int RotationQuarterTurns;
        public List<PlacedBuilding> Buildings = new();
        public List<PlacedFlora> Flora = new();
        public List<PlacedProp> Props = new();
        public string BaseTextureId = "";
        public List<PlacedOverlayTexture> OverlayTextures = new();
        public CirculationNetwork PedestrianNetwork = new() { Mode = CirculationMode.Pedestrian };
        public CirculationNetwork VehicleNetwork = new() { Mode = CirculationMode.Vehicle };
        public List<PlacedRoadPiece> RoadPieces = new();

        public LotSaveData Copy() =>
            new()
            {
                LotSizeMeters = LotSizeMeters,
                LotWidthCells = LotWidthCells,
                LotDepthCells = LotDepthCells,
                Schema = Schema,
                LotId = LotId,
                Name = Name,
                CreatedUtc = CreatedUtc,
                ModifiedUtc = ModifiedUtc,
                RequiredPackageIds = RequiredPackageIds,
                LotType = LotType,
                EraId = EraId,
                TrafficType = TrafficType,
                OutsideRoadConnectors = OutsideRoadConnectors,
                HasBuilding = HasBuilding,
                BuildingId = BuildingId,
                CellX = CellX,
                CellZ = CellZ,
                RotationQuarterTurns = RotationQuarterTurns,
                Buildings = Buildings,
                Flora = Flora,
                Props = Props,
                BaseTextureId = BaseTextureId,
                OverlayTextures = OverlayTextures,
                PedestrianNetwork = PedestrianNetwork,
                VehicleNetwork = VehicleNetwork,
                RoadPieces = RoadPieces
            };
    }

    public sealed class LotEditorSession
    {
        public const int MinimumX = -5;
        public const int MaximumX = 5;
        public const int MinimumZ = -7;
        public const int MaximumZ = 7;

        public LotSaveData Data { get; private set; } = new();
        private string _cleanSnapshot = "";
        public int SelectedBuildingIndex { get; private set; } = -1;
        public bool IsSelected { get; private set; }
        public LotToolMode ToolMode { get; private set; } = LotToolMode.Select;
        public bool IsDirty => _cleanSnapshot != Serialize();

        public LotEditorSession()
        {
            if (string.IsNullOrWhiteSpace(Data.CreatedUtc))
                Data.CreatedUtc = DateTime.UtcNow.ToString("O");
            _cleanSnapshot = Serialize();
        }

        public void NewLot(string name, LotType lotType, int sizeMeters)
        {
            var now = DateTime.UtcNow.ToString("O");
            var safeName = string.IsNullOrWhiteSpace(name) ? "Untitled Lot" : name.Trim();
            Data = new LotSaveData
            {
                LotId = LotSaveStore.Slug(safeName),
                Name = safeName,
                CreatedUtc = now,
                ModifiedUtc = now,
                LotType = lotType,
                LotSizeMeters = Mathf.Clamp(Mathf.RoundToInt(sizeMeters / 10f) * 10, 10, 80),
                LotWidthCells = Mathf.Clamp(Mathf.RoundToInt(sizeMeters / 10f), 1, 8),
                LotDepthCells = Mathf.Clamp(Mathf.RoundToInt(sizeMeters / 10f), 1, 8)
            };
            IsSelected = false;
            ToolMode = LotToolMode.Select;
            _cleanSnapshot = Serialize();
        }

        public void SetTool(LotToolMode mode)
        {
            ToolMode = mode;
        }

        public void SetLotType(LotType lotType)
        {
            Data.LotType = lotType;
        }

        public void SetEra(string eraId)
        {
            Data.EraId = LotEraCatalog.Ids[LotEraCatalog.IndexOf(eraId)];
        }

        public void SetTrafficType(TrafficLotType trafficType)
        {
            Data.TrafficType = trafficType;
        }

        public void SetLotSizeMeters(int meters)
        {
            var cells = Mathf.Clamp(Mathf.RoundToInt(meters / 10f), 1, 8);
            SetLotDimensions(cells, cells);
        }

        public void SetLotDimensions(int widthCells, int depthCells)
        {
            Data.LotWidthCells = Mathf.Clamp(widthCells, 1, 8);
            Data.LotDepthCells = Mathf.Clamp(depthCells, 1, 8);
            Data.LotSizeMeters = Mathf.Max(Data.LotWidthCells, Data.LotDepthCells) * 10;
        }

        public void Place(string buildingId, int cellX, int cellZ)
        {
            Data.Buildings ??= new List<PlacedBuilding>();
            Data.Flora ??= new List<PlacedFlora>();
            Data.Props ??= new List<PlacedProp>();
            Data.Flora ??= new List<PlacedFlora>();
            Data.Props ??= new List<PlacedProp>();
            Data.Buildings.Clear();
            AddBuilding(buildingId, cellX, cellZ);
        }

        public void AddBuilding(string buildingId, int cellX, int cellZ)
        {
            Data.Buildings ??= new List<PlacedBuilding>();
            var placed = new PlacedBuilding
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                BuildingId = buildingId,
                CellX = Mathf.Clamp(cellX, -Data.LotWidthCells * 5, Data.LotWidthCells * 5),
                CellZ = Mathf.Clamp(cellZ, -Data.LotDepthCells * 5, Data.LotDepthCells * 5),
                RotationQuarterTurns = 0
            };
            Data.Buildings.Add(placed);
            SelectedBuildingIndex = Data.Buildings.Count - 1;
            SyncLegacyBuilding();
            IsSelected = true;
            ToolMode = LotToolMode.Select;
        }

        public void SelectBuilding(int index)
        {
            SelectedBuildingIndex = Data.Buildings != null && index >= 0 && index < Data.Buildings.Count
                ? index : -1;
            IsSelected = SelectedBuildingIndex >= 0;
            SyncLegacyBuilding();
            ToolMode = LotToolMode.Select;
        }

        private void SyncLegacyBuilding()
        {
            var selected = Data.Buildings != null && SelectedBuildingIndex >= 0 &&
                SelectedBuildingIndex < Data.Buildings.Count
                ? Data.Buildings[SelectedBuildingIndex] : null;
            Data.HasBuilding = Data.Buildings != null && Data.Buildings.Count > 0;
            Data.BuildingId = selected?.BuildingId ?? "";
            Data.CellX = selected?.CellX ?? 0;
            Data.CellZ = selected?.CellZ ?? 0;
            Data.RotationQuarterTurns = selected?.RotationQuarterTurns ?? 0;
        }

        private void SyncSelectedBuilding()
        {
            if (Data.Buildings == null || SelectedBuildingIndex < 0 ||
                SelectedBuildingIndex >= Data.Buildings.Count) return;
            var selected = Data.Buildings[SelectedBuildingIndex];
            selected.CellX = Data.CellX;
            selected.CellZ = Data.CellZ;
            selected.RotationQuarterTurns = Data.RotationQuarterTurns;
        }

        public void Select(bool selected)
        {
            IsSelected = Data.HasBuilding && selected;
            ToolMode = LotToolMode.Select;
        }

        public void Move(int cellX, int cellZ)
        {
            if (!Data.HasBuilding)
            {
                return;
            }

            Data.CellX = Mathf.Clamp(cellX, -Data.LotWidthCells * 5, Data.LotWidthCells * 5);
            Data.CellZ = Mathf.Clamp(cellZ, -Data.LotDepthCells * 5, Data.LotDepthCells * 5);
            SyncSelectedBuilding();
            IsSelected = true;
            ToolMode = LotToolMode.Select;
        }

        public void Nudge(int deltaX, int deltaZ)
        {
            Move(Data.CellX + deltaX, Data.CellZ + deltaZ);
        }

        public void Rotate(int direction)
        {
            if (!Data.HasBuilding)
            {
                return;
            }

            Data.RotationQuarterTurns =
                FiveBayHybridContract.WrapFacing(Data.RotationQuarterTurns + direction);
            SyncSelectedBuilding();
            IsSelected = true;
        }

        public static string CardinalOrientation(int quarterTurns)
        {
            return FiveBayHybridContract.WrapFacing(quarterTurns) switch
            {
                0 => "North",
                1 => "East",
                2 => "South",
                _ => "West"
            };
        }

        public void Delete()
        {
            if (Data.Buildings != null && SelectedBuildingIndex >= 0 &&
                SelectedBuildingIndex < Data.Buildings.Count)
                Data.Buildings.RemoveAt(SelectedBuildingIndex);
            SelectedBuildingIndex = Data.Buildings != null && Data.Buildings.Count > 0
                ? Mathf.Min(SelectedBuildingIndex, Data.Buildings.Count - 1) : -1;
            IsSelected = SelectedBuildingIndex >= 0;
            SyncLegacyBuilding();
            ToolMode = LotToolMode.Select;
        }

        public string Serialize() => JsonUtility.ToJson(Data);

        public void Restore(string json)
        {
            var restored = string.IsNullOrWhiteSpace(json)
                ? new LotSaveData()
                : JsonUtility.FromJson<LotSaveData>(json);
            Data = restored ?? new LotSaveData();
            var legacySquare = Data.Schema == "cityforge-v3-lot-save-v2";
            if (Data.LotSizeMeters == 0) Data.LotSizeMeters = 20;
            if (legacySquare || Data.LotWidthCells <= 0)
                Data.LotWidthCells = Mathf.Clamp(Data.LotSizeMeters / 10, 1, 8);
            if (legacySquare || Data.LotDepthCells <= 0)
                Data.LotDepthCells = Mathf.Clamp(Data.LotSizeMeters / 10, 1, 8);
            SetLotDimensions(Data.LotWidthCells, Data.LotDepthCells);
            Data.Buildings ??= new List<PlacedBuilding>();
            if (Data.Buildings.Count == 0 && Data.HasBuilding &&
                !string.IsNullOrWhiteSpace(Data.BuildingId))
                Data.Buildings.Add(new PlacedBuilding
                {
                    InstanceId = Guid.NewGuid().ToString("N"),
                    BuildingId = Data.BuildingId,
                    CellX = Data.CellX,
                    CellZ = Data.CellZ,
                    RotationQuarterTurns = Data.RotationQuarterTurns
                });
            foreach (var building in Data.Buildings)
            {
                if (string.IsNullOrWhiteSpace(building.InstanceId))
                    building.InstanceId = Guid.NewGuid().ToString("N");
                building.RotationQuarterTurns =
                    FiveBayHybridContract.WrapFacing(building.RotationQuarterTurns);
                building.Attachments ??= new List<PlacedBuildingProp>();
            }
            Data.RotationQuarterTurns =
                FiveBayHybridContract.WrapFacing(Data.RotationQuarterTurns);
            Data.PedestrianNetwork ??= new CirculationNetwork { Mode = CirculationMode.Pedestrian };
            Data.VehicleNetwork ??= new CirculationNetwork { Mode = CirculationMode.Vehicle };
            Data.RoadPieces ??= new List<PlacedRoadPiece>();
            foreach (var road in Data.RoadPieces)
                if (road != null && string.IsNullOrWhiteSpace(road.PackageId))
                    road.PackageId = RoadPiecePackage.LegacyPackageId;
            Data.OutsideRoadConnectors ??= new List<OutsideRoadConnector>();
            Data.RequiredPackageIds ??= new List<string>();
            Data.OverlayTextures ??= new List<PlacedOverlayTexture>();
            SetEra(Data.EraId);
            Data.Schema = "cityforge-v3-lot-save-v7";
            if (string.IsNullOrWhiteSpace(Data.Name)) Data.Name = "Untitled Lot";
            if (string.IsNullOrWhiteSpace(Data.LotId)) Data.LotId = LotSaveStore.Slug(Data.Name);
            Data.PedestrianNetwork.Mode = CirculationMode.Pedestrian;
            Data.VehicleNetwork.Mode = CirculationMode.Vehicle;
            SelectedBuildingIndex = Data.Buildings.Count > 0 ? 0 : -1;
            IsSelected = SelectedBuildingIndex >= 0;
            SyncLegacyBuilding();
            ToolMode = LotToolMode.Select;
            _cleanSnapshot = Serialize();
        }

        public void PrepareForSave(IEnumerable<string> requiredPackageIds)
        {
            Data.ModifiedUtc = DateTime.UtcNow.ToString("O");
            Data.RequiredPackageIds = new List<string>(requiredPackageIds ?? Array.Empty<string>());
        }

        public void MarkClean() => _cleanSnapshot = Serialize();

        public void Rename(string name)
        {
            Data.Name = string.IsNullOrWhiteSpace(name) ? "Untitled Lot" : name.Trim();
        }

        public void ForkAs(string name, string lotId)
        {
            var now = DateTime.UtcNow.ToString("O");
            Data.Name = string.IsNullOrWhiteSpace(name) ? "Untitled Lot" : name.Trim();
            Data.LotId = lotId;
            Data.CreatedUtc = now;
            Data.ModifiedUtc = now;
        }
    }

    public sealed class LotSaveSummary
    {
        public string LotId;
        public string Name;
        public LotType LotType;
        public int LotSizeMeters;
        public int LotWidthCells;
        public int LotDepthCells;
        public string ModifiedUtc;
        public string Path;
        public string BuildingId;
        public List<string> RequiredPackageIds = new();
    }

    public static class LotSaveStore
    {
        public const string FolderName = "CityForge/Lots";

        public static string DefaultRoot => Path.Combine(Application.persistentDataPath, FolderName);

        public static string Slug(string value)
        {
            var result = "";
            foreach (var character in (value ?? "").ToLowerInvariant())
                if (char.IsLetterOrDigit(character)) result += character;
                else if ((character == ' ' || character == '-' || character == '_') &&
                    !result.EndsWith("-")) result += "-";
            result = result.Trim('-');
            return string.IsNullOrWhiteSpace(result) ? "untitled-lot" : result;
        }

        public static string Save(LotEditorSession session,
            IEnumerable<string> requiredPackageIds, string root = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            session.PrepareForSave(requiredPackageIds);
            root ??= DefaultRoot;
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, $"{Slug(session.Data.LotId)}.json");
            File.WriteAllText(path, session.Serialize());
            session.MarkClean();
            return path;
        }

        public static bool Load(LotEditorSession session, string lotId, string root = null)
        {
            root ??= DefaultRoot;
            var path = Path.Combine(root, $"{Slug(lotId)}.json");
            if (!File.Exists(path)) return false;
            session.Restore(File.ReadAllText(path));
            return true;
        }

        public static LotSaveData Read(string lotId, string root = null)
        {
            root ??= DefaultRoot;
            var path = Path.Combine(root, $"{Slug(lotId)}.json");
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<LotSaveData>(File.ReadAllText(path));
        }

        public static string UniqueId(string name, string root = null)
        {
            root ??= DefaultRoot;
            var stem = Slug(name);
            var candidate = stem;
            var suffix = 2;
            while (File.Exists(Path.Combine(root, $"{candidate}.json")))
                candidate = $"{stem}-{suffix++}";
            return candidate;
        }

        public static bool Delete(string lotId, string root = null)
        {
            root ??= DefaultRoot;
            var path = Path.Combine(root, $"{Slug(lotId)}.json");
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        public static LotSaveSummary Duplicate(string lotId, string root = null)
        {
            root ??= DefaultRoot;
            var data = Read(lotId, root);
            if (data == null) return null;
            var session = new LotEditorSession();
            session.Restore(JsonUtility.ToJson(data));
            var copyName = $"{data.Name} Copy";
            session.ForkAs(copyName, UniqueId(copyName, root));
            Save(session, data.RequiredPackageIds, root);
            return List(root).Find(summary => summary.LotId == session.Data.LotId);
        }

        public static List<string> MissingDependencies(
            string lotId, IEnumerable<string> availablePackageIds, string root = null)
        {
            var data = Read(lotId, root);
            var missing = new List<string>();
            if (data == null) return missing;
            var available = new HashSet<string>(
                availablePackageIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            foreach (var required in data.RequiredPackageIds ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(required) && !available.Contains(required))
                    missing.Add(required);
            return missing;
        }

        public static List<LotSaveSummary> List(string root = null)
        {
            root ??= DefaultRoot;
            var summaries = new List<LotSaveSummary>();
            if (!Directory.Exists(root)) return summaries;
            foreach (var path in Directory.GetFiles(root, "*.json"))
            {
                var data = JsonUtility.FromJson<LotSaveData>(File.ReadAllText(path));
                if (data == null || (data.Schema != "cityforge-v3-lot-save-v2" &&
                    data.Schema != "cityforge-v3-lot-save-v3" &&
                    data.Schema != "cityforge-v3-lot-save-v4" &&
                    data.Schema != "cityforge-v3-lot-save-v5" &&
                    data.Schema != "cityforge-v3-lot-save-v6" &&
                    data.Schema != "cityforge-v3-lot-save-v7")) continue;
                summaries.Add(new LotSaveSummary
                {
                    LotId = data.LotId,
                    Name = data.Name,
                    LotType = data.LotType,
                    LotSizeMeters = data.LotSizeMeters,
                    LotWidthCells = data.Schema == "cityforge-v3-lot-save-v2"
                        ? data.LotSizeMeters / 10
                        : data.LotWidthCells,
                    LotDepthCells = data.Schema == "cityforge-v3-lot-save-v2"
                        ? data.LotSizeMeters / 10
                        : data.LotDepthCells,
                    ModifiedUtc = data.ModifiedUtc,
                    Path = path,
                    BuildingId = data.Buildings != null && data.Buildings.Count > 0
                        ? data.Buildings[0].BuildingId : data.BuildingId,
                    RequiredPackageIds = data.RequiredPackageIds ?? new List<string>()
                });
            }
            summaries.Sort((left, right) => string.CompareOrdinal(right.ModifiedUtc, left.ModifiedUtc));
            return summaries;
        }
    }
}
