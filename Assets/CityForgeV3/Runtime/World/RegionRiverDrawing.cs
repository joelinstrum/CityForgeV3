using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum RegionRiverSize { Small, Large, Major }

    public static class RegionRiverDrawing
    {
        public static float WidthMeters(RegionRiverSize size) => size==RegionRiverSize.Major?128:size==RegionRiverSize.Large?64:18;

        // Filter only river input; road drawing keeps its existing freehand behavior.
        public static void AddPoint(RegionPikeStroke stroke, RegionRiverSize size, Vector2 point, bool final=false)
        {
            int count=stroke.Points.Count;
            if(count>=4096)return;
            stroke.Add(point,final);
            if(stroke.Points.Count==count)return;
            if(count==0)return;
            var points=stroke.Points;
            var end=points[count];var start=points[count-1];
            float width=WidthMeters(size)/DistrictScale.SizeMeters(1);
            // Stabilize movement at a scale relative to the selected channel.
            bool reject=Vector2.Distance(start,end)<Mathf.Max(.025f,width*.5f);
            if(count>=2 && Vector2.Angle(start-points[count-2],end-start)>70f)reject=true;
            // Prevent crossing or tracing alongside an older part of this stroke.
            // Nearby upstream segments are exempt so ordinary bends remain possible.
            float upstream=count>=2?Vector2.Distance(points[count-2],start):0;
            for(int i=count-3;!reject && i>=0;i--)
            {
                upstream+=Vector2.Distance(points[i],points[i+1]);
                if(upstream<=width*2f)continue;
                if(SegmentDistance(start,end,points[i],points[i+1])<width)reject=true;
            }
            if(reject)points.RemoveAt(count);
        }

        static float PointDistance(Vector2 point,Vector2 a,Vector2 b)
        {
            var delta=b-a;
            float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;
            return Vector2.Distance(point,a+delta*t);
        }

        static float Cross(Vector2 a,Vector2 b) => a.x*b.y-a.y*b.x;
        static float SegmentDistance(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            var ab=b-a;var cd=d-c;float cross=Cross(ab,cd);
            if(Mathf.Abs(cross)>.000001f)
            {
                float t=Cross(c-a,cd)/cross,u=Cross(c-a,ab)/cross;
                if(t>=0 && t<=1 && u>=0 && u<=1)return 0;
            }
            return Mathf.Min(Mathf.Min(PointDistance(a,c,d),PointDistance(b,c,d)),
                Mathf.Min(PointDistance(c,a,b),PointDistance(d,a,b)));
        }

        public static List<Vector2> Smooth(IReadOnlyList<Vector2> points)
        {
            var result=new List<Vector2>(points);
            if(points.Count<3)return result;
            // Two corner-cutting passes stay inside the stroke's bounds and preserve endpoints.
            for(int pass=0;pass<2;pass++)
            {
                var next=new List<Vector2>{result[0]};
                for(int i=1;i<result.Count;i++)
                {
                    next.Add(Vector2.Lerp(result[i-1],result[i],.25f));
                    next.Add(Vector2.Lerp(result[i-1],result[i],.75f));
                }
                next.Add(result[result.Count-1]);result=next;
            }
            return result;
        }

        public static List<Vector2> Preview(RegionPikeStroke stroke,RegionRiverSize size,RegionSaveData region)
        {
            var points=new List<Vector2>(stroke.Points);
            if(points.Count>0)
            {
                points[0]=SnapToRiver(points[0],size,region);
                if(points.Count>1)points[points.Count-1]=SnapToRiver(points[points.Count-1],size,region);
            }
            return Smooth(points);
        }

        // Snap endpoints onto an existing channel's centerline, including district-local rivers.
        // Channel width remains independent of whether it joins another river.
        public static Vector2 SnapToRiver(Vector2 point,RegionRiverSize size,RegionSaveData region)
        {
            var best=point;float bestDistance=float.PositiveInfinity;
            if(region==null)return best;
            foreach(var tile in region.Tiles)
            foreach(var river in tile.Rivers ?? new List<PlacedDistrictRiver>())
            {
                if(river?.Points==null)continue;
                float radius=(river.WidthMeters*.5f+WidthMeters(size)*.5f+16f)/DistrictScale.SizeMeters(1);
                for(int i=1;i<river.Points.Count;i++)
                {
                    var p=river.Points[i-1];var q=river.Points[i];
                    var a=new Vector2(tile.X+p.X*tile.Width,tile.Y+p.Z*tile.Height);
                    var b=new Vector2(tile.X+q.X*tile.Width,tile.Y+q.Z*tile.Height);
                    var delta=b-a;
                    float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;
                    var candidate=a+delta*t;float distance=Vector2.Distance(point,candidate);
                    if(distance<=radius && distance<bestDistance){best=candidate;bestDistance=distance;}
                }
            }
            return best;
        }

        public static RegionRiverPath Create(RegionPikeStroke stroke,RegionRiverSize size,RegionSaveData region=null)
        {
            if(stroke==null || !stroke.IsValid)return null;
            var path=new RegionRiverPath
            {
                Id="drawn-river-"+Guid.NewGuid().ToString("N"),HandDrawn=true,
                Depth=size==RegionRiverSize.Small?DistrictRiverDepth.Shallow:DistrictRiverDepth.Deep,
                WidthMeters=WidthMeters(size)
            };
            foreach(var point in Preview(stroke,size,region))path.Points.Add(new DistrictRiverPoint(point.x,point.y));
            return path;
        }
    }
}
