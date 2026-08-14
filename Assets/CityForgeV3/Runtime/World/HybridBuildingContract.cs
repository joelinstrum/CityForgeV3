using UnityEngine;

namespace CityForgeV3.World
{
    public readonly struct HybridFacingSpec
    {
        public HybridFacingSpec(
            string id,
            string approvedResourcePath,
            string neutralResourcePath,
            string nightOverlayResourcePath,
            string nightFullResourcePath,
            string morningShadeResourcePath,
            string noonShadeResourcePath,
            string afternoonShadeResourcePath,
            string eveningShadeResourcePath,
            string winterResourcePath,
            float azimuth,
            Vector2 pivot)
        {
            Id = id;
            ApprovedResourcePath = approvedResourcePath;
            NeutralResourcePath = neutralResourcePath;
            NightOverlayResourcePath = nightOverlayResourcePath;
            NightFullResourcePath = nightFullResourcePath;
            MorningShadeResourcePath = morningShadeResourcePath;
            NoonShadeResourcePath = noonShadeResourcePath;
            AfternoonShadeResourcePath = afternoonShadeResourcePath;
            EveningShadeResourcePath = eveningShadeResourcePath;
            WinterResourcePath = winterResourcePath;
            CameraAzimuthDegrees = azimuth;
            Pivot = pivot;
        }

        public string Id { get; }
        public string ResourcePath => ApprovedResourcePath;
        public string ApprovedResourcePath { get; }
        public string NeutralResourcePath { get; }
        public string NightOverlayResourcePath { get; }
        public string NightFullResourcePath { get; }
        public string MorningShadeResourcePath { get; }
        public string NoonShadeResourcePath { get; }
        public string AfternoonShadeResourcePath { get; }
        public string EveningShadeResourcePath { get; }
        public string WinterResourcePath { get; }
        public bool HasNightOverlay =>
            !string.IsNullOrWhiteSpace(NightOverlayResourcePath);
        public bool HasFullNightArtwork =>
            !string.IsNullOrWhiteSpace(NightFullResourcePath);
        public string ShadeResourcePath(TimeOfDayPreset preset)
        {
            return preset switch
            {
                TimeOfDayPreset.Morning => MorningShadeResourcePath,
                TimeOfDayPreset.Noon => NoonShadeResourcePath,
                TimeOfDayPreset.Afternoon => AfternoonShadeResourcePath,
                TimeOfDayPreset.Evening => EveningShadeResourcePath,
                _ => null
            };
        }
        public float CameraAzimuthDegrees { get; }
        public Vector2 SourcePivotTopOrigin => Pivot;
        public Vector2 UnityPivot => new(Pivot.x, 1f - Pivot.y);
        private Vector2 Pivot { get; }
    }

    public static class FiveBayHybridContract
    {
        private static HybridBuildingPackage Package =>
            HybridBuildingPackageRegistry.GovernmentHouse;

        public static string Schema => HybridBuildingPackage.Schema;
        public static float WidthMeters => Package.WidthMeters;
        public static float DepthMeters => Package.DepthMeters;
        public static float HeightMeters => Package.HeightMeters;
        public static float PixelsPerMeter => Package.PixelsPerMeter;
        public static float CameraElevationDegrees =>
            Package.CameraElevationDegrees;
        public static float CameraRadiusMeters => Package.CameraRadiusMeters;
        public static float CameraTargetHeightMeters =>
            Package.CameraTargetHeightMeters;
        public static string ProxyResourcePath => Package.PrimitiveResourcePath;
        public static Vector3 FoundationCenter => Package.RotationAnchor;
        public static Vector3 PresentationAnchor => Package.PresentationAnchor;
        public static int FacingCount => Package.FacingCount;

        public static HybridFacingSpec Facing(int index) =>
            Package.Facing(index);

        public static int WrapFacing(int index) =>
            Package.WrapFacing(index);
    }
}
