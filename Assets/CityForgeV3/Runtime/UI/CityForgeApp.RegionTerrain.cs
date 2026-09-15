using System;
using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private void ComposeRegionTerrainModal()
        {
            var region = _openRegion;
            if (region == null) return;
            var saved = region.Terrain ?? new RegionTerrainSettings();
            var draft = new RegionTerrainSettings { DeepRivers = saved.DeepRivers, Streams = saved.Streams, Flow = saved.Flow };
            var panel = CreateDocumentModal("REGION TERRAIN", "Choose terrain options for " + region.Name + ".");
            panel.name = "region-terrain-modal";
            panel.style.width = 1080;
            panel.style.maxWidth = Length.Percent(94);
            panel.style.maxHeight = Length.Percent(90);
            var tabs = new VisualElement();
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.style.flexWrap = Wrap.Wrap;
            panel.Add(tabs);
            var content = new ScrollView();
            content.name = "region-terrain-options";
            content.style.minHeight = 230;
            content.style.flexShrink = 1;
            content.style.marginTop = 18;
            panel.Add(content);
            var notice = StyledLabel("", "inspector-note");
            panel.Add(notice);
            var flow = new EnumField("Main river direction", draft.Flow);
            flow.name = "region-river-flow";
            flow.RegisterValueChangedCallback(e => draft.Flow = (RegionRiverFlow)e.newValue);
            panel.Add(flow);
            foreach (var field in new VisualElement[] {flow})
            {
                field.style.flexDirection = FlexDirection.Row;
                field.style.minHeight = 48;
                field.style.fontSize = 24;
                var input = field.Q<VisualElement>(className: "unity-base-field__input");
                if (input != null) { input.style.flexGrow = 1; input.style.backgroundColor = new Color(.12f,.21f,.27f); input.style.paddingLeft = 12; }
            }
            flow.labelElement.style.minWidth = 270;
            var actions = DocumentModalActions();
            var save = CfButton.Create("GENERATE RIVERS", () =>
            {
                notice.text = GenerateFreshRegionRivers(region, draft) ?? "";
            }, true, "primary");
            save.name = "generate-region-rivers";
            actions.Add(save);
            var cancel = CfButton.Create("CANCEL", RemoveDocumentModal, true, "quiet");
            cancel.name = "cancel-region-terrain-options";
            actions.Add(cancel);
            panel.Add(actions);

            void SelectCategory(string category)
            {
                content.Clear();
                save.SetEnabled(category == "Rivers");
                flow.style.display = category == "Rivers" ? DisplayStyle.Flex : DisplayStyle.None;
                notice.text = "";
                foreach (var button in tabs.Query<Button>().ToList())
                    button.EnableInClassList("cf-button--primary", button.text == category.ToUpperInvariant());
                content.Add(StyledLabel(category.ToUpperInvariant(), "document-modal-title"));
                if (category != "Rivers")
                {
                    content.Add(StyledLabel(category + " options will be added in a later pass.", "document-modal-copy"));
                    return;
                }
                content.Add(StyledLabel("Choose one amount per type. Leave both unchecked for none.", "document-modal-copy"));
                AddRegionWaterChoices(content, "deep-rivers", "A few deep rivers", "Many deep rivers", draft.DeepRivers,
                    value => draft.DeepRivers = value);
                AddRegionWaterChoices(content, "streams", "A few streams", "Many streams", draft.Streams,
                    value => draft.Streams = value);
                content.Add(StyledLabel("Few: 2 deep rivers or 4 streams. Many: 5 deep rivers or 10 streams. Rivers and streams form connected branches with varied bends. Generation replaces this tool’s rivers; manually placed rivers stay. Each generation chooses a fresh placement.", "inspector-note"));
            }
            foreach (var category in new[] { "Rivers", "Shorefront", "Roads", "Mountains", "Hills" })
            {
                var captured = category;
                var button = CfButton.Create(category.ToUpperInvariant(), () => SelectCategory(captured), true, "quiet");
                button.name = "region-terrain-" + category.ToLowerInvariant();
                button.style.marginRight = 8;
                button.style.minWidth = 174;
                button.style.fontSize = 22;
                button.style.whiteSpace = WhiteSpace.NoWrap;
                tabs.Add(button);
            }
            SelectCategory("Rivers");
        }

        private string GenerateFreshRegionRivers(RegionSaveData region, RegionTerrainSettings draft)
        {
            var newSeed = RegionRiverGenerator.FreshSeed(region.RiverSeed);
            var previous = region.Terrain;
            var previousPaths = region.RiverPaths;
            var previousSeed = region.RiverSeed;
            var previousRivers = region.Tiles.Select(tile => tile.Rivers).ToList();
            try
            {
                var paths = RegionRiverGenerator.Generate(region, draft, newSeed);
                var conflict = FindRegionRiverBuildingConflict(region, paths);
                if (conflict != null)
                {
                    return "This layout crosses a building in " + conflict + ". Try regenerating again or choosing fewer rivers.";
                }
                // Use fresh lists so a failed save can restore the complete old layout.
                foreach (var tile in region.Tiles)
                    tile.Rivers = new List<PlacedDistrictRiver>(tile.Rivers ?? new List<PlacedDistrictRiver>());
                RegionRiverGenerator.Apply(region, paths);
                region.Terrain = draft;
                region.RiverSeed = newSeed;
                RegionSaveStore.Save(region);
            }
            catch (Exception exception)
            {
                region.Terrain = previous;
                region.RiverPaths = previousPaths;
                region.RiverSeed = previousSeed;
                for (var i = 0; i < region.Tiles.Count; i++) region.Tiles[i].Rivers = previousRivers[i];
                return "Could not generate rivers: " + exception.Message;
            }
            _districtWorldCompositionKey = "";
            var scroll = _root.Q<ScrollView>("region-map-scroll");
            if (scroll != null) _regionMapScrollOffset = scroll.scrollOffset;
            RemoveDocumentModal();
            Show(AppScreen.RegionEditor);
            return null;
        }

        private void RegenerateRegionRivers()
        {
            if (_openRegion == null) return;
            var error = GenerateFreshRegionRivers(_openRegion, _openRegion.Terrain ?? new RegionTerrainSettings());
            if (error == null) return;
            var panel = CreateDocumentModal("RIVER LAYOUT", error);
            var actions = DocumentModalActions();
            actions.Add(CfButton.Create("TRY AGAIN", () => { RemoveDocumentModal(); RegenerateRegionRivers(); }, true, "primary"));
            actions.Add(CfButton.Create("CLOSE", RemoveDocumentModal, true, "quiet"));
            panel.Add(actions);
        }

        private static string FindRegionRiverBuildingConflict(RegionSaveData region, List<RegionRiverPath> paths)
        {
            foreach (var tile in region.Tiles)
            {
                if (tile.Lots == null || tile.Lots.Count == 0) continue;
                var sections = RegionRiverGenerator.Sections(tile, paths);
                foreach (var placed in tile.Lots)
                {
                    var lot = LotSaveStore.Read(placed.LotId);
                    if (lot == null) return tile.Name; // Cannot safely assess an unavailable building footprint.
                    var rotated = placed.RotationQuarterTurns % 2 != 0;
                    var width = (rotated ? lot.LotDepthCells : lot.LotWidthCells) * LotMetricScale.MajorGridMeters;
                    var depth = (rotated ? lot.LotWidthCells : lot.LotDepthCells) * LotMetricScale.MajorGridMeters;
                    foreach (var river in sections)
                    {
                        var margin = river.WidthMeters * .75f;
                        var rect = new Rect(placed.GridX * DistrictScale.CellSizeMeters + placed.ShoreOffsetX - margin,
                            placed.GridZ * DistrictScale.CellSizeMeters + placed.ShoreOffsetZ - margin, width + margin*2, depth + margin*2);
                        for (var i=1;i<river.Points.Count;i++)
                        {
                            var a = new Vector2(river.Points[i-1].X * DistrictScale.SizeMeters(tile.Width),river.Points[i-1].Z * DistrictScale.SizeMeters(tile.Height));
                            var b = new Vector2(river.Points[i].X * DistrictScale.SizeMeters(tile.Width),river.Points[i].Z * DistrictScale.SizeMeters(tile.Height));
                            if (RegionRiverGenerator.Clip(rect,ref a,ref b)) return tile.Name;
                        }
                    }
                }
            }
            return null;
        }

        private static void AddRegionWaterChoices(VisualElement parent, string id, string fewText, string manyText,
            RegionWaterAmount selected, Action<RegionWaterAmount> change)
        {
            var few = new Toggle(fewText) { name = id + "-few", value = selected == RegionWaterAmount.Few };
            var many = new Toggle(manyText) { name = id + "-many", value = selected == RegionWaterAmount.Many };
            foreach (var toggle in new[] { few, many })
            {
                toggle.style.fontSize = 28;
                toggle.style.flexDirection = FlexDirection.RowReverse;
                toggle.style.alignItems = Align.Center;
                toggle.labelElement.style.flexGrow = 1;
                toggle.labelElement.style.minWidth = 0;
                var input = toggle.Q<VisualElement>(className: "unity-base-field__input");
                if (input != null) { input.style.flexGrow = 0; input.style.flexShrink = 0; input.style.width = 44; }
                toggle.style.minHeight = 42;
                toggle.style.marginBottom = 6;
                // Runtime panels have no Editor theme supplying checkbox artwork.
                // Give each Toggle a real, visible check box and check glyph.
                var box = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
                if (box != null)
                {
                    box.style.width = 28; box.style.height = 28;
                    box.style.minWidth = 28; box.style.flexShrink = 0;
                    box.style.marginRight = 12;
                    box.style.backgroundColor = new Color(.08f,.14f,.19f);
                    box.style.borderTopWidth = box.style.borderBottomWidth = box.style.borderLeftWidth = box.style.borderRightWidth = 2;
                    box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = new Color(.8f,.7f,.4f);
                    var check = new Label(toggle.value ? "✓" : "") { name = "region-checkbox-glyph", pickingMode = PickingMode.Ignore };
                    check.style.color = new Color(.3f,1f,.55f); check.style.fontSize = 24;
                    check.style.unityTextAlign = TextAnchor.MiddleCenter;
                    box.Add(check);
                    toggle.RegisterValueChangedCallback(e => check.text = e.newValue ? "✓" : "");
                }
                parent.Add(toggle);
            }
            void RefreshChecks()
            {
                foreach (var toggle in new[] { few, many })
                {
                    var glyph = toggle.Q<Label>("region-checkbox-glyph");
                    if (glyph != null) glyph.text = toggle.value ? "✓" : "";
                }
            }
            few.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue) many.SetValueWithoutNotify(false);
                change(evt.newValue ? RegionWaterAmount.Few : many.value ? RegionWaterAmount.Many : RegionWaterAmount.None);
                RefreshChecks();
            });
            many.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue) few.SetValueWithoutNotify(false);
                change(evt.newValue ? RegionWaterAmount.Many : few.value ? RegionWaterAmount.Few : RegionWaterAmount.None);
                RefreshChecks();
            });
        }
    }
}
