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

    [Test]
    public void ProfileHeightMovesOneBuildingAndItsShadowCopiesWithoutRebuild()
    {
        var owner = new GameObject("Profile height fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.ConfigureLot("Profile fixture", LotType.Residential, 6, 6);
            Assert.That(world.AddExperimentalBuilding3D(Id, -8, 0, 0), Is.True);
            Assert.That(world.AddExperimentalBuilding3D(Id, 8, 0, 0), Is.True);
            var buildings = owner.GetComponentsInChildren<Transform>()
                .Where(x => x.name == "3D Building — New England Farmhouse")
                .OrderBy(x => x.localPosition.x).ToArray();
            Assert.That(buildings, Has.Length.EqualTo(2));
            var firstHeight = buildings[0].localPosition.y;
            var secondHeight = buildings[1].localPosition.y;
            var secondRoot = buildings[1].gameObject;
            var secondShadow = owner.GetComponentsInChildren<Transform>()
                .First(x => x.name.StartsWith("3D Building Ground Shadow — ") &&
                    x.localPosition.x > 0f);
            var shadowHeight = secondShadow.localPosition.y;

            Assert.That(world.ToggleProfileView(), Is.True);
            Assert.That(world.ProfileViewEnabled, Is.True);
            Assert.That(world.AdjustSelectedBuilding3DElevation(-.5f), Is.True);
            Assert.That(buildings[0].localPosition.y,
                Is.EqualTo(firstHeight).Within(.001f));
            Assert.That(secondRoot.transform.localPosition.y,
                Is.EqualTo(secondHeight - .5f).Within(.001f));
            Assert.That(secondShadow.localPosition.y,
                Is.EqualTo(shadowHeight - .5f).Within(.001f));
            Assert.That(world.Session.Data.Buildings3D[1].ElevationOffsetMeters,
                Is.EqualTo(-.5f));
            Assert.That(world.Session.Serialize(), Does.Contain(
                "\"ElevationOffsetMeters\":-0.5"));
            world.TurnProfileView();
            Assert.That(world.ProfileAxisLabel, Is.EqualTo("NORTH–SOUTH"));
            world.ToggleTopDownView();
            Assert.That(world.ProfileViewEnabled, Is.False);
            Assert.That(world.TopDownViewEnabled, Is.True);
            Assert.That(world.CycleSelectedBuilding3D(1), Is.True);
            Assert.That(world.SelectedBuilding3DIndex, Is.EqualTo(0));
            Assert.That(world.CycleSelectedBuilding3D(1), Is.True);
            Assert.That(world.SelectedBuilding3DIndex, Is.EqualTo(1));
            world.LoadRuntimeLot(world.Session.Data.Copy());
            var reloaded = owner.GetComponentsInChildren<Transform>()
                .Where(x => x.name == "3D Building — New England Farmhouse")
                .OrderBy(x => x.localPosition.x).ToArray();
            Assert.That(reloaded, Has.Length.EqualTo(2));
            Assert.That(reloaded[1].localPosition.y,
                Is.EqualTo(secondHeight - .5f).Within(.001f));
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void NewLotExitsProfileAndRestoresNormalCamera()
    {
        var owner = new GameObject("Profile new-lot fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.ConfigureLot("Profile source", LotType.Residential, 6, 6);
            Assert.That(world.AddExperimentalBuilding3D(Id, 0, 0, 0), Is.True);
            Assert.That(world.ToggleProfileView(), Is.True);
            var profileCamera = world.CaptureCameraFraming();

            world.NewEmptyLot("Empty destination", LotType.Residential, 6, 6);

            Assert.That(world.ProfileViewEnabled, Is.False);
            Assert.That(world.SelectedBuilding3DIndex, Is.EqualTo(-1));
            var normalCamera = world.CaptureCameraFraming();
            Assert.That(Quaternion.Angle(profileCamera.Rotation,
                normalCamera.Rotation), Is.GreaterThan(1f));
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void ProfileShowsTerrainCrossSectionAndLabelsPreviewVersusRealWater()
    {
        var owner = new GameObject("Profile references fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.ConfigureLot("Profile references", LotType.Residential, 6, 6);
            Assert.That(world.AddExperimentalBuilding3D(Id, 0, 0, 0), Is.True);
            var heights = world.Session.Data.TerrainHeights;
            var width = world.Session.Data.TerrainGridWidth;
            for (var index = 0; index < heights.Count; index++)
                heights[index] = (index % width - width / 2) * .03f;
            world.ToggleProfileView();
            var terrain = owner.GetComponentsInChildren<LineRenderer>()
                .Single(x => x.name == "Profile terrain cross-section");
            Assert.That(terrain.positionCount, Is.EqualTo(41));
            Assert.That(terrain.gameObject.activeSelf, Is.True);
            Assert.That(terrain.GetPosition(0).y,
                Is.LessThan(terrain.GetPosition(40).y));
            Assert.That(world.ProfileWaterReferenceVisible, Is.False);
            world.ToggleProfileWaterReference();
            var water = owner.GetComponentsInChildren<LineRenderer>()
                .Single(x => x.name == "Profile waterline reference");
            Assert.That(world.ProfileWaterReferenceIsPreview, Is.True);
            Assert.That(water.GetPosition(0).y, Is.EqualTo(.5f).Within(.001f));
            world.AdjustProfilePreviewWaterLevel(.25f);
            Assert.That(water.GetPosition(0).y, Is.EqualTo(.75f).Within(.001f));

            world.Session.Data.WaterAreas.Add(new PlacedWaterArea
            {
                HeightMeters = .7f,
                Boundary = new System.Collections.Generic.List<WaterBoundaryPoint>
                {
                    new(-10f, -10f), new(10f, -10f),
                    new(10f, 10f), new(-10f, 10f)
                }
            });
            world.ToggleProfileView();
            world.ToggleProfileView();
            Assert.That(world.ProfileWaterReferenceIsPreview, Is.False);
            Assert.That(world.ProfileWaterLevel, Is.EqualTo(.7f));
            Assert.That(water.GetPosition(0).y, Is.EqualTo(.7f).Within(.001f));
            for (var index = 0; index < heights.Count; index++)
                heights[index] = 2f;
            Assert.That(world.AlignSelectedBuilding3DToGround(), Is.True);
            Assert.That(world.SelectedBuilding3DElevation, Is.EqualTo(2f));
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void ProfileDragChangesHeightRatherThanHorizontalPlacement()
    {
        var owner = new GameObject("Profile drag fixture");
        var target = new RenderTexture(1280, 720, 24);
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.ConfigureLot("Profile drag", LotType.Residential, 6, 6);
            Assert.That(world.AddExperimentalBuilding3D(Id, 4, -3, 0), Is.True);
            world.ToggleProfileView();
            var camera = owner.GetComponentInChildren<Camera>(true);
            camera.targetTexture = target;
            var building = owner.GetComponentsInChildren<Transform>()
                .Single(x => x.name == "3D Building — New England Farmhouse");
            var renderers = building.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            var pixel = camera.WorldToScreenPoint(bounds.center);
            var panel = new Vector2(pixel.x, 720f - pixel.y);
            var panelSize = new Vector2(1280f, 720f);
            Assert.That(world.BeginBuilding3DDragFromPanel(panel, panelSize),
                Is.True);
            Assert.That(world.DragBuilding3DFromPanel(
                panel + Vector2.down * 40f, panelSize), Is.True);
            Assert.That(world.SelectedBuilding3DElevation, Is.GreaterThan(0f));
            Assert.That(world.Session.Data.Buildings3D[0].X, Is.EqualTo(4f));
            Assert.That(world.Session.Data.Buildings3D[0].Z, Is.EqualTo(-3f));
            world.EndBuilding3DDrag();
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(target);
        }
    }
}
