using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CityForgeV3.Buildings3D;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        Transform quarryRoot;LotWorldController quarryFactory;
        readonly Dictionary<string,QuarryView> quarryViews=new();
        sealed class QuarryView
        {public Transform Root,Transfer;public HorseCarriageController Wagon;public QuarryCranePresentation Crane;public List<Transform> Blocks=new();public List<QuarryWorkerPresentation> Workers=new();public DistrictStoneSite Site;public DistrictQuarryNavigation Navigation;public List<Vector2> Route;public float Retry;public int NavigationKey;public bool Running;}
        public void EnsureStoneDeposits(RegionCityTile d)
        {var nav=new DistrictLaborNavigation(d,IsUnderRiverWater);DistrictQuarry.Ensure(d,nav.Walkable);}
        public string QuarrySiteBlockReason(RegionCityTile d,DistrictStoneSite site)
        {
            if(d==null || site==null || site.Kind!="stone" || !d.StoneSites.Contains(site)) return "This is not a stone deposit in the selected district.";
            if(site.Built)return "A quarry already occupies this deposit.";
            if(DistrictQuarry.WagesDue(d,site)>d.Treasury)return "A quarry needs $500 to pay its two workers for the season.";
            var nav=new DistrictLaborNavigation(d,IsUnderRiverWater);
            return DistrictQuarry.SiteBlockReason(d,DistrictQuarry.Point(d,site),nav.Walkable);
        }
        public bool QuarrySiteClear(RegionCityTile d,DistrictStoneSite site)
        {var nav=new DistrictLaborNavigation(d,IsUnderRiverWater);return DistrictQuarry.CanBuild(d,site,nav.Walkable);}
        public Vector3 ResourceWorldPoint(Vector2 point) =>
            _content.TransformPoint(new Vector3(point.x, TerrainElevation(point.x, point.y) + .2f, point.y));
        public bool BuildQuarry(RegionCityTile d,DistrictStoneSite site)
        {var nav=new DistrictLaborNavigation(d,IsUnderRiverWater);return Resources.Load<GameObject>(DistrictQuarry.ResourcePath)!=null&&DistrictQuarry.Build(d,site,nav.Walkable);}
        public void PresentQuarries(RegionCityTile d,bool running=true)
        {
            if(quarryRoot==null||quarryRoot.parent!=_content)
            {quarryRoot=new GameObject("Stone Deposits and Quarries").transform;quarryRoot.SetParent(_content,false);quarryViews.Clear();}
            foreach(var id in quarryViews.Keys.Where(id=>!d.StoneSites.Any(p=>p.Id==id)).ToArray())
            {Destroy(quarryViews[id].Root.gameObject);quarryViews.Remove(id);}
            foreach(var site in d.StoneSites)
            {
                if(quarryViews.TryGetValue(site.Id,out var old)&&old.Root!=null&&(old.Wagon!=null)!=site.Built)
                {old.Root.gameObject.SetActive(false);Destroy(old.Root.gameObject);quarryViews.Remove(site.Id);}
                if(!quarryViews.TryGetValue(site.Id,out var v)||v.Root==null)
                {
                    v=new QuarryView{Root=new GameObject("Stone site — "+site.Id).transform,Site=site};v.Root.SetParent(quarryRoot,false);
                    var p=DistrictQuarry.Point(d,site);v.Root.localPosition=new Vector3(p.x,TerrainElevation(p.x,p.y)+.03f,p.y);v.Root.localRotation=Quaternion.Euler(0,site.Yaw,0);
                    var stone=new Material(Shader.Find("Standard")){color=new Color(.56f,.54f,.48f)};stone.SetFloat("_Glossiness",.08f);
                    v.Root.gameObject.AddComponent<QuarryMaterials>().Owned.Add(stone);
                    if(!site.Built)
                    {
                        v.Root.gameObject.AddComponent<DistrictStoneOutcrop>()
                            .Build(this, DistrictFloraMaterial());
                    }
                    else
                    {
                        var prefab=Resources.Load<GameObject>(DistrictQuarry.ResourcePath);
                        var craneBase=Resources.Load<GameObject>(QuarryCranePresentation.BaseResource);
                        if(craneBase!=null)
                        {
                            var model=Instantiate(craneBase,v.Root);
                            var material=prefab.GetComponentInChildren<Renderer>().sharedMaterial;
                            foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
                        }
                        else if(prefab!=null)Instantiate(prefab,v.Root);
                        for(int worker=0;worker<DistrictQuarry.WorkerCount;worker++)
                        {
                            var miner=CreateAxemanVisual(v.Root,"Quarry miner "+(worker+1));
                            if(miner==null)continue;
                            // Interior stations measured against the imported quarry's existing rock ledges.
                            // Local coordinates keep the miners and their strike direction attached when rotated.
                            miner.transform.localPosition=worker==0 ? new Vector3(-1.1f,.375f,0) : new Vector3(.1f,.365f,6);
                            miner.transform.localRotation=Quaternion.Euler(0,270,0);
                            var motion=miner.AddComponent<QuarryWorkerPresentation>();motion.Initialize(worker*.43f);v.Workers.Add(motion);
                        }
                        if(quarryFactory==null){var helper=new GameObject("Quarry vehicle factory");helper.transform.SetParent(_content,false);helper.SetActive(false);quarryFactory=helper.AddComponent<LotWorldController>();}
                        var team=quarryFactory.CreateHorseCarriagePresentation("Quarry stone cart",1,LotWorldController.HorseForestryWagonPropId,TimberGround);
                        if(team!=null)
                        {
                            team.SetParent(v.Root,false);v.Wagon=team.GetComponent<HorseCarriageController>();team.localPosition=QuarryCranePresentation.WagonBed+Vector3.forward*v.Wagon.Definition.HorseOffset;v.Wagon.RestoreHeadings(0,0,0);
                            if(site.HasWagonPose)
                            {
                                team.position=_content.TransformPoint(new Vector3(site.WagonPosition.x,TerrainElevation(site.WagonPosition.x,site.WagonPosition.y)+.03f,site.WagonPosition.y));
                                v.Wagon.RestoreHeadings(site.HorseHeading-site.Yaw,site.BodyHeading-site.Yaw,site.FrontHeading-site.Yaw);
                            }
                            foreach(var t in v.Wagon.Carriage.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Cargo_Log_"))t.gameObject.SetActive(false);
                            for(int i=0;i<8;i++){var b=StoneBlock(v.Wagon.Carriage,stone,"Loaded stone block "+i);b.localPosition=new Vector3((i%2-.5f)*.7f,1.37f+(i/4)*.5f,-.85f-((i/2)%2)*.95f);v.Blocks.Add(b);}
                            v.Transfer=StoneBlock(v.Root,stone,"Stone block being loaded");
                            v.Crane=v.Root.gameObject.AddComponent<QuarryCranePresentation>();v.Crane.Initialize(v.Transfer,v.Blocks);
                        }
                    }
                    quarryViews[site.Id]=v;
                    if (site.Built)
                    {
                        DistrictActionResult Rotate(float degrees) => TryRotateQuarry(d, site, degrees, out var reason) ? DistrictActionResult.Applied(reason) : new DistrictActionResult(false, reason);
                        RegisterSelectable(v.Root.gameObject, new DistrictSelectionRef(DistrictSelectionKind.Entity, "quarry:" + site.Id),
                            "STONE QUARRY", "Rotate around the stone deposit. Access conflicts are reported below.", true,
                            v.Root.GetComponentsInChildren<Renderer>().Where(r =>
                                (v.Wagon == null || !r.transform.IsChildOf(v.Wagon.transform)) &&
                                r.GetComponentInParent<QuarryWorkerPresentation>() == null),
                            new DistrictSelectionAction("↶ LEFT 90°", () => Rotate(-90)),
                            new DistrictSelectionAction("RIGHT 90° ↷", () => Rotate(90))).WithBuildingDeletion(() => DistrictQuarry.Demolish(site), true, () => PresentQuarries(d))
                            .WithStatus(() => DistrictQuarry.WorkStatus(d, site, v.Running))
                            .WithWarnings(() => DistrictQuarry.OperationalWarning(d, site))
                            .WithNudge(delta =>
                            {
                                bool away = site.Phase == "delivering" || site.Phase == "unloading" || site.Phase == "returning";
                                var wagonPosition = v.Wagon != null ? v.Wagon.transform.position : Vector3.zero;
                                v.Root.localPosition += new Vector3(delta.x, 0, delta.y);
                                if (v.Wagon != null)
                                {
                                    if (away) v.Wagon.transform.position = wagonPosition;
                                    SaveQuarryWagonPose(site, v.Wagon);
                                }
                                v.Navigation = null; v.Route = null; v.Retry = 0;
                                site.DeliveryRoute = null;
                                if (site.Phase == "returning") site.DeliveryDestination = DistrictBrickworks.QuarryHome(d, site);
                            });
                    }
                }
                v.Running=running;
                foreach(var worker in v.Workers)worker.SetWorking(running&&site.Enabled&&DistrictQuarry.WorkersPaid(d,site)&&site.Phase=="mining");
                if(v.Wagon==null)continue;
                for(int i=0;i<v.Blocks.Count;i++)v.Blocks[i].gameObject.SetActive(i<site.CartBlocks);
                v.Crane.Present(site);
            }
        }
        public bool TryRotateQuarry(RegionCityTile district, DistrictStoneSite site, float degrees, out string reason)
        {
            reason = "";
            if (site == null || !site.Built || !district.StoneSites.Contains(site))
            { reason = "This quarry is no longer available."; return false; }
            bool away = site.Phase == "delivering" || site.Phase == "unloading" || site.Phase == "returning";
            var yaw = Mathf.Repeat(site.Yaw + degrees, 360f);
            var nav = new DistrictLaborNavigation(district, IsUnderRiverWater);
            reason = DistrictQuarry.SiteBlockReason(district, DistrictQuarry.Point(district, site), nav.Walkable, yaw);
            reason = reason.Replace("Move nearby roads out of the quarry footprint before building.",
                "Nearby roads are within the quarry clearance area; access may be obstructed.");
            if (away) reason += (string.IsNullOrEmpty(reason) ? "" : " ") + "The wagon is away; its return access will be recalculated.";
            quarryViews.TryGetValue(site.Id, out var view);
            if (view?.Wagon != null) SaveQuarryWagonPose(site, view.Wagon);
            DistrictIndustryRotation.Apply(district, new DistrictSelectionRef(DistrictSelectionKind.Entity, "quarry:" + site.Id), degrees > 0 ? 1 : -1);
            if (view?.Root != null)
            {
                view.Root.localRotation = Quaternion.Euler(0, yaw, 0);
                if (view.Wagon != null)
                {
                    var position = _content.TransformPoint(new Vector3(site.WagonPosition.x, 0, site.WagonPosition.y));
                    position.y = view.Wagon.transform.position.y;
                    view.Wagon.transform.position = position;
                    view.Wagon.RestoreHeadings(site.HorseHeading-yaw, site.BodyHeading-yaw, site.FrontHeading-yaw);
                }
                view.Navigation = null; view.Route = null; view.Retry = 0;
            }
            return true;
        }

        static Transform StoneBlock(Transform parent,Material material,string name)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localScale=new Vector3(.62f,.5f,.72f);o.GetComponent<Renderer>().sharedMaterial=material;Destroy(o.GetComponent<Collider>());return o.transform;}
    }
    public sealed class DistrictStoneOutcrop : MonoBehaviour
    {
        public const string TexturePath = "CityForgeV3/NaturalResources/StoneV01/stones-for-quarry";
        DistrictWorldController world;
        Sprite sprite;
        SpriteRenderer artwork;
        Quaternion cameraRotation;
        bool hasCameraRotation;
        public void Build(DistrictWorldController host, Material material)
        {
            world = host;
            var texture = Resources.Load<Texture2D>(TexturePath);
            if (texture == null) return;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .07f), texture.width / 9f);
            var go = new GameObject("Stone deposit artwork");
            go.transform.SetParent(transform, false);
            artwork = go.AddComponent<SpriteRenderer>();
            artwork.sprite = sprite;
            artwork.sharedMaterial = material;
            artwork.color = Color.white;
            LateUpdate();
        }
        void LateUpdate()
        {
            if (artwork == null || world == null || world.WorldCamera == null) return;
            var rotation = world.WorldCamera.transform.rotation;
            if (hasCameraRotation && Quaternion.Angle(
                    cameraRotation, rotation) < .001f) return;
            cameraRotation = rotation;
            hasCameraRotation = true;
            artwork.transform.rotation = rotation;
        }
        void OnDestroy() { if (sprite != null) Destroy(sprite); }
    }
    public sealed class QuarryMaterials:MonoBehaviour
    {public readonly List<Material> Owned=new();void OnDestroy(){foreach(var m in Owned)if(m!=null)Destroy(m);}}
}
