using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    // A Garden prop that reuses the lot's exact Natural Grass source texture.
    // The grass reaches the muted perimeter border on every footprint.
    internal sealed class NaturalGrassGardenPatch : MonoBehaviour
    {
        private static readonly Dictionary<string, Material> Materials = new();
        private static readonly Dictionary<string, Mesh> BorderMeshes = new();
        private static Texture2D _grass;
        private static Material _borderMaterial;
        private MaterialPropertyBlock _properties;
        private Renderer _renderer;
        private Renderer _borderRenderer;
        private Material _material;
        private float _opacity;

        internal static bool TryDimensions(string propId,
            out float width, out float depth)
        {
            width = depth = 0f;
            if (string.Equals(propId, LotWorldController.NaturalGrassShortPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                width = 4f; depth = 2f;
            }
            else if (string.Equals(propId,
                    LotWorldController.NaturalGrassLongPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                width = 6f; depth = 3f;
            }
            else if (string.Equals(propId,
                    LotWorldController.NaturalGrassSquarePropId,
                    StringComparison.OrdinalIgnoreCase))
                width = depth = 4f;
            else if (string.Equals(propId,
                    LotWorldController.NaturalGrassLargeSquarePropId,
                    StringComparison.OrdinalIgnoreCase))
                width = depth = 6f;
            else if (IsCircle(propId))
                width = depth = 4f;
            return width > 0f;
        }

        internal static bool IsCircle(string propId) => string.Equals(propId,
            LotWorldController.NaturalGrassCirclePropId,
            StringComparison.OrdinalIgnoreCase);

        internal static Transform Create(string name, float width, float depth,
            float opacity, SeasonPreset season, TimeOfDayPreset timeOfDay,
            Vector3 sunDirection, bool circular)
        {
            var root = new GameObject(name).transform;
            var patch = root.gameObject.AddComponent<NaturalGrassGardenPatch>();
            patch._opacity = opacity;
            patch.Build(width, depth, circular);
            patch.SetAppearance(season, timeOfDay, sunDirection);
            return root;
        }

        private void Build(float width, float depth, bool circular)
        {
            if (_grass == null)
                _grass = Resources.Load<Texture2D>(
                    DistrictWorldController.DefaultGrassResource);
            var shader = Shader.Find("CityForgeV3/NaturalGrassGardenPatch");
            if (_grass == null || shader == null)
            {
                Debug.LogError("Natural Grass Garden source or shader is missing.");
                return;
            }
            var key = width + "x" + depth + (circular ? "-round" : "");
            if (!Materials.TryGetValue(key, out _material) || _material == null)
            {
                _material = new Material(shader)
                {
                    name = "Natural Grass Garden " + key,
                    mainTexture = _grass,
                    mainTextureScale = new Vector2(width / 5f, depth / 5f),
                    // Some building lots promote their base to queue 2000.
                    renderQueue = 2002
                };
                _material.SetVector("_PatchSize", new Vector4(width, depth, 0f, 0f));
                _material.SetFloat("_Circular", circular ? 1f : 0f);
                Materials[key] = _material;
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Soft edged Natural Grass";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(width, depth, 1f);
            quad.GetComponent<Collider>().enabled = false;
            _renderer = quad.GetComponent<Renderer>();
            _renderer.sharedMaterial = _material;
            BuildBorder(key, width, depth, circular);
        }

        private void BuildBorder(string key, float width, float depth,
            bool circular)
        {
            if (_borderMaterial == null)
            {
                var shader = Shader.Find("CityForgeV3/ShadowReceivingLotSurface");
                if (shader == null)
                {
                    Debug.LogError("Natural Grass Garden border shader is missing.");
                    return;
                }
                _borderMaterial = new Material(shader)
                {
                    name = "Natural Grass muted border",
                    renderQueue = 2003
                };
            }
            if (!BorderMeshes.TryGetValue(key, out var mesh) || mesh == null)
            {
                mesh = circular ? BuildCircularBorder(width * 0.5f) :
                    BuildRectangularBorder(width, depth);
                BorderMeshes[key] = mesh;
            }
            var border = new GameObject("Muted garden border");
            border.transform.SetParent(transform, false);
            border.transform.localPosition = new Vector3(0f, 0.018f, 0f);
            border.AddComponent<MeshFilter>().sharedMesh = mesh;
            _borderRenderer = border.AddComponent<MeshRenderer>();
            _borderRenderer.sharedMaterial = _borderMaterial;
            _borderRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Mesh BuildRectangularBorder(float width, float depth)
        {
            const float band = 0.12f;
            var vertices = new List<Vector3>(16);
            var triangles = new List<int>(24);
            void Strip(float minX, float maxX, float minZ, float maxZ)
            {
                var first = vertices.Count;
                vertices.Add(new Vector3(minX, 0f, minZ));
                vertices.Add(new Vector3(maxX, 0f, minZ));
                vertices.Add(new Vector3(maxX, 0f, maxZ));
                vertices.Add(new Vector3(minX, 0f, maxZ));
                triangles.AddRange(new[]
                {
                    first, first + 2, first + 1,
                    first, first + 3, first + 2
                });
            }
            var x = width * .5f;
            var z = depth * .5f;
            Strip(-x, x, -z, -z + band);
            Strip(-x, x, z - band, z);
            Strip(-x, -x + band, -z + band, z - band);
            Strip(x - band, x, -z + band, z - band);
            var mesh = new Mesh { name = "Natural Grass rectangular border" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh BuildCircularBorder(float radius)
        {
            const int segments = 64;
            const float band = 0.12f;
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            for (var index = 0; index <= segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f,
                    Mathf.Sin(angle));
                vertices[index * 2] = direction * radius;
                vertices[index * 2 + 1] = direction * (radius - band);
                if (index == segments) continue;
                var v = index * 2;
                var t = index * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 2;
                triangles[t + 4] = v + 1;
                triangles[t + 5] = v + 3;
            }
            var mesh = new Mesh { name = "Natural Grass circular border" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        internal void SetOpacity(float opacity, SeasonPreset season,
            TimeOfDayPreset timeOfDay, Vector3 sunDirection)
        {
            _opacity = opacity;
            SetAppearance(season, timeOfDay, sunDirection);
        }

        internal void SetAppearance(SeasonPreset season,
            TimeOfDayPreset timeOfDay, Vector3 sunDirection)
        {
            if (_renderer == null) return;
            var tint = SeasonLighting.GroundColor(season,
                LotWorldController.TextureTintForTimeOfDay(timeOfDay));
            tint.a = _opacity;
            _properties ??= new MaterialPropertyBlock();
            _properties.SetColor("_Color", tint);
            _renderer.SetPropertyBlock(_properties);
            _material.SetVector("_TerrainSunDirection", sunDirection);
            _material.SetFloat("_AmbientFloor", timeOfDay switch
            {
                TimeOfDayPreset.Morning => .68f,
                TimeOfDayPreset.Noon => .58f,
                TimeOfDayPreset.Afternoon => .66f,
                _ => .52f
            });
            if (_borderRenderer != null)
            {
                var edge = new Color(0.26f, 0.25f, 0.21f,
                    Mathf.Clamp01(_opacity) * 0.82f);
                var timeTint = LotWorldController.TextureTintForTimeOfDay(timeOfDay);
                edge.r *= timeTint.r;
                edge.g *= timeTint.g;
                edge.b *= timeTint.b;
                _properties.SetColor("_Color", edge);
                _borderRenderer.SetPropertyBlock(_properties);
                _borderMaterial.SetVector("_TerrainSunDirection", sunDirection);
                _borderMaterial.SetFloat("_AmbientFloor",
                    _material.GetFloat("_AmbientFloor"));
            }
        }
    }
}
