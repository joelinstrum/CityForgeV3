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
        // Zero in older saves means the pre-scale default of 1x.
        public float VerticalReliefScale = 1f;
    }
    // Deterministic district relief. Existing roads, lots and water retain level corridors.
    public sealed class DistrictElevation
    {
        private readonly struct RollingReliefRegion
        {
            public readonly Vector2 Center,Along,Across;
            public readonly Rect Bounds;
            public RollingReliefRegion(Vector2 center,Vector2 radii,Vector2 direction)
            {
                Center=center;
                Along=direction/radii.x;
                Across=new Vector2(-direction.y,direction.x)/radii.y;
                float halfX=Mathf.Sqrt(Mathf.Pow(radii.x*direction.x,2)+Mathf.Pow(radii.y*direction.y,2));
                float halfY=Mathf.Sqrt(Mathf.Pow(radii.x*direction.y,2)+Mathf.Pow(radii.y*direction.x,2));
                Bounds=Rect.MinMaxRect(center.x-halfX,center.y-halfY,center.x+halfX,center.y+halfY);
            }
        }
        // 1x reproduces the rolling-hill relief before this presentation tune.
        // This only scales Y after the horizontal height field is sampled.
        public const float RollingHillVerticalScale = 1.30f;
        // Fractions of the district's smaller span. The geometric-mean
        // wavelength stays constant as anisotropy changes.
        public const float RollingPrimaryWavelength = .42f;
        public const float RollingPrimaryAnisotropy = 1.35f;
        // Broad lateral displacement of primary coordinates, using the
        // existing secondary field. Zero restores straight primary coordinates.
        public const float RollingDomainWarpFraction = .08f;
        public readonly float Width, Depth;
        public readonly int Columns, Rows;
        public readonly float[] Heights;
        private readonly List<Rect> pads = new();
        readonly DistrictSpatialIndex<int> padIndex=new(),channelIndex=new();
        private readonly List<(Vector2 a, Vector2 b, float radius)> channels = new();
        private readonly List<Vector3> hills = new();
        private readonly List<RollingReliefRegion> rollingRegions = new();
        private readonly Vector2 rollingDirection,rollingSecondaryDirection;
        private readonly Vector2 rollingOffset,rollingSecondaryOffset;
        private readonly float rollingCoverage;
        private readonly float amplitude;
        private readonly float clearanceFadeMeters;
        private readonly float edgeFadeMeters;
        private float verticalCalibration = 1f;
        private readonly float verticalReliefScale;
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
            amplitude=settings==null?0:Mathf.Clamp(settings.HeightMeters,0,240);
            clearanceFadeMeters=mountains?45:Mathf.Max(90,Mathf.Min(60,amplitude)*4);
            edgeFadeMeters=mountains?clearanceFadeMeters:
                Mathf.Max(clearanceFadeMeters,Mathf.Min(Width,Depth)*.22f);
            verticalReliefScale=mountains || settings==null || settings.VerticalReliefScale<=0
                ? 1f : Mathf.Clamp(settings.VerticalReliefScale,.25f,4f);
            if(amplitude<=0)return;
            var random=new System.Random(settings.Seed);
            if(!mountains)
            {
                rollingCoverage=Mathf.Clamp01(settings.Coverage);
                float angle=(float)random.NextDouble()*Mathf.PI*2;
                rollingDirection=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                float secondaryAngle=angle+.55f+(float)random.NextDouble()*.45f;
                rollingSecondaryDirection=new Vector2(Mathf.Cos(secondaryAngle),Mathf.Sin(secondaryAngle));
                rollingOffset=new Vector2(13f+(float)random.NextDouble()*997f,29f+(float)random.NextDouble()*997f);
                rollingSecondaryOffset=new Vector2(41f+(float)random.NextDouble()*997f,73f+(float)random.NextDouble()*997f);
                float span=Mathf.Min(Width,Depth);
                float regionAngle=(float)random.NextDouble()*Mathf.PI*2;
                int[] ringOrder={0,4,2,6,1,5,3,7};
                for(int i=0;i<9;i++)
                {
                    Vector2 center;
                    if(i==0)center=new Vector2(((float)random.NextDouble()-.5f)*Width*.2f,
                        ((float)random.NextDouble()-.5f)*Depth*.2f);
                    else
                    {
                        float a=regionAngle+ringOrder[i-1]*Mathf.PI*.25f+
                            ((float)random.NextDouble()-.5f)*.3f;
                        float distance=Mathf.Lerp(.29f,.38f,(float)random.NextDouble());
                        center=new Vector2(Mathf.Cos(a)*Width*distance,Mathf.Sin(a)*Depth*distance);
                    }
                    float angleForRegion=(float)random.NextDouble()*Mathf.PI*2;
                    rollingRegions.Add(new RollingReliefRegion(center,
                        new Vector2(span*Mathf.Lerp(.37f,.45f,(float)random.NextDouble()),
                            span*Mathf.Lerp(.25f,.30f,(float)random.NextDouble())),
                        new Vector2(Mathf.Cos(angleForRegion),Mathf.Sin(angleForRegion))));
                }
            }
            else
            {
                int count=Mathf.RoundToInt(Mathf.Lerp(3,14,Mathf.Clamp01(settings.Coverage)));
                for(int i=0;i<count;i++)
                {
                    float x=(float)(random.NextDouble()-.5)*Width*.8f;
                    float z=(float)(random.NextDouble()-.5)*Depth*.8f;
                    float radius=Mathf.Lerp(90,140,(float)random.NextDouble());
                    hills.Add(new Vector3(x,z,radius));
                }
            }
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
            float unconstrainedPeak=0;
            for(int z=0;z<=Rows;z++)for(int x=0;x<=Columns;x++)
            {
                Heights[z*(Columns+1)+x]=Generate(new Vector2((float)x/Columns*Width-Width/2,(float)z/Rows*Depth-Depth/2),out float rawHeight);
                if(!mountains)unconstrainedPeak=Mathf.Max(unconstrainedPeak,rawHeight);
            }
            if(!mountains && unconstrainedPeak>0)
            {
                // Calibrate the sampled field once. Clearance edits reuse this
                // factor, so a local road or river change cannot rescale the
                // rest of the district or change the horizontal field.
                verticalCalibration=amplitude*verticalReliefScale*RollingHillVerticalScale/unconstrainedPeak;
                for(int i=0;i<Heights.Length;i++)Heights[i]*=verticalCalibration;
            }
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
            float fade=clearanceFadeMeters;
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
                float value=Generate(new Vector2((float)x/Columns*Width-Width/2,(float)z/Rows*Depth-Depth/2),out _)*verticalCalibration;
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
        private float Generate(Vector2 p,out float unconstrainedHeight)
        {
            float h=0;
            if(mountains)for(int i=0;i<hills.Count;i++)
            {
                var hill=hills[i];
                float distance=Vector2.Distance(p,new Vector2(hill.x,hill.y))/hill.z;
                if(distance<1)h=Mathf.Max(h,1-distance);
            }
            if(!mountains)
            {
                // Two continuous, broad fields shape each relief region.
                // The long/short axes stretch features into ridges;
                // the minor field bends them into shoulders and shallow saddles.
                float scale=Mathf.Min(Width,Depth);
                float su=(rollingSecondaryDirection.x*p.x-rollingSecondaryDirection.y*p.y)/(scale*1.1f)+rollingSecondaryOffset.x;
                float sv=(rollingSecondaryDirection.y*p.x+rollingSecondaryDirection.x*p.y)/(scale*.75f)+rollingSecondaryOffset.y;
                float secondary=Mathf.PerlinNoise(su,sv);
                // Coverage activates more of nine deterministic, soft-edged
                // regions. They gate the continuous field; they are not peaks.
                // A fractional last region makes coverage vary continuously.
                float active=Mathf.Min(rollingRegions.Count,rollingCoverage*12f);
                float outside=1f;
                Vector2 maskPoint=p+rollingDirection*(secondary-.5f)*scale*.18f;
                for(int i=0;i<rollingRegions.Count && i<active;i++)
                {
                    var region=rollingRegions[i];
                    if(!region.Bounds.Contains(maskPoint))continue;
                    float dx=maskPoint.x-region.Center.x,dz=maskPoint.y-region.Center.y;
                    float along=dx*region.Along.x+dz*region.Along.y;
                    float across=dx*region.Across.x+dz*region.Across.y;
                    float squared=along*along+across*across;
                    if(squared>=1)continue;
                    // The entire broad region eases toward its boundary so
                    // the spatial envelope cannot create an embankment edge.
                    float cap=1-squared;
                    float influence=cap*cap*(3-2*cap)*Mathf.Clamp01(active-i);
                    outside*=1-influence;
                    if(outside<=.0001f)break;
                }
                float local=1-outside;
                float broad=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,1f,rollingCoverage));
                float envelope=broad+(1-broad)*local;
                if(envelope<=0){unconstrainedHeight=0;return 0;}
                float longAxis=scale*RollingPrimaryWavelength*RollingPrimaryAnisotropy;
                float shortAxis=scale*RollingPrimaryWavelength/RollingPrimaryAnisotropy;
                float u=(rollingDirection.x*p.x-rollingDirection.y*p.y)/longAxis+rollingOffset.x;
                float v=(rollingDirection.y*p.x+rollingDirection.x*p.y
                    +(secondary-.5f)*scale*RollingDomainWarpFraction)/shortAxis+rollingOffset.y;
                float field=.84f*Mathf.PerlinNoise(u,v)+.16f*secondary;
                float form=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.255f,.78f,field));
                h=form*envelope;
            }
            if(connectedMountains)
            {
                float range=RangeHeight(p);
                float preserve=0;
                foreach(var site in preservedSites)
                    preserve=Mathf.Max(preserve,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(65,145,Vector2.Distance(site,p))));
                h=Mathf.Lerp(range,h,preserve);
            }
            float edgeX=Width/2-Mathf.Abs(p.x),edgeZ=Depth/2-Mathf.Abs(p.y);
            float edgeClearance=Mathf.Min(edgeX,edgeZ);
            if(!mountains)
            {
                // The hard minimum creates a diagonal ridge from each district
                // corner where the nearest edge switches. Lift its shoulders
                // into a rounded crown while keeping the actual border level.
                float joinWidth=edgeFadeMeters*.6f;
                float separation=Mathf.Abs(edgeX-edgeZ);
                if(separation<joinWidth)
                {
                    float join=1f-separation/joinWidth;
                    float border=Mathf.Clamp01(edgeClearance/(joinWidth*.5f));
                    edgeClearance+=separation*.5f*join*join*border;
                }
            }
            float shapeHeight=amplitude*(mountains?h:1-Mathf.Exp(-h*1.7f));
            float edgeBlend=Mathf.SmoothStep(0,1,Mathf.Clamp01(edgeClearance/edgeFadeMeters));
            unconstrainedHeight=shapeHeight*edgeBlend;
            float clearance=float.PositiveInfinity;
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
            return shapeHeight*Mathf.Min(edgeBlend,
                Mathf.SmoothStep(0,1,Mathf.Clamp01(clearance/clearanceFadeMeters)));
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
