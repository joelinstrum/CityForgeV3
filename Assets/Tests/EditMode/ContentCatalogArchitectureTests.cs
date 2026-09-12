using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class ContentCatalogArchitectureTests
    {
        [SetUp]
        public void ResetCatalogs()
        {
            BuildingContentCatalog.InvalidateCache();
            LotContentCatalog.InvalidateCache();
        }

        [Test]
        public void BuiltInBuildingManifestHasUniqueStableIdsAndResolvableAssets()
        {
            var entries = BuildingContentCatalog.All;
            Assert.That(entries, Is.Not.Empty);
            Assert.That(entries.Select(entry => entry.id).Distinct().Count(),
                Is.EqualTo(entries.Count));
            foreach (var entry in entries)
            {
                Assert.That(BuildingContentCatalog.LoadModel(entry), Is.Not.Null,
                    $"Model did not resolve for {entry.id}");
                Assert.That(BuildingContentCatalog.LoadThumbnail(entry), Is.Not.Null,
                    $"Thumbnail did not resolve for {entry.id}");
            }
        }

        [Test]
        public void EveryPlayerFacingBuildingIsPublishedToBothConsumers()
        {
            foreach (var entry in BuildingContentCatalog.All)
            {
                if (entry.hideFromLotEditor || entry.hideFromDistrictBuilder) continue;
                var category = BuildingContentCatalog.Category(entry);
                Assert.That(BuildingContentCatalog.ForLotEditor(category),
                    Does.Contain(entry));
                Assert.That(BuildingContentCatalog.ForDistrictBuilder(category),
                    Does.Contain(entry));
            }
        }

        [Test]
        public void OldChurchUsesOneManifestContractInBothEditors()
        {
            var church = BuildingContentCatalog.Find("old-church-eval-v01");
            Assert.That(church, Is.Not.Null);
            Assert.That(church.category, Is.EqualTo("Civics"));
            Assert.That(church.subcategory, Is.EqualTo("Culture"));
            Assert.That(church.normalizeAxis, Is.EqualTo("height"));
            Assert.That(church.normalizeMeters, Is.EqualTo(25f));
            Assert.That(church.materialMode, Is.EqualTo("pbr"));
            Assert.That(BuildingContentCatalog.ForLotEditor(
                BuildingUseCategory.Civics), Does.Contain(church));
            Assert.That(BuildingContentCatalog.ForDistrictBuilder(
                BuildingUseCategory.Civics), Does.Contain(church));
        }

        [Test]
        public void BuildingModManifestContractIsDataOnly()
        {
            const string json = "{\"schema\":\"cityforge-building-content-v1\"," +
                "\"buildings\":[{\"id\":\"example.mod.house\"," +
                "\"displayName\":\"Example House\",\"category\":\"Residential\"," +
                "\"provider\":\"asset-bundle\",\"bundlePath\":\"content.bundle\"," +
                "\"modelAssetName\":\"Assets/Example.prefab\"}]}";
            var manifest = JsonUtility.FromJson<BuildingContentManifest>(json);
            Assert.That(manifest.schema, Is.EqualTo("cityforge-building-content-v1"));
            Assert.That(manifest.buildings.Single().id, Is.EqualTo("example.mod.house"));
            Assert.That(manifest.buildings.Single().provider,
                Is.EqualTo("asset-bundle"));
        }

        [Test]
        public void BuildingEconomyContractIsOptionalAndDataOnly()
        {
            const string json = "{\"schema\":\"cityforge-building-content-v1\"," +
                "\"buildings\":[{\"id\":\"example.mod.sawmill\"," +
                "\"displayName\":\"Sawmill\",\"category\":\"Industrial\"," +
                "\"plopCost\":125,\"constructionRequirements\":[" +
                "{\"resourceId\":\"lumber\",\"amount\":8}]," +
                "\"effects\":[{\"effectId\":\"lumber-production\"," +
                "\"magnitude\":2}]}]}";
            var entry = JsonUtility.FromJson<BuildingContentManifest>(json)
                .buildings.Single();
            Assert.That(entry.plopCost, Is.EqualTo(125));
            Assert.That(entry.constructionRequirements.Single().resourceId,
                Is.EqualTo("lumber"));
            Assert.That(entry.effects.Single().effectId,
                Is.EqualTo("lumber-production"));
        }

        [Test]
        public void LotPlopCostAggregatesBaseAndPlacedBuildingCosts()
        {
            var entry = BuildingContentCatalog.All.First();
            var original = entry.plopCost;
            try
            {
                entry.plopCost = 275;
                var lot = new LotSaveData { BasePlopCost = 50 };
                lot.Buildings3D.Add(new PlacedBuilding3D { AssetId = entry.id });
                lot.Buildings3D.Add(new PlacedBuilding3D { AssetId = entry.id });
                Assert.That(LotEconomy.CalculatePlopCost(lot), Is.EqualTo(600));
            }
            finally
            {
                entry.plopCost = original;
            }
        }

        [Test]
        public void LotModManifestReferencesComposedLotFilesByStableId()
        {
            const string json = "{\"schema\":\"cityforge-lot-content-v1\"," +
                "\"lots\":[{\"id\":\"example.mod.farm\"," +
                "\"lotFile\":\"lots/farm.json\",\"previewFile\":\"lots/farm.png\"}]}";
            var manifest = JsonUtility.FromJson<LotModManifest>(json);
            Assert.That(manifest.lots.Single().id, Is.EqualTo("example.mod.farm"));
            Assert.That(manifest.lots.Single().lotFile,
                Is.EqualTo("lots/farm.json"));
        }
    }
}
