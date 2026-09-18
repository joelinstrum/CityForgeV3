#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using CityForgeV3.World;
using UnityEngine;
namespace CityForgeV3.UI
{
 public sealed partial class CityForgeApp
 {
  private string _harvestQaId;
  public void OpenDistrictHarvestQa()
  {
   if(_currentScreen!=AppScreen.MainMenu || _openRegion!=null){Debug.LogWarning("Harvest QA needs fresh main menu; live work preserved.");return;}
   OpenSavedDistrictSelectionQa();
   var saved=FindSelectedRegionTile();
   var tree=saved?.Flora.FirstOrDefault(t=>DistrictTreeHarvest.CanFell(t));
   if(tree==null){Debug.LogWarning("No standing saved Cilician Fir found; preview preserved.");return;}
   _openRegion=JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(_openRegion));
   _districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeHarvestQa-"+Guid.NewGuid().ToString("N"));
   _harvestQaId=tree.InstanceId;
   ClearDistrictUndo();
   var district=FindSelectedRegionTile();
   _terraformPanOffset=new Vector2((tree.NormalizedX-.5f)*DistrictScale.SizeMeters(district.Width),(tree.NormalizedZ-.5f)*DistrictScale.SizeMeters(district.Height));
   _terraformZoomLevel=DistrictZoomLevel.LOD0;
   EnsureDistrictWorld(district);SelectDistrictCategory("Select");
   _districtSelection.Clear();_districtSelection.Add(new(DistrictSelectionKind.Flora,_harvestQaId));
   Show(AppScreen.DistrictTerraform);_districtWorld.WorldCamera.orthographicSize=12f;
   Debug.Log("HARVEST QA copied saved tree="+_harvestQaId+" saves isolated to temporary folder");
  }
  public void IsolateDistrictHarvestQa()
  {
   if(_harvestQaId==null || _districtUndoQaSaveRoot==null)return;
   var d=FindSelectedRegionTile();
   d.Flora.RemoveAll(t=>t.InstanceId!=_harvestQaId);
   ClearDistrictUndo();_districtWorld.RefreshFlora(d);_districtWorldCompositionKey=DistrictCompositionKey(d);
   Show(AppScreen.DistrictTerraform);_districtWorld.WorldCamera.orthographicSize=12f;
   Debug.Log("HARVEST QA isolated existing saved tree in temporary copy only");
  }
  public void FallDistrictHarvestQa(){if(_harvestQaId!=null && _districtUndoQaSaveRoot!=null)BeginDistrictTreeFall(_harvestQaId,_districtFallDirection);}
  public void ClearDistrictHarvestQa(){if(_harvestQaId!=null && _districtUndoQaSaveRoot!=null)TakeDistrictTreeWood(_harvestQaId,int.MaxValue);}
  public void CheckDistrictHarvestQa(){if(_harvestQaId!=null && _districtUndoQaSaveRoot!=null)StartCoroutine(CheckHarvestRoutine());}
  private IEnumerator CheckHarvestRoutine()
  {
   var district=FindSelectedRegionTile();var before=JsonUtility.ToJson(district);
   if(!BeginDistrictTreeFall(_harvestQaId,1))throw new Exception("Could not fell QA tree");
   if(TakeDistrictTreeWood(_harvestQaId,1)!=0)throw new Exception("Pickup allowed during fall");
   yield return new WaitForSecondsRealtime(1.7f);
   if(TakeDistrictTreeWood(_harvestQaId,int.MaxValue)!=DistrictTreeHarvest.PrototypeWoodYield)throw new Exception("Incorrect wood pickup");
   var loaded=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot).Tiles.Find(t=>t.TileId==district.TileId);
   if(loaded.Flora.Find(t=>t.InstanceId==_harvestQaId).HarvestState!=DistrictTreeHarvestState.Stump)throw new Exception("Stump did not survive save/load");
   _districtWorld.Build(loaded); // Exercise actual saved-state presentation, not just JSON.
   if(!UndoDistrictEdit() || FindDistrictFlora(district,_harvestQaId).HarvestState!=DistrictTreeHarvestState.Fallen)throw new Exception("Undo stump failed");
   if(!UndoDistrictEdit() || JsonUtility.ToJson(district)!=before)throw new Exception("Undo fall failed");
   _districtSelection.Add(new(DistrictSelectionKind.Flora,_harvestQaId));_districtWorld.ShowDistrictSelection(district,_districtSelection);
   _districtWorld.WorldCamera.orthographicSize=12f;
   Debug.Log("HARVEST QA PASS savedStumpReload=true pickupBlockedDuringFall=true undoStump=true undoFall=true originalDistrictExact=true");
  }
 }
}
#endif
