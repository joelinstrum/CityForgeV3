#if UNITY_EDITOR
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Linq;
namespace CityForgeV3.UI
{
 public sealed partial class CityForgeApp
 {
  public void OpenPlaneUkQa()
  {
   if(_hasOpenLot||_currentScreen!=AppScreen.Splash){Debug.LogWarning("Tree comparison requires a fresh splash session; existing work is preserved.");return;}
   EnsureLotWorld();_lotWorld.NewEmptyLot("Plane UK tree comparison",LotType.Residential,6,4);
   _lotWorld.Session.Data.Flora.Add(new PlacedFlora{InstanceId="plane-qa-a",FloraId="plane-uk-3d-a",PositionX=-8,PositionZ=0});
   _lotWorld.Session.Data.Flora.Add(new PlacedFlora{InstanceId="plane-qa-b",FloraId="plane-uk-3d-b",PositionX=8,PositionZ=0});
   _lotWorld.Session.Data.Flora.Add(new PlacedFlora{InstanceId="plane-qa-billboard",FloraId="london-plane-a",PositionX=0,PositionZ=0});
   _lotWorld.SetBaseTexture("grass-lush");_lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
   _lotWorld.SetSeason(SeasonPreset.Spring);_lotWorld.SetSeason(SeasonPreset.Summer);
   _hasOpenLot=true;_lotEditorCategory=LotEditorCategory.Flora;_lotEditorCategoryExpanded=false;
   _lotStatus="Plane UK A / existing billboard / Plane UK B • 3D at zoom 1–2";Show(AppScreen.LotEditor);
   _lotWorld.SetZoomLevel(LotZoomLevel.Inspection);_lotWorld.SetQaOrthographicSize(16f);
  }
  public void CheckPlaneUkLibraryQa()
  {
   if(_lotWorld==null||!_lotWorld.Session.Data.Flora.Any(f=>f.InstanceId=="plane-qa-a"))throw new Exception("Use disposable comparison lot");
   ShowPlaneUkFloraLibrary();
   var report="";
   foreach(var category in new[]{"trees","shrubs","rocks","agriculture","3d"})
   {
    var tab=_root.Q<Button>("flora-category-"+category);
    if(tab==null||!tab.enabledInHierarchy)throw new Exception("Missing or disabled tab "+category);
    using(var submit=NavigationSubmitEvent.GetPooled()){submit.target=tab;tab.SendEvent(submit);}
    if(!string.Equals(_floraLibraryCategory,category,StringComparison.OrdinalIgnoreCase))throw new Exception("Tab callback failed "+category);
    report+="category="+category+" callback=PASS\n";
   }
   var card=_root.Q<Button>("flora-select-plane-uk-3d-a");
   if(card==null)throw new Exception("Missing tree card");
   using(var submit=NavigationSubmitEvent.GetPooled()){submit.target=card;card.SendEvent(submit);}
   if(_placementFloraId!="plane-uk-3d-a"||_root.Q<VisualElement>("document-modal")!=null)throw new Exception("Tree card callback failed");
   if(!_lotWorld.PlaceFloraForQa(_placementFloraId,0,-8))throw new Exception("Tree planting failed");
   report+="tree card=PASS; planting=PASS\n";
   Directory.CreateDirectory("QA/PlaneUK");File.WriteAllText("QA/PlaneUK/library.txt",report);
   _lotWorld.SetQaOrthographicSize(16f);
  }
  public void ShowPlaneUkFloraLibrary(){_floraLibraryCategory="3D";OpenFloraModal();}
 }
}
#endif
