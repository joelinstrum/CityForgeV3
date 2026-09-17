using System;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private Button CreateDistrictStatsButton()
        {
            var button = CfButton.Create("STATISTICS", ComposeDistrictStats, true, "quiet");
            button.name = "district-statistics";
            button.style.position = Position.Absolute; button.style.left = 710; button.style.top = 78;
            button.style.width = 190; button.style.height = 44; button.style.fontSize = 20; return button;
        }
        private void ComposeDistrictStats()
        {
            var d = FindSelectedRegionTile(); if (d == null) return;
            var sim = DistrictLotSimulation.For(d); var s = sim.Population;
            var panel = CreateDocumentModal("DISTRICT STATISTICS", d.Name + " · " + LotEraCatalog.DisplayName(_openRegion.EraId));
            panel.style.width = 1000; panel.style.maxWidth = Length.Percent(94);
            var tabs = DocumentModalActions(); panel.Add(tabs);
            var body = new VisualElement(); body.style.minHeight = 420; panel.Add(body);
            void Row(string key, string value)
            {
                var row = Property(key, value); row.style.minHeight = 44; row.style.flexShrink = 0;
                var name = row.Q<Label>(className: "property-key"); name.style.fontSize = 18; name.style.minWidth = 260;
                var data = row.Q<Label>(className: "property-value"); data.style.fontSize = 20; data.style.whiteSpace = WhiteSpace.Normal; data.style.flexGrow = 1;
                body.Add(row);
            }
            string PeopleValue(string value) => s.Population > 0 ? value : "No residents";
            void Population()
            {
                body.Clear();
                int season = DistrictLabor.State(d).SeasonIndex;
                Row("DISTRICT AGE", d.Founded ? $"{Math.Max(0,season-s.FoundedSeason)/4f:0.##} years · founded {d.FoundingYear}" : "Not founded");
                Row("POPULATION", $"{s.Population:N0} residents · {sim.Households:N0} households");
                Row("AVERAGE AGE", PeopleValue($"{sim.AverageAge:0.0} years"));
                Row("AGE GROUPS", $"0–17: {s.Children:N0} · 18–64: {s.WorkingAge:N0} · 65+: {s.Seniors:N0}");
                Row("JOBS / EMPLOYED", $"{sim.Jobs:N0} lot jobs · {sim.Employed:N0} residents employed");
                Row("HOUSEHOLD INCOME", PeopleValue($"${sim.HouseholdIncome:N0} per season · ${sim.HouseholdIncome*4:N0} per year"));
                Row("EDUCATION", PeopleValue($"{(s.Education<25?"Limited":s.Education<50?"Basic":s.Education<75?"Secondary":"Higher")} · {s.Education:0}/100"));
                Row("HAPPINESS", PeopleValue($"{s.Happiness:0}/100"));
                Row("HEALTH", PeopleValue($"{s.Health:0}/100"));
            }
            void Economy()
            {
                body.Clear(); Row("TREASURY", $"${d.Treasury:N0}");
                Row("LOT REVENUE", $"+${sim.Revenue:N0} per season");
                Row("LOT OPERATING COST", $"−${sim.Cost:N0} per season");
                long net = sim.Net;
                var label = StyledLabel($"LOT NET: {(net>0?"+":net<0?"−":"")}${Math.Abs(net):N0} per season", "inspector-title");
                label.style.color = net>0?new Color(.35f,.85f,.45f):net<0?new Color(1,.4f,.4f):Color.white; body.Add(label);
                Row("BRICKWORKS COST", $"${(long)(d.Brickworks?.Count ?? 0)*DistrictBusinessEconomy.ProductionRates.SeasonalCost:N0} per season");
                Row("AXEMEN WAGES", $"${(long)DistrictLabor.State(d).AssignedAxemen*DistrictLabor.Wage:N0} per season");
                Row("SCOPE", "Lot net excludes separately managed quarry and wildlife payroll.");
                Row("SEASONAL OUTPUT", $"Food {sim.SeasonalOutput(0):N0} t · Lumber {sim.SeasonalOutput(1):N0} t · Stone {sim.SeasonalOutput(2):N0} t · Bricks {sim.SeasonalOutput(3):N0} t");
            }
            void Services()
            {
                body.Clear(); Row("EDUCATION CAPACITY", $"{sim.EducationCapacity:N0} students / {s.Children:N0} children");
                Row("HEALTH CAPACITY", $"{sim.HealthCapacity:N0} people / {s.Population:N0} residents");
                Row("RECREATION / CULTURE", $"{sim.RecreationCapacity:N0} / {sim.CultureCapacity:N0} people");
                Row("LAST SEASON'S FOOD", $"{s.LastFoodConsumed:N0} / {s.LastFoodNeeded:N0} t consumed");
                Row("FOOD STOCK", $"{DistrictLotSimulation.ResourceAmount(d,0):N0} t");
                var explanation = StyledLabel("Education improves with school capacity. Food and healthcare support health. Employment, food, health and recreation support happiness. These scores change each season. One tonne of food supports 100 residents per season.", "document-modal-copy"); explanation.style.fontSize = 18; body.Add(explanation);
                body.Add(StyledLabel("Households average four residents. New residents arrive in a 20% child / 65% working-age / 15% senior mix; ages advance annually. Household income is modeled from filled lot jobs and their authored wages; wages are included in operating costs, not charged twice. Births, deaths and migration are not modeled yet.", "document-modal-copy"));
            }
            tabs.Add(CfButton.Create("PEOPLE", Population, true, "quiet"));
            tabs.Add(CfButton.Create("ECONOMY", Economy, true, "quiet"));
            tabs.Add(CfButton.Create("SERVICES", Services, true, "quiet")); Population();
            panel.Add(CfButton.Create("CLOSE", RemoveDocumentModal, true, "quiet"));
            panel.Query<Button>().ForEach(b => { b.style.fontSize = 20; b.style.minHeight = 44; });
            panel.Q<Label>(className: "document-modal-title").style.fontSize = 30;
            panel.Q<Label>(className: "document-modal-copy").style.fontSize = 18;
        }
    }
}
