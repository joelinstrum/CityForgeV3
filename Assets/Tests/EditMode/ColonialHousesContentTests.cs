using System.Reflection;
using System.Linq;
using CityForgeV3.Buildings3D;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class ColonialHousesContentTests
    {
        [Test]
        public void OrthographicFarViewUsesOneCameraFacingAcrossTheLot()
        {
            var cameraObject = new GameObject("Orthographic far camera");
            var left = new GameObject("Left house");
            var right = new GameObject("Right house");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                cameraObject.transform.position = new Vector3(10f, 15f, 10f);
                cameraObject.transform.LookAt(Vector3.zero);
                left.transform.position = new Vector3(-20f, 0f, 0f);
                right.transform.position = new Vector3(20f, 0f, 0f);
                var first = left.AddComponent<FarZoomBuildingBillboard>();
                var second = right.AddComponent<FarZoomBuildingBillboard>();
                const string views =
                    "CityForgeV3/Buildings3D/ColonialHouseA/FarViews";
                Assert.That(first.Configure(views, 50f, 0f, camera), Is.True);
                Assert.That(second.Configure(views, 50f, 0f, camera), Is.True);
                first.SetFar(true);
                second.SetFar(true);
                var angle = typeof(FarZoomBuildingBillboard).GetField(
                    "activeAngle", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(angle.GetValue(first), Is.EqualTo(angle.GetValue(second)));
            }
            finally
            {
                Object.DestroyImmediate(left);
                Object.DestroyImmediate(right);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [TestCase("colonial-eave-front-house-v01")]
        [TestCase("colonial-gable-front-house-v01")]
        public void HousePlacesInResidentialLotWithNearModelAndFarViews(string id)
        {
            BuildingContentCatalog.InvalidateCache();
            var entry = BuildingContentCatalog.Find(id);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.category, Is.EqualTo("Residential"));
            Assert.That(entry.subcategory, Is.EqualTo("Colonial"));
            Assert.That(entry.repaintable, Is.True);
            Assert.That(BuildingContentCatalog.ForLotEditor(
                BuildingUseCategory.Residential, null, "Colonial")
                .Contains(entry), Is.True);
            Assert.That(BuildingContentCatalog.ForDistrictBuilder(
                BuildingUseCategory.Residential).Contains(entry), Is.True);
            Assert.That(BuildingContentCatalog.LoadThumbnail(entry), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                entry.textureRoot + "_basecolor"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                entry.textureRoot + "_normal"), Is.Not.Null);
            Assert.That(entry.farBillboardPixelsPerMeter, Is.EqualTo(50f));
            var model = BuildingContentCatalog.LoadModel(entry);
            Assert.That(model, Is.Not.Null);
            Assert.That(model.GetComponentsInChildren<MeshFilter>(true)
                .Sum(filter => filter.sharedMesh?.vertexCount ?? 0),
                Is.GreaterThan(7000));

            var owner = new GameObject(id + " placement test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.NewEmptyLot("Colonial houses", LotType.Residential, 4, 4);
                Assert.That(world.AddExperimentalBuilding3D(id, 0f, 0f, 0),
                    Is.True);
                Assert.That(world.ExperimentalBuilding3DCount, Is.EqualTo(1));
                var far = owner.GetComponentInChildren<FarZoomBuildingBillboard>(true);
                Assert.That(far, Is.Not.Null);
                Assert.That(far.IsFar, Is.False,
                    "The editable lot should use the real-time model.");
                far.SetFar(true);
                Assert.That(far.IsFar, Is.True);
                Assert.That(far.transform.Find("Far Building Billboard")
                    ?.GetComponent<Renderer>()?.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void RepaintingOneHouseUpdatesOnlyThatInstanceAndSurvivesReload()
        {
            var owner = new GameObject("Repainted houses");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.NewEmptyLot("House paint", LotType.Residential, 4, 4);
                Assert.That(world.AddExperimentalBuilding3D(
                    "colonial-eave-front-house-v01", -6f, 0f, 0), Is.True);
                Assert.That(world.AddExperimentalBuilding3D(
                    "colonial-eave-front-house-v01", 6f, 0f, 0), Is.True);
                Assert.That(world.SelectBuilding3DForQa(0), Is.True);
                var fullUiRefreshRequests = 0;
                world.StateChanged += () => fullUiRefreshRequests++;
                Assert.That(world.SetSelectedBuildingPaint("#789DB8"), Is.True);
                Assert.That(fullUiRefreshRequests, Is.Zero,
                    "Repainting should update the selected house and controls locally.");
                Assert.That(world.Session.Data.Buildings3D[0].PaintHex,
                    Is.EqualTo("#789DB8"));
                Assert.That(world.Session.Data.Buildings3D[1].PaintHex,
                    Is.Empty);
                Assert.That(world.SetSelectedBuildingPaint("invalid"), Is.False);

                var roots = owner.transform.Cast<Transform>().Where(child =>
                    child.GetComponent<FarZoomBuildingBillboard>() != null)
                    .ToArray();
                Assert.That(roots.Length, Is.EqualTo(2));
                var block = new MaterialPropertyBlock();
                var first = roots[0].GetComponentsInChildren<Renderer>(true)
                    .First(renderer => renderer.sharedMaterial?.shader?.name ==
                        "CityForgeV3/Experimental3DBuildingPBR");
                var second = roots[1].GetComponentsInChildren<Renderer>(true)
                    .First(renderer => renderer.sharedMaterial?.shader?.name ==
                        "CityForgeV3/Experimental3DBuildingPBR");
                first.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_PaintEnabled"), Is.EqualTo(1f));
                second.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_PaintEnabled"), Is.EqualTo(0f));
                var far = roots[0].GetComponent<FarZoomBuildingBillboard>();
                far.SetFar(true);
                var distantRenderer = far.transform.Find("Far Building Billboard")
                    .GetComponent<Renderer>();
                Assert.That(distantRenderer.sharedMaterial.shader.name,
                    Does.StartWith("Unlit/"),
                    "Zoom 4 and 5 retain the source directional artwork.");
                distantRenderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_PaintEnabled"), Is.Zero);

                var saved = JsonUtility.FromJson<LotSaveData>(
                    world.Session.Serialize());
                world.LoadRuntimeLot(saved);
                Assert.That(world.Session.Data.Buildings3D[0].PaintHex,
                    Is.EqualTo("#789DB8"));
                Assert.That(world.Session.Data.Buildings3D[1].PaintHex,
                    Is.Empty);
                Assert.That(world.SelectBuilding3DForQa(0), Is.True);
                Assert.That(world.SelectedBuildingPaintHex,
                    Is.EqualTo("#789DB8"));
                Assert.That(world.SetSelectedBuildingPaint(""), Is.True);
                Assert.That(world.Session.Data.Buildings3D[0].PaintHex,
                    Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
