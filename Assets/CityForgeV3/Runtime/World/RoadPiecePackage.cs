using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum RoadPieceTopology
    {
        Straight,
        Corner,
        TJunction,
        FourWay,
        Endpoint
    }

    public enum RoadPiecePort
    {
        North,
        East,
        South,
        West
    }

    [Serializable]
    public sealed class RoadPieceManifest
    {
        public string id;
        public string topology;
        public string resourcePath;
        public string[] ports;
        public string artworkStatus = "approved";
    }

    [Serializable]
    public sealed class RoadTrafficManifest
    {
        public string mode = "vehicle";
        public string direction = "two_way";
        public int laneCount = 2;
        public string driveSide;
        public float laneOffsetMeters;
        public float[] laneOffsetsMeters;
        public float speedLimitMetersPerSecond;
        public string minorApproachControl;
    }

    [Serializable]
    public sealed class RoadPiecePackageManifest
    {
        public string schema;
        public string id;
        public string displayName;
        public float tileSizeMeters;
        public float roadWidthMeters;
        public float artworkWidthMeters;
        public float artworkLengthMeters;
        public int occupancyCrossCells = 1;
        public bool independentMarkings;
        public RoadTrafficManifest traffic;
        public string source;
        public RoadPieceManifest[] pieces;
    }

    public sealed class RoadPieceDefinition
    {
        public string Id { get; }
        public RoadPieceTopology Topology { get; }
        public string ResourcePath { get; }
        public IReadOnlyList<RoadPiecePort> Ports { get; }
        public string ArtworkStatus { get; }
        public bool HasArtwork => !string.IsNullOrWhiteSpace(ResourcePath);

        public RoadPieceDefinition(RoadPieceManifest manifest)
        {
            Id = manifest.id;
            Topology = Enum.Parse<RoadPieceTopology>(manifest.topology);
            ResourcePath = manifest.resourcePath ?? "";
            ArtworkStatus = string.IsNullOrWhiteSpace(manifest.artworkStatus)
                ? "approved"
                : manifest.artworkStatus;
            var ports = new List<RoadPiecePort>();
            foreach (var port in manifest.ports ?? Array.Empty<string>())
                ports.Add(Enum.Parse<RoadPiecePort>(port));
            Ports = ports;
        }

        public IReadOnlyList<RoadPiecePort> RotatedPorts(int quarterTurns)
        {
            var result = new List<RoadPiecePort>();
            var turns = ((quarterTurns % 4) + 4) % 4;
            foreach (var port in Ports)
                result.Add((RoadPiecePort)(((int)port + turns) % 4));
            return result;
        }
    }

    public sealed class RoadPiecePackage
    {
        public const string Schema = "cityforge-v3-road-piece-package-v1";
        public const string ManifestResourcePath = "CityForgeV3/Roads/BrickRoadV1/road-package";
        public const string LegacyPackageId = "cityforge.base.road.brick.v1";
        public string Id { get; }
        public string DisplayName { get; }
        public float TileSizeMeters { get; }
        public float RoadWidthMeters { get; }
        public float ArtworkWidthMeters { get; }
        public float ArtworkLengthMeters { get; }
        public int OccupancyCrossCells { get; }
        public bool SupportsIndependentMarkings { get; }
        public string DriveSide { get; }
        public string TrafficMode { get; }
        public string TrafficDirection { get; }
        public int LaneCount { get; }
        public bool AllowsVehicles => TrafficMode != "pedestrian" && LaneCount > 0;
        public float LaneOffsetMeters { get; }
        public IReadOnlyList<float> LaneOffsetsMeters { get; }
        public float SpeedLimitMetersPerSecond { get; }
        public string MinorApproachControl { get; }
        public IReadOnlyList<RoadPieceDefinition> Pieces { get; }

        private RoadPiecePackage(RoadPiecePackageManifest manifest)
        {
            Id = manifest.id;
            DisplayName = manifest.displayName;
            TileSizeMeters = manifest.tileSizeMeters;
            RoadWidthMeters = manifest.roadWidthMeters;
            ArtworkWidthMeters = manifest.artworkWidthMeters > 0f
                ? manifest.artworkWidthMeters : manifest.tileSizeMeters;
            ArtworkLengthMeters = manifest.artworkLengthMeters > 0f
                ? manifest.artworkLengthMeters : manifest.tileSizeMeters;
            OccupancyCrossCells = Mathf.Max(1, manifest.occupancyCrossCells);
            SupportsIndependentMarkings = manifest.independentMarkings;
            TrafficMode = manifest.traffic?.mode ?? "vehicle";
            TrafficDirection = manifest.traffic?.direction ?? "two_way";
            LaneCount = manifest.traffic != null && manifest.traffic.laneCount > 0
                ? manifest.traffic.laneCount :
                TrafficMode == "pedestrian" ? 0 : 2;
            DriveSide = manifest.traffic?.driveSide ?? "";
            LaneOffsetMeters = manifest.traffic?.laneOffsetMeters ?? 0f;
            LaneOffsetsMeters = manifest.traffic?.laneOffsetsMeters != null &&
                manifest.traffic.laneOffsetsMeters.Length > 0
                ? manifest.traffic.laneOffsetsMeters
                : new[] { LaneOffsetMeters };
            SpeedLimitMetersPerSecond = manifest.traffic?.speedLimitMetersPerSecond ?? 0f;
            MinorApproachControl = manifest.traffic?.minorApproachControl ?? "";
            var pieces = new List<RoadPieceDefinition>();
            foreach (var piece in manifest.pieces ?? Array.Empty<RoadPieceManifest>())
                pieces.Add(new RoadPieceDefinition(piece));
            Pieces = pieces;
        }

        public RoadPieceDefinition Piece(RoadPieceTopology topology) =>
            ((List<RoadPieceDefinition>)Pieces).Find(piece => piece.Topology == topology);

        public static RoadPiecePackage Load()
            => Load(ManifestResourcePath);

        public static RoadPiecePackage Load(string manifestResourcePath)
        {
            var asset = Resources.Load<TextAsset>(manifestResourcePath);
            if (asset == null) throw new MissingReferenceException(
                $"Missing road package manifest: {manifestResourcePath}");
            var manifest = JsonUtility.FromJson<RoadPiecePackageManifest>(asset.text);
            if (manifest == null || manifest.schema != Schema)
                throw new FormatException("Invalid road package schema.");
            return new RoadPiecePackage(manifest);
        }

        public List<string> Validate()
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) issues.Add("id is required");
            if (TileSizeMeters <= 0f || RoadWidthMeters <= 0f ||
                RoadWidthMeters > ArtworkWidthMeters)
                issues.Add("metric dimensions are invalid");
            if (ArtworkWidthMeters <= 0f || ArtworkLengthMeters <= 0f ||
                OccupancyCrossCells < 1 ||
                ArtworkWidthMeters > OccupancyCrossCells * RoadPlacementModel.TileSizeMeters)
                issues.Add("artwork footprint is invalid");
            if (TrafficMode != "vehicle" && TrafficMode != "pedestrian")
                issues.Add("traffic.mode is invalid");
            if (TrafficDirection != "two_way" && TrafficDirection != "one_way" &&
                TrafficDirection != "none") issues.Add("traffic.direction is invalid");
            if (AllowsVehicles && DriveSide != "right" && DriveSide != "left")
                issues.Add("traffic.driveSide is invalid");
            if (AllowsVehicles && (LaneOffsetMeters < 0f ||
                LaneOffsetMeters >= ArtworkWidthMeters * 0.5f))
                issues.Add("traffic.laneOffsetMeters is invalid");
            foreach (var offset in LaneOffsetsMeters)
                if (offset < 0f || offset >= ArtworkWidthMeters * 0.5f)
                    issues.Add("traffic.laneOffsetsMeters contains an invalid offset");
            if (TrafficMode == "pedestrian" && LaneCount != 0)
                issues.Add("pedestrian packages cannot publish vehicle lanes");
            if (SpeedLimitMetersPerSecond <= 0f) issues.Add("traffic.speedLimitMetersPerSecond is invalid");
            if (MinorApproachControl != "yield" && MinorApproachControl != "stop")
                issues.Add("traffic.minorApproachControl is invalid");
            foreach (RoadPieceTopology topology in Enum.GetValues(typeof(RoadPieceTopology)))
                if (Piece(topology) == null) issues.Add($"{topology} definition is required");
            foreach (var piece in Pieces)
            {
                if (piece.HasArtwork && Resources.Load<Texture2D>(piece.ResourcePath) == null)
                    issues.Add($"Missing artwork for {piece.Id}");
                if (!piece.HasArtwork && piece.ArtworkStatus != "pending")
                    issues.Add($"Missing artwork status for {piece.Id}");
            }
            return issues;
        }
    }

    public static class RoadPiecePackageCatalog
    {
        public const string TwoLaneSidewalkId =
            "cityforge.base.road.two-lane-with-sidewalk.v1";
        public const string OneWaySidewalkId =
            "cityforge.base.road.one-way-with-sidewalk.v1";
        public const string AlleyId = "cityforge.base.road.alley.v1";
        public const string PedestrianStreetId =
            "cityforge.base.road.pedestrian-street.v1";
        public const string DividedBoulevardId =
            "cityforge.base.road.divided-boulevard.v3";
        public const string WideTwoLaneAvenueId =
            "cityforge.base.road.wide-two-lane-avenue.v4";
        public const string DefaultId = TwoLaneSidewalkId;

        private static readonly string[] ManifestPaths =
        {
            "CityForgeV3/Roads/FlatColorV1/two-lane-with-sidewalk/road-package",
            "CityForgeV3/Roads/FlatColorV1/one-way-with-sidewalk/road-package",
            "CityForgeV3/Roads/FlatColorV1/alley/road-package",
            "CityForgeV3/Roads/FlatColorV1/pedestrian-street/road-package",
            "CityForgeV3/Roads/FlatColorV3/divided-boulevard/road-package",
            "CityForgeV3/Roads/FlatColorV4/wide-two-lane-avenue/road-package",
            RoadPiecePackage.ManifestResourcePath
        };

        private static List<RoadPiecePackage> _packages;
        public static IReadOnlyList<RoadPiecePackage> Packages
        {
            get
            {
                if (_packages != null) return _packages;
                _packages = new List<RoadPiecePackage>();
                foreach (var path in ManifestPaths) _packages.Add(RoadPiecePackage.Load(path));
                return _packages;
            }
        }

        public static RoadPiecePackage Resolve(string packageId)
        {
            var requested = string.IsNullOrWhiteSpace(packageId)
                ? RoadPiecePackage.LegacyPackageId : packageId;
            foreach (var package in Packages)
                if (package.Id == requested) return package;
            return RoadPiecePackage.Load();
        }

        public static RoadPiecePackage Default => Resolve(DefaultId);
    }

    [Serializable]
    public enum RoadMarkingStyle
    {
        SingleDotted,
        NoLines,
        DoubleLines
    }

    [Serializable]
    public enum RoadLaneMarkingStyle
    {
        Lines,
        NoLines
    }

    [Serializable]
    public enum RoadCenterMarkingStyle
    {
        DoubleLines,
        NoLines
    }

    [Serializable]
    public sealed class PlacedRoadPiece
    {
        public string Id = "";
        public string PackageId = RoadPiecePackage.LegacyPackageId;
        public string RoadMaterialId = "";
        public string SidewalkMaterialId = "";
        public RoadMarkingStyle MarkingStyle = RoadMarkingStyle.SingleDotted;
        public RoadLaneMarkingStyle LaneMarkingStyle = RoadLaneMarkingStyle.Lines;
        public RoadCenterMarkingStyle CenterMarkingStyle = RoadCenterMarkingStyle.DoubleLines;
        public RoadPieceTopology Topology;
        public int GridX;
        public int GridZ;
        public int RotationQuarterTurns;
    }

    public static class RoadPlacementModel
    {
        public const int MinimumCell = -1;
        public const int MaximumCell = 0;
        public const float TileSizeMeters = 10f;

        public static int MinimumCellForLot(int lotSizeMeters) =>
            -(Mathf.Clamp(lotSizeMeters, 10, 80) / (int)TileSizeMeters) / 2;

        public static int MaximumCellForLot(int lotSizeMeters) =>
            MinimumCellForLot(lotSizeMeters) +
            Mathf.Clamp(lotSizeMeters, 10, 80) / (int)TileSizeMeters - 1;

        public static Vector2 CellCenterMeters(int gridX, int gridZ, int lotSizeMeters = 20)
            => CellCenterMeters(gridX, gridZ, lotSizeMeters, lotSizeMeters);

        public static Vector2 CellCenterMeters(int gridX, int gridZ,
            int lotWidthMeters, int lotDepthMeters)
        {
            var minimumX = MinimumCellForLot(lotWidthMeters);
            var maximumX = MaximumCellForLot(lotWidthMeters);
            var minimumZ = MinimumCellForLot(lotDepthMeters);
            var maximumZ = MaximumCellForLot(lotDepthMeters);
            return new Vector2(
                -lotWidthMeters * 0.5f +
                    (Mathf.Clamp(gridX, minimumX, maximumX) - minimumX + 0.5f) * TileSizeMeters,
                -lotDepthMeters * 0.5f +
                    (Mathf.Clamp(gridZ, minimumZ, maximumZ) - minimumZ + 0.5f) * TileSizeMeters);
        }

        public static Vector2Int WorldToCell(float worldX, float worldZ, int lotSizeMeters = 20)
            => WorldToCell(worldX, worldZ, lotSizeMeters, lotSizeMeters);

        public static Vector2Int WorldToCell(float worldX, float worldZ,
            int lotWidthMeters, int lotDepthMeters)
        {
            var minimumX = MinimumCellForLot(lotWidthMeters);
            var maximumX = MaximumCellForLot(lotWidthMeters);
            var minimumZ = MinimumCellForLot(lotDepthMeters);
            var maximumZ = MaximumCellForLot(lotDepthMeters);
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt((worldX + lotWidthMeters * 0.5f) /
                    TileSizeMeters) + minimumX, minimumX, maximumX),
                Mathf.Clamp(Mathf.FloorToInt((worldZ + lotDepthMeters * 0.5f) /
                    TileSizeMeters) + minimumZ, minimumZ, maximumZ));
        }

        public static PlacedRoadPiece FindAt(List<PlacedRoadPiece> pieces, int gridX, int gridZ) =>
            pieces?.Find(piece => piece != null && piece.GridX == gridX && piece.GridZ == gridZ);

        public static IReadOnlyList<Vector2Int> OccupiedCells(
            PlacedRoadPiece piece, RoadPiecePackage package)
        {
            var cells = new List<Vector2Int>();
            var crossCells = Mathf.Max(1, package?.OccupancyCrossCells ?? 1);
            var crossAlongX = piece.RotationQuarterTurns % 2 == 0;
            for (var offset = 0; offset < crossCells; offset++)
                cells.Add(new Vector2Int(
                    piece.GridX + (crossAlongX ? offset : 0),
                    piece.GridZ + (crossAlongX ? 0 : offset)));
            return cells;
        }

        public static void PlaceOrReplace(List<PlacedRoadPiece> pieces, RoadPieceTopology topology,
            int gridX, int gridZ, int quarterTurns, int lotSizeMeters = 20,
            string packageId = "")
            => PlaceOrReplace(pieces, topology, gridX, gridZ, quarterTurns,
                lotSizeMeters, lotSizeMeters, packageId);

        public static void PlaceOrReplace(List<PlacedRoadPiece> pieces, RoadPieceTopology topology,
            int gridX, int gridZ, int quarterTurns, int lotWidthMeters, int lotDepthMeters,
            string packageId = "")
        {
            var minimumX = MinimumCellForLot(lotWidthMeters);
            var maximumX = MaximumCellForLot(lotWidthMeters);
            var minimumZ = MinimumCellForLot(lotDepthMeters);
            var maximumZ = MaximumCellForLot(lotDepthMeters);
            var selectedPackage = RoadPiecePackageCatalog.Resolve(packageId);
            var turns = FiveBayHybridContract.WrapFacing(quarterTurns);
            var crossAlongX = turns % 2 == 0;
            var crossExtra = Mathf.Max(0, selectedPackage.OccupancyCrossCells - 1);
            var clampedX = Mathf.Clamp(gridX, minimumX,
                maximumX - (crossAlongX ? crossExtra : 0));
            var clampedZ = Mathf.Clamp(gridZ, minimumZ,
                maximumZ - (crossAlongX ? 0 : crossExtra));
            var existing = FindAt(pieces, gridX, gridZ);
            if (existing == null)
            {
                existing = new PlacedRoadPiece { Id = $"road-{pieces.Count + 1}" };
                pieces.Add(existing);
            }
            existing.Topology = topology;
            existing.GridX = clampedX;
            existing.GridZ = clampedZ;
            existing.RotationQuarterTurns = turns;
            if (!string.IsNullOrWhiteSpace(packageId)) existing.PackageId = packageId;
            if (string.IsNullOrWhiteSpace(existing.PackageId))
                existing.PackageId = RoadPiecePackage.LegacyPackageId;
            var reserved = new HashSet<Vector2Int>(OccupiedCells(existing, selectedPackage));
            pieces.RemoveAll(piece => piece != existing && piece != null &&
                new List<Vector2Int>(OccupiedCells(piece,
                    RoadPiecePackageCatalog.Resolve(piece.PackageId)))
                .Exists(reserved.Contains));
        }

        public static bool DeleteAt(List<PlacedRoadPiece> pieces, int gridX, int gridZ) =>
            pieces != null && pieces.RemoveAll(piece => piece != null &&
                piece.GridX == gridX && piece.GridZ == gridZ) > 0;

        public static void RepairConnectedTopologies(
            List<PlacedRoadPiece> pieces,
            RoadPiecePackage package,
            int lotSizeMeters = 20)
            => RepairConnectedTopologies(pieces, package, lotSizeMeters, lotSizeMeters);

        public static void RepairConnectedTopologies(
            List<PlacedRoadPiece> pieces, RoadPiecePackage package,
            int lotWidthMeters, int lotDepthMeters)
        {
            if (pieces == null || package == null) return;
            foreach (var piece in pieces)
            {
                if (piece == null) continue;
                var piecePackage = PackageFor(piece, package);
                var desired = new List<RoadPiecePort>();
                foreach (RoadPiecePort port in Enum.GetValues(typeof(RoadPiecePort)))
                {
                    var neighbor = Neighbor(piece.GridX, piece.GridZ, port);
                    if (FindAt(pieces, neighbor.x, neighbor.y) != null) desired.Add(port);
                }
                if (desired.Count == 1)
                {
                    var continuation = Opposite(desired[0]);
                    var outside = Neighbor(piece.GridX, piece.GridZ, continuation);
                    if (!IsInside(outside.x, outside.y, lotWidthMeters, lotDepthMeters))
                        desired.Add(continuation);
                }
                if (desired.Count == 0) continue;
                if (!TryFindTopologyForPorts(piecePackage, desired, out var topology, out var turns))
                    continue;
                piece.Topology = topology;
                piece.RotationQuarterTurns = turns;
            }
        }

        public static bool TryFindTopologyForPorts(
            RoadPiecePackage package,
            IReadOnlyList<RoadPiecePort> desired,
            out RoadPieceTopology topology,
            out int quarterTurns)
        {
            foreach (RoadPieceTopology candidate in Enum.GetValues(typeof(RoadPieceTopology)))
            {
                var definition = package?.Piece(candidate);
                if (definition == null || !definition.HasArtwork) continue;
                for (var turns = 0; turns < 4; turns++)
                {
                    var ports = definition.RotatedPorts(turns);
                    if (ports.Count != desired.Count) continue;
                    var matches = true;
                    foreach (var port in desired)
                        if (!new List<RoadPiecePort>(ports).Contains(port)) { matches = false; break; }
                    if (!matches) continue;
                    topology = candidate;
                    quarterTurns = turns;
                    return true;
                }
            }
            topology = RoadPieceTopology.Straight;
            quarterTurns = 0;
            return false;
        }

        public static bool TrySuggest(List<PlacedRoadPiece> pieces, int gridX, int gridZ,
            RoadPiecePackage package, out RoadPieceTopology topology, out int quarterTurns)
        {
            var desired = new List<RoadPiecePort>();
            foreach (RoadPiecePort port in Enum.GetValues(typeof(RoadPiecePort)))
            {
                var neighborCell = Neighbor(gridX, gridZ, port);
                var neighbor = FindAt(pieces, neighborCell.x, neighborCell.y);
                if (neighbor != null && HasPort(neighbor, Opposite(port),
                    PackageFor(neighbor, package))) desired.Add(port);
            }
            topology = RoadPieceTopology.Straight;
            quarterTurns = 0;
            if (desired.Count == 0) return false;
            if (desired.Count == 1)
            {
                topology = RoadPieceTopology.Endpoint;
                quarterTurns = (int)desired[0];
            }
            else if (desired.Count == 2)
            {
                var opposite = ((int)desired[0] + 2) % 4 == (int)desired[1];
                if (opposite)
                {
                    topology = RoadPieceTopology.Straight;
                    quarterTurns = desired.Contains(RoadPiecePort.North) ? 0 : 1;
                }
                else
                {
                    topology = RoadPieceTopology.Corner;
                    for (var turns = 0; turns < 4; turns++)
                    {
                        var ports = package.Piece(topology).RotatedPorts(turns);
                        if (desired.TrueForAll(port => new List<RoadPiecePort>(ports).Contains(port)))
                        { quarterTurns = turns; break; }
                    }
                }
            }
            else if (desired.Count == 3)
            {
                topology = RoadPieceTopology.TJunction;
                foreach (RoadPiecePort port in Enum.GetValues(typeof(RoadPiecePort)))
                    if (!desired.Contains(port)) { quarterTurns = (int)port; break; }
            }
            else topology = RoadPieceTopology.FourWay;
            return package.Piece(topology)?.HasArtwork == true;
        }

        public static List<string> Validate(List<PlacedRoadPiece> pieces, RoadPiecePackage package,
            int lotSizeMeters = 20)
            => Validate(pieces, package, lotSizeMeters, lotSizeMeters);

        public static List<string> Validate(List<PlacedRoadPiece> pieces, RoadPiecePackage package,
            int lotWidthMeters, int lotDepthMeters)
        {
            var minimumX = MinimumCellForLot(lotWidthMeters);
            var maximumX = MaximumCellForLot(lotWidthMeters);
            var minimumZ = MinimumCellForLot(lotDepthMeters);
            var maximumZ = MaximumCellForLot(lotDepthMeters);
            var issues = new List<string>();
            var cells = new HashSet<string>();
            foreach (var piece in pieces ?? new List<PlacedRoadPiece>())
            {
                if (piece == null) { issues.Add("Invalid road piece"); continue; }
                var cell = $"{piece.GridX},{piece.GridZ}";
                if (!cells.Add(cell)) issues.Add($"Duplicate road cell {cell}");
                if (piece.GridX < minimumX || piece.GridX > maximumX ||
                    piece.GridZ < minimumZ || piece.GridZ > maximumZ)
                    issues.Add($"Road {piece.Id} is outside the lot");
                var piecePackage = PackageFor(piece, package);
                var definition = piecePackage.Piece(piece.Topology);
                if (definition == null || !definition.HasArtwork)
                    issues.Add($"{piece.Topology} artwork is unavailable");
                if (definition == null) continue;
                foreach (var port in definition.RotatedPorts(piece.RotationQuarterTurns))
                {
                    var neighborCell = Neighbor(piece.GridX, piece.GridZ, port);
                    if (!IsInside(neighborCell.x, neighborCell.y, lotWidthMeters, lotDepthMeters)) continue;
                    var neighbor = FindAt(pieces, neighborCell.x, neighborCell.y);
                    if (neighbor == null || !HasPort(neighbor, Opposite(port),
                        PackageFor(neighbor, package)))
                        issues.Add($"Unmatched {port} port at {cell}");
                }
            }
            return issues;
        }

        public static CirculationNetwork BuildVehicleNetwork(List<PlacedRoadPiece> pieces,
            RoadPiecePackage package, int lotSizeMeters = 20)
            => BuildVehicleNetwork(pieces, package, lotSizeMeters, lotSizeMeters);

        public static CirculationNetwork BuildVehicleNetwork(List<PlacedRoadPiece> pieces,
            RoadPiecePackage package, int lotWidthMeters, int lotDepthMeters)
            => BuildVehicleNetwork(pieces, _ => package, lotWidthMeters, lotDepthMeters);

        public static CirculationNetwork BuildVehicleNetwork(List<PlacedRoadPiece> pieces,
            Func<PlacedRoadPiece, RoadPiecePackage> packageForPiece,
            int lotWidthMeters, int lotDepthMeters)
        {
            var network = new CirculationNetwork { Mode = CirculationMode.Vehicle };
            var centers = new Dictionary<string, CirculationNode>();
            foreach (var piece in pieces ?? new List<PlacedRoadPiece>())
            {
                var package = piece == null ? null : packageForPiece?.Invoke(piece);
                if (piece == null || package == null || !package.AllowsVehicles ||
                    package.Piece(piece.Topology) == null) continue;
                var node = network.AddNode(CellCenterMeters(
                    piece.GridX, piece.GridZ, lotWidthMeters, lotDepthMeters));
                centers[$"{piece.GridX},{piece.GridZ}"] = node;
            }
            foreach (var piece in pieces ?? new List<PlacedRoadPiece>())
            {
                if (piece == null || !centers.TryGetValue($"{piece.GridX},{piece.GridZ}", out var center)) continue;
                var package = packageForPiece?.Invoke(piece);
                if (package == null || !package.AllowsVehicles) continue;
                var definition = package.Piece(piece.Topology);
                foreach (var port in definition.RotatedPorts(piece.RotationQuarterTurns))
                {
                    var neighborCell = Neighbor(piece.GridX, piece.GridZ, port);
                    if (IsInside(neighborCell.x, neighborCell.y, lotWidthMeters, lotDepthMeters))
                    {
                        var neighbor = FindAt(pieces, neighborCell.x, neighborCell.y);
                        var neighborPackage = neighbor == null ? null : packageForPiece?.Invoke(neighbor);
                        if (neighbor != null && neighborPackage?.AllowsVehicles == true &&
                            HasPort(neighbor, Opposite(port), neighborPackage) &&
                            centers.TryGetValue($"{neighborCell.x},{neighborCell.y}", out var neighborNode))
                            network.Connect(center.Id, neighborNode.Id);
                    }
                    else
                    {
                        var direction = PortDirection(port);
                        var boundary = network.AddNode(center.PositionMeters + direction * 5f,
                            CirculationNodeKind.LotBoundaryPort,
                            $"road-{piece.GridX}-{piece.GridZ}-{port.ToString().ToLowerInvariant()}");
                        network.Connect(center.Id, boundary.Id);
                    }
                }
            }
            return network;
        }

        private static bool HasPort(PlacedRoadPiece piece, RoadPiecePort port, RoadPiecePackage package)
        {
            var definition = package.Piece(piece.Topology);
            return definition != null && new List<RoadPiecePort>(definition.RotatedPorts(
                piece.RotationQuarterTurns)).Contains(port);
        }

        private static RoadPiecePackage PackageFor(PlacedRoadPiece piece,
            RoadPiecePackage fallback)
        {
            if (piece == null || string.IsNullOrWhiteSpace(piece.PackageId)) return fallback;
            return RoadPiecePackageCatalog.Resolve(piece.PackageId);
        }

        private static Vector2Int Neighbor(int x, int z, RoadPiecePort port) => port switch
        {
            RoadPiecePort.North => new Vector2Int(x, z + 1),
            RoadPiecePort.East => new Vector2Int(x + 1, z),
            RoadPiecePort.South => new Vector2Int(x, z - 1),
            _ => new Vector2Int(x - 1, z)
        };

        private static Vector2 PortDirection(RoadPiecePort port) => port switch
        {
            RoadPiecePort.North => Vector2.up,
            RoadPiecePort.East => Vector2.right,
            RoadPiecePort.South => Vector2.down,
            _ => Vector2.left
        };

        private static RoadPiecePort Opposite(RoadPiecePort port) =>
            (RoadPiecePort)(((int)port + 2) % 4);

        private static bool IsInside(int x, int z, int lotSizeMeters = 20) =>
            IsInside(x, z, lotSizeMeters, lotSizeMeters);

        public static bool IsInside(int x, int z, int lotWidthMeters, int lotDepthMeters) =>
            x >= MinimumCellForLot(lotWidthMeters) && x <= MaximumCellForLot(lotWidthMeters) &&
            z >= MinimumCellForLot(lotDepthMeters) && z <= MaximumCellForLot(lotDepthMeters);
    }
}
