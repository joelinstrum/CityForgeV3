using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class HybridBuildingPackageManifest
    {
        public string schema;
        public string id;
        public string displayName;
        public string shortDisplayName;
        public string category;
        public string sizeClass;
        public string libraryShortcut;
        public string reviewStatus;
        public int occupancyWidth;
        public int occupancyDepth;
        public HybridSpatialManifest spatial;
        public HybridCameraManifest camera;
        public HybridPrimitiveManifest primitive;
        public HybridRenderManifest render;
        public HybridPlanManifest plan;
        public HybridShadowManifest shadow;
        public HybridFacingManifest[] facings;
    }

    [Serializable]
    public sealed class HybridSpatialManifest
    {
        public string coordinateSystem;
        public string originPolicy;
        public float widthMeters;
        public float depthMeters;
        public float heightMeters;
        public float placementScale;
        public int frontFacingQuarterTurns;
        public string artworkRotationDirection;
        public float[] rotationAnchor;
    }

    [Serializable]
    public sealed class HybridCameraManifest
    {
        public float elevationDegrees;
        public float radiusMeters;
        public float targetHeightMeters;
    }

    [Serializable]
    public sealed class HybridPrimitiveManifest
    {
        public string resourcePath;
        public string sourceVersion;
        public string roofRidgeAxis;
        public float entranceFacingDegrees;
        public string[] requiredObjects;
    }

    [Serializable]
    public sealed class HybridRenderManifest
    {
        public float pixelsPerMeter;
        public int canvasWidth;
        public int canvasHeight;
        public float morningShadeOpacity;
        public float noonShadeOpacity;
        public float afternoonShadeOpacity;
        public float eveningShadeOpacity;
    }

    [Serializable]
    public sealed class HybridPlanManifest
    {
        public string resourcePath;
        public float[] pivot;
    }

    [Serializable]
    public sealed class HybridShadowManifest
    {
        public float footprintScale;
        public float maximumProjectionMeters;
        public float morningLengthScale;
        public float noonLengthScale;
        public float afternoonLengthScale;
        public float eveningLengthScale;
        public float opacityMultiplier;
        public float directionOffsetDegrees;
        public float[] semanticVertices;
    }

    [Serializable]
    public sealed class HybridFacingManifest
    {
        public string id;
        public float cameraAzimuthDegrees;
        public float[] pivotTopOrigin;
        public string approvedResourcePath;
        public string neutralResourcePath;
        public string nightOverlayResourcePath;
        public string nightFullResourcePath;
        public string morningShadeResourcePath;
        public string noonShadeResourcePath;
        public string afternoonShadeResourcePath;
        public string eveningShadeResourcePath;
        public string winterResourcePath;
    }

    public sealed class HybridBuildingPackage
    {
        public const string Schema = "cityforge-v3-hybrid-building-package-v1";
        private readonly HybridBuildingPackageManifest _manifest;
        private readonly string _resourcePath;

        private HybridBuildingPackage(
            HybridBuildingPackageManifest manifest,
            string resourcePath)
        {
            _manifest = manifest;
            _resourcePath = resourcePath;
        }

        public string Id => _manifest.id;
        public string ResourcePath => _resourcePath;
        public string DisplayName => _manifest.displayName;
        public string ShortDisplayName => string.IsNullOrWhiteSpace(
            _manifest.shortDisplayName) ? DisplayName : _manifest.shortDisplayName;
        public string Category => _manifest.category;
        public string SizeClass => _manifest.sizeClass;
        public string LibraryShortcut => _manifest.libraryShortcut;
        public string ReviewStatus => _manifest.reviewStatus;
        public int OccupancyWidth => Mathf.Max(1, _manifest.occupancyWidth);
        public int OccupancyDepth => Mathf.Max(1, _manifest.occupancyDepth);
        public float WidthMeters => _manifest.spatial.widthMeters;
        public float DepthMeters => _manifest.spatial.depthMeters;
        public float HeightMeters => _manifest.spatial.heightMeters;
        public float PlacementScale => _manifest.spatial.placementScale;
        public int FrontFacingQuarterTurns =>
            WrapFacing(_manifest.spatial.frontFacingQuarterTurns);
        public int ArtworkRotationStep =>
            _manifest.spatial.artworkRotationDirection == "with-building" ? 1 : -1;
        public Vector3 RotationAnchor => Vector3From(_manifest.spatial.rotationAnchor);
        public Vector3 PresentationAnchor => RotationAnchor;
        public float CameraElevationDegrees => _manifest.camera.elevationDegrees;
        public float CameraRadiusMeters => _manifest.camera.radiusMeters;
        public float CameraTargetHeightMeters => _manifest.camera.targetHeightMeters;
        public float PixelsPerMeter => _manifest.render.pixelsPerMeter;
        public int CanvasWidth => _manifest.render.canvasWidth;
        public int CanvasHeight => _manifest.render.canvasHeight;
        public float ShadeOpacity(
            TimeOfDayPreset preset, float fallback) => Mathf.Clamp01(
            PositiveOr(preset switch
            {
                TimeOfDayPreset.Morning => _manifest.render.morningShadeOpacity,
                TimeOfDayPreset.Noon => _manifest.render.noonShadeOpacity,
                TimeOfDayPreset.Afternoon => _manifest.render.afternoonShadeOpacity,
                TimeOfDayPreset.Evening => _manifest.render.eveningShadeOpacity,
                _ => 0f
            }, fallback));
        public string PrimitiveResourcePath => _manifest.primitive.resourcePath;
        public string PrimitiveSourceVersion => _manifest.primitive.sourceVersion;
        public string RoofRidgeAxis => _manifest.primitive.roofRidgeAxis;
        public float EntranceFacingDegrees =>
            _manifest.primitive.entranceFacingDegrees;
        public string PlanResourcePath => _manifest.plan?.resourcePath;
        public float ShadowFootprintScale => PositiveOr(
            _manifest.shadow?.footprintScale ?? 0f, 1.10f);
        public float MaximumShadowProjectionMeters => Mathf.Max(
            0f, _manifest.shadow?.maximumProjectionMeters ?? 0f);
        public float ShadowOpacityMultiplier => PositiveOr(
            _manifest.shadow?.opacityMultiplier ?? 0f, 1.45f);
        public float ShadowDirectionOffsetDegrees =>
            _manifest.shadow?.directionOffsetDegrees ?? 0f;
        public IReadOnlyList<float> ShadowSemanticVertices =>
            _manifest.shadow?.semanticVertices ?? Array.Empty<float>();
        public IReadOnlyList<string> RequiredPrimitiveObjects =>
            _manifest.primitive.requiredObjects;
        public int FacingCount => _manifest.facings.Length;
        public string CatalogThumbnailResourcePath
        {
            get
            {
                var preferred = _manifest.facings[0];
                foreach (var facing in _manifest.facings)
                    if (facing.id == "front-right") preferred = facing;

                return string.IsNullOrWhiteSpace(preferred.neutralResourcePath)
                    ? preferred.approvedResourcePath
                    : preferred.neutralResourcePath;
            }
        }

        public float ShadowLengthScale(TimeOfDayPreset preset) => preset switch
        {
            TimeOfDayPreset.Morning => PositiveOr(
                _manifest.shadow?.morningLengthScale ?? 0f, 0.90f),
            TimeOfDayPreset.Noon => PositiveOr(
                _manifest.shadow?.noonLengthScale ?? 0f, 0.45f),
            TimeOfDayPreset.Afternoon => PositiveOr(
                _manifest.shadow?.afternoonLengthScale ?? 0f, 1.15f),
            TimeOfDayPreset.Evening => PositiveOr(
                _manifest.shadow?.eveningLengthScale ?? 0f, 0.65f),
            _ => 0.45f
        };

        private static float PositiveOr(float value, float fallback) =>
            value > 0f ? value : fallback;

        public HybridFacingSpec Facing(int index)
        {
            var facing = _manifest.facings[WrapFacing(index)];
            return new HybridFacingSpec(
                facing.id,
                facing.approvedResourcePath,
                facing.neutralResourcePath,
                facing.nightOverlayResourcePath,
                facing.nightFullResourcePath,
                facing.morningShadeResourcePath,
                facing.noonShadeResourcePath,
                facing.afternoonShadeResourcePath,
                facing.eveningShadeResourcePath,
                facing.winterResourcePath,
                facing.cameraAzimuthDegrees,
                Vector2From(facing.pivotTopOrigin));
        }

        public int WrapFacing(int index) =>
            (index % FacingCount + FacingCount) % FacingCount;

        public int PresentationFacing(int cameraFacing, int buildingRotation) =>
            WrapFacing(cameraFacing + ArtworkRotationStep * buildingRotation);

        public static HybridBuildingPackage Load(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                throw new MissingReferenceException(
                    $"Missing hybrid building package: {resourcePath}");
            }

            var manifest =
                JsonUtility.FromJson<HybridBuildingPackageManifest>(asset.text);
            var issues = Validate(manifest);
            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Invalid hybrid building package {resourcePath}: " +
                    string.Join("; ", issues));
            }

            return new HybridBuildingPackage(manifest, resourcePath);
        }

        public static IReadOnlyList<string> Validate(
            HybridBuildingPackageManifest manifest)
        {
            var issues = new List<string>();
            if (manifest == null)
            {
                issues.Add("manifest is null");
                return issues;
            }

            if (manifest.schema != Schema) issues.Add("unsupported schema");
            if (string.IsNullOrWhiteSpace(manifest.id)) issues.Add("id is required");
            if (string.IsNullOrWhiteSpace(manifest.displayName)) issues.Add("displayName is required");
            if (manifest.occupancyWidth < 1 || manifest.occupancyDepth < 1)
                issues.Add("occupancy must be positive");
            if (manifest.spatial == null) issues.Add("spatial is required");
            if (manifest.camera == null) issues.Add("camera is required");
            if (manifest.primitive == null) issues.Add("primitive is required");
            if (manifest.render == null) issues.Add("render is required");
            if (manifest.facings == null || manifest.facings.Length != 4)
                issues.Add("exactly four facings are required");

            if (manifest.spatial != null)
            {
                if (manifest.spatial.coordinateSystem !=
                    "blender-metric-origin-centered")
                    issues.Add("coordinate system must be Blender metric centered");
                if (manifest.spatial.originPolicy != "foundation-center-ground")
                    issues.Add("origin must be foundation center ground");
                if (!Mathf.Approximately(manifest.spatial.placementScale, 1f))
                    issues.Add("placementScale must equal 1");
                if (!IsVector3(manifest.spatial.rotationAnchor))
                    issues.Add("rotationAnchor must contain three values");
                if (manifest.spatial.artworkRotationDirection != "with-building" &&
                    manifest.spatial.artworkRotationDirection != "against-building")
                    issues.Add("artworkRotationDirection must be with-building or against-building");
            }

            if (manifest.primitive != null &&
                string.IsNullOrWhiteSpace(manifest.primitive.resourcePath))
                issues.Add("primitive resource path is required");
            if (manifest.primitive != null &&
                manifest.primitive.roofRidgeAxis != "x" &&
                manifest.primitive.roofRidgeAxis != "z" &&
                manifest.primitive.roofRidgeAxis != "none")
                issues.Add("roofRidgeAxis must be x, z, or none");

            if (manifest.facings != null)
            {
                var ids = new HashSet<string>();
                foreach (var facing in manifest.facings)
                {
                    if (facing == null)
                    {
                        issues.Add("facing is null");
                        continue;
                    }

                    if (!ids.Add(facing.id)) issues.Add($"duplicate facing {facing.id}");
                    if (!IsVector2(facing.pivotTopOrigin))
                        issues.Add($"{facing.id} pivot must contain two values");
                    if (string.IsNullOrWhiteSpace(facing.approvedResourcePath))
                        issues.Add($"{facing.id} approved artwork is required");
                    if (string.IsNullOrWhiteSpace(facing.neutralResourcePath))
                        issues.Add($"{facing.id} neutral artwork is required");
                    if (string.IsNullOrWhiteSpace(facing.nightOverlayResourcePath) &&
                        string.IsNullOrWhiteSpace(facing.nightFullResourcePath))
                        issues.Add($"{facing.id} requires either a night overlay or full-night artwork");
                    var shadePathCount = 0;
                    if (!string.IsNullOrWhiteSpace(facing.morningShadeResourcePath)) shadePathCount++;
                    if (!string.IsNullOrWhiteSpace(facing.noonShadeResourcePath)) shadePathCount++;
                    if (!string.IsNullOrWhiteSpace(facing.afternoonShadeResourcePath)) shadePathCount++;
                    if (!string.IsNullOrWhiteSpace(facing.eveningShadeResourcePath)) shadePathCount++;
                    if (shadePathCount != 0 && shadePathCount != 4)
                        issues.Add($"{facing.id} must provide all four daytime shade overlays or none");
                }
            }

            return issues;
        }

        private static bool IsVector2(float[] values) =>
            values != null && values.Length == 2;

        private static bool IsVector3(float[] values) =>
            values != null && values.Length == 3;

        private static Vector2 Vector2From(float[] values) =>
            new(values[0], values[1]);

        private static Vector3 Vector3From(float[] values) =>
            new(values[0], values[1], values[2]);
    }

    public static class HybridBuildingPackageRegistry
    {
        private const string CatalogResource =
            "CityForgeV3/Buildings/active-building-catalog";
        public const string GovernmentHousePackageResource =
            "CityForgeV3/Buildings/ColonialGovernmentHouseV15/building-package";
        public const string NewEnglandHousePackageResource =
            "CityForgeV3/Buildings/NewEnglandHouse1720V21/building-package";

        private static HybridBuildingPackage _governmentHouse;
        private static HybridBuildingPackage _newEnglandHouse;
        private static IReadOnlyList<HybridBuildingPackage> _all;

        [Serializable]
        private sealed class ActiveCatalogManifest
        {
            public string schema;
            public string[] packageResourcePaths;
        }

        public static IReadOnlyList<HybridBuildingPackage> All
        {
            get
            {
                if (_all != null) return _all;
                var asset = Resources.Load<TextAsset>(CatalogResource);
                if (asset == null) throw new MissingReferenceException(
                    $"Missing active building catalog: {CatalogResource}");
                var catalog = JsonUtility.FromJson<ActiveCatalogManifest>(asset.text);
                if (catalog?.schema != "cityforge-v3-active-building-catalog-v1" ||
                    catalog.packageResourcePaths == null)
                    throw new InvalidOperationException("Invalid active building catalog");
                var packages = new List<HybridBuildingPackage>();
                var ids = new HashSet<string>();
                foreach (var path in catalog.packageResourcePaths)
                {
                    try
                    {
                        var package = Load(path);
                        if (!ids.Add(package.Id))
                            throw new InvalidOperationException($"Duplicate active package id: {package.Id}");
                        packages.Add(package);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(
                            $"Skipping invalid active building package '{path}' so the Buildings menu can remain available: " +
                            exception.Message);
                    }
                }
                if (packages.Count == 0)
                    throw new InvalidOperationException("Active building catalog contains no valid packages");
                return _all = packages;
            }
        }

        public static HybridBuildingPackage GovernmentHouse =>
            _governmentHouse ??= HybridBuildingPackage.Load(
                GovernmentHousePackageResource);

        public static HybridBuildingPackage NewEnglandHouse =>
            _newEnglandHouse ??= HybridBuildingPackage.Load(
                NewEnglandHousePackageResource);

        public static HybridBuildingPackage Load(string resourcePath)
        {
            if (resourcePath == GovernmentHousePackageResource)
                return GovernmentHouse;
            if (resourcePath == NewEnglandHousePackageResource)
                return NewEnglandHouse;
            return HybridBuildingPackage.Load(resourcePath);
        }

        public static void InvalidateCache()
        {
            _all = null;
            _governmentHouse = null;
            _newEnglandHouse = null;
        }
    }
}
