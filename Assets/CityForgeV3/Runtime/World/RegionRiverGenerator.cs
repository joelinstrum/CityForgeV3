using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class RegionRiverPath
    {
        public string Id;
        public DistrictRiverDepth Depth;
        public float WidthMeters;
        // Region map units, shared by all districts; Z corresponds to tile.Y.
        public List<DistrictRiverPoint> Points = new();
    }

    public static class RegionRiverGenerator
    {
        public static int FreshSeed(int previous)
        {
            var seed = Guid.NewGuid().GetHashCode() & int.MaxValue;
            return seed == 0 || seed == previous ? (previous == int.MaxValue ? 1 : previous + 1) : seed;
        }

        public static List<RegionRiverPath> Generate(RegionSaveData region, RegionTerrainSettings settings, int seed)
        {
            if (region == null || region.Width <= 0 || region.Height <= 0) throw new ArgumentException("Invalid region dimensions.");
            settings ??= new RegionTerrainSettings();
            var result = new List<RegionRiverPath>();
            var random = new System.Random(seed);
            float Next(float a, float b) => Mathf.Lerp(a,b,(float)random.NextDouble());
            int Count(RegionWaterAmount value,int few,int many) => value==RegionWaterAmount.Few?few:value==RegionWaterAmount.Many?many:0;
            int deep=Count(settings.DeepRivers,2,5), streams=Count(settings.Streams,4,10);
            var flow=settings.Flow==RegionRiverFlow.Varied ? (RegionRiverFlow)random.Next(1,5) : settings.Flow;
            // Construct drainage in a canonical downstream axis, then rotate the whole
            // network. Every branch terminates at its first encounter with older water.
            Vector2 Orient(Vector2 p) => flow switch
            {
                RegionRiverFlow.WestToEast => new Vector2(p.y*region.Width,p.x*region.Height),
                RegionRiverFlow.EastToWest => new Vector2((1-p.y)*region.Width,p.x*region.Height),
                RegionRiverFlow.NorthToSouth => new Vector2(p.x*region.Width,(1-p.y)*region.Height),
                _ => new Vector2(p.x*region.Width,p.y*region.Height)
            };
            var canonical=new List<List<Vector2>>();
            int attempts = 0;
            for(int n=0;n<deep+streams;n++)
            {
                var anchors=new List<Vector2>();
                if(n==0)
                {
                    var count=random.Next(7,11);
                    var x=Next(.18f,.82f);
                    for(int i=0;i<count;i++)
                    {
                        if(i>0)x=Mathf.Clamp(x+Next(-.30f,.30f),.08f,.92f);
                        anchors.Add(new Vector2(x,i/(float)(count-1)));
                    }
                }
                else
                {
                    // Choose from existing branches too, producing tributaries of
                    // tributaries instead of a row of independent parallel channels.
                    var parent=canonical[random.Next(canonical.Count)];
                    int join=random.Next(Mathf.Max(1,parent.Count/3),Mathf.Max(2,parent.Count-2));
                    var end=parent[join];
                    bool left=random.NextDouble()<.5;
                    var start=new Vector2(left?0:1,Mathf.Clamp(end.y-Next(.16f,.60f),.02f,.85f));
                    int count=random.Next(4,8);
                    for(int i=0;i<count;i++)
                    {
                        float t=i/(float)(count-1);var p=Vector2.Lerp(start,end,t);
                        if(i>0&&i<count-1)
                        {
                            p.x+=Next(-.12f,.12f)*Mathf.Sin(t*Mathf.PI);
                            p.y+=Next(-.12f,.12f)*Mathf.Sin(t*Mathf.PI);
                        }
                        anchors.Add(new Vector2(Mathf.Clamp01(p.x),Mathf.Clamp01(p.y)));
                    }
                }
                var points=new List<Vector2>();
                // Corner cutting retains irregular large bends without sharp mesh
                // mitres or a repeated sinusoid. Endpoints stay exact.
                points.AddRange(anchors);
                for(int pass=0;pass<3;pass++)
                {
                    var smooth=new List<Vector2>{points[0]};
                    for(int i=1;i<points.Count;i++)
                    {smooth.Add(Vector2.Lerp(points[i-1],points[i],.25f));smooth.Add(Vector2.Lerp(points[i-1],points[i],.75f));}
                    smooth.Add(points[points.Count-1]);points=smooth;
                }
                if(n>0)
                {
                    var trial = new List<List<Vector2>>();
                    foreach(var existing in canonical)trial.Add(new List<Vector2>(existing));
                    JoinFirstRiver(points,trial);
                    float length=0;
                    for(int i=1;i<points.Count;i++)length+=Vector2.Distance(points[i-1],points[i]);
                    if(length<.10f)
                    {
                        if(++attempts>128)throw new InvalidOperationException("Could not find enough distinct tributaries. Try another river seed.");
                        n--;continue;
                    }
                    canonical=trial;
                }
                attempts=0;
                canonical.Add(points);
                var path=new RegionRiverPath{Id=$"region-{seed}-{n}",Depth=n<deep?DistrictRiverDepth.Deep:DistrictRiverDepth.Shallow,
                    WidthMeters=n<deep?Next(48,76):Next(14,24)};
                foreach(var point in points){var p=Orient(point);path.Points.Add(new DistrictRiverPoint(p.x,p.y));}
                result.Add(path);
                // Joining inserts an exact shared point into the parent; synchronize
                // its master geometry before districts are clipped.
                for(int j=0;j<n;j++)
                {
                    result[j].Points.Clear();
                    foreach(var point in canonical[j]){var p=Orient(point);result[j].Points.Add(new DistrictRiverPoint(p.x,p.y));}
                }
            }
            return result;
        }

        private static void JoinFirstRiver(List<Vector2> branch,List<List<Vector2>> rivers)
        {
            for(int i=1;i<branch.Count;i++)
            {
                var a=branch[i-1];var d=branch[i]-a;
                var best=2f;List<Vector2> hitRiver=null;int hitIndex=0;Vector2 hit=default;
                foreach(var river in rivers)for(int j=1;j<river.Count;j++)
                {
                    var b=river[j-1];var e=river[j]-b;
                    float cross=d.x*e.y-d.y*e.x;
                    if(Mathf.Abs(cross)<1e-10f)continue;
                    var delta=b-a;
                    float t=(delta.x*e.y-delta.y*e.x)/cross;
                    float u=(delta.x*d.y-delta.y*d.x)/cross;
                    if(t<-.00001f||t>1.00001f||u<-.00001f||u>1.00001f||t>=best)continue;
                    best=t;hitRiver=river;hitIndex=j;hit=a+d*Mathf.Clamp01(t);
                }
                if(hitRiver==null)continue;
                branch.RemoveRange(i,branch.Count-i);
                if(Vector2.Distance(branch[branch.Count-1],hit)>1e-6f)branch.Add(hit);
                hitRiver.Insert(hitIndex,hit);
                return;
            }
        }

        // Clip exact shared segments rather than generating or snapping per district.
        public static List<PlacedDistrictRiver> Sections(RegionCityTile tile, IReadOnlyList<RegionRiverPath> paths)
        {
            var result = new List<PlacedDistrictRiver>();
            var rect = new Rect(tile.X, tile.Y, tile.Width, tile.Height);
            foreach (var path in paths)
            {
                PlacedDistrictRiver section = null;
                for (var i = 1; i < path.Points.Count; i++)
                {
                    var a = new Vector2(path.Points[i-1].X, path.Points[i-1].Z);
                    var b = new Vector2(path.Points[i].X, path.Points[i].Z);
                    if (!Clip(rect, ref a, ref b) || (a-b).sqrMagnitude < 1e-12f) { section = null; continue; }
                    var localA = new Vector2((a.x-tile.X)/tile.Width,(a.y-tile.Y)/tile.Height);
                    var localB = new Vector2((b.x-tile.X)/tile.Width,(b.y-tile.Y)/tile.Height);
                    if (section != null)
                    {
                        var last = section.Points[section.Points.Count-1];
                        if (Vector2.Distance(new Vector2(last.X,last.Z),localA) > .00001f) section = null;
                    }
                    if (section == null)
                    {
                        section = new PlacedDistrictRiver { InstanceId = $"{path.Id}-{tile.TileId}-{result.Count}", RegionRiverId = path.Id,
                            Depth = path.Depth, WidthMeters = path.WidthMeters, Direction = DirectionOf(path) };
                        section.Points.Add(new DistrictRiverPoint(localA.x,localA.y)); result.Add(section);
                    }
                    section.Points.Add(new DistrictRiverPoint(localB.x,localB.y));
                }
            }
            return result;
        }

        public static DistrictRiverDirection DirectionOf(RegionRiverPath path)
        {
            var first=path.Points[0];var last=path.Points[path.Points.Count-1];
            float x=last.X-first.X,z=last.Z-first.Z;
            return Mathf.Abs(x)>Mathf.Abs(z)?(x>=0?DistrictRiverDirection.WestToEast:DistrictRiverDirection.EastToWest)
                :(z>=0?DistrictRiverDirection.SouthToNorth:DistrictRiverDirection.NorthToSouth);
        }

        public static bool Clip(Rect rect, ref Vector2 a, ref Vector2 b)
        {
            var origin = a; var delta = b-a; var lo = 0f; var hi = 1f;
            bool Edge(float p, float q)
            {
                if (Mathf.Abs(p)<1e-8f) return q>=0;
                var t=q/p;
                if(p<0) { if(t>hi)return false; lo=Mathf.Max(lo,t); }
                else { if(t<lo)return false; hi=Mathf.Min(hi,t); }
                return true;
            }
            if(!Edge(-delta.x,origin.x-rect.xMin)||!Edge(delta.x,rect.xMax-origin.x)||
               !Edge(-delta.y,origin.y-rect.yMin)||!Edge(delta.y,rect.yMax-origin.y))return false;
            a=origin+delta*lo;b=origin+delta*hi;return true;
        }

        public static void Apply(RegionSaveData region, List<RegionRiverPath> paths)
        {
            foreach(var tile in region.Tiles)
            {
                tile.Rivers ??= new List<PlacedDistrictRiver>();
                tile.Rivers.RemoveAll(r=>r!=null&&!string.IsNullOrEmpty(r.RegionRiverId));
                tile.Rivers.AddRange(Sections(tile,paths));
            }
            region.RiverPaths=paths;
        }
    }
}
