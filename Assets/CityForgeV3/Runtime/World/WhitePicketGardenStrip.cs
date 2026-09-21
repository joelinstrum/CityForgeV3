using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    internal enum WhitePicketGardenStyle
    {
        Roses,
        Cottage,
        Mixed,
        Coneflowers,
        Daisies,
        BlackEyedSusans,
        HostaAndFern,
        Clematis,
        FullCottage
    }

    // A straight two-metre fence and its planting on both sides are one prop.
    // Local -Z is the front; the rear planting occupies local +Z.
    public sealed class WhitePicketGardenStrip : MonoBehaviour
    {
        internal const float FenceLengthMeters = 2f;
        internal const float WidthMeters = 2.2f;
        internal const float DepthMeters = 1.8f;
        private const string AgedFenceArt =
            "CityForgeV3/Garden/AgedWhitePicketV01/";
        private const string CottageArt =
            "CityForgeV3/Garden/FoundationPlantingsV01/";
        private const string GeorgianArt =
            "CityForgeV3/Garden/GeorgianBorderV01/";
        private static readonly Dictionary<string, Sprite> Sprites = new();
        private static Mesh _fenceMesh;
        private static Material _fenceMaterial;
        private static Material _fencePreviewMaterial;

        private readonly List<SpriteRenderer> _flowers = new();
        private readonly List<SpriteRenderer> _roses = new();
        private readonly List<SpriteRenderer> _purple = new();
        private readonly List<MeshRenderer> _fenceRenderers = new();
        private NaturalGrassGardenPatch _grass;
        private Sprite _winterShrub;
        private Sprite _summerRose;
        private float _opacity;

        internal static bool TryStyle(string propId,
            out WhitePicketGardenStyle style)
        {
            style = WhitePicketGardenStyle.Roses;
            if (string.Equals(propId,
                    LotWorldController.WhitePicketRoseStripPropId,
                    StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(propId,
                    LotWorldController.WhitePicketCottageStripPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = WhitePicketGardenStyle.Cottage;
            else if (string.Equals(propId,
                    LotWorldController.WhitePicketMixedStripPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = WhitePicketGardenStyle.Mixed;
            else if (string.Equals(propId,
                    LotWorldController.WhitePicketConeflowerStripPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = WhitePicketGardenStyle.Coneflowers;
            else if (string.Equals(propId,
                    LotWorldController.WhitePicketDaisyStripPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = WhitePicketGardenStyle.Daisies;
            else if (string.Equals(propId,
                    LotWorldController.WhitePicketSusanStripPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = WhitePicketGardenStyle.BlackEyedSusans;
            else if (string.Equals(propId,
                    LotWorldController.WhitePicketHostaFernStripPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = WhitePicketGardenStyle.HostaAndFern;
            else if (string.Equals(propId,
                    LotWorldController.WhitePicketClematisStripPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = WhitePicketGardenStyle.Clematis;
            else if (string.Equals(propId,
                    LotWorldController.WhitePicketFullCottageStripPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = WhitePicketGardenStyle.FullCottage;
            else return false;
            return true;
        }

        internal static Transform Create(string name, WhitePicketGardenStyle style,
            float opacity, SeasonPreset season, TimeOfDayPreset timeOfDay,
            Vector3 sunDirection)
        {
            if (!EnsureFenceMaterials()) return null;
            var root = new GameObject(name).transform;
            var strip = root.gameObject.AddComponent<WhitePicketGardenStrip>();
            strip._opacity = opacity;
            if (!strip.Build(style, season, timeOfDay, sunDirection))
            {
                if (Application.isPlaying) Destroy(root.gameObject);
                else DestroyImmediate(root.gameObject);
                return null;
            }
            strip.SetAppearance(season, timeOfDay, sunDirection);
            return root;
        }

        private static bool EnsureFenceMaterials()
        {
            if (_fenceMaterial != null) return true;
            var albedo = Resources.Load<Texture2D>(
                AgedFenceArt + "aged-painted-wood");
            if (albedo == null) return false;
            _fenceMaterial = new Material(Shader.Find(
                "CityForgeV3/GardenPropPBR"))
            {
                name = "Garden aged ivory picket fence",
                mainTexture = albedo,
                // The texture already carries its age, grain, and gray wear.
                // Keep the shared paint tint near white so a shaded district
                // fence still reads as aged ivory rather than neutral gray.
                color = new Color(.98f, .97f, .92f),
                enableInstancing = true
            };
            _fenceMaterial.SetFloat("_Metallic", 0f);
            _fenceMaterial.SetFloat("_Glossiness", .025f);
            _fencePreviewMaterial = new Material(_fenceMaterial)
            {
                name = "Garden white picket fence preview"
            };
            _fencePreviewMaterial.SetFloat("_Mode", 3f);
            _fencePreviewMaterial.SetInt("_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _fencePreviewMaterial.SetInt("_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _fencePreviewMaterial.SetInt("_ZWrite", 0);
            _fencePreviewMaterial.EnableKeyword("_ALPHABLEND_ON");
            _fencePreviewMaterial.renderQueue = 3000;
            return true;
        }

        private static Sprite Load(string path)
        {
            if (Sprites.TryGetValue(path, out var cached) && cached != null)
                return cached;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(.5f, .018f), texture.height, 0,
                SpriteMeshType.Tight);
            Sprites[path] = sprite;
            return sprite;
        }

        private static Sprite LoadCrop(string path, string key,
            float x, float y, float width, float height)
        {
            var cacheKey = path + "#" + key;
            if (Sprites.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            var rect = new Rect(texture.width * x, texture.height * y,
                texture.width * width, texture.height * height);
            var sprite = Sprite.Create(texture, rect,
                new Vector2(.5f, .018f), rect.height, 0, SpriteMeshType.Tight);
            Sprites[cacheKey] = sprite;
            return sprite;
        }

        private bool Build(WhitePicketGardenStyle style,
            SeasonPreset season, TimeOfDayPreset timeOfDay,
            Vector3 sunDirection)
        {
            _summerRose = Load(CottageArt + "rose-bush-summer");
            var purple = Load(CottageArt + "purple-phlox-summer");
            var flowers = Load(GeorgianArt + "flowers-summer");
            var cottagePlanting = Load(AgedFenceArt +
                "cottage-planting-summer");
            var cottagePath = AgedFenceArt + "cottage-planting-summer";
            var coneflowers = LoadCrop(cottagePath, "coneflowers",
                0f, 0f, .34f, 1f);
            var daisies = LoadCrop(cottagePath, "daisies",
                .28f, 0f, .28f, 1f);
            var susans = LoadCrop(cottagePath, "susans",
                .60f, 0f, .25f, 1f);
            var hostaFern = LoadCrop(cottagePath, "hosta-fern",
                0f, 0f, 1f, .53f);
            var clematis = LoadCrop(cottagePath, "clematis",
                .40f, 0f, .22f, 1f);
            _winterShrub = Load(GeorgianArt + "thicket-winter");
            if (_summerRose == null || purple == null || flowers == null ||
                cottagePlanting == null || coneflowers == null ||
                daisies == null || susans == null || hostaFern == null ||
                clematis == null || _winterShrub == null) return false;

            var grass = NaturalGrassGardenPatch.Create("Natural Grass under fence",
                WidthMeters, DepthMeters, _opacity, season, timeOfDay,
                sunDirection, false);
            grass.SetParent(transform, false);
            _grass = grass.GetComponent<NaturalGrassGardenPatch>();
            if (_grass == null) return false;

            var fence = new GameObject("Aged white picket fence — 2 m");
            fence.transform.SetParent(transform, false);
            fence.AddComponent<MeshFilter>().sharedMesh = FenceMesh();
            var fenceRenderer = fence.AddComponent<MeshRenderer>();
            fenceRenderer.sharedMaterial = _fenceMaterial;
            fenceRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.On;
            fenceRenderer.receiveShadows = true;
            _fenceRenderers.Add(fenceRenderer);

            // Dense planting cards knit the fence into the bed instead of
            // leaving a pristine prop between two sparse rows.
            if (style == WhitePicketGardenStyle.Roses ||
                style == WhitePicketGardenStyle.Cottage ||
                style == WhitePicketGardenStyle.Mixed ||
                style == WhitePicketGardenStyle.FullCottage)
                AddBothSides("Dense cottage planting", cottagePlanting,
                    .69f, .35f, _flowers, false);
            else
                AddBothSides("Hosta fern groundcover", hostaFern,
                    .35f, .48f, _flowers, false);

            switch (style)
            {
                case WhitePicketGardenStyle.Roses:
                    for (var i = 0; i < 3; i++)
                    {
                        var x = (i - 1) * .61f;
                        AddCross("Front rose " + i, _summerRose,
                            new Vector3(x, .025f, -.48f), .64f, _roses);
                        AddCross("Rear rose " + i, _summerRose,
                            new Vector3(x, .025f, .48f), .70f, _roses);
                    }
                    break;
                case WhitePicketGardenStyle.Cottage:
                    for (var i = 0; i < 4; i++)
                        AddCross("Rear purple flowers " + i, purple,
                            new Vector3((i - 1.5f) * .45f, .025f, .48f),
                            .95f + (i % 2) * .12f, _purple);
                    for (var i = 0; i < 3; i++)
                        AddCross("Front purple flowers " + i, purple,
                            new Vector3((i - 1) * .60f, .025f, -.47f),
                            .62f + (i % 2) * .05f, _purple);
                    break;
                case WhitePicketGardenStyle.Mixed:
                    AddCross("Rear rose left", _summerRose,
                        new Vector3(-.55f, .025f, .50f), .75f, _roses);
                    AddCross("Rear rose right", _summerRose,
                        new Vector3(.55f, .025f, .50f), .75f, _roses);
                    AddCross("Front rose center", _summerRose,
                        new Vector3(0f, .025f, -.47f), .68f, _roses);
                    AddCross("Rear purple center", purple,
                        new Vector3(0f, .025f, .52f), 1.02f, _purple);
                    break;
                case WhitePicketGardenStyle.Coneflowers:
                    AddFeatureClumps("Coneflowers", coneflowers, .82f);
                    break;
                case WhitePicketGardenStyle.Daisies:
                    AddFeatureClumps("Daisies", daisies, .78f);
                    break;
                case WhitePicketGardenStyle.BlackEyedSusans:
                    AddFeatureClumps("Black-eyed Susans", susans, .80f);
                    break;
                case WhitePicketGardenStyle.HostaAndFern:
                    AddBothSides("Hosta fern border", hostaFern,
                        .39f, .38f, _flowers, false);
                    AddCross("Rear purple accent", purple,
                        new Vector3(0f, .025f, .52f), .78f, _purple);
                    break;
                case WhitePicketGardenStyle.Clematis:
                    for (var i = 0; i < 3; i++)
                    {
                        var x = (i - 1) * .60f;
                        AddCross("Fence clematis " + i, clematis,
                            new Vector3(x, .025f, 0f), .96f, _purple);
                    }
                    break;
                case WhitePicketGardenStyle.FullCottage:
                    AddCross("Tall purple left", purple,
                        new Vector3(-.53f, .025f, .45f), .90f, _purple);
                    AddCross("Tall purple right", purple,
                        new Vector3(.53f, .025f, .45f), .90f, _purple);
                    AddCross("Rose center", _summerRose,
                        new Vector3(0f, .025f, -.43f), .67f, _roses);
                    break;
            }
            for (var i = 0; i < 5; i++)
            {
                var x = (i - 2) * .40f;
                AddCross("Front low flowers " + i, flowers,
                    new Vector3(x, .025f, -.71f), .27f, _flowers);
                AddCross("Rear low flowers " + i, flowers,
                    new Vector3(x, .025f, .71f), .27f, _flowers);
            }
            return true;
        }

        private static Mesh FenceMesh()
        {
            if (_fenceMesh != null) return _fenceMesh;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            const int pickets = 11;
            for (var i = 0; i < pickets; i++)
            {
                var x = -.78f + i * 1.56f / (pickets - 1);
                var height = .90f + .035f * Mathf.Sin(i * 2.17f) +
                    .018f * Mathf.Cos(i * 4.31f);
                var lean = .012f * Mathf.Sin(i * 1.73f);
                AddBox(vertices, uv, triangles,
                    new Vector3(x + lean, (height - .10f) * .5f, 0f),
                    new Vector3(.095f, height - .10f, .065f));
                AddPicketTip(vertices, uv, triangles,
                    new Vector3(x + lean, 0f, 0f), .0475f, .0325f,
                    height - .10f, height);
            }
            foreach (var y in new[] { .28f, .62f })
                AddBox(vertices, uv, triangles, new Vector3(0f, y, .04f),
                    new Vector3(1.66f, .075f, .075f));
            AddPost(vertices, uv, triangles, -.91f, .99f);
            AddPost(vertices, uv, triangles, .91f, 1.03f);
            _fenceMesh = new Mesh { name = "Organic aged picket fence 2 m" };
            _fenceMesh.SetVertices(vertices);
            _fenceMesh.SetUVs(0, uv);
            _fenceMesh.SetTriangles(triangles, 0);
            _fenceMesh.RecalculateNormals();
            _fenceMesh.RecalculateBounds();
            return _fenceMesh;
        }

        private static void AddPost(List<Vector3> vertices, List<Vector2> uv,
            List<int> triangles, float x, float height)
        {
            AddBox(vertices, uv, triangles,
                new Vector3(x, (height - .10f) * .5f, 0f),
                new Vector3(.15f, height - .10f, .14f));
            var baseY = height - .10f;
            var corners = new[]
            {
                new Vector3(x - .095f, baseY, -.095f),
                new Vector3(x + .095f, baseY, -.095f),
                new Vector3(x + .095f, baseY, .095f),
                new Vector3(x - .095f, baseY, .095f)
            };
            var apex = new Vector3(x, height + .045f, 0f);
            for (var side = 0; side < 4; side++)
                AddTriangle(vertices, uv, triangles, corners[side],
                    corners[(side + 1) % 4], apex);
        }

        private static void AddPicketTip(List<Vector3> vertices,
            List<Vector2> uv, List<int> triangles, Vector3 center,
            float halfX, float halfZ, float baseY, float height)
        {
            var corners = new[]
            {
                center + new Vector3(-halfX, baseY, -halfZ),
                center + new Vector3(halfX, baseY, -halfZ),
                center + new Vector3(halfX, baseY, halfZ),
                center + new Vector3(-halfX, baseY, halfZ)
            };
            var apex = center + Vector3.up * height;
            for (var side = 0; side < 4; side++)
                AddTriangle(vertices, uv, triangles, corners[side],
                    corners[(side + 1) % 4], apex);
        }

        private static void AddBox(List<Vector3> vertices, List<Vector2> uv,
            List<int> triangles, Vector3 center, Vector3 size)
        {
            var min = center - size * .5f;
            var max = center + size * .5f;
            var a = new Vector3(min.x, min.y, min.z);
            var b = new Vector3(max.x, min.y, min.z);
            var c = new Vector3(max.x, min.y, max.z);
            var d = new Vector3(min.x, min.y, max.z);
            var e = new Vector3(min.x, max.y, min.z);
            var f = new Vector3(max.x, max.y, min.z);
            var g = new Vector3(max.x, max.y, max.z);
            var h = new Vector3(min.x, max.y, max.z);
            AddQuad(vertices, uv, triangles, a, b, f, e);
            AddQuad(vertices, uv, triangles, c, d, h, g);
            AddQuad(vertices, uv, triangles, d, a, e, h);
            AddQuad(vertices, uv, triangles, b, c, g, f);
            AddQuad(vertices, uv, triangles, e, f, g, h);
            AddQuad(vertices, uv, triangles, d, c, b, a);
        }

        private static void AddQuad(List<Vector3> vertices, List<Vector2> uv,
            List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var index = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            uv.Add(Vector2.zero); uv.Add(Vector2.right);
            uv.Add(Vector2.one); uv.Add(Vector2.up);
            triangles.Add(index); triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index); triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        private static void AddTriangle(List<Vector3> vertices,
            List<Vector2> uv, List<int> triangles, Vector3 a, Vector3 b,
            Vector3 c)
        {
            var index = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.up);
            triangles.Add(index); triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        private void AddCross(string name, Sprite sprite, Vector3 position,
            float height, List<SpriteRenderer> output)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(transform, false);
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

        private void AddBothSides(string name, Sprite sprite, float height,
            float z, List<SpriteRenderer> output, bool crossed)
        {
            if (crossed)
            {
                AddCross("Front " + name, sprite,
                    new Vector3(0f, .025f, -z), height, output);
                AddCross("Rear " + name, sprite,
                    new Vector3(0f, .025f, z), height, output);
            }
            else
            {
                AddSingle("Front " + name, sprite,
                    new Vector3(0f, .025f, -z), height, output);
                AddSingle("Rear " + name, sprite,
                    new Vector3(0f, .025f, z), height, output);
            }
        }

        private void AddFeatureClumps(string name, Sprite sprite, float height)
        {
            for (var i = 0; i < 3; i++)
            {
                var x = (i - 1) * .62f;
                AddCross("Front " + name + " " + i, sprite,
                    new Vector3(x, .025f, -.46f),
                    height * (i == 1 ? 1f : .88f), _purple);
                AddCross("Rear " + name + " " + i, sprite,
                    new Vector3(x, .025f, .46f),
                    height * (i == 1 ? 1.04f : .92f), _purple);
            }
        }

        private void AddSingle(string name, Sprite sprite, Vector3 position,
            float height, List<SpriteRenderer> output)
        {
            var card = new GameObject(name);
            card.transform.SetParent(transform, false);
            card.transform.localPosition = position;
            card.transform.localScale = Vector3.one * height;
            var renderer = card.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            output.Add(renderer);
        }

        public void SetAppearance(SeasonPreset season,
            TimeOfDayPreset timeOfDay, Vector3 sunDirection)
        {
            _grass?.SetOpacity(_opacity, season, timeOfDay, sunDirection);
            var block = new MaterialPropertyBlock();
            var color = _fenceMaterial.color;
            color.a = _opacity;
            block.SetColor("_Color", color);
            foreach (var renderer in _fenceRenderers)
            {
                renderer.sharedMaterial = _opacity < .99f
                    ? _fencePreviewMaterial : _fenceMaterial;
                renderer.SetPropertyBlock(block);
            }
            foreach (var renderer in _roses)
            {
                renderer.sprite = season == SeasonPreset.Winter
                    ? _winterShrub : _summerRose;
                renderer.color = season switch
                {
                    SeasonPreset.Winter => new Color(.83f, .86f, .82f,
                        _opacity),
                    SeasonPreset.Autumn => new Color(.86f, .77f, .69f,
                        .85f * _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
            foreach (var renderer in _flowers)
            {
                renderer.enabled = season != SeasonPreset.Winter;
                renderer.color = new Color(1f, 1f, 1f, _opacity);
            }
            foreach (var renderer in _purple)
            {
                renderer.enabled = season != SeasonPreset.Winter;
                renderer.color = new Color(1f, 1f, 1f, _opacity);
            }
        }

        public void SetOpacity(float opacity, SeasonPreset season,
            TimeOfDayPreset timeOfDay, Vector3 sunDirection)
        {
            _opacity = Mathf.Clamp01(opacity);
            SetAppearance(season, timeOfDay, sunDirection);
        }
    }
}
