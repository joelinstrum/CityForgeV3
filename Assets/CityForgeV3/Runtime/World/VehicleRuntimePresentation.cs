using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum VehiclePaintVariant
    {
        Green,
        Blue,
        Red,
        Yellow
    }

    public enum TestVehicleModel
    {
        FordModelT,
        RollsRoyce1926
    }

    /// <summary>
    /// Presentation-only controller for a vehicle traveling on a circulation graph.
    /// Route ownership remains in the simulation/network layer.
    /// </summary>
    public sealed class VehicleRuntimePresentation : MonoBehaviour
    {
        public const string ModelTResourcePath =
            "CityForgeV3/Vehicles/FordModelT/CF_Vehicle_FordModelT_LOD0_v01";
        // The reviewed Blender hierarchy declares +X as the vehicle nose. Unity
        // route travelers face +Z, so +X must rotate toward +Z (negative yaw).
        public const float ModelYawOffsetDegrees = -90f;
        public const float MaximumVisualSteeringDegrees = 10f;
        public const float PresentationScale = 1.28f;
        public const float ShadowProxyHeightScale = 0.42f;
        public const int ContactShadowSortingOrder = 0;

        private readonly List<Transform> _rollingWheels = new();
        private readonly List<Transform> _steeringPivots = new();
        private readonly List<Quaternion> _steeringBaseRotations = new();
        private readonly List<Light> _headlights = new();
        private readonly List<Renderer> _headlightLenses = new();
        private Vector3 _previousLocalPosition;
        private bool _hasPreviousPosition;
        private float _wheelRadiusMeters = 0.41f;

        public int RollingWheelCount => _rollingWheels.Count;
        public int SteeringPivotCount => _steeringPivots.Count;
        public IReadOnlyList<Transform> RollingWheels => _rollingWheels;
        public IReadOnlyList<Transform> SteeringPivots => _steeringPivots;
        public Transform VisualRoot { get; private set; }
        public Transform FrontAxleBrace { get; private set; }
        public float AppliedVisualSteeringDegrees { get; private set; }
        public int HeadlightCount => _headlights.Count;
        public bool HeadlightsEnabled => _headlights.Count > 0 && _headlights[0].enabled;
        public int ShadowCastingRendererCount { get; private set; }
        public int ShadowProxyRendererCount { get; private set; }
        public bool HasBlackRoof { get; private set; }
        public Transform ShadowProxyRoot { get; private set; }
        public Transform ContactShadow { get; private set; }
        public StreetVehicleGroundShadow DirectionalShadow { get; private set; }
        public VehiclePaintVariant PaintVariant { get; private set; }
        public TestVehicleModel VehicleModel { get; private set; }

        public static VehicleRuntimePresentation Create(
            Transform parent, VehiclePaintVariant paintVariant = VehiclePaintVariant.Green)
            => Create(parent, TestVehicleModel.FordModelT, paintVariant);

        public static VehicleRuntimePresentation Create(
            Transform parent, TestVehicleModel vehicleModel,
            VehiclePaintVariant paintVariant = VehiclePaintVariant.Green)
        {
            var vehicleType = vehicleModel == TestVehicleModel.RollsRoyce1926
                ? VehicleTypePackage.LoadRollsRoyce1926()
                : VehicleTypePackage.LoadModelT();
            var asset = Resources.Load<GameObject>(vehicleType.ModelResourcePath);
            if (asset == null)
                throw new MissingReferenceException(
                    $"Missing City Forge vehicle model at Resources/{vehicleType.ModelResourcePath}.");

            var traveler = new GameObject("Vehicle Traveler");
            traveler.transform.SetParent(parent, false);
            var presentation = traveler.AddComponent<VehicleRuntimePresentation>();
            presentation.VehicleModel = vehicleModel;
            presentation.PaintVariant = paintVariant;
            var imported = Instantiate(asset, traveler.transform, false);
            var visual = vehicleModel == TestVehicleModel.RollsRoyce1926
                ? ExtractPrimaryVehicleMesh(imported, traveler.transform)
                : imported;
            visual.name = $"{vehicleType.DisplayName} Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation =
                (vehicleModel == TestVehicleModel.RollsRoyce1926
                    // This FBX already imports Y-up with its length on Unity Z.
                    ? Quaternion.identity
                    : Quaternion.Euler(0f, ModelYawOffsetDegrees, 0f)) *
                visual.transform.localRotation;
            presentation.VisualRoot = visual.transform;
            if (vehicleModel == TestVehicleModel.RollsRoyce1926)
                presentation.NormalizeVisualLength(vehicleType.LengthMeters);
            presentation.ConfigureImportedHierarchy();
            if (vehicleModel == TestVehicleModel.FordModelT)
                presentation.BuildFrontAxleBrace();
            presentation.GroundVisual();
            if (vehicleModel == TestVehicleModel.FordModelT)
                presentation.ApplyPaintVariant(paintVariant);
            presentation.PromoteVehicleAboveRoadDecals();
            presentation.BuildShadowPresentation();
            presentation.DirectionalShadow = traveler.AddComponent<
                StreetVehicleGroundShadow>();
            presentation.DirectionalShadow.Initialize(presentation.VisualRoot);
            if (vehicleModel == TestVehicleModel.FordModelT)
                presentation.BuildHeadlights();
            presentation.SetTimeOfDay(TimeOfDayPreset.Noon);
            traveler.transform.localScale = vehicleModel == TestVehicleModel.FordModelT
                ? Vector3.one * PresentationScale
                : Vector3.one;
            return presentation;
        }

        private static GameObject ExtractPrimaryVehicleMesh(
            GameObject imported, Transform parent)
        {
            Renderer primary = null;
            var largestBounds = 0f;
            foreach (var renderer in imported.GetComponentsInChildren<Renderer>(true))
            {
                var size = renderer.bounds.size.sqrMagnitude;
                if (!renderer.name.StartsWith("tripo_node_", StringComparison.OrdinalIgnoreCase) ||
                    size <= largestBounds) continue;
                primary = renderer;
                largestBounds = size;
            }
            if (primary == null) return imported;
            var visual = primary.gameObject;
            if (visual == imported) return imported;
            visual.transform.SetParent(parent, true);
            imported.SetActive(false);
            if (Application.isPlaying) Destroy(imported);
            else DestroyImmediate(imported);
            return visual;
        }

        private void NormalizeVisualLength(float desiredLengthMeters)
        {
            var renderers = VisualRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            var footprintLength = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprintLength <= 0.001f) return;
            VisualRoot.localScale *= desiredLengthMeters / footprintLength;
        }

        public static Color PaintColor(VehiclePaintVariant variant) => variant switch
        {
            VehiclePaintVariant.Blue => new Color(0.055f, 0.18f, 0.38f),
            VehiclePaintVariant.Red => new Color(0.42f, 0.055f, 0.04f),
            VehiclePaintVariant.Yellow => new Color(0.76f, 0.48f, 0.045f),
            _ => new Color(0.08f, 0.29f, 0.14f)
        };

        public void SetTimeOfDay(TimeOfDayPreset preset)
        {
            var enabled = preset == TimeOfDayPreset.Evening ||
                preset == TimeOfDayPreset.Night;
            var intensity = preset == TimeOfDayPreset.Night ? 5f : 2.5f;
            foreach (var headlight in _headlights)
            {
                headlight.enabled = enabled;
                headlight.intensity = intensity;
            }
            foreach (var lens in _headlightLenses)
                lens.gameObject.SetActive(enabled);
        }

        public void SetShadowLighting(Vector3 sunRay, bool visible) =>
            DirectionalShadow?.SetLighting(sunRay, visible);

        public void Place(Vector2 pointMeters, Vector2 direction, float steeringDegrees = 0f)
        {
            var next = new Vector3(pointMeters.x, 0.03f, pointMeters.y);
            if (direction.sqrMagnitude > 0.0001f)
                transform.localRotation = Quaternion.LookRotation(
                    new Vector3(direction.x, 0f, direction.y).normalized,
                    Vector3.up);

            if (_hasPreviousPosition)
            {
                var distance = Vector3.Distance(_previousLocalPosition, next);
                var degrees = distance / Mathf.Max(0.01f, _wheelRadiusMeters) * Mathf.Rad2Deg;
                foreach (var wheel in _rollingWheels)
                    wheel.Rotate(Vector3.up, degrees, Space.Self);
            }
            transform.localPosition = next;
            // The imported steering pivots retain Blender's handedness after the
            // +X-to-+Z body correction, so their visible yaw is the inverse of
            // the route's signed turn angle.
            var visualSteering = Mathf.Clamp(
                -steeringDegrees, -MaximumVisualSteeringDegrees, MaximumVisualSteeringDegrees);
            AppliedVisualSteeringDegrees = visualSteering;
            for (var index = 0; index < _steeringPivots.Count; index++)
            {
                var pivot = _steeringPivots[index];
                var vehicleUpInParent = pivot.parent.InverseTransformDirection(transform.up).normalized;
                pivot.localRotation = Quaternion.AngleAxis(visualSteering, vehicleUpInParent) *
                    _steeringBaseRotations[index];
            }
            _previousLocalPosition = next;
            _hasPreviousPosition = true;
        }

        private void ConfigureImportedHierarchy()
        {
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                var lower = child.name.ToLowerInvariant();
                if (lower.Contains("wheel_fl") || lower.Contains("wheel_fr") ||
                    lower.Contains("wheel_rl") || lower.Contains("wheel_rr"))
                    _rollingWheels.Add(child);
                if (lower.Contains("steer_fl") || lower.Contains("steer_fr"))
                {
                    _steeringPivots.Add(child);
                    _steeringBaseRotations.Add(child.localRotation);
                }
            }
            foreach (var renderer in VisualRoot.GetComponentsInChildren<Renderer>(true))
            {
                // The closed proxy below owns the authored vehicle silhouette.
                // Letting the open imported mesh cast as well creates a second,
                // overly long shadow beside the proxy.
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                ShadowCastingRendererCount++;
            }
        }

        private void ApplyPaintVariant(VehiclePaintVariant variant)
        {
            PaintVariant = variant;
            HasBlackRoof = variant is VehiclePaintVariant.Blue or VehiclePaintVariant.Red;
            var shader = Shader.Find("CityForgeV3/VehiclePaint");
            if (shader == null)
                throw new MissingReferenceException("Missing CityForge vehicle paint shader.");
            foreach (var renderer in VisualRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.name.ToLowerInvariant().Contains("body")) continue;
                var sourceMaterials = renderer.sharedMaterials;
                var paintedMaterials = new Material[sourceMaterials.Length];
                for (var index = 0; index < sourceMaterials.Length; index++)
                {
                    var source = sourceMaterials[index];
                    var painted = new Material(shader)
                    {
                        name = $"Model T {variant} Paint"
                    };
                    if (source != null)
                    {
                        painted.mainTexture = source.mainTexture;
                        CopyTexture(source, painted, "_BumpMap");
                        CopyTexture(source, painted, "_MetallicGlossMap");
                    }
                    painted.SetColor("_PaintColor", PaintColor(variant));
                    painted.SetFloat("_BlackRoof", HasBlackRoof ? 1f : 0f);
                    painted.SetFloat("_WorldMinY", renderer.bounds.min.y);
                    painted.SetFloat("_WorldMaxY", renderer.bounds.max.y);
                    paintedMaterials[index] = painted;
                }
                renderer.sharedMaterials = paintedMaterials;
            }
        }

        private static void CopyTexture(Material source, Material destination, string property)
        {
            if (source.HasProperty(property) && destination.HasProperty(property))
                destination.SetTexture(property, source.GetTexture(property));
        }

        private void PromoteVehicleAboveRoadDecals()
        {
            foreach (var renderer in VisualRoot.GetComponentsInChildren<Renderer>(true))
            {
                // Instance the imported materials so this vehicle alone draws
                // after its road-level shadow decal. The road is Geometry+2,
                // the decal Geometry+4, and the visible vehicle Geometry+10.
                var sourceMaterials = renderer.sharedMaterials;
                var materials = new Material[sourceMaterials.Length];
                for (var index = 0; index < sourceMaterials.Length; index++)
                {
                    var source = sourceMaterials[index];
                    materials[index] = source == null ? null : new Material(source)
                    {
                        name = $"{source.name} — Vehicle Instance",
                        renderQueue = 2460
                    };
                }
                renderer.sharedMaterials = materials;
            }
            if (FrontAxleBrace != null)
            {
                var renderer = FrontAxleBrace.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    renderer.sharedMaterial.renderQueue = 2460;
            }
        }

        private void BuildShadowPresentation()
        {
            var renderers = VisualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            var localCenter = transform.InverseTransformPoint(bounds.center);
            var width = bounds.size.x;
            var height = bounds.size.y;
            var length = bounds.size.z;

            ShadowProxyRoot = new GameObject("Model T Closed Shadow Proxy").transform;
            ShadowProxyRoot.SetParent(transform, false);
            ShadowProxyRoot.localScale = new Vector3(1f, ShadowProxyHeightScale, 1f);
            AddShadowVolume("Shadow Body", new Vector3(
                    localCenter.x,
                    localCenter.y - height * 0.16f,
                    localCenter.z + length * 0.03f),
                new Vector3(width * 0.78f, height * 0.34f, length * 0.82f));
            AddShadowVolume("Shadow Cabin", new Vector3(
                    localCenter.x,
                    localCenter.y + height * 0.13f,
                    localCenter.z - length * 0.17f),
                new Vector3(width * 0.70f, height * 0.43f, length * 0.38f));
            AddShadowVolume("Shadow Roof", new Vector3(
                    localCenter.x,
                    localCenter.y + height * 0.39f,
                    localCenter.z - length * 0.17f),
                new Vector3(width * 0.82f, height * 0.10f, length * 0.45f));

            // The closed body volumes prevent the open FBX chassis from casting
            // threadlike shadows. Preserve the four wheel lobes as well so the
            // projected shape still reads as this vehicle rather than a crate.
            foreach (var wheel in _rollingWheels)
            {
                var wheelRenderers = wheel.GetComponentsInChildren<Renderer>(true);
                if (wheelRenderers.Length == 0) continue;
                var wheelBounds = wheelRenderers[0].bounds;
                for (var index = 1; index < wheelRenderers.Length; index++)
                    wheelBounds.Encapsulate(wheelRenderers[index].bounds);
                AddWheelShadowVolume(
                    transform.InverseTransformPoint(wheelBounds.center),
                    wheelBounds.size);
            }

            var contact = GameObject.CreatePrimitive(PrimitiveType.Quad);
            contact.name = "Model T Soft Contact Shadow";
            contact.transform.SetParent(transform, false);
            // Keep the projected decal clearly above all road artwork while
            // ordinary depth testing still lets the vehicle obscure it.
            contact.transform.localPosition = new Vector3(localCenter.x, 0.14f, localCenter.z);
            contact.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            contact.transform.localScale = new Vector3(width * 0.72f, length * 0.72f, 1f);
            contact.GetComponent<Collider>().enabled = false;
            var contactShader = Shader.Find("CityForgeV3/VehicleContactShadow");
            if (contactShader == null)
                throw new MissingReferenceException("Missing CityForge vehicle contact shadow shader.");
            var contactRenderer = contact.GetComponent<Renderer>();
            contactRenderer.sharedMaterial = new Material(contactShader)
            {
                name = "Model T Soft Contact Shadow",
                // Road artwork is explicitly queued at 3002. The previous
                // 2454 queue drew this decal first, so the opaque road pass
                // simply painted over every vehicle contact shadow. Draw just
                // after the road while the car itself remains the final cover.
                renderQueue = 3100
            };
            contactRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            contactRenderer.receiveShadows = false;
            contactRenderer.sortingOrder = ContactShadowSortingOrder;
            ContactShadow = contact.transform;
        }

        private void AddShadowVolume(string name, Vector3 localPosition, Vector3 localScale)
        {
            var volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
            volume.name = name;
            volume.transform.SetParent(ShadowProxyRoot, false);
            volume.transform.localPosition = localPosition;
            volume.transform.localScale = localScale;
            volume.GetComponent<Collider>().enabled = false;
            var renderer = volume.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            renderer.receiveShadows = false;
            ShadowProxyRendererCount++;
        }

        private void AddWheelShadowVolume(Vector3 localPosition, Vector3 localScale)
        {
            var volume = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            volume.name = "Shadow Wheel";
            volume.transform.SetParent(ShadowProxyRoot, false);
            volume.transform.localPosition = localPosition;
            volume.transform.localScale = Vector3.Max(localScale, Vector3.one * 0.18f);
            volume.GetComponent<Collider>().enabled = false;
            var renderer = volume.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            renderer.receiveShadows = false;
            ShadowProxyRendererCount++;
        }

        private void GroundVisual()
        {
            var renderers = VisualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            VisualRoot.position += Vector3.up * (transform.position.y - bounds.min.y);
            _wheelRadiusMeters = Mathf.Clamp(bounds.size.y * 0.22f, 0.28f, 0.55f);
        }

        private void BuildFrontAxleBrace()
        {
            if (_steeringPivots.Count != 2) return;
            var leftRenderers = _steeringPivots[0].GetComponentsInChildren<Renderer>(true);
            var rightRenderers = _steeringPivots[1].GetComponentsInChildren<Renderer>(true);
            if (leftRenderers.Length == 0 || rightRenderers.Length == 0) return;
            // FBX empties can collapse to the same imported transform position;
            // renderer bounds retain the actual separated wheel centers.
            var left = transform.InverseTransformPoint(leftRenderers[0].bounds.center);
            var right = transform.InverseTransformPoint(rightRenderers[0].bounds.center);
            var span = right - left;
            if (span.sqrMagnitude < 0.001f) return;

            var axle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            axle.name = "CF Model T Front Axle Brace";
            axle.transform.SetParent(transform, false);
            axle.transform.localPosition = (left + right) * 0.5f;
            axle.transform.localRotation = Quaternion.FromToRotation(Vector3.up, span.normalized);
            axle.transform.localScale = new Vector3(0.045f, span.magnitude * 0.5f, 0.045f);
            axle.GetComponent<Collider>().enabled = false;
            var shader = Shader.Find("Standard");
            var material = new Material(shader) { name = "CF Model T Axle Metal" };
            material.color = new Color(0.055f, 0.06f, 0.062f);
            material.SetFloat("_Metallic", 0.7f);
            material.SetFloat("_Glossiness", 0.32f);
            axle.GetComponent<Renderer>().sharedMaterial = material;
            FrontAxleBrace = axle.transform;
        }

        private void BuildHeadlights()
        {
            foreach (var lateral in new[] { -0.48f, 0.48f })
            {
                var lightObject = new GameObject(lateral < 0f
                    ? "Model T Headlight Left"
                    : "Model T Headlight Right");
                lightObject.transform.SetParent(transform, false);
                lightObject.transform.localPosition = new Vector3(lateral, 0.72f, 1.55f);
                lightObject.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);
                var headlight = lightObject.AddComponent<Light>();
                headlight.type = LightType.Spot;
                headlight.color = new Color(1f, 0.82f, 0.55f);
                headlight.range = 14f;
                headlight.spotAngle = 36f;
                headlight.innerSpotAngle = 20f;
                headlight.shadows = LightShadows.Soft;
                headlight.shadowStrength = 0.55f;
                headlight.shadowBias = 0.04f;
                headlight.shadowNormalBias = 0.35f;
                headlight.renderMode = LightRenderMode.ForcePixel;
                _headlights.Add(headlight);

                var lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lens.name = lateral < 0f ? "Model T Lamp Glow Left" : "Model T Lamp Glow Right";
                lens.transform.SetParent(lightObject.transform, false);
                lens.transform.localPosition = new Vector3(0f, 0f, 0.04f);
                lens.transform.localScale = Vector3.one * 0.13f;
                lens.GetComponent<Collider>().enabled = false;
                var material = new Material(Shader.Find("Standard"))
                {
                    name = "Model T Warm Lamp Lens",
                    color = new Color(1f, 0.72f, 0.32f)
                };
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1f, 0.58f, 0.18f) * 2.2f);
                var renderer = lens.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _headlightLenses.Add(renderer);
            }
        }
    }
}
