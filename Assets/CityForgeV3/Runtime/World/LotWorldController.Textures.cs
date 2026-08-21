using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        public sealed class LotTextureOption
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string ResourcePath;
            public readonly string SpringResourcePath;
            public readonly string SummerResourcePath;
            public readonly string AutumnResourcePath;
            public readonly string WinterResourcePath;

            public LotTextureOption(string id, string displayName, string resourcePath,
                string springResourcePath = null, string summerResourcePath = null,
                string autumnResourcePath = null, string winterResourcePath = null)
            {
                Id = id; DisplayName = displayName; ResourcePath = resourcePath;
                SpringResourcePath = springResourcePath;
                SummerResourcePath = summerResourcePath;
                AutumnResourcePath = autumnResourcePath;
                WinterResourcePath = winterResourcePath;
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
        public static readonly IReadOnlyList<LotTextureOption> OverlayTextures = new[]
        {
            new LotTextureOption("brick-walkway", "Brick Walkway",
                "CityForgeV3/LotTextures/LegacyOverlaysV01/brick-walkway"),
            new LotTextureOption("concrete-sidewalk", "Concrete Sidewalk",
                "CityForgeV3/LotTextures/UrbanOverlaysV01/concrete-sidewalk"),
            new LotTextureOption("fancy-sidewalk", "Fancy Sidewalk",
                "CityForgeV3/LotTextures/UrbanOverlaysV01/fancy-sidewalk-overlay"),
        };
        public static LotTextureOption BrickWalkwayOverlay => OverlayTextures[0];

        public static LotTextureOption ResolveOverlayTexture(string id)
        {
            foreach (var option in OverlayTextures)
                if (string.Equals(option.Id, id, StringComparison.OrdinalIgnoreCase)) return option;
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
            if (!_overlayEditorActive || cellX < 0 || cellX >= LotWidthCells ||
                cellZ < 0 || cellZ >= LotDepthCells) return false;
            var cell = new Vector2Int(cellX, cellZ);
            var existing = OverlayTextureIndexAtCell(cell);
            if (existing >= 0)
            {
                SelectedOverlayTextureIndex = existing;
                _overlayPaintTextureId = _session.Data.OverlayTextures[existing].TextureId;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(armedTextureId)) return false;
                AddOverlayTextureAtCell(armedTextureId, cell);
                _overlayPaintTextureId = armedTextureId;
            }
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
            if (!_overlayPaintStrokeActive || cellX < 0 || cellX >= LotWidthCells ||
                cellZ < 0 || cellZ >= LotDepthCells || cell == _lastOverlayPaintCell) return false;
            _lastOverlayPaintCell = cell;
            var existing = OverlayTextureIndexAtCell(cell);
            if (existing >= 0)
            {
                SelectedOverlayTextureIndex = existing;
                ApplyOverlayTextureSelection();
                return false;
            }
            return AddOverlayTextureAtCell(_overlayPaintTextureId, cell);
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
            NotifyStateChanged();
            return true;
        }

        public void ClearOverlayTextureSelection()
        {
            SelectedOverlayTextureIndex = -1;
            _overlayPaintStrokeActive = false;
            _lastOverlayPaintCell = new Vector2Int(-1, -1);
            ApplyOverlayTextureSelection();
        }

        private bool AddOverlayTextureAtCell(string textureId, Vector2Int cell)
        {
            _session.Data.OverlayTextures ??= new List<PlacedOverlayTexture>();
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
                CellX = cell.x, CellZ = cell.y
            });
            SelectedOverlayTextureIndex = _session.Data.OverlayTextures.Count - 1;
            RebuildOverlayTexturePresentations();
            return true;
        }

        private bool TryOverlayCellFromPanel(Vector2 panelPosition, Vector2 panelSize,
            out Vector2Int cell)
        {
            cell = default;
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point) ||
                point.x < -LotWidthMeters * 0.5f || point.x >= LotWidthMeters * 0.5f ||
                point.z < -LotDepthMeters * 0.5f || point.z >= LotDepthMeters * 0.5f) return false;
            cell = new Vector2Int(
                Mathf.FloorToInt((point.x + LotWidthMeters * 0.5f) / 10f),
                Mathf.FloorToInt((point.z + LotDepthMeters * 0.5f) / 10f));
            return true;
        }

        private int OverlayTextureIndexAtCell(Vector2Int cell)
        {
            for (var index = OverlayTextureCount - 1; index >= 0; index--)
            {
                var placed = _session.Data.OverlayTextures[index];
                if (placed.CellX == cell.x && placed.CellZ == cell.y) return index;
            }
            return -1;
        }

        private void ApplyBaseTexturePresentation()
        {
            if (_groundRenderer == null) return;
            var option = ResolveBaseTexture(BaseTextureId);
            var texture = option == null ? null : Resources.Load<Texture2D>(
                option.ResolveResourcePath(Season));
            _groundRenderer.sharedMaterial.mainTexture = texture;
            _groundRenderer.sharedMaterial.mainTextureScale = new Vector2(
                Mathf.Max(1f, LotWidthMeters / 5f), Mathf.Max(1f, LotDepthMeters / 5f));
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
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Overlay — {placed.TextureId}";
                quad.transform.SetParent(_overlayTextureRoot, false);
                quad.transform.localPosition = new Vector3(
                    -LotWidthMeters * 0.5f + placed.CellX * 10f + 5f, 0.006f,
                    -LotDepthMeters * 0.5f + placed.CellZ * 10f + 5f);
                quad.transform.localRotation = Quaternion.Euler(90f, placed.RotationQuarterTurns * 90f, 0f);
                quad.transform.localScale = new Vector3(10f, 10f, 1f);
                quad.GetComponent<Collider>().enabled = false;
                var material = ShadowReceivingLotMaterial(LotTextureTint(TimeOfDay));
                material.renderQueue = 1999;
                var option = ResolveOverlayTexture(placed.TextureId);
                material.mainTexture = Resources.Load<Texture2D>(option.ResourcePath);
                var renderer = quad.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                _overlayTextureRenderers.Add(renderer);
            }
            if (SelectedOverlayTextureIndex >= OverlayTextureCount)
                SelectedOverlayTextureIndex = -1;
            ApplyOverlayTextureSelection();
        }

        private void ApplyOverlayTextureSelection()
        {
            if (_overlayTextureSelection == null) return;
            var visible = _overlayEditorActive && SelectedOverlayTextureIndex >= 0 &&
                SelectedOverlayTextureIndex < OverlayTextureCount;
            _overlayTextureSelection.gameObject.SetActive(visible);
            if (!visible) return;
            var placed = _session.Data.OverlayTextures[SelectedOverlayTextureIndex];
            _overlayTextureSelection.localPosition = new Vector3(
                -LotWidthMeters * 0.5f + placed.CellX * 10f + 5f, 0.009f,
                -LotDepthMeters * 0.5f + placed.CellZ * 10f + 5f);
        }

        private void UpdateLotTextureLighting()
        {
            var tint = SeasonLighting.GroundColor(
                Season, LotTextureTint(TimeOfDay));
            foreach (var renderer in _overlayTextureRenderers)
                if (renderer != null) renderer.sharedMaterial.color = tint;
        }

        private static Color LotTextureTint(TimeOfDayPreset preset) => preset switch
        {
            TimeOfDayPreset.Morning => new Color(0.92f, 0.88f, 0.80f, 1f),
            TimeOfDayPreset.Noon => Color.white,
            TimeOfDayPreset.Afternoon => new Color(0.92f, 0.78f, 0.62f, 1f),
            TimeOfDayPreset.Evening => new Color(0.56f, 0.48f, 0.55f, 1f),
            _ => new Color(0.22f, 0.26f, 0.38f, 1f)
        };
    }
}
