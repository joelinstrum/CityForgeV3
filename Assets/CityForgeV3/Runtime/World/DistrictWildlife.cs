using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    [Serializable] public sealed class DistrictBear
    {
        public string Id=Guid.NewGuid().ToString("N");
        public Vector2 Position, Home, Direction=Vector2.up, ShotOrigin;
        public bool Fleeing;
        public float SteerIn, FleeSeconds;
    }
    [Serializable] public sealed class DistrictMarksman
    {
        public string Id=Guid.NewGuid().ToString("N");
        public Vector2 Position, Facing=Vector2.up;
        public float Cooldown, ShotSeconds;
        public int Shots;
        // Existing guards keep the remainder of their current season on migration.
        public bool WagesPaid = true;
    }
    [Serializable] public sealed class DistrictWildlifeState
    {
        public List<DistrictBear> Bears=new();
        public List<DistrictMarksman> Marksmen=new();
        public float NextSighting=180, QuietSeconds;
        public int SightingCount;
        public string Status="Mountain woodland quiet";
    }
    public static class DistrictWildlife
    {
        public const float SightingRadius=30, ClearRadius=45, ShotRadius=55, DeterrenceSeconds=300;
        public static DistrictWildlifeState State(RegionCityTile d)=>d.Wildlife??=new();
        public const int Wage = DistrictLabor.Wage;
        public static DistrictMarksman Hire(RegionCityTile d, Vector2 position)
        {
            if(d==null || d.Treasury<Wage)return null;
            var guard=new DistrictMarksman{Position=position};
            d.Treasury-=Wage;State(d).Marksmen.Add(guard);return guard;
        }
        public static long WagesDue(RegionCityTile d)=>(long)State(d).Marksmen.Count(m=>!m.WagesPaid)*Wage;
        public static bool PayWages(RegionCityTile d)
        {
            var due=WagesDue(d);if(due>d.Treasury)return false;
            d.Treasury-=(int)due;foreach(var m in State(d).Marksmen)m.WagesPaid=true;return true;
        }
        public static void RenewWages(RegionCityTile d)
        {
            foreach(var m in State(d).Marksmen)m.WagesPaid=false;
            PayWages(d);
        }
        public static bool MountainTree(PlacedDistrictFlora t)=>t!=null&&t.HarvestState==DistrictTreeHarvestState.Standing&&FloraFamilies.ForTree(t.FloraId)==FloraFamilies.Mountain;
        public static bool Tick(RegionCityTile d,float dt,Func<Vector2,bool> walkable)
        {
            if(dt<=0)return false;
            dt=Mathf.Min(dt,.1f);var s=State(d);bool changed=false;
            s.QuietSeconds=Mathf.Max(0,s.QuietSeconds-dt);s.NextSighting-=dt;
            if(s.NextSighting<=0&&s.QuietSeconds<=0&&s.Bears.Count==0)
            {
                s.NextSighting=240+(s.SightingCount*73%181);
                var trees=d.Flora.Where(MountainTree).Select(t=>DistrictLabor.TreePoint(d,t)).ToList();
                var index=DistrictHarvestIndex.For(d);
                // A sighting needs just three nearby mountain trees, not an
                // all-pairs density count across the entire forest.
                var candidates=trees.Where(p=>walkable(p)&&
                    index.NearbyFlora(p,25).Where(MountainTree)
                        .Where(t=>(DistrictLabor.TreePoint(d,t)-p).sqrMagnitude<625).Take(3).Count()>=3 &&
                    !s.Marksmen.Any(m=>m.WagesPaid&&Vector2.Distance(m.Position,p)<ShotRadius)).ToList();
                if(candidates.Count>0)
                {
                    // Prefer woods near active forestry so sightings affect the work area.
                    var workers=DistrictLabor.State(d).Workers;
                    candidates=candidates.OrderBy(p=>workers.Count==0?0:workers.Min(w=>(w.Position-p).sqrMagnitude)).Take(12).ToList();
                    var p=candidates[s.SightingCount%candidates.Count];
                    s.Bears.Add(new(){Position=p,Home=p});s.SightingCount++;changed=true;
                }
            }
            foreach(var m in s.Marksmen)
            {
                m.Cooldown=Mathf.Max(0,m.Cooldown-dt);m.ShotSeconds=Mathf.Max(0,m.ShotSeconds-dt);
                if(!m.WagesPaid)continue;
                var bear=s.Bears.FirstOrDefault(b=>!b.Fleeing&&Vector2.Distance(b.Position,m.Position)<=ShotRadius);
                if(bear==null||m.Cooldown>0)continue;
                m.Facing=(bear.Position-m.Position).normalized;m.Cooldown=20;m.ShotSeconds=2;m.Shots++;
                foreach(var b in s.Bears.Where(b=>Vector2.Distance(b.Position,m.Position)<=ShotRadius))
                {b.Fleeing=true;b.ShotOrigin=m.Position;b.Direction=(b.Position-m.Position).normalized;if(b.Direction.sqrMagnitude<.1f)b.Direction=Vector2.up;}
                s.QuietSeconds=DeterrenceSeconds;s.NextSighting=Mathf.Max(s.NextSighting,DeterrenceSeconds);changed=true;
            }
            foreach(var b in s.Bears.ToArray())
            {
                b.SteerIn-=dt;
                if(b.Fleeing){b.FleeSeconds+=dt;b.Direction=(b.Position-b.ShotOrigin).normalized;}
                else if(b.SteerIn<=0){b.SteerIn=8;float a=(s.SightingCount*53+b.Position.x*3+b.Position.y)*Mathf.Deg2Rad;b.Direction=Vector2.Distance(b.Position,b.Home)>18?(b.Home-b.Position).normalized:new Vector2(Mathf.Cos(a),Mathf.Sin(a));}
                var speed=b.Fleeing?1.8f:.4f;
                foreach(float angle in new[]{0f,35f,-35f,70f,-70f,110f,-110f})
                {
                    var v=Quaternion.Euler(0,0,angle)*new Vector3(b.Direction.x,b.Direction.y,0);var dir=new Vector2(v.x,v.y);var next=b.Position+dir*speed*dt;
                    if(!walkable(next))continue;
                    if(b.Fleeing&&Vector2.Distance(next,b.ShotOrigin)<Vector2.Distance(b.Position,b.ShotOrigin))continue;
                    b.Position=next;b.Direction=dir;break;
                }
                if(b.Fleeing&&Vector2.Distance(b.Position,b.ShotOrigin)>90 && !DistrictLabor.State(d).Workers.Any(w=>Vector2.Distance(w.Position,b.Position)<ClearRadius))
                {s.Bears.Remove(b);changed=true;}
            }
            s.Status=s.Bears.Count==0?"Mountain woodland quiet":s.Bears.Any(b=>!b.Fleeing)?(DistrictLabor.State(d).Workers.Any(w=>w.BearAlarm)?"Bear sighting — workers seek safety":"Bear spotted in mountain woodland"):"Warning shot — bear leaving the area";
            return changed;
        }
        public static bool AvoidBear(RegionCityTile d,DistrictAxeman w,float dt,Func<Vector2,bool> walkable)
        {
            w.RetreatMoving=false;
            var bears=State(d).Bears;
            var bear=bears.FirstOrDefault(b=>Vector2.Distance(b.Position,w.Position)<SightingRadius || w.BearAlarm && Vector2.Distance(b.Position,w.BearWorkPoint)<ClearRadius);
            if(bear==null)
            {
                if(w.BearAlarm)
                {
                    w.BearAlarm=false;w.TreeId="";w.Progress=0;w.Route=null;
                    w.Activity=w.Cargo>0?AxemanActivity.Delivering:AxemanActivity.Waiting;
                }
                return false;
            }
            if(!w.BearAlarm){w.BearWorkPoint=w.Position;w.BearAlarm=true;w.TreeId="";w.Progress=0;w.Route=null;}
            w.Activity=AxemanActivity.Retreating;
            var away=(w.Position-bear.Position).normalized;if(away.sqrMagnitude<.1f)away=Vector2.down;
            if(Vector2.Distance(w.Position,bear.Position)<ClearRadius)
                foreach(float angle in new[]{0f,30f,-30f,60f,-60f,85f,-85f})
                {
                    var v=Quaternion.Euler(0,0,angle)*new Vector3(away.x,away.y,0);var dir=new Vector2(v.x,v.y);var next=w.Position+dir*2.2f*Mathf.Min(dt,.1f);
                    if(!walkable(next)||bears.Any(b=>Vector2.Distance(next,b.Position)<Vector2.Distance(w.Position,b.Position)-.001f))continue;
                    w.Position=next;w.Facing=dir;w.RetreatMoving=true;break;
                }
            return true;
        }
    }
}
