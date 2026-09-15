using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    [Serializable] public sealed class QuarryScript
    {
        public string schema="cityforge-quarry-v1";
        public float miningSeconds=60,loadingSeconds=16,fullCartSeconds=8;
        public int cartCapacity=4,stoneTonsPerBlock=1;
        public static QuarryScript Parse(string json)
        {
            var s=JsonUtility.FromJson<QuarryScript>(json);
            if(s==null||s.schema!="cityforge-quarry-v1"||!Valid(s.miningSeconds,1,3600)||!Valid(s.loadingSeconds,.1f,120)||!Valid(s.fullCartSeconds,1,600)||s.cartCapacity<1||s.cartCapacity>8||s.stoneTonsPerBlock<1||s.stoneTonsPerBlock>1000)throw new ArgumentException("Use mining 1–3600 seconds, loading 0.1–120, full cart 1–600, capacity 1–8 and 1–1000 tons per block.");
            return s;
        }
        static bool Valid(float v,float min,float max)=>!float.IsNaN(v)&&!float.IsInfinity(v)&&v>=min&&v<=max;
    }
    [Serializable] public sealed class DistrictStoneSite
    {
        public string Id=Guid.NewGuid().ToString("N");
        public string Kind="stone";
        public float NormalizedX,NormalizedZ,Yaw;
        public bool Built,Enabled=true;
        public QuarryScript Script=new();
        public string Phase="mining";
        public float Elapsed;
        public int CartBlocks,BlocksLoaded;
        public int CraneLoadingVersion;
        public int CargoStoneTons,DeliveryVersion;
        public bool HasWagonPose;
        public Vector2 WagonPosition,DeliveryDestination;
        public float HorseHeading,BodyHeading,FrontHeading,DeliveryRetry;
        public string DeliveryTargetId="",DeliveryStatus="";
        public List<Vector2> DeliveryRoute;
        public int WorkersPaidSeason = -1;
        public bool HasWorkerPayroll;
    }
    public static class DistrictQuarry
    {
        public static void Demolish(DistrictStoneSite site)
        {
            site.Built=false;site.Elapsed=0;site.CartBlocks=0;site.CargoStoneTons=0;
            site.Phase="mining";site.HasWagonPose=false;site.DeliveryRoute=null;
            site.DeliveryTargetId="";site.DeliveryStatus="";site.DeliveryRetry=0;
        }
        public static readonly Vector2 WagonHomeOffset=new(5.65f,4.57f);
        public const int WorkerCount = 2;
        public const int WorkerWage = 250;
        public const int SeasonalPayroll = WorkerCount * WorkerWage;
        public const float TreeClearanceRadius = 20f;
        public const string ResourcePath="CityForgeV3/Industry/StoneQuarryV01/StoneQuarry";
        public static Vector2 Point(RegionCityTile d,DistrictStoneSite p)=>new((p.NormalizedX-.5f)*DistrictScale.SizeMeters(d.Width),(p.NormalizedZ-.5f)*DistrictScale.SizeMeters(d.Height));
        public static bool SiteClear(RegionCityTile d,Vector2 p,Func<Vector2,bool> walkable)
            => string.IsNullOrEmpty(SiteBlockReason(d, p, walkable));
        public static string SiteBlockReason(RegionCityTile d,Vector2 p,Func<Vector2,bool> walkable,float yaw=0)
        {
            var terrain=new DistrictElevation(d);float low=float.MaxValue,high=float.MinValue;
            for(float z=-14;z<=14;z+=2)for(float x=-15;x<=15;x+=2)
            {var offset=Quaternion.Euler(0,yaw,0)*new Vector3(x,0,z);var q=p+new Vector2(offset.x,offset.z);if((d.Brickworks??new()).Any(b=>DistrictBrickworks.Contains(d,b,q,1)))return "A Brickworks occupies the quarry footprint.";if(!walkable(q))return "The quarry footprint overlaps water, a lot, or the district edge.";float y=terrain.Sample(q.x,q.y);low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
            if(high-low>1.5f)return "Level the quarry site first; the ground varies by more than 1.5 meters.";
            foreach(var road in d.Roads??new())
            {var q=new Vector2((road.GridX+.5f)*10-DistrictScale.SizeMeters(d.Width)/2,(road.GridZ+.5f)*10-DistrictScale.SizeMeters(d.Height)/2);var local=Quaternion.Euler(0,-yaw,0)*new Vector3(q.x-p.x,0,q.y-p.y);if(Mathf.Abs(local.x)<20&&Mathf.Abs(local.z)<19)return "Move nearby roads out of the quarry footprint before building.";}
            return "";
        }
        public static void Ensure(RegionCityTile d,Func<Vector2,bool> walkable)
        {
            d.StoneSites??=new();if(d.StoneDepositsGenerated)return;
            if(d.StoneSites.Count>0){d.StoneDepositsGenerated=true;return;}
            uint seed=2166136261;foreach(char c in d.TileId??"")seed=unchecked((seed^c)*16777619);
            var random=new System.Random(unchecked((int)seed));float width=DistrictScale.SizeMeters(d.Width),depth=DistrictScale.SizeMeters(d.Height);
            for(int i=0;i<256&&d.StoneSites.Count<2;i++)
            {
                var p=new Vector2((float)(random.NextDouble()-.5)*(width-80),(float)(random.NextDouble()-.5)*(depth-80));
                if(!SiteClear(d,p,walkable)||d.StoneSites.Any(s=>Vector2.Distance(Point(d,s),p)<80))continue;
                d.StoneSites.Add(new(){Id="stone-"+d.TileId+"-"+i,NormalizedX=p.x/width+.5f,NormalizedZ=p.y/depth+.5f});
            }
            d.StoneDepositsGenerated=true;
        }
        public static bool CanBuild(RegionCityTile d,DistrictStoneSite p,Func<Vector2,bool> walkable)=>d!=null&&p!=null&&p.Kind=="stone"&&!p.Built&&d.StoneSites.Contains(p)&&SiteClear(d,Point(d,p),walkable);
        public static bool Build(RegionCityTile d,DistrictStoneSite p,Func<Vector2,bool> walkable)
        {
            // Validate first: a rejected placement must not clear any vegetation.
            if(!CanBuild(d,p,walkable) || WagesDue(d,p)>d.Treasury)return false;
            PayWorkers(d,p);
            UpgradeCraneLoading(p);
            var center=Point(d,p);
            d.Flora?.RemoveAll(tree=>tree!=null && Vector2.Distance(DistrictLabor.TreePoint(d,tree),center)<TreeClearanceRadius);
            p.Built=true;p.Enabled=true;p.Phase="mining";p.Elapsed=0;p.CartBlocks=0;p.CargoStoneTons=0;p.DeliveryVersion=1;p.HasWagonPose=false;p.DeliveryRoute=null;p.DeliveryStatus="";
            return true;
        }
        public static int WagesDue(RegionCityTile d,DistrictStoneSite site)
            => site.HasWorkerPayroll && site.WorkersPaidSeason==DistrictLabor.State(d).SeasonIndex ? 0 : SeasonalPayroll;
        public static bool WorkersPaid(RegionCityTile d,DistrictStoneSite site) => WagesDue(d,site)==0;
        public static bool PayWorkers(RegionCityTile d,DistrictStoneSite site)
        {
            var due=WagesDue(d,site);if(due==0 || d.Treasury<due)return false;
            d.Treasury-=due;site.HasWorkerPayroll=true;site.WorkersPaidSeason=DistrictLabor.State(d).SeasonIndex;return true;
        }
        public static bool PayWages(RegionCityTile d)
        {
            bool changed=false;
            foreach(var site in d.StoneSites??new())if(site.Built)changed|=PayWorkers(d,site);
            return changed;
        }
        // Upgrade only the old default; preserve explicitly tuned loading durations.
        public static bool UpgradeCraneLoading(DistrictStoneSite site)
        {
            if(site.CraneLoadingVersion>=1)return false;
            site.Script??=new();
            if(Mathf.Approximately(site.Script.loadingSeconds,4))
            {
                if(site.Phase=="loading")site.Elapsed*=4;
                site.Script.loadingSeconds=16;
            }
            site.CraneLoadingVersion=1;return true;
        }
        public static string Status(DistrictStoneSite p)=>!p.Built?"Stone deposit available":!p.Enabled?"Paused":p.Phase=="loading"?"Loading stone into cart":!string.IsNullOrEmpty(p.DeliveryStatus)?p.DeliveryStatus:p.Phase=="full"?"Cart full — waiting for Brickworks":$"Mining block · {Mathf.Max(0,Mathf.CeilToInt(p.Script.miningSeconds-p.Elapsed))}s";
        public static bool Tick(RegionCityTile d,float dt,Func<Vector2,bool> walkable)
        {
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return false;
            bool changed=PayWages(d);d.ResourceInventory??=new();
            foreach(var p in d.StoneSites??new())
            {
                if(!p.Built)continue;
                changed|=UpgradeCraneLoading(p);
                if(p.DeliveryVersion==0){p.CargoStoneTons=(int)Math.Min(int.MaxValue,(long)p.CartBlocks*p.Script.stoneTonsPerBlock);p.DeliveryVersion=1;changed=true;}
                if(!p.Enabled||!WorkersPaid(d,p)||!walkable(Point(d,p))||p.Phase=="full"||p.Phase=="delivering"||p.Phase=="unloading"||p.Phase=="returning")continue;
                p.Script??=new();var s=p.Script;float left=dt;
                for(int steps=0;left>0&&steps<256;steps++)
                {
                    if(p.Phase!="mining"&&p.Phase!="loading"&&p.Phase!="full"){p.Phase="mining";p.Elapsed=0;}
                    float duration=p.Phase=="mining"?s.miningSeconds:p.Phase=="loading"?s.loadingSeconds:s.fullCartSeconds;
                    float take=Mathf.Min(left,Mathf.Max(0,duration-p.Elapsed));p.Elapsed+=take;left-=take;
                    if(p.Elapsed+.00001f<duration)break;
                    p.Elapsed=0;changed=true;
                    if(p.Phase=="mining")p.Phase="loading";
                    else if(p.Phase=="loading")
                    {
                        p.CargoStoneTons=(int)Math.Min(int.MaxValue,(long)p.CargoStoneTons+s.stoneTonsPerBlock);
                        p.CartBlocks++;p.BlocksLoaded=(int)Math.Min(int.MaxValue,(long)p.BlocksLoaded+1);
                        d.ResourceInventory.Stone=(int)Math.Min(int.MaxValue,(long)d.ResourceInventory.Stone+s.stoneTonsPerBlock);
                        p.Phase=p.CartBlocks>=s.cartCapacity?"full":"mining";
                        if(p.Phase=="full")break;
                    }
                    else{p.CartBlocks=0;p.Phase="mining";}
                }
            }
            return changed;
        }
    }
}
