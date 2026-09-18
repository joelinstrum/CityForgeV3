using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // One rotatable PlacedProp contains an entire bed. Its plan is photographed;
    // upright plant cards and a real low rim supply depth at the shallow lot angle.
    internal sealed class GeorgianGardenBed : MonoBehaviour
    {
        private const string BedArt = "CityForgeV3/Garden/GeorgianBedsV01/";
        private const string BorderArt = "CityForgeV3/Garden/GeorgianBorderV01/";
        private static readonly Dictionary<string, Sprite> Sprites = new();
        private static Material _rimMaterial;
        private static Sprite _edgeSprite;
        private readonly List<SpriteRenderer> _flowers = new();
        private readonly List<SpriteRenderer> _shrubs = new();
        private readonly List<SpriteRenderer> _edges = new();
        private SpriteRenderer _plan;
        private Sprite _summerPlan;
        private Sprite _winterPlan;
        private Sprite _summerShrub;
        private Sprite _winterShrub;
        private float _opacity;

        internal static Transform Create(string name, bool wide, float opacity,
            SeasonPreset season)
        {
            var root = new GameObject(name).transform;
            var bed = root.gameObject.AddComponent<GeorgianGardenBed>();
            bed._opacity = opacity;
            bed.Build(root, wide);
            bed.SetSeason(season);
            return root;
        }

        private static Sprite Load(string path, bool plan, bool wide = false)
        {
            if (Sprites.TryGetValue(path, out var cached) && cached != null)
                return cached;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            // Generated plan art has transparent canvas padding. The authored
            // opaque rectangle is cropped into the exact world footprint.
            var rect = plan ? (wide
                ? new Rect(texture.width * (29f / 1536f),
                    texture.height * (152f / 1024f),
                    texture.width * (1479f / 1536f),
                    texture.height * (700f / 1024f))
                : new Rect(texture.width * (55f / 1254f),
                    texture.height * (53f / 1254f),
                    texture.width * (1147f / 1254f),
                    texture.height * (1137f / 1254f)))
                : new Rect(0f, 0f, texture.width, texture.height);
            var pivot = plan ? new Vector2(0.5f, 0.5f) :
                new Vector2(0.5f, 0.018f);
            var sprite = Sprite.Create(texture, rect, pivot,
                plan ? rect.width / (wide ? 6f : 4f) : texture.height,
                0, SpriteMeshType.Tight);
            Sprites[path] = sprite;
            return sprite;
        }

        private void Build(Transform root, bool wide)
        {
            var width = wide ? 6f : 4f;
            var depth = wide ? 3f : 4f;
            var shape = wide ? "rectangle" : "square";
            _summerPlan = Load(BedArt + shape + "-summer", true, wide);
            _winterPlan = Load(BedArt + shape + "-winter", true, wide);
            var flower = Load(BorderArt + "flowers-summer", false);
            _summerShrub = Load(BorderArt + "thicket-summer", false);
            _winterShrub = Load(BorderArt + "thicket-winter", false);
            if (_summerPlan == null || _winterPlan == null || flower == null ||
                _summerShrub == null || _winterShrub == null)
            {
                Debug.LogError("Georgian garden bed art is missing.");
                return;
            }

            var plan = new GameObject("Photographic planting plan").transform;
            plan.SetParent(root, false);
            plan.localPosition = new Vector3(0f, 0.035f, 0f);
            plan.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _plan = plan.gameObject.AddComponent<SpriteRenderer>();
            _plan.sprite = _summerPlan;
            _plan.sortingOrder = 0;
            plan.localScale = new Vector3(width / _summerPlan.bounds.size.x,
                depth / _summerPlan.bounds.size.y, 1f);

            // This low dark lip covers the bright border baked into the plan
            // image without changing the source art or its footprint.
            AddRim(root, "North muted edge", new Vector3(0f, 0.03f,
                depth * 0.5f - 0.08f),
                new Vector3(width - 0.02f, 0.06f, 0.16f));
            AddRim(root, "South muted edge", new Vector3(0f, 0.03f,
                -depth * 0.5f + 0.08f),
                new Vector3(width - 0.02f, 0.06f, 0.16f));
            AddRim(root, "West muted edge", new Vector3(
                -width * 0.5f + 0.08f, 0.03f, 0f),
                new Vector3(0.16f, 0.06f, depth - 0.32f));
            AddRim(root, "East muted edge", new Vector3(
                width * 0.5f - 0.08f, 0.03f, 0f),
                new Vector3(0.16f, 0.06f, depth - 0.32f));
            // The plan is transparent and renders after opaque geometry, so
            // its photographed white trim would otherwise cover the low rim.
            AddEdge(root, "North edge color", 0f,
                depth * 0.5f - 0.09f, width, 0.18f);
            AddEdge(root, "South edge color", 0f,
                -depth * 0.5f + 0.09f, width, 0.18f);
            AddEdge(root, "West edge color",
                -width * 0.5f + 0.09f, 0f, 0.18f, depth - 0.36f);
            AddEdge(root, "East edge color",
                width * 0.5f - 0.09f, 0f, 0.18f, depth - 0.36f);

            // Across the whole plot rather than a single planting row. All
            // offsets are local, so quarter-turn rotation keeps the bed intact.
            var shrubPositions = wide ? new[]
            {
                new Vector2(-2.45f, -0.96f), new Vector2(2.45f, -0.96f),
                new Vector2(-2.45f, 0.96f), new Vector2(2.45f, 0.96f),
                new Vector2(-0.92f, 0f), new Vector2(0.92f, 0f),
                new Vector2(0f, 0.52f)
            } : new[]
            {
                new Vector2(-1.45f, -1.45f), new Vector2(1.45f, -1.45f),
                new Vector2(-1.45f, 1.45f), new Vector2(1.45f, 1.45f),
                new Vector2(-0.87f, 0f), new Vector2(0.87f, 0f),
                new Vector2(0f, -0.84f), new Vector2(0f, 0.84f),
                new Vector2(0f, 0f)
            };
            for (var i = 0; i < shrubPositions.Length; i++)
                AddCross(root, "Shrub " + i, _summerShrub,
                    shrubPositions[i], i == shrubPositions.Length - 1 ? 0.90f :
                    0.63f + (i % 3) * 0.08f, _shrubs);
            var flowerPositions = wide ? new[]
            {
                new Vector2(-1.7f, -0.45f), new Vector2(-0.6f, -0.85f),
                new Vector2(0.6f, -0.85f), new Vector2(1.7f, -0.45f),
                new Vector2(-1.7f, 0.45f), new Vector2(-0.6f, 0.85f),
                new Vector2(0.6f, 0.85f), new Vector2(1.7f, 0.45f)
            } : new[]
            {
                new Vector2(-1.25f, -0.7f), new Vector2(-0.65f, -1.24f),
                new Vector2(0.65f, -1.24f), new Vector2(1.25f, -0.7f),
                new Vector2(-1.25f, 0.7f), new Vector2(-0.65f, 1.24f),
                new Vector2(0.65f, 1.24f), new Vector2(1.25f, 0.7f)
            };
            for (var i = 0; i < flowerPositions.Length; i++)
                AddCross(root, "Flower mass " + i, flower,
                    flowerPositions[i], 0.42f + (i % 3) * 0.03f, _flowers);
        }

        private static void AddRim(Transform parent, string name,
            Vector3 position, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.GetComponent<Collider>().enabled = false;
            if (_rimMaterial == null)
                _rimMaterial = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.26f, 0.25f, 0.21f)
                };
            _rimMaterial.SetFloat("_Glossiness", 0.03f);
            block.GetComponent<Renderer>().sharedMaterial = _rimMaterial;
        }

        private void AddEdge(Transform root, string name, float x, float z,
            float width, float depth)
        {
            if (_edgeSprite == null)
                _edgeSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            var edge = new GameObject(name).transform;
            edge.SetParent(root, false);
            edge.localPosition = new Vector3(x, 0.064f, z);
            edge.localRotation = Quaternion.Euler(90f, 0f, 0f);
            edge.localScale = new Vector3(width, depth, 1f);
            var renderer = edge.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _edgeSprite;
            renderer.color = new Color(0.29f, 0.28f, 0.24f, _opacity);
            renderer.sortingOrder = 1;
            _edges.Add(renderer);
        }

        private static void AddCross(Transform root, string name, Sprite sprite,
            Vector2 position, float size, List<SpriteRenderer> output)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(position.x, 0.045f, position.y);
            for (var side = 0; side < 2; side++)
            {
                var card = new GameObject("Photographic plant " + side);
                card.transform.SetParent(pivot, false);
                card.transform.localRotation = Quaternion.Euler(0f,
                    side == 0 ? -35f : 55f, 0f);
                card.transform.localScale = Vector3.one * size;
                var renderer = card.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 1;
                output.Add(renderer);
            }
        }

        internal void SetSeason(SeasonPreset season)
        {
            for (var i = 0; i < _edges.Count; i++)
                _edges[i].color = new Color(0.29f, 0.28f, 0.24f,
                    _opacity);
            if (_plan != null)
            {
                _plan.sprite = season == SeasonPreset.Winter
                    ? _winterPlan : _summerPlan;
                _plan.color = season switch
                {
                    SeasonPreset.Spring => new Color(0.91f, 1f, 0.94f,
                        _opacity),
                    SeasonPreset.Autumn => new Color(0.84f, 0.77f, 0.66f,
                        _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
            for (var i = 0; i < _shrubs.Count; i++)
            {
                _shrubs[i].sprite = season == SeasonPreset.Winter
                    ? _winterShrub : _summerShrub;
                _shrubs[i].color = season switch
                {
                    SeasonPreset.Spring => new Color(0.91f, 1f, 0.92f,
                        _opacity),
                    SeasonPreset.Autumn => new Color(0.83f, 0.79f, 0.66f,
                        _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
            for (var i = 0; i < _flowers.Count; i++)
            {
                _flowers[i].enabled = season != SeasonPreset.Winter;
                _flowers[i].color = season switch
                {
                    SeasonPreset.Spring => new Color(0.92f, 1f, 0.94f,
                        (i / 2) % 2 == 0 ? 0.7f * _opacity :
                        0.25f * _opacity),
                    SeasonPreset.Autumn => new Color(0.74f, 0.66f, 0.56f,
                        0.55f * _opacity),
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
