using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    // Derived state only. The region save remains the authority; workers do not use this cache.
    // Snapshots store geometry values, never mutable source-list references.
    public sealed class DistrictSurfaceCache
    {
        HashSet<(Vector2 a,Vector2 b,float width,int depth)> rivers=new();
        HashSet<Rect> pads=new();
        string terrain;
        RegionCityTile owner;
        public int Revision {get;private set;}
        public sealed class Changes
        {
            public bool Full;
            public readonly List<Rect> Areas=new();
            public bool Any=>Full || Areas.Count>0;
        }
        public Changes Update(RegionCityTile district)
        {
            float width=DistrictScale.SizeMeters(district.Width),depth=DistrictScale.SizeMeters(district.Height);
            Vector2 Point(DistrictRiverPoint p)=>new((p.X-.5f)*width,(p.Z-.5f)*depth);
            var nextRivers=new HashSet<(Vector2 a,Vector2 b,float width,int depth)>();
            foreach(var river in district.Rivers ?? new())
                if(river?.Points!=null)for(int i=1;i<river.Points.Count;i++)nextRivers.Add((Point(river.Points[i-1]),Point(river.Points[i]),river.WidthMeters,(int)river.Depth));
            var nextPads=new HashSet<Rect>();
            foreach(var road in district.Roads ?? new())if(road!=null)nextPads.Add(new Rect(-width/2+road.GridX*10,-depth/2+road.GridZ*10,10,10));
            foreach(var placed in district.Lots ?? new())
            {
                if(placed==null)continue;var lot=LotContentCatalog.Read(placed.LotId);if(lot==null)continue;
                float w=DistrictScale.GridSpanForMeters(lot.LotWidthCells*LotMetricScale.MajorGridMeters)*10;
                float d=DistrictScale.GridSpanForMeters(lot.LotDepthCells*LotMetricScale.MajorGridMeters)*10;
                if((placed.RotationQuarterTurns&1)!=0)(w,d)=(d,w);
                nextPads.Add(new Rect(-width/2+placed.GridX*10,-depth/2+placed.GridZ*10,w,d));
            }
            var key=width+":"+depth+":"+JsonUtility.ToJson(district.Hills);
            // Only terrain-preserving coal sites influence elevation; labor is intentionally excluded.
            if(district.Hills?.PreserveLegacyCoalSites==true)
                foreach(var deposit in district.ResourceDeposits ?? new())if(deposit.Kind=="coal")key+=$":{deposit.NormalizedX},{deposit.NormalizedZ}";
            var changes=new Changes{Full=owner!=district || terrain!=key};
            float relief= district.Hills?.HeightMeters>0 ? (district.Hills.Mountains?45:Mathf.Max(90,Mathf.Min(60,district.Hills.HeightMeters)*4)) : 0;
            void RiverArea((Vector2 a,Vector2 b,float width,int depth) r)
            {
                var min=Vector2.Min(r.a,r.b);var max=Vector2.Max(r.a,r.b);
                changes.Areas.Add(DistrictDirtyGrid.Expand(Rect.MinMaxRect(min.x,min.y,max.x,max.y),r.width*.64f+25+relief));
            }
            if(!changes.Full)
            {
                foreach(var r in rivers)if(!nextRivers.Contains(r))RiverArea(r);
                foreach(var r in nextRivers)if(!rivers.Contains(r))RiverArea(r);
                foreach(var p in pads)if(!nextPads.Contains(p))changes.Areas.Add(DistrictDirtyGrid.Expand(p,12+relief));
                foreach(var p in nextPads)if(!pads.Contains(p))changes.Areas.Add(DistrictDirtyGrid.Expand(p,12+relief));
            }
            rivers=nextRivers;pads=nextPads;terrain=key;owner=district;
            if(changes.Any)Revision++;
            return changes;
        }
    }
}
