using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class NewEnglandFarmhouseContentTests
{
    const string Id = "new-england-farmhouse-v02";
    [TestCase("founders", true)]
    [TestCase("industrial", true)]
    [TestCase("discovery", false)]
    [TestCase("modern", false)]
    public void EraAvailabilityAppliesToBothCatalogViews(string era, bool expected)
    {
        BuildingContentCatalog.InvalidateCache();
        Assert.AreEqual(expected, BuildingContentCatalog.ForLotEditor(BuildingUseCategory.Residential, era).Any(x => x.id == Id));
        Assert.AreEqual(expected, BuildingContentCatalog.ForDistrictBuilder(BuildingUseCategory.Residential, era).Any(x => x.id == Id));
    }

    [Test]
    public void ExistingEntriesWithoutEraRestrictionsRemainAvailable()
    {
        var legacy = new BuildingContentEntry();
        foreach (var era in LotEraCatalog.Ids) Assert.IsTrue(BuildingContentCatalog.IsAvailableInEra(legacy, era));
        legacy.eraIds = new string[0];
        foreach (var era in LotEraCatalog.Ids) Assert.IsTrue(BuildingContentCatalog.IsAvailableInEra(legacy, era));
    }

    [Test]
    public void ModelAndThumbnailResolveWithAuthoredTextures()
    {
        var entry = BuildingContentCatalog.Find(Id);
        Assert.NotNull(entry);
        Assert.NotNull(BuildingContentCatalog.LoadThumbnail(entry));
        var model = BuildingContentCatalog.LoadModel(entry);
        Assert.NotNull(model);
        var meshes = model.GetComponentsInChildren<MeshFilter>(true);
        Assert.Greater(meshes.Sum(x => (int)x.sharedMesh.GetIndexCount(0) / 3), 20000);
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        foreach (var material in renderer.sharedMaterials)
        {
            Assert.NotNull(material.mainTexture);
            Assert.NotNull(material.GetTexture("_BumpMap"));
        }
    }

    [Test]
    public void PlacementUsesSharedNormalizationAndPreservesCamera()
    {
        var root = new GameObject("New farmhouse placement fixture");
        try
        {
            var world = root.AddComponent<LotWorldController>();
            world.Build();
            world.ConfigureLot("Farmhouse fixture", LotType.Residential, 6, 6);
            var before = world.CaptureCameraFraming();
            Assert.IsTrue(world.AddExperimentalBuilding3D(Id, 0, 0, 0));
            var after = world.CaptureCameraFraming();
            Assert.That(Quaternion.Angle(before.Rotation, after.Rotation), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(before.Position, after.Position), Is.LessThan(.001f));
            var visual = root.GetComponentsInChildren<Transform>().Single(x => x.name == "3D Building — New England Farmhouse");
            var renderers = visual.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Assert.That(bounds.size.y, Is.EqualTo(9f).Within(.02f));
            Assert.That(bounds.min.y, Is.EqualTo(0f).Within(.03f));
        }
        finally { Object.DestroyImmediate(root); }
    }
}
