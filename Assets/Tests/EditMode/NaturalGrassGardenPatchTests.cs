using System;
using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class NaturalGrassGardenPatchTests
{
    private static readonly (string id, float width, float depth)[] Cases =
    {
        (LotWorldController.NaturalGrassShortPropId, 4f, 2f),
        (LotWorldController.NaturalGrassLongPropId, 6f, 3f),
        (LotWorldController.NaturalGrassSquarePropId, 4f, 4f),
        (LotWorldController.NaturalGrassLargeSquarePropId, 6f, 6f),
        (LotWorldController.NaturalGrassCirclePropId, 4f, 4f)
    };

    [Test]
    public void EachPatchUsesTheBaseGrassAtItsNormalScaleWithABorder()
    {
        var owner = new GameObject("Natural grass patch fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            Assert.That(LotWorldController.ResolveBaseTexture("default-grass")
                .DisplayName, Is.EqualTo("Natural Grass"));
            foreach (var item in Cases)
            {
                Assert.IsTrue(LotWorldController.IsGardenPropId(item.id));
                var root = world.CreatePropPresentation(item.id, item.id, 1f);
                try
                {
                    var quad = root.Find("Soft edged Natural Grass");
                    Assert.NotNull(quad);
                    Assert.That(quad.localScale.x, Is.EqualTo(item.width));
                    Assert.That(quad.localScale.y, Is.EqualTo(item.depth));
                    Assert.That(quad.localPosition.y, Is.EqualTo(.012f));
                    Assert.IsFalse(quad.GetComponent<Collider>().enabled);
                    var material = quad.GetComponent<Renderer>().sharedMaterial;
                    Assert.That(material.shader.name,
                        Is.EqualTo("CityForgeV3/NaturalGrassGardenPatch"));
                    Assert.That(material.mainTexture,
                        Is.EqualTo(Resources.Load<Texture2D>(
                            DistrictWorldController.DefaultGrassResource)));
                    Assert.That(material.mainTextureScale.x,
                        Is.EqualTo(item.width / 5f));
                    Assert.That(material.mainTextureScale.y,
                        Is.EqualTo(item.depth / 5f));
                    Assert.IsFalse(material.HasProperty("_EdgeFadeMeters"));
                    Assert.That(material.renderQueue, Is.EqualTo(2002));
                    var circular = item.id ==
                        LotWorldController.NaturalGrassCirclePropId;
                    Assert.That(material.GetFloat("_Circular"),
                        Is.EqualTo(circular ? 1f : 0f));
                    var border = root.Find("Muted garden border");
                    Assert.NotNull(border);
                    Assert.That(border.localPosition.y, Is.EqualTo(.018f));
                    Assert.That(border.GetComponent<Renderer>()
                        .sharedMaterial.renderQueue, Is.EqualTo(2003));
                    var mesh = border.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(mesh.vertexCount,
                        Is.EqualTo(circular ? 130 : 16));
                }
                finally { Object.DestroyImmediate(root.gameObject); }
            }
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void NewGardenIdsSurvivePlacementRotationAndSessionRoundTrip()
    {
        var owner = new GameObject("Natural grass lot fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.NewEmptyLot("Natural grass fixture", LotType.Residential, 4, 4);
            foreach (var item in Cases)
                Assert.IsTrue(world.PlacePropForQa(item.id, 0f, 0f));
            typeof(LotWorldController).GetProperty("SelectedPropIndex")
                .SetValue(world, 1);
            Assert.IsTrue(world.RotateSelectedProp(1));
            var restored = new LotEditorSession();
            restored.Restore(world.Session.Serialize());
            Assert.That(restored.Data.Props.Select(prop => prop.PropId),
                Is.EquivalentTo(Cases.Select(item => item.id)));
            Assert.That(restored.Data.Props[1].RotationQuarterTurns,
                Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(owner); }
    }
}
