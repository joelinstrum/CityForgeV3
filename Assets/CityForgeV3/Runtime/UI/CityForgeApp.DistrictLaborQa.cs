#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenDistrictLaborQa()
        {
            OpenDistrictHarvestQa();if(_districtUndoQaSaveRoot==null)return;
            var d=FindSelectedRegionTile();var tree=d.Flora.FirstOrDefault(DistrictTreeHarvest.CanFell);if(tree==null)return;
            d.Labor=new DistrictLaborState();_laborNavigation=null;
            var preferred=DistrictLabor.TreePoint(d,tree)+new Vector2(0,-7);
            var camp=LaborNavigation(d).FindCamp(preferred);if(!camp.HasValue)throw new Exception("No QA camp");
            d.Labor.Camp=camp.Value;d.Labor.CampPlaced=true;
            ClearDistrictUndo();EnsureDistrictUndo(d);SaveDistrictEdit();
            _terraformPanOffset=DistrictLabor.TreePoint(d,tree);_terraformZoomLevel=DistrictZoomLevel.LOD0;
            _districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=15f;
            _root.RegisterCallback<PointerDownEvent>(e=>Debug.Log($"LABOR POINTER position={e.position} target={e.target}"),TrickleDown.TrickleDown);
            ComposeDistrictLaborModal();Debug.Log("LABOR QA ready with saved district copy at "+_districtUndoQaSaveRoot);
        }
        public void LogDistrictLaborQa()
        {
            if(_districtUndoQaSaveRoot==null)return;var d=FindSelectedRegionTile();
            Debug.Log("LABOR QA "+JsonUtility.ToJson(d.Labor));
            Debug.Log($"LABOR MOUSE {Input.mousePosition} screen={UnityEngine.Screen.width}x{UnityEngine.Screen.height}");
            foreach(var w in d.Labor.Workers)Debug.Log($"LABOR QA worker={w.Id} state={w.Activity} pos={w.Position}");
            foreach(var name in new[]{"district-labor-menu","labor-axemen-count","labor-assign"}){var e=_root.Q(name);if(e!=null)Debug.Log($"LABOR UI {name} bounds={e.worldBound}");}
        }
        public void ReloadDistrictLaborQa()
        {
            if(_districtUndoQaSaveRoot==null)return;var d=FindSelectedRegionTile();SaveDistrictEdit();
            var before=JsonUtility.ToJson(d);var loaded=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot);var next=loaded.Tiles.Find(t=>t.TileId==d.TileId);
            if(JsonUtility.ToJson(next)!=before)throw new Exception("Labor saved district differs");
            _openRegion=loaded;ClearDistrictUndo();_laborNavigation=null;_districtWorld.Build(next);_districtWorldCompositionKey=DistrictCompositionKey(next);Show(AppScreen.DistrictTerraform);_districtWorld.SetPan(next.Labor.Camp);_districtWorld.WorldCamera.orthographicSize=15f;
            Debug.Log($"LABOR QA RELOAD PASS workers={next.Labor.Workers.Count} wood={next.Labor.Wood} treasury={next.Treasury}");
        }
        public void CheckDistrictWoodHudQa()
        {
            if(_districtUndoQaSaveRoot==null)return;
            var d=FindSelectedRegionTile();RemoveDocumentModal();SetDistrictSimulationPaused(true);
            var tree=d.Flora.FirstOrDefault(DistrictTreeHarvest.CanFell);if(tree==null)throw new Exception("No saved tree for resource QA");
            int before=d.Labor.Wood;
            ComposeDistrictResourcesModal();
            if(!BeginDistrictTreeFall(tree.InstanceId,0))throw new Exception("Saved tree fall failed");
            if(d.Labor.Wood!=before || !_root.Q<Label>("district-resource-wood").text.Contains((before).ToString("N0")))throw new Exception("Felling incorrectly changed lumber tally");
            ReloadDistrictLaborQa();d=FindSelectedRegionTile();ComposeDistrictResourcesModal();
            if(d.Labor.Wood!=before || !_root.Q<Label>("district-resource-wood").text.Contains((before).ToString("N0")))throw new Exception("HUD tally did not survive reload");
            Debug.Log($"WOOD HUD QA PASS savedTree={tree.InstanceId} before={before} after={d.Labor.Wood} reload=true");
        }
        public void InspectDistrictWorkerQa()
        {
            if(_districtUndoQaSaveRoot==null)return;var d=FindSelectedRegionTile();var worker=d.Labor.Workers.FirstOrDefault();if(worker==null)return;
            _terraformPanOffset=worker.Position;_districtWorld.SetPan(worker.Position);_districtWorld.WorldCamera.orthographicSize=4;
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
                Debug.Log($"LABOR RENDER {renderer.name} bounds={renderer.bounds} material={renderer.sharedMaterial?.name} texture={renderer.sharedMaterial?.mainTexture?.name}");
        }
        public void AssignDistrictLaborQa()
        {
            if(_districtUndoQaSaveRoot==null)return;
            var count=_root.Q<IntegerField>("labor-axemen-count");if(count==null){ComposeDistrictLaborModal();count=_root.Q<IntegerField>("labor-axemen-count");}
            count.value=2;var button=_root.Q<Button>("labor-assign");button.Focus();using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}
            _districtWorld.WorldCamera.orthographicSize=15f;
        }
        public void ShowDistrictLaborQaModal(){if(_districtUndoQaSaveRoot!=null)ComposeDistrictLaborModal();}
    }
}
#endif
