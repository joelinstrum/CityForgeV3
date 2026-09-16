using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class QuarryCraneTests
    {
        [Test] public void LoadLiftsBeforeSwingingAndLowersAtCargoSlot()
        {
            var start=QuarryCranePresentation.Pickup;var end=new Vector3(5.3f,1.4f,1.2f);
            Assert.That(Vector3.Distance(start,QuarryCranePresentation.LoadPath(start,end,0)),Is.LessThan(.0001f));
            var lift=QuarryCranePresentation.LoadPath(start,end,.22f);
            Assert.That(lift.x,Is.EqualTo(start.x).Within(.0001));Assert.That(lift.z,Is.EqualTo(start.z).Within(.0001));Assert.Greater(lift.y,2.5f);
            var swing=QuarryCranePresentation.LoadPath(start,end,.46f);
            Assert.Greater(swing.y,end.y+1);Assert.Greater(Vector3.Distance(swing,QuarryCranePresentation.Pivot),2);
            var overCart=QuarryCranePresentation.LoadPath(start,end,.70f);
            Assert.That(overCart.x,Is.EqualTo(end.x).Within(.0001));Assert.That(overCart.z,Is.EqualTo(end.z).Within(.0001));
            Assert.That(Vector3.Distance(end,QuarryCranePresentation.LoadPath(start,end,1)),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(end,QuarryCranePresentation.ReturnPath(end,start,0)),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(start,QuarryCranePresentation.ReturnPath(end,start,1)),Is.LessThan(.0001f));
        }
        [Test] public void LegacyDefaultUpgradePreservesLoadFractionAndCustomTiming()
        {
            var site=new DistrictStoneSite{Phase="loading",Elapsed=2,Script=new QuarryScript{loadingSeconds=4}};
            Assert.True(DistrictQuarry.UpgradeCraneLoading(site));Assert.AreEqual(16,site.Script.loadingSeconds);Assert.AreEqual(8,site.Elapsed);
            var reload=JsonUtility.FromJson<DistrictStoneSite>(JsonUtility.ToJson(site));
            Assert.False(DistrictQuarry.UpgradeCraneLoading(reload));Assert.AreEqual(8,reload.Elapsed);
            var custom=new DistrictStoneSite{Script=new QuarryScript{loadingSeconds=7}};
            DistrictQuarry.UpgradeCraneLoading(custom);Assert.AreEqual(7,custom.Script.loadingSeconds);
        }
        [Test] public void SlowLoadingCreditsOnlyAtLandingAndResumesAfterPauseAndReload()
        {
            var d=new RegionCityTile{Width=1,Height=1,Treasury=2000};var site=new DistrictStoneSite{NormalizedX=.5f,NormalizedZ=.5f};d.StoneSites.Add(site);
            Assert.True(DistrictQuarry.Build(d,site,_=>true));DistrictQuarry.Tick(d,68,_=>true);
            Assert.AreEqual("loading",site.Phase);Assert.AreEqual(8,site.Elapsed);Assert.AreEqual(0,d.ResourceInventory.Stone);
            site.Enabled=false;DistrictQuarry.Tick(d,10,_=>true);Assert.AreEqual(8,site.Elapsed);
            d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));site=d.StoneSites[0];site.Enabled=true;
            DistrictQuarry.Tick(d,7,_=>true);Assert.AreEqual(0,site.CartBlocks);
            DistrictQuarry.Tick(d,1,_=>true);Assert.AreEqual(1,site.CartBlocks);Assert.AreEqual(0,d.ResourceInventory.Stone);
            DistrictQuarry.Tick(d,0,_=>true);Assert.AreEqual(0,d.ResourceInventory.Stone);
        }
    }
}
