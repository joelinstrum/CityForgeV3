#if UNITY_EDITOR
using UnityEngine;
using CityForgeV3.World;
namespace CityForgeV3.UI
{
 public sealed partial class CityForgeApp
 {
  bool _treeRepairQaActive;
  public void OpenTreeRepairQa(string id)
  {
   if(!_treeRepairQaActive&&(_hasOpenLot||_currentScreen!=AppScreen.Splash)){Debug.LogWarning("Tree repair QA requires fresh splash; existing lot preserved.");return;}
   _treeRepairQaActive=true;EnsureLotWorld();_lotWorld.NewEmptyLot("Tree repairs - "+id,LotType.Residential,4,4);
   _lotWorld.PlaceFloraForQa(id,0,0);_lotWorld.SetBaseTexture("grass-lush");_lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
   _hasOpenLot=true;_lotEditorCategory=LotEditorCategory.Flora;_lotEditorCategoryExpanded=false;_placementFloraId="";Show(AppScreen.LotEditor);
   _lotWorld.SetZoomLevel(LotZoomLevel.Inspection);_lotWorld.SetQaOrthographicSize(id == "fraser-fir-snowy" ? 5f : 10f);_lotWorld.SelectTreeRepairForQa();
  }
 }
}
#endif
