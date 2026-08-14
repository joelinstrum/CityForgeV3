using UnityEngine;

namespace CityForgeV3.World
{
    /// <summary>Generic playback for catalog-defined pendulum attachments.</summary>
    public sealed class BuildingPropSwingMotion : MonoBehaviour
    {
        private Transform _swingTransform;
        private Quaternion _restRotation;
        private float _amplitudeDegrees;
        private float _periodSeconds;
        private float _phase;

        public void Configure(string transformName, float amplitudeDegrees,
            float periodSeconds, float phase)
        {
            _swingTransform = FindDescendant(transform, transformName);
            _amplitudeDegrees = amplitudeDegrees;
            _periodSeconds = Mathf.Max(0.1f, periodSeconds);
            _phase = Mathf.Repeat(phase, 1f);
            if (_swingTransform != null)
                _restRotation = _swingTransform.localRotation;
        }

        private void Update()
        {
            if (_swingTransform == null || _amplitudeDegrees <= 0f) return;
            var cycle = Time.time / _periodSeconds + _phase;
            var angle = Mathf.Sin(cycle * Mathf.PI * 2f) * _amplitudeDegrees;
            _swingTransform.localRotation =
                _restRotation * Quaternion.AngleAxis(angle, Vector3.right);
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name)) return null;
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name) return item;
            return null;
        }
    }
}
