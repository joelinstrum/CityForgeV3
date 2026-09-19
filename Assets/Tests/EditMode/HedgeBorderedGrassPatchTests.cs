using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class HedgeBorderedGrassPatchTests
{
    private static readonly (string id, float width, float depth)[] Cases =
    {
        (LotWorldController.HedgedGrassShortPropId, 4f, 2f),
        (LotWorldController.HedgedGrassLongPropId, 6f, 3f),
        (LotWorldController.HedgedGrassSquarePropId, 4f, 4f),
        (LotWorldController.HedgedGrassLargeSquarePropId, 6f, 6f),
        (LotWorldController.HedgedGrassCirclePropId, 4f, 4f)
    };

    [Test]
    public void AllFiveKeepNaturalGrassInsideTheOriginalClippedHedgeMaterial()
    {
        var owner = new GameObject("Hedged grass fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            var grassSource = Resources.Load<Texture2D>(
                DistrictWorldController.DefaultGrassResource);
            var leafSource = Resources.Load<Texture2D>(
                "CityForgeV3/Garden/GeorgianClippedHedgesV01/clipped-leaves");
            foreach (var item in Cases)
            {
                Assert.IsTrue(LotWorldController.IsGardenPropId(item.id));
                var root = world.CreatePropPresentation(item.id, item.id, 1f);
                try
                {
                    Assert.NotNull(root);
                    var center = root.Find("Natural Grass center/Soft edged Natural Grass");
                    var hedge = root.Find("Clipped hedge perimeter");
                    Assert.NotNull(center);
                    Assert.NotNull(hedge);
                    Assert.That(center.localScale.x, Is.EqualTo(item.width));
                    Assert.That(center.localScale.y, Is.EqualTo(item.depth));
                    Assert.That(center.GetComponent<Renderer>().sharedMaterial.mainTexture,
                        Is.SameAs(grassSource));
                    Assert.That(hedge.GetComponent<Renderer>().sharedMaterial.mainTexture,
                        Is.SameAs(leafSource));
                    var bounds = hedge.GetComponent<MeshFilter>().sharedMesh.bounds;
                    Assert.That(bounds.size.x, Is.EqualTo(item.width - .06f).Within(.02f));
                    Assert.That(bounds.size.z, Is.EqualTo(item.depth - .06f).Within(.02f));
                    Assert.That(bounds.max.y, Is.EqualTo(.54f).Within(.01f));
                }
                finally { if (root != null) Object.DestroyImmediate(root.gameObject); }
            }
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void HedgedGrassIdsAndRotationSurviveSessionRoundTrip()
    {
        var owner = new GameObject("Hedged grass lot fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.NewEmptyLot("Hedged grass fixture", LotType.Residential, 4, 4);
            foreach (var item in Cases)
                Assert.IsTrue(world.PlacePropForQa(item.id, 0f, 0f));
            typeof(LotWorldController).GetProperty("SelectedPropIndex")
                .SetValue(world, 1);
            Assert.IsTrue(world.RotateSelectedProp(1));
            var restored = new LotEditorSession();
            restored.Restore(world.Session.Serialize());
            Assert.That(restored.Data.Props.Select(prop => prop.PropId),
                Is.EquivalentTo(Cases.Select(item => item.id)));
            Assert.That(restored.Data.Props[1].RotationQuarterTurns, Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(owner); }
    }
}
