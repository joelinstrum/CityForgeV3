using UnityEngine;

namespace CityForgeV3.World
{
    // Animation follows real travel. A later team/vehicle controller can move
    // this root without giving each horse competing pathfinding behavior.
    public sealed class HorseGaitController : MonoBehaviour
    {
        public const float WalkMetersPerSecond = 1.06f;
        // Original clip stride speed stays fixed as travel speed changes.
        private const float WalkClipMetersPerSecond = 0.53f;
        public const float TrotMetersPerSecond = 1.65f;
        public float TravelSpeed { get; private set; }
        private ThreeDimensionalCharacterAnimator player;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private bool initialized;
        private float phase;

        private void Awake()
        {
            player = GetComponent<ThreeDimensionalCharacterAnimator>();
            phase = Random.value;
        }

        private void LateUpdate()
        {
            if (player == null || Time.deltaTime <= 0f) return;
            if (!initialized)
            {
                previousPosition = transform.position;
                previousRotation = transform.rotation;
                initialized = true;
                player.SetPlaybackPhase(phase);
            }
            var delta = transform.position - previousPosition;
            delta.y = 0f;
            var speed = delta.magnitude / Time.deltaTime;
            var turning = Quaternion.Angle(previousRotation, transform.rotation) / Time.deltaTime;
            // Placement/teleporting is not locomotion.
            SetTravelSpeed(speed > 6f ? 0f : speed, turning > 180f ? 0f : turning);
            previousPosition = transform.position;
            previousRotation = transform.rotation;
        }

        public void SetTravelSpeed(float metersPerSecond, float turnDegreesPerSecond = 0f)
        {
            if (player == null) return;
            TravelSpeed = Mathf.Max(0f, metersPerSecond);
            var state = TravelSpeed > 1.35f ? "trot" : TravelSpeed > 0.025f || turnDegreesPerSecond > 2f ? "walk" : "idle";
            var changed = player.State != state;
            if (!player.Play(state)) return;
            if (changed) player.SetPlaybackPhase(phase);
            var speed = state == "idle" ? 1f : Mathf.Max(0.65f,
                TravelSpeed / (state == "trot" ? TrotMetersPerSecond : WalkClipMetersPerSecond));
            player.SetPlaybackSpeed(Mathf.Min(speed, 2.5f));
        }
    }
}
