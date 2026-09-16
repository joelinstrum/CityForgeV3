using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        DistrictSelectable _nudgingLot;
        DistrictLotNudge _lotNudge;
        RegionCityTile _nudgeDistrict;
        VisualElement _nudgeSurface;
        int _nudgePointer;
        Vector2 _nudgeStartPoint, _nudgeStartPixel, _nudgeStartOffset;
        Rect _nudgeStartBounds;
        bool _nudgeCreated, _nudgeMoved;

        void BeginLotNudge(VisualElement surface, int pointer, Vector2 pixel)
        {
            if (_districtSelection.Count != 1 || _nudgingLot != null) return;
            var target = _districtWorld.ResolveSelectable(_districtSelection[0]);
            if (target == null || (target.Identity.Kind != DistrictSelectionKind.Lot &&
                target.Identity.Kind != DistrictSelectionKind.Entity)) return;
            if (!_districtWorld.NudgeBounds(target, out var bounds) ||
                !_districtWorld.TryLotDragPoint(pixel, out var point)) return;
            var district = FindSelectedRegionTile();
            if (district == null) return;
            EnsureDistrictUndo(district);
            district.LotNudges ??= new();
            _lotNudge = district.LotNudges.Find(n => n.Kind == target.Identity.Kind && n.Id == target.Identity.Id);
            _nudgeCreated = _lotNudge == null;
            if (_nudgeCreated)
            {
                // Freeze the original enclosing cells. Never re-snap after a drag.
                _lotNudge = new DistrictLotNudge { Kind = target.Identity.Kind, Id = target.Identity.Id,
                    TileBounds = DistrictLotNudge.EnclosingTiles(bounds,
                        DistrictScale.SizeMeters(district.Width), DistrictScale.SizeMeters(district.Height)) };
            }
            _nudgingLot = target; _nudgeDistrict = district;
            _nudgeStartBounds = bounds; _nudgeStartPoint = point; _nudgeStartPixel = pixel;
            _nudgeStartOffset = _lotNudge.Offset; _nudgeMoved = false;
            _nudgeSurface = surface; _nudgePointer = pointer;
            surface.CapturePointer(pointer);
            _districtWorld.ShowNudgeBoundary(_lotNudge.TileBounds);
        }

        void UpdateLotNudge(Vector2 pixel)
        {
            if (_nudgingLot == null || !_districtWorld.TryLotDragPoint(pixel, out var point)) return;
            if (!_nudgeMoved && (pixel - _nudgeStartPixel).sqrMagnitude < 16) return;
            var offset = _nudgeStartOffset + DistrictLotNudge.ClampDelta(_nudgeStartBounds,
                _lotNudge.TileBounds, point - _nudgeStartPoint);
            var delta = offset - _lotNudge.Offset;
            if (delta.sqrMagnitude < .000001f) return;
            if (!_nudgeMoved && _nudgeCreated) _nudgeDistrict.LotNudges.Add(_lotNudge);
            _nudgeMoved = true;
            _lotNudge.Offset = offset;
            _nudgingLot.NudgeBy(delta);
            _districtWorld.ShowNudgeBoundary(_lotNudge.TileBounds);
        }

        void EndLotNudge(bool cancel)
        {
            if (_lotNudge == null) return;
            bool changed = (_lotNudge.Offset - _nudgeStartOffset).sqrMagnitude > .000001f;
            if (cancel && _nudgingLot != null)
            {
                var delta = _nudgeStartOffset - _lotNudge.Offset;
                _lotNudge.Offset = _nudgeStartOffset;
                _nudgingLot.NudgeBy(delta);
            }
            if (_nudgeCreated && (cancel || !changed)) _nudgeDistrict.LotNudges.Remove(_lotNudge);
            var surface = _nudgeSurface; var pointer = _nudgePointer;
            _nudgingLot = null; _lotNudge = null; _nudgeSurface = null;
            if (surface != null && surface.HasPointerCapture(pointer)) surface.ReleasePointer(pointer);
            _laborNavigation = null;
            _districtWorldCompositionKey = DistrictCompositionKey(_nudgeDistrict);
            if (!cancel && changed) SaveDistrictEdit();
            _districtWorld?.HideLotOutline();
            _districtWorld?.ShowDistrictSelection(_nudgeDistrict, _districtSelection);
        }
    }
}
