using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class DistrictUndoHistoryTests
    {
        [Test] public void OnlyFiveRecentEditsSurvive()
        {
            var h = new DistrictUndoHistory(); h.Reset("0");
            for (var i = 1; i <= 8; i++) h.Commit(i.ToString());
            Assert.AreEqual(5, h.Count);
            for (var i = 7; i >= 3; i--) { Assert.IsTrue(h.TryUndo(out var s)); Assert.AreEqual(i.ToString(), s); }
            Assert.IsFalse(h.TryUndo(out _));
        }
        [Test] public void NoOpAndNewEditAfterUndoDoNotResurrectDiscardedState()
        {
            var h = new DistrictUndoHistory(); h.Reset("a");
            Assert.IsFalse(h.Commit("a")); h.Commit("b"); h.Commit("c");
            h.TryUndo(out _); h.Commit("d");
            h.TryUndo(out var s); Assert.AreEqual("b", s);
            h.TryUndo(out s); Assert.AreEqual("a", s);
            Assert.IsFalse(h.TryUndo(out _));
        }
        [Test] public void ResetDropsHistoryWhenChangingDistricts()
        {
            var h = new DistrictUndoHistory(); h.Reset("a"); h.Commit("b"); h.Reset("other");
            Assert.IsFalse(h.TryUndo(out _));
        }
        [Test] public void MixedDeletionRestoresExactObjectsAndTreasury()
        {
            var d = new RegionCityTile();
            d.Flora.Add(new PlacedDistrictFlora {InstanceId="tree", FloraId="tree-a", NormalizedX=.4f});
            d.Lots.Add(new PlacedDistrictLot {InstanceId="lot", GridX=4});
            d.Roads.Add(new PlacedRoadPiece {Id="road", GridX=9});
            d.Rivers.Add(new PlacedDistrictRiver {InstanceId="river"});
            var before = JsonUtility.ToJson(d); var h = new DistrictUndoHistory(); h.Reset(before);
            d.Flora.Clear(); d.Lots.Clear(); d.Roads.Clear(); d.Rivers.Clear(); d.Treasury-=500;
            h.Commit(JsonUtility.ToJson(d)); h.TryUndo(out var restored); JsonUtility.FromJsonOverwrite(restored,d);
            Assert.AreEqual(before, JsonUtility.ToJson(d));
        }
    }
}
