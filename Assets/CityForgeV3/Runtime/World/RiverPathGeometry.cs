using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    internal static class RiverPathGeometry
    {
        public static List<Vector2> RoundOrthogonalCorners(
            IReadOnlyList<Vector2> points, float maximumRadius,
            int curveSegments = 4)
        {
            var result = new List<Vector2>();
            if (points == null || points.Count == 0) return result;
            AddUnique(result, points[0]);
            for (var index = 1; index < points.Count - 1; index++)
            {
                var prior = points[index - 1];
                var corner = points[index];
                var next = points[index + 1];
                var incoming = corner - prior;
                var outgoing = next - corner;
                var incomingLength = incoming.magnitude;
                var outgoingLength = outgoing.magnitude;
                if (incomingLength < .000001f || outgoingLength < .000001f)
                    continue;
                incoming /= incomingLength;
                outgoing /= outgoingLength;
                if (Mathf.Abs(Vector2.Dot(incoming, outgoing)) > .0001f)
                {
                    AddUnique(result, corner);
                    continue;
                }

                // A cut below half of both neighboring segments guarantees
                // that curves at opposite ends of a short run cannot overlap.
                var radius = Mathf.Min(maximumRadius,
                    Mathf.Min(incomingLength, outgoingLength) * .42f);
                var entry = corner - incoming * radius;
                var exit = corner + outgoing * radius;
                AddUnique(result, entry);
                for (var sample = 1; sample <= curveSegments; sample++)
                {
                    var t = sample / (float)curveSegments;
                    var inverse = 1f - t;
                    AddUnique(result, inverse * inverse * entry +
                        2f * inverse * t * corner + t * t * exit);
                }
            }
            AddUnique(result, points[^1]);
            return result;
        }

        private static void AddUnique(List<Vector2> points, Vector2 point)
        {
            if (points.Count == 0 ||
                Vector2.Distance(points[^1], point) > .000001f)
                points.Add(point);
        }
    }
}
