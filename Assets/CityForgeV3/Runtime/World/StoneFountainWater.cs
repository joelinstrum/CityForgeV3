using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    // Presentation-only water attached to the grounded 3 m stone fountain.
    // Geometry and materials are shared; no water state enters the lot save.
    internal sealed class StoneFountainWater : MonoBehaviour
    {
        private const int DiskSegments = 64;
        private static Mesh _basinDisk;
        private static Mesh _middleDisk;
        private static Mesh _topDisk;
        private static Mesh _streams;
        private static Material _poolMaterial;
        private static Material _streamMaterial;
        private static Material _splashMaterial;
        private readonly List<Renderer> _renderers = new();
        private MaterialPropertyBlock _properties;

        internal static StoneFountainWater Create(Transform parent, float opacity)
        {
            if (!EnsureResources()) return null;
            var root = new GameObject("Running fountain water").transform;
            root.SetParent(parent, false);
            var water = root.gameObject.AddComponent<StoneFountainWater>();
            water.AddSurface("Lower basin water", _basinDisk, _poolMaterial,
                new Vector3(0f, 0.38f, 0f));
            water.AddSurface("Middle bowl water", _middleDisk, _poolMaterial,
                new Vector3(0f, 1.82f, 0f));
            water.AddSurface("Top bowl water", _topDisk, _poolMaterial,
                new Vector3(0f, 2.73f, 0f));
            water.AddSurface("Falling water streams", _streams,
                _streamMaterial, Vector3.zero);
            water.AddSplash("Lower basin splashes", 0.38f, 0.98f, 18f);
            water.AddSplash("Middle bowl splashes", 1.82f, 0.58f, 10f);
            water.SetOpacity(opacity);
            return water;
        }

        private static bool EnsureResources()
        {
            if (_poolMaterial != null && _basinDisk != null &&
                _streams != null) return true;
            var shader = Shader.Find("CityForgeV3/StoneFountainWater");
            if (shader == null)
            {
                Debug.LogError("Stone fountain water shader is missing.");
                return false;
            }
            _poolMaterial = new Material(shader)
            {
                name = "Fountain shaded water",
                color = new Color(0.27f, 0.39f, 0.40f, 0.78f)
            };
            _poolMaterial.SetFloat("_Mode", 0f);
            _streamMaterial = new Material(shader)
            {
                name = "Fountain falling water",
                color = new Color(0.63f, 0.76f, 0.76f, 0.55f)
            };
            _streamMaterial.SetFloat("_Mode", 1f);
            _splashMaterial = new Material(shader)
            {
                name = "Fountain fine splashes",
                color = new Color(0.72f, 0.81f, 0.80f, 0.60f)
            };
            _splashMaterial.SetFloat("_Mode", 2f);
            _basinDisk = BuildDisk("Lower fountain basin water", 1.11f);
            _middleDisk = BuildDisk("Middle fountain bowl water", 0.55f);
            _topDisk = BuildDisk("Top fountain bowl water", 0.32f);
            _streams = BuildStreams();
            return true;
        }

        private static Mesh BuildDisk(string name, float radius)
        {
            var vertices = new Vector3[DiskSegments + 1];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[DiskSegments * 3];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(.5f, .5f);
            for (var i = 0; i < DiskSegments; i++)
            {
                var angle = i * Mathf.PI * 2f / DiskSegments;
                var x = Mathf.Cos(angle);
                var z = Mathf.Sin(angle);
                vertices[i + 1] = new Vector3(x * radius, 0f, z * radius);
                uv[i + 1] = new Vector2(.5f + x * .5f,
                    .5f + z * .5f);
                var first = i * 3;
                triangles[first] = 0;
                triangles[first + 1] = (i + 1) % DiskSegments + 1;
                triangles[first + 2] = i + 1;
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh BuildStreams()
        {
            var vertices = new List<Vector3>(1024);
            var uv = new List<Vector2>(1024);
            var triangles = new List<int>(2048);
            AddTierStreams(vertices, uv, triangles, 8,
                0.46f, 2.75f, 0.59f, 1.82f, .025f, .035f);
            AddTierStreams(vertices, uv, triangles, 10,
                0.73f, 1.88f, 1.00f, 0.38f, .031f, .08f);
            var mesh = new Mesh { name = "Two-tier falling fountain water" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddTierStreams(List<Vector3> vertices,
            List<Vector2> uv, List<int> triangles, int count,
            float startRadius, float startHeight, float endRadius,
            float endHeight, float halfWidth, float outwardBulge)
        {
            const int steps = 8;
            for (var i = 0; i < count; i++)
            {
                var angle = (i + .15f) * Mathf.PI * 2f / count;
                var radial = new Vector3(Mathf.Cos(angle), 0f,
                    Mathf.Sin(angle));
                var tangent = new Vector3(-radial.z, 0f, radial.x);
                for (var side = 0; side < 2; side++)
                {
                    var axis = side == 0 ? tangent : radial;
                    var start = vertices.Count;
                    for (var step = 0; step <= steps; step++)
                    {
                        var t = step / (float)steps;
                        var radius = Mathf.Lerp(startRadius, endRadius, t)
                            + outwardBulge * Mathf.Sin(t * Mathf.PI);
                        var height = Mathf.Lerp(startHeight, endHeight,
                            t * t);
                        var point = radial * radius + Vector3.up * height;
                        vertices.Add(point - axis * halfWidth);
                        vertices.Add(point + axis * halfWidth);
                        uv.Add(new Vector2(0f, t));
                        uv.Add(new Vector2(1f, t));
                        if (step == steps) continue;
                        var v = start + step * 2;
                        triangles.Add(v); triangles.Add(v + 2);
                        triangles.Add(v + 1);
                        triangles.Add(v + 1); triangles.Add(v + 2);
                        triangles.Add(v + 3);
                    }
                }
            }
        }

        private void AddSurface(string name, Mesh mesh, Material material,
            Vector3 position)
        {
            var surface = new GameObject(name);
            surface.transform.SetParent(transform, false);
            surface.transform.localPosition = position;
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _renderers.Add(renderer);
        }

        private void AddSplash(string name, float height, float radius,
            float rate)
        {
            var splash = new GameObject(name);
            splash.transform.SetParent(transform, false);
            splash.transform.localPosition = new Vector3(0f, height, 0f);
            splash.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var system = splash.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.20f, .38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.09f, .21f);
            main.startSize = new ParticleSystem.MinMaxCurve(.026f, .052f);
            main.startColor = new Color(1f, 1f, 1f, .8f);
            main.gravityModifier = .14f;
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = system.emission;
            emission.rateOverTime = rate;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = .07f;
            var renderer = splash.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = _splashMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderers.Add(renderer);
            if (Application.isPlaying) system.Play();
        }

        internal void SetOpacity(float opacity)
        {
            _properties ??= new MaterialPropertyBlock();
            _properties.SetFloat("_Opacity", Mathf.Clamp01(opacity));
            foreach (var renderer in _renderers)
                if (renderer != null) renderer.SetPropertyBlock(_properties);
        }
    }
}
