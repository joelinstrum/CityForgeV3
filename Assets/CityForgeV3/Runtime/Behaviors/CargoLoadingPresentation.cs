using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.Behaviors
{
    public sealed class CargoLoadingPresentation : IDisposable
    {
        public readonly LotBehaviorInstance Instance;
        public readonly CargoLoadingDefinition Definition;
        private readonly Transform _root;
        private readonly DockWorkerVisual[] _workers;
        private readonly Material _wood;
        private Transform _cargo;
        private Transform _boat;
        private int _shownCargo = -1;
        private Vector3 _shownPickup;
        private Transform _pile;
        public CargoLoadingPresentation(Transform parent, LotBehaviorInstance instance, CargoLoadingDefinition definition)
        {
            Instance = instance; Definition = definition;
            _root = new GameObject("Lot Behavior — " + definition.displayName).transform;
            _root.SetParent(parent, false);
            _wood = new Material(Shader.Find("Standard")) { name = "Cargo Lumber", color = new Color(.48f, .29f, .13f) };
            _wood.SetFloat("_Glossiness", .12f);
            _wood.SetFloat("_SpecularHighlights", 0);
            _wood.SetFloat("_GlossyReflections", 0);
            var prefab = Resources.Load<GameObject>(definition.workerPrefabResourcePath);
            _workers = new DockWorkerVisual[definition.workers];
            for (var i = 0; i < _workers.Length; i++)
            {
                var actor = UnityEngine.Object.Instantiate(prefab, _root, false);
                actor.name = "Dock Worker " + (i + 1);
                LotRuntimeObject.Attach(actor.transform, instance.InstanceId + "/worker/" + (i + 1), actor.name);
                var bundle = Bundle(actor.transform, new Vector3(0, 1.05f, .39f), 1.45f);
                _workers[i] = actor.AddComponent<DockWorkerVisual>();
                _workers[i].Initialize(bundle);
            }
            BuildPile();
        }
        public float TravelSeconds => Mathf.Max(.1f, Vector3.Distance(Instance.Pickup, Instance.Dock) / Definition.walkSpeed);
        private Transform Bundle(Transform parent, Vector3 position, float length)
        {
            var bundle = new GameObject("Lumber Bundle").transform;
            bundle.SetParent(parent, false); bundle.localPosition = position;
            for (var j = 0; j < 3; j++)
            {
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(plank.GetComponent<Collider>());
                plank.name = "Lumber Plank"; plank.transform.SetParent(bundle, false);
                plank.transform.localPosition = new Vector3(0, j * .085f, 0);
                plank.transform.localScale = new Vector3(length, .075f, .22f);
                plank.GetComponent<Renderer>().sharedMaterial = _wood;
            }
            return bundle;
        }
        private void BuildPile()
        {
            if (_pile != null) UnityEngine.Object.Destroy(_pile.gameObject);
            _pile = new GameObject("Lumber Pickup Stack").transform; _pile.SetParent(_root, false);
            LotRuntimeObject.Attach(_pile, Instance.InstanceId + "/lumber-pile", "Lumber Pickup Stack");
            _shownPickup = Instance.Pickup;
            _pile.localPosition = Instance.Pickup + new Vector3(1.4f, 0, 0);
            for (var i = 0; i < 12; i++) Bundle(_pile, new Vector3(0, (i / 3) * .25f, (i % 3 - 1) * .3f), 1.65f);
        }
        public void Present(Transform boat, IReadOnlyList<Vector3> route, bool simulationRunning = true)
        {
            if (_shownPickup != Instance.Pickup) BuildPile();
            var state = Instance.State;
            var stockRemaining = Mathf.Max(0, Definition.capacity - state.Loaded - state.Workers.Count(w => w.Carrying));
            var visibleBundles = Instance.WaitingForTimber ? 0 : Mathf.CeilToInt(12f * stockRemaining / Definition.capacity);
            for (var bundle = 0; bundle < _pile.childCount; bundle++)
                _pile.GetChild(bundle).gameObject.SetActive(bundle < visibleBundles);
            var direction = Instance.Dock - Instance.Pickup; direction.y = 0;
            var side = direction.sqrMagnitude > .01f ? Vector3.Cross(Vector3.up, direction.normalized) : Vector3.right;
            for (var i = 0; i < _workers.Length; i++)
            {
                var worker = state.Workers[i];
                var offset = side * ((i - (_workers.Length - 1) * .5f) * .85f);
                var start = Instance.WorkerHomes?.FirstOrDefault(h => h.Worker == i)?.Position ?? (Instance.Pickup + offset); var end = Instance.Dock + offset;
                var moving = worker.Phase == "carry" || worker.Phase == "return";
                var t = Mathf.Clamp01(worker.Elapsed / TravelSeconds);
                var point = worker.Phase == "carry" ? Vector3.Lerp(start, end, t) :
                    worker.Phase == "return" ? Vector3.Lerp(end, start, t) : worker.Phase == "unload" ? end : start;
                var actor = _workers[i].transform;
                actor.localPosition = point;
                var facing = worker.Phase == "return" ? start - end : end - start;
                if (facing.sqrMagnitude > .01f) actor.localRotation = Quaternion.LookRotation(facing);
                _workers[i].SetAction(Instance.Enabled && simulationRunning && boat != null && moving, worker.Carrying);
            }
            if (boat == null) return;
            if (_boat != boat || _shownCargo != state.Loaded)
            {
                if (_cargo != null) UnityEngine.Object.Destroy(_cargo.gameObject);
                _boat = boat; _shownCargo = state.Loaded;
                _cargo = new GameObject("Loaded Lumber — " + state.Loaded).transform;
                _cargo.SetParent(boat, false);
                LotRuntimeObject.Attach(_cargo, Instance.InstanceId + "/loaded-lumber", "Loaded Lumber");
                for (var i = 0; i < state.Loaded; i++)
                    Bundle(_cargo, new Vector3(i % 2 == 0 ? -.72f : .72f, .98f + (i / 6) * .28f, -1f + ((i / 2) % 3) * 1.25f), 1.25f);
            }
            _cargo.localPosition = Instance.CargoOffset;
            if (state.Departing && route != null && route.Count > 1)
            {
                var remaining = state.Distance;
                for (var i = 1; i < route.Count; i++)
                {
                    var segment = route[i] - route[i - 1]; var length = segment.magnitude;
                    if (remaining <= length || i == route.Count - 1)
                    {
                        boat.position = Vector3.Lerp(route[i - 1], route[i], Mathf.Clamp01(remaining / Mathf.Max(.001f, length)));
                        if (segment.sqrMagnitude > .001f) boat.rotation = Quaternion.LookRotation(new Vector3(segment.x, 0, segment.z));
                        break;
                    }
                    remaining -= length;
                }
            }
            boat.gameObject.SetActive(!state.Departed);
        }
        public void Dispose()
        {
            if (_cargo != null) UnityEngine.Object.Destroy(_cargo.gameObject);
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            if (_wood != null) UnityEngine.Object.Destroy(_wood);
        }
    }
}
