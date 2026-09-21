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
            float TileAlongMin(RegionCityTile tile)=>horizontal?tile.X:tile.Y;
            float TileAlongMax(RegionCityTile tile)=>horizontal?
                tile.X+tile.Width:tile.Y+tile.Height;
            float TileCrossMin(RegionCityTile tile)=>horizontal?tile.Y:tile.X;
            float TileCrossMax(RegionCityTile tile)=>horizontal?
                tile.Y+tile.Height:tile.X+tile.Width;
            RegionCityTile TileAt(float along,float cross)
            {
                RegionCityTile closestTile=null;var closest=float.PositiveInfinity;
                foreach(var tile in region.Tiles??new List<RegionCityTile>())
                {
                    if(tile==null||along<TileAlongMin(tile)-.0001f||
                        along>TileAlongMax(tile)+.0001f)continue;
                    if(cross>=TileCrossMin(tile)-.0001f&&
                        cross<=TileCrossMax(tile)+.0001f)return tile;
                    var distance=Mathf.Min(Mathf.Abs(cross-TileCrossMin(tile)),
                        Mathf.Abs(cross-TileCrossMax(tile)));
                    if(distance<closest){closest=distance;closestTile=tile;}
                }
                return closestTile;
            }
            int NextDriftRun()=>path.GeneratedSize switch
            {
                GeneratedRegionRiverSize.Major=>random.Next(3,6),
                GeneratedRegionRiverSize.Medium=>random.Next(3,5),
                GeneratedRegionRiverSize.Small=>random.Next(2,5),
                _=>random.Next(1,4)
            };
            float MaximumShift()=>path.GeneratedSize switch
            {
                GeneratedRegionRiverSize.Major=>.9f,
                GeneratedRegionRiverSize.Medium=>.72f,
                GeneratedRegionRiverSize.Small=>.56f,
                _=>.42f
            };
            float ShiftFraction()=>path.GeneratedSize switch
            {
                GeneratedRegionRiverSize.Major=>.24f,
                GeneratedRegionRiverSize.Medium=>.2f,
                GeneratedRegionRiverSize.Small=>.17f,
                _=>.14f
            };
            float currentAlong=0,currentCross=baseCross;
            int driftDirection=random.NextDouble()<.5?-1:1;
            int driftRemaining=NextDriftRun();
            Add(currentAlong,currentCross);
            var guard=0;
            while(currentAlong<alongCells-.0001f&&
                guard++<(region.Tiles?.Count??0)+8)
            {
                var probe=Mathf.Min(alongCells-.0001f,currentAlong+.001f);
                var tile=TileAt(probe,currentCross);
                if(tile==null)
                {
                    Add(alongCells,currentCross);break;
                }
                var exit=Mathf.Min(alongCells,TileAlongMax(tile));
                var span=exit-currentAlong;
                if(span<.0001f)
                {
                    currentAlong=Mathf.Min(alongCells,currentAlong+.001f);
                    continue;
                }
                var crossMinimum=Mathf.Max(lowerCross,
                    TileCrossMin(tile))+.1f;
                var crossMaximum=Mathf.Min(upperCross,
                    TileCrossMax(tile))-.1f;
                if(crossMaximum-crossMinimum<.24f)
                {
                    Add(exit,currentCross);currentAlong=exit;continue;
                }
                var safeCross=Mathf.Clamp(currentCross,crossMinimum,crossMaximum);
                if(Mathf.Abs(safeCross-currentCross)>.0001f)
                {
                    Add(currentAlong,currentCross);Add(currentAlong,safeCross);
                    currentCross=safeCross;
                }
                var room=driftDirection>0?crossMaximum-currentCross:
                    currentCross-crossMinimum;
                if(room<.16f)
                {
                    driftDirection=-driftDirection;
                    driftRemaining=NextDriftRun();
                    room=driftDirection>0?crossMaximum-currentCross:
                        currentCross-crossMinimum;
                }
                var tileCrossSpan=TileCrossMax(tile)-TileCrossMin(tile);
                var shift=Mathf.Min(MaximumShift(),
                    Mathf.Min(tileCrossSpan*ShiftFraction(),room*.72f));
                shift*=Next(.72f,1f);
                var nextCross=currentCross+driftDirection*shift;
                if(tile.Lots==null||tile.Lots.Count==0)
                {
                    var center=Mathf.Clamp((TileCrossMin(tile)+
                        TileCrossMax(tile))*.5f,crossMinimum,crossMaximum);
                    nextCross=Mathf.Lerp(nextCross,center,.18f);
                }
                nextCross=Mathf.Clamp(nextCross,crossMinimum,crossMaximum);
                if(Mathf.Abs(nextCross-currentCross)<.1f)
                {
                    var alternate=currentCross-driftDirection*
                        Mathf.Min(MaximumShift()*.55f,
                            driftDirection>0?currentCross-crossMinimum:
                            crossMaximum-currentCross);
                    nextCross=Mathf.Clamp(alternate,crossMinimum,crossMaximum);
                    driftDirection=-driftDirection;
                    driftRemaining=NextDriftRun();
                }
                var turn=currentAlong+span*Next(.24f,.72f);
                Add(turn,currentCross);Add(turn,nextCross);Add(exit,nextCross);
                currentAlong=exit;currentCross=nextCross;
                driftRemaining--;
                if(driftRemaining<=0)
                {
                    driftDirection=-driftDirection;
                    driftRemaining=NextDriftRun();
                }
            }
            if(currentAlong<alongCells-.0001f)Add(alongCells,currentCross);
            // Region Z increases south-to-north, so reverse vertical point
            // order to produce the requested north-to-south flow.
            if(!horizontal)logical.Reverse();
            foreach(var point in RiverPathGeometry.RoundOrthogonalCorners(
                        logical,1.8f))
                path.Points.Add(new DistrictRiverPoint(point.x,point.y));
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
