#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CityForgeV3.World;
using CityForgeV3.Buildings3D;
namespace CityForgeV3.UI
{
 public sealed partial class CityForgeApp
 {
  public void OpenDryGoodsDistrictQa()
  {
   var lot=LotSaveStore.List().Select(x=>LotSaveStore.Read(x.LotId)).FirstOrDefault(x=>x!=null && x.Buildings3D!=null && x.Buildings3D.Any(b=>b.AssetId=="dry-goods-v05"));
   if(lot==null)throw new Exception("No saved Dry Goods lot found");
   var district=new RegionCityTile{TileId="dry-goods-light-review",Name="Dry Goods afternoon review",Width=2,Height=2,TimeOfDay=TimeOfDayPreset.Afternoon};
   district.Lots.Add(new PlacedDistrictLot{InstanceId="dry-goods-review",LotId=lot.LotId,GridX=20,GridZ=20});
   _openRegion=new RegionSaveData{RegionId="transient-dry-goods-v06",Name="Dry Goods material and sun review",Tiles=new List<RegionCityTile>{district}};
   _openRegionWasCreatedThisSession=false;_districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeDryGoodsV06Qa");
   if(_lotWorld!=null)_lotWorld.gameObject.SetActive(false);
   EnsureDistrictWorld(district);SelectRegionTile(district.TileId);_terraformZoomLevel=DistrictZoomLevel.LOD0;SelectDistrictCategory("Select");Show(AppScreen.DistrictTerraform);
   _districtWorld.SetTimeOfDay(TimeOfDayPreset.Afternoon);FocusDryGoodsDistrictQa();
  }
  public void SetDryGoodsDistrictQaNight(bool night){_districtWorld.SetTimeOfDay(night?TimeOfDayPreset.Night:TimeOfDayPreset.Afternoon);FocusDryGoodsDistrictQa();}
  public void FocusDryGoodsDistrictQa()
  {
   var package=_districtWorld.GetComponentsInChildren<Building3DPackageInstance>().First(x=>x.Package!=null && x.Package.AssetId=="dry-goods-v05");
   var bounds=package.GetComponentsInChildren<Renderer>().First().bounds;foreach(var r in package.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
   _terraformPanOffset=new Vector2(bounds.center.x,bounds.center.z);_districtWorld.SetPan(_terraformPanOffset);
   var camera=_districtWorld.WorldCamera;camera.orthographicSize=12;
  }
 }
}
#endif
