using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class DistrictWildlifeSpatialTests
{
    [Test] public void SharedIndexIncludesNonHarvestableTreesWithoutGivingThemToLumberjacks()
    {
        var d = new RegionCityTile { Width = 1, Height = 1 };
        var tree = new PlacedDistrictFlora { InstanceId = "mountain", FloraId = "fraser-fir-small", NormalizedX = .5f, NormalizedZ = .5f };
        d.Flora.Add(tree); var index = DistrictHarvestIndex.For(d);
        Assert.AreSame(tree, index.NearbyFlora(Vector2.zero, 25).Single());
        Assert.IsEmpty(index.Nearby(Vector2.zero, 25));
        tree.NormalizedX = .9f; DistrictHarvestIndex.Changed(d, tree);
        Assert.IsEmpty(index.NearbyFlora(Vector2.zero, 25));
        d.Flora.Remove(tree); DistrictHarvestIndex.Removed(d, tree.InstanceId);
        Assert.IsNull(index.Find(tree.InstanceId));
    }
    [Test] public void WildlifeStillRequiresThreeStandingMountainTreesNearby()
    {
        var d = new RegionCityTile { Width = 1, Height = 1 };
        for (int i = 0; i < 3; i++) d.Flora.Add(new PlacedDistrictFlora
            { InstanceId = "tree-" + i, FloraId = "fraser-fir-small", NormalizedX = .5f + i * .001f, NormalizedZ = .5f });
        var wildlife = DistrictWildlife.State(d); wildlife.NextSighting = 0;
        Assert.True(DistrictWildlife.Tick(d, .1f, _ => true)); Assert.AreEqual(1, wildlife.Bears.Count);
        wildlife.Bears.Clear(); wildlife.NextSighting = 0;
        d.Flora[2].HarvestState = DistrictTreeHarvestState.Stump; DistrictHarvestIndex.Changed(d, d.Flora[2]);
        Assert.False(DistrictWildlife.Tick(d, .1f, _ => true)); Assert.IsEmpty(wildlife.Bears);
    }
}
