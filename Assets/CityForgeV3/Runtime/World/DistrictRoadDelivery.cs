using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    // Shared contract for any cargo/building: connected road cells, nearby service tile.
    // No vegetation, articulated collision search, or authored receiving-bay requirement.
    public sealed class DistrictRoadDelivery
    {
        readonly HashSet<Vector2Int> cells;
        readonly Dictionary<Vector2Int, int> diagonalConnections = new();
        readonly float width, depth;
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
        }
        Vector2 Center(Vector2Int c)=>new((c.x+.5f)*10-width/2,(c.y+.5f)*10-depth/2);
        public List<Vector2> Route(Vector2 from,Rect building,float serviceDistance=30)
        {
            if(cells.Count==0)return null;
            var start=cells.OrderBy(c=>(Center(c)-from).sqrMagnitude).ThenBy(c=>c.x).ThenBy(c=>c.y).First();
            if(Vector2.Distance(Center(start),from)>serviceDistance)return null;
            var previous=new Dictionary<Vector2Int,Vector2Int>{{start,start}};
            var queue=new Queue<Vector2Int>();queue.Enqueue(start);
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();
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
            var result=new List<Vector2>();for(var c=end;;c=previous[c]){result.Add(Center(c));if(c==start)break;}
            result.Reverse();
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
            wagon.SetRoute(route.Select(TimberWorld).ToList());return true;
        }
        // Road connectivity is resolved once for the trip; follow its centerline directly.
        static bool RoadDeliveryClear(Vector3 from,Vector3 to)=>true;
    }
}
