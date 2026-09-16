using System;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        // Both scopes use the same choices and climate restrictions.
        private static void AddFloraCoverageChoices(VisualElement parent, string prefix, RegionClimate climate,
            RegionTreeCoverage selected, Action<RegionTreeCoverage> change)
        {
            bool enabled = RegionClimateRules.AllowsForest(climate);
            foreach (var coverage in new[] { RegionTreeCoverage.Sparse, RegionTreeCoverage.Wooded })
            {
                var choice = coverage;
                var button = CfButton.Create(coverage == RegionTreeCoverage.Sparse
                    ? "SPARSE — GOOD FOR AGRICULTURE" : "WOODED — GOOD FOR LUMBER",
                    () => change(choice), enabled, selected == coverage ? "primary" : "secondary");
                button.name = prefix + "-" + coverage.ToString().ToLowerInvariant();
                button.style.width = Length.Percent(100); button.style.whiteSpace = WhiteSpace.NoWrap; button.style.fontSize = 18;
                if (!enabled) button.tooltip = "Unavailable in Desert climate";
                parent.Add(button);
            }
        }

        private void AddDistrictFloraTabs(VisualElement panel, string selected)
        {
            var tabs = DocumentModalActions();
            foreach (var name in new[] { "COVERAGE", "TREES", "STONES" })
            {
                var tab = name;
                var button = CfButton.Create(name, () =>
                {
                    if (tab == "COVERAGE") { ComposeDistrictFloraCoverageModal(); return; }
                    SelectDistrictFloraLibrary(tab == "STONES"); ComposeDistrictFloraModal();
                }, true, selected == name ? "primary" : "secondary");
                button.name = "district-flora-tab-" + name.ToLowerInvariant();
                tabs.Add(button);
            }
            panel.Add(tabs);
        }

        private void ComposeDistrictFloraCoverageModal()
        {
            var region = _openRegion; var district = FindSelectedRegionTile();
            if (region == null || district == null) return;
            FinishDistrictFloraPaint(district); CancelDistrictSelectionPointer();
            EnsureDistrictUndo(district);
            var selected = district.TreeCoverage;
            var climate = CurrentRegionClimate;
            bool cancelled = false, busy = false;
            var panel = CreateDocumentModal("DISTRICT FLORA", "Generate tree coverage for " + district.Name + " only.");
            panel.name = "district-flora-coverage-modal";
            panel.style.width = 1080; panel.style.maxWidth = Length.Percent(94); panel.style.maxHeight = Length.Percent(90);
            AddDistrictFloraTabs(panel, "COVERAGE");
            var tabs = panel.Q<Button>("district-flora-tab-coverage").parent;
            var content = new ScrollView(); content.style.minHeight = 230; content.style.marginTop = 18; content.style.flexShrink = 1;
            panel.Add(content);
            var notice = StyledLabel("", "inspector-note"); notice.name = "district-flora-generation-notice";
            notice.style.display = DisplayStyle.None; panel.Add(notice);
            var actions = DocumentModalActions();
            var cancel = CfButton.Create("CANCEL", () => { cancelled = true; RemoveDocumentModal(); }, true, "quiet");
            cancel.name = "cancel-district-flora-coverage";
            Button generate = null;
            void SetBusy(bool value)
            {
                busy = value; tabs.SetEnabled(!value); content.SetEnabled(!value); generate.SetEnabled(!value);
                cancel.text = value ? "CANCEL GENERATION" : "CANCEL";
            }
            void Generate()
            {
                if (busy) return;
                SetBusy(true); notice.style.display = DisplayStyle.Flex; notice.text = "Generating tree coverage…";
                bool Current() => !cancelled && panel.panel != null && _openRegion == region && FindSelectedRegionTile() == district;
                panel.schedule.Execute(() =>
                {
                    if (!Current()) return;
                    try
                    {
                        var job = new RegionFloraGeneration(region, selected, RegionRiverGenerator.FreshSeed(district.FloraSeed), district);
                        job.Step(); notice.text = $"Ready to apply {job.TreeCount:N0} trees in {district.Name}…";
                        panel.schedule.Execute(() =>
                        {
                            if (!Current()) return;
                            try
                            {
                                // Apply in memory and retain undo; persistence is explicit.
                                job.Commit(_ => { });
                                _districtUndo.Commit(JsonUtility.ToJson(district));
                            }
                            catch (Exception e) { SetBusy(false); notice.text = "Could not apply tree coverage: " + e.Message; return; }
                            _districtSelection.Clear(); _selectedDistrictFloraInstanceId = ""; _activeDistrictRandomFloraGroupId = "";
                            _pendingDistrictFloraId = ""; _pendingDistrictFloraMode = 0;
                            _terraformCategory = "Flora"; _terraformTool = "Forest";
                            _districtWorld?.RefreshFlora(district);
                            _districtWorld?.ShowDistrictSelection(district, _districtSelection);
                            _districtWorldCompositionKey = DistrictCompositionKey(district);
                            RemoveDocumentModal(); Show(AppScreen.DistrictTerraform);
                            ShowDistrictNotice($"{selected} coverage generated for {district.Name}: {job.TreeCount:N0} trees. Undo restores the previous coverage.");
                        }).ExecuteLater(20);
                    }
                    catch (Exception e) { SetBusy(false); notice.text = "Could not generate tree coverage: " + e.Message; }
                }).ExecuteLater(1);
            }
            generate = CfButton.Create("GENERATE TREE COVERAGE", Generate, true, "primary");
            generate.name = "generate-district-flora"; generate.tooltip = "Choose Sparse or Wooded coverage";
            actions.Add(generate); actions.Add(cancel); panel.Add(actions);
            void RefreshChoices()
            {
                content.Clear();
                content.Add(StyledLabel("TREE COVERAGE", "document-modal-title"));
                content.Add(StyledLabel($"{district.Name} · Climate: {climate} (from region) · {district.Flora?.Count ?? 0:N0} existing trees", "document-modal-copy"));
                AddFloraCoverageChoices(content, "district-flora", climate, selected, value => { selected = value; RefreshChoices(); });
                bool enabled = RegionClimateRules.AllowsForest(climate);
                generate.SetEnabled(enabled && selected != RegionTreeCoverage.None);
                generate.tooltip = !enabled ? "Unavailable in Desert climate" : "Generate coverage in this district only";
                content.Add(StyledLabel(enabled
                    ? "Generate tree coverage in this district only. Sparse leaves open land; Wooded creates dense forest. Roads, water and buildings stay clear. Regeneration replaces generated standing trees; planted and harvested trees stay."
                    : "Sparse and Wooded are unavailable in Desert. Change the regional climate to generate tree coverage.", "document-modal-copy"));
                if (climate == RegionClimate.Tropical)
                    content.Add(StyledLabel("Tropical coverage uses tropical trees. Lumber crews currently harvest Cilician firs, available in Temperate and Mediterranean forests.", "inspector-note"));
            }
            RefreshChoices();
        }
    }
}
