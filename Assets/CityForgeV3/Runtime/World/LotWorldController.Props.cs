using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private const string FencePropId = "wrought-iron-fence-straight-v01";
        private const string FenceCornerPropId = "wrought-iron-fence-corner-v01";
        private const string ThreeLanternLamppostPropId =
            "three-lantern-lamppost-v01";
        private const string FenceResourcePath =
            "CityForgeV3/Props/WroughtIronFenceV01/CF_WroughtIronFence_Straight_LOD0_v01";
        private const string FenceCornerResourcePath =
            "CityForgeV3/Props/WroughtIronFenceV01/CF_WroughtIronFence_Corner_LShape_LOD0_v02";
        private const string ThreeLanternLamppostResourcePath =
            "CityForgeV3/Props/ThreeLanternLamppostV01/CF_Prop_ThreeLanternLamppost_01_game_v01";
        // Blender FBX imports through Unity at 0.01; 336 restores the authored
        // 1.9-unit section to a 6.38 m City Forge prop.
        private const float FenceScale = 336f;
        // The corner source was authored 2.51x larger than the straight source.
        // 134 restores its pier/fence height to the straight section's 1.5 m.
        private const float FenceCornerScale = 134f;
        private const float FenceLengthMeters = 6.4f;
        private const float FenceDepthMeters = 0.48f;
        private const float FenceCornerLengthMeters = 2.55f;
        private const float ThreeLanternLamppostFootprintMeters = 0.9f;
        // Blender's FBX is authored in meters but Unity imports this exporter
        // contract at 0.01, so 100 restores the intended 4.5 m height.
        private const float ThreeLanternLamppostScale = 100f;
        public const float ThreeLanternLightPoolDiameterMeters = 12f;
        private Transform _propRoot;
        private Transform _propSelection;
        private Transform _propPreview;
        private readonly List<Transform> _propPresentations = new();
        private readonly List<Renderer> _propProjectedShadowRenderers = new();
        private readonly List<Material> _propProjectedShadowMaterials = new();
        private bool _propEditorActive;
        private bool _propPlacementActive;
        private bool _propDragActive;
        private Vector2 _propDragOffset;
        private string _propPreviewId = "";
        private bool _propPreviewHasPoint;
        private int _propPlacementRotationQuarterTurns;

        public int PropCount => _session.Data.Props?.Count ?? 0;
        public int SelectedPropIndex { get; private set; } = -1;
        public int PropPlacementRotationQuarterTurns =>
            _propPlacementRotationQuarterTurns;
        public bool HasArmedPropPlacement =>
            !string.IsNullOrWhiteSpace(_propPreviewId);
        public int ActivePropRotationQuarterTurns =>
            SelectedPropIndex >= 0 && SelectedPropIndex < PropCount
                ? _session.Data.Props[SelectedPropIndex].RotationQuarterTurns
                : _propPlacementRotationQuarterTurns;

#if UNITY_EDITOR
        public bool PlacePropForQa(string propId, float positionX, float positionZ,
            int rotationQuarterTurns = 0, bool alwaysAdd = false)
        {
            _session.Data.Props ??= new List<PlacedProp>();
            var prop = alwaysAdd ? null : _session.Data.Props.Find(item =>
                string.Equals(item.PropId, propId, StringComparison.OrdinalIgnoreCase));
            if (prop == null)
            {
                prop = new PlacedProp
                {
                    InstanceId = Guid.NewGuid().ToString("N"),
                    PropId = propId,
                    RotationQuarterTurns =
                        ((rotationQuarterTurns % 4) + 4) % 4
                };
                _session.Data.Props.Add(prop);
            }
            prop.PositionX = positionX;
            prop.PositionZ = positionZ;
            prop.RotationQuarterTurns =
                ((rotationQuarterTurns % 4) + 4) % 4;
            RebuildPropPresentations();
            NotifyStateChanged();
            return true;
        }
#endif

        private void BuildPropRoot()
        {
            _propRoot = new GameObject("Placed Props").transform;
            _propRoot.SetParent(transform, false);
            _propSelection = new GameObject("Selected Prop Highlight").transform;
            _propSelection.SetParent(transform, false);
            var material = LotSurfaceMaterial(new Color(1f, 0.72f, 0.12f, 0.9f), 2015);
            foreach (var edge in new[]
                     {
                         (new Vector3(0f, 0f, FenceDepthMeters * 0.5f),
                             new Vector3(FenceLengthMeters, 0.05f, 0.07f)),
                         (new Vector3(0f, 0f, -FenceDepthMeters * 0.5f),
                             new Vector3(FenceLengthMeters, 0.05f, 0.07f)),
                         (new Vector3(FenceLengthMeters * 0.5f, 0f, 0f),
                             new Vector3(0.07f, 0.05f, FenceDepthMeters)),
                         (new Vector3(-FenceLengthMeters * 0.5f, 0f, 0f),
                             new Vector3(0.07f, 0.05f, FenceDepthMeters))
                     })
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.transform.SetParent(_propSelection, false);
                marker.transform.localPosition = edge.Item1;
                marker.transform.localScale = edge.Item2;
                marker.GetComponent<Collider>().enabled = false;
                marker.GetComponent<Renderer>().sharedMaterial = material;
            }
            _propSelection.gameObject.SetActive(false);
        }

        public void SetPropEditorContext(bool active)
        {
            _propEditorActive = true;
            _propPlacementActive = active;
            ApplyPropSelection();
            if (_propPreview != null)
                _propPreview.gameObject.SetActive(_propPlacementActive && _propPreviewHasPoint &&
                    !string.IsNullOrWhiteSpace(_propPreviewId));
        }

        public void SetPropPlacementPreview(string propId)
        {
            _propPreviewId = propId ?? "";
            _propPreviewHasPoint = false;
            _propPlacementRotationQuarterTurns = 0;
            if (_propPreview != null)
            {
                if (Application.isPlaying) Destroy(_propPreview.gameObject);
                else DestroyImmediate(_propPreview.gameObject);
            }
            _propPreview = CreatePropPresentation(_propPreviewId, "Prop Placement Preview", 0.5f);
            if (_propPreview != null)
            {
                _propPreview.SetParent(transform, false);
                _propPreview.localRotation = Quaternion.Euler(0f,
                    _propPlacementRotationQuarterTurns * 90f, 0f);
                _propPreview.gameObject.SetActive(false);
            }
        }

        public bool RotatePropPlacementPreview(int direction)
        {
            if (!_propPlacementActive || _propPreview == null ||
                string.IsNullOrWhiteSpace(_propPreviewId)) return false;
            _propPlacementRotationQuarterTurns =
                ((_propPlacementRotationQuarterTurns + direction) % 4 + 4) % 4;
            _propPreview.localRotation = Quaternion.Euler(0f,
                _propPlacementRotationQuarterTurns * 90f, 0f);
            if (_propPreviewHasPoint)
            {
                var position = ClampPropPosition(new Vector2(
                    _propPreview.localPosition.x,
                    _propPreview.localPosition.z),
                    _propPlacementRotationQuarterTurns);
                _propPreview.localPosition = new Vector3(position.x, 0.055f, position.y);
                var canPlace = CanPlacePropAt(position,
                    _propPlacementRotationQuarterTurns, -1);
                SetPropOpacity(_propPreview, _propPreviewId,
                    canPlace ? 0.5f : 0.28f, canPlace);
            }
            return true;
        }

        public bool UpdatePropPreviewFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_propPlacementActive || string.IsNullOrWhiteSpace(_propPreviewId) ||
                _propPreview == null ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            var position = ClampPropPosition(new Vector2(point.x, point.z),
                _propPlacementRotationQuarterTurns);
            _propPreview.localPosition = new Vector3(position.x, 0.055f, position.y);
            _propPreview.localRotation = Quaternion.Euler(0f,
                _propPlacementRotationQuarterTurns * 90f, 0f);
            var canPlace = CanPlacePropAt(position,
                _propPlacementRotationQuarterTurns, -1);
            SetPropOpacity(_propPreview, _propPreviewId,
                canPlace ? 0.5f : 0.28f, canPlace);
            _propPreviewHasPoint = true;
            _propPreview.gameObject.SetActive(true);
            return true;
        }

        public bool BeginPropDragFromPanel(string propId,
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            var placingNewProp = !string.IsNullOrWhiteSpace(propId);
            if (placingNewProp)
            {
                // An armed catalog choice means "place another", even when its
                // camera-space bounds overlap an existing prop. This keeps
                // placement continuous within a 10 m lot tile; physical
                // footprint validation below still rejects true intersections.
                SelectedPropIndex = -1;
                _propPreviewId = propId;
                var position = ClampPropPosition(new Vector2(point.x, point.z),
                    _propPlacementRotationQuarterTurns);
                if (!CanPlacePropAt(position,
                        _propPlacementRotationQuarterTurns, -1)) return false;
                _session.Data.Props ??= new List<PlacedProp>();
                _session.Data.Props.Add(new PlacedProp
                {
                    InstanceId = Guid.NewGuid().ToString("N"),
                    PropId = propId,
                    PositionX = position.x,
                    PositionZ = position.y,
                    RotationQuarterTurns = _propPlacementRotationQuarterTurns
                });
                SelectedPropIndex = _session.Data.Props.Count - 1;
                RebuildPropPresentations();
            }
            else
            {
                var pixel = PanelToCameraPixel(panelPosition, panelSize,
                    new Vector2(_camera.pixelWidth, _camera.pixelHeight));
                SelectedPropIndex = PropIndexAtCameraPixel(pixel);
                if (SelectedPropIndex < 0) return false;
            }
            ActiveObjectSelection = LotObjectSelectionKind.Prop;
            SelectedFloraIndex = -1;
            ApplyFloraSelection();
            _session.Select(false);
            if (_selectionFootprint != null)
                _selectionFootprint.gameObject.SetActive(false);
            var selected = _session.Data.Props[SelectedPropIndex];
            _propDragOffset = new Vector2(selected.PositionX - point.x,
                selected.PositionZ - point.z);
            _propDragActive = true;
            _propPreviewHasPoint = false;
            if (_propPreview != null) _propPreview.gameObject.SetActive(false);
            ApplyPropSelection();
            return true;
        }

        public bool NudgeSelectedPropByScreenPixels(int horizontal, int vertical)
        {
            if (SelectedPropIndex < 0 || SelectedPropIndex >= PropCount ||
                _camera == null || (horizontal == 0 && vertical == 0)) return false;
            var pixelWidth = Mathf.Max(1, _camera.pixelWidth);
            var pixelHeight = Mathf.Max(1, _camera.pixelHeight);
            var center = new Vector2(pixelWidth * 0.5f, pixelHeight * 0.5f);
            var ground = new Plane(Vector3.up, Vector3.zero);
            var fromRay = _camera.ScreenPointToRay(center);
            var toRay = _camera.ScreenPointToRay(
                center + new Vector2(horizontal, vertical));
            if (!ground.Raycast(fromRay, out var fromDistance) ||
                !ground.Raycast(toRay, out var toDistance)) return false;
            var delta = toRay.GetPoint(toDistance) - fromRay.GetPoint(fromDistance);
            var prop = _session.Data.Props[SelectedPropIndex];
            var target = ClampPropPosition(
                new Vector2(prop.PositionX + delta.x, prop.PositionZ + delta.z),
                prop.RotationQuarterTurns);
            if (!CanPlacePropAt(target, prop.RotationQuarterTurns, SelectedPropIndex))
                return false;
            prop.PositionX = target.x;
            prop.PositionZ = target.y;
            if (SelectedPropIndex < _propPresentations.Count)
                _propPresentations[SelectedPropIndex].localPosition =
                    new Vector3(target.x, 0.055f, target.y);
            UpdatePropProjectedShadows();
            ApplyPropSelection();
            NotifyStateChanged();
            return true;
        }

        public bool DragPropFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_propDragActive || SelectedPropIndex < 0 ||
                SelectedPropIndex >= (_session.Data.Props?.Count ?? 0) ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            var prop = _session.Data.Props[SelectedPropIndex];
            var target = ClampPropPosition(new Vector2(
                point.x + _propDragOffset.x, point.z + _propDragOffset.y),
                prop.RotationQuarterTurns);
            if (!CanPlacePropAt(target, prop.RotationQuarterTurns, SelectedPropIndex))
                return false;
            prop.PositionX = target.x;
            prop.PositionZ = target.y;
            if (SelectedPropIndex < _propPresentations.Count)
                _propPresentations[SelectedPropIndex].localPosition =
                    new Vector3(target.x, 0.055f, target.y);
            UpdatePropProjectedShadows();
            ApplyPropSelection();
            return true;
        }

        public bool EndPropDrag()
        {
            if (!_propDragActive) return false;
            _propDragActive = false;
            NotifyStateChanged();
            return true;
        }

        public bool RotateSelectedProp(int direction)
        {
            if (SelectedPropIndex < 0 || SelectedPropIndex >= PropCount) return false;
            var prop = _session.Data.Props[SelectedPropIndex];
            var turns = ((prop.RotationQuarterTurns + direction) % 4 + 4) % 4;
            var position = ClampPropPosition(new Vector2(prop.PositionX, prop.PositionZ), turns);
            if (!CanPlacePropAt(position, turns, SelectedPropIndex)) return false;
            prop.RotationQuarterTurns = turns;
            prop.PositionX = position.x;
            prop.PositionZ = position.y;
            RebuildPropPresentations();
            NotifyStateChanged();
            return true;
        }

        public bool DeleteSelectedProp()
        {
            if (SelectedPropIndex < 0 || SelectedPropIndex >= PropCount) return false;
            _session.Data.Props.RemoveAt(SelectedPropIndex);
            SelectedPropIndex = -1;
            RebuildPropPresentations();
            NotifyStateChanged();
            return true;
        }

        public void ClearPropSelectionAndPreview()
        {
            SelectedPropIndex = -1;
            _propDragActive = false;
            _propPreviewId = "";
            _propPreviewHasPoint = false;
            _propPlacementRotationQuarterTurns = 0;
            if (_propPreview != null) _propPreview.gameObject.SetActive(false);
            ApplyPropSelection();
        }

        private void RebuildPropPresentations()
        {
            if (_propRoot == null) return;
            for (var index = _propRoot.childCount - 1; index >= 0; index--)
            {
                var child = _propRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            _propPresentations.Clear();
            _propProjectedShadowRenderers.Clear();
            _propProjectedShadowMaterials.Clear();
            foreach (var prop in _session.Data.Props ?? new List<PlacedProp>())
            {
                var presentation = CreatePropPresentation(prop.PropId,
                    $"Prop — {prop.PropId}", 1f);
                if (presentation == null) continue;
                presentation.SetParent(_propRoot, false);
                presentation.localRotation = Quaternion.Euler(
                    0f, prop.RotationQuarterTurns * 90f, 0f);
                presentation.localPosition = new Vector3(
                    prop.PositionX, 0.055f, prop.PositionZ);
                ApplyFrontPropPresentationPriority(presentation, new Vector3(
                    prop.PositionX, 0f, prop.PositionZ));
                _propPresentations.Add(presentation);
            }
            UpdatePropProjectedShadows();
            ApplyPropSelection();
        }

        private void ApplyFrontPropPresentationPriority(Transform presentation,
            Vector3 logicalPosition)
        {
            if (presentation == null || _camera == null ||
                !IsOnNearestBuildingCameraFacingSide(logicalPosition)) return;

            // Match the already-approved placement-preview path after drop.
            // Its transparent pass draws after the always-visible building
            // artwork and does not let the proxy consume the prop crown.
            foreach (Transform child in presentation)
            {
                if (!child.name.EndsWith(" Model", StringComparison.Ordinal)) continue;
                foreach (var renderer in child.GetComponentsInChildren<Renderer>())
                {
                    var material = renderer.material;
                    material.SetFloat("_Mode", 3f);
                    material.SetInt("_SrcBlend",
                        (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend",
                        (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                }
            }
        }

        private void UpdatePropProjectedShadows()
        {
            var ray = TimeOfDayLighting.SunRotation(TimeOfDay) * Vector3.forward;
            if (_buildingPackage != null)
                ray = Quaternion.Euler(0f,
                    _buildingPackage.ShadowDirectionOffsetDegrees, 0f) * ray;
            var visible = TimeOfDay != TimeOfDayPreset.Night && ray.y < -0.01f;
            var rawDisplacement = new Vector2(ray.x, ray.z) *
                (-1.5f / ray.y) * PropShadowLengthScale(TimeOfDay);
            var displacement = Vector2.ClampMagnitude(rawDisplacement, 8.5f);
            var color = ProjectedShadowColor(TimeOfDay);
            color.a *= PropShadowOpacityMultiplier(TimeOfDay);
            for (var index = 0; index < _propProjectedShadowRenderers.Count; index++)
            {
                var renderer = _propProjectedShadowRenderers[index];
                if (renderer != null) renderer.enabled = visible;
                if (index >= _propProjectedShadowMaterials.Count) continue;
                var material = _propProjectedShadowMaterials[index];
                if (material == null) continue;
                material.SetVector("_ShadowDisplacement",
                    new Vector4(displacement.x, 0f, displacement.y, 0f));
                material.SetFloat("_GroundY", 0.062f);
                material.SetFloat("_ReferenceHeight", 1.5f);
                material.color = color;
            }
        }

        public static float PropShadowLengthScale(TimeOfDayPreset preset) =>
            preset == TimeOfDayPreset.Afternoon ? 1f : ShadowLengthScale(preset);

        public static float PropShadowOpacityMultiplier(TimeOfDayPreset preset) =>
            preset == TimeOfDayPreset.Afternoon ? 1.25f : 0.78f;

        private Transform CreatePropPresentation(string propId, string name, float alpha)
        {
            var resourcePath = string.Equals(propId, FencePropId,
                    StringComparison.OrdinalIgnoreCase)
                ? FenceResourcePath
                : string.Equals(propId, FenceCornerPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? FenceCornerResourcePath
                    : string.Equals(propId, ThreeLanternLamppostPropId,
                        StringComparison.OrdinalIgnoreCase)
                        ? ThreeLanternLamppostResourcePath
                        : "";
            if (string.IsNullOrWhiteSpace(resourcePath))
                return null;
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return null;
            var root = new GameObject(name).transform;
            var model = Instantiate(prefab, root, false);
            model.name = string.Equals(propId, ThreeLanternLamppostPropId,
                StringComparison.OrdinalIgnoreCase)
                ? "Three-Lantern Lamppost Model"
                : "Wrought-Iron Fence Model";
            model.transform.localScale = Vector3.one *
                (string.Equals(propId, ThreeLanternLamppostPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? ThreeLanternLamppostScale
                    : string.Equals(propId, FenceCornerPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? FenceCornerScale
                    : FenceScale);
            SetPropOpacity(root, propId, alpha, true);
            if (alpha >= 0.999f)
            {
                CreatePropDepthPrepass(prefab, root, model.transform.localScale);
                CreateProjectedPropShadow(prefab, root, model.transform.localScale);
                if (string.Equals(propId, ThreeLanternLamppostPropId,
                        StringComparison.OrdinalIgnoreCase))
                    CreateThreeLanternLights(root);
            }
            foreach (var collider in root.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            return root;
        }

        private static void CreatePropDepthPrepass(GameObject prefab,
            Transform root, Vector3 modelScale)
        {
            var shader = Shader.Find("CityForgeV3/BuildingDepthOccluder");
            if (shader == null) throw new MissingReferenceException(
                "CityForge V3 depth-only shader is required for committed props.");
            var depthModel = Instantiate(prefab, root, false);
            depthModel.name = "Committed Prop Depth Prepass";
            depthModel.transform.localScale = modelScale;
            foreach (var collider in depthModel.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            foreach (var renderer in depthModel.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = new Material(shader)
                {
                    name = "CF Committed Prop Depth Prepass",
                    renderQueue = 2435
                };
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void CreateProjectedPropShadow(GameObject prefab, Transform root,
            Vector3 modelScale)
        {
            var shader = Shader.Find("CityForgeV3/ProjectedPropShadow");
            if (shader == null) throw new MissingReferenceException(
                "CityForge V3 projected prop shadow shader is required.");
            var shadowModel = Instantiate(prefab, root, false);
            shadowModel.name = "Projected Prop Silhouette";
            shadowModel.transform.localScale = modelScale;
            foreach (var collider in shadowModel.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            foreach (var renderer in shadowModel.GetComponentsInChildren<Renderer>())
            {
                var material = new Material(shader)
                {
                    name = "CF Projected Prop Silhouette",
                    renderQueue = 2004
                };
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _propProjectedShadowRenderers.Add(renderer);
                _propProjectedShadowMaterials.Add(material);
            }
        }

        private void SetPropOpacity(Transform root, string propId,
            float alpha, bool valid)
        {
            if (root == null) return;
            var lamppost = string.Equals(propId, ThreeLanternLamppostPropId,
                StringComparison.OrdinalIgnoreCase);
            var corner = string.Equals(propId, FenceCornerPropId,
                StringComparison.OrdinalIgnoreCase);
            var texturePrefix = corner ? "corner-" : "";
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                // Meshy FBXs refer to model.fbm aliases that are not present in
                // the supplied archive. Always bind the stable City Forge
                // texture contract instead of inheriting that white fallback.
                var material = new Material(Shader.Find("Standard"))
                {
                    name = lamppost
                        ? "CF Three-Lantern Lamppost Runtime Material"
                        : "CF Wrought-Iron Fence Runtime Material"
                };
                var baseColor = Resources.Load<Texture2D>(
                    lamppost
                        ? "CityForgeV3/Props/ThreeLanternLamppostV01/base-color"
                        : $"CityForgeV3/Props/WroughtIronFenceV01/{texturePrefix}base-color");
                if (baseColor != null) material.mainTexture = baseColor;
                var normal = Resources.Load<Texture2D>(
                    lamppost
                        ? "CityForgeV3/Props/ThreeLanternLamppostV01/normal"
                        : $"CityForgeV3/Props/WroughtIronFenceV01/{texturePrefix}normal");
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.SetFloat("_BumpScale", 0.75f);
                    material.EnableKeyword("_NORMALMAP");
                }
                var metallicSmoothness = Resources.Load<Texture2D>(
                    lamppost
                        ? "CityForgeV3/Props/ThreeLanternLamppostV01/metallic-smoothness"
                        : $"CityForgeV3/Props/WroughtIronFenceV01/{texturePrefix}metallic-smoothness");
                if (metallicSmoothness != null)
                {
                    material.SetTexture("_MetallicGlossMap", metallicSmoothness);
                    material.EnableKeyword("_METALLICGLOSSMAP");
                }
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_GlossMapScale", 0.72f);
                if (lamppost)
                {
                    var emission = Resources.Load<Texture2D>(
                        "CityForgeV3/Props/ThreeLanternLamppostV01/emission");
                    if (emission != null)
                    {
                        material.SetTexture("_EmissionMap", emission);
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor",
                            IsLamppostLightingActive()
                                ? new Color(1.8f, 1.12f, 0.48f)
                                : Color.black);
                    }
                }
                var color = valid ? Color.white : new Color(1f, 0.2f, 0.15f);
                color.a = alpha;
                if (material.HasProperty("_Color")) material.color = color;
                if (alpha < 0.999f)
                {
                    material.SetFloat("_Mode", 3f);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                }
                else
                {
                    // Proxy depth and registered building art draw first.
                    // Committed 3D props then use their real mesh depth, so a
                    // front lamppost survives while a rear one remains hidden.
                    material.renderQueue = 2455;
                }
                renderer.sharedMaterial = material;
                // Committed fence meshes use the controlled projected shadow
                // above. Raw shadow-map projection turns thin pickets into
                // broken, extremely long streaks at low sun elevations.
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = alpha >= 0.999f;
            }
        }

        private bool IsLamppostLightingActive() =>
            TimeOfDay == TimeOfDayPreset.Evening ||
            TimeOfDay == TimeOfDayPreset.Night;

        private void CreateThreeLanternLights(Transform root)
        {
            var active = IsLamppostLightingActive();
            foreach (var spec in new[]
                     {
                         (new Vector3(-0.78f, 3.55f, -0.18f), 2.25f),
                         (new Vector3(0f, 4.05f, -0.18f), 2.7f),
                         (new Vector3(0.78f, 3.55f, -0.18f), 2.25f)
                     })
            {
                var lamp = new GameObject("CF Runtime Lantern Light");
                lamp.transform.SetParent(root, false);
                lamp.transform.localPosition = spec.Item1;
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.62f, 0.28f);
                light.intensity = spec.Item2;
                light.range = 10.5f;
                light.shadows = LightShadows.Soft;
                light.enabled = active;
            }

            var shader = Shader.Find("CityForgeV3/LanternLightPool");
            if (shader == null) throw new MissingReferenceException(
                "City Forge V3 lantern light-pool shader is required.");
            var pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pool.name = "CF Runtime Lantern Light Pool";
            pool.transform.SetParent(root, false);
            pool.transform.localPosition = new Vector3(0f, 0.045f, 0f);
            pool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pool.transform.localScale = new Vector3(
                ThreeLanternLightPoolDiameterMeters,
                ThreeLanternLightPoolDiameterMeters, 1f);
            var collider = pool.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            var renderer = pool.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(shader)
            {
                name = "CF Runtime Lantern Light Pool Material"
            };
            renderer.sharedMaterial.SetColor("_Color",
                new Color(1f, 0.55f, 0.20f, 0.22f));
            renderer.enabled = active;
        }

        private void UpdateThreeLanternLamppostLighting()
        {
            var active = IsLamppostLightingActive();
            foreach (var presentation in _propPresentations)
            {
                if (presentation == null) continue;
                foreach (var light in presentation.GetComponentsInChildren<Light>(true))
                    if (light.name == "CF Runtime Lantern Light") light.enabled = active;
                foreach (var renderer in presentation.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.name == "CF Runtime Lantern Light Pool")
                    {
                        renderer.enabled = active;
                        continue;
                    }
                    var material = renderer.sharedMaterial;
                    if (material == null || material.name.IndexOf(
                            "Three-Lantern Lamppost", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    material.SetColor("_EmissionColor", active
                        ? new Color(1.8f, 1.12f, 0.48f)
                        : Color.black);
                }
            }
        }

        private Vector2 ClampPropPosition(Vector2 position, int turns)
        {
            var propId = SelectedPropIndex >= 0 && SelectedPropIndex < PropCount
                ? _session.Data.Props[SelectedPropIndex].PropId
                : _propPreviewId;
            PropDimensions(propId, turns, out var width, out var depth);
            return new Vector2(
                Mathf.Clamp(position.x, -LotWidthMeters * 0.5f + width * 0.5f,
                    LotWidthMeters * 0.5f - width * 0.5f),
                Mathf.Clamp(position.y, -LotDepthMeters * 0.5f + depth * 0.5f,
                    LotDepthMeters * 0.5f - depth * 0.5f));
        }

        private bool CanPlacePropAt(Vector2 position, int turns, int ignoredIndex)
        {
            var propId = ignoredIndex >= 0 && ignoredIndex < PropCount
                ? _session.Data.Props[ignoredIndex].PropId
                : _propPreviewId;
            PropDimensions(propId, turns, out var width, out var depth);
            foreach (var building in _session.Data.Buildings ?? new List<PlacedBuilding>())
            {
                var package = HybridBuildingPackageRegistry.Load(
                    BuildingCatalog.Find(building.BuildingId).PackageResourcePath);
                var odd = Mathf.Abs(building.RotationQuarterTurns) % 2 == 1;
                var buildingWidth = odd ? package.DepthMeters : package.WidthMeters;
                var buildingDepth = odd ? package.WidthMeters : package.DepthMeters;
                if (Mathf.Abs(position.x - building.CellX) < (width + buildingWidth) * 0.5f &&
                    Mathf.Abs(position.y - building.CellZ) < (depth + buildingDepth) * 0.5f)
                    return false;
            }
            for (var index = 0; index < (_session.Data.Props?.Count ?? 0); index++)
            {
                if (index == ignoredIndex) continue;
                var other = _session.Data.Props[index];
                PropDimensions(other.PropId, other.RotationQuarterTurns,
                    out var otherWidth, out var otherDepth);
                if (Mathf.Abs(position.x - other.PositionX) < (width + otherWidth) * 0.5f - 0.08f &&
                    Mathf.Abs(position.y - other.PositionZ) < (depth + otherDepth) * 0.5f - 0.08f)
                    return false;
            }
            return true;
        }

        private static void PropDimensions(string propId, int turns,
            out float width, out float depth)
        {
            if (string.Equals(propId, ThreeLanternLamppostPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                width = ThreeLanternLamppostFootprintMeters;
                depth = ThreeLanternLamppostFootprintMeters;
                return;
            }
            if (string.Equals(propId, FenceCornerPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                width = FenceCornerLengthMeters;
                depth = FenceCornerLengthMeters;
                return;
            }
            var odd = Mathf.Abs(turns) % 2 == 1;
            width = odd ? FenceDepthMeters : FenceLengthMeters;
            depth = odd ? FenceLengthMeters : FenceDepthMeters;
        }

        private int PropIndexAtCameraPixel(Vector2 pixel)
        {
            var best = -1;
            var bestDepth = float.PositiveInfinity;
            for (var index = 0; index < _propPresentations.Count; index++)
            {
                var root = _propPresentations[index];
                if (root == null) continue;
                if (!TrySelectablePropBounds(root, out var bounds)) continue;
                var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (var corner in BoundsCorners(bounds))
                {
                    var projected = (Vector2)_camera.WorldToScreenPoint(corner);
                    minimum = Vector2.Min(minimum, projected);
                    maximum = Vector2.Max(maximum, projected);
                }
                if (pixel.x < minimum.x || pixel.x > maximum.x ||
                    pixel.y < minimum.y || pixel.y > maximum.y) continue;
                var depth = _camera.WorldToScreenPoint(bounds.center).z;
                if (depth >= bestDepth) continue;
                bestDepth = depth;
                best = index;
            }
            return best;
        }

        public static bool TrySelectablePropBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            if (root == null) return false;
            var found = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer.name == "CF Runtime Lantern Light Pool" ||
                    HasAncestorNamed(renderer.transform, root,
                        "Projected Prop Silhouette")) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static bool HasAncestorNamed(Transform candidate, Transform stop,
            string name)
        {
            for (var current = candidate; current != null && current != stop;
                 current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
        {
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
                yield return bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3(x, y, z));
        }

        private void ApplyPropSelection()
        {
            if (_propSelection == null) return;
            var visible = _propEditorActive &&
                ActiveObjectSelection == LotObjectSelectionKind.Prop &&
                SelectedPropIndex >= 0 &&
                SelectedPropIndex < PropCount;
            _propSelection.gameObject.SetActive(visible);
            if (!visible) return;
            var prop = _session.Data.Props[SelectedPropIndex];
            PropDimensions(prop.PropId, prop.RotationQuarterTurns,
                out var selectionWidth, out var selectionDepth);
            _propSelection.localPosition = new Vector3(prop.PositionX, 0.07f, prop.PositionZ);
            _propSelection.localRotation = Quaternion.Euler(
                0f, prop.RotationQuarterTurns * 90f, 0f);
            // The selection transform supplies rotation, so scale from the
            // unrotated footprint to avoid swapping dimensions twice.
            PropDimensions(prop.PropId, 0, out var baseWidth, out var baseDepth);
            _propSelection.localScale = new Vector3(
                baseWidth / FenceLengthMeters, 1f,
                baseDepth / FenceDepthMeters);
        }
    }
}
