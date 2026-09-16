using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum RiverSculptMode { Shape, Soften, Erase }

    // Pure edit draft in district metres. Width and depth never change with the brush.
    public static class DistrictRiverSculpt
    {
        public static List<PlacedDistrictRiver> Edit(RegionCityTile district, IReadOnlyList<Vector2> stroke,
            float radius, RiverSculptMode mode)
        {
            var result=new List<PlacedDistrictRiver>();
            var size=new Vector2(DistrictScale.SizeMeters(district.Width),DistrictScale.SizeMeters(district.Height));
            var brush=stroke.Select(p=>Vector2.Scale(p,size)).ToList();
            if(brush.Count==0)return district.Rivers;
            var selected=DistrictRiverEditing.FindAt(district,stroke[0],radius);
            foreach(var river in district.Rivers ?? new())
            {
                if(river?.Points==null || river.Points.Count<2)continue;
                var points=Resample(river.Points.Select(p=>Vector2.Scale(new Vector2(p.X,p.Z),size)).ToList(),Mathf.Max(8,radius/8));
                if(mode==RiverSculptMode.Shape && river!=selected){result.Add(river);continue;}
                if(!points.Any(p=>brush.Any(c=>Vector2.Distance(p,c)<=radius))){result.Add(river);continue;}
                if(mode==RiverSculptMode.Erase)
                {
                    var pieces=new List<List<Vector2>>{points};
                    foreach(var center in brush)pieces=pieces.SelectMany(p=>Erase(p,center,radius)).ToList();
                    foreach(var piece in pieces)if(piece.Count>=2)result.Add(Copy(river,piece,size));
                    continue;
                }
                var moved=new List<Vector2>(points);
                for(int i=0;i<points.Count;i++)
                {
                    var p=points[i];
                    float edge=Mathf.Min(Mathf.Min(p.x,size.x-p.x),Mathf.Min(p.y,size.y-p.y));
                    // Keep border crossings and their tangents fixed for the neighboring district.
                    float border=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/Mathf.Max(32,river.WidthMeters)));
                    float distance=mode==RiverSculptMode.Shape?Vector2.Distance(p,brush[0]):brush.Min(c=>Vector2.Distance(c,p));
                    float weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(1-distance/radius))*border;
                    if(mode==RiverSculptMode.Shape)
                        moved[i]=p+Vector2.ClampMagnitude(brush[brush.Count-1]-brush[0],radius)*weight;
                    else if(i>0 && i<points.Count-1)
                    {
                        int span=Mathf.Max(1,Mathf.RoundToInt(radius/Mathf.Max(8,radius/8)/3));
                        var average=Vector2.zero;int count=0;
                        for(int j=Mathf.Max(0,i-span);j<=Mathf.Min(points.Count-1,i+span);j++){average+=points[j];count++;}
                        moved[i]=Vector2.Lerp(p,average/count,weight*.8f);
                    }
                    moved[i]=new Vector2(Mathf.Clamp(moved[i].x,0,size.x),Mathf.Clamp(moved[i].y,0,size.y));
                }
                if(mode==RiverSculptMode.Shape)
                {
                    SnapEnd(0);SnapEnd(moved.Count-1);
                    void SnapEnd(int index)
                    {
                        var p=points[index];
                        if(p.x<.01f || p.y<.01f || p.x>size.x-.01f || p.y>size.y-.01f || Vector2.Distance(p,brush[0])>radius)return;
                        var best=moved[index];float limit=river.WidthMeters*.5f+16;
                        foreach(var other in district.Rivers)
                        {
                            if(other==river)continue;
                            for(int j=1;j<other.Points.Count;j++)
                            {
                                var a=Vector2.Scale(new Vector2(other.Points[j-1].X,other.Points[j-1].Z),size);
                                var b=Vector2.Scale(new Vector2(other.Points[j].X,other.Points[j].Z),size);
                                var d=b-a;var q=a+d*(d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(moved[index]-a,d)/d.sqrMagnitude):0);
                                float distance=Vector2.Distance(moved[index],q);
                                if(distance<limit){limit=distance;best=q;}
                            }
                        }
                        moved[index]=best;
                    }
                }
                result.Add(points.SequenceEqual(moved)?river:Copy(river,moved,size));
            }
            return result;
        }
        // Replace the selected reach once, after mouse-up. Retain untouched upstream
        // and downstream geometry, avoiding a second overlapping painted channel.
        public static List<PlacedDistrictRiver> Redraw(RegionCityTile district,PlacedDistrictRiver selected,
            IReadOnlyList<Vector2> stroke, float? widthMeters = null)
        {
            if(selected==null || stroke.Count<2)return district.Rivers;
            var size=new Vector2(DistrictScale.SizeMeters(district.Width),DistrictScale.SizeMeters(district.Height));
            var old=selected.Points.Select(p=>Vector2.Scale(new Vector2(p.X,p.Z),size)).ToList();
            var drawn=stroke.Select(p=>Vector2.Scale(p,size)).ToList();
            float length=0;for(int i=1;i<drawn.Count;i++)length+=Vector2.Distance(drawn[i-1],drawn[i]);
            if(length<Mathf.Max(8,selected.WidthMeters*.15f))return district.Rivers;
            var distances=new float[old.Count];for(int i=1;i<old.Count;i++)distances[i]=distances[i-1]+Vector2.Distance(old[i-1],old[i]);
            (float along,Vector2 point) Nearest(Vector2 point)
            {
                float best=float.PositiveInfinity,along=0;var hit=old[0];
                for(int i=1;i<old.Count;i++)
                {
                    var d=old[i]-old[i-1];float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-old[i-1],d)/d.sqrMagnitude):0;
                    var q=old[i-1]+d*t;float distance=(point-q).sqrMagnitude;
                    if(distance<best){best=distance;hit=q;along=Mathf.Lerp(distances[i-1],distances[i],t);}
                }
                return (along,hit);
            }
            var start=Nearest(drawn[0]);var end=Nearest(drawn[drawn.Count-1]);
            if(end.along<start.along){drawn.Reverse();var swap=start;start=end;end=swap;}
            // A stationary click or perpendicular tug is not a redraw of a reach.
            if(end.along-start.along<Mathf.Max(8,selected.WidthMeters*.15f))return district.Rivers;
            drawn[0]=start.point;drawn[drawn.Count-1]=end.point;
            var replacement=RegionRiverDrawing.Smooth(drawn);
            var points=new List<Vector2>();
            for(int i=0;i<old.Count;i++)if(distances[i]<start.along-.01f)points.Add(old[i]);
            points.AddRange(replacement);
            for(int i=0;i<old.Count;i++)if(distances[i]>end.along+.01f)points.Add(old[i]);
            var replacementRiver=Copy(selected,points,size);
            if(widthMeters.HasValue)replacementRiver.WidthMeters=Mathf.Max(1,widthMeters.Value);
            return district.Rivers.Select(r=>r==selected?replacementRiver:r).ToList();
        }

        public static List<PlacedDistrictRiver> RepairAll(RegionCityTile district)
        {
            var size=new Vector2(DistrictScale.SizeMeters(district.Width),DistrictScale.SizeMeters(district.Height));
            List<Vector2> Metres(PlacedDistrictRiver r)=>r.Points.Select(p=>Vector2.Scale(new Vector2(p.X,p.Z),size)).ToList();
            float Length(PlacedDistrictRiver r){var p=Metres(r);float n=0;for(int i=1;i<p.Count;i++)n+=Vector2.Distance(p[i-1],p[i]);return n;}
            bool Border(Vector2 p)=>p.x<.01f || p.y<.01f || p.x>size.x-.01f || p.y>size.y-.01f;
            float Distance(Vector2 p,List<Vector2> path)
            {
                float best=float.MaxValue;
                for(int k=1;k<path.Count;k++){var v=path[k]-path[k-1];var q=path[k-1]+v*(v.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-path[k-1],v)/v.sqrMagnitude):0);best=Mathf.Min(best,Vector2.Distance(p,q));}
                return best;
            }
            var channels=(district.Rivers??new()).Where(r=>r?.Points?.Count>=2).OrderByDescending(Length).ToList();
            for(int i=0;i<channels.Count;i++)
            for(int j=channels.Count-1;j>i;j--)
            {
                var main=channels[i];var piece=channels[j];
                if(main.Depth!=piece.Depth || Mathf.Abs(main.WidthMeters-piece.WidthMeters)>.01f)continue;
                var path=Metres(main);var other=Metres(piece);
                // A separate bank strip wholly inside the main channel is a remnant,
                // not a tributary. Keep every distinct border crossing.
                if(!Border(other[0]) && !Border(other[other.Count-1]) &&
                    Resample(other,Mathf.Max(2,main.WidthMeters*.25f)).All(p=>Distance(p,path)<main.WidthMeters*.5f))
                {channels.RemoveAt(j);continue;}
                float best=main.WidthMeters*2;int endA=-1,endB=-1;
                foreach(int x in new[]{0,path.Count-1})foreach(int y in new[]{0,other.Count-1})
                {
                    float distance=Vector2.Distance(path[x],other[y]);
                    if(!Border(path[x]) && !Border(other[y]) && distance<best){best=distance;endA=x;endB=y;}
                }
                if(endA<0)continue;
                if(endA==0)path.Reverse();if(endB!=0)other.Reverse();
                // Overlapping reaches can point past each other after Shape.
                // Remove the obsolete terminal tails before building the shared
                // centerline, rather than carrying both cut-off ends into it.
                var bridge=other[0]-path[path.Count-1];
                if(Vector2.Dot(bridge,path[path.Count-1]-path[path.Count-2])<0 ||
                    Vector2.Dot(bridge,other[1]-other[0])<0)
                {
                    TrimTail(path,main.WidthMeters);
                    other.Reverse();TrimTail(other,main.WidthMeters);other.Reverse();
                }
                path.AddRange(other);
                var joined=Copy(main,path,size);joined.InstanceId=main.InstanceId;
                channels[i]=joined;channels.RemoveAt(j);
            }
            var draft=new RegionCityTile{Width=district.Width,Height=district.Height,Rivers=channels};
            foreach(var river in channels)draft.Rivers=Repair(draft,river,rebuild:true);
            return draft.Rivers;
        }

        static void TrimTail(List<Vector2> path,float distance)
        {
            while(path.Count>2)
            {
                int last=path.Count-1;float length=Vector2.Distance(path[last],path[last-1]);
                if(length>distance){path[last]=Vector2.MoveTowards(path[last],path[last-1],distance);return;}
                distance-=length;path.RemoveAt(last);
            }
            // Preserve the outer border endpoint even on a very short reach.
            path[1]=Vector2.MoveTowards(path[1],path[0],Mathf.Min(distance,Vector2.Distance(path[0],path[1])*.5f));
        }

        // Repair only the selected channel. Work in metres so the cleanup radius
        // follows river width, not district size or the density of mouse samples.
        public static List<PlacedDistrictRiver> Repair(RegionCityTile district, PlacedDistrictRiver selected, bool rebuild=false)
        {
            if(selected?.Points==null || selected.Points.Count<2)return district.Rivers;
            var size=new Vector2(DistrictScale.SizeMeters(district.Width),DistrictScale.SizeMeters(district.Height));
            var points=selected.Points.Select(p=>Vector2.Scale(new Vector2(p.X,p.Z),size)).ToList();
            float width=Mathf.Max(1,selected.WidthMeters);
            bool changed=rebuild;
            // Collapse tiny excursions/backtracking onto the surrounding channel.
            // Long bends are retained; endpoints (including connections) never move.
            for(int i=1;i<points.Count-1;)
            {
                var a=points[i-1];var b=points[i];var c=points[i+1];
                var before=b-a;var after=c-b;
                bool duplicate=before.sqrMagnitude<.01f || after.sqrMagnitude<.01f;
                bool spike=Vector2.Dot(before.normalized,after.normalized)<-.15f &&
                    before.magnitude+after.magnitude<width*3;
                if(duplicate || spike){points.RemoveAt(i);changed=true;i=Mathf.Max(1,i-1);}else i++;
            }
            // Uniform arc-length samples avoid amplifying dense clusters of points.
            float spacing=Mathf.Max(2,width*(rebuild?1.25f:.20f));
            var uniform=new List<Vector2>{points[0]};float remaining=spacing;
            for(int i=1;i<points.Count;i++)
            {
                var a=points[i-1];var b=points[i];float length=Vector2.Distance(a,b);
                while(length>=remaining && length>.0001f)
                {
                    a=Vector2.MoveTowards(a,b,remaining);uniform.Add(a);
                    length=Vector2.Distance(a,b);remaining=spacing;
                }
                remaining-=length;
            }
            if(Vector2.Distance(uniform[uniform.Count-1],points[points.Count-1])<spacing*.5f && uniform.Count>1)uniform.RemoveAt(uniform.Count-1);
            uniform.Add(points[points.Count-1]);
            // Rebuild the general course, including small jagged corners that
            // were not sharp enough to trigger fold repair. Symmetric smoothing
            // over physical distance keeps the broad bends, rather than fitting
            // a straight line between the entry and exit.
            for(int pass=0;pass<3;pass++)
            {
                var next=new List<Vector2>(uniform);
                for(int i=2;i<uniform.Count-2;i++)
                {
                    var sum=Vector2.zero;float total=0;
                    int reach=Mathf.Min(8,Mathf.Min(i,uniform.Count-1-i));
                    for(int offset=-reach;offset<=reach;offset++)
                    {
                        float weight=Mathf.Exp(-offset*offset/18f);
                        sum+=uniform[i+offset]*weight;total+=weight;
                    }
                    var target=sum/total;
                    if((target-uniform[i]).sqrMagnitude>.0025f){next[i]=target;changed=true;}
                }
                uniform=next;
            }
            if(rebuild)
            {
                // Discard the old vertex layout: these sparse centerline guides
                // define a fresh smooth channel. Corner cutting stays within
                // their convex hull, so it cannot overshoot the district border.
                for(int pass=0;pass<4;pass++)
                {
                    var curve=new List<Vector2>{uniform[0]};
                    for(int i=1;i<uniform.Count;i++)
                    {
                        curve.Add(Vector2.Lerp(uniform[i-1],uniform[i],.25f));
                        curve.Add(Vector2.Lerp(uniform[i-1],uniform[i],.75f));
                    }
                    curve.Add(uniform[uniform.Count-1]);uniform=curve;
                }
            }
            // A bank folds when its turning radius is smaller than the channel's
            // half-width. Relax only those corners, leaving broad bends alone.
            for(int pass=0;pass<160;pass++)
            {
                var next=new List<Vector2>(uniform);bool moved=false;
                for(int i=2;i<uniform.Count-2;i++)
                {
                    var a=uniform[i]-uniform[i-1];var b=uniform[i+1]-uniform[i];
                    float angle=Vector2.Angle(a,b)*Mathf.Deg2Rad;
                    float radius=Mathf.Min(a.magnitude,b.magnitude)/Mathf.Max(.00001f,2*Mathf.Tan(angle*.5f));
                    if(radius>=width*.85f)continue;
                    next[i]=Vector2.Lerp(uniform[i],(uniform[i-1]+uniform[i+1])*.5f,.5f);
                    if((next[i]-uniform[i]).sqrMagnitude>.000001f)moved=true;
                }
                if(!moved)break;
                changed=true;uniform=next;
            }
            if(!changed)return district.Rivers;
            var repaired=Copy(selected,uniform,size);
            // Repair preserves identity and authored width/depth; undo stores the old geometry.
            repaired.InstanceId=selected.InstanceId;
            return district.Rivers.Select(r=>r==selected?repaired:r).ToList();
        }

        static PlacedDistrictRiver Copy(PlacedDistrictRiver source,List<Vector2> points,Vector2 size) => new()
        {
            InstanceId=Guid.NewGuid().ToString("N"),RegionRiverId=source.RegionRiverId,Depth=source.Depth,
            Direction=source.Direction,WidthMeters=source.WidthMeters,
            Points=points.Select(p=>new DistrictRiverPoint(p.x/size.x,p.y/size.y)).ToList()
        };
        static List<Vector2> Resample(List<Vector2> points,float step)
        {
            var result=new List<Vector2>{points[0]};
            for(int i=1;i<points.Count;i++)
            {
                int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(points[i-1],points[i])/step));
                for(int j=1;j<=count;j++)result.Add(Vector2.Lerp(points[i-1],points[i],(float)j/count));
            }
            return result;
        }
        static List<List<Vector2>> Erase(List<Vector2> points,Vector2 center,float radius)
        {
            var result=new List<List<Vector2>>();List<Vector2> current=null;
            for(int i=1;i<points.Count;i++)
            {
                var a=points[i-1];var d=points[i]-a;var f=a-center;
                float aa=Vector2.Dot(d,d),bb=2*Vector2.Dot(f,d),cc=Vector2.Dot(f,f)-radius*radius;
                float discriminant=bb*bb-4*aa*cc;
                var cuts=new List<float>{0,1};
                if(aa>1e-8f && discriminant>=0)
                    foreach(float t in new[]{(-bb-Mathf.Sqrt(discriminant))/(2*aa),(-bb+Mathf.Sqrt(discriminant))/(2*aa)})if(t>0 && t<1)cuts.Add(t);
                cuts.Sort();
                for(int k=1;k<cuts.Count;k++)
                {
                    float lo=cuts[k-1],hi=cuts[k];
                    if(Vector2.Distance(a+d*((lo+hi)*.5f),center)<radius){current=null;continue;}
                    var start=a+d*lo;var end=a+d*hi;
                    if(current==null){current=new(){start};result.Add(current);}
                    current.Add(end);
                }
            }
            return result;
        }
    }
}
