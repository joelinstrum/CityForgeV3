using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CityForgeV3.Behaviors;
using CityForgeV3.World;
namespace CityForgeV3.Tests.EditMode
{
 public class CargoLoadingBehaviorTests
 {
  [Test] public void RepeatWaitsThenResetsCargoAndCrewButKeepsCycleCount()
  {
   var d=new CargoLoadingDefinition{capacity=1,repeat=true,restartDelaySeconds=60};var s=CargoLoadingSimulation.Create(d);
   CargoLoadingSimulation.Step(d,s,100,1,true,true,10);Assert.IsTrue(s.Departed);Assert.AreEqual(1,s.CompletedCycles);
   CargoLoadingSimulation.Step(d,s,59,1,true,true,10);Assert.IsTrue(s.Departed);
   s=JsonUtility.FromJson<CargoLoadingState>(JsonUtility.ToJson(s));
   CargoLoadingSimulation.Step(d,s,1,1,true,true,10);Assert.IsFalse(s.Departed);Assert.IsFalse(s.Departing);Assert.AreEqual(0,s.Loaded);Assert.AreEqual(0,s.Distance);Assert.AreEqual(1,s.CompletedCycles);Assert.IsTrue(s.Workers.All(w=>w.Phase=="pickup"&&!w.Carrying));
   CargoLoadingSimulation.Step(d,s,100,1,true,true,10);Assert.AreEqual(2,s.CompletedCycles);
  }
  [Test] public void RepeatWithoutRiverNeverDispatchesOrResetsFullBoat()
  {var d=new CargoLoadingDefinition{capacity=1,repeat=true,restartDelaySeconds=1};var s=CargoLoadingSimulation.Create(d);CargoLoadingSimulation.Step(d,s,200,1,true,false,0);Assert.AreEqual(1,s.Loaded);Assert.IsFalse(s.Departed);Assert.AreEqual(0,s.CompletedCycles);}
  [Test] public void NonRepeatingShipmentRemainsFinished()
  {var d=new CargoLoadingDefinition{capacity=1};var s=CargoLoadingSimulation.Create(d);CargoLoadingSimulation.Step(d,s,100,1,true,true,10);CargoLoadingSimulation.Step(d,s,100,1,true,true,10);Assert.IsTrue(s.Departed);Assert.AreEqual(1,s.CompletedCycles);}
  [Test] public void InvalidRepeatDelayRejected()
  {Assert.Throws<ArgumentException>(()=>new CargoLoadingDefinition{restartDelaySeconds=0}.Validate());}
  [Test] public void EntireBoatFootprintMustFitWaterIncludingRotatedBow()
  {
   var go=MakeRiver(false,50,out var w);try {
    Assert.IsTrue(w.BoatFootprintOnRiver(Vector3.zero,Quaternion.identity,3.25f,10));
    Assert.IsFalse(w.BoatFootprintOnRiver(new Vector3(9,0,0),Quaternion.identity,3.25f,10));
    Assert.IsFalse(w.BoatFootprintOnRiver(new Vector3(6,0,0),Quaternion.Euler(0,90,0),3.25f,10));
   } finally {UnityEngine.Object.DestroyImmediate(go);}
  }
  [Test] public void LotWithoutBoatsDoesNotNeedRiver()
  {var go=MakeRiver(false,30,out var w);try{Assert.IsTrue(w.ValidateLotBoatPlacement(new RegionCityTile(),new PlacedDistrictLot(),new LotSaveData(),out _));}finally{UnityEngine.Object.DestroyImmediate(go);}}
  [Test] public void ShoreArrowRequiresDryTailAndWetTipInPlacementRotation()
  {
   var go=MakeRiver(false,50,out var w);try {
    var district=new RegionCityTile{Width=1,Height=1};var placement=new PlacedDistrictLot{GridX=31,GridZ=31};
    var lot=new LotSaveData{LotWidthCells=2,LotDepthCells=2,HasWaterOrientation=true,WaterOrientationLand=new Vector3(15,0,0),WaterOrientationWater=Vector3.zero};
    Assert.IsTrue(w.ValidateLotBoatPlacement(district,placement,lot,out _));
    placement.RotationQuarterTurns=1;Assert.IsFalse(w.ValidateLotBoatPlacement(district,placement,lot,out _));
    lot.WaterOrientationLand=new Vector3(0,0,15);Assert.IsTrue(w.ValidateLotBoatPlacement(district,placement,lot,out _));
    placement.RotationQuarterTurns=0;lot.WaterOrientationLand=Vector3.zero;Assert.IsFalse(w.ValidateLotBoatPlacement(district,placement,lot,out _));
    lot.WaterOrientationLand=new Vector3(15,0,0);lot.WaterOrientationWater=new Vector3(18,0,0);Assert.IsFalse(w.ValidateLotBoatPlacement(district,placement,lot,out _));
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [Test] public void ShoreArrowSurvivesCopyAndSave()
  {var lot=new LotSaveData{HasWaterOrientation=true,WaterOrientationLand=new Vector3(4,0,8),WaterOrientationWater=new Vector3(9,0,3)};var copy=JsonUtility.FromJson<LotSaveData>(JsonUtility.ToJson(lot.Copy()));Assert.IsTrue(copy.HasWaterOrientation);Assert.AreEqual(lot.WaterOrientationLand,copy.WaterOrientationLand);Assert.AreEqual(lot.WaterOrientationWater,copy.WaterOrientationWater);}
  [Test] public void RotatedRectangularLotCenterMatchesRotatedFootprint()
  {var c=DistrictWorldController.DistrictLotCenterMeters(new RegionCityTile{Width=1,Height=1},new PlacedDistrictLot{GridX=30,GridZ=30,RotationQuarterTurns=1},new LotSaveData{LotWidthCells=4,LotDepthCells=3});Assert.AreEqual(new Vector2(-5,0),c);}
  [Test] public void DecorativeArrowPositionDoesNotOverrideActualMillAndBoatAnchors()
  {
   var go=MakeRiver(false,50,out var w);try{
    var d=new LotSaveData{HasWaterOrientation=true,WaterOrientationLand=new Vector3(100,0,100),WaterOrientationWater=new Vector3(90,0,100)};
    d.Buildings3D.Add(new PlacedBuilding3D{X=15,Z=0});d.Props.Add(new PlacedProp{PropId="wooden-lumber-barge-v01",PositionX=5,PositionZ=0});
    var placement=new PlacedDistrictLot{GridX=31,GridZ=31};
    Assert.IsTrue(w.ValidateLotBoatPlacement(new RegionCityTile{Width=1,Height=1},placement,d,out _));
    d.WaterOrientationWater=new Vector3(110,0,100);Assert.IsFalse(w.ValidateLotBoatPlacement(new RegionCityTile{Width=1,Height=1},new PlacedDistrictLot{GridX=31,GridZ=31},d,out _));
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [Test] public void ShorelineAlignmentOffsetSurvivesSaveAndMovesLotCenter()
  {var p=new PlacedDistrictLot{GridX=31,GridZ=31,ShoreOffsetX=2,ShoreOffsetZ=-4};p=JsonUtility.FromJson<PlacedDistrictLot>(JsonUtility.ToJson(p));Assert.AreEqual(new Vector2(2,-4),DistrictWorldController.DistrictLotCenterMeters(new RegionCityTile{Width=1,Height=1},p,new LotSaveData{LotWidthCells=2,LotDepthCells=2}));}
  [Test] public void PositiveShoreOffsetMovesCenterWithoutChangingFootprintSpan()
  {var p=new PlacedDistrictLot{GridX=31,GridZ=31,ShoreOffsetX=2,ShoreOffsetZ=4};Assert.AreEqual(new Vector2(2,4),DistrictWorldController.DistrictLotCenterMeters(new RegionCityTile{Width=1,Height=1},p,new LotSaveData{LotWidthCells=2,LotDepthCells=2}));}
  [Test] public void LegacyDistrictDockOverrideRotatesButDoesNotMoveTheAuthoredBarge()
  {
   var source=new LotSaveData();source.Props.Add(new PlacedProp{InstanceId="boat",PropId="wooden-lumber-barge-v01",PositionX=7,PositionZ=-4});
   var placement=JsonUtility.FromJson<PlacedDistrictLot>(JsonUtility.ToJson(new PlacedDistrictLot{HasBoatDockOverride=true,BoatDockLocalX=2,BoatDockLocalZ=3,BoatDockRotationQuarterTurns=1,BoatMooringLocalX=4,BoatMooringLocalZ=5}));
   var go=new GameObject("runtime dock override");var world=go.AddComponent<LotWorldController>();
   try{world.LoadRuntimeLot(source);world.ApplyDistrictBoatDockOverride(placement);var session=(LotEditorSession)typeof(LotWorldController).GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(world);Assert.AreEqual(7,session.Data.Props[0].PositionX);Assert.AreEqual(-4,session.Data.Props[0].PositionZ);Assert.AreEqual(1,session.Data.Props[0].RotationQuarterTurns);var dock=LotObjectRegistry.ResolvePoint(session.Data,new LotScriptPoint{objectId="boat",offset=new Vector3(-2,.06f,-2.5f)});Assert.That(Vector3.Distance(dock,new Vector3(4.5f,.06f,-2)),Is.LessThan(.001f));Assert.AreEqual(7,source.Props[0].PositionX);Assert.AreEqual(-4,source.Props[0].PositionZ);Assert.AreEqual(0,source.Props[0].RotationQuarterTurns);}
   finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [Test] public void ScriptDockRemainsAttachedToAuthoredBarge()
  {
   var data=new LotSaveData();data.Props.Add(new PlacedProp{InstanceId="barge",PropId="wooden-lumber-barge-v01",PositionX=7,PositionZ=-4});
   var point=new LotScriptPoint{objectId="barge",offset=new Vector3(-2,.06f,-2.5f)};
   Assert.AreEqual(new Vector3(5,.06f,-6.5f),LotObjectRegistry.ResolvePoint(data,point));
  }
  [TestCase(1)] [TestCase(12)] [TestCase(13)] public void WorkersNeverOverfillAndWaitWithoutRiver(int capacity){var d=new CargoLoadingDefinition{capacity=capacity};var s=CargoLoadingSimulation.Create(d);for(int i=0;i<4000;i++){CargoLoadingSimulation.Step(d,s,.1f,2,true,false,0);Assert.LessOrEqual(s.Loaded+s.Workers.Count(x=>x.Carrying),capacity);}Assert.AreEqual(capacity,s.Loaded);Assert.IsTrue(s.Workers.All(x=>x.Phase=="idle"));Assert.IsFalse(s.Departing);}
  [Test] public void FullBoatDepartsOnlyAfterWorkersReturn(){var d=new CargoLoadingDefinition{capacity=2};var s=CargoLoadingSimulation.Create(d);for(int i=0;i<1000&&!s.Departing;i++)CargoLoadingSimulation.Step(d,s,.1f,3,true,true,100);Assert.IsTrue(s.Departing);Assert.IsTrue(s.Workers.All(x=>x.Phase=="idle"&&!x.Carrying));Assert.AreEqual(2,s.Loaded);}
  [Test] public void RouteLossPausesShipmentWithoutLosingCargo(){var d=new CargoLoadingDefinition{capacity=1};var s=CargoLoadingSimulation.Create(d);CargoLoadingSimulation.Step(d,s,100,1,true,true,1000);Assert.IsTrue(s.Departing);var distance=s.Distance;CargoLoadingSimulation.Step(d,s,20,1,true,false,0);Assert.AreEqual(distance,s.Distance);Assert.AreEqual(1,s.Loaded);CargoLoadingSimulation.Step(d,s,2,1,true,true,1000);Assert.Greater(s.Distance,distance);}
  [Test] public void MissingBoatDoesNotAdvanceWorkers(){var d=new CargoLoadingDefinition();var s=CargoLoadingSimulation.Create(d);var before=JsonUtility.ToJson(s);CargoLoadingSimulation.Step(d,s,100,1,false,true,100);Assert.AreEqual(before,JsonUtility.ToJson(s));}
  [Test] public void SaveRoundTripRetainsInFlightBundlesAndDoesNotDuplicate(){var d=new CargoLoadingDefinition{capacity=3};var s=CargoLoadingSimulation.Create(d);CargoLoadingSimulation.Step(d,s,3,4,true,false,0);Assert.IsTrue(s.Workers.Any(x=>x.Carrying));s=JsonUtility.FromJson<CargoLoadingState>(JsonUtility.ToJson(s));CargoLoadingSimulation.Step(d,s,200,4,true,false,0);Assert.AreEqual(3,s.Loaded);Assert.IsFalse(s.Workers.Any(x=>x.Carrying));}
  [Test] public void PositiveRouteLengthIsRequired(){var d=new CargoLoadingDefinition{capacity=1};var s=CargoLoadingSimulation.Create(d);CargoLoadingSimulation.Step(d,s,100,1,true,true,0);Assert.IsFalse(s.Departing);}
  [Test] public void InvalidModDefinitionRejected(){Assert.Throws<ArgumentException>(()=>new CargoLoadingDefinition{workers=0}.Validate());Assert.Throws<ArgumentException>(()=>new CargoLoadingDefinition{walkSpeed=float.NaN}.Validate());}
  [Test] public void OldLotsHaveNoAutomaticCrew(){var data=JsonUtility.FromJson<LotSaveData>("{\"Name\":\"old\"}");Assert.IsTrue(data.Behaviors==null||data.Behaviors.Count==0);}
  [TestCase(false)] [TestCase(true)] public void RiverRouteFollowsAuthoredDownstreamDirection(bool reversed){var go=MakeRiver(reversed,50,out var world);try{var route=world.FindDownstreamBoatRoute(Vector3.zero,3.25f,10);Assert.IsNotNull(route);Assert.That(route.Last().z,Is.EqualTo(reversed?-50:50).Within(.01));Assert.IsNull(world.FindDownstreamBoatRoute(new Vector3(20,0,0),3.25f,10));}finally{UnityEngine.Object.DestroyImmediate(go);}}
  [Test] public void ClosedRiverWithoutExitCannotDispatch(){var go=MakeRiver(false,30,out var world);try{Assert.IsNull(world.FindDownstreamBoatRoute(Vector3.zero,3.25f,10));}finally{UnityEngine.Object.DestroyImmediate(go);}}
  [Test] public void PlacedMillOwnsStateAndSurvivesReloadWithoutSharingTemplate(){
    var go=new GameObject("behavior persistence test");var w=go.AddComponent<LotWorldController>();
    try { var session=(LotEditorSession)typeof(LotWorldController).GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(w);
      var d=new CargoLoadingDefinition();var template=new LotBehaviorInstance{InstanceId="routine",State=CargoLoadingSimulation.Create(d)};template.State.Loaded=12;
      session.Data.Behaviors=new List<LotBehaviorInstance>{template};var placed=new PlacedDistrictLot();w.BindDistrictBehaviors(placed);
      Assert.AreEqual(0,placed.Behaviors[0].State.Loaded,"Fresh placement starts a fresh shipment");
      placed.Behaviors[0].State.Loaded=5;Assert.AreEqual(5,w.LotBehaviors[0].State.Loaded);
      var restored=JsonUtility.FromJson<PlacedDistrictLot>(JsonUtility.ToJson(placed));w.BindDistrictBehaviors(restored);
      Assert.AreEqual(5,w.LotBehaviors[0].State.Loaded);Assert.IsTrue(restored.BehaviorsInitialized);
    } finally { UnityEngine.Object.DestroyImmediate(go); }
  }
  [Test] public void DepartureRouteAndDistanceSurviveSave(){var b=new LotBehaviorInstance{State=new CargoLoadingState{Departing=true,Distance=32,Loaded=12},DepartureRoute=new List<Vector3>{Vector3.zero,new Vector3(0,0,100)}};var copy=JsonUtility.FromJson<LotBehaviorInstance>(JsonUtility.ToJson(b));Assert.AreEqual(32,copy.State.Distance);Assert.AreEqual(new Vector3(0,0,100),copy.DepartureRoute[1]);}
  static GameObject MakeRiver(bool reversed,float end,out DistrictWorldController world){var go=new GameObject("river route test");world=go.AddComponent<DistrictWorldController>();var f=BindingFlags.NonPublic|BindingFlags.Instance;var t=typeof(DistrictWorldController);t.GetField("_content",f).SetValue(world,go.transform);t.GetField("_widthMeters",f).SetValue(world,100f);t.GetField("_depthMeters",f).SetValue(world,100f);var points=new List<Vector2>{new Vector2(0,-end),new Vector2(0,0),new Vector2(0,end)};if(reversed)points.Reverse();var nested=t.GetNestedType("RuntimeRiverSurface",BindingFlags.NonPublic);var river=Activator.CreateInstance(nested,new object[]{points,12f,10f,8f,0f,.1f,1f,false});((IList)t.GetField("_riverSurfaces",f).GetValue(world)).Add(river);var index=t.GetField("_riverSurfaceIndex",f).GetValue(world);index.GetType().GetMethod("Add").Invoke(index,new[]{(object)new Rect(-12,-end-12,24,end*2+24),river,true});return go;}
 }
}
