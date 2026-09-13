using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private int _districtFallDirection;
        private void AddDistrictHarvestControls(VisualElement flyout)
        {
            if (ActiveDistrictCategory != "Select") return;
            var directions = new[] { "East", "North", "West", "South" };
            var direction = new Button { text = "Fall: " + directions[_districtFallDirection], tooltip = "Choose a fall animation direction." };
            direction.clicked += () => { _districtFallDirection = (_districtFallDirection + 1) % 4; direction.text = "Fall: " + directions[_districtFallDirection]; };
            void Size(Button button)
            {
                button.style.height = 48;
                button.style.fontSize = 20;
                button.style.whiteSpace = WhiteSpace.Normal;
                button.style.marginTop = 4;
            }
            Size(direction);
            flyout.Add(direction);
            var fell = new Button(() => FellSelectedDistrictTrees()) { text = "Fell firs", tooltip = "Fell selected standing Cilician Fir trees. Ctrl+Z undoes this." };
            Size(fell); flyout.Add(fell);
            var clear = new Button(() => ClearSelectedFallenWood()) { text = "Clear wood", tooltip = "Prototype: remove selected fallen wood and leave stumps. Does not credit resources." };
            Size(clear); flyout.Add(clear);
        }
        private bool HarvestInputBusy => _districtMarqueeActive || _districtSelectionDragActive || _districtFloraPainting || _districtRoadPointerDown || _districtFloraPointerDown;
        private void FellSelectedDistrictTrees()
        {
            if (HarvestInputBusy) return;
            var district = FindSelectedRegionTile(); if (district == null) return;
            var ids = new HashSet<string>(_districtSelection.Where(s => s.Kind == DistrictSelectionKind.Flora).Select(s => s.Id));
            ids.RemoveWhere(id => !DistrictTreeHarvest.Fell(FindDistrictFlora(district,id), _districtFallDirection));
            if (ids.Count > 0) SaveAndPresentHarvest(district, ids);
        }
        // Worker hook: call once when chopping finishes, using the stable saved tree ID.
        public bool BeginDistrictTreeFall(string instanceId, int direction)
        {
            if (_currentScreen != AppScreen.DistrictTerraform || HarvestInputBusy) return false;
            var district = FindSelectedRegionTile();
            if (!DistrictTreeHarvest.Fell(FindDistrictFlora(district,instanceId),direction)) return false;
            SaveAndPresentHarvest(district,new HashSet<string> { instanceId }); return true;
        }
        // Returns wood removed from this tree, not resources delivered to a stockpile.
        public int TakeDistrictTreeWood(string instanceId, int amount)
        {
            if (_currentScreen != AppScreen.DistrictTerraform || HarvestInputBusy || _districtWorld.IsTreeFalling(instanceId)) return 0;
            var district = FindSelectedRegionTile();
            var taken = DistrictTreeHarvest.TakeWood(FindDistrictFlora(district,instanceId),amount);
            if (taken > 0) SaveAndPresentHarvest(district,null,new HashSet<string> { instanceId });
            return taken;
        }
        private void ClearSelectedFallenWood()
        {
            if (HarvestInputBusy) return;
            var district = FindSelectedRegionTile(); if (district == null) return;
            var changed = new HashSet<string>();
            foreach (var selected in _districtSelection.Where(s => s.Kind == DistrictSelectionKind.Flora))
                if (!_districtWorld.IsTreeFalling(selected.Id))
                    if (DistrictTreeHarvest.TakeWood(FindDistrictFlora(district,selected.Id),int.MaxValue) > 0) changed.Add(selected.Id);
            if (changed.Count > 0) SaveAndPresentHarvest(district,null,changed);
        }
        private void SaveAndPresentHarvest(RegionCityTile district, HashSet<string> falling, HashSet<string> changed = null)
        {
            // Store the durable end state immediately. Loading mid-fall never replays/yields twice.
            SaveDistrictEdit();
            _districtWorld.RefreshHarvestTrees(district, falling ?? changed);
            if (falling != null) _districtWorld.PlayTreeFalls(district,falling);
            _districtWorld.ShowDistrictSelection(district,_districtSelection);
            _districtWorldCompositionKey = DistrictCompositionKey(district);
        }
    }
}
