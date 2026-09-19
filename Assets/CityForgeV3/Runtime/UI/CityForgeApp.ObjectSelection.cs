using CityForgeV3.World;
using UnityEngine.UIElements;
using UnityEngine;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        bool _districtSelectionDetailsExpanded;
        bool _districtSelectionPanelDismissed;

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
            if (DistrictBusinessEconomy.IsBusiness(data) || DistrictBusinessEconomy.Rates(data) != null)
                description += "\n" + DistrictBusinessEconomy.Describe(DistrictBusinessEconomy.Rates(data));
            target.SetInspector(description,
                new DistrictSelectionAction("↶ LEFT 90°", () => TryRotatePlacedDistrictLot(district, placement, -1)),
                new DistrictSelectionAction("RIGHT 90° ↷", () => TryRotatePlacedDistrictLot(district, placement, 1)));
        }

        void ClearSelectedObject()
        {
            _districtSelectionDetailsExpanded = false;
            _districtSelectionPanelDismissed = false;
            _selectedDistrictLotInstanceId = "";
            _districtSelection.Clear();
            _districtWorld?.ShowDistrictSelection(FindSelectedRegionTile(), _districtSelection);
            RefreshSelectedObjectPanel();
        }

        void ComposeSelectedObject(DistrictSelectionRef identity)
        {
            RemoveDocumentModal();
            _districtPaletteOpen = false; _districtPaletteCategoryOpen = false;
            SelectDistrictCategory("Select");
            _districtSelectionDetailsExpanded = false;
            _districtSelectionPanelDismissed = false;
            _districtSelection.Clear(); _districtSelection.Add(identity);
            _selectedDistrictLotInstanceId = identity.Kind == DistrictSelectionKind.Lot ? identity.Id : "";
            RefreshSelectedObjectPanel();
        }

        void RefreshSelectedObjectPanel()
        {
            var screen = _root?.Q<VisualElement>(className: "district-terraform-screen");
            if (screen == null) return;
            screen.Q<VisualElement>("selected-object-panel")?.RemoveFromHierarchy();
            if (_districtSelectionPanelDismissed)
            {
                SetDistrictChromeVisibility(screen);
                return;
            }
            var panel = ComposeSelectedDistrictLotPanel(FindSelectedRegionTile());
            if (panel == null) { SetDistrictChromeVisibility(screen); return; }
            panel.style.display = _districtInterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _districtInfoVisible = false;
            screen.Add(panel);
            SetDistrictChromeVisibility(screen);
        }

        void CloseSelectedObjectPanel()
        {
            _districtSelectionDetailsExpanded = false;
            _districtSelectionPanelDismissed = true;
            var screen = _root?.Q<VisualElement>(className: "district-terraform-screen");
            screen?.Q<VisualElement>("selected-object-panel")?.RemoveFromHierarchy();
            if (screen != null) SetDistrictChromeVisibility(screen);
        }

        VisualElement BuildSelectedObjectPanel(DistrictSelectionRef identity)
        {
            var target = _districtWorld?.ResolveSelectable(identity);
            if (target == null || !target.ShowInspector) return null;
            BindPlacedLotInspector(target);
            var panel = new ScrollView(ScrollViewMode.Vertical) { name = "selected-object-panel", horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            panel.AddToClassList("cf-map-chrome");
            panel.AddToClassList("district-selected-lot-panel");
            var close = new Button(CloseSelectedObjectPanel) { text = "CLOSE", tooltip = "Close details", name = "quiet-selection-close" };
            close.AddToClassList("cf-quiet-close");
            close.AddToClassList("cf-selection-close"); panel.Add(close);
            panel.Add(CfMapChrome.Title(target.Title, "district-lot-info-name"));
            var preview = CfMapChrome.Icon(identity.Kind == DistrictSelectionKind.Lot ? "Lots" : "Industry");
            if (identity.Kind == DistrictSelectionKind.Lot)
            {
                var placed = FindSelectedRegionTile()?.Lots?.Find(item => item.InstanceId == identity.Id);
                var texture = placed == null ? null : LoadSavedLotPreview(placed.LotId);
                if (texture != null) { preview.image = texture; preview.uv = new Rect(0, 0, 1, 1); }
            }
            preview.AddToClassList("cf-map-selection-preview"); panel.Add(preview);
            if (identity.Kind == DistrictSelectionKind.Lot)
            {
                var placed = FindSelectedRegionTile()?.Lots?.Find(item => item.InstanceId == identity.Id);
                var data = placed == null ? null : LotContentCatalog.Read(placed.LotId);
                void Summary(string icon, string value, string tip, Color? color = null)
                {
                    var row = new VisualElement { tooltip = tip, focusable = true };
                    row.AddToClassList("cf-quiet-summary"); row.Add(CfMapChrome.ResourceIcon(icon));
                    var text = new Label(value); if (color.HasValue) text.style.color = color.Value;
                    row.Add(text); panel.Add(row);
                }
                if (data?.Stats != null)
                {
                    Summary("POPULATION", data.Stats.Residents.ToString("N0"), "Population added by this Lot");
                    // Only the selected content's authored benefits, never district objects.
                    if (data.Stats.Benefits != null) foreach (var benefit in data.Stats.Benefits)
                        if (benefit.ResourceId == "food" && benefit.Amount != 0)
                            Summary("FOOD", $"{benefit.Amount:N0} t · {benefit.Timing}", "Food benefit");
                }
                var rates = DistrictBusinessEconomy.Rates(data);
                if (rates != null)
                {
                    int net = rates.SeasonalRevenue - rates.SeasonalCost;
                    Summary("TREASURY", $"{(net >= 0 ? "+" : "−")}${System.Math.Abs((long)net):N0} / season", "Seasonal revenue minus cost",
                        net >= 0 ? new Color(.5f, .85f, .55f) : new Color(1f, .45f, .4f));
                }
            }
            var details = new Foldout { text = "Details & actions", value = _districtSelectionDetailsExpanded, name = "quiet-selection-details" };
            details.RegisterValueChangedCallback(evt => _districtSelectionDetailsExpanded = evt.newValue);
            details.Add(StyledLabel(target.Description, "district-lot-info-meta"));
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
            details.Add(actions);
            panel.Add(message);
            if (identity.Kind == DistrictSelectionKind.Lot || target.DeleteBuilding != null)
            {
                if (target.PreservesResource)
                    details.Add(StyledLabel("Deleting this building preserves the resource for rebuilding.", "district-lot-info-hint"));
                var delete = CfButton.Create("DELETE BUILDING", () =>
                {
                    if (_districtWorld?.ResolveSelectable(identity) != target) { RefreshSelectedObjectPanel(); return; }
                    _districtSelection.Clear(); _districtSelection.Add(identity);
                    // Explicit button presses are separate commands, including while the editor is paused.
                    _districtDeleteFrame = -1;
                    DeleteDistrictSelection();
                }, true, "danger");
                delete.name = "delete-selected-building";
                details.Add(delete);
            }
            details.Add(StyledLabel("Drag this lot within its outlined tiles. Click empty land to clear selection.", "district-lot-info-hint"));
            panel.Add(details);
            return panel;
        }
    }
}
