using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class SchoolyardChildrenAutomataTests
    {
        [Test]
        public void PreRenderedGroupPlacesAndRoundTripsWithoutSaving()
        {
            var owner = new GameObject("Schoolyard automata test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Schoolyard", LotType.Civics, 4, 4);
                world.SetSeason(SeasonPreset.Spring);
                var clip = AutomataClipCatalog.Find(
                    "group-18th-century-children-v01");
                Assert.That(clip, Is.Not.Null);
                Assert.That(AutomataClipCatalog.ResourcesAvailable(clip),
                    Is.True);
                Assert.That(world.PlaceAutomataAt(
                    clip.id, 8f, 0f), Is.True);
                Assert.That(world.AutomataCount, Is.EqualTo(1));
                var group = owner.GetComponentInChildren<
                    AutomataClipPlayer>(true);
                Assert.That(group, Is.Not.Null);
                Assert.That(group.transform.childCount, Is.EqualTo(2));
                var art = group.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name ==
                        "Pre-rendered scene");
                Assert.That(art.sprite, Is.Not.Null);
                var lowestVisiblePoint = art.transform.position -
                    art.transform.up * clip.visibleBelowPivotMeters;
                Assert.That(lowestVisiblePoint.y,
                    Is.GreaterThan(group.transform.position.y));
                var outline = group.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name ==
                        "Group selection outline");
                Assert.That(outline.enabled, Is.True);
                Assert.That(world.SelectedAutomataName,
                    Is.EqualTo(clip.displayName));
                Assert.That(world.SelectAutomataAt(-15f, -15f), Is.False);
                Assert.That(outline.enabled, Is.False);
                var camera = owner.GetComponentInChildren<Camera>(true);
                var upperImagePoint = art.transform.TransformPoint(
                    new Vector3(0f, art.sprite.bounds.max.y * 0.8f, 0f));
                var pixel = camera.WorldToScreenPoint(upperImagePoint);
                var panelSize = new Vector2(camera.pixelWidth,
                    camera.pixelHeight);
                Assert.That(group.ContainsScreenPixel(pixel), Is.True);
                Assert.That(world.BeginAutomataDragFromPanel(
                    new Vector2(pixel.x, panelSize.y - pixel.y),
                    panelSize), Is.True);
                Assert.That(world.EndAutomataDrag(), Is.False);
                Assert.That(outline.enabled, Is.True);
                Assert.That(world.RotateSelectedAutomata(1), Is.True);
                var restored = new LotEditorSession();
                restored.Restore(world.Session.Serialize());
                Assert.That(restored.Data.Automata.Count, Is.EqualTo(1));
                Assert.That(restored.Data.Automata[0].RotationQuarterTurns,
                    Is.EqualTo(1));
                Assert.That(world.UndoAutomata(), Is.True);
                Assert.That(world.Session.Data.Automata[0]
                    .RotationQuarterTurns, Is.Zero);
                Assert.That(world.BeginAutomataDragAt(9f, 0f), Is.True);
                Assert.That(world.DragAutomataTo(11f, 2f), Is.True);
                Assert.That(world.EndAutomataDrag(), Is.True);
                Assert.That(world.Session.Data.Automata[0].PositionX,
                    Is.EqualTo(10f).Within(0.001f));
                Assert.That(world.Session.Data.Automata[0].PositionZ,
                    Is.EqualTo(2f).Within(0.001f));
                Assert.That(world.UndoAutomata(), Is.True);
                Assert.That(world.Session.Data.Automata[0].PositionX,
                    Is.EqualTo(8f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void GroupScheduleUsesTimeAndSeasonWithoutSaving()
        {
            Assert.That(LotWorldController.AutomataSeasonForDistrictIndex(0),
                Is.EqualTo(SeasonPreset.Summer));
            Assert.That(LotWorldController.AutomataSeasonForDistrictIndex(1),
                Is.EqualTo(SeasonPreset.Autumn));
            Assert.That(LotWorldController.AutomataSeasonForDistrictIndex(2),
                Is.EqualTo(SeasonPreset.Winter));
            Assert.That(LotWorldController.AutomataSeasonForDistrictIndex(3),
                Is.EqualTo(SeasonPreset.Spring));
            var owner = new GameObject("Schoolyard schedule test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Schoolyard", LotType.Civics, 4, 4);
                world.SetSeason(SeasonPreset.Spring);
                Assert.That(world.PlaceAutomataAt(
                    "group-18th-century-children-v01", 0f, 0f), Is.True);
                var group = owner.GetComponentInChildren<
                    AutomataClipPlayer>(true);
                var renderers = group.GetComponentsInChildren<
                    SpriteRenderer>(true);
                var art = renderers.Single(renderer => renderer.gameObject
                    .name == "Pre-rendered scene");
                var outline = renderers.Single(renderer => renderer.gameObject
                    .name == "Group selection outline");
                Assert.That(world.SelectedAutomataTimeEnabled(
                    TimeOfDayPreset.Noon), Is.True);
                Assert.That(world.SelectedAutomataSeasonEnabled(
                    SeasonPreset.Spring), Is.True);
                Assert.That(world.SelectedAutomataSeasonEnabled(
                    SeasonPreset.Summer), Is.False);
                Assert.That(art.enabled, Is.True);
                world.SetTimeOfDay(TimeOfDayPreset.Morning);
                group.RefreshVisibility();
                Assert.That(art.enabled, Is.False);
                Assert.That(outline.enabled, Is.True);
                world.SetTimeOfDay(TimeOfDayPreset.Noon);
                world.SetSeason(SeasonPreset.Winter);
                group.RefreshVisibility();
                Assert.That(art.enabled, Is.False);
                Assert.That(world.SelectAutomataAt(-15f, -15f), Is.False);
                Assert.That(outline.enabled, Is.False);
                Assert.That(world.SelectNextAutomata(), Is.True);
                Assert.That(outline.enabled, Is.True);
                world.SetSeason(SeasonPreset.Spring);
                group.RefreshVisibility();
                Assert.That(art.enabled, Is.True);
                Assert.That(world.SetSelectedAutomataSeasonEnabled(
                    SeasonPreset.Spring, false), Is.True);
                Assert.That(art.enabled, Is.False);
                Assert.That(world.UndoAutomata(), Is.True);
                Assert.That(world.SelectedAutomataSeasonEnabled(
                    SeasonPreset.Spring), Is.True);
                var districtSeason = SeasonPreset.Autumn;
                world.BindAutomataSeasonProvider(() => districtSeason);
                group.RefreshVisibility();
                Assert.That(art.enabled, Is.True);
                districtSeason = SeasonPreset.Winter;
                group.RefreshVisibility();
                Assert.That(art.enabled, Is.False);
                var restored = new LotEditorSession();
                restored.Restore(world.Session.Serialize());
                Assert.That(restored.Data.Automata[0].VisibleTimeMask,
                    Is.EqualTo(2));
                Assert.That(restored.Data.Automata[0].VisibleSeasonMask,
                    Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
