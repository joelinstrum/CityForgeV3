using System;

namespace CityForgeV3.World
{
    public enum RegionClimate { Temperate, Desert, Tropical, Mediterranean }
    public enum RegionTreeCoverage { None, Sparse, Wooded }

    public enum RegionWaterAmount { None, Few, Many }

    public enum RegionRiverFlow { Varied, WestToEast, EastToWest, SouthToNorth, NorthToSouth }

    [Serializable]
    public sealed class RegionTerrainSettings
    {
        public RegionClimate Climate = RegionClimate.Temperate;
        public RegionTreeCoverage TreeCoverage = RegionTreeCoverage.None;
        public int FloraSeed;
        public RegionTerrainSettings Copy() => (RegionTerrainSettings)MemberwiseClone();

        public RegionRiverFlow Flow = RegionRiverFlow.Varied;
        public RegionWaterAmount DeepRivers = RegionWaterAmount.None;
        public RegionWaterAmount Streams = RegionWaterAmount.None;
    }
}
