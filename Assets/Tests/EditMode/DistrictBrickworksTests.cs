using System.Collections.Generic;using System.Linq;using CityForgeV3.World;using NUnit.Framework;using UnityEngine;
namespace CityForgeV3.Tests.EditMode
{
 public class DistrictBrickworksTests
 {
  RegionCityTile District()=>new(){Width=1,Height=1,TileId="brickworks-test"};
  DistrictStoneSite Quarry(RegionCityTile d)
  {var s=new DistrictStoneSite{NormalizedX=.5f,NormalizedZ=.5f};d.StoneSites.Add(s);Assert.True(DistrictQuarry.Build(d,s,_=>true));return s;}
  DistrictBrickworksSite Works(RegionCityTile d)=>new(){NormalizedX=.5f+40/640f,NormalizedZ=.5f+60/640f};
  [Test] public void RoadDeliveryIgnoresTreesAndStopsAtConnectedRoadTileNearBuilding()
  {
   var d=District();var q=Quarry(d);var b=Works(d);d.Brickworks.Add(b);
   for(int z=32;z<=37;z++){d.Roads.Add(new(){GridX=33,GridZ=z});d.Flora.Add(new(){NormalizedX=.5f+15/640f,NormalizedZ=.5f+(z*10-315)/640f});}
   var nav=new DistrictQuarryNavigation(d,q,_=>true);
   var target=nav.Destinations(DistrictBrickworks.QuarryHome(d,q)).Single();
   Assert.AreNotEqual(DistrictBrickworks.ReceivingPoint(d,b),target.Point);
   foreach(var point in target.Route)Assert.True(d.Roads.Any(road=>Vector2.Distance(point,new Vector2(road.GridX*10-315,road.GridZ*10-315))<.01f));
   Assert.IsNotNull(nav.Route(target.Point,DistrictBrickworks.QuarryHome(d,q)));
   d.Roads.RemoveAll(road=>road.GridZ==33);
   Assert.IsEmpty(new DistrictQuarryNavigation(d,q,_=>false).Destinations(DistrictBrickworks.QuarryHome(d,q)));
  }
  [Test] public void GenericRoadDeliveryRejectsDistantBuildingsAndUsesReachableTile()
  {
   var d=District();d.Roads.Add(new(){GridX=32,GridZ=32});d.Roads.Add(new(){GridX=33,GridZ=32});
   d.Roads.Add(new(){GridX=35,GridZ=32}); // nearer destination, but disconnected
   var roads=new DistrictRoadDelivery(d);
   Assert.AreEqual(new Vector2(15,5),roads.Route(new Vector2(5,5),new Rect(35,5,0,0)).Last());
   Assert.IsNull(roads.Route(new Vector2(5,5),new Rect(200,200,10,10)));
   Assert.IsNull(roads.Route(new Vector2(-200,-200),new Rect(5,5,0,0)));
  }
  [Test] public void OperationalWarningsFollowDeliveryStateAndClearAfterRecovery()
  {
   var d=District();var q=Quarry(d);
   StringAssert.Contains("No Brickworks",DistrictQuarry.OperationalWarning(d,q));
   var b=Works(d);d.Brickworks.Add(b);
   Assert.AreEqual("",DistrictQuarry.OperationalWarning(d,q));
   q.DeliveryStatus="Bricksworks required — clear the road";
   StringAssert.Contains("delivery is blocked",DistrictQuarry.OperationalWarning(d,q));
   Assert.AreEqual("",DistrictBrickworks.OperationalWarning(d,b),"Unassigned route failure must not be blamed on this Brickworks");
   q.DeliveryTargetId=b.Id;
   StringAssert.Contains("delivery problem",DistrictBrickworks.OperationalWarning(d,b));
   q.DeliveryStatus="Delivering stone to Brickworks";q.Phase="delivering";
   Assert.AreEqual("",DistrictQuarry.OperationalWarning(d,q));
   Assert.AreEqual("",DistrictBrickworks.OperationalWarning(d,b));
   q.Phase="returning";q.DeliveryStatus="Return route blocked — clear the road";
   StringAssert.Contains("cannot return",DistrictQuarry.OperationalWarning(d,q));
   q.DeliveryStatus="Returning to quarry";Assert.AreEqual("",DistrictQuarry.OperationalWarning(d,q));
   q.Phase="mining";b.Enabled=false;
   StringAssert.Contains("All Brickworks are paused",DistrictQuarry.OperationalWarning(d,q));
   StringAssert.Contains("paused",DistrictBrickworks.OperationalWarning(d,b));
   b.Enabled=true;q.Enabled=false;
   StringAssert.Contains("No working quarry",DistrictBrickworks.OperationalWarning(d,b));
   b.StoneInput=4;Assert.AreEqual("",DistrictBrickworks.OperationalWarning(d,b));
  }
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
 }
}
