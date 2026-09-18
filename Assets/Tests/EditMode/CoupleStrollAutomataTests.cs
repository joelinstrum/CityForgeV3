using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class CoupleStrollAutomataTests
    {
        [Test]
        public void StrollingPairClearsBrickRoadAtSouthEnd()
        {
            var owner = new GameObject("Strolling pair road clearance test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Stroll over brick road",
                    LotType.Neighborhood, 4, 4);
                world.SelectRoadPackage(RoadPiecePackage.LegacyPackageId);
                world.SelectRoadPiece(RoadPieceTopology.Straight);
                Assert.That(world.SelectRoadCellAtWorld(-5f, -5f), Is.True);
                Assert.That(world.PlaceRoadPiece(), Is.True);
                Assert.That(world.PlaceAutomataAt(
                    "gentleman-and-lady-strolling-v01", -5f, -5f), Is.True);

                var clip = AutomataClipCatalog.Find(
                    "gentleman-and-lady-strolling-v01");
                var player = owner.GetComponentInChildren<AutomataClipPlayer>(true);
                var art = player.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name ==
                        "Pre-rendered scene");
                var road = owner.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(renderer => renderer.gameObject.name ==
                        "Colonial Brick Road Straight");
                Assert.That(art.sharedMaterial.renderQueue,
                    Is.GreaterThan(road.sharedMaterial.renderQueue));
                var lowestVisibleArt = art.transform.position -
                    art.transform.up * 1.14f;
                Assert.That(lowestVisibleArt.y,
                    Is.GreaterThan(road.bounds.max.y));
                Assert.That(clip.visibleBelowPivotMeters,
                    Is.GreaterThan(1.14f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void StrollingPairLoadsMovesAndSchedulesAsOneGroup()
        {
            var clip = AutomataClipCatalog.Find(
                "gentleman-and-lady-strolling-v01");
            Assert.That(clip, Is.Not.Null);
            Assert.That(AutomataClipCatalog.ResourcesAvailable(clip), Is.True);
            Assert.That(clip.footprintMeters, Is.EqualTo(8f));
            Assert.That(clip.frameCount / clip.framesPerSecond,
                Is.EqualTo(20f));
            Assert.That(clip.defaultTimeMask, Is.EqualTo(7));
            Assert.That(clip.defaultSeasonMask, Is.EqualTo(15));

            var owner = new GameObject("Strolling pair clip test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Stroll", LotType.Residential, 4, 4);
                world.SetSeason(SeasonPreset.Spring);
                world.SetTimeOfDay(TimeOfDayPreset.Noon);
                Assert.That(world.PlaceAutomataAt(clip.id, 0f, 0f), Is.True);
                Assert.That(world.AutomataCount, Is.EqualTo(1));
                Assert.That(world.SelectedAutomataName,
                    Is.EqualTo("Gentleman and Lady Strolling"));
                var player = owner.GetComponentInChildren<AutomataClipPlayer>(true);
                Assert.That(player, Is.Not.Null);
                var art = player.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name ==
                        "Pre-rendered scene");
                var outline = player.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name ==
                        "Group selection outline");
                Assert.That(art.sprite, Is.Not.Null);
                Assert.That(art.enabled, Is.True);
                Assert.That(outline.enabled, Is.True);

                Assert.That(world.BeginAutomataDragAt(0f, 0f), Is.True);
                Assert.That(world.DragAutomataTo(2f, 1f), Is.True);
                Assert.That(world.EndAutomataDrag(), Is.True);
                Assert.That(world.Session.Data.Automata[0].PositionX,
                    Is.EqualTo(2f));
                Assert.That(world.RotateSelectedAutomata(1), Is.True);
                Assert.That(world.Session.Data.Automata[0].RotationQuarterTurns,
                    Is.EqualTo(1));
                world.SetTimeOfDay(TimeOfDayPreset.Night);
                player.RefreshVisibility();
                Assert.That(art.enabled, Is.False);
                Assert.That(outline.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void StrollingPairsShareClipButKeepSeparateGarmentColors()
        {
            var clip = AutomataClipCatalog.Find(
                "gentleman-and-lady-strolling-v01");
            Assert.That(clip.recolorSlotOne, Is.EqualTo("Lady's dress"));
            Assert.That(clip.recolorSlotTwo,
                Is.EqualTo("Gentleman's outfit"));
            Assert.That(AutomataClipCatalog.ResourcesAvailable(clip), Is.True);
            var owner = new GameObject("Strolling pair recolor test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Two couples", LotType.Residential, 4, 4);
                Assert.That(world.PlaceAutomataAt(clip.id, -5f, 0f), Is.True);
                Assert.That(world.SetSelectedAutomataRecolor(0, "#6589B5"),
                    Is.True);
                Assert.That(world.SetSelectedAutomataRecolor(1, "#8B7058"),
                    Is.True);
                Assert.That(world.PlaceAutomataAt(clip.id, 5f, 0f), Is.True);
                Assert.That(world.SetSelectedAutomataRecolor(0, "#8EA78D"),
                    Is.True);
                Assert.That(world.SetSelectedAutomataRecolor(1, "#596D8B"),
                    Is.True);
                var placements = world.Session.Data.Automata;
                Assert.That(placements[0].RecolorOneHex,
                    Is.EqualTo("#6589B5"));
                Assert.That(placements[1].RecolorOneHex,
                    Is.EqualTo("#8EA78D"));
                var art = owner.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(renderer => renderer.gameObject.name ==
                        "Pre-rendered scene").ToArray();
                Assert.That(art, Has.Length.EqualTo(2));
                Assert.That(art[0].sharedMaterial,
                    Is.SameAs(art[1].sharedMaterial));
                var first = new MaterialPropertyBlock();
                var second = new MaterialPropertyBlock();
                art[0].GetPropertyBlock(first);
                art[1].GetPropertyBlock(second);
                Assert.That(first.GetColor("_RecolorOne"),
                    Is.Not.EqualTo(second.GetColor("_RecolorOne")));
                Assert.That(first.GetTexture("_MaskTex"), Is.Not.Null);
                Assert.That(second.GetTexture("_MaskTex"), Is.Not.Null);
                Assert.That(world.UndoAutomata(), Is.True);
                Assert.That(world.Session.Data.Automata[1].RecolorTwoHex,
                    Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
