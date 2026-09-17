using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        // Rebound only when composing a new district HUD; local palette/selection changes retain these widgets.
        readonly System.Collections.Generic.Dictionary<string, (Label value, Button button)> _mapMetrics = new();
        Label _mapSeasonLabel;
        bool _districtPaletteOpen;
        bool _districtPaletteCategoryOpen;

        void ToggleDistrictPalette(DistrictEditorMode mode)
        {
            var screen = _root?.Q(className: "district-terraform-screen");
            if (screen == null) return;
            bool open = !_districtPaletteOpen || _districtEditorMode != mode;
            ReturnToQuietDistrict();
            _districtEditorMode = mode;
            SelectDistrictCategory("Select");
            _districtPaletteOpen = open;
            RefreshDistrictPalette(screen, FindSelectedRegionTile());
        }

        void ReturnToQuietDistrict()
        {
            _districtPaletteOpen = false;
            _districtPaletteCategoryOpen = false;
            _districtInfoVisible = false;
            CancelBrickworksPlacement(); CancelIndustryPlacement();
            SelectDistrictCategory("Select");
            ClearSelectedObject();
            var screen = _root?.Q(className: "district-terraform-screen");
            if (screen != null) SetDistrictChromeVisibility(screen);
        }

        VisualElement MapHeader(string title)
        {
            var header = CfMapChrome.Panel("map-header", "cf-map-header");

            header.Add(StyledLabel(title, "cf-map-location"));
            return header;
        }

        void AddMapSave(VisualElement header, string name)
        {
            var save = CreateRegionSaveButton(name, "quiet");
            save.AddToClassList("cf-map-action");
            CfMapChrome.Illustrate(save, "Save");
            header.Add(save);
        }

        VisualElement ComposeRegionChrome(VisualElement screen)
        {
            _mapMetrics.Clear(); _mapSeasonLabel = null;
            screen.AddToClassList("cf-quiet-map");
            var header = MapHeader(_openRegion.Name);
            header.Add(CfMapChrome.Action("Statistics", "Statistics", () =>
            {
                if (FindSelectedRegionTile() != null) ComposeDistrictStats();
                else ShowMapNotice("DISTRICT STATISTICS", "Select a district on the map to inspect its statistics.");
            }, "region-statistics"));
            AddMapSave(header, "region-save-button");
            header.Add(CfMapChrome.Action("Menu", "Menu", () => ShowMapMenu(false), "region-back-button"));
            screen.Add(header);
            var rail = CfMapChrome.Panel("region-tool-rail", "cf-map-region-rail");
            foreach (var key in new[] { "Terrain", "Rivers", "Flora", "Climate", "Roads" })
            {
                var category = key;
                var button = CfMapChrome.Action(category, category,
                    () => ComposeRegionTerrainCategory(category == "Terrain" ? "Hills" : category),
                    "region-tool-" + key.ToLowerInvariant());
                button.AddToClassList("cf-map-rail-action"); rail.Add(button);
            }
            rail.style.display = DisplayStyle.None;
            screen.Add(rail);
            var dock = CfMapChrome.Panel("region-intent-dock", "cf-quiet-dock");
            dock.Add(CfMapChrome.Action("Enter selected district", "Build", () =>
            {
                var selected = FindSelectedRegionTile();
                if (selected != null) SelectRegionTile(selected.TileId);
                else ShowMapNotice("ENTER DISTRICT", "Select a district first.");
            }, "region-build"));
            dock.Add(CfMapChrome.Action("Terrain", "TerrainAction", () =>
                rail.style.display = rail.style.display.value == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None,
                "region-terrain"));
            screen.Add(dock);
            var layers = CfMapChrome.Panel("map-layer-dock", "cf-map-layers");
            AddRegionMapLayerMenu(screen, layers);
            var layerButton = layers.Q<Button>("region-map-layers-button");
            layerButton.style.minWidth = 42; layerButton.style.width = 42;
            layerButton.style.fontSize = 0; layerButton.Add(CfMapChrome.Icon("Region"));
            screen.Add(layers);
            return header;
        }

        void PreviewRegionTile(string id)
        {
            var screen = _root?.Q(className: "region-editor-screen");
            if (screen == null) return;
            // Change only the old/new selection and its inspector; preserve the map tree and scroll.
            screen.Q("region-tile-" + _selectedRegionTileId)?.RemoveFromClassList("region-city-tile--selected");
            _selectedRegionTileId = id;
            screen.Q("region-tile-" + id)?.AddToClassList("region-city-tile--selected");
            var rail = screen.Q("region-tool-rail");
            if (rail != null) rail.style.display = DisplayStyle.None;
            RefreshRegionInspector(screen);
        }

        void RefreshRegionInspector(VisualElement screen)
        {
            screen.Q("region-inspector")?.RemoveFromHierarchy();
            var district = FindSelectedRegionTile();
            if (district == null) return;
            var panel = CfMapChrome.Panel("region-inspector", "cf-map-inspector");
            var close = new Button(() => PreviewRegionTile("")) { text = "×", tooltip = "Clear selection", name = "quiet-region-close" };
            close.AddToClassList("cf-quiet-close"); panel.Add(close);
            panel.Add(StyledLabel("REGIONAL MAP", "cf-map-kicker"));
            panel.Add(CfMapChrome.Title(district?.Name ?? _openRegion.Name, "cf-map-heading"));
            var picture = CfMapChrome.Icon(district == null ? "Region" : "Terrain");
            picture.AddToClassList("cf-map-portrait"); panel.Add(picture);
            if (district == null)
                panel.Add(StyledLabel("Select a district to view its details and enter your settlement.", "cf-map-copy"));
            else
            {
                panel.Add(Property("STATUS", district.Founded ? "Founded" : "Not founded"));
                panel.Add(Property("CLIMATE", district.Climate.ToString()));
                panel.Add(Property("ERA", LotEraCatalog.DisplayName(_openRegion.EraId)));
                panel.Add(Property("SIZE", $"{district.Width} × {district.Height}"));
                panel.Add(StyledLabel("Build and manage this district.", "cf-map-copy"));
                var enter = CfMapChrome.Action("Enter District", "Region", () => SelectRegionTile(district.TileId), "region-enter-district");
                enter.AddToClassList("cf-map-primary"); panel.Add(enter);
                panel.Add(CfMapChrome.Action("District Statistics", "Statistics", ComposeDistrictStats));
            }
            screen.Add(panel);
        }

        void ComposeDistrictChrome(VisualElement screen, RegionCityTile district)
        {
            _mapMetrics.Clear();
            screen.AddToClassList("cf-quiet-map");
            var header = MapHeader(_openRegion.Name + " · " + district.Name);
            header.Add(CfMapChrome.Action("Statistics", "Statistics", ComposeDistrictStats, "district-statistics"));
            AddMapSave(header, "district-save-button");
            header.Add(CfMapChrome.Action("Menu", "Menu", () => ShowMapMenu(true), "district-menu"));
            screen.Add(header);
            var resources = CfMapChrome.Panel("map-resources", "cf-map-resources");
            void Metric(string label, string name, string value)
            {
                var item = new Button(label == "POPULATION" ? (System.Action)ComposeDistrictStats : ComposeDistrictResourcesModal);
                item.name = name + "-metric"; item.AddToClassList("cf-map-metric");
                item.Add(CfMapChrome.ResourceIcon(label));
                var amount = StyledLabel(value, "cf-map-value"); amount.name = name; amount.pickingMode = PickingMode.Ignore; item.Add(amount); resources.Add(item);
                _mapMetrics[name] = (amount, item);
            }
            Metric("TREASURY", "district-simulation-money", $"${district.Treasury:N0}");
            Metric("POPULATION", "map-population", DistrictLotSimulation.For(district).Population.Population.ToString("N0"));
            foreach (var resource in new[] { ("FOOD", 6), ("LUMBER", 0), ("STONE", 2), ("BRICK", 9) })
                Metric(resource.Item1, "map-stock-" + resource.Item2, ResourceAmount(district, resource.Item2));
            screen.Add(resources);
            var management = CfMapChrome.Panel("map-management", "cf-map-management");
            management.Add(CfMapChrome.Action("Industry", "Industry", ComposeDistrictIndustryModal, "district-industry-menu"));
            management.Add(CfMapChrome.Action("Labor", "Labor", ComposeDistrictLaborModal, "district-labor-menu"));
            management.Add(CfMapChrome.Action("Resources", "Resources", ComposeDistrictResourcesModal, "district-resource-bar"));
            screen.Add(management);
            var footer = CfMapChrome.Panel("map-time", "cf-map-time");
            var season = StyledLabel("", "cf-map-season"); season.name = "district-labor-season"; _mapSeasonLabel = season; footer.Add(season);
            var pause = new Button(() => SetDistrictSimulationPaused(true)) { text = "Pause", name = "district-simulation-pause" };
            pause.AddToClassList("district-time-button"); footer.Add(pause);
            var play = new Button(() => SetDistrictSimulationPaused(false)) { text = "Play", name = "district-simulation-go" };
            play.AddToClassList("district-time-button"); footer.Add(play);
            pause.SetEnabled(district.Founded); play.SetEnabled(district.Founded);
            pause.EnableInClassList("district-time-button--selected", _districtSimulationPaused);
            play.EnableInClassList("district-time-button--selected", !_districtSimulationPaused);
            screen.Add(footer);
            var back = CfMapChrome.Action("Region Map", "Region", LeaveDistrictEditor, "district-region-back");
            back.AddToClassList("cf-map-region-back"); back.AddToClassList("cf-map-chrome"); screen.Add(back);
            RefreshMapMetrics(screen, district);
        }

        void RefreshMapMetrics(VisualElement screen, RegionCityTile district)
        {
            // Fixed HUD counters; no enumeration of district contents.
            void Describe(string name, string title, string full, string compact)
            {
                if (!_mapMetrics.TryGetValue(name, out var metric)) return;
                metric.value.text = compact;
                metric.button.tooltip = title + ": " + full;
            }
            Describe("district-simulation-money", "TREASURY", $"${district.Treasury:N0}", $"{district.Treasury:N0}");
            var people = DistrictLotSimulation.For(district).Population.Population.ToString("N0");
            Describe("map-population", "POPULATION", people, people);
            for (int i = 0; i < MapStockIndices.Length; i++)
            {
                string amount = ResourceAmount(district, MapStockIndices[i]);
                string number = amount.EndsWith(" t") ? amount.Substring(0, amount.Length - 2) : amount;
                Describe("map-stock-" + MapStockIndices[i], MapStockNames[i], number + " tons", number);
            }
            var season = _mapSeasonLabel;
            if (season != null)
            {
                int index = DistrictLabor.State(district).SeasonIndex;
                season.text = district.Founded ? $"{DistrictLabor.SeasonName(index)} · Year {district.FoundingYear + index / 4}" : "Not founded";
            }
        }
        static readonly string[] MapStockNames = { "FOOD", "LUMBER", "STONE", "BRICK" };
        static readonly int[] MapStockIndices = { 6, 0, 2, 9 };

        void SetDistrictChromeVisibility(VisualElement screen)
        {
            var visible = _districtInterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var mode in new[] { DistrictEditorMode.Builder, DistrictEditorMode.Terraform })
                screen.Q("district-mode-" + mode.ToString().ToLowerInvariant())?.EnableInClassList("district-mode-button--selected", _districtPaletteOpen && _districtEditorMode == mode);
            foreach (var name in new[] { "map-header", "map-resources", "map-mode-switch", "selected-object-panel" })
            { var element = screen.Q(name); if (element != null) element.style.display = visible; }
            foreach (var name in new[] { "map-management", "map-time", "district-region-back", "district-info-toggle", "district-interface-toggle" })
            { var element = screen.Q(name); if (element != null) element.style.display = DisplayStyle.None; }
            var rail = screen.Q("map-tool-rail");
            if (rail != null) rail.style.display = _districtInterfaceVisible && _districtPaletteOpen ? DisplayStyle.Flex : DisplayStyle.None;
            var options = screen.Q("map-tool-options");
            if (options != null) options.style.display = _districtInterfaceVisible && _districtPaletteOpen && _districtPaletteCategoryOpen && !IsDistrictRoadToolActive() ? DisplayStyle.Flex : DisplayStyle.None;
            var info = screen.Q(className: "terraform-hud");
            if (info != null) info.style.display = _districtInterfaceVisible && _districtInfoVisible ? DisplayStyle.Flex : DisplayStyle.None;
            var toggle = screen.Q<Button>("district-info-toggle");
            if (toggle != null) toggle.style.display = DisplayStyle.None;
            bool inspecting = screen.Q("selected-object-panel") != null;
            screen.Query<VisualElement>(className: "district-simulation-panel").ForEach(element =>
                element.style.display = _districtInterfaceVisible && !inspecting && !_districtInfoVisible && (_districtPaletteOpen || !string.IsNullOrEmpty(_pendingDistrictLotId)) ? DisplayStyle.Flex : DisplayStyle.None);
            var hide = screen.Q<Button>("district-interface-toggle");
            if (hide != null) hide.text = _districtInterfaceVisible ? "Hide UI" : "Show UI";
        }

        void ShowMapNotice(string title, string copy)
        {
            var panel = CreateDocumentModal(title, copy);
            panel.Add(CfButton.Create("CLOSE", RemoveDocumentModal, true, "quiet"));
        }
        void ShowMapMenu(bool district)
        {
            var panel = CreateDocumentModal("CITY FORGE", district ? "District controls" : "Region controls");
            panel.Add(CfMapChrome.Action(district ? "Region Map" : "Main Menu", "Region", () =>
            { RemoveDocumentModal(); if (district) LeaveDistrictEditor(); else Show(AppScreen.MainMenu); }));
            if (district)
            {
                panel.Add(CfMapChrome.Action("Statistics", "Statistics", ComposeDistrictStats));
                panel.Add(CfMapChrome.Action("Industry", "Industry", ComposeDistrictIndustryModal, "quiet-industry"));
                panel.Add(CfMapChrome.Action("Labor", "Labor", ComposeDistrictLaborModal, "quiet-labor"));
                panel.Add(CfMapChrome.Action("Resources", "Resources", ComposeDistrictResourcesModal, "quiet-resources"));
                panel.Add(CfMapChrome.Action("District information", "Region", () =>
                {
                    RemoveDocumentModal(); ReturnToQuietDistrict(); _districtInfoVisible = true;
                    SetDistrictChromeVisibility(_root.Q(className: "district-terraform-screen"));
                }));
                var d = FindSelectedRegionTile();
                int season = DistrictLabor.State(d).SeasonIndex;
                panel.Add(StyledLabel(d.Founded ? $"{DistrictLabor.SeasonName(season)} · Year {d.FoundingYear + season / 4}" : "Not founded", "cf-map-copy"));
                var pause = CfButton.Create(_districtSimulationPaused ? "RESUME" : "PAUSE", () =>
                { SetDistrictSimulationPaused(!_districtSimulationPaused); RemoveDocumentModal(); }, d.Founded, "quiet");
                panel.Add(pause);
            }
            if (!district)
            {
                panel.Add(CfMapChrome.Action("All Terrain Options", "Terrain", ComposeRegionTerrainModal));
                if (_openRegionWasCreatedThisSession)
                    panel.Add(CfMapChrome.Action("Regenerate District Layout", "Terrain", () =>
                    {
                        RegionSaveStore.RegenerateTiles(_openRegion); _selectedRegionTileId = "";
                        _regionMapScrollOffset = Vector2.zero; _regionMapScrollInitialized = false;
                        RemoveDocumentModal(); Show(AppScreen.RegionEditor);
                    }));
                if (_openRegion.RiverSeed != 0)
                    panel.Add(CfMapChrome.Action("Regenerate Rivers", "Rivers", () => { RemoveDocumentModal(); RegenerateRegionRivers(); }));
            }
            panel.Add(CfButton.Create("CLOSE", RemoveDocumentModal, true, "quiet"));
        }
    }
}
