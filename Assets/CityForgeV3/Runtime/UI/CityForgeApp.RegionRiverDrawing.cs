using System;
using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        void CommitDrawnRegionRiver()
        {
            var path=RegionRiverDrawing.Create(_pikeStroke,_riverSize,_openRegion);
            if(path==null)return;
            var paths=new List<RegionRiverPath>{path};
            var conflict=FindRegionRiverBuildingConflict(_openRegion,paths);
            if(conflict!=null)
            {
                _pikeHint.text="River crosses a building in "+conflict+". Draw another route, or press Esc to cancel.";
                return;
            }
            var previousPaths=_openRegion.RiverPaths;
            var rivers=_openRegion.Tiles.ToDictionary(t=>t,t=>t.Rivers);
            var roads=_openRegion.Tiles.ToDictionary(t=>t,t=>t.Roads);
            try
            {
                _openRegion.RiverPaths=new List<RegionRiverPath>(previousPaths ?? new()){path};
                var waterRegion=new RegionSaveData();
                foreach(var tile in _openRegion.Tiles)
                {
                    var sections=RegionRiverGenerator.Sections(tile,paths);
                    tile.Rivers=new List<PlacedDistrictRiver>(tile.Rivers ?? new());tile.Rivers.AddRange(sections);
                    waterRegion.Tiles.Add(new RegionCityTile{X=tile.X,Y=tile.Y,Width=tile.Width,Height=tile.Height,Rivers=sections});
                }
                var water=new NationalPikePlacement.WaterMask(waterRegion);
                foreach(var tile in _openRegion.Tiles)
                {
                    // Until bridges exist, remove only road tiles covered by the new water.
                    var kept=(tile.Roads ?? new()).Where(r=>r==null || !water.Contains(
                        new Vector2(tile.X*640+(r.GridX+.5f)*10,tile.Y*640+(r.GridZ+.5f)*10),7.1f)).ToList();
                    if(kept.Count==(tile.Roads?.Count??0))continue;
                    // Repair copies so a failed save also restores original junctions.
                    tile.Roads=kept.Select(r=>r==null?null:JsonUtility.FromJson<PlacedRoadPiece>(JsonUtility.ToJson(r))).ToList();
                    DistrictRoadPlacementModel.Repair(tile.Roads);
                }
#if UNITY_EDITOR
                RegionSaveStore.Save(_openRegion,_mapQaActive?System.IO.Path.Combine(System.IO.Path.GetTempPath(),"CityForgeMapLayersQa"):null);
#else
                RegionSaveStore.Save(_openRegion);
#endif
            }
            catch(Exception ex)
            {
                _openRegion.RiverPaths=previousPaths;
                foreach(var tile in _openRegion.Tiles){tile.Rivers=rivers[tile];tile.Roads=roads[tile];}
                _pikeHint.text="Could not save river: "+ex.Message+". Draw again to retry, or press Esc to cancel.";
                return;
            }
            _districtWorldCompositionKey="";
            var scroll=_root.Q<ScrollView>("region-map-scroll");
            if(scroll!=null){_regionMapScrollOffset=scroll.scrollOffset;_regionMapScrollInitialized=true;}
            var size=_riverSize;
            var pencilLeft=_pikePencil.style.left;var pencilTop=_pikePencil.style.top;
            CancelNationalPike();Show(AppScreen.RegionEditor);BeginRegionRiver(size);
            _pikePencil.style.left=pencilLeft;_pikePencil.style.top=pencilTop;
            _pikePencil.style.display=DisplayStyle.Flex;
        }
    }
}
