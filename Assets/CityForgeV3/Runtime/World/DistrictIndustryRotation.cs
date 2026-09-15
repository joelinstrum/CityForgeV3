using UnityEngine;

namespace CityForgeV3.World
{
    public static class DistrictIndustryRotation
    {
        public static bool TryFootprint(RegionCityTile d, DistrictSelectionRef item,
            out Vector2 center, out Vector2 half, out float yaw)
        {
            center = half = default; yaw = 0;
            if (item.Kind == DistrictSelectionKind.Entity && item.Id.StartsWith("quarry:"))
            {
                var site = d?.StoneSites?.Find(s => s.Id == item.Id.Substring(item.Id.IndexOf(':') + 1) && s.Built);
                if (site == null) return false;
                center = DistrictQuarry.Point(d, site); half = new Vector2(15, 14); yaw = site.Yaw; return true;
            }
            if (item.Kind == DistrictSelectionKind.Entity && item.Id.StartsWith("brickworks:"))
            {
                var site = d?.Brickworks?.Find(s => s.Id == item.Id.Substring(item.Id.IndexOf(':') + 1));
                if (site == null) return false;
                center = DistrictBrickworks.Point(d, site); half = new Vector2(DistrictBrickworks.HalfWidth, DistrictBrickworks.HalfDepth); yaw = site.Yaw; return true;
            }
            return false;
        }

        public static void Apply(RegionCityTile d, DistrictSelectionRef item, int direction)
        {
            if (!TryFootprint(d, item, out var center, out _, out _)) return;
            if (item.Kind == DistrictSelectionKind.Entity && item.Id.StartsWith("quarry:"))
            {
                var q = d.StoneSites.Find(s => s.Id == item.Id.Substring(item.Id.IndexOf(':') + 1));
                bool parked = !q.HasWagonPose || Vector2.Distance(q.WagonPosition, DistrictBrickworks.QuarryHome(d,q)) < 2;
                q.Yaw = Mathf.Repeat(q.Yaw + direction * 90, 360);
                if (parked && q.Phase != "delivering" && q.Phase != "returning" && q.Phase != "unloading")
                {
                    if (q.HasWagonPose) q.WagonPosition = DistrictBrickworks.Offset(center, direction * 90, q.WagonPosition - center);
                    q.HorseHeading += direction * 90; q.BodyHeading += direction * 90; q.FrontHeading += direction * 90;
                }
                if (q.Phase == "returning") q.DeliveryDestination = DistrictBrickworks.QuarryHome(d, q);
            }
            else
            {
                var b = d.Brickworks.Find(s => s.Id == item.Id.Substring(item.Id.IndexOf(':') + 1));
                b.Yaw = Mathf.Repeat(b.Yaw + direction * 90, 360);
                foreach (var q in d.StoneSites ?? new())
                    if (q.DeliveryTargetId == b.Id && (q.Phase == "delivering" || q.Phase == "unloading"))
                    {
                        q.DeliveryDestination = DistrictBrickworks.ReceivingPoint(d, b);
                        q.Phase = "delivering"; q.Elapsed = 0;
                    }
            }
            foreach (var q in d.StoneSites ?? new()) { q.DeliveryRoute = null; q.DeliveryRetry = 0; }
        }
    }
}
