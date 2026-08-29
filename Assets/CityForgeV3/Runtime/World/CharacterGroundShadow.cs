using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    /// <summary>
    /// A character-owned directional ground shadow. It follows the presentation
    /// root instead of relying on a skinned renderer's animated shadow bounds,
    /// which can drift away from a root-motion-suppressed character.
    /// </summary>
    public sealed class CharacterGroundShadow : MonoBehaviour
    {
        private Transform _shadow;
        private Material _material;
        private TimeOfDayPreset _preset;
        private Vector3 _sunRay;
        private bool _visible = true;

        public void Initialize()
        {
            var child = new GameObject("CF Character Anchored Ground Shadow");
            _shadow = child.transform;
            _shadow.SetParent(transform, false);
            var filter = child.AddComponent<MeshFilter>();
            filter.sharedMesh = BuildFeatheredEllipse();
            var renderer = child.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            _material = new Material(shader)
            {
                name = "CF Character Anchored Shadow Material",
                color = new Color(0.055f, 0.065f, 0.08f, 0.42f),
                renderQueue = 2448
            };
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public void SetLighting(TimeOfDayPreset preset, bool visible)
        {
            SetLighting(preset, visible,
                TimeOfDayLighting.SunRotation(preset) * Vector3.forward);
        }

        public void SetLighting(TimeOfDayPreset preset, bool visible,
            Vector3 sunRay)
        {
            _preset = preset;
            _visible = visible;
            _sunRay = sunRay;
        }

        private void LateUpdate()
        {
            if (_shadow == null) return;
            var ray = _sunRay.sqrMagnitude > 0.0001f
                ? _sunRay.normalized
                : TimeOfDayLighting.SunRotation(_preset) * Vector3.forward;
            var horizontal = new Vector3(ray.x, 0f, ray.z);
            var active = _visible && ray.y < -0.01f &&
                horizontal.sqrMagnitude > 0.0001f;
            _shadow.gameObject.SetActive(active);
            if (!active) return;
            horizontal.Normalize();
            var length = Mathf.Clamp((-1.45f / ray.y) *
                LotWorldController.PropShadowLengthScale(_preset), 0.55f, 3.4f);
            _shadow.position = transform.position + Vector3.up * 0.012f;
            _shadow.rotation = Quaternion.LookRotation(horizontal, Vector3.up);
            _shadow.localScale = new Vector3(1f, 1f, length);
        }

        public static Vector2 Direction(TimeOfDayPreset preset)
        {
            var ray = TimeOfDayLighting.SunRotation(preset) * Vector3.forward;
            return Direction(ray);
        }

        public static Vector2 Direction(Vector3 sunRay) =>
            new Vector2(sunRay.x, sunRay.z).normalized;

        private static Mesh BuildFeatheredEllipse()
        {
            const int segments = 28;
            var vertices = new Vector3[segments + 1];
            var colors = new Color[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = new Vector3(0f, 0f, 0.5f);
            colors[0] = new Color(1f, 1f, 1f, 0.72f);
            for (var index = 0; index < segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                vertices[index + 1] = new Vector3(
                    Mathf.Cos(angle) * 0.28f, 0f,
                    0.5f + Mathf.Sin(angle) * 0.5f);
                colors[index + 1] = new Color(1f, 1f, 1f, 0f);
                var triangle = index * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = (index + 1) % segments + 1;
                triangles[triangle + 2] = index + 1;
            }
            var mesh = new Mesh { name = "CF Character Feathered Shadow Mesh" };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_material == null) return;
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }
    }
}
