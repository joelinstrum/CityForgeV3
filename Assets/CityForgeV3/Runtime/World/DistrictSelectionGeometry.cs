using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public static class DistrictSelectionGeometry
    {
        public static Rect Rectangle(Vector2 a, Vector2 b) =>
            Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));

        // SAT intersection against a convex projected footprint. Testing its
        // bounding rectangle alone incorrectly includes empty isometric corners.
        public static bool Overlaps(Rect rectangle, IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3) return false;
            if (Separated(Vector2.right) || Separated(Vector2.up)) return false;
            for (var i = 0; i < polygon.Count; i++)
            {
                var edge = polygon[(i + 1) % polygon.Count] - polygon[i];
                if (edge.sqrMagnitude > .000001f &&
                    Separated(new Vector2(-edge.y, edge.x).normalized)) return false;
            }
            return true;

            bool Separated(Vector2 axis)
            {
                var low = float.PositiveInfinity;
                var high = float.NegativeInfinity;
                foreach (var vertex in polygon)
                {
                    var projection = Vector2.Dot(vertex, axis);
                    low = Mathf.Min(low, projection);
                    high = Mathf.Max(high, projection);
                }
                var center = Vector2.Dot(rectangle.center, axis);
                var radius = Mathf.Abs(axis.x) * rectangle.width * .5f +
                             Mathf.Abs(axis.y) * rectangle.height * .5f;
                return high < center - radius - .0001f ||
                       low > center + radius + .0001f;
            }
        }
    }
}
