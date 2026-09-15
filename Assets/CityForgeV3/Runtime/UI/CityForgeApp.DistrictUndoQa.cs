#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void CheckDistrictUndoQa()
        {
            if (_currentScreen != AppScreen.DistrictTerraform || _districtWorld == null ||
                _districtMarqueeActive || _districtFloraPainting || _districtSelectionDragActive) return;
            var originalRegion = _openRegion;
            var originalTile = FindSelectedRegionTile();
            if (originalTile == null) return;
            var originalSelection = _districtSelection.ToArray();
            var root = Path.Combine(Path.GetTempPath(), "CityForgeUndoQa-" + Guid.NewGuid().ToString("N"));
            try
            {
                // Work on a deep copy and a temporary save folder. Never edit Joe's save.
                _openRegion = JsonUtility.FromJson<RegionSaveData>(JsonUtility.ToJson(originalRegion));
                _districtUndoQaSaveRoot = root;
                var tile = FindSelectedRegionTile();
                var before = JsonUtility.ToJson(tile);
                EnsureDistrictUndo(tile);
                _districtSelection.Clear();
                if (tile.Flora.Count > 0) _districtSelection.Add(new(DistrictSelectionKind.Flora, tile.Flora[0].InstanceId));
                if (tile.Lots.Count > 0) _districtSelection.Add(new(DistrictSelectionKind.Lot, tile.Lots[0].InstanceId));
                if (tile.Rivers.Count > 0) _districtSelection.Add(new(DistrictSelectionKind.River, tile.Rivers[0].InstanceId));
                if (_districtSelection.Count == 0) throw new Exception("QA requires populated district");
                if (!DeleteDistrictSelection() || _districtUndo.Count != 1) throw new Exception("Deletion was not one undo step");
                if (!UndoDistrictEdit() || JsonUtility.ToJson(tile) != before) throw new Exception("Undo did not restore exact district");
                var saved = RegionSaveStore.Load(_openRegion.RegionId, root).Tiles.Find(t => t.TileId == tile.TileId);
                if (JsonUtility.ToJson(saved) != before) throw new Exception("Saved undo reload differs");
                if (UndoDistrictEdit()) throw new Exception("Empty undo mutated district");
                SaveDistrictEdit();
                if (_districtUndo.Count != 0) throw new Exception("No-op save consumed history");
                Debug.Log("DISTRICT UNDO QA PASS mixedDelete=true exactRestore=true savedReload=true emptySafe=true noOp=true");
            }
            finally
            {
                _districtUndoQaSaveRoot = null;
                _openRegion = originalRegion;
                ClearDistrictUndo();
                _districtSelection.Clear(); _districtSelection.AddRange(originalSelection);
                _districtWorldCompositionKey = "";
                EnsureDistrictWorld(originalTile);
                Show(AppScreen.DistrictTerraform);
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
#endif
