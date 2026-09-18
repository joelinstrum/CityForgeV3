using System;
using System.Collections.Generic;
using System.Diagnostics;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class DistrictBridgeTests
    {
        static RegionCityTile District()=>new(){Width=2,Height=2};
        static DistrictBridgePlanner.Surface River(Vector2 p)=>new(Mathf.Abs(p.x)<15,Mathf.Abs(p.x)<10,0,-1);
        [Test]
        public void CrossingHasDryApproachesAndBoundedQueries()
        {
            var d=District();int queries=0;
            bool ok=DistrictBridgePlanner.TryPlan(d,new(62,64),Vector2Int.right,p=>{queries++;return River(p);},_=>false,out var b,out var reason);
            Assert.That(ok,Is.True,reason);Assert.That(b.Start.x,Is.EqualTo(60));Assert.That(b.End.x,Is.GreaterThanOrEqualTo(67));
            Assert.That(b.DeckHeight,Is.EqualTo(1));Assert.That(queries,Is.LessThan(800));
            Assert.That(DistrictBridgePlanner.Height(d,b,0),Is.EqualTo(b.StartHeight));
            Assert.That(DistrictBridgePlanner.Height(d,b,20),Is.EqualTo(b.DeckHeight));
        }
        [Test]
        public void RejectsWaterWithoutAnOppositeBankWithinBudget()
        {
            int queries=0;var d=District();
            Assert.That(DistrictBridgePlanner.TryPlan(d,new(40,40),Vector2Int.right,p=>
            {queries++;return new(p.x>-235,p.x>-235,0,-1);},_=>false,out _,out _),Is.False);
            Assert.That(queries,Is.LessThan(200));
        }
        [Test]
        public void RejectsOccupiedCorridorWithoutChangingDistrict()
        {
            var d=District();string before=JsonUtility.ToJson(d);
            Assert.That(DistrictBridgePlanner.TryPlan(d,new(62,64),Vector2Int.right,River,p=>p.x>0,out _,out _),Is.False);
            Assert.That(JsonUtility.ToJson(d),Is.EqualTo(before));
        }
        [TestCase(1,1)][TestCase(1,-1)][TestCase(-1,1)][TestCase(-1,-1)]
        public void SupportsDiagonalCrossings(int x,int z)
        {
            var d=District();var dir=new Vector2Int(x,z);var axis=((Vector2)dir).normalized;
            var bank=new Vector2Int(64-x*2,64-z*2);
            DistrictBridgePlanner.Surface Sample(Vector2 p)
            {float lateral=Vector2.Dot(p-new Vector2(5,5),axis);return new(Mathf.Abs(lateral)<15,Mathf.Abs(lateral)<10,0,-1);}
            Assert.That(DistrictBridgePlanner.TryPlan(d,bank,dir,Sample,_=>false,out var b,out var reason),Is.True,reason);
            Assert.That(Math.Abs(b.End.x-b.Start.x),Is.EqualTo(Math.Abs(b.End.y-b.Start.y)));
        }
        [Test]
        public void BridgeConnectsDeliveryRoadsAndRoundTripsThroughSave()
        {
            var d=District();d.Roads.Add(new(){GridX=60,GridZ=64});d.Roads.Add(new(){GridX=68,GridZ=64});
            var a=DistrictBridgePlanner.Center(d,new(60,64));var z=DistrictBridgePlanner.Center(d,new(68,64));
            Assert.That(new DistrictRoadDelivery(d).Route(a,new Rect(z,Vector2.zero),10),Is.Null);
            d.Bridges.Add(new(){Id="test",Start=new(60,64),End=new(68,64),StyleId="stone",DeckHeight=1,Cost=5800});
            var restored=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
            Assert.That(restored.Bridges[0].StyleId,Is.EqualTo("stone"));
            Assert.That(new DistrictRoadDelivery(restored).Route(a,z),Has.Count.EqualTo(2));
            var mid=(a+z)*.5f;
            var resumed=new DistrictRoadDelivery(restored).Route(mid,z);
            Assert.That(resumed,Is.Not.Null,"A saved wagon in the middle of a bridge must be able to resume.");
            Assert.That(resumed[0],Is.EqualTo(mid));Assert.That(resumed[resumed.Count-1],Is.EqualTo(z));
            restored.Bridges.Clear();Assert.That(new DistrictRoadDelivery(restored).Route(a,z),Is.Null);
        }
        [Test]
        public void SpatialIndexRemovesOnlyAffectedBridgeAndDeduplicatesRiverBuckets()
        {
            var index=new DistrictSpatialIndex<string>(10);var bounds=new Rect(0,0,25,25);
            index.Add(bounds,"a",true);index.Add(bounds,"a",true);index.Add(bounds,"b");
            Assert.That(index.Query(new(12,12)),Has.Count.EqualTo(2));index.Remove(bounds,"a");
            Assert.That(index.Query(new(12,12)),Is.EquivalentTo(new[]{"b"}));
        }
        [Test]
        public void BothStylesHaveReadableMeshesAndTextures()
        {
            foreach(var s in DistrictBridgeCatalog.Styles)
            {
                var data=Resources.Load<TextAsset>(s.Resource+"/modules");Assert.That(data,Is.Not.Null);
                Assert.That(data.text,Does.Contain("Entrance_Start"));Assert.That(data.text,Does.Contain("Middle_Bay"));
                Assert.That(Resources.Load<Texture2D>(s.Resource+"/albedo"),Is.Not.Null);
            }
        }
        [Test]
        public void DeliveryCacheInvalidatesForSameCountDiagonalEditsAndBridgeRemoval()
        {
            var d=District();var edit=new DistrictRoadPlacementModel.EditSession(d.Roads);int money=1000;
            edit.TryPlace(10,10,128,128,DistrictRoadPlacementModel.AntiqueBrickFamily,ref money);
            edit.TryPlace(11,11,128,128,DistrictRoadPlacementModel.AntiqueBrickFamily,ref money);
            var old=DistrictRoadDelivery.For(d);Assert.That(DistrictRoadDelivery.For(d),Is.SameAs(old));
            edit.TryConnectDiagonal(new(10,10),new(11,11));
            var linked=DistrictRoadDelivery.For(d);Assert.That(linked,Is.Not.SameAs(old));
            d.BridgeRevision++;Assert.That(DistrictRoadDelivery.For(d),Is.Not.SameAs(linked));
            var beforeRestore=DistrictRoadDelivery.For(d);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(d),d);
            // Undo/reload may reuse a List instance; the full restoration path explicitly repairs it.
            DistrictRoadPlacementModel.Repair(d.Roads);
            Assert.That(DistrictRoadDelivery.For(d),Is.Not.SameAs(beforeRestore));
        }
        [Test]
        public void UndoSnapshotsRestoreBridgeCostAndListWithoutDiskPersistence()
        {
            var d=District();var history=new DistrictUndoHistory();history.Reset(JsonUtility.ToJson(d));
            int money=d.Treasury;d.Bridges.Add(new(){Id="undo-bridge",Cost=5000});d.Treasury-=5000;d.BridgeRevision++;
            history.Commit(JsonUtility.ToJson(d));Assert.That(history.TryUndo(out var snapshot),Is.True);
            JsonUtility.FromJsonOverwrite(snapshot,d);Assert.That(d.Bridges,Is.Empty);Assert.That(d.Treasury,Is.EqualTo(money));
        }
        [Test]
        public void DenseSpatialQueriesStayLocal()
        {
            var index=new DistrictSpatialIndex<int>(32);
            for(int x=0;x<100;x++)for(int y=0;y<100;y++)index.Add(new Rect(x*64,y*64,8,80),x*100+y);
            var watch=Stopwatch.StartNew();long memory=GC.GetAllocatedBytesForCurrentThread();int max=0;
            for(int i=0;i<100000;i++)max=Math.Max(max,index.Query(new Vector2((i%100)*64,((i/100)%100)*64)).Count);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-memory;watch.Stop();
            UnityEngine.Debug.Log($"Bridge dense index: 10,000 objects, 100,000 queries, {watch.Elapsed.TotalMilliseconds:F2} ms, {allocated} bytes, max candidates {max}");
            Assert.That(max,Is.LessThanOrEqualTo(2));Assert.That(allocated,Is.LessThan(1024));
        }
    }
}
