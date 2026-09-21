using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    /// <summary>Presentation-only bend weights, measured over metres rather than point indices.</summary>
    public sealed class RiverBankAppearance
    {
        public const string ResourceRoot = "CityForgeV3/Water/River/BanksV1/";
        public const string ShorelineResourceRoot =
            "CityForgeV3/Water/River/BanksV4/";
        public const string SubmergedGravelResource =
            "CityForgeV3/Water/River/BanksV4/submerged-gravel-light";
        public const string OpenGravelResource =
            "CityForgeV3/Water/River/BanksV4/open-gravel-light";
        public const float DetailMeters = 48f;
        public readonly float[] Bend;
        public float ShoreDistance { get; set; }

        public RiverBankAppearance(IReadOnlyList<Vector2> points, float widthMeters)
        {
            Bend = new float[points.Count];
            if (points.Count < 3) return;
            var distances = new float[points.Count];
            for (int i = 1; i < points.Count; i++)
                distances[i] = distances[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            float reach = Mathf.Clamp(widthMeters * .75f, 24f, 120f);
            for (int i = 0; i < points.Count; i++)
            {
                // A symmetric window avoids classifying one-sided border samples
                // as a bend. Duplicate points and short fragments remain finite.
                float span = Mathf.Min(reach, distances[i], distances[^1] - distances[i]);
                if (span < .01f) continue;
                var incoming = points[i] - AtDistance(points, distances, distances[i] - span);
                var outgoing = AtDistance(points, distances, distances[i] + span) - points[i];
                if (incoming.sqrMagnitude < .0001f || outgoing.sqrMagnitude < .0001f) continue;
                float angle = Mathf.Atan2(incoming.x * outgoing.y - incoming.y * outgoing.x,
                    Vector2.Dot(incoming, outgoing));
                Bend[i] = Mathf.Clamp(angle * widthMeters / Mathf.Max(1f, span), -1f, 1f);
            }
        }

        static Vector2 AtDistance(IReadOnlyList<Vector2> points, float[] distances, float distance)
        {
            int low = 0, high = distances.Length - 1;
            while (high - low > 1)
            {
                int middle = (low + high) / 2;
                if (distances[middle] <= distance) low = middle;
                else high = middle;
            }
            return Vector2.Lerp(points[low], points[high],
                Mathf.InverseLerp(distances[low], distances[high], distance));
        }
    }
}
