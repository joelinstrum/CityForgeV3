using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

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
        // Record an editor undo boundary in memory. Only the Save button writes to disk.
        private void SaveDistrictEdit()
        {
            var district = FindSelectedRegionTile();
            if (_currentScreen == AppScreen.DistrictTerraform && district != null &&
                district == _districtUndoTile && _openRegion == _districtUndoRegion)
                _districtUndo.Commit(JsonUtility.ToJson(district));
        }
        private void PersistDistrictRegion()
        {
#if UNITY_EDITOR
            RegionSaveStore.Save(_openRegion, string.IsNullOrWhiteSpace(_districtUndoQaSaveRoot) ? null : _districtUndoQaSaveRoot);
#else
            RegionSaveStore.Save(_openRegion);
#endif
        }
        private Button CreateRegionSaveButton(string name, string variant)
        {
            Button button = null;
            button = CfButton.Create("SAVE", () =>
            {
                try
                {
                    PersistDistrictRegion();
                    CfMapChrome.SetCaption(button, "SAVED");
                    button.tooltip = "Region and all districts saved.";
                    if (name == "district-save-button")
                        ShowDistrictNotice("District Saved");
                    button.schedule.Execute(() => { CfMapChrome.SetCaption(button, "SAVE"); button.tooltip = "Save region and all districts. Changes are not saved automatically."; }).ExecuteLater(2000);
                }
                catch (System.Exception error)
                {
                    CfMapChrome.SetCaption(button, "SAVE FAILED");
                    button.tooltip = "Could not save: " + error.Message + " — click to retry.";
                    Debug.LogException(error);
                }
            }, true, variant);
            button.name = name;
            button.tooltip = "Save region and all districts. Changes are not saved automatically.";
            return button;
        }
        private bool UndoDistrictEdit()
        {
            // Never restore objects out from under an unfinished pointer gesture.
            if (_lotNudge != null || _currentScreen != AppScreen.DistrictTerraform || _districtMarqueeActive ||
                _districtSelectionDragActive || _districtFloraPainting ||
                _districtFloraPointerDown || _districtRoadPointerDown || _riverSculptPointer >= 0) return false;
            var district = FindSelectedRegionTile();
            if (district == null || district != _districtUndoTile ||
                _openRegion != _districtUndoRegion || !_districtUndo.TryUndo(out var snapshot)) return false;
            JsonUtility.FromJsonOverwrite(snapshot, district);
            DistrictHarvestIndex.Invalidate(district);
            if (DistrictLabor.State(district).Workers.Count > 0) SetDistrictSimulationPaused(true);
            _laborNavigation = null;
            _districtSelection.Clear();
            _selectedDistrictFloraInstanceId = "";
            _selectedDistrictLotInstanceId = "";
            _hoveredDistrictLotInstanceId = "";
            _hasSelectedDistrictRoad = false;
            _activeDistrictRandomFloraGroupId = "";
            _districtWorldCompositionKey = "";
            // Rebuild from restored data through the normal saved-district path.
            EnsureDistrictWorld(district);
            Show(AppScreen.DistrictTerraform);
            Debug.Log($"DISTRICT UNDO restored; remaining={_districtUndo.Count}");
            return true;
        }
    }
}
