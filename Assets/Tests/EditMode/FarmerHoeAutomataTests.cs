using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class FarmerHoeAutomataTests
    {
        [Test]
        public void FarmerClipIsSharedContainedAndScheduled()
        {
            var clip = AutomataClipCatalog.Find("founders-farmer-hoeing-v01");
            Assert.That(clip, Is.Not.Null);
            Assert.That(AutomataClipCatalog.ResourcesAvailable(clip), Is.True);
            Assert.That(clip.footprintMeters, Is.EqualTo(3f));
            Assert.That(clip.frameCount, Is.EqualTo(32));
            Assert.That(clip.facingCount, Is.EqualTo(8));
            Assert.That(clip.defaultTimeMask, Is.EqualTo(7));

            var owner = new GameObject("Farmer hoe clip test");
            try
            {
                var world = owner.AddComponent<LotWorldController>();
                world.Build();
                world.NewEmptyLot("Field", LotType.Agricultural, 5, 5);
                world.SetSeason(SeasonPreset.Spring);
                world.SetTimeOfDay(TimeOfDayPreset.Noon);
                Assert.That(world.PlaceAutomataAt(clip.id, 0f, 0f), Is.True);
                Assert.That(world.AutomataCount, Is.EqualTo(1));
                var player = owner.GetComponentInChildren<AutomataClipPlayer>(true);
                Assert.That(player, Is.Not.Null);
                var art = player.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name ==
                        "Pre-rendered scene");
                Assert.That(art.sprite, Is.Not.Null);
                Assert.That(art.enabled, Is.True);
                world.SetTimeOfDay(TimeOfDayPreset.Night);
                player.RefreshVisibility();
                Assert.That(art.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
