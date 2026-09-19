using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityForgeV3.Behaviors;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
namespace CityForgeV3.Tests.EditMode
{
    public class LotScriptTests
    {
        private GameObject _go;
        private LotWorldController _world;
        private LotEditorSession _session;
        [SetUp] public void SetUp()
        {
            _go = new GameObject("Lot scripts test"); _world = _go.AddComponent<LotWorldController>();
            _session = (LotEditorSession)typeof(LotWorldController).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_world);
            _session.Data.Props.Add(new PlacedProp { InstanceId = "barge-1", PropId = "wooden-lumber-barge-v01" });
        }
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(_go);
        private string Script(int capacity = 8) => JsonUtility.ToJson(new LotScript
        { boatId = "barge-1", behavior = new CargoLoadingDefinition { capacity = capacity }, pickup = new LotScriptPoint { offset = new Vector3(2, .06f, 2) }, dock = new LotScriptPoint { objectId = "barge-1", offset = new Vector3(2, .06f, 0) } });
        [Test] public void ImportedScriptIsPausedAndUsesItsOwnConfiguration()
        {
            var id = _world.ApplyBehaviorScript(Script()); var b = _world.LotBehaviors.Single();
            Assert.AreEqual(id, b.InstanceId); Assert.IsFalse(b.Enabled); Assert.IsFalse(b.HasStarted);
            Assert.AreEqual(8, _world.BehaviorDefinition(b).capacity); Assert.AreEqual(0, b.State.Loaded);
        }
        [Test] public void InvalidTargetIsRejectedWithoutChangingLot()
        {
            var before = _session.Serialize(); Assert.Throws<ArgumentException>(() => _world.ApplyBehaviorScript(Script().Replace("barge-1", "missing")));
            Assert.AreEqual(before, _session.Serialize());
        }
        [Test] public void EditingOneScriptPreservesItsIdAndResetsProgress()
        {
            var id = _world.ApplyBehaviorScript(Script()); _world.LotBehaviors[0].State.Loaded = 4;
            _world.ApplyBehaviorScript(Script(3), id); Assert.AreEqual(1, _world.LotBehaviors.Count);
            Assert.AreEqual(id, _world.LotBehaviors[0].InstanceId); Assert.AreEqual(0, _world.LotBehaviors[0].State.Loaded);
            Assert.AreEqual(3, _world.BehaviorDefinition(_world.LotBehaviors[0]).capacity);
        }
        [Test] public void SecondScriptCannotClaimSameBoat()
        { _world.ApplyBehaviorScript(Script()); Assert.Throws<ArgumentException>(() => _world.ApplyBehaviorScript(Script())); Assert.AreEqual(1, _world.LotBehaviors.Count); }
        [Test] public void RunResumesAndRerunResets()
        {
            var id = _world.ApplyBehaviorScript(Script()); var b = _world.LotBehaviors[0];
            _world.RunBehavior(id); Assert.IsTrue(b.Enabled); Assert.IsTrue(b.HasStarted);
            b.State.Loaded = 3; _world.ToggleBehavior(id); Assert.IsFalse(b.Enabled);
            _world.RunBehavior(id); Assert.AreEqual(3,b.State.Loaded);
            _world.RunBehavior(id,true); Assert.AreEqual(0,b.State.Loaded); Assert.IsTrue(b.Enabled);
        }
        [Test] public void RunWakesRepositionedWorkersAndPreservesCargoAndHomes()
        {
            var id = _world.ApplyBehaviorScript(Script()); var b = _world.LotBehaviors[0];
            b.State.Loaded = 2;
            b.WorkerHomes.Add(new LotActorHome { Worker = 0, Position = new Vector3(3, .06f, 4) });
            foreach (var worker in b.State.Workers) worker.Phase = "idle";
            _world.RunBehavior(id);
            Assert.IsTrue(b.State.Workers.All(w => w.Phase == "pickup"));
            Assert.AreEqual(2, b.State.Loaded);
            Assert.AreEqual(new Vector3(3, .06f, 4), b.WorkerHomes[0].Position);
            CargoLoadingSimulation.Step(b.Script.behavior, b.State, 20, 1, true, false, 0);
            Assert.Greater(b.State.Loaded, 2);
        }
        [Test] public void RunDoesNotRestartFullWaitingCrew()
        {
            var id = _world.ApplyBehaviorScript(Script()); var b = _world.LotBehaviors[0];
            b.State.Loaded = b.Script.behavior.capacity;
            foreach (var worker in b.State.Workers) worker.Phase = "idle";
            _world.RunBehavior(id);
            Assert.IsTrue(b.State.Workers.All(w => w.Phase == "idle"));
        }
        [Test] public void RunPreservesPausedRepeatCountdown()
        {
            var id = _world.ApplyBehaviorScript(Script()); var b = _world.LotBehaviors[0];
            b.Script.behavior.repeat = true; b.State.Departed = true; b.State.RestartElapsed = 25;
            _world.RunBehavior(id); Assert.IsTrue(b.State.Departed); Assert.AreEqual(25,b.State.RestartElapsed);
        }
        [Test] public void WaterArrowMovesBothAnchorsAndRotatesFourDirections()
        {
            var d=_session.Data;d.HasWaterOrientation=true;d.WaterOrientationLand=Vector3.zero;d.WaterOrientationWater=Vector3.forward*4;
            _world.MoveWaterArrow(new Vector3(1,0,2));Assert.AreEqual(new Vector3(1,0,2),d.WaterOrientationLand);Assert.AreEqual(new Vector3(1,0,6),d.WaterOrientationWater);
            for(var i=0;i<4;i++) { _world.SetWaterArrowDirection(i);var expected=new[]{Vector3.left,Vector3.forward,Vector3.right,Vector3.back}[i]*4;Assert.AreEqual(i,_world.WaterArrowDirection);Assert.Less(Vector3.Distance(expected,d.WaterOrientationWater-d.WaterOrientationLand),.001f); }
            var saved=JsonUtility.FromJson<LotSaveData>(JsonUtility.ToJson(d.Copy()));Assert.AreEqual(d.WaterOrientationWater,saved.WaterOrientationWater);
        }
        [Test] public void WaterArrowDragClampsBothEndpointsInsideLot()
        {
            var d=_session.Data;d.HasWaterOrientation=true;d.WaterOrientationLand=Vector3.zero;d.WaterOrientationWater=Vector3.forward*4;
            _world.MoveWaterArrow(new Vector3(100,0,100));Assert.LessOrEqual(d.WaterOrientationWater.z,_world.LotDepthMeters/2);Assert.LessOrEqual(d.WaterOrientationLand.x,_world.LotWidthMeters/2);
            Assert.That(Vector3.Distance(d.WaterOrientationLand,d.WaterOrientationWater),Is.EqualTo(4).Within(.001f));
        }
        [Test] public void ScriptAndStableIdsSurviveSaveAndCopy()
        {
            _world.ApplyBehaviorScript(Script()); var copy = _session.Data.Copy(); Assert.AreEqual(1,copy.Behaviors.Count);
            var json = _session.Serialize(); _session.Restore(json);
            Assert.AreEqual("barge-1", _session.Data.Props[0].InstanceId);
            Assert.AreEqual(8, _session.Data.Behaviors[0].Script.behavior.capacity);
        }
        [Test] public void LegacyObjectsAndAttachmentsReceiveStableUniqueIds()
        {
            _session.Data.Flora.Add(new PlacedFlora());
            _session.Data.Buildings3D.Add(new PlacedBuilding3D { Attachments = new List<PlacedBuildingProp> { new() } });
            _session.Data.RoadPieces.Add(new PlacedRoadPiece());
            LotObjectRegistry.EnsureIds(_session.Data); var first = LotObjectRegistry.Read(_session.Data).Select(x=>x.Id).ToArray();
            Assert.IsTrue(first.All(x=>!string.IsNullOrWhiteSpace(x))); Assert.AreEqual(first.Length,first.Distinct().Count());
            LotObjectRegistry.EnsureIds(_session.Data); CollectionAssert.AreEqual(first,LotObjectRegistry.Read(_session.Data).Select(x=>x.Id).ToArray());
        }
        [Test] public void ObjectPointReferencesFollowPlacedObjectMovement()
        {
            var point = new LotScriptPoint { objectId = "barge-1", offset = new Vector3(2,.1f,1) };
            _session.Data.Props[0].PositionX = 4; Assert.AreEqual(new Vector3(6,.1f,1),LotObjectRegistry.ResolvePoint(_session.Data,point));
            _session.Data.Props.Clear(); Assert.Throws<ArgumentException>(()=>LotObjectRegistry.ResolvePoint(_session.Data,point));
        }
        [TestCase("{}")] [TestCase("not json")] [TestCase("{\"schema\":\"wrong\"}")]
        public void InvalidScriptDocumentsRejected(string source) => Assert.Throws<ArgumentException>(()=>LotScriptCodec.Parse(source));
        [Test] public void LegacyRoutineUsesExistingBindingsAfterUnityRoundTrip()
        {
            var d = new CargoLoadingDefinition();
            _session.Data.Behaviors.Add(new LotBehaviorInstance { InstanceId="legacy", BoatInstanceId="barge-1", Pickup=new Vector3(2,.06f,2), Dock=new Vector3(2,.06f,0), State=CargoLoadingSimulation.Create(d) });
            _session.Restore(_session.Serialize());
            var script=LotScriptCodec.Parse(_world.BehaviorScriptSource("legacy"));
            Assert.AreEqual("barge-1",script.boatId);Assert.AreEqual(new Vector3(2,.06f,2),script.pickup.offset);
            Assert.IsFalse(_world.LotBehaviors[0].HasScript);
        }
        [Test] public void OutsideLotAnchorIsRejected()
        { var script=LotScriptCodec.Parse(Script());script.pickup.offset=new Vector3(100,0,100);Assert.Throws<ArgumentException>(()=>_world.ApplyBehaviorScript(JsonUtility.ToJson(script))); }

        private void AddTimberCampObjects()
        {
            _session.Data.Props.Add(new PlacedProp
            {
                InstanceId = "axeman-1",
                PropId = LotWorldController.LumberjackCharacterId,
                PositionX = -2,
                PositionZ = 1
            });
            _session.Data.Props.Add(new PlacedProp
            {
                InstanceId = "axeman-2",
                PropId = LotWorldController.LumberjackCharacterId,
                PositionX = 2,
                PositionZ = 1
            });
            _session.Data.Props.Add(new PlacedProp
            {
                InstanceId = "wagon-1",
                PropId = LotWorldController.HorseForestryWagonPropId
            });
            _session.Data.Connectors.Add(new PlacedLotConnector
            {
                InstanceId = "entry-1",
                ConnectorId = LotWorldController.DirtEntryConnectorId,
                Edge = LotConnectorEdge.South,
                OffsetMeters = 20,
                AllowsPedestrians = true,
                AllowsVehicles = true
            });
        }

        [Test] public void TimberCampLibraryBuildsEditableScriptFromLotObjects()
        {
            AddTimberCampObjects();
            Assert.IsTrue(_world.AddTimberCampBehavior(out var error), error);
            var behavior = _world.LotBehaviors.Single();
            var script = TimberCampLotScriptCodec.Parse(
                _world.BehaviorScriptSource(behavior.InstanceId));
            CollectionAssert.AreEqual(new[] { "axeman-1", "axeman-2" },
                script.lumberjackIds);
            Assert.AreEqual("wagon-1", script.wagonId);
            Assert.AreEqual("entry-1", script.connectorId);
            Assert.AreEqual("Lumberjack Camp",
                _world.BehaviorDisplayName(behavior));
        }

        [Test] public void TimberCampRequiresAuthoredActorsAndVehicleConnector()
        {
            Assert.IsFalse(_world.AddTimberCampBehavior(out var error));
            StringAssert.Contains("Lumberjack", error);
            Assert.IsEmpty(_world.LotBehaviors);
        }

        [Test] public void TimberCampScriptSurvivesLotJsonRoundTrip()
        {
            AddTimberCampObjects();
            Assert.IsTrue(_world.AddTimberCampBehavior(out _));
            _session.Restore(_session.Serialize());
            var behavior = _session.Data.Behaviors.Single();
            Assert.IsTrue(behavior.HasTimberCampScript);
            Assert.AreEqual(2, behavior.TimberCampScript.lumberjackIds.Count);
            Assert.AreEqual(3, behavior.TimberCampScript.harvest.treesPerLoad);
        }

        [Test] public void PlacedTimberCampCreatesOneBoundDistrictCrew()
        {
            AddTimberCampObjects();
            Assert.IsTrue(_world.AddTimberCampBehavior(out _));
            typeof(LotWorldController).GetField("_districtHosted",
                BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_world, true);
            var district = new RegionCityTile { Treasury = 10000, Width = 1,
                Height = 1 };
            var placement = new PlacedDistrictLot
            {
                InstanceId = "camp-lot-1",
                Behaviors = _session.Data.Behaviors
            };
            _world.BindDistrictBehaviors(placement, district);
            _world.BindDistrictBehaviors(placement, district);
            var crew = DistrictLabor.State(district).TimberCrews.Single();
            Assert.AreEqual("camp-lot-1", crew.SourceLotInstanceId);
            Assert.AreEqual(_world.LotBehaviors.Single().InstanceId,
                crew.SourceBehaviorInstanceId);
            Assert.AreEqual(2, DistrictLabor.State(district).Workers.Count);
            var connector = _session.Data.Connectors.Single();
            var outside = LotWorldController.ConnectorAccess(connector,
                _world.LotWidthMeters, _world.LotDepthMeters).Outside;
            Assert.That(crew.Camp, Is.EqualTo(outside),
                "The live crew must begin beyond the Lot boundary through its Connector.");
            Assert.That(crew.WagonHome, Is.EqualTo(outside));
            Assert.That(DistrictLabor.State(district).Workers.All(worker =>
                worker.Position == outside), Is.True);

            // Existing saves from the first Lot-script implementation may
            // still contain an inside-Lot camp and stranded workers.
            crew.Camp = Vector2.zero;
            foreach (var worker in DistrictLabor.State(district).Workers)
                worker.Position = new Vector2(-2f, 1f);
            _world.BindDistrictBehaviors(placement, district);
            Assert.That(crew.Camp, Is.EqualTo(outside));
            Assert.That(DistrictLabor.State(district).Workers.All(worker =>
                worker.Position == outside), Is.True);
        }

        [Test]
        public void LegacyPlacedLumberjackCampGetsBuiltInBehaviorOnce()
        {
            AddTimberCampObjects();
            _session.Data.Props.Single(prop =>
                prop.InstanceId == "wagon-1").PropId =
                LotWorldController.HorseLumberWagonPropId;
            typeof(LotWorldController).GetField("_districtHosted",
                BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_world, true);
            var district = new RegionCityTile
            {
                Treasury = 10000,
                Width = 1,
                Height = 1
            };
            var placement = new PlacedDistrictLot
            {
                InstanceId = "legacy-camp-lot",
                LotId = "lumberjack-camp",
                BehaviorsInitialized = true,
                Behaviors = new List<LotBehaviorInstance>()
            };

            _world.BindDistrictBehaviors(placement, district);

            Assert.IsTrue(placement.BuiltInTimberCampChecked);
            Assert.That(placement.Behaviors.Count, Is.EqualTo(1));
            Assert.IsTrue(LotWorldController.IsTimberCampBehavior(
                placement.Behaviors[0]));
            Assert.IsTrue(placement.Behaviors[0].Enabled);
            Assert.IsTrue(placement.Behaviors[0].HasStarted);
            Assert.That(DistrictLabor.State(district).TimberCrews.Count,
                Is.EqualTo(1));
            Assert.That(DistrictLabor.State(district).Workers.Count,
                Is.EqualTo(2));

            // A later explicit removal remains removed after presentation
            // rebuilds because the compatibility check is persisted.
            placement.Behaviors.Clear();
            _world.BindDistrictBehaviors(placement, district);
            Assert.That(placement.Behaviors, Is.Empty);
        }
    }
}
