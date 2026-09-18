using System.Linq;
using CityForgeV3.Buildings3D;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class SmallSchool3DContentTests
    {
        [Test]
        public void SchoolPlacesInCivicsLotAsThreeDimensionalBuilding()
        {
            var owner = new GameObject("School placement test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.NewEmptyLot("School", LotType.Civics, 4, 4);
                Assert.That(world.AddExperimentalBuilding3D(
                    "small-schoolhouse-3d-v01", 0f, 0f, 0), Is.True);
                Assert.That(world.ExperimentalBuilding3DCount, Is.EqualTo(1));
                var far = owner.GetComponentInChildren<FarZoomBuildingBillboard>(true);
                Assert.That(far, Is.Not.Null);
                Assert.That(far.IsFar, Is.False,
                    "The editable lot must show its real-time model.");
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void SchoolUsesThreeDimensionalModelWithSharedFarViews()
        {
            BuildingContentCatalog.InvalidateCache();
            var entry = BuildingContentCatalog.Find("small-schoolhouse-3d-v01");
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.category, Is.EqualTo("Civics"));
            Assert.That(entry.subcategory, Is.EqualTo("Education"));
            Assert.That(BuildingContentCatalog.ForLotEditor(
                    BuildingUseCategory.Civics).Contains(entry), Is.True);
            Assert.That(BuildingContentCatalog.LotEditorSubcategories(
                    BuildingUseCategory.Civics).Contains("Education"), Is.True);
            Assert.That(BuildingContentCatalog.ForLotEditor(
                    BuildingUseCategory.Civics, null, "Education").Contains(entry),
                Is.True);
            Assert.That(BuildingContentCatalog.ForLotEditor(
                    BuildingUseCategory.Civics, null, "Culture").Contains(entry),
                Is.False);
            Assert.That(BuildingContentCatalog.LoadThumbnail(entry), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                entry.textureRoot + "_basecolor"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                entry.textureRoot + "_normal"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                entry.textureRoot + "_metallic"), Is.Not.Null);
            Assert.That(entry.farBillboardPixelsPerMeter, Is.EqualTo(50f));
            var model = BuildingContentCatalog.LoadModel(entry);
            Assert.That(model, Is.Not.Null);
            Assert.That(model.GetComponentsInChildren<MeshFilter>(true)
                .Sum(filter => filter.sharedMesh?.vertexCount ?? 0),
                Is.GreaterThan(8000));

            var cameraObject = new GameObject("School test camera");
            var instance = Object.Instantiate(model);
            try
            {
                cameraObject.transform.position = new Vector3(20, 15, 20);
                var camera = cameraObject.AddComponent<Camera>();
                var modelRenderer = instance.GetComponentInChildren<Renderer>();
                var billboard = instance.AddComponent<FarZoomBuildingBillboard>();
                Assert.That(billboard.Configure(entry.farBillboardResourceRoot,
                    entry.farBillboardPixelsPerMeter,
                    entry.farBillboardYawOffset, camera), Is.True);
                billboard.SetFar(false);
                Assert.That(modelRenderer.enabled, Is.True);
                billboard.SetFar(true);
                Assert.That(modelRenderer.enabled, Is.False);
                Assert.That(billboard.IsFar, Is.True);
                var farRenderer = instance.transform.Find("Far Building Billboard")
                    ?.GetComponent<Renderer>();
                Assert.That(farRenderer, Is.Not.Null);
                Assert.That(farRenderer.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
