using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    // Wood remains in Labor.Wood for compatibility with existing district saves.
    [Serializable] public sealed class DistrictResourceInventory
    {
        public int Bricks;
        public int Coal, Stone, IronOre, Gold, Oil, Food, Jewels, Cloth;
    }
    public enum AxemanActivity { Waiting, Walking, Chopping, WaitingForFall, Delivering, Returning, Retreating }
    [Serializable] public sealed class DistrictAxeman
    {
        public string Id = Guid.NewGuid().ToString("N");
        public int Slot;
        public string CrewId = "";
        public int CargoTrees;
        public bool BearAlarm;
        public bool RetreatMoving;
        public Vector2 BearWorkPoint;
        public Vector2 Position;
        public Vector2 Facing = Vector2.up;
        public string TreeId = "";
        public AxemanActivity Activity;
        public float Progress;
        public int Cargo;
        public bool CargoAlreadyCredited;
        public List<Vector2> Route = new();
        [NonSerialized] public float RouteRetry;
        [NonSerialized] public int SearchOffset;
    }
    [Serializable] public sealed class DistrictLaborState
    {
        public int AssignedAxemen;
        public int PaidSlots;
        public int SeasonIndex;
        public float SeasonSeconds;
        public bool CampPlaced;
        public Vector2 Camp;
        public int Wood;
        public List<DistrictAxeman> Workers = new();
        public List<DistrictTimberCrew> TimberCrews = new();
    }
    public sealed class DistrictLaborChanges
    {
        public bool Durable;
        public HashSet<string> Changed = new();
        public HashSet<string> Falling = new();
    }
    // Saved simulation only. Rendering, UI and navigation are supplied by the host.
    public static class DistrictLabor
    {
        public const int Wage = 250;
        public const float SeasonDuration = 600f;
        public const float WalkSpeed = 1.5f;
        public const float ChopDuration = 2.25f * 2f + 28f / 24f;
        public static DistrictLaborState State(RegionCityTile d) => d.Labor ??= new DistrictLaborState();
        public static long AssignmentCost(RegionCityTile d, int count) => Math.Max(0L, (long)count - State(d).PaidSlots) * Wage;
        public static bool Assign(RegionCityTile d, int count)
        {
            if (d == null || count < 0) return false;
            var s = State(d); var due = AssignmentCost(d,count);
            if (due > d.Treasury || (count > 0 && !s.CampPlaced)) return false;
            d.Treasury -= (int)due; s.PaidSlots = Math.Max(s.PaidSlots,count); s.AssignedAxemen = count;
            for (var i=0;i<count;i++) if (!s.Workers.Any(w=>w.Slot==i)) s.Workers.Add(new DistrictAxeman {Slot=i,Position=s.Camp});
            return true;
        }
        public static Vector2 TreePoint(RegionCityTile d, PlacedDistrictFlora t) => new((t.NormalizedX-.5f)*DistrictScale.SizeMeters(d.Width),(t.NormalizedZ-.5f)*DistrictScale.SizeMeters(d.Height));
        public static string SeasonName(int index) => new[]{"Summer","Autumn","Winter","Spring"}[Math.Max(0,index)%4];
        public static DistrictLaborChanges Tick(RegionCityTile d,float dt,Func<Vector2,Vector2,List<Vector2>> route,Func<Vector2,bool> walkable)
        {
            var changes=new DistrictLaborChanges();var s=State(d);if(dt<=0 || (!s.CampPlaced && DistrictWildlife.State(d).Marksmen.Count==0 && !(d.StoneSites?.Any(q=>q.Built)??false)))return changes;
            s.SeasonSeconds+=dt;
            if(s.SeasonSeconds>=SeasonDuration)
            {
                s.SeasonSeconds-=SeasonDuration;s.SeasonIndex++;s.PaidSlots=0;
                var due=(long)s.AssignedAxemen*Wage;
                if(due<=d.Treasury){d.Treasury-=(int)due;s.PaidSlots=s.AssignedAxemen;}
                DistrictWildlife.RenewWages(d);
                DistrictQuarry.PayWages(d);
                changes.Durable=true;
            }
            if(!s.CampPlaced)return changes;
            if(s.PaidSlots<s.AssignedAxemen)return changes;
            var index=DistrictHarvestIndex.For(d);int routeBudget=2;
            var claimed=new HashSet<string>(s.Workers.Where(w=>!string.IsNullOrEmpty(w.TreeId)).Select(w=>w.TreeId));
            foreach(var w in s.Workers.ToArray())
            {
                var crew=s.TimberCrews?.Find(c=>c.Id==w.CrewId);
                if(crew!=null&&!crew.Enabled)continue;
                var alarmBefore=w.BearAlarm;var oldTree=w.TreeId;
                if(DistrictWildlife.AvoidBear(d,w,dt,walkable))
                {claimed.Remove(oldTree);changes.Durable|=!alarmBefore;continue;}
                if(alarmBefore)changes.Durable=true;
                var home=crew?.Camp??s.Camp;
                bool retiring=w.Slot>=s.AssignedAxemen;
                w.RouteRetry=Mathf.Max(0,w.RouteRetry-dt);
                var tree=index.Find(w.TreeId);
                if(retiring && w.Activity!=AxemanActivity.Returning)
                {claimed.Remove(w.TreeId);w.TreeId="";w.Route=null;w.Activity=AxemanActivity.Returning;changes.Durable=true;}
                if(w.Activity is AxemanActivity.Returning or AxemanActivity.Delivering)
                {
                    if(w.Route==null || w.Route.Count==0 && Vector2.Distance(w.Position,home)>.1f)
                    {
                        if(w.RouteRetry>0||routeBudget<=0)continue;
                        routeBudget--;w.RouteRetry=2f+(w.Slot%7)*.13f;w.Route=route(w.Position,home);
                    }
                    if(!Move(w,home,dt,walkable))continue;
                    // Returning axemen contribute raw trees, never spendable lumber.
                    w.Cargo=0;w.CargoAlreadyCredited=false;
                    if(crew!=null)crew.PendingTrees+=w.CargoTrees;
                    w.CargoTrees=0;
                    if(retiring)s.Workers.Remove(w);else{w.Activity=AxemanActivity.Waiting;w.Progress=0;}
                    changes.Durable=true;continue;
                }
                if(w.Activity!=AxemanActivity.Waiting && (tree==null || tree.HarvestState==DistrictTreeHarvestState.Stump))
                {claimed.Remove(w.TreeId);w.TreeId="";w.Activity=AxemanActivity.Waiting;w.Progress=0;}
                if(w.Activity==AxemanActivity.Waiting)
                {
                    w.Progress-=dt;if(w.Progress>0||routeBudget<=0)continue;w.Progress=2f+(w.Slot%7)*.13f;
                    var center=crew?.Camp??s.Camp;var radius=crew?.Script.harvestRadiusMeters??100f;
                    var candidates=index.Nearby(center,radius).Where(t=>!claimed.Contains(t.InstanceId)).OrderBy(t=>(TreePoint(d,t)-w.Position).sqrMagnitude).ToList();
                    if(w.SearchOffset>=candidates.Count)w.SearchOffset=0;
                    foreach(var candidate in candidates.Skip(w.SearchOffset))
                    {
                        if(routeBudget<=0)break;
                        var point=TreePoint(d,candidate);var approach=(w.Position-point).normalized;if(approach.sqrMagnitude<.1f)approach=Vector2.down;
                        routeBudget--;w.SearchOffset++;var path=route(w.Position,point+approach*1.1f);if(path==null)continue;
                        w.SearchOffset=0;
                        w.TreeId=candidate.InstanceId;claimed.Add(w.TreeId);w.Route=path;w.Activity=AxemanActivity.Walking;w.Progress=0;break;
                    }
                    continue;
                }
                if(tree==null)continue;
                if(w.Activity is AxemanActivity.Chopping or AxemanActivity.WaitingForFall && Vector2.Distance(w.Position,TreePoint(d,tree))>2f)
                {claimed.Remove(w.TreeId);w.TreeId="";w.Activity=AxemanActivity.Waiting;w.Progress=0;w.Route.Clear();continue;}
                if(w.Activity==AxemanActivity.Walking)
                {
                    if(w.Route==null){claimed.Remove(w.TreeId);w.TreeId="";w.Activity=AxemanActivity.Waiting;continue;}
                    var goal=w.Route.Count>0?w.Route[w.Route.Count-1]:w.Position;
                    if(!Move(w,goal,dt,walkable))continue;
                    // A moved target is reacquired through a fresh path.
                    if(Vector2.Distance(w.Position,TreePoint(d,tree))>2f){claimed.Remove(w.TreeId);w.TreeId="";w.Activity=AxemanActivity.Waiting;continue;}
                    w.Facing=(TreePoint(d,tree)-w.Position).normalized;w.Activity=tree.HarvestState==DistrictTreeHarvestState.Fallen?AxemanActivity.WaitingForFall:AxemanActivity.Chopping;w.Progress=0;
                }
                else if(w.Activity==AxemanActivity.Chopping)
                {
                    w.Progress+=dt;
                    if(tree.HarvestState==DistrictTreeHarvestState.Fallen || w.Progress>=ChopDuration)
                    {
                        if(DistrictTreeHarvest.FellForTransport(d,tree,0)){changes.Changed.Add(tree.InstanceId);changes.Falling.Add(tree.InstanceId);index.Update(tree);}
                        w.Activity=AxemanActivity.WaitingForFall;w.Progress=0;changes.Durable=true;
                    }
                }
                else if(w.Activity==AxemanActivity.WaitingForFall)
                {
                    w.Progress+=dt;if(w.Progress<1.7f)continue;
                    w.CargoAlreadyCredited=tree.WoodCredited;var taken=DistrictTreeHarvest.TakeWood(tree,int.MaxValue);w.Cargo+=taken;if(taken>0)w.CargoTrees++;changes.Changed.Add(tree.InstanceId);index.Update(tree);claimed.Remove(w.TreeId);w.TreeId="";w.Route=null;w.RouteRetry=0;w.Activity=AxemanActivity.Delivering;w.Progress=0;changes.Durable=true;
                }
            }
            return changes;
        }
        private static bool Move(DistrictAxeman w,Vector2 target,float dt,Func<Vector2,bool> walkable)
        {
            if(w.Route==null)return false;
            float distance=WalkSpeed*dt;
            while(w.Route.Count>0 && distance>0)
            {
                var next=w.Route[0];var delta=next-w.Position;var step=Vector2.MoveTowards(w.Position,next,distance);
                if(!walkable(step)){w.Route=null;return false;}
                if(delta.sqrMagnitude>.0001f)w.Facing=delta.normalized;
                distance-=Vector2.Distance(w.Position,step);w.Position=step;
                if(Vector2.Distance(step,next)<.001f)w.Route.RemoveAt(0);else break;
            }
            return w.Route.Count==0 && Vector2.Distance(w.Position,target)<.1f;
        }
    }
}
