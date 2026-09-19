using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    // A kinematic drawbar: the horse leads and the rear axle follows at a fixed
    // distance. The wagon's heading is derived from the axle, never copied
    // directly from the horse's heading.
    public sealed class HorseCarriageController : MonoBehaviour
    {
        public const float HorseOffset = 2.05f;
        public const float RearAxleOffset = 2.0295f;
        public const float FrontAxleOffset=.176f;
        public const float ShaftLength=HorseOffset-FrontAxleOffset;
        public const float Wheelbase=RearAxleOffset+FrontAxleOffset;
        public const float HitchLength = HorseOffset + RearAxleOffset;
        [SerializeField] HorseWagonDefinition definition;
        public HorseWagonDefinition Definition=>definition??HorseWagonDefinition.Carriage;
        public Transform Forecarriage { get; private set; }
        public Transform Horse { get; private set; }
        public Transform SecondHorse { get; private set; }
        public Transform Carriage { get; private set; }
        public const float Acceleration = .55f;
        public const float Deceleration = .85f;
        public float CurrentSpeed { get; private set; }
        public bool IsStopping { get; private set; }
        float distanceToEnd;
        public bool Fast { get; set; }
        public float MotionSpeedMultiplier { get; set; } = 1f;
        public float SpeedMetersPerSecond => HorseGaitController.WalkMetersPerSecond *
            (Fast ? 2f : 1f) * Mathf.Max(.1f, MotionSpeedMultiplier);
        public bool IsMoving => path != null && cursor < path.Count;
        public float ArticulationDegrees => Quaternion.Angle(Horse.rotation,Carriage.rotation);
        public float HitchError => Mathf.Max(Mathf.Abs(Vector3.Distance(Flat(transform.position),Flat(Forecarriage.position))-Definition.ShaftLength),Mathf.Abs(Vector3.Distance(Flat(Forecarriage.position),Flat(Carriage.TransformPoint(Vector3.back*Definition.RearAxleOffset)))-Definition.Wheelbase));
        public bool WasBlocked { get; private set; }
        List<Vector3> path;
        int cursor;
        int loopStart = -1;
        Vector3 rear,front;
        [SerializeField] List<LineRenderer> straps = new();
        Material leather;
        static Vector3 Flat(Vector3 p) { p.y=0; return p; }
        public void Configure(Transform horse,Transform carriage,HorseWagonDefinition vehicle=null,Transform secondHorse=null)
        {
            definition=vehicle??HorseWagonDefinition.Carriage;
            Horse=horse; SecondHorse=secondHorse; Carriage=carriage;Forecarriage=carriage.Find("Forecarriage Steering");
            carriage.localPosition=Vector3.back*Definition.HorseOffset;
            leather=new Material(Shader.Find("Standard"));
            leather.color=new Color(.12f,.075f,.04f);leather.SetFloat("_Glossiness",.12f);
            for(var i=0;i<(secondHorse!=null?10:5);i++)
            {
                var go=new GameObject(i%5==4?"Harness Girth":"Harness Strap");go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=leather;line.useWorldSpace=true;
                line.widthMultiplier=i%5==4?.055f:.025f;line.positionCount=i%5==4?17:3;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                straps.Add(line);
            }
        }
        void OnEnable()
        {
            if(straps.Count==0)
                foreach(Transform child in transform)
                    if(child.name.StartsWith("Harness ")&&child.TryGetComponent<LineRenderer>(out var line))straps.Add(line);
            if(leather==null&&straps.Count>0)leather=straps[0].sharedMaterial;
        }
        void OnDestroy(){if(leather!=null)Destroy(leather);}
        public sealed class MotionState
        { public List<Vector3> Path; public int Cursor; public int LoopStart = -1; public Vector3 Rear,Front; public float Speed, DistanceToEnd; public bool Stopping; }
        public MotionState CaptureMotion()=>new MotionState { Path=path,Cursor=cursor,LoopStart=loopStart,Rear=rear,Front=front,Speed=CurrentSpeed,Stopping=IsStopping,DistanceToEnd=distanceToEnd };
        public void RestoreMotion(MotionState state)
        {if(state==null)return;path=state.Path;cursor=state.Cursor;loopStart=state.LoopStart;rear=state.Rear;front=state.Front;CurrentSpeed=state.Speed;IsStopping=state.Stopping;distanceToEnd=state.DistanceToEnd;}
        public void RestoreHeadings(float horseYaw,float carriageYaw,float forecarriageYaw)
        {
            transform.localRotation=Quaternion.Euler(0,horseYaw,0);
            Carriage.rotation=(transform.parent!=null?transform.parent.rotation:Quaternion.identity)*Quaternion.Euler(0,carriageYaw,0);
            var foreRotation=(transform.parent!=null?transform.parent.rotation:Quaternion.identity)*Quaternion.Euler(0,forecarriageYaw,0);
            front=Flat(transform.position)-(foreRotation*Vector3.forward)*Definition.ShaftLength;
            var body=front-Carriage.forward*Definition.FrontAxleOffset;
            Carriage.position=new Vector3(body.x,transform.position.y,body.z);
            Forecarriage.rotation=foreRotation;
            rear=front-Carriage.forward*Definition.Wheelbase;
        }
        static Vector2 Left(Vector2 v)=>new Vector2(-v.y,v.x);
        static float Angle(Vector2 v)=>Mathf.Atan2(v.y,v.x);
        static Vector3 GroundPoint(Vector2 v)=>new Vector3(v.x,0,v.y);
        static void Arc(List<Vector3> points,Vector2 center,Vector2 from,Vector2 to,int turn,float radius)
        {
            var begin=Angle(from-center);var sweep=Mathf.Repeat((Angle(to-center)-begin)*turn,Mathf.PI*2f);
            if(sweep<.0001f||sweep>Mathf.PI*2f-.0001f)return;
            var count=Mathf.Max(1,Mathf.CeilToInt(sweep*radius/.10f));
            for(var i=1;i<=count;i++)
            {var a=begin+turn*sweep*i/count;points.Add(GroundPoint(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius));}
        }
        static void Straight(List<Vector3> points,Vector2 from,Vector2 to)
        {
            var count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/.10f));
            for(var i=1;i<=count;i++)points.Add(GroundPoint(Vector2.Lerp(from,to,(float)i/count)));
        }
        public List<Vector3> Plan(IReadOnlyList<Vector3> waypoints,Func<Vector3,Vector3,bool> clear, Vector3? arrivalDirection = null)
        {
            if(waypoints.Count==0)return new List<Vector3>();
            var start=new Vector2(transform.position.x,transform.position.z);
            var last=waypoints[waypoints.Count-1];var goal=new Vector2(last.x,last.z);
            if(Vector2.Distance(start,goal)<.03f)return new List<Vector3>();
            var forward=new Vector2(transform.forward.x,transform.forward.z).normalized;
            var radius=Definition.TurningRadius;
            var candidates=new List<(float length,List<Vector3> points)>();
            // Common tangents connect two turning circles. Try both turn
            // directions and free arrival headings, then select the shortest
            // complete convoy route that clears the lot. This also gives a
            // nearby destination behind the horse a real forward U-turn.
            for(var heading=0;heading<(arrivalDirection.HasValue?1:26);heading++)
            {
                var angle=arrivalDirection.HasValue?Angle(new Vector2(arrivalDirection.Value.x,arrivalDirection.Value.z)):heading<24?heading*Mathf.PI/12:heading==24?Angle(goal-start):Angle(forward);
                var endForward=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                for(var first=-1;first<=1;first+=2)for(var second=-1;second<=1;second+=2)
                {
                    var c1=start+Left(forward)*radius*first;var c2=goal+Left(endForward)*radius*second;
                    var delta=c2-c1;var distance=delta.magnitude;if(distance<.001f)continue;
                    var lineAngle=Angle(delta);
                    if(first!=second)
                    {if(distance<radius*2)continue;lineAngle+=Mathf.Asin(radius*2*first/distance);}
                    var direction=new Vector2(Mathf.Cos(lineAngle),Mathf.Sin(lineAngle));
                    var p1=c1-Left(direction)*radius*first;var p2=c2-Left(direction)*radius*second;
                    if(Vector2.Dot(p2-p1,direction)<-.001f)continue;
                    var points=new List<Vector3>();Arc(points,c1,start,p1,first,radius);Straight(points,p1,p2);Arc(points,c2,p2,goal,second,radius);
                    if(points.Count==0||Vector3.Distance(points[points.Count-1],GroundPoint(goal))>.001f)points.Add(GroundPoint(goal));
                    var length=0f;var previous=GroundPoint(start);
                    foreach(var point in points){length+=Vector3.Distance(previous,point);previous=point;}
                    candidates.Add((length,points));
                }
            }
            candidates.Sort((a,b)=>a.length.CompareTo(b.length));
            foreach(var candidate in candidates)if(RouteClear(candidate.points,clear))return candidate.points;
            return null;
        }
        public List<Vector3> PlanRouteWithTurnaround(
            IReadOnlyList<Vector3> waypoints,
            Func<Vector3,Vector3,bool> clear)
        {
            if (waypoints == null || waypoints.Count == 0) return null;
            var route = new List<Vector3>();
            var start = Flat(transform.position);
            foreach (var point in waypoints)
                if (route.Count > 0 || Vector3.Distance(start, Flat(point)) > .05f)
                    route.Add(Flat(point));
            if (route.Count == 0) return route;
            var firstDirection = route[0] - start;
            if (firstDirection.sqrMagnitude < .0001f ||
                Vector3.Dot(transform.forward, firstDirection.normalized) >= 0f)
                return route;

            // A horse cannot reverse a cart. Join the new route far enough
            // ahead for the complete team to turn, then follow its remaining
            // road centerline in the opposite direction.
            var joinDistance = Mathf.Max(Definition.TeamLength,
                Definition.TurningRadius * 2f);
            var join = route.Count - 1;
            for (var index = 0; index < route.Count; index++)
                if (Vector3.Distance(start, route[index]) >= joinDistance)
                { join = index; break; }
            var arrival = join + 1 < route.Count
                ? route[join + 1] - route[join]
                : route[join] - (join > 0 ? route[join - 1] : start);
            if (arrival.sqrMagnitude < .0001f)
                arrival = -transform.forward;
            var turn = Plan(new[] { route[join] }, clear,
                arrival.normalized);
            if (turn == null) return null;
            for (var index = join + 1; index < route.Count; index++)
                turn.Add(route[index]);
            return turn;
        }
        static Vector3 Follow(Vector3 lead,Vector3 follower,float length)
        { return lead-(lead-follower).normalized*length; }
        bool LeadSegmentClear(Vector3 from,Vector3 to,Vector3 previousForward,Func<Vector3,Vector3,bool> clear)
        {
            if(!clear(from,to))return false;
            if(Definition.HorseCount<2)return true;
            var direction=to-from;
            if(direction.sqrMagnitude<.0000001f)direction=previousForward;
            var oldRight=Vector3.Cross(Vector3.up,previousForward.normalized)*Definition.HorseSpacing*.5f;
            var newRight=Vector3.Cross(Vector3.up,direction.normalized)*Definition.HorseSpacing*.5f;
            return clear(from-oldRight,to-newRight)&&clear(from+oldRight,to+newRight);
        }
        public bool RouteClear(List<Vector3> proposed,Func<Vector3,Vector3,bool> clear)
        {
            var last=Flat(transform.position);
            var lastForward=transform.forward;
            var axle=Flat(Carriage.TransformPoint(Vector3.back*Definition.RearAxleOffset));
            var frontAxle=Flat(Forecarriage.position);
            foreach(var p in proposed)
            {
                var nextFront=Follow(p,frontAxle,Definition.ShaftLength);
                var nextAxle=Follow(nextFront,axle,Definition.Wheelbase);
                if((p-last).sqrMagnitude>.000001f&&(Vector3.Angle(p-last,p-nextFront)>45f||Vector3.Angle(p-nextFront,nextFront-nextAxle)>55f))return false;
                if(!LeadSegmentClear(last,p,lastForward,clear)||!clear(axle,nextAxle)||!clear(frontAxle,nextFront)||!clear((frontAxle+axle)*.5f,(nextFront+nextAxle)*.5f))return false;
                if((p-last).sqrMagnitude>.0000001f)lastForward=(p-last).normalized;
                last=p;axle=nextAxle;frontAxle=nextFront;
            }
            return true;
        }
        public void Stop()
        {
            IsStopping=IsMoving;
            if(CurrentSpeed<=.001f)Park();
        }
        void Park(){path=null;loopStart=-1;CurrentSpeed=0;IsStopping=false;}
        public void SetRoute(List<Vector3> route, int repeatFrom = -1)
        { path=route;loopStart=repeatFrom>=0&&repeatFrom<route.Count?repeatFrom:-1;cursor=0;rear=Flat(Carriage.TransformPoint(Vector3.back*Definition.RearAxleOffset));front=Flat(Forecarriage.position);WasBlocked=false;IsStopping=false;
            distanceToEnd=0;var previous=Flat(transform.position);
            foreach(var point in route){distanceToEnd+=Vector3.Distance(previous,point);previous=point;}
        }
        public bool Step(float dt,Func<Vector3,float> ground,Func<Vector3,Vector3,bool> clear)
        {
            if(!IsMoving)return false;
            // Float accumulation over a long route can exhaust the braking
            // distance a few millimeters before the final waypoint. Finish
            // only when both the remaining path and actual endpoint are near;
            // a loop or a route passing near its destination must keep moving.
            if(loopStart<0 && distanceToEnd<=.01f &&
                Vector3.Distance(Flat(transform.position),path[path.Count-1])<=.01f)
            {Park();return true;}
            dt=Mathf.Clamp(dt,0,.05f);
            var target=IsStopping?0f:SpeedMetersPerSecond;
            if(loopStart<0)
            {
                // Reserve one integration step so braking starts before the
                // discrete frame crosses the continuous stopping-distance limit.
                var reaction=Deceleration*dt;
                var arrivalSpeed=Mathf.Sqrt(reaction*reaction+2f*Deceleration*Mathf.Max(0,distanceToEnd))-reaction;
                target=Mathf.Min(target,arrivalSpeed);
            }
            var oldSpeed=CurrentSpeed;
            CurrentSpeed=Mathf.MoveTowards(CurrentSpeed,target,
                (target>CurrentSpeed?Acceleration:Deceleration)*
                Mathf.Max(.1f, MotionSpeedMultiplier)*dt);
            var remaining=(oldSpeed+CurrentSpeed)*.5f*dt;
            var brakingFinished=IsStopping&&CurrentSpeed<=.001f;
            var lead=Flat(transform.position);var oldLead=lead;var leadForward=transform.forward;
            while(remaining>0 && cursor<path.Count)
            {
                var gap=Vector3.Distance(lead,path[cursor]);
                if(gap<.0001f){cursor++;continue;}
                var step=Mathf.Min(gap,remaining);var next=Vector3.MoveTowards(lead,path[cursor],step);
                var nextFront=Follow(next,front,Definition.ShaftLength);
                var axle=Follow(nextFront,rear,Definition.Wheelbase);
                if(!LeadSegmentClear(lead,next,leadForward,clear)||!clear(rear,axle)||!clear(front,nextFront)||!clear((front+rear)*.5f,(nextFront+axle)*.5f))
                {Park();WasBlocked=true;break;}
                if((next-lead).sqrMagnitude>.0000001f)leadForward=(next-lead).normalized;
                rear=axle;front=nextFront;lead=next;remaining-=step;distanceToEnd=Mathf.Max(0,distanceToEnd-step);
                if(step>=gap-.00001f)cursor++;
            }
            if(path!=null&&cursor>=path.Count)
            {
                if(loopStart>=0)cursor=loopStart;
                else Park();
            }
            if(brakingFinished)Park();
            var heading=lead-oldLead;
            if(heading.sqrMagnitude>.0000001f)transform.rotation=Quaternion.LookRotation(heading);
            transform.position=new Vector3(lead.x,ground(lead),lead.z);
            var wagonForward=(front-rear).normalized;
            var wagon=rear+wagonForward*Definition.RearAxleOffset;
            Carriage.SetPositionAndRotation(new Vector3(wagon.x,ground(wagon),wagon.z),Quaternion.LookRotation(wagonForward));
            Forecarriage.rotation=Quaternion.LookRotation((lead-front).normalized);
            return true;
        }
        void LateUpdate()
        {
            if(Horse==null||Carriage==null||straps.Count!=(SecondHorse!=null?10:5))return;
            UpdateHarness(Horse,0);
            if(SecondHorse!=null)UpdateHarness(SecondHorse,5);
        }
        void UpdateHarness(Transform animal,int start)
        {
            var lateral=animal.localPosition.x;
            for(var side=0;side<2;side++)
            {
                var sign=side==0?-1f:1f;
                var a=Forecarriage.TransformPoint(new Vector3(lateral+sign*Definition.ShaftAnchor.x,Definition.ShaftAnchor.y,Definition.ShaftAnchor.z-Definition.FrontAxleOffset));
                var b=animal.TransformPoint(new Vector3(sign*.34f,1.12f,.45f));
                straps[start+side].SetPosition(0,a);straps[start+side].SetPosition(1,(a+b)*.5f-Vector3.up*.035f);straps[start+side].SetPosition(2,b);
                a=Carriage.TransformPoint(new Vector3(sign*Definition.DriverHands.x,Definition.DriverHands.y,Definition.DriverHands.z));
                b=animal.TransformPoint(new Vector3(sign*.12f,1.55f,1.10f));
                straps[start+side+2].SetPosition(0,a);straps[start+side+2].SetPosition(1,(a+b)*.5f-Vector3.up*.08f);straps[start+side+2].SetPosition(2,b);
            }
            for(var i=0;i<=16;i++)
            { var a=i*Mathf.PI*2/16;straps[start+4].SetPosition(i,animal.TransformPoint(new Vector3(Mathf.Cos(a)*.36f,1.27f+Mathf.Sin(a)*.37f,.05f))); }
        }
    }
}
