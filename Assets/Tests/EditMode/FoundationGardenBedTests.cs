using System.Linq;
using System.Reflection;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class FoundationGardenBedTests
{
    private static readonly string[] Ids =
    {
        LotWorldController.FoundationRearHedgePropId,
        LotWorldController.FoundationFramedHedgePropId
    };
    private static readonly string[] CottageIds =
    {
        LotWorldController.FoundationOpenRosesPropId,
        LotWorldController.FoundationPicketRoseShrubsPropId,
        LotWorldController.FoundationPicketCottageFlowersPropId,
        LotWorldController.FoundationPicketRosePairPropId,
        LotWorldController.FoundationPicketRoundedShrubsPropId
    };

    [Test]
    public void BothHouseFrontBedsUseOneShallowFootprintAndSharedPlantingArt()
    {
        var owner = new GameObject("Foundation garden fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            var leaves = Resources.Load<Texture2D>(
                "CityForgeV3/Garden/GeorgianClippedHedgesV01/clipped-leaves");
            foreach (var id in Ids)
            {
                Assert.IsTrue(LotWorldController.IsGardenPropId(id));
                var root = world.CreatePropPresentation(id, id, 1f);
                try
                {
                    Assert.NotNull(root);
                    Assert.NotNull(root.GetComponent<FoundationGardenBed>());
                    Assert.NotNull(root.Find("Photographic planting plan"));
                    var hedge = root.Find(id == Ids[0] ?
                        "Rear clipped hedge" : "Framing clipped hedges");
                    Assert.NotNull(hedge);
                    Assert.That(hedge.GetComponent<MeshRenderer>()
                        .sharedMaterial.mainTexture, Is.SameAs(leaves));
                    var bounds = hedge.GetComponent<MeshFilter>().sharedMesh.bounds;
                    Assert.That(bounds.size.x, Is.LessThanOrEqualTo(2f));
                    Assert.That(bounds.size.z, Is.LessThanOrEqualTo(1f));
                    Assert.That(root.GetComponentsInChildren<SpriteRenderer>()
                        .Count(renderer => renderer.name.StartsWith(
                            "Photographic foliage")), Is.GreaterThanOrEqualTo(10));

                    var dimensions = typeof(LotWorldController).GetMethod(
                        "PropDimensions", BindingFlags.Static | BindingFlags.NonPublic);
                    Assert.NotNull(dimensions);
                    var straight = new object[] { id, 0, 0f, 0f };
                    dimensions.Invoke(null, straight);
                    Assert.That(straight[2], Is.EqualTo(2f));
                    Assert.That(straight[3], Is.EqualTo(1f));
                    var turned = new object[] { id, 1, 0f, 0f };
                    dimensions.Invoke(null, turned);
                    Assert.That(turned[2], Is.EqualTo(1f));
                    Assert.That(turned[3], Is.EqualTo(2f));

                    root.GetComponent<FoundationGardenBed>()
                        .SetSeason(SeasonPreset.Winter);
                    Assert.IsFalse(root.Find("Photographic planting plan")
                        .GetComponent<SpriteRenderer>().enabled);
                    Assert.IsTrue(root.GetComponentsInChildren<SpriteRenderer>(true)
                        .Where(renderer => renderer.transform.parent.name
                            .StartsWith("Front flowers"))
                        .All(renderer => !renderer.enabled));
                }
                finally
                {
                    if (root != null) Object.DestroyImmediate(root.gameObject);
                }
            }
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void FiveCottageVariantsHaveRoundedPlantsAndLowPicketEnclosures()
    {
        var owner = new GameObject("Cottage foundation fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            var rose = Resources.Load<Texture2D>(
                "CityForgeV3/Garden/FoundationPlantingsV01/rose-bush-summer");
            var purple = Resources.Load<Texture2D>(
                "CityForgeV3/Garden/FoundationPlantingsV01/purple-phlox-summer");
            Assert.NotNull(rose);
            Assert.NotNull(purple);
            foreach (var id in CottageIds)
            {
                Assert.IsTrue(LotWorldController.IsGardenPropId(id));
                var root = world.CreatePropPresentation(id, id, 1f);
                try
                {
                    Assert.NotNull(root, id);
                    Assert.NotNull(root.Find("Photographic planting plan"));
                    Assert.IsNull(root.Find("Rear clipped hedge"));
                    Assert.IsNull(root.Find("Framing clipped hedges"));
                    var fence = root.Find("Weathered picket enclosure");
                    if (id == CottageIds[0]) Assert.IsNull(fence);
                    else
                    {
                        Assert.NotNull(fence, id);
                        var mesh = fence.GetComponent<MeshFilter>().sharedMesh;
                        Assert.That(mesh.bounds.size.x, Is.LessThanOrEqualTo(2f));
                        Assert.That(mesh.bounds.size.z, Is.LessThanOrEqualTo(1f));
                        Assert.That(mesh.bounds.max.y, Is.LessThan(.6f));
                    }
                    foreach (var shrub in root.GetComponentsInChildren<MeshRenderer>()
                        .Where(renderer => renderer.name == "Rounded clipped shrub"))
                        Assert.That(shrub.sharedMaterial.mainTexture,
                            Is.SameAs(Resources.Load<Texture2D>(
                                LowPolyBoxwoodHedge.FoliageResource)));
                    Assert.That(root.GetComponentsInChildren<SpriteRenderer>()
                        .Count(renderer => renderer.name.StartsWith(
                            "Photographic foliage")), Is.GreaterThanOrEqualTo(8));
                    var dimensions = typeof(LotWorldController).GetMethod(
                        "PropDimensions", BindingFlags.Static | BindingFlags.NonPublic);
                    var straight = new object[] { id, 0, 0f, 0f };
                    dimensions.Invoke(null, straight);
                    Assert.That(straight[2], Is.EqualTo(2f));
                    Assert.That(straight[3], Is.EqualTo(1f));
                    var turned = new object[] { id, 1, 0f, 0f };
                    dimensions.Invoke(null, turned);
                    Assert.That(turned[2], Is.EqualTo(1f));
                    Assert.That(turned[3], Is.EqualTo(2f));

                    root.GetComponent<FoundationGardenBed>()
                        .SetSeason(SeasonPreset.Winter);
                    Assert.IsFalse(root.Find("Photographic planting plan")
                        .GetComponent<SpriteRenderer>().enabled);
                    Assert.IsTrue(root.GetComponentsInChildren<SpriteRenderer>(true)
                        .Where(renderer => renderer.transform.parent.name
                            .StartsWith("Front flowers") ||
                            renderer.transform.parent.name
                                .StartsWith("Purple accent") ||
                            renderer.transform.parent.name
                                .StartsWith("Tall purple flowers"))
                        .All(renderer => !renderer.enabled));
                }
                finally
                {
                    if (root != null) Object.DestroyImmediate(root.gameObject);
                }
            }
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void FoundationBedIdsAndQuarterTurnSurviveSessionRoundTrip()
    {
        var owner = new GameObject("Foundation garden session fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.NewEmptyLot("Foundation garden fixture", LotType.Residential,
                4, 4);
            foreach (var id in Ids.Concat(CottageIds))
                Assert.IsTrue(world.PlacePropForQa(id, 0f, 0f));
            typeof(LotWorldController).GetProperty("SelectedPropIndex")
                .SetValue(world, 6);
            Assert.IsTrue(world.RotateSelectedProp(1));
            var restored = new LotEditorSession();
            restored.Restore(world.Session.Serialize());
            Assert.That(restored.Data.Props.Select(prop => prop.PropId),
                Is.EquivalentTo(Ids.Concat(CottageIds)));
            Assert.That(restored.Data.Props[6].RotationQuarterTurns,
                Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(owner); }
    }
}
