using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        // The imported brownstones face the established CityForge world view
        // at this authored yaw; camera corrections must not modify this value.
        private const float BrownstoneDefaultFacingDegrees = 90f;
        private const float DefaultExperimentalSkyExposure = 0.95f;
        private const float DefaultExperimentalBuildingContrast = 1.42f;
        private const float DefaultExperimentalBuildingSaturation = 1.34f;
        public const string BrownstoneBuilding22kId = "brownstone-building-22k";
        public const string BrownstoneBuilding22kResource =
            "CityForgeV3/Buildings3D/BrownstoneBuilding22k/brownstone-building-22k";
        public const string LowPolyBrownstoneV01Id = "low-poly-brownstone-v01";
        public const string LowPolyBrownstoneV01Resource =
            "CityForgeV3/Buildings3D/LowPolyBrownstoneV01/LowPolyBrownstone";
        private readonly List<GameObject> _experimentalBuilding3DRoots = new();
        private readonly List<Material> _experimentalBuilding3DMaterials = new();
        private readonly Dictionary<GameObject, GameObject>
            _experimentalBuilding3DGroundShadows = new();
        private float _shadowDistanceBeforeExperimental3D;
        private int _shadowCascadesBeforeExperimental3D;
        private bool _experimental3DShadowDistanceApplied;
        private Material _experimental3DStudioSkybox;
        private Material _skyboxBeforeExperimental3D;
        private AmbientMode _ambientModeBeforeExperimental3D;
        private float _ambientIntensityBeforeExperimental3D;
        private DefaultReflectionMode _reflectionModeBeforeExperimental3D;
        private float _reflectionIntensityBeforeExperimental3D;
        private bool _experimental3DStudioEnvironmentApplied;
        private float _environmentSunIntensityScale = 1f;
        private float _environmentSunElevationOffset;
        private float _environmentSunAzimuthOffset;
        private float _environmentAmbientIntensityScale = 1f;
        private float _environmentSkyExposure = DefaultExperimentalSkyExposure;
        private float _environmentShadowStrength = 0.86f;
        private float _environmentBuildingContrast =
            DefaultExperimentalBuildingContrast;
        private float _environmentBuildingSaturation =
            DefaultExperimentalBuildingSaturation;

        public float EnvironmentSunIntensityScale => _environmentSunIntensityScale;
        public float EnvironmentSunElevationOffset => _environmentSunElevationOffset;
        public float EnvironmentSunAzimuthOffset => _environmentSunAzimuthOffset;
        public float EnvironmentAmbientIntensityScale => _environmentAmbientIntensityScale;
        public float EnvironmentSkyExposure => _environmentSkyExposure;
        public float EnvironmentShadowStrength => _environmentShadowStrength;
        public float EnvironmentBuildingContrast => _environmentBuildingContrast;
        public float EnvironmentBuildingSaturation => _environmentBuildingSaturation;

        public int ExperimentalBuilding3DCount =>
            _session?.Data?.Buildings3D?.Count ?? 0;
        public int ExperimentalBuilding3DFloraShadowCasterCount
        {
            get
            {
                var count = 0;
                foreach (var root in _experimentalBuilding3DRoots)
                {
                    if (root == null ||
                        !root.name.Contains("Flora/Prop Shadow Caster"))
                        continue;
                    count += root.GetComponentsInChildren<Renderer>(true).Length;
                }
                return count;
            }
        }

        public void CreateExperimental3DBuildingsLot()
        {
            // Keep enough receiver area around the comparison buildings for
            // the correctly directed morning/afternoon mesh shadows to remain
            // visible. The fixture is presented as an 80 m experimental lot;
            // the former 24 m plane clipped nearly every low-angle shadow.
            NewEmptyLot("3D Buildings", LotType.Mixed, 8, 8);
            _session.Data.Buildings3D = new List<PlacedBuilding3D>();
            // The experimental camera is fixed at 20 degrees. Its screen-right
            // world vector is (-sin(20), 0, cos(20)); place the comparison
            // models along that vector so they read as one horizontal row in
            // the actual Game view rather than a world-X diagonal/vertical row.
            AddExperimentalBuilding3D(BrownstoneBuilding22kId,
                4.8f, -13.2f, 0);
            AddExperimentalBuilding3D(BrownstoneBuilding22kId,
                0f, 0f, 0);
            AddExperimentalBuilding3D(LowPolyBrownstoneV01Id,
                -4.8f, 13.2f, 0);
            SetBaseTexture("grass-lush");
            SaveLot();
        }

        public bool AddExperimentalBuilding3D(string assetId,
            float? worldX = null, float? worldZ = null,
            int? rotationQuarterTurns = null)
        {
            if (assetId != BrownstoneBuilding22kId &&
                assetId != LowPolyBrownstoneV01Id) return false;
            _session.Data.Buildings3D ??= new List<PlacedBuilding3D>();
            var index = _session.Data.Buildings3D.Count;
            var offset = index * 2.5f;
            _session.Data.Buildings3D.Add(new PlacedBuilding3D
            {
                AssetId = assetId,
                X = worldX ?? Mathf.Clamp(-10f + offset, -15f, 15f),
                Z = worldZ ?? Mathf.Clamp(-5f + offset, -15f, 15f),
                RotationQuarterTurns = rotationQuarterTurns ?? index % 4
            });
            RebuildExperimentalBuilding3DPresentations();
            ApplyTimeOfDay();
            ApplyCameraFacing(false);
            NotifyStateChanged();
            return true;
        }

        private void ApplyExperimentalBuilding3DShadowDistance()
        {
            ApplyExperimentalBuilding3DGroundReceiver();

            if (ExperimentalBuilding3DCount > 0)
            {
                if (!_experimental3DShadowDistanceApplied)
                {
                    _shadowDistanceBeforeExperimental3D =
                        QualitySettings.shadowDistance;
                    _shadowCascadesBeforeExperimental3D =
                        QualitySettings.shadowCascades;
                    _experimental3DShadowDistanceApplied = true;
                }
                // The 80 m orthographic pilot is viewed from outside the
                // lower quality tiers' 15-70 m shadow range. Keep the actual
                // mesh casters in Unity's directional shadow map. The 3D
                // pilot's morning elevation is calibrated separately below.
                QualitySettings.shadowDistance = Mathf.Max(
                    _shadowDistanceBeforeExperimental3D, 150f);
                QualitySettings.shadowCascades = 4;
            }
            else if (_experimental3DShadowDistanceApplied)
            {
                QualitySettings.shadowDistance =
                    _shadowDistanceBeforeExperimental3D;
                QualitySettings.shadowCascades =
                    _shadowCascadesBeforeExperimental3D;
                _experimental3DShadowDistanceApplied = false;
            }
        }

        private Quaternion ExperimentalBuilding3DSunRotation()
        {
            if (ExperimentalBuilding3DCount <= 0)
                return TimeOfDayLighting.SunRotation(TimeOfDay);
            var spec = TimeOfDayLighting.For(TimeOfDay);
            // Raising only the 3D pilot's morning elevation from 24° to 48°
            // retains its due-east compass while shortening the native shadow
            // footprint to roughly 40% of the former projection.
            // Preserve the universal compass contract: the azimuth describes
            // where the sun is, while a Unity directional light points along
            // the rays. Morning sun is east, therefore its rays and shadows
            // travel west. Afternoon uses the exact opposite bearing.
            return Quaternion.Euler(
                (TimeOfDay == TimeOfDayPreset.Morning
                    ? 48f : spec.SunElevation) +
                _environmentSunElevationOffset,
                spec.SunAzimuth + 90f + _environmentSunAzimuthOffset,
                0f);
        }

        private Color ExperimentalBuilding3DGroundColor(Color fallback)
        {
            if (ExperimentalBuilding3DCount <= 0) return fallback;
            return TimeOfDay switch
            {
                TimeOfDayPreset.Morning => new Color(0.085f, 0.125f, 0.095f),
                TimeOfDayPreset.Noon => new Color(0.095f, 0.145f, 0.095f),
                TimeOfDayPreset.Afternoon => new Color(0.09f, 0.125f, 0.08f),
                TimeOfDayPreset.Evening => new Color(0.045f, 0.065f, 0.055f),
                _ => new Color(0.025f, 0.04f, 0.035f)
            };
        }

        private void ApplyExperimentalBuilding3DStudioEnvironment()
        {
            if (ExperimentalBuilding3DCount <= 0)
            {
                RestoreExperimentalBuilding3DStudioEnvironment();
                return;
            }

            if (!_experimental3DStudioEnvironmentApplied)
            {
                _skyboxBeforeExperimental3D = RenderSettings.skybox;
                _ambientModeBeforeExperimental3D = RenderSettings.ambientMode;
                _ambientIntensityBeforeExperimental3D =
                    RenderSettings.ambientIntensity;
                _reflectionModeBeforeExperimental3D =
                    RenderSettings.defaultReflectionMode;
                _reflectionIntensityBeforeExperimental3D =
                    RenderSettings.reflectionIntensity;
                _experimental3DStudioEnvironmentApplied = true;
            }

            if (_experimental3DStudioSkybox == null)
            {
                var panorama = Resources.Load<Texture2D>(
                    "CityForgeV3/Environment/TripoStudioV01/tripo-studio-lighting");
                var shader = Shader.Find("Skybox/Panoramic");
                if (panorama == null || shader == null) return;
                _experimental3DStudioSkybox = new Material(shader)
                {
                    name = "CF Tripo Studio IBL — Strength 0.92"
                };
                _experimental3DStudioSkybox.SetTexture("_MainTex", panorama);
                _experimental3DStudioSkybox.SetFloat("_Exposure",
                    _environmentSkyExposure);
                _experimental3DStudioSkybox.SetFloat("_Rotation", 0f);
            }

            RenderSettings.skybox = _experimental3DStudioSkybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            _experimental3DStudioSkybox.SetFloat("_Exposure",
                _environmentSkyExposure);
            RenderSettings.ambientIntensity =
                0.92f * _environmentAmbientIntensityScale;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 1f;
            DynamicGI.UpdateEnvironment();
        }

        private void ApplyExperimentalBuilding3DColorGrade()
        {
            foreach (var root in _experimentalBuilding3DRoots)
            {
                if (root == null) continue;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null ||
                        material.shader.name !=
                        "CityForgeV3/Experimental3DBuildingPBR") continue;
                    material.SetFloat("_Contrast", _environmentBuildingContrast);
                    material.SetFloat("_Saturation", _environmentBuildingSaturation);
                }
            }
        }

        public void SetEnvironmentLightingControl(string control, float value)
        {
            switch (control)
            {
                case "sun-intensity":
                    _environmentSunIntensityScale = Mathf.Clamp(value, 0f, 2f);
                    break;
                case "sun-elevation":
                    _environmentSunElevationOffset = Mathf.Clamp(value, -35f, 35f);
                    break;
                case "sun-azimuth":
                    _environmentSunAzimuthOffset = Mathf.Clamp(value, -45f, 45f);
                    break;
                case "ambient":
                    _environmentAmbientIntensityScale = Mathf.Clamp(value, 0f, 1.5f);
                    break;
                case "exposure":
                    _environmentSkyExposure = Mathf.Clamp(value, 0.25f, 1.5f);
                    break;
                case "shadow-strength":
                    _environmentShadowStrength = Mathf.Clamp01(value);
                    break;
                case "contrast":
                    _environmentBuildingContrast = Mathf.Clamp(value, 0.8f, 2f);
                    break;
                case "saturation":
                    _environmentBuildingSaturation = Mathf.Clamp(value, 0f, 2f);
                    break;
                default:
                    return;
            }
            ApplyTimeOfDay();
            ApplyExperimentalBuilding3DColorGrade();
        }

        public void ResetEnvironmentLightingControls()
        {
            _environmentSunIntensityScale = 1f;
            _environmentSunElevationOffset = 0f;
            _environmentSunAzimuthOffset = 0f;
            _environmentAmbientIntensityScale = 1f;
            _environmentSkyExposure = DefaultExperimentalSkyExposure;
            _environmentShadowStrength = 0.86f;
            _environmentBuildingContrast = DefaultExperimentalBuildingContrast;
            _environmentBuildingSaturation = DefaultExperimentalBuildingSaturation;
            ApplyTimeOfDay();
            ApplyExperimentalBuilding3DColorGrade();
        }

        private void RestoreExperimentalBuilding3DStudioEnvironment()
        {
            if (!_experimental3DStudioEnvironmentApplied) return;
            RenderSettings.skybox = _skyboxBeforeExperimental3D;
            RenderSettings.ambientMode = _ambientModeBeforeExperimental3D;
            RenderSettings.ambientIntensity = _ambientIntensityBeforeExperimental3D;
            RenderSettings.defaultReflectionMode =
                _reflectionModeBeforeExperimental3D;
            RenderSettings.reflectionIntensity =
                _reflectionIntensityBeforeExperimental3D;
            _experimental3DStudioEnvironmentApplied = false;
            DynamicGI.UpdateEnvironment();
        }

        private void ApplyExperimentalBuilding3DGroundReceiver()
        {
            if (_groundRenderer == null ||
                _groundRenderer.sharedMaterial == null) return;
            var material = _groundRenderer.sharedMaterial;
            var shaderName = ExperimentalBuilding3DCount > 0
                ? "CityForgeV3/Experimental3DGroundReceiver"
                : "CityForgeV3/ShadowReceivingLotSurface";
            var shader = Shader.Find(shaderName);
            if (shader == null) return;

            var texture = material.mainTexture;
            var textureScale = material.mainTextureScale;
            var textureOffset = material.mainTextureOffset;
            if (material.shader != shader)
            {
                material.shader = shader;
                material.mainTexture = texture;
                material.mainTextureScale = textureScale;
                material.mainTextureOffset = textureOffset;
            }

            // A shader transition can occur after the base-texture pass while a
            // saved lot is rebuilding. Do not rely on the previous shader's
            // property state: resolve and bind the authored seasonal texture to
            // the receiver explicitly every time lighting is applied.
            var option = ResolveBaseTexture(BaseTextureId);
            if (option == null) return;
            var authoredTexture = Resources.Load<Texture2D>(
                option.ResolveResourcePath(Season));
            material.SetTexture("_MainTex", authoredTexture);
            material.SetTextureScale("_MainTex", new Vector2(
                Mathf.Max(1f, LotWidthMeters / 5f),
                Mathf.Max(1f, LotDepthMeters / 5f)));
            material.SetTextureOffset("_MainTex", Vector2.zero);
            // The selected base texture must draw first. Projected 3D-building
            // shadows use transparent queue 3001, so they blend visibly over
            // the opaque grass without changing its color calculation.
            material.renderQueue = 2000;
        }

        private void OnDestroy()
        {
            RestoreExperimentalBuilding3DStudioEnvironment();
            if (_experimental3DStudioSkybox != null)
            {
                if (Application.isPlaying) Destroy(_experimental3DStudioSkybox);
                else DestroyImmediate(_experimental3DStudioSkybox);
            }
            if (_experimental3DShadowDistanceApplied)
            {
                QualitySettings.shadowDistance =
                    _shadowDistanceBeforeExperimental3D;
                QualitySettings.shadowCascades =
                    _shadowCascadesBeforeExperimental3D;
            }
        }

        private void RebuildExperimentalBuilding3DPresentations()
        {
            // Editor domain reloads reset the runtime tracking collections but
            // preserve scene objects. Sweep only our own top-level pilot roots
            // so a stale morning/afternoon shadow can never survive into a
            // later preset and appear as a duplicate.
            var staleRoots = new List<GameObject>();
            foreach (Transform child in transform)
                if (child != null && child.name.StartsWith("3D Building"))
                    staleRoots.Add(child.gameObject);
            foreach (var staleRoot in staleRoots)
                DestroyForCurrentMode(staleRoot);
            foreach (var root in _experimentalBuilding3DRoots)
                if (root != null && !staleRoots.Contains(root))
                    DestroyForCurrentMode(root);
            _experimentalBuilding3DRoots.Clear();
            _experimentalBuilding3DGroundShadows.Clear();
            foreach (var material in _experimentalBuilding3DMaterials)
                if (material != null)
                {
                    if (Application.isPlaying) Destroy(material);
                    else DestroyImmediate(material);
                }
            _experimentalBuilding3DMaterials.Clear();
            if (_session?.Data?.Buildings3D == null) return;

            foreach (var placed in _session.Data.Buildings3D)
            {
                if (placed == null)
                    continue;
                var source = Resources.Load<GameObject>(
                    placed.AssetId == LowPolyBrownstoneV01Id
                        ? LowPolyBrownstoneV01Resource
                        : BrownstoneBuilding22kResource);
                if (source == null)
                {
                    Debug.LogError($"Missing experimental 3D building: {BrownstoneBuilding22kResource}");
                    continue;
                }
                var root = Instantiate(source, transform);
                root.name = placed.AssetId == LowPolyBrownstoneV01Id
                    ? "3D Building — Low-Poly Brownstone V01"
                    : "3D Building — Brownstone 22K";
                root.transform.localPosition = new Vector3(placed.X, 0f, placed.Z);
                root.transform.localRotation = ExperimentalBuildingRotation(placed);
                var material = placed.AssetId == BrownstoneBuilding22kId
                    ? CreateBrownstoneBuilding22kMaterial()
                    : null;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (material != null)
                    {
                        var materials = renderer.sharedMaterials;
                        if (materials.Length == 0)
                            materials = new[] { material };
                        else
                            for (var index = 0; index < materials.Length; index++)
                                if (materials[index] == null ||
                                    materials[index].name.Contains("tripo_mat"))
                                    materials[index] = material;
                        renderer.sharedMaterials = materials;
                    }
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    renderer.allowOcclusionWhenDynamic = true;
                }
                GroundExperimentalBuilding(root);
                _experimentalBuilding3DRoots.Add(root);
                BuildExperimentalBuilding3DReceiverShadowCaster(root);
                BuildExperimentalBuilding3DProjectedGroundShadow(root);
            }
            ApplyExperimentalBuilding3DColorGrade();
            // Normal saved-lot loading applies the current lighting before
            // ApplySessionState reconstructs these runtime shadow objects.
            // Populate their meshes immediately after reconstruction so the
            // loaded lot does not wait for a later time-of-day interaction.
            UpdateExperimentalBuilding3DProjectedGroundShadows();
        }

        private void BuildExperimentalBuilding3DProjectedGroundShadow(
            GameObject visibleRoot)
        {
            // Project a hidden copy of the real render meshes. This preserves
            // stairs, cornices, chimneys, and every other silhouette feature;
            // the former convex hull could only ever produce a rectangle.
            var shadow = Instantiate(visibleRoot, transform);
            shadow.name = $"3D Building Ground Shadow — {visibleRoot.name}";
            var shader = Shader.Find(
                "CityForgeV3/ProjectedBuildingMeshShadow");
            if (shader == null)
                throw new MissingReferenceException(
                    "The mesh-projected ground-shadow shader is required.");
            var material = new Material(shader)
            {
                color = new Color(0f, 0f, 0f, 0.20f),
                renderQueue = 3001
            };
            material.name = $"CF Ground Shadow — {visibleRoot.name}";
            foreach (var collider in shadow.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (var renderer in shadow.GetComponentsInChildren<Renderer>(true))
            {
                var count = Mathf.Max(1, renderer.sharedMaterials.Length);
                var materials = new Material[count];
                for (var index = 0; index < count; index++)
                    materials[index] = material;
                renderer.sharedMaterials = materials;
                renderer.enabled = true;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.allowOcclusionWhenDynamic = false;
            }
            _experimentalBuilding3DMaterials.Add(material);
            _experimentalBuilding3DGroundShadows[visibleRoot] = shadow;
            _experimentalBuilding3DRoots.Add(shadow);
        }

        private void UpdateExperimentalBuilding3DProjectedGroundShadows()
        {
            if (ExperimentalBuilding3DCount <= 0 || _sun == null) return;
            var ray = _sun.transform.forward;
            // The exact-color grass receiver is intentionally unlit, so it
            // cannot display Unity's native shadow map. Keep the mesh-derived
            // projection active at noon as a short contact shadow; disabling
            // it left a freshly loaded 3D Buildings lot with no ground shadow
            // at all. Night and rain remain intentionally shadow-free.
            var visible = !IsRaining &&
                TimeOfDay != TimeOfDayPreset.Night &&
                ray.y < -0.01f;
            var lengthScale = TimeOfDay == TimeOfDayPreset.Noon
                ? 0f
                : BuildingShadowLengthScale(TimeOfDay);
            var opacity = TimeOfDay switch
            {
                TimeOfDayPreset.Morning => 0.26f,
                TimeOfDayPreset.Noon => 0.26f,
                TimeOfDayPreset.Afternoon => 0.26f,
                _ => 0.22f
            };
            opacity *= Mathf.Clamp01(_environmentShadowStrength);

            foreach (var pair in _experimentalBuilding3DGroundShadows)
            {
                var source = pair.Key;
                var shadow = pair.Value;
                if (source == null || shadow == null) continue;
                shadow.SetActive(visible);
                if (!visible) continue;

                var bounds = default(Bounds);
                var hasBounds = false;
                foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (!hasBounds) continue;
                var referenceHeight = Mathf.Max(0.01f, bounds.max.y - 0.018f);
                var displacement = new Vector3(ray.x, 0f, ray.z) *
                    (referenceHeight / -ray.y) * lengthScale;
                foreach (var renderer in shadow.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                {
                    material.SetColor("_Color", new Color(0f, 0f, 0f, opacity));
                    material.SetVector("_ShadowDisplacement", displacement);
                    material.SetFloat("_GroundY", 0.018f);
                    material.SetFloat("_ReferenceHeight", referenceHeight);
                }
            }
        }

        private static Quaternion ExperimentalBuildingRotation(
            PlacedBuilding3D placed) => Quaternion.Euler(
            -90f,
            BrownstoneDefaultFacingDegrees +
            placed.RotationQuarterTurns * 90f,
            0f);

        private static void GroundExperimentalBuilding(GameObject root)
        {
            var bounds = default(Bounds);
            var hasBounds = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (hasBounds)
                root.transform.position += Vector3.up * -bounds.min.y;
        }

        private void BuildExperimentalBuilding3DReceiverShadowCaster(
            GameObject visibleRoot)
        {
            // The experimental PBR beauty shader is not a reliable native
            // caster on every graphics backend. Give the main sun one hidden,
            // mesh-identical caster and disable casting on the visible copy so
            // the ground receives exactly one detailed silhouette (never the
            // old bounds rectangle and never a doubled shadow).
            foreach (var renderer in visibleRoot.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            var groundCaster = Instantiate(visibleRoot, transform);
            groundCaster.name = "3D Building — Brownstone Native Ground Shadow Caster";
            foreach (var child in groundCaster.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 0;
            foreach (var collider in groundCaster.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            var casterShader = Shader.Find("Standard");
            if (casterShader == null)
                throw new MissingReferenceException(
                    "Unity Standard shader is required for native 3D building shadows.");
            foreach (var renderer in groundCaster.GetComponentsInChildren<Renderer>(true))
            {
                // The caster is never drawn, so use the engine's canonical
                // opaque ShadowCaster pass instead of the beauty material.
                renderer.sharedMaterial = new Material(casterShader)
                {
                    name = "CF Hidden Native Building Shadow Caster"
                };
                renderer.enabled = true;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderer.allowOcclusionWhenDynamic = false;
            }
            _experimentalBuilding3DRoots.Add(groundCaster);

            // Flora and transparent billboard receivers are deliberately kept
            // on an isolated layer so the main sun cannot light them twice.
            // Mirror the brownstone mesh into that layer as ShadowsOnly: the
            // visible model continues to cast onto opaque ground/3D props,
            // while this copy casts the identical time-of-day shadow across
            // alpha-cutout flora and any future billboard prop receivers.
            var caster = Instantiate(visibleRoot, transform);
            caster.name = "3D Building — Brownstone Flora/Prop Shadow Caster";
            foreach (var child in caster.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = FloraShadowReceiverLayer;
            foreach (var collider in caster.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (var renderer in caster.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderer.allowOcclusionWhenDynamic = false;
            }
            _experimentalBuilding3DRoots.Add(caster);
        }

        private Material CreateBrownstoneBuilding22kMaterial()
        {
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            if (shader == null)
                throw new MissingReferenceException(
                    "CityForge's contrast-preserving PBR shader is required for the 3D building pilot.");
            var material = new Material(shader)
            {
                name = "CF Brownstone Building 22K PBR"
            };
            material.SetFloat("_Contrast", _environmentBuildingContrast);
            material.SetFloat("_Saturation", _environmentBuildingSaturation);
            var root = "CityForgeV3/Buildings3D/BrownstoneBuilding22k/";
            material.mainTexture = Resources.Load<Texture2D>(root +
                "brownstone_building_3d_model_basecolor");
            var normal = Resources.Load<Texture2D>(root +
                "brownstone_building_3d_model_normal");
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            var metallicSmoothness = Resources.Load<Texture2D>(root +
                "brownstone-metallic-smoothness");
            if (metallicSmoothness != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicSmoothness);
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_GlossMapScale", 1f);
                material.EnableKeyword("_METALLICGLOSSMAP");
            }
            else
            {
                material.SetFloat("_Metallic", 0.05f);
                material.SetFloat("_Glossiness", 0.28f);
            }
            _experimentalBuilding3DMaterials.Add(material);
            return material;
        }
    }
}
