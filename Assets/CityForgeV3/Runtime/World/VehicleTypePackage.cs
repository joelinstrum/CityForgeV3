using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class VehicleTrafficManifest
    {
        public float lengthMeters;
        public float minimumStoppedGapMeters;
        public float followingTimeSeconds;
        public float accelerationMetersPerSecondSquared;
        public float comfortableBrakeMetersPerSecondSquared;
        public float minimumDesiredSpeedFactor;
        public float maximumDesiredSpeedFactor;
    }

    [Serializable]
    public sealed class VehicleTypeManifest
    {
        public string schema;
        public string id;
        public string displayName;
        public string modelResourcePath;
        public VehicleTrafficManifest traffic;
    }

    /// <summary>
    /// JSON-backed, mod-ready definition for one vehicle type. Runtime systems
    /// consume this contract and never need Model-T-specific driving constants.
    /// </summary>
    public sealed class VehicleTypePackage
    {
        public const string Schema = "cityforge-v3-vehicle-package-v1";
        public const string ModelTManifestResourcePath =
            "CityForgeV3/Vehicles/FordModelT/vehicle-package";
        public const string RollsRoyce1926ManifestResourcePath =
            "CityForgeV3/Vehicles/RollsRoyce1926/vehicle-package";

        private readonly VehicleTypeManifest _manifest;
        public string Id => _manifest.id;
        public string DisplayName => _manifest.displayName;
        public string ModelResourcePath => _manifest.modelResourcePath;
        public float LengthMeters => _manifest.traffic.lengthMeters;
        public float MinimumStoppedGapMeters => _manifest.traffic.minimumStoppedGapMeters;
        public float FollowingTimeSeconds => _manifest.traffic.followingTimeSeconds;
        public float AccelerationMetersPerSecondSquared =>
            _manifest.traffic.accelerationMetersPerSecondSquared;
        public float ComfortableBrakeMetersPerSecondSquared =>
            _manifest.traffic.comfortableBrakeMetersPerSecondSquared;
        public float MinimumDesiredSpeedFactor => _manifest.traffic.minimumDesiredSpeedFactor;
        public float MaximumDesiredSpeedFactor => _manifest.traffic.maximumDesiredSpeedFactor;

        private VehicleTypePackage(VehicleTypeManifest manifest) => _manifest = manifest;

        public static VehicleTypePackage LoadModelT() => Load(ModelTManifestResourcePath);
        public static VehicleTypePackage LoadRollsRoyce1926() =>
            Load(RollsRoyce1926ManifestResourcePath);

        public static VehicleTypePackage Load(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new MissingReferenceException($"Missing vehicle package at {resourcePath}.");
            var manifest = JsonUtility.FromJson<VehicleTypeManifest>(asset.text);
            var package = new VehicleTypePackage(manifest ?? new VehicleTypeManifest());
            var issues = package.Validate();
            if (issues.Count > 0)
                throw new FormatException($"Invalid vehicle package: {string.Join("; ", issues)}");
            return package;
        }

        public List<string> Validate()
        {
            var issues = new List<string>();
            if (_manifest.schema != Schema) issues.Add("unsupported schema");
            if (string.IsNullOrWhiteSpace(Id)) issues.Add("id is required");
            if (string.IsNullOrWhiteSpace(DisplayName)) issues.Add("displayName is required");
            if (string.IsNullOrWhiteSpace(ModelResourcePath) ||
                Resources.Load<GameObject>(ModelResourcePath) == null)
                issues.Add("modelResourcePath is missing or invalid");
            if (_manifest.traffic == null) issues.Add("traffic is required");
            else
            {
                if (LengthMeters <= 0f) issues.Add("traffic.lengthMeters must be positive");
                if (MinimumStoppedGapMeters < 0f) issues.Add("minimumStoppedGapMeters cannot be negative");
                if (FollowingTimeSeconds <= 0f) issues.Add("followingTimeSeconds must be positive");
                if (AccelerationMetersPerSecondSquared <= 0f)
                    issues.Add("acceleration must be positive");
                if (ComfortableBrakeMetersPerSecondSquared <= 0f)
                    issues.Add("comfortableBrake must be positive");
                if (MinimumDesiredSpeedFactor <= 0f ||
                    MaximumDesiredSpeedFactor < MinimumDesiredSpeedFactor)
                    issues.Add("desired speed factor range is invalid");
            }
            return issues;
        }
    }
}
