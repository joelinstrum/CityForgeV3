using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    // Shared contract for any cargo/building: connected road cells, nearby service tile.
    // No vegetation, articulated collision search, or authored receiving-bay requirement.
    public sealed class DistrictRoadDelivery
    {
        sealed class CachedNetwork { public int Key; public DistrictRoadDelivery Network; }
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RegionCityTile,CachedNetwork> Cache=new();
        public static DistrictRoadDelivery For(RegionCityTile district)
        {
            var cache=Cache.GetValue(district,_=>new CachedNetwork());var key=DistrictRoadPlacementModel.NetworkKey(district);
            if(cache.Network==null || cache.Key!=key){cache.Network=new DistrictRoadDelivery(district);cache.Key=key;}
            return cache.Network;
        }
        readonly HashSet<Vector2Int> cells;
        readonly Dictionary<Vector2Int, int> diagonalConnections = new();
        readonly float width, depth;
        readonly Dictionary<Vector2Int,List<Vector2Int>> bridgeLinks=new();
        readonly DistrictSpatialIndex<PlacedDistrictBridge> bridges=new(32);
        bool BridgeContains(PlacedDistrictBridge bridge,Vector2 p)
        {
            var a=Center(bridge.Start);var z=Center(bridge.End);var delta=z-a;
            float t=Vector2.Dot(p-a,delta)/Mathf.Max(.001f,delta.sqrMagnitude);
            return t>=0 && t<=1 && Vector2.Distance(p,a+delta*t)<=DistrictBridgePlanner.TravelHalfWidth;
        }
        static readonly Vector2Int[] Directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left,
            new(1,1),new(1,-1),new(-1,-1),new(-1,1)};
        public DistrictRoadDelivery(RegionCityTile district)
        {
            width=DistrictScale.SizeMeters(district.Width);depth=DistrictScale.SizeMeters(district.Height);
            cells=new((district.Roads??new()).Select(r=>new Vector2Int(r.GridX,r.GridZ)));
            foreach (var road in district.Roads ?? new List<PlacedRoadPiece>())
                if (road != null && road.DistrictDiagonalConnections != 0)
                    diagonalConnections[new Vector2Int(road.GridX, road.GridZ)] =
                        road.DistrictDiagonalConnections;
            foreach(var b in district.Bridges??new List<PlacedDistrictBridge>())
            {
                if(b==null||!cells.Contains(b.Start)||!cells.Contains(b.End))continue;
                void Link(Vector2Int a,Vector2Int z){if(!bridgeLinks.TryGetValue(a,out var links))bridgeLinks[a]=links=new();links.Add(z);}
                Link(b.Start,b.End);Link(b.End,b.Start);
                bridges.Add(DistrictBridgePlanner.Bounds(district,b),b);
            }
        }
        Vector2 Center(Vector2Int c)=>new((c.x+.5f)*10-width/2,(c.y+.5f)*10-depth/2);
        public List<Vector2> Route(Vector2 from,Rect building,float serviceDistance=30)
        {
            if(cells.Count==0)return null;
            PlacedDistrictBridge onBridge=null;
            foreach(var b in bridges.Query(from))if(BridgeContains(b,from)){onBridge=b;break;}
            var start=Vector2Int.zero;bool found=false;float nearest=serviceDistance*serviceDistance;
            var origin=new Vector2Int(Mathf.FloorToInt((from.x+width/2)/10),Mathf.FloorToInt((from.y+depth/2)/10));
            int radius=Mathf.Clamp(Mathf.CeilToInt(serviceDistance/10)+1,1,32);
            for(int x=origin.x-radius;x<=origin.x+radius;x++)for(int y=origin.y-radius;y<=origin.y+radius;y++)
            {
                var c=new Vector2Int(x,y);if(!cells.Contains(c))continue;float distance=(Center(c)-from).sqrMagnitude;
                if(distance>nearest || (found && Mathf.Approximately(distance,nearest)))continue;
                nearest=distance;start=c;found=true;
            }
            if(!found && onBridge==null)return null;
            var previous=new Dictionary<Vector2Int,Vector2Int>();var queue=new Queue<Vector2Int>();
            if(onBridge==null){previous[start]=start;queue.Enqueue(start);}
            else{previous[onBridge.Start]=onBridge.Start;previous[onBridge.End]=onBridge.End;queue.Enqueue(onBridge.Start);queue.Enqueue(onBridge.End);}
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();
                if(bridgeLinks.TryGetValue(cell,out var links))foreach(var next in links)
                    if(!previous.ContainsKey(next)){previous[next]=cell;queue.Enqueue(next);}
                foreach(var direction in Directions)
                {
                    var next=cell+direction;
                    if(!cells.Contains(next)||previous.ContainsKey(next))continue;
                    if(direction.x!=0 && direction.y!=0)
                    {
                        var port=DistrictRoadPlacementModel.DiagonalPort(direction.x,direction.y);
                        var opposite=DistrictRoadPlacementModel.Opposite(port);
                        if(!diagonalConnections.TryGetValue(cell,out var a) ||
                           !diagonalConnections.TryGetValue(next,out var b) ||
                           (a & (1 << (int)port)) == 0 ||
                           (b & (1 << (int)opposite)) == 0)continue;
                    }
                    previous[next]=cell;queue.Enqueue(next);
                }
            }
            float Gap(Vector2Int c){var p=Center(c);return (p-new Vector2(Mathf.Clamp(p.x,building.xMin,building.xMax),Mathf.Clamp(p.y,building.yMin,building.yMax))).sqrMagnitude;}
            var end=previous.Keys.OrderBy(Gap).ThenBy(c=>c.x).ThenBy(c=>c.y).First();
            if(Gap(end)>serviceDistance*serviceDistance)return null;
            var result=new List<Vector2>();for(var c=end;;c=previous[c]){result.Add(Center(c));if(previous[c]==c)break;}
            result.Reverse();
            if(onBridge!=null && Vector2.Distance(from,result[0])>.01f)result.Insert(0,from);
            if(result.Count>1){var edge=result[1]-result[0];float t=Vector2.Dot(from-result[0],edge)/edge.sqrMagnitude;
                if(t>0&&t<=1&&Vector2.Distance(from,result[0]+edge*t)<=5)result.RemoveAt(0);}
            return result;
        }
        public List<Vector2> Route(Vector2 from,Vector2 destination)=>Route(from,new Rect(destination,Vector2.zero));
    }
    public sealed partial class DistrictWorldController
    {
        bool SetRoadDeliveryRoute(HorseCarriageController wagon,List<Vector2> route)
        {
            if(route==null||route.Count==0)return false;
            var planned=wagon.PlanRouteWithTurnaround(
                route.Select(TimberWorld).ToList(),RoadDeliveryClear);
            if(planned==null||planned.Count==0)return false;
            wagon.SetRoute(planned);return true;
        }
        // Road connectivity is resolved once for the trip; follow its centerline directly.
        static bool RoadDeliveryClear(Vector3 from,Vector3 to)=>true;
    }
}
