using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    /// <summary>
    /// Stable directional shadow for moving street vehicles, rendered after
    /// road and track artwork and driven by the same world sun as buildings.
    /// </summary>
    public sealed class StreetVehicleGroundShadow : MonoBehaviour
    {
        public const int RenderQueue = 3150;
        public const float MaximumDirectionalTailMeters = 1.65f;
        public const float FootprintWidthScale = 1.04f;
        private Transform _projection;
        private Material _material;
        private Vector3 _sunRay;
        private float _width;
        private float _length;
        private float _height;
        private bool _visible;

        public void Initialize(Transform visualRoot)
        {
            var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            _width = Mathf.Max(0.6f, Mathf.Min(bounds.size.x, bounds.size.z));
            _length = Mathf.Max(1.2f, Mathf.Max(bounds.size.x, bounds.size.z));
            _height = Mathf.Max(0.8f, bounds.size.y);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "CF Directional Street Vehicle Shadow";
            _projection = quad.transform;
            _projection.SetParent(transform, true);
            quad.GetComponent<Collider>().enabled = false;
            var shader = Shader.Find("CityForgeV3/VehicleContactShadow");
            _material = new Material(shader)
            {
                name = "CF Directional Street Vehicle Shadow",
                renderQueue = RenderQueue
            };
            _material.SetColor("_Color",
                new Color(0.018f, 0.022f, 0.027f, 0.34f));
            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 2200;
        }

        public void SetLighting(Vector3 sunRay, bool visible)
        {
            _sunRay = sunRay.normalized;
            _visible = visible;
        }

        private void LateUpdate()
        {
            if (_projection == null) return;
            var horizontal = new Vector3(_sunRay.x, 0f, _sunRay.z);
            var active = _visible && _sunRay.y < -0.01f &&
                horizontal.sqrMagnitude > 0.0001f;
            _projection.gameObject.SetActive(active);
            if (!active) return;
            horizontal.Normalize();
            // Street vehicles need a readable footprint, not the full
            // building-height projection used for architectural shadows. A
            // short capped tail suggests direction while wheels/body remain
            // recognizable as one broad vehicle-shaped shadow.
            var projectedLength = Mathf.Clamp(
                -(_height * 0.42f) / _sunRay.y, 0.30f,
                MaximumDirectionalTailMeters);
            var totalLength = _length * 0.94f + projectedLength * 0.55f;
            _projection.position = new Vector3(transform.position.x, 0.145f,
                transform.position.z) + horizontal * (projectedLength * 0.24f);
            var yaw = Mathf.Atan2(horizontal.x, horizontal.z) * Mathf.Rad2Deg;
            _projection.rotation = Quaternion.Euler(90f, yaw, 0f);
            _projection.localScale = new Vector3(
                _width * FootprintWidthScale, totalLength, 1f);
        }

        private void OnDestroy()
        {
            if (_material == null) return;
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }
    }
}
