using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    // Reusable district-local dirty matrix. Multiple marks coalesce into one update per chunk.
    public sealed class DistrictDirtyGrid
    {
        public readonly Rect Bounds;
        public readonly float CellSize;
        public readonly int Columns,Rows;
        readonly bool[] dirty;
        public DistrictDirtyGrid(Rect bounds,float cellSize)
        {
            Bounds=bounds;CellSize=Mathf.Max(1,cellSize);
            Columns=Mathf.Max(1,Mathf.CeilToInt(bounds.width/CellSize));Rows=Mathf.Max(1,Mathf.CeilToInt(bounds.height/CellSize));
            dirty=new bool[Columns*Rows];
        }
        public void MarkAll(){for(int i=0;i<dirty.Length;i++)dirty[i]=true;}
        public void Mark(Rect area)
        {
            if(area.xMax<Bounds.xMin || area.xMin>Bounds.xMax || area.yMax<Bounds.yMin || area.yMin>Bounds.yMax)return;
            int x0=Mathf.Clamp(Mathf.FloorToInt((area.xMin-Bounds.xMin)/CellSize),0,Columns-1),x1=Mathf.Clamp(Mathf.FloorToInt((area.xMax-Bounds.xMin)/CellSize),0,Columns-1);
            int z0=Mathf.Clamp(Mathf.FloorToInt((area.yMin-Bounds.yMin)/CellSize),0,Rows-1),z1=Mathf.Clamp(Mathf.FloorToInt((area.yMax-Bounds.yMin)/CellSize),0,Rows-1);
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)dirty[z*Columns+x]=true;
        }
        public Rect CellBounds(Vector2Int cell)=>Rect.MinMaxRect(Bounds.xMin+cell.x*CellSize,Bounds.yMin+cell.y*CellSize,
            Mathf.Min(Bounds.xMax,Bounds.xMin+(cell.x+1)*CellSize),Mathf.Min(Bounds.yMax,Bounds.yMin+(cell.y+1)*CellSize));
        public List<Vector2Int> Consume()
        {
            var result=new List<Vector2Int>();
            for(int i=0;i<dirty.Length;i++)if(dirty[i]){result.Add(new Vector2Int(i%Columns,i/Columns));dirty[i]=false;}
            return result;
        }
        public static Rect Expand(Rect area,float amount)=>Rect.MinMaxRect(area.xMin-amount,area.yMin-amount,area.xMax+amount,area.yMax+amount);
    }
}
