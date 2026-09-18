using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // A rotatable assembly of crossed photographic plant cards on one rectangular footprint.
    // Placement and rotation remain owned by the existing PlacedProp model.
    internal sealed class GeorgianGardenBorder : MonoBehaviour
    {
        private const string ResourceRoot = "CityForgeV3/Garden/GeorgianBorderV01/";
        private static readonly Dictionary<string, Sprite> Sprites = new();
        private static Material _soilMaterial;
        private static Material _rimMaterial;
        private readonly List<SpriteRenderer> _flowers = new();
        private readonly List<SpriteRenderer> _thicket = new();
        private SpriteRenderer _canopy;
        private Sprite _summerThicket;
        private Sprite _winterThicket;
        private float _opacity;

        internal static Transform Create(string name, float opacity, SeasonPreset season)
        {
            var root = new GameObject(name).transform;
            var view = root.gameObject.AddComponent<GeorgianGardenBorder>();
            view._opacity = opacity;
            view.Build(root);
            view.SetSeason(season);
            return root;
        }

        private static Sprite Load(string name)
        {
            if (Sprites.TryGetValue(name, out var sprite) && sprite != null)
                return sprite;
            var texture = Resources.Load<Texture2D>(ResourceRoot + name);
            if (texture == null) return null;
            var overhead = name == "planting-canopy-summer";
            sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                overhead ? new Vector2(0.5f, 0.5f) :
                    new Vector2(0.5f, 0.018f),
                overhead ? texture.width / 4f : texture.height,
                0, SpriteMeshType.Tight);
            Sprites[name] = sprite;
            return sprite;
        }

        private void Build(Transform root)
        {
            var flowers = Load("flowers-summer");
            _summerThicket = Load("thicket-summer");
            _winterThicket = Load("thicket-winter");
            if (flowers == null || _summerThicket == null || _winterThicket == null)
            {
                Debug.LogError("Georgian garden border cutouts are missing.");
                return;
            }
            AddBlock(root, "Contained earth bed", new Vector3(0f, -0.035f, 0f),
                new Vector3(4f, 0.07f, 1.5f), false);
            AddBlock(root, "Front stone edging", new Vector3(0f, 0.045f, -0.72f),
                new Vector3(4f, 0.09f, 0.07f), true);
            AddBlock(root, "Back stone edging", new Vector3(0f, 0.045f, 0.72f),
                new Vector3(4f, 0.09f, 0.07f), true);
            AddBlock(root, "Left stone edging", new Vector3(-1.95f, 0.045f, 0f),
                new Vector3(0.07f, 0.09f, 1.35f), true);
            AddBlock(root, "Right stone edging", new Vector3(1.95f, 0.045f, 0f),
                new Vector3(0.07f, 0.09f, 1.35f), true);
            var canopySprite = Load("planting-canopy-summer");
            if (canopySprite != null)
            {
                var canopy = new GameObject("Overhead planted rows").transform;
                canopy.SetParent(root, false);
                canopy.localPosition = new Vector3(0f, 0.075f, 0f);
                canopy.localRotation = Quaternion.Euler(90f, 0f, 0f);
                canopy.localScale = new Vector3(
                    4f / canopySprite.bounds.size.x,
                    1.5f / canopySprite.bounds.size.y, 1f);
                _canopy = canopy.gameObject.AddComponent<SpriteRenderer>();
                _canopy.sprite = canopySprite;
            }
            for (var index = 0; index < 5; index++)
            {
                var x = -1.52f + index * 0.76f;
                AddCross(root, "Thicket " + index, _summerThicket,
                    new Vector3(x, 0.02f, 0.26f + (index % 2) * 0.05f),
                    0.86f + (index % 3) * 0.04f, _thicket);
            }
            for (var index = 0; index < 7; index++)
            {
                var x = -1.63f + index * (3.26f / 6f);
                AddCross(root, "Flower " + index, flowers,
                    new Vector3(x, 0.03f, -0.36f + (index % 2) * 0.035f),
                    0.41f + (index % 3) * 0.025f, _flowers);
            }
        }

        private static void AddBlock(Transform parent, string name,
            Vector3 position, Vector3 scale, bool rim)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.GetComponent<Collider>().enabled = false;
            var material = rim ? _rimMaterial : _soilMaterial;
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"))
                {
                    color = rim ? new Color(0.27f, 0.24f, 0.20f) :
                        new Color(0.21f, 0.18f, 0.13f)
                };
                if (rim) _rimMaterial = material;
                else _soilMaterial = material;
            }
            block.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void AddCross(Transform parent, string name, Sprite sprite,
            Vector3 position, float height, List<SpriteRenderer> output)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            for (var side = 0; side < 2; side++)
            {
                var card = new GameObject("Photographic foliage " + side);
                card.transform.SetParent(pivot, false);
                card.transform.localRotation = Quaternion.Euler(0f,
                    side == 0 ? -35f : 55f, 0f);
                card.transform.localScale = Vector3.one * height;
                var renderer = card.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                output.Add(renderer);
            }
        }

        internal void SetSeason(SeasonPreset season)
        {
            if (_canopy != null)
            {
                _canopy.enabled = season != SeasonPreset.Winter;
                _canopy.color = season switch
                {
                    SeasonPreset.Spring => new Color(0.9f, 1f, 0.9f,
                        0.75f * _opacity),
                    SeasonPreset.Autumn => new Color(0.87f, 0.78f, 0.64f,
                        0.8f * _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
            for (var index = 0; index < _thicket.Count; index++)
            {
                var renderer = _thicket[index];
                renderer.sprite = season == SeasonPreset.Winter
                    ? _winterThicket : _summerThicket;
                renderer.color = season switch
                {
                    SeasonPreset.Spring => new Color(0.92f, 1f, 0.91f, _opacity),
                    SeasonPreset.Autumn => new Color(0.86f, 0.82f, 0.67f, _opacity),
                    SeasonPreset.Winter => new Color(0.92f, 0.94f, 0.96f, _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
            for (var index = 0; index < _flowers.Count; index++)
            {
                var renderer = _flowers[index];
                renderer.enabled = season != SeasonPreset.Winter;
                renderer.color = season switch
                {
                    SeasonPreset.Spring => new Color(0.88f, 0.97f, 0.9f,
                        index / 2 % 2 == 0 ? _opacity : 0.35f * _opacity),
                    SeasonPreset.Autumn => new Color(0.73f, 0.65f, 0.57f,
                        0.65f * _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
        }

        internal void SetOpacity(float opacity, SeasonPreset season)
        {
            _opacity = opacity;
            SetSeason(season);
        }
    }
}
