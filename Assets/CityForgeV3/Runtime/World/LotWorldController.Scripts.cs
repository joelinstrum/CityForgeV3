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
                var count = BehaviorDefinition(instance)?.workers ?? 0;
                for (var i = 0; i < count; i++) result.Add(new LotObjectReference
                { Id = instance.InstanceId + "/worker/" + (i + 1), Kind = "Spawned workers", Name = "Worker " + (i + 1) });
            }
            return result;
        }
        public CargoLoadingDefinition BehaviorDefinition(LotBehaviorInstance instance) => instance.HasScript && instance.Script != null ? instance.Script.behavior : LotBehaviorCatalog.Find(instance.DefinitionId);
        public string BehaviorScriptSource(string instanceId)
        {
            var instance = LotBehaviors.FirstOrDefault(x => x.InstanceId == instanceId);
            if (instance == null) throw new ArgumentException("Behavior not found.");
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
        public void RunBehavior(string id, bool restart = false)
        {
            var instance = LotBehaviors.FirstOrDefault(x => x.InstanceId == id);
            if (instance == null) return;
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
