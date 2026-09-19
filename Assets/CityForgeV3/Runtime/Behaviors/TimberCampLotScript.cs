using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.Behaviors
{
    [Serializable]
    public sealed class TimberCampDefinition
    {
        public int treesPerLoad = 3;
        public int bundlesPerTree = 4;
        public float unloadSeconds = 3f;
        public float retrySeconds = 5f;
        public float harvestRadiusMeters = 100f;
        public bool repeat = true;
        public bool fastWagon;

        public void Validate()
        {
            if (treesPerLoad < 1 || treesPerLoad > 30 ||
                bundlesPerTree < 1 || bundlesPerTree > 100 ||
                !Positive(unloadSeconds) || !float.IsFinite(retrySeconds) ||
                retrySeconds < 1f || !float.IsFinite(harvestRadiusMeters) ||
                harvestRadiusMeters < 5f || harvestRadiusMeters > 1000f)
                throw new ArgumentException(
                    "Use 1-30 trees per load, 1-100 bundles per tree, positive unloading time, retry >= 1s, and harvest radius 5-1000m.");
        }

        private static bool Positive(float value) =>
            value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // Portable Lot-owned recipe. Actor IDs and the connector refer to objects
    // authored in the same Lot; placed-Lot progress remains in district state.
    [Serializable]
    public sealed class TimberCampLotScript
    {
        public string schema = TimberCampLotScriptCodec.Schema;
        public string kind = "timber-camp-v1";
        public string displayName = "Lumberjack Camp";
        public List<string> lumberjackIds = new();
        public string wagonId = "";
        public string connectorId = "";
        public TimberCampDefinition harvest = new();
    }

    public static class TimberCampLotScriptCodec
    {
        public const string Schema = "cityforge-timber-camp-script-v1";

        public static TimberCampLotScript Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source) ||
                source.Length > LotScriptCodec.MaxCharacters)
                throw new ArgumentException("Enter a script smaller than 256 KB.");
            TimberCampLotScript script;
            try { script = JsonUtility.FromJson<TimberCampLotScript>(source); }
            catch { throw new ArgumentException("Invalid JSON. Check quotes, commas, and braces."); }
            if (script == null || script.schema != Schema ||
                script.kind != "timber-camp-v1" || script.harvest == null)
                throw new ArgumentException(
                    "Use a cityforge-timber-camp-script-v1 JSON script.");
            if (string.IsNullOrWhiteSpace(script.displayName))
                throw new ArgumentException("The behavior needs a displayName.");
            if (script.lumberjackIds == null || script.lumberjackIds.Count == 0 ||
                script.lumberjackIds.Count > 16)
                throw new ArgumentException("Assign 1-16 lumberjacks from this Lot.");
            if (string.IsNullOrWhiteSpace(script.wagonId))
                throw new ArgumentException("Assign a Forestry Cart from this Lot.");
            if (string.IsNullOrWhiteSpace(script.connectorId))
                throw new ArgumentException("Assign a vehicle Connector from this Lot.");
            script.harvest.Validate();
            return script;
        }

        public static string Describe(TimberCampLotScript script) =>
            $"{script.lumberjackIds.Count} lumberjack" +
            (script.lumberjackIds.Count == 1 ? "" : "s") +
            $" harvest within {script.harvest.harvestRadiusMeters:0} m, return to camp, " +
            $"and dispatch {script.harvest.treesPerLoad} trees per wagon load through the assigned Connector.";
    }
}
