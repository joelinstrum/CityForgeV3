using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    [Serializable] public sealed class DistrictHillSettings
    {
        public int Version = 1;
        public bool Mountains;
        public bool PreserveLegacyCoalSites;
        public int Seed = 1209;
        public float HeightMeters;
        public float Coverage = .6f;
    }
    // Deterministic district relief. Existing roads, lots and water retain level corridors.
    public sealed class DistrictElevation
    {
        public readonly float Width, Depth;
        public readonly int Columns, Rows;
        public readonly float[] Heights;
        private readonly List<Rect> pads = new();
        readonly DistrictSpatialIndex<int> padIndex=new(),channelIndex=new();
        private readonly List<(Vector2 a, Vector2 b, float radius)> channels = new();
        private readonly List<Vector3> hills = new();
        private readonly float amplitude;
        private readonly bool mountains;
        private readonly bool connectedMountains;
        private readonly List<Vector4> peakShapes=new();
        private readonly List<Vector2Int> ridgeLinks=new();
        private readonly List<Vector2> preservedSites=new();
        public DistrictElevation(RegionCityTile district, float sampleSpacingMeters = 5f)
        {
            Width=DistrictScale.SizeMeters(district.Width);Depth=DistrictScale.SizeMeters(district.Height);
            sampleSpacingMeters=Mathf.Max(5f,sampleSpacingMeters);
            Columns=Mathf.Clamp(Mathf.CeilToInt(Width/sampleSpacingMeters),2,512);Rows=Mathf.Clamp(Mathf.CeilToInt(Depth/sampleSpacingMeters),2,512);
            Heights=new float[(Columns+1)*(Rows+1)];
            var settings=district.Hills;
            mountains=settings?.Mountains??false;
            connectedMountains=mountains && settings.Version>=2;
            if(connectedMountains && settings.PreserveLegacyCoalSites)foreach(var site in district.ResourceDeposits??new List<DistrictResourceDeposit>())
                if(site.Kind=="coal")preservedSites.Add(new Vector2((site.NormalizedX-.5f)*Width,(site.NormalizedZ-.5f)*Depth));
            amplitude=settings==null?0:Mathf.Clamp(settings.HeightMeters,0,mountains?240:60);
            if(amplitude<=0)return;
            var random=new System.Random(settings.Seed);
            int count=Mathf.RoundToInt(Mathf.Lerp(3,14,Mathf.Clamp01(settings.Coverage)));
            for(int i=0;i<count;i++)hills.Add(new Vector3((float)(random.NextDouble()-.5)*Width*.8f,(float)(random.NextDouble()-.5)*Depth*.8f,mountains?Mathf.Lerp(90,140,(float)random.NextDouble()):Mathf.Min(Width,Depth)*(.13f+(float)random.NextDouble()*.10f)));
            if(connectedMountains)
            {
                for(int i=0;i<hills.Count;i++)peakShapes.Add(new Vector4(
                    Mathf.Lerp(.65f,1.05f,(float)random.NextDouble()),
                    Mathf.Lerp(.75f,1.45f,(float)random.NextDouble()),
                    Mathf.Lerp(.75f,1.5f,(float)random.NextDouble()),
                    (float)random.NextDouble()*Mathf.PI*2));
                // Minimum spanning tree joins every peak by its nearest range saddle.
                var joined=new HashSet<int>{0};
                while(joined.Count<hills.Count)
                {
                    float best=float.MaxValue;int a=0,b=0;
                    for(int i=0;i<hills.Count;i++)if(joined.Contains(i))
                        for(int j=0;j<hills.Count;j++)if(!joined.Contains(j))
                        {float d=Vector2.SqrMagnitude(new Vector2(hills[i].x-hills[j].x,hills[i].y-hills[j].y));if(d<best){best=d;a=i;b=j;}}
                    ridgeLinks.Add(new Vector2Int(a,b));joined.Add(b);
                }
            }
            RefreshConstraints(district);
            for(int z=0;z<=Rows;z++)for(int x=0;x<=Columns;x++)Heights[z*(Columns+1)+x]=Generate(new Vector2((float)x/Columns*Width-Width/2,(float)z/Rows*Depth-Depth/2));
        }
        public int LastUpdatedSampleCount {get;private set;}
        readonly List<int> changedSamples=new();
        void RefreshConstraints(RegionCityTile district)
        {
            pads.Clear();channels.Clear();padIndex.Clear();channelIndex.Clear();
            foreach(var road in district.Roads??new List<PlacedRoadPiece>())
                if(road!=null)pads.Add(new Rect(-Width/2+road.GridX*DistrictScale.CellSizeMeters,-Depth/2+road.GridZ*DistrictScale.CellSizeMeters,DistrictScale.CellSizeMeters,DistrictScale.CellSizeMeters));
            foreach(var placed in district.Lots??new List<PlacedDistrictLot>())
            {
                if(placed==null)continue;var lot=LotContentCatalog.Read(placed.LotId);if(lot==null)continue;
                float w=DistrictScale.GridSpanForMeters(lot.LotWidthCells*LotMetricScale.MajorGridMeters)*DistrictScale.CellSizeMeters;
                float d=DistrictScale.GridSpanForMeters(lot.LotDepthCells*LotMetricScale.MajorGridMeters)*DistrictScale.CellSizeMeters;
                if((placed.RotationQuarterTurns&1)!=0)(w,d)=(d,w);
                pads.Add(new Rect(-Width/2+placed.GridX*DistrictScale.CellSizeMeters,-Depth/2+placed.GridZ*DistrictScale.CellSizeMeters,w,d));
            }
            foreach(var river in district.Rivers??new List<PlacedDistrictRiver>())
            {
                if(river?.Points==null)continue;
                for(int i=1;i<river.Points.Count;i++)
                {
                    var a=river.Points[i-1];var b=river.Points[i];
                    channels.Add((new Vector2((a.X-.5f)*Width,(a.Z-.5f)*Depth),new Vector2((b.X-.5f)*Width,(b.Z-.5f)*Depth),river.WidthMeters*.5f+25));
                }
            }
            float fade=mountains?45:Mathf.Max(90,amplitude*4);
            for(int i=0;i<pads.Count;i++)padIndex.Add(DistrictDirtyGrid.Expand(pads[i],12+fade),i);
            for(int i=0;i<channels.Count;i++)
            {
                var c=channels[i];var min=Vector2.Min(c.a,c.b);var max=Vector2.Max(c.a,c.b);
                channelIndex.Add(DistrictDirtyGrid.Expand(Rect.MinMaxRect(min.x,min.y,max.x,max.y),c.radius+fade),i);
            }
        }
        public bool RefreshLocal(RegionCityTile district,IReadOnlyList<Rect> areas)
        {
            RefreshConstraints(district);changedSamples.Clear();LastUpdatedSampleCount=0;
            var marked=new bool[Heights.Length];
            foreach(var area in areas)
            {
                int x0=Mathf.Clamp(Mathf.FloorToInt((area.xMin/Width+.5f)*Columns),0,Columns),x1=Mathf.Clamp(Mathf.CeilToInt((area.xMax/Width+.5f)*Columns),0,Columns);
                int z0=Mathf.Clamp(Mathf.FloorToInt((area.yMin/Depth+.5f)*Rows),0,Rows),z1=Mathf.Clamp(Mathf.CeilToInt((area.yMax/Depth+.5f)*Rows),0,Rows);
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)marked[z*(Columns+1)+x]=true;
            }
            for(int i=0;i<marked.Length;i++)if(marked[i])
            {
                LastUpdatedSampleCount++;int x=i%(Columns+1),z=i/(Columns+1);
                float value=Generate(new Vector2((float)x/Columns*Width-Width/2,(float)z/Rows*Depth-Depth/2));
                if(value!=Heights[i]){Heights[i]=value;changedSamples.Add(i);}
            }
            return changedSamples.Count>0;
        }
        public void UpdateMesh(Mesh mesh)
        {
            var vertices=mesh.vertices;
            foreach(int i in changedSamples){var v=vertices[i];v.y=Heights[i];vertices[i]=v;}
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        private float Generate(Vector2 p)
        {
            float h=0;
            foreach(var hill in hills)
            {
                float distance=Vector2.Distance(p,new Vector2(hill.x,hill.y))/hill.z;
                if(distance>=1)continue;
                if(mountains)h=Mathf.Max(h,1-distance);
                else {float cap=1-distance*distance;h+=cap*cap;}
            }
            if(connectedMountains)
            {
                float range=RangeHeight(p);
                float preserve=0;
                foreach(var site in preservedSites)
                    preserve=Mathf.Max(preserve,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(65,145,Vector2.Distance(site,p))));
                h=Mathf.Lerp(range,h,preserve);
            }
            float clearance=Mathf.Min(Width/2-Mathf.Abs(p.x),Depth/2-Mathf.Abs(p.y));
            foreach(var index in padIndex.Query(p))
            {
                var pad=pads[index];
                var delta=new Vector2(Mathf.Max(pad.xMin-p.x,0,p.x-pad.xMax),Mathf.Max(pad.yMin-p.y,0,p.y-pad.yMax));
                clearance=Mathf.Min(clearance,delta.magnitude-12);
            }
            foreach(var index in channelIndex.Query(p))
            {
                var c=channels[index];
                var ab=c.b-c.a;float t=ab.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector2.Dot(p-c.a,ab)/ab.sqrMagnitude);
                clearance=Mathf.Min(clearance,Vector2.Distance(p,c.a+t*ab)-c.radius);
            }
            return amplitude*(mountains?h:1-Mathf.Exp(-h))*Mathf.SmoothStep(0,1,Mathf.Clamp01(clearance/(mountains?45:Mathf.Max(90,amplitude*4))));
        }
        private float RangeHeight(Vector2 p)
        {
            var warp=new Vector2(Mathf.PerlinNoise(p.x/140f+18.3f,p.y/140f+7.1f)-.5f,
                Mathf.PerlinNoise(p.x/140f+91.7f,p.y/140f+37.2f)-.5f)*38;
            var q=p+warp;float h=0;
            for(int i=0;i<hills.Count;i++)
            {
                var hill=hills[i];var shape=peakShapes[i];var d=q-new Vector2(hill.x,hill.y);
                float c=Mathf.Cos(shape.w),s=Mathf.Sin(shape.w);
                var local=new Vector2((c*d.x-s*d.y)/(hill.z*shape.y),(s*d.x+c*d.y)/(hill.z*shape.z));
                float cap=Mathf.Max(0,1-local.magnitude);
                h=Mathf.Max(h,shape.x*Mathf.Pow(cap,.75f+shape.y*.35f));
            }
            foreach(var link in ridgeLinks)
            {
                var a=new Vector2(hills[link.x].x,hills[link.x].y);var b=new Vector2(hills[link.y].x,hills[link.y].y);
                var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(q-a,ab)/Mathf.Max(ab.sqrMagnitude,.001f));
                float width=Mathf.Lerp(hills[link.x].z,hills[link.y].z,t)*1.15f;
                float distance=Vector2.Distance(q,a+t*ab);
                float crest=Mathf.Lerp(peakShapes[link.x].x,peakShapes[link.y].x,t)*(.62f-.15f*Mathf.Sin(t*Mathf.PI));
                float ridge=crest*Mathf.Pow(Mathf.Max(0,1-distance/width),1.25f);
                float foothill=.14f*Mathf.Pow(Mathf.Max(0,1-distance/(width*1.7f)),2);
                h=Mathf.Max(h,ridge+foothill);
            }
            // Irregular shoulders and small gullies, with no change to texture scale.
            float detail=(Mathf.PerlinNoise(p.x/37+23.4f,p.y/37+64.2f)-.5f)*.16f
                +(Mathf.PerlinNoise(p.x/13+71.3f,p.y/13+16.4f)-.5f)*.04f;
            return Mathf.Clamp01(h+detail*Mathf.SmoothStep(0,1,h*5));
        }
        // Interpolate the same two triangles used by the rendered and collidable mesh.
        public float Sample(float x,float z)
        {
            float gx=Mathf.Clamp01(x/Width+.5f)*Columns,gz=Mathf.Clamp01(z/Depth+.5f)*Rows;
            int ix=Mathf.Min(Mathf.FloorToInt(gx),Columns-1),iz=Mathf.Min(Mathf.FloorToInt(gz),Rows-1);
            float u=gx-ix,v=gz-iz;int i=iz*(Columns+1)+ix;
            float a=Heights[i],b=Heights[i+1],c=Heights[i+Columns+1],d=Heights[i+Columns+2];
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        public Mesh CreateMesh()
        {
            var vertices=new Vector3[Heights.Length];var uv=new Vector2[Heights.Length];var triangles=new int[Columns*Rows*6];int k=0;
            for(int z=0;z<=Rows;z++)for(int x=0;x<=Columns;x++)
            {
                int i=z*(Columns+1)+x;uv[i]=new Vector2((float)x/Columns,(float)z/Rows);vertices[i]=new Vector3(uv[i].x*Width-Width/2,Heights[i],uv[i].y*Depth-Depth/2);
                if(x==Columns||z==Rows)continue;
                triangles[k++]=i;triangles[k++]=i+Columns+1;triangles[k++]=i+1;
                triangles[k++]=i+1;triangles[k++]=i+Columns+1;triangles[k++]=i+Columns+2;
            }
            var mesh=new Mesh{name="District Elevation",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
    public sealed class DistrictTerrainMeshOwner : MonoBehaviour
    {
        private void OnDestroy(){var mesh=GetComponent<MeshFilter>()?.sharedMesh;if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
