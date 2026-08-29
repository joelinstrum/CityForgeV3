using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.Buildings3D
{
    public enum Building3DLevel
    {
        LOD0,
        LOD1,
        LOD2,
        LOD3,
        LOD4Impostor
    }

    [Serializable]
    public sealed class Building3DRepresentation
    {
        public Building3DLevel Level;
        [Range(0.001f, 1f)] public float ScreenRelativeHeight = 0.1f;
        public GameObject VisualPrefab;
        public Material OverrideMaterial;
        public GameObject ShadowPrefab;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale = Vector3.one;
        [Min(0)] public int TargetTriangleBudget;
        [TextArea] public string Provenance;
    }

    [CreateAssetMenu(fileName = "Building3DPackage",
        menuName = "City Forge/3D Building Package")]
    public sealed class Building3DPackage : ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;

        [Min(1)] public int SchemaVersion = CurrentSchemaVersion;
        public string AssetId;
        [TextArea] public string SourceProvenance;
        public Vector3 AuthoredScale = Vector3.one;
        public Vector3 PivotOffset;
        public float FrontYawDegrees = 90f;
        public Vector2 FootprintMeters;
        [Tooltip("Maximum normalized difference allowed between representation bounds.")]
        [Range(0f, 0.25f)] public float BoundsTolerance = 0.05f;
        public bool UseCrossFade = true;
        [Range(0f, 1f)] public float CrossFadeWidth = 0.1f;
        public bool KeepShadowMeshWithImpostor = true;
        public GameObject CollisionPrefab;
        [Tooltip("Optional lightweight mesh that remains active across every LOD.")]
        public GameObject NightLightingPrefab;
        public Material NightLightingMaterial;
        public Vector3 NightLightingLocalPosition;
        public Vector3 NightLightingLocalEulerAngles;
        public Vector3 NightLightingLocalScale = Vector3.one;
        public List<Building3DRepresentation> Representations = new();

        public Building3DRepresentation Find(Building3DLevel level) =>
            Representations?.Find(entry => entry != null && entry.Level == level);
    }
}
