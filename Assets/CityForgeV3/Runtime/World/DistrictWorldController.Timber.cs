using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        sealed class TimberView
        {
            public HorseCarriageController Wagon;
            public Transform Cargo;
            public List<Vector2> Route;
            public float Retry;
        }
        readonly Dictionary<string, TimberView> timberViews = new();
        Transform timberRoot;
        LotWorldController timberFactory;
        GameObject axemanPreview;
        bool marksmanPreview;
        public GameObject CreateAxemanVisual(Transform parent, string name)
        {
            var prefab = Resources.Load<GameObject>(AxemanResource); if (prefab == null) return null;
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            var model = Instantiate(prefab, root.transform); model.transform.localScale = Vector3.one * 1.85f;
            var owner = root.AddComponent<CharacterShadowMaterialOwner>();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    owner.Add(material);
                    material.mainTexture = Resources.Load<Texture2D>("Characters/AxemanLaborV01/" +
                        (material.name.Contains("3ece3eba") ? "AxeBaseColor" : "AxemanBaseColor"));
                    material.color = Color.white; material.SetFloat("_Metallic", 0); material.SetFloat("_Glossiness", .15f);
                }
            }
            var animator = model.GetComponentInChildren<Animator>() ?? model.AddComponent<Animator>();
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            root.AddComponent<ThreeDimensionalCharacterAnimator>().Initialize(animator, Resources.LoadAll<AnimationClip>(AxemanResource));
            return root;
        }
        public void ShowAxemanPlacement(Vector2 normalized, bool valid,bool marksman=false)
        {
            if (_content == null) return;
            if(axemanPreview!=null&&marksmanPreview!=marksman){Destroy(axemanPreview);axemanPreview=null;}
            marksmanPreview=marksman;
            if (axemanPreview == null)
            {
                axemanPreview = marksman ? CreateWildlifeActor(LotWorldController.MusketmanCharacterId,_content,"Marksman Placement Preview") : CreateAxemanVisual(_content, "Axeman Placement Preview");
                if (axemanPreview != null)
                    foreach (var renderer in axemanPreview.GetComponentsInChildren<Renderer>())
                    {
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        foreach (var material in renderer.sharedMaterials)
                        {
                            var texture = material.mainTexture;
                            var shader = Resources.Load<Shader>("CityForgeV3/Shaders/LaborPlacementGhost");
                            if (shader != null) { material.shader = shader; material.mainTexture = texture; }
                        }
                    }
            }
            if (axemanPreview == null) return;
            var x = (normalized.x - .5f) * DistrictScale.SizeMeters(_terrainDistrict.Width);
            var z = (normalized.y - .5f) * DistrictScale.SizeMeters(_terrainDistrict.Height);
            axemanPreview.SetActive(true);
            axemanPreview.transform.localPosition = new Vector3(x, TerrainElevation(x,z) + .02f,z);
            foreach (var r in axemanPreview.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials) m.color = valid ? Color.white : new Color(1,.25f,.25f);
        }
        public void HideAxemanPlacement() { if (axemanPreview != null) axemanPreview.SetActive(false); }
        float TimberGround(Vector3 world)
        {
            var local = _content.InverseTransformPoint(world);
            return _content.TransformPoint(new Vector3(local.x, TerrainElevation(local.x,local.z) + .02f, local.z)).y;
        }
        Vector3 TimberWorld(Vector2 p) => _content.TransformPoint(new Vector3(p.x,0,p.y));
        Vector2 TimberLocal(Vector3 p) { var local = _content.InverseTransformPoint(p); return new Vector2(local.x,local.z); }
        // Round each graph corner inside its road cell. The convoy validator
        // checks the complete horse, front axle and rear axle sweep afterward.
        List<Vector3> RoundedRoadRoute(List<Vector2> points)
        {
            var result = new List<Vector3>();
            void Line(Vector2 a, Vector2 b)
            {
                var n = Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.15f));
                for(int i=1;i<=n;i++)result.Add(TimberWorld(Vector2.Lerp(a,b,(float)i/n)));
            }
            if (points.Count == 0) return result;
            var last = points[0]; result.Add(TimberWorld(last));
            for (int i=1;i<points.Count-1;i++)
            {
                var a=points[i-1];var b=points[i];var c=points[i+1];
                var incoming=(b-a).normalized;var outgoing=(c-b).normalized;
                if (Vector2.Dot(incoming,outgoing) > .99f || incoming.sqrMagnitude < .1f || outgoing.sqrMagnitude < .1f) continue;
                var radius=Mathf.Min(3.5f,Vector2.Distance(a,b)*.45f,Vector2.Distance(b,c)*.45f);
                var entry=b-incoming*radius;var exit=b+outgoing*radius;
                Line(last,entry);
                for(int j=1;j<=40;j++){float t=j/40f;var p=(1-t)*(1-t)*entry+2*(1-t)*t*b+t*t*exit;result.Add(TimberWorld(p));}
                last=exit;
            }
            Line(last,points[points.Count-1]);return result;
        }
        // Use both sides of a road for a forward turnaround, rather than
        // starting a full-diameter U-turn at the center line. Every candidate
        // still has to pass the complete articulated convoy's clearance check.
        List<Vector3> TimberTurnaround(TimberView view, List<Vector2> route,
            System.Func<Vector3,Vector3,bool> clear)
        {
            var start=view.Wagon.transform.position;start.y=0;
            var forward=view.Wagon.transform.forward;forward.y=0;forward.Normalize();
            var goal=TimberWorld(route[Mathf.Min(1,route.Count-1)]);goal.y=0;
            if(Vector3.Dot((goal-start).normalized,forward)>-.5f)return null;
            var right=Vector3.Cross(Vector3.up,forward);
            var radius=view.Wagon.Definition.TurningRadius;
            foreach(float approach in new[]{12f,10f,14f,8f,16f})
            foreach(float side in new[]{-1f,1f})
            {
                var points=new List<Vector3>{start};
                void Curve(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
                {
                    int count=Mathf.CeilToInt((Vector3.Distance(a,b)+Vector3.Distance(b,c)+Vector3.Distance(c,d))/.1f);
                    for(int n=1;n<=count;n++){float t=(float)n/count,u=1-t;points.Add(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}
                }
                var entry=start+forward*approach-right*side*radius;
                Curve(start,start+forward*approach*.4f,entry-forward*approach*.4f,entry);
                var center=entry+right*side*radius;
                for(int n=1;n<=120;n++){
                    float angle=Mathf.PI*n/120f;
                    points.Add(center-right*side*radius*Mathf.Cos(angle)+forward*radius*Mathf.Sin(angle));
                }
                var exit=center+right*side*radius;
                float length=Vector3.Distance(exit,goal);
                Curve(exit,exit-forward*length*.4f,goal+forward*length*.4f,goal);
                points.AddRange(RoundedRoadRoute(route.Skip(Mathf.Min(1,route.Count-1)).ToList()));
                if(view.Wagon.RouteClear(points,clear))return points;
            }
            return null;
        }
        bool PlanTimber(TimberView view, DistrictTimberCrew crew, List<Vector2> route, DistrictTimberNavigation nav)
        {
            if (route == null || route.Count == 0) return false;
            bool Clear(Vector3 a,Vector3 b)=>nav.Segment(TimberLocal(a),TimberLocal(b));
            var points = new List<Vector2> { crew.WagonPosition }; points.AddRange(route);
            var path = RoundedRoadRoute(points);
            // Straight initial travel needs no joining arc. For a return trip,
            // the accepted wagon planner supplies a forward U-turn on the road.
            if (!view.Wagon.RouteClear(path,Clear))
            {
                var joinIndex = Mathf.Min(1,route.Count-1);
                var direction = route.Count>joinIndex+1 ? route[joinIndex+1]-route[joinIndex] : route[joinIndex]-crew.WagonPosition;
                var join = view.Wagon.Plan(new[]{TimberWorld(route[joinIndex])},Clear,
                    direction.sqrMagnitude>.01f ? (Vector3?)_content.TransformDirection(new Vector3(direction.x,0,direction.y)) : null);
                if (join != null)
                {
                    join.AddRange(RoundedRoadRoute(route.Skip(joinIndex).ToList()));
                    path=view.Wagon.RouteClear(join,Clear)?join:null;
                }
                else path=null;
                path ??= TimberTurnaround(view,route,Clear);
                if(path==null)return false;
            }
            view.Wagon.SetRoute(path);view.Route=route;return true;
        }
#if UNITY_EDITOR
        public string DiagnoseTimber(RegionCityTile d)
        {
            var nav=new DistrictTimberNavigation(d,IsUnderRiverWater);
            var report="";
            foreach(var c in d.Labor.TimberCrews){
                var targets=nav.Mills(c.WagonPosition);
                report+=$"Crew {c.Id} road={nav.OnRoad(c.WagonPosition)} mills={targets.Count}\n";
                if(!timberViews.TryGetValue(c.Id,out var v))continue;
                foreach(var t in targets){
                    var points=new List<Vector2>{c.WagonPosition};points.AddRange(t.Route);
                    bool Clear(Vector3 a,Vector3 b)=>nav.Segment(TimberLocal(a),TimberLocal(b));
                    var ok=v.Wagon.RouteClear(RoundedRoadRoute(points),Clear);
                    report+=$"Mill {t.MillId} end={t.Point} distance={t.Distance} clear={ok} route={string.Join(";",t.Route)}\n";
                }
            }
            return report;
        }
#endif
        public bool TickTimber(RegionCityTile district, LotWorldController factory, bool running, float dt)
        {
            if (_content == null) return false;
            var crews=DistrictLabor.State(district).TimberCrews ??= new();
            // District play may begin without ever opening the lot editor.
            // A dormant presentation factory needs no lot, camera or lighting rig.
            if (factory == null && crews.Count > 0)
            {
                if (timberFactory == null)
                {
                    var helper = new GameObject("Timber Presentation Factory");
                    helper.transform.SetParent(_content,false); helper.SetActive(false);
                    timberFactory = helper.AddComponent<LotWorldController>();
                }
                factory = timberFactory;
            }
            if(timberRoot==null||timberRoot.parent!=_content)
            {timberRoot=new GameObject("Timber Wagons").transform;timberRoot.SetParent(_content,false);timberViews.Clear();}
            foreach(var id in timberViews.Keys.Where(id=>!crews.Any(c=>c.Id==id)).ToArray())
            {if(timberViews[id].Wagon!=null)Destroy(timberViews[id].Wagon.gameObject);timberViews.Remove(id);}
            var nav=new DistrictTimberNavigation(district,IsUnderRiverWater);
            bool durable=false;
            foreach(var crew in crews)
            {
                crew.Script ??= new TimberScript();
                if(!timberViews.TryGetValue(crew.Id,out var view)||view.Wagon==null)
                {
                    var root=factory.CreateHorseCarriagePresentation("Lumber Wagon "+crew.WagonId,1,LotWorldController.HorseForestryWagonPropId,TimberGround);
                    if(root==null){crew.Status="Lumber wagon model unavailable";continue;}
                    root.SetParent(timberRoot,false);root.localPosition=new Vector3(crew.WagonPosition.x,TerrainElevation(crew.WagonPosition.x,crew.WagonPosition.y)+.02f,crew.WagonPosition.y);
                    var wagon=root.GetComponent<HorseCarriageController>();wagon.RestoreHeadings(crew.HorseHeading,crew.BodyHeading,crew.FrontHeading);
                    view=new TimberView{Wagon=wagon,Cargo=new GameObject("Timber Cargo").transform};
                    view.Cargo.SetParent(wagon.Carriage,false);
                    // Cargo is authored separately in Blender. Move it under one
                    // state-controlled group without changing its imported placement.
                    foreach(var item in wagon.Carriage.GetComponentsInChildren<Transform>())
                        if(item.name.StartsWith("Cargo_Log_"))item.SetParent(view.Cargo,true);
                    timberViews[crew.Id]=view;
                }
                view.Wagon.Fast=crew.Script.fastWagon;
                if(running && crew.Enabled)
                {
                    durable |= DistrictTimber.Tick(district,crew,dt,
                        c=>{
                            foreach(var target in nav.Mills(c.WagonPosition))
                                if(PlanTimber(view,c,target.Route,nav))return target;
                            c.Status="Waiting for connected roads and wagon turning room";return null;
                        },
                        (c,to)=>{var route=nav.Route(c.WagonPosition,to);return PlanTimber(view,c,route,nav)?route:null;},
                        (c,step)=>{
                            bool Clear(Vector3 a,Vector3 b)=>nav.Segment(TimberLocal(a),TimberLocal(b));
                            if(!ReferenceEquals(view.Route,c.Route)||view.Wagon.WasBlocked)
                            {
                                view.Retry-=step;
                                if(view.Retry>0)return false;
                                view.Retry=c.Script.retrySeconds;
                                var fresh=nav.Route(c.WagonPosition,c.Destination);
                                if(!PlanTimber(view,c,fresh,nav)){c.Status="Road blocked or no wagon turning room";return false;}
                                c.Route=fresh;view.Route=fresh;c.Status=c.Phase=="returning"?"Returning to crew":"Delivering timber by road";
                            }
                            view.Wagon.Step(step,TimberGround,Clear);
                            c.WagonPosition=TimberLocal(view.Wagon.transform.position);
                            c.HorseHeading=view.Wagon.transform.localEulerAngles.y;
                            c.BodyHeading=view.Wagon.Carriage.eulerAngles.y-_content.eulerAngles.y;
                            c.FrontHeading=view.Wagon.Forecarriage.eulerAngles.y-_content.eulerAngles.y;
                            return !view.Wagon.IsMoving&&!view.Wagon.WasBlocked&&Vector2.Distance(c.WagonPosition,c.Destination)<.2f;
                        });
                }
                view.Cargo.gameObject.SetActive(crew.CargoTrees>0);
            }
            return durable;
        }
    }
}
