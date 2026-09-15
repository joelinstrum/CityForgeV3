using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    // Connected roads, with short dry access lanes at the two industrial yards.
    public sealed class DistrictQuarryNavigation
    {
        readonly RegionCityTile d;readonly DistrictTimberNavigation roads;readonly DistrictLaborNavigation ground;
        readonly DistrictStoneSite quarry;readonly List<(Vector2 a,Vector2 b)> access=new();
        readonly List<Vector2> trees;readonly DistrictElevation elevation;
        public DistrictQuarryNavigation(RegionCityTile district,DistrictStoneSite site,Func<Vector2,bool> water)
        {
            d=district;quarry=site;roads=new(d,water);ground=new(d,water);elevation=new(d);
            trees=(d.Flora??new()).Where(t=>t.HarvestState==DistrictTreeHarvestState.Standing).Select(t=>DistrictLabor.TreePoint(d,t)).ToList();
            AddAccess(DistrictBrickworks.QuarryHome(d,site));
            foreach(var b in d.Brickworks??new())AddAccess(DistrictBrickworks.ReceivingPoint(d,b));
        }
        void AddAccess(Vector2 point)
        {
            foreach(var road in d.Roads??new())
            {var center=roads.Center(new Vector2Int(road.GridX,road.GridZ));if(Vector2.Distance(center,point)<=30&&GroundSegment(point,center))access.Add((point,center));}
        }
        static float Distance(Vector2 p,Vector2 a,Vector2 b)
        {var ab=b-a;float t=ab.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);return Vector2.Distance(p,a+ab*t);}
        bool Ground(Vector2 p)
        {
            if(!ground.Walkable(p)||(d.Brickworks??new()).Any(b=>DistrictBrickworks.Contains(d,b,p,1)))return false;
            if(trees.Any(t=>(t-p).sqrMagnitude<2.25f))return false;
            foreach(var q in d.StoneSites.Where(q=>q.Built))
            {
                var center=DistrictQuarry.Point(d,q);var v=Quaternion.Euler(0,-q.Yaw,0)*new Vector3(p.x-center.x,0,p.y-center.y);
                if(Mathf.Abs(v.x)<11&&Mathf.Abs(v.z)<13&&!(q.Id==quarry.Id&&v.x>3&&v.z>-4))return false;
            }
            return true;
        }
        bool GroundSegment(Vector2 a,Vector2 b)
        {
            int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)*2));var last=a;
            for(int i=0;i<=n;i++)
            {var p=Vector2.Lerp(a,b,(float)i/n);if(!Ground(p))return false;if(i>0&&Mathf.Abs(elevation.Sample(p.x,p.y)-elevation.Sample(last.x,last.y))>.4f)return false;last=p;}
            return true;
        }
        public bool Segment(Vector2 a,Vector2 b)
        {
            int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)*2));
            for(int i=0;i<=n;i++)
            {
                var p=Vector2.Lerp(a,b,(float)i/n);
                foreach(var side in new[]{Vector2.zero,Vector2.right*.8f,Vector2.left*.8f,Vector2.up*.8f,Vector2.down*.8f})
                {var q=p+side;if(!Ground(q)||(!roads.OnRoad(q)&&!access.Any(l=>Distance(q,l.a,l.b)<7)))return false;}
            }
            return GroundSegment(a,b);
        }
        List<Vector2> Entries(Vector2 point)=>access.Where(l=>Vector2.Distance(l.a,point)<.1f).Select(l=>l.b).ToList();
        public List<Vector2> Route(Vector2 from,Vector2 to)
        {
            var starts=roads.OnRoad(from)?new List<Vector2>{from}:Entries(from);
            // A saved wagon may be halfway along an access lane.
            if(starts.Count==0)starts=access.Where(l=>Distance(from,l.a,l.b)<7&&Segment(from,l.b)).Select(l=>l.b).ToList();
            var ends=roads.OnRoad(to)?new List<Vector2>{to}:Entries(to);
            List<Vector2> best=null;float bestLength=float.MaxValue;
            foreach(var a in starts)foreach(var b in ends)
            {
                if(!Segment(from,a)||!Segment(b,to))continue;var path=roads.Route(a,b);if(path==null)continue;
                var result=new List<Vector2>{from,a};result.AddRange(path);result.Add(to);
                for(int i=result.Count-1;i>0;i--)if(Vector2.Distance(result[i],result[i-1])<.01f)result.RemoveAt(i);
                float length=0;bool clear=true;for(int i=1;i<result.Count;i++){if(!Segment(result[i-1],result[i])){clear=false;break;}length+=Vector2.Distance(result[i-1],result[i]);}
                if(clear&&length<bestLength){best=result;bestLength=length;}
            }
            return best;
        }
        public IEnumerable<QuarryDeliveryDestination> Destinations(Vector2 from)
        {
            var options=new List<QuarryDeliveryDestination>();
            foreach(var b in d.Brickworks??new())
            {
                if(!b.Enabled)continue;var point=DistrictBrickworks.ReceivingPoint(d,b);var route=Route(from,point);
                if(route!=null)options.Add(new(){Id=b.Id,Point=point,Route=route});
            }
            return options.OrderBy(o=>o.Route.Zip(o.Route.Skip(1),(a,b)=>Vector2.Distance(a,b)).Sum());
        }
    }
}
