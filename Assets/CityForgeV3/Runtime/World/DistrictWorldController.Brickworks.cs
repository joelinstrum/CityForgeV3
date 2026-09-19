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
                    DistrictIndustryRotation.Apply(d, new DistrictSelectionRef(DistrictSelectionKind.Entity, "brickworks:" + b.Id), degrees > 0 ? 1 : -1); root.transform.localRotation = Quaternion.Euler(0, b.Yaw, 0);
                    return DistrictActionResult.Applied(reason);
                }
                RegisterSelectable(root, new DistrictSelectionRef(DistrictSelectionKind.Entity, "brickworks:" + b.Id),
                    "BRICKWORKS", "Rotate the Brickworks. Placement or delivery conflicts are reported below.", true, null,
                    new DistrictSelectionAction("↶ LEFT 90°", () => Rotate(-90)),
                    new DistrictSelectionAction("RIGHT 90° ↷", () => Rotate(90))).WithBuildingDeletion(() => d.Brickworks.Remove(b), refresh: () => PresentBrickworks(d))
                    .WithStatus(() => DistrictBrickworks.WorkStatus(b))
                    .WithWarnings(() => DistrictBrickworks.OperationalWarning(d, b));
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
            if(!SetRoadDeliveryRoute(view.Wagon,route))return false;
            view.Route=route;site.DeliveryDestination=route.Last();return true;
        }

#if UNITY_EDITOR
        public string DiagnoseQuarryDelivery(RegionCityTile d,DistrictStoneSite site)
        {
            var nav=new DistrictQuarryNavigation(d,site,IsUnderRiverWater);var view=quarryViews[site.Id];var result="";
            bool Clear(Vector3 a,Vector3 b)=>RoadDeliveryClear(a,b);
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
            if(!running||dt<=0||d.StoneSites==null||!d.StoneSites.Any(s=>s.Built&&s.Enabled&&(s.Phase=="full"||s.Phase=="delivering"||s.Phase=="unloading"||s.Phase=="returning")))return false;bool changed=false;
            int navigationKey=DistrictRoadPlacementModel.NetworkKey(d);
            unchecked
            {
                foreach(var q in d.StoneSites??new())navigationKey=navigationKey*31+q.Yaw.GetHashCode()+DistrictQuarry.Point(d,q).GetHashCode();
                foreach(var b in d.Brickworks??new())navigationKey=navigationKey*31+b.Id.GetHashCode()+b.NormalizedX.GetHashCode()+b.NormalizedZ.GetHashCode()+b.Yaw.GetHashCode()+DistrictBrickworks.Point(d,b).GetHashCode();
            }
            foreach(var site in d.StoneSites.Where(s=>s.Built&&s.Enabled))
            {
                if(!quarryViews.TryGetValue(site.Id,out var view)||view.Wagon==null)continue;
                if(site.Phase!="full"&&site.Phase!="delivering"&&site.Phase!="unloading"&&site.Phase!="returning")continue;
                if(view.Navigation==null||view.NavigationKey!=navigationKey)
                {view.Navigation=new DistrictQuarryNavigation(d,site,IsUnderRiverWater);view.NavigationKey=navigationKey;view.Route=null;view.Retry=0;}
                var nav=view.Navigation;nav.BeginQuery();
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
                            {s.DeliveryStatus=s.Phase=="returning"?"Return route blocked — clear the road connection to the quarry":"Bricksworks required — clear the road connection.";return false;}
                            s.DeliveryRoute=fresh;view.Route=fresh;s.DeliveryStatus=s.Phase=="returning"?"Returning to quarry":"Delivering stone to Brickworks";
                        }
                        bool Clear(Vector3 a,Vector3 b)=>RoadDeliveryClear(a,b);
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
