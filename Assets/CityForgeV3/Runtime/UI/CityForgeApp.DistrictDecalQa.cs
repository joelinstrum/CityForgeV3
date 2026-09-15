#if UNITY_EDITOR
using System;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenSavedDistrictDecalQa()
        {
            if (_currentScreen != AppScreen.Splash || _hasOpenLot || _openRegion != null)
            {
                Debug.LogWarning("Saved district decal preview requires fresh splash; live work preserved.");
                return;
            }
            var candidates = RegionSaveStore.List()
                .Where(s => !s.RegionId.StartsWith("transient-"))
                .Select(s => RegionSaveStore.Load(s.RegionId)).Where(r => r != null)
                .SelectMany(r => r.Tiles.Select(t => new { Region = r, Tile = t }))
                .OrderByDescending(c => c.Tile.Flora?.Count ?? 0).ToArray();
            if (candidates.Length == 0) { Debug.LogWarning("No saved district available."); return; }
            var chosen = candidates[0];
            _openRegion = chosen.Region;
            _openRegionWasCreatedThisSession = false;
            EnsureDistrictWorld(chosen.Tile);
            SelectRegionTile(chosen.Tile.TileId);
            _terraformZoomLevel = DistrictZoomLevel.LOD1;
            // Inspect the saved grove, without planting a replacement fixture.
            var tree = chosen.Tile.Flora?.FirstOrDefault();
            if (tree != null)
            {
                var p = _districtWorld.GetComponentsInChildren<SpriteRenderer>()
                    .FirstOrDefault(s => s.name.StartsWith("District Flora —"));
                if (p != null) _terraformPanOffset = new Vector2(p.transform.localPosition.x, p.transform.localPosition.z);
            }
            _districtWorld.SetZoom(_terraformZoomLevel);
            _districtWorld.SetPan(_terraformPanOffset);
            Debug.Log($"SAVED DISTRICT DECAL PREVIEW region={chosen.Region.RegionId} tile={chosen.Tile.TileId} flora={chosen.Tile.Flora?.Count ?? 0}");
            CheckDistrictDecalReloadQa();
        }

        public void CheckDistrictDecalReloadQa()
        {
            if (_districtWorld == null || _openRegion == null) return;
            var current = FindSelectedRegionTile();
            var saved = RegionSaveStore.Load(_openRegion.RegionId)?.Tiles
                .FirstOrDefault(t => t.TileId == current.TileId);
            if (saved == null || JsonUtility.ToJson(saved) != JsonUtility.ToJson(current))
            {
                Debug.LogWarning("Saved reload check requires unchanged saved district; live edits preserved.");
                return;
            }
            var before = _districtWorld.GetComponentInChildren<DistrictGroundDecals>();
            var count = before?.PatchCount ?? 0;
            if (count == 0) throw new Exception("Default district decals missing");
            _districtWorld.Build(saved);
            _districtWorld.SetZoom(_terraformZoomLevel);
            _districtWorld.SetPan(_terraformPanOffset);
            // ClearWorld destroys old content at end of frame; inspect only the
            // newest content root to avoid counting retired flora and meshes.
            var decals = _districtWorld.GetComponentsInChildren<DistrictGroundDecals>().Last();
            if (decals.PatchCount != count) throw new Exception("Decal scatter changed on disk reload");
            var camera = _districtWorld.WorldCamera;
            var trees = decals.transform.parent.GetComponentsInChildren<SpriteRenderer>()
                .Where(s => s.name.StartsWith("District Flora —") && !s.name.Contains("stone")).ToArray();
            if ((saved.Flora?.Count ?? 0) > 0 && trees.Length == 0) throw new Exception("No saved trees restored");
            foreach (var tree in trees)
                if (Quaternion.Angle(tree.transform.rotation, camera.transform.rotation) > .01f)
                    throw new Exception("Saved tree not aligned with camera");
            Debug.Log($"DISTRICT SAVED RELOAD PASS decals={count} alignedTrees={trees.Length}");
        }
    }
}
#endif
