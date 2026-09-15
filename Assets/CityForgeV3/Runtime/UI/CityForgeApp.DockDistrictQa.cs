#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using CityForgeV3.World;
namespace CityForgeV3.UI { public sealed partial class CityForgeApp {
public void OpenDistrictNinePlacementQa(bool review=true){
var path=Path.Combine(Application.persistentDataPath,"CityForge","Regions","62cf8923205e4a2994561301f10c28d9.json");
var source=JsonUtility.FromJson<RegionSaveData>(File.ReadAllText(path));var district=source.Tiles.First(x=>x.Name=="District 9");
_openRegion=review ? new RegionSaveData{RegionId="transient-district-nine-placement",Name="District 9 placement review",Tiles=new List<RegionCityTile>{district}} : source;
_openRegionWasCreatedThisSession=false;_districtUndoQaSaveRoot=review ? Path.Combine(Path.GetTempPath(),"CityForgeDistrictNinePlacementQa") : null;
if(_lotWorld!=null)_lotWorld.gameObject.SetActive(false);EnsureDistrictWorld(district);SelectRegionTile(district.TileId);_terraformZoomLevel=DistrictZoomLevel.LOD0;SelectDistrictCategory("Select");Show(AppScreen.DistrictTerraform);
_pendingDistrictLotId="lumber-mill-dock-operations-v01";_pendingDistrictLotName="Lumber Mill Dock Operations v01";
if(!review){_pendingDistrictLotId="";_pendingDistrictLotName="";var userLot=district.Lots.FirstOrDefault(x=>x.LotId=="lumber-mill-dock-operations-v01");_terraformPanOffset=userLot==null?new Vector2(-590,-240):DistrictWorldController.DistrictLotCenterMeters(district,userLot,LotSaveStore.Read(userLot.LotId));_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=35;Show(AppScreen.DistrictTerraform);return;}
var report="";var found=false;var firstX=0f;var firstZ=0f;
for(var z=104;z<=112;z+=4)for(var x=60;x<=77;x++){
TryDistrictLotFootprint(district,x/256f,z/256f,out var gx,out var gz,out var sx,out var sz,out var valid);
report+=x+","+z+" "+valid+" "+_pendingDistrictLotRotation+" "+_districtLotWaterHint+"\n";
if(valid&&!found){found=true;firstX=x/256f;firstZ=z/256f;_terraformPanOffset=new Vector2((gx+sx*.5f)*10-1280,(gz+sz*.5f)*10-1280);_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=35;_districtWorld.ShowLotPlacementGuide(gx,gz,sx,sz,true);}}
if(found){PlaceDistrictLot(district,firstX,firstZ);report+="Placed through normal UI handler; lots="+district.Lots.Count+"\n";}
File.WriteAllText("/Users/joelinstrum/dev/CityForgeMCP/artifacts/behaviors/lumber-loading/v01/district-nine-placement.txt",report);
}
public void CheckLotDeleteGroundQa(){
var original=_openRegion;var originalTile=FindSelectedRegionTile();
var regionPath=Path.Combine(Application.persistentDataPath,"CityForge","Regions",original.RegionId+".json");
var originalBytes=File.ReadAllBytes(regionPath);
try{
_openRegion=JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(original));
_districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeLotDeleteQa");
var district=FindSelectedRegionTile();EnsureDistrictUndo(district);
var before=JsonUtility.ToJson(district);
var lot=district.Lots.First(x=>x.LotId=="lumber-mill-dock-operations-v01");
_districtSelection.Clear();_selectedDistrictLotInstanceId=lot.InstanceId;_districtDeleteFrame=-1;
if(!DeleteDistrictSelection()||district.Lots.Any(x=>x.InstanceId==lot.InstanceId))throw new Exception("Single lot delete failed");
var saved=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot).Tiles.Find(x=>x.TileId==district.TileId);
if(saved.Lots.Any(x=>x.InstanceId==lot.InstanceId))throw new Exception("Deleted lot persisted");
if(!DeleteDistrictSelection()||district.Rivers.Count!=originalTile.Rivers.Count)throw new Exception("Duplicate key damaged river");
if(!UndoDistrictEdit()||JsonUtility.ToJson(district)!=before)throw new Exception("Undo failed");
var hosts=_districtWorld.GetComponentsInChildren<LotWorldController>();
var blank=0;var painted=0;
foreach(var host in hosts){
var ground=host.GetComponentsInChildren<Renderer>().FirstOrDefault(x=>x.name=="Lot Surface");
if(ground==null)continue;
if(string.IsNullOrWhiteSpace(host.BaseTextureId)){blank++;if(ground.enabled)throw new Exception("Blank ground visible");}
else{painted++;if(!ground.enabled)throw new Exception("Painted ground hidden");}
}
if(blank==0)throw new Exception("No blank ground tested");
var blankHost=hosts.First(x=>string.IsNullOrWhiteSpace(x.BaseTextureId));
var blankGround=blankHost.GetComponentsInChildren<Renderer>().First(x=>x.name=="Lot Surface");
blankHost.SetBaseTexture("grass");
if(!blankGround.enabled)throw new Exception("Setting base did not enable surface");
blankHost.SetBaseTexture("");
if(blankGround.enabled)throw new Exception("Clearing base did not hide surface");
File.WriteAllText("/tmp/cityforge-lot-delete-ground-qa.txt","PASS single selected lot deleted; saved deletion; duplicate input safe; exact undo; blank surfaces hidden="+blank+"; painted visible="+painted);
}finally{
_openRegion=original;_districtUndoQaSaveRoot=null;ClearDistrictUndo();_districtSelection.Clear();_selectedDistrictLotInstanceId="";_pendingDistrictLotId="";_districtWorldCompositionKey="";EnsureDistrictWorld(originalTile);Show(AppScreen.DistrictTerraform);
var placed=originalTile.Lots.First(x=>x.LotId=="lumber-mill-dock-operations-v01");
var data=LotSaveStore.Read(placed.LotId);
_terraformPanOffset=DistrictWorldController.DistrictLotCenterMeters(originalTile,placed,data);
_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=35;
if(!originalBytes.SequenceEqual(File.ReadAllBytes(regionPath)))throw new Exception("Original region changed");
}
}
public void OpenDockDistrictQa(){
var lot=LotSaveStore.Read("lumber-mill-dock-operations-v01");if(lot==null)throw new Exception("Save dock review lot first");
var district=new RegionCityTile{TileId="dock-loading-review",Name="Lumber loading river review",Width=1,Height=1,TimeOfDay=TimeOfDayPreset.Noon};
var placement=new PlacedDistrictLot{InstanceId="dock-review",LotId=lot.LotId,GridX=30,GridZ=30};district.Lots.Add(placement);
var center=DistrictWorldController.DistrictLotCenterMeters(district,placement,lot);var boat=lot.Props.First(p=>BoatCatalog.Find(p.PropId)!=null);
var boatX=center.x-boat.PositionX;var width=DistrictScale.SizeMeters(1);
// The host faces 180 degrees. The channel is beyond the east side of the source lot.
var channelX=boatX-4;
district.Rivers.Add(new PlacedDistrictRiver{InstanceId="dock-river",Depth=DistrictRiverDepth.Deep,WidthMeters=18,Points=new List<DistrictRiverPoint>{new DistrictRiverPoint(.5f+channelX/width,0),new DistrictRiverPoint(.5f+channelX/width,1)}});
_openRegion=new RegionSaveData{RegionId="transient-dock-loading-v01",Name=district.Name,Tiles=new List<RegionCityTile>{district}};
_openRegionWasCreatedThisSession=false;_districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeDockLoadingQa");
if(_lotWorld!=null)_lotWorld.gameObject.SetActive(false);EnsureDistrictWorld(district);SelectRegionTile(district.TileId);_terraformZoomLevel=DistrictZoomLevel.LOD0;SelectDistrictCategory("Select");Show(AppScreen.DistrictTerraform);
_terraformPanOffset=new Vector2(boatX,center.y-boat.PositionZ);_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=18;
var host=_districtWorld.GetComponentsInChildren<LotWorldController>().First();var origin=host.transform.TransformPoint(new Vector3(boat.PositionX,0,boat.PositionZ));var route=_districtWorld.FindDownstreamBoatRoute(origin,3.25f,10);
File.WriteAllText("/Users/joelinstrum/dev/CityForgeMCP/artifacts/behaviors/lumber-loading/v01/district-route.txt","Origin "+origin+" route "+(route==null?"NONE":string.Join(";",route)));
}
}}
#endif
