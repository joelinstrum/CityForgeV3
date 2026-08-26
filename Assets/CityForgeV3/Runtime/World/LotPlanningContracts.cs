using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum LotType
    {
        Residential = 0,
        Commercial = 1,
        Business = Commercial,
        Transportation = 2,
        Neighborhood = Transportation,
        Industrial = 3,
        Mixed = 4
    }

    public enum LotZoomLevel
    {
        Detail,
        Inspection,
        CloseUp,
        Close,
        Near,
        Lot,
        Wide,
        Far,
        Neighborhood
    }

    public enum RoadTrafficFlow
    {
        TwoWay,
        InboundOnly,
        OutboundOnly
    }

    [Serializable]
    public sealed class RoadConnectionPort
    {
        public string Id = "south-main";
        public Vector2 PositionMeters = new(0f, -10f);
        public Vector2 OutwardDirection = Vector2.down;
        public float WidthMeters = 6f;
        public int LaneCount = 2;
        public RoadTrafficFlow Flow = RoadTrafficFlow.TwoWay;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Id) && WidthMeters > 0f &&
            LaneCount > 0 && OutwardDirection.sqrMagnitude > 0.99f;
    }

    [Serializable]
    public sealed class LotTypeContract
    {
        public LotType Type;
        public string DisplayName;
        public bool AllowsInternalRoads;
        public bool AllowsThroughTraffic;
        public int MaximumBuildings;
        public List<RoadConnectionPort> RoadPorts = new();

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(DisplayName) && MaximumBuildings > 0 &&
            RoadPorts.TrueForAll(port => port != null && port.IsValid);
    }

    public static class LotTypeCatalog
    {
        private static readonly LotTypeContract[] Contracts =
        {
            new()
            {
                Type = LotType.Residential,
                DisplayName = "RESIDENTIAL LOT",
                AllowsInternalRoads = false,
                AllowsThroughTraffic = false,
                MaximumBuildings = 4,
                RoadPorts = new List<RoadConnectionPort>
                {
                    new() { Id = "residential-driveway", WidthMeters = 3.5f, LaneCount = 1 }
                }
            },
            new()
            {
                Type = LotType.Commercial,
                DisplayName = "COMMERCIAL LOT",
                AllowsInternalRoads = true,
                AllowsThroughTraffic = false,
                MaximumBuildings = 8,
                RoadPorts = new List<RoadConnectionPort>
                {
                    new() { Id = "business-access", WidthMeters = 6f, LaneCount = 2 }
                }
            },
            new()
            {
                Type = LotType.Transportation,
                DisplayName = "TRANSPORTATION LOT",
                AllowsInternalRoads = true,
                AllowsThroughTraffic = true,
                MaximumBuildings = 32,
                RoadPorts = new List<RoadConnectionPort>
                {
                    new() { Id = "south-main", WidthMeters = 6f, LaneCount = 2, Flow = RoadTrafficFlow.TwoWay }
                }
            },
            new()
            {
                Type = LotType.Industrial,
                DisplayName = "INDUSTRIAL LOT",
                AllowsInternalRoads = true,
                AllowsThroughTraffic = false,
                MaximumBuildings = 16,
                RoadPorts = new List<RoadConnectionPort>
                {
                    new() { Id = "industrial-access", WidthMeters = 7f, LaneCount = 2 }
                }
            },
            new()
            {
                Type = LotType.Mixed,
                DisplayName = "MIXED-USE LOT",
                AllowsInternalRoads = true,
                AllowsThroughTraffic = false,
                MaximumBuildings = 16,
                RoadPorts = new List<RoadConnectionPort>
                {
                    new() { Id = "mixed-access", WidthMeters = 6f, LaneCount = 2 }
                }
            }
        };

        public static LotTypeContract For(LotType type) => Contracts[(int)type];
    }

    public static class LotMetricScale
    {
        public const float MinorGridMeters = 1f;
        public const float MajorGridMeters = 10f;

        public static float OrthographicSize(LotZoomLevel level) => level switch
        {
            LotZoomLevel.Detail => 11.4375f,
            LotZoomLevel.Inspection => 9f,
            LotZoomLevel.CloseUp => 9.5f,
            LotZoomLevel.Close => 10f,
            LotZoomLevel.Near => 10.75f,
            LotZoomLevel.Lot => 11.5f,
            LotZoomLevel.Wide => 14.5f,
            LotZoomLevel.Far => 16f,
            LotZoomLevel.Neighborhood => 17.5f,
            _ => 13f
        };

        public static bool ShowsMinorGrid(LotZoomLevel level) =>
            level != LotZoomLevel.Neighborhood;
    }
}
