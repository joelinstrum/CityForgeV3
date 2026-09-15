using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    [Serializable] public sealed class DistrictResourceDeposit
    {
        public string Id;
        public string Kind = "coal";
        public float NormalizedX, NormalizedZ;
        public bool MineBuilt;
        public bool ManuallyPlaced;
        public float MineYawDegrees;
    }
    public static class DistrictNaturalResources
    {
        public static void Ensure(RegionCityTile district, DistrictElevation terrain)
        {
            string key="coal-mountain-v2:"+district.TileId+":"+district.Width+":"+district.Height+":"+JsonUtility.ToJson(district.Hills);
            if(district.NaturalResourceGenerationKey==key && district.ResourceDeposits!=null)return;
            // Keep developed sites stable through later terrain edits. Never silently demolish a mine.
            if(district.ResourceDeposits!=null && district.ResourceDeposits.Exists(p=>p.MineBuilt))
            {district.NaturalResourceGenerationKey=key;return;}
            district.NaturalResourceGenerationKey=key;
            var placed = district.ResourceDeposits?.FindAll(p => p.ManuallyPlaced);
            district.ResourceDeposits=Generate(district,terrain);
            if (placed != null) district.ResourceDeposits.AddRange(placed);
        }
        public static List<DistrictResourceDeposit> Generate(RegionCityTile district,DistrictElevation terrain)
        {
            var result=new List<DistrictResourceDeposit>();
            float height=district.Hills?.HeightMeters??0;if(height<=0||district.Hills?.Mountains!=true)return result;
            uint seed=2166136261;foreach(char c in district.TileId??"")seed=unchecked((seed^c)*16777619);
            seed=Hash(seed^unchecked((uint)district.Hills.Seed));
            int wanted=1+(int)(seed%2);
            var candidates=new List<(uint rank,Vector2 position)>();
            for(float z=-terrain.Depth/2+30;z<terrain.Depth/2-30;z+=5)
            for(float x=-terrain.Width/2+30;x<terrain.Width/2-30;x+=5)
            {
                float y=terrain.Sample(x,z);
                // Low access apron with a steep mountain face immediately behind the mine.
                if(!DistrictCoalMine.SuitableMountainSite(district,terrain,x/terrain.Width+.5f,z/terrain.Depth+.5f,out _))continue;
                bool clear=true;
                // Existing flora is preserved. Use a naturally clear pocket for initial seeding.
                foreach(var f in district.Flora??new List<PlacedDistrictFlora>())
                    if(f!=null && Vector2.Distance(new Vector2(x,z),new Vector2((f.NormalizedX-.5f)*terrain.Width,(f.NormalizedZ-.5f)*terrain.Depth))<14) {clear=false;break;}
                if(!clear)continue;
                uint rank=Hash(seed^unchecked((uint)Mathf.RoundToInt(x+terrain.Width)*73856093u)^unchecked((uint)Mathf.RoundToInt(z+terrain.Depth)*19349663u));
                candidates.Add((rank,new Vector2(x,z)));
            }
            candidates.Sort((a,b)=>a.rank!=b.rank?a.rank.CompareTo(b.rank):a.position.x!=b.position.x?a.position.x.CompareTo(b.position.x):a.position.y.CompareTo(b.position.y));
            foreach(var candidate in candidates)
            {
                bool separate=true;foreach(var p in result)
                    if(Vector2.Distance(candidate.position,new Vector2((p.NormalizedX-.5f)*terrain.Width,(p.NormalizedZ-.5f)*terrain.Depth))<Mathf.Min(terrain.Width,terrain.Depth)*.25f)separate=false;
                if(!separate)continue;
                result.Add(new DistrictResourceDeposit{Id="coal-"+result.Count,NormalizedX=candidate.position.x/terrain.Width+.5f,NormalizedZ=candidate.position.y/terrain.Depth+.5f});
                if(result.Count==wanted)break;
            }
            return result;
        }
        private static uint Hash(uint v){unchecked{v^=v>>16;v*=0x7feb352du;v^=v>>15;v*=0x846ca68bu;return v^(v>>16);}}
    }
    public sealed class DistrictCoalPresentation : MonoBehaviour
    {
        public const string TexturePath="CityForgeV3/NaturalResources/CoalV03/coal-iso";
        private Sprite sprite;
        private DistrictWorldController world;
        private readonly List<SpriteRenderer> renderers=new();
        public void Build(DistrictWorldController host,RegionCityTile district,float width,float depth,Material material)
        {
            world=host;var texture=Resources.Load<Texture2D>(TexturePath);if(texture==null)return;
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.17f),texture.width/8f);
            foreach(var deposit in district.ResourceDeposits)
            {
                if(deposit.Kind!="coal")continue;
                if(deposit.MineBuilt)
                {
                    var mine=new GameObject("Coal Mine — "+deposit.Id);mine.transform.SetParent(transform,false);
                    mine.AddComponent<DistrictCoalMinePresentation>().Build(world,deposit,width,depth);continue;
                }
                var center=new Vector2((deposit.NormalizedX-.5f)*width,(deposit.NormalizedZ-.5f)*depth);
                // Three close, overlapping piles form one deposit. They do not multiply inventory.
                var offsets=new[]{new Vector2(-2.2f,-.8f),new Vector2(1.8f,-1.1f),new Vector2(.3f,2f)};
                for(int i=0;i<offsets.Length;i++)
                {
                    float x=center.x+offsets[i].x,z=center.y+offsets[i].y;
                    if(world.TerrainElevation(x,z)<.1f||world.SampleRiverSurface(transform.TransformPoint(new Vector3(x,0,z))).HasValue)continue;
                    var item=new GameObject("Coal Deposit — "+deposit.Id+" / pile "+i);item.transform.SetParent(transform,false);
                    item.transform.localPosition=new Vector3(x,.19f+world.TerrainElevation(x,z),z);
                    var renderer=item.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;
                    renderer.sortingOrder=5000-Mathf.RoundToInt((x+z)*4);renderers.Add(renderer);
                }
            }
            LateUpdate();
        }
        private void LateUpdate()
        {
            if(world==null||world.WorldCamera==null)return;
            foreach(var renderer in renderers){renderer.transform.rotation=world.WorldCamera.transform.rotation;renderer.color=TimeOfDayLighting.For(world.TimeOfDay).NeutralArtworkTint;}
        }
        private void OnDestroy(){if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}}
    }
    public sealed partial class DistrictWorldController
    {
        private Transform _naturalResourceRoot;
        private void RefreshNaturalResources(RegionCityTile district)
        {
            DistrictNaturalResources.Ensure(district,_elevation);
            EnsureStoneDeposits(district);PresentQuarries(district);
            RefreshCoalBuildings();
        }
        public void RefreshCoalBuildings()
        {
            if(_naturalResourceRoot!=null){_naturalResourceRoot.gameObject.SetActive(false);Destroy(_naturalResourceRoot.gameObject);}
            var district=_terrainDistrict;
            _naturalResourceRoot=new GameObject("District Natural Resources").transform;_naturalResourceRoot.SetParent(_content,false);
            _naturalResourceRoot.gameObject.AddComponent<DistrictCoalPresentation>().Build(this,district,_widthMeters,_depthMeters,DistrictFloraMaterial());
        }
    }
}
