using System;

namespace CityForgeV3.Behaviors
{
    // No Unity dependency: this state machine can be hosted by a headless .NET runner.
    [Serializable] public sealed class CargoLoadingDefinition
    {
        public string id = "lumber-loading-v01";
        public string displayName = "Load lumber barge";
        public string kind = "cargo-loading-v1";
        public string workerPrefabResourcePath = "CityForgeV3/Characters/DockWorkerV01/DockWorkerV01";
        public int workers = 2;
        public int capacity = 12;
        public float pickupSeconds = 2;
        public float unloadSeconds = 2;
        public float walkSpeed = 1.1f;
        public float departureSpeed = 2;
        public float staggerSeconds = 1.5f;
        public bool autoStartInDistrict = true;
        public bool requireTimberDelivery = true;
        public bool repeat;
        public float restartDelaySeconds = 60;
        public void Validate()
        {
            if (kind != "cargo-loading-v1" || workers < 1 || workers > 16 || capacity < 1 || capacity > 1000 ||
                !Positive(restartDelaySeconds) ||
                !Positive(pickupSeconds) || !Positive(unloadSeconds) || !Positive(walkSpeed) || !Positive(departureSpeed) ||
                float.IsNaN(staggerSeconds) || float.IsInfinity(staggerSeconds) || staggerSeconds < 0)
                throw new ArgumentException("Invalid cargo-loading definition: " + id);
        }
        private static bool Positive(float n) => n > 0 && !float.IsNaN(n) && !float.IsInfinity(n);
    }

    [Serializable] public sealed class CargoWorkerState
    {
        public string Phase = "pickup";
        public float Elapsed;
        public bool Carrying;
    }
    [Serializable] public sealed class CargoLoadingState
    {
        public int Loaded;
        public bool TimberReserved;
        public bool Departing;
        public bool Departed;
        public float Distance;
        public float RestartElapsed;
        public int CompletedCycles;
        public CargoWorkerState[] Workers;
    }

    public static class CargoLoadingSimulation
    {
        public static bool ReserveTimber(CargoLoadingDefinition definition, CargoLoadingState state, ref int available)
        {
            if (!definition.requireTimberDelivery || state.TimberReserved || state.Departing || state.Departed) return true;
            // Preserve shipments already in progress in older district saves.
            if (state.Loaded > 0 || Array.Exists(state.Workers, w => w.Carrying)) { state.TimberReserved = true; return true; }
            if (available < definition.capacity) return false;
            available -= definition.capacity; state.TimberReserved = true; return true;
        }
        public static CargoLoadingState Create(CargoLoadingDefinition definition)
        {
            definition.Validate();
            var state = new CargoLoadingState { Workers = new CargoWorkerState[definition.workers] };
            for (var i = 0; i < state.Workers.Length; i++)
                state.Workers[i] = new CargoWorkerState { Elapsed = -i * definition.staggerSeconds };
            return state;
        }
        public static string Status(CargoLoadingDefinition definition, CargoLoadingState state, bool boatPresent, bool routeConnected)
        {
            if (!boatPresent) return "Waiting for assigned boat";
            if (state.Departed) return definition.repeat
                ? "Next shipment in " + Math.Ceiling(Math.Max(0, definition.restartDelaySeconds - state.RestartElapsed)) + "s"
                : "Shipment reached downstream exit";
            if (state.Departing) return routeConnected ? "Sailing downstream" : "Route unavailable — shipment paused";
            if (state.Loaded >= definition.capacity)
            {
                foreach (var worker in state.Workers) if (worker.Phase != "idle") return "Workers returning to shore";
                return routeConnected ? "Ready to depart" : "Full — waiting for a connected river";
            }
            return routeConnected ? "Loading lumber — downstream route ready" : "Loading lumber — departure requires a connected river";
        }
        public static float PhaseDuration(CargoLoadingDefinition definition, CargoWorkerState worker, float travelSeconds) =>
            worker.Phase == "pickup" ? definition.pickupSeconds :
            worker.Phase == "unload" ? definition.unloadSeconds : Math.Max(.1f, travelSeconds);
        public static void Step(CargoLoadingDefinition definition, CargoLoadingState state, float deltaSeconds,
            float travelSeconds, bool boatPresent, bool routeConnected, float routeLength)
        {
            if (!boatPresent || deltaSeconds <= 0 || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds)) return;
            if (state.Departed)
            {
                if (!definition.repeat) return;
                state.RestartElapsed += deltaSeconds;
                if (state.RestartElapsed < definition.restartDelaySeconds) return;
                var fresh = Create(definition);
                state.Loaded = 0; state.TimberReserved = false; state.Distance = 0; state.Departing = false; state.Departed = false;
                state.RestartElapsed = 0; state.Workers = fresh.Workers;
                return;
            }
            routeConnected &= routeLength > 0;
            // Small fixed upper step avoids lost transitions and oversubscription on long frames.
            var remaining = deltaSeconds;
            while (remaining > .00001f)
            {
                var dt = Math.Min(.05f, remaining); remaining -= dt;
                if (state.Departing)
                {
                    if (!routeConnected) return;
                    state.Distance = Math.Min(routeLength, state.Distance + dt * definition.departureSpeed);
                    state.Departed = state.Distance >= routeLength;
                    if (state.Departed) { state.CompletedCycles++; state.RestartElapsed = 0; return; }
                    continue;
                }
                foreach (var worker in state.Workers)
                {
                    if (worker.Phase == "idle") continue;
                    worker.Elapsed += dt;
                    var duration = PhaseDuration(definition, worker, travelSeconds);
                    if (worker.Elapsed < duration) continue;
                    worker.Elapsed -= duration;
                    switch (worker.Phase)
                    {
                        case "pickup":
                            var reserved = state.Loaded;
                            foreach (var other in state.Workers) if (other.Carrying) reserved++;
                            if (reserved < definition.capacity) { worker.Carrying = true; worker.Phase = "carry"; }
                            else { worker.Phase = "idle"; worker.Elapsed = 0; }
                            break;
                        case "carry": worker.Phase = "unload"; break;
                        case "unload":
                            if (worker.Carrying) state.Loaded++;
                            worker.Carrying = false; worker.Phase = "return";
                            break;
                        case "return": worker.Phase = state.Loaded >= definition.capacity ? "idle" : "pickup"; break;
                        default: worker.Phase = "idle"; break;
                    }
                }
                var ready = state.Loaded >= definition.capacity;
                foreach (var worker in state.Workers) ready &= worker.Phase == "idle" && !worker.Carrying;
                if (ready && routeConnected) state.Departing = true;
            }
        }
    }
}
