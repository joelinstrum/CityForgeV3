#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using CityForgeV3.World;using UnityEngine;using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
 public sealed partial class CityForgeApp
 {
  public void BrickworksQa(string command)
  {
   const string dir="/tmp/cityforge-brickworks-review/";Directory.CreateDirectory(dir);
   if(command=="restore"){QuarryQa("placement-restore");return;}
   if(command=="prepare")
   {
    QuarryQa("placement-prepare");var d=FindSelectedRegionTile();var s=d.StoneSites[1];
    ComposeDistrictIndustryModal();if(_root.Q<Button>("industry-add-brickworks").enabledSelf)throw new Exception("Brickworks unlocked before a quarry");
    SetQuarry(s,true);var center=DistrictQuarry.Point(d,s);
    var b=new DistrictBrickworksSite{NormalizedX=.5f+(center.x+40)/640,NormalizedZ=.5f+60/640f};
    if(!DistrictBrickworks.Build(d,b,_=>true))throw new Exception("Brickworks fixture placement failed");
    int gridX=Mathf.FloorToInt((center.x+15+320)/10);
    for(int z=32;z<=41;z++)d.Roads.Add(new(){GridX=gridX,GridZ=z});
    s.Phase="full";s.Elapsed=s.Script.fullCartSeconds;s.CartBlocks=4;s.CargoStoneTons=4;d.ResourceInventory.Stone=4;
    _districtWorldCompositionKey="";EnsureDistrictWorld(d);Show(AppScreen.DistrictTerraform);SetDistrictSimulationPaused(true);
    _districtWorld.PresentQuarries(d,false);_districtWorld.PresentBrickworks(d);SaveDistrictEdit();return;
   }
   if(_districtUndoQaSaveRoot==null||FindSelectedRegionTile().TileId!="quarry-placement-review")throw new Exception("Isolated Brickworks fixture required");
   var district=FindSelectedRegionTile();var site=district.StoneSites[1];
   if(command=="merge-check")
   {
    CheckDistrictDeletionPerformanceQa();
    district=FindSelectedRegionTile();site=district.StoneSites[1];
    foreach(var identity in new[]{new DistrictSelectionRef(DistrictSelectionKind.Entity,"quarry:"+site.Id),new DistrictSelectionRef(DistrictSelectionKind.Entity,"brickworks:"+district.Brickworks[0].Id)})
    {
     _districtWorld.PresentQuarries(district,false);_districtWorld.PresentBrickworks(district);
     var target=_districtWorld.ResolveSelectable(identity);
     if(target==null)throw new Exception("Missing shared selection target "+identity.Id);
     ComposeSelectedObject(identity);
     DistrictIndustryRotation.TryFootprint(district,identity,out _,out _,out var before);
     if(!RotateSelectedDistrictObject(1))throw new Exception("Shared keyboard rotation failed");
     DistrictIndustryRotation.TryFootprint(district,identity,out _,out _,out var after);
     if(after!=Mathf.Repeat(before+90,360))throw new Exception("Incorrect shared rotation");
     if(!UndoDistrictEdit())throw new Exception("Shared rotation undo failed");
    }
    File.WriteAllText(dir+"merge-check.txt","PASS: shared quarry and Brickworks selection, keyboard rotation, undo, and incremental tree deletion.");return;
   }
   if(command=="diagnose"){File.WriteAllText(dir+"diagnostic.txt",_districtWorld.DiagnoseQuarryDelivery(district,site));return;}
   if(command=="menu"){ComposeDistrictIndustryModal();return;}
   if(command=="focus")
   {
    RemoveDocumentModal();var b=district.Brickworks[0];var target=_districtWorld.ResourceWorldPoint(DistrictBrickworks.Point(district,b))+Vector3.up*4;
    var camera=_districtWorld.WorldCamera;camera.transform.position=target+new Vector3(28,23,32);camera.transform.LookAt(target);camera.orthographicSize=20;return;
   }
   if(command=="overview")
   {
    RemoveDocumentModal();var p=DistrictQuarry.Point(district,site)+new Vector2(20,35);var target=_districtWorld.ResourceWorldPoint(p);
    var camera=_districtWorld.WorldCamera;camera.transform.position=target+new Vector3(60,80,70);camera.transform.LookAt(target);camera.orthographicSize=62;return;
   }
   if(command=="step")
   {
    for(int i=0;i<100;i++)
    {
     _districtWorld.TickQuarryDeliveries(district,true,.1f);DistrictBrickworks.Tick(district,.1f);_districtWorld.PresentQuarries(district,false);
    }
    RefreshBrickworksWarnings(district);
   }
   if(command=="reload")
   {
    SaveDistrictEdit();_openRegion=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot);
    _districtWorldCompositionKey="";EnsureDistrictWorld(FindSelectedRegionTile());_districtWorld.PresentQuarries(FindSelectedRegionTile(),false);_districtWorld.PresentBrickworks(FindSelectedRegionTile());
    district=FindSelectedRegionTile();site=district.StoneSites[1];
   }
   if(command=="missing")
   {
    district.Brickworks[0].Enabled=false;
    for(int i=0;i<100;i++)_districtWorld.TickQuarryDeliveries(district,true,.1f);
    RefreshBrickworksWarnings(district);
    if(!site.DeliveryStatus.StartsWith("Bricksworks required")||site.CartBlocks!=4)throw new Exception("Missing Brickworks must retain cargo and warn");
    File.WriteAllText(dir+"missing.txt","PASS: full cargo retained and temporary Bricksworks required warning shown");district.Brickworks[0].Enabled=true;site.DeliveryRetry=0;
   }
   if(command=="check")
   {
    if(district.ResourceInventory.Bricks!=4||district.ResourceInventory.Stone!=0||district.Brickworks[0].StoneInput!=0||site.CartBlocks!=0||site.Phase!="mining")throw new Exception("Delivery/conversion/return incomplete: "+site.Phase+" bricks="+district.ResourceInventory.Bricks+" "+site.DeliveryStatus);
    File.WriteAllText(dir+"verified.txt","PASS: quarry prerequisite, full wagon road delivery, unload, 4 stone -> 4 bricks, empty wagon return; saved and reloaded during travel");
   }
   var nav=new DistrictQuarryNavigation(district,site,p=>false);var from=site.HasWagonPose?site.WagonPosition:DistrictBrickworks.QuarryHome(district,site);
   File.WriteAllText(dir+"status.txt",$"phase={site.Phase} cargo={site.CartBlocks} tons={site.CargoStoneTons} position={site.WagonPosition} destination={site.DeliveryDestination} bricks={district.ResourceInventory.Bricks} input={district.Brickworks[0].StoneInput} roads={nav.Destinations(from).Count()} status={site.DeliveryStatus}");
  }
 }
}
#endif
