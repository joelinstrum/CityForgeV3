using System;
using UnityEngine;
namespace CityForgeV3.Behaviors
{
    [Serializable] public sealed class LotScriptPoint
    {
        public string objectId = "";
        public Vector3 offset;
    }
    // Portable declarative script. Runtime progress is stored separately from its source.
    [Serializable] public sealed class LotScript
    {
        public string schema = "cityforge-lot-script-v1";
        public string boatId = "";
        public LotScriptPoint pickup = new();
        public LotScriptPoint dock = new();
        public CargoLoadingDefinition behavior = new();
    }
    public static class LotScriptCodec
    {
        public const int MaxCharacters = 262144;
        public static LotScript Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source) || source.Length > MaxCharacters)
                throw new ArgumentException("Enter a script smaller than 256 KB.");
            LotScript script;
            try { script = JsonUtility.FromJson<LotScript>(source); }
            catch (Exception) { throw new ArgumentException("Invalid JSON. Check quotes, commas, and braces."); }
            if (script == null || script.schema != "cityforge-lot-script-v1" || script.behavior == null || script.pickup == null || script.dock == null)
                throw new ArgumentException("Use a cityforge-lot-script-v1 JSON script with behavior, pickup, and dock fields.");
            script.behavior.Validate();
            if (string.IsNullOrWhiteSpace(script.behavior.id) || string.IsNullOrWhiteSpace(script.behavior.displayName))
                throw new ArgumentException("The behavior needs an id and displayName.");
            if (string.IsNullOrWhiteSpace(script.boatId)) throw new ArgumentException("Set boatId to a boat in this lot. Copy it from Objects.");
            CheckPoint(script.pickup.offset); CheckPoint(script.dock.offset);
            return script;
        }
        private static void CheckPoint(Vector3 p)
        {
            if (!float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z))
                throw new ArgumentException("Point coordinates must be finite numbers.");
        }
        public static string Describe(LotScript script) =>
            $"{script.behavior.workers} workers pick up → walk → unload → return. Repeat until {script.behavior.capacity} bundles are loaded. Wait for workers to clear, then follow a connected river downstream." +
            (script.behavior.requireTimberDelivery ? " In a district, each shipment waits for delivered timber." : "") +
            (script.behavior.repeat ? $" Restart {script.behavior.restartDelaySeconds:0}s after departure completes." : "");
    }
}
