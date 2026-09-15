using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    // Coarse four-way routing with fine segment checks. Lots and water are impassable.
    public sealed class DistrictLaborNavigation
    {
        private const float Cell=4f;
        private readonly float width,depth;
        private readonly int columns,rows;
        private readonly Func<Vector2,bool> blocked;
        private readonly List<Rect> lots=new();
        public DistrictLaborNavigation(RegionCityTile d,Func<Vector2,bool> water)
        {
            width=DistrictScale.SizeMeters(d.Width);depth=DistrictScale.SizeMeters(d.Height);columns=Mathf.CeilToInt(width/Cell);rows=Mathf.CeilToInt(depth/Cell);blocked=water;
            foreach(var p in d.Lots??new())
            {
                var lot=LotContentCatalog.Read(p.LotId);if(lot==null)continue;
                var center=DistrictWorldController.DistrictLotCenterMeters(d,p,lot);
                float x=lot.LotWidthCells*LotMetricScale.MajorGridMeters,z=lot.LotDepthCells*LotMetricScale.MajorGridMeters;
                if(p.RotationQuarterTurns%2!=0)(x,z)=(z,x);
                lots.Add(new Rect(center.x-x/2-1,center.y-z/2-1,x+2,z+2));
            }
            if(lots.Count==0 && d.Founded && !string.IsNullOrEmpty(d.LotId))
            {
                var lot=LotContentCatalog.Read(d.LotId);if(lot!=null){float x=lot.LotWidthCells*LotMetricScale.MajorGridMeters,z=lot.LotDepthCells*LotMetricScale.MajorGridMeters;var c=DistrictWorldController.DistrictLotCenterMeters(d,d.FounderNormalizedX,d.FounderNormalizedY,x,z);lots.Add(new Rect(c.x-x/2-1,c.y-z/2-1,x+2,z+2));}
            }
        }
        public bool Walkable(Vector2 p)
        {
            if(Mathf.Abs(p.x)>width/2-1 || Mathf.Abs(p.y)>depth/2-1)return false;
            foreach(var lot in lots)if(lot.Contains(p))return false;
            return !blocked(new Vector2(p.x/width+.5f,p.y/depth+.5f));
        }
        private bool Segment(Vector2 a,Vector2 b)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)));
            for(int i=0;i<=steps;i++)if(!Walkable(Vector2.Lerp(a,b,(float)i/steps)))return false;
            return true;
        }
        private Vector2 Point(int id)=>new((id%columns+.5f)*Cell-width/2,(id/columns+.5f)*Cell-depth/2);
        private int Id(Vector2 p)=>Mathf.Clamp((int)((p.y+depth/2)/Cell),0,rows-1)*columns+Mathf.Clamp((int)((p.x+width/2)/Cell),0,columns-1);
        public List<Vector2> Route(Vector2 from,Vector2 to)
        {
            if(!Walkable(from)||!Walkable(to))return null;
            if(Segment(from,to))return new(){to};
            int start=Id(from),end=Id(to);if(!Segment(from,Point(start))||!Segment(Point(end),to))return null;
            var prev=new Dictionary<int,int>{{start,-1}};var queue=new Queue<int>();queue.Enqueue(start);
            while(queue.Count>0)
            {
                int id=queue.Dequeue();if(id==end)break;
                foreach(int next in new[]{id-1,id+1,id-columns,id+columns})
                {
                    if(next<0||next>=columns*rows||Math.Abs(next%columns-id%columns)+Math.Abs(next/columns-id/columns)!=1||prev.ContainsKey(next)||!Segment(Point(id),Point(next)))continue;
                    prev[next]=id;queue.Enqueue(next);
                }
            }
            if(!prev.ContainsKey(end))return null;
            var result=new List<Vector2>{to};for(int id=end;id!=-1;id=prev[id])result.Add(Point(id));result.Reverse();return result;
        }
        public Vector2? FindCamp(Vector2 preferred)
        {
            if(Walkable(preferred))return preferred;
            for(int ring=1;ring<Mathf.Max(columns,rows);ring++)
                for(int i=0;i<16;i++){float a=i*Mathf.PI/8;var p=preferred+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ring*Cell;if(Walkable(p))return p;}
            return null;
        }
    }
}
