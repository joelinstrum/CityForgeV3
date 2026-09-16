using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace CityForgeV3.World
{
    // Runtime-only shared district index. Saved flora remains the source of truth.
    public sealed class DistrictHarvestIndex
    {
        static readonly ConditionalWeakTable<RegionCityTile,DistrictHarvestIndex> Cache=new();
        readonly RegionCityTile district;
        readonly Dictionary<string,PlacedDistrictFlora> byId=new();
        readonly Dictionary<Vector2Int,HashSet<string>> cells=new();
        readonly Dictionary<string,Vector2Int> membership=new();
        object source;int count;
        const float CellSize=32;
        DistrictHarvestIndex(RegionCityTile d){district=d;Rebuild();}
        public static DistrictHarvestIndex For(RegionCityTile d)
        {
            var index=Cache.GetValue(d,key=>new DistrictHarvestIndex(key));
            // Bulk clearing/generation and deserialization are infrequent edit boundaries.
            if(!ReferenceEquals(index.source,d.Flora)||index.count!=(d.Flora?.Count??0))index.Rebuild();
            return index;
        }
        public static void Invalidate(RegionCityTile d){if(d!=null)Cache.Remove(d);}
        public static void Changed(RegionCityTile d,PlacedDistrictFlora tree)
        {
            if(tree==null)return;
            if(Cache.TryGetValue(d,out var index))
            {
                int expected=index.count+(index.byId.ContainsKey(tree.InstanceId)?0:1);
                if(!ReferenceEquals(index.source,d.Flora)||expected!=(d.Flora?.Count??0)){Cache.Remove(d);return;}
                index.Update(tree);index.count=expected;
            }
        }
        public static void Removed(RegionCityTile d,string id)
        {
            if(Cache.TryGetValue(d,out var index))
            {
                int expected=index.count-(index.byId.ContainsKey(id)?1:0);
                if(expected!=(d.Flora?.Count??0)){Cache.Remove(d);return;}
                index.Remove(id);index.count=expected;
            }
        }
        static Vector2Int Cell(Vector2 p)=>new(Mathf.FloorToInt(p.x/CellSize),Mathf.FloorToInt(p.y/CellSize));
        static bool Available(PlacedDistrictFlora t)=>DistrictTreeHarvest.CanFell(t)||t.FloraId=="cilician-fir"&&t.HarvestState==DistrictTreeHarvestState.Fallen&&t.RemainingWood>0;
        void Rebuild(){source=district.Flora;count=district.Flora?.Count??0;byId.Clear();cells.Clear();membership.Clear();foreach(var tree in district.Flora??new())Update(tree);}
        void Remove(string id)
        {
            if(membership.TryGetValue(id,out var cell)){cells[cell].Remove(id);if(cells[cell].Count==0)cells.Remove(cell);membership.Remove(id);}
            byId.Remove(id);
        }
        public void Update(PlacedDistrictFlora tree)
        {
            if(tree==null||string.IsNullOrEmpty(tree.InstanceId))return;
            Remove(tree.InstanceId);byId[tree.InstanceId]=tree;
            // Keep all flora indexed; harvest queries filter availability,
            // while wildlife can query non-harvestable mountain species too.
            var cell=Cell(DistrictLabor.TreePoint(district,tree));
            if(!cells.TryGetValue(cell,out var ids))cells[cell]=ids=new();
            ids.Add(tree.InstanceId);membership[tree.InstanceId]=cell;
        }
        public PlacedDistrictFlora Find(string id)=>id!=null&&byId.TryGetValue(id,out var tree)?tree:null;
        public IEnumerable<PlacedDistrictFlora> Nearby(Vector2 center,float radius) =>
            NearbyFlora(center,radius).Where(Available);
        public IEnumerable<PlacedDistrictFlora> NearbyFlora(Vector2 center,float radius)
        {
            var min=Cell(center-Vector2.one*radius);var max=Cell(center+Vector2.one*radius);
            for(int z=min.y;z<=max.y;z++)for(int x=min.x;x<=max.x;x++)
                if(cells.TryGetValue(new Vector2Int(x,z),out var ids))foreach(var id in ids)
                {var tree=byId[id];if((DistrictLabor.TreePoint(district,tree)-center).sqrMagnitude<=radius*radius)yield return tree;}
        }
    }
}
