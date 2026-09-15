using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForgeV3.World
{
    // Decorative surface detail only: does not create resource deposits or alter district data.
    public sealed class DistrictHillGroundOverlay : MonoBehaviour
    {
        public const string TexturePath="CityForgeV3/Decals/HillsV01/dry-grass-earth-v02";
        private const float Spacing=110f;
        private const int ChunkCells=3;
        private readonly List<Mesh> meshes=new();
        private readonly List<MeshRenderer> renderers=new();
        private Material material;
        private DistrictWorldController world;
        public bool PresentationEnabled {get;set;}=true;
        public int PatchCount {get;private set;}
        public Vector2 ReviewPoint {get;private set;}
        public float GeometrySignature {get;private set;}
        public void Rebuild(DistrictWorldController host,RegionCityTile district,float width,float depth)
        {
            world=host;
            var texture=Resources.Load<Texture2D>(TexturePath);var shader=Shader.Find(district.Hills.Mountains ? "CityForgeV3/HillGroundOverlayForestV02" : "CityForgeV3/HillGroundOverlay");
            if(texture==null||shader==null)return;
            material=new Material(shader){name="Hill dry grass and earth",mainTexture=texture,renderQueue=3002};
            uint seed=2166136261;foreach(char c in district.TileId??"")seed=unchecked((seed^c)*16777619);
            seed=Hash(seed^unchecked((uint)district.Hills.Seed));
            int cols=Mathf.CeilToInt(width/Spacing),rows=Mathf.CeilToInt(depth/Spacing);float reviewDistance=float.MaxValue;
            for(int cz=0;cz<rows;cz+=ChunkCells)for(int cx=0;cx<cols;cx+=ChunkCells)
            {
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
                for(int z=cz;z<Mathf.Min(rows,cz+ChunkCells);z++)for(int x=cx;x<Mathf.Min(cols,cx+ChunkCells);x++)
                {
                    uint h=Hash(seed^unchecked((uint)x*73856093u)^unchecked((uint)z*19349663u));
                    if(h%10<3)continue;
                    float px=-width*.5f+(x+.5f)*Spacing+(Unit(h)-.5f)*90,pz=-depth*.5f+(z+.5f)*Spacing+(Unit(Hash(h))- .5f)*90;
                    float half=55+Unit(Hash(h+1))*35;
                    if(Mathf.Abs(px)+half>width*.5f||Mathf.Abs(pz)+half>depth*.5f)continue;
                    float elevation=world.TerrainElevation(px,pz);if(elevation<1)continue;
                    int start=vertices.Count,turn=(int)(Hash(h+2)%4);float variation=.65f+.35f*Unit(Hash(h+3));
                    int divisions=Mathf.CeilToInt(half*2/5f);
                    for(int vz=0;vz<=divisions;vz++)for(int vx=0;vx<=divisions;vx++)
                    {
                        float u=(float)vx/divisions,v=(float)vz/divisions;
                        float wx=px+(u*2-1)*half,wz=pz+(v*2-1)*half,wy=world.TerrainElevation(wx,wz);
                        vertices.Add(new Vector3(wx,wy+.04f,wz));
                        float dx=(world.TerrainElevation(wx+2.5f,wz)-world.TerrainElevation(wx-2.5f,wz))/5;
                        float dz=(world.TerrainElevation(wx,wz+2.5f)-world.TerrainElevation(wx,wz-2.5f))/5;
                        normals.Add(new Vector3(-dx,1,-dz).normalized);
                        var coord=new Vector2(u,v);for(int t=0;t<turn;t++)coord=new Vector2(1-coord.y,coord.x);uv.Add(coord);
                        float slope=Mathf.Sqrt(dx*dx+dz*dz);
                        float coverage=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,4,wy))*Mathf.Lerp(.35f,1,Mathf.InverseLerp(.01f,.16f,slope));
                        if(world.SampleRiverSurface(transform.TransformPoint(new Vector3(wx,0,wz))).HasValue)coverage=0;
                        colors.Add(new Color(1,1,1,coverage*variation));
                        if(vx==divisions||vz==divisions)continue;
                        int i=start+vz*(divisions+1)+vx;triangles.Add(i);triangles.Add(i+divisions+1);triangles.Add(i+1);triangles.Add(i+1);triangles.Add(i+divisions+1);triangles.Add(i+divisions+2);
                    }
                    PatchCount++;GeometrySignature+=px*.13f+pz*.17f+elevation*.19f;
                    float distance=Vector2.Distance(new Vector2(px,pz),new Vector2(-100,0));
                    if(elevation>8&&distance<reviewDistance){reviewDistance=distance;ReviewPoint=new Vector2(px,pz);}
                }
                if(vertices.Count==0)continue;
                var mesh=new Mesh{name=$"Hill detail {cx},{cz}",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();meshes.Add(mesh);
                var chunk=new GameObject(mesh.name);chunk.transform.SetParent(transform,false);chunk.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=chunk.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;renderers.Add(renderer);
            }
            LateUpdate();
        }
        private void LateUpdate()
        {
            if(world==null||material==null)return;
            world.ConfigureHillOverlayLighting(material,.20f);
            foreach(var renderer in renderers)renderer.enabled=PresentationEnabled;
        }
        private static uint Hash(uint v){unchecked{v^=v>>16;v*=0x7feb352du;v^=v>>15;v*=0x846ca68bu;return v^(v>>16);}}
        private static float Unit(uint v)=>(v&65535)/65535f;
        private void OnDestroy(){foreach(var mesh in meshes)Dispose(mesh);Dispose(material);}
        private static void Dispose(Object obj){if(obj==null)return;if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
    }
    public sealed partial class DistrictWorldController
    {
        public void ConfigureHillOverlayLighting(Material material,float opacity)
        {
            var ground=_groundRenderer?.sharedMaterial;if(ground==null)return;
            var tint=ground.color;tint.a=opacity;material.color=tint;
            material.SetFloat("_AmbientFloor",ground.GetFloat("_AmbientFloor"));
            material.SetFloat("_TerrainReliefStrength",ground.GetFloat("_TerrainReliefStrength"));
            material.SetVector("_TerrainSunDirection",ground.GetVector("_TerrainSunDirection"));
        }
    }
}
