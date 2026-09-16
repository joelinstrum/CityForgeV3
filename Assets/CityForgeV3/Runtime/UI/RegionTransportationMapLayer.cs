using System.Collections.Generic;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed class RegionTransportationMapLayer : VisualElement
    {
        readonly RegionSaveData region;
        readonly float unit;
        readonly Dictionary<RegionTransportRoute,List<List<Vector2>>> drySections = new();
        public RegionTransportationMapLayer(RegionSaveData region,float unit)
        {
            this.region=region;this.unit=unit;
            RefreshRouteLabels();
            name="region-transportation-map-layer"; pickingMode=PickingMode.Ignore;
            style.position=Position.Absolute;style.left=0;style.top=0;
            style.width=region.Width*unit;style.height=region.Height*unit;
            generateVisualContent += context =>
            {
                var p=context.painter2D;p.lineCap=LineCap.Round;p.lineJoin=LineJoin.Round;
                // Existing local roads remain distinguishable from regional highways.
                p.strokeColor=new Color(.89f,.84f,.68f,.9f);p.lineWidth=1.25f;
                foreach(var tile in region.Tiles)
                {
                    var cells=new HashSet<Vector2Int>();
                    foreach(var road in tile.Roads??new())if(road!=null && string.IsNullOrEmpty(road.NationalPikeId))cells.Add(new Vector2Int(road.GridX,road.GridZ));
                    float scale=unit/DistrictScale.SizeMeters(1);
                    Vector2 Point(Vector2Int cell)=>new Vector2(tile.X*unit+(cell.x+.5f)*10*scale,tile.Y*unit+(cell.y+.5f)*10*scale);
                    foreach(var c in cells)
                    {
                        bool connected=false;
                        foreach(var delta in new[]{Vector2Int.right,Vector2Int.up})
                            if(cells.Contains(c+delta)){p.BeginPath();p.MoveTo(Point(c));p.LineTo(Point(c+delta));p.Stroke();connected=true;}
                        if(!connected&&!cells.Contains(c+Vector2Int.left)&&!cells.Contains(c+Vector2Int.down))
                        {p.BeginPath();p.MoveTo(Point(c)-Vector2.right*.7f);p.LineTo(Point(c)+Vector2.right*.7f);p.Stroke();}
                    }
                }
                foreach(var route in region.TransportRoutes??new())
                {
                    if(route?.Points==null||route.Points.Count<2)continue;
                    void Stroke(Color color,float thickness)
                    {
                        p.strokeColor=color;p.lineWidth=thickness;
                        var sections=drySections.TryGetValue(route,out var dry)?dry:new List<List<Vector2>>{route.Points};
                        foreach(var section in sections)
                        {
                            if(section.Count<2)continue;
                            p.BeginPath();p.MoveTo(section[0]*unit);
                            for(int i=1;i<section.Count;i++)p.LineTo(section[i]*unit);
                            p.Stroke();
                        }
                    }
                    if(route.Kind==RegionTransportKind.Highway)
                    {Stroke(new Color(.35f,.30f,.22f),4);Stroke(route.SurfaceId=="dirt"?new Color(.69f,.54f,.36f):new Color(.97f,.74f,.33f),2.5f);}
                    else
                    {
                        Stroke(new Color(.15f,.21f,.25f),2);
                        p.lineWidth=1.5f;
                        for(int i=1;i<route.Points.Count;i++)
                        {
                            var a=route.Points[i-1]*unit;var b=route.Points[i]*unit;var v=b-a;
                            if(v.sqrMagnitude<.01f)continue;
                            var side=new Vector2(-v.y,v.x).normalized*3;
                            for(float distance=3;distance<v.magnitude;distance+=8)
                            {var center=a+v.normalized*distance;p.BeginPath();p.MoveTo(center-side);p.LineTo(center+side);p.Stroke();}
                        }
                    }
                }
            };
        }
        public void RefreshRouteLabels()
        {
            Clear();drySections.Clear();
            foreach(var route in region.TransportRoutes??new())
            {
                if(route.HasDistrictRoads)drySections[route]=NationalPikePlacement.DryMapSections(region,route,unit);
                if(string.IsNullOrWhiteSpace(route.Name)||route.Points==null||route.Points.Count<2)continue;
                float length=0;for(int i=1;i<route.Points.Count;i++)length+=Vector2.Distance(route.Points[i-1],route.Points[i]);
                var center=route.Points[0];float remaining=length*.5f;
                for(int i=1;i<route.Points.Count;i++)
                {
                    float segment=Vector2.Distance(route.Points[i-1],route.Points[i]);
                    if(remaining<=segment){center=Vector2.Lerp(route.Points[i-1],route.Points[i],segment>0?remaining/segment:0);break;}
                    remaining-=segment;
                }
                var label=new Label(route.Name){name="region-road-label-"+route.Id,pickingMode=PickingMode.Ignore};
                var anchor=RegionMapProjection.CreateLabelAnchor(region.Width,region.Height,out var content);
                anchor.style.left=center.x*unit;anchor.style.top=center.y*unit;
                label.AddToClassList("region-road-map-label");label.style.left=0;label.style.top=-8;
                content.Add(label);Add(anchor);
            }
        }
    }
}
