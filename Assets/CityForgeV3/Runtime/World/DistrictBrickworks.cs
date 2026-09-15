using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable] public sealed class DistrictBrickworksSite
    {
        public string Id=Guid.NewGuid().ToString("N");
        public float NormalizedX,NormalizedZ,Yaw;
        public bool Enabled=true;
        public int StoneInput,BricksProduced;
        public float Elapsed;
    }
    public static class DistrictBrickworks
    {
        public const string ResourcePath="CityForgeV3/Industry/BrickworksV01/Brickworks";
        // Keep the rendered asset, placement bounds and wagon access in the same scale contract.
        public const float PresentationScale=2f;
        public const float HalfWidth=10f*PresentationScale,HalfDepth=12.5f*PresentationScale;
        public const float SecondsPerTon=30,UnloadSeconds=8;
        public static bool Unlocked(RegionCityTile d)=>d?.StoneSites?.Any(s=>s.Built)==true;
        public static Vector2 Point(RegionCityTile d,DistrictBrickworksSite s)=>new((s.NormalizedX-.5f)*DistrictScale.SizeMeters(d.Width),(s.NormalizedZ-.5f)*DistrictScale.SizeMeters(d.Height));
        public static Vector2 Offset(Vector2 center,float yaw,Vector2 offset)
        {var p=Quaternion.Euler(0,yaw,0)*new Vector3(offset.x,0,offset.y);return center+new Vector2(p.x,p.z);}
        public static Vector2 ReceivingPoint(RegionCityTile d,DistrictBrickworksSite s)=>Offset(Point(d,s),s.Yaw,new Vector2(0,24*PresentationScale));
        public static Vector2 QuarryHome(RegionCityTile d,DistrictStoneSite s)=>Offset(DistrictQuarry.Point(d,s),s.Yaw,DistrictQuarry.WagonHomeOffset);
        public static bool Contains(RegionCityTile d,DistrictBrickworksSite s,Vector2 p,float margin=0)
        {var v=Quaternion.Euler(0,-s.Yaw,0)*new Vector3(p.x-Point(d,s).x,0,p.y-Point(d,s).y);return Mathf.Abs(v.x)<HalfWidth+margin&&Mathf.Abs(v.z)<HalfDepth+margin;}
        public static string PlacementReason(RegionCityTile d,DistrictBrickworksSite s,Func<Vector2,bool> dry)
        {
            if(!Unlocked(d))return "Place a Stone Quarry before building a Brickworks.";
            var center=Point(d,s);var elevation=new DistrictElevation(d);float low=float.MaxValue,high=float.MinValue;
            for(float z=-HalfDepth-1;z<=HalfDepth+1;z+=2)for(float x=-HalfWidth-1;x<=HalfWidth+1;x+=2)
            {
                var p=Offset(center,s.Yaw,new Vector2(x,z));
                if(!dry(p))return "Brickworks needs dry ground clear of lots and the district edge.";
                if(d.StoneSites.Any(q=>q.Built&&Vector2.Distance(p,DistrictQuarry.Point(d,q))<19))return "Leave space around the quarry.";
                if((d.Brickworks??new()).Any(b=>b.Id!=s.Id&&Contains(d,b,p,1)))return "Another Brickworks occupies this space.";
                foreach(var road in d.Roads??new())
                {var r=new Vector2((road.GridX+.5f)*10-DistrictScale.SizeMeters(d.Width)/2,(road.GridZ+.5f)*10-DistrictScale.SizeMeters(d.Height)/2);if(Mathf.Abs(r.x-p.x)<5&&Mathf.Abs(r.y-p.y)<5)return "Keep roads outside the Brickworks footprint.";}
                float y=elevation.Sample(p.x,p.y);low=Mathf.Min(low,y);high=Mathf.Max(high,y);
            }
            return high-low>1.5f?"Level the Brickworks site first.":"";
        }
        public static bool Build(RegionCityTile d,DistrictBrickworksSite s,Func<Vector2,bool> dry)
        {
            if(!string.IsNullOrEmpty(PlacementReason(d,s,dry)))return false;
            d.Brickworks??=new();d.Brickworks.Add(s);
            d.Flora?.RemoveAll(f=>Contains(d,s,DistrictLabor.TreePoint(d,f),3));return true;
        }
        public static bool Tick(RegionCityTile d,float dt)
        {
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return false;
            d.ResourceInventory??=new();bool changed=false;
            foreach(var b in d.Brickworks??new())
            {
                if(!b.Enabled||b.StoneInput<=0||d.ResourceInventory.Stone<=0||d.ResourceInventory.Bricks==int.MaxValue)continue;
                b.Elapsed+=dt;
                int count=(int)Math.Min(Math.Min(b.StoneInput,d.ResourceInventory.Stone),Math.Min(int.MaxValue-d.ResourceInventory.Bricks,Math.Floor(b.Elapsed/SecondsPerTon)));
                if(count==0)continue;
                b.Elapsed-=count*SecondsPerTon;b.StoneInput-=count;d.ResourceInventory.Stone-=count;d.ResourceInventory.Bricks+=count;
                b.BricksProduced=(int)Math.Min(int.MaxValue,(long)b.BricksProduced+count);if(b.StoneInput==0)b.Elapsed=0;changed=true;
            }
            return changed;
        }
    }
    public sealed class QuarryDeliveryDestination
    {public string Id;public Vector2 Point;public List<Vector2> Route;}
    public static class DistrictQuarryDelivery
    {
        public static bool Tick(RegionCityTile d,DistrictStoneSite s,float dt,Func<DistrictStoneSite,QuarryDeliveryDestination> find,Func<DistrictStoneSite,bool> move)
        {
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt)||!s.Built||!s.Enabled||!DistrictQuarry.WorkersPaid(d,s))return false;
            if(s.Phase=="full")
            {
                if(s.Elapsed<s.Script.fullCartSeconds){s.Elapsed=Mathf.Min(s.Script.fullCartSeconds,s.Elapsed+dt);return false;}
                s.DeliveryRetry-=dt;if(s.DeliveryRetry>0)return false;s.DeliveryRetry=3;
                var target=find(s);
                if(target==null){s.DeliveryStatus="Bricksworks required — build a Brickworks and connect a clear wagon route by road.";return false;}
                s.DeliveryTargetId=target.Id;s.DeliveryDestination=target.Point;s.DeliveryRoute=target.Route;s.Phase="delivering";s.Elapsed=0;s.DeliveryStatus="Delivering stone to Brickworks";return true;
            }
            if(s.Phase=="delivering"||s.Phase=="unloading")
            {
                if(!(d.Brickworks?.Any(b=>b.Id==s.DeliveryTargetId&&b.Enabled)??false))
                {s.Phase="full";s.Elapsed=0;s.DeliveryRoute=null;s.DeliveryRetry=0;s.DeliveryStatus="Bricksworks required — destination is unavailable.";return true;}
            }
            if(s.Phase=="delivering"||s.Phase=="returning")
            {
                if(!move(s))return false;
                s.Elapsed=0;s.DeliveryRoute=null;
                if(s.Phase=="delivering"){s.Phase="unloading";s.DeliveryStatus="Unloading stone at Brickworks";}
                else{s.Phase="mining";s.DeliveryTargetId="";s.DeliveryStatus="";}
                return true;
            }
            if(s.Phase!="unloading")return false;
            s.Elapsed+=dt;if(s.Elapsed< DistrictBrickworks.UnloadSeconds)return false;
            var works=d.Brickworks.First(b=>b.Id==s.DeliveryTargetId);
            // Stone was credited at quarry loading. Delivery reserves that same material for firing.
            works.StoneInput=(int)Math.Min(int.MaxValue,(long)works.StoneInput+s.CargoStoneTons);
            s.CargoStoneTons=0;s.CartBlocks=0;s.Elapsed=0;s.Phase="returning";s.DeliveryDestination=DistrictBrickworks.QuarryHome(d,s);s.DeliveryRoute=null;
            s.DeliveryStatus="Returning to quarry";return true;
        }
    }
}
