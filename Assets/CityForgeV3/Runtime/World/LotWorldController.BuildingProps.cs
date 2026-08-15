using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private Transform _buildingPropRoot;
        private Transform _buildingPropPreview;
        private CommandBuffer _buildingPropOverlayCommands;
        private int _buildingPropOverlayLayer = -1;
        private readonly List<GameObject> _buildingPropPresentations = new();
        private string _buildingPropPreviewId = "";
        private int _buildingPropPreviewHostIndex = -1;
        private float _buildingPropPreviewX = 0.5f;
        private float _buildingPropPreviewY = 0.5f;
        private bool _buildingPropPlacementActive;

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
                    SetBuildingPropOverlayLayer(root);
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

        private void PositionBuildingPropModel(Transform model,
            int buildingIndex, float normalizedX, float normalizedY,
            string componentId, float scale)
        {
            var definition = BuildingPropCatalog.Find(componentId);
            if (model == null || definition == null ||
                !TryBuildingArtworkScreenBounds(buildingIndex,
                    out var hostRenderer, out var minimum, out var maximum)) return;
            // Attachments are deliberately camera-nearer than building art.
            // This exceptional foreground layer prevents depth testing from
            // burying a component that occupies the host facade's screen area.
            var hostDepth = _camera.WorldToScreenPoint(hostRenderer.bounds.center).z;
            var pixel = new Vector3(
                Mathf.Lerp(minimum.x, maximum.x, normalizedX),
                Mathf.Lerp(minimum.y, maximum.y, normalizedY),
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
            if (_camera == null || _buildingPropOverlayCommands != null) return;
            _buildingPropOverlayCommands = new CommandBuffer
            {
                name = "City Forge Building Prop Final Pass"
            };
            _camera.AddCommandBuffer(CameraEvent.AfterEverything,
                _buildingPropOverlayCommands);
            _camera.cullingMask &= ~(1 << _buildingPropOverlayLayer);
            RebuildBuildingPropOverlayPass();
        }

        private void RebuildBuildingPropOverlayPass()
        {
            if (_buildingPropOverlayCommands == null) return;
            _buildingPropOverlayCommands.Clear();
            foreach (var presentation in _buildingPropPresentations)
                AppendBuildingPropToOverlayPass(presentation);
            if (_buildingPropPreview != null &&
                _buildingPropPreview.gameObject.activeInHierarchy)
                AppendBuildingPropToOverlayPass(_buildingPropPreview.gameObject);
        }

        private void AppendBuildingPropToOverlayPass(GameObject root)
        {
            if (root == null || _buildingPropOverlayCommands == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                var materials = renderer.sharedMaterials;
                for (var submesh = 0; submesh < materials.Length; submesh++)
                    if (materials[submesh] != null)
                        _buildingPropOverlayCommands.DrawRenderer(
                            renderer, materials[submesh], submesh);
            }
        }

        private void SetBuildingPropOverlayLayer(GameObject root)
        {
            if (root == null || _buildingPropOverlayLayer < 0) return;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = _buildingPropOverlayLayer;
        }

    }
}
