using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    public static class DistrictHarvestSprites
    {
        public const float Duration = 1.5f;
        private const float Ppu = 24.477361f;
        private static readonly Vector2 Pivot = new(.50119257f, .31772473f);
        private static readonly Dictionary<int, Sprite> Cache = new();
        private static readonly Dictionary<Sprite, Bounds> Visible = new();
        private static readonly string[] Directions = { "east", "north", "west", "south" };
        private static readonly Rect[] Alpha = {
            new(190, 154, 134, 250),
            new(190, 154, 134, 250),
            new(190, 155, 133, 250),
            new(190, 155, 133, 251),
            new(190, 154, 133, 251),
            new(190, 154, 135, 249),
            new(191, 153, 136, 247),
            new(191, 151, 141, 244),
            new(191, 149, 145, 240),
            new(192, 146, 151, 235),
            new(192, 142, 158, 227),
            new(193, 139, 164, 215),
            new(194, 135, 171, 201),
            new(197, 130, 183, 183),
            new(199, 127, 196, 166),
            new(201, 123, 208, 155),
            new(204, 121, 217, 150),
            new(206, 112, 224, 150),
            new(208, 101, 225, 153),
            new(210, 98, 224, 154),
            new(208, 101, 226, 153),
            new(208, 105, 225, 151),
            new(208, 101, 226, 153),
            new(210, 98, 224, 154),
            new(190, 154, 134, 250),
            new(190, 154, 134, 249),
            new(190, 153, 133, 249),
            new(190, 153, 133, 249),
            new(190, 154, 133, 249),
            new(190, 154, 134, 250),
            new(190, 156, 137, 250),
            new(191, 156, 140, 253),
            new(191, 159, 147, 253),
            new(191, 160, 153, 254),
            new(192, 161, 158, 254),
            new(193, 158, 165, 256),
            new(195, 154, 171, 258),
            new(198, 150, 184, 255),
            new(200, 145, 197, 249),
            new(204, 141, 207, 237),
            new(206, 137, 218, 221),
            new(208, 133, 225, 203),
            new(208, 132, 230, 187),
            new(208, 131, 231, 181),
            new(209, 132, 229, 186),
            new(207, 132, 229, 193),
            new(209, 132, 229, 186),
            new(208, 131, 231, 181),
            new(190, 154, 134, 250),
            new(191, 154, 133, 249),
            new(191, 154, 135, 248),
            new(191, 154, 135, 247),
            new(191, 154, 135, 248),
            new(190, 154, 134, 250),
            new(190, 155, 132, 252),
            new(189, 157, 132, 253),
            new(185, 158, 135, 256),
            new(179, 160, 141, 257),
            new(172, 162, 147, 257),
            new(166, 160, 152, 261),
            new(157, 157, 159, 263),
            new(145, 153, 168, 263),
            new(130, 150, 181, 258),
            new(113, 146, 195, 248),
            new(99, 142, 204, 232),
            new(89, 138, 211, 216),
            new(84, 134, 215, 203),
            new(82, 133, 217, 198),
            new(83, 134, 217, 202),
            new(85, 135, 214, 207),
            new(83, 134, 217, 202),
            new(82, 133, 217, 198),
            new(190, 154, 134, 250),
            new(191, 154, 134, 250),
            new(191, 155, 135, 250),
            new(190, 154, 137, 251),
            new(191, 155, 136, 250),
            new(190, 154, 135, 249),
            new(190, 153, 133, 248),
            new(189, 151, 132, 246),
            new(185, 149, 135, 242),
            new(178, 146, 142, 237),
            new(173, 143, 146, 231),
            new(164, 139, 154, 221),
            new(156, 135, 162, 207),
            new(144, 131, 172, 189),
            new(128, 128, 187, 170),
            new(113, 124, 198, 157),
            new(99, 122, 209, 148),
            new(89, 114, 216, 145),
            new(85, 109, 218, 145),
            new(83, 107, 219, 147),
            new(84, 107, 219, 146),
            new(86, 110, 218, 143),
            new(84, 107, 219, 146),
            new(83, 107, 219, 147),
            new(239, 171, 27, 19),
        };
        public static Sprite Get(int direction, int frame, bool stump = false)
        {
            direction = Mathf.Clamp(direction, 0, 3); frame = Mathf.Clamp(frame, 0, 23);
            var key = stump ? 96 : direction * 24 + frame;
            if (Cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>("CityForgeV3/Flora/CilicianHarvestV01/" +
                (stump ? "stump" : Directions[direction] + "-sheet"));
            if (texture == null) return null;
            var rect = stump ? new Rect(0,0,512,512) : new Rect(frame % 6 * 512, (3 - frame / 6) * 512, 512,512);
            sprite = Sprite.Create(texture, rect, Pivot, Ppu, 0, SpriteMeshType.FullRect);
            sprite.name = "Cilician harvest " + key;
            Cache[key] = sprite;
            var a = Alpha[key];
            Visible[sprite] = new Bounds(new Vector3((a.center.x-512*Pivot.x)/Ppu,(a.center.y-512*Pivot.y)/Ppu,0),new Vector3(a.width/Ppu,a.height/Ppu,0));
            return sprite;
        }
        public static Bounds BoundsFor(Sprite sprite) => Visible.TryGetValue(sprite, out var bounds) ? bounds : sprite.bounds;
    }
    public sealed class DistrictTreeFallPlayer : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private int _direction;
        private float _started;
        private int _frame = -1;
        public bool Playing { get; private set; }
        public void Begin(int direction)
        {
            _renderer = GetComponent<SpriteRenderer>(); _direction = direction;
            _started = Time.unscaledTime; Playing = true; Apply(0);
        }
        private void Update()
        {
            if (!Playing) return;
            var elapsed = Time.unscaledTime - _started;
            Apply(Mathf.Min(23, Mathf.FloorToInt(elapsed * 16)));
            if (elapsed >= DistrictHarvestSprites.Duration) Playing = false;
        }
        private void Apply(int frame)
        {
            if (_frame == frame) return; _frame = frame;
            _renderer.sprite = DistrictHarvestSprites.Get(_direction, frame);
            // All frames share geometry/pivot; update only the existing shadow UVs.
            var shadow = transform.Find("District Flora Shadow")?.GetComponent<MeshFilter>();
            if (shadow != null) shadow.sharedMesh.uv = _renderer.sprite.uv;
        }
    }
}
