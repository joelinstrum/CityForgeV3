using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        // A single opt-in sawmill reflection is rendered at most five times a
        // second. The spatial index bounds selection to the current close-up
        // camera area; no district-wide scan runs while panning or rendering.
        public const DistrictZoomLevel RiverBuildingReflectionFarthestZoom =
            DistrictZoomLevel.LOD2;
        public const int RiverBuildingReflectionTextureHeight = 256;
        public const float RiverBuildingReflectionMinimumInterval = .2f;
        private const int RiverBuildingReflectionLayer = 30;
        private const string RiverBuildingReflectionAssetId = "lumber-mill-v01";
        private const float RiverBuildingReflectionRadiusMeters = 40f;
        private static readonly int RiverReflectionEnabledId =
            Shader.PropertyToID("_CF_RiverBuildingReflectionEnabled");
        private static readonly int RiverReflectionTextureId =
            Shader.PropertyToID("_CF_RiverBuildingReflectionTex");
        private static readonly int RiverReflectionMatrixId =
            Shader.PropertyToID("_CF_RiverBuildingReflectionVP");
        private static readonly int RiverReflectionCenterId =
            Shader.PropertyToID("_CF_RiverBuildingReflectionCenter");

        private sealed class RiverReflectionCandidate
        {
            public Transform Root;
            public Renderer[] Renderers;
            public Rect IndexBounds;
        }

        private readonly DistrictSpatialIndex<RiverReflectionCandidate>
            _riverBuildingReflectionIndex = new(64f);
        private readonly Dictionary<LotWorldController, List<RiverReflectionCandidate>>
            _riverBuildingReflectionByLot = new();
        private readonly HashSet<RiverReflectionCandidate>
            _riverBuildingReflectionNearby = new();
        private readonly List<Transform> _riverBuildingReflectionRoots = new();
        private readonly List<(GameObject Node, int Layer)>
            _riverBuildingReflectionOriginalLayers = new();
        private RiverReflectionCandidate _activeRiverBuildingReflection;
        private Camera _riverBuildingReflectionCamera;
        private RenderTexture _riverBuildingReflectionTexture;
        private float _riverBuildingReflectionLastRender = float.NegativeInfinity;
        private int _riverBuildingReflectionTextureWidth;
        private bool _riverBuildingReflectionsEnabled = true;
        public bool RiverBuildingReflectionsEnabled
        {
            get => _riverBuildingReflectionsEnabled;
            set
            {
                if (_riverBuildingReflectionsEnabled == value) return;
                _riverBuildingReflectionsEnabled = value;
                if (!value) DisableRiverBuildingReflection();
                else _riverBuildingReflectionLastRender = float.NegativeInfinity;
            }
        }
        public int RiverBuildingReflectionRenderCountForQa { get; private set; }
        public int RiverBuildingReflectionCandidateCountForQa { get; private set; }
        public bool RiverBuildingReflectionActiveForQa =>
            _activeRiverBuildingReflection != null &&
            _riverBuildingReflectionTexture != null;

        public static bool AllowsRiverBuildingReflection(DistrictZoomLevel zoom)
            => zoom <= RiverBuildingReflectionFarthestZoom;

        private void RegisterRiverBuildingReflectionCandidates(
            LotWorldController lot)
        {
            if (lot == null) return;
            _riverBuildingReflectionRoots.Clear();
            lot.CollectNativeBuildingRoots(RiverBuildingReflectionAssetId,
                _riverBuildingReflectionRoots);
            if (_riverBuildingReflectionRoots.Count == 0) return;
            var candidates = new List<RiverReflectionCandidate>(
                _riverBuildingReflectionRoots.Count);
            foreach (var root in _riverBuildingReflectionRoots)
            {
                var candidate = new RiverReflectionCandidate
                {
                    Root = root,
                    Renderers = root.GetComponentsInChildren<Renderer>(true),
                    IndexBounds = ReflectionIndexBounds(root.position)
                };
                _riverBuildingReflectionIndex.Add(candidate.IndexBounds,
                    candidate);
                candidates.Add(candidate);
                RiverBuildingReflectionCandidateCountForQa++;
            }
            _riverBuildingReflectionByLot[lot] = candidates;
            _riverBuildingReflectionLastRender = float.NegativeInfinity;
        }

        private static Rect ReflectionIndexBounds(Vector3 world) =>
            new(world.x - 1f, world.z - 1f, 2f, 2f);

        private void UpdateRiverBuildingReflectionCandidates(LotWorldController lot)
        {
            if (!_riverBuildingReflectionByLot.TryGetValue(lot,
                    out var candidates)) return;
            foreach (var candidate in candidates)
            {
                _riverBuildingReflectionIndex.Remove(candidate.IndexBounds,
                    candidate);
                if (candidate.Root == null) continue;
                candidate.IndexBounds = ReflectionIndexBounds(
                    candidate.Root.position);
                _riverBuildingReflectionIndex.Add(candidate.IndexBounds,
                    candidate);
            }
            _riverBuildingReflectionLastRender = float.NegativeInfinity;
        }

        private void UnregisterRiverBuildingReflectionCandidates(
            LotWorldController lot)
        {
            if (!_riverBuildingReflectionByLot.TryGetValue(lot,
                    out var candidates)) return;
            foreach (var candidate in candidates)
            {
                if (candidate == _activeRiverBuildingReflection)
                    DisableRiverBuildingReflection();
                _riverBuildingReflectionIndex.Remove(candidate.IndexBounds,
                    candidate);
                RiverBuildingReflectionCandidateCountForQa--;
            }
            _riverBuildingReflectionByLot.Remove(lot);
        }

        private void InvalidateRiverBuildingReflection()
        {
            DeactivateRiverBuildingReflection();
            _riverBuildingReflectionLastRender = float.NegativeInfinity;
        }

        private void SetRiverBuildingReflectionZoom(DistrictZoomLevel zoom)
        {
            if (!AllowsRiverBuildingReflection(zoom))
                DisableRiverBuildingReflection();
            else if (zoom != _zoomLevel)
                _riverBuildingReflectionLastRender = float.NegativeInfinity;
        }

        private void OnRiverBuildingReflectionCameraPreRender(Camera camera)
        {
            if (camera != _camera) return;
            if (!isActiveAndEnabled || !_riverBuildingReflectionsEnabled ||
                !AllowsRiverBuildingReflection(_zoomLevel) ||
                _riverBuildingReflectionByLot.Count == 0 ||
                _riverSurfaces.Count == 0)
            {
                DisableRiverBuildingReflection();
                return;
            }
            var now = Time.realtimeSinceStartup;
            if (now - _riverBuildingReflectionLastRender <
                RiverBuildingReflectionMinimumInterval &&
                _activeRiverBuildingReflection != null)
            {
                PublishRiverBuildingReflection();
                return;
            }
            var candidate = FindVisibleRiverBuildingReflectionCandidate(
                out var waterElevation);
            if (candidate == null)
            {
                DisableRiverBuildingReflection();
                return;
            }
            if (candidate != _activeRiverBuildingReflection)
            {
                DeactivateRiverBuildingReflection();
                _activeRiverBuildingReflection = candidate;
                foreach (var renderer in candidate.Renderers)
                {
                    if (renderer == null) continue;
                    var node = renderer.gameObject;
                    _riverBuildingReflectionOriginalLayers.Add(
                        (node, node.layer));
                    node.layer = RiverBuildingReflectionLayer;
                }
            }
            RenderRiverBuildingReflection(waterElevation);
            _riverBuildingReflectionLastRender = now;
            PublishRiverBuildingReflection();
        }

        private RiverReflectionCandidate
            FindVisibleRiverBuildingReflectionCandidate(out float waterElevation)
        {
            waterElevation = 0f;
            var radius = Mathf.Max(60f, _camera.orthographicSize * 3f);
            _riverBuildingReflectionIndex.QueryBounds(
                new Rect(_pan.x - radius, _pan.z - radius,
                    radius * 2f, radius * 2f),
                _riverBuildingReflectionNearby);
            RiverReflectionCandidate best = null;
            var bestDistance = float.PositiveInfinity;
            foreach (var candidate in _riverBuildingReflectionNearby)
            {
                if (candidate.Root == null ||
                    !candidate.Root.gameObject.activeInHierarchy) continue;
                var center = candidate.Root.position;
                var screen = _camera.WorldToViewportPoint(center + Vector3.up * 4f);
                if (screen.z <= 0f || screen.x < -.15f || screen.x > 1.15f ||
                    screen.y < -.15f || screen.y > 1.15f) continue;
                var distance = (new Vector2(center.x - _pan.x,
                    center.z - _pan.z)).sqrMagnitude;
                if (distance >= bestDistance ||
                    !TryRiverWaterAtReflectionCandidate(candidate,
                        out var water)) continue;
                best = candidate;
                bestDistance = distance;
                waterElevation = water;
            }
            return best;
        }

        private bool TryRiverWaterAtReflectionCandidate(
            RiverReflectionCandidate candidate, out float elevation)
        {
            elevation = 0f;
            var center = candidate.Root.position;
            var bounds = default(Bounds);
            var hasBounds = false;
            foreach (var renderer in candidate.Renderers)
            {
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!hasBounds) return false;
            // Leave room beyond a dry foundation for a waterwheel or mill
            // deck that reaches over the bank without putting the lot pivot
            // itself under water.
            var halfX = Mathf.Min(24f, bounds.extents.x + 6f);
            var halfZ = Mathf.Min(24f, bounds.extents.z + 6f);
            // The mill's wheel can reach the river while its pivot remains on
            // dry land. Sample the local footprint, not the entire district.
            for (var x = -1; x <= 1; x++)
            for (var z = -1; z <= 1; z++)
            {
                var point = new Vector3(
                    center.x + x * halfX, center.y,
                    center.z + z * halfZ);
                var sample = SampleRiverSurface(point);
                if (sample?.UnderWater != true) continue;
                point.y = sample.Value.WaterElevation;
                var screen = _camera.WorldToViewportPoint(point);
                if (screen.z <= 0f || screen.x < -.1f || screen.x > 1.1f ||
                    screen.y < -.1f || screen.y > 1.1f) continue;
                elevation = sample.Value.WaterElevation;
                return true;
            }
            return false;
        }

        private void RenderRiverBuildingReflection(float waterElevation)
        {
            if (_riverBuildingReflectionCamera == null)
            {
                var node = new GameObject("Sawmill river reflection camera");
                node.transform.SetParent(transform, false);
                _riverBuildingReflectionCamera = node.AddComponent<Camera>();
            }
            var width = Mathf.Clamp(Mathf.RoundToInt(
                RiverBuildingReflectionTextureHeight * _camera.aspect / 16f)
                * 16, 256, 512);
            if (_riverBuildingReflectionTexture == null ||
                width != _riverBuildingReflectionTextureWidth)
            {
                ReleaseRiverBuildingReflectionTexture();
                _riverBuildingReflectionTextureWidth = width;
                _riverBuildingReflectionTexture = new RenderTexture(width,
                    RiverBuildingReflectionTextureHeight, 16,
                    RenderTextureFormat.ARGB32)
                {
                    name = "Sawmill river reflection (close zoom only)",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                _riverBuildingReflectionTexture.Create();
            }
            var mirror = _riverBuildingReflectionCamera;
            mirror.CopyFrom(_camera);
            mirror.enabled = false;
            mirror.targetTexture = _riverBuildingReflectionTexture;
            mirror.clearFlags = CameraClearFlags.SolidColor;
            mirror.backgroundColor = Color.clear;
            mirror.cullingMask = 1 << RiverBuildingReflectionLayer;
            mirror.useOcclusionCulling = false;
            mirror.depthTextureMode = DepthTextureMode.None;
            mirror.aspect = _camera.aspect;
            var position = _camera.transform.position;
            position.y = 2f * waterElevation - position.y;
            var forward = _camera.transform.forward;
            forward.y = -forward.y;
            var up = _camera.transform.up;
            up.y = -up.y;
            mirror.transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(forward, up));
            var previousCulling = GL.invertCulling;
            try
            {
                GL.invertCulling = !previousCulling;
                mirror.Render();
                RiverBuildingReflectionRenderCountForQa++;
            }
            finally
            {
                GL.invertCulling = previousCulling;
            }
            var projection = GL.GetGPUProjectionMatrix(
                mirror.projectionMatrix, true);
            Shader.SetGlobalMatrix(RiverReflectionMatrixId,
                projection * mirror.worldToCameraMatrix);
        }

        private void PublishRiverBuildingReflection()
        {
            if (_activeRiverBuildingReflection?.Root == null ||
                _riverBuildingReflectionTexture == null)
            {
                Shader.SetGlobalFloat(RiverReflectionEnabledId, 0f);
                return;
            }
            var center = _activeRiverBuildingReflection.Root.position;
            Shader.SetGlobalTexture(RiverReflectionTextureId,
                _riverBuildingReflectionTexture);
            Shader.SetGlobalVector(RiverReflectionCenterId,
                new Vector4(center.x, center.z,
                    RiverBuildingReflectionRadiusMeters, 0f));
            Shader.SetGlobalFloat(RiverReflectionEnabledId, 1f);
        }

        private void DeactivateRiverBuildingReflection()
        {
            foreach (var original in _riverBuildingReflectionOriginalLayers)
                if (original.Node != null)
                    original.Node.layer = original.Layer;
            _riverBuildingReflectionOriginalLayers.Clear();
            _activeRiverBuildingReflection = null;
        }

        private void DisableRiverBuildingReflection()
        {
            Shader.SetGlobalFloat(RiverReflectionEnabledId, 0f);
            DeactivateRiverBuildingReflection();
        }

        private void ReleaseRiverBuildingReflectionTexture()
        {
            if (_riverBuildingReflectionTexture == null) return;
            _riverBuildingReflectionTexture.Release();
            if (Application.isPlaying) Destroy(_riverBuildingReflectionTexture);
            else DestroyImmediate(_riverBuildingReflectionTexture);
            _riverBuildingReflectionTexture = null;
        }

        private void DisposeRiverBuildingReflection()
        {
            DisableRiverBuildingReflection();
            ReleaseRiverBuildingReflectionTexture();
            Shader.SetGlobalTexture(RiverReflectionTextureId,
                Texture2D.blackTexture);
            if (_riverBuildingReflectionCamera != null)
            {
                var node = _riverBuildingReflectionCamera.gameObject;
                node.SetActive(false);
                node.transform.SetParent(null, false);
                if (Application.isPlaying) Destroy(node);
                else DestroyImmediate(node);
                _riverBuildingReflectionCamera = null;
            }
            _riverBuildingReflectionByLot.Clear();
            _riverBuildingReflectionIndex.Clear();
            _riverBuildingReflectionNearby.Clear();
            _riverBuildingReflectionRoots.Clear();
            RiverBuildingReflectionCandidateCountForQa = 0;
            _riverBuildingReflectionLastRender = float.NegativeInfinity;
        }
    }
}
