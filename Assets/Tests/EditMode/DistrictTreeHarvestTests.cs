using NUnit.Framework;
using UnityEngine;
using CityForgeV3.World;
namespace CityForgeV3.Tests.EditMode
{
 public class DistrictTreeHarvestTests
 {
  [Test] public void OldSavesRemainStanding(){var t=JsonUtility.FromJson<PlacedDistrictFlora>("{\"FloraId\":\"cilician-fir\"}");Assert.IsTrue(DistrictTreeHarvest.CanFell(t));Assert.AreEqual(0,t.RemainingWood);}
  [Test] public void OnlyCilicianCanFallAndYieldCannotDuplicate(){var t=new PlacedDistrictFlora{FloraId="maple"};Assert.IsFalse(DistrictTreeHarvest.Fell(t,0));t.FloraId="cilician-fir";Assert.IsTrue(DistrictTreeHarvest.Fell(t,99));Assert.AreEqual(3,t.HarvestDirection);Assert.IsFalse(DistrictTreeHarvest.Fell(t,0));Assert.AreEqual(300,t.RemainingWood);}
  [Test] public void PartialPickupRoundTripsAndEmptyTreeBecomesStump(){var t=new PlacedDistrictFlora{FloraId="cilician-fir"};DistrictTreeHarvest.Fell(t,2);Assert.AreEqual(3,DistrictTreeHarvest.TakeWood(t,3));t=JsonUtility.FromJson<PlacedDistrictFlora>(JsonUtility.ToJson(t));Assert.AreEqual(297,t.RemainingWood);Assert.AreEqual(297,DistrictTreeHarvest.TakeWood(t,1000));Assert.AreEqual(DistrictTreeHarvestState.Stump,t.HarvestState);Assert.AreEqual(0,DistrictTreeHarvest.TakeWood(t,100));Assert.IsFalse(DistrictTreeHarvest.Fell(t,0));}
  [Test] public void AllSpritesHaveExpectedDimensionsAndVisibleBounds(){for(int d=0;d<4;d++)for(int f=0;f<24;f++){var s=DistrictHarvestSprites.Get(d,f);Assert.IsNotNull(s);Assert.AreEqual(512,s.rect.width);Assert.AreEqual(3072,s.texture.width);Assert.Less(DistrictHarvestSprites.BoundsFor(s).size.x,s.bounds.size.x);}Assert.IsNotNull(DistrictHarvestSprites.Get(0,0,true));}
  [Test] public void UndoRestoresStandingWithoutRetainingWood(){var t=new PlacedDistrictFlora{FloraId="cilician-fir"};var h=new DistrictUndoHistory();h.Reset(JsonUtility.ToJson(t));DistrictTreeHarvest.Fell(t,1);h.Commit(JsonUtility.ToJson(t));h.TryUndo(out var old);JsonUtility.FromJsonOverwrite(old,t);Assert.IsTrue(DistrictTreeHarvest.CanFell(t));Assert.AreEqual(0,t.RemainingWood);}
 }
}
