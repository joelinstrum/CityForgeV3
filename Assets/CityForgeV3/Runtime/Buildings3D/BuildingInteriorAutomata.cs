using CityForgeV3.World;
using UnityEngine;

namespace CityForgeV3.Buildings3D
{
    /// <summary>One shared, depth-tested clip inside an authored room. No district queries.</summary>
    public sealed class BuildingInteriorAutomata : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Renderer shell;
        [SerializeField] private string clipId = "gentleman-and-lady-strolling-v01";
        [SerializeField] private float minimumScreenHeight = .16f;
        [SerializeField] private float clipYawDegrees = 90f;
        [SerializeField] private Vector3 roomMinimum = new(-4.7f,4.13f,-3.95f);
        [SerializeField] private Vector3 roomMaximum = new(5.4f,6.71f,2.12f);
        [SerializeField] private BuildingNightLighting[] lighting;
        private Camera view;
        private AutomataClipEntry entry;
        private Sprite[][] frames;
        private Light[] lights;
        private float elapsed;
        private int lastFrame = -1, lastFacing = -1;
        private bool shadowCopy;
        private MaterialPropertyBlock roomProperties;
        private static readonly int WorldToRoom = Shader.PropertyToID("_WorldToRoom");
        private static readonly int RoomMin = Shader.PropertyToID("_RoomMin");
        private static readonly int RoomMax = Shader.PropertyToID("_RoomMax");

        public bool IsPresenting => art != null && art.enabled;
        public int CurrentFrame => lastFrame;

        public void Configure(SpriteRenderer renderer, Renderer building,
            BuildingNightLighting[] controls)
        {
            art = renderer;
            shell = building;
            lighting = controls;
        }

        public void DisableForShadowCopy()
        {
            shadowCopy = true;
            if (art != null) { art.enabled = false; art.gameObject.SetActive(false); }
            enabled = false;
        }

        private void LateUpdate()
        {
            if (view == null || !view.isActiveAndEnabled) view = Camera.main;
            Advance(Time.deltaTime, view);
        }

        public void Advance(float seconds, Camera camera)
        {
            if (shadowCopy || art == null || shell == null) return;
            var visible = false;
            if (camera != null && camera.isActiveAndEnabled)
            {
                var bounds = shell.bounds;
                var point = camera.WorldToViewportPoint(bounds.center);
                var screenHeight = camera.orthographic
                    ? bounds.size.y / (2f * camera.orthographicSize)
                    : bounds.size.y / (2f * Mathf.Max(.01f, point.z) *
                        Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad));
                visible = point.z > 0 && point.x > -.15f && point.x < 1.15f &&
                    point.y > -.15f && point.y < 1.15f &&
                    screenHeight >= minimumScreenHeight;
            }
            if (art.enabled != visible) art.enabled = visible;
            // These fixed bindings are created once with the building, refreshed
            // by ordinary instantiation/reload. Never search a district or Lot.
            if (lights == null) lights = GetComponentsInChildren<Light>(true);
            var night = lighting != null && lighting.Length > 0 &&
                lighting[0] != null && lighting[0].NightAmount > .05f;
            foreach (var light in lights)
                if (light != null && light.enabled != (visible && night))
                    light.enabled = visible && night;
            if (!visible) return;
            entry ??= AutomataClipCatalog.Find(clipId);
            if (entry == null) return;
            frames ??= AutomataClipPlayer.FramesFor(entry);
            if (frames == null) return;
            elapsed = (elapsed + Mathf.Max(0, seconds)) %
                (entry.frameCount / entry.framesPerSecond);
            var toward = Quaternion.Euler(0,-clipYawDegrees,0) * transform.InverseTransformDirection(
                camera.transform.position - art.transform.position);
            var facing = Mathf.RoundToInt(Mathf.Atan2(toward.x, -toward.z) *
                Mathf.Rad2Deg / (360f / entry.facingCount));
            facing = (facing % entry.facingCount + entry.facingCount) % entry.facingCount;
            var frame = Mathf.FloorToInt(elapsed * entry.framesPerSecond) % entry.frameCount;
            // Preserve the authored interior position: the outdoor Automata
            // ground-clearance offset would pull the people through the wall.
            art.transform.rotation = camera.transform.rotation;
            roomProperties ??= new MaterialPropertyBlock();
            art.GetPropertyBlock(roomProperties,0);
            roomProperties.SetMatrix(WorldToRoom,transform.worldToLocalMatrix);
            roomProperties.SetVector(RoomMin,roomMinimum);
            roomProperties.SetVector(RoomMax,roomMaximum);
            art.SetPropertyBlock(roomProperties,0);
            if (frame == lastFrame && facing == lastFacing) return;
            art.sprite = frames[facing][frame];
            lastFrame = frame;
            lastFacing = facing;
        }
    }
}
