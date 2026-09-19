using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    [Serializable]
    public sealed class LotStats
    {
        public string MinimumEraId = "founders";
        public int MinimumPopulation;
        public int MinimumEducationScore;
        public bool RequiresRoad;
        public bool RequiresWaterfront;
        public int Residents;
        public int SeasonalWagePerJob = 150;
        public string Service = "None";
        public int ServiceCapacity;
        public List<ContentResourceAmount> ConstructionResources = new();
        public List<LotBenefit> Benefits = new();
        public LotStats Copy() => JsonUtility.FromJson<LotStats>(JsonUtility.ToJson(this));
    }
    [Serializable]
    public sealed class LotBenefit
    {
        public string ResourceId = "food";
        public int Amount;
        public string Timing = "Per season";
    }
}
