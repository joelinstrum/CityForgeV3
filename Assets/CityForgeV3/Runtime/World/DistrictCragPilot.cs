using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForgeV3.World
{
    // Presentation-only pilot; no saved elevation/resource/navigation mutation.
    public sealed class DistrictCragPilot : MonoBehaviour
    {
        public Vector2 Center {get;private set;}
        public int RockCount {get;private set;}
        public float ExposedHeight {get;private set;}
        readonly List<Mesh> meshes=new();
        Material material;
        float previousShadowDistance;
        DistrictWorldController world;
        public static DistrictCragPilot Create(DistrictWorldController world)
        {
            var previous=world.GetComponentInChildren<DistrictCragPilot>(true);
            if(previous!=null){previous.RestoreShadowDistance();previous.gameObject.SetActive(false);Destroy(previous.gameObject);}
            var go=new GameObject("Crag Pilot V01 — isolated hillside");go.transform.SetParent(world.transform,false);
            var pilot=go.AddComponent<DistrictCragPilot>();pilot.Build(world);return pilot;
        }
        void Build(DistrictWorldController host)
        {
            previousShadowDistance=QualitySettings.shadowDistance;QualitySettings.shadowDistance=Mathf.Max(previousShadowDistance,300f);
            world=host;
            material=new Material(Shader.Find("CityForgeV3/CragSurfaceV01")){name="Silver gray crag pilot",renderQueue=1998};
            material.mainTexture=Resources.Load<Texture2D>("CityForgeV3/Terrain/QuietSilverV01/quiet-silver-v01");
            float best=float.MinValue;Vector2 bestGradient=Vector2.up;
            for(float z=-350;z<=350;z+=12)for(float x=-350;x<=350;x+=12)
            {
                float h=world.TerrainElevation(x,z);var g=Gradient(x,z);float slope=g.magnitude;
                if(h<18||h>65||slope<.35f||slope>1.1f)continue;
                float score=-Vector2.Distance(new Vector2(x,z),new Vector2(160,-110))*.04f-Mathf.Abs(h-38)*.15f-Mathf.Abs(slope-.65f)*10;
                if(score>best){best=score;Center=new Vector2(x,z);bestGradient=g.normalized;}
            }
            if(best==float.MinValue)throw new InvalidOperationException("No suitable pilot slope");
            var across=new Vector2(bestGradient.y,-bestGradient.x);var rng=new System.Random(8143);
            float Rand(float lo,float hi)=>lo+(float)rng.NextDouble()*(hi-lo);
            for(int row=0;row<2;row++)for(int col=0;col<3;col++)
            {
                var p=Center+across*((col-1f)*16+Rand(-5,5))+bestGradient*((row-.5f)*17+Rand(-4,4));
                if(world.TerrainElevation(p.x,p.y)<8)continue;
                var yaw=Mathf.Atan2(across.x,across.y)*Mathf.Rad2Deg+Rand(-22,22);
                AddRock(p,new Vector3(Rand(16,24),Rand(9,15),Rand(7,12)),yaw,rng);
                AddRock(p+across*Rand(3,6)-bestGradient*Rand(1,4),new Vector3(Rand(5,8),Rand(4,7),Rand(4,7)),yaw+Rand(-30,30),rng);
                for(int i=0;i<3;i++)AddRock(p-bestGradient*Rand(7,14)+across*Rand(-7,7),new Vector3(Rand(1,2.8f),Rand(.8f,2),Rand(1,2.5f)),Rand(0,360),rng);
            }
            material.color=Color.white;
        }
        Vector2 Gradient(float x,float z)=>new Vector2(world.TerrainElevation(x+2,z)-world.TerrainElevation(x-2,z),world.TerrainElevation(x,z+2)-world.TerrainElevation(x,z-2))/4;
        void AddRock(Vector2 p,Vector3 size,float yaw,System.Random rng)
        {
            float groundY=world.TerrainElevation(p.x,p.y);if(groundY<2)return;
            var go=new GameObject("Embedded crag "+RockCount++);go.transform.SetParent(transform,false);
            go.transform.localPosition=new Vector3(p.x,groundY-size.y*.53f,p.y);go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            float Rand(float lo,float hi)=>lo+(float)rng.NextDouble()*(hi-lo);
            // Broad bevelled wedges: clipped corners, slanted crown, irregular shoulders.
            Vector2[] outline={new(.34f,.5f),new(-.34f,.5f),new(-.5f,.30f),new(-.5f,-.30f),new(-.32f,-.5f),new(.32f,-.5f),new(.5f,-.28f),new(.5f,.28f)};
            for(int i=0;i<outline.Length;i++)outline[i]*=Rand(.86f,1.12f);
            var points=new Vector3[24];float leanX=Rand(-.14f,.14f),leanZ=Rand(-.12f,.12f),tilt=Rand(-.18f,.18f);
            for(int ring=0;ring<3;ring++)for(int i=0;i<8;i++){
                var o=outline[i];float scale=ring==0?.82f:ring==1?1f:Rand(.52f,.85f);
                float y=ring==0?0:ring==1?Rand(.48f,.64f):Rand(.86f,1.12f)+o.x*tilt;
                points[ring*8+i]=Vector3.Scale(new Vector3(o.x*scale+leanX*ring*.5f,y,o.y*scale+leanZ*ring*.5f),size);
            }
            // Seat the whole footprint into the slope, including its downhill edge.
            for(int i=0;i<24;i++){
                var wp=go.transform.localRotation*points[i]+go.transform.localPosition;
                float localSurface=world.TerrainElevation(wp.x,wp.z)-go.transform.localPosition.y;
                if(i<8)points[i].y=localSurface-2;
                else points[i].y=Mathf.Max(points[i].y,localSurface+(i<16?.25f:1.2f));
            }
            var vertices=new List<Vector3>();var triangles=new List<int>();
            void Tri(Vector3 a,Vector3 b,Vector3 c){int i=vertices.Count;vertices.Add(a);vertices.Add(c);vertices.Add(b);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);}
            // Outline order is counterclockwise as viewed from above in Unity XZ.
            for(int ring=0;ring<2;ring++)for(int i=0;i<8;i++){int j=(i+1)%8;Tri(points[ring*8+i],points[ring*8+j],points[(ring+1)*8+i]);Tri(points[ring*8+j],points[(ring+1)*8+j],points[(ring+1)*8+i]);}
            var top=Vector3.zero;for(int i=16;i<24;i++)top+=points[i]/8;
            for(int i=0;i<8;i++){int j=(i+1)%8;Tri(top,points[16+i],points[16+j]);Tri(Vector3.zero,points[j],points[i]);}
            var mesh=new Mesh{name="Crag wedge V01"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            ExposedHeight=Mathf.Max(ExposedHeight,size.y*.47f);
        }
        void RestoreShadowDistance(){if(previousShadowDistance>0)QualitySettings.shadowDistance=previousShadowDistance;previousShadowDistance=0;}
        void OnDestroy(){RestoreShadowDistance();foreach(var mesh in meshes)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
