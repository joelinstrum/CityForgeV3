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
        public string TributaryOfRiverId;
        public bool HasGeneratedDirection;
        public DistrictRiverDirection GeneratedDirection;
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
            var sizes=new List<GeneratedRegionRiverSize>();
            if(major>0)sizes.Add(GeneratedRegionRiverSize.Major);
            for(var index=0;index<medium;index++)sizes.Add(GeneratedRegionRiverSize.Medium);
            for(var index=0;index<small;index++)sizes.Add(GeneratedRegionRiverSize.Small);
            for(var index=0;index<streams;index++)sizes.Add(GeneratedRegionRiverSize.Stream);
            int total=sizes.Count;
            if(total==0)return result;
            var orientations=new List<bool>();
            for(var index=0;index<total;index++)
            {
                if(index==0)orientations.Add(random.NextDouble()<.5);
                else if(index==1)orientations.Add(!orientations[0]);
                else orientations.Add(random.NextDouble()<.5);
            }
            var horizontalPositions=RandomCrossPositions(
                orientations.FindAll(value=>value).Count,region.Height,random);
            var verticalPositions=RandomCrossPositions(
                orientations.FindAll(value=>!value).Count,region.Width,random);
            int horizontalIndex=0,verticalIndex=0;
            for(int n=0;n<total;n++)
            {
                bool horizontal=orientations[n];
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
                var positions=horizontal?horizontalPositions:verticalPositions;
                var positionIndex=horizontal?horizontalIndex++:verticalIndex++;
                CrossEnvelope(positions,positionIndex,
                    horizontal?region.Height:region.Width,
                    out var lowerCross,out var upperCross);
                BuildNaturalRoute(region,path,horizontal,
                    positions[positionIndex],lowerCross,upperCross,random);
                TruncateAtFirstConfluence(path,result);
                var useInteriorHeadwater=
                    size==GeneratedRegionRiverSize.Small&&random.NextDouble()<.48||
                    size==GeneratedRegionRiverSize.Stream&&random.NextDouble()<.72;
                if(useInteriorHeadwater)
                    TrimInteriorHeadwater(path,random);
                result.Add(path);
            }
            return result;
        }

        private static List<float> RandomCrossPositions(int count,float extent,
            System.Random random)
        {
            var result=new List<float>();
            if(count<=0)return result;
            var margin=Mathf.Min(.7f,extent*.06f);
            var minimum=Mathf.Clamp(extent/(count*2.25f),.5f,2.4f);
            for(var index=0;index<count;index++)
            {
                var accepted=false;
                for(var attempt=0;attempt<80;attempt++)
                {
                    var candidate=Mathf.Lerp(margin,extent-margin,
                        (float)random.NextDouble());
                    if(result.TrueForAll(value=>Mathf.Abs(value-candidate)>=minimum))
                    {result.Add(candidate);accepted=true;break;}
                }
                if(accepted)continue;
                var best=margin;var bestDistance=-1f;
                for(var attempt=0;attempt<80;attempt++)
                {
                    var candidate=Mathf.Lerp(margin,extent-margin,
                        (float)random.NextDouble());
                    var distance=float.PositiveInfinity;
                    foreach(var value in result)
                        distance=Mathf.Min(distance,Mathf.Abs(value-candidate));
                    if(distance>bestDistance){best=candidate;bestDistance=distance;}
                }
                result.Add(best);
            }
            return result;
        }

        private static void CrossEnvelope(List<float> positions,int index,
            float extent,out float lower,out float upper)
        {
            var center=positions[index];
            float nearestLower=0,nearestUpper=extent;
            foreach(var other in positions)
            {
                if(other<center&&other>nearestLower)nearestLower=other;
                if(other>center&&other<nearestUpper)nearestUpper=other;
            }
            lower=nearestLower==0?.15f:(nearestLower+center)*.5f+.06f;
            upper=nearestUpper==extent?extent-.15f:(nearestUpper+center)*.5f-.06f;
            if(upper-lower<.5f){lower=center-.25f;upper=center+.25f;}
        }

        private static void BuildNaturalRoute(RegionSaveData region,
            RegionRiverPath path,bool horizontal,float baseCross,
            float lowerCross,float upperCross,System.Random random)
        {
            path.HasGeneratedDirection=true;
            path.GeneratedDirection=horizontal?DistrictRiverDirection.WestToEast:
                DistrictRiverDirection.NorthToSouth;
            float alongCells=horizontal?region.Width:region.Height;
            var logical=new List<Vector2>();
            void Add(float along,float cross)
            {
                var point=new Vector2(along,cross);
                if(logical.Count==0||Vector2.Distance(logical[^1],point)>.000001f)
                    logical.Add(point);
            }
            float Next(float a,float b)=>Mathf.Lerp(a,b,
                (float)random.NextDouble());
            (float Minimum,float Maximum,float Shift,float SampleSpacing,
                float Undulation) Shape()=>
                path.GeneratedSize switch
            {
                GeneratedRegionRiverSize.Major=>(1.45f,3.2f,2.3f,.42f,.24f),
                GeneratedRegionRiverSize.Medium=>(1.15f,2.65f,1.7f,.34f,.19f),
                GeneratedRegionRiverSize.Small=>(.9f,2.15f,1.2f,.28f,.14f),
                _=>(.65f,1.65f,.8f,.22f,.1f)
            };
            var shape=Shape();
            var inset=Mathf.Min(.18f,(upperCross-lowerCross)*.12f);
            lowerCross+=inset;upperCross-=inset;
            var startCross=Mathf.Clamp(baseCross+Next(-.45f,.45f),
                lowerCross,upperCross);
            var exitCross=Next(lowerCross,upperCross);
            float currentAlong=0,currentCross=startCross;
            int driftDirection=random.NextDouble()<.5?-1:1;
            int driftRemaining=random.Next(1,4);
            Add(currentAlong,currentCross);
            var guard=0;
            while(alongCells-currentAlong>shape.Maximum&&guard++<64)
            {
                var turn=currentAlong+Next(shape.Minimum,shape.Maximum);
                var room=driftDirection>0?upperCross-currentCross:
                    currentCross-lowerCross;
                if(room<.28f)
                {
                    driftDirection=-driftDirection;
                    driftRemaining=random.Next(1,4);
                    room=driftDirection>0?upperCross-currentCross:
                        currentCross-lowerCross;
                }
                var reach=turn-currentAlong;
                var shift=Mathf.Min(shape.Shift,
                    Mathf.Min(room*.78f,reach*.62f))*Next(.42f,1f);
                var nextCross=currentCross+driftDirection*shift;
                var progress=turn/alongCells;
                nextCross=Mathf.Lerp(nextCross,exitCross,.06f+progress*.12f);
                nextCross=Mathf.Clamp(nextCross,lowerCross,upperCross);
                Add(turn,nextCross);
                currentAlong=turn;currentCross=nextCross;
                driftRemaining--;
                if(driftRemaining<=0)
                {
                    driftDirection=-driftDirection;
                    driftRemaining=random.Next(1,4);
                }
            }
            Add(alongCells,exitCross);

            // The anchors retain a clear west-east or north-south structure,
            // but every reach wanders gently between them. This keeps bridge
            // and waterfront planning legible without drawing ruler-straight
            // stair steps across the region.
            var natural=new List<Vector2>();
            natural.Add(logical[0]);
            for(var index=1;index<logical.Count;index++)
            {
                var prior=logical[index-1];
                var next=logical[index];
                var length=next.x-prior.x;
                var samples=Mathf.Max(2,
                    Mathf.CeilToInt(length/shape.SampleSpacing));
                var available=Mathf.Max(0,
                    Mathf.Min(prior.y-lowerCross,upperCross-prior.y));
                available=Mathf.Min(available,Mathf.Max(0,
                    Mathf.Min(next.y-lowerCross,upperCross-next.y)));
                var amplitude=Mathf.Min(shape.Undulation,available*.72f)*
                    Next(.38f,1f)*(random.NextDouble()<.5?-1:1);
                for(var sample=1;sample<=samples;sample++)
                {
                    var t=sample/(float)samples;
                    var smooth=t*t*(3f-2f*t);
                    var cross=Mathf.Lerp(prior.y,next.y,smooth)+
                        amplitude*Mathf.Pow(Mathf.Sin(Mathf.PI*t),2f);
                    cross=Mathf.Clamp(cross,lowerCross,upperCross);
                    AddNatural(natural,new Vector2(
                        Mathf.Lerp(prior.x,next.x,t),cross));
                }
            }
            if(!horizontal)natural.Reverse();
            foreach(var point in natural)
            {
                var mapped=horizontal?point:new Vector2(point.y,point.x);
                path.Points.Add(new DistrictRiverPoint(mapped.x,mapped.y));
            }

            static void AddNatural(List<Vector2> points,Vector2 point)
            {
                if(points.Count==0||Vector2.Distance(points[^1],point)>.000001f)
                    points.Add(point);
            }
        }

        private static void TrimInteriorHeadwater(RegionRiverPath path,
            System.Random random)
        {
            if(path.Points.Count<8)return;
            var fraction=Mathf.Lerp(.12f,.36f,(float)random.NextDouble());
            var remove=Mathf.Clamp(Mathf.RoundToInt(
                (path.Points.Count-1)*fraction),1,path.Points.Count-5);
            path.Points.RemoveRange(0,remove);
        }

        private static void TruncateAtFirstConfluence(RegionRiverPath candidate,
            List<RegionRiverPath> existingPaths)
        {
            if(candidate.Points.Count<2||existingPaths.Count==0)return;
            var bestDistance=float.PositiveInfinity;
            var bestSegment=-1;
            var bestPoint=Vector2.zero;
            string bestParent=null;
            var travelled=0f;
            const float minimumTravel=.05f;
            for(var index=1;index<candidate.Points.Count;index++)
            {
                var a=new Vector2(candidate.Points[index-1].X,
                    candidate.Points[index-1].Z);
                var b=new Vector2(candidate.Points[index].X,
                    candidate.Points[index].Z);
                var segmentLength=Vector2.Distance(a,b);
                foreach(var parent in existingPaths)
                for(var parentIndex=1;parentIndex<parent.Points.Count;parentIndex++)
                {
                    var c=new Vector2(parent.Points[parentIndex-1].X,
                        parent.Points[parentIndex-1].Z);
                    var d=new Vector2(parent.Points[parentIndex].X,
                        parent.Points[parentIndex].Z);
                    if(!TrySegmentIntersection(a,b,c,d,out var point,out var t))
                        continue;
                    var distance=travelled+segmentLength*t;
                    if(distance<minimumTravel||distance>=bestDistance)continue;
                    bestDistance=distance;bestSegment=index;
                    bestPoint=point;bestParent=parent.Id;
                }
                travelled+=segmentLength;
            }
            if(bestSegment<1)return;
            candidate.Points.RemoveRange(bestSegment,
                candidate.Points.Count-bestSegment);
            var last=candidate.Points[^1];
            if(Vector2.Distance(new Vector2(last.X,last.Z),bestPoint)>.00001f)
                candidate.Points.Add(new DistrictRiverPoint(bestPoint.x,bestPoint.y));
            candidate.TributaryOfRiverId=bestParent;
        }

        private static bool TrySegmentIntersection(Vector2 a,Vector2 b,
            Vector2 c,Vector2 d,out Vector2 point,out float alongFirst)
        {
            float Cross(Vector2 one,Vector2 two)=>one.x*two.y-one.y*two.x;
            var first=b-a;var second=d-c;
            var denominator=Cross(first,second);
            if(Mathf.Abs(denominator)<.000001f)
            {point=default;alongFirst=0;return false;}
            var offset=c-a;
            var t=Cross(offset,second)/denominator;
            var u=Cross(offset,first)/denominator;
            if(t<-.0001f||t>1.0001f||u<-.0001f||u>1.0001f)
            {point=default;alongFirst=0;return false;}
            alongFirst=Mathf.Clamp01(t);point=a+first*alongFirst;return true;
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
            if(path.HasGeneratedDirection)return path.GeneratedDirection;
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
