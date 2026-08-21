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
        private Material _alwaysVisibleMaterial;
        private Transform _visualRoot;
        private Sprite[] _sprites;
        private Sprite[] _registrationSprites;
        private Sprite[] _neutralSprites;
        private Sprite[] _nightOverlays;
        private Sprite[] _fullNightSprites;
        private Sprite[,] _shadeOverlays;
        private int _facing;
        private bool _visible;
        private float _opacity = 1f;
        private BuildingArtworkSource _artworkSource;
        private TimeOfDayPreset _timeOfDay = TimeOfDayPreset.Noon;
        private SeasonPreset _season = SeasonPreset.Summer;
        private int _buildingRotationQuarterTurns;
        private float _proxyRegistrationScale = 1f;
        private Vector2 _proxyRegistrationOffset;

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

        public void SetSortingOrder(int order)
        {
            if (_renderer != null) _renderer.sortingOrder = order;
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
            _neutralSprites = new Sprite[_package.FacingCount];
            _nightOverlays = new Sprite[_package.FacingCount];
            _fullNightSprites = new Sprite[_package.FacingCount];
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
            _renderer.sharedMaterial = _alwaysVisibleMaterial;
            var shadeOverlayObject =
                new GameObject("Directional Light and Shade Overlay");
            shadeOverlayObject.transform.SetParent(_visualRoot, false);
            _shadeRenderer = shadeOverlayObject.AddComponent<SpriteRenderer>();
            _shadeRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            _shadeRenderer.receiveShadows = false;
            _shadeRenderer.sortingOrder = 11;
            _shadeRenderer.sharedMaterial = _alwaysVisibleMaterial;
            var nightOverlayObject =
                new GameObject("Directional Night Light Overlay");
            nightOverlayObject.transform.SetParent(_visualRoot, false);
            _nightRenderer = nightOverlayObject.AddComponent<SpriteRenderer>();
            _nightRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            _nightRenderer.receiveShadows = false;
            _nightRenderer.sortingOrder = 12;
            _nightRenderer.sharedMaterial = _alwaysVisibleMaterial;
            transform.position = _package.PresentationAnchor;
            ApplyFacing(0);
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

        public void SetBuildingRotation(int quarterTurns)
        {
            _buildingRotationQuarterTurns =
                FiveBayHybridContract.WrapFacing(quarterTurns);
            ApplyAppearance();
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
                _fullNightSprites[_facing] != null;

            if (useFullNight)
            {
                _renderer.sprite = _fullNightSprites[_facing];
                _renderer.color = Color.white;
            }
            else if (NeutralPilotShowing && _neutralSprites[_facing] != null)
            {
                _renderer.sprite = _neutralSprites[_facing];
                var shadePreset = DirectionalShadePreset;
                var hasDirectionalShade =
                    _timeOfDay != TimeOfDayPreset.Night &&
                    _shadeOverlays[_facing, (int)shadePreset] != null;
                _renderer.color = NeutralBaseTintFor(
                    _timeOfDay,
                    hasDirectionalShade);
            }
            else
            {
                _renderer.sprite = _sprites[_facing];
                _renderer.color = Color.white;
            }

            if (_shadeRenderer != null)
            {
                var daytime = _timeOfDay != TimeOfDayPreset.Night;
                _shadeRenderer.sprite = daytime
                    ? _shadeOverlays[_facing, (int)DirectionalShadePreset]
                    : null;
                _shadeRenderer.color = new Color(
                    1f, 1f, 1f,
                    _package.ShadeOpacity(
                        _timeOfDay,
                        DirectionalShadeOpacityFor(_timeOfDay)));
                _shadeRenderer.enabled =
                    _visible && NeutralPilotShowing && !useFullNight &&
                    _shadeRenderer.sprite != null;
            }

            if (_nightRenderer != null)
            {
                _nightRenderer.sprite = _nightOverlays[_facing];
                _nightRenderer.color =
                    _timeOfDay == TimeOfDayPreset.Night
                        ? new Color(1.10f, 1.02f, 0.92f, 1f)
                        : new Color(1f, 0.86f, 0.68f, 0.38f);
                _nightRenderer.enabled =
                    _visible &&
                    NeutralPilotShowing &&
                    !useFullNight &&
                    (_timeOfDay == TimeOfDayPreset.Evening ||
                     _timeOfDay == TimeOfDayPreset.Night);
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
            // Registered overlays describe directional variation; they do not
            // replace the low ambient exposure of dusk or night.
            if (!hasDirectionalOverlay || preset == TimeOfDayPreset.Evening)
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
            }
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
