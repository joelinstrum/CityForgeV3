using System.Linq;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        public static bool IndustryFootprint(RegionCityTile d, DistrictSelectionRef item,
            out Vector2 center, out Vector2 half, out float yaw) =>
            DistrictIndustryRotation.TryFootprint(d, item, out center, out half, out yaw);

        public string IndustryRotationReason(RegionCityTile d, DistrictSelectionRef item, int direction)
        {
            if (!IndustryFootprint(d, item, out var center, out var half, out var yaw)) return "Select a building first.";
            var ground = new DistrictLaborNavigation(d, IsUnderRiverWater);
            var terrain = new DistrictElevation(d);
            float low = float.MaxValue, high = float.MinValue;
            int nx = Mathf.CeilToInt(half.x), nz = Mathf.CeilToInt(half.y);
            for (int z = 0; z <= nz; z++) for (int x = 0; x <= nx; x++)
            {
                var p = DistrictBrickworks.Offset(center, yaw + direction * 90,
                    new Vector2(Mathf.Lerp(-half.x, half.x, x / (float)nx), Mathf.Lerp(-half.y, half.y, z / (float)nz)));
                if (!ground.Walkable(p)) return "Rotation needs dry ground clear of lots and the district edge.";
                if ((d.Brickworks ?? new()).Any(b => !(item.Kind == DistrictSelectionKind.Brickworks && b.Id == item.Id) && DistrictBrickworks.Contains(d, b, p, 1)))
                    return "Rotation would overlap a Brickworks.";
                foreach (var q in d.StoneSites ?? new())
                {
                    if (!q.Built || item.Kind == DistrictSelectionKind.Quarry && q.Id == item.Id) continue;
                    var local = DistrictBrickworks.Offset(Vector2.zero, -q.Yaw, p - DistrictQuarry.Point(d, q));
                    if (Mathf.Abs(local.x) < 16 && Mathf.Abs(local.y) < 15) return "Rotation would overlap a quarry.";
                }
                foreach (var road in d.Roads ?? new())
                {
                    var r = new Vector2((road.GridX + .5f) * 10 - DistrictScale.SizeMeters(d.Width) / 2,
                        (road.GridZ + .5f) * 10 - DistrictScale.SizeMeters(d.Height) / 2);
                    if (Mathf.Abs(r.x - p.x) < 5 && Mathf.Abs(r.y - p.y) < 5) return "Move roads outside the rotated footprint.";
                }
                float height = terrain.Sample(p.x, p.y); low = Mathf.Min(low, height); high = Mathf.Max(high, height);
            }
            return high - low > 1.5f ? "Level the ground before rotating this building." : "";
        }

        // Capture the convoy before rebuilding the rotated presentation. Away wagons keep
        // their district pose; parked wagons turn with the quarry and its loading crane.
        public void PrepareIndustryRotation(RegionCityTile d, DistrictSelectionRef item, int direction)
        {
            foreach (var q in d.StoneSites ?? new())
                if (quarryViews.TryGetValue(q.Id, out var view) && view.Wagon != null)
                    SaveQuarryWagonPose(q, view.Wagon);
            DistrictIndustryRotation.Apply(d, item, direction);
        }
    }

}
