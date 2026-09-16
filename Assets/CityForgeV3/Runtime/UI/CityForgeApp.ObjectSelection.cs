using CityForgeV3.World;
using UnityEngine.UIElements;
using UnityEngine;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        bool RotateSelectedDistrictObject(int direction)
        {
            if (_lotNudge != null || _placingBrickworks || IndustryPlacementActive || !string.IsNullOrEmpty(_pendingDistrictLotId) || _districtSelection.Count != 1) return false;
            var identity = _districtSelection[0];
            var target = _districtWorld?.ResolveSelectable(identity);
            if (target == null) return false;
            BindPlacedLotInspector(target);
            var label = direction < 0 ? "LEFT 90" : "RIGHT 90";
            foreach (var action in target.Actions)
                if (action.Label.Contains(label))
                {
                    var district = FindSelectedRegionTile(); EnsureDistrictUndo(district);
                    var result = action.Execute();
                    _selectionWarnings[SelectionWarningKey(identity)] = result.Message;
                    if (result.Succeeded) { SaveDistrictEdit(); _districtWorldCompositionKey = DistrictCompositionKey(district); }
                    _districtWorld.ShowDistrictSelection(district, _districtSelection);
                    RefreshSelectedObjectPanel(); return true;
                }
            return false;
        }

        readonly System.Collections.Generic.Dictionary<string,string> _selectionWarnings = new();
        string SelectionWarningKey(DistrictSelectionRef identity) =>
            (_openRegion?.RegionId ?? "") + "/" + (FindSelectedRegionTile()?.TileId ?? "") + "/" + identity.Kind + "/" + identity.Id;
        bool TryInspectDistrictObject(Vector2 pixel)
        {
            var target = _districtWorld?.FindSelectableAtScreenPoint(pixel, inspectorsOnly: true);
            if (target == null)
            {
                if (!string.IsNullOrEmpty(_selectedDistrictLotInstanceId) ||
                    _districtSelection.Exists(item => item.Kind == DistrictSelectionKind.Entity))
                    ClearSelectedObject();
                return false;
            }
            CancelDistrictSelectionPointer();
            _districtSelectionDragActive = false;
            _districtRoadPointerDown = false; _districtFloraPointerDown = false;
            _districtEdgePanDirection = Vector2Int.zero;
            _districtSelection.Clear(); _districtSelection.Add(target.Identity);
            _selectedDistrictLotInstanceId = target.Identity.Kind == DistrictSelectionKind.Lot ? target.Identity.Id : "";
            _districtWorld.ShowDistrictSelection(FindSelectedRegionTile(), _districtSelection);
            ComposeSelectedObject(target.Identity);
            return true;
        }

        void BindPlacedLotInspector(DistrictSelectable target)
        {
            if (target.Identity.Kind != DistrictSelectionKind.Lot) return;
            var district = FindSelectedRegionTile();
            var placement = district?.Lots?.Find(lot => lot.InstanceId == target.Identity.Id);
            if (placement == null) return;
            var data = LotContentCatalog.Read(placement.LotId);
            var description = data == null ? "Placed lot" :
                $"{LotTypeLabel(data.LotType)} · {data.LotWidthCells} × {data.LotDepthCells} cells · Facing {placement.RotationQuarterTurns * 90}° · Plop cost ${LotEconomy.CalculatePlopCost(data):N0}";
            target.SetInspector(description,
                new DistrictSelectionAction("↶ LEFT 90°", () => TryRotatePlacedDistrictLot(district, placement, -1)),
                new DistrictSelectionAction("RIGHT 90° ↷", () => TryRotatePlacedDistrictLot(district, placement, 1)));
        }

        void ClearSelectedObject()
        {
            _selectedDistrictLotInstanceId = "";
            _districtSelection.Clear();
            _districtWorld?.ShowDistrictSelection(FindSelectedRegionTile(), _districtSelection);
            RefreshSelectedObjectPanel();
        }

        void ComposeSelectedObject(DistrictSelectionRef identity)
        {
            RemoveDocumentModal();
            _districtSelection.Clear(); _districtSelection.Add(identity);
            _selectedDistrictLotInstanceId = identity.Kind == DistrictSelectionKind.Lot ? identity.Id : "";
            RefreshSelectedObjectPanel();
        }

        void RefreshSelectedObjectPanel()
        {
            var screen = _root?.Q<VisualElement>(className: "district-terraform-screen");
            if (screen == null) return;
            screen.Q<VisualElement>("selected-object-panel")?.RemoveFromHierarchy();
            var panel = ComposeSelectedDistrictLotPanel(FindSelectedRegionTile());
            if (panel == null) return;
            panel.style.display = _districtInterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
            screen.Add(panel);
        }

        VisualElement BuildSelectedObjectPanel(DistrictSelectionRef identity)
        {
            var target = _districtWorld?.ResolveSelectable(identity);
            if (target == null || !target.ShowInspector) return null;
            BindPlacedLotInspector(target);
            var panel = new VisualElement { name = "selected-object-panel" };
            panel.AddToClassList("district-selected-lot-panel");
            panel.Add(StyledLabel(identity.Kind == DistrictSelectionKind.Lot ? "SELECTED LOT" : "SELECTED OBJECT", "district-lot-info-kicker"));
            panel.Add(StyledLabel(target.Title, "district-lot-info-name"));
            panel.Add(StyledLabel(target.Description, "district-lot-info-meta"));
            if (target.StatusText != null)
            {
                var status = StyledLabel(target.StatusText(), "district-lot-info-hint");
                status.name = "selected-object-status";
                panel.Add(status);
                // UI Toolkit suspends this schedule when the selection panel detaches.
                status.schedule.Execute(() =>
                {
                    if (target != null && target.StatusText != null) status.text = target.StatusText();
                }).Every(500);
            }
            if (target.WarningText != null)
            {
                var diagnostics = StyledLabel("", "district-lot-info-hint");
                diagnostics.name = "selected-object-warning";
                diagnostics.style.color = new Color(1f, .77f, .35f);
                diagnostics.style.whiteSpace = WhiteSpace.Normal;
                panel.Add(diagnostics);
                void RefreshWarnings()
                {
                    var text = target != null ? target.WarningText?.Invoke() : "";
                    diagnostics.text = string.IsNullOrEmpty(text) ? "" : "ATTENTION\n" + text;
                    diagnostics.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
                }
                RefreshWarnings();
                diagnostics.schedule.Execute(RefreshWarnings).Every(500);
            }
            var message = StyledLabel(_selectionWarnings.TryGetValue(SelectionWarningKey(identity), out var warning) ? warning : "", "district-lot-info-hint");
            message.name = "selection-action-message";
            var actions = new VisualElement();
            actions.AddToClassList("district-lot-rotation-controls");
            actions.style.flexWrap = Wrap.Wrap;
            foreach (var action in target.Actions)
                actions.Add(CfButton.Create(action.Label, () =>
                {
                    var current = _districtWorld?.ResolveSelectable(identity);
                    if (current == null || current != target) { RefreshSelectedObjectPanel(); return; }
                    var district = FindSelectedRegionTile(); if (district == null) return;
                    EnsureDistrictUndo(district);
                    var result = action.Execute();
                    if (!result.Succeeded) { message.text = result.Message; return; }
                    _selectionWarnings[SelectionWarningKey(identity)] = string.IsNullOrEmpty(result.Message) ? "" : "Rotation applied. Possible issue: " + result.Message;
                    SaveDistrictEdit();
                    _districtWorldCompositionKey = DistrictCompositionKey(district);
                    _districtWorld.ShowDistrictSelection(district, _districtSelection);
                    RefreshSelectedObjectPanel();
                }, true, "quiet"));
            panel.Add(actions);
            panel.Add(message);
            if (identity.Kind == DistrictSelectionKind.Lot || target.DeleteBuilding != null)
            {
                if (target.PreservesResource)
                    panel.Add(StyledLabel("Deleting this building preserves the resource for rebuilding.", "district-lot-info-hint"));
                var delete = CfButton.Create("DELETE BUILDING", () =>
                {
                    if (_districtWorld?.ResolveSelectable(identity) != target) { RefreshSelectedObjectPanel(); return; }
                    _districtSelection.Clear(); _districtSelection.Add(identity);
                    // Explicit button presses are separate commands, including while the editor is paused.
                    _districtDeleteFrame = -1;
                    DeleteDistrictSelection();
                }, true, "danger");
                delete.name = "delete-selected-building";
                panel.Add(delete);
            }
            panel.Add(StyledLabel("Drag this lot within its outlined tiles. Click empty land to clear selection.", "district-lot-info-hint"));
            panel.Add(CfButton.Create("CLEAR SELECTION", ClearSelectedObject, true, "quiet"));
            return panel;
        }
    }
}
