using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
namespace CityForgeV3.Tests.EditMode
{
    public class DistrictLaborTests
    {
        private static RegionCityTile District()=>new(){Treasury=10000,Labor=new(){CampPlaced=true,Camp=new Vector2(0,-5)},Flora=new(){new(){InstanceId="a",FloraId="cilician-fir",NormalizedX=.5f,NormalizedZ=.5f},new(){InstanceId="b",FloraId="cilician-fir",NormalizedX=.51f,NormalizedZ=.5f}}};
        private static List<Vector2> Route(Vector2 a,Vector2 b)=>new(){b};
        private static void Run(RegionCityTile d,int steps){for(int i=0;i<steps;i++)DistrictLabor.Tick(d,.1f,Route,_=>true);}
        [Test] public void WagesChargeOnlyNewPaidSlotsAndCannotOverdraw()
        {
            var d=District();Assert.True(DistrictLabor.Assign(d,2));Assert.AreEqual(9500,d.Treasury);
            Assert.True(DistrictLabor.Assign(d,2));Assert.True(DistrictLabor.Assign(d,0));Assert.True(DistrictLabor.Assign(d,2));Assert.AreEqual(9500,d.Treasury);
            Assert.False(DistrictLabor.Assign(d,int.MaxValue));Assert.False(DistrictLabor.Assign(d,-1));Assert.AreEqual(2,d.Labor.AssignedAxemen);
        }
        [Test] public void SeasonChargesOnceAndInsufficientFundsStopWork()
        {
            var d=District();DistrictLabor.Assign(d,2);d.Labor.SeasonSeconds=599.95f;Run(d,1);Assert.AreEqual(9000,d.Treasury);Assert.AreEqual(1,d.Labor.SeasonIndex);
            Run(d,1);Assert.AreEqual(9000,d.Treasury);d.Treasury=0;d.Labor.SeasonSeconds=599.95f;Run(d,1);var p=d.Labor.Workers[0].Position;Run(d,10);Assert.AreEqual(p,d.Labor.Workers[0].Position);Assert.AreEqual(0,d.Treasury);
        }
        [Test] public void TwoWorkersReserveDifferentTreesAndDeliverExactlyOnce()
        {
            var d=District();DistrictLabor.Assign(d,2);Run(d,1);Assert.AreEqual(2,d.Labor.Workers.Select(w=>w.TreeId).Distinct().Count());
            Run(d,1800);Assert.AreEqual(0,d.Labor.Wood);Assert.True(d.Flora.All(t=>t.HarvestState==DistrictTreeHarvestState.Stump));Run(d,100);Assert.AreEqual(0,d.Labor.Wood);
        }
        [Test] public void ReloadWithCargoDeliversWithoutDuplicateWoodOrWages()
        {
            var d=District();DistrictLabor.Assign(d,1);
            for(int i=0;i<1000&&d.Labor.Workers[0].Cargo==0;i++)Run(d,1);
            Assert.AreEqual(300,d.Labor.Workers[0].Cargo);var loaded=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));Run(loaded,1800);Assert.AreEqual(0,loaded.Labor.Wood);Assert.AreEqual(d.Treasury,loaded.Treasury);
        }
        [Test] public void ReducingAssignmentsReturnsCargoBeforeRemovingWorker()
        {
            var d=District();DistrictLabor.Assign(d,1);for(int i=0;i<1000&&d.Labor.Workers[0].Cargo==0;i++)Run(d,1);
            DistrictLabor.Assign(d,0);Run(d,300);Assert.AreEqual(0,d.Labor.Wood);Assert.IsEmpty(d.Labor.Workers);
        }
        [Test] public void UnreachableTreeAndMissingTargetDoNotGrantWood()
        {
            var d=District();DistrictLabor.Assign(d,1);DistrictLabor.Tick(d,.1f,(_,_)=>null,_=>true);Assert.AreEqual("",d.Labor.Workers[0].TreeId);Assert.AreEqual(0,d.Labor.Wood);
            Run(d,25);d.Flora.Clear();Run(d,500);Assert.AreEqual(0,d.Labor.Wood);Assert.AreEqual(AxemanActivity.Waiting,d.Labor.Workers[0].Activity);
        }
        [Test] public void MovedTreeIsNotChoppedRemotely()
        {
            var d=District();DistrictLabor.Assign(d,1);for(int i=0;i<100 && d.Labor.Workers[0].Activity!=AxemanActivity.Chopping;i++)Run(d,1);
            var tree=d.Flora.Find(t=>t.InstanceId==d.Labor.Workers[0].TreeId);tree.NormalizedX=.9f;Run(d,1);
            Assert.AreEqual(DistrictTreeHarvestState.Standing,tree.HarvestState);Assert.AreEqual(AxemanActivity.Waiting,d.Labor.Workers[0].Activity);
        }
        [Test] public void NavigationDetoursAroundWater()
        {
            var d=District();var nav=new DistrictLaborNavigation(d,p=>Mathf.Abs(p.x-.5f)<.003f && p.y<.52f);
            var route=nav.Route(new Vector2(-10,0),new Vector2(10,0));Assert.NotNull(route);Assert.True(route.Any(p=>p.y>=.02f*DistrictScale.SizeMeters(d.Height)));
            Assert.True(route.All(nav.Walkable));Assert.IsNull(nav.Route(new Vector2(-10,0),Vector2.zero));
        }
        [Test] public void OlderSaveGetsEmptyLaborAndZeroDeltaDoesNotMove()
        {
            var d=JsonUtility.FromJson<RegionCityTile>("{\"Treasury\":1000}");Assert.AreEqual(0,DistrictLabor.State(d).AssignedAxemen);d.Labor.CampPlaced=true;DistrictLabor.Assign(d,1);DistrictLabor.Tick(d,0,Route,_=>true);Assert.AreEqual(750,d.Treasury);Assert.AreEqual(0,d.Labor.SeasonSeconds);
        }
    }
}
