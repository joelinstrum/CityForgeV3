using CityForgeV3.World;
using UnityEngine;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private readonly DistrictUndoHistory _districtUndo = new();
        private RegionCityTile _districtUndoTile;
        private RegionSaveData _districtUndoRegion;
#if UNITY_EDITOR
        private string _districtUndoQaSaveRoot;
#endif
        private void EnsureDistrictUndo(RegionCityTile district)
        {
            if (_districtUndoTile == district && _districtUndoRegion == _openRegion) return;
            _districtUndoTile = district;
            _districtUndoRegion = _openRegion;
            _districtUndo.Reset(district == null ? null : JsonUtility.ToJson(district));
        }
        private void ClearDistrictUndo()
        {
            _districtUndoTile = null;
            _districtUndoRegion = null;
            _districtUndo.Reset(null);
        }
        private void SaveDistrictEdit()
        {
            var district = FindSelectedRegionTile();
            if (_currentScreen == AppScreen.DistrictTerraform && district != null &&
                district == _districtUndoTile && _openRegion == _districtUndoRegion)
                _districtUndo.Commit(JsonUtility.ToJson(district));
            PersistDistrictRegion();
        }
        private void PersistDistrictRegion()
        {
#if UNITY_EDITOR
            RegionSaveStore.Save(_openRegion, _districtUndoQaSaveRoot);
#else
            RegionSaveStore.Save(_openRegion);
#endif
        }
        private bool UndoDistrictEdit()
        {
            // Never restore objects out from under an unfinished pointer gesture.
            if (_currentScreen != AppScreen.DistrictTerraform || _districtMarqueeActive ||
                _districtSelectionDragActive || _districtFloraPainting ||
                _districtFloraPointerDown || _districtRoadPointerDown) return false;
            var district = FindSelectedRegionTile();
            if (district == null || district != _districtUndoTile ||
                _openRegion != _districtUndoRegion || !_districtUndo.TryUndo(out var snapshot)) return false;
            JsonUtility.FromJsonOverwrite(snapshot, district);
            if (DistrictLabor.State(district).Workers.Count > 0) SetDistrictSimulationPaused(true);
            _laborNavigation = null;
            _districtSelection.Clear();
            _selectedDistrictFloraInstanceId = "";
            _selectedDistrictLotInstanceId = "";
            _hoveredDistrictLotInstanceId = "";
            _hasSelectedDistrictRoad = false;
            _activeDistrictRandomFloraGroupId = "";
            _districtWorldCompositionKey = "";
            PersistDistrictRegion();
            // Rebuild from restored data through the normal saved-district path.
            EnsureDistrictWorld(district);
            Show(AppScreen.DistrictTerraform);
            Debug.Log($"DISTRICT UNDO restored; remaining={_districtUndo.Count}");
            return true;
        }
    }
}
