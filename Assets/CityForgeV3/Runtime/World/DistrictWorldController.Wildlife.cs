using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        Transform wildlifeRoot;
        LotWorldController wildlifeFactory;
        readonly Dictionary<string,GameObject> wildlifeActors=new();
        public GameObject CreateWildlifeActor(string id,Transform parent,string name)
        {
            if(wildlifeFactory==null)
            {
                var go=new GameObject("Wildlife Presentation Factory");go.transform.SetParent(_content,false);go.SetActive(false);wildlifeFactory=go.AddComponent<LotWorldController>();
            }
            var actor=wildlifeFactory.CreatePropPresentation(id,name,1);
            if(actor==null)return null;actor.SetParent(parent,false);return actor.gameObject;
        }
        public void PresentWildlife(RegionCityTile d,bool running)
        {
            if(_content==null)return;
            if(wildlifeRoot==null||wildlifeRoot.parent!=_content){wildlifeRoot=new GameObject("District Wildlife").transform;wildlifeRoot.SetParent(_content,false);wildlifeActors.Clear();}
            var state=DistrictWildlife.State(d);
            var ids=new HashSet<string>(state.Bears.Select(b=>b.Id).Concat(state.Marksmen.Select(m=>m.Id)));
            foreach(var id in wildlifeActors.Keys.Where(id=>!ids.Contains(id)).ToArray()){Destroy(wildlifeActors[id]);wildlifeActors.Remove(id);}
            GameObject Actor(string key,string type,Vector2 p,Vector2 facing,string animation)
            {
                if(!wildlifeActors.TryGetValue(key,out var actor)||actor==null)
                {actor=CreateWildlifeActor(type,wildlifeRoot,type+" "+key);if(actor==null)return null;wildlifeActors[key]=actor;}
                actor.transform.localPosition=new Vector3(p.x,TerrainElevation(p.x,p.y)+.02f,p.y);
                if(facing.sqrMagnitude>.01f)actor.transform.localRotation=Quaternion.LookRotation(new Vector3(facing.x,0,facing.y));
                var player=actor.GetComponent<ThreeDimensionalCharacterAnimator>();
                if(player!=null){if(player.State!=animation)player.Play(animation);player.SetPlaybackSpeed(running?1:0);}
                return actor;
            }
            foreach(var b in state.Bears)Actor(b.Id,LotWorldController.BearAnimalId,b.Position,b.Direction,"walk");
            foreach(var m in state.Marksmen)
            {
                var actor=Actor(m.Id,LotWorldController.MusketmanCharacterId,m.Position,m.Facing,"idle");
                if(actor==null)continue;
                if(!FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Any(l=>l.enabled)&&WorldCamera!=null)WorldCamera.gameObject.AddComponent<AudioListener>();
                var shot=actor.GetComponent<MarksmanWarningShot>()??actor.AddComponent<MarksmanWarningShot>();
                shot.Present(m.ShotSeconds,m.Shots,running);
            }
        }
    }
}
