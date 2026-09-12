using UnityEngine;
namespace CityForgeV3.World
{
    // The sprite remains the persistent flora/selection anchor. Only presentation changes with zoom.
    public sealed class PlaneUkFloraPresentation : MonoBehaviour
    {
        public const string ResourceRoot="CityForgeV3/Flora/PlaneUK3DV01/";
        public static bool IsTree(string id)=>id=="plane-uk-3d-a"||id=="plane-uk-3d-b"||id=="angel-oak-spanish-moss";
        public static bool UsesMesh(LotZoomLevel zoom)=>zoom==LotZoomLevel.Detail||zoom==LotZoomLevel.Inspection;
        public static string Variant(string id)=>id.EndsWith("-b")?"b":"a";
        public static string SeasonName(SeasonPreset season)=>season==SeasonPreset.Autumn?"autumn":season==SeasonPreset.Winter?"winter":"summer";
        public static string BillboardPath(string id,SeasonPreset season)=>id=="angel-oak-spanish-moss"?"CityForgeV3/Flora/LegacyTreesV01/angel-oak-spanish-moss-"+season.ToString().ToLowerInvariant():ResourceRoot+"plane-"+Variant(id)+"-"+SeasonName(season);
        SpriteRenderer sprite;
        GameObject model;
        CapsuleCollider pick;
        string modelPath;
        bool near;
        public bool IsMeshVisible=>near&&model!=null&&model.activeInHierarchy;
        public void Configure(string id,SeasonPreset season,LotZoomLevel zoom)
        {
            sprite=GetComponent<SpriteRenderer>();
            if(!IsTree(id)){near=false;ReleaseModel();if(sprite!=null)sprite.forceRenderingOff=false;return;}
            var path=id=="angel-oak-spanish-moss"?FloraTreeRepairs.Root+"AngelOak":ResourceRoot+"Tree-"+Variant(id)+"-"+SeasonName(season);
            if(modelPath!=path){ReleaseModel();modelPath=path;}
            near=UsesMesh(zoom);
            if(near&&model==null)
            {
                var prefab=Resources.Load<GameObject>(path);
                if(prefab!=null)
                {
                    model=new GameObject("Plane UK mesh presentation");
                    model.transform.SetParent(transform,false);
                    Instantiate(prefab,model.transform,false);
                    var hit=new GameObject("Tree trunk selection");hit.layer=2;hit.transform.SetParent(model.transform,false);
                    pick=hit.AddComponent<CapsuleCollider>();pick.center=new Vector3(0,1.5f,0);pick.height=3f;pick.radius=.55f;pick.isTrigger=true;
                }
            }
            Apply();
        }
        public bool ContainsPixel(Camera camera,Vector2 pixel)=>pick!=null&&pick.Raycast(camera.ScreenPointToRay(pixel),out _,10000f);
        void LateUpdate()=>Apply();
        void Apply()
        {
            var visible=near&&model!=null;
            if(model!=null){model.SetActive(visible);model.transform.rotation=Quaternion.identity;}
            if(sprite!=null)sprite.forceRenderingOff=visible;
            var shadow=transform.Find("Flora Shadow — Canopy");
            if(shadow!=null&&shadow.TryGetComponent<SpriteRenderer>(out var cast))cast.forceRenderingOff=visible;
        }
        void ReleaseModel()
        {
            if(model!=null){model.SetActive(false);if(Application.isPlaying)Destroy(model);else DestroyImmediate(model);}
            model=null;pick=null;
        }
    }
}
