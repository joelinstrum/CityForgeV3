using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    public sealed class DistrictQuarryNavigation
    {
        readonly RegionCityTile district;readonly DistrictRoadDelivery roads;
        public DistrictQuarryNavigation(RegionCityTile d,DistrictStoneSite site,Func<Vector2,bool> water)
        {district=d;roads=new(d);}
        public void BeginQuery() { }
        public bool Segment(Vector2 a,Vector2 b)=>true; // Delivery movement follows the precomputed road route.
        public List<Vector2> Route(Vector2 from,Vector2 to)=>roads.Route(from,to);
        public IEnumerable<QuarryDeliveryDestination> Destinations(Vector2 from)
        {
            var result=new List<QuarryDeliveryDestination>();
            foreach(var building in district.Brickworks??new())
            {
                if(!building.Enabled)continue;
                var center=DistrictBrickworks.Point(district,building);
                bool turned=Mathf.Abs(Mathf.Sin(building.Yaw*Mathf.Deg2Rad))>.5f;
                float x=turned?DistrictBrickworks.HalfDepth:DistrictBrickworks.HalfWidth;
                float z=turned?DistrictBrickworks.HalfWidth:DistrictBrickworks.HalfDepth;
                var route=roads.Route(from,new Rect(center.x-x,center.y-z,x*2,z*2));
                if(route!=null)result.Add(new(){Id=building.Id,Point=route.Last(),Route=route});
            }
            return result.OrderBy(r=>r.Route.Count);
        }
    }
}
