using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
namespace CityForgeV3.Tests.EditMode
{
    public class DistrictWildlifeTests
    {
        [Test] public void MarksmanWagesRenewWithoutForestryAndSurviveReload()
        {
            var d=Forest();d.Treasury=1000;
            var guard=DistrictWildlife.Hire(d,Vector2.zero);Assert.NotNull(guard);Assert.AreEqual(750,d.Treasury);
            d.Labor.SeasonSeconds=599.95f;
            DistrictLabor.Tick(d,0,(_,_)=>null,_=>true);Assert.AreEqual(750,d.Treasury);
            DistrictLabor.Tick(d,.1f,(_,_)=>null,_=>true);Assert.AreEqual(500,d.Treasury);Assert.True(guard.WagesPaid);
            d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
            DistrictLabor.Tick(d,.1f,(_,_)=>null,_=>true);Assert.AreEqual(500,d.Treasury);
            d.Wildlife.Marksmen.Clear();d.Labor.SeasonSeconds=599.95f;
            DistrictLabor.Tick(d,.1f,(_,_)=>null,_=>true);Assert.AreEqual(500,d.Treasury);
        }
        [Test] public void UnpaidMarksmenDoNotProtectAndCanBePaidWithoutDoubleCharge()
        {
            var d=Forest();d.Treasury=250;var guard=DistrictWildlife.Hire(d,Vector2.zero);
            Assert.Null(DistrictWildlife.Hire(d,Vector2.one));Assert.AreEqual(1,d.Wildlife.Marksmen.Count);
            d.Labor.SeasonSeconds=599.95f;DistrictLabor.Tick(d,.1f,(_,_)=>null,_=>true);
            Assert.False(guard.WagesPaid);Assert.False(DistrictWildlife.PayWages(d));
            DistrictWildlife.Tick(d,.1f,_=>true);Assert.AreEqual(1,d.Wildlife.Bears.Count);Assert.AreEqual(0,guard.Shots);
            d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));Assert.False(d.Wildlife.Marksmen[0].WagesPaid);
            d.Treasury=500;Assert.True(DistrictWildlife.PayWages(d));Assert.AreEqual(250,d.Treasury);
            Assert.True(DistrictWildlife.PayWages(d));Assert.AreEqual(250,d.Treasury);
            DistrictWildlife.Tick(d,.1f,_=>true);Assert.AreEqual(1,d.Wildlife.Marksmen[0].Shots);
        }
        [Test] public void SharedSeasonChargesAxemenAndMarksmenOnce()
        {
            var d=Forest();d.Treasury=2000;d.Labor.CampPlaced=true;
            DistrictLabor.Assign(d,2);DistrictWildlife.Hire(d,Vector2.zero);Assert.AreEqual(1250,d.Treasury);
            d.Labor.SeasonSeconds=599.95f;DistrictLabor.Tick(d,.1f,(_,_)=>null,_=>true);Assert.AreEqual(500,d.Treasury);
            DistrictLabor.Tick(d,.1f,(_,_)=>null,_=>true);Assert.AreEqual(500,d.Treasury);
        }
        RegionCityTile Forest()
        {
            var d=new RegionCityTile{Width=1,Height=1};
            for(int i=0;i<5;i++)d.Flora.Add(new(){InstanceId="tree"+i,FloraId="cilician-fir",NormalizedX=.5f+i*.004f,NormalizedZ=.5f});
            DistrictWildlife.State(d).NextSighting=0;return d;
        }
        [Test] public void SightingsRequireMountainWoodlandAndRespectQuietPeriod()
        {
            var d=Forest();DistrictWildlife.Tick(d,.1f,_=>true);Assert.AreEqual(1,d.Wildlife.Bears.Count);
            d.Wildlife.NextSighting=0;DistrictWildlife.Tick(d,.1f,_=>true);Assert.AreEqual(1,d.Wildlife.Bears.Count);
            d=Forest();foreach(var t in d.Flora)t.FloraId="date-palm";DistrictWildlife.Tick(d,.1f,_=>true);Assert.IsEmpty(d.Wildlife.Bears);
            d=Forest();d.Wildlife.QuietSeconds=100;DistrictWildlife.Tick(d,.1f,_=>true);Assert.IsEmpty(d.Wildlife.Bears);
            d=Forest();DistrictWildlife.Tick(d,.1f,_=>false);Assert.IsEmpty(d.Wildlife.Bears);
        }
        [Test] public void BearInterruptsChoppingPreservesCargoAndHoldsUntilAreaClear()
        {
            var d=Forest();d.Labor.CampPlaced=true;d.Labor.AssignedAxemen=d.Labor.PaidSlots=1;
            var w=new DistrictAxeman{Position=Vector2.zero,TreeId="tree0",Activity=AxemanActivity.Chopping,Progress=100,Cargo=300,CargoTrees=1};d.Labor.Workers.Add(w);
            var b=new DistrictBear{Position=new Vector2(5,0)};d.Wildlife.Bears.Add(b);
            DistrictLabor.Tick(d,.1f,(_,p)=>new(){p},_=>true);
            Assert.True(w.BearAlarm);Assert.AreEqual(AxemanActivity.Retreating,w.Activity);Assert.Less(w.Position.x,0);
            Assert.AreEqual(DistrictTreeHarvestState.Standing,d.Flora[0].HarvestState);Assert.AreEqual(300,w.Cargo);Assert.AreEqual(1,w.CargoTrees);Assert.Zero(d.Labor.Wood);
            w.Position=new Vector2(-50,0);Assert.True(DistrictWildlife.AvoidBear(d,w,.1f,_=>true));
            b.Position=new Vector2(100,0);Assert.False(DistrictWildlife.AvoidBear(d,w,.1f,_=>true));Assert.AreEqual(AxemanActivity.Delivering,w.Activity);Assert.AreEqual(300,w.Cargo);
        }
        [Test] public void OnlyMarksmanWarningScaresBearAndSuppressesNewSightings()
        {
            var d=Forest();var b=new DistrictBear{Position=new Vector2(10,0),Home=new Vector2(10,0)};d.Wildlife.Bears.Add(b);
            d.Labor.Workers.Add(new(){Position=Vector2.zero});DistrictWildlife.Tick(d,.1f,_=>true);Assert.False(b.Fleeing);
            var m=new DistrictMarksman{Position=Vector2.zero};d.Wildlife.Marksmen.Add(m);DistrictWildlife.Tick(d,.1f,_=>true);
            Assert.True(b.Fleeing);Assert.AreEqual(1,m.Shots);Assert.Greater(b.Position.x,10);Assert.AreEqual(300,d.Wildlife.QuietSeconds);
            for(int i=0;i<600;i++)DistrictWildlife.Tick(d,.1f,_=>true);
            Assert.IsEmpty(d.Wildlife.Bears);Assert.AreEqual(1,m.Shots);Assert.Greater(d.Wildlife.QuietSeconds,200);
        }
        [Test] public void PauseReloadAndBlockedEscapePreserveState()
        {
            var d=Forest();d.Wildlife.Bears.Add(new(){Position=new Vector2(5,0)});d.Wildlife.Marksmen.Add(new(){Position=Vector2.zero});
            var before=JsonUtility.ToJson(d);DistrictWildlife.Tick(d,0,_=>true);Assert.AreEqual(before,JsonUtility.ToJson(d));
            DistrictWildlife.Tick(d,.1f,_=>false);Assert.AreEqual(new Vector2(5,0),d.Wildlife.Bears[0].Position);
            var loaded=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));Assert.AreEqual(d.Wildlife.Marksmen[0].Shots,loaded.Wildlife.Marksmen[0].Shots);Assert.True(loaded.Wildlife.Bears[0].Fleeing);
            DistrictWildlife.Tick(loaded,.1f,_=>true);Assert.AreEqual(1,loaded.Wildlife.Marksmen[0].Shots);
            var worker=new DistrictAxeman();Assert.True(DistrictWildlife.AvoidBear(loaded,worker,.1f,_=>false));Assert.AreEqual(Vector2.zero,worker.Position);
        }
    }
}
