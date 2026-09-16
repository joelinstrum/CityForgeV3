using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // Builds edits without changing the live region. The caller can save or roll
    // back the whole road, including junction repairs, as a single transaction.
    public static class NationalPikePlacement
    {
        public static string SurfaceForEra(string era) => era switch
        {
            "discovery" => "early-concrete",
            "modern" => "blacktop",
            _ => "dirt"
        };

        public sealed class WaterMask
        {
            readonly List<(Vector2 a, Vector2 b, float radius)> segments = new();
            public WaterMask(RegionSaveData region, float minimumRadiusMeters = 0)
            {
                foreach (var tile in region.Tiles)
                    foreach (var river in tile.Rivers ?? new())
                    {
                        if (river?.Points == null) continue;
                        Vector2 Point(DistrictRiverPoint p) => new Vector2(
                            tile.X + p.X * tile.Width, tile.Y + p.Z * tile.Height) * 640;
                        float radius = Mathf.Max(minimumRadiusMeters, river.WidthMeters *
                            (river.Depth == DistrictRiverDepth.Deep ? .54f : .64f));
                        for (int i = 1; i < river.Points.Count; i++)
                            segments.Add((Point(river.Points[i-1]), Point(river.Points[i]), radius));
                    }
            }
            public bool Contains(Vector2 metres, float clearance = 0)
            {
                foreach (var s in segments)
                {
                    float radius = s.radius + clearance;
                    if (metres.x < Mathf.Min(s.a.x,s.b.x)-radius || metres.x > Mathf.Max(s.a.x,s.b.x)+radius ||
                        metres.y < Mathf.Min(s.a.y,s.b.y)-radius || metres.y > Mathf.Max(s.a.y,s.b.y)+radius) continue;
                    var v = s.b-s.a;
                    float t = v.sqrMagnitude < .0001f ? 0 : Mathf.Clamp01(Vector2.Dot(metres-s.a,v)/v.sqrMagnitude);
                    if ((metres-s.a-v*t).sqrMagnitude <= radius*radius) return true;
                }
                return false;
            }
        }

        // Grid traversal emits cardinal steps, so a diagonal pencil stroke never
        // creates disconnected tiles. Ties use X then Y consistently.
        public static IEnumerable<Vector2Int> Cells(IReadOnlyList<Vector2> points)
        {
            for (int i=1;i<points.Count;i++)
            {
                var a=points[i-1]*64; var b=points[i]*64;
                var c=Vector2Int.FloorToInt(a); var end=Vector2Int.FloorToInt(b);
                var d=b-a; int sx=d.x>=0?1:-1, sy=d.y>=0?1:-1;
                float tx=Mathf.Abs(d.x)<.000001f?float.PositiveInfinity:((sx>0?c.x+1:c.x)-a.x)/d.x;
                float ty=Mathf.Abs(d.y)<.000001f?float.PositiveInfinity:((sy>0?c.y+1:c.y)-a.y)/d.y;
                float dx=Mathf.Abs(d.x)<.000001f?float.PositiveInfinity:1/Mathf.Abs(d.x);
                float dy=Mathf.Abs(d.y)<.000001f?float.PositiveInfinity:1/Mathf.Abs(d.y);
                yield return c;
                int steps=Mathf.Abs(end.x-c.x)+Mathf.Abs(end.y-c.y);
                for (int j=0;j<steps;j++)
                {
                    if (tx<=ty) { c.x+=sx;tx+=dx; }
                    else { c.y+=sy;ty+=dy; }
                    yield return c;
                }
            }
        }

        static readonly Vector2Int[] Offsets = { Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left };
        static readonly RoadPiecePort[] Ports = { RoadPiecePort.North,RoadPiecePort.East,RoadPiecePort.South,RoadPiecePort.West };

        public static Dictionary<RegionCityTile,List<PlacedRoadPiece>> Build(RegionSaveData region, RegionTransportRoute route)
        {
            var result=new Dictionary<RegionCityTile,List<PlacedRoadPiece>>();
            var global=new Dictionary<Vector2Int,PlacedRoadPiece>();
            var owners=new Dictionary<Vector2Int,RegionCityTile>();
            foreach (var tile in region.Tiles)
                foreach (var road in tile.Roads ?? new())
                {
                    if (road==null) continue;
                    var c=new Vector2Int(tile.X*64+road.GridX,tile.Y*64+road.GridZ);
                    global[c]=road;owners[c]=tile;
                }
            var water=new WaterMask(region);
            var affected=new HashSet<Vector2Int>();
            var visited=new HashSet<Vector2Int>();
            route.SurfaceId=SurfaceForEra(region.EraId);
            foreach (var c in Cells(route.Points))
            {
                if (!visited.Add(c) || global.ContainsKey(c) || water.Contains((Vector2)c*10+Vector2.one*5,7.1f)) continue;
                RegionCityTile owner=null;
                foreach (var tile in region.Tiles)
                    if(c.x>=tile.X*64 && c.x<(tile.X+tile.Width)*64 && c.y>=tile.Y*64 && c.y<(tile.Y+tile.Height)*64)
                    {owner=tile;break;}
                if(owner==null)continue;
                if(!result.TryGetValue(owner,out var list)) result[owner]=list=new List<PlacedRoadPiece>(owner.Roads ?? new());
                var road=new PlacedRoadPiece
                {
                    Id="pike-"+Guid.NewGuid().ToString("N"),
                    NationalPikeId=route.Id,
                    GridX=c.x-owner.X*64, GridZ=c.y-owner.Y*64,
                    PackageId=route.SurfaceId=="dirt"?RoadPiecePackageCatalog.NationalPikeDirtId:RoadPiecePackageCatalog.TwoLaneSidewalkId,
                    RoadMaterialId=route.SurfaceId, SidewalkMaterialId=route.SurfaceId,
                    MarkingStyle=RoadMarkingStyle.NoLines,
                    LaneMarkingStyle=RoadLaneMarkingStyle.NoLines,
                    CenterMarkingStyle=RoadCenterMarkingStyle.NoLines
                };
                list.Add(road);global[c]=road;owners[c]=owner;
                affected.Add(c);foreach(var offset in Offsets)affected.Add(c+offset);
            }
            foreach(var c in affected)
            {
                if(!global.TryGetValue(c,out var old))continue;
                var ports=new List<RoadPiecePort>();
                for(int i=0;i<4;i++)if(global.ContainsKey(c+Offsets[i]))ports.Add(Ports[i]);
                if(ports.Count==0)ports.Add(RoadPiecePort.North);
                if(!RoadPlacementModel.TryFindTopologyForPorts(RoadPiecePackageCatalog.Resolve(old.PackageId),ports,out var topology,out var turns))continue;
                if(old.Topology==topology && old.RotationQuarterTurns==turns)continue;
                var owner=owners[c];
                if(!result.TryGetValue(owner,out var list))result[owner]=list=new List<PlacedRoadPiece>(owner.Roads ?? new());
                var copy=JsonUtility.FromJson<PlacedRoadPiece>(JsonUtility.ToJson(old));
                copy.Topology=topology;copy.RotationQuarterTurns=turns;list[list.IndexOf(old)]=copy;
            }
            route.HasDistrictRoads=true;
            return result;
        }

        // Cache this presentation geometry when the map/route changes, never per frame.
        public static List<List<Vector2>> DryMapSections(RegionSaveData region, RegionTransportRoute route, float pixelsPerUnit)
        {
            var water=new WaterMask(region,3.5f/pixelsPerUnit*640);
            var sections=new List<List<Vector2>>(); List<Vector2> current=null;
            for(int i=1;i<route.Points.Count;i++)
            {
                var a=route.Points[i-1];var b=route.Points[i];
                int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)*128));
                for(int j=0;j<=steps;j++)
                {
                    var point=Vector2.Lerp(a,b,(float)j/steps);
                    if(water.Contains(point*640,10)) {current=null;continue;}
                    if(current==null){current=new();sections.Add(current);}
                    current.Add(point);
                }
            }
            return sections;
        }
    }
}
