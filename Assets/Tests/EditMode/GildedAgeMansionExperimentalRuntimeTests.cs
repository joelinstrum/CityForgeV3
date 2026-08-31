using CityForgeV3.Buildings3D;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class GildedAgeMansionExperimentalRuntimeTests
    {
        [Test]
        public void ExperimentalPrefabPreservesAuthoredNightLightingContract()
        {
            var prefab = Resources.Load<GameObject>(
                LotWorldController.GildedAgeMansionExperimentalResource);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.name, Is.EqualTo("GildedAgeMansionExpV01"));
            Assert.That(prefab.GetComponent<Building3DPackageInstance>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<BuildingNightLighting>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Area"), Is.Not.Null);
            Assert.That(prefab.transform.Find("CF_WindowLight_Front_05"), Is.Not.Null);
            Assert.That(prefab.transform.Find("CF_WindowLight_BaySide_05"), Is.Not.Null);
            Assert.That(prefab.transform.Find("CF_ExteriorLamp_01"), Is.Not.Null);
            Assert.That(prefab.transform.Find("CF_ExteriorLamp_02"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Representations/LOD0"), Is.Not.Null,
                "The saved prefab must include its generated visual representation.");
        }

        [Test]
        public void ExperimentalBuildingCanBePlacedAndSurvivesDayNightSwitch()
        {
            var root = new GameObject("Gilded mansion experimental test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.NewEmptyLot("Exp mansion", LotType.Residential, 6, 6);
                Assert.That(world.AddExperimentalBuilding3D(
                    LotWorldController.GildedAgeMansionExperimentalId), Is.True);
                Assert.That(world.ExperimentalBuilding3DCount, Is.EqualTo(1));
                world.SetTimeOfDay(TimeOfDayPreset.Noon);
                world.SetTimeOfDay(TimeOfDayPreset.Night);
                Assert.That(world.ExperimentalBuilding3DCount, Is.EqualTo(1));
                var lightingControllers =
                    root.GetComponentsInChildren<BuildingNightLighting>(true);
                Assert.That(lightingControllers, Is.Not.Empty);
                var bestBoundCount = 0;
                var activeLightCount = 0;
                foreach (var lighting in lightingControllers)
                {
                    bestBoundCount = Mathf.Max(bestBoundCount,
                        lighting.BoundRendererCount);
                    activeLightCount += lighting.ActiveRuntimeLightCount;
                }
                Assert.That(bestBoundCount, Is.GreaterThan(0),
                    "Night emission must bind after runtime LOD renderers are created.");
                Assert.That(activeLightCount, Is.GreaterThan(0),
                    "Night must enable the transported window and entrance lights.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DeleteSelectedBuilding3DRemovesOnlyTheSelectedPackage()
        {
            var root = new GameObject("3D building delete test");
            try
            {
                var world = root.AddComponent<LotWorldController>();
                world.NewEmptyLot("Delete 3D", LotType.Residential, 6, 6);
                Assert.That(world.AddExperimentalBuilding3D(
                    LotWorldController.GildedAgeMansionExperimentalId), Is.True);
                Assert.That(world.AddExperimentalBuilding3D(
                    LotWorldController.PlymouthStoreProductionId), Is.True);
                Assert.That(world.SelectedBuilding3DIndex, Is.EqualTo(1));

                Assert.That(world.DeleteSelectedBuilding3D(), Is.True);
                Assert.That(world.ExperimentalBuilding3DCount, Is.EqualTo(1));
                Assert.That(world.SelectedBuilding3DIndex, Is.EqualTo(-1));
                Assert.That(world.DeleteSelectedBuilding3D(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
