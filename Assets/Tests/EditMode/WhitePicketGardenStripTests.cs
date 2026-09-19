using System.IO;
using System.Linq;
using System.Reflection;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class WhitePicketGardenStripTests
{
    private static readonly string[] Ids =
    {
        LotWorldController.WhitePicketRoseStripPropId,
        LotWorldController.WhitePicketCottageStripPropId,
        LotWorldController.WhitePicketMixedStripPropId,
        LotWorldController.WhitePicketConeflowerStripPropId,
        LotWorldController.WhitePicketDaisyStripPropId,
        LotWorldController.WhitePicketSusanStripPropId,
        LotWorldController.WhitePicketHostaFernStripPropId,
        LotWorldController.WhitePicketClematisStripPropId,
        LotWorldController.WhitePicketFullCottageStripPropId
    };

    [Test]
    public void GardenLibraryScrollsItsCatalogAboveAFixedDoneBar()
    {
        var app = File.ReadAllText(
            "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");
        var styles = File.ReadAllText(
            "Assets/CityForgeV3/Resources/CityForgeV3/UI/CityForgeV3.uss");
        var start = app.IndexOf("private void OpenGardenModal()");
        var end = app.IndexOf("private void AddGardenBedCard", start);
        var garden = app.Substring(start, end - start);
        StringAssert.Contains("new ScrollView(ScrollViewMode.Vertical)", garden);
        StringAssert.Contains("scroll.Add(grid)", garden);
        StringAssert.Contains("panel.Add(scroll)", garden);
        StringAssert.Contains("panel.Add(actions)", garden);
        Assert.That(garden.IndexOf("panel.Add(scroll)"),
            Is.LessThan(garden.IndexOf("panel.Add(actions)")));
        StringAssert.Contains(".document-modal-panel.garden-library-modal-panel",
            styles);
        StringAssert.Contains(".garden-library-scroll", styles);
        StringAssert.Contains("flex-grow: 1", styles);
        StringAssert.Contains(
            ".garden-library-modal-panel > .document-modal-actions", styles);
        StringAssert.Contains("flex-shrink: 0", styles);
    }

    [Test]
    public void AllStripsHaveTwoMetreFenceAndPlantingOnBothSides()
    {
        var owner = new GameObject("White picket garden fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            var picketAlbedo = Resources.Load<Texture2D>(
                "CityForgeV3/Garden/AgedWhitePicketV01/aged-painted-wood");
            var grass = Resources.Load<Texture2D>(
                DistrictWorldController.DefaultGrassResource);
            foreach (var id in Ids)
            {
                Assert.IsTrue(LotWorldController.IsGardenPropId(id));
                var root = world.CreatePropPresentation(id, id, 1f);
                try
                {
                    Assert.NotNull(root, id);
                    Assert.NotNull(root.GetComponent<WhitePicketGardenStrip>());
                    var fence = root.Find("Aged white picket fence — 2 m");
                    Assert.NotNull(fence, id);
                    var renderer = fence.GetComponentInChildren<MeshRenderer>();
                    Assert.NotNull(renderer);
                    Assert.That(renderer.bounds.size.x, Is.EqualTo(2f).Within(.04f));
                    Assert.That(renderer.bounds.size.y,
                        Is.GreaterThan(.9f).And.LessThan(1.2f));
                    Assert.That(renderer.sharedMaterial.mainTexture,
                        Is.SameAs(picketAlbedo));
                    Assert.That(root.Find("Natural Grass under fence/Soft edged Natural Grass")
                        .GetComponent<Renderer>().sharedMaterial.mainTexture,
                        Is.SameAs(grass));
                    Assert.That(root.GetComponentsInChildren<Transform>()
                        .Count(item => item.name.StartsWith("Front ")),
                        Is.GreaterThanOrEqualTo(5));
                    Assert.That(root.GetComponentsInChildren<Transform>()
                        .Count(item => item.name.StartsWith("Rear ")),
                        Is.GreaterThanOrEqualTo(5));
                    Assert.That(root.GetComponentsInChildren<Transform>()
                        .Where(item => item.name.StartsWith("Front "))
                        .All(item => item.localPosition.z < 0f), Is.True);
                    Assert.That(root.GetComponentsInChildren<Transform>()
                        .Where(item => item.name.StartsWith("Rear "))
                        .All(item => item.localPosition.z > 0f), Is.True);

                    var dimensions = typeof(LotWorldController).GetMethod(
                        "PropDimensions", BindingFlags.Static | BindingFlags.NonPublic);
                    var straight = new object[] { id, 0, 0f, 0f };
                    dimensions.Invoke(null, straight);
                    Assert.That(straight[2], Is.EqualTo(2.2f));
                    Assert.That(straight[3], Is.EqualTo(1.8f));
                    var turned = new object[] { id, 1, 0f, 0f };
                    dimensions.Invoke(null, turned);
                    Assert.That(turned[2], Is.EqualTo(1.8f));
                    Assert.That(turned[3], Is.EqualTo(2.2f));
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
    public void WinterAndPreviewKeepFenceWhileHidingSummerFlowers()
    {
        var owner = new GameObject("White picket seasons fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            var root = world.CreatePropPresentation(Ids[1], Ids[1], 1f);
            try
            {
                var strip = root.GetComponent<WhitePicketGardenStrip>();
                strip.SetAppearance(SeasonPreset.Winter,
                    TimeOfDayPreset.Noon, Vector3.down);
                Assert.IsTrue(root.Find("Aged white picket fence — 2 m")
                    .GetComponentInChildren<MeshRenderer>().enabled);
                Assert.IsTrue(root.GetComponentsInChildren<SpriteRenderer>(true)
                    .All(renderer => !renderer.enabled));
                strip.SetOpacity(.45f, SeasonPreset.Summer,
                    TimeOfDayPreset.Noon, Vector3.down);
                var renderer = root.Find("Aged white picket fence — 2 m")
                    .GetComponentInChildren<MeshRenderer>();
                Assert.That(renderer.sharedMaterial.renderQueue,
                    Is.GreaterThanOrEqualTo(3000));
            }
            finally { if (root != null) Object.DestroyImmediate(root.gameObject); }
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void PicketGardenIdsAndRotationSurviveSessionRoundTrip()
    {
        var owner = new GameObject("White picket session fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.NewEmptyLot("White picket garden", LotType.Residential, 4, 4);
            foreach (var id in Ids)
                Assert.IsTrue(world.PlacePropForQa(id, 0f, 0f));
            typeof(LotWorldController).GetProperty("SelectedPropIndex")
                .SetValue(world, Ids.Length - 1);
            Assert.IsTrue(world.RotateSelectedProp(1));
            var restored = new LotEditorSession();
            restored.Restore(world.Session.Serialize());
            Assert.That(restored.Data.Props.Select(prop => prop.PropId),
                Is.EquivalentTo(Ids));
            Assert.That(restored.Data.Props[Ids.Length - 1].RotationQuarterTurns,
                Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(owner); }
    }
}
