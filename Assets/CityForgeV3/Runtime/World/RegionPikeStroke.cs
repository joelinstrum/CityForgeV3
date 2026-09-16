using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // Region-space draft only. Nothing is committed until the name is accepted.
    public sealed class RegionPikeStroke
    {
        public readonly List<Vector2> Points = new();
        readonly Vector2 size;
        public RegionPikeStroke(float width,float height){size=new Vector2(width,height);}
        public void Add(Vector2 point,bool final=false)
        {
            if(float.IsNaN(point.x)||float.IsNaN(point.y)||float.IsInfinity(point.x)||float.IsInfinity(point.y))return;
            point=new Vector2(Mathf.Clamp(point.x,0,size.x),Mathf.Clamp(point.y,0,size.y));
            if(Points.Count>0&&Vector2.Distance(Points[Points.Count-1],point)<(final ? .001f : .025f))return;
            if(Points.Count>=4096)for(int i=Points.Count-2;i>0;i-=2)Points.RemoveAt(i);
            Points.Add(point);
        }
        public float Length
        {
            get {float length=0;for(int i=1;i<Points.Count;i++)length+=Vector2.Distance(Points[i-1],Points[i]);return length;}
        }
        public bool IsValid => Points.Count>=2&&Length>=.15f;
        public RegionTransportRoute CreateRoute(string name)
        {
            if(!IsValid||string.IsNullOrWhiteSpace(name))return null;
            return new RegionTransportRoute{Id=System.Guid.NewGuid().ToString("N"),Name=name.Trim(),Kind=RegionTransportKind.Highway,Points=new List<Vector2>(Points)};
        }
    }
}
