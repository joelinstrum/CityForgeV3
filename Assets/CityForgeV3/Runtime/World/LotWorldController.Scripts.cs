using System;
using System.Collections.Generic;
using System.Linq;
using CityForgeV3.Behaviors;
using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        public List<LotObjectReference> ScriptObjects()
        {
            LotObjectRegistry.EnsureIds(_session.Data);
            var result = LotObjectRegistry.Read(_session.Data);
            foreach (var instance in LotBehaviors)
            {
                if (IsTimberCampBehavior(instance)) continue;
                var count = BehaviorDefinition(instance)?.workers ?? 0;
                for (var i = 0; i < count; i++) result.Add(new LotObjectReference
                { Id = instance.InstanceId + "/worker/" + (i + 1), Kind = "Spawned workers", Name = "Worker " + (i + 1) });
            }
            return result;
        }
        public CargoLoadingDefinition BehaviorDefinition(LotBehaviorInstance instance) => instance.HasScript && instance.Script != null ? instance.Script.behavior : LotBehaviorCatalog.Find(instance.DefinitionId);
        public static bool IsTimberCampBehavior(LotBehaviorInstance instance) =>
            instance?.HasTimberCampScript == true && instance.TimberCampScript != null;
        public string BehaviorDisplayName(LotBehaviorInstance instance) =>
            IsTimberCampBehavior(instance)
                ? instance.TimberCampScript.displayName
                : BehaviorDefinition(instance)?.displayName ?? instance?.DefinitionId ?? "Behavior";
        public string BehaviorScriptSource(string instanceId)
        {
            var instance = LotBehaviors.FirstOrDefault(x => x.InstanceId == instanceId);
            if (instance == null) throw new ArgumentException("Behavior not found.");
            if (IsTimberCampBehavior(instance))
                return JsonUtility.ToJson(instance.TimberCampScript, true);
            var script = instance.HasScript && instance.Script != null ? instance.Script : new LotScript
            {
                boatId = instance.BoatInstanceId,
                pickup = new LotScriptPoint { offset = instance.Pickup },
                dock = new LotScriptPoint { offset = instance.Dock },
                behavior = BehaviorDefinition(instance)
            };
            return JsonUtility.ToJson(script, true);
        }
        public string ValidateBehaviorScript(string source, string editingId = null)
        {
            if (IsTimberCampScriptSource(source))
            {
                var timber = TimberCampLotScriptCodec.Parse(source);
                ValidateTimberCampScript(timber, editingId);
                return TimberCampLotScriptCodec.Describe(timber);
            }
            var script = LotScriptCodec.Parse(source);
            if (!_session.Data.Props.Any(p => p.InstanceId == script.boatId && BoatCatalog.Find(p.PropId) != null))
                throw new ArgumentException("boatId must identify a boat in this lot. Copy its ID from Objects.");
            if (LotBehaviors.Any(b => b.InstanceId != editingId && b.BoatInstanceId == script.boatId))
                throw new ArgumentException("That boat already has a behavior. Edit its Script instead.");
            if (Resources.Load<GameObject>(script.behavior.workerPrefabResourcePath) == null)
                throw new ArgumentException("Worker prefab not found: " + script.behavior.workerPrefabResourcePath);
            foreach (var point in new[] { script.pickup, script.dock })
            {
                var position = LotObjectRegistry.ResolvePoint(_session.Data, point);
                if (Mathf.Abs(position.x) > LotWidthMeters * .5f || Mathf.Abs(position.z) > LotDepthMeters * .5f)
                    throw new ArgumentException("Pickup and dock points must be inside the lot.");
            }
            return LotScriptCodec.Describe(script);
        }
        public string ApplyBehaviorScript(string source, string editingId = null)
        {
            if (IsTimberCampScriptSource(source))
                return ApplyTimberCampScript(source, editingId);
            ValidateBehaviorScript(source, editingId); // All checks precede mutation.
            var script = LotScriptCodec.Parse(source);
            var instance = LotBehaviors.FirstOrDefault(x => x.InstanceId == editingId);
            if (editingId != null && instance == null) throw new ArgumentException("The behavior being edited no longer exists.");
            if (instance == null)
            {
                instance = new LotBehaviorInstance { InstanceId = Guid.NewGuid().ToString("N") };
                (_session.Data.Behaviors ??= new List<LotBehaviorInstance>()).Add(instance);
            }
            instance.Script = script; instance.HasScript = true; instance.DefinitionId = script.behavior.id;
            instance.BoatInstanceId = script.boatId;
            instance.Pickup = LotObjectRegistry.ResolvePoint(_session.Data, script.pickup);
            instance.Dock = LotObjectRegistry.ResolvePoint(_session.Data, script.dock);
            instance.Enabled = false; instance.HasStarted = false;
            ResetBehavior(instance.InstanceId);
            return instance.InstanceId;
        }

        public string DescribeBehaviorScript(string source)
        {
            if (IsTimberCampScriptSource(source))
                return TimberCampLotScriptCodec.Describe(
                    TimberCampLotScriptCodec.Parse(source));
            return LotScriptCodec.Describe(LotScriptCodec.Parse(source));
        }

        private static bool IsTimberCampScriptSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            try
            {
                var probe = JsonUtility.FromJson<ScriptSchemaProbe>(source);
                return probe?.schema == TimberCampLotScriptCodec.Schema;
            }
            catch { return false; }
        }

        [Serializable]
        private sealed class ScriptSchemaProbe { public string schema = ""; }

        private void ValidateTimberCampScript(TimberCampLotScript script,
            string editingId)
        {
            LotObjectRegistry.EnsureIds(_session.Data);
            var ids = new HashSet<string>(script.lumberjackIds,
                StringComparer.Ordinal);
            if (ids.Count != script.lumberjackIds.Count)
                throw new ArgumentException("Each lumberjack may be assigned once.");
            foreach (var id in ids)
                if (!_session.Data.Props.Any(p => p.InstanceId == id &&
                    IsLumberjack(p.PropId)))
                    throw new ArgumentException(
                        "Lumberjack ID is not a Lumberjack in this Lot: " + id);
            if (!_session.Data.Props.Any(p => p.InstanceId == script.wagonId &&
                IsTimberCampWagon(p.PropId)))
                throw new ArgumentException(
                    "wagonId must identify a Forestry Cart or Lumber Wagon in this Lot.");
            var connector = _session.Data.Connectors?.FirstOrDefault(c =>
                c.InstanceId == script.connectorId);
            if (connector == null || !connector.AllowsVehicles)
                throw new ArgumentException(
                    "connectorId must identify a vehicle Connector in this Lot.");
            if (LotBehaviors.Any(b => b.InstanceId != editingId &&
                IsTimberCampBehavior(b)))
                throw new ArgumentException(
                    "This Lot already has a Lumberjack Camp behavior.");
        }

        private string ApplyTimberCampScript(string source, string editingId)
        {
            var script = TimberCampLotScriptCodec.Parse(source);
            ValidateTimberCampScript(script, editingId);
            var instance = LotBehaviors.FirstOrDefault(x =>
                x.InstanceId == editingId);
            if (editingId != null && instance == null)
                throw new ArgumentException(
                    "The behavior being edited no longer exists.");
            if (instance == null)
            {
                instance = new LotBehaviorInstance
                { InstanceId = Guid.NewGuid().ToString("N") };
                (_session.Data.Behaviors ??= new List<LotBehaviorInstance>())
                    .Add(instance);
            }
            instance.DefinitionId = "timber-camp-v1";
            instance.HasScript = false;
            instance.Script = null;
            instance.HasTimberCampScript = true;
            instance.TimberCampScript = script;
            instance.Enabled = false;
            instance.HasStarted = false;
            if (_districtHosted && _timberPlacement != null &&
                _timberDistrict != null)
                BindTimberCampBehaviors(_timberPlacement, _timberDistrict);
            NotifyStateChanged();
            return instance.InstanceId;
        }

        public bool AddTimberCampBehavior(out string error)
        {
            error = "";
            LotObjectRegistry.EnsureIds(_session.Data);
            var lumberjacks = _session.Data.Props
                .Where(p => IsLumberjack(p.PropId)).ToList();
            var wagon = _session.Data.Props.FirstOrDefault(p =>
                IsTimberCampWagon(p.PropId));
            var connector = _session.Data.Connectors?.FirstOrDefault(c =>
                c.AllowsVehicles);
            if (lumberjacks.Count == 0)
                error = "Add at least one Lumberjack to this Lot first.";
            else if (lumberjacks.Count > 16)
                error = "A Lumberjack Camp supports at most 16 Lumberjacks.";
            else if (wagon == null)
                error = "Add a Forestry Cart or Lumber Wagon to this Lot first.";
            else if (connector == null)
                error = "Add a vehicle Connector to this Lot first.";
            else if (LotBehaviors.Any(IsTimberCampBehavior))
                error = "This Lot already has a Lumberjack Camp behavior.";
            if (!string.IsNullOrEmpty(error)) return false;

            var script = new TimberCampLotScript
            {
                lumberjackIds = lumberjacks.Select(p => p.InstanceId).ToList(),
                wagonId = wagon.InstanceId,
                connectorId = connector.InstanceId
            };
            ApplyTimberCampScript(JsonUtility.ToJson(script), null);
            return true;
        }

        public static bool IsTimberCampWagon(string propId) =>
            propId == HorseForestryWagonPropId ||
            propId == HorseLumberWagonPropId;
        public void RunBehavior(string id, bool restart = false)
        {
            var instance = LotBehaviors.FirstOrDefault(x => x.InstanceId == id);
            if (instance == null) return;
            if (IsTimberCampBehavior(instance))
            {
                if (restart && _timberDistrict != null &&
                    !string.IsNullOrWhiteSpace(instance.DistrictCrewId))
                {
                    var crew = DistrictLabor.State(_timberDistrict).TimberCrews?
                        .Find(c => c.Id == instance.DistrictCrewId);
                    if (crew != null) crew.Enabled = true;
                }
                instance.Enabled = true;
                instance.HasStarted = true;
                NotifyStateChanged();
                return;
            }
            var definition = BehaviorDefinition(instance);
            if (restart || (instance.State?.Departed == true && !definition.repeat)) ResetBehavior(id);
            // Placement pauses individual actors at their authored homes. Run
            // wakes those actors without discarding cargo already aboard.
            if (instance.State != null && !instance.State.Departing && instance.State.Loaded < definition.capacity)
                for (var i = 0; i < instance.State.Workers.Length; i++)
                {
                    var worker = instance.State.Workers[i];
                    if (worker.Phase != "idle") continue;
                    worker.Phase = "pickup"; worker.Elapsed = -i * definition.staggerSeconds; worker.Carrying = false;
                }
            instance.Enabled = true; instance.HasStarted = true; NotifyStateChanged();
        }
    }
}
