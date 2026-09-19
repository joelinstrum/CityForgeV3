using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class DistrictPopulationState
    {
        public bool Initialized;
        public int LastSeason;
        public int HousingCapacity;
        public int[] Ages = new int[101];
        public int Population;
        public int Children, WorkingAge, Seniors;
        public double TotalAge;
        public float Education = 20, Happiness = 60, Health = 70;
        public int LastFoodNeeded, LastFoodConsumed;
        public float FoodCoverage = 1;
        public int FoundedSeason;
        public bool FoundingClockInitialized;
    }

    // Rebuilt only at district load/undo/bulk restoration. Ordinary placement and
    // removal update one profile and fixed-size aggregates, never the district list.
    public sealed class DistrictLotSimulation
    {
        static readonly ConditionalWeakTable<RegionCityTile, DistrictLotSimulation> Cache = new();
        readonly RegionCityTile district;
        readonly Dictionary<string, Profile> profiles = new();
        readonly Dictionary<string, HashSet<string>> byDefinition = new();
        readonly long[] seasonalOutput = new long[4];
        public long Revenue { get; private set; }
        public long Cost { get; private set; }
        public long Jobs { get; private set; }
        public long WageBudget { get; private set; }
        public long EducationCapacity { get; private set; }
        public long HealthCapacity { get; private set; }
        public long RecreationCapacity { get; private set; }
        public long CultureCapacity { get; private set; }
        public int LotCount => profiles.Count;
        public DistrictPopulationState Population => district.Population;
        public int Households => (int)(((long)Population.Population + 3) / 4);
        public long Employed => Math.Min(Population.WorkingAge, Jobs);
        public double HouseholdIncome => Households == 0 || Jobs == 0 ? 0 : (double)WageBudget / Jobs * Employed / Households;
        public double AverageAge => Population.Population == 0 ? 0 : Population.TotalAge / Population.Population;
        public long Net => Revenue - Cost;
        public long SeasonalOutput(int resource) => seasonalOutput[resource];
        sealed class Profile
        {
            public int Residents, Jobs, Cost, Revenue, Wage, Capacity;
            public bool HasPopulationOverride;
            public int PopulationOverride;
            public string Service, DefinitionId;
            public readonly long[] Seasonal = new long[4], Delivery = new long[4];
            public static Profile From(LotSaveData lot,
                bool hasPopulationOverride = false,
                int populationOverride = 0)
            {
                var rates = DistrictBusinessEconomy.Rates(lot);
                var stats = lot?.Stats;
                var p = new Profile { DefinitionId = lot?.LotId ?? "",
                    // Population is an authored Lot contribution rather than a
                    // category assumption. A fort, work camp, civic complex,
                    // or future custom Lot can therefore bring residents too.
                    Residents = Math.Max(0, hasPopulationOverride
                        ? populationOverride : stats?.Residents ?? 0),
                    HasPopulationOverride = hasPopulationOverride,
                    PopulationOverride = Math.Max(0, populationOverride),
                    Jobs = Math.Max(0, rates?.Employees ?? 0), Cost = Math.Max(0, rates?.SeasonalCost ?? 0), Revenue = Math.Max(0, rates?.SeasonalRevenue ?? 0),
                    Wage = Math.Max(0, stats?.SeasonalWagePerJob ?? 150), Capacity = Math.Max(0, stats?.ServiceCapacity ?? 0), Service = stats?.Service ?? "None"
                };
                if (stats?.Benefits != null) foreach (var benefit in stats.Benefits)
                {
                    if (benefit == null) continue;
                    int resource = ResourceIndex(benefit.ResourceId); if (resource < 0) continue;
                    var target = benefit.Timing == "Per delivery" ? p.Delivery : p.Seasonal;
                    target[resource] = Math.Min(int.MaxValue, target[resource] + Math.Max(0, benefit.Amount));
                }
                return p;
            }
        }
        DistrictLotSimulation(RegionCityTile d) { district = d; }
        public static DistrictLotSimulation For(RegionCityTile d, Func<string, LotSaveData> read = null)
        {
            if (Cache.TryGetValue(d, out var found)) return found;
            return Rebuild(d, read);
        }
        public static DistrictLotSimulation Rebuild(RegionCityTile d, Func<string, LotSaveData> read = null)
        {
            var state = d.Population ??= new DistrictPopulationState();
            if (state.Ages == null || state.Ages.Length != 101) state.Ages = new int[101];
            int previousCapacity = state.HousingCapacity;
            state.HousingCapacity = 0;
            var sim = new DistrictLotSimulation(d); read ??= LotContentCatalog.Read;
            if ((d.FounderBuildingId == "fortress" ||
                 d.FounderBuildingId == "city-charter-house") &&
                !string.IsNullOrEmpty(d.LotId))
            {
                // Compatibility for founder Lots placed before the explicit
                // zero-population override was stored on their placement.
                var founder = d.Lots?.Find(placed => placed != null &&
                    placed.LotId == d.LotId);
                if (founder != null && !founder.HasPopulationOverride)
                {
                    founder.HasPopulationOverride = true;
                    founder.PopulationOverride = 0;
                }
            }
            foreach (var lot in d.Lots ?? new List<PlacedDistrictLot>())
                if (lot != null)
                { if (string.IsNullOrEmpty(lot.InstanceId) || sim.profiles.ContainsKey(lot.InstanceId)) lot.InstanceId = Guid.NewGuid().ToString("N");
                  var p = Profile.From(read(lot.LotId), lot.HasPopulationOverride,
                      lot.PopulationOverride); p.DefinitionId = lot.LotId; sim.profiles.Add(lot.InstanceId, p); sim.Index(lot.InstanceId, p.DefinitionId); sim.Accumulate(p, 1); }
            int currentSeason = DistrictLabor.State(d).SeasonIndex;
            if (!state.Initialized) { state.Initialized = true; state.LastSeason = currentSeason; previousCapacity = 0; }
            if (d.Founded && !state.FoundingClockInitialized) { state.FoundedSeason = 0; state.FoundingClockInitialized = true; }
            sim.ChangePopulation(state.HousingCapacity - previousCapacity);
            sim.RefreshDemographicTotals();
            Cache.Remove(d); Cache.Add(d, sim); return sim;
        }
        public void Add(string instanceId, LotSaveData lot)
        {
            Add(instanceId, lot, false, 0);
        }
        public void Add(string instanceId, LotSaveData lot,
            bool hasPopulationOverride, int populationOverride)
        {
            if (profiles.ContainsKey(instanceId)) return;
            var p = Profile.From(lot, hasPopulationOverride,
                populationOverride); profiles.Add(instanceId, p); Index(instanceId, p.DefinitionId); Accumulate(p, 1); ChangePopulation(p.Residents);
        }
        public void Remove(string instanceId)
        {
            if (!profiles.TryGetValue(instanceId, out var p)) return;
            profiles.Remove(instanceId); if (byDefinition.TryGetValue(p.DefinitionId, out var ids)) ids.Remove(instanceId); Accumulate(p, -1); ChangePopulation(-p.Residents);
        }
        void Index(string instanceId, string definitionId)
        {
            if (!byDefinition.TryGetValue(definitionId, out var ids)) byDefinition[definitionId] = ids = new HashSet<string>();
            ids.Add(instanceId);
        }
        public static void SavedDefinitionChanged(RegionCityTile d, LotSaveData lot)
        {
            // Hidden/unloaded districts rebuild on entry. A small authoring edit
            // touches only placed instances of that definition in the live cache.
            if (d == null || lot == null || !Cache.TryGetValue(d, out var sim) || !sim.byDefinition.TryGetValue(lot.LotId, out var ids)) return;
            int capacityBefore = sim.Population.HousingCapacity;
            foreach (var id in ids)
            {
                sim.Accumulate(sim.profiles[id], -1);
                var old = sim.profiles[id];
                var profile = Profile.From(lot, old.HasPopulationOverride,
                    old.PopulationOverride); sim.profiles[id] = profile; sim.Accumulate(profile, 1);
            }
            sim.ChangePopulation(sim.Population.HousingCapacity - capacityBefore);
        }
        void Accumulate(Profile p, int sign)
        {
            Population.HousingCapacity = Clamp((long)Population.HousingCapacity + (long)p.Residents * sign);
            Jobs += (long)p.Jobs * sign; Revenue += (long)p.Revenue * sign; Cost += (long)p.Cost * sign;
            WageBudget += (long)p.Jobs * p.Wage * sign;
            if (p.Service == "Education") EducationCapacity += (long)p.Capacity * sign;
            if (p.Service == "Health") HealthCapacity += (long)p.Capacity * sign;
            if (p.Service == "Recreation") RecreationCapacity += (long)p.Capacity * sign;
            if (p.Service == "Culture") CultureCapacity += (long)p.Capacity * sign;
            for (int i = 0; i < 4; i++) seasonalOutput[i] += p.Seasonal[i] * sign;
        }
        void ChangePopulation(int delta)
        {
            var s = Population;
            if (delta > 0)
            {
                // Stable arrival mix: 20% children, 65% working-age, 15% seniors.
                int children = delta / 5, seniors = (int)((long)delta * 15 / 100);
                s.Ages[10] = Clamp((long)s.Ages[10] + children);
                s.Ages[35] = Clamp((long)s.Ages[35] + delta - children - seniors);
                s.Ages[70] = Clamp((long)s.Ages[70] + seniors);
            }
            else if (delta < 0)
            {
                long remaining = Math.Min(-(long)delta, s.Population), total = s.Population;
                for (int age = 0; age < 101; age++)
                {
                    int original = s.Ages[age];
                    int remove = total > 0 ? (int)Math.Min(original, remaining * original / total) : 0;
                    s.Ages[age] -= remove; remaining -= remove; total -= original;
                }
            }
            RefreshDemographicTotals();
        }
        void RefreshDemographicTotals()
        {
            var s = Population; long total = 0; s.Children = s.WorkingAge = s.Seniors = 0; s.TotalAge = 0;
            double fraction = (s.LastSeason % 4) * .25;
            for (int age = 0; age < 101; age++)
            {
                int n = Math.Max(0, s.Ages[age]); total += n; s.TotalAge += n * (age + fraction);
                if (age < 18) s.Children = Clamp((long)s.Children + n);
                else if (age < 65) s.WorkingAge = Clamp((long)s.WorkingAge + n);
                else s.Seniors = Clamp((long)s.Seniors + n);
            }
            s.Population = Clamp(total);
        }
        public bool AdvanceSeason(int season)
        {
            var s = Population;
            if (season <= s.LastSeason) return false;
            // Each tick crosses at most the fixed season clock's elapsed boundaries.
            while (s.LastSeason < season)
            {
                s.LastSeason++;
                if (s.LastSeason % 4 == 0)
                {
                    s.Ages[100] = Clamp((long)s.Ages[100] + s.Ages[99]);
                    for (int age = 99; age > 0; age--) s.Ages[age] = s.Ages[age - 1];
                    s.Ages[0] = 0;
                }
                for (int i = 0; i < 4; i++) AddResource(district, i, seasonalOutput[i]);
                RefreshDemographicTotals();
                s.LastFoodNeeded = (int)(((long)s.Population + 99) / 100); // 1 tonne / 100 residents / season.
                var inventory = district.ResourceInventory ??= new DistrictResourceInventory();
                s.LastFoodConsumed = Math.Min(Math.Max(0, inventory.Food), s.LastFoodNeeded); inventory.Food -= s.LastFoodConsumed;
                s.FoodCoverage = s.LastFoodNeeded == 0 ? 1 : (float)s.LastFoodConsumed / s.LastFoodNeeded;
                if (s.Population == 0) continue;
                float education = Mathf.Clamp01((float)EducationCapacity / Math.Max(1, s.Children));
                float health = Mathf.Clamp01((float)HealthCapacity / s.Population);
                float recreation = Mathf.Clamp01((float)(RecreationCapacity + CultureCapacity) / s.Population);
                float employment = s.WorkingAge == 0 ? 1 : Mathf.Clamp01((float)Jobs / s.WorkingAge);
                s.Education = Mathf.MoveTowards(s.Education, 20 + 80 * education, 5);
                s.Health = Mathf.MoveTowards(s.Health, 25 + 45 * s.FoodCoverage + 30 * health, 10);
                float target = 20 + 25 * s.FoodCoverage + 20 * employment + 20 * recreation + 15 * s.Health / 100;
                s.Happiness = Mathf.MoveTowards(s.Happiness, target, 10);
            }
            return true;
        }
        // Called only by a real completed-delivery event. Resource already credited
        // by that delivery is excluded, preserving existing lumber yield.
        public void DeliveryCompleted(string instanceId, string alreadyCreditedResource = null)
        {
            if (!profiles.TryGetValue(instanceId, out var p)) return;
            int existing = ResourceIndex(alreadyCreditedResource);
            for (int i = 0; i < 4; i++) if (i != existing) AddResource(district, i, p.Delivery[i]);
        }
        public static int ResourceIndex(string id) => id?.ToLowerInvariant() switch { "food" => 0, "wood" or "lumber" => 1, "stone" => 2, "brick" or "bricks" => 3, _ => -1 };
        public static int ResourceAmount(RegionCityTile d, int index)
        {
            var s = d.ResourceInventory ??= new DistrictResourceInventory();
            return index switch { 0 => s.Food, 1 => DistrictLabor.State(d).Wood, 2 => s.Stone, 3 => s.Bricks, _ => 0 };
        }
        public static void AddResource(RegionCityTile d, int index, long delta)
        {
            var s = d.ResourceInventory ??= new DistrictResourceInventory(); int amount = Clamp((long)ResourceAmount(d, index) + delta);
            switch (index) { case 0: s.Food = amount; break; case 1: DistrictLabor.State(d).Wood = amount; break; case 2: s.Stone = amount; break; case 3: s.Bricks = amount; break; }
        }
        public static int Clamp(long value) => (int)Math.Clamp(value, 0, int.MaxValue);
    }
}
