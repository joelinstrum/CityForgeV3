using System.Collections;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        public const float RainBuildUpDurationSeconds = 5f;
        public const float RainFadeOutDurationSeconds = 4f;

        private ParticleSystem _rainParticles;
        private ParticleSystem _foregroundRainParticles;
        private Coroutine _rainBuildUpRoutine;
        private Coroutine _rainFadeOutRoutine;
        private Coroutine _wetReflectionRefreshRoutine;

        public float RainVisualIntensity { get; private set; }
        public float RoadWetness { get; private set; }

        public void SetRaining(bool raining)
        {
            if (IsRaining == raining) return;
            IsRaining = raining;
            if (raining)
                BuildRainParticles();
            else
                BeginRainFadeOut();
            ApplyTimeOfDay();
            NotifyStateChanged();
        }

        private void ClearRoadWetness()
        {
            RoadWetness = Season == SeasonPreset.Winter
                ? SnowAccumulation
                : 0f;
            UpdateWetStreetReflections();
        }

        private void ScheduleWetStreetReflectionRefresh()
        {
            if (!Application.isPlaying) return;
            if (_wetReflectionRefreshRoutine != null)
                StopCoroutine(_wetReflectionRefreshRoutine);
            _wetReflectionRefreshRoutine = StartCoroutine(
                RefreshWetStreetReflectionsAfterRebuild());
        }

        private IEnumerator RefreshWetStreetReflectionsAfterRebuild()
        {
            // Destroy() removes the previous selected/unselected presentation
            // hierarchy at frame end. Synchronize once more after that point
            // so a freshly loaded multi-building lot cannot miss one view.
            yield return null;
            UpdateWetStreetReflections();
            _wetReflectionRefreshRoutine = null;
        }

        private void BuildRainParticles()
        {
            ClearRainParticles();
            _rainParticles = BuildRainLayer(
                "Rainfall — Behind Buildings", false);
            _foregroundRainParticles = BuildRainLayer(
                "Rainfall — In Front of Buildings", true);
            RainVisualIntensity = 0f;
            _rainBuildUpRoutine = StartCoroutine(RainBuildUpRoutine());
        }

        private ParticleSystem BuildRainLayer(string name, bool foreground)
        {
            var rain = new GameObject(name);
            rain.transform.SetParent(transform, false);
            rain.transform.localPosition = new Vector3(0f, 20f, 0f);

            var particles = rain.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.startLifetime = foreground
                ? new ParticleSystem.MinMaxCurve(1.0f, 1.5f)
                : new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
            main.startSpeed = 0f;
            // Orthographic projection has no natural distance scaling, so
            // deliberately separate the apparent near/far drop sizes.
            main.startSize = foreground
                ? new ParticleSystem.MinMaxCurve(0.045f, 0.085f)
                : new ParticleSystem.MinMaxCurve(0.012f, 0.030f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.74f, 0.75f, 0.74f, foreground ? 0.16f : 0.07f),
                new Color(0.96f, 0.97f, 0.96f, foreground ? 0.58f : 0.30f));
            main.maxParticles = foreground ? 3200 : 9000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particles.emission;
            emission.rateOverTime = foreground ? 8f : 35f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            var stormSpan = Mathf.Max(80f,
                Mathf.Max(LotWidthMeters, LotDepthMeters) * 3f);
            shape.scale = new Vector3(stormSpan, 3f, stormSpan);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(2.4f, 3.2f);
            velocity.y = new ParticleSystem.MinMaxCurve(-15f, -11f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
            var collision = particles.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = 1f;
            collision.bounce = 0f;
            collision.lifetimeLoss = 1f;
            collision.radiusScale = 0.08f;
            collision.quality = ParticleSystemCollisionQuality.High;

            var renderer = rain.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            var material = new Material(shader)
            {
                name = foreground
                    ? "Runtime Foreground Rain Material"
                    : "Runtime Background Rain Material",
                mainTexture = BuildRaindropTexture()
            };
            // The foreground layer is intentionally rendered after the
            // always-visible building sprites. The background layer remains
            // depth-tested, so randomized streaks read on both sides of them.
            material.renderQueue = foreground ? 4000 : 2990;
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = foreground ? 5.2f : 3.1f;
            renderer.velocityScale = foreground ? 0.11f : 0.065f;
            renderer.sortingOrder = foreground ? 3200 : 5;
            particles.Play();
            return particles;
        }

        private IEnumerator RainBuildUpRoutine()
        {
            var elapsed = 0f;
            while (elapsed < RainBuildUpDurationSeconds && IsRaining)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / RainBuildUpDurationSeconds));
                RainVisualIntensity = progress;
                RoadWetness = Mathf.Max(RoadWetness, progress);
                UpdateWetStreetReflections();
                SetRainEmission(_rainParticles,
                    Mathf.Lerp(35f, 2700f, progress));
                SetRainEmission(_foregroundRainParticles,
                    Mathf.Lerp(8f, 1100f, progress));
                yield return null;
            }

            if (IsRaining)
            {
                RainVisualIntensity = 1f;
                RoadWetness = 1f;
                UpdateWetStreetReflections();
                SetRainEmission(_rainParticles, 2700f);
                SetRainEmission(_foregroundRainParticles, 1100f);
            }
            _rainBuildUpRoutine = null;
        }

        private void BeginRainFadeOut()
        {
            if (_rainBuildUpRoutine != null)
            {
                StopCoroutine(_rainBuildUpRoutine);
                _rainBuildUpRoutine = null;
            }
            if (_rainFadeOutRoutine != null)
                StopCoroutine(_rainFadeOutRoutine);
            _rainFadeOutRoutine = StartCoroutine(RainFadeOutRoutine(
                RainEmissionRate(_rainParticles),
                RainEmissionRate(_foregroundRainParticles),
                RainVisualIntensity));
        }

        private IEnumerator RainFadeOutRoutine(
            float backgroundStartRate, float foregroundStartRate,
            float fogStartIntensity)
        {
            var elapsed = 0f;
            while (elapsed < RainFadeOutDurationSeconds && !IsRaining)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / RainFadeOutDurationSeconds));
                RainVisualIntensity = Mathf.Lerp(
                    fogStartIntensity, 0f, progress);
                SetRainEmission(_rainParticles,
                    Mathf.Lerp(backgroundStartRate, 0f, progress));
                SetRainEmission(_foregroundRainParticles,
                    Mathf.Lerp(foregroundStartRate, 0f, progress));
                yield return null;
            }

            if (IsRaining)
            {
                _rainFadeOutRoutine = null;
                yield break;
            }

            StopRainEmission(_rainParticles);
            StopRainEmission(_foregroundRainParticles);
            RainVisualIntensity = 0f;
            yield return new WaitForSeconds(3f);
            if (!IsRaining)
                DestroyRainLayers();
            _rainFadeOutRoutine = null;
        }

        private static float RainEmissionRate(ParticleSystem particles)
        {
            if (particles == null) return 0f;
            return particles.emission.rateOverTime.constant;
        }

        private static void StopRainEmission(ParticleSystem particles)
        {
            if (particles == null) return;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void UpdateWetStreetReflections()
        {
            // Weather is lot-wide. Do not rely on the selected-building view
            // plus its parallel index list: those collections are rebuilt in
            // different orders as selection changes and can temporarily omit
            // a valid presentation. Every presentation owns an independent
            // ground-projected reflection and receives the same wetness.
            var presentations = GetComponentsInChildren<
                HybridBuildingPresentation>(true);
            foreach (var presentation in presentations)
            {
                if (presentation == null) continue;
                presentation.SetWetReflection(RoadWetness,
                    WetReflectionDirectionFor(presentation.transform.position));
            }
            UpdatePropWetStreetReflections();
        }

        private Vector3 WetReflectionDirectionFor(Vector3 buildingPosition)
        {
            var nearestDistance = float.PositiveInfinity;
            var nearestDirection = Vector3.back;
            if (_session.Data.RoadPieces == null) return nearestDirection;
            foreach (var piece in _session.Data.RoadPieces)
            {
                var package = RoadPiecePackageCatalog.Resolve(piece.PackageId);
                var center = RoadArtworkCenter(piece, package);
                var direction = new Vector3(
                    center.x - buildingPosition.x, 0f,
                    center.y - buildingPosition.z);
                var distance = direction.sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestDirection = direction;
            }
            return nearestDirection.normalized;
        }

        private static void SetRainEmission(
            ParticleSystem particles, float rate)
        {
            if (particles == null) return;
            var emission = particles.emission;
            emission.rateOverTime = rate;
        }

        private static Texture2D BuildRaindropTexture()
        {
            const int width = 8;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = "Runtime Rain Streak",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var horizontal = 1f - Mathf.Abs(x - (width - 1) * 0.5f) / 4f;
                var vertical = Mathf.Sin((y + 0.5f) / height * Mathf.PI);
                pixels[y * width + x] = new Color(
                    0.90f, 0.91f, 0.90f,
                    Mathf.Clamp01(horizontal * vertical) * 0.8f);
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private void ClearRainParticles()
        {
            if (_rainBuildUpRoutine != null)
            {
                StopCoroutine(_rainBuildUpRoutine);
                _rainBuildUpRoutine = null;
            }
            if (_rainFadeOutRoutine != null)
            {
                StopCoroutine(_rainFadeOutRoutine);
                _rainFadeOutRoutine = null;
            }
            RainVisualIntensity = 0f;
            DestroyRainLayers();
        }

        private void DestroyRainLayers()
        {
            if (_rainParticles != null)
                DestroyForCurrentMode(_rainParticles.gameObject);
            if (_foregroundRainParticles != null)
                DestroyForCurrentMode(_foregroundRainParticles.gameObject);
            _rainParticles = null;
            _foregroundRainParticles = null;
        }
    }
}
