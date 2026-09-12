#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CityForgeV3.World;
namespace CityForgeV3.UI
{
 public sealed partial class CityForgeApp
 {
  public void ReloadFloraPoseQa()
  {
   if(_districtWorld==null)return;
   var district=FindSelectedRegionTile();
   _districtWorld.Build(district);
   _districtWorld.SetZoom(_terraformZoomLevel);
   _districtWorld.SetPan(_terraformPanOffset);
   var rotation=_districtWorld.WorldCamera.transform.rotation;
   foreach(var r in _districtWorld.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("District Flora —") && !r.name.Contains("stone")))
    if(Quaternion.Angle(r.transform.rotation,rotation)>.01f)throw new Exception("Flora not facing district camera");
   if(_openRegion?.RegionId=="transient-district-scale-qa") _districtWorld.WorldCamera.orthographicSize=35f;
   Debug.Log("DISTRICT FLORA POSE PASS");
  }
  public void SetFloraShadowComparisonQa(bool visible)
  {
   if(_districtWorld==null)return;
   if(visible && _districtWorld.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="District Flora Shadow"))
    _districtWorld.RefreshFlora(FindSelectedRegionTile());
   foreach(var r in _districtWorld.GetComponentsInChildren<Renderer>().Where(r=>r.name=="District Flora Shadow"))r.enabled=visible;
   var shadows=_districtWorld.GetComponentsInChildren<Renderer>().Where(r=>r.name=="District Flora Shadow").ToArray();
   if(shadows.Length>0){var b=new MaterialPropertyBlock();shadows[0].GetPropertyBlock(b);Debug.Log("SHADOW STATE time="+_districtWorld.TimeOfDay+" count="+shadows.Length+" color="+b.GetColor("_Color")+" ray="+b.GetVector("_SunRay")+" upright="+b.GetFloat("_UprightSource")+" ground="+b.GetFloat("_GroundY"));}
   Debug.Log("FLORA SHADOW COMPARISON: "+(visible?"ON":"OFF"));
  }
  public void CheckMountainPaintQa()
  {
   if(_currentScreen!=AppScreen.Splash) {Debug.LogWarning("Family paint QA requires fresh splash; user work preserved.");return;}
   StartCoroutine(CheckMountainPaintRoutine());
  }
  private IEnumerator CheckMountainPaintRoutine()
  {
   foreach(SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
   {
    var id=LotWorldController.ResolveFloraPresentationId("fraser-fir-snowy",0,season);
    if((id=="fraser-fir-snowy")!=(season==SeasonPreset.Winter))throw new Exception("Snow outside winter");
    var resource=LotWorldController.ResolveFloraResourcePath("fraser-fir-snowy",season);
    if(Resources.Load<Texture2D>(resource)==null)throw new Exception("Missing seasonal Fraser texture: "+resource);
   }
   OpenDistrictScaleQa(DistrictEditorMode.Terraform);
   _terraformZoomLevel=DistrictZoomLevel.LOD1;
   ArmDistrictFloraPlacement(StoneFloraCatalog.Families[1].Id,0);
   SelectDistrictFloraLibrary(true);
   if(_pendingDistrictFloraId!=StoneFloraCatalog.Families[1].Id)throw new Exception("Stone choice not restored");
   SelectDistrictFloraLibrary(false);
   if(!string.IsNullOrEmpty(_pendingDistrictFloraId))throw new Exception("Trees retained stone selection");
   // Exercise the same family-tab event used by the modal, starting from a stone.
   ArmDistrictFloraPlacement(StoneFloraCatalog.Families[1].Id,0);
   EnsureDistrictWorld(FindSelectedRegionTile());
   Show(AppScreen.DistrictTerraform);
   var tabs=new VisualElement();
   AddTreeFamilyTabs(tabs,()=>{},()=>SelectDistrictFloraLibrary(false));
   _root.Add(tabs);
   yield return null;
   using(var submit=NavigationSubmitEvent.GetPooled())
   {
    var button=tabs.Q<Button>("flora-family-fir-and-mountain");
    submit.target=button;
    button.SendEvent(submit);
   }
   tabs.RemoveFromHierarchy();
   if(_pendingDistrictTreeFamily!=FloraFamilies.Mountain||!string.IsNullOrEmpty(_pendingDistrictFloraId))throw new Exception("Fir tab retained stale selection");
   if(!DistrictFloraPlacementArmed())throw new Exception("Mountain tool failed to arm");
   var district=FindSelectedRegionTile();
   EnsureDistrictWorld(district);
   Show(AppScreen.DistrictTerraform);
   foreach(var tree in DistrictTrees.Where(t=>FloraFamilies.ForTree(t.Id)==FloraFamilies.Mountain))
    if(Resources.Load<Texture2D>(LotWorldController.ResolveFloraResourcePath(tree.Id,SeasonPreset.Summer))==null)throw new Exception("Missing fir texture: "+tree.Id);
   BeginDistrictFloraPaint(district,new Vector2(.5f,.5f));
   int first=district.Flora.Count;
   if(first<9)throw new Exception("Mountain click placed no group");
   ContinueDistrictFloraPaint(district,new Vector2(.5f,.5f));
   if(district.Flora.Count!=first)throw new Exception("Stationary pointer duplicated trees");
   ContinueDistrictFloraPaint(district,new Vector2(.5f+72f/DistrictScale.SizeMeters(district.Width),.5f));
   FinishDistrictFloraPaint(district);
   if(district.Flora.Count<=first||_districtFloraPainting)throw new Exception("Drag planting failed");
   if(district.Flora.Select(t=>t.GroupId).Distinct().Count()!=1)throw new Exception("Stroke not grouped");
   if(district.Flora.Any(t=>FloraFamilies.ForTree(t.FloraId)!=FloraFamilies.Mountain))throw new Exception("Cross-family paint");
   yield return null; // Destroy from RefreshFlora completes at the end of the frame.
   var visible=_districtWorld.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.gameObject.name.StartsWith("District Flora —"));
   if(visible!=district.Flora.Count)throw new Exception("Missing rendered firs: "+visible+" / "+district.Flora.Count);
   foreach(var shadow in _districtWorld.GetComponentsInChildren<Renderer>().Where(r=>r.name=="District Flora Shadow"))
   {
    var b=new MaterialPropertyBlock();shadow.GetPropertyBlock(b);
    if(!shadow.enabled || b.GetFloat("_UprightSource")!=1f || b.GetColor("_Color").a<=0f)
     throw new Exception("District shadow projection not configured");
   }
   Debug.Log("MOUNTAIN PAINT QA PASS: seasonal snow verified; click="+first+", stroke="+district.Flora.Count+", rendered="+visible+"; stationary pointer stable; stroke grouped; released.");
  }
 }
}
#endif
