using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        void RefreshDistrictSelectionPanel(RegionCityTile district)
        {
            _root?.Q<VisualElement>(className: "district-selected-lot-panel")?.RemoveFromHierarchy();
            var panel = ComposeSelectedDistrictLotPanel(district);
            if (panel != null) _root?.Q<VisualElement>(className: "district-terraform-screen")?.Add(panel);
        }

        bool SelectIndustryAt(RegionCityTile district, Vector2 panelPosition)
        {
            var p = DistrictCameraPoint(panelPosition);
            var hits = _districtWorld.CollectDistrictSelectionInScreenRect(district, new Rect(p.x - 4, p.y - 4, 8, 8));
            var item = hits.FirstOrDefault(s => s.Kind == DistrictSelectionKind.Quarry || s.Kind == DistrictSelectionKind.Brickworks);
            if (string.IsNullOrEmpty(item.Id)) return false;
            _selectedDistrictLotInstanceId = "";
            _districtSelection.Clear(); _districtSelection.Add(item);
            _districtWorld.ShowDistrictSelection(district, _districtSelection);
            RefreshDistrictSelectionPanel(district); return true;
        }

        VisualElement ComposeSelectedIndustryPanel(RegionCityTile district)
        {
            if (_districtSelection.Count != 1) return null;
            var item = _districtSelection[0];
            if (!DistrictWorldController.IndustryFootprint(district, item, out _, out _, out var yaw)) return null;
            var panel = new VisualElement(); panel.AddToClassList("district-selected-lot-panel");
            panel.Add(StyledLabel("SELECTED BUILDING", "district-lot-info-kicker"));
            panel.Add(StyledLabel(item.Kind == DistrictSelectionKind.Quarry ? "Stone Quarry" : "Brickworks", "district-lot-info-name"));
            panel.Add(StyledLabel($"FACING {yaw:0}°", "district-lot-info-meta"));
            var controls = new VisualElement(); controls.AddToClassList("district-lot-rotation-controls");
            foreach (int direction in new[] { -1, 1 })
            {
                int turn = direction;
                var reason = _districtWorld.IndustryRotationReason(district, item, turn);
                var button = CfButton.Create(turn < 0 ? "↶" : "↷", () => RotateSelectedDistrictObject(turn), string.IsNullOrEmpty(reason), "icon");
                button.name = turn < 0 ? "selected-rotate-left" : "selected-rotate-right";
                button.tooltip = string.IsNullOrEmpty(reason) ? "Rotate 90 degrees" : reason; controls.Add(button);
            }
            panel.Add(controls);
            panel.Add(StyledLabel("R rotates clockwise · Shift+R counter-clockwise · Click empty land to deselect", "district-lot-info-hint"));
            return panel;
        }

        bool RotateSelectedDistrictObject(int direction)
        {
            if (_placingBrickworks || IndustryPlacementActive || !string.IsNullOrEmpty(_pendingDistrictLotId)) return false;
            var d = FindSelectedRegionTile(); if (d == null) return false;
            if (_districtSelection.Count == 1 && DistrictWorldController.IndustryFootprint(d, _districtSelection[0], out _, out _, out _))
            {
                var item = _districtSelection[0];
                var reason = _districtWorld.IndustryRotationReason(d, item, direction);
                if (!string.IsNullOrEmpty(reason)) { ShowDistrictNotice(reason); return true; }
                EnsureDistrictUndo(d);
                _districtWorld.PrepareIndustryRotation(d, item, direction);
                SaveDistrictEdit(); _districtWorldCompositionKey = "";
                EnsureDistrictWorld(d);
                Show(AppScreen.DistrictTerraform); return true;
            }
            var lot = d.Lots?.Find(l => l.InstanceId == _selectedDistrictLotInstanceId);
            if (lot == null) return false;
            if (CanRotateDistrictLot(d, lot, direction)) RotateDistrictLot(d, lot, direction);
            else ShowDistrictNotice("This rotation would overlap another lot or leave valid ground.");
            return true;
        }
    }
}
