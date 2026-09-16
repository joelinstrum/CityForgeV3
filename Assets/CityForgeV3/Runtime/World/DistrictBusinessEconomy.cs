using System;
using System.Linq;

namespace CityForgeV3.World
{
    // Shared, optional rates for present and future business lot content.
    [Serializable]
    public sealed class BusinessRates
    {
        public bool Configured;
        public int SeasonalCost;
        public int SeasonalRevenue;
        public int Employees;
        public BusinessRates Copy() => new() { Configured = Configured, SeasonalCost = SeasonalCost, SeasonalRevenue = SeasonalRevenue, Employees = Employees };
    }

    public static class DistrictBusinessEconomy
    {
        public static BusinessRates ProductionRates => new() { Configured = true, SeasonalCost = 1500, Employees = 10 };
        public static bool IsBusiness(LotSaveData lot) => lot != null &&
            (lot.LotType == LotType.Industrial || lot.LotType == LotType.Commercial || lot.LotType == LotType.Mixed);
        public static BusinessRates Rates(LotSaveData lot)
        {
            if (lot == null) return null;
            if (lot.BusinessRates?.Configured == true) return lot.BusinessRates;
            return lot.Buildings3D?.Any(b => b.AssetId == "lumber-mill-v01") == true ? ProductionRates : null;
        }
        public static string Describe(BusinessRates rates) => rates == null
            ? "Seasonal Cost: Not configured\nSeasonal Revenue: Not configured\nEmployees: Not configured"
            : $"Seasonal Cost: ${Math.Max(0, rates.SeasonalCost):N0}\nSeasonal Revenue: ${Math.Max(0, rates.SeasonalRevenue):N0}\nEmployees: {Math.Max(0, rates.Employees):N0}";

        // Settle at season boundaries, not on load or selection. Content is read only
        // once per boundary, never per simulation tick. Existing saves start now.
        public static bool SettleSeason(RegionCityTile district, Func<string, LotSaveData> readLot = null)
        {
            int season = DistrictLabor.State(district).SeasonIndex;
            if (!district.BusinessEconomyInitialized)
            {
                district.BusinessEconomyInitialized = true;
                district.BusinessSettledSeason = season;
                return true;
            }
            if (district.BusinessSettledSeason >= season) return false;
            readLot ??= LotContentCatalog.Read;
            long balance = 0;
            void Add(BusinessRates rates)
            {
                if (rates != null) balance += (long)Math.Max(0, rates.SeasonalRevenue) - Math.Max(0, rates.SeasonalCost);
            }
            foreach (var lot in district.Lots) Add(Rates(readLot(lot.LotId)));
            foreach (var works in district.Brickworks) Add(ProductionRates);
            // Retained employees incur costs even when a building is paused.
            // A negative treasury preserves the expense instead of silently waiving it.
            district.Treasury = (int)Math.Clamp((long)district.Treasury + balance, int.MinValue, int.MaxValue);
            district.BusinessSettledSeason = season;
            return true;
        }
    }
}
