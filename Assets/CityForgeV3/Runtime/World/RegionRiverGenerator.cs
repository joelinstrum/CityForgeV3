using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class RegionRiverPath
    {
        public string Id;
        public bool HandDrawn;
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
            int total=deep+streams;
            if(total==0)return result;
            var flow=settings.Flow==RegionRiverFlow.Varied ? (RegionRiverFlow)random.Next(1,5) : settings.Flow;
            bool horizontal=flow==RegionRiverFlow.WestToEast||flow==RegionRiverFlow.EastToWest;
            bool reversed=flow==RegionRiverFlow.EastToWest||flow==RegionRiverFlow.NorthToSouth;
            // Deep rivers are full-span stair-step trunks. If only streams were
            // requested, the first stream becomes the trunk. Remaining streams
            // are exact perpendicular segments that terminate on a trunk.
            int trunks=deep>0?deep:1;
            for(int n=0;n<total;n++)
            {
                var path=new RegionRiverPath
                {
                    Id=$"region-{seed}-{n}",
                    Depth=n<deep?DistrictRiverDepth.Deep:DistrictRiverDepth.Shallow,
                    WidthMeters=n<deep?Next(48,76):Next(14,24)
                };
                if(n<trunks)
                {
                    float slot=(n+1f)/(trunks+1f);
                    float jitter=(Next(-.18f,.18f)/(trunks+1f));
                    float cross=Mathf.Lerp(.08f,.92f,Mathf.Clamp01(slot+jitter));
                    BuildStairTrunk(path,horizontal,reversed,
                        horizontal?region.Width:region.Height,
                        horizontal?region.Height:region.Width,cross,random);
                }
                else
                {
                    var parent=result[random.Next(trunks)];
                    var primarySegments=new List<int>();
                    for(int index=0;index<parent.Points.Count-1;index++)
                    {
                        var a=parent.Points[index];var b=parent.Points[index+1];
                        if(horizontal?Mathf.Abs(a.X-b.X)>.000001f:
                            Mathf.Abs(a.Z-b.Z)>.000001f)
                            primarySegments.Add(index);
                    }
                    var segment=primarySegments[random.Next(primarySegments.Count)];
                    var segmentStart=new Vector2(parent.Points[segment].X,
                        parent.Points[segment].Z);
                    var segmentEnd=new Vector2(parent.Points[segment+1].X,
                        parent.Points[segment+1].Z);
                    var join=Vector2.Lerp(segmentStart,segmentEnd,Next(.2f,.8f));
                    bool lowSide=random.NextDouble()<.5;
                    var start=horizontal
                        ? new Vector2(join.x,lowSide?0:region.Height)
                        : new Vector2(lowSide?0:region.Width,join.y);
                    path.Points.Add(new DistrictRiverPoint(start.x,start.y));
                    path.Points.Add(new DistrictRiverPoint(join.x,join.y));
                    InsertPointOnSegment(parent,segment,join);
                }
                result.Add(path);
            }
            return result;
        }

        private static void BuildStairTrunk(RegionRiverPath path,
            bool horizontal, bool reversed, int alongCells, int crossCells,
            float normalizedCross, System.Random random)
        {
            var minimumCross=Mathf.Max(0,Mathf.CeilToInt(crossCells*.08f));
            var maximumCross=Mathf.Min(crossCells,
                Mathf.FloorToInt(crossCells*.92f));
            var baseCross=Mathf.Clamp(Mathf.RoundToInt(
                normalizedCross*crossCells),minimumCross,maximumCross);
            var maximumOffset=Mathf.Max(1,Mathf.RoundToInt(crossCells*.20f));
            var lowerCross=Mathf.Max(minimumCross,baseCross-maximumOffset);
            var upperCross=Mathf.Min(maximumCross,baseCross+maximumOffset);
            var logical=new List<Vector2>();
            void Add(int along,int cross)
            {
                var point=horizontal?new Vector2(along,cross):
                    new Vector2(cross,along);
                if(logical.Count==0||Vector2.Distance(logical[^1],point)>.000001f)
                    logical.Add(point);
            }

            var currentAlong=0;var currentCross=baseCross;var previousRun=0;
            Add(currentAlong,currentCross);
            while(alongCells-currentAlong>1)
            {
                var remaining=alongCells-currentAlong;
                var maximumRun=Mathf.Min(remaining-1,
                    Mathf.Max(2,Mathf.RoundToInt(alongCells*.34f)));
                var run=random.Next(1,maximumRun+1);
                if(maximumRun>1&&run==previousRun)run=run%maximumRun+1;
                previousRun=run;currentAlong+=run;Add(currentAlong,currentCross);
                if(alongCells-currentAlong<=1)break;
                if(lowerCross==upperCross)continue;
                var target=random.Next(lowerCross,upperCross+1);
                if(target==currentCross)target=target==upperCross?lowerCross:target+1;
                currentCross=target;Add(currentAlong,currentCross);
            }
            if(currentCross!=baseCross)Add(currentAlong,baseCross);
            Add(alongCells,baseCross);
            if(reversed)logical.Reverse();
            foreach(var point in RiverPathGeometry.RoundOrthogonalCorners(
                        logical,1.8f))
                path.Points.Add(new DistrictRiverPoint(point.x,point.y));
        }

        private static void InsertPointOnSegment(RegionRiverPath path,
            int segment, Vector2 point)
        {
            var a=new Vector2(path.Points[segment].X,path.Points[segment].Z);
            var b=new Vector2(path.Points[segment+1].X,path.Points[segment+1].Z);
            if(Vector2.Distance(a,point)<.000001f||
                Vector2.Distance(b,point)<.000001f)return;
            path.Points.Insert(segment+1,new DistrictRiverPoint(point.x,point.y));
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
            // Regeneration replaces generated rivers, retaining authored rivers.
            var combined=new List<RegionRiverPath>(paths);
            foreach(var existing in region.RiverPaths ?? new())
                if(existing.HandDrawn && !combined.Exists(p=>p.Id==existing.Id))combined.Add(existing);
            paths=combined;
            foreach(var tile in region.Tiles)
            {
                if(tile.RiversEditedLocally)continue;
                tile.Rivers ??= new List<PlacedDistrictRiver>();
                tile.Rivers.RemoveAll(r=>r!=null&&!string.IsNullOrEmpty(r.RegionRiverId));
                tile.Rivers.AddRange(Sections(tile,paths));
            }
            region.RiverPaths=paths;
        }
    }
}
