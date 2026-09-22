using System;
using System.IO;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.Tests
{
    public sealed class MapChromeTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Call(CityForgeApp app, string method, params object[] args) => typeof(CityForgeApp).GetMethod(method, Private).Invoke(app, args);
        static void Set(CityForgeApp app, string field, object value) => typeof(CityForgeApp).GetField(field, Private).SetValue(app, value);
        static GameObject Fixture(out CityForgeApp app, out VisualElement root, out RegionSaveData region)
        {
            var go = new GameObject("Map chrome test"); go.SetActive(false);
            app = go.AddComponent<CityForgeApp>(); root = new VisualElement();
            region = new RegionSaveData { Name = "UI test", Width = 4, Height = 4 };
            region.Tiles.Add(new RegionCityTile { TileId = "a", Name = "A", Width = 2, Height = 4 });
            region.Tiles.Add(new RegionCityTile { TileId = "b", Name = "B", X = 2, Width = 2, Height = 4 });
            Set(app, "_root", root); Set(app, "_openRegion", region);
            return go;
        }
        [Test] public void SelectingRegionUpdatesOnlyInspectorAndTwoTileStates()
        {
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                Call(app, "ComposeRegionEditor");
                var screen = root.Q(className: "region-editor-screen");
                var map = root.Q("region-map"); var header = root.Q("map-header");
                Assert.That(screen.IndexOf(root.Q("region-tool-rail")), Is.GreaterThan(screen.IndexOf(root.Q("region-map-scroll"))));
                Call(app, "PreviewRegionTile", "a"); var inspector = root.Q("region-inspector");
                Assert.That(root.Q("region-tile-a").ClassListContains("region-city-tile--selected"));
                Call(app, "PreviewRegionTile", "b");
                Assert.That(root.Q(className: "region-editor-screen"), Is.SameAs(screen));
                Assert.That(root.Q("region-map"), Is.SameAs(map)); Assert.That(root.Q("map-header"), Is.SameAs(header));
                Assert.That(root.Q("region-inspector"), Is.Not.SameAs(inspector));
                Assert.That(root.Q("region-tile-a").ClassListContains("region-city-tile--selected"), Is.False);
                Assert.That(root.Q("region-tile-b").ClassListContains("region-city-tile--selected"));
                Assert.That(root.Q<Button>("region-enter-district"), Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void RegionTileClickEntersDistrictWithoutRedundantDockAction()
        {
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                Call(app, "ComposeRegionEditor");
                Assert.That(root.Q<Button>("region-build"), Is.Null);
                var tile = root.Q<Button>("region-tile-a");
                Assert.That(tile, Is.Not.Null);
                typeof(Clickable).GetMethod("SimulateSingleClick", Private)
                    .Invoke(tile.clickable, new object[] { null, 0 });
                Assert.That(root.Q(className: "district-terraform-screen"),
                    Is.Not.Null);
                Assert.That(typeof(CityForgeApp)
                    .GetField("_selectedRegionTileId", Private).GetValue(app),
                    Is.EqualTo("a"));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void PaletteSwitchPreservesHeaderAndWorldViewport()
        {
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                var screen = new VisualElement(); screen.AddToClassList("district-terraform-screen"); root.Add(screen);
                var viewport = new VisualElement { name = "test-viewport" }; screen.Add(viewport);
                var header = new VisualElement { name = "map-header" }; screen.Add(header);
                Call(app, "RefreshDistrictPalette", screen, region.Tiles[0]);
                var rail = screen.Q("map-tool-rail");
                var flora = screen.Q<Button>("district-category-flora");
                Call(app, "SelectDistrictCategory", "Flora");
                Call(app, "RefreshDistrictPalette", screen, region.Tiles[0]);
                Assert.That(root.Q("map-header"), Is.SameAs(header)); Assert.That(root.Q("test-viewport"), Is.SameAs(viewport));
                Assert.That(screen.Q("map-tool-rail"), Is.Not.SameAs(rail));
                Assert.That(screen.Q<Button>("district-tool-forest"), Is.Not.Null);
                Assert.That(screen.Q<Button>("district-category-flora").Q<Image>(), Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void QuietDistrictStartsClosedAndToggleDoesNotReplaceViewport()
        {
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                Set(app, "_selectedRegionTileId", "a");
                Set(app, "_districtInterfaceVisible", true);
                var screen = new VisualElement(); screen.AddToClassList("district-terraform-screen"); root.Add(screen);
                var viewport = new VisualElement { name = "test-viewport" }; screen.Add(viewport);
                Call(app, "ComposeDistrictChrome", screen, region.Tiles[0]);
                Call(app, "RefreshDistrictPalette", screen, region.Tiles[0]);
                var header = screen.Q("map-header");
                Assert.That(screen.Q("map-tool-rail").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(screen.Q("map-tool-options").style.display.value, Is.EqualTo(DisplayStyle.None));
                var modeField = typeof(CityForgeApp).GetField("_districtEditorMode", Private);
                var builder = Enum.Parse(modeField.FieldType, "Builder");
                Call(app, "ToggleDistrictPalette", builder);
                Assert.That(screen.Q("map-tool-rail").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(screen.Q("map-tool-options").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(typeof(CityForgeApp).GetProperty("ActiveDistrictCategory", Private).GetValue(app), Is.EqualTo("Select"));
                Call(app, "SelectDistrictCategory", "Roads");
                Set(app, "_pendingDistrictLotId", "pending-test-lot");
                Call(app, "ReturnToQuietDistrict");
                Assert.That(screen.Q("map-tool-rail").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(typeof(CityForgeApp).GetField("_pendingDistrictLotId", Private).GetValue(app), Is.EqualTo(""));
                Assert.That(typeof(CityForgeApp).GetProperty("ActiveDistrictCategory", Private).GetValue(app), Is.EqualTo("Select"));
                Assert.That(screen.Q("map-header"), Is.SameAs(header));
                Assert.That(screen.Q("test-viewport"), Is.SameAs(viewport));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void ResourceCountersKeepNumbersCompactAndTooltipsCurrent()
        {
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                var screen = new VisualElement(); root.Add(screen);
                var district = region.Tiles[0];
                district.ResourceInventory = new DistrictResourceInventory { Stone = 3 };
                Call(app, "ComposeDistrictChrome", screen, district);
                var metric = screen.Q<Button>("map-stock-2-metric");
                Assert.That(screen.Q<Label>("map-stock-2").text, Is.EqualTo("3"));
                Assert.That(metric.tooltip, Is.EqualTo("STONE: 3 tons"));
                var icon = metric.Q<Image>();
                district.ResourceInventory.Stone = 12;
                Call(app, "RefreshMapMetrics", screen, district);
                Assert.That(screen.Q<Label>("map-stock-2").text, Is.EqualTo("12"));
                Assert.That(metric.tooltip, Is.EqualTo("STONE: 12 tons"));
                Assert.That(metric.Q<Image>(), Is.SameAs(icon));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void CalendarControlsAdvanceYearSeasonAndClock()
        {
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                var screen = new VisualElement(); root.Add(screen);
                var district = region.Tiles[0];
                district.Founded = true; district.FoundingYear = 1747;
                district.Labor = new DistrictLaborState { SeasonIndex = 2 };
                Set(app, "_selectedRegionTileId", district.TileId);
                Call(app, "ComposeDistrictChrome", screen, district);

                Assert.That(screen.Q<Button>("district-calendar-year").Q<Label>().text,
                    Is.EqualTo("Year 1747"));
                Assert.That(screen.Q<Label>("district-labor-season").text,
                    Is.EqualTo("Winter"));
                Assert.That(screen.Q<Button>("district-calendar-clock").Q<Label>().text,
                    Is.EqualTo("🕛"));

                void Click(string name) => typeof(Clickable)
                    .GetMethod("SimulateSingleClick", Private)
                    .Invoke(screen.Q<Button>(name).clickable,
                        new object[] { null, 0 });
                Click("district-calendar-season");
                Assert.That(district.Labor.SeasonIndex, Is.EqualTo(3));
                Assert.That(screen.Q<Button>("district-calendar-year").Q<Label>().text,
                    Is.EqualTo("Year 1748"));
                Assert.That(screen.Q<Label>("district-labor-season").text,
                    Is.EqualTo("Spring"));

                Click("district-calendar-clock");
                Assert.That(district.TimeOfDay,
                    Is.EqualTo(TimeOfDayPreset.Afternoon));
                Assert.That(screen.Q<Button>("district-calendar-clock").Q<Label>().text,
                    Is.EqualTo("🕔"));

                Click("district-calendar-year");
                Assert.That(district.Labor.SeasonIndex, Is.EqualTo(7));
                Assert.That(screen.Q<Button>("district-calendar-year").Q<Label>().text,
                    Is.EqualTo("Year 1749"));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void DistrictSaveShowsVisibleConfirmationNotice()
        {
            var folder = Path.Combine(Path.GetTempPath(),
                "cityforge-map-save-" + Guid.NewGuid().ToString("N"));
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                var screen = new VisualElement();
                screen.AddToClassList("district-terraform-screen");
                root.Add(screen);
                var district = region.Tiles[0];
                Set(app, "_selectedRegionTileId", district.TileId);
                Set(app, "_districtUndoQaSaveRoot", folder);
                Call(app, "ComposeDistrictChrome", screen, district);
                typeof(Clickable).GetMethod("SimulateSingleClick", Private)
                    .Invoke(screen.Q<Button>("district-save-button").clickable,
                        new object[] { null, 0 });
                Assert.That(screen.Q<Label>("district-notice").text,
                    Is.EqualTo("District Saved"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }
        [Test] public void RuntimeHoverHelpExpiresAfterThreeSeconds()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath,
                "CityForgeV3/Runtime/UI/CityForgeApp.RegionEditor.cs"));
            StringAssert.Contains("helpPanel.schedule.Execute", source);
            StringAssert.Contains("ExecuteLater(3000)", source);
            StringAssert.Contains("shownRevision == helpRevision", source);
        }
        [Test] public void QuietTerrainPaletteRetainsMergedWeatherCommands()
        {
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                var screen = new VisualElement(); screen.AddToClassList("district-terraform-screen"); root.Add(screen);
                Set(app, "_districtPaletteOpen", true); Set(app, "_districtPaletteCategoryOpen", true);
                Set(app, "_districtInterfaceVisible", true);
                Call(app, "SelectDistrictCategory", "Environment");
                Call(app, "RefreshDistrictPalette", screen, region.Tiles[0]);
                Assert.That(screen.Q<Button>("district-tool-rain").tooltip, Does.Contain("rain for ten seconds"));
                Assert.That(screen.Q<Button>("district-tool-snow").tooltip, Does.Contain("Temporary test weather"));
                Assert.That(screen.Q<Button>("district-tool-clear-skies").tooltip, Does.Contain("restore fair weather"));
                Assert.That(screen.Q("map-tool-options").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void RegionShortcutOpensRequestedCategory()
        {
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                foreach (var category in new[] { "Climate", "Flora", "Roads", "Hills" })
                {
                    Call(app, "ComposeRegionTerrainCategory", category);
                    Assert.That(root.Q("region-terrain-options").Q<Label>().text, Is.EqualTo(category.ToUpperInvariant()));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void RiverGenerationDefaultsAndEmptySelectionAreVisible()
        {
            var go = Fixture(out var app, out var root, out var region);
            try
            {
                Call(app, "RegenerateRegionRivers");
                Assert.That(root.Q("region-terrain-modal"), Is.Not.Null);
                Assert.That(root.Q<Toggle>("major-river-toggle").value, Is.True);
                Assert.That(ChoiceValue(root.Q("medium-river-count")), Is.EqualTo("None"));
                Assert.That(ChoiceValue(root.Q("small-river-count")), Is.EqualTo("2"));
                Assert.That(ChoiceValue(root.Q("stream-count")), Is.EqualTo("None"));
                var error = (string)Call(app, "GenerateFreshRegionRivers",
                    region, new RegionTerrainSettings { RiverCountsVersion = 1 });
                Assert.That(error, Does.Contain("Choose at least one"));
                Assert.That(region.RiverPaths, Is.Empty);
                var generate = root.Q<Button>("generate-region-rivers");
                typeof(Clickable).GetMethod("SimulateSingleClick", Private)
                    .Invoke(generate.clickable, new object[] { null, 0 });
                Assert.That(region.RiverPaths, Has.Count.EqualTo(3));
                Assert.That(root.Q("region-terrain-modal"), Is.Null);
                var repeat = root.Q<Button>("region-repeat-action");
                Assert.That(repeat, Is.Not.Null);
                Assert.That(repeat.Q<Label>("map-caption").text,
                    Is.EqualTo("Regenerate Rivers"));
                var previousSeed = region.RiverSeed;
                typeof(Clickable).GetMethod("SimulateSingleClick", Private)
                    .Invoke(repeat.clickable, new object[] { null, 0 });
                Assert.That(region.RiverSeed, Is.Not.EqualTo(previousSeed));
                Assert.That(root.Q<Button>("region-repeat-action"), Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        static string ChoiceValue(VisualElement element) =>
            (string)element.GetType().GetProperty("value").GetValue(element);
        [Test] public void CaptionUpdatesPreserveIconAndUseSeparateLabel()
        {
            var chrome = typeof(CityForgeApp).Assembly.GetType("CityForgeV3.UI.CfMapChrome");
            var button = (Button)chrome.GetMethod("Action", BindingFlags.Static|BindingFlags.NonPublic).Invoke(null, new object[] {"Save", "Save", (Action)(() => {}), "save"});
            var icon = button.Q<Image>();
            chrome.GetMethod("SetCaption", BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{button,"SAVED"});
            Assert.That(button.text, Is.Null.Or.Empty); Assert.That(button.Q<Label>("map-caption").text, Is.EqualTo("SAVED"));
            Assert.That(button.Q<Image>(), Is.SameAs(icon));
        }
    }
}
