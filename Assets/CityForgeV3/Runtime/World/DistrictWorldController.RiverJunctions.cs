using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        // Build-time polygon subtraction: the water footprint owns a junction,
        // with no banks through it and no stacked transparent water triangles.
        private void MergeRiverJunctions(RegionCityTile district)
        {
            var filters = _riverRoot.GetComponentsInChildren<MeshFilter>();
            var widths = district.Rivers
                .Where(river => river != null &&
                                !string.IsNullOrWhiteSpace(river.InstanceId))
                .GroupBy(river => river.InstanceId)
                .ToDictionary(group => group.Key,
                    group => group.Max(river => river.WidthMeters));
            string RiverId(MeshFilter filter) =>
                filter.name.Substring("River Water — ".Length);
            float RiverWidth(MeshFilter filter) =>
                widths.TryGetValue(RiverId(filter), out var width)
                    ? width
                    : filter.sharedMesh.bounds.size.x;
            // A wider river owns the shared water footprint. Tributaries are
            // cut beneath it irrespective of serialization/generation order.
            var waters = filters
                .Where(f => f.name.StartsWith("River Water — "))
                .OrderByDescending(RiverWidth)
                .ThenBy(RiverId)
                .ToArray();
            var footprints = waters.Select(f => RiverMeshUnion.Footprint(f.sharedMesh)).ToArray();
            for (int i = 0; i < waters.Length; i++)
            {
                var suffix = RiverId(waters[i]);
                var others = new List<RiverMeshUnion.Quad>();
                var earlier = new List<RiverMeshUnion.Quad>();
                for (int j = 0; j < waters.Length; j++)
                {
                    if (j == i) continue;
                    others.AddRange(footprints[j]);
                    if (j < i) earlier.AddRange(footprints[j]);
                }
                var junctionFade = earlier.Count == 0
                    ? 0f
                    : Mathf.Clamp(RiverWidth(waters[i]) * .65f, 6f, 18f);
                RiverMeshUnion.Subtract(waters[i].sharedMesh, earlier, others,
                    junctionFade);
                foreach (var filter in filters)
                    if (filter != waters[i] && filter.name.EndsWith("— " + suffix)
                        && filter.sharedMesh.bounds.max.y >= waters[i].sharedMesh.bounds.center.y)
                        RiverMeshUnion.Subtract(filter.sharedMesh, others, null);
            }
        }
    }

    public static class RiverMeshUnion
    {
        public sealed class Quad
        {
            public Vector2[] Points;
            public Rect Bounds;
            public Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                Points = new[]{a,b,c,d};
                if(Cross(b-a,c-b)<0) System.Array.Reverse(Points);
                var min=Vector2.Min(Vector2.Min(a,b),Vector2.Min(c,d));
                var max=Vector2.Max(Vector2.Max(a,b),Vector2.Max(c,d));
                Bounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
            }
            public float Depth(Vector2 p)
            {
                if(!Bounds.Contains(p))return 0;
                for(int i=0;i<4;i++)if(Cross(Points[(i+1)%4]-Points[i],p-Points[i])<0)return 0;
                // Distance to the two shore edges, normalized by channel width.
                float left=DistanceLine(p,Points[0],Points[1]);
                float right=DistanceLine(p,Points[2],Points[3]);
                return Mathf.Clamp01(2*Mathf.Min(left,right)/Mathf.Max(.001f,left+right));
            }
            public float Distance(Vector2 p)
            {
                var inside = Bounds.Contains(p);
                for (int i = 0; inside && i < 4; i++)
                    inside = Cross(Points[(i + 1) % 4] - Points[i],
                        p - Points[i]) >= 0;
                if (inside) return 0f;
                var distance = float.PositiveInfinity;
                for (int i = 0; i < 4; i++)
                    distance = Mathf.Min(distance, DistanceSegment(p,
                        Points[i], Points[(i + 1) % 4]));
                return distance;
            }
        }
        sealed class SpatialIndex
        {
            const float Cell=128f;
            readonly Dictionary<Vector2Int,List<Quad>> cells=new();
            public SpatialIndex(List<Quad> quads)
            {
                if(quads==null)return;
                foreach(var q in quads)
                    for(int x=Mathf.FloorToInt(q.Bounds.xMin/Cell);x<=Mathf.FloorToInt(q.Bounds.xMax/Cell);x++)
                    for(int y=Mathf.FloorToInt(q.Bounds.yMin/Cell);y<=Mathf.FloorToInt(q.Bounds.yMax/Cell);y++)
                    {var key=new Vector2Int(x,y);if(!cells.TryGetValue(key,out var list))cells[key]=list=new();list.Add(q);}
            }
            public IEnumerable<Quad> Query(Rect bounds)
            {
                var seen=new HashSet<Quad>();
                for(int x=Mathf.FloorToInt(bounds.xMin/Cell);x<=Mathf.FloorToInt(bounds.xMax/Cell);x++)
                for(int y=Mathf.FloorToInt(bounds.yMin/Cell);y<=Mathf.FloorToInt(bounds.yMax/Cell);y++)
                    if(cells.TryGetValue(new Vector2Int(x,y),out var list))foreach(var q in list)if(seen.Add(q))yield return q;
            }
            public float Depth(Vector2 p)
            {
                float depth=0;
                if(cells.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x/Cell),Mathf.FloorToInt(p.y/Cell)),out var list))
                    foreach(var q in list)depth=Mathf.Max(depth,q.Depth(p));
                return depth;
            }
            public float Distance(Vector2 p,float maximum)
            {
                if(maximum<=0)return maximum;
                float distance=maximum;
                var extent=Vector2.one*maximum;
                foreach(var q in Query(new Rect(p-extent,extent*2f)))
                    distance=Mathf.Min(distance,q.Distance(p));
                return distance;
            }
        }
        struct Vertex
        {
            public Vector3 P;public Vector2 UV;public Vector2 Flow;public Color C;
            public static Vertex Lerp(Vertex a,Vertex b,float t)=>new(){P=Vector3.Lerp(a.P,b.P,t),UV=Vector2.Lerp(a.UV,b.UV,t),Flow=Vector2.Lerp(a.Flow,b.Flow,t),C=Color.Lerp(a.C,b.C,t)};
        }
        static Vector2 XZ(Vector3 p)=>new(p.x,p.z);
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static float DistanceLine(Vector2 p,Vector2 a,Vector2 b)=>Mathf.Abs(Cross(b-a,p-a))/Mathf.Max(.001f,(b-a).magnitude);
        static float DistanceSegment(Vector2 p,Vector2 a,Vector2 b)
        {
            var segment=b-a;
            var lengthSquared=segment.sqrMagnitude;
            if(lengthSquared<.000001f)return Vector2.Distance(p,a);
            var t=Mathf.Clamp01(Vector2.Dot(p-a,segment)/lengthSquared);
            return Vector2.Distance(p,a+segment*t);
        }
        public static List<Quad> Footprint(Mesh mesh)
        {
            var v=mesh.vertices;var result=new List<Quad>();
            for(int i=0;i+9<v.Length;i+=5)
                result.Add(new Quad(XZ(v[i]),XZ(v[i+5]),XZ(v[i+9]),XZ(v[i+4])));
            return result;
        }
        static List<Vertex> Half(List<Vertex> polygon,Vector2 a,Vector2 b,bool inside)
        {
            var result=new List<Vertex>();if(polygon.Count==0)return result;
            var previous=polygon[polygon.Count-1];float pd=Cross(b-a,XZ(previous.P)-a);
            bool pin=inside?pd>=0:pd<=0;
            foreach(var current in polygon)
            {
                float cd=Cross(b-a,XZ(current.P)-a);bool cin=inside?cd>=0:cd<=0;
                if(pin!=cin)result.Add(Vertex.Lerp(previous,current,pd/(pd-cd)));
                if(cin)result.Add(current);
                previous=current;pd=cd;pin=cin;
            }
            return result;
        }
        static Rect Bounds(List<Vertex> p)
        {
            var min=XZ(p[0].P);var max=min;
            foreach(var v in p){min=Vector2.Min(min,XZ(v.P));max=Vector2.Max(max,XZ(v.P));}
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        public static void ClipToRect(Mesh mesh,Rect rect)
        {
            var original=mesh.vertices;var uv=mesh.uv;var flow=mesh.uv2;var colors=mesh.colors;
            var vertices=new List<Vector3>();var tex=new List<Vector2>();var flows=new List<Vector2>();var tint=new List<Color>();
            var submeshes=new List<int[]>();
            var corners=new[]{new Vector2(rect.xMin,rect.yMin),new Vector2(rect.xMax,rect.yMin),new Vector2(rect.xMax,rect.yMax),new Vector2(rect.xMin,rect.yMax)};
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                var output=new List<int>();var triangles=mesh.GetTriangles(sub);
                for(int t=0;t<triangles.Length;t+=3)
                {
                    var polygon=new List<Vertex>();
                    for(int k=0;k<3;k++){int index=triangles[t+k];polygon.Add(new Vertex{P=original[index],UV=uv.Length>index?uv[index]:Vector2.zero,Flow=flow.Length>index?flow[index]:Vector2.zero,C=colors.Length>index?colors[index]:Color.white});}
                    for(int edge=0;edge<4;edge++)polygon=Half(polygon,corners[edge],corners[(edge+1)%4],true);
                    int start=vertices.Count;
                    foreach(var v in polygon){vertices.Add(v.P);tex.Add(v.UV);flows.Add(v.Flow);tint.Add(v.C);}
                    for(int k=1;k<polygon.Count-1;k++){output.Add(start);output.Add(start+k);output.Add(start+k+1);}
                }
                submeshes.Add(output.ToArray());
            }
            mesh.Clear();mesh.indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16;
            mesh.SetVertices(vertices);mesh.SetUVs(0,tex);mesh.SetUVs(1,flows);mesh.SetColors(tint);mesh.subMeshCount=submeshes.Count;
            for(int i=0;i<submeshes.Count;i++)mesh.SetTriangles(submeshes[i],i);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
        }

        public static void Subtract(Mesh mesh,List<Quad> masks,
            List<Quad> depthMasks,float fadeDistance=0f)
        {
            if(masks.Count==0&&(depthMasks==null||depthMasks.Count==0))return;
            var cutIndex=new SpatialIndex(masks);var depthIndex=new SpatialIndex(depthMasks);
            var original=mesh.vertices;var uv=mesh.uv;var flow=mesh.uv2;var colors=mesh.colors;
            var vertices=new List<Vector3>();var tex=new List<Vector2>();var flows=new List<Vector2>();var tint=new List<Color>();
            var submeshes=new List<int[]>();
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                var output=new List<int>();var triangles=mesh.GetTriangles(sub);
                for(int t=0;t<triangles.Length;t+=3)
                {
                    var polygon=new List<Vertex>();
                    for(int k=0;k<3;k++){int index=triangles[t+k];polygon.Add(new Vertex{P=original[index],UV=uv.Length>index?uv[index]:Vector2.zero,Flow=flow.Length>index?flow[index]:Vector2.zero,C=colors.Length>index?colors[index]:Color.white});}
                    var bounds=Bounds(polygon);
                    var fragments=new List<List<Vertex>>{polygon};
                    foreach(var mask in cutIndex.Query(bounds))
                    {
                        if(!bounds.Overlaps(mask.Bounds))continue;
                        var next=new List<List<Vertex>>();
                        foreach(var fragment in fragments)
                        {
                            if(!Bounds(fragment).Overlaps(mask.Bounds)){next.Add(fragment);continue;}
                            var remaining=fragment;
                            for(int edge=0;edge<4&&remaining.Count>=3;edge++)
                            {
                                var a=mask.Points[edge];var b=mask.Points[(edge+1)%4];
                                var outside=Half(remaining,a,b,false);
                                if(outside.Count>=3)next.Add(outside);
                                remaining=Half(remaining,a,b,true);
                            }
                        }
                        fragments=next;if(fragments.Count==0)break;
                    }
                    foreach(var fragment in fragments)
                    {
                        int start=vertices.Count;
                        foreach(var v in fragment)
                        {
                            var color=v.C;
                            if(depthMasks!=null)color.r=Mathf.Max(color.r,depthIndex.Depth(XZ(v.P)));
                            if(fadeDistance>0f&&masks.Count>0)
                            {
                                var distance=cutIndex.Distance(XZ(v.P),fadeDistance);
                                var normalized=Mathf.Clamp01(distance/fadeDistance);
                                color.a*=normalized*normalized*(3f-2f*normalized);
                            }
                            vertices.Add(v.P);tex.Add(v.UV);flows.Add(v.Flow);tint.Add(color);
                        }
                        for(int k=1;k<fragment.Count-1;k++)
                        {
                            if(Vector3.Cross(fragment[k].P-fragment[0].P,fragment[k+1].P-fragment[0].P).sqrMagnitude<1e-10f)continue;
                            output.Add(start);output.Add(start+k);output.Add(start+k+1);
                        }
                    }
                }
                submeshes.Add(output.ToArray());
            }
            mesh.Clear();mesh.indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16;
            mesh.SetVertices(vertices);mesh.SetUVs(0,tex);mesh.SetUVs(1,flows);mesh.SetColors(tint);mesh.subMeshCount=submeshes.Count;
            for(int i=0;i<submeshes.Count;i++)mesh.SetTriangles(submeshes[i],i);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
    }
}
