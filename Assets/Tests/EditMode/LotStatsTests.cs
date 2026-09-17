using NUnit.Framework;
using UnityEngine;
using CityForgeV3.World;
namespace CityForgeV3.Tests.EditMode {
public class LotStatsTests {
[Test] public void StatsRoundTripAndCopyRemainIndependent(){
var lot=new LotSaveData{Stats=new LotStats{Residents=12,MinimumEraId="industrial",RequiresRoad=true,Service="Education",ServiceCapacity=100},BusinessRates=new BusinessRates{Configured=true,SeasonalCost=200,SeasonalRevenue=50,Employees=3}};
lot.Stats.ConstructionResources.Add(new ContentResourceAmount{resourceId="lumber",amount=25});
lot.Stats.Benefits.Add(new LotBenefit{ResourceId="food",Amount=10,Timing="Per delivery"});
var saved=JsonUtility.FromJson<LotSaveData>(JsonUtility.ToJson(lot));
Assert.That(saved.Stats.Residents,Is.EqualTo(12));Assert.That(saved.Stats.MinimumEraId,Is.EqualTo("industrial"));Assert.That(saved.Stats.RequiresRoad,Is.True);Assert.That(saved.Stats.ServiceCapacity,Is.EqualTo(100));Assert.That(saved.Stats.Benefits[0].Timing,Is.EqualTo("Per delivery"));
var copy=saved.Copy();copy.Stats.ConstructionResources[0].amount=99;copy.Stats.Benefits[0].Amount=99;copy.BusinessRates.SeasonalCost=0;
Assert.That(saved.Stats.ConstructionResources[0].amount,Is.EqualTo(25));Assert.That(saved.Stats.Benefits[0].Amount,Is.EqualTo(10));Assert.That(saved.BusinessRates.SeasonalCost,Is.EqualTo(200));
Assert.That(LotEconomy.ConstructionRequirements(saved)["lumber"],Is.EqualTo(25));
}
[Test] public void LegacyLotsDoNotGainRequirementsOrRates(){var lot=JsonUtility.FromJson<LotSaveData>("{\"Name\":\"Old lot\"}");Assert.That(lot.Stats?.Residents ?? 0,Is.Zero);Assert.That(DistrictBusinessEconomy.Rates(lot),Is.Null);Assert.That(LotEconomy.ConstructionRequirements(lot),Is.Empty);}
}}
