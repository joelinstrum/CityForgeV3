using System.Collections;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        public const float WinterSnowfallDurationSeconds = 10f;

        private Coroutine _winterSnowfallRoutine;
        private ParticleSystem _winterSnowfallParticles;
        private ParticleSystem _foregroundWinterSnowfallParticles;
        private Renderer _snowGroundCover;
        private float _snowAccumulation;

        public bool IsWinterSnowing => _winterSnowfallRoutine != null;
        public float SnowAccumulation => _snowAccumulation;
        public bool CanStartWinterSnowfall =>
            Season == SeasonPreset.Winter && !IsWinterSnowing &&
            _snowAccumulation < 0.999f;

        public bool StartWinterSnowfall()
        {
            if (!CanStartWinterSnowfall) return false;
            _snowAccumulation = 0.01f;
            EnsureSnowGroundCover();
            _winterSnowfallRoutine = StartCoroutine(WinterSnowfallRoutine());
            NotifyStateChanged();
            return true;
        }

        private IEnumerator WinterSnowfallRoutine()
        {
            BuildWinterSnowfallParticles();
            var elapsed = 0f;
            while (elapsed < WinterSnowfallDurationSeconds &&
                   Season == SeasonPreset.Winter)
            {
                elapsed += Time.deltaTime;
                var stormProgress = Mathf.Clamp01(
                    elapsed / WinterSnowfallDurationSeconds);
                _snowAccumulation = Mathf.Lerp(0.01f, 1f, stormProgress);
                ApplySnowAccumulation();
                yield return null;
            }

            _snowAccumulation = Season == SeasonPreset.Winter ? 1f : 0f;
            ApplySnowAccumulation();
            StopAndReleaseSnowLayer(ref _winterSnowfallParticles);
            StopAndReleaseSnowLayer(ref _foregroundWinterSnowfallParticles);
            _winterSnowfallRoutine = null;
            NotifyStateChanged();
        }

        private void EnsureSnowGroundCover()
        {
            if (_snowGroundCover != null) return;
            var cover = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cover.name = "Winter Snow Accumulation";
            cover.transform.SetParent(transform, false);
            cover.transform.localPosition = new Vector3(0f, 0.008f, 0f);
            cover.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cover.transform.localScale = new Vector3(
                LotWidthMeters, LotDepthMeters, 1f);
            cover.GetComponent<Collider>().enabled = false;

            var shader = Shader.Find("CityForge/SnowAccumulation") ??
                Shader.Find("Unlit/Transparent") ??
                Shader.Find("Sprites/Default");
            var material = new Material(shader)
            {
                name = "Runtime Winter Snow Accumulation",
                mainTexture = BuildSnowAccumulationTexture(),
                // Draw after the base lot surface so the transparent accumulation
                // remains visible instead of being overwritten by the grass.
                renderQueue = 2001
            };
            material.mainTextureScale = new Vector2(
                Mathf.Max(1f, LotWidthMeters / 8f),
                Mathf.Max(1f, LotDepthMeters / 8f));
            _snowGroundCover = cover.GetComponent<Renderer>();
            _snowGroundCover.sharedMaterial = material;
            ApplySnowAccumulation();
        }

        private void ResizeSnowGroundCover()
        {
            if (_snowGroundCover == null) return;
            _snowGroundCover.transform.localPosition =
                new Vector3(0f, 0.008f, 0f);
            _snowGroundCover.transform.localScale = new Vector3(
                LotWidthMeters, LotDepthMeters, 1f);
            var material = _snowGroundCover.sharedMaterial;
            if (material != null)
                material.mainTextureScale = new Vector2(
                    Mathf.Max(1f, LotWidthMeters / 8f),
                    Mathf.Max(1f, LotDepthMeters / 8f));
        }

        private static Texture2D BuildSnowAccumulationTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Runtime Snow Accumulation Texture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var broad = Mathf.PerlinNoise(x / 23f + 4.1f, y / 23f + 8.7f);
                var fine = Mathf.PerlinNoise(x / 7f + 17.3f, y / 7f + 2.9f);
                var coverage = Mathf.SmoothStep(0.24f, 0.78f,
                    broad * 0.72f + fine * 0.28f);
                pixels[y * size + x] = new Color(
                    0.94f, 0.97f, 1f, Mathf.Lerp(0.54f, 0.96f, coverage));
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private void ApplySnowAccumulation()
        {
            if (_snowGroundCover == null) return;
            var material = _snowGroundCover.sharedMaterial;
            var opacity = Mathf.Clamp01(_snowAccumulation);
            if (material.HasProperty("_Accumulation"))
                material.SetFloat("_Accumulation", opacity);
            else
            {
                var color = material.color;
                color.a = opacity;
                material.color = color;
            }
            _snowGroundCover.gameObject.SetActive(
                Season == SeasonPreset.Winter && _snowAccumulation > 0.001f);
            if (Season == SeasonPreset.Winter)
            {
                // Accumulated winter snow keeps streets wet after the flakes
                // stop, including across time-of-day changes.
                RoadWetness = Mathf.Max(RoadWetness, _snowAccumulation);
                UpdateWetStreetReflections();
            }
        }

        private void BuildWinterSnowfallParticles()
        {
            if (_winterSnowfallParticles != null)
                Destroy(_winterSnowfallParticles.gameObject);
            if (_foregroundWinterSnowfallParticles != null)
                Destroy(_foregroundWinterSnowfallParticles.gameObject);
            _winterSnowfallParticles = BuildWinterSnowLayer(
                "Winter Snowfall — Behind Buildings", false);
            _foregroundWinterSnowfallParticles = BuildWinterSnowLayer(
                "Winter Snowfall — In Front of Buildings", true);
        }

        private ParticleSystem BuildWinterSnowLayer(string name, bool foreground)
        {
            var snowfall = new GameObject(name);
            snowfall.transform.SetParent(transform, false);
            snowfall.transform.localPosition = new Vector3(0f, 18f, 0f);
            var particles = snowfall.AddComponent<ParticleSystem>();
            particles.Stop(true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = WinterSnowfallDurationSeconds;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 6.5f);
            main.startSpeed = 0f;
            main.startSize = foreground
                ? new ParticleSystem.MinMaxCurve(0.05f, 0.11f)
                : new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.96f, 0.98f, 1f, 0.08f),
                new Color(0.96f, 0.98f, 1f, 1f));
            main.maxParticles = 8000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particles.emission;
            emission.rateOverTime = foreground ? 230f : 620f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(
                LotWidthMeters * 1.15f, 1f, LotDepthMeters * 1.15f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.32f, 0.42f);
            velocity.y = new ParticleSystem.MinMaxCurve(-3.8f, -2.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.18f, 0.22f);
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.38f;
            noise.frequency = 0.24f;
            noise.scrollSpeed = 0.18f;

            var renderer = snowfall.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            renderer.sharedMaterial = new Material(shader)
            {
                name = foreground
                    ? "Runtime Foreground Snowflake Material"
                    : "Runtime Background Snowflake Material",
                mainTexture = BuildSnowflakeTexture(),
                // The near layer deliberately draws after always-visible
                // billboard buildings, matching the approved rain depth cue.
                renderQueue = foreground ? 4000 : 2990
            };
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = foreground ? 3300 : 5;
            particles.Play();
            return particles;
        }

        private void StopAndReleaseSnowLayer(ref ParticleSystem particles)
        {
            if (particles == null) return;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(particles.gameObject, 6f);
            particles = null;
        }

        private static Texture2D BuildSnowflakeTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Runtime Soft Snowflake",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y),
                    new Vector2(center, center)) / center;
                var alpha = Mathf.SmoothStep(1f, 0.18f, distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private void ClearWinterSnow()
        {
            if (_winterSnowfallRoutine != null)
            {
                StopCoroutine(_winterSnowfallRoutine);
                _winterSnowfallRoutine = null;
            }
            if (_winterSnowfallParticles != null)
            {
                Destroy(_winterSnowfallParticles.gameObject);
                _winterSnowfallParticles = null;
            }
            if (_foregroundWinterSnowfallParticles != null)
            {
                Destroy(_foregroundWinterSnowfallParticles.gameObject);
                _foregroundWinterSnowfallParticles = null;
            }
            _snowAccumulation = 0f;
            if (!IsRaining)
            {
                RoadWetness = 0f;
                UpdateWetStreetReflections();
            }
            if (_snowGroundCover != null)
            {
                Destroy(_snowGroundCover.gameObject);
                _snowGroundCover = null;
            }
        }
    }
}
