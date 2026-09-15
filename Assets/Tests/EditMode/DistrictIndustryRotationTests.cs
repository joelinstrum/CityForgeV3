using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
namespace CityForgeV3.Tests.EditMode
{
    public class DistrictIndustryRotationTests
    {
        RegionCityTile District() => new() { Width=1, Height=1 };
        DistrictStoneSite Quarry(RegionCityTile d, string phase)
        {
            var q=new DistrictStoneSite { Id="q", Built=true, NormalizedX=.5f, NormalizedZ=.5f, Phase=phase,
                HasWagonPose=true, WagonPosition=new Vector2(5,4), CargoStoneTons=4, CartBlocks=4 };
            d.StoneSites.Add(q); return q;
        }
        [Test] public void ParkedWagonRotatesWithLoadingBayAndPreservesCargo()
        {
            var d=District(); var q=Quarry(d,"full");
            DistrictIndustryRotation.Apply(d,new(DistrictSelectionKind.Entity,"quarry:"+q.Id),1);
            Assert.AreEqual(90,q.Yaw); Assert.That(Vector2.Distance(q.WagonPosition,new Vector2(4,-5)),Is.LessThan(.001));
            Assert.AreEqual(90,q.HorseHeading); Assert.AreEqual(4,q.CargoStoneTons); Assert.AreEqual("full",q.Phase);
            for(int i=0;i<3;i++)DistrictIndustryRotation.Apply(d,new(DistrictSelectionKind.Entity,"quarry:"+q.Id),1);
            Assert.AreEqual(0,q.Yaw); Assert.That(Vector2.Distance(q.WagonPosition,new Vector2(5,4)),Is.LessThan(.001));
        }
        [TestCase("delivering")] [TestCase("unloading")] [TestCase("returning")]
        public void TravelingWagonKeepsPoseAndReturningDestinationFollowsQuarry(string phase)
        {
            var d=District(); var q=Quarry(d,phase);var before=q.WagonPosition;
            DistrictIndustryRotation.Apply(d,new(DistrictSelectionKind.Entity,"quarry:"+q.Id),-1);
            Assert.AreEqual(270,q.Yaw); Assert.AreEqual(before,q.WagonPosition);Assert.AreEqual(0,q.HorseHeading);
            Assert.AreEqual(phase,q.Phase);Assert.AreEqual(4,q.CargoStoneTons);
            if(phase=="returning")Assert.AreEqual(DistrictBrickworks.QuarryHome(d,q),q.DeliveryDestination);
        }
        [Test] public void RotatingReceiverRedirectsUnloadingWagonWithoutCreditingCargoAndSurvivesReload()
        {
            var d=District();var q=Quarry(d,"unloading");var b=new DistrictBrickworksSite{Id="b",NormalizedX=.6f,NormalizedZ=.6f,StoneInput=2};d.Brickworks.Add(b);
            q.DeliveryTargetId=b.Id;q.Elapsed=7;var position=q.WagonPosition;
            DistrictIndustryRotation.Apply(d,new(DistrictSelectionKind.Entity,"brickworks:"+b.Id),1);
            var copy=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));q=copy.StoneSites[0];b=copy.Brickworks[0];
            Assert.AreEqual(90,b.Yaw);Assert.AreEqual(2,b.StoneInput);Assert.AreEqual(4,q.CargoStoneTons);Assert.AreEqual(position,q.WagonPosition);
            Assert.AreEqual("delivering",q.Phase);Assert.AreEqual(0,q.Elapsed);Assert.AreEqual(DistrictBrickworks.ReceivingPoint(copy,b),q.DeliveryDestination);
        }
    }
}
