using System;
using System.Collections.Generic;
using CityForgeV3.Buildings3D;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private Transform _buildingPropRoot;
        private Transform _buildingPropPreview;
        private int _buildingPropOverlayLayer = -1;
        private readonly List<GameObject> _buildingPropPresentations = new();
        private readonly List<Vector2Int> _buildingPropPresentationKeys = new();
        private int _hoverBuildingPropPresentationIndex = -1;
        private int _selectedBuildingPropPresentationIndex = -1;
        private int _selectedBuildingPropBuildingIndex = -1;
        private int _selectedBuildingPropAttachmentIndex = -1;
        private bool _buildingPropDragActive;
        private Vector2 _buildingPropDragOffsetPixels;
        private string _buildingPropPreviewId = "";
        private int _buildingPropPreviewHostIndex = -1;
        private float _buildingPropPreviewX = 0.5f;
        private float _buildingPropPreviewY = 0.5f;
        private bool _buildingPropPlacementActive;

        public bool BuildingPropDragActive => _buildingPropDragActive;

        public float SelectedBuildingPropScale
        {
            get
            {
                var attachment = SelectedBuildingPropAttachment();
                return attachment == null ? 1f : Mathf.Max(0.1f, attachment.Scale);
            }
        }

        public string SelectedBuildingPropDisplayName
        {
            get
            {
                var attachment = SelectedBuildingPropAttachment();
                return attachment == null
                    ? "Building Prop"
                    : BuildingPropCatalog.Find(attachment.ComponentId)?.DisplayName ??
                      "Building Prop";
            }
        }

        private PlacedBuildingProp SelectedBuildingPropAttachment()
        {
            var attachments = AttachmentsForHost(
                _selectedBuildingPropBuildingIndex);
            return _selectedBuildingPropAttachmentIndex < 0 ||
                   attachments == null ||
                   _selectedBuildingPropAttachmentIndex >= attachments.Count
                ? null
                : attachments[_selectedBuildingPropAttachmentIndex];
        }

        private static int Building3DHostKey(int index) => -index - 2;
        private static bool IsBuilding3DHost(int hostKey) => hostKey <= -2;
        private static int Building3DIndex(int hostKey) => -hostKey - 2;

        private List<PlacedBuildingProp> AttachmentsForHost(int hostKey)
        {
            if (IsBuilding3DHost(hostKey))
            {
                var index = Building3DIndex(hostKey);
                if (index < 0 || index >= (_session.Data.Buildings3D?.Count ?? 0))
                    return null;
                var host = _session.Data.Buildings3D[index];
                host.Attachments ??= new List<PlacedBuildingProp>();
                return host.Attachments;
            }
            if (hostKey < 0 || hostKey >= (_session.Data.Buildings?.Count ?? 0))
                return null;
            var building = _session.Data.Buildings[hostKey];
            building.Attachments ??= new List<PlacedBuildingProp>();
            return building.Attachments;
        }

        private void BuildBuildingPropRoot()
        {
            _buildingPropOverlayLayer = LayerMask.NameToLayer("BuildingPropOverlay");
            if (_buildingPropOverlayLayer < 0)
                throw new InvalidOperationException(
                    "The BuildingPropOverlay Unity layer is required.");
            BuildBuildingPropOverlayPass();
            _buildingPropRoot = new GameObject("Building Attachments").transform;
            _buildingPropRoot.SetParent(transform, false);
            _buildingPropRoot.gameObject.layer = _buildingPropOverlayLayer;
            var preview = new GameObject("Building Prop Placement Preview");
            preview.transform.SetParent(transform, false);
            preview.layer = _buildingPropOverlayLayer;
            _buildingPropPreview = preview.transform;
            preview.SetActive(false);
        }

        public void SetBuildingPropEditorContext(bool active)
        {
            _buildingPropPlacementActive =
                active && !_cameraPanInteractionActive;
            if (!_buildingPropPlacementActive && _buildingPropPreview != null)
                _buildingPropPreview.gameObject.SetActive(false);
        }

        public void SetBuildingPropPlacementPreview(string componentId)
        {
            _buildingPropPreviewId = componentId ?? "";
            _buildingPropPreviewHostIndex = -1;
            if (_buildingPropPreview == null) return;
            for (var index = _buildingPropPreview.childCount - 1; index >= 0; index--)
            {
                var child = _buildingPropPreview.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            var definition = BuildingPropCatalog.Find(componentId);
            var prefab = definition == null ? null :
                Resources.Load<GameObject>(definition.ModelResourcePath);
            if (prefab != null)
            {
                var model = Instantiate(prefab, _buildingPropPreview);
                model.name = $"{componentId} Placement Model";
                SetBuildingPropOverlayLayer(model);
                ApplyBuildingPropMaterials(model, definition);
                ApplyBuildingPropPreviewMaterials(model);
            }
            _buildingPropPreview.gameObject.SetActive(false);
        }

        public bool UpdateBuildingPropPreviewFromPanel(
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_buildingPropPlacementActive || _buildingPropPreview == null ||
                string.IsNullOrWhiteSpace(_buildingPropPreviewId) ||
                _buildingPropPreview.childCount == 0 ||
                !TryBuildingAttachmentPoint(panelPosition, panelSize,
                    out var buildingIndex, out var normalizedX, out var normalizedY))
            {
                if (_buildingPropPreview != null)
                    _buildingPropPreview.gameObject.SetActive(false);
                _buildingPropPreviewHostIndex = -1;
                return false;
            }
            _buildingPropPreviewHostIndex = buildingIndex;
            RebuildBuildingPropOverlayPass();
            _buildingPropPreviewX = normalizedX;
            _buildingPropPreviewY = normalizedY;
            PositionBuildingPropModel(_buildingPropPreview.GetChild(0), buildingIndex,
                normalizedX, normalizedY, _buildingPropPreviewId, 1f);
            ConfigureBuildingPropPreviewRenderers(
                _buildingPropPreview.GetChild(0).gameObject);
            _buildingPropPreview.gameObject.SetActive(true);
            return true;
        }

        public bool PlaceBuildingPropFromPanel(
            string componentId, Vector2 panelPosition, Vector2 panelSize)
        {
            if (string.IsNullOrWhiteSpace(componentId) ||
                !TryBuildingAttachmentPoint(panelPosition, panelSize,
                    out var buildingIndex, out var normalizedX, out var normalizedY))
                return false;
            return PlaceBuildingPropOnHost(componentId, buildingIndex,
                normalizedX, normalizedY);
        }

        // Commit the facade hit already established by the visible preview.
        // Repeating the broad building hit-test at pointer-up made a valid
        // preview disappear when pointer capture shifted the release event.
        public bool CommitBuildingPropPlacementPreview(string componentId)
        {
            if (!_buildingPropPlacementActive || _buildingPropPreview == null ||
                !_buildingPropPreview.gameObject.activeSelf ||
                _buildingPropPreviewHostIndex == -1 ||
                string.IsNullOrWhiteSpace(componentId) ||
                !string.Equals(componentId, _buildingPropPreviewId,
                    StringComparison.OrdinalIgnoreCase))
                return false;
            return PlaceBuildingPropOnHost(componentId,
                _buildingPropPreviewHostIndex, _buildingPropPreviewX,
                _buildingPropPreviewY);
        }

        private bool PlaceBuildingPropOnHost(string componentId,
            int buildingIndex, float normalizedX, float normalizedY)
        {
            var definition = BuildingPropCatalog.Find(componentId);
            if (definition == null) return false;
            var attachments = AttachmentsForHost(buildingIndex);
            if (attachments == null) return false;
            var attachment = new PlacedBuildingProp
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                ComponentId = definition.Id,
                Revision = definition.Revision,
                HostElevation = definition.HostElevation,
                NormalizedX = normalizedX,
                NormalizedY = normalizedY,
                ProjectionDepthMeters = definition.ProjectionDepthMeters,
                Scale = 1f
            };
            InitializeHostLocalPosition(attachment, buildingIndex,
                normalizedX, normalizedY);
            attachments.Add(attachment);
            if (IsBuilding3DHost(buildingIndex))
            {
                _selectedBuilding3DIndex = Building3DIndex(buildingIndex);
                _session.Select(false);
            }
            else _session.SelectBuilding(buildingIndex);
            ActiveObjectSelection = LotObjectSelectionKind.BuildingProp;
            ApplySessionState();
            NotifyStateChanged();
            return true;
        }

#if UNITY_EDITOR
        public bool ShowBuildingPropPreviewForQa(string componentId,
            int buildingIndex, float normalizedX, float normalizedY)
        {
            if (_camera == null || buildingIndex < 0 ||
                buildingIndex >= (_session.Data.Buildings?.Count ?? 0))
                return false;
            SetBuildingPropPlacementPreview(componentId);
            if (_buildingPropPreview == null || _buildingPropPreview.childCount == 0 ||
                !TryBuildingArtworkScreenBounds(buildingIndex, out _,
                    out var minimum, out var maximum))
                return false;
            _buildingPropPlacementActive = true;
            var cameraPixel = new Vector2(
                Mathf.Lerp(minimum.x, maximum.x, Mathf.Clamp01(normalizedX)),
                Mathf.Lerp(minimum.y, maximum.y, Mathf.Clamp01(normalizedY)));
            var shown = UpdateBuildingPropPreviewFromPanel(
                new Vector2(cameraPixel.x, _camera.pixelHeight - cameraPixel.y),
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            if (!shown) return false;
            var model = _buildingPropPreview.GetChild(0);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                Debug.Log($"building-prop-preview renderer={renderer.name} " +
                    $"enabled={renderer.enabled} bounds={renderer.bounds} " +
                    $"shader={renderer.sharedMaterial?.shader?.name} " +
                    $"queue={renderer.sharedMaterial?.renderQueue}");
            return shown;
        }

        public void SetBuildingPropQaCameraZoom(float orthographicSize)
        {
            if (_camera != null && _camera.orthographic)
            {
                _camera.orthographicSize = Mathf.Max(1f, orthographicSize);
                RefreshBuildingPropPresentations();
                foreach (var presentation in _buildingPropPresentations)
                    foreach (var renderer in presentation.GetComponentsInChildren<Renderer>(true))
                        Debug.Log($"building-prop-committed renderer={renderer.name} " +
                            $"enabled={renderer.enabled} bounds={renderer.bounds} " +
                            $"shader={renderer.sharedMaterial?.shader?.name} " +
                            $"queue={renderer.sharedMaterial?.renderQueue}");
            }
        }

        public bool CommitBuildingPropForQa(string componentId,
            int buildingIndex, float normalizedX, float normalizedY)
        {
            var definition = BuildingPropCatalog.Find(componentId);
            if (definition == null || buildingIndex < 0 ||
                buildingIndex >= (_session.Data.Buildings?.Count ?? 0))
                return false;
            var building = _session.Data.Buildings[buildingIndex];
            building.Attachments ??= new List<PlacedBuildingProp>();
            building.Attachments.Add(new PlacedBuildingProp
            {
                InstanceId = "qa-building-prop-commit",
                ComponentId = definition.Id,
                Revision = definition.Revision,
                HostElevation = definition.HostElevation,
                NormalizedX = Mathf.Clamp01(normalizedX),
                NormalizedY = Mathf.Clamp01(normalizedY),
                ProjectionDepthMeters = definition.ProjectionDepthMeters,
                Scale = 1f
            });
            if (_buildingPropPreview != null)
                _buildingPropPreview.gameObject.SetActive(false);
            ApplySessionState();
            return true;
        }

        public bool CommitBuildingProp3DForQa(string componentId,
            int building3DIndex, string hostElevation, float facadeOffset,
            float heightMeters)
        {
            var definition = BuildingPropCatalog.Find(componentId);
            var hostKey = Building3DHostKey(building3DIndex);
            if (definition == null ||
                building3DIndex < 0 ||
                building3DIndex >= (_session.Data.Buildings3D?.Count ?? 0) ||
                !TryHostMetrics(hostKey, out _, out _, out var width,
                    out var depth, out var height))
                return false;

            var attachments = AttachmentsForHost(hostKey);
            if (attachments == null) return false;
            attachments.RemoveAll(existing => existing != null &&
                string.Equals(existing.ComponentId, componentId,
                    StringComparison.OrdinalIgnoreCase));

            var elevation = string.IsNullOrWhiteSpace(hostElevation)
                ? "Front" : hostElevation;
            var projection = definition.ProjectionDepthMeters;
            var tangent = Mathf.Clamp(facadeOffset, -1f, 1f);
            var local = elevation switch
            {
                "Right" => new Vector3(width * 0.5f + projection,
                    Mathf.Clamp(heightMeters, 0f, height), tangent * depth * 0.5f),
                "Left" => new Vector3(-width * 0.5f - projection,
                    Mathf.Clamp(heightMeters, 0f, height), tangent * depth * 0.5f),
                "Back" => new Vector3(tangent * width * 0.5f,
                    Mathf.Clamp(heightMeters, 0f, height), -depth * 0.5f - projection),
                _ => new Vector3(tangent * width * 0.5f,
                    Mathf.Clamp(heightMeters, 0f, height), depth * 0.5f + projection)
            };
            attachments.Add(new PlacedBuildingProp
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                ComponentId = definition.Id,
                Revision = definition.Revision,
                HostElevation = elevation,
                NormalizedX = 0.5f,
                NormalizedY = 0.2f,
                ProjectionDepthMeters = projection,
                Scale = 1f,
                HasHostLocalPosition = true,
                HostLocalX = local.x,
                HostLocalY = local.y,
                HostLocalZ = local.z
            });
            _selectedBuilding3DIndex = building3DIndex;
            _session.Select(false);
            ActiveObjectSelection = LotObjectSelectionKind.BuildingProp;
            ApplySessionState();
            NotifyStateChanged();
            return true;
        }

        public bool RunBuildingPropSelectionMoveQa(float panelDeltaX)
        {
            SetBuildingPropEditorContext(false);
            SetBuildingPropQaCameraZoom(6f);
            if (_buildingPropPresentations.Count == 0 || _camera == null)
                return false;
            var presentation = _buildingPropPresentations[0];
            var cameraPixel3 = _camera.WorldToScreenPoint(
                presentation.transform.position);
            var panelPoint = new Vector2(cameraPixel3.x,
                _camera.pixelHeight - cameraPixel3.y);
            var panelSize = new Vector2(_camera.pixelWidth, _camera.pixelHeight);
            if (UpdateObjectHoverFromPanel(panelPoint, panelSize) !=
                    LotObjectSelectionKind.BuildingProp ||
                BeginExistingObjectManipulationFromPanel(panelPoint, panelSize) !=
                    LotObjectSelectionKind.BuildingProp)
                return false;
            // Recompose the Buildings workspace exactly as the runtime UI
            // does after pointer-up. The selected prop must survive that
            // presentation refresh and remain the active highlighted object.
            SetBuildingEditorContext(true, false);
            if (ActiveObjectSelection != LotObjectSelectionKind.BuildingProp ||
                _selectedBuildingPropPresentationIndex < 0)
                return false;
            var moved = DragBuildingPropFromPanel(
                panelPoint + new Vector2(panelDeltaX, 0f), panelSize);
            EndBuildingPropDrag();
            ApplyBuildingPropHover(_selectedBuildingPropPresentationIndex);
            return moved;
        }
#endif

        private bool TryBuildingAttachmentPoint(Vector2 panelPosition,
            Vector2 panelSize, out int buildingIndex, out float normalizedX,
            out float normalizedY)
        {
            buildingIndex = -1;
            normalizedX = normalizedY = 0.5f;
            if (_camera == null)
                return false;
            var pixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            buildingIndex = FindBuildingArtworkHitIndex(pixel);
            if (buildingIndex < 0)
            {
                var building3DIndex = FindBuilding3DHitIndex(pixel);
                if (building3DIndex >= 0)
                    buildingIndex = Building3DHostKey(building3DIndex);
            }
            if (!TryHostScreenBounds(buildingIndex, out _,
                    out var minimum, out var maximum))
                return false;
            var left = minimum.x;
            var right = maximum.x;
            var bottom = minimum.y;
            var top = maximum.y;
            normalizedX = Mathf.Clamp01(Mathf.InverseLerp(left, right, pixel.x));
            normalizedY = Mathf.Clamp01(Mathf.InverseLerp(bottom, top, pixel.y));
            return pixel.x >= left && pixel.x <= right &&
                   pixel.y >= bottom && pixel.y <= top;
        }

        private void RefreshBuildingPropPresentations()
        {
            if (_buildingPropRoot == null || _camera == null) return;
            var restoreSelectedProp =
                ActiveObjectSelection == LotObjectSelectionKind.BuildingProp &&
                _selectedBuildingPropBuildingIndex != -1 &&
                _selectedBuildingPropAttachmentIndex >= 0;
            foreach (var presentation in _buildingPropPresentations)
                if (presentation != null) Destroy(presentation);
            _buildingPropPresentations.Clear();
            _buildingPropPresentationKeys.Clear();
            _hoverBuildingPropPresentationIndex = -1;
            _selectedBuildingPropPresentationIndex = -1;
            void AddHostPresentations(int hostKey)
            {
                var attachments = AttachmentsForHost(hostKey);
                if (attachments == null) return;
                for (var attachmentIndex = 0;
                     attachmentIndex < attachments.Count;
                     attachmentIndex++)
                {
                    var attachment = attachments[attachmentIndex];
                    var definition = BuildingPropCatalog.Find(attachment.ComponentId);
                    var prefab = definition == null ? null :
                        Resources.Load<GameObject>(definition.ModelResourcePath);
                    if (prefab == null)
                    {
                        Debug.LogError($"Missing building-prop model: " +
                            $"{definition?.ModelResourcePath}");
                        continue;
                    }
                    var root = Instantiate(prefab, _buildingPropRoot);
                    root.name = $"Attachment {attachment.ComponentId}";
                    SetBuildingPropOverlayLayer(root);
                    ApplyBuildingPropMaterials(root, definition);
                    PositionBuildingPropModel(root.transform, hostKey,
                        attachment);
                    var doorMotion = root.AddComponent<BuildingPropDoorMotion>();
                    doorMotion.Configure(definition.DoorTransformName,
                        definition.DoorOpenAngleDegrees, attachment.DoorOpen);
                    ConfigureBuildingPropNightLighting(root, definition);
                    var motion = root.AddComponent<BuildingPropSwingMotion>();
                    var stablePhase = Mathf.Abs(
                        attachment.InstanceId?.GetHashCode() ?? 0) % 1000 / 1000f;
                    motion.Configure(definition.SwingTransformName,
                        definition.SwingAmplitudeDegrees,
                        definition.SwingPeriodSeconds, stablePhase);
                    _buildingPropPresentations.Add(root);
                    _buildingPropPresentationKeys.Add(
                        new Vector2Int(hostKey, attachmentIndex));
                    if (restoreSelectedProp &&
                        hostKey == _selectedBuildingPropBuildingIndex &&
                        attachmentIndex == _selectedBuildingPropAttachmentIndex)
                        _selectedBuildingPropPresentationIndex =
                            _buildingPropPresentations.Count - 1;
                }
            }
            for (var index = 0;
                 index < (_session.Data.Buildings?.Count ?? 0); index++)
                AddHostPresentations(index);
            for (var index = 0;
                 index < (_session.Data.Buildings3D?.Count ?? 0); index++)
                AddHostPresentations(Building3DHostKey(index));
            if (_selectedBuildingPropPresentationIndex >= 0)
                ApplyBuildingPropHover(_selectedBuildingPropPresentationIndex);
        }

        private void RepositionBuildingPropPresentationsForBuilding(
            int buildingIndex)
        {
            if (buildingIndex < 0 ||
                buildingIndex >= (_session.Data.Buildings?.Count ?? 0)) return;
            var attachments = _session.Data.Buildings[buildingIndex].Attachments;
            if (attachments == null) return;
            var count = Mathf.Min(
                _buildingPropPresentations.Count,
                _buildingPropPresentationKeys.Count);
            for (var presentationIndex = 0;
                 presentationIndex < count;
                 presentationIndex++)
            {
                var key = _buildingPropPresentationKeys[presentationIndex];
                if (key.x != buildingIndex || key.y < 0 ||
                    key.y >= attachments.Count ||
                    _buildingPropPresentations[presentationIndex] == null)
                    continue;
                PositionBuildingPropModel(
                    _buildingPropPresentations[presentationIndex].transform,
                    buildingIndex,
                    attachments[key.y]);
            }
        }

        private void TranslateBuildingPropPresentationsForBuilding(
            int buildingIndex, Vector3 worldDelta)
        {
            if (buildingIndex < 0 || worldDelta.sqrMagnitude <= 0.0000001f)
                return;
            var count = Mathf.Min(
                _buildingPropPresentations.Count,
                _buildingPropPresentationKeys.Count);
            for (var presentationIndex = 0;
                 presentationIndex < count;
                 presentationIndex++)
            {
                if (_buildingPropPresentationKeys[presentationIndex].x != buildingIndex)
                    continue;
                var presentation = _buildingPropPresentations[presentationIndex];
                if (presentation != null)
                    presentation.transform.position += worldDelta;
            }
        }

        private int BuildingPropPresentationIndexAtCameraPixel(Vector2 pixel)
        {
            if (_camera == null) return -1;
            var bestIndex = -1;
            var bestDepth = float.PositiveInfinity;
            for (var index = 0; index < _buildingPropPresentations.Count; index++)
            {
                var root = _buildingPropPresentations[index];
                if (root == null || !root.activeInHierarchy) continue;
                var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                var depth = float.PositiveInfinity;
                var found = false;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        continue;
                    var bounds = renderer.bounds;
                    for (var x = -1; x <= 1; x += 2)
                    for (var y = -1; y <= 1; y += 2)
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var screen = _camera.WorldToScreenPoint(bounds.center +
                            Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                        if (screen.z <= 0f) continue;
                        minimum = Vector2.Min(minimum, screen);
                        maximum = Vector2.Max(maximum, screen);
                        depth = Mathf.Min(depth, screen.z);
                        found = true;
                    }
                }
                const float hitPaddingPixels = 7f;
                if (!found || pixel.x < minimum.x - hitPaddingPixels ||
                    pixel.x > maximum.x + hitPaddingPixels ||
                    pixel.y < minimum.y - hitPaddingPixels ||
                    pixel.y > maximum.y + hitPaddingPixels || depth >= bestDepth)
                    continue;
                bestDepth = depth;
                bestIndex = index;
            }
            return bestIndex;
        }

        private void ApplyBuildingPropHover(int presentationIndex)
        {
            if (_hoverBuildingPropPresentationIndex == presentationIndex) return;
            ClearBuildingPropHover();
            if (presentationIndex < 0 ||
                presentationIndex >= _buildingPropPresentations.Count) return;
            _hoverBuildingPropPresentationIndex = presentationIndex;
            HoverObjectKind = LotObjectSelectionKind.BuildingProp;
            _hoverObjectIndex = presentationIndex;
            if (_objectHoverHighlight != null)
                _objectHoverHighlight.gameObject.SetActive(false);
            SetBuildingPropHighlight(presentationIndex, true);
        }

        private void ClearBuildingPropHover()
        {
            if (_hoverBuildingPropPresentationIndex >= 0)
                SetBuildingPropHighlight(_hoverBuildingPropPresentationIndex, false);
            _hoverBuildingPropPresentationIndex = -1;
        }

        private void SetBuildingPropHighlight(int presentationIndex, bool highlighted)
        {
            if (presentationIndex < 0 ||
                presentationIndex >= _buildingPropPresentations.Count) return;
            var root = _buildingPropPresentations[presentationIndex];
            if (root == null) return;
            var tint = highlighted
                ? new Color(0.65f, 2f, 0.82f, 1f)
                : Color.white;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.materials)
                if (material != null && material.HasProperty("_Color"))
                    material.color = tint;
        }

        private bool BeginBuildingPropDragFromPanel(int presentationIndex,
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (presentationIndex < 0 ||
                presentationIndex >= _buildingPropPresentationKeys.Count) return false;
            var key = _buildingPropPresentationKeys[presentationIndex];
            var attachments = AttachmentsForHost(key.x);
            if (attachments == null || key.y < 0 || key.y >= attachments.Count)
                return false;
            _selectedBuildingPropPresentationIndex = presentationIndex;
            _selectedBuildingPropBuildingIndex = key.x;
            _selectedBuildingPropAttachmentIndex = key.y;
            _buildingPropDragActive = true;
            var pointerPixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            var rootPixel = _camera.WorldToScreenPoint(
                _buildingPropPresentations[presentationIndex].transform.position);
            _buildingPropDragOffsetPixels =
                new Vector2(rootPixel.x, rootPixel.y) - pointerPixel;
            DeselectBuilding3D();
            ActiveObjectSelection = LotObjectSelectionKind.BuildingProp;
            SelectedFloraIndex = -1;
            SelectedPropIndex = -1;
            _session.Select(false);
            ApplyFloraSelection();
            ApplyPropSelection();
            ApplyBuildingPropHover(presentationIndex);
            return true;
        }

        public bool DragBuildingPropFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_buildingPropDragActive || _camera == null ||
                _selectedBuildingPropPresentationIndex < 0 ||
                _selectedBuildingPropPresentationIndex >= _buildingPropPresentations.Count ||
                _selectedBuildingPropBuildingIndex == -1)
                return false;
            var attachments = AttachmentsForHost(_selectedBuildingPropBuildingIndex);
            if (_selectedBuildingPropAttachmentIndex < 0 ||
                attachments == null ||
                _selectedBuildingPropAttachmentIndex >= attachments.Count ||
                !TryHostScreenBounds(_selectedBuildingPropBuildingIndex,
                    out _, out var minimum, out var maximum)) return false;
            var pixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight)) +
                _buildingPropDragOffsetPixels;
            var attachment = attachments[_selectedBuildingPropAttachmentIndex];
            attachment.NormalizedX = Mathf.Clamp01(
                Mathf.InverseLerp(minimum.x, maximum.x, pixel.x));
            attachment.NormalizedY = Mathf.Clamp01(
                Mathf.InverseLerp(minimum.y, maximum.y, pixel.y));
            SetHostLocalPositionFromPixel(attachment,
                _selectedBuildingPropBuildingIndex, pixel);
            PositionBuildingPropModel(
                _buildingPropPresentations[_selectedBuildingPropPresentationIndex].transform,
                _selectedBuildingPropBuildingIndex, attachment);
            return true;
        }

        public void EndBuildingPropDrag()
        {
            if (!_buildingPropDragActive) return;
            _buildingPropDragActive = false;
            NotifyStateChanged();
        }

        public bool DeleteSelectedBuildingProp()
        {
            var attachments = AttachmentsForHost(_selectedBuildingPropBuildingIndex);
            if (_selectedBuildingPropAttachmentIndex < 0 ||
                attachments == null ||
                _selectedBuildingPropAttachmentIndex >=
                    attachments.Count) return false;
            attachments.RemoveAt(_selectedBuildingPropAttachmentIndex);
            _buildingPropDragActive = false;
            _selectedBuildingPropPresentationIndex = -1;
            _selectedBuildingPropBuildingIndex = -1;
            _selectedBuildingPropAttachmentIndex = -1;
            ActiveObjectSelection = LotObjectSelectionKind.None;
            ClearBuildingPropHover();
            RefreshBuildingPropPresentations();
            RebuildBuildingPropOverlayPass();
            NotifyStateChanged();
            return true;
        }

        public bool RotateSelectedBuildingProp45Degrees(int direction = 1)
        {
            if (_selectedBuildingPropPresentationIndex < 0 ||
                _selectedBuildingPropPresentationIndex >= _buildingPropPresentations.Count ||
                _selectedBuildingPropBuildingIndex == -1)
                return false;
            var attachments = AttachmentsForHost(_selectedBuildingPropBuildingIndex);
            if (_selectedBuildingPropAttachmentIndex < 0 ||
                attachments == null ||
                _selectedBuildingPropAttachmentIndex >= attachments.Count)
                return false;
            var attachment = attachments[_selectedBuildingPropAttachmentIndex];
            var nextPreset = (Mathf.RoundToInt(
                attachment.RotationDegrees / 45f) +
                (direction >= 0 ? 1 : -1) + 8) % 8;
            attachment.RotationDegrees = nextPreset * 45f;
            PositionBuildingPropModel(
                _buildingPropPresentations[_selectedBuildingPropPresentationIndex].transform,
                _selectedBuildingPropBuildingIndex, attachment);
            RebuildBuildingPropOverlayPass();
            ApplyBuildingPropHover(_selectedBuildingPropPresentationIndex);
            NotifyStateChanged();
            return true;
        }

        public bool SetSelectedBuildingPropScale(float scale)
        {
            if (_selectedBuildingPropPresentationIndex < 0 ||
                _selectedBuildingPropPresentationIndex >=
                _buildingPropPresentations.Count) return false;
            var attachment = SelectedBuildingPropAttachment();
            if (attachment == null) return false;
            attachment.Scale = Mathf.Clamp(scale, 0.25f, 3f);
            PositionBuildingPropModel(
                _buildingPropPresentations[
                    _selectedBuildingPropPresentationIndex].transform,
                _selectedBuildingPropBuildingIndex, attachment);
            RebuildBuildingPropOverlayPass();
            ApplyBuildingPropHover(_selectedBuildingPropPresentationIndex);
            NotifyStateChanged();
            return true;
        }

        public bool ToggleSelectedBuildingPropDoor()
        {
            if (_selectedBuildingPropPresentationIndex < 0 ||
                _selectedBuildingPropPresentationIndex >=
                _buildingPropPresentations.Count ||
                _selectedBuildingPropBuildingIndex == -1) return false;
            var attachments = AttachmentsForHost(
                _selectedBuildingPropBuildingIndex);
            if (_selectedBuildingPropAttachmentIndex < 0 ||
                attachments == null ||
                _selectedBuildingPropAttachmentIndex >=
                    attachments.Count) return false;
            var attachment = attachments[_selectedBuildingPropAttachmentIndex];
            var definition = BuildingPropCatalog.Find(attachment.ComponentId);
            if (definition == null ||
                string.IsNullOrWhiteSpace(definition.DoorTransformName))
                return false;
            attachment.DoorOpen = !attachment.DoorOpen;
            var presentation = _buildingPropPresentations[
                _selectedBuildingPropPresentationIndex];
            var motion = presentation.GetComponent<BuildingPropDoorMotion>();
            if (motion == null)
            {
                motion = presentation.AddComponent<BuildingPropDoorMotion>();
                motion.Configure(definition.DoorTransformName,
                    definition.DoorOpenAngleDegrees, attachment.DoorOpen);
            }
            else motion.SetOpen(attachment.DoorOpen);
            NotifyStateChanged();
            return true;
        }

        private void PositionBuildingPropModel(Transform model,
            int buildingIndex, float normalizedX, float normalizedY,
            string componentId, float scale, float rotationDegrees = 0f)
        {
            var definition = BuildingPropCatalog.Find(componentId);
            if (model == null || definition == null ||
                !TryHostScreenBounds(buildingIndex,
                    out var hostRenderer, out var minimum, out var maximum)) return;
            // Attachments are deliberately camera-nearer than building art.
            // This exceptional foreground layer prevents depth testing from
            // burying a component that occupies the host facade's screen area.
            var hostDepth = _camera.WorldToScreenPoint(hostRenderer.bounds.center).z;
            var anchorPixel = new Vector2(
                Mathf.Lerp(minimum.x, maximum.x, normalizedX),
                Mathf.Lerp(minimum.y, maximum.y, normalizedY));
            var pixel = new Vector3(anchorPixel.x, anchorPixel.y,
                hostDepth - Mathf.Max(0.1f, definition.ForegroundDepthMeters));
            model.position = _camera.ScreenToWorldPoint(pixel);
            // Building attachments are physical world objects. Keep their
            // vertical axis upright and align yaw to the host building rather
            // than the camera; the mounting bar then follows the same receding
            // screen direction as the building's side roof-line.
            var hostQuarterTurns = HostRotationQuarterTurns(buildingIndex);
            model.rotation = Quaternion.Euler(
                definition.ModelPitchDegrees,
                BuildingPropCatalog.ResolveYawDegrees(definition,
                    hostQuarterTurns, rotationDegrees),
                definition.ModelRollDegrees);
            var uniform = definition.VisibleWidthMeters /
                Mathf.Max(0.01f, definition.ModelNativeWidthMeters) *
                Mathf.Max(0.1f, scale);
            model.localScale = Vector3.one * uniform;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private void PositionBuildingPropModel(Transform model,
            int buildingIndex, PlacedBuildingProp attachment)
        {
            if (attachment == null) return;
            if (!attachment.HasHostLocalPosition &&
                !InitializeHostLocalPosition(attachment, buildingIndex,
                    attachment.NormalizedX, attachment.NormalizedY))
            {
                PositionBuildingPropModel(model, buildingIndex,
                    attachment.NormalizedX, attachment.NormalizedY,
                    attachment.ComponentId, attachment.Scale,
                    attachment.RotationDegrees);
                return;
            }
            var definition = BuildingPropCatalog.Find(attachment.ComponentId);
            if (model == null || definition == null ||
                !TryHostMetrics(buildingIndex, out var center,
                    out var hostRotation, out _, out _, out _)) return;
            model.position = center + hostRotation * new Vector3(
                attachment.HostLocalX,
                attachment.HostLocalY,
                attachment.HostLocalZ);
            model.rotation = Quaternion.Euler(
                definition.ModelPitchDegrees,
                BuildingPropCatalog.ResolveYawDegrees(definition,
                    HostRotationQuarterTurns(buildingIndex),
                    attachment.RotationDegrees),
                definition.ModelRollDegrees);
            var uniform = definition.VisibleWidthMeters /
                Mathf.Max(0.01f, definition.ModelNativeWidthMeters) *
                Mathf.Max(0.1f, attachment.Scale);
            model.localScale = Vector3.one * uniform;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.materials)
                {
                    if (material != null && material.HasProperty("_ZTest"))
                        material.SetFloat("_ZTest", (float)
                            UnityEngine.Rendering.CompareFunction.LessEqual);
                }
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private bool IsHostFacadeFacingCamera(PlacedBuildingProp attachment,
            int hostKey)
        {
            if (_camera == null) return true;
            if (!TryHostMetrics(hostKey, out var center, out var hostRotation,
                    out var width, out var depth, out _)) return true;
            var halfWidth = Mathf.Max(0.01f, width * 0.5f);
            var halfDepth = Mathf.Max(0.01f, depth * 0.5f);
            var xRatio = Mathf.Abs(attachment.HostLocalX) / halfWidth;
            var zRatio = Mathf.Abs(attachment.HostLocalZ) / halfDepth;
            var localNormal = xRatio >= zRatio
                ? Vector3.right * Mathf.Sign(attachment.HostLocalX)
                : Vector3.forward * Mathf.Sign(attachment.HostLocalZ);
            var worldNormal = hostRotation * localNormal;
            var toCamera = (_camera.transform.position -
                (center + hostRotation * new Vector3(
                    attachment.HostLocalX, attachment.HostLocalY,
                    attachment.HostLocalZ))).normalized;
            return Vector3.Dot(worldNormal, toCamera) > 0f;
        }

        public static Vector3 ResolveHostLocalWorldPosition(
            PlacedBuilding building, Vector3 hostLocalPosition)
        {
            if (building == null) return hostLocalPosition;
            var center = new Vector3(building.CellX, 0f, building.CellZ);
            var hostRotation = Quaternion.Euler(
                0f, building.RotationQuarterTurns * 90f, 0f);
            return center + hostRotation * hostLocalPosition;
        }

        private bool InitializeHostLocalPosition(PlacedBuildingProp attachment,
            int buildingIndex, float normalizedX, float normalizedY)
        {
            if (attachment == null) return false;
            if (attachment.HasHostLocalPosition) return true;
            if (!TryHostScreenBounds(buildingIndex, out _,
                    out var minimum, out var maximum)) return false;
            var pixel = new Vector2(
                Mathf.Lerp(minimum.x, maximum.x, normalizedX),
                Mathf.Lerp(minimum.y, maximum.y, normalizedY));
            return SetHostLocalPositionFromPixel(attachment, buildingIndex, pixel);
        }

        private bool SetHostLocalPositionFromPixel(
            PlacedBuildingProp attachment, int buildingIndex, Vector2 pixel)
        {
            if (attachment == null || _camera == null ||
                !TryHostMetrics(buildingIndex, out var center,
                    out var hostRotation, out var width, out var depth,
                    out var height))
                return false;
            var ray = _camera.ScreenPointToRay(pixel);
            if (!TryResolvePrimitiveFacadeLocalPosition(ray, center,
                    hostRotation, width, depth, height,
                    attachment.ProjectionDepthMeters,
                    out var local, out var hostElevation)) return false;
            attachment.HostLocalX = local.x;
            attachment.HostLocalY = local.y;
            attachment.HostLocalZ = local.z;
            attachment.HostElevation = hostElevation;
            attachment.HasHostLocalPosition = true;
            return true;
        }

        public static bool TryResolvePrimitiveFacadeLocalPosition(
            Ray worldRay, Vector3 center, Quaternion hostRotation,
            float widthMeters, float depthMeters, float heightMeters,
            float projectionDepthMeters, out Vector3 localPosition,
            out string hostElevation)
        {
            localPosition = default;
            hostElevation = "";
            var inverse = Quaternion.Inverse(hostRotation);
            var origin = inverse * (worldRay.origin - center);
            var direction = inverse * worldRay.direction;
            var halfWidth = Mathf.Max(0.01f, widthMeters * 0.5f);
            var halfDepth = Mathf.Max(0.01f, depthMeters * 0.5f);
            var projection = Mathf.Max(0f, projectionDepthMeters);
            var bestDistance = float.PositiveInfinity;

            TryFacadePlane(origin, direction, 0, halfWidth + projection,
                halfDepth, heightMeters, "Right", ref bestDistance,
                ref localPosition, ref hostElevation);
            TryFacadePlane(origin, direction, 0, -halfWidth - projection,
                halfDepth, heightMeters, "Left", ref bestDistance,
                ref localPosition, ref hostElevation);
            TryFacadePlane(origin, direction, 2, halfDepth + projection,
                halfWidth, heightMeters, "Front", ref bestDistance,
                ref localPosition, ref hostElevation);
            TryFacadePlane(origin, direction, 2, -halfDepth - projection,
                halfWidth, heightMeters, "Back", ref bestDistance,
                ref localPosition, ref hostElevation);
            return bestDistance < float.PositiveInfinity;
        }

        private static void TryFacadePlane(Vector3 origin, Vector3 direction,
            int normalAxis, float planeCoordinate, float tangentLimit,
            float heightMeters, string elevation, ref float bestDistance,
            ref Vector3 bestPosition, ref string bestElevation)
        {
            var denominator = normalAxis == 0 ? direction.x : direction.z;
            if (Mathf.Abs(denominator) <= 0.0001f) return;
            var originCoordinate = normalAxis == 0 ? origin.x : origin.z;
            var distance = (planeCoordinate - originCoordinate) / denominator;
            if (distance < 0f || distance >= bestDistance) return;
            var point = origin + direction * distance;
            var tangent = normalAxis == 0 ? point.z : point.x;
            if (Mathf.Abs(tangent) > tangentLimit || point.y < 0f ||
                point.y > heightMeters) return;
            bestDistance = distance;
            bestPosition = point;
            bestElevation = elevation;
        }

        private int HostRotationQuarterTurns(int hostKey)
        {
            if (!IsBuilding3DHost(hostKey))
                return hostKey >= 0 &&
                    hostKey < (_session.Data.Buildings?.Count ?? 0)
                        ? _session.Data.Buildings[hostKey].RotationQuarterTurns
                        : 0;
            var index = Building3DIndex(hostKey);
            if (index < 0 || index >= (_session.Data.Buildings3D?.Count ?? 0))
                return 0;
            var placed = _session.Data.Buildings3D[index];
            return placed.RotationEighthTurns >= 0
                ? Mathf.RoundToInt(placed.RotationEighthTurns * 0.5f)
                : placed.RotationQuarterTurns;
        }

        private bool TryHostMetrics(int hostKey, out Vector3 center,
            out Quaternion rotation, out float width, out float depth,
            out float height)
        {
            center = Vector3.zero;
            rotation = Quaternion.identity;
            width = depth = height = 0f;
            if (!IsBuilding3DHost(hostKey))
            {
                if (hostKey < 0 ||
                    hostKey >= (_session.Data.Buildings?.Count ?? 0)) return false;
                var building = _session.Data.Buildings[hostKey];
                var catalogItem = BuildingCatalog.Find(building.BuildingId);
                if (string.IsNullOrWhiteSpace(catalogItem.PackageResourcePath))
                    return false;
                var package = HybridBuildingPackageRegistry.Load(
                    catalogItem.PackageResourcePath);
                if (package == null) return false;
                center = new Vector3(building.CellX, 0f, building.CellZ);
                rotation = Quaternion.Euler(
                    0f, building.RotationQuarterTurns * 90f, 0f);
                width = package.WidthMeters;
                depth = package.DepthMeters;
                height = package.HeightMeters;
                return true;
            }

            var index = Building3DIndex(hostKey);
            if (index < 0 || index >= (_session.Data.Buildings3D?.Count ?? 0) ||
                index >= _experimentalBuilding3DVisibleRoots.Count) return false;
            var placed3D = _session.Data.Buildings3D[index];
            var root = _experimentalBuilding3DVisibleRoots[index];
            if (root == null) return false;
            var package3D = root.GetComponentInChildren<Building3DPackageInstance>(true)
                ?.Package;
            var bounds = BuildingPropCombinedRendererBounds(root,
                out var hasBounds);
            if (!hasBounds) return false;
            center = new Vector3(placed3D.X, 0f, placed3D.Z);
            var yaw = BrownstoneDefaultFacingDegrees +
                (placed3D.RotationEighthTurns >= 0
                    ? placed3D.RotationEighthTurns * 45f
                    : placed3D.RotationQuarterTurns * 90f);
            rotation = Quaternion.Euler(0f, yaw, 0f);
            width = package3D != null && package3D.FootprintMeters.x > 0f
                ? package3D.FootprintMeters.x : bounds.size.x;
            depth = package3D != null && package3D.FootprintMeters.y > 0f
                ? package3D.FootprintMeters.y : bounds.size.z;
            height = bounds.size.y;
            return true;
        }

        private static Bounds BuildingPropCombinedRendererBounds(GameObject root,
            out bool found)
        {
            var result = default(Bounds);
            found = false;
            if (root == null) return result;
            var geometryRoot = BuildingSelectionGeometryRoot(root);
            foreach (var renderer in geometryRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    !IsBuildingSelectionBeautyRenderer(renderer,
                        geometryRoot.transform))
                    continue;
                if (!found) { result = renderer.bounds; found = true; }
                else result.Encapsulate(renderer.bounds);
            }
            return result;
        }

        private int FindBuilding3DHitIndex(Vector2 pixel)
        {
            var bestIndex = -1;
            var bestDepth = float.PositiveInfinity;
            if (_camera == null) return bestIndex;
            var ray = _camera.ScreenPointToRay(pixel);
            // Only this list has a one-to-one ordering with Data.Buildings3D.
            // _experimentalBuilding3DRoots also contains shadow clones,
            // receiver casters, and selection outlines and cannot be used as
            // a persistent host index.
            for (var index = 0;
                 index < _experimentalBuilding3DVisibleRoots.Count; index++)
            {
                var root = _experimentalBuilding3DVisibleRoots[index];
                if (!TryRaycastBuildingBeautyMesh(root, ray,
                        _camera.farClipPlane, out var hit) ||
                    hit.distance >= bestDepth) continue;
                bestIndex = index;
                bestDepth = hit.distance;
            }
            return bestIndex;
        }

        private bool TryHostScreenBounds(int hostKey, out Renderer renderer,
            out Vector2 minimum, out Vector2 maximum)
        {
            if (!IsBuilding3DHost(hostKey))
            {
                var success = TryBuildingArtworkScreenBounds(hostKey,
                    out var sprite, out minimum, out maximum);
                renderer = sprite;
                return success;
            }
            return TryBuilding3DScreenBounds(Building3DIndex(hostKey),
                out renderer, out minimum, out maximum, out _);
        }

        private bool TryBuilding3DScreenBounds(int index, out Renderer renderer,
            out Vector2 minimum, out Vector2 maximum, out float nearestDepth)
        {
            renderer = null;
            minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            nearestDepth = float.PositiveInfinity;
            if (_camera == null || index < 0 ||
                index >= _experimentalBuilding3DVisibleRoots.Count) return false;
            var root = _experimentalBuilding3DVisibleRoots[index];
            if (root == null || !root.activeInHierarchy) return false;
            var geometryRoot = BuildingSelectionGeometryRoot(root);
            var found = false;
            foreach (var candidate in geometryRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!candidate.enabled || !candidate.gameObject.activeInHierarchy ||
                    !IsBuildingSelectionBeautyRenderer(candidate,
                        geometryRoot.transform))
                    continue;
                var bounds = candidate.bounds;
                for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                for (var z = -1; z <= 1; z += 2)
                {
                    var screen = _camera.WorldToScreenPoint(bounds.center +
                        Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                    if (screen.z <= 0f) continue;
                    minimum = Vector2.Min(minimum, screen);
                    maximum = Vector2.Max(maximum, screen);
                    if (screen.z < nearestDepth)
                    {
                        nearestDepth = screen.z;
                        renderer = candidate;
                    }
                    found = true;
                }
            }
            return found;
        }

        private int FindBuildingArtworkHitIndex(Vector2 pixel)
        {
            var bestIndex = -1;
            var bestScore = float.PositiveInfinity;
            for (var index = 0; index < (_session.Data.Buildings?.Count ?? 0); index++)
            {
                if (!TryBuildingArtworkScreenBounds(index, out _,
                        out var minimum, out var maximum) ||
                    pixel.x < minimum.x || pixel.x > maximum.x ||
                    pixel.y < minimum.y || pixel.y > maximum.y) continue;
                var score = ((minimum + maximum) * 0.5f - pixel).sqrMagnitude;
                if (score >= bestScore) continue;
                bestScore = score;
                bestIndex = index;
            }
            return bestIndex;
        }

        private bool TryBuildingArtworkScreenBounds(int buildingIndex,
            out SpriteRenderer renderer, out Vector2 minimum, out Vector2 maximum)
        {
            renderer = null;
            minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            if (_camera == null) return false;
            var presentation = PresentationForBuildingIndex(buildingIndex);
            if (presentation == null ||
                !presentation.TryGetArtworkRenderer(out renderer)) return false;
            var bounds = renderer.bounds;
            var found = false;
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var screen = _camera.WorldToScreenPoint(
                    bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3(x, y, z)));
                if (screen.z <= 0f) continue;
                minimum = Vector2.Min(minimum, screen);
                maximum = Vector2.Max(maximum, screen);
                found = true;
            }
            return found;
        }

        public bool TryGetSelectedBuildingPanelBounds(
            Vector2 panelSize, out Rect panelBounds)
        {
            panelBounds = default;
            if (_camera == null || panelSize.x <= 0f || panelSize.y <= 0f ||
                _session.SelectedBuildingIndex < 0)
                return false;
            var selectedIndex = _session.SelectedBuildingIndex;
            var presentation = PresentationForBuildingIndex(selectedIndex);
            var minimum = new Vector2(
                float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(
                float.NegativeInfinity, float.NegativeInfinity);
            var hasTightBounds = presentation != null &&
                presentation.TryGetVisibleArtworkScreenBounds(
                    _camera, out minimum, out maximum);
            if (!hasTightBounds && !TryBuildingArtworkScreenBounds(
                    selectedIndex, out _, out minimum, out maximum))
                return false;
            var cameraWidth = Mathf.Max(1f, _camera.pixelWidth);
            var cameraHeight = Mathf.Max(1f, _camera.pixelHeight);
            var left = minimum.x / cameraWidth * panelSize.x;
            var right = maximum.x / cameraWidth * panelSize.x;
            var top = (1f - maximum.y / cameraHeight) * panelSize.y;
            var bottom = (1f - minimum.y / cameraHeight) * panelSize.y;
            panelBounds = Rect.MinMaxRect(
                Mathf.Clamp(left, 0f, panelSize.x),
                Mathf.Clamp(top, 0f, panelSize.y),
                Mathf.Clamp(right, 0f, panelSize.x),
                Mathf.Clamp(bottom, 0f, panelSize.y));
            return panelBounds.width > 0f && panelBounds.height > 0f;
        }

        private static void ApplyBuildingPropMaterials(GameObject root,
            BuildingPropDefinition definition)
        {
            var baseColor = Resources.Load<Texture2D>(definition.BaseColorResourcePath);
            if (baseColor == null) return;
            var normal = Resources.Load<Texture2D>(definition.NormalResourcePath);
            var shader = Shader.Find("CityForgeV3/AlwaysVisibleBuildingProp");
            if (shader == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var isInterior = renderer.name == "CF_STOREFRONT_INTERIOR";
                var materials = renderer.materials;
                for (var index = 0; index < materials.Length; index++)
                {
                    var material = new Material(shader)
                    {
                        name = $"{definition.Id} Runtime Material",
                        mainTexture = isInterior
                            ? Texture2D.whiteTexture : baseColor,
                        color = isInterior
                            ? new Color(0.16f, 0.085f, 0.035f, 1f)
                            : Color.white
                    };
                    if (normal != null && !isInterior)
                    {
                        material.EnableKeyword("_NORMALMAP");
                        material.SetTexture("_BumpMap", normal);
                    }
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Glossiness",
                        definition.NightLighting ? 0.16f : 0.32f);
                    material.SetFloat("_UseWoodBackface",
                        definition.Id == BuildingPropCatalog.AleHouseSignId
                            ? 1f : 0f);
                    material.SetFloat("_ZTest", (float)
                        UnityEngine.Rendering.CompareFunction.LessEqual);
                    material.renderQueue = 2455;
                    materials[index] = material;
                }
                renderer.materials = materials;
                renderer.sortingOrder = 2200;
            }
        }

        private static void ApplyBuildingPropPreviewMaterials(GameObject root)
        {
            var shader = Shader.Find("CityForgeV3/BuildingPropPlacementPreview");
            if (root == null || shader == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                var preview = new Material[source.Length];
                for (var index = 0; index < source.Length; index++)
                {
                    var material = new Material(shader)
                    {
                        name = "Building Prop Always-Visible Placement Material",
                        mainTexture = source[index] == null ? null : source[index].mainTexture,
                        // Keep the source artwork legible while giving the
                        // preview a restrained cool placement wash. The old
                        // saturated green multiplier made the blue storefront
                        // look fluorescent and materially broken.
                        color = new Color(0.88f, 0.98f, 1f, 0.82f),
                        renderQueue = 5000
                    };
                    preview[index] = material;
                }
                renderer.sharedMaterials = preview;
            }
            ConfigureBuildingPropPreviewRenderers(root);
        }

        private static void ConfigureBuildingPropPreviewRenderers(GameObject root)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingOrder = 2200;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void BuildBuildingPropOverlayPass()
        {
            if (_camera == null) return;
            _camera.cullingMask |= 1 << _buildingPropOverlayLayer;
        }

        private void RebuildBuildingPropOverlayPass()
        {
            // Building props render normally on their dedicated camera layer.
            // Their retained Resources shaders own the always-visible depth
            // policy, so no separate camera or command-buffer pass is needed.
        }

        private void SetBuildingPropOverlayLayer(GameObject root)
        {
            if (root == null || _buildingPropOverlayLayer < 0) return;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = _buildingPropOverlayLayer;
        }

        private void ConfigureBuildingPropNightLighting(GameObject root,
            BuildingPropDefinition definition)
        {
            if (root == null || definition?.NightLighting != true) return;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            var active = IsLamppostLightingActive();
            var facadeForward = root.transform.forward;
            var facadeRight = root.transform.right;
            var lampHeight = bounds.min.y + bounds.size.y * 0.48f;
            for (var side = -1; side <= 1; side += 2)
            {
                var lampObject = new GameObject(side < 0
                    ? "CF Storefront Lamp Left" : "CF Storefront Lamp Right");
                lampObject.transform.SetParent(root.transform, true);
                lampObject.transform.position = new Vector3(
                    bounds.center.x, lampHeight, bounds.center.z) +
                    facadeRight * (bounds.size.x * 0.34f * side) +
                    facadeForward * (bounds.size.z * 0.6f + 0.08f);
                var lamp = lampObject.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.color = new Color(1f, 0.58f, 0.25f);
                lamp.intensity = 1.65f;
                lamp.range = 4.2f;
                lamp.shadows = LightShadows.Soft;
                lamp.enabled = active;
            }
            var windowObject = new GameObject("CF Storefront Window Light");
            windowObject.transform.SetParent(root.transform, true);
            windowObject.transform.position = new Vector3(
                bounds.center.x, bounds.min.y + bounds.size.y * 0.42f,
                bounds.center.z) + facadeForward *
                (bounds.size.z * 0.6f + 0.12f);
            var windowLight = windowObject.AddComponent<Light>();
            windowLight.type = LightType.Point;
            windowLight.color = new Color(1f, 0.48f, 0.18f);
            windowLight.intensity = 1.15f;
            windowLight.range = 3.6f;
            windowLight.shadows = LightShadows.None;
            windowLight.enabled = active;
        }

        private void UpdateBuildingPropNightLighting()
        {
            if (_buildingPropRoot == null) return;
            var active = IsLamppostLightingActive();
            foreach (var light in _buildingPropRoot.GetComponentsInChildren<Light>(true))
                if (light != null && light.name.StartsWith("CF Storefront ",
                        StringComparison.Ordinal))
                    light.enabled = active;
        }

    }
}
