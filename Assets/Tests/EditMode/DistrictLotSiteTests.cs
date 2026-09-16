using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class DistrictLotSiteTests
{
    [Test]
    public void DenseDistrictClearsOnlyFootprintAndKeepsIndexValid()
    {
        var district = new RegionCityTile { Width = 4, Height = 4 };
        var size = DistrictScale.SizeMeters(district.Width);
        for (int z = 0; z < 200; z++)
        for (int x = 0; x < 200; x++)
            district.Flora.Add(new PlacedDistrictFlora {
                InstanceId = $"{x}-{z}", FloraId = "cilician-fir",
                NormalizedX = .5f + (-995 + x * 10) / size,
                NormalizedZ = .5f + (-995 + z * 10) / size });
        var index = DistrictHarvestIndex.For(district);
        var watch = Stopwatch.StartNew();
        var removed = index.ClearFootprint(new Rect(-20, -10, 40, 20));
        watch.Stop();
        UnityEngine.Debug.Log($"Lot clearing: 40,000 trees, {removed.Count} removed, {watch.Elapsed.TotalMilliseconds:F3}ms");
        Assert.AreEqual(8, removed.Count);
        Assert.AreEqual(39992, district.Flora.Count);
        foreach (var id in removed) Assert.IsNull(index.Find(id));
        Assert.AreSame(index, DistrictHarvestIndex.For(district));
        Assert.IsEmpty(index.NearbyFlora(Vector2.zero, 10));
        // Swap removal must keep the moved tail's slot valid for the next edit.
        var tail = district.Flora[0];
        Assert.IsTrue(index.RemoveFlora(tail.InstanceId));
        Assert.IsFalse(index.RemoveFlora(tail.InstanceId));
        var restored = JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(district));
        Assert.AreEqual(district.Flora.Count, restored.Flora.Count);
        foreach (var id in removed) Assert.IsNull(DistrictHarvestIndex.For(restored).Find(id));
    }

    [Test]
    public void RectangularClearanceIncludesNonHarvestableFlora()
    {
        var d = new RegionCityTile { Width = 1, Height = 1 };
        var size = DistrictScale.SizeMeters(d.Width);
        void Add(string id, float x, float z) => d.Flora.Add(new PlacedDistrictFlora {
            InstanceId = id, FloraId = "fraser-fir-small", NormalizedX = .5f + x / size, NormalizedZ = .5f + z / size });
        Add("inside", 5, 15); Add("outside", 15, 5); Add("far", 100, 100);
        var index = DistrictHarvestIndex.For(d);
        CollectionAssert.AreEquivalent(new[] { "inside" }, index.ClearFootprint(new Rect(-10, -20, 20, 40)));
        Assert.NotNull(index.Find("outside")); Assert.NotNull(index.Find("far"));
        var added = new PlacedDistrictFlora { InstanceId = "added", FloraId = "cilician-fir" };
        d.Flora.Add(added); DistrictHarvestIndex.Changed(d, added);
        Assert.IsTrue(index.RemoveFlora("added"));
    }

    [Test]
    public void PlacingRotatedLotClearsDistrictFloraAndCompletesDirtSurface()
    {
        var owner = new GameObject("District placement fixture");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid()+".json");
        try
        {
            var data = new LotSaveData { LotId = "site-test-" + System.Guid.NewGuid().ToString("N"),
                Name = "Site fixture", LotWidthCells = 2, LotDepthCells = 4, LotSizeMeters = 40 };
            data.Buildings3D.Add(new PlacedBuilding3D { AssetId = "new-england-farmhouse-v02" });
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(data));
            _ = LotContentCatalog.All;
            typeof(LotContentCatalog).GetMethod("Add", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { new LotSaveSummary { LotId = data.LotId, Name = data.Name }, path, "", "test", false, false });
            var district = new RegionCityTile { Width = 1, Height = 1 };
            var placement = new PlacedDistrictLot { InstanceId = "placed", LotId = data.LotId, GridX = 32, GridZ = 32, RotationQuarterTurns = 1 };
            var center = DistrictWorldController.DistrictLotCenterMeters(district, placement, data);
            var size = DistrictScale.SizeMeters(district.Width);
            void Tree(string id, float x, float z) => district.Flora.Add(new PlacedDistrictFlora {
                InstanceId = id, FloraId = "cilician-fir", NormalizedX = .5f + (center.x+x)/size, NormalizedZ = .5f + (center.y+z)/size });
            Tree("inside", 15, 5); Tree("outside", 5, 15);
            var world = owner.AddComponent<DistrictWorldController>(); world.Build(district);
            district.Lots.Add(placement);
            Assert.IsTrue(world.AddPlacedLot(district, placement));
            CollectionAssert.AreEqual(new[] { "outside" }, district.Flora.Select(x => x.InstanceId));
            var site = owner.GetComponentInChildren<LotConstructionSite>();
            Assert.NotNull(site); Assert.IsTrue(site.SurfaceVisible);
            CaptureSite("construction", owner, center);
            foreach (var sequence in site.GetComponentsInChildren<BuildingConstructionSequence>())
                for (int stage = 0; stage < 20 && !sequence.IsComplete; stage++) sequence.AdvanceOneStageForQa();
            Assert.IsFalse(site.SurfaceVisible);
            CaptureSite("completed", owner, center);
            Assert.AreEqual(1, district.Flora.Count);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            System.IO.File.Delete(path);
            LotContentCatalog.InvalidateCache();
        }
    }

    static void CaptureSite(string stage, GameObject owner, Vector2 center)
    {
        if (!Application.isBatchMode) return;
        var camera = owner.GetComponentInChildren<Camera>();
        var target = new Vector3(center.x, 0, center.y);
        camera.transform.position = target + new Vector3(60, 55, -60);
        camera.transform.LookAt(target);
        camera.orthographicSize = 35;
        var output = new RenderTexture(800, 600, 24);
        var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = output; camera.Render(); RenderTexture.active = output;
            image.ReadPixels(new Rect(0, 0, 800, 600), 0, 0); image.Apply();
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cityforge-lot-site");
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, stage+".png"), image.EncodeToPNG());
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(output); Object.DestroyImmediate(image); }
    }

    [Test]
    public void DirtStaysUntilEveryBuildingFinishesThenDisappears()
    {
        var host = new GameObject("Construction site fixture");
        try
        {
            host.transform.rotation = Quaternion.Euler(0, 90, 0);
            var site = host.AddComponent<LotConstructionSite>();
            var sequences = new List<BuildingConstructionSequence>();
            for (int i = 0; i < 2; i++)
            {
                var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.transform.SetParent(host.transform, false);
                var sequence = building.AddComponent<BuildingConstructionSequence>();
                site.Track(sequence, 20, 40);
                sequence.Begin(building, 4, 4, 6, () => site.SequenceChanged(sequence));
                sequences.Add(sequence);
            }
            Assert.IsTrue(site.SurfaceVisible);
            var surface = host.transform.Find("Temporary Lot Construction Dirt");
            Assert.AreEqual(new Vector3(20, .02f, 40), surface.localScale);
            Assert.That(surface.GetComponent<Renderer>().bounds.size.x, Is.EqualTo(40).Within(.01));
            while (!sequences[0].IsComplete) sequences[0].AdvanceOneStageForQa();
            Assert.IsTrue(site.SurfaceVisible); Assert.AreEqual(1, site.PendingBuildings);
            while (!sequences[1].IsComplete) sequences[1].AdvanceOneStageForQa();
            Assert.IsFalse(site.SurfaceVisible); Assert.AreEqual(0, site.PendingBuildings);
            site.SequenceChanged(sequences[1]); Assert.AreEqual(0, site.PendingBuildings);
        }
        finally { Object.DestroyImmediate(host); }
    }
}
