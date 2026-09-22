using System;

namespace CityForgeV3.World
{
    public enum RegionClimate { Temperate, Desert, Tropical, Mediterranean }
    public enum RegionTreeCoverage { None = 0, Sparse = 1, Wooded = 2, Heavy = 3 }

    public enum RegionWaterAmount { None, Few, Many }

    public enum RegionRiverFlow { Varied, WestToEast, EastToWest, SouthToNorth, NorthToSouth }

    [Serializable]
    public sealed class ForestFamilyMix
    {
        // Relative weights. They intentionally need not add to exactly 100 so
        // the friendly 33 / 33 / 33 default remains valid.
        public int Deciduous = 33;
        public int Mountain = 33;
        public int Tropical = 33;
        public int Total => Math.Max(0, Deciduous) + Math.Max(0, Mountain) +
            Math.Max(0, Tropical);
        public ForestFamilyMix Copy() => new()
        {
            Deciduous = Deciduous,
            Mountain = Mountain,
            Tropical = Tropical
        };
    }

    [Serializable]
    public sealed class RegionTerrainSettings
    {
        public RegionClimate Climate = RegionClimate.Temperate;
        public RegionTreeCoverage TreeCoverage = RegionTreeCoverage.None;
        public int FloraSeed;
        public ForestFamilyMix ForestMix = new();
        public RegionTerrainSettings Copy()
        {
            var copy = (RegionTerrainSettings)MemberwiseClone();
            copy.ForestMix = (ForestMix ?? new ForestFamilyMix()).Copy();
            return copy;
        }

        public RegionRiverFlow Flow = RegionRiverFlow.Varied;
        public RegionWaterAmount DeepRivers = RegionWaterAmount.None;
        public RegionWaterAmount Streams = RegionWaterAmount.None;
        public int RiverCountsVersion;
        public int MediumRiverCount;
        public int SmallRiverCount;
        public int StreamCount;
    }
}
