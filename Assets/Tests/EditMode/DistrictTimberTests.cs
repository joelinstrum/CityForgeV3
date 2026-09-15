using System;
using System.Collections.Generic;
using System.Linq;
using CityForgeV3.Behaviors;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public class DistrictTimberTests
    {
        [Test] public void DropCreatesSeparateCrewsAtExactPointsAndChargesOnce()
        {
            var d=new RegionCityTile{Treasury=2000};
            var a=DistrictTimber.Place(d,new Vector2(12,14),new Vector2(15,15),2,new());
            var b=DistrictTimber.Place(d,new Vector2(-20,8),new Vector2(-15,5),1,new());
            Assert.AreEqual(1250,d.Treasury);Assert.AreNotEqual(a.WagonId,b.WagonId);
            Assert.True(d.Labor.Workers.Where(w=>w.CrewId==a.Id).All(w=>w.Position==a.Camp));
            Assert.AreEqual(b.Camp,d.Labor.Workers.Single(w=>w.CrewId==b.Id).Position);
            Assert.AreEqual(3,d.Labor.AssignedAxemen);
            Assert.Null(DistrictTimber.Place(d,Vector2.zero,Vector2.zero,16,new()));
            Assert.AreEqual(2,d.Labor.TimberCrews.Count);
        }
        [Test] public void FullLoadWaitsForRoadThenUnloadsExactlyOnceAcrossReload()
        {
            var d=new RegionCityTile{Treasury=2000,Lots=new(){new(){InstanceId="mill"}}};
            var c=DistrictTimber.Place(d,Vector2.zero,Vector2.zero,1,new());c.PendingTrees=3;
            bool Move(DistrictTimberCrew x,float dt){x.WagonPosition=x.Destination;return true;}
            TimberDestination Target(DistrictTimberCrew x)=>new(){MillId="mill",Point=Vector2.right*20,Route=new(){Vector2.right*20}};
            DistrictTimber.Tick(d,c,1,_=>null,(_,p)=>new(){p},Move);
            Assert.AreEqual(3,c.CargoTrees);Assert.AreEqual(0,c.PendingTrees);
            DistrictTimber.Tick(d,c,1,_=>null,(_,p)=>new(){p},Move);
            Assert.AreEqual("dispatch",c.Phase);Assert.AreEqual(0,d.Lots[0].TimberBundles);
            DistrictTimber.Tick(d,c,6,Target,(_,p)=>new(){p},Move);
            DistrictTimber.Tick(d,c,1,Target,(_,p)=>new(){p},Move);
            Assert.AreEqual("unload",c.Phase);
            d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));c=d.Labor.TimberCrews[0];
            DistrictTimber.Tick(d,c,3,Target,(_,p)=>new(){p},Move);
            Assert.AreEqual(12,d.Lots[0].TimberBundles);Assert.Zero(c.CargoTrees);
            for(int i=0;i<10;i++)DistrictTimber.Tick(d,c,1,Target,(_,p)=>new(){p},Move);
            Assert.AreEqual(12,d.Lots[0].TimberBundles);Assert.AreEqual(1,c.DeliveredLoads);Assert.AreEqual("gather",c.Phase);
        }
        [Test] public void PauseFreezesCargoAndDeletingDestinationCannotGrantTimber()
        {
            var d=new RegionCityTile{Treasury=1000};var c=DistrictTimber.Place(d,Vector2.zero,Vector2.zero,1,new());
            c.PendingTrees=3;c.Enabled=false;
            DistrictTimber.Tick(d,c,10,_=>null,(_,_)=>null,(_,_)=>false);Assert.AreEqual(3,c.PendingTrees);
            c.Enabled=true;c.Phase="unload";c.CargoTrees=3;c.MillId="deleted";
            DistrictTimber.Tick(d,c,10,_=>null,(_,_)=>null,(_,_)=>false);
            Assert.AreEqual("dispatch",c.Phase);Assert.AreEqual(3,c.CargoTrees);
        }
        [Test] public void RoadRoutingRejectsDisconnectedCellsWaterAndOffRoadStart()
        {
            var d=new RegionCityTile{Width=1,Height=1};
            for(int x=30;x<=35;x++)d.Roads.Add(new(){GridX=x,GridZ=32});
            var nav=new DistrictTimberNavigation(d,_=>false);
            Assert.NotNull(nav.Route(new(-15,5),new(35,5)));
            Assert.Null(nav.Route(new(-15,20),new(35,5)));
            d.Roads.RemoveAt(2);nav=new(d,_=>false);
            Assert.Null(nav.Route(new(-15,5),new(35,5)));
            d.Roads.Add(new(){GridX=32,GridZ=32});
            nav=new(d,p=>p.x>.5f&&p.x<.516f);
            Assert.Null(nav.Route(new(-15,5),new(35,5)));
        }
        [Test] public void FindsNearestReachableMillNotNearestDisconnectedMill()
        {
            var d=new RegionCityTile{Width=1,Height=1};
            for(int x=30;x<=40;x++)d.Roads.Add(new(){GridX=x,GridZ=32});
            d.Lots.Add(new(){InstanceId="far",LotId="far",GridX=40,GridZ=33});
            d.Lots.Add(new(){InstanceId="near-off-road",LotId="near",GridX=30,GridZ=28});
            var data=new LotSaveData{LotWidthCells=1,LotDepthCells=1,Buildings3D=new(){new(){AssetId="lumber-mill-v01"}}};
            var nav=new DistrictTimberNavigation(d,_=>false,_=>data);
            var targets=nav.Mills(new(-15,5));
            Assert.IsNotEmpty(targets);Assert.AreEqual("far",targets[0].MillId);
            Assert.False(targets.Any(x=>x.MillId=="near-off-road"));
        }
        [Test] public void DistrictNineRoadInsideShoreMillLotIsAReachableDeliveryStop()
        {
            var d=new RegionCityTile{Width=4,Height=4};
            for(int z=127;z<=132;z++)d.Roads.Add(new(){GridX=69,GridZ=z});
            for(int x=70;x<=77;x++)d.Roads.Add(new(){GridX=x,GridZ=132});
            for(int z=133;z<=139;z++)d.Roads.Add(new(){GridX=77,GridZ=z});
            d.Lots.Add(new(){InstanceId="dock-mill",LotId="dock",GridX=66,GridZ=126,ShoreOffsetX=2,ShoreOffsetZ=2});
            var lot=new LotSaveData{LotWidthCells=3,LotDepthCells=3,Buildings3D=new(){new(){AssetId="lumber-mill-v01"}}};
            var nav=new DistrictTimberNavigation(d,_=>false,_=>lot);
            var targets=nav.Mills(new(-505,95));
            Assert.IsNotEmpty(targets,"A connected road inside the lot is a valid driveway, not inaccessible land");
            Assert.AreEqual("dock-mill",targets[0].MillId);
            Assert.True(targets[0].Route.All(nav.OnRoad));
            // The same driveway must still be rejected if the road is severed.
            d.Roads.RemoveAll(r=>r.GridX==73);
            Assert.IsEmpty(new DistrictTimberNavigation(d,_=>false,_=>lot).Mills(new(-505,95)));
        }
        [Test] public void MidCellReloadContinuesForwardInsteadOfBacktrackingToCellCenter()
        {
            var d=new RegionCityTile{Width=4,Height=4};
            d.Roads.Add(new(){GridX=76,GridZ=132});
            for(int z=132;z<=139;z++)d.Roads.Add(new(){GridX=77,GridZ=z});
            var nav=new DistrictTimberNavigation(d,_=>false);
            var route=nav.Route(new(-512.94f,45),new(-505,95));
            Assert.NotNull(route);
            Assert.AreEqual(new Vector2(-505,45),route[0]);
        }
        [Test] public void WagonCompletesMillimeterArrivalWhenBrakingDistanceIsExhausted()
        {
            var root=new GameObject("Arrival regression wagon");
            var wagon=root.AddComponent<HorseCarriageController>();
            try
            {
                var horse=new GameObject("Horse").transform;horse.SetParent(root.transform,false);
                var body=new GameObject("Body").transform;body.SetParent(root.transform,false);
                var fore=new GameObject("Forecarriage Steering").transform;fore.SetParent(body,false);
                fore.localPosition=Vector3.forward*HorseWagonDefinition.Lumber.FrontAxleOffset;
                wagon.Configure(horse,body,HorseWagonDefinition.Lumber);
                root.transform.position=new Vector3(-505,0,94.997856f);
                wagon.RestoreHeadings(0,0,0);
                wagon.SetRoute(new(){new Vector3(-505,0,95)});
                var motion=wagon.CaptureMotion();motion.DistanceToEnd=0;wagon.RestoreMotion(motion);
                wagon.Step(.016f,_=>0,(_,_)=>true);
                Assert.False(wagon.IsMoving,"Exhausted float distance must not leave a wagon moving forever at its endpoint");
                Assert.False(wagon.WasBlocked);
                // Exhausted distance alone must never finish a distant stop.
                wagon.SetRoute(new(){new Vector3(-505,0,105)});
                motion=wagon.CaptureMotion();motion.DistanceToEnd=0;wagon.RestoreMotion(motion);
                wagon.Step(.016f,_=>0,(_,_)=>true);Assert.True(wagon.IsMoving);
            }
            finally
            {
                var field=typeof(HorseCarriageController).GetField("leather",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                var material=(Material)field.GetValue(wagon);field.SetValue(wagon,null);
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(material);
            }
        }
        [Test] public void MillReservesDeliveredTimberOnceAndWaitsAgainNextShipment()
        {
            var def=new CargoLoadingDefinition{repeat=true,restartDelaySeconds=1};
            var state=CargoLoadingSimulation.Create(def);var inventory=11;
            Assert.False(CargoLoadingSimulation.ReserveTimber(def,state,ref inventory));Assert.AreEqual(11,inventory);
            inventory=24;Assert.True(CargoLoadingSimulation.ReserveTimber(def,state,ref inventory));Assert.AreEqual(12,inventory);
            Assert.True(CargoLoadingSimulation.ReserveTimber(def,state,ref inventory));Assert.AreEqual(12,inventory);
            state.Departed=true;CargoLoadingSimulation.Step(def,state,1,.1f,true,true,20);
            Assert.False(state.TimberReserved);
            Assert.True(CargoLoadingSimulation.ReserveTimber(def,state,ref inventory));Assert.Zero(inventory);
        }
        [Test] public void ScriptRejectsInvalidValuesAndRoundTrips()
        {
            Assert.Throws<ArgumentException>(()=>TimberScript.Parse("{\"schema\":\"python\"}"));
            Assert.Throws<ArgumentException>(()=>new TimberScript{treesPerLoad=0}.Validate());
            Assert.Throws<ArgumentException>(()=>new TimberScript{unloadSeconds=float.NaN}.Validate());
            var script=new TimberScript{treesPerLoad=5,repeat=false};
            var read=TimberScript.Parse(JsonUtility.ToJson(script));Assert.AreEqual(5,read.treesPerLoad);Assert.False(read.repeat);
        }
        [Test] public void WorkerDeliversTreeToOwnCrewAndDoesNotDoubleCreditAfterReload()
        {
            var d=new RegionCityTile{Treasury=1000};
            var c=DistrictTimber.Place(d,new(0,-5),new(0,-5),1,new());
            d.Flora.Add(new(){InstanceId="tree",FloraId="cilician-fir",NormalizedX=.5f,NormalizedZ=.5f});
            for(int i=0;i<500;i++)DistrictLabor.Tick(d,.1f,(_,p)=>new(){p},_=>true);
            Assert.AreEqual(1,c.PendingTrees);Assert.AreEqual(0,d.Labor.Wood);
            d=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
            for(int i=0;i<100;i++)DistrictLabor.Tick(d,.1f,(_,p)=>new(){p},_=>true);
            Assert.AreEqual(1,d.Labor.TimberCrews[0].PendingTrees);Assert.AreEqual(0,d.Labor.Wood);
        }
    }
}
