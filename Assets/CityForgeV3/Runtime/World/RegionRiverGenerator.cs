using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum GeneratedRegionRiverSize
    {
        None,
        Major,
        Medium,
        Small,
        Stream
    }

    [Serializable]
    public sealed class RegionRiverPath
    {
        public string Id;
        public bool HandDrawn;
        public GeneratedRegionRiverSize GeneratedSize;
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
            bool explicitCounts=settings.RiverCountsVersion>0;
            int major=settings.DeepRivers==RegionWaterAmount.None?0:1;
            int medium=explicitCounts?Mathf.Clamp(settings.MediumRiverCount,0,3):0;
            int small=explicitCounts?Mathf.Clamp(settings.SmallRiverCount,0,5):
                Count(settings.Streams,2,5);
            int streams=explicitCounts?Mathf.Clamp(settings.StreamCount,0,5):0;
            var horizontalSizes=new List<GeneratedRegionRiverSize>();
            var verticalSizes=new List<GeneratedRegionRiverSize>();
            void AddBalanced(GeneratedRegionRiverSize size,int count)
            {
                // Alternate within every size. An odd remainder goes to the
                // direction with fewer rivers so the complete layout remains
                // as balanced as its total count permits.
                bool horizontal=horizontalSizes.Count<=verticalSizes.Count;
                for(int index=0;index<count;index++)
                {
                    (horizontal?horizontalSizes:verticalSizes).Add(size);
                    horizontal=!horizontal;
                }
            }
            AddBalanced(GeneratedRegionRiverSize.Major,major);
            AddBalanced(GeneratedRegionRiverSize.Medium,medium);
            AddBalanced(GeneratedRegionRiverSize.Small,small);
            AddBalanced(GeneratedRegionRiverSize.Stream,streams);
            var sizes=new List<GeneratedRegionRiverSize>(horizontalSizes);
            sizes.AddRange(verticalSizes);
            int total=sizes.Count;
            if(total==0)return result;
            int horizontalCount=horizontalSizes.Count;
            int verticalCount=total-horizontalCount;
            int horizontalLane=0,verticalLane=0;
            for(int n=0;n<total;n++)
            {
                bool horizontal=n<horizontalCount;
                var size=sizes[n];
                var path=new RegionRiverPath
                {
                    Id=$"region-{seed}-{n}",
                    GeneratedSize=size,
                    Depth=size is GeneratedRegionRiverSize.Major or
                        GeneratedRegionRiverSize.Medium?
                        DistrictRiverDepth.Deep:DistrictRiverDepth.Shallow,
                    WidthMeters=size switch
                    {
                        GeneratedRegionRiverSize.Major=>Next(144,228),
                        GeneratedRegionRiverSize.Medium=>Next(48,76),
                        GeneratedRegionRiverSize.Small=>Next(24,36),
                        _=>Next(10,16)
                    }
                };
                int lane=horizontal?horizontalLane++:verticalLane++;
                int laneCount=horizontal?horizontalCount:verticalCount;
                BuildCenterSeekingTrunk(region,path,horizontal,lane,laneCount,
                    random);
                result.Add(path);
            }
            return result;
        }

        private static void BuildCenterSeekingTrunk(RegionSaveData region,
            RegionRiverPath path, bool horizontal, int lane, int laneCount,
            System.Random random)
        {
            int alongCells=horizontal?region.Width:region.Height;
            int crossCells=horizontal?region.Height:region.Width;
            float bandWidth=crossCells/(float)laneCount;
            float margin=Mathf.Min(.75f,bandWidth*.1f);
            float lowerCross=lane*bandWidth+margin;
            float upperCross=(lane+1)*bandWidth-margin;
            float idealCross=(lowerCross+upperCross)*.5f;

            var centers=new List<Vector2>();
            foreach(var tile in region.Tiles??new List<RegionCityTile>())
            {
                if(tile==null)continue;
                if(tile.Lots!=null&&tile.Lots.Count>0)continue;
                float along=horizontal?tile.X+tile.Width*.5f:
                    tile.Y+tile.Height*.5f;
                float cross=horizontal?tile.Y+tile.Height*.5f:
                    tile.X+tile.Width*.5f;
                if(cross>=lowerCross-.0001f&&cross<=upperCross+.0001f)
                    centers.Add(new Vector2(along,cross));
            }
            float baseCross=idealCross;
            float closest=float.PositiveInfinity;
            foreach(var center in centers)
            {
                float distance=Mathf.Abs(center.y-idealCross);
                if(distance>=closest)continue;
                closest=distance;baseCross=center.y;
            }

            centers.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):
                Mathf.Abs(a.y-baseCross).CompareTo(Mathf.Abs(b.y-baseCross)));
            var targets=new List<Vector2>();
            float maximumCenterSwing=Mathf.Min(2.25f,bandWidth*.24f);
            foreach(var center in centers)
            {
                if(center.x<=.0001f||center.x>=alongCells-.0001f||
                    Mathf.Abs(center.y-baseCross)>maximumCenterSwing)continue;
                if(targets.Count>0&&Mathf.Abs(targets[^1].x-center.x)<.0001f)
                    continue;
                targets.Add(center);
            }

            var logical=new List<Vector2>();
            void Add(float along,float cross)
            {
                var point=horizontal?new Vector2(along,cross):
                    new Vector2(cross,along);
                if(logical.Count==0||Vector2.Distance(logical[^1],point)>.000001f)
                    logical.Add(point);
            }
            float Next(float a,float b)=>Mathf.Lerp(a,b,
                (float)random.NextDouble());
            float currentAlong=0,currentCross=baseCross;
            Add(currentAlong,currentCross);
            targets.Add(new Vector2(alongCells,baseCross));
            foreach(var target in targets)
            {
                float span=target.x-currentAlong;
                if(span<=.0001f)continue;
                if(Mathf.Abs(target.y-currentCross)>.0001f)
                {
                    float turn=currentAlong+span*Next(.34f,.58f);
                    Add(turn,currentCross);Add(turn,target.y);
                }
                else if(span>=4f)
                {
                    float bendAmount=Mathf.Min(1.25f,bandWidth*.12f);
                    bool canRise=currentCross+bendAmount<=upperCross;
                    bool canFall=currentCross-bendAmount>=lowerCross;
                    float direction=canRise&&canFall?
                        (random.NextDouble()<.5?-1f:1f):canRise?1f:-1f;
                    float bendCross=Mathf.Clamp(currentCross+
                        direction*bendAmount,lowerCross,upperCross);
                    float entry=currentAlong+span*Next(.22f,.36f);
                    float exit=currentAlong+span*Next(.64f,.78f);
                    Add(entry,currentCross);Add(entry,bendCross);
                    Add(exit,bendCross);Add(exit,currentCross);
                }
                Add(target.x,target.y);
                currentAlong=target.x;currentCross=target.y;
            }
            logical=AddDistrictMeanders(region,logical,horizontal,
                lowerCross,upperCross,random);
            // Region Z increases south-to-north, so reverse vertical point
            // order to produce the requested north-to-south flow.
            if(!horizontal)logical.Reverse();
            foreach(var point in RiverPathGeometry.RoundOrthogonalCorners(
                        logical,1.8f))
                path.Points.Add(new DistrictRiverPoint(point.x,point.y));
        }

        private static List<Vector2> AddDistrictMeanders(RegionSaveData region,
            List<Vector2> logical, bool horizontal, float lowerCross,
            float upperCross, System.Random random)
        {
            if(logical.Count<2||region.Tiles==null||region.Tiles.Count==0)
                return logical;
            float Along(Vector2 point)=>horizontal?point.x:point.y;
            float Cross(Vector2 point)=>horizontal?point.y:point.x;
            float TileAlongMin(RegionCityTile tile)=>horizontal?tile.X:tile.Y;
            float TileAlongMax(RegionCityTile tile)=>horizontal?
                tile.X+tile.Width:tile.Y+tile.Height;
            float TileCrossMin(RegionCityTile tile)=>horizontal?tile.Y:tile.X;
            float TileCrossMax(RegionCityTile tile)=>horizontal?
                tile.Y+tile.Height:tile.X+tile.Width;
            bool Inside(RegionCityTile tile,Vector2 point)
            {
                const float inset=.0001f;
                var along=Along(point);var cross=Cross(point);
                return along>TileAlongMin(tile)+inset&&
                    along<TileAlongMax(tile)-inset&&
                    cross>TileCrossMin(tile)+inset&&
                    cross<TileCrossMax(tile)-inset;
            }

            var turns=new Dictionary<RegionCityTile,int>();
            foreach(var tile in region.Tiles)
            {
                if(tile==null)continue;
                var count=0;
                for(var index=1;index<logical.Count-1;index++)
                {
                    if(!Inside(tile,logical[index]))continue;
                    var incoming=logical[index]-logical[index-1];
                    var outgoing=logical[index+1]-logical[index];
                    if(incoming.sqrMagnitude>.000001f&&
                        outgoing.sqrMagnitude>.000001f&&
                        Mathf.Abs(Vector2.Dot(incoming.normalized,
                            outgoing.normalized))<.001f)count++;
                }
                turns[tile]=count;
            }

            // Pick only the longest forward run in each straight district.
            // One contained dogleg contributes four rounded corners while
            // leaving the route on its original centerline at the border.
            var selected=new Dictionary<RegionCityTile,
                (int Segment,float Start,float End,float Cross)>();
            for(var segment=0;segment<logical.Count-1;segment++)
            {
                var a=logical[segment];var b=logical[segment+1];
                var start=Along(a);var end=Along(b);var cross=Cross(a);
                if(end-start<.0001f||Mathf.Abs(Cross(b)-cross)>.0001f)
                    continue;
                foreach(var tile in region.Tiles)
                {
                    if(tile==null||turns[tile]>=2||
                        cross<TileCrossMin(tile)+.0001f||
                        cross>TileCrossMax(tile)-.0001f)continue;
                    var overlapStart=Mathf.Max(start,TileAlongMin(tile));
                    var overlapEnd=Mathf.Min(end,TileAlongMax(tile));
                    if(overlapEnd-overlapStart<.6f)continue;
                    if(!selected.TryGetValue(tile,out var existing)||
                        overlapEnd-overlapStart>existing.End-existing.Start)
                        selected[tile]=(segment,overlapStart,overlapEnd,cross);
                }
            }

            var bySegment=new Dictionary<int,List<(float Start,float End,
                float Cross,RegionCityTile Tile)>>();
            foreach(var pair in selected)
            {
                var value=pair.Value;
                if(!bySegment.TryGetValue(value.Segment,out var list))
                {
                    list=new List<(float,float,float,RegionCityTile)>();
                    bySegment[value.Segment]=list;
                }
                list.Add((value.Start,value.End,value.Cross,pair.Key));
            }
            var result=new List<Vector2>();
            void Add(float along,float cross)
            {
                var point=horizontal?new Vector2(along,cross):
                    new Vector2(cross,along);
                if(result.Count==0||Vector2.Distance(result[^1],point)>.000001f)
                    result.Add(point);
            }
            for(var segment=0;segment<logical.Count-1;segment++)
            {
                Add(Along(logical[segment]),Cross(logical[segment]));
                if(!bySegment.TryGetValue(segment,out var candidates))continue;
                candidates.Sort((a,b)=>a.Start.CompareTo(b.Start));
                foreach(var candidate in candidates)
                {
                    var span=candidate.End-candidate.Start;
                    var entry=candidate.Start+span*Mathf.Lerp(.18f,.32f,
                        (float)random.NextDouble());
                    var exit=candidate.Start+span*Mathf.Lerp(.58f,.76f,
                        (float)random.NextDouble());
                    var minimum=Mathf.Max(lowerCross,
                        TileCrossMin(candidate.Tile))+.08f;
                    var maximum=Mathf.Min(upperCross,
                        TileCrossMax(candidate.Tile))-.08f;
                    var positive=maximum-candidate.Cross;
                    var negative=candidate.Cross-minimum;
                    bool rise=positive>=.12f&&negative>=.12f?
                        random.NextDouble()>=.5:positive>=negative;
                    var room=rise?positive:negative;
                    if(room<.12f)continue;
                    var amplitude=Mathf.Min(.55f,
                        Mathf.Min(span*.2f,room*.6f));
                    amplitude*=Mathf.Lerp(.72f,1f,(float)random.NextDouble());
                    var bend=candidate.Cross+(rise?amplitude:-amplitude);
                    Add(entry,candidate.Cross);Add(entry,bend);
                    Add(exit,bend);Add(exit,candidate.Cross);
                }
            }
            Add(Along(logical[^1]),Cross(logical[^1]));
            return result;
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
