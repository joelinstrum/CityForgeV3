using System;using System.Linq;using NUnit.Framework;using UnityEngine;using CityForgeV3.World;
namespace CityForgeV3.Tests.EditMode
{
 public class DistrictQuarryTests
 {
  RegionCityTile District(){var d=new RegionCityTile{TileId="quarry-test",Width=1,Height=1};d.StoneSites.Add(new(){Id="stone-test",NormalizedX=.5f,NormalizedZ=.5f,CraneLoadingVersion=1,Script=new QuarryScript{loadingSeconds=4}});return d;}
  [Test] public void ManuallyPlacedCoalSurvivesRegenerationAndReload()
  {
   var d=District();
   d.ResourceDeposits.Add(new DistrictResourceDeposit { Id="placed-coal", Kind="coal", ManuallyPlaced=true, NormalizedX=.4f, NormalizedZ=.6f });
   DistrictNaturalResources.Ensure(d,new DistrictElevation(d));
   Assert.AreEqual(1,d.ResourceDeposits.Count);
   d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
   d.NaturalResourceGenerationKey="";
   DistrictNaturalResources.Ensure(d,new DistrictElevation(d));
   Assert.AreEqual(1,d.ResourceDeposits.Count);
   Assert.AreEqual("placed-coal",d.ResourceDeposits[0].Id);
   Assert.AreEqual(.4f,d.ResourceDeposits[0].NormalizedX);
   Assert.False(d.ResourceDeposits[0].MineBuilt);
  }
  [Test] public void QuarryAutomaticallyClearsNearbyTreesAndPreservesDistantTrees()
  {
   var d=District();var site=d.StoneSites[0];
   foreach(var state in new[]{DistrictTreeHarvestState.Standing,DistrictTreeHarvestState.Fallen,DistrictTreeHarvestState.Stump})
    d.Flora.Add(new PlacedDistrictFlora{InstanceId=state.ToString(),NormalizedX=.5f+5.3f/640f,NormalizedZ=.5f,HarvestState=state});
   d.Flora.Add(new PlacedDistrictFlora{InstanceId="outside",NormalizedX=.5f+24f/640f,NormalizedZ=.5f});
   Assert.True(DistrictQuarry.CanBuild(d,site,_=>true));
   Assert.AreEqual(4,d.Flora.Count,"Eligibility must not clear trees");
   Assert.True(DistrictQuarry.Build(d,site,_=>true));
   Assert.AreEqual("outside",d.Flora.Single().InstanceId);
   Assert.AreEqual(0,DistrictLabor.State(d).Wood,"Construction clearing does not harvest wood");
   var reloaded=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
   Assert.True(reloaded.StoneSites[0].Built);Assert.AreEqual("outside",reloaded.Flora.Single().InstanceId);
  }
  [Test] public void FailedQuarryPlacementLeavesTreesUntouched()
  {
   var d=District();d.Flora.Add(new PlacedDistrictFlora{InstanceId="keep",NormalizedX=.5f,NormalizedZ=.5f});
   var before=JsonUtility.ToJson(d);
   Assert.False(DistrictQuarry.Build(d,d.StoneSites[0],_=>false));
   Assert.AreEqual(before,JsonUtility.ToJson(d));
  }
  [Test] public void UndoRestoresQuarryAndClearedTreesTogether()
  {
   var d=District();d.Flora.Add(new PlacedDistrictFlora{InstanceId="restore",NormalizedX=.5f,NormalizedZ=.5f});
   var history=new DistrictUndoHistory();history.Reset(JsonUtility.ToJson(d));
   Assert.True(DistrictQuarry.Build(d,d.StoneSites[0],_=>true));history.Commit(JsonUtility.ToJson(d));
   Assert.True(history.TryUndo(out var snapshot));JsonUtility.FromJsonOverwrite(snapshot,d);
   Assert.False(d.StoneSites[0].Built);Assert.AreEqual("restore",d.Flora.Single().InstanceId);
  }
  [Test] public void TwoWorkersCostFiveHundredOncePerSeasonIncludingReload()
  {
   var d=District();d.Treasury=2000;var site=d.StoneSites[0];
   Assert.AreEqual(2,DistrictQuarry.WorkerCount);Assert.AreEqual(250,DistrictQuarry.WorkerWage);
   Assert.True(DistrictQuarry.Build(d,site,_=>true));Assert.AreEqual(1500,d.Treasury);
   d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));site=d.StoneSites[0];
   DistrictQuarry.Tick(d,1,_=>true);Assert.AreEqual(1500,d.Treasury);
   d.Labor.SeasonSeconds=DistrictLabor.SeasonDuration-.1f;
   DistrictLabor.Tick(d,.2f,(a,b)=>new(),_=>true);
   Assert.AreEqual(1,d.Labor.SeasonIndex);Assert.AreEqual(1000,d.Treasury);
   DistrictQuarry.Tick(d,1,_=>true);Assert.AreEqual(1000,d.Treasury);
  }
  [Test] public void UnpaidWorkersStopProductionAndResumeWhenFunded()
  {
   var d=District();d.Treasury=500;var site=d.StoneSites[0];DistrictQuarry.Build(d,site,_=>true);
   d.Labor.SeasonIndex++;DistrictQuarry.Tick(d,64,_=>true);
   Assert.AreEqual(0,d.ResourceInventory.Stone);Assert.AreEqual(0,site.Elapsed);
   d.Treasury=500;DistrictQuarry.Tick(d,64,_=>true);
   Assert.AreEqual(0,d.Treasury);Assert.AreEqual(1,d.ResourceInventory.Stone);
   site.Built=false;d.Labor.SeasonIndex++;d.Treasury=1000;DistrictQuarry.PayWages(d);Assert.AreEqual(1000,d.Treasury);
  }
  [Test] public void UnaffordableBuildPreservesTreesAndTreasury()
  {
   var d=District();d.Treasury=499;d.Flora.Add(new PlacedDistrictFlora{NormalizedX=.5f,NormalizedZ=.5f});
   Assert.False(DistrictQuarry.Build(d,d.StoneSites[0],_=>true));Assert.AreEqual(499,d.Treasury);Assert.AreEqual(1,d.Flora.Count);
  }
  [Test] public void ExistingQuarryGetsPayrollOnceAndPauseDoesNotDismissWorkers()
  {
   var d=District();var site=d.StoneSites[0];site.Built=true;site.Enabled=false;d.Treasury=1500;
   Assert.True(DistrictQuarry.PayWages(d));Assert.AreEqual(1000,d.Treasury);
   Assert.False(DistrictQuarry.PayWages(d));d.Labor.SeasonIndex++;
   Assert.True(DistrictQuarry.PayWages(d));Assert.AreEqual(500,d.Treasury);
  }
  [Test] public void RequiresOwnedStoneDepositAndDryClearGround(){var d=District();var p=d.StoneSites[0];Assert.False(DistrictQuarry.Build(d,new(),_=>true));p.Kind="coal";Assert.False(DistrictQuarry.Build(d,p,_=>true));p.Kind="stone";Assert.False(DistrictQuarry.Build(d,p,_=>false));Assert.True(DistrictQuarry.Build(d,p,_=>true));Assert.False(DistrictQuarry.Build(d,p,_=>true));}
  [Test] public void CreditsOnlyAtCompletedLoadingAndNotOnReloadOrCartReset(){var d=District();var p=d.StoneSites[0];DistrictQuarry.Build(d,p,_=>true);DistrictQuarry.Tick(d,60,_=>true);Assert.AreEqual("loading",p.Phase);Assert.AreEqual(0,d.ResourceInventory.Stone);DistrictQuarry.Tick(d,2,_=>true);d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));p=d.StoneSites[0];DistrictQuarry.Tick(d,0,_=>true);Assert.AreEqual(0,d.ResourceInventory.Stone);DistrictQuarry.Tick(d,2,_=>true);Assert.AreEqual(1,d.ResourceInventory.Stone);Assert.AreEqual(1,p.CartBlocks);DistrictQuarry.Tick(d,192,_=>true);Assert.AreEqual(4,d.ResourceInventory.Stone);Assert.AreEqual("full",p.Phase);DistrictQuarry.Tick(d,8,_=>true);Assert.AreEqual(4,p.CartBlocks);Assert.AreEqual(4,d.ResourceInventory.Stone);DistrictQuarry.Tick(d,64,_=>true);Assert.AreEqual(4,d.ResourceInventory.Stone);}
  [Test] public void PauseRemovalAndFloodingStopProgressAndInventory(){var d=District();var p=d.StoneSites[0];DistrictQuarry.Build(d,p,_=>true);DistrictQuarry.Tick(d,20,_=>true);p.Enabled=false;DistrictQuarry.Tick(d,100,_=>true);Assert.AreEqual(20,p.Elapsed);p.Enabled=true;DistrictQuarry.Tick(d,100,_=>false);Assert.AreEqual(20,p.Elapsed);p.Built=false;DistrictQuarry.Tick(d,100,_=>true);Assert.AreEqual(0,d.ResourceInventory.Stone);}
  [Test] public void ScriptValidationAndInventorySaturation(){Assert.Throws<ArgumentException>(()=>QuarryScript.Parse("{\"schema\":\"bad\"}"));Assert.Throws<ArgumentException>(()=>QuarryScript.Parse("{\"miningSeconds\":0}"));var d=District();var p=d.StoneSites[0];p.Script=QuarryScript.Parse("{\"miningSeconds\":1,\"loadingSeconds\":1,\"cartCapacity\":1,\"stoneTonsPerBlock\":10}");DistrictQuarry.Build(d,p,_=>true);d.ResourceInventory.Stone=int.MaxValue-1;DistrictQuarry.Tick(d,2,_=>true);Assert.AreEqual(int.MaxValue,d.ResourceInventory.Stone);}
  [Test] public void DepositsAreDeterministicAndExistingSitesPreserved(){var a=new RegionCityTile{TileId="stone-seed",Width=1,Height=1};var b=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(a));DistrictQuarry.Ensure(a,_=>true);DistrictQuarry.Ensure(b,_=>true);Assert.AreEqual(2,a.StoneSites.Count);Assert.AreEqual(JsonUtility.ToJson(a.StoneSites[0]),JsonUtility.ToJson(b.StoneSites[0]));a.StoneSites[0].Built=true;var before=JsonUtility.ToJson(a.StoneSites[0]);DistrictQuarry.Ensure(a,_=>false);Assert.AreEqual(before,JsonUtility.ToJson(a.StoneSites[0]));}
 }
}
