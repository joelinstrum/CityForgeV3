using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    /// <summary>Plays a pre-rendered scene as one selectable lot object.</summary>
    public sealed class AutomataClipPlayer : MonoBehaviour
    {
        private static readonly Dictionary<string, Sprite[][]> SharedFrames =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Texture2D[]>
            SharedRecolorMasks = new(StringComparer.Ordinal);
        private static Sprite _selectionOutline;
        private static Material _sharedArtMaterial;
        private static Material _sharedRecolorMaterial;
        private LotWorldController _owner;
        private Camera _camera;
        private AutomataClipEntry _entry;
        private Sprite[][] _frames;
        private SpriteRenderer _art;
        private SpriteRenderer _selection;
        private Sprite _lastSprite;
        private MaterialPropertyBlock _recolorProperties;
        private Texture2D[] _recolorMasks;
        private int _lastFacing = -1;
        private float _elapsed;
        private bool _visible = true;
        private bool _selected;
        private int _timeMask = 31;
        private int _seasonMask = 15;

        public void Initialize(LotWorldController owner, Camera camera,
            AutomataClipEntry entry, string instanceId)
        {
            _owner = owner;
            _camera = camera;
            _entry = entry;
            _frames = FramesFor(entry);
            if (_frames == null) return;
            _art = new GameObject("Pre-rendered scene")
                .AddComponent<SpriteRenderer>();
            _art.transform.SetParent(transform, false);
            if (!string.IsNullOrEmpty(entry.recolorMaskRoot))
            {
                _recolorMasks = MasksFor(entry);
                _recolorProperties = new MaterialPropertyBlock();
                _art.sharedMaterial = SharedRecolorMaterial();
            }
            else _art.sharedMaterial = SharedArtMaterial(_art.sharedMaterial);
            _selection = new GameObject("Group selection outline")
                .AddComponent<SpriteRenderer>();
            _selection.transform.SetParent(transform, false);
            _selection.sprite = SelectionOutline();
            _selection.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            _selection.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            _selection.transform.localScale = Vector3.one *
                (entry.footprintMeters / 8f);
            _selection.color = new Color(1f, 0.72f, 0.12f, 0.95f);
            _selection.enabled = false;
            _elapsed = (Seed(instanceId) % (uint)entry.frameCount) /
                entry.framesPerSecond;
            RefreshPose();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (_selection != null) _selection.enabled = selected && _visible;
        }

        public void SetVisibilitySchedule(int timeMask, int seasonMask)
        {
            _timeMask = timeMask;
            _seasonMask = seasonMask;
            RefreshVisibility();
        }

        public void SetRecolors(string firstHex, string secondHex)
        {
            if (_recolorProperties == null || _art == null) return;
            var firstColor = Color.white;
            var secondColor = Color.white;
            var first = !string.IsNullOrEmpty(firstHex) &&
                ColorUtility.TryParseHtmlString(firstHex, out firstColor);
            var second = !string.IsNullOrEmpty(secondHex) &&
                ColorUtility.TryParseHtmlString(secondHex, out secondColor);
            _recolorProperties.SetColor("_RecolorOne",
                first ? firstColor : Color.white);
            _recolorProperties.SetColor("_RecolorTwo",
                second ? secondColor : Color.white);
            _recolorProperties.SetFloat("_RecolorOneMix", first ? 1f : 0f);
            _recolorProperties.SetFloat("_RecolorTwoMix", second ? 1f : 0f);
            _art.SetPropertyBlock(_recolorProperties);
        }

        public void RefreshVisibility()
        {
            if (_owner == null || _art == null || _selection == null) return;
            var artVisible = _visible &&
                (_timeMask & (1 << (int)_owner.TimeOfDay)) != 0 &&
                (_seasonMask & (1 << (int)_owner.AutomataSeason)) != 0;
            if (_art.enabled != artVisible) _art.enabled = artVisible;
            var outlineVisible = _visible && _selected;
            if (_selection.enabled != outlineVisible)
                _selection.enabled = outlineVisible;
        }

        public bool ContainsScreenPixel(Vector2 pixel)
        {
            if (_camera == null || _art == null || !_art.enabled ||
                _art.sprite == null) return false;
            var bounds = _art.sprite.bounds;
            var min = new Vector2(float.PositiveInfinity,
                float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity,
                float.NegativeInfinity);
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
            {
                var corner = _art.transform.TransformPoint(new Vector3(
                    x == 0 ? bounds.min.x : bounds.max.x,
                    y == 0 ? bounds.min.y : bounds.max.y, 0f));
                var screen = _camera.WorldToScreenPoint(corner);
                if (screen.z <= 0f) return false;
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }
            return pixel.x >= min.x && pixel.x <= max.x &&
                pixel.y >= min.y && pixel.y <= max.y;
        }

        public void RefreshPose()
        {
            if (_art == null || _camera == null || _frames == null) return;
            _art.transform.rotation = _camera.transform.rotation;
            // Camera-facing quads extend below their anchor in world space.
            // Slide the artwork along the orthographic sightline so the
            // lowest visible pixel clears the terrain without moving it on
            // screen. The selection outline remains on the actual ground.
            var towardCamera = -_camera.transform.forward;
            var below = Mathf.Max(0f, _entry.visibleBelowPivotMeters);
            var depth = towardCamera.y > 0.01f
                ? (below * Mathf.Max(0f, _camera.transform.up.y) + 0.03f) /
                  towardCamera.y
                : 0f;
            _art.transform.position = transform.position +
                towardCamera * depth;
            var toward = _camera.transform.position - transform.position;
            var local = transform.InverseTransformDirection(toward);
            var facing = Mathf.RoundToInt(
                Mathf.Atan2(local.x, -local.z) * Mathf.Rad2Deg /
                (360f / _entry.facingCount));
            facing = (facing % _entry.facingCount + _entry.facingCount) %
                _entry.facingCount;
            if (_recolorProperties != null && facing != _lastFacing)
            {
                _recolorProperties.SetTexture("_MaskTex",
                    _recolorMasks[facing]);
                _art.SetPropertyBlock(_recolorProperties);
                _lastFacing = facing;
            }
            var frame = Mathf.FloorToInt(_elapsed * _entry.framesPerSecond) %
                _entry.frameCount;
            var sprite = _frames[facing][frame];
            if (sprite == _lastSprite) return;
            _art.sprite = sprite;
            _lastSprite = sprite;
        }

        private void Update()
        {
            if (_owner == null || _camera == null || _frames == null) return;
            var visible = LotWorldController.ShowsThreeDimensionalCharacters(
                _owner.ZoomLevel);
            if (visible)
            {
                var viewport = _camera.WorldToViewportPoint(transform.position);
                visible = viewport.z > 0f && viewport.x > -0.2f &&
                    viewport.x < 1.2f && viewport.y > -0.2f &&
                    viewport.y < 1.2f;
            }
            if (visible != _visible)
            {
                _visible = visible;
            }
            // A district calendar change only reads one scalar season here;
            // no lot or district collection is traversed per frame.
            RefreshVisibility();
            if (!visible) return;
            _elapsed += Time.deltaTime;
            var length = _entry.frameCount / _entry.framesPerSecond;
            if (_elapsed >= length) _elapsed %= length;
            RefreshPose();
        }

        private static Sprite[][] FramesFor(AutomataClipEntry entry)
        {
            if (SharedFrames.TryGetValue(entry.id, out var cached))
                return cached;
            if (!AutomataClipCatalog.ResourcesAvailable(entry)) return null;
            var frames = new Sprite[entry.facingCount][];
            var rows = Mathf.CeilToInt((float)entry.frameCount /
                                       entry.frameColumns);
            for (var facing = 0; facing < entry.facingCount; facing++)
            {
                var texture = Resources.Load<Texture2D>(
                    entry.resourceRoot + "-facing-" + facing);
                frames[facing] = new Sprite[entry.frameCount];
                for (var frame = 0; frame < entry.frameCount; frame++)
                {
                    var rect = new Rect(
                        frame % entry.frameColumns * entry.frameWidth,
                        (rows - 1 - frame / entry.frameColumns) *
                        entry.frameHeight,
                        entry.frameWidth, entry.frameHeight);
                    frames[facing][frame] = Sprite.Create(texture, rect,
                        new Vector2(0.5f, entry.pivotY),
                        entry.pixelsPerMeter, 0,
                        SpriteMeshType.FullRect);
                }
            }
            SharedFrames.Add(entry.id, frames);
            return frames;
        }

        private static Texture2D[] MasksFor(AutomataClipEntry entry)
        {
            if (SharedRecolorMasks.TryGetValue(entry.id, out var cached))
                return cached;
            var masks = new Texture2D[entry.facingCount];
            for (var facing = 0; facing < entry.facingCount; facing++)
                masks[facing] = Resources.Load<Texture2D>(
                    entry.recolorMaskRoot + "-facing-" + facing);
            SharedRecolorMasks.Add(entry.id, masks);
            return masks;
        }

        private static Sprite SelectionOutline()
        {
            if (_selectionOutline != null) return _selectionOutline;
            var pixels = new Color32[64 * 64];
            for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
            {
                var border = x < 2 || x >= 62 || y < 2 || y >= 62;
                pixels[y * 64 + x] = new Color32(255, 255, 255,
                    border ? (byte)220 : (byte)0);
            }
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32,
                false) { name = "Shared automata selection outline" };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _selectionOutline = Sprite.Create(texture,
                new Rect(0f, 0f, 64f, 64f),
                new Vector2(0.5f, 0.5f), 8f);
            return _selectionOutline;
        }

        private static Material SharedArtMaterial(Material spriteMaterial)
        {
            if (_sharedArtMaterial != null) return _sharedArtMaterial;
            // Road artwork (3002) and ground decals (3003) are surfaces, so
            // they must finish before camera-facing automata are blended.
            // A single shared material preserves batching across placements.
            _sharedArtMaterial = new Material(spriteMaterial)
            {
                name = "Shared automata art above ground",
                renderQueue = 3004,
                hideFlags = HideFlags.DontSave
            };
            return _sharedArtMaterial;
        }

        private static Material SharedRecolorMaterial()
        {
            if (_sharedRecolorMaterial != null) return _sharedRecolorMaterial;
            var shader = Resources.Load<Shader>(
                "CityForgeV3/Shaders/AutomataGarmentRecolor") ??
                Shader.Find("CityForgeV3/AutomataGarmentRecolor");
            if (shader == null) return null;
            _sharedRecolorMaterial = new Material(shader)
            {
                name = "Shared automata garment recolor",
                renderQueue = 3004,
                hideFlags = HideFlags.DontSave
            };
            return _sharedRecolorMaterial;
        }

        private static uint Seed(string value)
        {
            var hash = 2166136261u;
            foreach (var c in value ?? "automata")
                hash = (hash ^ c) * 16777619u;
            return hash;
        }
    }
}
