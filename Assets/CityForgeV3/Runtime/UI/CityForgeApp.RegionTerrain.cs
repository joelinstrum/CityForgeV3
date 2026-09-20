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
        private void ComposeRegionTerrainModal() => ComposeRegionTerrainCategory("Rivers");

        private void ComposeRegionTerrainCategory(string initialCategory)
        {
            var region = _openRegion;
            if (region == null) return;
            var saved = region.Terrain ?? new RegionTerrainSettings();
            var draft = saved.Copy();
            draft.Flow = RegionRiverFlow.Varied;
            var category = initialCategory;
            bool busy = false, cancelled = false;
            var panel = CreateDocumentModal("REGION TERRAIN", "Choose terrain options for " + region.Name + ".");
            panel.name = "region-terrain-modal";
            panel.style.width = 1080;
            panel.style.maxWidth = Length.Percent(94);
            panel.style.maxHeight = Length.Percent(90);
            var tabs = new VisualElement();
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.style.flexWrap = Wrap.Wrap;
            panel.Add(tabs);
            var content = new ScrollView { name = "region-terrain-options" };
            content.style.minHeight = 230;
            content.style.flexShrink = 1;
            content.style.marginTop = 18;
            panel.Add(content);
            var notice = StyledLabel("", "inspector-note");
            notice.name = "region-terrain-notice";
            notice.style.display = DisplayStyle.None;
            panel.Add(notice);
            var drawingActions = new VisualElement();
            drawingActions.style.flexDirection = FlexDirection.Row;
            drawingActions.style.flexWrap = Wrap.Wrap;
            var major = CfButton.Create("CREATE MAJOR RIVER", () => BeginRegionRiver(RegionRiverSize.Major), true, "primary");
            major.name = "create-major-river"; drawingActions.Add(major);
            var large = CfButton.Create("CREATE LARGE RIVER", () => BeginRegionRiver(RegionRiverSize.Large), true, "primary");
            large.name = "create-large-river"; large.style.marginLeft = 12; drawingActions.Add(large);
            var small = CfButton.Create("CREATE SMALL RIVER", () => BeginRegionRiver(RegionRiverSize.Small), true, "primary");
            small.name = "create-small-river"; small.style.marginLeft = 12; drawingActions.Add(small);
            var remove = CfButton.Create("REMOVE RIVERS", () =>
            {
                notice.style.display = DisplayStyle.Flex;
                notice.text = RemoveRegionRivers(region) ?? "";
            }, true, "quiet");
            remove.name = "remove-region-rivers"; remove.style.marginLeft = 12; drawingActions.Add(remove);
            panel.Add(drawingActions);
            var actions = DocumentModalActions();
            Button save = null;
            var cancel = CfButton.Create("CANCEL", () => { cancelled = true; RemoveDocumentModal(); }, true, "quiet");
            cancel.name = "cancel-region-terrain-options";
            void Finish()
            {
                _districtWorldCompositionKey = "";
                var scroll = _root.Q<ScrollView>("region-map-scroll");
                if (scroll != null) _regionMapScrollOffset = scroll.scrollOffset;
                RemoveDocumentModal(); Show(AppScreen.RegionEditor);
            }
            void SetBusy(bool value)
            {
                busy = value; drawingActions.SetEnabled(!value); tabs.SetEnabled(!value); content.SetEnabled(!value); save.SetEnabled(!value);
                cancel.text = value ? "CANCEL GENERATION" : "CANCEL";
            }
            void GenerateFlora()
            {
                var seed = RegionRiverGenerator.FreshSeed(saved.FloraSeed);
                var generation = new RegionFloraGeneration(region, draft.TreeCoverage, seed,
                    familyMix: draft.ForestMix);
                SetBusy(true);
                void Step()
                {
                    if (cancelled || panel.panel == null || _openRegion != region) return;
                    try
                    {
                        generation.Step();
                        notice.text = $"Generating tree coverage… {generation.Completed}/{region.Tiles.Count} districts · {generation.TreeCount:N0} flora placements";
                        if (generation.Ready)
                        {
                            notice.text = $"Applying {generation.TreeCount:N0} flora placements…";
                            panel.schedule.Execute(() =>
                            {
                                if (cancelled || panel.panel == null || _openRegion != region) return;
                                try { generation.Commit(_ => { }); Finish(); }
                                catch (Exception e) { SetBusy(false); notice.text = "Could not apply tree coverage: " + e.Message; }
                            }).ExecuteLater(20);
                        }
                        else panel.schedule.Execute(Step).ExecuteLater(1);
                    }
                    catch (Exception e) { SetBusy(false); notice.text = "Could not generate tree coverage: " + e.Message; }
                }
                panel.schedule.Execute(Step).ExecuteLater(1);
            }
            save = CfButton.Create("GENERATE RIVERS", () =>
            {
                if (busy) return;
                notice.style.display = DisplayStyle.Flex;
                if (category == "Rivers")
                {
                    var rivers = saved.Copy(); rivers.DeepRivers = draft.DeepRivers; rivers.Streams = draft.Streams; rivers.Flow = draft.Flow;
                    notice.text = GenerateFreshRegionRivers(region, rivers) ?? "";
                }
                else if (category == "Flora")
                {
                    try { GenerateFlora(); } catch (Exception e) { notice.text = e.Message; }
                }
                else if (category == "Climate")
                {
                    var previous = region.Terrain; var modified = region.ModifiedUtc;
                    try
                    {
                        region.Terrain = saved.Copy(); region.Terrain.Climate = draft.Climate;
                        if (!RegionClimateRules.AllowsForest(draft.Climate)) region.Terrain.TreeCoverage = RegionTreeCoverage.None;
                        RegionClimateRules.Apply(region); Finish();
                    }
                    catch (Exception e)
                    {
                        region.Terrain = previous; region.ModifiedUtc = modified; RegionClimateRules.Apply(region);
                        notice.text = "Could not apply climate: " + e.Message;
                    }
                }
            }, true, "primary");
            actions.Add(save); actions.Add(cancel); panel.Add(actions);

            void SelectCategory(string selected)
            {
                category = selected; content.Clear(); notice.text = ""; notice.style.display = DisplayStyle.None;
                save.name = category == "Rivers" ? "generate-region-rivers" : category == "Flora" ? "generate-region-flora" : "save-region-climate";
                save.text = category == "Flora" ? "GENERATE TREE COVERAGE" : category == "Climate" ? "APPLY CLIMATE" : "GENERATE RIVERS";
                save.SetEnabled(category == "Rivers" || category == "Climate" || category == "Flora" && RegionClimateRules.AllowsForest(saved.Climate));
                drawingActions.style.display = category == "Rivers" ? DisplayStyle.Flex : DisplayStyle.None;
                save.style.display = category == "Rivers" || category == "Flora" || category == "Climate" ? DisplayStyle.Flex : DisplayStyle.None;
                foreach (var button in tabs.Query<Button>().ToList())
                {
                    bool active = button.text == category.ToUpperInvariant();
                    button.EnableInClassList("cf-button--primary", active);
                    button.EnableInClassList("cf-button--quiet", !active);
                }
                content.Add(StyledLabel(category.ToUpperInvariant(), "document-modal-title"));
                if (category == "Climate")
                {
                    foreach (RegionClimate climate in Enum.GetValues(typeof(RegionClimate)))
                    {
                        var choice = climate;
                        var button = CfButton.Create(climate.ToString().ToUpperInvariant(), () => { draft.Climate = choice; SelectCategory("Climate"); }, true,
                            draft.Climate == climate ? "primary" : "secondary");
                        button.style.width = Length.Percent(100); button.style.whiteSpace = WhiteSpace.NoWrap; button.style.fontSize = 18;
                        button.name = "region-climate-" + climate.ToString().ToLowerInvariant();
                        content.Add(button);
                    }
                    content.Add(StyledLabel(RegionClimateRules.Description(draft.Climate), "document-modal-copy"));
                    content.Add(StyledLabel("Apply the climate before generating tree coverage. Existing trees and buildings stay in place.", "inspector-note"));
                    return;
                }
                if (category == "Flora")
                {
                    content.Add(StyledLabel("Climate: " + saved.Climate + $" · {region.Tiles.Count} districts · {region.Tiles.Sum(tile => tile.Flora?.Count ?? 0):N0} existing flora placements", "document-modal-copy"));
                    bool enabled = RegionClimateRules.AllowsForest(saved.Climate);
                    AddFloraCoverageChoices(content, "region-flora", saved.Climate, draft.TreeCoverage,
                        value => { draft.TreeCoverage = value; SelectCategory("Flora"); });
                    AddForestFamilyMix(content, "region-forest-mix", draft.ForestMix,
                        () => save.SetEnabled(enabled && draft.TreeCoverage != RegionTreeCoverage.None && draft.ForestMix.Total > 0));
                    save.SetEnabled(enabled && draft.TreeCoverage != RegionTreeCoverage.None && draft.ForestMix.Total > 0);
                    content.Add(StyledLabel(enabled
                        ? "Generate tree coverage across all districts. The family percentages select each clump’s dominant family; deciduous and fir clumps contain one cross-family tree. Level ground uses broader nine-tree billboards and slopes use compact five-tree billboards. Separate harvestable firs preserve lumber-worker routing. Roads, water and buildings stay clear."
                        : "Tree coverage is unavailable in Desert. Choose and apply another climate to generate tree coverage.", "document-modal-copy"));
                    return;
                }
                if (category == "Roads")
                {
                    content.Add(StyledLabel("Draw a road across your region, then give it a name.", "document-modal-copy"));
                    var create = CfButton.Create("CREATE A NATIONAL PIKE", BeginNationalPike, true, "primary");
                    create.name = "create-national-pike"; content.Add(create); return;
                }
                if (category != "Rivers")
                {
                    content.Add(StyledLabel(category + " options will be added in a later pass.", "document-modal-copy")); return;
                }
                content.Add(StyledLabel("Choose one amount per type. Leave both unchecked for none.", "document-modal-copy"));
                AddRegionWaterChoices(content, "deep-rivers", "A few deep rivers", "Many deep rivers", draft.DeepRivers, value => draft.DeepRivers = value);
                AddRegionWaterChoices(content, "streams", "A few streams", "Many streams", draft.Streams, value => draft.Streams = value);
                content.Add(StyledLabel("Few: 2 deep rivers or 4 streams. Many: 5 deep rivers or 10 streams. Rivers and streams form connected branches with varied bends. Generation replaces this tool’s rivers; manually placed rivers stay. Each generation chooses a fresh placement.", "inspector-note"));
            }
            foreach (var name in new[] { "Rivers", "Flora", "Climate", "Shorefront", "Roads", "Mountains", "Hills" })
            {
                var captured = name;
                var button = CfButton.Create(name.ToUpperInvariant(), () => SelectCategory(captured), true, "quiet");
                button.name = "region-terrain-" + name.ToLowerInvariant();
                button.style.marginRight = 8; button.style.width = 174; button.style.fontSize = 18;
                button.style.whiteSpace = WhiteSpace.NoWrap; tabs.Add(button);
            }
            SelectCategory(initialCategory);
        }

        private string RemoveRegionRivers(RegionSaveData region)
        {
            var previousPaths = region.RiverPaths;
            var previousOverrides = region.Tiles.Select(tile => tile.RiversEditedLocally).ToList();
            var previousRivers = region.Tiles.Select(tile => tile.Rivers).ToList();
            try
            {
                region.RiverPaths = new List<RegionRiverPath>();
                foreach (var tile in region.Tiles)
                { tile.Rivers = new List<PlacedDistrictRiver>(); tile.RiversEditedLocally = false; }
            }
            catch (Exception exception)
            {
                region.RiverPaths = previousPaths;
                for (var i = 0; i < region.Tiles.Count; i++) { region.Tiles[i].Rivers = previousRivers[i]; region.Tiles[i].RiversEditedLocally = previousOverrides[i]; }
                return "Could not remove rivers: " + exception.Message;
            }
            _districtWorldCompositionKey = "";
            var scroll = _root.Q<ScrollView>("region-map-scroll");
            if (scroll != null) { _regionMapScrollOffset = scroll.scrollOffset; _regionMapScrollInitialized = true; }
            RemoveDocumentModal();
            Show(AppScreen.RegionEditor);
            return null;
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
                // Use fresh lists so a failed generation can restore the complete old layout.
                foreach (var tile in region.Tiles)
                    tile.Rivers = new List<PlacedDistrictRiver>(tile.Rivers ?? new List<PlacedDistrictRiver>());
                RegionRiverGenerator.Apply(region, paths);
                region.Terrain = draft;
                region.RiverSeed = newSeed;
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
            var saved = _openRegion.Terrain ?? new RegionTerrainSettings();
            var error = GenerateFreshRegionRivers(_openRegion, saved.Copy());
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
