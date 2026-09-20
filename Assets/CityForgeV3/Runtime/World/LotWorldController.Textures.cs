using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        public enum PedestrianOverlayLayout
        {
            None,
            Centerline,
            ParallelFlanks,
            Stairs
        }

        public sealed class LotTextureOption
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string ResourcePath;
            public readonly string SpringResourcePath;
            public readonly string SummerResourcePath;
            public readonly string AutumnResourcePath;
            public readonly string WinterResourcePath;
            public readonly PedestrianOverlayLayout PedestrianLayout;
            public readonly float PedestrianWidthMeters;
            public readonly float StairRiseMeters;
            public readonly float BaseRepeatMeters;
            public readonly int FootprintWidthCells;
            public readonly int FootprintDepthCells;

            public LotTextureOption(string id, string displayName, string resourcePath,
                string springResourcePath = null, string summerResourcePath = null,
                string autumnResourcePath = null, string winterResourcePath = null,
                PedestrianOverlayLayout pedestrianLayout =
                    PedestrianOverlayLayout.None,
                float pedestrianWidthMeters = 1.8f,
                float stairRiseMeters = 0f,
                float baseRepeatMeters = 5f,
                int footprintWidthCells = 1,
                int footprintDepthCells = 1)
            {
                Id = id; DisplayName = displayName; ResourcePath = resourcePath;
                SpringResourcePath = springResourcePath;
                SummerResourcePath = summerResourcePath;
                AutumnResourcePath = autumnResourcePath;
                WinterResourcePath = winterResourcePath;
                PedestrianLayout = pedestrianLayout;
                PedestrianWidthMeters = pedestrianWidthMeters;
                StairRiseMeters = stairRiseMeters;
                BaseRepeatMeters = baseRepeatMeters;
                FootprintWidthCells = Mathf.Max(1, footprintWidthCells);
                FootprintDepthCells = Mathf.Max(1, footprintDepthCells);
            }

            public bool HasResourceForSeason(SeasonPreset season) =>
                !string.IsNullOrWhiteSpace(ResourceForSeason(season));

            public string ResolveResourcePath(SeasonPreset season)
            {
                var exact = ResourceForSeason(season);
                if (!string.IsNullOrWhiteSpace(exact)) return exact;
                // Prefer the neutral/default summer artwork, then use a stable
                // seasonal order. The legacy resource is the final fallback.
                foreach (var fallback in new[]
                         {
                             SummerResourcePath, SpringResourcePath,
                             AutumnResourcePath, WinterResourcePath
                         })
                    if (!string.IsNullOrWhiteSpace(fallback)) return fallback;
                return ResourcePath;
            }

            private string ResourceForSeason(SeasonPreset season) => season switch
            {
                SeasonPreset.Spring => SpringResourcePath,
                SeasonPreset.Summer => SummerResourcePath,
                SeasonPreset.Autumn => AutumnResourcePath,
                SeasonPreset.Winter => WinterResourcePath,
                _ => null
            };
        }

        public static readonly IReadOnlyList<LotTextureOption> GrassBaseTextures = new[]
        {
            new LotTextureOption("default-grass", "Natural Grass",
                DistrictWorldController.DefaultGrassResource),
            new LotTextureOption("brick-paving-v01", "Brick Paving",
                "CityForgeV3/LotTextures/BrickPavingV01/brick-texture-1",
                baseRepeatMeters: 10f),
            new LotTextureOption("dark-cobblestone-v01", "Cobblestone",
                "CityForgeV3/LotTextures/CobblestoneSuppliedV01/cobblestone-texture",
                baseRepeatMeters: 10f),
            new LotTextureOption("wild-grass-lawn", "Wild Grass Lawn",
                "CityForgeV3/LotTextures/RuralOverlaysV01/wild-grass-lawn"),
            new LotTextureOption("grass-poor", "Natural Grass — Poor", "CityForgeV3/LotTextures/LegacyGrassV01/lawn-poor-2"),
            new LotTextureOption("grass-middle", "Natural Grass — Middle", "CityForgeV3/LotTextures/LegacyGrassV01/lawn-middle-2"),
            new LotTextureOption("grass-lush", "Natural Grass — Lush",
                "CityForgeV3/LotTextures/LegacyGrassV01/lawn-lush-2",
                "CityForgeV3/LotTextures/SeasonalLushV01/lawn-lush-spring",
                "CityForgeV3/LotTextures/SeasonalLushV01/lawn-lush-summer",
                "CityForgeV3/LotTextures/SeasonalLushV01/lawn-lush-autumn",
                "CityForgeV3/LotTextures/SeasonalLushV01/lawn-lush-winter"),
            new LotTextureOption("lawn-poor", "Mowed Lawn — Poor", "CityForgeV3/LotTextures/LegacyGrassV01/lawn-poor"),
            new LotTextureOption("lawn-middle", "Mowed Lawn — Middle", "CityForgeV3/LotTextures/LegacyGrassV01/lawn-middle"),
            new LotTextureOption("lawn-wealthy", "Mowed Lawn — Wealthy", "CityForgeV3/LotTextures/LegacyGrassV01/lawn-wealthy"),
        };
        private static readonly LotTextureOption LegacyBrickWalkwayOverlay =
            new("brick-walkway", "Brick Walkway",
                "CityForgeV3/LotTextures/LegacyOverlaysV01/brick-walkway",
                pedestrianLayout: PedestrianOverlayLayout.Centerline);
        public static readonly IReadOnlyList<LotTextureOption> OverlayTextures = new[]
        {
            new LotTextureOption("brick-paving-v01", "Brick Paving",
                "CityForgeV3/LotTextures/BrickPavingV01/brick-texture-1"),
            new LotTextureOption("dark-cobblestone-v01", "Cobblestone",
                "CityForgeV3/LotTextures/CobblestoneSuppliedV01/cobblestone-texture"),
            new LotTextureOption("brick-sidewalk-straight", "Brick Sidewalk — Straight",
                "CityForgeV3/LotTextures/BrickSidewalkV01/brick-sidewalk-straight"),
            new LotTextureOption("brick-sidewalk-corner", "Brick Sidewalk — Corner",
                "CityForgeV3/LotTextures/BrickSidewalkV01/brick-sidewalk-corner"),
            new LotTextureOption("brick-sidewalk-t-junction", "Brick Sidewalk — T-Junction",
                "CityForgeV3/LotTextures/BrickSidewalkV01/brick-sidewalk-t-junction"),
            new LotTextureOption("dirt-path-straight", "Dirt Path — Straight",
                "CityForgeV3/LotTextures/DirtPathV01/dirt-path-straight"),
            new LotTextureOption("dirt-path-curve", "Dirt Path — Curve",
                "CityForgeV3/LotTextures/DirtPathV01/dirt-path-curve"),
            new LotTextureOption("dirt-path-tee", "Dirt Path — T-Junction",
                "CityForgeV3/LotTextures/DirtPathV01/dirt-path-tee"),
            new LotTextureOption("dirt-path-cross", "Dirt Path — Cross",
                "CityForgeV3/LotTextures/DirtPathV01/dirt-path-cross"),
            new LotTextureOption("concrete-sidewalk", "Concrete Sidewalk",
                "CityForgeV3/LotTextures/UrbanOverlaysV01/concrete-sidewalk",
                pedestrianLayout: PedestrianOverlayLayout.Centerline),
            new LotTextureOption("fancy-sidewalk", "Fancy Sidewalk",
                "CityForgeV3/LotTextures/UrbanOverlaysV01/fancy-sidewalk-overlay",
                pedestrianLayout: PedestrianOverlayLayout.Centerline),
            new LotTextureOption("worn-sidewalk", "Worn Sidewalk",
                "CityForgeV3/LotTextures/UrbanOverlaysV01/worn-sidewalk-overlay",
                pedestrianLayout: PedestrianOverlayLayout.Centerline),
            new LotTextureOption("sidewalk-flanks", "Sidewalk Flanks",
                "CityForgeV3/LotTextures/UrbanOverlaysV01/sidewalk-flanks",
                pedestrianLayout: PedestrianOverlayLayout.ParallelFlanks),
            new LotTextureOption("pedestrian-stairs", "Pedestrian Stairs",
                "CityForgeV3/LotTextures/UrbanOverlaysV01/concrete-sidewalk",
                pedestrianLayout: PedestrianOverlayLayout.Stairs,
                pedestrianWidthMeters: 2.2f, stairRiseMeters: 3.2f),
            new LotTextureOption("camp-overlay-4x4", "Camp Ground",
                "CityForgeV3/LotTextures/CampOverlayV01/camp-overlay-4x4",
                footprintWidthCells: 2, footprintDepthCells: 2),
        };
        public static LotTextureOption BrickWalkwayOverlay =>
            LegacyBrickWalkwayOverlay;

        public static LotTextureOption ResolveOverlayTexture(string id)
        {
            foreach (var option in OverlayTextures)
                if (string.Equals(option.Id, id, StringComparison.OrdinalIgnoreCase)) return option;
            // Preserve old saved lots that placed Wild Grass Lawn as an
            // overlay. New selections expose it only as a whole-lot base.
            if (string.Equals(id, "wild-grass-lawn",
                    StringComparison.OrdinalIgnoreCase))
                return ResolveBaseTexture(id);
            // The retired Brick Walkway stays resolvable for old saved lots,
            // but is intentionally absent from the authoring menu.
            if (string.Equals(id, "brick-walkway",
                    StringComparison.OrdinalIgnoreCase))
                return LegacyBrickWalkwayOverlay;
            return OverlayTextures[0];
        }

        public static LotTextureOption ResolveBaseTexture(string id)
        {
            foreach (var option in GrassBaseTextures)
                if (string.Equals(option.Id, id, StringComparison.OrdinalIgnoreCase))
                    return option;
            return null;
        }

        private Transform _overlayTextureRoot;
        private Transform _overlayTextureSelection;
        private readonly List<Renderer> _overlayTextureRenderers = new();
        private bool _overlayEditorActive;
        private bool _overlayPaintStrokeActive;
        private string _overlayPaintTextureId = "";
        private int _overlayPaintRotationQuarterTurns;
        private Vector2Int _lastOverlayPaintCell = new(-1, -1);
        public string BaseTextureId => _session.Data.BaseTextureId ?? "";
        public int OverlayTextureCount => _session.Data.OverlayTextures?.Count ?? 0;
        public int SelectedOverlayTextureIndex { get; private set; } = -1;

        private void BuildLotTextureRoot()
        {
            _overlayTextureRoot = new GameObject("Placed Overlay Textures").transform;
            _overlayTextureRoot.SetParent(transform, false);
            var selection = GameObject.CreatePrimitive(PrimitiveType.Quad);
            selection.name = "Selected Overlay Texture";
            selection.transform.SetParent(transform, false);
            selection.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            selection.transform.localScale = new Vector3(10f, 10f, 1f);
            selection.GetComponent<Collider>().enabled = false;
            selection.GetComponent<Renderer>().sharedMaterial = LotSurfaceMaterial(
                new Color(1f, 0.68f, 0.08f, 0.24f), 2001);
            _overlayTextureSelection = selection.transform;
            _overlayTextureSelection.gameObject.SetActive(false);
            ApplyBaseTexturePresentation();
        }

        public void SetOverlayEditorContext(bool active)
        {
            _overlayEditorActive = active && !_cameraPanInteractionActive;
            ApplyOverlayTextureSelection();
        }

        public void SetBaseTexture(string textureId)
        {
            _session.Data.BaseTextureId = textureId ?? "";
            ApplyBaseTexturePresentation();
            NotifyStateChanged();
        }

        public bool PlaceOverlayTextureFromPanel(string textureId, Vector2 panelPosition, Vector2 panelSize)
        {
            if (!TryOverlayCellFromPanel(panelPosition, panelSize, out var cell)) return false;
            var added = AddOverlayTextureAtCell(textureId, cell);
            if (added) NotifyStateChanged();
            return added;
        }

        public bool BeginOverlayPaintFromPanel(string armedTextureId,
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_overlayEditorActive ||
                !TryOverlayCellFromPanel(panelPosition, panelSize, out var cell)) return false;
            return BeginOverlayPaintAtCell(armedTextureId, cell.x, cell.y);
        }

        public bool BeginOverlayPaintAtCell(string armedTextureId, int cellX, int cellZ)
        {
            if (!_overlayEditorActive || !OverlayCellInPaintRange(cellX, cellZ))
                return false;
            var cell = new Vector2Int(cellX, cellZ);
            var existing = OverlayTextureIndexAtCell(cell);
            if (existing >= 0)
            {
                SelectedOverlayTextureIndex = existing;
                _overlayPaintTextureId = _session.Data.OverlayTextures[existing].TextureId;
                _overlayPaintRotationQuarterTurns =
                    _session.Data.OverlayTextures[existing].RotationQuarterTurns;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(armedTextureId)) return false;
                if (!AddOverlayTextureAtCell(armedTextureId, cell,
                        _overlayPaintRotationQuarterTurns)) return false;
                _overlayPaintTextureId = armedTextureId;
            }
            var option = ResolveOverlayTexture(_overlayPaintTextureId);
            _lastOverlayPaintCell = cell;
            _overlayPaintStrokeActive = true;
            ApplyOverlayTextureSelection();
            return true;
        }

        public bool PaintOverlayStrokeFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_overlayPaintStrokeActive ||
                !TryOverlayCellFromPanel(panelPosition, panelSize, out var cell)) return false;
            return PaintOverlayStrokeCell(cell.x, cell.y);
        }

        public bool PaintOverlayStrokeCell(int cellX, int cellZ)
        {
            var cell = new Vector2Int(cellX, cellZ);
            if (!_overlayPaintStrokeActive ||
                !OverlayCellInPaintRange(cellX, cellZ) ||
                cell == _lastOverlayPaintCell) return false;
            var paintOption = ResolveOverlayTexture(_overlayPaintTextureId);
            if (paintOption.FootprintWidthCells > 1 ||
                paintOption.FootprintDepthCells > 1) return false;
            _lastOverlayPaintCell = cell;
            var existing = OverlayTextureIndexAtCell(cell);
            if (existing >= 0)
            {
                SelectedOverlayTextureIndex = existing;
                ApplyOverlayTextureSelection();
                return false;
            }
            return AddOverlayTextureAtCell(_overlayPaintTextureId, cell,
                _overlayPaintRotationQuarterTurns);
        }

        public void EndOverlayPaint()
        {
            var wasActive = _overlayPaintStrokeActive;
            _overlayPaintStrokeActive = false;
            _lastOverlayPaintCell = new Vector2Int(-1, -1);
            if (wasActive) NotifyStateChanged();
        }

        public bool DeleteSelectedOverlayTexture()
        {
            if (!_overlayEditorActive || SelectedOverlayTextureIndex < 0 ||
                SelectedOverlayTextureIndex >= OverlayTextureCount) return false;
            _session.Data.OverlayTextures.RemoveAt(SelectedOverlayTextureIndex);
            SelectedOverlayTextureIndex = -1;
            RebuildOverlayTexturePresentations();
            RebuildPedestrianNetworkFromOverlays();
            NotifyStateChanged();
            return true;
        }

        public bool RotateSelectedOverlayTexture(int quarterTurnDelta)
        {
            if (!_overlayEditorActive || SelectedOverlayTextureIndex < 0 ||
                SelectedOverlayTextureIndex >= OverlayTextureCount ||
                quarterTurnDelta == 0) return false;
            var placed = _session.Data.OverlayTextures[SelectedOverlayTextureIndex];
            placed.RotationQuarterTurns =
                ((placed.RotationQuarterTurns + quarterTurnDelta) % 4 + 4) % 4;
            _overlayPaintRotationQuarterTurns = placed.RotationQuarterTurns;
            RebuildOverlayTexturePresentations();
            RebuildPedestrianNetworkFromOverlays();
            NotifyStateChanged();
            return true;
        }

        public void ClearOverlayTextureSelection()
        {
            SelectedOverlayTextureIndex = -1;
            _overlayPaintStrokeActive = false;
            _overlayPaintRotationQuarterTurns = 0;
            _lastOverlayPaintCell = new Vector2Int(-1, -1);
            ApplyOverlayTextureSelection();
        }

        private bool AddOverlayTextureAtCell(string textureId, Vector2Int cell,
            int rotationQuarterTurns = 0)
        {
            _session.Data.OverlayTextures ??= new List<PlacedOverlayTexture>();
            var option = ResolveOverlayTexture(textureId);
            if (!TryNormalizeOverlayAnchor(option, rotationQuarterTurns, cell,
                    out cell)) return false;
            var existing = OverlayTextureIndexAtCell(cell);
            if (existing >= 0)
            {
                SelectedOverlayTextureIndex = existing;
                ApplyOverlayTextureSelection();
                return false;
            }
            _session.Data.OverlayTextures.Add(new PlacedOverlayTexture
            {
                InstanceId = Guid.NewGuid().ToString("N"), TextureId = textureId,
                CellX = cell.x, CellZ = cell.y,
                RotationQuarterTurns =
                    ((rotationQuarterTurns % 4) + 4) % 4
            });
            SelectedOverlayTextureIndex = _session.Data.OverlayTextures.Count - 1;
            RebuildOverlayTexturePresentations();
            RebuildPedestrianNetworkFromOverlays();
            return true;
        }

        private bool TryOverlayCellFromPanel(Vector2 panelPosition, Vector2 panelSize,
            out Vector2Int cell)
        {
            cell = default;
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point) ||
                point.x < -LotWidthMeters * 0.5f - 10f ||
                point.x >= LotWidthMeters * 0.5f + 10f ||
                point.z < -LotDepthMeters * 0.5f - 10f ||
                point.z >= LotDepthMeters * 0.5f + 10f) return false;
            cell = new Vector2Int(
                Mathf.FloorToInt((point.x + LotWidthMeters * 0.5f) / 10f),
                Mathf.FloorToInt((point.z + LotDepthMeters * 0.5f) / 10f));
            return OverlayCellInPaintRange(cell.x, cell.y);
        }

        private bool OverlayCellInPaintRange(int cellX, int cellZ) =>
            cellX >= -1 && cellX <= LotWidthCells &&
            cellZ >= -1 && cellZ <= LotDepthCells;

        private int OverlayTextureIndexAtCell(Vector2Int cell)
        {
            for (var index = OverlayTextureCount - 1; index >= 0; index--)
            {
                var placed = _session.Data.OverlayTextures[index];
                var size = OverlayFootprintCells(placed);
                if (cell.x >= placed.CellX && cell.x < placed.CellX + size.x &&
                    cell.y >= placed.CellZ && cell.y < placed.CellZ + size.y)
                    return index;
            }
            return -1;
        }

        public static Vector2Int OverlayFootprintCells(PlacedOverlayTexture placed)
        {
            if (placed == null) return Vector2Int.one;
            var option = ResolveOverlayTexture(placed.TextureId);
            return OverlayFootprintCells(option, placed.RotationQuarterTurns);
        }

        public static Vector2Int OverlayFootprintCells(LotTextureOption option,
            int rotationQuarterTurns = 0)
        {
            if (option == null) return Vector2Int.one;
            return Mathf.Abs(rotationQuarterTurns) % 2 == 0
                ? new Vector2Int(option.FootprintWidthCells,
                    option.FootprintDepthCells)
                : new Vector2Int(option.FootprintDepthCells,
                    option.FootprintWidthCells);
        }

        public static bool OverlayFootprintFitsLot(int footprintCells,
            int lotWidthCells, int lotDepthCells) =>
            footprintCells >= 1 && footprintCells <= lotWidthCells &&
            footprintCells <= lotDepthCells;

        private bool TryNormalizeOverlayAnchor(LotTextureOption option,
            int rotationQuarterTurns, Vector2Int clickedCell,
            out Vector2Int anchor)
        {
            var size = OverlayFootprintCells(option, rotationQuarterTurns);
            if (size == Vector2Int.one)
            {
                anchor = clickedCell;
                return OverlayCellInPaintRange(anchor.x, anchor.y);
            }
            if (size.x > LotWidthCells || size.y > LotDepthCells)
            {
                anchor = default;
                return false;
            }
            anchor = new Vector2Int(
                Mathf.Clamp(clickedCell.x - size.x / 2, 0,
                    LotWidthCells - size.x),
                Mathf.Clamp(clickedCell.y - size.y / 2, 0,
                    LotDepthCells - size.y));
            var candidate = new RectInt(anchor, size);
            foreach (var placed in _session.Data.OverlayTextures)
            {
                if (placed == null) continue;
                var placedRect = new RectInt(
                    new Vector2Int(placed.CellX, placed.CellZ),
                    OverlayFootprintCells(placed));
                if (candidate.Overlaps(placedRect)) return false;
            }
            return true;
        }

        private void RebuildPedestrianNetworkFromOverlays()
        {
            var overlays = _session.Data.OverlayTextures ??
                new List<PlacedOverlayTexture>();
            var semantic = overlays.FindAll(placed => placed != null &&
                ResolveOverlayTexture(placed.TextureId).PedestrianLayout !=
                PedestrianOverlayLayout.None);
            var previous = _session.Data.PedestrianNetwork;
            var hadGeneratedNetwork = previous?.Nodes?.Exists(node => node != null &&
                (node.PortId ?? "").StartsWith("overlay-",
                    StringComparison.Ordinal)) == true;
            if (semantic.Count == 0 && !hadGeneratedNetwork) return;

            var network = new CirculationNetwork
            {
                Mode = CirculationMode.Pedestrian
            };
            // User-drawn paths remain authoritative additions. Regenerating
            // semantic sidewalk routes must never erase the familiar
            // click-to-click path chains.
            foreach (var node in previous?.Nodes ?? new List<CirculationNode>())
                if (node != null && (node.PortId ?? "").StartsWith("manual-",
                        StringComparison.Ordinal))
                    network.Nodes.Add(new CirculationNode
                    {
                        Id = node.Id,
                        PositionMeters = node.PositionMeters,
                        ElevationMeters = node.ElevationMeters,
                        Kind = node.Kind,
                        PortId = node.PortId
                    });
            foreach (var segment in previous?.Segments ??
                     new List<CirculationSegment>())
                if (segment != null &&
                    network.FindNode(segment.StartNodeId) != null &&
                    network.FindNode(segment.EndNodeId) != null)
                    network.Segments.Add(new CirculationSegment
                    {
                        Id = segment.Id,
                        StartNodeId = segment.StartNodeId,
                        EndNodeId = segment.EndNodeId,
                        Direction = segment.Direction,
                        WidthMeters = segment.WidthMeters,
                        SpeedMetersPerSecond = segment.SpeedMetersPerSecond,
                        PedestrianPathKind = segment.PedestrianPathKind
                    });
            CirculationNode FindOrAdd(Vector2 position, float elevation,
                CirculationNodeKind kind, string portId)
            {
                var existing = network.Nodes.Find(node => node != null &&
                    Vector2.Distance(node.PositionMeters, position) < 0.05f &&
                    Mathf.Abs(node.ElevationMeters - elevation) < 0.05f);
                if (existing != null)
                {
                    if (kind == CirculationNodeKind.BuildingEntrance)
                    {
                        existing.Kind = kind;
                        existing.PortId = portId;
                    }
                    return existing;
                }
                return network.AddNode(position, kind, portId, elevation);
            }

            foreach (var placed in semantic)
            {
                var option = ResolveOverlayTexture(placed.TextureId);
                var center = new Vector2(
                    -LotWidthMeters * 0.5f + placed.CellX * 10f + 5f,
                    -LotDepthMeters * 0.5f + placed.CellZ * 10f + 5f);
                var rotation = Quaternion.Euler(0f,
                    placed.RotationQuarterTurns * 90f, 0f);
                Vector2 Rotate(Vector2 local)
                {
                    var point = rotation * new Vector3(local.x, 0f, local.y);
                    return center + new Vector2(point.x, point.z);
                }
                void AddRoute(float offset, bool stairs)
                {
                    var startPosition = Rotate(new Vector2(offset, -5f));
                    var endPosition = Rotate(new Vector2(offset, 5f));
                    var prefix = $"overlay-{placed.InstanceId}";
                    var start = FindOrAdd(startPosition, 0f,
                        CirculationNodeKind.Waypoint, prefix + "-start");
                    var end = FindOrAdd(endPosition,
                        stairs ? option.StairRiseMeters : 0f,
                        stairs ? CirculationNodeKind.BuildingEntrance :
                            CirculationNodeKind.Waypoint,
                        stairs ? prefix + "-doorway" : prefix + "-end");
                    var segment = network.Connect(start.Id, end.Id,
                        CirculationDirection.TwoWay,
                        stairs ? PedestrianPathKind.Stairs :
                            PedestrianPathKind.Flat);
                    if (segment != null)
                    {
                        segment.WidthMeters = option.PedestrianWidthMeters;
                        segment.SpeedMetersPerSecond = stairs ? 0.85f : 1.4f;
                    }
                }

                switch (option.PedestrianLayout)
                {
                    case PedestrianOverlayLayout.Centerline:
                        AddRoute(0f, false);
                        break;
                    case PedestrianOverlayLayout.ParallelFlanks:
                        AddRoute(-3.25f, false);
                        AddRoute(3.25f, false);
                        break;
                    case PedestrianOverlayLayout.Stairs:
                        AddRoute(0f, true);
                        break;
                }
            }
            _session.Data.PedestrianNetwork = network;
            RebuildCirculationVisualization();
        }

        private void ApplyBaseTexturePresentation()
        {
            if (_groundRenderer == null) return;
            var option = ResolveBaseTexture(BaseTextureId);
            var texture = option == null ? null : Resources.Load<Texture2D>(
                option.ResolveResourcePath(Season));
            _groundRenderer.sharedMaterial.mainTexture = texture;
            var repeatMeters = option?.BaseRepeatMeters ?? 5f;
            _groundRenderer.sharedMaterial.mainTextureScale = new Vector2(
                Mathf.Max(1f, LotWidthMeters / repeatMeters),
                Mathf.Max(1f, LotDepthMeters / repeatMeters));
            ApplyTimeOfDay();
        }

        private bool BaseTextureHasExactSeasonResource()
        {
            var option = ResolveBaseTexture(BaseTextureId);
            return option != null && option.HasResourceForSeason(Season);
        }

        private void RebuildOverlayTexturePresentations()
        {
            if (_overlayTextureRoot == null) return;
            for (var index = _overlayTextureRoot.childCount - 1; index >= 0; index--)
                if (Application.isPlaying) Destroy(_overlayTextureRoot.GetChild(index).gameObject);
                else DestroyImmediate(_overlayTextureRoot.GetChild(index).gameObject);
            _overlayTextureRenderers.Clear();
            foreach (var placed in _session.Data.OverlayTextures ?? new List<PlacedOverlayTexture>())
            {
                var option = ResolveOverlayTexture(placed.TextureId);
                var size = OverlayFootprintCells(option,
                    placed.RotationQuarterTurns);
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Overlay — {placed.TextureId}";
                quad.transform.SetParent(_overlayTextureRoot, false);
                quad.transform.localPosition = new Vector3(
                    -LotWidthMeters * 0.5f + (placed.CellX + size.x * 0.5f) * 10f,
                    0.006f,
                    -LotDepthMeters * 0.5f + (placed.CellZ + size.y * 0.5f) * 10f);
                quad.transform.localRotation = Quaternion.Euler(90f, placed.RotationQuarterTurns * 90f, 0f);
                quad.transform.localScale = new Vector3(
                    size.x * 10f, size.y * 10f, 1f);
                quad.GetComponent<Collider>().enabled = false;
                var material = ShadowReceivingLotMaterial(LotTextureTint(TimeOfDay));
                // Experimental 3D lots promote their authored grass receiver
                // to queue 2000. Painted surface layers must remain above that
                // receiver (and below roads at 2430), otherwise the grass
                // overwrites brick/concrete on Art Museum LOD lots.
                material.renderQueue = 2001;
                material.mainTexture = Resources.Load<Texture2D>(option.ResourcePath);
                var renderer = quad.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                _overlayTextureRenderers.Add(renderer);
                if (option.PedestrianLayout == PedestrianOverlayLayout.Stairs)
                    BuildPedestrianStairPresentation(quad.transform, option);
            }
            if (SelectedOverlayTextureIndex >= OverlayTextureCount)
                SelectedOverlayTextureIndex = -1;
            ApplyOverlayTextureSelection();
        }

        private void BuildPedestrianStairPresentation(Transform overlay,
            LotTextureOption option)
        {
            const int stepCount = 12;
            var material = ShadowReceivingLotMaterial(LotTextureTint(TimeOfDay));
            material.mainTexture = Resources.Load<Texture2D>(option.ResourcePath);
            material.renderQueue = 2002;
            for (var step = 0; step < stepCount; step++)
            {
                var progress = (step + 0.5f) / stepCount;
                var tread = Cube($"Pedestrian Stair {step + 1}", overlay,
                    new Vector3(0f, -5f + progress * 10f,
                        -progress * option.StairRiseMeters - 0.03f),
                    new Vector3(option.PedestrianWidthMeters,
                        10f / stepCount + 0.03f,
                        option.StairRiseMeters / stepCount + 0.08f),
                    Color.white);
                tread.GetComponent<Renderer>().sharedMaterial = material;
                var collider = tread.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }
        }

        private void ApplyOverlayTextureSelection()
        {
            if (_overlayTextureSelection == null) return;
            var visible = _overlayEditorActive && SelectedOverlayTextureIndex >= 0 &&
                SelectedOverlayTextureIndex < OverlayTextureCount;
            _overlayTextureSelection.gameObject.SetActive(visible);
            if (!visible) return;
            var placed = _session.Data.OverlayTextures[SelectedOverlayTextureIndex];
            var size = OverlayFootprintCells(placed);
            _overlayTextureSelection.localPosition = new Vector3(
                -LotWidthMeters * 0.5f + (placed.CellX + size.x * 0.5f) * 10f,
                0.009f,
                -LotDepthMeters * 0.5f + (placed.CellZ + size.y * 0.5f) * 10f);
            _overlayTextureSelection.localScale = new Vector3(
                size.x * 10f, size.y * 10f, 1f);
        }

        private void UpdateLotTextureLighting()
        {
            var tint = SeasonLighting.GroundColor(
                Season, Color.white);
            foreach (var renderer in _overlayTextureRenderers)
                if (renderer != null) renderer.sharedMaterial.color = tint;
            foreach (var renderer in _connectorRenderers)
                if (renderer != null) renderer.sharedMaterial.color = tint;
        }

        private static Color LotTextureTint(TimeOfDayPreset preset) =>
            Color.white;

        public static Color TextureTintForTimeOfDay(TimeOfDayPreset preset) =>
            LotTextureTint(preset);
    }
}
