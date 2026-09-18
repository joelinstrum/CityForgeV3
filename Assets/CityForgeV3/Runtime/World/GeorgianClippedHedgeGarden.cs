using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    // A complete formal hedge plot is one placed prop. All clipped volumes
    // share one mesh and material; the lot camera supplies the viewing angle.
    internal sealed class GeorgianClippedHedgeGarden : MonoBehaviour
    {
        private const string LeafPath =
            "CityForgeV3/Garden/GeorgianClippedHedgesV01/clipped-leaves";
        private static Material _leafMaterial;
        private static Material _previewLeafMaterial;
        private static Material _earthMaterial;
        private static Material _previewEarthMaterial;
        private Mesh _mesh;
        private MeshRenderer _leafRenderer;
        private Renderer _earthRenderer;
        private float _opacity;

        internal static Transform Create(string name, bool wide,
            float opacity, SeasonPreset season)
        {
            var root = new GameObject(name).transform;
            var garden = root.gameObject.AddComponent<GeorgianClippedHedgeGarden>();
            garden._opacity = opacity;
            garden.Build(wide);
            garden.SetSeason(season);
            return root;
        }

        private static bool EnsureMaterials()
        {
            if (_leafMaterial != null) return true;
            var leaves = Resources.Load<Texture2D>(LeafPath);
            if (leaves == null)
            {
                Debug.LogError("Clipped hedge leaf texture is missing.");
                return false;
            }
            leaves.wrapMode = TextureWrapMode.Repeat;
            var shader = Shader.Find("Standard");
            _leafMaterial = new Material(shader)
            {
                name = "Garden clipped leaves",
                mainTexture = leaves,
                color = Color.white
            };
            _leafMaterial.SetFloat("_Glossiness", 0.08f);
            _previewLeafMaterial = new Material(_leafMaterial)
            {
                name = "Garden clipped leaves preview"
            };
            _previewLeafMaterial.SetFloat("_Mode", 2f);
            _previewLeafMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _previewLeafMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _previewLeafMaterial.SetInt("_ZWrite", 0);
            _previewLeafMaterial.DisableKeyword("_ALPHATEST_ON");
            _previewLeafMaterial.EnableKeyword("_ALPHABLEND_ON");
            _previewLeafMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _previewLeafMaterial.renderQueue = (int)RenderQueue.Transparent;
            _earthMaterial = new Material(shader)
            {
                name = "Garden dark earth",
                color = new Color(0.20f, 0.18f, 0.14f)
            };
            _earthMaterial.SetFloat("_Glossiness", 0.02f);
            _previewEarthMaterial = new Material(_earthMaterial)
            {
                name = "Garden dark earth preview"
            };
            _previewEarthMaterial.SetFloat("_Mode", 2f);
            _previewEarthMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _previewEarthMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _previewEarthMaterial.SetInt("_ZWrite", 0);
            _previewEarthMaterial.DisableKeyword("_ALPHATEST_ON");
            _previewEarthMaterial.EnableKeyword("_ALPHABLEND_ON");
            _previewEarthMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _previewEarthMaterial.renderQueue = (int)RenderQueue.Transparent;
            return true;
        }

        private void Build(bool wide)
        {
            if (!EnsureMaterials()) return;
            var width = wide ? 6f : 4f;
            var depth = wide ? 3f : 4f;
            var earth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earth.name = "Contained dark earth";
            earth.transform.SetParent(transform, false);
            earth.transform.localPosition = new Vector3(0f, -0.035f, 0f);
            earth.transform.localScale = new Vector3(width, 0.07f, depth);
            earth.GetComponent<Collider>().enabled = false;
            _earthRenderer = earth.GetComponent<Renderer>();
            _earthRenderer.sharedMaterial = _earthMaterial;

            var builder = new HedgeMeshBuilder();
            var border = wide ? 0.38f : 0.40f;
            var edgeX = width * 0.5f - border * 0.5f - 0.03f;
            var edgeZ = depth * 0.5f - border * 0.5f - 0.03f;
            builder.AddBox(0f, edgeZ, width - 0.06f, border, 0.54f);
            builder.AddBox(0f, -edgeZ, width - 0.06f, border, 0.54f);
            builder.AddBox(-edgeX, 0f, border, depth - border * 2f - 0.06f,
                0.54f);
            builder.AddBox(edgeX, 0f, border, depth - border * 2f - 0.06f,
                0.54f);
            if (wide)
            {
                for (var i = -1; i <= 1; i++)
                    builder.AddBox(i * 1.63f, 0f, 1.05f, 0.80f, 0.42f);
            }
            else
            {
                for (var x = -1; x <= 1; x += 2)
                for (var z = -1; z <= 1; z += 2)
                    builder.AddBox(x * 0.81f, z * 0.81f,
                        0.82f, 0.82f, 0.42f);
            }
            _mesh = builder.Finish();
            _mesh.name = wide ? "Georgian hedge rectangle" :
                "Georgian hedge square";
            var foliage = new GameObject("Combined clipped hedges");
            foliage.transform.SetParent(transform, false);
            foliage.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _leafRenderer = foliage.AddComponent<MeshRenderer>();
            _leafRenderer.sharedMaterial = _leafMaterial;
        }

        internal void SetSeason(SeasonPreset season)
        {
            if (_leafRenderer == null) return;
            _leafRenderer.sharedMaterial = _opacity < 0.99f
                ? _previewLeafMaterial : _leafMaterial;
            _earthRenderer.sharedMaterial = _opacity < 0.99f
                ? _previewEarthMaterial : _earthMaterial;
            var color = season switch
            {
                SeasonPreset.Spring => new Color(0.59f, 0.67f, 0.52f, _opacity),
                SeasonPreset.Autumn => new Color(0.46f, 0.52f, 0.40f, _opacity),
                SeasonPreset.Winter => new Color(0.38f, 0.44f, 0.36f, _opacity),
                _ => new Color(0.52f, 0.60f, 0.46f, _opacity)
            };
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            _leafRenderer.SetPropertyBlock(block);
            block.SetColor("_Color", new Color(0.20f, 0.18f, 0.14f,
                _opacity));
            _earthRenderer.SetPropertyBlock(block);
        }

        internal void SetOpacity(float opacity, SeasonPreset season)
        {
            _opacity = opacity;
            SetSeason(season);
        }

        private void OnDestroy()
        {
            if (_mesh == null) return;
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
        }

        private sealed class HedgeMeshBuilder
        {
            private readonly List<Vector3> _vertices = new();
            private readonly List<Vector2> _uv = new();
            private readonly List<int> _triangles = new();

            internal void AddBox(float centerX, float centerZ,
                float width, float depth, float height)
            {
                var x0 = centerX - width * 0.5f;
                var x1 = centerX + width * 0.5f;
                var z0 = centerZ - depth * 0.5f;
                var z1 = centerZ + depth * 0.5f;
                var y0 = 0.005f;
                var bevel = Mathf.Min(0.07f, width * 0.18f,
                    depth * 0.18f);
                var shoulder = height - bevel;
                var top = height;
                Face(new(x0, y0, z0), new(x1, y0, z0),
                    new(x1, shoulder, z0), new(x0, shoulder, z0),
                    Vector3.back);
                Face(new(x1, y0, z1), new(x0, y0, z1),
                    new(x0, shoulder, z1), new(x1, shoulder, z1),
                    Vector3.forward);
                Face(new(x0, y0, z1), new(x0, y0, z0),
                    new(x0, shoulder, z0), new(x0, shoulder, z1),
                    Vector3.left);
                Face(new(x1, y0, z0), new(x1, y0, z1),
                    new(x1, shoulder, z1), new(x1, shoulder, z0),
                    Vector3.right);
                Face(new(x0, shoulder, z0), new(x1, shoulder, z0),
                    new(x1 - bevel, top, z0 + bevel),
                    new(x0 + bevel, top, z0 + bevel),
                    Vector3.back + Vector3.up);
                Face(new(x1, shoulder, z1), new(x0, shoulder, z1),
                    new(x0 + bevel, top, z1 - bevel),
                    new(x1 - bevel, top, z1 - bevel),
                    Vector3.forward + Vector3.up);
                Face(new(x0, shoulder, z1), new(x0, shoulder, z0),
                    new(x0 + bevel, top, z0 + bevel),
                    new(x0 + bevel, top, z1 - bevel),
                    Vector3.left + Vector3.up);
                Face(new(x1, shoulder, z0), new(x1, shoulder, z1),
                    new(x1 - bevel, top, z1 - bevel),
                    new(x1 - bevel, top, z0 + bevel),
                    Vector3.right + Vector3.up);
                Face(new(x0 + bevel, top, z0 + bevel),
                    new(x1 - bevel, top, z0 + bevel),
                    new(x1 - bevel, top, z1 - bevel),
                    new(x0 + bevel, top, z1 - bevel), Vector3.up);
            }

            private void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
                    (b, d) = (d, b);
                var start = _vertices.Count;
                _vertices.Add(a); _vertices.Add(b);
                _vertices.Add(c); _vertices.Add(d);
                var u = Vector3.Distance(a, b) / 2.2f;
                var v = Vector3.Distance(a, d) / 2.2f;
                _uv.Add(Vector2.zero); _uv.Add(new Vector2(u, 0f));
                _uv.Add(new Vector2(u, v)); _uv.Add(new Vector2(0f, v));
                _triangles.Add(start); _triangles.Add(start + 1);
                _triangles.Add(start + 2);
                _triangles.Add(start); _triangles.Add(start + 2);
                _triangles.Add(start + 3);
            }

            internal Mesh Finish()
            {
                var mesh = new Mesh();
                mesh.SetVertices(_vertices);
                mesh.SetUVs(0, _uv);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
