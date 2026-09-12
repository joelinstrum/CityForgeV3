using UnityEngine;
namespace CityForgeV3.World
{
    // Transient decisions live on the presentation; position remains in the lot save.
    public sealed class BearRoamingAgent : MonoBehaviour
    {
        public readonly BearBehavior Behavior = new();
        public Vector2 Direction;
        public float NextSteer;
        public BearMode Mode;
        public bool ThreatDetected;
        public string ThreatId;
    }
}
