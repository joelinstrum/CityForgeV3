using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.Buildings3D
{
    [DisallowMultipleComponent]
    public sealed class Building3DPackageInstance : MonoBehaviour
    {
        [SerializeField] private Building3DPackage package;
        [SerializeField] private LODGroup lodGroup;
        [SerializeField] private Transform representationRoot;
        [SerializeField] private GameObject nightLightingRoot;
        private BuildingNightLighting[] buildingNightLighting =
            System.Array.Empty<BuildingNightLighting>();

        public Building3DPackage Package => package;
        public LODGroup LodGroup => lodGroup;

        public void Configure(Building3DPackage value)
        {
            package = value;
            Rebuild();
        }

        [ContextMenu("Rebuild Package Instance")]
        public void Rebuild()
        {
            ClearGeneratedChildren();
            if (package == null) return;

            representationRoot = new GameObject("Representations").transform;
            representationRoot.SetParent(transform, false);
            representationRoot.localPosition = package.PivotOffset;
            representationRoot.localScale = package.AuthoredScale;

            lodGroup = gameObject.GetComponent<LODGroup>();
            if (lodGroup == null)
                lodGroup = gameObject.AddComponent<LODGroup>();
            lodGroup.fadeMode = package.UseCrossFade
                ? LODFadeMode.CrossFade : LODFadeMode.None;
            lodGroup.animateCrossFading = false;

            var lods = new List<LOD>();
            if (package.Representations != null)
            foreach (var representation in package.Representations)
            {
                if (representation == null || representation.VisualPrefab == null)
                    continue;
                var levelRoot = Instantiate(representation.VisualPrefab,
                    representationRoot);
                levelRoot.name = representation.Level.ToString();
                levelRoot.transform.localPosition = representation.LocalPosition;
                levelRoot.transform.localRotation = Quaternion.Euler(
                    representation.LocalEulerAngles);
                levelRoot.transform.localScale = representation.LocalScale;

                var renderers = new List<Renderer>(
                    levelRoot.GetComponentsInChildren<Renderer>(true));
                if (representation.OverrideMaterial != null)
                    foreach (var renderer in renderers)
                    {
                        var count = Mathf.Max(1, renderer.sharedMaterials.Length);
                        var materials = new Material[count];
                        for (var index = 0; index < count; index++)
                            materials[index] = representation.OverrideMaterial;
                        renderer.sharedMaterials = materials;
                    }
                if (representation.Level == Building3DLevel.LOD5Billboard)
                {
                    var angleCount = representation.BillboardAngleCount > 0
                        ? representation.BillboardAngleCount : 8;
                    if (renderers.Count == angleCount)
                    {
                        var selector = levelRoot.GetComponent<
                            EightAngleBuildingBillboard>();
                        if (selector == null)
                            selector = levelRoot.AddComponent<
                                EightAngleBuildingBillboard>();
                        selector.Configure(renderers.ToArray(),
                            representation.BillboardYawOffset);
                    }
                    else
                    {
                        Debug.LogWarning($"{package.AssetId} LOD5 requires " +
                            $"{angleCount} billboard renderers; found " +
                            $"{renderers.Count}.", this);
                    }
                }
                if (representation.ShadowPrefab != null)
                {
                    foreach (var renderer in renderers)
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                    var shadow = Instantiate(representation.ShadowPrefab, levelRoot.transform);
                    shadow.name = "ShadowLOD";
                    shadow.transform.localPosition = Vector3.zero;
                    shadow.transform.localRotation = Quaternion.identity;
                    shadow.transform.localScale = Vector3.one;
                    foreach (var renderer in shadow.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                        renderer.receiveShadows = false;
                        renderers.Add(renderer);
                    }
                }
                lods.Add(new LOD(representation.ScreenRelativeHeight,
                    renderers.ToArray())
                {
                    fadeTransitionWidth = package.CrossFadeWidth
                });
            }

            if (package.CollisionPrefab != null)
            {
                var collision = Instantiate(package.CollisionPrefab, transform);
                collision.name = "Collision";
            }
            if (package.NightLightingPrefab != null)
            {
                nightLightingRoot = Instantiate(package.NightLightingPrefab,
                    representationRoot);
                nightLightingRoot.name = "Night Lighting Overlay";
                nightLightingRoot.transform.localPosition =
                    package.NightLightingLocalPosition;
                nightLightingRoot.transform.localRotation = Quaternion.Euler(
                    package.NightLightingLocalEulerAngles);
                nightLightingRoot.transform.localScale =
                    package.NightLightingLocalScale;
                if (package.NightLightingMaterial != null)
                    foreach (var renderer in nightLightingRoot
                                 .GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.sharedMaterial = package.NightLightingMaterial;
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        renderer.receiveShadows = false;
                    }
                SetNightLighting(false);
            }
            lodGroup.SetLODs(lods.ToArray());
            lodGroup.RecalculateBounds();
            buildingNightLighting = GetComponentsInChildren<BuildingNightLighting>(true);
            foreach (var lighting in buildingNightLighting)
                if (lighting != null) lighting.RefreshRuntimeBindings();
        }

        public void SetNightLighting(bool enabled)
        {
            if (nightLightingRoot != null)
                nightLightingRoot.SetActive(enabled);
            SetNightAmount(enabled ? 1f : 0f);
        }

        public void SetNightAmount(float value)
        {
            if (buildingNightLighting == null ||
                buildingNightLighting.Length == 0)
                buildingNightLighting =
                    GetComponentsInChildren<BuildingNightLighting>(true);
            foreach (var lighting in buildingNightLighting)
                if (lighting != null) lighting.SetNightAmount(value);
        }

        private void ClearGeneratedChildren()
        {
            var children = new List<GameObject>();
            foreach (Transform child in transform)
                if (child.name == "Representations" || child.name == "Collision")
                    children.Add(child.gameObject);
            foreach (var child in children)
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
        }
    }
}
