using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.Behaviors
{
    [Serializable] public sealed class LotBehaviorInstance
    {
        public string InstanceId = "";
        public string DefinitionId = "lumber-loading-v01";
        public string BoatInstanceId = "";
        public bool Enabled = true;
        public bool HasStarted;
        [NonSerialized] public bool WaitingForTimber;
        public bool HasScript;
        public LotScript Script;
        public bool HasTimberCampScript;
        public TimberCampLotScript TimberCampScript;
        // The district owns progress; this stable binding prevents duplicate
        // crews when the district presentation is rebuilt.
        public string DistrictCrewId = "";
        public List<LotActorHome> WorkerHomes = new();
        public Vector3 CargoOffset;
        public Vector3 Pickup;
        public Vector3 Dock;
        public CargoLoadingState State;
        public List<Vector3> DepartureRoute = new();
    }
    [Serializable] public sealed class LotActorHome { public int Worker; public Vector3 Position; }
    public static class LotBehaviorCatalog
    {
        private static CargoLoadingDefinition[] _all;
        public static string ModDirectory => Path.Combine(Application.persistentDataPath, "Mods", "LotBehaviors");
        public static IReadOnlyList<CargoLoadingDefinition> All => _all ??= Load();
        public static CargoLoadingDefinition Find(string id) => All.FirstOrDefault(x => x.id == id);
        public static void Reload() => _all = Load();
        private static CargoLoadingDefinition[] Load()
        {
            var definitions = new Dictionary<string, CargoLoadingDefinition>();
            void Read(string json, string origin)
            {
                try
                {
                    var item = JsonUtility.FromJson<CargoLoadingDefinition>(json);
                    item.Validate();
                    if (string.IsNullOrWhiteSpace(item.id)) throw new ArgumentException("Missing id");
                    definitions[item.id] = item;
                }
                catch (Exception e) { Debug.LogWarning($"Lot behavior '{origin}' was not loaded: {e.Message}"); }
            }
            foreach (var source in Resources.LoadAll<TextAsset>("CityForgeV3/LotBehaviors")) Read(source.text, source.name);
            if (Directory.Exists(ModDirectory))
                foreach (var source in Directory.GetFiles(ModDirectory, "*.json").OrderBy(x => x)) Read(File.ReadAllText(source), source);
            return definitions.Values.OrderBy(x => x.displayName).ToArray();
        }
    }
}
