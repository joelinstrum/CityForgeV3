using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class VictorianGentlemenAutomataTests
    {
        [Test]
        public void ChattingPairLoadsAsOneSelectableScheduledClip()
        {
            var clip = AutomataClipCatalog.Find(
                "victorian-gentlemen-chatting-v01");
            Assert.That(clip, Is.Not.Null);
            Assert.That(AutomataClipCatalog.ResourcesAvailable(clip), Is.True);
            Assert.That(clip.displayName, Is.EqualTo(
                "Victorian Gentlemen Chatting"));
            Assert.That(clip.defaultTimeMask, Is.EqualTo(7));
            Assert.That(clip.defaultSeasonMask, Is.EqualTo(15));

            var owner = new GameObject("Gentlemen chatting clip test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Conversation", LotType.Civics, 4, 4);
                world.SetSeason(SeasonPreset.Spring);
                world.SetTimeOfDay(TimeOfDayPreset.Noon);
                Assert.That(world.PlaceAutomataAt(clip.id, 0f, 0f), Is.True);
                Assert.That(world.AutomataCount, Is.EqualTo(1));
                Assert.That(world.SelectedAutomataName,
                    Is.EqualTo(clip.displayName));
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
    }
}
