using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    internal enum FoundationGardenStyle
    {
        RearHedge,
        FramedHedges,
        OpenRoses,
        PicketRoseAndShrubs,
        PicketCottageFlowers,
        PicketRosePair,
        PicketRoundedShrubs
    }

    // A shallow house-front planting is one rotatable, saved PlacedProp.
    // Local +Z is the house side; the flowers face local -Z.
    public sealed class FoundationGardenBed : MonoBehaviour
    {
        internal const float WidthMeters = 2f;
        internal const float DepthMeters = 1f;
        private const string Art = "CityForgeV3/Garden/GeorgianBorderV01/";
        private const string CottageArt =
            "CityForgeV3/Garden/FoundationPlantingsV01/";
        private static readonly Dictionary<string, Sprite> Sprites = new();
        private static readonly Dictionary<bool, Mesh> HedgeMeshes = new();
        private static Mesh _roundedShrubMesh;
        private static Mesh _picketMesh;
        private static Material _earth;
        private static Material _earthPreview;
        private static Material _edge;
        private static Material _edgePreview;
        private static Material _picketMaterial;
        private static Material _picketPreview;
        private static Material _roundMaterial;
        private static Material _roundPreview;

        private readonly List<SpriteRenderer> _flowers = new();
        private readonly List<SpriteRenderer> _shrubs = new();
        private readonly List<SpriteRenderer> _roses = new();
        private readonly List<SpriteRenderer> _purpleFlowers = new();
        private readonly List<Renderer> _ground = new();
        private readonly List<MeshRenderer> _roundedShrubs = new();
        private SpriteRenderer _plan;
        private MeshRenderer _hedge;
        private MeshRenderer _picket;
        private Sprite _summerShrub;
        private Sprite _winterShrub;
        private MaterialPropertyBlock _hedgeProperties;
        private MaterialPropertyBlock _groundProperties;
        private FoundationGardenStyle _style;
        private float _opacity;

        internal static bool TryStyle(string propId, out FoundationGardenStyle style)
        {
            style = FoundationGardenStyle.RearHedge;
            if (string.Equals(propId,
                    LotWorldController.FoundationRearHedgePropId,
                    StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(propId,
                    LotWorldController.FoundationFramedHedgePropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                style = FoundationGardenStyle.FramedHedges;
                return true;
            }
            if (string.Equals(propId,
                    LotWorldController.FoundationOpenRosesPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = FoundationGardenStyle.OpenRoses;
            else if (string.Equals(propId,
                    LotWorldController.FoundationPicketRoseShrubsPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = FoundationGardenStyle.PicketRoseAndShrubs;
            else if (string.Equals(propId,
                    LotWorldController.FoundationPicketCottageFlowersPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = FoundationGardenStyle.PicketCottageFlowers;
            else if (string.Equals(propId,
                    LotWorldController.FoundationPicketRosePairPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = FoundationGardenStyle.PicketRosePair;
            else if (string.Equals(propId,
                    LotWorldController.FoundationPicketRoundedShrubsPropId,
                    StringComparison.OrdinalIgnoreCase))
                style = FoundationGardenStyle.PicketRoundedShrubs;
            else return false;
            return true;
        }

        internal static Transform Create(string name, bool framed,
            float opacity, SeasonPreset season)
        {
            return Create(name, framed ? FoundationGardenStyle.FramedHedges :
                FoundationGardenStyle.RearHedge, opacity, season);
        }

        internal static Transform Create(string name, FoundationGardenStyle style,
            float opacity, SeasonPreset season)
        {
            if (GeorgianClippedHedgeGarden.LeafMaterial(false) == null)
                return null;
            var root = new GameObject(name).transform;
            var bed = root.gameObject.AddComponent<FoundationGardenBed>();
            bed._style = style;
            bed._opacity = opacity;
            if (!bed.Build())
            {
                if (Application.isPlaying) Destroy(root.gameObject);
                else DestroyImmediate(root.gameObject);
                return null;
            }
            bed.SetSeason(season);
            return root;
        }

        private static Sprite Load(string name, bool overhead = false)
        {
            if (Sprites.TryGetValue(name, out var cached) && cached != null)
                return cached;
            var cottage = name == "rose-bush-summer" ||
                name == "purple-phlox-summer";
            var sourceName = name == "planting-canopy-flowers"
                ? "planting-canopy-summer" : name;
            var texture = Resources.Load<Texture2D>((cottage ? CottageArt : Art) +
                sourceName);
            if (texture == null) return null;
            // The source canopy has dark canvas padding. Crop to its planted
            // rows before fitting it to the small foundation footprint.
            var rect = name == "planting-canopy-flowers"
                ? new Rect(texture.width * .035f, texture.height * .055f,
                    texture.width * .93f, texture.height * .43f)
                : overhead
                    ? new Rect(texture.width * .035f, texture.height * .055f,
                        texture.width * .93f, texture.height * .78f)
                    : new Rect(0f, 0f, texture.width, texture.height);
            var sprite = Sprite.Create(texture, rect,
                overhead ? new Vector2(.5f, .5f) : new Vector2(.5f, .018f),
                overhead ? texture.width / WidthMeters : texture.height,
                0, SpriteMeshType.Tight);
            Sprites[name] = sprite;
            return sprite;
        }

        private static void EnsureGroundMaterials()
        {
            if (_earth != null) return;
            _earth = MakeGround("Foundation planting earth",
                new Color(.20f, .18f, .14f));
            _edge = MakeGround("Foundation muted edge",
                new Color(.27f, .25f, .21f));
            _picketMaterial = MakeGround("Weathered garden pickets",
                new Color(.48f, .46f, .40f));
            _roundMaterial = MakeGround("Rounded boxwood leaves", Color.white);
            _roundMaterial.mainTexture = Resources.Load<Texture2D>(
                LowPolyBoxwoodHedge.FoliageResource);
            _earthPreview = MakePreview(_earth);
            _edgePreview = MakePreview(_edge);
            _picketPreview = MakePreview(_picketMaterial);
            _roundPreview = MakePreview(_roundMaterial);
        }

        private static Material MakeGround(string name, Color color)
        {
            var material = new Material(Shader.Find("Standard"))
            {
                name = name, color = color, enableInstancing = true
            };
            material.SetFloat("_Glossiness", .03f);
            return material;
        }

        private static Material MakePreview(Material source)
        {
            var material = new Material(source) { name = source.name + " preview" };
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private bool Build()
        {
            var cottage = _style >= FoundationGardenStyle.OpenRoses;
            var flowers = Load("flowers-summer");
            _summerShrub = Load("thicket-summer");
            _winterShrub = Load("thicket-winter");
            var canopy = Load(cottage ? "planting-canopy-flowers" :
                "planting-canopy-summer", true);
            var roses = cottage ? Load("rose-bush-summer") : null;
            var purple = cottage ? Load("purple-phlox-summer") : null;
            if (flowers == null || _summerShrub == null ||
                _winterShrub == null || canopy == null ||
                (cottage && (roses == null || purple == null)))
            {
                Debug.LogError("Foundation garden planting art is missing.");
                return false;
            }
            EnsureGroundMaterials();
            AddGround("Dark planting earth", new Vector3(0f, -.017f, 0f),
                new Vector3(WidthMeters, .034f, DepthMeters), false);
            AddGround("Front muted edge", new Vector3(0f, .025f, -.475f),
                new Vector3(WidthMeters, .05f, .05f), true);
            AddGround("Back muted edge", new Vector3(0f, .025f, .475f),
                new Vector3(WidthMeters, .05f, .05f), true);
            AddGround("Left muted edge", new Vector3(-.975f, .025f, 0f),
                new Vector3(.05f, .05f, .90f), true);
            AddGround("Right muted edge", new Vector3(.975f, .025f, 0f),
                new Vector3(.05f, .05f, .90f), true);

            var plan = new GameObject("Photographic planting plan").transform;
            plan.SetParent(transform, false);
            plan.localPosition = new Vector3(0f, .008f, 0f);
            plan.localRotation = Quaternion.Euler(90f, 0f, 0f);
            plan.localScale = new Vector3(1.90f / canopy.bounds.size.x,
                (cottage ? .74f : .88f) / canopy.bounds.size.y, 1f);
            if (cottage) plan.localPosition += new Vector3(0f, 0f, -.09f);
            _plan = plan.gameObject.AddComponent<SpriteRenderer>();
            _plan.sprite = canopy;

            if (cottage)
                BuildCottage(roses, purple, flowers);
            else
            {
                var framed = _style == FoundationGardenStyle.FramedHedges;
                var hedge = new GameObject(framed ? "Framing clipped hedges" :
                    "Rear clipped hedge");
                hedge.transform.SetParent(transform, false);
                hedge.AddComponent<MeshFilter>().sharedMesh = HedgeMesh(framed);
                _hedge = hedge.AddComponent<MeshRenderer>();
                _hedge.shadowCastingMode = ShadowCastingMode.On;
                _hedge.receiveShadows = true;
                if (framed)
                {
                    for (var i = 0; i < 3; i++)
                        AddCross("Rear shrub " + i, _summerShrub,
                            new Vector3((i - 1) * .48f, .012f, .22f),
                            .39f + (i % 2) * .05f, _shrubs);
                }
                for (var i = 0; i < 5; i++)
                    AddCross("Front flowers " + i, flowers,
                        new Vector3((i - 2) * (framed ? .34f : .42f), .012f,
                            -.20f + (i % 2) * .045f),
                        .41f + (i % 3) * .025f, _flowers);
            }
            return true;
        }

        private void BuildCottage(Sprite roses, Sprite purple, Sprite flowers)
        {
            if (_style != FoundationGardenStyle.OpenRoses)
            {
                var fence = new GameObject("Weathered picket enclosure");
                fence.transform.SetParent(transform, false);
                fence.AddComponent<MeshFilter>().sharedMesh = PicketMesh();
                _picket = fence.AddComponent<MeshRenderer>();
                _picket.shadowCastingMode = ShadowCastingMode.On;
            }

            switch (_style)
            {
                case FoundationGardenStyle.OpenRoses:
                    for (var i = 0; i < 3; i++)
                        AddCross("Rose bush " + i, roses,
                            new Vector3((i - 1) * .56f, .015f, .12f),
                            i == 1 ? .78f : .70f, _roses);
                    break;
                case FoundationGardenStyle.PicketRoseAndShrubs:
                    AddRoundedShrub(-.67f, .14f, .46f, .48f);
                    AddRoundedShrub(.67f, .14f, .46f, .48f);
                    AddCross("Rose bush center", roses,
                        new Vector3(0f, .015f, .09f), .74f, _roses);
                    AddCross("Purple accent left", purple,
                        new Vector3(-.33f, .015f, .14f), .55f, _purpleFlowers);
                    AddCross("Purple accent right", purple,
                        new Vector3(.33f, .015f, .14f), .55f, _purpleFlowers);
                    break;
                case FoundationGardenStyle.PicketCottageFlowers:
                    for (var i = 0; i < 4; i++)
                        AddCross("Tall purple flowers " + i, purple,
                            new Vector3((i - 1.5f) * .47f, .015f, .13f),
                            i % 2 == 0 ? .76f : .86f, _purpleFlowers);
                    break;
                case FoundationGardenStyle.PicketRosePair:
                    AddCross("Rose bush left", roses,
                        new Vector3(-.45f, .015f, .08f), .73f, _roses);
                    AddCross("Rose bush right", roses,
                        new Vector3(.45f, .015f, .08f), .73f, _roses);
                    AddRoundedShrub(0f, .20f, .48f, .45f);
                    break;
                case FoundationGardenStyle.PicketRoundedShrubs:
                    for (var i = 0; i < 3; i++)
                        AddRoundedShrub((i - 1) * .58f, .17f,
                            i == 1 ? .44f : .49f, i == 1 ? .43f : .50f);
                    AddCross("Purple accent left", purple,
                        new Vector3(-.32f, .015f, -.07f), .52f,
                        _purpleFlowers);
                    AddCross("Purple accent right", purple,
                        new Vector3(.32f, .015f, -.07f), .52f,
                        _purpleFlowers);
                    break;
            }
            var count = _style == FoundationGardenStyle.OpenRoses ? 4 : 5;
            for (var i = 0; i < count; i++)
                AddCross("Front flowers " + i, flowers,
                    new Vector3((i - (count - 1) * .5f) * .38f,
                        .012f, -.26f + (i % 2) * .03f),
                    .30f + (i % 3) * .02f, _flowers);
        }

        private void AddGround(string name, Vector3 position,
            Vector3 scale, bool edge)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(transform, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.GetComponent<Collider>().enabled = false;
            var renderer = block.GetComponent<Renderer>();
            renderer.sharedMaterial = edge ? _edge : _earth;
            _ground.Add(renderer);
        }

        private static Mesh HedgeMesh(bool framed)
        {
            if (HedgeMeshes.TryGetValue(framed, out var cached) && cached != null)
                return cached;
            var builder = new GeorgianClippedHedgeGarden.HedgeMeshBuilder();
            if (framed)
            {
                builder.AddBox(-.79f, .08f, .30f, .72f, .40f);
                builder.AddBox(.79f, .08f, .30f, .72f, .40f);
            }
            else
                builder.AddBox(0f, .32f, 1.88f, .24f, .42f);
            var mesh = builder.Finish();
            mesh.name = framed ? "Foundation framed hedges" :
                "Foundation rear hedge";
            HedgeMeshes[framed] = mesh;
            return mesh;
        }

        private void AddRoundedShrub(float x, float z, float width,
            float height)
        {
            var shrub = new GameObject("Rounded clipped shrub");
            shrub.transform.SetParent(transform, false);
            shrub.transform.localPosition = new Vector3(x, .008f, z);
            shrub.transform.localScale = new Vector3(width, height,
                width * .88f);
            shrub.AddComponent<MeshFilter>().sharedMesh = RoundedShrubMesh();
            var renderer = shrub.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            _roundedShrubs.Add(renderer);
        }

        private static Mesh RoundedShrubMesh()
        {
            if (_roundedShrubMesh != null) return _roundedShrubMesh;
            const int segments = 12;
            const int rings = 7;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (var ring = 0; ring <= rings; ring++)
            {
                var theta = Mathf.PI * ring / rings;
                var y = .5f + .5f * Mathf.Cos(theta);
                for (var segment = 0; segment <= segments; segment++)
                {
                    var angle = Mathf.PI * 2f * segment / segments;
                    var irregular = 1f + .13f * Mathf.Sin(segment * 2.7f +
                        ring * 1.6f);
                    var radius = .5f * Mathf.Sin(theta) * irregular;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, y,
                        Mathf.Sin(angle) * radius));
                    uv.Add(new Vector2(segment / (float)segments,
                        ring / (float)rings));
                    if (ring == rings || segment == segments) continue;
                    var a = ring * (segments + 1) + segment;
                    var b = a + segments + 1;
                    triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                    triangles.Add(a + 1); triangles.Add(b + 1);
                    triangles.Add(b);
                }
            }
            _roundedShrubMesh = new Mesh
            {
                name = "Low-poly rounded garden shrub"
            };
            _roundedShrubMesh.SetVertices(vertices);
            _roundedShrubMesh.SetUVs(0, uv);
            _roundedShrubMesh.SetTriangles(triangles, 0);
            _roundedShrubMesh.RecalculateNormals();
            _roundedShrubMesh.RecalculateBounds();
            return _roundedShrubMesh;
        }

        private static Mesh PicketMesh()
        {
            if (_picketMesh != null) return _picketMesh;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            AddPicketRun(vertices, uv, triangles, true, -.45f,
                1.90f, .26f);
            AddPicketRun(vertices, uv, triangles, true, .45f,
                1.90f, .47f);
            AddPicketRun(vertices, uv, triangles, false, -.95f,
                .90f, .36f);
            AddPicketRun(vertices, uv, triangles, false, .95f,
                .90f, .36f);
            _picketMesh = new Mesh { name = "Weathered low picket enclosure" };
            _picketMesh.SetVertices(vertices);
            _picketMesh.SetUVs(0, uv);
            _picketMesh.SetTriangles(triangles, 0);
            _picketMesh.RecalculateNormals();
            _picketMesh.RecalculateBounds();
            return _picketMesh;
        }

        private static void AddPicketRun(List<Vector3> vertices,
            List<Vector2> uv, List<int> triangles, bool alongX,
            float fixedCoordinate, float length, float height)
        {
            var count = Mathf.RoundToInt(length / .15f);
            for (var i = 0; i < count; i++)
            {
                var offset = -length * .5f + (i + .5f) * length / count;
                var center = alongX ? new Vector3(offset, 0f, fixedCoordinate)
                    : new Vector3(fixedCoordinate, 0f, offset);
                var picketHeight = height + .016f * Mathf.Sin(i * 2.3f);
                AddFenceBox(vertices, uv, triangles,
                    center + Vector3.up * (picketHeight - .07f) * .5f,
                    alongX ? new Vector3(.068f, picketHeight - .07f, .047f)
                        : new Vector3(.047f, picketHeight - .07f, .068f));
                AddPicketTip(vertices, uv, triangles, center,
                    alongX, picketHeight);
            }
            var railCenter = alongX ? new Vector3(0f, 0f, fixedCoordinate)
                : new Vector3(fixedCoordinate, 0f, 0f);
            foreach (var railY in new[] { .08f, height - .07f })
                AddFenceBox(vertices, uv, triangles,
                    railCenter + Vector3.up * railY,
                    alongX ? new Vector3(length, .035f, .07f) :
                        new Vector3(.07f, .035f, length));
        }

        private static void AddPicketTip(List<Vector3> vertices,
            List<Vector2> uv, List<int> triangles, Vector3 center,
            bool alongX, float height)
        {
            var halfX = alongX ? .034f : .0235f;
            var halfZ = alongX ? .0235f : .034f;
            var baseY = height - .07f;
            var corners = new[]
            {
                center + new Vector3(-halfX, baseY, -halfZ),
                center + new Vector3(halfX, baseY, -halfZ),
                center + new Vector3(halfX, baseY, halfZ),
                center + new Vector3(-halfX, baseY, halfZ)
            };
            var apex = center + Vector3.up * height;
            for (var side = 0; side < 4; side++)
                AddFenceFace(vertices, uv, triangles, corners[side],
                    corners[(side + 1) % 4], apex);
        }

        private static void AddFenceBox(List<Vector3> vertices,
            List<Vector2> uv, List<int> triangles, Vector3 center,
            Vector3 size)
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
            AddFenceQuad(vertices, uv, triangles, a, b, f, e);
            AddFenceQuad(vertices, uv, triangles, c, d, h, g);
            AddFenceQuad(vertices, uv, triangles, d, a, e, h);
            AddFenceQuad(vertices, uv, triangles, b, c, g, f);
            AddFenceQuad(vertices, uv, triangles, e, f, g, h);
            AddFenceQuad(vertices, uv, triangles, d, c, b, a);
        }

        private static void AddFenceQuad(List<Vector3> vertices,
            List<Vector2> uv, List<int> triangles, Vector3 a, Vector3 b,
            Vector3 c, Vector3 d)
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

        private static void AddFenceFace(List<Vector3> vertices,
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

        public void SetSeason(SeasonPreset season)
        {
            if (_hedge != null || _roundedShrubs.Count > 0)
            {
                _hedgeProperties ??= new MaterialPropertyBlock();
                _hedgeProperties.SetColor("_Color",
                    GeorgianClippedHedgeGarden.LeafColorForSeason(
                        season, _opacity));
                var material = GeorgianClippedHedgeGarden.LeafMaterial(
                    _opacity < .99f);
                if (_hedge != null)
                {
                    _hedge.sharedMaterial = material;
                    _hedge.SetPropertyBlock(_hedgeProperties);
                }
                var roundedColor = season switch
                {
                    SeasonPreset.Spring => new Color(.68f, .80f, .55f,
                        _opacity),
                    SeasonPreset.Autumn => new Color(.58f, .69f, .48f,
                        _opacity),
                    SeasonPreset.Winter => new Color(.50f, .60f, .42f,
                        _opacity),
                    _ => new Color(.63f, .76f, .52f, _opacity)
                };
                var roundedProperties = new MaterialPropertyBlock();
                roundedProperties.SetColor("_Color", roundedColor);
                foreach (var shrub in _roundedShrubs)
                {
                    shrub.sharedMaterial = _opacity < .99f ?
                        _roundPreview : _roundMaterial;
                    shrub.SetPropertyBlock(roundedProperties);
                }
            }
            _groundProperties ??= new MaterialPropertyBlock();
            foreach (var ground in _ground)
            {
                var edge = ground.name.Contains("edge");
                ground.sharedMaterial = edge
                    ? (_opacity < .99f ? _edgePreview : _edge)
                    : (_opacity < .99f ? _earthPreview : _earth);
                var color = edge ? _edge.color : _earth.color;
                color.a = _opacity;
                _groundProperties.SetColor("_Color", color);
                ground.SetPropertyBlock(_groundProperties);
            }
            if (_picket != null)
            {
                _picket.sharedMaterial = _opacity < .99f ?
                    _picketPreview : _picketMaterial;
                var color = _picketMaterial.color;
                color.a = _opacity;
                var block = new MaterialPropertyBlock();
                block.SetColor("_Color", color);
                _picket.SetPropertyBlock(block);
            }
            if (_plan != null)
            {
                _plan.enabled = season != SeasonPreset.Winter;
                _plan.color = season switch
                {
                    SeasonPreset.Spring => new Color(.92f, 1f, .93f,
                        .72f * _opacity),
                    SeasonPreset.Autumn => new Color(.82f, .76f, .65f,
                        .65f * _opacity),
                    _ => new Color(1f, 1f, 1f, .80f * _opacity)
                };
            }
            foreach (var shrub in _shrubs)
            {
                shrub.sprite = season == SeasonPreset.Winter
                    ? _winterShrub : _summerShrub;
                shrub.color = season switch
                {
                    SeasonPreset.Autumn => new Color(.84f, .80f, .70f,
                        _opacity),
                    SeasonPreset.Winter => new Color(.92f, .94f, .96f,
                        _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
            foreach (var flower in _flowers)
            {
                flower.enabled = season != SeasonPreset.Winter;
                flower.color = season switch
                {
                    SeasonPreset.Spring => new Color(.92f, 1f, .94f,
                        .8f * _opacity),
                    SeasonPreset.Autumn => new Color(.78f, .70f, .61f,
                        .7f * _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
            foreach (var rose in _roses)
            {
                rose.sprite = season == SeasonPreset.Winter
                    ? _winterShrub : Load("rose-bush-summer");
                rose.color = season switch
                {
                    SeasonPreset.Autumn => new Color(.88f, .79f, .72f,
                        .85f * _opacity),
                    SeasonPreset.Winter => new Color(.83f, .86f, .82f,
                        _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
            foreach (var purple in _purpleFlowers)
            {
                purple.enabled = season != SeasonPreset.Winter;
                purple.color = season switch
                {
                    SeasonPreset.Spring => new Color(.89f, .95f, 1f,
                        .80f * _opacity),
                    SeasonPreset.Autumn => new Color(.81f, .75f, .68f,
                        .68f * _opacity),
                    _ => new Color(1f, 1f, 1f, _opacity)
                };
            }
        }

        internal void SetOpacity(float opacity, SeasonPreset season)
        {
            _opacity = Mathf.Clamp01(opacity);
            SetSeason(season);
        }
    }
}
