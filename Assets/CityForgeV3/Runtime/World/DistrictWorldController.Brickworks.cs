using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        Transform brickworksRoot;readonly Dictionary<string,GameObject> brickworksViews=new();
        GameObject brickworksGhost;LineRenderer brickworksOutline;
        public string BrickworksPlacementReason(RegionCityTile d,DistrictBrickworksSite s)=>DistrictBrickworks.PlacementReason(d,s,new DistrictLaborNavigation(d,IsUnderRiverWater).Walkable);
        GameObject CreateBrickworks(Transform parent)
        {
            var prefab=Resources.Load<GameObject>(DistrictBrickworks.ResourcePath);if(prefab==null)return null;
            var go=Instantiate(prefab,parent);go.transform.localScale*=DistrictBrickworks.PresentationScale;var material=Resources.Load<Material>("CityForgeV3/Industry/BrickworksV01/BrickworksMaterial");
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;return go;
        }
        public void PresentBrickworks(RegionCityTile d)
        {
            if(brickworksRoot==null||brickworksRoot.parent!=_content)
            {brickworksRoot=new GameObject("District Brickworks").transform;brickworksRoot.SetParent(_content,false);brickworksViews.Clear();}
            foreach(var id in brickworksViews.Keys.Where(id=>!(d.Brickworks??new()).Any(b=>b.Id==id)).ToArray())
            {brickworksViews[id].SetActive(false);Destroy(brickworksViews[id]);brickworksViews.Remove(id);}
            foreach(var b in d.Brickworks??new())
            {
                if(brickworksViews.ContainsKey(b.Id))continue;
                var root=new GameObject("Brickworks — "+b.Id);root.transform.SetParent(brickworksRoot,false);
                var p=DistrictBrickworks.Point(d,b);root.transform.localPosition=new Vector3(p.x,TerrainElevation(p.x,p.y),p.y);root.transform.localRotation=Quaternion.Euler(0,b.Yaw,0);
                CreateBrickworks(root.transform);brickworksViews[b.Id]=root;
                DistrictActionResult Rotate(float degrees)
                {
                    bool deliveries = d.StoneSites.Any(q => q.Phase == "delivering" || q.Phase == "unloading");
                    var candidate = JsonUtility.FromJson<DistrictBrickworksSite>(JsonUtility.ToJson(b));
                    candidate.Yaw = Mathf.Repeat(b.Yaw + degrees, 360);
                    var reason = BrickworksPlacementReason(d, candidate);
                    if (deliveries) reason += " Active stone deliveries may need a new approach.";
                    b.Yaw = candidate.Yaw; root.transform.localRotation = Quaternion.Euler(0, b.Yaw, 0);
                    return DistrictActionResult.Applied(reason);
                }
                RegisterSelectable(root, new DistrictSelectionRef(DistrictSelectionKind.Entity, "brickworks:" + b.Id),
                    "BRICKWORKS", "Rotate the Brickworks. Placement or delivery conflicts are reported below.", true, null,
                    new DistrictSelectionAction("↶ LEFT 90°", () => Rotate(-90)),
                    new DistrictSelectionAction("RIGHT 90° ↷", () => Rotate(90))).WithBuildingDeletion(() => d.Brickworks.Remove(b), refresh: () => PresentBrickworks(d));
            }
        }
        public void ShowBrickworksPlacement(RegionCityTile d,DistrictBrickworksSite site)
        {
            if(brickworksGhost==null)
            {
                brickworksGhost=new GameObject("Brickworks placement preview");brickworksGhost.transform.SetParent(_content,false);CreateBrickworks(brickworksGhost.transform);
                brickworksOutline=brickworksGhost.AddComponent<LineRenderer>();brickworksOutline.useWorldSpace=false;brickworksOutline.loop=true;brickworksOutline.positionCount=4;brickworksOutline.widthMultiplier=.2f;
                var material=new Material(Shader.Find("Sprites/Default"));brickworksOutline.sharedMaterial=material;brickworksGhost.AddComponent<QuarryMaterials>().Owned.Add(material);
                brickworksOutline.SetPositions(new[]{new Vector3(-DistrictBrickworks.HalfWidth,.3f,-DistrictBrickworks.HalfDepth),new Vector3(DistrictBrickworks.HalfWidth,.3f,-DistrictBrickworks.HalfDepth),new Vector3(DistrictBrickworks.HalfWidth,.3f,DistrictBrickworks.HalfDepth),new Vector3(-DistrictBrickworks.HalfWidth,.3f,DistrictBrickworks.HalfDepth)});
            }
            brickworksGhost.SetActive(true);var p=DistrictBrickworks.Point(d,site);
            brickworksGhost.transform.localPosition=new Vector3(p.x,TerrainElevation(p.x,p.y),p.y);brickworksGhost.transform.localRotation=Quaternion.Euler(0,site.Yaw,0);
            var valid=string.IsNullOrEmpty(BrickworksPlacementReason(d,site));brickworksOutline.startColor=brickworksOutline.endColor=valid?Color.green:Color.red;
        }
        public void HideBrickworksPlacement(){if(brickworksGhost!=null)brickworksGhost.SetActive(false);}
        bool PlanQuarryRoute(QuarryView view,DistrictStoneSite site,List<Vector2> route,DistrictQuarryNavigation nav)
        {
            if(route==null||route.Count==0)return false;
            bool Clear(Vector3 a,Vector3 b)=>nav.Segment(TimberLocal(a),TimberLocal(b));
            // First try a complete smooth convoy path; the road graph supplies longer routes around obstacles.
            Vector3? heading=site.Phase=="returning"?_content.TransformDirection(Quaternion.Euler(0,site.Yaw,0)*Vector3.forward):null;
            var direct=view.Wagon.Plan(new[]{TimberWorld(route.Last())},Clear,heading);
            if(direct!=null){view.Wagon.SetRoute(direct);view.Route=route;return true;}
            // Try joining farther along the first road: a sideways driveway needs a gradual approach.
            var starts=new List<List<Vector3>>{new()};var joins=new List<int>{0};
            for(int join=1;join<Mathf.Min(route.Count,9);join++)
            {
                int next=join+1;while(next<route.Count&&(route[next]-route[join]).sqrMagnitude<.01f)next++;
                var direction=next<route.Count?route[next]-route[join]:Vector2.zero;
                var prefix=view.Wagon.Plan(new[]{TimberWorld(route[join])},Clear,direction.sqrMagnitude>.01f?_content.TransformDirection(new Vector3(direction.x,0,direction.y)):(Vector3?)null);
                if(prefix!=null){starts.Add(prefix);joins.Add(join);}
            }
            for(int candidate=0;candidate<starts.Count;candidate++)
            {
                int join=joins[candidate];
                if(!heading.HasValue)
                {
                    var path=new List<Vector3>(starts[candidate]);path.AddRange(RoundedRoadRoute(route.Skip(join).ToList()));
                    if(!view.Wagon.RouteClear(path,Clear))continue;
                    view.Wagon.SetRoute(path);view.Route=route;return true;
                }
                // Plan a forward arrival into the loading bay, then validate the entire convoy from its real pose.
                for(int cut=route.Count-2;cut>=Mathf.Max(join+1,route.Count-7);cut--)
                {
                    var incoming=route[cut]-route[cut-1];if(incoming.sqrMagnitude<.01f)continue;
                    var suffix=QuarryArrival(view.Wagon,TimberWorld(route[cut]),incoming,TimberWorld(route.Last()),heading.Value,Clear);
                    if(suffix==null)continue;
                    var path=new List<Vector3>(starts[candidate]);path.AddRange(RoundedRoadRoute(route.Skip(join).Take(cut-join+1).ToList()));path.AddRange(suffix);
                    if(!view.Wagon.RouteClear(path,Clear))continue;
                    view.Wagon.SetRoute(path);view.Route=route;return true;
                }
            }
            return false;
        }
        List<Vector3> QuarryArrival(HorseCarriageController wagon,Vector3 from,Vector2 direction,Vector3 goal,Vector3 heading,System.Func<Vector3,Vector3,bool> clear)
        {
            var motion=wagon.CaptureMotion();var headPosition=wagon.transform.position;var headRotation=wagon.transform.rotation;
            var bodyPosition=wagon.Carriage.position;var bodyRotation=wagon.Carriage.rotation;var frontPosition=wagon.Forecarriage.position;var frontRotation=wagon.Forecarriage.rotation;
            try
            {
                wagon.transform.position=from;
                float yaw=Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg+_content.eulerAngles.y-wagon.transform.parent.eulerAngles.y;
                wagon.RestoreHeadings(yaw,yaw,yaw);
                return wagon.Plan(new[]{goal},clear,heading);
            }
            finally
            {
                wagon.transform.SetPositionAndRotation(headPosition,headRotation);wagon.Carriage.SetPositionAndRotation(bodyPosition,bodyRotation);wagon.Forecarriage.SetPositionAndRotation(frontPosition,frontRotation);wagon.RestoreMotion(motion);
            }
        }

#if UNITY_EDITOR
        public string DiagnoseQuarryDelivery(RegionCityTile d,DistrictStoneSite site)
        {
            var nav=new DistrictQuarryNavigation(d,site,IsUnderRiverWater);var view=quarryViews[site.Id];var result="";
            bool Clear(Vector3 a,Vector3 b)=>nav.Segment(TimberLocal(a),TimberLocal(b));
            result+="head clear="+Clear(view.Wagon.transform.position,view.Wagon.transform.position)+" axle clear="+Clear(view.Wagon.Carriage.position,view.Wagon.Carriage.position)+"\n";
            foreach(var target in nav.Destinations(site.WagonPosition))
            {
                result+="route="+string.Join(";",target.Route)+"\n";
                result+="raw="+view.Wagon.RouteClear(RoundedRoadRoute(target.Route),Clear)+"\n";
                for(int j=1;j<target.Route.Count;j++)
                {
                    var prefix=view.Wagon.Plan(new[]{TimberWorld(target.Route[j])},Clear,_content.TransformDirection(Vector3.forward));
                    if(prefix==null){result+="join "+j+" none\n";continue;}
                    var path=new List<Vector3>(prefix);path.AddRange(RoundedRoadRoute(target.Route.Skip(j).ToList()));
                    result+="join "+j+" prefix="+prefix.Count+" complete="+view.Wagon.RouteClear(path,Clear)+"\n";
                }
            }
            return result;
        }
#endif
        public bool TickQuarryDeliveries(RegionCityTile d,bool running,float dt)
        {
            if(!running||dt<=0)return false;bool changed=false;
            int navigationKey=17;
            unchecked
            {
                foreach(var road in d.Roads??new())navigationKey=navigationKey*31+road.GridX*397+road.GridZ;
                foreach(var tree in d.Flora??new())navigationKey=navigationKey*31+tree.NormalizedX.GetHashCode()+tree.NormalizedZ.GetHashCode()+(int)tree.HarvestState;
                foreach(var b in d.Brickworks??new())navigationKey=navigationKey*31+b.Id.GetHashCode()+b.NormalizedX.GetHashCode()+b.NormalizedZ.GetHashCode()+b.Yaw.GetHashCode();
            }
            foreach(var site in d.StoneSites.Where(s=>s.Built&&s.Enabled))
            {
                if(!quarryViews.TryGetValue(site.Id,out var view)||view.Wagon==null)continue;
                if(site.Phase!="full"&&site.Phase!="delivering"&&site.Phase!="unloading"&&site.Phase!="returning")continue;
                if(view.Navigation==null||view.NavigationKey!=navigationKey)
                {view.Navigation=new DistrictQuarryNavigation(d,site,IsUnderRiverWater);view.NavigationKey=navigationKey;view.Route=null;view.Retry=0;}
                var nav=view.Navigation;
                if(!site.HasWagonPose)SaveQuarryWagonPose(site,view.Wagon);
                changed|=DistrictQuarryDelivery.Tick(d,site,dt,
                    s=>{
                        foreach(var target in nav.Destinations(s.WagonPosition))if(PlanQuarryRoute(view,s,target.Route,nav))return target;
                        return null;
                    },
                    s=>{
                        if(!ReferenceEquals(view.Route,s.DeliveryRoute)||!view.Wagon.IsMoving||view.Wagon.WasBlocked)
                        {
                            view.Retry-=dt;if(view.Retry>0)return false;view.Retry=3;
                            var fresh=nav.Route(s.WagonPosition,s.DeliveryDestination);
                            if(!PlanQuarryRoute(view,s,fresh,nav))
                            {s.DeliveryStatus=s.Phase=="returning"?"Return route blocked — clear the road and quarry access":"Bricksworks required — clear the road and wagon turning space.";return false;}
                            s.DeliveryRoute=fresh;view.Route=fresh;s.DeliveryStatus=s.Phase=="returning"?"Returning to quarry":"Delivering stone to Brickworks";
                        }
                        bool Clear(Vector3 a,Vector3 b)=>nav.Segment(TimberLocal(a),TimberLocal(b));
                        view.Wagon.Step(dt,p=>{
                            var local=TimberLocal(p);float distance=Vector2.Distance(local,DistrictQuarry.Point(d,site));
                            return TimberGround(p)+.28f*(1-Mathf.InverseLerp(13,20,distance));
                        },Clear);
                        SaveQuarryWagonPose(s,view.Wagon);
                        return !view.Wagon.IsMoving&&!view.Wagon.WasBlocked&&Vector2.Distance(s.WagonPosition,s.DeliveryDestination)<.3f;
                    });
            }
            return changed;
        }
        void SaveQuarryWagonPose(DistrictStoneSite s,HorseCarriageController wagon)
        {
            s.HasWagonPose=true;s.WagonPosition=TimberLocal(wagon.transform.position);
            s.HorseHeading=wagon.transform.eulerAngles.y-_content.eulerAngles.y;s.BodyHeading=wagon.Carriage.eulerAngles.y-_content.eulerAngles.y;s.FrontHeading=wagon.Forecarriage.eulerAngles.y-_content.eulerAngles.y;
        }
    }
}
