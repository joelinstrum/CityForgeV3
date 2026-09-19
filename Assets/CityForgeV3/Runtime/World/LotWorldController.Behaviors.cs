using System;
using System.Collections.Generic;
using System.Linq;
using CityForgeV3.Behaviors;
using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private readonly Dictionary<string, CargoLoadingPresentation> _behaviorViews = new();
        private readonly Dictionary<string, string> _behaviorStatuses = new();
        private Func<Vector3, float, float, List<Vector3>> _boatRouteProvider;
        public bool BehaviorSimulationPaused { get; set; }
        private PlacedDistrictLot _timberPlacement;
        private RegionCityTile _timberDistrict;
        public void BindDistrictBehaviors(PlacedDistrictLot placement, RegionCityTile district = null)
        {
            _timberPlacement = placement;
            _timberDistrict = district;
            if (!placement.BehaviorsInitialized)
            {
                placement.Behaviors = _session.Data.Behaviors ?? new List<LotBehaviorInstance>();
                foreach (var behavior in placement.Behaviors)
                {
                    if (IsTimberCampBehavior(behavior))
                    {
                        behavior.Enabled = true;
                        behavior.HasStarted = true;
                        behavior.DistrictCrewId = "";
                        continue;
                    }
                    var definition = BehaviorDefinition(behavior);
                    if (definition != null) behavior.State = CargoLoadingSimulation.Create(definition);
                    behavior.DepartureRoute = new List<Vector3>();
                    behavior.Enabled = definition != null && definition.autoStartInDistrict;
                    behavior.HasStarted = behavior.Enabled;
                }
                placement.BehaviorsInitialized = true;
            }
            _session.Data.Behaviors = placement.Behaviors ??= new List<LotBehaviorInstance>();
            EnsureBuiltInTimberCampBehavior(placement);
            BindTimberCampBehaviors(placement, district);
        }

        private void EnsureBuiltInTimberCampBehavior(
            PlacedDistrictLot placement)
        {
            if (placement.BuiltInTimberCampChecked) return;
            placement.BuiltInTimberCampChecked = true;
            if (placement.LotId != "lumberjack-camp" ||
                LotBehaviors.Any(IsTimberCampBehavior)) return;
            if (!AddTimberCampBehavior(out var error))
            {
                Debug.LogWarning($"Lumberjack Camp migration skipped: {error}");
                return;
            }
            var behavior = LotBehaviors.First(IsTimberCampBehavior);
            behavior.Enabled = true;
            behavior.HasStarted = true;
            behavior.DistrictCrewId = "";
            placement.Behaviors = _session.Data.Behaviors;
        }
        public IReadOnlyList<LotBehaviorInstance> LotBehaviors => _session?.Data?.Behaviors ?? (IReadOnlyList<LotBehaviorInstance>)Array.Empty<LotBehaviorInstance>();
        public void ConfigureBoatRouteProvider(Func<Vector3, float, float, List<Vector3>> provider) => _boatRouteProvider = provider;
        public string BehaviorStatus(string id) => _behaviorStatuses.TryGetValue(id, out var value) ? value : "Ready";
        public bool AddCargoLoadingBehavior(string definitionId)
        {
            var definition = LotBehaviorCatalog.Find(definitionId);
            if (definition == null) return false;
            var props = _session.Data.Props;
            var boat = SelectedPropIsBoat ? props[SelectedPropIndex] : props.FirstOrDefault(p => BoatCatalog.Find(p.PropId) != null);
            if (boat == null) return false;
            _session.Data.Behaviors ??= new List<LotBehaviorInstance>();
            if (_session.Data.Behaviors.Any(b => b.BoatInstanceId == boat.InstanceId)) return false;
            var rotation = Quaternion.Euler(0, boat.RotationQuarterTurns * 90, 0);
            var center = new Vector3(boat.PositionX, .06f, boat.PositionZ);
            var side = rotation * Vector3.right;
            var width = BoatCatalog.Find(boat.PropId).widthMeters;
            var dock = center + side * (width * .5f + .7f);
            var pickup = dock + side * 4f;
            pickup.x = Mathf.Clamp(pickup.x, -LotWidthMeters * .5f + 2, LotWidthMeters * .5f - 2);
            pickup.z = Mathf.Clamp(pickup.z, -LotDepthMeters * .5f + 2, LotDepthMeters * .5f - 2);
            dock.y = SampleTerrainHeight(dock.x, dock.z) + .06f;
            pickup.y = SampleTerrainHeight(pickup.x, pickup.z) + .06f;
            _session.Data.Behaviors.Add(new LotBehaviorInstance
            {
                InstanceId = Guid.NewGuid().ToString("N"), DefinitionId = definitionId,
                BoatInstanceId = boat.InstanceId, Pickup = pickup, Dock = dock,
                State = CargoLoadingSimulation.Create(definition), Enabled = false
            });
            NotifyStateChanged(); return true;
        }
        public bool SetBehaviorAnchorFromPanel(string id, bool pickup, Vector2 point, Vector2 size)
        {
            var behavior = _session.Data.Behaviors?.Find(x => x.InstanceId == id);
            if (behavior == null || !TryLotPointFromPanel(point, size, out var ground)) return false;
            var position = new Vector3(ground.x, SampleTerrainHeight(ground.x, ground.z) + .06f, ground.z);
            if (pickup) behavior.Pickup = position; else behavior.Dock = position;
            ResetBehavior(id); return true;
        }
        public void ResetBehavior(string id)
        {
            var behavior = _session.Data.Behaviors?.Find(x => x.InstanceId == id);
            if (behavior == null) return;
            if (IsTimberCampBehavior(behavior))
            {
                if (_timberDistrict != null &&
                    !string.IsNullOrWhiteSpace(behavior.DistrictCrewId))
                {
                    var crew = DistrictLabor.State(_timberDistrict).TimberCrews?
                        .Find(c => c.Id == behavior.DistrictCrewId);
                    if (crew != null)
                    {
                        crew.PendingTrees = 0;
                        crew.CargoTrees = 0;
                        crew.Phase = "gather";
                        crew.Route?.Clear();
                        crew.Elapsed = 0;
                        crew.Enabled = behavior.Enabled;
                        crew.Status = "Gathering trees";
                    }
                }
                NotifyStateChanged();
                return;
            }
            var definition = BehaviorDefinition(behavior);
            if (definition == null) return;
            behavior.State = CargoLoadingSimulation.Create(definition);
            behavior.DepartureRoute = new List<Vector3>();
            RebuildPropPresentations(); NotifyStateChanged();
        }
        public void RemoveBehavior(string id)
        {
            _session.Data.Behaviors?.RemoveAll(x => x.InstanceId == id);
            RebuildPropPresentations(); NotifyStateChanged();
        }
        public void ToggleBehavior(string id)
        {
            var behavior = _session.Data.Behaviors?.Find(x => x.InstanceId == id);
            if (behavior != null)
            {
                behavior.Enabled = !behavior.Enabled;
                if (IsTimberCampBehavior(behavior) && _timberDistrict != null &&
                    !string.IsNullOrWhiteSpace(behavior.DistrictCrewId))
                {
                    var crew = DistrictLabor.State(_timberDistrict).TimberCrews?
                        .Find(c => c.Id == behavior.DistrictCrewId);
                    if (crew != null) crew.Enabled = behavior.Enabled;
                }
            }
            NotifyStateChanged();
        }

        private void BindTimberCampBehaviors(PlacedDistrictLot placement,
            RegionCityTile district)
        {
            if (!_districtHosted || placement == null || district == null) return;
            var state = DistrictLabor.State(district);
            state.TimberCrews ??= new List<DistrictTimberCrew>();
            foreach (var instance in LotBehaviors.Where(IsTimberCampBehavior))
            {
                var script = instance.TimberCampScript;
                var connector = _session.Data.Connectors?.Find(c =>
                    c.InstanceId == script.connectorId);
                if (connector == null || !connector.AllowsPedestrians)
                {
                    _behaviorStatuses[instance.InstanceId] =
                        "The Lumberjack Camp needs a pedestrian Connector.";
                    continue;
                }
                var access = ConnectorAccess(connector, LotWidthMeters,
                    LotDepthMeters);
                var camp = DistrictPoint(new Vector3(access.Outside.x,
                    0f, access.Outside.y));
                var crew = state.TimberCrews.FirstOrDefault(c =>
                    c.SourceLotInstanceId == placement.InstanceId &&
                    c.SourceBehaviorInstanceId == instance.InstanceId);
                if (crew == null && !string.IsNullOrWhiteSpace(
                    instance.DistrictCrewId))
                    crew = state.TimberCrews.FirstOrDefault(c =>
                        c.Id == instance.DistrictCrewId);
                if (crew == null)
                {
                    try { ValidateTimberCampScript(script, instance.InstanceId); }
                    catch (ArgumentException e)
                    {
                        _behaviorStatuses[instance.InstanceId] = e.Message;
                        continue;
                    }
                    var actors = script.lumberjackIds.Select(id =>
                        _session.Data.Props.Find(p => p.InstanceId == id)).ToList();
                    // District walking treats Lot interiors as obstacles. The
                    // authored actors still identify this crew, but their live
                    // district route starts at the Connector's outside access
                    // point so the first path can actually leave the Lot.
                    var wagonHome = DistrictPoint(new Vector3(access.Outside.x,
                        0f, access.Outside.y));
                    crew = DistrictTimber.Place(district, camp, wagonHome,
                        actors.Count, ToTimberScript(script.harvest));
                    if (crew == null)
                    {
                        _behaviorStatuses[instance.InstanceId] =
                            "Unable to start crew: check treasury and Lot access.";
                        continue;
                    }
                    crew.SourceLotInstanceId = placement.InstanceId;
                    crew.SourceBehaviorInstanceId = instance.InstanceId;
                    crew.Name = script.displayName;
                }
                // Repair crews created by the earlier binding code as well as
                // new crews. Only actors still trapped inside this Lot move;
                // lumberjacks already working in the district keep their pose.
                crew.Camp = camp;
                crew.WagonHome = camp;
                foreach (var worker in state.Workers.Where(w =>
                             w.CrewId == crew.Id))
                    if (DistrictPointIsInsideThisLot(worker.Position))
                    {
                        worker.Position = camp;
                        worker.Route?.Clear();
                        worker.RouteRetry = 0f;
                    }
                instance.DistrictCrewId = crew.Id;
                crew.Script = ToTimberScript(script.harvest);
                crew.Enabled = instance.Enabled;
            }
        }

        private Vector2 DistrictPoint(Vector3 lotLocal)
        {
            var world = transform.TransformPoint(lotLocal);
            var districtLocal = transform.parent != null
                ? transform.parent.InverseTransformPoint(world)
                : world;
            return new Vector2(districtLocal.x, districtLocal.z);
        }

        private bool DistrictPointIsInsideThisLot(Vector2 point)
        {
            var districtPoint = new Vector3(point.x, 0f, point.y);
            var world = transform.parent != null
                ? transform.parent.TransformPoint(districtPoint)
                : districtPoint;
            var local = transform.InverseTransformPoint(world);
            return Mathf.Abs(local.x) <= LotWidthMeters * .5f + .01f &&
                   Mathf.Abs(local.z) <= LotDepthMeters * .5f + .01f;
        }

        private static TimberScript ToTimberScript(
            TimberCampDefinition definition) => new()
        {
            treesPerLoad = definition.treesPerLoad,
            bundlesPerTree = definition.bundlesPerTree,
            unloadSeconds = definition.unloadSeconds,
            retrySeconds = definition.retrySeconds,
            harvestRadiusMeters = definition.harvestRadiusMeters,
            repeat = definition.repeat,
            fastWagon = definition.fastWagon
        };
        private void UpdateLotBehaviors()
        {
            if (_session?.Data == null) return;
            foreach (var key in _behaviorViews.Keys.ToArray())
                if (!LotBehaviors.Any(x => x.InstanceId == key && ReferenceEquals(x, _behaviorViews[key].Instance)))
                { _behaviorViews[key].Dispose(); _behaviorViews.Remove(key); _behaviorStatuses.Remove(key); }
            foreach (var instance in LotBehaviors)
            {
                if (IsTimberCampBehavior(instance))
                {
                    PresentTimberCampPrototypeState(instance);
                    var crew = _timberDistrict == null ||
                        string.IsNullOrWhiteSpace(instance.DistrictCrewId)
                        ? null
                        : DistrictLabor.State(_timberDistrict).TimberCrews?
                            .Find(c => c.Id == instance.DistrictCrewId);
                    _behaviorStatuses[instance.InstanceId] = crew != null
                        ? crew.Status
                        : _districtHosted
                            ? _behaviorStatuses.TryGetValue(instance.InstanceId,
                                out var blocked) ? blocked : "Waiting to create crew"
                            : "Ready - starts when this Lot is placed in a district";
                    continue;
                }
                var definition = BehaviorDefinition(instance);
                if (definition == null) { _behaviorStatuses[instance.InstanceId] = "Behavior definition unavailable"; continue; }
                if (_behaviorViews.TryGetValue(instance.InstanceId, out var previous) && !ReferenceEquals(previous.Definition, definition))
                { previous.Dispose(); _behaviorViews.Remove(instance.InstanceId); }
                if (instance.HasScript && instance.Script != null)
                {
                    try
                    {
                        instance.Pickup = LotObjectRegistry.ResolvePoint(_session.Data, instance.Script.pickup);
                        instance.Dock = LotObjectRegistry.ResolvePoint(_session.Data, instance.Script.dock);
                    }
                    catch (ArgumentException e)
                    {
                        if (_behaviorViews.TryGetValue(instance.InstanceId, out var blocked)) blocked.Present(null, null, false);
                        _behaviorStatuses[instance.InstanceId] = e.Message; continue;
                    }
                }
                instance.State ??= CargoLoadingSimulation.Create(definition);
                if (instance.State.Workers == null || instance.State.Workers.Length != definition.workers || instance.State.Loaded > definition.capacity)
                { _behaviorStatuses[instance.InstanceId] = "Definition changed — reset behavior"; continue; }
                if (!_behaviorViews.TryGetValue(instance.InstanceId, out var view))
                {
                    if (Resources.Load<GameObject>(definition.workerPrefabResourcePath) == null)
                    { _behaviorStatuses[instance.InstanceId] = "Worker model unavailable"; continue; }
                    view = new CargoLoadingPresentation(transform, instance, definition);
                    _behaviorViews.Add(instance.InstanceId, view);
                }
                var boatIndex = _session.Data.Props.FindIndex(p => p.InstanceId == instance.BoatInstanceId && BoatCatalog.Find(p.PropId) != null);
                var boat = boatIndex >= 0 && boatIndex < _propPresentations.Count ? _propPresentations[boatIndex] : null;
                List<Vector3> route = null;
                if (boat != null && _boatRouteProvider != null)
                {
                    var savedBoat = _session.Data.Props[boatIndex];
                    var boatDefinition = BoatCatalog.Find(savedBoat.PropId);
                    var origin = transform.TransformPoint(new Vector3(savedBoat.PositionX, .055f, savedBoat.PositionZ));
                    route = _boatRouteProvider(origin, boatDefinition.widthMeters, boatDefinition.lengthMeters);
                    var water = _districtRiverSurfaceSampler?.Invoke(origin);
                    if (!instance.State.Departing && water.HasValue && water.Value.UnderWater)
                    { var pos = boat.position; pos.y = water.Value.WaterElevation - .25f; boat.position = pos; }
                }
                var length = 0f;
                if (route != null) for (var i = 1; i < route.Count; i++) length += Vector3.Distance(route[i - 1], route[i]);
                var connected = length > 0;
                instance.DepartureRoute ??= new List<Vector3>();
                if (instance.State.Departing && instance.DepartureRoute.Count > 1)
                {
                    connected &= route != null && route.Count == instance.DepartureRoute.Count;
                    if (connected) for (var i = 0; i < route.Count; i++)
                        connected &= Vector3.Distance(transform.InverseTransformPoint(route[i]), instance.DepartureRoute[i]) < .05f;
                    // Retain the last valid pose when terrain editing removes or changes the channel.
                    route = instance.DepartureRoute.Select(transform.TransformPoint).ToList();
                }
                var wasDeparted = instance.State.Departed;
                var supplied = true;
                if (_districtHosted && _timberPlacement != null && definition.requireTimberDelivery)
                {
                    supplied = instance.State.TimberReserved || instance.State.Departing || instance.State.Departed;
                    if (instance.Enabled && !BehaviorSimulationPaused)
                        supplied = CargoLoadingSimulation.ReserveTimber(definition, instance.State, ref _timberPlacement.TimberBundles);
                }
                instance.WaitingForTimber = !supplied;
                if (instance.Enabled && !BehaviorSimulationPaused && supplied)
                {
                    instance.HasStarted = true;
                    var loadedBefore=instance.State.Loaded;
                    var deliveriesBefore = instance.State.CompletedCycles;
                    CargoLoadingSimulation.Step(definition, instance.State, Time.deltaTime, view.TravelSeconds, boat != null, connected, length);
                    // State and inventory belong to the same district save. Only
                    // newly placed bundles count; reload, sailing and reset do not.
                    if(_districtHosted)DistrictTimber.CreditBargeLoading(_timberDistrict,loadedBefore,instance.State.Loaded);
                    if (_districtHosted && _timberDistrict != null && _timberPlacement != null && instance.State.CompletedCycles > deliveriesBefore)
                        DistrictLotSimulation.For(_timberDistrict).DeliveryCompleted(_timberPlacement.InstanceId, "lumber");
                }
                if (wasDeparted && !instance.State.Departed)
                {
                    instance.DepartureRoute.Clear();
                    if (boat != null)
                    {
                        var savedBoat = _session.Data.Props[boatIndex];
                        boat.localPosition = new Vector3(savedBoat.PositionX, .055f, savedBoat.PositionZ);
                        boat.localRotation = Quaternion.Euler(0, savedBoat.RotationQuarterTurns * 90, 0);
                        boat.gameObject.SetActive(true);
                    }
                }
                if (instance.State.Departing && instance.DepartureRoute.Count == 0 && route != null)
                    instance.DepartureRoute = route.Select(transform.InverseTransformPoint).ToList();
                view.Present(boat, route, !BehaviorSimulationPaused && supplied);
                _behaviorStatuses[instance.InstanceId] = instance.Enabled && !BehaviorSimulationPaused
                    ? !supplied ? $"Waiting for timber delivery · {_timberPlacement?.TimberBundles ?? 0}/{definition.capacity} bundles"
                        : CargoLoadingSimulation.Status(definition, instance.State, boat != null, connected) : instance.HasStarted ? "Paused" : "Ready";
            }
        }

        private void PresentTimberCampPrototypeState(LotBehaviorInstance instance)
        {
            if (!IsTimberCampBehavior(instance) || _session?.Data?.Props == null)
                return;
            var script = instance.TimberCampScript;
            var hidden = new HashSet<string>(script.lumberjackIds,
                StringComparer.Ordinal) { script.wagonId };
            for (var i = 0; i < _session.Data.Props.Count &&
                i < _propPresentations.Count; i++)
            {
                if (!hidden.Contains(_session.Data.Props[i].InstanceId) ||
                    _propPresentations[i] == null) continue;
                _propPresentations[i].gameObject.SetActive(!_districtHosted);
            }
        }
        private void DisposeLotBehaviors()
        {
            foreach (var view in _behaviorViews.Values) view.Dispose();
            _behaviorViews.Clear();
        }
    }
}
