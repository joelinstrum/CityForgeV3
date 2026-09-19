using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        public const string AxemanResource="Characters/AxemanLaborV01/Axeman";
        private Transform laborRoot;
        private readonly Dictionary<string,GameObject> axemen=new();
        private GameObject campMarker;
        public bool LaborAssetReady=>Resources.Load<GameObject>(AxemanResource)!=null;
        public void PresentLabor(RegionCityTile district,bool running)
        {
            if(_content==null)return;
            if(laborRoot==null || laborRoot.parent!=_content){laborRoot=new GameObject("District Labor").transform;laborRoot.SetParent(_content,false);axemen.Clear();campMarker=null;}
            var s=DistrictLabor.State(district);
            if(s.CampPlaced && (s.TimberCrews==null||s.TimberCrews.Count==0) && campMarker==null)
            {
                campMarker=new GameObject("Temporary Lumber Camp");campMarker.transform.SetParent(laborRoot,false);
                var campMaterials=campMarker.AddComponent<CharacterShadowMaterialOwner>();
                // Small timber stack marks the dispatch/storage point until a mill exists.
                for(int i=0;i<5;i++)
                {
                    var log=GameObject.CreatePrimitive(PrimitiveType.Cylinder);log.name="Camp timber";log.transform.SetParent(campMarker.transform,false);log.transform.localPosition=new Vector3((i%3)*.32f-.32f,.18f+(i/3)*.28f,0);log.transform.localRotation=Quaternion.Euler(90,0,0);log.transform.localScale=new Vector3(.28f,.7f,.28f);Destroy(log.GetComponent<Collider>());
                    var mat=new Material(Shader.Find("Standard"));mat.color=new Color(.30f,.18f,.09f);log.GetComponent<Renderer>().material=mat;campMaterials.Add(mat);
                }
            }
            if(campMarker!=null){campMarker.SetActive(s.CampPlaced && (s.TimberCrews==null||s.TimberCrews.Count==0));campMarker.transform.localPosition=new Vector3(s.Camp.x,TerrainElevation(s.Camp.x,s.Camp.y),s.Camp.y);}
            var ids=new HashSet<string>(s.Workers.Select(w=>w.Id));
            foreach(var id in axemen.Keys.Where(id=>!ids.Contains(id)).ToArray()){Destroy(axemen[id]);axemen.Remove(id);}
            foreach(var w in s.Workers)
            {
                if(!axemen.TryGetValue(w.Id,out var root)||root==null)
                {
                    root=CreateAxemanVisual(laborRoot,"Axeman "+(w.Slot+1));
                    if(root==null)continue;
                    axemen[w.Id]=root;
                }
                root.transform.localPosition=new Vector3(w.Position.x,TravelElevation(w.Position),w.Position.y);
                if(w.Facing.sqrMagnitude>.01f)root.transform.localRotation=Quaternion.LookRotation(new Vector3(w.Facing.x,0,w.Facing.y));
                var p=root.GetComponent<ThreeDimensionalCharacterAnimator>();
                string state=w.Activity==AxemanActivity.Chopping?"chop":w.Activity is AxemanActivity.Walking or AxemanActivity.Delivering or AxemanActivity.Returning or AxemanActivity.Retreating?"walk":"idle";
                if(w.Activity==AxemanActivity.Retreating&&!w.RetreatMoving)state="idle";
                if(p.State!=state)p.Play(state);p.SetPlaybackSpeed(running?1:0);
            }
        }
        public void FocusLaborCamp(RegionCityTile d)
        {
            SetPan(DistrictLabor.State(d).Camp);SetZoom(DistrictZoomLevel.LOD0);
        }
    }
}
