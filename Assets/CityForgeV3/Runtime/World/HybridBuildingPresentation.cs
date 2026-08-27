using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum BuildingArtworkSource
    {
        Approved,
        NeutralPilot
    }

    public sealed class HybridBuildingPresentation : MonoBehaviour
    {
        private Camera _camera;
        private HybridBuildingPackage _package;
        private SpriteRenderer _renderer;
        private SpriteRenderer _nightRenderer;
        private SpriteRenderer _shadeRenderer;
        private MeshRenderer _wetReflectionRenderer;
        private MeshFilter _wetReflectionFilter;
        private Material _alwaysVisibleMaterial;
        private Material _wetReflectionMaterial;
        private Mesh _wetReflectionMesh;
        private Transform _visualRoot;
        private Sprite[] _sprites;
        private Sprite[] _registrationSprites;
        private Bounds[] _registrationLocalBounds;
        private bool[] _registrationLocalBoundsValid;
        private Sprite[] _neutralSprites;
        private Bounds[] _neutralRegistrationLocalBounds;
        private bool[] _neutralRegistrationLocalBoundsValid;
        private Sprite[] _nightOverlays;
        private Sprite[] _fullNightSprites;
        private Bounds[] _fullNightRegistrationLocalBounds;
        private bool[] _fullNightRegistrationLocalBoundsValid;
        private Sprite[,] _shadeOverlays;
        private int _facing;
        private bool _visible;
        private float _opacity = 1f;
        private BuildingArtworkSource _artworkSource;
        private TimeOfDayPreset _timeOfDay = TimeOfDayPreset.Noon;
        private SeasonPreset _season = SeasonPreset.Summer;
        private bool _isRaining;
        private float _wetReflectionStrength;
        private Vector3 _wetReflectionWorldDirection = Vector3.back;
        private int _buildingRotationQuarterTurns;
        private float _proxyRegistrationScale = 1f;
        private Vector2 _proxyRegistrationOffset;
        private int _hostBuildingStencilReference;

        public string FacingId => _package.Facing(_facing).Id;
        public int FacingIndex => _facing;
        public bool Visible => _renderer != null && _renderer.enabled;
        public bool ShadeOverlayShowing =>
            _shadeRenderer != null && _shadeRenderer.enabled &&
            _shadeRenderer.sprite != null;
        public Vector3 VisualPlaneLocalPosition =>
            _visualRoot != null ? _visualRoot.localPosition : Vector3.zero;
        public bool NightOverlayShowing =>
            _nightRenderer != null && _nightRenderer.enabled &&
            _nightRenderer.sprite != null;
        public bool NeutralPilotCompatible => SupportsNeutralPilot(_facing);
        public bool NeutralPilotShowing =>
            _artworkSource == BuildingArtworkSource.NeutralPilot &&
            NeutralPilotCompatible;
        public bool PilotRequestedButUnavailable =>
            _artworkSource == BuildingArtworkSource.NeutralPilot &&
            !NeutralPilotCompatible;
        public float Opacity => _opacity;
        public int HostBuildingStencilReference =>
            _hostBuildingStencilReference;

        public bool TryGetArtworkRenderer(out SpriteRenderer renderer)
        {
            renderer = _renderer;
            return renderer != null && renderer.enabled &&
                   renderer.gameObject.activeInHierarchy &&
                   renderer.sprite != null;
        }

        public bool TryGetVisibleArtworkScreenBounds(
            Camera renderCamera, out Vector2 minimum, out Vector2 maximum)
        {
            minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            if (!_visible || renderCamera == null || _renderer == null ||
                !_renderer.enabled || !_renderer.gameObject.activeInHierarchy ||
                !TryGetVisibleArtworkLocalBounds(out var bounds))
                return false;

            var found = false;
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            {
                var local = new Vector3(
                    x < 0 ? bounds.min.x : bounds.max.x,
                    y < 0 ? bounds.min.y : bounds.max.y,
                    0f);
                if (_renderer.flipX) local.x = -local.x;
                if (_renderer.flipY) local.y = -local.y;
                var screen = renderCamera.WorldToScreenPoint(
                    _renderer.transform.TransformPoint(local));
                if (screen.z <= 0f) continue;
                minimum = Vector2.Min(minimum, screen);
                maximum = Vector2.Max(maximum, screen);
                found = true;
            }

            return found;
        }

        public bool ContainsVisibleArtworkPixel(Camera renderCamera, Vector2 pixel)
        {
            if (!_visible || _renderer == null || !_renderer.enabled ||
                renderCamera == null || _registrationSprites == null ||
                _facing < 0 || _facing >= _registrationSprites.Length)
                return false;
            var tightSprite = _registrationSprites[_facing];
            if (tightSprite == null) return false;

            var plane = new Plane(_renderer.transform.forward,
                _renderer.transform.position);
            var ray = renderCamera.ScreenPointToRay(pixel);
            if (!plane.Raycast(ray, out var distance)) return false;
            var local = (Vector2)_renderer.transform.InverseTransformPoint(
                ray.GetPoint(distance));
            var vertices = tightSprite.vertices;
            var triangles = tightSprite.triangles;
            for (var index = 0; index + 2 < triangles.Length; index += 3)
            {
                if (PointInTriangle(local,
                        vertices[triangles[index]],
                        vertices[triangles[index + 1]],
                        vertices[triangles[index + 2]]))
                    return true;
            }
            return false;
        }

        private static bool PointInTriangle(Vector2 point, Vector2 a,
            Vector2 b, Vector2 c)
        {
            static float Cross(Vector2 lhs, Vector2 rhs) =>
                lhs.x * rhs.y - lhs.y * rhs.x;
            var ab = Cross(b - a, point - a);
            var bc = Cross(c - b, point - b);
            var ca = Cross(a - c, point - c);
            const float tolerance = 0.00001f;
            var hasNegative = ab < -tolerance || bc < -tolerance || ca < -tolerance;
            var hasPositive = ab > tolerance || bc > tolerance || ca > tolerance;
            return !(hasNegative && hasPositive);
        }

        public void SetSortingOrder(int order)
        {
            if (_renderer != null) _renderer.sortingOrder = order;
            if (_wetReflectionRenderer != null)
                _wetReflectionRenderer.sortingOrder = order - 2;
            if (_shadeRenderer != null) _shadeRenderer.sortingOrder = order + 1;
            if (_nightRenderer != null) _nightRenderer.sortingOrder = order + 2;
        }

        public void Build(
            Camera presentationCamera,
            HybridBuildingPackage package)
        {
            _camera = presentationCamera;
            _package = package;
            _sprites = new Sprite[_package.FacingCount];
            _registrationSprites = new Sprite[_package.FacingCount];
            _registrationLocalBounds = new Bounds[_package.FacingCount];
            _registrationLocalBoundsValid = new bool[_package.FacingCount];
            _neutralSprites = new Sprite[_package.FacingCount];
            _neutralRegistrationLocalBounds = new Bounds[_package.FacingCount];
            _neutralRegistrationLocalBoundsValid = new bool[_package.FacingCount];
            _nightOverlays = new Sprite[_package.FacingCount];
            _fullNightSprites = new Sprite[_package.FacingCount];
            _fullNightRegistrationLocalBounds = new Bounds[_package.FacingCount];
            _fullNightRegistrationLocalBoundsValid = new bool[_package.FacingCount];
            _shadeOverlays = new Sprite[_package.FacingCount, 4];

            for (var index = 0; index < _sprites.Length; index++)
            {
                var spec = _package.Facing(index);
                var texture =
                    Resources.Load<Texture2D>(spec.ApprovedResourcePath);
                if (texture == null)
                {
                    Debug.LogError($"Missing hybrid render: {spec.ResourcePath}");
                    continue;
                }

                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                _sprites[index] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    spec.UnityPivot,
                    _package.PixelsPerMeter,
                    0,
                    SpriteMeshType.FullRect);
                _sprites[index].name = $"Five Bay {spec.Id}";
                _registrationSprites[index] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    spec.UnityPivot,
                    _package.PixelsPerMeter,
                    0,
                    SpriteMeshType.Tight);
                _registrationSprites[index].name =
                    $"{_package.DisplayName} Tight Registration {spec.Id}";
                CacheRegistrationLocalBounds(
                    _registrationSprites[index],
                    _registrationLocalBounds,
                    _registrationLocalBoundsValid,
                    index);

                var neutralTexture =
                    Resources.Load<Texture2D>(spec.NeutralResourcePath);
                var nightTexture = spec.HasNightOverlay
                    ? Resources.Load<Texture2D>(spec.NightOverlayResourcePath)
                    : null;
                var fullNightTexture = spec.HasFullNightArtwork
                    ? Resources.Load<Texture2D>(spec.NightFullResourcePath)
                    : null;
                if (neutralTexture == null ||
                    (spec.HasNightOverlay && nightTexture == null) ||
                    (spec.HasFullNightArtwork && fullNightTexture == null))
                {
                    Debug.LogError(
                        $"Missing V12 neutral/night render for {spec.Id}.");
                    continue;
                }

                neutralTexture.wrapMode = TextureWrapMode.Clamp;
                neutralTexture.filterMode = FilterMode.Bilinear;
                _neutralSprites[index] = Sprite.Create(
                    neutralTexture,
                    new Rect(0f, 0f, neutralTexture.width, neutralTexture.height),
                    spec.UnityPivot,
                    _package.PixelsPerMeter,
                    0,
                    SpriteMeshType.FullRect);
                _neutralSprites[index].name =
                    $"Five Bay Neutral v12 {spec.Id}";
                var neutralRegistrationSprite = Sprite.Create(
                    neutralTexture,
                    new Rect(0f, 0f, neutralTexture.width, neutralTexture.height),
                    spec.UnityPivot,
                    _package.PixelsPerMeter,
                    0,
                    SpriteMeshType.Tight);
                neutralRegistrationSprite.name =
                    $"{_package.DisplayName} Tight Neutral Registration {spec.Id}";
                CacheRegistrationLocalBounds(
                    neutralRegistrationSprite,
                    _neutralRegistrationLocalBounds,
                    _neutralRegistrationLocalBoundsValid,
                    index);
                DestroyTemporaryRegistrationSprite(neutralRegistrationSprite);

                if (nightTexture != null)
                {
                    nightTexture.wrapMode = TextureWrapMode.Clamp;
                    nightTexture.filterMode = FilterMode.Bilinear;
                    _nightOverlays[index] = Sprite.Create(
                        nightTexture,
                        new Rect(0f, 0f, nightTexture.width, nightTexture.height),
                        spec.UnityPivot,
                        _package.PixelsPerMeter,
                        0,
                        SpriteMeshType.FullRect);
                    _nightOverlays[index].name =
                        $"{_package.DisplayName} Night Lights {spec.Id}";
                }

                if (fullNightTexture != null)
                {
                    fullNightTexture.wrapMode = TextureWrapMode.Clamp;
                    fullNightTexture.filterMode = FilterMode.Bilinear;
                    _fullNightSprites[index] = Sprite.Create(
                        fullNightTexture,
                        new Rect(0f, 0f, fullNightTexture.width, fullNightTexture.height),
                        spec.UnityPivot,
                        _package.PixelsPerMeter,
                        0,
                        SpriteMeshType.FullRect);
                    _fullNightSprites[index].name =
                        $"{_package.DisplayName} Full Night {spec.Id}";
                    var fullNightRegistrationSprite = Sprite.Create(
                        fullNightTexture,
                        new Rect(0f, 0f,
                            fullNightTexture.width, fullNightTexture.height),
                        spec.UnityPivot,
                        _package.PixelsPerMeter,
                        0,
                        SpriteMeshType.Tight);
                    fullNightRegistrationSprite.name =
                        $"{_package.DisplayName} Tight Full Night Registration {spec.Id}";
                    CacheRegistrationLocalBounds(
                        fullNightRegistrationSprite,
                        _fullNightRegistrationLocalBounds,
                        _fullNightRegistrationLocalBoundsValid,
                        index);
                    DestroyTemporaryRegistrationSprite(fullNightRegistrationSprite);
                }

                for (var timeIndex = 0; timeIndex < 4; timeIndex++)
                {
                    var preset = (TimeOfDayPreset)timeIndex;
                    var shadePath = spec.ShadeResourcePath(preset);
                    if (string.IsNullOrWhiteSpace(shadePath)) continue;
                    var shadeTexture = Resources.Load<Texture2D>(shadePath);
                    if (shadeTexture == null)
                    {
                        Debug.LogError($"Missing directional shade overlay: {shadePath}");
                        continue;
                    }

                    shadeTexture.wrapMode = TextureWrapMode.Clamp;
                    shadeTexture.filterMode = FilterMode.Bilinear;
                    _shadeOverlays[index, timeIndex] = Sprite.Create(
                        shadeTexture,
                        new Rect(0f, 0f, shadeTexture.width, shadeTexture.height),
                        spec.UnityPivot,
                        _package.PixelsPerMeter,
                        0,
                        SpriteMeshType.FullRect);
                    _shadeOverlays[index, timeIndex].name =
                        $"{_package.DisplayName} {preset} Self Shade {spec.Id}";
                }
            }

            _visualRoot = new GameObject("Camera-Safe Billboard Layer").transform;
            _visualRoot.SetParent(transform, false);
            _visualRoot.localPosition = new Vector3(0f, 0f, -0.08f);
            _renderer = _visualRoot.gameObject.AddComponent<SpriteRenderer>();
            _renderer.name = "Directional Render";
            _renderer.allowOcclusionWhenDynamic = false;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.sortingOrder = 10;
            var alwaysVisibleShader = Shader.Find("CityForgeV3/AlwaysVisibleBuildingSprite");
            if (alwaysVisibleShader == null)
                throw new MissingReferenceException(
                    "CityForge V3 always-visible building sprite shader is required.");
            _alwaysVisibleMaterial = new Material(alwaysVisibleShader)
            {
                name = "CF Always Visible Building Sprite"
            };
            _alwaysVisibleMaterial.SetFloat("_BuildingHostStencilRef",
                _hostBuildingStencilReference);
            _renderer.sharedMaterial = _alwaysVisibleMaterial;
            var reflectionObject = new GameObject("Wet Street Reflection");
            reflectionObject.transform.SetParent(transform, false);
            _wetReflectionFilter = reflectionObject.AddComponent<MeshFilter>();
            _wetReflectionRenderer = reflectionObject.AddComponent<MeshRenderer>();
            _wetReflectionRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            _wetReflectionRenderer.receiveShadows = false;
            var reflectionShader = Shader.Find("CityForgeV3/WetStreetReflection");
            if (reflectionShader == null)
                throw new MissingReferenceException(
                    "City Forge V3 wet street reflection shader is required.");
            _wetReflectionMaterial = new Material(reflectionShader)
            {
                name = "CF Ground-Projected Wet Street Reflection",
                renderQueue = 2460
            };
            _wetReflectionRenderer.sharedMaterial = _wetReflectionMaterial;
            _wetReflectionMesh = new Mesh
            {
                name = "CF Screen-Mirrored Ground Projection"
            };
            _wetReflectionMesh.vertices = new Vector3[4];
            _wetReflectionMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            _wetReflectionMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _wetReflectionFilter.sharedMesh = _wetReflectionMesh;
            var shadeOverlayObject =
                new GameObject("Directional Light and Shade Overlay");
            shadeOverlayObject.transform.SetParent(_visualRoot, false);
            _shadeRenderer = shadeOverlayObject.AddComponent<SpriteRenderer>();
            _shadeRenderer.allowOcclusionWhenDynamic = false;
            _shadeRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            _shadeRenderer.receiveShadows = false;
            _shadeRenderer.sortingOrder = 11;
            _shadeRenderer.sharedMaterial = _alwaysVisibleMaterial;
            var nightOverlayObject =
                new GameObject("Directional Night Light Overlay");
            nightOverlayObject.transform.SetParent(_visualRoot, false);
            _nightRenderer = nightOverlayObject.AddComponent<SpriteRenderer>();
            _nightRenderer.allowOcclusionWhenDynamic = false;
            _nightRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            _nightRenderer.receiveShadows = false;
            _nightRenderer.sortingOrder = 12;
            _nightRenderer.sharedMaterial = _alwaysVisibleMaterial;
            transform.position = _package.PresentationAnchor;
            ApplyFacing(0);
        }

        private bool TryGetVisibleArtworkLocalBounds(out Bounds bounds)
        {
            bounds = default;
            if (_renderer == null || _facing < 0) return false;

            if (_fullNightSprites != null &&
                _facing < _fullNightSprites.Length &&
                _renderer.sprite == _fullNightSprites[_facing] &&
                TryGetCachedLocalBounds(
                    _fullNightRegistrationLocalBounds,
                    _fullNightRegistrationLocalBoundsValid,
                    _facing,
                    out bounds))
                return true;

            if (_neutralSprites != null &&
                _facing < _neutralSprites.Length &&
                _renderer.sprite == _neutralSprites[_facing] &&
                TryGetCachedLocalBounds(
                    _neutralRegistrationLocalBounds,
                    _neutralRegistrationLocalBoundsValid,
                    _facing,
                    out bounds))
                return true;

            return TryGetCachedLocalBounds(
                _registrationLocalBounds,
                _registrationLocalBoundsValid,
                _facing,
                out bounds);
        }

        private static bool TryGetCachedLocalBounds(
            Bounds[] localBounds,
            bool[] validBounds,
            int facing,
            out Bounds bounds)
        {
            bounds = default;
            if (localBounds == null || validBounds == null ||
                facing < 0 || facing >= localBounds.Length ||
                facing >= validBounds.Length || !validBounds[facing])
                return false;
            bounds = localBounds[facing];
            return true;
        }

        private static void CacheRegistrationLocalBounds(
            Sprite registrationSprite,
            Bounds[] localBounds,
            bool[] validBounds,
            int facing)
        {
            if (registrationSprite == null || localBounds == null ||
                validBounds == null || facing < 0 ||
                facing >= localBounds.Length || facing >= validBounds.Length)
                return;

            var vertices = registrationSprite.vertices;
            if (vertices == null || vertices.Length == 0) return;
            var minimum = vertices[0];
            var maximum = vertices[0];
            for (var index = 1; index < vertices.Length; index++)
            {
                minimum = Vector2.Min(minimum, vertices[index]);
                maximum = Vector2.Max(maximum, vertices[index]);
            }

            var center = (minimum + maximum) * 0.5f;
            var size = maximum - minimum;
            localBounds[facing] = new Bounds(
                new Vector3(center.x, center.y, 0f),
                new Vector3(size.x, size.y, 0f));
            validBounds[facing] = size.x > 0.001f && size.y > 0.001f;
        }

        private static void DestroyTemporaryRegistrationSprite(Sprite sprite)
        {
            if (sprite == null) return;
            if (Application.isPlaying)
                Object.Destroy(sprite);
            else
                Object.DestroyImmediate(sprite);
        }

        public void ApplyFacing(int facing)
        {
            _facing = _package.WrapFacing(facing);
            if (_renderer != null)
            {
                ApplyAppearance();
            }

            AlignToCamera();
        }

        public void SetArtworkSource(BuildingArtworkSource source)
        {
            _artworkSource = source;
            ApplyAppearance();
        }

        public void SetTimeOfDay(TimeOfDayPreset preset)
        {
            _timeOfDay = preset;
            ApplyAppearance();
        }

        public void SetSeason(SeasonPreset preset)
        {
            _season = preset;
            ApplyAppearance();
        }

        public void SetRain(bool isRaining)
        {
            _isRaining = isRaining;
            ApplyAppearance();
        }

        public void SetWetReflection(float strength, Vector3 worldDirection)
        {
            _wetReflectionStrength = Mathf.Clamp01(strength);
            if (worldDirection.sqrMagnitude > 0.0001f)
                _wetReflectionWorldDirection = worldDirection.normalized;
            ApplyAppearance();
        }

        public void SetBuildingRotation(int quarterTurns)
        {
            _buildingRotationQuarterTurns =
                FiveBayHybridContract.WrapFacing(quarterTurns);
            ApplyAppearance();
        }

        public void SetHostBuildingStencilReference(int reference)
        {
            _hostBuildingStencilReference = Mathf.Clamp(reference, 0, 252);
            if (_alwaysVisibleMaterial != null)
                _alwaysVisibleMaterial.SetFloat("_BuildingHostStencilRef",
                    _hostBuildingStencilReference);
        }

        public void RegisterToProxy(
            IReadOnlyList<Vector3> proxyLocalVertices,
            Quaternion buildingRotation)
        {
            if (_package != null && _package.UsesPersistedArtworkPivot)
            {
                // Source-derived packages already share a metric origin and
                // persist the exact foundation-center sprite pivot produced by
                // their render camera. Re-centering their tight alpha bounds
                // against an intentionally eroded occluder shifts the artwork
                // away from that architectural anchor.
                _proxyRegistrationScale = 1f;
                _proxyRegistrationOffset = Vector2.zero;
                AlignToCamera();
                return;
            }

            if (_camera == null || _visualRoot == null ||
                proxyLocalVertices == null || proxyLocalVertices.Count == 0 ||
                _registrationSprites == null ||
                _facing < 0 || _facing >= _registrationSprites.Length ||
                _registrationSprites[_facing] == null)
            {
                _proxyRegistrationScale = 1f;
                _proxyRegistrationOffset = Vector2.zero;
                AlignToCamera();
                return;
            }

            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            foreach (var localVertex in proxyLocalVertices)
            {
                var rotated = buildingRotation * localVertex;
                var projectedX = Vector3.Dot(rotated, _camera.transform.right);
                var projectedY = Vector3.Dot(rotated, _camera.transform.up);
                minX = Mathf.Min(minX, projectedX);
                maxX = Mathf.Max(maxX, projectedX);
                minY = Mathf.Min(minY, projectedY);
                maxY = Mathf.Max(maxY, projectedY);
            }

            var artworkBounds = _registrationSprites[_facing].bounds;
            if (artworkBounds.size.x <= 0.001f || artworkBounds.size.y <= 0.001f)
                return;

            var widthScale = (maxX - minX) / artworkBounds.size.x;
            var heightScale = (maxY - minY) / artworkBounds.size.y;
            // Preserve the artwork's aspect ratio. A non-uniform fit would
            // conceal a bad proxy by visibly distorting the approved render.
            // The larger ratio is the smallest uniform scale that contains
            // the proxy projection; any remaining visible diagnostic volume
            // is therefore a real silhouette mismatch, not an anchor error.
            _proxyRegistrationScale = Mathf.Max(1f, widthScale, heightScale);
            var proxyCenter = new Vector2(
                (minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f);
            var artworkCenter = new Vector2(
                artworkBounds.center.x,
                artworkBounds.center.y) * _proxyRegistrationScale;
            _proxyRegistrationOffset = proxyCenter - artworkCenter;
            AlignToCamera();
        }

        public TimeOfDayPreset DirectionalShadePreset =>
            ShadePresetForRotation(_timeOfDay, _buildingRotationQuarterTurns);

        public static TimeOfDayPreset ShadePresetForRotation(
            TimeOfDayPreset preset,
            int buildingRotationQuarterTurns)
        {
            // Each facing's registered daylight overlay was rendered from the
            // same fixed world-space sun. Changing the presentation facing is
            // therefore the complete rotation adjustment. Cycling Morning,
            // Noon, Afternoon, and Evening here treated time presets as four
            // compass points and double-rotated the lighting.
            return preset;
        }

        public static bool SupportsNeutralPilot(int facing)
        {
            var package = HybridBuildingPackageRegistry.GovernmentHouse;
            var wrapped = package.WrapFacing(facing);
            return wrapped >= 0 && wrapped < package.FacingCount;
        }

        private void ApplyAppearance()
        {
            if (_renderer == null)
            {
                return;
            }

            // During an editor domain reload, surviving scene presentations can
            // receive a time-of-day refresh before their package data is rebound.
            // Leave their current artwork intact until initialization completes.
            if (_package == null)
            {
                _renderer.enabled = _visible;
                return;
            }

            // Appearance refreshes happen after placement, selection, movement,
            // time-of-day changes, and package-facing changes. Reassert the
            // presentation's authoritative visibility here so a refresh cannot
            // leave complete artwork disabled while its proxy remains available.
            _renderer.enabled = _visible;

            // Full-night artwork is a package capability, not a Neutral Pilot
            // capability. Production packages can be shown through the approved
            // artwork path while still declaring a complete evening/night render.
            var useFullNight =
                (_timeOfDay == TimeOfDayPreset.Evening ||
                 _timeOfDay == TimeOfDayPreset.Night) &&
                _fullNightSprites != null &&
                _facing >= 0 && _facing < _fullNightSprites.Length &&
                _fullNightSprites[_facing] != null;

            if (useFullNight)
            {
                _renderer.sprite = _fullNightSprites[_facing];
                _renderer.color = Color.white;
            }
            else if (NeutralPilotShowing &&
                     _neutralSprites != null &&
                     _facing >= 0 && _facing < _neutralSprites.Length &&
                     _neutralSprites[_facing] != null)
            {
                _renderer.sprite = _neutralSprites[_facing];
                var shadePreset = DirectionalShadePreset;
                var hasDirectionalShade =
                    _timeOfDay != TimeOfDayPreset.Night &&
                    _shadeOverlays != null &&
                    _facing >= 0 && _facing < _shadeOverlays.GetLength(0) &&
                    (int)shadePreset < _shadeOverlays.GetLength(1) &&
                    _shadeOverlays[_facing, (int)shadePreset] != null;
                _renderer.color = NeutralBaseTintFor(
                    _timeOfDay,
                    hasDirectionalShade);
            }
            else
            {
                _renderer.sprite = _sprites != null &&
                                   _facing >= 0 && _facing < _sprites.Length
                    ? _sprites[_facing]
                    : null;
                _renderer.color = Color.white;
            }

            if (_shadeRenderer != null)
            {
                var daytime = _timeOfDay != TimeOfDayPreset.Night;
                var shadePresetIndex = (int)DirectionalShadePreset;
                var hasShadeOverlay = daytime &&
                                      _shadeOverlays != null &&
                                      _facing >= 0 &&
                                      _facing < _shadeOverlays.GetLength(0) &&
                                      shadePresetIndex >= 0 &&
                                      shadePresetIndex < _shadeOverlays.GetLength(1);
                _shadeRenderer.sprite = hasShadeOverlay
                    ? _shadeOverlays[_facing, (int)DirectionalShadePreset]
                    : null;
                _shadeRenderer.color = new Color(
                    1f, 1f, 1f,
                    _package.ShadeOpacity(
                        _timeOfDay,
                        DirectionalShadeOpacityFor(_timeOfDay)) *
                    MorningAfternoonShadowOpacityScale(_timeOfDay));
                _shadeRenderer.enabled =
                    _visible && NeutralPilotShowing && !useFullNight && !_isRaining &&
                    _shadeRenderer.sprite != null;
            }

            if (_nightRenderer != null)
            {
                _nightRenderer.sprite = _nightOverlays[_facing];
                _nightRenderer.color =
                    _timeOfDay == TimeOfDayPreset.Night
                        ? new Color(
                            1.10f, 1.02f, 0.92f,
                            _package.NightOverlayOpacity)
                        : new Color(1f, 0.86f, 0.68f, 0.38f);
                _nightRenderer.enabled =
                    _visible &&
                    NeutralPilotShowing &&
                    !useFullNight &&
                    (_timeOfDay == TimeOfDayPreset.Evening ||
                     _timeOfDay == TimeOfDayPreset.Night);
            }

            if (_wetReflectionRenderer != null)
            {
                _wetReflectionRenderer.enabled = _visible &&
                    _wetReflectionStrength > 0.001f && _renderer.sprite != null;
                if (_wetReflectionMaterial != null && _renderer.sprite != null)
                {
                    _wetReflectionMaterial.mainTexture =
                        _renderer.sprite.texture;
                    _wetReflectionMaterial.SetFloat(
                        "_Wetness", _wetReflectionStrength);
                    _wetReflectionMaterial.SetFloat(
                        "_RainActive", _isRaining ? 1f : 0f);
                }
            }

            ApplyOpacity(_renderer);
            ApplyOpacity(_shadeRenderer);
            ApplyOpacity(_nightRenderer);

            _renderer.color = SeasonLighting.Multiply(
                _renderer.color, SeasonLighting.BuildingTint(_season));
        }

        public static float DirectionalShadeOpacityFor(
            TimeOfDayPreset preset) => preset switch
        {
            TimeOfDayPreset.Morning => 0.55f,
            // Noon is a high, hard sun—not an exposure boost. Keep enough of
            // the registered pass to describe short directional shadows while
            // avoiding the bleached, giant-spotlight appearance on pale trim.
            TimeOfDayPreset.Noon => 0.42f,
            // A due-west afternoon sun leaves the east/right facade in shade.
            // The hybrid artwork cannot receive directional light per face,
            // so this shared shade pass restores that contrast.
            TimeOfDayPreset.Afternoon => 0.2375f,
            TimeOfDayPreset.Evening => 0.55f,
            _ => 0f
        };

        public static float MorningAfternoonShadowOpacityScale(
            TimeOfDayPreset preset) =>
            preset is TimeOfDayPreset.Morning or TimeOfDayPreset.Afternoon
                ? 0.70f
                : 1f;

        private void ApplyOpacity(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null) return;
            var color = spriteRenderer.color;
            color.a *= _opacity;
            spriteRenderer.color = color;
        }

        public static Color NeutralBaseTintFor(
            TimeOfDayPreset preset,
            bool hasDirectionalOverlay)
        {
            // Hybrid sprites already contain their neutral material response.
            // Keep dusk and night readable while the environment darkens;
            // window overlays should accent the building, not become the only
            // visible part of it.
            if (preset == TimeOfDayPreset.Evening)
            {
                return new Color(0.66f, 0.69f, 0.76f);
            }

            if (preset == TimeOfDayPreset.Night)
            {
                return new Color(0.38f, 0.43f, 0.54f);
            }

            if (!hasDirectionalOverlay)
            {
                return TimeOfDayLighting.For(preset).NeutralArtworkTint;
            }

            return Color.white;
        }

        private void AlignToCamera(Camera renderCamera)
        {
            if (renderCamera != null && _visualRoot != null)
            {
                // Keep the placement anchor free of presentation rotation.
                // The visible plane receives the camera's WORLD basis
                // directly. This remains screen-upright even when a rebuilt
                // presentation inherits a non-identity parent transform.
                transform.rotation = Quaternion.identity;
                _visualRoot.rotation = renderCamera.transform.rotation;
                _visualRoot.position =
                    transform.position - renderCamera.transform.forward * 0.08f +
                    renderCamera.transform.right * _proxyRegistrationOffset.x +
                    renderCamera.transform.up * _proxyRegistrationOffset.y;
                _visualRoot.localScale = Vector3.one * _proxyRegistrationScale;
                ProjectWetReflectionOntoRoad(renderCamera);
            }
        }

        private void ProjectWetReflectionOntoRoad(Camera renderCamera)
        {
            if (_wetReflectionMesh == null || _renderer == null ||
                _renderer.sprite == null || renderCamera == null) return;

            var bounds = _renderer.sprite.bounds;
            var spriteCorners = new[]
            {
                new Vector3(bounds.min.x, bounds.min.y, 0f),
                new Vector3(bounds.max.x, bounds.min.y, 0f),
                new Vector3(bounds.max.x, bounds.max.y, 0f),
                new Vector3(bounds.min.x, bounds.max.y, 0f)
            };
            var foundationScreenY = renderCamera.WorldToScreenPoint(
                transform.position).y;
            var roadPlane = new Plane(Vector3.up,
                new Vector3(0f, 0.058f, 0f));
            var vertices = new Vector3[4];
            for (var index = 0; index < spriteCorners.Length; index++)
            {
                var worldCorner = _renderer.transform.TransformPoint(
                    spriteCorners[index]);
                var screenCorner = renderCamera.WorldToScreenPoint(worldCorner);
                screenCorner.y = 2f * foundationScreenY - screenCorner.y;
                var ray = renderCamera.ScreenPointToRay(screenCorner);
                if (!roadPlane.Raycast(ray, out var distance)) continue;
                vertices[index] = transform.InverseTransformPoint(
                    ray.GetPoint(distance));
            }
            _wetReflectionMesh.vertices = vertices;
            _wetReflectionMesh.RecalculateBounds();
        }

        public void AlignToCamera()
        {
            AlignToCamera(_camera);
        }

        private void OnWillRenderObject()
        {
            // Reassert against the final camera transform used for this draw.
            // This closes the ordering gap between UI-driven camera framing
            // and billboard LateUpdate calls in the live Lot Editor.
            // Camera.current is the camera that is actually drawing this
            // renderer. Using only the cached lot camera left the billboard
            // aligned to one camera while a bootstrap camera displayed it,
            // which produced the rolled, enormous building seen in Game view.
            AlignToCamera(Camera.current != null ? Camera.current : _camera);
        }

        private void LateUpdate()
        {
            // Camera zoom/view changes can occur after package facing is
            // applied. Keep the artwork screen-upright on the final camera
            // transform used for this frame.
            AlignToCamera();
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_renderer != null)
            {
                _renderer.enabled = visible;
            }

            ApplyAppearance();
        }

        public void SetOpacity(float opacity)
        {
            _opacity = Mathf.Clamp01(opacity);
            ApplyAppearance();
        }
    }
}
