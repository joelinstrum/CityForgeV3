using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForgeV3.World
{
    // Presentation contract: identity, visible picking geometry, and supported actions.
    // Successful changes may carry advisory diagnostics without preventing persistence.
    public readonly struct DistrictActionResult
    {
        public readonly bool Succeeded;
        public readonly string Message;
        public DistrictActionResult(bool succeeded, string message = "") { Succeeded = succeeded; Message = message; }
        public static DistrictActionResult Applied(string warning = "") => new(true, warning);
        public static implicit operator DistrictActionResult(string error) => new(string.IsNullOrEmpty(error), error);
    }
    public sealed class DistrictSelectionAction
    {
        public readonly string Label;
        public readonly Func<DistrictActionResult> Execute;
        public DistrictSelectionAction(string label, Func<DistrictActionResult> execute)
        { Label = label; Execute = execute; }
    }

    public sealed class DistrictSelectable : MonoBehaviour
    {
        public DistrictSelectionRef Identity { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public IReadOnlyList<DistrictSelectionAction> Actions { get; private set; }
        public bool ShowInspector { get; private set; }
        public Func<string> StatusText { get; private set; }
        public Func<string> WarningText { get; private set; }
        // Operational diagnostics are supplied by the owner, rendered by the shared inspector.
        public DistrictSelectable WithWarnings(Func<string> warnings) { WarningText = warnings; return this; }
        public Action<Vector2> Nudge { get; private set; }
        public DistrictSelectable WithNudge(Action<Vector2> move) { Nudge=move;return this; }
        public void NudgeBy(Vector2 delta)
        { if(Nudge!=null)Nudge(delta);else transform.localPosition+=new Vector3(delta.x,0,delta.y); }
        public DistrictSelectable WithStatus(Func<string> status) { StatusText = status; return this; }
        public Action DeleteBuilding { get; private set; }
        public bool PreservesResource { get; private set; }
        public Action RefreshAfterDeletion { get; private set; }
        // Each owner defines demolition; the inspector and keyboard share this capability.
        public DistrictSelectable WithBuildingDeletion(Action delete, bool preservesResource = false, Action refresh = null)
        { DeleteBuilding = delete; PreservesResource = preservesResource; RefreshAfterDeletion = refresh; return this; }
        Renderer[] geometry = Array.Empty<Renderer>();
        public void Configure(DistrictSelectionRef identity, string title, string description,
            IEnumerable<Renderer> renderers, bool inspector, params DistrictSelectionAction[] actions)
        {
            Identity = identity; Title = title; Description = description;
            geometry = renderers.Where(r => r != null && !(r is LineRenderer)).ToArray();
            Actions = actions ?? Array.Empty<DistrictSelectionAction>(); ShowInspector = inspector;
        }
        public void SetInspector(string description, params DistrictSelectionAction[] actions)
        { Description = description; Actions = actions; ShowInspector = true; }
        bool Visible(Renderer r) => r != null && r.enabled && r.gameObject.activeInHierarchy;
        public bool Hit(Ray ray, out float distance)
        {
            distance = float.PositiveInfinity;
            foreach (var renderer in geometry)
                if (Visible(renderer) && renderer.bounds.IntersectRay(ray, out var hit))
                    distance = Mathf.Min(distance, hit);
            return !float.IsPositiveInfinity(distance);
        }
        public bool WorldBounds(out Bounds bounds)
        {
            bounds = default; bool any = false;
            foreach (var renderer in geometry)
                if (Visible(renderer))
                { if (!any) bounds = renderer.bounds; else bounds.Encapsulate(renderer.bounds); any = true; }
            return any;
        }
        public bool Overlaps(Camera camera, Rect rectangle)
        {
            foreach (var renderer in geometry)
            {
                if (!Visible(renderer)) continue;
                var b = renderer.bounds; var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = -min; bool inFront = false;
                for (int i = 0; i < 8; i++)
                {
                    var p = camera.WorldToScreenPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
                    if (p.z <= 0) continue;
                    inFront = true; var pixel = new Vector2(p.x, Screen.height - p.y);
                    min = Vector2.Min(min, pixel); max = Vector2.Max(max, pixel);
                }
                if (inFront && rectangle.Overlaps(Rect.MinMaxRect(min.x, min.y, max.x, max.y))) return true;
            }
            return false;
        }
    }

    public sealed partial class DistrictWorldController
    {
        public Vector2 SavedNudge(DistrictSelectionRef identity) => DistrictLotNudge.GetOffset(_terrainDistrict,identity.Kind,identity.Id);
        public bool NudgeBounds(DistrictSelectable target,out Rect bounds)
        {
            bounds=default;if(_content==null||target==null||!target.WorldBounds(out var b))return false;
            var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var max=-min;
            for(int i=0;i<8;i++){
                var v=_content.InverseTransformPoint(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z));
                min=Vector2.Min(min,new Vector2(v.x,v.z));max=Vector2.Max(max,new Vector2(v.x,v.z));}
            bounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return true;
        }
        public void ShowNudgeBoundary(Rect rect)
        {
            float cell=DistrictScale.CellSizeMeters;
            ShowLotOutline(Mathf.RoundToInt((rect.xMin+_widthMeters/2)/cell),Mathf.RoundToInt((rect.yMin+_depthMeters/2)/cell),Mathf.RoundToInt(rect.width/cell),Mathf.RoundToInt(rect.height/cell),true);
            _lotOutlineRenderer.startWidth = _lotOutlineRenderer.endWidth = .09f;
        }

        public void RemoveFloraPresentations(IEnumerable<string> ids)
        {
            foreach (var id in ids)
                if (_districtFloraPresentations.TryGetValue(id, out var renderer))
                {
                    _districtFloraPresentations.Remove(id);
                    if (renderer != null) { renderer.gameObject.SetActive(false); Destroy(renderer.gameObject); }
                }
            // Shadows are children of each flora root; surviving trees need no refresh.
            SelectDistrictFlora("");
        }

        public void RemoveBuildingPresentation(DistrictSelectable target)
        {
            if (target == null) return;
            if (target.Identity.Kind == DistrictSelectionKind.Lot &&
                _lotsByInstance.TryGetValue(target.Identity.Id, out var lot))
            { _lots.Remove(lot); _lotsByInstance.Remove(target.Identity.Id); }
            if (target.RefreshAfterDeletion != null) target.RefreshAfterDeletion();
            else { target.gameObject.SetActive(false); Destroy(target.gameObject); }
        }

        public DistrictSelectable RegisterSelectable(GameObject root, DistrictSelectionRef identity,
            string title, string description = "", bool inspector = true,
            IEnumerable<Renderer> geometry = null, params DistrictSelectionAction[] actions)
        {
            var target = root.GetComponent<DistrictSelectable>();
            if (target == null) target = root.AddComponent<DistrictSelectable>();
            target.Configure(identity, title, description,
                geometry ?? root.GetComponentsInChildren<Renderer>(), inspector, actions);
            return target;
        }
        public DistrictSelectable ResolveSelectable(DistrictSelectionRef identity) =>
            SelectionTargets().FirstOrDefault(t => t.Identity.Kind == identity.Kind && t.Identity.Id == identity.Id);
        IEnumerable<DistrictSelectable> SelectionTargets() => _content == null
            ? Array.Empty<DistrictSelectable>() : _content.GetComponentsInChildren<DistrictSelectable>().Where(t => t.Actions != null);
        public DistrictSelectable FindSelectableAtScreenPoint(Vector2 pixel, bool inspectorsOnly = false)
        {
            if (_camera == null) return null;
            var ray = _camera.ScreenPointToRay(new Vector3(pixel.x, Screen.height - pixel.y, 0));
            float nearest = float.PositiveInfinity; DistrictSelectable result = null;
            foreach (var target in SelectionTargets())
                if ((!inspectorsOnly || target.ShowInspector) && target.Hit(ray, out var distance) && distance < nearest)
                { nearest = distance; result = target; }
            return result;
        }
    }
}
