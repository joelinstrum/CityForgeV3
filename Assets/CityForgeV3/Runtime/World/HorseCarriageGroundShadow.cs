using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForgeV3.World
{
    // Road artwork is layered above ordinary mesh shadows. These two soft
    // contact footprints use the existing road-safe vehicle-shadow shader.
    // Each follows its own heading and samples the actual lot terrain.
    public sealed class HorseCarriageGroundShadow : MonoBehaviour
    {
        sealed class Footprint
        {
            public Transform Target, Root;
            public Mesh Mesh;
            public Vector3[] Vertices;
            public Vector2 Size;
            public float CenterZ;
        }
        readonly Footprint[] footprints=new Footprint[3];
        Material material;
        Func<Vector3,float> ground;
        Vector3 sunRay;
        const int Columns=4,Rows=8;
        public void Initialize(Transform horse,Transform carriage,Func<Vector3,float> sampleGround,HorseWagonDefinition vehicle=null,Transform secondHorse=null)
        {
            vehicle??=HorseWagonDefinition.Carriage;
            ground=sampleGround;
            var group=new GameObject("Projected Prop Silhouette").transform;
            group.SetParent(transform,false);
            material=new Material(Shader.Find("CityForgeV3/VehicleContactShadow"))
            {name="Horse and Carriage Road Contact Shadow",renderQueue=StreetVehicleGroundShadow.RenderQueue};
            material.SetColor("_Color",new Color(.02f,.024f,.028f,.38f));
            footprints[0]=Create(group,horse,new Vector2(1.25f,2.9f),-.15f,"Horse Ground Shadow");
            footprints[1]=Create(group,carriage,vehicle.ShadowSize,vehicle.ShadowCenterZ,"Carriage Ground Shadow");
            if(secondHorse!=null)footprints[2]=Create(group,secondHorse,new Vector2(1.25f,2.9f),-.15f,"Second Horse Ground Shadow");
        }
        Footprint Create(Transform parent,Transform target,Vector2 size,float centerZ,string name)
        {
            var child=new GameObject(name);child.transform.SetParent(parent,false);
            var vertices=new Vector3[(Columns+1)*(Rows+1)];var uv=new Vector2[vertices.Length];
            for(var z=0;z<=Rows;z++)for(var x=0;x<=Columns;x++)uv[z*(Columns+1)+x]=new Vector2((float)x/Columns,(float)z/Rows);
            var triangles=new int[Columns*Rows*6];var n=0;
            for(var z=0;z<Rows;z++)for(var x=0;x<Columns;x++)
            {var a=z*(Columns+1)+x;triangles[n++]=a;triangles[n++]=a+Columns+1;triangles[n++]=a+1;triangles[n++]=a+1;triangles[n++]=a+Columns+1;triangles[n++]=a+Columns+2;}
            var mesh=new Mesh{name=name};mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
            child.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sortingOrder=2200;
            return new Footprint{Target=target,Root=child.transform,Mesh=mesh,Vertices=vertices,Size=size,CenterZ=centerZ};
        }
        public void SetLighting(Vector3 ray,bool subdued)
        {
            sunRay=ray;
            if(material!=null)material.SetColor("_Color",new Color(.02f,.024f,.028f,subdued?.18f:.38f));
        }
        void LateUpdate()
        {
            if(ground==null)return;
            var shift=new Vector3(sunRay.x,0,sunRay.z);
            shift=Vector3.ClampMagnitude(shift*.35f, .35f);
            foreach(var f in footprints)
            {
                if(f==null||f.Target==null)continue;
                var forward=Vector3.ProjectOnPlane(f.Target.forward,Vector3.up).normalized;
                var right=Vector3.Cross(Vector3.up,forward);
                for(var z=0;z<=Rows;z++)for(var x=0;x<=Columns;x++)
                {
                    var point=f.Target.position+right*((float)x/Columns-.5f)*f.Size.x+
                        forward*(((float)z/Rows-.5f)*f.Size.y+f.CenterZ)+shift;
                    point.y=ground(point);
                    f.Vertices[z*(Columns+1)+x]=f.Root.InverseTransformPoint(point);
                }
                f.Mesh.vertices=f.Vertices;f.Mesh.RecalculateBounds();
            }
        }
        void OnDestroy()
        {
            if(material!=null)Destroy(material);
            foreach(var f in footprints)if(f?.Mesh!=null)Destroy(f.Mesh);
        }
    }
}
