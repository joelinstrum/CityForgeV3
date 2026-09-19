using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    // Shared broad-phase index for immutable per-operation geometry. Exact checks
    // still decide coverage, so bucket size cannot change the result.
    public sealed class DistrictSpatialIndex<T>
    {
        readonly float cellSize;
        readonly Dictionary<Vector2Int,List<T>> buckets=new();
        public DistrictSpatialIndex(float cellSize=128){this.cellSize=Mathf.Max(1,cellSize);}
        public void Clear()=>buckets.Clear();
        public void Add(Rect bounds,T value,bool unique=false)
        {
            for(int x=Mathf.FloorToInt(bounds.xMin/cellSize);x<=Mathf.FloorToInt(bounds.xMax/cellSize);x++)
            for(int y=Mathf.FloorToInt(bounds.yMin/cellSize);y<=Mathf.FloorToInt(bounds.yMax/cellSize);y++)
            {
                var key=new Vector2Int(x,y);if(!buckets.TryGetValue(key,out var values))buckets[key]=values=new();if(!unique || !values.Contains(value))values.Add(value);
            }
        }
        public void Remove(Rect bounds,T value)
        {
            for(int x=Mathf.FloorToInt(bounds.xMin/cellSize);x<=Mathf.FloorToInt(bounds.xMax/cellSize);x++)
            for(int y=Mathf.FloorToInt(bounds.yMin/cellSize);y<=Mathf.FloorToInt(bounds.yMax/cellSize);y++)
            {
                var key=new Vector2Int(x,y);if(!buckets.TryGetValue(key,out var values))continue;
                values.Remove(value);if(values.Count==0)buckets.Remove(key);
            }
        }
        public IReadOnlyList<T> Query(Vector2 point)=>buckets.TryGetValue(new Vector2Int(Mathf.FloorToInt(point.x/cellSize),Mathf.FloorToInt(point.y/cellSize)),out var values)?values:Array.Empty<T>();
        public void QueryBounds(Rect bounds, HashSet<T> result)
        {
            result.Clear();
            for (int x = Mathf.FloorToInt(bounds.xMin / cellSize);
                 x <= Mathf.FloorToInt(bounds.xMax / cellSize); x++)
            for (int y = Mathf.FloorToInt(bounds.yMin / cellSize);
                 y <= Mathf.FloorToInt(bounds.yMax / cellSize); y++)
                if (buckets.TryGetValue(new Vector2Int(x, y), out var values))
                    foreach (var value in values)
                        result.Add(value);
        }
    }
}
