using System.Collections.Generic;using System.Linq;using CityForgeV3.World;using NUnit.Framework;using UnityEngine;
namespace CityForgeV3.Tests.EditMode
{
 public class DistrictBrickworksTests
 {
  RegionCityTile District()=>new(){Width=1,Height=1,TileId="brickworks-test"};
  DistrictStoneSite Quarry(RegionCityTile d)
  {var s=new DistrictStoneSite{NormalizedX=.5f,NormalizedZ=.5f};d.StoneSites.Add(s);Assert.True(DistrictQuarry.Build(d,s,_=>true));return s;}
  DistrictBrickworksSite Works(RegionCityTile d)=>new(){NormalizedX=.5f+40/640f,NormalizedZ=.5f+60/640f};
  [Test] public void QuarryPrerequisiteAndPlacementAreEnforcedBeforeClearingTrees()
  {
   var d=District();var b=Works(d);d.Flora.Add(new(){NormalizedX=b.NormalizedX,NormalizedZ=b.NormalizedZ});
   Assert.False(DistrictBrickworks.Build(d,b,_=>true));Assert.AreEqual(1,d.Flora.Count);
   Quarry(d);Assert.False(DistrictBrickworks.Build(d,b,_=>false));Assert.True(DistrictBrickworks.Build(d,b,_=>true));Assert.AreEqual(0,d.Flora.Count);
   Assert.False(DistrictBrickworks.Build(d,Works(d),_=>true));
  }
  [Test] public void FullWagonRetainsCargoWithoutBrickworksAndCanDepartAfterOneIsBuilt()
  {
   var d=District();var s=Quarry(d);s.Script.miningSeconds=1;s.Script.loadingSeconds=1;
   DistrictQuarry.Tick(d,8,_=>true);Assert.AreEqual("full",s.Phase);Assert.AreEqual(4,s.CargoStoneTons);
   DistrictQuarry.Tick(d,100,_=>true);Assert.AreEqual(4,s.CartBlocks);Assert.AreEqual(4,d.ResourceInventory.Stone);
   DistrictQuarryDelivery.Tick(d,s,8,_=>null,_=>false);DistrictQuarryDelivery.Tick(d,s,1,_=>null,_=>false);
   StringAssert.StartsWith("Bricksworks required",s.DeliveryStatus);Assert.AreEqual(4,s.CartBlocks);
   var b=Works(d);DistrictBrickworks.Build(d,b,_=>true);
   DistrictQuarryDelivery.Tick(d,s,4,_=>new(){Id=b.Id,Point=DistrictBrickworks.ReceivingPoint(d,b),Route=new(){Vector2.zero}},_=>false);
   Assert.AreEqual("delivering",s.Phase);Assert.AreEqual(0,b.StoneInput);
  }
  [Test] public void DeliveryReloadPauseAndConversionDoNotDuplicateMaterial()
  {
   var d=District();var s=Quarry(d);var b=Works(d);DistrictBrickworks.Build(d,b,_=>true);
   s.Phase="delivering";s.CargoStoneTons=4;s.CartBlocks=4;s.DeliveryTargetId=b.Id;d.ResourceInventory.Stone=4;
   DistrictQuarryDelivery.Tick(d,s,1,_=>null,_=>true);Assert.AreEqual("unloading",s.Phase);
   s.Enabled=false;DistrictQuarryDelivery.Tick(d,s,100,_=>null,_=>true);Assert.AreEqual(0,b.StoneInput);s.Enabled=true;
   DistrictQuarryDelivery.Tick(d,s,4,_=>null,_=>true);d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));s=d.StoneSites[0];b=d.Brickworks[0];
   DistrictQuarryDelivery.Tick(d,s,4,_=>null,_=>true);Assert.AreEqual(4,b.StoneInput);Assert.AreEqual(0,s.CartBlocks);Assert.AreEqual("returning",s.Phase);
   DistrictQuarryDelivery.Tick(d,s,1,_=>null,_=>true);Assert.AreEqual("mining",s.Phase);Assert.AreEqual(4,b.StoneInput);
   DistrictBrickworks.Tick(d,29);Assert.AreEqual(0,d.ResourceInventory.Bricks);DistrictBrickworks.Tick(d,1);Assert.AreEqual(1,d.ResourceInventory.Bricks);Assert.AreEqual(3,d.ResourceInventory.Stone);
   DistrictBrickworks.Tick(d,90);Assert.AreEqual(4,d.ResourceInventory.Bricks);Assert.AreEqual(0,b.StoneInput);Assert.AreEqual(0,d.ResourceInventory.Stone);
   DistrictBrickworks.Tick(d,100);Assert.AreEqual(4,d.ResourceInventory.Bricks);
  }
  [Test] public void RemovedDestinationKeepsCargoAndPausedBrickworksDoesNotProcess()
  {
   var d=District();var s=Quarry(d);s.Phase="delivering";s.CartBlocks=4;s.CargoStoneTons=4;s.DeliveryTargetId="removed";
   DistrictQuarryDelivery.Tick(d,s,1,_=>null,_=>false);Assert.AreEqual("full",s.Phase);Assert.AreEqual(4,s.CartBlocks);
   var b=Works(d);b.Enabled=false;b.StoneInput=4;d.Brickworks.Add(b);d.ResourceInventory.Stone=4;DistrictBrickworks.Tick(d,100);Assert.AreEqual(0,d.ResourceInventory.Bricks);
  }
  [Test] public void ConnectedRoadsReachBrickworksButDisconnectedOrFloodedRoadsDoNot()
  {
   var d=District();var s=Quarry(d);var b=Works(d);Assert.True(DistrictBrickworks.Build(d,b,_=>true));
   for(int z=32;z<=41;z++)d.Roads.Add(new(){GridX=34,GridZ=z});
   var from=DistrictBrickworks.QuarryHome(d,s);var nav=new DistrictQuarryNavigation(d,s,_=>false);
   var destinations=nav.Destinations(from).ToList();Assert.IsNotEmpty(destinations);
   foreach(var route in destinations.Select(t=>t.Route))for(int i=1;i<route.Count;i++)Assert.Greater(Vector2.Distance(route[i],route[i-1]),.01f,"Duplicate junctions prevent wagon corner rounding");
   d.Roads.RemoveAll(r=>r.GridZ==36);Assert.IsEmpty(new DistrictQuarryNavigation(d,s,_=>false).Destinations(from).ToList());
   Assert.IsEmpty(new DistrictQuarryNavigation(d,s,_=>true).Destinations(from).ToList());
  }
 }
}
