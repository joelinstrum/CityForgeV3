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
        public const string WideShorelineResourceRoot =
            "CityForgeV3/Water/River/BanksV6Varied/";
        public const string WideSubmergedGravelResource =
            "CityForgeV3/Water/River/BanksV6Varied/submerged-neutral";
        public const string WideOpenGravelResource =
            "CityForgeV3/Water/River/BanksV6Varied/bank-02-bars";
        public const string WideBankVariantThreeResource =
            "CityForgeV3/Water/River/BanksV6Varied/bank-03-open";
        public const string WideBankVariantFourResource =
            "CityForgeV3/Water/River/BanksV6Varied/bank-04-cobbles";
        // Region generation reserves widths of 144 metres and above for major
        // rivers. Keeping the cutoff between the 76 m medium maximum and the
        // 144 m major minimum makes the art choice stable and size-driven.
        public const float WideRiverMinimumWidthMeters = 100f;
        public const float WideOuterBlendMeters = 8f;
        public const float DefaultOuterFadeEnd = 0.833333f;
        public const float WideOuterFadeNoise = 0.18f;
        public const float DetailMeters = 48f;
        public const float DefaultSubmergedBedBrightness = 0.8f;
        public const float WideSubmergedBedBrightness = 0.94f;
        public const float DefaultSubmergedBlendStart = 0.015f;
        public const float DefaultSubmergedBlendEnd = 0.13f;
        public const float WideSubmergedBlendStart = -0.15f;
        public const float WideSubmergedBlendEnd = 0.38f;
        public const float WideWaterEdgeOpacity = 0.42f;
        public const float WideDeepWaterStart = 0.34f;
        public const float WideDepthBlendSoftness = 0.58f;
        public const float WideSubmergedWaterOpacity = 0.82f;
        public const float WideWaterEdgeFeatherMeters = 6f;
        public const float DefaultWaterEdgeFeatherMeters = 1.5f;
        // Presentation-only shoulder beyond the calculated gameplay waterline.
        // It gives the shader real geometry on which to show shallow opacity.
        public const float WideVisualWaterShoulderMeters = 12f;
        public readonly float[] Bend;
        public readonly string ShorelineResource;
        public readonly string SubmergedGravelTextureResource;
        public readonly string OpenGravelTextureResource;
        public readonly string BankVariantThreeTextureResource;
        public readonly string BankVariantFourTextureResource;
        public readonly int BankVariantCount;
        public readonly float OuterBlendMeters;
        public readonly float OuterFadeEnd;
        public readonly float OuterFadeNoise;
        public readonly float TerrainBlendStrength;
        public readonly float SubmergedBedBrightness;
        public readonly float SubmergedBlendStart;
        public readonly float SubmergedBlendEnd;
        public float ShoreDistance { get; set; }
        public float PatternOffset { get; set; }

        public RiverBankAppearance(IReadOnlyList<Vector2> points, float widthMeters)
        {
            bool usesWideBank = UsesWideRiverBank(widthMeters);
            ShorelineResource = (usesWideBank
                ? WideShorelineResourceRoot : ShorelineResourceRoot) +
                (usesWideBank ? "bank-01-neutral" : "shoreline-light");
            SubmergedGravelTextureResource = usesWideBank
                ? WideSubmergedGravelResource : SubmergedGravelResource;
            OpenGravelTextureResource = usesWideBank
                ? WideOpenGravelResource : OpenGravelResource;
            BankVariantThreeTextureResource = usesWideBank
                ? WideBankVariantThreeResource : ShorelineResource;
            BankVariantFourTextureResource = usesWideBank
                ? WideBankVariantFourResource : OpenGravelTextureResource;
            BankVariantCount = usesWideBank ? 4 : 2;
            OuterBlendMeters = usesWideBank ? WideOuterBlendMeters : 0f;
            OuterFadeEnd = DefaultOuterFadeEnd + OuterBlendMeters / 16f;
            OuterFadeNoise = usesWideBank ? WideOuterFadeNoise : 0f;
            TerrainBlendStrength = usesWideBank ? 1f : 0f;
            SubmergedBedBrightness = usesWideBank
                ? WideSubmergedBedBrightness : DefaultSubmergedBedBrightness;
            SubmergedBlendStart = usesWideBank
                ? WideSubmergedBlendStart : DefaultSubmergedBlendStart;
            SubmergedBlendEnd = usesWideBank
                ? WideSubmergedBlendEnd : DefaultSubmergedBlendEnd;
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

        public static bool UsesWideRiverBank(float widthMeters) =>
            widthMeters >= WideRiverMinimumWidthMeters;

        public static float VisualWaterHalfWidth(float waterlineHalfWidth,
            float bankHalfWidth, float riverWidthMeters) =>
            Mathf.Min(bankHalfWidth, waterlineHalfWidth +
                (UsesWideRiverBank(riverWidthMeters)
                    ? WideVisualWaterShoulderMeters : 0f));

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
