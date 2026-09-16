using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public class DistrictBusinessEconomyTests
    {
        [Test] public void SeasonCostIsPaidOnceAfterReloadAndPausedBuildingsKeepEmployees()
        {
            var d = new RegionCityTile { Treasury = 5000 };
            d.Brickworks.Add(new DistrictBrickworksSite { Enabled = false });
            DistrictLabor.Tick(d, 1, (_, _) => null, _ => true);
            Assert.AreEqual(5000, d.Treasury, "Do not retroactively bill an existing save");
            d.Labor.SeasonSeconds = 599;
            DistrictLabor.Tick(d, 1, (_, _) => null, _ => true);
            Assert.AreEqual(3500, d.Treasury);
            d = JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));
            DistrictLabor.Tick(d, 1, (_, _) => null, _ => true);
            Assert.AreEqual(3500, d.Treasury);
            d.Brickworks.Clear();
            DistrictLabor.Tick(d, 600, (_, _) => null, _ => true);
            Assert.AreEqual(3500, d.Treasury, "Demolished buildings stop incurring cost");
        }
        [Test] public void LumberDefaultsAndGenericBusinessRatesSurviveCopiesAndReload()
        {
            var lot = new LotSaveData { LotType = LotType.Industrial };
            lot.Buildings3D.Add(new PlacedBuilding3D { AssetId = "lumber-mill-v01" });
            var rates = DistrictBusinessEconomy.Rates(lot);
            Assert.AreEqual(1500, rates.SeasonalCost); Assert.AreEqual(10, rates.Employees);
            Assert.AreEqual(0, rates.SeasonalRevenue);
            lot.BusinessRates = new BusinessRates { Configured = true, SeasonalCost = 250, SeasonalRevenue = 800, Employees = 3 };
            var copy = JsonUtility.FromJson<LotSaveData>(JsonUtility.ToJson(lot.Copy()));
            Assert.AreEqual(800, DistrictBusinessEconomy.Rates(copy).SeasonalRevenue);
            Assert.AreEqual(3, DistrictBusinessEconomy.Rates(copy).Employees);
            Assert.AreNotSame(lot.BusinessRates, lot.Copy().BusinessRates);
        }
        [Test] public void SharedSettlementAppliesLumberCostAndConfiguredBusinessRevenueWithoutPerTickReads()
        {
            var d = new RegionCityTile { Treasury = 1000 };
            d.Lots.Add(new PlacedDistrictLot { LotId = "mill" });
            d.Lots.Add(new PlacedDistrictLot { LotId = "shop" });
            var mill = new LotSaveData();
            mill.Buildings3D.Add(new PlacedBuilding3D { AssetId = "lumber-mill-v01" });
            var shop = new LotSaveData { LotType = LotType.Commercial,
                BusinessRates = new BusinessRates { Configured = true, SeasonalCost = 100, SeasonalRevenue = 300 } };
            int reads = 0;
            LotSaveData Read(string id) { reads++; return id == "mill" ? mill : shop; }
            DistrictBusinessEconomy.SettleSeason(d, Read);
            DistrictBusinessEconomy.SettleSeason(d, Read);
            Assert.AreEqual(0, reads);
            d.Labor.SeasonIndex++;
            DistrictBusinessEconomy.SettleSeason(d, Read);
            Assert.AreEqual(-300, d.Treasury, "Expenses remain accounted for even when cash runs out");
            Assert.AreEqual(2, reads);
            DistrictBusinessEconomy.SettleSeason(d, Read);
            Assert.AreEqual(2, reads); Assert.AreEqual(-300, d.Treasury);
        }
        [TestCase(LotType.Industrial)] [TestCase(LotType.Commercial)] [TestCase(LotType.Mixed)]
        public void UnconfiguredBusinessesDoNotInventFinancialValues(LotType type)
        {
            var lot = JsonUtility.FromJson<LotSaveData>(JsonUtility.ToJson(new LotSaveData { LotType = type }));
            Assert.True(DistrictBusinessEconomy.IsBusiness(lot));
            Assert.IsNull(DistrictBusinessEconomy.Rates(lot));
            StringAssert.Contains("Seasonal Revenue: Not configured", DistrictBusinessEconomy.Describe(null));
        }
    }
}
