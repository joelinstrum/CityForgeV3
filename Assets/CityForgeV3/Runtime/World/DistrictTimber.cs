using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForgeV3.World
{
    // Portable recipe and saved progress are separate. The host supplies routing/motion.
    [Serializable] public sealed class TimberScript
    {
        public string schema = "cityforge-timber-script-v1";
        public int treesPerLoad = 3;
        public int bundlesPerTree = 4;
        public float unloadSeconds = 3;
        public float retrySeconds = 5;
        public float harvestRadiusMeters = 100;
        public bool repeat = true;
        public bool fastWagon;
        public void Validate()
        {
            if (schema != "cityforge-timber-script-v1" || treesPerLoad < 1 || treesPerLoad > 30 ||
                bundlesPerTree < 1 || bundlesPerTree > 100 || !float.IsFinite(unloadSeconds) ||
                unloadSeconds <= 0 || !float.IsFinite(retrySeconds) || retrySeconds < 1 ||
                !float.IsFinite(harvestRadiusMeters) || harvestRadiusMeters < 5 || harvestRadiusMeters > 1000)
                throw new ArgumentException("Use a timber script with 1–30 trees per load, 1–100 bundles per tree, positive unloading time, retry ≥ 1s and harvest radius 5–1000m.");
        }
        public static TimberScript Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source) || source.Length > 262144) throw new ArgumentException("Enter a JSON script smaller than 256 KB.");
            TimberScript result;
            try { result = JsonUtility.FromJson<TimberScript>(source); }
            catch { throw new ArgumentException("Invalid JSON. Check quotes, commas and braces."); }
            if (result == null) throw new ArgumentException("Missing timber script.");
            result.Validate(); return result;
        }
    }
    [Serializable] public sealed class DistrictTimberCrew
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string WagonId = Guid.NewGuid().ToString("N");
        public string Name = "Axemen";
        public Vector2 Camp;
        public Vector2 WagonHome;
        public Vector2 WagonPosition;
        public float HorseHeading, BodyHeading, FrontHeading;
        public int PendingTrees, CargoTrees, DeliveredLoads;
        public bool Enabled = true;
        public string Phase = "gather";
        public string MillId = "";
        public Vector2 Destination;
        public List<Vector2> Route = new();
        public float Elapsed;
        public string Status = "Gathering trees";
        public TimberScript Script = new();
    }
    public sealed class TimberDestination
    {
        public string MillId;
        public Vector2 Point;
        public List<Vector2> Route;
        public float Distance;
    }
    public static class DistrictTimber
    {
        // Existing prototype yield: four bundles per 300-unit tree.
        public const int WoodPerBargeBundle = DistrictTreeHarvest.PrototypeWoodYield / 4;
        public static int CreditBargeLoading(RegionCityTile district, int before, int after)
        {
            if(district==null || after<=before)return 0;
            var inventory=DistrictLabor.State(district);
            var amount=(int)Math.Min((long)(after-before)*WoodPerBargeBundle,(long)int.MaxValue-inventory.Wood);
            inventory.Wood+=amount;
            return amount;
        }

        public static DistrictTimberCrew Place(RegionCityTile d, Vector2 point, Vector2 wagonHome, int count, TimberScript script)
        {
            script.Validate();
            if (count < 1 || count > 16 || !float.IsFinite(point.x) || !float.IsFinite(point.y)) return null;
            var state = DistrictLabor.State(d);
            if (DistrictLabor.AssignmentCost(d, state.AssignedAxemen + count) > d.Treasury) return null;
            var oldCount = state.AssignedAxemen;
            state.CampPlaced = true;
            if (oldCount == 0) state.Camp = point;
            if (!DistrictLabor.Assign(d, oldCount + count)) return null;
            var crew = new DistrictTimberCrew { Camp = point, WagonHome = wagonHome, WagonPosition = wagonHome,
                Script = JsonUtility.FromJson<TimberScript>(JsonUtility.ToJson(script)) };
            state.TimberCrews ??= new();
            state.TimberCrews.Add(crew);
            foreach (var worker in state.Workers.Where(w => w.Slot >= oldCount))
            {
                worker.CrewId = crew.Id; worker.Position = point; worker.Activity = AxemanActivity.Waiting;
                worker.Route = new(); worker.TreeId = ""; worker.Cargo = 0; worker.CargoTrees = 0;
            }
            return crew;
        }
        // Motion callback returns arrival only after the wagon actually reaches the road endpoint.
        public static bool Tick(RegionCityTile d, DistrictTimberCrew crew, float dt,
            Func<DistrictTimberCrew, TimberDestination> destination,
            Func<DistrictTimberCrew, Vector2, List<Vector2>> returnRoute,
            Func<DistrictTimberCrew, float, bool> move)
        {
            if (!crew.Enabled || dt <= 0 || !float.IsFinite(dt)) return false;
            crew.Script ??= new TimberScript();
            var script = crew.Script;
            if (crew.Phase == "gather")
            {
                if (crew.PendingTrees < script.treesPerLoad) { crew.Status = $"Gathering trees · {crew.PendingTrees}/{script.treesPerLoad}"; return false; }
                crew.PendingTrees -= script.treesPerLoad; crew.CargoTrees = script.treesPerLoad;
                crew.Phase = "dispatch"; crew.Elapsed = 0; return true;
            }
            if (crew.Phase == "dispatch")
            {
                crew.Elapsed -= dt; if (crew.Elapsed > 0) return false;
                crew.Elapsed = script.retrySeconds;
                var target = destination(crew);
                if (target == null) { crew.Status = "Loaded · waiting for road access to a lumber mill"; return false; }
                crew.MillId = target.MillId; crew.Destination = target.Point; crew.Route = target.Route;
                crew.Phase = "outbound"; crew.Elapsed = 0; crew.Status = "Delivering timber by road"; return true;
            }
            if (crew.Phase == "outbound")
            {
                if (!d.Lots.Any(l => l.InstanceId == crew.MillId))
                { crew.Phase = "dispatch"; crew.Elapsed = 0; crew.Route.Clear(); return true; }
                if (!move(crew, dt)) return false;
                crew.Phase = "unload"; crew.Elapsed = 0; crew.Status = "Unloading timber at mill"; return true;
            }
            if (crew.Phase == "unload")
            {
                var mill = d.Lots.Find(l => l.InstanceId == crew.MillId);
                if (mill == null) { crew.Phase = "dispatch"; crew.Elapsed = 0; return true; }
                crew.Elapsed += dt; if (crew.Elapsed < script.unloadSeconds) return false;
                mill.TimberBundles += crew.CargoTrees * script.bundlesPerTree;
                crew.CargoTrees = 0; crew.DeliveredLoads++; crew.Phase = "return-route"; crew.Elapsed = 0;
                crew.Status = "Timber delivered · returning to crew"; return true;
            }
            if (crew.Phase == "return-route")
            {
                crew.Elapsed -= dt; if (crew.Elapsed > 0) return false;
                crew.Elapsed = script.retrySeconds;
                var route = returnRoute(crew, crew.WagonHome);
                if (route == null) { crew.Status = "Waiting for a road back to the crew"; return false; }
                crew.Route = route; crew.Destination = route[route.Count-1]; crew.Phase = "returning"; crew.Elapsed = 0; crew.Status = "Returning to crew"; return true;
            }
            if (crew.Phase == "returning")
            {
                if (!move(crew, dt)) return false;
                crew.Phase = "gather"; crew.MillId = ""; crew.Route.Clear(); crew.Enabled = script.repeat;
                crew.Status = crew.Enabled ? "Gathering next load" : "Cycle complete"; return true;
            }
            return false;
        }
    }
}
