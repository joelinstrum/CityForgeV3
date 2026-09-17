using UnityEngine;

namespace CityForgeV3.World
{
    // Transient presentation only. Two fixed draws; no scene scans, particles or save mutations.
    public sealed class DistrictRainStorm : MonoBehaviour
    {
        public const float BuildSeconds = 5, RainSeconds = 10, ClearSeconds = 4;
        public enum Phase { Clear, Gathering, Raining, Clearing, Snowing, Settled, Melting }
        public const float SnowFallSeconds = 10, SnowHoldSeconds = 10, SnowMeltSeconds = 5;
        public float SnowAccumulation { get; private set; }
        public static Vector2 EvaluateSnow(float snowElapsed)
        {
            if (snowElapsed < 0 || snowElapsed >= 25) return Vector2.zero;
            if (snowElapsed < SnowFallSeconds)
                return new Vector2(Mathf.SmoothStep(0,1,snowElapsed/.7f)*Mathf.SmoothStep(0,1,(10-snowElapsed)/.7f),
                    Mathf.SmoothStep(0,1,snowElapsed/SnowFallSeconds));
            if (snowElapsed < SnowFallSeconds + SnowHoldSeconds) return new Vector2(0,1);
            return new Vector2(0,1-Mathf.SmoothStep(0,1,(snowElapsed-20)/SnowMeltSeconds));
        }
        public Phase CurrentPhase { get; private set; }
        public float Coverage { get; private set; }
        public float RainIntensity { get; private set; }
        public float MistIntensity { get; private set; }
        public static float EvaluateMist(float elapsed)
        {
            if (elapsed < BuildSeconds) return 0;
            if (elapsed < BuildSeconds + RainSeconds)
                return Mathf.SmoothStep(0, 1, (elapsed - BuildSeconds) / 2f);
            return 1 - Mathf.SmoothStep(0, 1, (elapsed - BuildSeconds - RainSeconds) / ClearSeconds);
        }
        public static Vector2 Evaluate(float elapsed)
        {
            if (elapsed < 0 || elapsed >= BuildSeconds + RainSeconds + ClearSeconds) return Vector2.zero;
            if (elapsed < BuildSeconds) return new Vector2(Mathf.SmoothStep(0, 1, elapsed / BuildSeconds), 0);
            if (elapsed < BuildSeconds + RainSeconds)
                return new Vector2(1, Mathf.SmoothStep(0, 1, (elapsed - BuildSeconds) / .7f) *
                    Mathf.SmoothStep(0, 1, (BuildSeconds + RainSeconds - elapsed) / .7f));
            return new Vector2(1 - Mathf.SmoothStep(0, 1, (elapsed - BuildSeconds - RainSeconds) / ClearSeconds), 0);
        }
        Camera _camera;
        Material _deck, _rain, _snowMaterial;
        MeshFilter _terrain, _snowMesh;
        MeshRenderer _snowRenderer;
        bool _snowMode;
        Mesh _mesh;
        MeshRenderer _deckRenderer, _rainRenderer;
        DistrictCloudLayer _clouds;
        double _started;
        bool _running;
        DistrictZoomLevel _zoom;
        public void Initialize(Camera camera, float width, float depth, float height, DistrictCloudLayer clouds, MeshFilter terrain = null)
        {
            _camera = camera; _clouds = clouds;
            var shader = Shader.Find("CityForgeV3/DistrictStorm");
            _mesh = new Mesh { name = "District weather screen quad" };
            _mesh.vertices = new[] { new Vector3(-1,-1), new Vector3(1,-1), new Vector3(1,1), new Vector3(-1,1) };
            _mesh.triangles = new[] { 0,1,2,0,2,3 };
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "Overcast cloud canopy" : "District rainfall");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                var mat = new Material(shader) { name = go.name, renderQueue = i == 0 ? 3120 : 3130 };
                mat.SetFloat("_RainPass", i);
                mat.SetTexture("_CloudTex", Resources.Load<Texture2D>(DistrictCloudLayer.TextureResource));
                mat.SetVector("_District", new Vector4(width, depth, Mathf.Max(0,height) + 170, 0));
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.enabled = false;
                if (i == 0) { _deck = mat; _deckRenderer = renderer; }
                else { _rain = mat; _rainRenderer = renderer; }
            }
            _terrain = terrain;
            if (terrain != null)
            {
                var ground = new GameObject("Temporary district snow cover");
                ground.transform.SetParent(transform,false);
                _snowMesh = ground.AddComponent<MeshFilter>(); _snowMesh.sharedMesh = terrain.sharedMesh;
                _snowRenderer = ground.AddComponent<MeshRenderer>();
                _snowMaterial = new Material(Shader.Find("CityForgeV3/DistrictSnowCover"));
                _snowRenderer.sharedMaterial = _snowMaterial;
                _snowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _snowRenderer.enabled = false;
            }
        }
        public void Begin(bool snow = false)
        {
            if (_camera == null) return;
            Clear(); _snowMode = snow;
            _started = Time.realtimeSinceStartupAsDouble; _running = true;
            CurrentPhase = Phase.Gathering;
            Tick(0);
        }
        public void Clear()
        {
            _running = false; CurrentPhase = Phase.Clear; Coverage = RainIntensity = MistIntensity = SnowAccumulation = 0;
            if (_snowRenderer != null) _snowRenderer.enabled = false;
            if (_deckRenderer != null) _deckRenderer.enabled = false;
            if (_rainRenderer != null) _rainRenderer.enabled = false;
            _clouds?.SetStormCoverage(0);
        }
        public void SetZoom(DistrictZoomLevel zoom) { _zoom = zoom; }
        void LateUpdate()
        {
            if (_running) Tick((float)(Time.realtimeSinceStartupAsDouble - _started));
        }
        void Tick(float elapsed)
        {
            var state = Evaluate(elapsed); Coverage = state.x; RainIntensity = state.y;
            MistIntensity = EvaluateMist(elapsed);
            float duration = _snowMode ? BuildSeconds + SnowFallSeconds + SnowHoldSeconds + SnowMeltSeconds : BuildSeconds + RainSeconds + ClearSeconds;
            if (elapsed >= duration) { Clear(); return; }
            CurrentPhase = elapsed < BuildSeconds ? Phase.Gathering : elapsed < BuildSeconds + RainSeconds ? Phase.Raining : Phase.Clearing;
            if (_snowMode)
            {
                float age = elapsed - BuildSeconds;
                var snow = EvaluateSnow(age); RainIntensity = snow.x; SnowAccumulation = snow.y;
                CurrentPhase = age < 0 ? Phase.Gathering : age < 10 ? Phase.Snowing : age < 20 ? Phase.Settled : Phase.Melting;
            }
            if (_snowRenderer != null)
            {
                _snowRenderer.enabled = SnowAccumulation > 0;
                if (_snowRenderer.enabled)
                {
                    if (_snowMesh.sharedMesh != _terrain.sharedMesh) _snowMesh.sharedMesh = _terrain.sharedMesh;
                    _snowMaterial.SetFloat("_Accumulation", SnowAccumulation);
                }
            }
            _clouds?.SetStormCoverage(Coverage);
            _deckRenderer.enabled = DistrictCloudLayer.VisibleAt(_zoom) && Coverage > 0;
            _rainRenderer.enabled = Coverage > 0;
            // Fixed uniform writes track camera pan/zoom; no district traversal.
            Apply(_deck, elapsed); Apply(_rain, elapsed);
        }
        void Apply(Material material, float elapsed)
        {
            var t = _camera.transform;
            material.SetVector("_Eye", t.position);
            material.SetVector("_Right", t.right * (_camera.orthographicSize * _camera.aspect));
            material.SetVector("_Up", t.up * _camera.orthographicSize);
            material.SetVector("_Forward", t.forward);
            material.SetVector("_Weather", new Vector4(Coverage, RainIntensity, elapsed, _camera.aspect));
            material.SetFloat("_MistIntensity", MistIntensity);
            material.SetFloat("_Snowfall", _snowMode ? 1 : 0);
        }
        void OnDisable() { Clear(); }
        void OnDestroy()
        {
            Release(_mesh); Release(_deck); Release(_rain); Release(_snowMaterial);
        }
        static void Release(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
    }
}
