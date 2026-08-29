using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.Buildings3D
{
    [Serializable]
    public sealed class WindowMaterialTarget
    {
        public Renderer Renderer;
        [Min(0)] public int MaterialIndex;
    }

    [Serializable]
    public sealed class WindowLightPoint
    {
        public Transform Anchor;
        [Min(0f)] public float IntensityMultiplier = 1f;
        [Min(0f)] public float RangeMultiplier = 1f;
        public bool EnabledAtNight = true;
        [NonSerialized] public Light RuntimeLight;
        [NonSerialized] public bool Selected;
    }

    [Serializable]
    public sealed class ExteriorLampPoint
    {
        public Transform Anchor;
        [Min(0f)] public float IntensityMultiplier = 1f;
        [Min(0f)] public float RangeMultiplier = 1f;
        public bool CastShadows;
        [NonSerialized] public Light RuntimeLight;
    }

    [DisallowMultipleComponent]
    public sealed class BuildingNightLighting : MonoBehaviour
    {
        private static readonly int EmissionMaskId =
            Shader.PropertyToID("_NightEmissionMask");
        private static readonly int EmissionColorId =
            Shader.PropertyToID("_NightEmissionColor");
        private static readonly int EmissionIntensityId =
            Shader.PropertyToID("_NightEmissionIntensity");
        private static readonly int StandardEmissionColorId =
            Shader.PropertyToID("_EmissionColor");
        private static readonly int StandardEmissionMapId =
            Shader.PropertyToID("_EmissionMap");

        [Header("Window emission")]
        [Tooltip("Optional material used only as a selector. Matching material slots receive night properties without modifying the shared asset.")]
        [SerializeField] private Material windowMaterialOverride;
        [SerializeField] private List<WindowMaterialTarget> windowMaterialTargets = new();
        [SerializeField] private Texture2D emissionMask;
        [ColorUsage(true, true)]
        [SerializeField] private Color windowEmissionColor =
            new(1f, 0.55f, 0.22f, 1f);
        [Min(0f)] [SerializeField] private float windowEmissionIntensity = 2f;
        [Range(0f, 1f)] [SerializeField] private float nightAmount;

        [Header("Window light spill")]
        [SerializeField] private List<WindowLightPoint> windowLights = new();
        [Min(0f)] [SerializeField] private float windowSpillIntensity = 0.55f;
        [Min(0f)] [SerializeField] private float windowSpillRange = 2f;
        [SerializeField] private LightType windowSpillType = LightType.Point;
        [Range(1f, 179f)] [SerializeField] private float windowSpotAngle = 105f;

        [Header("Exterior lamps")]
        [SerializeField] private List<ExteriorLampPoint> exteriorLamps = new();
        [Min(0f)] [SerializeField] private float exteriorLampIntensity = 1.35f;
        [Min(0f)] [SerializeField] private float exteriorLampRange = 4f;

        [Header("Occupancy pattern")]
        [Range(0f, 1f)] [SerializeField] private float litWindowPercentage = 0.55f;
        [SerializeField] private int randomSeed;
        [SerializeField] private bool rerollEachNight = true;

        private readonly List<WindowMaterialTarget> cachedTargets = new();
        private MaterialPropertyBlock propertyBlock;
        private bool initialized;
        private bool wasNight;
        private int nightCycle;

        public float NightAmount => nightAmount;

        public void ConfigureEmissionMask(Texture2D value)
        {
            emissionMask = value;
            initialized = false;
        }

        public void ConfigureAnchors(IEnumerable<Transform> windows,
            IEnumerable<Transform> lamps)
        {
            windowLights.Clear();
            if (windows != null)
                foreach (var anchor in windows)
                    if (anchor != null)
                        windowLights.Add(new WindowLightPoint { Anchor = anchor });
            exteriorLamps.Clear();
            if (lamps != null)
                foreach (var anchor in lamps)
                    if (anchor != null)
                        exteriorLamps.Add(new ExteriorLampPoint { Anchor = anchor });
            initialized = false;
        }

        public void ConfigureTuning(float emissionIntensity,
            float spillIntensity, float spillRangeMeters,
            float lampIntensity, float lampRangeMeters)
        {
            windowEmissionIntensity = Mathf.Max(0f, emissionIntensity);
            windowSpillIntensity = Mathf.Max(0f, spillIntensity);
            windowSpillRange = Mathf.Max(0f, spillRangeMeters);
            exteriorLampIntensity = Mathf.Max(0f, lampIntensity);
            exteriorLampRange = Mathf.Max(0f, lampRangeMeters);
            initialized = false;
        }

        private void Awake()
        {
            Initialize();
            SetNightAmount(nightAmount);
        }

        private void OnEnable()
        {
            Initialize();
            SetNightAmount(nightAmount);
        }

        private void OnValidate()
        {
            nightAmount = Mathf.Clamp01(nightAmount);
            litWindowPercentage = Mathf.Clamp01(litWindowPercentage);
            initialized = false;
        }

        public void SetNightAmount(float value)
        {
            Initialize();
            nightAmount = Mathf.Clamp01(value);
            var isNight = nightAmount > 0.05f;
            if (isNight && !wasNight && (rerollEachNight || nightCycle == 0))
            {
                nightCycle++;
                RollWindowPattern();
            }
            wasNight = isNight;
            ApplyEmission();
            ApplyLights(isNight);
        }

        [ContextMenu("Preview Night")]
        public void PreviewNight() => SetNightAmount(1f);

        [ContextMenu("Preview Day")]
        public void PreviewDay() => SetNightAmount(0f);

        [ContextMenu("Reroll Window Pattern")]
        public void RerollWindowPattern()
        {
            nightCycle++;
            Initialize();
            RollWindowPattern();
            ApplyLights(nightAmount > 0.05f);
        }

        [ContextMenu("Create Window Light Anchor")]
        public void CreateWindowLightAnchor()
        {
            var anchor = CreateAnchor("WindowLight", windowLights.Count + 1);
            windowLights.Add(new WindowLightPoint { Anchor = anchor });
            initialized = false;
        }

        [ContextMenu("Create Exterior Lamp Anchor")]
        public void CreateExteriorLampAnchor()
        {
            var anchor = CreateAnchor("ExteriorLamp", exteriorLamps.Count + 1);
            exteriorLamps.Add(new ExteriorLampPoint { Anchor = anchor });
            initialized = false;
        }

        private void Initialize()
        {
            if (initialized) return;
            propertyBlock ??= new MaterialPropertyBlock();
            cachedTargets.Clear();
            foreach (var target in windowMaterialTargets)
                if (IsValid(target)) cachedTargets.Add(target);
            if (cachedTargets.Count == 0)
                CacheMatchingOrMaskedRenderers();
            foreach (var point in windowLights)
                if (point?.Anchor != null)
                    point.RuntimeLight = ConfigureLight(point.Anchor,
                        "Window Spill", windowSpillType, false, windowSpotAngle);
            foreach (var point in exteriorLamps)
                if (point?.Anchor != null)
                    point.RuntimeLight = ConfigureLight(point.Anchor,
                        "Exterior Lamp", LightType.Point, point.CastShadows, 120f);
            initialized = true;
            if (nightCycle == 0) RollWindowPattern();
        }

        private void CacheMatchingOrMaskedRenderers()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null) continue;
                    var matchesOverride = windowMaterialOverride != null &&
                        material == windowMaterialOverride;
                    var supportsMask = emissionMask != null &&
                        material.HasProperty(EmissionMaskId);
                    if (matchesOverride || supportsMask)
                        cachedTargets.Add(new WindowMaterialTarget
                            { Renderer = renderer, MaterialIndex = index });
                }
            }
        }

        private void ApplyEmission()
        {
            foreach (var target in cachedTargets)
            {
                if (!IsValid(target)) continue;
                target.Renderer.GetPropertyBlock(propertyBlock, target.MaterialIndex);
                var material = target.Renderer.sharedMaterials[target.MaterialIndex];
                var intensity = windowEmissionIntensity * nightAmount;
                if (material.HasProperty(EmissionIntensityId))
                {
                    if (emissionMask != null)
                        propertyBlock.SetTexture(EmissionMaskId, emissionMask);
                    propertyBlock.SetColor(EmissionColorId, windowEmissionColor);
                    propertyBlock.SetFloat(EmissionIntensityId, intensity);
                }
                else if (material.HasProperty(StandardEmissionColorId))
                {
                    // Separate-window materials may use Unity's Standard
                    // emission contract. Keep _EMISSION enabled on that
                    // authored material; per-instance color and map remain in
                    // the property block and never mutate the shared asset.
                    propertyBlock.SetColor(StandardEmissionColorId,
                        windowEmissionColor * intensity);
                    if (emissionMask != null &&
                        material.HasProperty(StandardEmissionMapId))
                        propertyBlock.SetTexture(StandardEmissionMapId, emissionMask);
                }
                target.Renderer.SetPropertyBlock(propertyBlock, target.MaterialIndex);
                propertyBlock.Clear();
            }
        }

        private void ApplyLights(bool active)
        {
            foreach (var point in windowLights)
            {
                if (point?.RuntimeLight == null) continue;
                var enabled = active && point.EnabledAtNight && point.Selected;
                point.RuntimeLight.enabled = enabled;
                point.RuntimeLight.color = windowEmissionColor;
                point.RuntimeLight.range = windowSpillRange * point.RangeMultiplier;
                point.RuntimeLight.intensity = windowSpillIntensity *
                    point.IntensityMultiplier * nightAmount;
            }
            foreach (var point in exteriorLamps)
            {
                if (point?.RuntimeLight == null) continue;
                point.RuntimeLight.enabled = active;
                point.RuntimeLight.color = windowEmissionColor;
                point.RuntimeLight.range = exteriorLampRange * point.RangeMultiplier;
                point.RuntimeLight.intensity = exteriorLampIntensity *
                    point.IntensityMultiplier * nightAmount;
                point.RuntimeLight.shadows = point.CastShadows
                    ? LightShadows.Soft : LightShadows.None;
            }
        }

        private void RollWindowPattern()
        {
            var state = UnityEngine.Random.state;
            UnityEngine.Random.InitState(ResolvedSeed() + nightCycle * 486187739);
            foreach (var point in windowLights)
                if (point != null)
                    point.Selected = UnityEngine.Random.value < litWindowPercentage;
            UnityEngine.Random.state = state;
        }

        private int ResolvedSeed()
        {
            if (randomSeed != 0) return randomSeed;
            var p = transform.position;
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + gameObject.name.GetHashCode();
                hash = hash * 31 + Mathf.RoundToInt(p.x * 10f);
                hash = hash * 31 + Mathf.RoundToInt(p.y * 10f);
                hash = hash * 31 + Mathf.RoundToInt(p.z * 10f);
                return hash;
            }
        }

        private static bool IsValid(WindowMaterialTarget target) =>
            target?.Renderer != null && target.MaterialIndex >= 0 &&
            target.MaterialIndex < target.Renderer.sharedMaterials.Length;

        private static Light ConfigureLight(Transform anchor, string childName,
            LightType type, bool shadows, float spotAngle)
        {
            var light = anchor.GetComponent<Light>();
            if (light == null)
            {
                var child = new GameObject(childName);
                child.transform.SetParent(anchor, false);
                light = child.AddComponent<Light>();
            }
            light.type = type;
            light.spotAngle = spotAngle;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            light.enabled = false;
            return light;
        }

        private Transform CreateAnchor(string prefix, int index)
        {
            var anchor = new GameObject($"{prefix}_{index:000}").transform;
            anchor.SetParent(transform, false);
            return anchor;
        }
    }
}
