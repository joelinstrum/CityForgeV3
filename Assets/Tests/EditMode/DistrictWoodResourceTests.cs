using System.Collections.Generic;
using CityForgeV3.World;
using CityForgeV3.Behaviors;
using NUnit.Framework;
using UnityEngine;
namespace CityForgeV3.Tests.EditMode
{
    public class DistrictWoodResourceTests
    {
        private static RegionCityTile District()=>new(){Labor=new(){CampPlaced=true,Camp=new Vector2(0,-5)},Flora=new(){new(){InstanceId="fir",FloraId="cilician-fir"}}};
        private static void Tick(RegionCityTile d)=>DistrictLabor.Tick(d,.1f,(_,to)=>new List<Vector2>{to},_=>true);
        [Test] public void OnlyNewBargeBundlesCreditWoodAcrossReloadAndRepeat()
        {
            var d=District();d.Labor.Wood=42;
            var def=new CargoLoadingDefinition{capacity=4,workers=2,pickupSeconds=.1f,unloadSeconds=.1f,staggerSeconds=0,repeat=true,restartDelaySeconds=.1f,departureSpeed=100};
            var state=CargoLoadingSimulation.Create(def);bool reloaded=false;
            for(int i=0;i<2000 && state.CompletedCycles<2;i++)
            {
                var before=state.Loaded;
                CargoLoadingSimulation.Step(def,state,.1f,.1f,true,true,1);
                DistrictTimber.CreditBargeLoading(d,before,state.Loaded);
                if(!reloaded && state.Loaded>0 && state.Loaded<4)
                {
                    d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
                    state=JsonUtility.FromJson<CargoLoadingState>(JsonUtility.ToJson(state));
                    var balance=d.Labor.Wood;
                    Assert.Zero(DistrictTimber.CreditBargeLoading(d,state.Loaded,state.Loaded));
                    Assert.AreEqual(balance,d.Labor.Wood);reloaded=true;
                }
            }
            Assert.True(reloaded);Assert.AreEqual(2,state.CompletedCycles);
            Assert.AreEqual(42+8*DistrictTimber.WoodPerBargeBundle,d.Labor.Wood);
        }
        [Test] public void LoadingCreditIgnoresResetAndPreviewAndCannotOverflow()
        {
            var d=District();d.Labor.Wood=int.MaxValue-10;
            Assert.Zero(DistrictTimber.CreditBargeLoading(null,0,12));
            Assert.Zero(DistrictTimber.CreditBargeLoading(d,12,0));
            Assert.AreEqual(10,DistrictTimber.CreditBargeLoading(d,0,2));
            Assert.AreEqual(int.MaxValue,d.Labor.Wood);
            Assert.Zero(DistrictTimber.CreditBargeLoading(d,2,3));
        }
        [Test] public void ResourceInventorySurvivesSaveAndOlderSavesStartEmpty()
        {
            var old=JsonUtility.FromJson<RegionCityTile>("{\"Labor\":{\"Wood\":42}}");
            var empty=old.ResourceInventory??new DistrictResourceInventory();
            Assert.AreEqual(0,empty.Coal+empty.Stone+empty.IronOre+empty.Gold+empty.Oil+empty.Food+empty.Jewels+empty.Cloth);
            old.ResourceInventory=new(){Coal=1,Stone=2,IronOre=3,Gold=4,Oil=5,Food=6,Jewels=7,Cloth=8};
            var loaded=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(old));
            Assert.AreEqual(42,loaded.Labor.Wood);
            CollectionAssert.AreEqual(new[]{1,2,3,4,5,6,7,8},new[]{loaded.ResourceInventory.Coal,loaded.ResourceInventory.Stone,loaded.ResourceInventory.IronOre,loaded.ResourceInventory.Gold,loaded.ResourceInventory.Oil,loaded.ResourceInventory.Food,loaded.ResourceInventory.Jewels,loaded.ResourceInventory.Cloth});
        }
        [Test] public void FellingAndPickupDoNotIncreaseLumberInventory()
        {
            var d=District();DistrictLabor.Assign(d,1);
            for(int i=0;i<300&&d.Flora[0].HarvestState==DistrictTreeHarvestState.Standing;i++)Tick(d);
            Assert.AreEqual(DistrictTreeHarvestState.Fallen,d.Flora[0].HarvestState);Assert.AreEqual(0,d.Labor.Wood);Assert.AreEqual(0,d.Labor.Workers[0].Cargo);
            Assert.IsFalse(DistrictTreeHarvest.FellForTransport(d,d.Flora[0],0));Assert.AreEqual(0,d.Labor.Wood);
            d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
            for(int i=0;i<500;i++)Tick(d);
            Assert.AreEqual(0,d.Labor.Wood);Assert.AreEqual(DistrictTreeHarvestState.Stump,d.Flora[0].HarvestState);
        }
        [Test] public void OldRawCargoDoesNotBypassBargeLoading()
        {
            var d=District();d.Flora.Clear();d.Labor.Wood=12;
            d.Labor.Workers.Add(JsonUtility.FromJson<DistrictAxeman>("{\"Slot\":0,\"Cargo\":8,\"Activity\":4,\"Position\":{\"x\":0,\"y\":-5},\"Route\":[]}"));
            d.Labor.AssignedAxemen=1;d.Labor.PaidSlots=1;
            Tick(d);Assert.AreEqual(12,d.Labor.Wood);Tick(d);Assert.AreEqual(12,d.Labor.Wood);
        }
        [Test] public void OldFallenTreesKeepTheirRemainingYield()
        {
            var d=District();d.Flora[0].HarvestState=DistrictTreeHarvestState.Fallen;d.Flora[0].RemainingWood=8;DistrictLabor.Assign(d,1);
            for(int i=0;i<300;i++)Tick(d);
            Assert.AreEqual(0,d.Labor.Wood);Assert.AreEqual(DistrictTreeHarvestState.Stump,d.Flora[0].HarvestState);
        }
        [Test] public void UndoAndFreshFellingDoNotGrantLumber()
        {
            var d=District();var history=new DistrictUndoHistory();history.Reset(JsonUtility.ToJson(d));
            Assert.True(DistrictTreeHarvest.FellForTransport(d,d.Flora[0],0));history.Commit(JsonUtility.ToJson(d));
            Assert.True(history.TryUndo(out var saved));JsonUtility.FromJsonOverwrite(saved,d);
            Assert.AreEqual(0,d.Labor.Wood);Assert.False(d.Flora[0].WoodCredited);
            Assert.True(DistrictTreeHarvest.FellForTransport(d,d.Flora[0],0));Assert.AreEqual(0,d.Labor.Wood);
            Assert.AreEqual(3,DistrictTreeHarvest.PrototypeWoodYield/DistrictTreeHarvest.ProvisionalWoodBuildingCost);
        }
    }
}
