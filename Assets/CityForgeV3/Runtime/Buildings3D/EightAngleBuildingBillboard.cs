using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.Buildings3D
{
    [DisallowMultipleComponent]
    public sealed class EightAngleBuildingBillboard : MonoBehaviour
    {
        [SerializeField] private Renderer[] angleRenderers = Array.Empty<Renderer>();
        [SerializeField] private float yawOffset;
        [SerializeField] private Camera targetCamera;

        private int activeIndex = -1;

        public int ActiveIndex => activeIndex;

        public void Configure(Renderer[] renderers, float offset)
        {
            angleRenderers = renderers ?? Array.Empty<Renderer>();
            yawOffset = offset;
            Refresh(true);
        }

        private void LateUpdate() => Refresh(false);

        private void OnDisable()
        {
            foreach (var item in angleRenderers)
                if (item != null) item.forceRenderingOff = false;
            activeIndex = -1;
        }

        private void Refresh(bool force)
        {
            if (angleRenderers == null || angleRenderers.Length == 0) return;
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            var direction = targetCamera.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            var localDirection = transform.parent != null
                ? transform.parent.InverseTransformDirection(direction)
                : direction;
            var index = CalculateAngleIndex(localDirection, angleRenderers.Length,
                yawOffset);
            if (force || index != activeIndex)
            {
                activeIndex = index;
                for (var i = 0; i < angleRenderers.Length; i++)
                    if (angleRenderers[i] != null)
                        angleRenderers[i].forceRenderingOff = i != activeIndex;
            }

            var facing = Quaternion.LookRotation(direction.normalized, Vector3.up);
            foreach (var item in angleRenderers)
                if (item != null) item.transform.rotation = facing;
        }

        public static int CalculateAngleIndex(Vector3 localCameraDirection,
            int angleCount = 8, float yawOffset = 0f)
        {
            if (angleCount <= 0) return 0;
            var angle = Mathf.Atan2(localCameraDirection.x,
                localCameraDirection.z) * Mathf.Rad2Deg - yawOffset;
            var step = 360f / angleCount;
            return ((Mathf.RoundToInt(angle / step) % angleCount) + angleCount)
                % angleCount;
        }
    }
}
