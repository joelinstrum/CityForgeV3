using System;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum BearMode { Roaming, Looking, Retreating }

    public sealed class BearBehavior
    {
        public const float WalkSpeed = 0.40f;
        public const float RetreatSpeed = 0.72f;
        public BearMode Mode { get; private set; } = BearMode.Roaming;
        public float Until { get; private set; }
        public bool Initialized { get; private set; }
        public string Animation => Mode == BearMode.Looking ? "idle" : "walk";
        public float Speed => Mode == BearMode.Looking ? 0f :
            Mode == BearMode.Retreating ? RetreatSpeed : WalkSpeed;
        public static bool FearsPerson(string id) =>
            string.Equals(id, LotWorldController.MusketmanCharacterId,
                StringComparison.OrdinalIgnoreCase);
        public static bool FearsBuilding(string id) =>
            string.Equals(id, LotWorldController.FortWatchtowerEvaluationId,
                StringComparison.OrdinalIgnoreCase);
        public static float ThreatRadius(bool tower, bool retreating) =>
            tower ? (retreating ? 23f : 18f) : (retreating ? 14f : 10f);

        public void Tick(float now, bool threatened, float roll)
        {
            if (threatened)
            {
                Mode = BearMode.Retreating;
                Until = now + 2f;
                Initialized = true;
                return;
            }
            if (Initialized && now < Until) return;
            if (!Initialized || Mode != BearMode.Roaming)
            {
                Mode = BearMode.Roaming;
                Until = now + Mathf.Lerp(12f, 24f, Mathf.Clamp01(roll));
            }
            else
            {
                Mode = BearMode.Looking;
                Until = now + Mathf.Lerp(2f, 4f, Mathf.Clamp01(roll));
            }
            Initialized = true;
        }
    }
}
