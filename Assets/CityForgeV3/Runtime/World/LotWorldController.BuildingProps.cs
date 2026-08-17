using System;
using System.Collections.Generic;
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
            _buildingPropPlacementActive = active;
            if (!active && _buildingPropPreview != null)
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
            var definition = BuildingPropCatalog.Find(componentId);
            if (definition == null) return false;
            var building = _session.Data.Buildings[buildingIndex];
            building.Attachments ??= new List<PlacedBuildingProp>();
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
            building.Attachments.Add(attachment);
            _session.SelectBuilding(buildingIndex);
            ActiveObjectSelection = LotObjectSelectionKind.Building;
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
            if (buildingIndex < 0 || !TryBuildingArtworkScreenBounds(
                    buildingIndex, out _, out var minimum, out var maximum))
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
                _selectedBuildingPropBuildingIndex >= 0 &&
                _selectedBuildingPropAttachmentIndex >= 0;
            foreach (var presentation in _buildingPropPresentations)
                if (presentation != null) Destroy(presentation);
            _buildingPropPresentations.Clear();
            _buildingPropPresentationKeys.Clear();
            _hoverBuildingPropPresentationIndex = -1;
            _selectedBuildingPropPresentationIndex = -1;
            for (var buildingIndex = 0;
                 buildingIndex < (_session.Data.Buildings?.Count ?? 0);
                 buildingIndex++)
            {
                var building = _session.Data.Buildings[buildingIndex];
                building.Attachments ??= new List<PlacedBuildingProp>();
                for (var attachmentIndex = 0;
                     attachmentIndex < building.Attachments.Count;
                     attachmentIndex++)
                {
                    var attachment = building.Attachments[attachmentIndex];
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
                    PositionBuildingPropModel(root.transform, buildingIndex,
                        attachment);
                    var motion = root.AddComponent<BuildingPropSwingMotion>();
                    var stablePhase = Mathf.Abs(
                        attachment.InstanceId?.GetHashCode() ?? 0) % 1000 / 1000f;
                    motion.Configure(definition.SwingTransformName,
                        definition.SwingAmplitudeDegrees,
                        definition.SwingPeriodSeconds, stablePhase);
                    _buildingPropPresentations.Add(root);
                    _buildingPropPresentationKeys.Add(
                        new Vector2Int(buildingIndex, attachmentIndex));
                    if (restoreSelectedProp &&
                        buildingIndex == _selectedBuildingPropBuildingIndex &&
                        attachmentIndex == _selectedBuildingPropAttachmentIndex)
                        _selectedBuildingPropPresentationIndex =
                            _buildingPropPresentations.Count - 1;
                }
            }
            if (_selectedBuildingPropPresentationIndex >= 0)
                ApplyBuildingPropHover(_selectedBuildingPropPresentationIndex);
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
            if (key.x < 0 || key.x >= (_session.Data.Buildings?.Count ?? 0) ||
                key.y < 0 || key.y >= _session.Data.Buildings[key.x].Attachments.Count)
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
                _selectedBuildingPropBuildingIndex < 0 ||
                _selectedBuildingPropBuildingIndex >= (_session.Data.Buildings?.Count ?? 0))
                return false;
            var building = _session.Data.Buildings[_selectedBuildingPropBuildingIndex];
            if (_selectedBuildingPropAttachmentIndex < 0 ||
                _selectedBuildingPropAttachmentIndex >= building.Attachments.Count ||
                !TryBuildingArtworkScreenBounds(_selectedBuildingPropBuildingIndex,
                    out _, out var minimum, out var maximum)) return false;
            var pixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight)) +
                _buildingPropDragOffsetPixels;
            var attachment = building.Attachments[_selectedBuildingPropAttachmentIndex];
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
            if (_selectedBuildingPropBuildingIndex < 0 ||
                _selectedBuildingPropBuildingIndex >=
                    (_session.Data.Buildings?.Count ?? 0)) return false;
            var building = _session.Data.Buildings[_selectedBuildingPropBuildingIndex];
            if (_selectedBuildingPropAttachmentIndex < 0 ||
                _selectedBuildingPropAttachmentIndex >=
                    (building.Attachments?.Count ?? 0)) return false;
            building.Attachments.RemoveAt(_selectedBuildingPropAttachmentIndex);
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

        public bool RotateSelectedBuildingProp45Degrees()
        {
            if (_selectedBuildingPropPresentationIndex < 0 ||
                _selectedBuildingPropPresentationIndex >= _buildingPropPresentations.Count ||
                _selectedBuildingPropBuildingIndex < 0 ||
                _selectedBuildingPropBuildingIndex >= (_session.Data.Buildings?.Count ?? 0))
                return false;
            var building = _session.Data.Buildings[_selectedBuildingPropBuildingIndex];
            if (_selectedBuildingPropAttachmentIndex < 0 ||
                _selectedBuildingPropAttachmentIndex >= building.Attachments.Count)
                return false;
            var attachment = building.Attachments[_selectedBuildingPropAttachmentIndex];
            var nextPreset = (Mathf.RoundToInt(
                attachment.RotationDegrees / 45f) + 1) % 8;
            attachment.RotationDegrees = nextPreset * 45f;
            PositionBuildingPropModel(
                _buildingPropPresentations[_selectedBuildingPropPresentationIndex].transform,
                _selectedBuildingPropBuildingIndex, attachment);
            RebuildBuildingPropOverlayPass();
            ApplyBuildingPropHover(_selectedBuildingPropPresentationIndex);
            NotifyStateChanged();
            return true;
        }

        private void PositionBuildingPropModel(Transform model,
            int buildingIndex, float normalizedX, float normalizedY,
            string componentId, float scale, float rotationDegrees = 0f)
        {
            var definition = BuildingPropCatalog.Find(componentId);
            if (model == null || definition == null ||
                !TryBuildingArtworkScreenBounds(buildingIndex,
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
            var hostQuarterTurns = buildingIndex >= 0 &&
                buildingIndex < (_session.Data.Buildings?.Count ?? 0)
                    ? _session.Data.Buildings[buildingIndex].RotationQuarterTurns
                    : 0;
            model.rotation = Quaternion.Euler(
                0f, BuildingPropCatalog.ResolveYawDegrees(definition,
                    hostQuarterTurns, rotationDegrees), 0f);
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
                buildingIndex < 0 ||
                buildingIndex >= (_session.Data.Buildings?.Count ?? 0)) return;
            var building = _session.Data.Buildings[buildingIndex];
            model.position = ResolveHostLocalWorldPosition(building, new Vector3(
                attachment.HostLocalX,
                attachment.HostLocalY,
                attachment.HostLocalZ));
            model.rotation = Quaternion.Euler(
                0f, BuildingPropCatalog.ResolveYawDegrees(definition,
                    building.RotationQuarterTurns,
                    attachment.RotationDegrees), 0f);
            var uniform = definition.VisibleWidthMeters /
                Mathf.Max(0.01f, definition.ModelNativeWidthMeters) *
                Mathf.Max(0.1f, attachment.Scale);
            model.localScale = Vector3.one * uniform;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
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
            if (TryInitializeAttachmentSocket(attachment, buildingIndex,
                    normalizedX, normalizedY)) return true;
            if (!TryBuildingArtworkScreenBounds(buildingIndex, out _,
                    out var minimum, out var maximum)) return false;
            var pixel = new Vector2(
                Mathf.Lerp(minimum.x, maximum.x, normalizedX),
                Mathf.Lerp(minimum.y, maximum.y, normalizedY));
            return SetHostLocalPositionFromPixel(attachment, buildingIndex, pixel);
        }

        private bool TryInitializeAttachmentSocket(PlacedBuildingProp attachment,
            int buildingIndex, float normalizedX, float normalizedY)
        {
            if (buildingIndex < 0 ||
                buildingIndex >= (_session.Data.Buildings?.Count ?? 0))
                return false;
            var building = _session.Data.Buildings[buildingIndex];
            var catalogItem = BuildingCatalog.Find(building.BuildingId);
            if (string.IsNullOrWhiteSpace(catalogItem.PackageResourcePath))
                return false;
            var package = HybridBuildingPackageRegistry.Load(
                catalogItem.PackageResourcePath);
            if (package == null || !package.TryAttachmentSocket(
                    attachment.ComponentId, attachment.HostElevation,
                    normalizedX, normalizedY, out var local)) return false;
            attachment.HostLocalX = local.x;
            attachment.HostLocalY = local.y;
            attachment.HostLocalZ = local.z;
            attachment.HasHostLocalPosition = true;
            return true;
        }

        private bool SetHostLocalPositionFromPixel(
            PlacedBuildingProp attachment, int buildingIndex, Vector2 pixel)
        {
            if (attachment == null || _camera == null || buildingIndex < 0 ||
                buildingIndex >= (_session.Data.Buildings?.Count ?? 0))
                return false;
            var building = _session.Data.Buildings[buildingIndex];
            var catalogItem = BuildingCatalog.Find(building.BuildingId);
            if (string.IsNullOrWhiteSpace(catalogItem.PackageResourcePath))
                return false;
            var package = HybridBuildingPackageRegistry.Load(
                catalogItem.PackageResourcePath);
            if (package == null) return false;
            var center = new Vector3(building.CellX, 0f, building.CellZ);
            var hostRotation = Quaternion.Euler(
                0f, building.RotationQuarterTurns * 90f, 0f);
            var outward = hostRotation * Vector3.forward;
            var facadePoint = center + outward *
                (package.DepthMeters * 0.5f +
                 Mathf.Max(0f, attachment.ProjectionDepthMeters));
            var ray = _camera.ScreenPointToRay(pixel);
            var plane = new Plane(outward, facadePoint);
            if (!plane.Raycast(ray, out var distance)) return false;
            var local = Quaternion.Inverse(hostRotation) *
                (ray.GetPoint(distance) - center);
            attachment.HostLocalX = Mathf.Clamp(local.x,
                -package.WidthMeters * 0.5f, package.WidthMeters * 0.5f);
            attachment.HostLocalY = Mathf.Clamp(local.y, 0f,
                package.HeightMeters);
            attachment.HostLocalZ = package.DepthMeters * 0.5f +
                Mathf.Max(0f, attachment.ProjectionDepthMeters);
            attachment.HasHostLocalPosition = true;
            return true;
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
            if (presentation == null) return false;
            foreach (var candidate in
                     presentation.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!candidate.enabled || !candidate.gameObject.activeInHierarchy ||
                    candidate.sprite == null) continue;
                if (renderer == null || candidate.name == "Directional Render")
                    renderer = candidate;
                if (candidate.name == "Directional Render") break;
            }
            if (renderer == null) return false;
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

        private static void ApplyBuildingPropMaterials(GameObject root,
            BuildingPropDefinition definition)
        {
            var baseColor = Resources.Load<Texture2D>(definition.BaseColorResourcePath);
            if (baseColor == null) return;
            var normal = Resources.Load<Texture2D>(definition.NormalResourcePath);
            var metallic = Resources.Load<Texture2D>(definition.MetallicResourcePath);
            var shader = Shader.Find("CityForgeV3/AlwaysVisibleBuildingProp");
            if (shader == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                for (var index = 0; index < materials.Length; index++)
                {
                    var material = new Material(shader)
                    {
                        name = $"{definition.Id} Runtime Material",
                        mainTexture = baseColor
                    };
                    if (normal != null)
                    {
                        material.EnableKeyword("_NORMALMAP");
                        material.SetTexture("_BumpMap", normal);
                    }
                    if (metallic != null)
                    {
                        material.EnableKeyword("_METALLICGLOSSMAP");
                        material.SetTexture("_MetallicGlossMap", metallic);
                        material.SetFloat("_Metallic", 0.35f);
                    }
                    material.SetFloat("_Glossiness", 0.32f);
                    material.renderQueue = 5000;
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
                        color = new Color(0.45f, 1f, 0.62f, 0.68f),
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

    }
}
