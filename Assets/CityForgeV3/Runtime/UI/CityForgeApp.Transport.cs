using UnityEngine;
using CityForgeV3.World;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private void ComposeTransportFlyout(VisualElement screen)
        {
            var flyout = new VisualElement { name = "transport-category-flyout" };
            flyout.AddToClassList("context-panel");
            flyout.style.width = 250;
            flyout.Add(StyledLabel("TRANSPORT", "section-label"));
            flyout.Add(StyledLabel("CHOOSE A CATEGORY", "catalog-title"));
            AddTransportChoice(flyout, "VEHICLES", "Cars & trucks", LotEditorCategory.Vehicles);
            AddTransportChoice(flyout, "RAILROADS", "Rail lines & streetcars", LotEditorCategory.Railroad);
            AddTransportChoice(flyout, "BOATS", "Boats & canoes", LotEditorCategory.Boats);
            screen.Add(flyout);
        }

        private void AddTransportChoice(VisualElement flyout, string title,
            string description, LotEditorCategory category)
        {
            var button = CfButton.Create(title + "  ›", () => SetLotEditorCategory(category), true, "quiet");
            button.name = "transport-choice-" + category.ToString().ToLowerInvariant();
            button.tooltip = description;
            flyout.Add(button);
            flyout.Add(StyledLabel(description, "inspector-note"));
        }

        private void ComposeVehicleInspector(VisualElement inspector)
        {
            inspector.Add(StyledLabel("VEHICLES", "inspector-title"));
            inspector.Add(StyledLabel("CARS & TRUCKS", "section-label"));
            inspector.Add(Property("ACTIVE", _lotWorld.TestVehicleCount.ToString()));
            inspector.Add(CfButton.Create("OPEN VEHICLE LIBRARY…", OpenTestVehicleModal, true, "primary"));
            inspector.Add(StyledLabel(_lotWorld.CanSpawnTestVehicle
                ? "Add vehicles to the connected road circuit."
                : "Build a connected road circuit to run vehicles on this lot.", "inspector-note"));
            inspector.Add(CfButton.Create("TRANSPORT CATEGORIES…",
                () => SetLotEditorCategory(LotEditorCategory.Transport), true, "quiet"));
        }

        private void OpenBoatsModal()
        {
            _placementPropId = "";
            _lotWorld.SetPropPlacementPreview("");
            var panel = CreateDocumentModal("BOAT LIBRARY",
                "Choose a boat, then click the lot to place it.");
            panel.name = "boat-library-panel";
            panel.AddToClassList("road-material-modal-panel");
            panel.AddToClassList("flora-modal-panel");
            var search = new TextField("SEARCH BOATS") { name = "boat-library-search" };
            search.AddToClassList("document-field");
            panel.Add(search);
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "boat-library-scroll" };
            scroll.AddToClassList("flora-modal-scroll");
            var grid = new VisualElement { name = "boat-library-grid" };
            grid.AddToClassList("road-material-grid");
            grid.style.flexShrink = 0;
            scroll.Add(grid);
            panel.Add(scroll);
            void Populate(string query)
            {
                grid.Clear();
                foreach (var boat in BoatCatalog.All)
                {
                    if (!string.IsNullOrWhiteSpace(query) &&
                        (boat.displayName + " " + boat.category).IndexOf(query.Trim(),
                            System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var card = CfButton.Create("", () =>
                    {
                        _placementPropId = boat.id;
                        _lotWorld.SetPropPlacementPreview(boat.id);
                        _lotStatus = boat.displayName + " selected • click the lot to place";
                        RemoveDocumentModal();
                        ComposeLotEditor();
                    }, true, "quiet");
                    card.name = "boat-select-" + boat.id;
                    card.tooltip = "Place " + boat.displayName;
                    card.AddToClassList("road-material-card");
                    card.AddToClassList("character-library-card");
                    var thumbnail = new Image
                    {
                        image = Resources.Load<Texture2D>(boat.thumbnailResourcePath),
                        scaleMode = ScaleMode.ScaleToFit,
                        pickingMode = PickingMode.Ignore
                    };
                    thumbnail.AddToClassList("character-library-image");
                    card.Add(thumbnail);
                    var title = StyledLabel(boat.displayName, "character-library-name");
                    title.pickingMode = PickingMode.Ignore;
                    card.Add(title);
                    var detail = StyledLabel($"{boat.category} • {boat.lengthMeters:0.#} m", "catalog-meta");
                    detail.pickingMode = PickingMode.Ignore;
                    card.Add(detail);
                    grid.Add(card);
                }
                if (grid.childCount == 0)
                    grid.Add(StyledLabel("No boats match your search.", "inspector-note"));
            }
            search.RegisterValueChangedCallback(evt => Populate(evt.newValue));
            Populate("");
            var actions = DocumentModalActions();
            actions.Add(CfButton.Create("CANCEL", RemoveDocumentModal, true, "quiet"));
            panel.Add(actions);
        }

        private void ComposeBoatInspector(VisualElement inspector)
        {
            inspector.Add(StyledLabel("BOATS", "inspector-title"));
            inspector.Add(StyledLabel("BOATS & CANOES", "section-label"));
            inspector.Add(CfButton.Create("LOT BEHAVIORS…", OpenLotBehaviorsModal, true, "quiet"));
            inspector.Add(CfButton.Create("CHOOSE BOAT…", OpenBoatsModal, true, "primary"));
            var pendingBoat = BoatCatalog.Find(_placementPropId);
            if (pendingBoat != null)
                inspector.Add(StyledLabel(pendingBoat.displayName + " • click the lot to place", "inspector-note"));
            if (_lotWorld.SelectedPropIsBoat)
            {
                inspector.Add(StyledLabel(_lotWorld.SelectedBoatDisplayName.ToUpperInvariant(), "inspector-title"));
                var rotate = new VisualElement(); rotate.AddToClassList("inspector-actions");
                rotate.Add(CfButton.Create("↶ ROTATE LEFT", () => RotateSelectedProp(-1), true, "quiet"));
                rotate.Add(CfButton.Create("ROTATE RIGHT ↷", () => RotateSelectedProp(1), true, "quiet"));
                inspector.Add(rotate);
                inspector.Add(CfButton.Create("DELETE SELECTED BOAT", DeleteSelectedProp, true, "danger"));
            }
            inspector.Add(CfButton.Create("TRANSPORT CATEGORIES…",
                () => SetLotEditorCategory(LotEditorCategory.Transport), true, "quiet"));
        }
    }
}
