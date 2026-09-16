using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class NewEnglandBarnPropTests
{
    [Test]
    public void BarnHasUprightGroundedTexturedModelAndMatchingRenderPasses()
    {
        var owner = new GameObject("Barn asset fixture");
        Transform prop = null;
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            prop = world.CreatePropPresentation(LotWorldController.NewEnglandBarnPropId, "Barn", 1f);
            Assert.NotNull(prop);
            var model = prop.Find("New England Barn Model");
            var renderer = model.GetComponentInChildren<Renderer>();
            Assert.That(renderer.bounds.size.y, Is.EqualTo(9.8f).Within(.02f));
            Assert.That(renderer.bounds.size.x, Is.EqualTo(11.354f).Within(.1f));
            Assert.That(renderer.bounds.size.z, Is.EqualTo(14.084f).Within(.1f));
            Assert.That(renderer.bounds.min.y, Is.EqualTo(0f).Within(.02f));
            Assert.That(renderer.sharedMaterial.mainTexture.name, Is.EqualTo("tripo_image_52715d3f_0"));
            Assert.That(renderer.sharedMaterial.GetTexture("_BumpMap").name, Is.EqualTo("tripo_image_52715d3f_2"));
            Assert.That(renderer.sharedMaterial.GetFloat("_Metallic"), Is.Zero);
            foreach (var name in new[] { "Committed Prop Depth Prepass", "Projected Prop Silhouette", "Wet Street Prop Reflection" })
            {
                var pass = prop.Find(name);
                Assert.NotNull(pass);
                Assert.That(Quaternion.Angle(model.localRotation, pass.localRotation), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(model.localScale, pass.localScale), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(model.localPosition, pass.localPosition), Is.LessThan(.001f));
            }
            Assert.NotNull(Resources.Load<Texture2D>("CityForgeV3/UI/PropThumbnails/" + LotWorldController.NewEnglandBarnPropId));
        }
        finally
        {
            if (prop != null) Object.DestroyImmediate(prop.gameObject);
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BarnPlacementAndRotationRoundTripAsAProp()
    {
        var owner = new GameObject("Barn placement fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.NewEmptyLot("Barn fixture", LotType.Residential, 4, 4);
            Assert.IsTrue(world.PlacePropForQa(LotWorldController.NewEnglandBarnPropId, 3, -4));
            typeof(LotWorldController).GetProperty("SelectedPropIndex").SetValue(world, 0);
            Assert.IsTrue(world.RotateSelectedProp(1));
            var restored = new LotEditorSession();
            restored.Restore(world.Session.Serialize());
            var prop = restored.Data.Props.Single();
            Assert.AreEqual(LotWorldController.NewEnglandBarnPropId, prop.PropId);
            Assert.AreEqual(1, prop.RotationQuarterTurns);
            Assert.AreEqual(3, prop.PositionX);
            Assert.AreEqual(-4, prop.PositionZ);
        }
        finally { Object.DestroyImmediate(owner); }
    }
}
