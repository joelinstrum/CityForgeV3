using System.Linq;
using UnityEngine;
using CityForgeV3.Behaviors;
namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private string _selectedRuntimeObjectId;
        private Transform _runtimeSelectionMarker;
        private Vector3 _runtimeDragOffset;
        public void BeginRuntimeObjectDrag(Vector2 point, Vector2 size)
        {
            var item = RuntimeSelection;
            if (item != null && TryLotPointFromPanel(point, size, out var ground))
                _runtimeDragOffset = transform.InverseTransformPoint(item.transform.position) - ground;
        }
        public bool DragRuntimeObjectFromPanel(Vector2 point, Vector2 size)
        {
            if (!TryLotPointFromPanel(point, size, out var ground)) return false;
            return MoveSelectedRuntimeObject(ground + _runtimeDragOffset);
        }
        public bool MoveSelectedRuntimeObject(Vector3 target)
        {
            var item = RuntimeSelection;
            if (item == null || ActiveObjectSelection != LotObjectSelectionKind.RuntimeObject) return false;
            var instance = LotBehaviors.FirstOrDefault(b => item.Id.StartsWith(b.InstanceId + "/"));
            if (instance == null) return false;
            target.x = Mathf.Clamp(target.x, -LotWidthMeters / 2 + .5f, LotWidthMeters / 2 - .5f);
            target.z = Mathf.Clamp(target.z, -LotDepthMeters / 2 + .5f, LotDepthMeters / 2 - .5f);
            target.y = SampleTerrainHeight(target.x, target.z) + .06f;
            instance.Enabled = false;
            if (item.Id.EndsWith("/lumber-pile"))
            {
                var delta = target - transform.InverseTransformPoint(item.transform.position);
                instance.Pickup += delta;
                if (instance.HasScript && instance.Script != null)
                    instance.Script.pickup.offset += delta;
                if (instance.WorkerHomes != null) foreach (var home in instance.WorkerHomes) home.Position += delta;
            }
            else if (item.Id.EndsWith("/loaded-lumber"))
            {
                // Cargo remains attached to the boat and keeps its deck elevation.
                target.y = transform.InverseTransformPoint(item.transform.position).y;
                instance.CargoOffset = item.transform.parent.InverseTransformPoint(transform.TransformPoint(target));
            }
            else
            {
                var index = int.Parse(item.Id.Substring(item.Id.LastIndexOf('/') + 1)) - 1;
                instance.WorkerHomes ??= new();
                var home = instance.WorkerHomes.FirstOrDefault(h => h.Worker == index);
                if (home == null) { home = new LotActorHome { Worker = index }; instance.WorkerHomes.Add(home); }
                home.Position = target;
                var worker = instance.State.Workers[index]; worker.Phase = "idle"; worker.Elapsed = 0; worker.Carrying = false;
            }
            item.transform.position = transform.TransformPoint(target);
            return true;
        }
        public void EndRuntimeObjectDrag() { NotifyStateChanged(); }
        private void UpdateRuntimeObjectSelectionMarker()
        {
            var item = ActiveObjectSelection == LotObjectSelectionKind.RuntimeObject ? RuntimeSelection : null;
            if (item == null) { if (_runtimeSelectionMarker != null) _runtimeSelectionMarker.gameObject.SetActive(false); return; }
            if (_runtimeSelectionMarker == null)
            {
                _runtimeSelectionMarker = new GameObject("Runtime Object Selection").transform;
                _runtimeSelectionMarker.SetParent(transform, false);
                var material = LotSurfaceMaterial(new Color(.2f, .7f, 1f, .95f), 2015);
                for (var i = 0; i < 4; i++)
                {
                    var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    edge.GetComponent<Collider>().enabled = false;
                    edge.transform.SetParent(_runtimeSelectionMarker, false);
                    var horizontal = i < 2;
                    edge.transform.localPosition = horizontal ? new Vector3(0,0,i == 0 ? -.5f : .5f) : new Vector3(i == 2 ? -.5f : .5f,0,0);
                    edge.transform.localScale = horizontal ? new Vector3(1,.02f,.04f) : new Vector3(.04f,.02f,1);
                    edge.GetComponent<Renderer>().sharedMaterial = material;
                }
            }
            _runtimeSelectionMarker.gameObject.SetActive(true);
            var position = transform.InverseTransformPoint(item.transform.position);
            position.y = SampleTerrainHeight(position.x, position.z) + .085f;
            _runtimeSelectionMarker.localPosition = position;
            _runtimeSelectionMarker.localScale = item.Id.Contains("/worker/") ? Vector3.one * .8f : new Vector3(1.9f,1,1.2f);
        }
        private LotRuntimeObject RuntimeSelection => GetComponentsInChildren<LotRuntimeObject>()
            .FirstOrDefault(x => x.Id == _selectedRuntimeObjectId && x.gameObject.activeInHierarchy);
        public bool TrySelectRuntimeObjectFromPanel(Vector2 point, Vector2 size) =>
            !_cameraPanInteractionActive && TrySelectRuntimeObject(PanelToCameraPixel(point, size,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight)));
        private bool TrySelectRuntimeObject(Vector2 pixel)
        {
            var ray = _camera.ScreenPointToRay(pixel);
            LotRuntimeObject best = null; var nearest = float.PositiveInfinity;
            foreach (var item in GetComponentsInChildren<LotRuntimeObject>())
            {
                if (!item.gameObject.activeInHierarchy) continue;
                foreach (var renderer in item.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                        !renderer.bounds.IntersectRay(ray, out var distance) || distance >= nearest) continue;
                    best = item; nearest = distance;
                }
            }
            if (best == null) return false;
            // Clear manipulation state; script-owned objects are inspected, not dragged.
            DeselectBuilding3D();
            ReconcileBuildingFocusBeforeObjectSwitch();
            ClearPropSelectionAndPreview(); ClearFloraSelectionAndPreview();
            _session.Select(false);
            _selectedRuntimeObjectId = best.Id;
            ActiveObjectSelection = LotObjectSelectionKind.RuntimeObject;
            return true;
        }
        public bool TrySelectedObjectInfo(out string name, out string id, out Vector3 position)
        {
            name = id = ""; position = default;
            if (ActiveObjectSelection == LotObjectSelectionKind.RuntimeObject)
            {
                var item = RuntimeSelection;
                if (item == null) return false;
                name = item.FriendlyName; id = item.Id;
                position = transform.InverseTransformPoint(item.transform.position); return true;
            }
            if (ActiveObjectSelection != LotObjectSelectionKind.Prop || SelectedPropIndex < 0 || SelectedPropIndex >= PropCount) return false;
            var prop = _session.Data.Props[SelectedPropIndex]; id = prop.InstanceId;
            var view = SelectedPropIndex < _propPresentations.Count ? _propPresentations[SelectedPropIndex] : null;
            name = view != null ? view.name.Replace("Prop — ", "") : prop.PropId;
            name = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Replace('-', ' '));
            position = view != null ? transform.InverseTransformPoint(view.position) : new Vector3(prop.PositionX, 0, prop.PositionZ);
            return true;
        }
    }
}
