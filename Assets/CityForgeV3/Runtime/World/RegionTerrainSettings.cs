using System;

namespace CityForgeV3.World
{
    public enum RegionWaterAmount { None, Few, Many }

    public enum RegionRiverFlow { Varied, WestToEast, EastToWest, SouthToNorth, NorthToSouth }

    [Serializable]
    public sealed class RegionTerrainSettings
    {
        public RegionRiverFlow Flow = RegionRiverFlow.Varied;
        public RegionWaterAmount DeepRivers = RegionWaterAmount.None;
        public RegionWaterAmount Streams = RegionWaterAmount.None;
    }
}
