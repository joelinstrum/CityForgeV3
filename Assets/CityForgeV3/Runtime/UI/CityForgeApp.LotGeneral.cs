using System;
using System.Collections.Generic;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private void OpenLotGeneralModal()
        {
            if (!_hasOpenLot || _lotWorld == null) return;
            var lot = _lotWorld.Session.Data;
            var requirements = lot.Stats?.Copy() ?? new LotStats();
            var baseCost = Math.Max(0, lot.BasePlopCost);
            var widthCells = Mathf.Clamp(_lotWorld.LotWidthCells, 1, 8);
            var depthCells = Mathf.Clamp(_lotWorld.LotDepthCells, 1, 8);
            var riverReflectionEnabled = _lotWorld.RiverReflectionEnabled;
            var panel = CreateDocumentModal("LOT GENERAL",
                "Set the category, size, and build requirements. Apply changes, then Save the Lot to keep them.");
            panel.name = "lot-general-panel";
            panel.style.width = 760;
            var scroll = new ScrollView(ScrollViewMode.Vertical)
            { name = "lot-general-scroll" };
            scroll.style.height = 490;
            scroll.style.flexShrink = 0;
            scroll.style.overflow = Overflow.Hidden;
            scroll.contentViewport.style.overflow = Overflow.Hidden;
            scroll.contentViewport.style.height = 490;
            scroll.contentViewport.style.maxHeight = 490;
            panel.Add(scroll);

            void Heading(string title)
            {
                var heading = StyledLabel(title, "inspector-title");
                heading.style.marginTop = 16;
                scroll.Add(heading);
            }
            void Number(string title, int value, Action<int> set,
                int maximum = int.MaxValue)
            {
                var field = new IntegerField(title)
                { value = Math.Clamp(value, 0, maximum), isDelayed = true };
                field.AddToClassList("document-field");
                field.style.flexDirection = FlexDirection.Row;
                field.style.minHeight = 42;
                field.style.flexShrink = 0;
                field.Q(className: "unity-base-field__input").style.flexGrow = 1;
                field.RegisterValueChangedCallback(e =>
                {
                    var amount = Math.Clamp(e.newValue, 0, maximum);
                    field.SetValueWithoutNotify(amount);
                    set(amount);
                });
                scroll.Add(field);
            }
            void Flag(string title, bool value, Action<bool> set)
            {
                var field = CfButton.Create((value ? "✓ " : "○ ") + title,
                    null, true, "quiet");
                field.clicked += () =>
                {
                    value = !value;
                    field.text = (value ? "✓ " : "○ ") + title;
                    set(value);
                };
                field.style.minHeight = 38;
                field.style.width = Length.Percent(100);
                scroll.Add(field);
            }

            Heading("IDENTITY & CATEGORY");
            var nameField = new TextField("LOT NAME")
            { value = _lotWorld.CurrentLotName };
            nameField.AddToClassList("document-field");
            scroll.Add(nameField);
            var parents = LotTypeParentChoices();
            var parentField = new CityForgeChoiceField(_root, "PARENT CATEGORY",
                parents, Math.Max(0, parents.IndexOf(
                    LotTypeParentLabel(_lotWorld.LotType))));
            scroll.Add(parentField);
            var subcategoryField = new CityForgeChoiceField(_root,
                "CIVICS SUBCATEGORY", CivicsSubcategoryChoices(),
                CivicsSubcategoryIndex(_lotWorld.LotType));
            void ShowSubcategory(string parent) => subcategoryField.style.display =
                parent == "Civics" ? DisplayStyle.Flex : DisplayStyle.None;
            parentField.changed += ShowSubcategory;
            ShowSubcategory(parentField.value);
            scroll.Add(subcategoryField);
            var eraField = new CityForgeChoiceField(_root, "LOT ERA",
                new List<string>(LotEraCatalog.DisplayNames),
                LotEraCatalog.IndexOf(_lotWorld.CurrentEraId));
            scroll.Add(eraField);
            var trafficField = new CityForgeChoiceField(_root, "TRAFFIC TYPE",
                new List<string> { "None", "Suburban Street", "Parking Lot" },
                Mathf.Clamp((int)_lotWorld.TrafficType, 0, 2));
            scroll.Add(trafficField);
            var widthField = new CityForgeCellCountField("WIDTH", widthCells,
                cells => widthCells = cells);
            scroll.Add(widthField);
            var depthField = new CityForgeCellCountField("LENGTH", depthCells,
                cells => depthCells = cells);
            scroll.Add(depthField);
            scroll.Add(StyledLabel("Each major cell is 10 × 10 meters.",
                "inspector-note"));

            Heading("VISUALS");
            Flag("River reflections", riverReflectionEnabled,
                value => riverReflectionEnabled = value);
            scroll.Add(StyledLabel(
                "Reflect eligible buildings in nearby river water at close zoom.",
                "inspector-note"));

            Heading("BUILD REQUIREMENTS");
            var buildingCost = Math.Max(0,
                LotEconomy.CalculatePlopCost(lot) - Math.Max(0, lot.BasePlopCost));
            var totalCost = new Label();
            void UpdateTotal() => totalCost.text =
                $"Total placement: ${Math.Min(int.MaxValue, (long)baseCost + buildingCost):N0} (includes ${buildingCost:N0} in buildings)";
            Number("Lot base cost ($)", baseCost, n =>
            { baseCost = n; UpdateTotal(); });
            UpdateTotal();
            scroll.Add(totalCost);
            var minimumEra = new CityForgeChoiceField(_root,
                "MINIMUM BUILD ERA", new List<string>(LotEraCatalog.DisplayNames),
                LotEraCatalog.IndexOf(requirements.MinimumEraId));
            scroll.Add(minimumEra);
            Number("Min population", requirements.MinimumPopulation,
                n => requirements.MinimumPopulation = n);
            Number("Min education score (0–100)",
                requirements.MinimumEducationScore,
                n => requirements.MinimumEducationScore = n, 100);
            Flag("Requires road access", requirements.RequiresRoad,
                value => requirements.RequiresRoad = value);
            Flag("Requires waterfront", requirements.RequiresWaterfront,
                value => requirements.RequiresWaterfront = value);
            scroll.Add(StyledLabel(
                "Construction materials are tonnes paid once from the district stockpile. Building materials are added to these amounts.",
                "inspector-note"));
            requirements.ConstructionResources ??= new List<ContentResourceAmount>();
            foreach (var id in new[] { "food", "lumber", "stone", "brick",
                "coal", "iron-ore", "gold", "oil", "jewels", "cloth" })
            {
                var item = requirements.ConstructionResources.Find(x =>
                    x != null && SameConstructionResourceSlot(x.resourceId, id));
                if (item == null)
                {
                    item = new ContentResourceAmount { resourceId = id };
                    requirements.ConstructionResources.Add(item);
                }
                var captured = item;
                var label = id == "brick" ? "BRICKS" :
                    id == "iron-ore" ? "IRON ORE" : id.ToUpperInvariant();
                Number(label + " (t)", Mathf.CeilToInt(item.amount),
                    amount => captured.amount = amount);
            }

            void ApplyChanges()
            {
                requirements.MinimumEraId = LotEraCatalog.IdForDisplayName(
                    minimumEra.value);
                var type = LotTypeFromCategorySelection(parentField.value,
                    subcategoryField.value);
                _lotWorld.ConfigureLot(nameField.value, type, widthCells,
                    depthCells, LotEraCatalog.IdForDisplayName(eraField.value));
                _lotWorld.SetTrafficType(
                    TrafficLotModel.ForDisplayName(trafficField.value));
                _lotWorld.SetRiverReflectionEnabled(riverReflectionEnabled);
                lot.BasePlopCost = baseCost;
                lot.Stats = requirements;
            }

            Heading("WATER ORIENTATION");
            scroll.Add(StyledLabel(
                "Place an arrow on the Lot, then choose which side faces water.",
                "inspector-note"));
            scroll.Add(CfButton.Create("WATER-FACING ARROW", () =>
            {
                ApplyChanges();
                RemoveDocumentModal();
                _waterOrientationClick = 1;
                _lotStatus = "Click to place the arrow, then choose a direction";
                RefreshLotEditor();
            }, true, "quiet"));
            if (_waterOrientationClick > 0)
                scroll.Add(CfButton.Create("CANCEL ARROW", () =>
                {
                    ApplyChanges();
                    RemoveDocumentModal();
                    _waterOrientationClick = 0;
                    RefreshLotEditor();
                }, true, "quiet"));
            if (_lotWorld.HasWaterOrientation)
                scroll.Add(CfButton.Create("REMOVE WATER ARROW", () =>
                {
                    ApplyChanges();
                    RemoveDocumentModal();
                    _lotWorld.ClearWaterOrientation();
                    RefreshLotEditor();
                }, true, "secondary"));

            foreach (var child in scroll.contentContainer.Children())
                child.style.flexShrink = 0;
            var actions = DocumentModalActions();
            actions.Add(CfButton.Create("APPLY", () =>
            {
                ApplyChanges();
                RemoveDocumentModal();
                _lotStatus = "Lot General updated — Save to keep changes";
                Show(AppScreen.LotEditor);
            }, true, "primary"));
            actions.Add(CfButton.Create("CANCEL", RemoveDocumentModal,
                true, "quiet"));
            panel.Add(actions);
        }

        private static bool SameConstructionResourceSlot(string authored,
            string slot)
        {
            if (string.Equals(authored, slot,
                    StringComparison.OrdinalIgnoreCase)) return true;
            var id = authored?.Trim().ToLowerInvariant();
            return slot switch
            {
                "lumber" => id == "wood",
                "brick" => id == "bricks",
                "iron-ore" => id is "iron ore" or "ironore",
                "jewels" => id == "jewel",
                _ => false
            };
        }
    }
}
