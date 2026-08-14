using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private Transform _buildingPropRoot;
        private SpriteRenderer _buildingPropPreview;
        private readonly List<GameObject> _buildingPropPresentations = new();
        private string _buildingPropPreviewId = "";
        private int _buildingPropPreviewHostIndex = -1;
        private float _buildingPropPreviewX = 0.5f;
        private float _buildingPropPreviewY = 0.5f;
        private bool _buildingPropPlacementActive;

        private void BuildBuildingPropRoot()
        {
            _buildingPropRoot = new GameObject("Building Attachments").transform;
            _buildingPropRoot.SetParent(transform, false);
            var preview = new GameObject("Building Prop Placement Preview");
            preview.transform.SetParent(transform, false);
            _buildingPropPreview = preview.AddComponent<SpriteRenderer>();
            _buildingPropPreview.sortingOrder = 2200;
            var previewShader = Shader.Find("CityForgeV3/BuildingPropPlacementPreview");
            if (previewShader != null)
            {
                _buildingPropPreview.material = new Material(previewShader);
                _buildingPropPreview.material.renderQueue = 5000;
            }
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
            _buildingPropPreview.sprite = LoadBuildingPropSprite(componentId);
            _buildingPropPreview.color = new Color(0.45f, 1f, 0.62f, 0.58f);
            _buildingPropPreview.gameObject.SetActive(false);
        }

        public bool UpdateBuildingPropPreviewFromPanel(
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_buildingPropPlacementActive || _buildingPropPreview == null ||
                string.IsNullOrWhiteSpace(_buildingPropPreviewId) ||
                !TryBuildingAttachmentPoint(panelPosition, panelSize,
                    out var buildingIndex, out var normalizedX, out var normalizedY))
            {
                if (_buildingPropPreview != null)
                    _buildingPropPreview.gameObject.SetActive(false);
                _buildingPropPreviewHostIndex = -1;
                return false;
            }
            _buildingPropPreviewHostIndex = buildingIndex;
            _buildingPropPreviewX = normalizedX;
            _buildingPropPreviewY = normalizedY;
            PositionBuildingPropRenderer(_buildingPropPreview, buildingIndex,
                normalizedX, normalizedY, _buildingPropPreviewId, 1f);
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
            building.Attachments.Add(new PlacedBuildingProp
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                ComponentId = definition.Id,
                Revision = definition.Revision,
                HostElevation = definition.HostElevation,
                NormalizedX = normalizedX,
                NormalizedY = normalizedY,
                ProjectionDepthMeters = definition.ProjectionDepthMeters,
                Scale = 1f
            });
            _session.SelectBuilding(buildingIndex);
            ActiveObjectSelection = LotObjectSelectionKind.Building;
            ApplySessionState();
            NotifyStateChanged();
            return true;
        }

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
            buildingIndex = FindBuildingVisualHitIndex(pixel);
            if (buildingIndex < 0) return false;
            var renderer = PresentationForBuildingIndex(buildingIndex)?
                .GetComponentInChildren<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return false;
            var bounds = renderer.bounds;
            var min = _camera.WorldToScreenPoint(bounds.min);
            var max = _camera.WorldToScreenPoint(bounds.max);
            var left = Mathf.Min(min.x, max.x);
            var right = Mathf.Max(min.x, max.x);
            var bottom = Mathf.Min(min.y, max.y);
            var top = Mathf.Max(min.y, max.y);
            normalizedX = Mathf.Clamp01(Mathf.InverseLerp(left, right, pixel.x));
            normalizedY = Mathf.Clamp01(Mathf.InverseLerp(bottom, top, pixel.y));
            return pixel.x >= left && pixel.x <= right &&
                   pixel.y >= bottom && pixel.y <= top;
        }

        private void RefreshBuildingPropPresentations()
        {
            if (_buildingPropRoot == null || _camera == null) return;
            foreach (var presentation in _buildingPropPresentations)
                if (presentation != null) Destroy(presentation);
            _buildingPropPresentations.Clear();
            for (var buildingIndex = 0;
                 buildingIndex < (_session.Data.Buildings?.Count ?? 0);
                 buildingIndex++)
            {
                var building = _session.Data.Buildings[buildingIndex];
                building.Attachments ??= new List<PlacedBuildingProp>();
                foreach (var attachment in building.Attachments)
                {
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
                    ApplyBuildingPropMaterials(root, definition);
                    PositionBuildingPropModel(root.transform, buildingIndex,
                        attachment.NormalizedX, attachment.NormalizedY,
                        attachment.ComponentId, attachment.Scale);
                    var motion = root.AddComponent<BuildingPropSwingMotion>();
                    var stablePhase = Mathf.Abs(
                        attachment.InstanceId?.GetHashCode() ?? 0) % 1000 / 1000f;
                    motion.Configure(definition.SwingTransformName,
                        definition.SwingAmplitudeDegrees,
                        definition.SwingPeriodSeconds, stablePhase);
                    _buildingPropPresentations.Add(root);
                }
            }
        }

        private void PositionBuildingPropRenderer(SpriteRenderer renderer,
            int buildingIndex, float normalizedX, float normalizedY,
            string componentId, float scale)
        {
            var hostRenderer = PresentationForBuildingIndex(buildingIndex)?
                .GetComponentInChildren<SpriteRenderer>();
            var definition = BuildingPropCatalog.Find(componentId);
            if (renderer == null || renderer.sprite == null || hostRenderer == null ||
                definition == null) return;
            var bounds = hostRenderer.bounds;
            var min = _camera.WorldToScreenPoint(bounds.min);
            var max = _camera.WorldToScreenPoint(bounds.max);
            var depth = _camera.WorldToScreenPoint(bounds.center).z -
                definition.ForegroundDepthMeters;
            var pixel = new Vector3(
                Mathf.Lerp(Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x), normalizedX),
                Mathf.Lerp(Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y), normalizedY),
                depth);
            renderer.transform.position = _camera.ScreenToWorldPoint(pixel);
            renderer.transform.rotation = _camera.transform.rotation;
            var spriteWidth = Mathf.Max(0.01f, renderer.sprite.bounds.size.x);
            var uniform = definition.VisibleWidthMeters / spriteWidth * Mathf.Max(0.1f, scale);
            renderer.transform.localScale = Vector3.one * uniform;
        }

        private void PositionBuildingPropModel(Transform model,
            int buildingIndex, float normalizedX, float normalizedY,
            string componentId, float scale)
        {
            var hostRenderer = PresentationForBuildingIndex(buildingIndex)?
                .GetComponentInChildren<SpriteRenderer>();
            var definition = BuildingPropCatalog.Find(componentId);
            if (model == null || hostRenderer == null || definition == null) return;
            var bounds = hostRenderer.bounds;
            var min = _camera.WorldToScreenPoint(bounds.min);
            var max = _camera.WorldToScreenPoint(bounds.max);
            // Attachments are deliberately camera-nearer than building art.
            // This exceptional foreground layer prevents depth testing from
            // burying a component that occupies the host facade's screen area.
            var hostDepth = _camera.WorldToScreenPoint(bounds.center).z;
            var pixel = new Vector3(
                Mathf.Lerp(Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x), normalizedX),
                Mathf.Lerp(Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y), normalizedY),
                hostDepth - Mathf.Max(0.1f, definition.ForegroundDepthMeters));
            model.position = _camera.ScreenToWorldPoint(pixel);
            // First inherit the host artwork/camera alignment, then turn the
            // attachment around its own vertical axis. A world-space yaw here
            // also tilts the model in an isometric camera, making hanging
            // signs read as rolled or upside down.
            var hostQuarterTurns = buildingIndex >= 0 &&
                buildingIndex < (_session.Data.Buildings?.Count ?? 0)
                    ? _session.Data.Buildings[buildingIndex].RotationQuarterTurns
                    : 0;
            model.rotation = _camera.transform.rotation * Quaternion.Euler(
                0f,
                definition.ModelYawDegrees + hostQuarterTurns * 90f,
                0f);
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

        private int FindBuildingVisualHitIndex(Vector2 pixel)
        {
            var bestIndex = -1;
            var bestScore = float.PositiveInfinity;
            for (var index = 0; index < (_session.Data.Buildings?.Count ?? 0); index++)
            {
                var presentation = PresentationForBuildingIndex(index);
                if (!BuildingVisualContainsCameraPixel(presentation, pixel)) continue;
                var score = BuildingVisualHitScore(presentation, pixel);
                if (score >= bestScore) continue;
                bestScore = score;
                bestIndex = index;
            }
            return bestIndex;
        }

        private static void ApplyBuildingPropMaterials(GameObject root,
            BuildingPropDefinition definition)
        {
            var baseColor = Resources.Load<Texture2D>(definition.BaseColorResourcePath);
            if (baseColor == null) return;
            var normal = Resources.Load<Texture2D>(definition.NormalResourcePath);
            var metallic = Resources.Load<Texture2D>(definition.MetallicResourcePath);
            var shader = Shader.Find("Standard");
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
                    materials[index] = material;
                }
                renderer.materials = materials;
            }
        }

        private static Sprite LoadBuildingPropSprite(string componentId)
        {
            var definition = BuildingPropCatalog.Find(componentId);
            var texture = definition == null ? null :
                Resources.Load<Texture2D>(definition.PreviewResourcePath);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 500f, 0, SpriteMeshType.FullRect);
        }
    }
}
