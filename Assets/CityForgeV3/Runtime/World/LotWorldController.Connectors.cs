using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed class LotConnectorDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly string TextureResourcePath;
        public readonly float WidthMeters;
        public readonly float LengthMeters;
        public readonly bool AllowsPedestrians;
        public readonly bool AllowsVehicles;

        public LotConnectorDefinition(string id, string displayName,
            string description, string textureResourcePath, float widthMeters,
            float lengthMeters, bool allowsPedestrians, bool allowsVehicles)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            TextureResourcePath = textureResourcePath;
            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            AllowsPedestrians = allowsPedestrians;
            AllowsVehicles = allowsVehicles;
        }
    }

    public readonly struct LotConnectorAccess
    {
        public readonly Vector2 Inside;
        public readonly Vector2 Outside;
        public readonly bool AllowsPedestrians;
        public readonly bool AllowsVehicles;

        public LotConnectorAccess(Vector2 inside, Vector2 outside,
            bool allowsPedestrians, bool allowsVehicles)
        {
            Inside = inside;
            Outside = outside;
            AllowsPedestrians = allowsPedestrians;
            AllowsVehicles = allowsVehicles;
        }
    }

    public sealed partial class LotWorldController
    {
        public const string DirtEntryConnectorId = "dirt-entry-v01";
        public const float ConnectorOutsideExtensionMeters =
            LotMetricScale.MajorGridMeters * 0.25f;

        public static readonly IReadOnlyList<LotConnectorDefinition> Connectors =
            new[]
            {
                new LotConnectorDefinition(
                    DirtEntryConnectorId,
                    "Dirt Entry",
                    "A compact dirt entrance for workers and wagons",
                    "CityForgeV3/Materials/RoadsDirtV01/dirt-road-square",
                    5f,
                    ConnectorOutsideExtensionMeters * 2f,
                    true,
                    true)
            };

        private Transform _connectorRoot;
        private Transform _connectorSelection;
        private readonly List<Renderer> _connectorRenderers = new();
        private bool _connectorEditorActive;
        public int SelectedConnectorIndex { get; private set; } = -1;
        public int ConnectorCount => _session?.Data?.Connectors?.Count ?? 0;

        public static LotConnectorDefinition ResolveConnector(string id)
        {
            foreach (var connector in Connectors)
                if (string.Equals(connector.Id, id,
                        StringComparison.OrdinalIgnoreCase)) return connector;
            return Connectors[0];
        }

        private void BuildConnectorRoot()
        {
            _connectorRoot = new GameObject("Placed Lot Connectors").transform;
            _connectorRoot.SetParent(transform, false);
            var selection = GameObject.CreatePrimitive(PrimitiveType.Quad);
            selection.name = "Selected Lot Connector";
            selection.transform.SetParent(transform, false);
            selection.GetComponent<Collider>().enabled = false;
            selection.GetComponent<Renderer>().sharedMaterial = LotSurfaceMaterial(
                new Color(1f, 0.68f, 0.08f, 0.30f), 2002);
            _connectorSelection = selection.transform;
            _connectorSelection.gameObject.SetActive(false);
        }

        public void SetConnectorEditorContext(bool active)
        {
            _connectorEditorActive = active && !_cameraPanInteractionActive;
            ApplyConnectorSelection();
        }

        public bool PlaceOrSelectConnectorFromPanel(string connectorId,
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_connectorEditorActive || string.IsNullOrWhiteSpace(connectorId) ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            if (!PointNearConnectorEdge(point, LotWidthMeters, LotDepthMeters))
                return false;
            var edge = ClosestConnectorEdge(point, LotWidthMeters, LotDepthMeters);
            var definition = ResolveConnector(connectorId);
            var offset = ConnectorOffset(edge, point, LotWidthMeters,
                LotDepthMeters, definition.WidthMeters);
            _session.Data.Connectors ??= new List<PlacedLotConnector>();
            var existing = FindConnectorAt(edge, offset, definition.WidthMeters);
            if (existing >= 0)
            {
                SelectedConnectorIndex = existing;
                ApplyConnectorSelection();
                return true;
            }
            _session.Data.Connectors.Add(new PlacedLotConnector
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                ConnectorId = definition.Id,
                Edge = edge,
                OffsetMeters = offset,
                AllowsPedestrians = definition.AllowsPedestrians,
                AllowsVehicles = definition.AllowsVehicles
            });
            SelectedConnectorIndex = _session.Data.Connectors.Count - 1;
            RebuildConnectorPresentations();
            NotifyStateChanged();
            return true;
        }

        public bool DeleteSelectedConnector()
        {
            if (!_connectorEditorActive || SelectedConnectorIndex < 0 ||
                SelectedConnectorIndex >= ConnectorCount) return false;
            _session.Data.Connectors.RemoveAt(SelectedConnectorIndex);
            SelectedConnectorIndex = -1;
            RebuildConnectorPresentations();
            NotifyStateChanged();
            return true;
        }

        public void ClearConnectorSelection()
        {
            SelectedConnectorIndex = -1;
            ApplyConnectorSelection();
        }

        public static LotConnectorAccess ConnectorAccess(
            PlacedLotConnector connector, int lotWidthMeters,
            int lotDepthMeters)
        {
            if (connector == null) return default;
            var halfWidth = lotWidthMeters * 0.5f;
            var halfDepth = lotDepthMeters * 0.5f;
            var definition = ResolveConnector(connector.ConnectorId);
            var edgeLength = connector.Edge is LotConnectorEdge.North or
                LotConnectorEdge.South ? lotWidthMeters : lotDepthMeters;
            var halfConnector = definition.WidthMeters * 0.5f;
            var along = Mathf.Clamp(connector.OffsetMeters, halfConnector,
                Mathf.Max(halfConnector, edgeLength - halfConnector));
            Vector2 boundary;
            Vector2 inward;
            switch (connector.Edge)
            {
                case LotConnectorEdge.North:
                    boundary = new Vector2(-halfWidth + along, halfDepth);
                    inward = Vector2.down;
                    break;
                case LotConnectorEdge.East:
                    boundary = new Vector2(halfWidth, -halfDepth + along);
                    inward = Vector2.left;
                    break;
                case LotConnectorEdge.South:
                    boundary = new Vector2(-halfWidth + along, -halfDepth);
                    inward = Vector2.up;
                    break;
                default:
                    boundary = new Vector2(-halfWidth, -halfDepth + along);
                    inward = Vector2.right;
                    break;
            }
            return new LotConnectorAccess(
                boundary + inward * ConnectorOutsideExtensionMeters,
                boundary - inward * ConnectorOutsideExtensionMeters,
                connector.AllowsPedestrians, connector.AllowsVehicles);
        }

        private int FindConnectorAt(LotConnectorEdge edge, float offset,
            float widthMeters)
        {
            for (var index = ConnectorCount - 1; index >= 0; index--)
            {
                var candidate = _session.Data.Connectors[index];
                if (candidate != null && candidate.Edge == edge &&
                    Mathf.Abs(candidate.OffsetMeters - offset) < widthMeters * 0.5f)
                    return index;
            }
            return -1;
        }

        private static LotConnectorEdge ClosestConnectorEdge(Vector3 point,
            float widthMeters, float depthMeters)
        {
            var halfWidth = widthMeters * 0.5f;
            var halfDepth = depthMeters * 0.5f;
            var north = Mathf.Abs(point.z - halfDepth);
            var east = Mathf.Abs(point.x - halfWidth);
            var south = Mathf.Abs(point.z + halfDepth);
            var west = Mathf.Abs(point.x + halfWidth);
            var minimum = Mathf.Min(north, east, south, west);
            if (minimum == north) return LotConnectorEdge.North;
            if (minimum == east) return LotConnectorEdge.East;
            return minimum == south ? LotConnectorEdge.South : LotConnectorEdge.West;
        }

        public static bool PointNearConnectorEdge(Vector3 point,
            float widthMeters, float depthMeters)
        {
            var halfWidth = widthMeters * 0.5f;
            var halfDepth = depthMeters * 0.5f;
            var nearest = Mathf.Min(Mathf.Abs(point.z - halfDepth),
                Mathf.Abs(point.x - halfWidth),
                Mathf.Abs(point.z + halfDepth),
                Mathf.Abs(point.x + halfWidth));
            return nearest <= ConnectorOutsideExtensionMeters * 2f &&
                point.x >= -halfWidth - ConnectorOutsideExtensionMeters &&
                point.x <= halfWidth + ConnectorOutsideExtensionMeters &&
                point.z >= -halfDepth - ConnectorOutsideExtensionMeters &&
                point.z <= halfDepth + ConnectorOutsideExtensionMeters;
        }

        private static float ConnectorOffset(LotConnectorEdge edge,
            Vector3 point, float widthMeters, float depthMeters,
            float connectorWidthMeters)
        {
            var edgeLength = edge is LotConnectorEdge.North or
                LotConnectorEdge.South ? widthMeters : depthMeters;
            var coordinate = edge is LotConnectorEdge.North or
                LotConnectorEdge.South
                ? point.x + widthMeters * 0.5f
                : point.z + depthMeters * 0.5f;
            var halfConnector = connectorWidthMeters * 0.5f;
            return Mathf.Clamp(coordinate, halfConnector,
                Mathf.Max(halfConnector, edgeLength - halfConnector));
        }

        private void RebuildConnectorPresentations()
        {
            if (_connectorRoot == null) return;
            for (var index = _connectorRoot.childCount - 1; index >= 0; index--)
            {
                var child = _connectorRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            _connectorRenderers.Clear();
            foreach (var placed in _session.Data.Connectors ??
                     new List<PlacedLotConnector>())
            {
                if (placed == null) continue;
                var definition = ResolveConnector(placed.ConnectorId);
                var access = ConnectorAccess(placed, LotWidthMeters,
                    LotDepthMeters);
                var center = (access.Inside + access.Outside) * 0.5f;
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Connector — {definition.DisplayName}";
                quad.transform.SetParent(_connectorRoot, false);
                quad.transform.localPosition = new Vector3(center.x, 0.012f,
                    center.y);
                quad.transform.localRotation = Quaternion.Euler(90f,
                    ConnectorYaw(placed.Edge), 0f);
                quad.transform.localScale = new Vector3(definition.WidthMeters,
                    definition.LengthMeters, 1f);
                quad.GetComponent<Collider>().enabled = false;
                var material = ShadowReceivingLotMaterial(
                    LotTextureTint(TimeOfDay));
                material.name = $"{definition.DisplayName} Connector";
                material.renderQueue = 2002;
                material.mainTexture = Resources.Load<Texture2D>(
                    definition.TextureResourcePath);
                var renderer = quad.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                _connectorRenderers.Add(renderer);
            }
            if (SelectedConnectorIndex >= ConnectorCount)
                SelectedConnectorIndex = -1;
            ApplyConnectorSelection();
        }

        private void ApplyConnectorSelection()
        {
            if (_connectorSelection == null) return;
            var visible = _connectorEditorActive && SelectedConnectorIndex >= 0 &&
                SelectedConnectorIndex < ConnectorCount;
            _connectorSelection.gameObject.SetActive(visible);
            if (!visible) return;
            var placed = _session.Data.Connectors[SelectedConnectorIndex];
            var definition = ResolveConnector(placed.ConnectorId);
            var access = ConnectorAccess(placed, LotWidthMeters, LotDepthMeters);
            var center = (access.Inside + access.Outside) * 0.5f;
            _connectorSelection.localPosition = new Vector3(center.x, 0.009f,
                center.y);
            _connectorSelection.localRotation = Quaternion.Euler(90f,
                ConnectorYaw(placed.Edge), 0f);
            _connectorSelection.localScale = new Vector3(
                definition.WidthMeters + 0.5f,
                definition.LengthMeters + 0.5f, 1f);
        }

        private static float ConnectorYaw(LotConnectorEdge edge) => edge switch
        {
            LotConnectorEdge.East => 90f,
            LotConnectorEdge.South => 180f,
            LotConnectorEdge.West => 270f,
            _ => 0f
        };
    }
}
