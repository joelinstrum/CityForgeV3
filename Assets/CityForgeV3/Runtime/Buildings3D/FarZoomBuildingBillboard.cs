using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.Buildings3D
{
    // One quad per distant building; textures and material are shared across instances.
    public sealed class FarZoomBuildingBillboard : MonoBehaviour
    {
        private static readonly Dictionary<string, Texture2D[]> Views = new();
        private static Material sharedMaterial;
        private MaterialPropertyBlock properties;
        private Renderer[] modelRenderers = Array.Empty<Renderer>();
        private MeshRenderer quadRenderer;
        private Transform quad;
        private Texture2D[] views;
        private Camera targetCamera;
        private float pixelsPerMeter;
        private float yawOffset;
        private bool far;
        private int activeAngle = -1;
        private float activeHeight;
        private Vector3 lastRootPosition;
        private Vector3 lastDirection;

        public bool IsFar => far;

        public void SetCamera(Camera camera)
        {
            targetCamera = camera;
            if (far) Refresh(true);
        }

        public bool Configure(string resourceRoot, float imagePixelsPerMeter,
            float billboardYawOffset, Camera camera)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot) ||
                imagePixelsPerMeter <= 0f) return false;
            if (!Views.TryGetValue(resourceRoot, out views))
            {
                views = new Texture2D[8];
                for (var index = 0; index < views.Length; index++)
                {
                    views[index] = Resources.Load<Texture2D>(
                        resourceRoot + "/angle-" + index);
                    if (views[index] == null) return false;
                }
                Views.Add(resourceRoot, views);
            }
            targetCamera = camera;
            properties = new MaterialPropertyBlock();
            pixelsPerMeter = imagePixelsPerMeter;
            yawOffset = billboardYawOffset;
            modelRenderers = GetComponentsInChildren<Renderer>(true);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = "Far Building Billboard";
            plane.transform.SetParent(transform, false);
            quad = plane.transform;
            var collider = plane.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            quadRenderer = plane.GetComponent<MeshRenderer>();
            if (sharedMaterial == null)
            {
                var shader = Shader.Find("Unlit/Transparent Cutout") ??
                             Shader.Find("Unlit/Transparent");
                if (shader == null) return false;
                sharedMaterial = new Material(shader)
                { name = "City Forge Far Building Billboard" };
            }
            quadRenderer.sharedMaterial = sharedMaterial;
            quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;
            SetFar(false);
            return true;
        }

        public void SetFar(bool value)
        {
            far = value;
            foreach (var renderer in modelRenderers)
                if (renderer != null) renderer.enabled = !far;
            if (quadRenderer != null) quadRenderer.enabled = far;
            if (far) Refresh(true);
        }

        private void LateUpdate()
        {
            if (far) Refresh(false);
        }

        private void Refresh(bool force)
        {
            if (targetCamera == null || quad == null || views == null) return;
            // Orthographic rays are parallel. Using the camera's position
            // makes two buildings in the same view choose different facings.
            var direction = targetCamera.orthographic
                ? -targetCamera.transform.forward
                : targetCamera.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            // The imported FBX root is pitched upright. Measure compass yaw
            // in its unpitched lot parent, then apply the placed building turn.
            var localDirection = transform.parent != null
                ? transform.parent.InverseTransformDirection(direction)
                : direction;
            var angle = -Mathf.Atan2(localDirection.x, localDirection.z) *
                        Mathf.Rad2Deg + yawOffset;
            var index = ((Mathf.RoundToInt(angle / 45f) % 8) + 8) % 8;
            var changedAngle = index != activeAngle;
            if (force || changedAngle)
            {
                activeAngle = index;
                var image = views[index];
                var width = image.width / pixelsPerMeter;
                var height = image.height / pixelsPerMeter;
                activeHeight = height;
                var rootScale = Mathf.Max(0.0001f, transform.lossyScale.x);
                quad.localScale = new Vector3(width / rootScale,
                    height / rootScale, 1f / rootScale);
                properties.SetTexture("_MainTex", image);
                quadRenderer.SetPropertyBlock(properties);
            }
            if (force || changedAngle || transform.position != lastRootPosition)
            {
                lastRootPosition = transform.position;
                quad.position = lastRootPosition + Vector3.up * (activeHeight * .5f);
            }
            if (force || direction != lastDirection)
            {
                lastDirection = direction;
                // Unity's built-in Quad faces local -Z.
                quad.rotation = Quaternion.LookRotation(-direction.normalized,
                    Vector3.up);
            }
        }
    }
}
