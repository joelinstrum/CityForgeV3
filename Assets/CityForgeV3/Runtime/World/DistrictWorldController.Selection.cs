using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        // Coordinates match TryGroundPoint: Game-view pixels, Y down.
        // All candidates are collected once, after the pointer is released.
        public List<DistrictSelectionRef> CollectDistrictSelectionInScreenRect(
            RegionCityTile district, Rect rectangle)
        {
            var result = new List<DistrictSelectionRef>();
            if (district == null || _camera == null || _content == null) return result;
            var projected = new Vector2[4];
            bool Hits(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var corners = new[] { a, b, c, d };
                for (var i = 0; i < 4; i++)
                {
                    var pixel = _camera.WorldToScreenPoint(corners[i]);
                    if (pixel.z <= 0f) return false;
                    projected[i] = new Vector2(pixel.x, Screen.height - pixel.y);
                }
                return DistrictSelectionGeometry.Overlaps(rectangle, projected);
            }
            bool HitsGround(Rect footprint)
            {
                float y = .17f + TerrainElevation(footprint.center.x, footprint.center.y);
                return Hits(
                    _content.TransformPoint(new Vector3(footprint.xMin, y, footprint.yMin)),
                    _content.TransformPoint(new Vector3(footprint.xMax, y, footprint.yMin)),
                    _content.TransformPoint(new Vector3(footprint.xMax, y, footprint.yMax)),
                    _content.TransformPoint(new Vector3(footprint.xMin, y, footprint.yMax)));
            }
            foreach (var flora in district.Flora ?? new List<PlacedDistrictFlora>())
            {
                if (flora == null || !_districtFloraPresentations.TryGetValue(
                        flora.InstanceId, out var renderer) || renderer == null || renderer.sprite == null) continue;
                var bounds = DistrictHarvestSprites.BoundsFor(renderer.sprite);
                var t = renderer.transform;
                if (Hits(t.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, 0f)),
                         t.TransformPoint(new Vector3(bounds.max.x, bounds.min.y, 0f)),
                         t.TransformPoint(new Vector3(bounds.max.x, bounds.max.y, 0f)),
                         t.TransformPoint(new Vector3(bounds.min.x, bounds.max.y, 0f))))
                    result.Add(new DistrictSelectionRef(DistrictSelectionKind.Flora, flora.InstanceId));
            }
            foreach (var lot in district.Lots ?? new List<PlacedDistrictLot>())
            {
                if (lot == null) continue;
                var item = new DistrictSelectionRef(DistrictSelectionKind.Lot, lot.InstanceId);
                if (TryDistrictSelectionBounds(district, item, out var footprint) && HitsGround(footprint))
                    result.Add(item);
            }
            bool HitsModel(Transform root)
            {
                if (root == null) return false;
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    var b = renderer.bounds;
                    var a = new Vector3(b.min.x,b.min.y,b.min.z); var c = b.max;
                    if (Hits(a,new Vector3(c.x,a.y,a.z),new Vector3(c.x,c.y,a.z),new Vector3(a.x,c.y,a.z)) ||
                        Hits(new Vector3(a.x,a.y,c.z),new Vector3(c.x,a.y,c.z),c,new Vector3(a.x,c.y,c.z)) ||
                        Hits(new Vector3(a.x,c.y,a.z),new Vector3(c.x,c.y,a.z),c,new Vector3(a.x,c.y,c.z))) return true;
                }
                return false;
            }
            foreach (var site in district.StoneSites ?? new List<DistrictStoneSite>())
            {
                if (!site.Built) continue;
                var item = new DistrictSelectionRef(DistrictSelectionKind.Quarry, site.Id);
                if (TryDistrictSelectionBounds(district, item, out var footprint) && (HitsGround(footprint) || (quarryViews.TryGetValue(site.Id, out var view) && HitsModel(view.Root)))) result.Add(item);
            }
            foreach (var site in district.Brickworks ?? new List<DistrictBrickworksSite>())
            {
                var item = new DistrictSelectionRef(DistrictSelectionKind.Brickworks, site.Id);
                if (TryDistrictSelectionBounds(district, item, out var footprint) && (HitsGround(footprint) || (brickworksViews.TryGetValue(site.Id, out var view) && HitsModel(view.transform)))) result.Add(item);
            }
            foreach (var road in district.Roads ?? new List<PlacedRoadPiece>())
            {
                if (road == null) continue;
                var footprint = new Rect(-_widthMeters * .5f + road.GridX * DistrictScale.CellSizeMeters,
                    -_depthMeters * .5f + road.GridZ * DistrictScale.CellSizeMeters,
                    DistrictScale.CellSizeMeters, DistrictScale.CellSizeMeters);
                if (HitsGround(footprint)) result.Add(new DistrictSelectionRef(DistrictSelectionKind.Road, road.Id));
            }
            foreach (var river in district.Rivers ?? new List<PlacedDistrictRiver>())
            {
                if (river?.Points == null) continue;
                var radius = river.WidthMeters * (river.Depth == DistrictRiverDepth.Deep ? .54f : .64f);
                for (var i = 1; i < river.Points.Count; i++)
                {
                    var previous = river.Points[i - 1];
                    var next = river.Points[i];
                    if (previous == null || next == null) continue;
                    var a = new Vector3((previous.X - .5f) * _widthMeters, .17f, (previous.Z - .5f) * _depthMeters);
                    var b = new Vector3((next.X - .5f) * _widthMeters, .17f, (next.Z - .5f) * _depthMeters);
                    var along = b - a;
                    if (along.sqrMagnitude < .000001f) continue;
                    var side = Vector3.Cross(Vector3.up, along.normalized) * radius;
                    if (!Hits(_content.TransformPoint(a - side), _content.TransformPoint(a + side),
                            _content.TransformPoint(b + side), _content.TransformPoint(b - side))) continue;
                    result.Add(new DistrictSelectionRef(DistrictSelectionKind.River, river.InstanceId));
                    break;
                }
            }
            return result;
        }
    }
}
