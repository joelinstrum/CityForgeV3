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
        readonly Dictionary<string,int> slots=new();
        bool slotsDirty;
        object source;int count;
        const float CellSize=32;
        DistrictHarvestIndex(RegionCityTile d){district=d;Rebuild();}
        public static DistrictHarvestIndex For(RegionCityTile d)
        {
            var index=Cache.GetValue(d,key=>new DistrictHarvestIndex(key));
            // Bulk clearing/generation and deserialization are infrequent edit boundaries.
            if(!ReferenceEquals(index.source,d.Flora)||index.count!=(d.Flora?.Count??0)||index.slotsDirty)index.Rebuild();
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
                if(!index.slots.ContainsKey(tree.InstanceId))index.slots[tree.InstanceId]=expected-1;
            }
        }
        public static void Removed(RegionCityTile d,string id)
        {
            if(Cache.TryGetValue(d,out var index))
            {
                int expected=index.count-(index.byId.ContainsKey(id)?1:0);
                if(expected!=(d.Flora?.Count??0)){Cache.Remove(d);return;}
                index.Remove(id);index.count=expected;index.slotsDirty=true;
            }
        }
        static Vector2Int Cell(Vector2 p)=>new(Mathf.FloorToInt(p.x/CellSize),Mathf.FloorToInt(p.y/CellSize));
        static bool Available(PlacedDistrictFlora t)=>DistrictTreeHarvest.CanFell(t)||t.FloraId=="cilician-fir"&&t.HarvestState==DistrictTreeHarvestState.Fallen&&t.RemainingWood>0;
        void Rebuild(){source=district.Flora;count=district.Flora?.Count??0;byId.Clear();cells.Clear();membership.Clear();slots.Clear();slotsDirty=false;
            for(int i=0;i<count;i++){var tree=district.Flora[i];Update(tree);if(tree!=null&&!string.IsNullOrEmpty(tree.InstanceId))slots[tree.InstanceId]=i;}}
        // Swap-remove keeps small edits proportional to the affected flora,
        // without scanning or shifting the district's saved list.
        public bool RemoveFlora(string id)
        {
            if(slotsDirty)Rebuild();
            if(id==null||!slots.TryGetValue(id,out var slot))return false;
            int last=district.Flora.Count-1;var tail=district.Flora[last];
            district.Flora[slot]=tail;district.Flora.RemoveAt(last);
            if(slot!=last&&tail!=null&&!string.IsNullOrEmpty(tail.InstanceId))slots[tail.InstanceId]=slot;
            slots.Remove(id);Remove(id);count=district.Flora.Count;return true;
        }
        public List<string> ClearFootprint(Rect footprint)
        {
            var candidates=new List<string>();
            foreach(var tree in NearbyFlora(footprint.center,footprint.size.magnitude*.5f))
                if(footprint.Contains(DistrictLabor.TreePoint(district,tree)))candidates.Add(tree.InstanceId);
            foreach(var id in candidates)RemoveFlora(id);
            return candidates;
        }
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
