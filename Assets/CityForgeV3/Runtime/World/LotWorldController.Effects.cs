using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        public const string WindowLightEffectId = "window-light-v01";
        private Transform _effectRoot;
        private readonly List<GameObject> _effectPresentations = new();
        private GameObject _effectPreview;
        private bool _effectPlacementActive;
        private float _windowLightPlacementScale = 1f;

        private const string EffectHostBuilding3D = "building3d";
        private const string EffectHostBuilding = "building";
        private const string EffectHostProp = "prop";

        public int EffectCount => _session?.Data?.Effects?.Count ?? 0;
        public bool LargeWindowLightPlacement =>
            _windowLightPlacementScale > 1.5f;

        public void ToggleWindowLightPlacementSize()
        {
            _windowLightPlacementScale = LargeWindowLightPlacement ? 1f : 2f;
            ApplyWindowLightPanelScale(_effectPreview,
                _windowLightPlacementScale);
        }

        public void SetEffectPlacementPreview(string effectId)
        {
            _effectPlacementActive = string.Equals(effectId,
                WindowLightEffectId, StringComparison.OrdinalIgnoreCase);
            if (_effectPreview != null) DestroyForCurrentMode(_effectPreview);
            _effectPreview = _effectPlacementActive
                ? CreateWindowLightPresentation("Window Light Preview", true,
                    _windowLightPlacementScale)
                : null;
            if (_effectPreview != null)
            {
                EnsureEffectRoot();
                _effectPreview.transform.SetParent(_effectRoot, true);
                _effectPreview.SetActive(false);
            }
        }

        public bool UpdateEffectPreviewFromPanel(Vector2 panelPosition,
            Vector2 panelSize)
        {
            if (!_effectPlacementActive || _effectPreview == null ||
                !TryEffectSurfaceFromPanel(panelPosition, panelSize,
                    out var point, out var normal, out _, out _, out _))
            {
                if (_effectPreview != null) _effectPreview.SetActive(false);
                return false;
            }
            PositionWindowLight(_effectPreview.transform, point, normal);
            _effectPreview.SetActive(true);
            return true;
        }

        public bool PlaceWindowLightFromPanel(Vector2 panelPosition,
            Vector2 panelSize)
        {
            if (!_effectPlacementActive ||
                !TryEffectSurfaceFromPanel(panelPosition, panelSize,
                    out var point, out var normal, out var host,
                    out var hostKind, out var hostInstanceId)) return false;
            var localPoint = host != null
                ? host.InverseTransformPoint(point)
                : HostLocalPoint(hostKind, hostInstanceId, point);
            var localNormal = host != null
                ? host.InverseTransformDirection(normal).normalized
                : HostLocalDirection(hostKind, hostInstanceId, normal);
            _session.Data.Effects ??= new List<PlacedEffect>();
            _session.Data.Effects.Add(new PlacedEffect
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                EffectId = WindowLightEffectId,
                HostKind = hostKind,
                HostInstanceId = hostInstanceId,
                HasHostAttachment = !string.IsNullOrWhiteSpace(hostInstanceId),
                HostLocalX = localPoint.x,
                HostLocalY = localPoint.y,
                HostLocalZ = localPoint.z,
                HostNormalX = localNormal.x,
                HostNormalY = localNormal.y,
                HostNormalZ = localNormal.z,
                Scale = _windowLightPlacementScale,
                PositionX = point.x,
                PositionY = point.y,
                PositionZ = point.z,
                NormalX = normal.x,
                NormalY = normal.y,
                NormalZ = normal.z
            });
            RebuildEffectPresentations();
            NotifyStateChanged();
            return true;
        }

        private bool TryEffectSurfaceFromPanel(Vector2 panelPosition,
            Vector2 panelSize, out Vector3 point, out Vector3 normal,
            out Transform host, out string hostKind,
            out string hostInstanceId)
        {
            point = default;
            normal = Vector3.forward;
            host = null;
            hostKind = "";
            hostInstanceId = "";
            if (_camera == null) return false;
            var pixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            var ray = _camera.ScreenPointToRay(pixel);
            var bestDistance = float.PositiveInfinity;
            var found = false;
            foreach (var surface in EffectSurfaceRoots())
            {
                var root = surface.Root;
                if (root == null) continue;
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer = filter.GetComponent<Renderer>();
                    if (filter.sharedMesh == null || renderer == null ||
                        !renderer.enabled || IsPackageShadowRenderer(renderer))
                        continue;
                    var collider = filter.GetComponent<MeshCollider>();
                    if (collider == null)
                        collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                    collider.convex = false;
                    collider.enabled = true;
                    if (!collider.Raycast(ray, out var hit,
                            _camera.farClipPlane) || hit.distance >= bestDistance)
                        continue;
                    bestDistance = hit.distance;
                    point = hit.point;
                    normal = hit.normal.normalized;
                    host = root.transform;
                    hostKind = surface.Kind;
                    hostInstanceId = surface.InstanceId;
                    found = true;
                }
            }
            if (found) return true;

            // Hybrid CityForge buildings are camera-facing presentations with
            // semantic massing rather than imported visible meshes. Reuse the
            // proven building-attachment facade resolver so Window Light can
            // be placed on those buildings too.
            if (!TryBuildingAttachmentPoint(panelPosition, panelSize,
                    out var buildingIndex, out var normalizedX,
                    out var normalizedY)) return false;
            var attachment = new PlacedBuildingProp
            {
                ProjectionDepthMeters = 0.035f
            };
            if (!InitializeHostLocalPosition(attachment, buildingIndex,
                    normalizedX, normalizedY)) return false;
            var building = _session.Data.Buildings[buildingIndex];
            var rotation = Quaternion.Euler(
                0f, building.RotationQuarterTurns * 90f, 0f);
            var local = new Vector3(attachment.HostLocalX,
                attachment.HostLocalY, attachment.HostLocalZ);
            point = new Vector3(building.CellX, 0f, building.CellZ) +
                rotation * local;
            var axisNormal = Mathf.Abs(local.x) > Mathf.Abs(local.z)
                ? new Vector3(Mathf.Sign(local.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(local.z));
            normal = rotation * axisNormal;
            hostKind = EffectHostBuilding;
            hostInstanceId = building.InstanceId;
            return true;
        }

        private IEnumerable<(GameObject Root, string Kind, string InstanceId)>
            EffectSurfaceRoots()
        {
            for (var index = 0; index < _experimentalBuilding3DVisibleRoots.Count;
                 index++)
            {
                var root = _experimentalBuilding3DVisibleRoots[index];
                if (root == null || index >= (_session.Data.Buildings3D?.Count ?? 0))
                    continue;
                var placed = _session.Data.Buildings3D[index];
                if (string.IsNullOrWhiteSpace(placed.InstanceId))
                    placed.InstanceId = Guid.NewGuid().ToString("N");
                yield return (root, EffectHostBuilding3D, placed.InstanceId);
            }
            for (var index = 0; index < _propPresentations.Count; index++)
            {
                var presentation = _propPresentations[index];
                if (presentation == null ||
                    index >= (_session.Data.Props?.Count ?? 0)) continue;
                yield return (presentation.gameObject, EffectHostProp,
                    _session.Data.Props[index].InstanceId);
            }
        }

        private Vector3 HostLocalPoint(string hostKind, string hostInstanceId,
            Vector3 worldPoint)
        {
            if (TryResolveEffectHostTransform(hostKind, hostInstanceId,
                    out var host)) return host.InverseTransformPoint(worldPoint);
            if (string.Equals(hostKind, EffectHostBuilding,
                    StringComparison.OrdinalIgnoreCase))
            {
                var building = FindHybridEffectHost(hostInstanceId);
                if (building != null)
                {
                    var rotation = Quaternion.Euler(0f,
                        building.RotationQuarterTurns * 90f, 0f);
                    return Quaternion.Inverse(rotation) * (worldPoint -
                        new Vector3(building.CellX, 0f, building.CellZ));
                }
            }
            return worldPoint;
        }

        private Vector3 HostLocalDirection(string hostKind,
            string hostInstanceId, Vector3 worldDirection)
        {
            if (TryResolveEffectHostTransform(hostKind, hostInstanceId,
                    out var host))
                return host.InverseTransformDirection(worldDirection).normalized;
            if (string.Equals(hostKind, EffectHostBuilding,
                    StringComparison.OrdinalIgnoreCase))
            {
                var building = FindHybridEffectHost(hostInstanceId);
                if (building != null)
                    return Quaternion.Inverse(Quaternion.Euler(0f,
                        building.RotationQuarterTurns * 90f, 0f)) *
                        worldDirection.normalized;
            }
            return worldDirection.normalized;
        }

        private void ResolveEffectWorldPose(PlacedEffect effect,
            out Vector3 point, out Vector3 normal)
        {
            point = new Vector3(effect.PositionX, effect.PositionY,
                effect.PositionZ);
            normal = new Vector3(effect.NormalX, effect.NormalY,
                effect.NormalZ).normalized;
            if (!effect.HasHostAttachment) return;
            var localPoint = new Vector3(effect.HostLocalX,
                effect.HostLocalY, effect.HostLocalZ);
            var localNormal = new Vector3(effect.HostNormalX,
                effect.HostNormalY, effect.HostNormalZ).normalized;
            if (TryResolveEffectHostTransform(effect.HostKind,
                    effect.HostInstanceId, out var host))
            {
                point = host.TransformPoint(localPoint);
                normal = host.TransformDirection(localNormal).normalized;
                return;
            }
            if (!string.Equals(effect.HostKind, EffectHostBuilding,
                    StringComparison.OrdinalIgnoreCase)) return;
            var building = FindHybridEffectHost(effect.HostInstanceId);
            if (building == null) return;
            var rotation = Quaternion.Euler(0f,
                building.RotationQuarterTurns * 90f, 0f);
            point = new Vector3(building.CellX, 0f, building.CellZ) +
                rotation * localPoint;
            normal = rotation * localNormal;
        }

        private bool TryResolveEffectHostTransform(string hostKind,
            string hostInstanceId, out Transform host)
        {
            host = null;
            if (string.IsNullOrWhiteSpace(hostInstanceId)) return false;
            if (string.Equals(hostKind, EffectHostBuilding3D,
                    StringComparison.OrdinalIgnoreCase))
            {
                for (var index = 0;
                     index < (_session.Data.Buildings3D?.Count ?? 0) &&
                     index < _experimentalBuilding3DVisibleRoots.Count; index++)
                {
                    if (_session.Data.Buildings3D[index]?.InstanceId !=
                        hostInstanceId) continue;
                    var root = _experimentalBuilding3DVisibleRoots[index];
                    if (root == null) return false;
                    host = root.transform;
                    return true;
                }
            }
            if (!string.Equals(hostKind, EffectHostProp,
                    StringComparison.OrdinalIgnoreCase)) return false;
            for (var index = 0; index < (_session.Data.Props?.Count ?? 0) &&
                 index < _propPresentations.Count; index++)
            {
                if (_session.Data.Props[index]?.InstanceId != hostInstanceId)
                    continue;
                host = _propPresentations[index];
                return host != null;
            }
            return false;
        }

        private PlacedBuilding FindHybridEffectHost(string instanceId)
        {
            foreach (var building in _session.Data.Buildings ??
                     new List<PlacedBuilding>())
                if (building != null && building.InstanceId == instanceId)
                    return building;
            return null;
        }

        private void UpdateEffectAttachmentTransforms()
        {
            var presentationIndex = 0;
            foreach (var effect in _session?.Data?.Effects ??
                     new List<PlacedEffect>())
            {
                if (effect == null || !string.Equals(effect.EffectId,
                        WindowLightEffectId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (presentationIndex >= _effectPresentations.Count) break;
                var presentation = _effectPresentations[presentationIndex++];
                if (presentation == null) continue;
                ResolveEffectWorldPose(effect, out var point, out var normal);
                PositionWindowLight(presentation.transform, point, normal);
            }
        }

        private void EnsureEffectRoot()
        {
            if (_effectRoot != null) return;
            _effectRoot = new GameObject("Lot Effects").transform;
            _effectRoot.SetParent(transform, false);
        }

        private void RebuildEffectPresentations()
        {
            EnsureEffectRoot();
            foreach (var presentation in _effectPresentations)
                if (presentation != null) DestroyForCurrentMode(presentation);
            _effectPresentations.Clear();
            foreach (var effect in _session?.Data?.Effects ??
                     new List<PlacedEffect>())
            {
                if (effect == null || !string.Equals(effect.EffectId,
                        WindowLightEffectId, StringComparison.OrdinalIgnoreCase))
                    continue;
                var presentation = CreateWindowLightPresentation(
                    "Effect — Window Light", false,
                    effect.Scale <= 0f ? 1f : effect.Scale);
                presentation.transform.SetParent(_effectRoot, true);
                ResolveEffectWorldPose(effect, out var point, out var normal);
                PositionWindowLight(presentation.transform, point, normal);
                _effectPresentations.Add(presentation);
            }
            UpdateWindowEffectLighting();
        }

        private static void PositionWindowLight(Transform target,
            Vector3 point, Vector3 normal)
        {
            if (normal.sqrMagnitude < 0.01f) normal = Vector3.forward;
            // Window effects remain vertical even when noisy mesh normals
            // contain a small roofward/downward component.
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.01f) normal = Vector3.forward;
            normal.Normalize();
            target.SetPositionAndRotation(point + normal * 0.035f,
                Quaternion.LookRotation(normal, Vector3.up));
        }

        private static GameObject CreateWindowLightPresentation(string name,
            bool preview, float scale)
        {
            var root = new GameObject(name);
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad);
            square.name = "Window Light Square";
            square.transform.SetParent(root.transform, false);
            // Unity's generated Quad presents its front face along local -Z.
            // The effect root's +Z points away from the facade, so turn only
            // the panel around while leaving the light aimed outward.
            square.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            // A typical tall sash-window proportion: one meter wide and two
            // meters high in world space.
            square.transform.localScale =
                new Vector3(scale, 2f * scale, 1f);
            var collider = square.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var glowShader = Shader.Find("CityForgeV3/WindowLightGlow") ??
                Shader.Find("Standard");
            var material = new Material(glowShader)
            {
                name = preview
                    ? "CF Window Light Preview"
                    : "CF Window Light Emissive"
            };
            material.color = preview
                ? new Color(0.25f, 0.86f, 1f, 0.55f)
                : new Color(1f, 0.43f, 0.12f, 0f);
            if (material.HasProperty("_EmissionStrength"))
                material.SetFloat("_EmissionStrength", preview ? 0.75f : 0f);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);
            square.GetComponent<Renderer>().sharedMaterial = material;
            square.GetComponent<Renderer>().shadowCastingMode =
                ShadowCastingMode.Off;
            if (!preview)
            {
                var lightObject = new GameObject("CF Window Light Spot");
                lightObject.transform.SetParent(root.transform, false);
                lightObject.transform.localPosition =
                    new Vector3(0f, -0.08f, 0.08f);
                // A facade-normal spotlight fires into empty air. Pitch it
                // downward so the pool becomes visible on pavement and the
                // ground immediately outside the window.
                lightObject.transform.localRotation =
                    Quaternion.Euler(28f, 0f, 0f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = new Color(1f, 0.45f, 0.14f);
                light.range = 8f;
                light.spotAngle = 72f;
                light.innerSpotAngle = 42f;
                light.intensity = 3.0f;
                light.shadows = LightShadows.None;
                light.bounceIntensity = 0f;

                // A small omnidirectional fill makes adjacent trim and facade
                // surfaces react to the window, while the spot above creates
                // the more legible pool on the ground.
                var fillObject = new GameObject("CF Window Light Fill");
                fillObject.transform.SetParent(root.transform, false);
                fillObject.transform.localPosition =
                    new Vector3(0f, 0f, 0.22f);
                var fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Point;
                fill.color = new Color(1f, 0.40f, 0.10f);
                fill.range = 4.5f;
                fill.intensity = 1.3f;
                fill.shadows = LightShadows.None;
                fill.bounceIntensity = 0f;
            }
            return root;
        }

        private static void ApplyWindowLightPanelScale(GameObject presentation,
            float scale)
        {
            if (presentation == null) return;
            var square = presentation.transform.Find("Window Light Square");
            if (square != null)
                square.localScale = new Vector3(scale, 2f * scale, 1f);
        }

        private void UpdateWindowEffectLighting()
        {
            var active = TimeOfDay is TimeOfDayPreset.Evening or
                TimeOfDayPreset.Night;
            foreach (var presentation in _effectPresentations)
            {
                if (presentation == null) continue;
                foreach (var light in
                         presentation.GetComponentsInChildren<Light>(true))
                    light.enabled = active;
                var renderer = presentation.GetComponentInChildren<Renderer>();
                if (renderer == null || renderer.sharedMaterial == null) continue;
                var material = renderer.sharedMaterial;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", active
                        ? new Color(2.5f, 1.25f, 0.42f)
                        : Color.black);
                    if (active) material.EnableKeyword("_EMISSION");
                    else material.DisableKeyword("_EMISSION");
                }
                if (material.HasProperty("_EmissionStrength"))
                    material.SetFloat("_EmissionStrength", active ? 1.65f : 0f);
                material.color = active
                    ? new Color(1f, 0.43f, 0.12f, 0.42f)
                    : new Color(0.18f, 0.13f, 0.07f, 0f);
            }
        }
    }
}
