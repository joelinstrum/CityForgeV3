using UnityEngine;

namespace CityForgeV3.World
{
    public sealed class BuildingPropDoorMotion : MonoBehaviour
    {
        private Transform _hinge;
        private Quaternion _closedRotation;
        private float _openAngleDegrees;
        private float _targetAmount;
        private float _amount;

        public void Configure(string transformName, float openAngleDegrees,
            bool open)
        {
            _hinge = FindDescendant(transform, transformName);
            _openAngleDegrees = openAngleDegrees;
            _targetAmount = _amount = open ? 1f : 0f;
            if (_hinge == null) return;
            _closedRotation = _hinge.localRotation;
            ApplyRotation();
        }

        public void SetOpen(bool open)
        {
            _targetAmount = open ? 1f : 0f;
        }

        private void Update()
        {
            if (_hinge == null) return;
            _amount = Mathf.MoveTowards(_amount, _targetAmount,
                Time.deltaTime * 2.5f);
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            var eased = Mathf.SmoothStep(0f, 1f, _amount);
            _hinge.localRotation = _closedRotation *
                Quaternion.AngleAxis(_openAngleDegrees * eased, Vector3.up);
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
