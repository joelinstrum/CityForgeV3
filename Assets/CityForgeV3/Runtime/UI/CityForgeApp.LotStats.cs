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
        private Button CreateLotStatsButton()
        {
            var button = new Button(OpenLotStats) { name = "lot-stats", tooltip = "Stats — people, benefits and seasonal finances" };
            button.AddToClassList("cf-image-button");
            button.AddToClassList("cf-image-button--tool-category");
            var chart = new VisualElement { pickingMode = PickingMode.Ignore };
            chart.style.flexDirection = FlexDirection.Row;
            chart.style.alignItems = Align.FlexEnd;
            chart.style.height = 26;
            chart.style.justifyContent = Justify.Center;
            foreach (int height in new[] { 10, 18, 26 })
            {
                var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.style.width = 6; bar.style.height = height; bar.style.marginRight = 3;
                bar.style.backgroundColor = new Color(.85f, .73f, .42f);
                chart.Add(bar);
            }
            button.Add(chart);
            var caption = new Label("STATS") { pickingMode = PickingMode.Ignore };
            caption.AddToClassList("tool-category-caption"); button.Add(caption);
            button.SetEnabled(_hasOpenLot);
            return button;
        }

        private void OpenLotStats()
        {
            if (!_hasOpenLot || _lotWorld == null) return;
            var lot = _lotWorld.Session.Data;
            var draft = lot.Stats?.Copy() ?? new LotStats();
            var rates = DistrictBusinessEconomy.Rates(lot)?.Copy() ?? new BusinessRates();
            var panel = CreateDocumentModal("LOT STATS", "People, benefits, and finances affect the district. Apply changes, then Save the Lot to keep them. Build requirements are in General.");
            panel.style.width = 760;
            var sections = DocumentModalActions(); panel.Add(sections);
            var applyChoices = new List<Action>();
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "lot-stats-scroll" }; scroll.style.height = 440; scroll.style.flexShrink = 0; scroll.style.overflow = Overflow.Hidden;
            scroll.contentViewport.style.overflow = Overflow.Hidden; scroll.contentViewport.style.height = 440; scroll.contentViewport.style.maxHeight = 440; scroll.contentViewport.style.flexShrink = 0; scroll.contentContainer.style.flexShrink = 0; scroll.contentContainer.style.flexGrow = 0; panel.Add(scroll);
            void Heading(string title) { var label = StyledLabel(title, "inspector-title"); label.style.marginTop = 16; label.style.flexShrink = 0; scroll.Add(label);
                sections.Add(CfButton.Create(title == "BENEFITS & SERVICES" ? "BENEFITS" : title == "SEASONAL FINANCES" ? "FINANCES" : title, () => scroll.ScrollTo(label), true, "quiet")); }
            void Number(string title, int value, Action<int> set,
                int maximum = int.MaxValue)
            {
                var field = new IntegerField(title)
                { value = Math.Clamp(value, 0, maximum), isDelayed = true };
                field.AddToClassList("document-field"); field.style.flexDirection = FlexDirection.Row; field.style.minHeight = 42; field.style.flexShrink = 0;
                field.Q(className: "unity-base-field__input").style.flexGrow = 1;
                field.RegisterValueChangedCallback(e => { int n = Math.Clamp(e.newValue, 0, maximum); field.SetValueWithoutNotify(n); set(n); });
                scroll.Add(field);
            }
            void Choice(string title, List<string> choices, string value, Action<string> set)
            {
                var field = new CityForgeChoiceField(_root, title, choices, Math.Max(0, choices.IndexOf(value)));
                field.style.flexShrink = 0; applyChoices.Add(() => set(field.value)); scroll.Add(field);
            }
            void Flag(string title, bool value, Action<bool> set)
            {
                var field = CfButton.Create((value ? "✓ " : "○ ") + title, null, true, "quiet");
                field.clicked += () => { value = !value; field.text = (value ? "✓ " : "○ ") + title; set(value); };
                field.style.minHeight = 38; field.style.width = Length.Percent(100); field.style.flexShrink = 0; scroll.Add(field);
            }
            Heading("PEOPLE");
            Number("Population added", draft.Residents,
                n => draft.Residents = n);
            Number("Jobs / employees", rates.Employees, n => rates.Employees = n);
            Number("Wage per job / season ($)", draft.SeasonalWagePerJob, n => draft.SeasonalWagePerJob = n);
            Heading("SEASONAL FINANCES");
            var net = new Label();
            void Net()
            {
                long value = (long)rates.SeasonalRevenue - rates.SeasonalCost;
                net.text = $"Net per season: {(value > 0 ? "+" : value < 0 ? "−" : "")}${Math.Abs(value):N0}";
                net.style.color = value > 0 ? new Color(.35f,.85f,.45f) : value < 0 ? new Color(1f,.4f,.4f) : Color.white;
            }
            Number("Tax / other revenue ($)", rates.SeasonalRevenue, n => { rates.SeasonalRevenue = n; Net(); });
            Number("Operating cost ($)", rates.SeasonalCost, n => { rates.SeasonalCost = n; Net(); }); Net(); scroll.Add(net);
            Heading("BENEFITS & SERVICES");
            draft.Benefits ??= new List<LotBenefit>();
            foreach (var id in new[] { "food", "lumber", "stone", "brick" })
            {
                var benefit = draft.Benefits.Find(x => x.ResourceId == id);
                if (benefit == null) { benefit = new LotBenefit { ResourceId = id }; draft.Benefits.Add(benefit); }
                var captured = benefit;
                Number(id.ToUpperInvariant() + " produced", benefit.Amount, n => captured.Amount = n);
                Choice(id + " timing", new List<string> { "Per season", "Per delivery" }, benefit.Timing, v => captured.Timing = v);
            }
            Choice("Service benefit", new List<string> { "None", "Education", "Health", "Recreation", "Culture" }, draft.Service, v => draft.Service = v);
            Number("Service capacity (people)", draft.ServiceCapacity, n => draft.ServiceCapacity = n);
            scroll.Add(StyledLabel("Population is added when this Lot is placed and removed when the Lot is deleted. Services and seasonal outputs also affect the district. Per-delivery outputs require a completed delivery; existing delivery yields are not credited twice. Build requirements are edited in General.", "inspector-note"));
            foreach (var child in scroll.contentContainer.Children()) child.style.flexShrink = 0;
            var actions = DocumentModalActions();
            actions.Add(CfButton.Create("APPLY", () => {
                foreach (var apply in applyChoices) apply();
                lot.Stats = draft; rates.Configured = true; lot.BusinessRates = rates;
                RemoveDocumentModal(); _lotStatus = "Lot stats updated — Save to keep changes"; Show(AppScreen.LotEditor);
            }, true, "primary"));
            actions.Add(CfButton.Create("CANCEL", RemoveDocumentModal, true, "quiet")); panel.Add(actions);
        }
    }
}
