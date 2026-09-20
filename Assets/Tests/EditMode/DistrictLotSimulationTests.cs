using System;
using NUnit.Framework;
using UnityEngine;
using CityForgeV3.World;
namespace CityForgeV3.Tests.EditMode
{
public class DistrictLotSimulationTests
{
    static LotSaveData House(int residents = 100) => new LotSaveData { LotType=LotType.Residential,Stats=new LotStats { Residents=residents } };
    [TestCase(LotType.Residential)]
    [TestCase(LotType.Commercial)]
    [TestCase(LotType.Industrial)]
    [TestCase(LotType.Mixed)]
    [TestCase(LotType.Civics)]
    [TestCase(LotType.Agricultural)]
    [TestCase(LotType.Transportation)]
    [TestCase(LotType.CivicsParks)]
    [TestCase(LotType.DistrictTownCenter)]
    public void EveryLotTypeAddsAndRemovesItsAuthoredPopulation(LotType type)
    {
        var district = new RegionCityTile();
        var lot = new LotSaveData
        {
            LotId = "population-lot",
            LotType = type,
            Stats = new LotStats { Residents = 10 }
        };
        var simulation = DistrictLotSimulation.For(district, _ => null);

        simulation.Add("placed", lot);
        Assert.That(simulation.Population.Population, Is.EqualTo(10));
        Assert.That(simulation.Population.HousingCapacity, Is.EqualTo(10));

        simulation.Remove("placed");
        Assert.That(simulation.Population.Population, Is.Zero);
        Assert.That(simulation.Population.HousingCapacity, Is.Zero);
    }
    [Test]
    public void FounderLotSuppressesReusableDefinitionPopulation()
    {
        var district = new RegionCityTile
        {
            Founded = true,
            FounderBuildingId = "fortress",
            LotId = "founder-lot"
        };
        district.Lots.Add(new PlacedDistrictLot
        {
            InstanceId = "founder",
            LotId = "founder-lot"
        });
        var definition = new LotSaveData
        {
            LotId = "founder-lot",
            Stats = new LotStats { Residents = 999 }
        };
        var simulation = DistrictLotSimulation.Rebuild(district,
            _ => definition);

        Assert.That(simulation.Population.Population, Is.Zero);
        Assert.That(district.Lots[0].HasPopulationOverride, Is.True,
            "Older founder placements migrate to the population-free contract.");
        var restored = JsonUtility.FromJson<RegionCityTile>(
            JsonUtility.ToJson(district));
        simulation = DistrictLotSimulation.Rebuild(restored,
            _ => definition);
        Assert.That(simulation.Population.Population, Is.Zero);
        definition.Stats.Residents = 42;
        DistrictLotSimulation.SavedDefinitionChanged(restored, definition);
        Assert.That(simulation.Population.Population, Is.Zero);
    }
    [Test] public void PlacementRemovalAndReloadDoNotDuplicateResidents()
    {
        var d=new RegionCityTile();var house=House();var sim=DistrictLotSimulation.For(d,_=>house);
        d.Lots.Add(new PlacedDistrictLot{InstanceId="home",LotId="home"});sim.Add("home",house);sim.Add("home",house);
        Assert.That(sim.Population.Population,Is.EqualTo(100));Assert.That(sim.Population.Children,Is.EqualTo(20));Assert.That(sim.Households,Is.EqualTo(25));
        d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));sim=DistrictLotSimulation.Rebuild(d,_=>house);
        Assert.That(sim.Population.Population,Is.EqualTo(100));sim.Remove("home");d.Lots.Clear();
        Assert.That(sim.Population.Population,Is.Zero);DistrictLotSimulation.Rebuild(d,_=>house);Assert.That(d.Population.Population,Is.Zero);
    }
    [Test] public void CachedFinancesAndProductionAdvanceOnlyOncePerSeason()
    {
        var d=new RegionCityTile{Treasury=100};var lot=House(0);lot.BusinessRates=new BusinessRates{Configured=true,SeasonalRevenue=100,SeasonalCost=20,Employees=2};
        lot.Stats.Benefits.Add(new LotBenefit{ResourceId="food",Amount=10});d.Lots.Add(new PlacedDistrictLot{InstanceId="farm",LotId="farm"});int reads=0;
        LotSaveData Read(string _){reads++;return lot;}
        DistrictBusinessEconomy.SettleSeason(d,Read);var sim=DistrictLotSimulation.For(d,Read);
        for(int i=0;i<100;i++){DistrictBusinessEconomy.SettleSeason(d,Read);sim.AdvanceSeason(0);}
        Assert.That(reads,Is.EqualTo(1));d.Labor.SeasonIndex=1;DistrictBusinessEconomy.SettleSeason(d,Read);sim.AdvanceSeason(1);sim.AdvanceSeason(1);
        Assert.That(d.Treasury,Is.EqualTo(180));Assert.That(d.ResourceInventory.Food,Is.EqualTo(10));Assert.That(reads,Is.EqualTo(1));
        d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));sim=DistrictLotSimulation.Rebuild(d,Read);Assert.That(sim.AdvanceSeason(1),Is.False);Assert.That(d.ResourceInventory.Food,Is.EqualTo(10));
    }
    [Test] public void ServicesFoodJobsAndAgingDriveDemographicScores()
    {
        var d=new RegionCityTile();var sim=DistrictLotSimulation.For(d,_=>null);sim.Add("home",House());
        var school=House(0);school.Stats.Service="Education";school.Stats.ServiceCapacity=20;school.BusinessRates=new BusinessRates{Configured=true,Employees=65};school.Stats.SeasonalWagePerJob=100;
        sim.Add("school",school);var clinic=House(0);clinic.Stats.Service="Health";clinic.Stats.ServiceCapacity=100;sim.Add("clinic",clinic);
        var park=House(0);park.Stats.Service="Recreation";park.Stats.ServiceCapacity=100;park.Stats.Benefits.Add(new LotBenefit{ResourceId="food",Amount=1});sim.Add("park",park);
        double age=sim.AverageAge;float health=sim.Population.Health;sim.AdvanceSeason(4);
        Assert.That(sim.AverageAge,Is.EqualTo(age+1).Within(.001));Assert.That(sim.Population.Education,Is.GreaterThan(20));Assert.That(sim.Population.Health,Is.GreaterThan(health));Assert.That(sim.Population.Happiness,Is.GreaterThan(60));
        Assert.That(sim.HouseholdIncome,Is.EqualTo(260));Assert.That(sim.Population.LastFoodConsumed,Is.EqualTo(1));Assert.That(d.ResourceInventory.Food,Is.Zero);
        sim.Remove("park");sim.Remove("clinic");sim.Remove("school");sim.AdvanceSeason(8);Assert.That(sim.Population.Health,Is.LessThan(100));Assert.That(sim.HouseholdIncome,Is.Zero);
    }
    [Test] public void PlacementRequirementsRejectWithoutChargingAndConsumeExactlyOnce()
    {
        var d=new RegionCityTile{Treasury=100};d.Labor.Wood=10;var lot=House();lot.BasePlopCost=20;lot.Stats.MinimumEraId="industrial";lot.Stats.RequiresRoad=true;lot.Stats.RequiresWaterfront=true;
        lot.Stats.ConstructionResources.Add(new ContentResourceAmount{resourceId="lumber",amount=10});
        bool Quote(string era, bool road, bool water,out long[] resources)=>DistrictLotRequirements.Quote(d,lot,era,1,1,2,2,(_,_)=>road,_=>water,Vector2.zero,out resources,out _);
        Assert.That(Quote("founders",true,true,out _),Is.False);Assert.That(Quote("industrial",false,true,out _),Is.False);Assert.That(Quote("industrial",true,false,out _),Is.False);
        Assert.That(d.Treasury,Is.EqualTo(100));Assert.That(d.Labor.Wood,Is.EqualTo(10));Assert.That(Quote("industrial",true,true,out var required),Is.True);
        DistrictLotRequirements.Consume(d,required);Assert.That(d.Labor.Wood,Is.Zero);Assert.That(Quote("industrial",true,true,out _),Is.False);
    }
    [Test] public void PopulationEducationAndMultipleStockpilesGateConstruction()
    {
        var d = new RegionCityTile { Treasury = 500 };
        d.Population.Population = 199;
        d.Population.Education = 49.5f;
        d.Labor.Wood = 200;
        d.ResourceInventory.Bricks = 1;
        d.ResourceInventory.Coal = 2;
        var lot = House(0);
        lot.BasePlopCost = 50;
        lot.Stats.MinimumEraId = "industrial";
        lot.Stats.MinimumPopulation = 200;
        lot.Stats.MinimumEducationScore = 50;
        lot.Stats.ConstructionResources.Add(new ContentResourceAmount
            { resourceId = "lumber", amount = 200 });
        lot.Stats.ConstructionResources.Add(new ContentResourceAmount
            { resourceId = "brick", amount = 1 });
        lot.Stats.ConstructionResources.Add(new ContentResourceAmount
            { resourceId = "coal", amount = 2 });
        bool Quote(out long[] resources, out string reason) =>
            DistrictLotRequirements.Quote(d, lot, "industrial", 1, 1, 2, 2,
                null, null, Vector2.zero, out resources, out reason);

        Assert.That(Quote(out _, out var reason), Is.False);
        Assert.That(reason, Does.Contain("population 200"));
        d.Population.Population = 200;
        Assert.That(Quote(out _, out reason), Is.False);
        Assert.That(reason, Does.Contain("education score 50"));
        d.Population.Education = 50f;
        d.ResourceInventory.Bricks = 0;
        Assert.That(Quote(out _, out reason), Is.False);
        Assert.That(reason, Does.Contain("bricks"));
        Assert.That(d.Treasury, Is.EqualTo(500));
        Assert.That(d.Labor.Wood, Is.EqualTo(200));
        Assert.That(d.ResourceInventory.Coal, Is.EqualTo(2));

        d.ResourceInventory.Bricks = 1;
        Assert.That(Quote(out var required, out reason), Is.True, reason);
        DistrictLotRequirements.Consume(d, required);
        Assert.That(d.Labor.Wood, Is.Zero);
        Assert.That(d.ResourceInventory.Bricks, Is.Zero);
        Assert.That(d.ResourceInventory.Coal, Is.Zero);
        Assert.That(d.Treasury, Is.EqualTo(500),
            "Resource consumption does not perform an implicit Save or cash charge");
        Assert.That(Quote(out _, out _), Is.False,
            "The same stockpile cannot fund a second placement");
    }
    [Test] public void DeliveryBenefitsExcludeExistingYieldAndStopAfterRemoval()
    {
        var d=new RegionCityTile();var sim=DistrictLotSimulation.For(d,_=>null);var lot=House(0);
        lot.Stats.Benefits.Add(new LotBenefit{ResourceId="food",Amount=3,Timing="Per delivery"});lot.Stats.Benefits.Add(new LotBenefit{ResourceId="wood",Amount=10,Timing="Per delivery"});sim.Add("mill",lot);
        sim.AdvanceSeason(1);Assert.That(d.ResourceInventory.Food,Is.Zero);sim.DeliveryCompleted("mill","lumber");Assert.That(d.ResourceInventory.Food,Is.EqualTo(3));Assert.That(d.Labor.Wood,Is.Zero);
        sim.Remove("mill");sim.DeliveryCompleted("mill");Assert.That(d.ResourceInventory.Food,Is.EqualTo(3));
    }
    [Test] public void SavingLotStatsUpdatesOnlyItsPlacedInstances()
    {
        var d=new RegionCityTile();var sim=DistrictLotSimulation.For(d,_=>null);var lot=House(4);lot.LotId="house";
        sim.Add("one",lot);sim.Add("two",lot);var other=House(10);other.LotId="other";sim.Add("three",other);
        lot.Stats.Residents=8;lot.BusinessRates=new BusinessRates{Configured=true,SeasonalRevenue=5};
        DistrictLotSimulation.SavedDefinitionChanged(d,lot);
        Assert.That(sim.Population.Population,Is.EqualTo(26));Assert.That(sim.Revenue,Is.EqualTo(10));
        DistrictLotSimulation.SavedDefinitionChanged(d,lot);Assert.That(sim.Population.Population,Is.EqualTo(26));
    }
    [Test] public void DenseDistrictOnlyReadsDefinitionsAtLoadBoundary()
    {
        var d=new RegionCityTile();var house=House(4);int reads=0;
        for(int i=0;i<10000;i++)d.Lots.Add(new PlacedDistrictLot{InstanceId="lot"+i,LotId="house"});
        var sim=DistrictLotSimulation.Rebuild(d,_=>{reads++;return house;});Assert.That(reads,Is.EqualTo(10000));
        var clock=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<1000;i++){sim.Add("new"+i,house);sim.Remove("new"+i);}clock.Stop();
        Assert.That(reads,Is.EqualTo(10000));Assert.That(sim.Population.Population,Is.EqualTo(40000));
        UnityEngine.Debug.Log("STATS PERF 10,000 lots; 1,000 incremental add/remove pairs: "+clock.Elapsed.TotalMilliseconds+"ms; zero definition reads after warmup");
        sim.AdvanceSeason(1);Assert.That(reads,Is.EqualTo(10000));
        long allocatedBefore=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<10000;i++) { var cached=DistrictLotSimulation.For(d); cached.AdvanceSeason(1); }
        long allocated=GC.GetAllocatedBytesForCurrentThread()-allocatedBefore;
        Assert.That(allocated,Is.Zero,"Steady-state demographic checks should not allocate");
        UnityEngine.Debug.Log("STATS ALLOC 10,000 cached same-season checks: "+allocated+" bytes");
    }
}
}
