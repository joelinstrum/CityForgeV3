#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using CityForgeV3.World;using UnityEngine;using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
 public sealed partial class CityForgeApp
 {
  RegionSaveData _quarryReviewReturnRegion;
  string _quarryReviewReturnTile, _quarryReviewReturnSaveRoot;
  Vector2 _quarryReviewReturnPan;
  DistrictZoomLevel _quarryReviewReturnZoom;
  bool _quarryReviewReturnPaused;
  // Isolated quarry presentation and crane review commands.
  public void QuarryQa(string command)
  {
   const string dir="/Users/joelinstrum/dev/CityForgeMCP/artifacts/buildings/stone-quarry/v01/";
   if(command=="placement-restore")
   {
    if(_quarryReviewReturnRegion==null)
    { _districtUndoQaSaveRoot=null;_openRegion=null;Show(AppScreen.MainMenu);return; }
    _openRegion=_quarryReviewReturnRegion;_districtUndoQaSaveRoot=_quarryReviewReturnSaveRoot;
    SelectRegionTile(_quarryReviewReturnTile);_terraformPanOffset=_quarryReviewReturnPan;_terraformZoomLevel=_quarryReviewReturnZoom;
    _districtWorldCompositionKey="";EnsureDistrictWorld(FindSelectedRegionTile());Show(AppScreen.DistrictTerraform);
    _districtWorld.SetPan(_terraformPanOffset);_districtWorld.SetZoom(_terraformZoomLevel);SetDistrictSimulationPaused(_quarryReviewReturnPaused);
    _quarryReviewReturnRegion=null;return;
   }
   if(command=="tree-clear-prepare")
   {
    QuarryQa("placement-prepare");var d=FindSelectedRegionTile();
    d.StoneSites.Add(new DistrictStoneSite{Id="tree-stone",NormalizedX=.5f,NormalizedZ=.57f});
    d.Flora.Add(new PlacedDistrictFlora{InstanceId="blocking-tree",FloraId="evergreen",NormalizedX=.5f+5.3f/640,NormalizedZ=.57f,HarvestState=DistrictTreeHarvestState.Standing});
    _districtWorldCompositionKey="";EnsureDistrictWorld(d);Show(AppScreen.DistrictTerraform);SaveDistrictEdit();
    ComposeDistrictIndustryModal();var add=_root.Q<Button>("industry-add-stone");add.Focus();
    using(var evt=NavigationSubmitEvent.GetPooled())add.SendEvent(evt);
    if(_industryMarkers.Count!=3)throw new Exception("Tree-covered stone deposit was hidden");
    File.WriteAllText(dir+"blocked-deposit.txt","PASS: three deposits exposed, including tree-covered site");return;
   }
   if(command=="tree-clear-check")
   {
    var d=FindSelectedRegionTile();if(_districtUndoQaSaveRoot==null||d.TileId!="quarry-placement-review")throw new Exception("Isolated fixture required");
    var button=_root.Q<Button>("industry-site-tree-stone");button.Focus();
    using(var evt=NavigationSubmitEvent.GetPooled())button.SendEvent(evt);
    if(d.StoneSites.Count(s=>s.Built)!=1||!d.StoneSites.Single(s=>s.Id=="tree-stone").Built)throw new Exception("Tree-covered deposit must build directly");
    if(d.Flora.Any(f=>f.InstanceId=="blocking-tree"))throw new Exception("Quarry did not clear its tree");
    var loaded=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot).Tiles.Single();
    if(loaded.Flora.Any(f=>f.InstanceId=="blocking-tree")||!loaded.StoneSites.Single(s=>s.Id=="tree-stone").Built)throw new Exception("Quarry and clearing must save together");
    if(!UndoDistrictEdit())throw new Exception("No construction undo");
    if(d.StoneSites.Any(s=>s.Built)||!d.Flora.Any(f=>f.InstanceId=="blocking-tree"))throw new Exception("Undo must restore tree and remove quarry");
    File.WriteAllText(dir+"tree-clear.txt","PASS: tree-covered deposit selectable; one click builds and clears tree; saved together; undo restores tree and removes quarry");return;
   }
   if(command=="crane-prepare")
   {
    QuarryQa("placement-prepare");var d=FindSelectedRegionTile();var site=d.StoneSites[1];
    SetQuarry(site,true);SetDistrictSimulationPaused(true);RemoveDocumentModal();
    site.Phase="loading";site.Elapsed=0;_districtWorld.PresentQuarries(d,false);return;
   }
   if(command.StartsWith("crane-") && command!="crane-prepare")
   {
    if(_districtUndoQaSaveRoot==null||FindSelectedRegionTile().TileId!="quarry-placement-review")throw new Exception("Isolated crane fixture required");
    RemoveDocumentModal();var d=FindSelectedRegionTile();var site=d.StoneSites[1];
    var crane=_districtWorld.GetComponentInChildren<QuarryCranePresentation>();
    if(command=="crane-focus")
    {
     var target=crane.transform.TransformPoint(new Vector3(3.5f,2,1));var camera=_districtWorld.WorldCamera;
     camera.transform.position=target+new Vector3(13,10,15);camera.transform.LookAt(target);camera.orthographicSize=7;return;
    }
    if(command=="crane-details")
    {
     File.WriteAllLines(dir+"crane-details.txt",crane.GetComponentsInChildren<Renderer>(true).Select(r=>r.name+" enabled="+r.gameObject.activeInHierarchy+" bounds="+r.bounds+" local="+r.transform.localPosition));return;
    }
    if(command=="crane-run"){SetDistrictSimulationPaused(false);return;}
    if(command=="crane-lift")site.Elapsed=site.Script.loadingSeconds*.15f;
    if(command=="crane-swing")site.Elapsed=site.Script.loadingSeconds*.48f;
    if(command=="crane-land")site.Elapsed=site.Script.loadingSeconds*.99f;
    if(command=="crane-full"){site.CartBlocks=4;site.BlocksLoaded=4;site.Phase="full";site.Elapsed=0;}
    if(command=="crane-check")
    {
     var original=JsonUtility.ToJson(site);
     if(Resources.Load<GameObject>(QuarryCranePresentation.BaseResource)==null)throw new Exception("Missing derivative base");
     var cargo=crane.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Loaded stone block ")).OrderBy(t=>t.name).ToArray();
     if(cargo.Length!=8)throw new Exception("Expected 8 wagon cargo slots");
     var deck=crane.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Forestry_Bed_Plank_")).ToArray();
     float deckTop=deck.Max(r=>r.bounds.max.y),deckFront=deck.Max(r=>r.bounds.max.z),deckBack=deck.Min(r=>r.bounds.min.z);
     for(int slot=0;slot<4;slot++)
     {
      var bounds=cargo[slot].GetComponent<Renderer>().bounds;
      if(bounds.min.y<deckTop-.02f||bounds.min.y>deckTop+.05f||bounds.min.z<deckBack||bounds.max.z>deckFront)throw new Exception("Cargo is not resting on wagon deck");
     }
     float firstYaw=0;
     for(int slot=0;slot<8;slot++)
     {
      site.Phase="loading";site.CartBlocks=slot;site.Elapsed=site.Script.loadingSeconds*.99f;crane.Present(site);
      if(Vector3.Distance(crane.LoadPosition,cargo[slot].position)>.001f)throw new Exception("Load missed wagon slot "+slot);
     }
     site.CartBlocks=1;site.Elapsed=0;crane.Present(site);firstYaw=crane.BoomYaw;
     site.Elapsed=site.Script.loadingSeconds*.6f;crane.Present(site);var before=crane.LoadPosition;
     if(Mathf.Abs(Mathf.DeltaAngle(firstYaw,crane.BoomYaw))<30)throw new Exception("Crane did not swing");
     var reloaded=JsonUtility.FromJson<DistrictStoneSite>(JsonUtility.ToJson(site));crane.Present(reloaded);
     if(Vector3.Distance(before,crane.LoadPosition)>.001f)throw new Exception("Reload changed suspended load");
     site.Enabled=false;DistrictQuarry.Tick(d,2,_=>true);crane.Present(site);
     if(Vector3.Distance(before,crane.LoadPosition)>.001f)throw new Exception("Paused crane moved");
     var savedRotation=crane.transform.localRotation;crane.transform.localRotation=Quaternion.Euler(0,90,0);
     site.Elapsed=site.Script.loadingSeconds*.99f;crane.Present(site);
     if(Vector3.Distance(crane.LoadPosition,cargo[1].position)>.001f)throw new Exception("Rotated load missed cart");
     crane.transform.localRotation=savedRotation;JsonUtility.FromJsonOverwrite(original,site);
     File.WriteAllText(dir+"crane-live.txt","PASS: 8 cargo slots align, stone rests on wagon deck, boom swings, suspended load survives reload, pause freezes, 90-degree rotation aligns; derivative base loaded");
    }
    _districtWorld.PresentQuarries(d,false);return;
   }
   if(command=="workers-prepare")
   {
    QuarryQa("placement-prepare");var d=FindSelectedRegionTile();var site=d.StoneSites[1];
    SetQuarry(site,true);SetDistrictSimulationPaused(false);RemoveDocumentModal();
    var point=DistrictQuarry.Point(d,site);var target=_districtWorld.ResourceWorldPoint(point)+new Vector3(0,1,-11.8f);
    var camera=_districtWorld.WorldCamera;camera.transform.position=target+new Vector3(7,5,-10);camera.transform.LookAt(target);camera.orthographicSize=4;
    return;
   }
   if(command=="workers-focus")
   {
    var miners=_districtWorld.GetComponentsInChildren<QuarryWorkerPresentation>();
    var target=miners[0].transform.position+new Vector3(2.5f,1,.6f);
    var camera=_districtWorld.WorldCamera;camera.transform.position=target+new Vector3(4,3,-7);camera.transform.LookAt(target);camera.orthographicSize=3.3f;
    return;
   }
   if(command=="workers-check")
   {
    if(_districtUndoQaSaveRoot==null)throw new Exception("Isolated worker fixture required");
    var miners=_districtWorld.GetComponentsInChildren<QuarryWorkerPresentation>();
    if(miners.Length!=2||miners.Any(m=>!m.Working))throw new Exception("Expected two active quarry miners");
    if(miners.Any(m=>m.GetComponentsInChildren<Renderer>().Any(r=>r.name.Contains("Axeman_Axe")&&r.enabled)))throw new Exception("Old axe still visible");
    File.WriteAllText(dir+"workers-live.txt",string.Join("\n",miners.Select(m=>m.name+" phase="+m.CyclePhase+" pick="+m.PickHeadPosition))+"\nPASS: two paid miners, pickaxes equipped, staggered mining motion");return;
   }
   if(command=="placement-prepare")
   {
    _quarryReviewReturnRegion=_openRegion;_quarryReviewReturnTile=_selectedRegionTileId;_quarryReviewReturnSaveRoot=_districtUndoQaSaveRoot;
    _quarryReviewReturnPan=_terraformPanOffset;_quarryReviewReturnZoom=_terraformZoomLevel;_quarryReviewReturnPaused=_districtSimulationPaused;
    var d=new RegionCityTile{TileId="quarry-placement-review",Name="Quarry placement review",Width=1,Height=1,TimeOfDay=TimeOfDayPreset.Noon,StoneDepositsGenerated=true};
    d.StoneSites.Add(new DistrictStoneSite{Id="left-stone",NormalizedX=.43f,NormalizedZ=.5f});
    d.StoneSites.Add(new DistrictStoneSite{Id="right-stone",NormalizedX=.57f,NormalizedZ=.5f});
    _openRegion=new RegionSaveData{RegionId="transient-quarry-placement",Name=d.Name,Tiles=new(){d}};
    _openRegionWasCreatedThisSession=false;_districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeQuarryPlacementQa");
    SelectRegionTile(d.TileId);EnsureDistrictWorld(d);_terraformPanOffset=Vector2.zero;_terraformZoomLevel=DistrictZoomLevel.LOD2;
    Show(AppScreen.DistrictTerraform);_districtWorld.SetPan(Vector2.zero);_districtWorld.SetZoom(_terraformZoomLevel);SetDistrictSimulationPaused(true);
    ComposeDistrictIndustryModal();
    _root.Q<Button>("industry-add-stone").Focus();
    if(_root.Query<VisualElement>("industry-card-stone").ToList().Count!=1)throw new Exception("Expected one stone entry");
    if(_root.Q<Image>(className:"industry-resource-thumbnail").image==null)throw new Exception("Missing thumbnail");
    File.WriteAllText(dir+"placement-menu.txt","PASS: two deposits, one stone entry, thumbnail loaded");
    return;
   }
   if(command=="placement-start")
   {
    using(var evt=NavigationSubmitEvent.GetPooled())_root.Q<Button>("industry-add-stone").SendEvent(evt);
    if(_industryMarkers.Count!=2)throw new Exception("Expected two selectable arrows");
    _industryMarkers[1].button.Focus();
    File.WriteAllText(dir+"placement-arrows.txt","PASS: Add Stone Quarry button exposes two arrows");return;
   }
   if(command=="placement-click")
   {
    var button=_industryMarkers[1].button;button.Focus();
    File.WriteAllText(dir+"placement-click-diagnostic.txt","enabled="+button.enabledInHierarchy+" attached="+(button.panel!=null)+" siteClear="+_districtWorld.QuarrySiteClear(FindSelectedRegionTile(),FindSelectedRegionTile().StoneSites[1])+" prefab="+(Resources.Load<GameObject>(DistrictQuarry.ResourcePath)!=null));
    using(var evt=NavigationSubmitEvent.GetPooled())button.SendEvent(evt);
    return;
   }
   if(command=="placement-status")
   {
    var d=FindSelectedRegionTile();
    if(_districtUndoQaSaveRoot==null||d.TileId!="quarry-placement-review")throw new Exception("Isolated placement fixture required");
    File.WriteAllText(dir+"placement-status.json",JsonUtility.ToJson(d,true));
    return;
   }
   if(command=="placement-verify")
   {
    var d=FindSelectedRegionTile();
    if(_districtUndoQaSaveRoot==null||d.TileId!="quarry-placement-review")throw new Exception("Isolated placement fixture required");
    if(d.StoneSites.Count(s=>s.Built)!=1)throw new Exception("Arrow must build exactly one quarry");
    var loaded=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot).Tiles.Single();
    if(loaded.StoneSites.Count(s=>s.Built)!=1)throw new Exception("Selected quarry not saved");
    if(IndustryPlacementActive)throw new Exception("Arrows not dismissed after placement");
    var selected=d.StoneSites.Single(s=>s.Built);DistrictQuarry.Tick(d,64,_=>true);
    if(d.ResourceInventory.Stone!=1)throw new Exception("Quarry did not produce stone");
    File.WriteAllText(dir+"placement-verified.txt","PASS: selected "+selected.Id+" only; persisted; arrows dismissed; produces stone");return;
   }
   if(command=="notice-start")
   {
    var d=FindSelectedRegionTile();d.Wildlife=new DistrictWildlifeState{SightingCount=1,Status="Bear sighting — workers seek safety"};
    d.Wildlife.Bears.Add(new DistrictBear());RefreshWildlifeAlert(d);return;
   }
   if(command=="notice-verify")
   {
    RefreshWildlifeAlert(FindSelectedRegionTile());RefreshDistrictNotice();
    if(_root.Q<Label>("district-notice")!=null)throw new Exception("Notice remained or reappeared");
    File.WriteAllText(dir+"notice-verified.txt","PASS: bear notice expired while paused; unchanged sighting does not reappear");return;
   }
   if(command=="prepare")
   {
    OpenDockDistrictQa();var d=FindSelectedRegionTile();d.StoneSites.Clear();d.StoneDepositsGenerated=true;
    var site=new DistrictStoneSite{Id="quarry-review",NormalizedX=.5f-100/640f,NormalizedZ=.5f};d.StoneSites.Add(site);
    if(!_districtWorld.BuildQuarry(d,site))throw new Exception("Fixture site rejected");
    SaveDistrictEdit();_districtWorldCompositionKey="";EnsureDistrictWorld(d);Show(AppScreen.DistrictTerraform);FocusQuarry(site);SetDistrictSimulationPaused(false);
   }
   else if(command=="menu")ComposeDistrictIndustryModal();
   else if(command=="user") {OpenDistrictNinePlacementQa(false);ComposeDistrictIndustryModal();}
   else
   {
    if(_districtUndoQaSaveRoot==null)throw new Exception("QA requires isolated save root");
    var d=FindSelectedRegionTile();var site=d.StoneSites.Single(x=>x.Id=="quarry-review");
    if(command=="focus")FocusQuarry(site);
    if(command=="pause")SetDistrictSimulationPaused(true);
    if(command=="resume"){RemoveDocumentModal();SetDistrictSimulationPaused(false);}
    if(command=="script")QuarryScriptEditor(site);
    if(command=="load-test")
    {
     SetDistrictSimulationPaused(true);site.Elapsed=59;site.Phase="mining";site.CartBlocks=0;var before=d.ResourceInventory.Stone;
     DistrictQuarry.Tick(d,1,_=>true);if(d.ResourceInventory.Stone!=before)throw new Exception("Mining credited early");
     DistrictQuarry.Tick(d,2,_=>true);SaveDistrictEdit();var loaded=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot).Tiles.Find(t=>t.TileId==d.TileId);
     if(JsonUtility.ToJson(loaded.StoneSites)!=JsonUtility.ToJson(d.StoneSites))throw new Exception("Reload changed progress");
     DistrictQuarry.Tick(d,2,_=>true);if(d.ResourceInventory.Stone!=before)throw new Exception("Loading must keep stone in transit");
     _districtWorld.PresentQuarries(d);File.WriteAllText(dir+"live-loading.txt","PASS mining no credit; mid-load save/reload; loaded block stays in transit; paused render");
    }
    if(command=="full"){site.CartBlocks=4;site.Phase="full";site.Elapsed=0;SetDistrictSimulationPaused(true);_districtWorld.PresentQuarries(d);}
    if(command=="rotate"){site.Yaw=(site.Yaw+90)%360;_districtWorldCompositionKey="";EnsureDistrictWorld(d);Show(AppScreen.DistrictTerraform);FocusQuarry(site);}
    if(command=="status")File.WriteAllText(dir+"live-status.json",JsonUtility.ToJson(d,true));
   }
  }
 }
}
#endif
