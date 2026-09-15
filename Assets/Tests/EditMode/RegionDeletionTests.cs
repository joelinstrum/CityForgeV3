using System;
using System.IO;
using CityForgeV3.World;
using NUnit.Framework;

public class RegionDeletionTests
{
    string root;
    [SetUp] public void SetUp() => root = Path.Combine(Path.GetTempPath(), "region-delete-" + Guid.NewGuid().ToString("N"));
    [TearDown] public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    [Test] public void DeletesOnlyChosenRegionAndRemovesItFromBrowser()
    {
        var chosen = RegionSaveStore.Create("Duplicate name", 8, 8);
        var other = RegionSaveStore.Create("Duplicate name", 8, 8);
        RegionSaveStore.Save(chosen, root);
        var otherPath = RegionSaveStore.Save(other, root);
        var before = File.ReadAllBytes(otherPath);
        Assert.That(RegionSaveStore.Delete(chosen.RegionId, root), Is.True);
        Assert.That(RegionSaveStore.Load(chosen.RegionId, root), Is.Null);
        Assert.That(RegionSaveStore.List(root).Count, Is.EqualTo(1));
        Assert.That(RegionSaveStore.List(root)[0].RegionId, Is.EqualTo(other.RegionId));
        CollectionAssert.AreEqual(before, File.ReadAllBytes(otherPath));
        Assert.That(RegionSaveStore.Delete(chosen.RegionId, root), Is.False);
    }
    [Test] public void DeletingLastRegionLeavesEmptyBrowser()
    {
        var region = RegionSaveStore.Create("Last", 8, 8);
        RegionSaveStore.Save(region, root);
        RegionSaveStore.Delete(region.RegionId, root);
        Assert.That(RegionSaveStore.List(root), Is.Empty);
    }
    [TestCase("../outside")]
    [TestCase("/tmp/outside")]
    [TestCase("..\\outside")]
    [TestCase("")]
    [TestCase(null)]
    public void RejectsInvalidIds(string id)
    {
        Assert.Throws<ArgumentException>(() => RegionSaveStore.Delete(id, root));
    }
}
