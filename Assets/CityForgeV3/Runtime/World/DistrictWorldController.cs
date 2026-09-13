using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public readonly struct RiverSurfaceSample
    {
        public readonly bool InsideChannel;
        public readonly bool UnderWater;
        public readonly float BedElevation;
        public readonly float WaterElevation;
        public readonly float WaterDepth;
        public readonly float DistanceFromCenter;
        public readonly Vector3 DownstreamDirection;

        public RiverSurfaceSample(bool insideChannel, bool underWater,
            float bedElevation, float waterElevation, float waterDepth,
            float distanceFromCenter, Vector3 downstreamDirection)
        {
            InsideChannel = insideChannel;
            UnderWater = underWater;
            BedElevation = bedElevation;
            WaterElevation = waterElevation;
            WaterDepth = waterDepth;
            DistanceFromCenter = distanceFromCenter;
            DownstreamDirection = downstreamDirection;
        }
    }

    /// <summary>
    /// A district is a metre-scaled world containing metre-scaled saved lots.
    /// It deliberately shares LotMetricScale instead of maintaining a second
    /// visual scale or a screenshot representation of a lot.
    /// </summary>
    public sealed partial class DistrictWorldController : MonoBehaviour
    {
        public const string DefaultGrassResource =
            "CityForgeV3/Art/Regions/default-grass-texture";
        // The entered district again uses the established default grass. The
        // four newer authored variants remain in Resources for optional use.
        public const string DistrictGrassResource = DefaultGrassResource;
        public const string RiverBedResource =
            "CityForgeV3/Water/River/river-bed";
        public const string RiverBedBorderResource =
            "CityForgeV3/Water/River/river-bed-border";
        public const string RiverEdge01Resource =
            "CityForgeV3/Water/River/01-river-edge-border";
        public const string RiverShallow02Resource =
            "CityForgeV3/Water/River/02-river-texture-shallow";
        public const string RiverTransition03Resource =
            "CityForgeV3/Water/River/03-river-side-deeper-transition";
        public const string RiverMiddle04Resource =
            "CityForgeV3/Water/River/04-river-middle-deep";
        public const string RiverGrassToDirtResource =
            "CityForgeV3/Water/River/01-grass-to-dirt";
        public static readonly string[] RiverGrassBankVariantResources =
        {
            "CityForgeV3/Water/River/01-river-bank-to-grass-1",
            "CityForgeV3/Water/River/01-river-bank-to-grass-2"
        };
        public const string RiverBedDirtResource =
            "CityForgeV3/Water/River/02-river-bed-dirt";
        public const string RiverWaterTextureResource =
            "CityForgeV3/Water/River/river-texture";
        public const string RiverWhitecapTextureResource =
            "CityForgeV3/Water/River/white-cap-river";
        private const float RiverBedTextureWorldSizeMeters = 48f;
        private const float RiverBedTransitionWidthMeters = 14f;
        public const float GrassTextureWorldSizeMeters = 5f;
        public const float DistrictGrassTextureWorldSizeMeters = 5f;
        private const float HostedLotFacingOffsetDegrees = 180f;

        [Header("River Water")]
        [SerializeField, InspectorName("Water Height")]
        private float _waterHeight = -0.06f;
        [SerializeField, Range(0f, 1f), InspectorName("Center Opacity")]
        private float _waterOpacity = 0.72f;
        [SerializeField, Range(0f, 1f), InspectorName("Edge Opacity")]
        private float _waterEdgeOpacity = 0.08f;
        [SerializeField, Range(0.05f, 0.45f), InspectorName("Edge Fade Width")]
        private float _waterEdgeFadeWidth = 0.22f;
        [SerializeField, Range(0f, 1f), InspectorName("Deep Water Start")]
        private float _deepWaterStart = 0.24f;
        [SerializeField, Range(0f, 1f), InspectorName("Deep Water Strength")]
        private float _deepWaterStrength = 0.58f;
        [SerializeField, Range(0.02f, 1f), InspectorName("Depth Blend Softness")]
        private float _depthBlendSoftness = 0.56f;
        [SerializeField, Min(0.25f), InspectorName("Water Texture Tiling")]
        private float _waterTextureTiling = 18f;
        [SerializeField, InspectorName("Water Tint")]
        private Color _waterTint = new(0.94f, 1.02f, 1.06f, 1f);
        [SerializeField, Range(0.1f, 2f), InspectorName("Brightness")]
        private float _waterBrightness = 1.08f;
        [SerializeField, Range(0f, 1f), InspectorName("Smoothness")]
        private float _waterSmoothness = 0.62f;
        [SerializeField, Range(-0.1f, 0.1f), InspectorName("Flow Speed")]
        private float _waterFlowSpeed = 0.06f;
        [SerializeField, Range(0f, 0.08f), InspectorName("Wave Distortion")]
        private float _waterWaveDistortion = 0.025f;
        [SerializeField, Range(0.1f, 4f), InspectorName("Wave Scale")]
        private float _waterWaveScale = 0.75f;
        [SerializeField, Range(0f, 2f), InspectorName("Wave Speed")]
        private float _waterWaveSpeed = 0.28f;
        [SerializeField, Range(0f, 1f), InspectorName("Reflection Strength")]
        private float _waterReflectionStrength = 0.18f;
        [SerializeField, Range(0f, 1f), InspectorName("Shimmer Strength")]
        private float _waterShimmerStrength = 0.24f;
        [SerializeField, Range(0f, 2f), InspectorName("Shimmer Speed")]
        private float _waterShimmerSpeed = 0.35f;
        [SerializeField, Range(0f, 1f), InspectorName("Whitecap Strength")]
        private float _whitecapStrength = 0.32f;
        [SerializeField, Range(0.05f, 1f), InspectorName("Whitecap Coverage")]
        private float _whitecapCoverage = 0.55f;
        [SerializeField, Range(0.1f, 4f), InspectorName("Whitecap Tiling")]
        private float _whitecapTiling = 0.85f;
        [SerializeField, Range(0.1f, 3f), InspectorName("Whitecap Speed")]
        private float _whitecapSpeed = 1.35f;
        [SerializeField, Range(0f, 2f), InspectorName("Whitecap Pulse Speed")]
        private float _whitecapPulseSpeed = 0.12f;
        private readonly List<LotWorldController> _lots = new();
        private readonly Dictionary<string, LotWorldController> _lotsByInstance =
            new();
        private readonly Dictionary<Vector2Int, GameObject> _roadsByCell = new();
        private readonly List<RuntimeRiverSurface> _riverSurfaces = new();
        private readonly List<Renderer> _riverGrassEdgeRenderers = new();
        private Camera _camera;
        private Light _sun;
        private Transform _content;
        private Transform _grid;
        private Transform _roadArtworkRoot;
        private Transform _riverRoot;
        private Transform _districtFloraRoot;
        private LineRenderer _districtFloraSelection;
        private readonly Dictionary<string, SpriteRenderer>
            _districtFloraPresentations = new();
        private readonly Dictionary<string, Sprite> _districtFloraSprites = new();
        private Material _districtFloraMaterial;
        private Material _districtFloraShadowMaterial;
        private Transform _districtSelectionRoot;
        private Material _districtSelectionMaterial;
        private GameObject _minorGrid;
        private GameObject _majorGrid;
        private GameObject _placementGuide;
        private Mesh _placementGuideMesh;
        private Material _placementGuideMaterial;
        private GameObject _lotOutline;
        private LineRenderer _lotOutlineRenderer;
        private Renderer _groundRenderer;
        private DistrictGroundDecals _groundDecals;
        private float _widthMeters;
        private float _depthMeters;
        private Vector3 _pan;
        private DistrictZoomLevel _zoomLevel = DistrictZoom.DefaultLevel;
        private bool _districtGridVisible = true;

        public int LiveLotCount => _lots.Count;
        public Camera WorldCamera => _camera;
        public Light WorldSun => _sun;
        public DistrictZoomLevel ZoomLevel => _zoomLevel;
        public bool GridVisible => _districtGridVisible;
        public float WaterHeight
        {
            get => _waterHeight;
            set => _waterHeight = Mathf.Clamp(value, -5f, -0.05f);
        }
        public float WaterOpacity
        {
            get => _waterOpacity;
            set => _waterOpacity = Mathf.Clamp01(value);
        }
        public float WaterEdgeOpacity
        {
            get => _waterEdgeOpacity;
            set => _waterEdgeOpacity = Mathf.Clamp01(value);
        }
        public float WaterEdgeFadeWidth
        {
            get => _waterEdgeFadeWidth;
            set => _waterEdgeFadeWidth = Mathf.Clamp(value, 0.05f, 0.45f);
        }
        public float DeepWaterStart
        {
            get => _deepWaterStart;
            set => _deepWaterStart = Mathf.Clamp01(value);
        }
        public float DeepWaterStrength
        {
            get => _deepWaterStrength;
            set => _deepWaterStrength = Mathf.Clamp01(value);
        }
        public float DepthBlendSoftness
        {
            get => _depthBlendSoftness;
            set => _depthBlendSoftness = Mathf.Clamp(value, 0.02f, 1f);
        }
        public float WaterTextureTiling
        {
            get => _waterTextureTiling;
            set => _waterTextureTiling = Mathf.Max(0.25f, value);
        }
        public Color WaterTint
        {
            get => _waterTint;
            set => _waterTint = value;
        }
        public float WaterBrightness
        {
            get => _waterBrightness;
            set => _waterBrightness = Mathf.Clamp(value, 0.1f, 2f);
        }
        public float WaterSmoothness
        {
            get => _waterSmoothness;
            set => _waterSmoothness = Mathf.Clamp01(value);
        }
        public float WaterFlowSpeed
        {
            get => _waterFlowSpeed;
            set => _waterFlowSpeed = Mathf.Clamp(value, -0.1f, 0.1f);
        }
        public float WaterWaveDistortion
        {
            get => _waterWaveDistortion;
            set => _waterWaveDistortion = Mathf.Clamp(value, 0f, 0.08f);
        }
        public float WaterWaveScale
        {
            get => _waterWaveScale;
            set => _waterWaveScale = Mathf.Clamp(value, 0.1f, 4f);
        }
        public float WaterWaveSpeed
        {
            get => _waterWaveSpeed;
            set => _waterWaveSpeed = Mathf.Clamp(value, 0f, 2f);
        }
        public float WaterReflectionStrength
        {
            get => _waterReflectionStrength;
            set => _waterReflectionStrength = Mathf.Clamp01(value);
        }
        public float WaterShimmerStrength
        {
            get => _waterShimmerStrength;
            set => _waterShimmerStrength = Mathf.Clamp01(value);
        }
        public float WaterShimmerSpeed
        {
            get => _waterShimmerSpeed;
            set => _waterShimmerSpeed = Mathf.Clamp(value, 0f, 2f);
        }
        public TimeOfDayPreset TimeOfDay { get; private set; } =
            TimeOfDayPreset.Noon;

        public void Build(RegionCityTile district)
        {
            ClearWorld();
            if (district == null) return;
            _widthMeters = DistrictScale.SizeMeters(district.Width);
            _depthMeters = DistrictScale.SizeMeters(district.Height);
            _content = new GameObject("District World Content").transform;
            _content.SetParent(transform, false);
            BuildCamera();
            // Flora copies the camera rotation when created. Establish the
            // viewing pose first, including when reloading a saved district.
            ApplyCameraPose();
            BuildSun();
            BuildGround();
            RefreshRivers(district);
            RefreshRoads(district);
            RefreshFlora(district);
            BuildGrid();
            var placements = district.Lots ?? new List<PlacedDistrictLot>();
            foreach (var placement in placements)
            {
                if (placement == null || string.IsNullOrWhiteSpace(placement.LotId))
                    continue;
                var lot = LotContentCatalog.Read(placement.LotId);
                if (lot == null) continue;
                AddLot(lot, DistrictLotCenterMeters(district, placement, lot),
                    placement.RotationQuarterTurns, placement.InstanceId);
            }
            // Region v1 stored only its founder lot. Preserve those saves by
            // projecting the legacy normalized position onto the shared grid.
            if (_lots.Count == 0 && district.Founded &&
                !string.IsNullOrWhiteSpace(district.LotId))
            {
                var founderLot = LotContentCatalog.Read(district.LotId);
                if (founderLot != null)
                {
                    var center = DistrictLotCenterMeters(district,
                        district.FounderNormalizedX,
                        district.FounderNormalizedY,
                        founderLot.LotWidthCells * LotMetricScale.MajorGridMeters,
                        founderLot.LotDepthCells * LotMetricScale.MajorGridMeters);
                    AddLot(founderLot, center, 0, "legacy-founder");
                }
            }
            SetZoom(DistrictZoom.DefaultLevel);
            SetTimeOfDay(district.TimeOfDay);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void RefreshRoads(RegionCityTile district)
        {
            if (_content == null || district == null) return;
            if (_roadArtworkRoot != null)
            {
                var old = _roadArtworkRoot.gameObject;
                if (Application.isPlaying) Destroy(old); else DestroyImmediate(old);
            }
            _roadArtworkRoot = new GameObject("District Roads").transform;
            _roadArtworkRoot.SetParent(_content, false);
            _roadsByCell.Clear();
            foreach (var road in district.Roads ?? new List<PlacedRoadPiece>())
                AddRoadPiece(road);
        }

        public void RefreshRivers(RegionCityTile district)
        {
            if (_content == null || district == null) return;
            if (_riverRoot != null)
            {
                var old = _riverRoot.gameObject;
                if (Application.isPlaying) Destroy(old);
                else DestroyImmediate(old);
            }
            _riverRoot = new GameObject("District Rivers").transform;
            _riverRoot.SetParent(_content, false);
            _riverSurfaces.Clear();
            _riverGrassEdgeRenderers.Clear();
            foreach (var river in district.Rivers ??
                     new List<PlacedDistrictRiver>())
                BuildRiver(river);
            ApplyRiverGrassEdgeVisibility();
            if (_groundDecals == null)
            {
                var decals = new GameObject("Default District Grass Decals");
                decals.transform.SetParent(_content, false);
                _groundDecals = decals.AddComponent<DistrictGroundDecals>();
            }
            _groundDecals.Rebuild(this, district, _widthMeters, _depthMeters);
        }

        public void RefreshFlora(RegionCityTile district,
            string selectedInstanceId = "")
        {
            if (_content == null || district == null) return;
            if (_districtFloraRoot != null)
            {
                var old = _districtFloraRoot.gameObject;
                if (Application.isPlaying) Destroy(old);
                else DestroyImmediate(old);
            }
            _districtFloraPresentations.Clear();
            _districtFloraRoot = new GameObject("District Flora").transform;
            _districtFloraRoot.SetParent(_content, false);
            foreach (var placed in district.Flora ??
                     new List<PlacedDistrictFlora>())
                AddDistrictFloraPresentation(placed);
            UpdateDistrictFloraShadows();
            BuildDistrictFloraSelection(selectedInstanceId);
        }

        public string FindDistrictFloraAt(Vector2 normalized,
            float radiusMeters = 4f)
        {
            var point = new Vector2((normalized.x - .5f) * _widthMeters,
                (normalized.y - .5f) * _depthMeters);
            var best = radiusMeters;
            var found = "";
            foreach (var pair in _districtFloraPresentations)
            {
                if (pair.Value == null) continue;
                var p = pair.Value.transform.localPosition;
                var distance = Vector2.Distance(point, new Vector2(p.x, p.z));
                if (distance >= best) continue;
                best = distance;
                found = pair.Key;
            }
            return found;
        }

        public string FindDistrictFloraAtPanel(Vector2 panelPosition,
            float paddingPixels = 14f)
        {
            if (_camera == null) return "";
            var found = "";
            var bestDistance = float.PositiveInfinity;
            foreach (var pair in _districtFloraPresentations)
            {
                var renderer = pair.Value;
                if (renderer == null || !renderer.gameObject.activeInHierarchy)
                    continue;
                var bounds = renderer.bounds;
                var min = new Vector2(float.PositiveInfinity,
                    float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity,
                    float.NegativeInfinity);
                for (var corner = 0; corner < 8; corner++)
                {
                    var world = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((corner & 1) == 0 ? -1f : 1f,
                            (corner & 2) == 0 ? -1f : 1f,
                            (corner & 4) == 0 ? -1f : 1f));
                    var projected = _camera.WorldToScreenPoint(world);
                    if (projected.z <= 0f) continue;
                    var panel = new Vector2(projected.x,
                        Screen.height - projected.y);
                    min = Vector2.Min(min, panel);
                    max = Vector2.Max(max, panel);
                }
                if (float.IsInfinity(min.x)) continue;
                var rect = Rect.MinMaxRect(min.x - paddingPixels,
                    min.y - paddingPixels, max.x + paddingPixels,
                    max.y + paddingPixels);
                if (!rect.Contains(panelPosition)) continue;
                var distance = Vector2.SqrMagnitude(panelPosition -
                    new Vector2((min.x + max.x) * .5f,
                        (min.y + max.y) * .5f));
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                found = pair.Key;
            }
            return found;
        }

        public void SelectDistrictFlora(string instanceId)
        {
            BuildDistrictFloraSelection(instanceId);
        }

        public void MoveDistrictFlora(PlacedDistrictFlora placed)
        {
            if (placed == null || !_districtFloraPresentations.TryGetValue(
                    placed.InstanceId, out var renderer) || renderer == null)
                return;
            renderer.transform.localPosition = DistrictFloraPosition(placed);
            renderer.sortingOrder = DistrictFloraSortingOrder(
                renderer.transform.localPosition);
            BuildDistrictFloraSelection(placed.InstanceId);
        }

        public void ShowDistrictSelection(RegionCityTile district,
            IReadOnlyList<DistrictSelectionRef> selection)
        {
            if (_content == null || district == null) return;
            if (_districtSelectionRoot != null)
            {
                var old = _districtSelectionRoot.gameObject;
                if (Application.isPlaying) Destroy(old);
                else DestroyImmediate(old);
            }
            _districtSelectionRoot = new GameObject(
                "District Multi Selection").transform;
            _districtSelectionRoot.SetParent(_content, false);
            _districtSelectionMaterial ??= new Material(
                Shader.Find("Sprites/Default"))
            {
                name = "District Multi Selection Material"
            };
            foreach (var item in selection ??
                     System.Array.Empty<DistrictSelectionRef>())
            {
                if (item.Kind == DistrictSelectionKind.River)
                {
                    var river = district.Rivers?.Find(candidate =>
                        candidate != null && candidate.InstanceId == item.Id);
                    if (river?.Points == null || river.Points.Count < 2)
                        continue;
                    var riverObject = new GameObject(
                        $"Selected River — {item.Id}");
                    riverObject.transform.SetParent(_districtSelectionRoot,
                        false);
                    var riverLine = riverObject.AddComponent<LineRenderer>();
                    riverLine.useWorldSpace = false;
                    riverLine.loop = false;
                    riverLine.positionCount = river.Points.Count;
                    // Keep the selection stroke legible at district scale.
                    // The previous .28 m line became sub-pixel at the wider
                    // zoom levels, making a successfully selected river look
                    // unselected. Target roughly four screen pixels and clamp
                    // the result so close zooms do not produce a huge stripe.
                    var cameraHeight = Mathf.Max(1f, _camera.pixelHeight);
                    var visibleWorldHeight = _camera.orthographic
                        ? _camera.orthographicSize * 2f
                        : Mathf.Max(_widthMeters, _depthMeters);
                    riverLine.widthMultiplier = Mathf.Clamp(
                        visibleWorldHeight * 4f / cameraHeight, .8f, 4f);
                    riverLine.sharedMaterial = _districtSelectionMaterial;
                    riverLine.startColor = riverLine.endColor =
                        new Color(.2f, .9f, 1f, 1f);
                    riverLine.sortingOrder = 1000;
                    riverLine.shadowCastingMode = ShadowCastingMode.Off;
                    riverLine.receiveShadows = false;
                    for (var index = 0; index < river.Points.Count; index++)
                        riverLine.SetPosition(index, new Vector3(
                            (river.Points[index].X - .5f) * _widthMeters,
                            .65f,
                            (river.Points[index].Z - .5f) * _depthMeters));
                    continue;
                }
                if (!TryDistrictSelectionBounds(district, item,
                        out var bounds)) continue;
                var lineObject = new GameObject(
                    $"Selected {item.Kind} — {item.Id}");
                lineObject.transform.SetParent(_districtSelectionRoot, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 4;
                line.widthMultiplier = .16f;
                line.sharedMaterial = _districtSelectionMaterial;
                line.startColor = line.endColor =
                    new Color(.35f, .82f, 1f, .96f);
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                const float y = .34f;
                line.SetPosition(0, new Vector3(bounds.xMin, y, bounds.yMin));
                line.SetPosition(1, new Vector3(bounds.xMax, y, bounds.yMin));
                line.SetPosition(2, new Vector3(bounds.xMax, y, bounds.yMax));
                line.SetPosition(3, new Vector3(bounds.xMin, y, bounds.yMax));
            }
        }

        private bool TryDistrictSelectionBounds(RegionCityTile district,
            DistrictSelectionRef selection, out Rect bounds)
        {
            bounds = default;
            if (selection.Kind == DistrictSelectionKind.Flora &&
                _districtFloraPresentations.TryGetValue(selection.Id,
                    out var renderer) && renderer != null)
            {
                var p = renderer.transform.localPosition;
                bounds = new Rect(p.x - 1.25f, p.z - 1.25f, 2.5f, 2.5f);
                return true;
            }
            if (selection.Kind == DistrictSelectionKind.Lot)
            {
                var lotPlacement = district.Lots?.Find(item => item != null &&
                    item.InstanceId == selection.Id);
                var lot = lotPlacement == null ? null :
                    LotContentCatalog.Read(lotPlacement.LotId);
                if (lot == null) return false;
                var spanX = DistrictScale.GridSpanForMeters(
                    lot.LotWidthCells * LotMetricScale.MajorGridMeters);
                var spanZ = DistrictScale.GridSpanForMeters(
                    lot.LotDepthCells * LotMetricScale.MajorGridMeters);
                if ((lotPlacement.RotationQuarterTurns & 1) != 0)
                    (spanX, spanZ) = (spanZ, spanX);
                bounds = new Rect(-_widthMeters * .5f +
                    lotPlacement.GridX * DistrictScale.CellSizeMeters,
                    -_depthMeters * .5f +
                    lotPlacement.GridZ * DistrictScale.CellSizeMeters,
                    spanX * DistrictScale.CellSizeMeters,
                    spanZ * DistrictScale.CellSizeMeters);
                return true;
            }
            if (selection.Kind == DistrictSelectionKind.Road)
            {
                var road = district.Roads?.Find(item => item != null &&
                    item.Id == selection.Id);
                if (road == null) return false;
                bounds = new Rect(-_widthMeters * .5f +
                    road.GridX * DistrictScale.CellSizeMeters,
                    -_depthMeters * .5f +
                    road.GridZ * DistrictScale.CellSizeMeters,
                    DistrictScale.CellSizeMeters,
                    DistrictScale.CellSizeMeters);
                return true;
            }
            if (selection.Kind == DistrictSelectionKind.River)
            {
                var river = district.Rivers?.Find(item => item != null &&
                    item.InstanceId == selection.Id);
                if (river?.Points == null || river.Points.Count == 0)
                    return false;
                var minX = river.Points.Min(point => point.X) * _widthMeters;
                var maxX = river.Points.Max(point => point.X) * _widthMeters;
                var minZ = river.Points.Min(point => point.Z) * _depthMeters;
                var maxZ = river.Points.Max(point => point.Z) * _depthMeters;
                var padding = river.WidthMeters *
                    (river.Depth == DistrictRiverDepth.Deep ? .54f : .64f);
                bounds = Rect.MinMaxRect(minX - _widthMeters * .5f - padding,
                    minZ - _depthMeters * .5f - padding,
                    maxX - _widthMeters * .5f + padding,
                    maxZ - _depthMeters * .5f + padding);
                return true;
            }
            return false;
        }

        private void AddDistrictFloraPresentation(PlacedDistrictFlora placed)
        {
            if (placed == null || string.IsNullOrWhiteSpace(placed.FloraId)) return;
            var variation = LotWorldController.StableFloraVariationProfile(
                placed.InstanceId);
            var presentationId = LotWorldController.ResolveFloraPresentationId(
                placed.FloraId, variation, SeasonPreset.Summer);
            var resource = LotWorldController.ResolveFloraResourcePath(
                presentationId, SeasonPreset.Summer);
            if (string.IsNullOrWhiteSpace(resource)) return;
            if (!_districtFloraSprites.TryGetValue(resource, out var sprite) ||
                sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resource);
                if (texture == null) return;
                sprite = StoneFloraCatalog.IsStone(presentationId) ? StoneFloraCatalog.CreateSprite(presentationId) : Sprite.Create(texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    LotWorldController.FloraPivot(texture.name),
                    LotWorldController.FloraPixelsPerUnit(
                        presentationId, texture.name));
                _districtFloraSprites[resource] = sprite;
            }
            var item = new GameObject($"District Flora — {placed.FloraId}");
            item.transform.SetParent(_districtFloraRoot, false);
            item.transform.localPosition = DistrictFloraPosition(placed);
            item.transform.rotation = _camera.transform.rotation *
                Quaternion.Euler(0f, 0f,
                    StoneFloraCatalog.IsStone(placed.FloraId)
                        ? placed.RotationEighthTurns * 45f : 0f);
            item.transform.localScale = Vector3.one * Mathf.Clamp(
                placed.Scale, .65f, 1.45f);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = DistrictFloraMaterial();
            FloraTreeRepairs.Apply(renderer, presentationId);
            renderer.color = TimeOfDayLighting.For(TimeOfDay).NeutralArtworkTint;
            renderer.sortingOrder = DistrictFloraSortingOrder(
                item.transform.localPosition);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (!StoneFloraCatalog.IsStone(placed.FloraId)) BuildDistrictFloraShadow(item.transform, sprite);
            _districtFloraPresentations[placed.InstanceId] = renderer;
        }

        private Material DistrictFloraMaterial()
        {
            if (_districtFloraMaterial != null) return _districtFloraMaterial;
            var shader = Shader.Find("CityForgeV3/LitShadowReceivingSprite");
            if (shader == null)
                throw new MissingReferenceException(
                    "CityForge V3 lit flora shader is required.");
            _districtFloraMaterial = new Material(shader)
            {
                name = "District Lit Flora",
                // Flora must establish depth before transparent river water
                // is drawn. The water then covers only pixels whose world
                // position is below/behind its surface, making trees and
                // stones visibly submerged instead of pasted over the river.
                renderQueue = (int)RenderQueue.AlphaTest
            };
            _districtFloraMaterial.SetFloat("_Cutoff", 0.02f);
            _districtFloraMaterial.SetFloat("_ShadowFloor", 0.38f);
            _districtFloraMaterial.SetFloat("_ZTest",
                (float)CompareFunction.LessEqual);
            return _districtFloraMaterial;
        }

        private void BuildDistrictFloraShadow(Transform flora, Sprite sprite)
        {
            var shader = Shader.Find("CityForgeV3/DistrictFloraGroundShadow");
            if (flora == null || sprite == null || shader == null) return;
            _districtFloraShadowMaterial ??= new Material(shader)
            { name = "District Projected Flora Shadow" };
            _districtFloraShadowMaterial.shader = shader;
            var shadowObject = new GameObject("District Flora Shadow");
            shadowObject.transform.SetParent(flora, false);
            var filter = shadowObject.AddComponent<MeshFilter>();
            var mesh = new Mesh { name = "District Flora Ground Silhouette" };
            mesh.vertices = System.Array.ConvertAll(sprite.vertices, v => new Vector3(v.x,v.y,0));
            mesh.uv = sprite.uv;
            mesh.triangles = System.Array.ConvertAll(sprite.triangles, i => (int)i);
            filter.sharedMesh = mesh;
            shadowObject.AddComponent<DistrictFloraShadowMesh>();
            var shadow = shadowObject.AddComponent<MeshRenderer>();
            shadow.sharedMaterial = _districtFloraShadowMaterial;
            shadow.receiveShadows = false;
            shadow.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void UpdateDistrictFloraShadows()
        {
            if (_districtFloraRoot == null) return;
            var visible = TimeOfDay != TimeOfDayPreset.Night;
            var ray = _sun != null
                ? _sun.transform.rotation * Vector3.forward
                : TimeOfDayLighting.SunRotation(TimeOfDay) * Vector3.forward;
            var opacity = TimeOfDay switch
            {
                TimeOfDayPreset.Morning => .22f,
                TimeOfDayPreset.Noon => .30f,
                TimeOfDayPreset.Afternoon => .25f,
                _ => 0f
            };
            foreach (var visibleRenderer in _districtFloraPresentations.Values)
            {
                if (visibleRenderer == null) continue;
                var shadow = visibleRenderer.transform.Find(
                    "District Flora Shadow")?.GetComponent<MeshRenderer>();
                if (shadow == null) continue;
                shadow.enabled = visible;
                if (!visible) continue;
                shadow.transform.localPosition = Vector3.zero;
                shadow.transform.localRotation = Quaternion.identity;
                shadow.transform.localScale = Vector3.one;
                shadow.sortingOrder = visibleRenderer.sortingOrder - 1;
                var properties = new MaterialPropertyBlock();
                shadow.GetPropertyBlock(properties);
                properties.SetVector("_SunRay", ray.normalized);
                // The flora root is registered to its actual receiver height,
                // including depressed riverbeds. Project just above that root.
                // The former fixed .145 m value sat beneath the .184 m district
                // ground and was therefore depth-culled at the two closest
                // (highest precision) camera levels.
                var groundY = visibleRenderer.transform.position.y + .006f;
                properties.SetFloat("_GroundY", groundY);
                properties.SetFloat("_ProjectionScale", 1f);
                properties.SetFloat("_UprightSource", 1f);
                properties.SetFloat("_ReferenceHeight", Mathf.Max(.01f,
                    visibleRenderer.sprite.bounds.size.y * visibleRenderer.transform.lossyScale.y));
                properties.SetFloat("_SinkCompensation", 0f);
                properties.SetColor("_Color",
                    new Color(.018f, .022f, .026f, opacity));
                shadow.SetPropertyBlock(properties);
                // Explicit ground geometry avoids SpriteRenderer projection/depth
                // inconsistencies. Keep each silhouette anchored to its tree.
                var source = visibleRenderer.sprite;
                var root = visibleRenderer.transform.position;
                var right = visibleRenderer.transform.right;
                right.y = 0f;
                right.Normalize();
                var scale = visibleRenderer.transform.lossyScale;
                var vertices = source.vertices;
                var projected = new Vector3[vertices.Length];
                var colors = new Color[vertices.Length];
                var referenceHeight = Mathf.Max(.01f, source.bounds.size.y * scale.y);
                for (var i=0;i<vertices.Length;i++)
                {
                    var height = Mathf.Max(0f, vertices[i].y * scale.y);
                    var world = root + right * (vertices[i].x * scale.x);
                    var travel = height / Mathf.Max(.05f, -ray.y);
                    world += new Vector3(ray.x,0f,ray.z) * travel;
                    world.y = groundY + .025f;
                    projected[i] = shadow.transform.InverseTransformPoint(world);
                    colors[i] = new Color(1f,1f,1f,Mathf.Clamp01(height/referenceHeight));
                }
                var mesh = shadow.GetComponent<MeshFilter>().sharedMesh;
                mesh.vertices = projected;
                mesh.colors = colors;
                mesh.RecalculateBounds();
                properties.SetTexture("_MainTex", source.texture);
                shadow.SetPropertyBlock(properties);
            }
        }

        private Vector3 DistrictFloraPosition(PlacedDistrictFlora placed)
        {
            var x = (placed.NormalizedX - .5f) * _widthMeters;
            var z = (placed.NormalizedZ - .5f) * _depthMeters;
            const float ground = .19f;
            var local = new Vector3(x, ground, z);
            var sample = SampleRiverSurface(_content.TransformPoint(local));
            if (sample.HasValue)
                local.y = _content.InverseTransformPoint(new Vector3(
                    _content.TransformPoint(local).x,
                    sample.Value.BedElevation,
                    _content.TransformPoint(local).z)).y;
            return local;
        }

        private int DistrictFloraSortingOrder(Vector3 localPosition) =>
            5000 - Mathf.RoundToInt((localPosition.x + localPosition.z) * 4f);

        private void BuildDistrictFloraSelection(string instanceId)
        {
            if (_districtFloraSelection == null)
            {
                var selection = new GameObject("District Flora Selection");
                selection.transform.SetParent(_districtFloraRoot, false);
                _districtFloraSelection = selection.AddComponent<LineRenderer>();
                _districtFloraSelection.useWorldSpace = false;
                _districtFloraSelection.loop = true;
                _districtFloraSelection.positionCount = 4;
                _districtFloraSelection.widthMultiplier = .16f;
                _districtFloraSelection.sharedMaterial = new Material(
                    Shader.Find("Sprites/Default"));
                _districtFloraSelection.startColor =
                    _districtFloraSelection.endColor =
                        new Color(1f, .74f, .08f, .95f);
            }
            if (string.IsNullOrWhiteSpace(instanceId) ||
                !_districtFloraPresentations.TryGetValue(instanceId,
                    out var renderer) || renderer == null)
            {
                _districtFloraSelection.gameObject.SetActive(false);
                return;
            }
            var p = renderer.transform.localPosition;
            const float radius = 1.15f;
            _districtFloraSelection.transform.localPosition =
                new Vector3(p.x, p.y + .03f, p.z);
            _districtFloraSelection.SetPosition(0,
                new Vector3(-radius, 0f, -radius));
            _districtFloraSelection.SetPosition(1,
                new Vector3(radius, 0f, -radius));
            _districtFloraSelection.SetPosition(2,
                new Vector3(radius, 0f, radius));
            _districtFloraSelection.SetPosition(3,
                new Vector3(-radius, 0f, radius));
            _districtFloraSelection.gameObject.SetActive(true);
        }

        private void BuildRiver(PlacedDistrictRiver river)
        {
            if (river?.Points == null || river.Points.Count < 2) return;
            var centerline = new List<Vector2>(river.Points.Count);
            foreach (var point in river.Points)
                centerline.Add(new Vector2(
                    (point.X - 0.5f) * _widthMeters,
                    (point.Z - 0.5f) * _depthMeters));
            var deep = river.Depth == DistrictRiverDepth.Deep;
            var bedWidth = river.WidthMeters * (deep ? 1.08f : 1.28f);
            var edgeTexture = Resources.Load<Texture2D>(
                RiverGrassToDirtResource);
            var edgeVariants = RiverGrassBankVariantResources
                .Select(resource => Resources.Load<Texture2D>(resource))
                .Where(candidate => candidate != null)
                .ToArray();
            if (edgeVariants.Length > 0) edgeTexture = edgeVariants[0];
            var dirtTexture = Resources.Load<Texture2D>(RiverBedDirtResource);
            var halfWidth = bedWidth * 0.5f;
            var edgeWidth = Mathf.Min(
                deep ? RiverBedTransitionWidthMeters * .55f
                     : RiverBedTransitionWidthMeters,
                halfWidth * (deep ? .16f : .30f));
            var dirtOuterDistance = halfWidth - edgeWidth;
            const float terrainSurface = 0.184f;
            // Deep rivers have a genuinely navigable, broad 10-foot channel.
            // Shallow rivers reuse the profile at a reduced scale so their bed
            // remains visible through less than a metre of water.
            var depthScale = deep ? 1f : 0.286f;
            // A deep channel keeps a broad flat floor, then climbs through a
            // compressed section near the shore. The shallow profile retains
            // its broad, gradual slopes. Previously both profiles used the
            // shallow lateral spacing, so "steep" only changed depth.
            var centerEnd = dirtOuterDistance * (deep ? .68f : .45f);
            var darkBedEnd = dirtOuterDistance * (deep ? .76f : .65f);
            var lowerBankEnd = dirtOuterDistance * (deep ? .84f : .82f);
            var upperBankEnd = dirtOuterDistance * (deep ? .92f : 1f);
            float RiverElevation(float depth) => terrainSurface - depth * depthScale;
            foreach (var side in new[] { -1, 1 })
            {
                var sideName = side < 0 ? "Left" : "Right";
                // Broad, nearly flat channel floor. The remaining bands climb
                // outward in stages so this reads as a river valley, not a V-cut.
                AddRiverBand(centerline, 0f, centerEnd,
                    RiverElevation(3.15f), RiverElevation(3.15f), side,
                    dirtTexture, 0.04f, 0.25f, 48f,
                    1f, 1f, 0,
                    $"Riverbed {sideName} Broad Center — {river.InstanceId}");
                AddRiverBand(centerline, centerEnd, darkBedEnd,
                    RiverElevation(3.15f), RiverElevation(2.35f), side,
                    dirtTexture, 0.25f, 0.48f, 48f,
                    1f, 1f, 0,
                    $"Riverbed {sideName} Dark Bed — {river.InstanceId}");
                AddRiverBand(centerline, darkBedEnd, lowerBankEnd,
                    RiverElevation(2.35f), RiverElevation(0.95f), side,
                    dirtTexture, 0.48f, 0.72f, 48f,
                    1f, 1f, 0,
                    $"Riverbed {sideName} Lower Bank — {river.InstanceId}");
                AddRiverBand(centerline, lowerBankEnd, upperBankEnd,
                    RiverElevation(0.95f), RiverElevation(0.20f), side,
                    dirtTexture, 0.72f, 0.96f, 48f,
                    1f, 1f, 0,
                    $"Riverbed {sideName} Upper Bank — {river.InstanceId}");
                AddRiverBand(centerline, upperBankEnd, halfWidth,
                    RiverElevation(0.20f), terrainSurface + 0.002f, side,
                    edgeTexture, 0.04f, 0.96f, 24f,
                    1f, 1f, 1,
                    $"Riverbed {sideName} Grass Edge — {river.InstanceId}",
                    true, edgeVariants,
                    StableStringHash(river.InstanceId) + side * 7919);
            }

            var waterTexture = Resources.Load<Texture2D>(
                RiverWaterTextureResource);
            var requestedDepth = Mathf.Abs(_waterHeight);
            var waterDepth = deep ? requestedDepth : requestedDepth * depthScale;
            var waterElevation = terrainSurface - waterDepth;
            // Follow the existing bank profile up to the chosen water level.
            // Raising the surface must also cover the newly submerged slope.
            var bankDistances = new[] { 0f, centerEnd, darkBedEnd,
                lowerBankEnd, upperBankEnd, halfWidth };
            var bankElevations = new[] { RiverElevation(3.15f), RiverElevation(3.15f),
                RiverElevation(2.35f), RiverElevation(.95f), RiverElevation(.20f),
                terrainSurface + .002f };
            if (waterElevation <= bankElevations[0]) return;
            var waterHalfWidth = halfWidth;
            for (var band = 1; band < bankElevations.Length; band++)
            {
                if (waterElevation > bankElevations[band]) continue;
                waterHalfWidth = Mathf.Lerp(bankDistances[band - 1], bankDistances[band],
                    Mathf.InverseLerp(bankElevations[band - 1], bankElevations[band], waterElevation));
                break;
            }
            var waterWidth = Mathf.Max(.01f, waterHalfWidth - .10f) * 2f;
            _riverSurfaces.Add(new RuntimeRiverSurface(centerline, halfWidth,
                dirtOuterDistance, waterWidth * .5f, waterElevation,
                terrainSurface, depthScale, deep));
            AddRiverWaterSurface(centerline, waterWidth, waterElevation,
                waterTexture, $"River Water — {river.InstanceId}", deep);
        }

        /// <summary>
        /// Samples the same procedural cross-section used to build the visible
        /// channel. Returned elevations are world-space so hosted lots, boats,
        /// docks, rocks, and navigation can share one authoritative contract.
        /// </summary>
        public RiverSurfaceSample? SampleRiverSurface(Vector3 worldPosition)
        {
            if (_content == null || _riverSurfaces.Count == 0) return null;
            var local = _content.InverseTransformPoint(worldPosition);
            RuntimeRiverSurface best = null;
            RuntimeRiverSurface.ClosestPoint closest = default;
            var bestDistance = float.PositiveInfinity;
            foreach (var river in _riverSurfaces)
            {
                var candidate = river.FindClosest(new Vector2(local.x, local.z));
                if (candidate.AbsoluteLateral >= bestDistance) continue;
                best = river;
                closest = candidate;
                bestDistance = candidate.AbsoluteLateral;
            }
            if (best == null || bestDistance > best.HalfWidth) return null;
            var bedLocal = best.BedElevation(bestDistance);
            var left = closest.SignedLateral >= 0f;
            var shore = best.WaterHalfWidth * RiverShoreWidthScale(
                closest.DistanceAlong, best.WaterHalfWidth * 2f, left);
            var underWater = bestDistance <= shore &&
                             best.WaterElevation > bedLocal;
            var bedWorld = _content.TransformPoint(new Vector3(local.x,
                bedLocal, local.z)).y;
            var waterWorld = _content.TransformPoint(new Vector3(local.x,
                best.WaterElevation, local.z)).y;
            var downstreamWorld = _content.TransformDirection(new Vector3(
                closest.Tangent.x, 0f, closest.Tangent.y)).normalized;
            return new RiverSurfaceSample(true, underWater, bedWorld,
                waterWorld, underWater ? waterWorld - bedWorld : 0f,
                bestDistance, downstreamWorld);
        }

        public bool IsUnderRiverWater(Vector2 normalizedPosition)
        {
            if (_content == null) return false;
            var local = new Vector3(
                (normalizedPosition.x - .5f) * _widthMeters,
                .19f,
                (normalizedPosition.y - .5f) * _depthMeters);
            return SampleRiverSurface(_content.TransformPoint(local))
                ?.UnderWater == true;
        }

        private sealed class RuntimeRiverSurface
        {
            public readonly struct ClosestPoint
            {
                public readonly float AbsoluteLateral;
                public readonly float SignedLateral;
                public readonly float DistanceAlong;
                public readonly Vector2 Tangent;

                public ClosestPoint(float absoluteLateral,
                    float signedLateral, float distanceAlong, Vector2 tangent)
                {
                    AbsoluteLateral = absoluteLateral;
                    SignedLateral = signedLateral;
                    DistanceAlong = distanceAlong;
                    Tangent = tangent;
                }
            }

            private readonly List<Vector2> _points;
            private readonly float _dirtOuterDistance;
            private readonly float _terrainSurface;
            private readonly float _depthScale;
            private readonly bool _steepBanks;
            public readonly float HalfWidth;
            public readonly float WaterHalfWidth;
            public readonly float WaterElevation;

            public RuntimeRiverSurface(List<Vector2> points, float halfWidth,
                float dirtOuterDistance, float waterHalfWidth,
                float waterElevation, float terrainSurface, float depthScale,
                bool steepBanks)
            {
                _points = new List<Vector2>(points);
                HalfWidth = halfWidth;
                _dirtOuterDistance = dirtOuterDistance;
                WaterHalfWidth = waterHalfWidth;
                WaterElevation = waterElevation;
                _terrainSurface = terrainSurface;
                _depthScale = depthScale;
                _steepBanks = steepBanks;
            }

            public float BedElevation(float distance)
            {
                var d0 = _dirtOuterDistance * (_steepBanks ? .68f : .45f);
                var d1 = _dirtOuterDistance * (_steepBanks ? .76f : .65f);
                var d2 = _dirtOuterDistance * (_steepBanks ? .84f : .82f);
                var d3 = _dirtOuterDistance * (_steepBanks ? .92f : 1f);
                float Elevation(float depth) =>
                    _terrainSurface - depth * _depthScale;
                if (distance <= d0) return Elevation(3.15f);
                if (distance <= d1) return Mathf.Lerp(Elevation(3.15f),
                    Elevation(2.35f), Mathf.InverseLerp(d0, d1, distance));
                if (distance <= d2) return Mathf.Lerp(Elevation(2.35f),
                    Elevation(.95f), Mathf.InverseLerp(d1, d2, distance));
                if (distance <= d3)
                    return Mathf.Lerp(Elevation(.95f), Elevation(.20f),
                        Mathf.InverseLerp(d2, d3, distance));
                return Mathf.Lerp(Elevation(.20f), _terrainSurface + .002f,
                    Mathf.InverseLerp(d3, HalfWidth, distance));
            }

            public ClosestPoint FindClosest(Vector2 point)
            {
                var bestDistance = float.PositiveInfinity;
                var bestSigned = 0f;
                var bestAlong = 0f;
                var bestTangent = Vector2.up;
                var traveled = 0f;
                for (var i = 0; i < _points.Count - 1; i++)
                {
                    var segment = _points[i + 1] - _points[i];
                    var length = segment.magnitude;
                    if (length <= .0001f) continue;
                    var tangent = segment / length;
                    var t = Mathf.Clamp01(Vector2.Dot(point - _points[i],
                        segment) / (length * length));
                    var onSegment = _points[i] + segment * t;
                    var offset = point - onSegment;
                    var distance = offset.magnitude;
                    if (distance < bestDistance)
                    {
                        var normal = new Vector2(-tangent.y, tangent.x);
                        bestDistance = distance;
                        bestSigned = Vector2.Dot(offset, normal);
                        bestAlong = traveled + length * t;
                        bestTangent = tangent;
                    }
                    traveled += length;
                }
                return new ClosestPoint(bestDistance, bestSigned, bestAlong,
                    bestTangent);
            }
        }

        public static float RiverShoreWidthScale(float distanceMeters, float waterWidth, bool left)
        {
            // Two long waves per shore, with different wavelengths/phases.
            // Range is 88–100% of the water-level bank intersection: even
            // the broadest bulge remains inside the existing sloped banks.
            var wavelength = Mathf.Max(160f, waterWidth * 7f);
            var phase = distanceMeters * Mathf.PI * 2f / wavelength;
            var broad = Mathf.Sin(phase * (left ? 1f : .79f) + (left ? .35f : 2.1f));
            var secondary = Mathf.Sin(phase * (left ? .47f : .61f) + (left ? 1.7f : -.8f));
            return Mathf.Clamp(.94f + .045f * broad + .015f * secondary, .88f, 1f);
        }

        private void AddRiverWaterSurface(IReadOnlyList<Vector2> centerline,
            float width, float elevation, Texture2D texture, string name,
            bool deepRiver)
        {
            if (texture == null || centerline == null || centerline.Count < 2)
                return;
            // Subdivide only the water. Interpolate the exact bank-section
            // normals instead of fitting a new curve, keeping the surface
            // inside the existing ruled bank strips even around bends.
            var centers = new List<Vector2>();
            var normals = new List<Vector2>();
            var distances = new List<float>();
            var traveled = 0f;
            Vector2 BankNormal(int i)
            {
                var prior = centerline[Mathf.Max(0, i - 1)];
                var next = centerline[Mathf.Min(centerline.Count - 1, i + 1)];
                var tangent = (next - prior).normalized;
                return new Vector2(-tangent.y, tangent.x);
            }
            var spacing = Mathf.Clamp(width * .12f, 2f, 8f);
            for (var segment = 0; segment < centerline.Count - 1; segment++)
            {
                var length = Vector2.Distance(centerline[segment], centerline[segment + 1]);
                var steps = Mathf.Max(1, Mathf.CeilToInt(length / spacing));
                var firstNormal = BankNormal(segment);
                var lastNormal = BankNormal(segment + 1);
                for (var step = 0; step < steps; step++)
                {
                    var t = (float)step / steps;
                    centers.Add(Vector2.Lerp(centerline[segment], centerline[segment + 1], t));
                    normals.Add(Vector2.Lerp(firstNormal, lastNormal, t));
                    distances.Add(traveled + length * t);
                }
                traveled += length;
            }
            centers.Add(centerline[centerline.Count - 1]);
            normals.Add(BankNormal(centerline.Count - 1));
            distances.Add(traveled);
            const int rows = 5;
            var vertices = new Vector3[centers.Count * rows];
            var uv = new Vector2[centers.Count * rows];
            var colors = new Color[centers.Count * rows];
            var triangles = new int[(centers.Count - 1) * 24];
            var halfWidth = width * 0.5f;
            var fadeScale = 1f - Mathf.Clamp(_waterEdgeFadeWidth, 0.05f, 0.45f);
            var worldTileSize = Mathf.Max(0.25f, _waterTextureTiling);
            for (var index = 0; index < centers.Count; index++)
            {
                var normal = normals[index];
                var leftWidth = halfWidth * RiverShoreWidthScale(distances[index], width, true);
                var rightWidth = halfWidth * RiverShoreWidthScale(distances[index], width, false);
                var left = centers[index] + normal * leftWidth;
                var leftCenter = centers[index] + normal * (leftWidth * fadeScale);
                var rightCenter = centers[index] - normal * (rightWidth * fadeScale);
                var right = centers[index] - normal * rightWidth;
                var center = centers[index];
                var row = index * rows;
                vertices[row] = new Vector3(left.x, elevation, left.y);
                vertices[row + 1] = new Vector3(
                    leftCenter.x, elevation, leftCenter.y);
                vertices[row + 2] = new Vector3(
                    center.x, elevation, center.y);
                vertices[row + 3] = new Vector3(
                    rightCenter.x, elevation, rightCenter.y);
                vertices[row + 4] = new Vector3(right.x, elevation, right.y);
                // River-relative mapping: U is accumulated centerline distance
                // and V is physical distance across the channel. Both remain
                // metre-scaled, so the texture tiles consistently without
                // stretching while U provides a local downstream flow axis.
                var along = distances[index] / worldTileSize;
                uv[row] = new Vector2(along, leftWidth / worldTileSize);
                uv[row + 1] = new Vector2(along,
                    leftWidth * fadeScale / worldTileSize);
                uv[row + 2] = new Vector2(along, 0f);
                uv[row + 3] = new Vector2(along,
                    -rightWidth * fadeScale / worldTileSize);
                uv[row + 4] = new Vector2(along,
                    -rightWidth / worldTileSize);
                var fadeCoordinate = Mathf.Clamp(_waterEdgeFadeWidth,
                    0.05f, 0.45f);
                // Red stores continuous normalized depth: zero at either bank,
                // one at the centerline. The shader turns this into one smooth
                // opacity/color gradient with no shallow/deep mesh boundary.
                colors[row] = new Color(0f, 1f, 1f, 1f);
                colors[row + 1] = new Color(fadeCoordinate, 1f, 1f, 1f);
                colors[row + 2] = new Color(1f, 1f, 1f, 1f);
                colors[row + 3] = new Color(fadeCoordinate, 1f, 1f, 1f);
                colors[row + 4] = new Color(0f, 1f, 1f, 1f);
                if (index >= centers.Count - 1) continue;
                for (var band = 0; band < rows - 1; band++)
                {
                    var triangle = index * 24 + band * 6;
                    var vertex = row + band;
                    triangles[triangle] = vertex;
                    triangles[triangle + 1] = vertex + rows;
                    triangles[triangle + 2] = vertex + 1;
                    triangles[triangle + 3] = vertex + 1;
                    triangles[triangle + 4] = vertex + rows;
                    triangles[triangle + 5] = vertex + rows + 1;
                }
            }

            var mesh = new Mesh { name = name + " Mesh",
                indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var item = new GameObject(name);
            item.transform.SetParent(_riverRoot, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            var shader = Shader.Find("CityForgeV3/RiverWaterSurface") ??
                         Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            var tint = _waterTint;
            tint.a = 1f;
            var material = new Material(shader)
            {
                name = name + " Material",
                color = tint,
                mainTexture = texture,
                renderQueue = (int)RenderQueue.Transparent
            };
            texture.wrapMode = TextureWrapMode.Repeat;
            var whitecapTexture = Resources.Load<Texture2D>(
                RiverWhitecapTextureResource);
            if (whitecapTexture != null)
            {
                whitecapTexture.wrapMode = TextureWrapMode.Repeat;
                if (material.HasProperty("_WhitecapTex"))
                    material.SetTexture("_WhitecapTex", whitecapTexture);
            }
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", tint);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", _waterSmoothness);
            if (material.HasProperty("_Brightness"))
                material.SetFloat("_Brightness", _waterBrightness);
            if (material.HasProperty("_CenterOpacity"))
                material.SetFloat("_CenterOpacity", deepRiver
                    ? 1f - (1f - _waterOpacity) * .12f
                    : _waterOpacity);
            if (material.HasProperty("_EdgeOpacity"))
                material.SetFloat("_EdgeOpacity", _waterEdgeOpacity);
            if (material.HasProperty("_DeepWaterStart"))
                material.SetFloat("_DeepWaterStart", _deepWaterStart);
            if (material.HasProperty("_DeepWaterStrength"))
                material.SetFloat("_DeepWaterStrength", deepRiver
                    ? Mathf.Lerp(_deepWaterStrength, 1f, .78f)
                    : Mathf.Min(_deepWaterStrength, .38f));
            if (material.HasProperty("_DepthBlendSoftness"))
                material.SetFloat("_DepthBlendSoftness", _depthBlendSoftness);
            if (material.HasProperty("_FlowSpeed"))
                material.SetFloat("_FlowSpeed", _waterFlowSpeed);
            if (material.HasProperty("_WaveDistortion"))
                material.SetFloat("_WaveDistortion", _waterWaveDistortion);
            if (material.HasProperty("_WaveScale"))
                material.SetFloat("_WaveScale", _waterWaveScale);
            if (material.HasProperty("_WaveSpeed"))
                material.SetFloat("_WaveSpeed", _waterWaveSpeed);
            if (material.HasProperty("_ReflectionStrength"))
                material.SetFloat("_ReflectionStrength",
                    _waterReflectionStrength);
            if (material.HasProperty("_ShimmerStrength"))
                material.SetFloat("_ShimmerStrength", _waterShimmerStrength);
            if (material.HasProperty("_ShimmerSpeed"))
                material.SetFloat("_ShimmerSpeed", _waterShimmerSpeed);
            if (material.HasProperty("_WhitecapStrength"))
                material.SetFloat("_WhitecapStrength", _whitecapStrength);
            if (material.HasProperty("_WhitecapCoverage"))
                material.SetFloat("_WhitecapCoverage", _whitecapCoverage);
            if (material.HasProperty("_WhitecapTiling"))
                material.SetFloat("_WhitecapTiling", _whitecapTiling);
            if (material.HasProperty("_WhitecapSpeed"))
                material.SetFloat("_WhitecapSpeed", _whitecapSpeed);
            if (material.HasProperty("_WhitecapPulseSpeed"))
                material.SetFloat("_WhitecapPulseSpeed",
                    _whitecapPulseSpeed);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", _waterSmoothness);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            var renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 2;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private void AddRiverRibbon(IReadOnlyList<Vector2> centerline,
            float width, float elevation, Color color, string name,
            int renderQueue, Texture2D texture = null)
        {
            var vertices = new Vector3[centerline.Count * 2];
            var uv = new Vector2[centerline.Count * 2];
            var triangles = new int[(centerline.Count - 1) * 6];
            var traveled = 0f;
            for (var index = 0; index < centerline.Count; index++)
            {
                var prior = centerline[Mathf.Max(0, index - 1)];
                var next = centerline[Mathf.Min(centerline.Count - 1,
                    index + 1)];
                var tangent = (next - prior).normalized;
                var normal = new Vector2(-tangent.y, tangent.x) * width * 0.5f;
                if (index > 0) traveled += Vector2.Distance(
                    centerline[index - 1], centerline[index]);
                vertices[index * 2] = new Vector3(
                    centerline[index].x + normal.x, elevation,
                    centerline[index].y + normal.y);
                vertices[index * 2 + 1] = new Vector3(
                    centerline[index].x - normal.x, elevation,
                    centerline[index].y - normal.y);
                var textureWidth = texture != null
                    ? width / RiverBedTextureWorldSizeMeters
                    : 1f;
                var textureLength = texture != null
                    ? traveled / RiverBedTextureWorldSizeMeters
                    : traveled / 10f;
                uv[index * 2] = new Vector2(0f, textureLength);
                uv[index * 2 + 1] = new Vector2(textureWidth, textureLength);
                if (index >= centerline.Count - 1) continue;
                var triangle = index * 6;
                var vertex = index * 2;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }
            var mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var item = new GameObject(name);
            item.transform.SetParent(_riverRoot, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            var shader = texture != null
                ? Shader.Find("CityForgeV3/RiverBedSurface")
                : Shader.Find("Universal Render Pipeline/Lit");
            shader ??=
                         Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name + " Material",
                color = color,
                renderQueue = renderQueue
            };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                material.mainTexture = texture;
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", texture);
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
            }
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.12f);
            var renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = !name.Contains("Water");
        }

        private void AddRiverBand(IReadOnlyList<Vector2> centerline,
            float innerDistance, float outerDistance,
            float innerElevation, float outerElevation, int side,
            Texture2D texture, float innerTextureV, float outerTextureV,
            float repeatLengthMeters, float innerAlpha, float outerAlpha,
            int sortingOrder, string name, bool hideAtFarZoom = false,
            Texture2D[] textureVariants = null, int variantSeed = 0)
        {
            if (texture == null || centerline == null || centerline.Count < 2)
                return;
            var vertices = new Vector3[centerline.Count * 2];
            var uv = new Vector2[centerline.Count * 2];
            var colors = new Color[centerline.Count * 2];
            var triangles = new int[(centerline.Count - 1) * 6];
            var traveled = 0f;
            for (var index = 0; index < centerline.Count; index++)
            {
                var prior = centerline[Mathf.Max(0, index - 1)];
                var next = centerline[Mathf.Min(centerline.Count - 1,
                    index + 1)];
                var tangent = (next - prior).normalized;
                var normal = new Vector2(-tangent.y, tangent.x) * side;
                if (index > 0)
                    traveled += Vector2.Distance(centerline[index - 1],
                        centerline[index]);
                var inner = centerline[index] + normal * innerDistance;
                var outer = centerline[index] + normal * outerDistance;
                vertices[index * 2] = new Vector3(
                    inner.x, innerElevation, inner.y);
                vertices[index * 2 + 1] = new Vector3(
                    outer.x, outerElevation, outer.y);
                var along = traveled / repeatLengthMeters;
                uv[index * 2] = new Vector2(along, innerTextureV);
                uv[index * 2 + 1] = new Vector2(along, outerTextureV);
                colors[index * 2] = new Color(1f, 1f, 1f, innerAlpha);
                colors[index * 2 + 1] = new Color(1f, 1f, 1f, outerAlpha);
                if (index >= centerline.Count - 1) continue;
                var triangle = index * 6;
                var vertex = index * 2;
                // The band shader renders both faces, so keep identical winding on
                // both banks. Reversing the mirrored bank here caused its grass
                // transition strip to disappear on some render paths.
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            var mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            var activeTextures = textureVariants?
                .Where(candidate => candidate != null).ToArray();
            if (activeTextures != null && activeTextures.Length > 1)
            {
                mesh.subMeshCount = activeTextures.Length;
                var submeshTriangles = new List<int>[activeTextures.Length];
                for (var variant = 0; variant < activeTextures.Length; variant++)
                    submeshTriangles[variant] = new List<int>();
                for (var segment = 0; segment < centerline.Count - 1; segment++)
                {
                    // Adjacent segments deliberately walk through the set in a
                    // seeded order. This prevents long repeated runs while
                    // keeping saved rivers visually stable across reloads.
                    var variant = (int)((uint)(variantSeed + segment * 3) %
                                        activeTextures.Length);
                    var triangle = segment * 6;
                    for (var offset = 0; offset < 6; offset++)
                        submeshTriangles[variant].Add(triangles[triangle + offset]);
                }
                for (var variant = 0; variant < activeTextures.Length; variant++)
                    mesh.SetTriangles(submeshTriangles[variant], variant);
            }
            else mesh.triangles = triangles;
            mesh.RecalculateNormals();
            // Both banks deliberately retain the same triangle winding for
            // two-sided transparency. Mirroring the geometry flips the normals,
            // however: correct lighting orientation without changing topology.
            var bankNormals = mesh.normals;
            for (var i = 0; i < bankNormals.Length; i++)
                if (bankNormals[i].y < 0f) bankNormals[i] = -bankNormals[i];
            mesh.normals = bankNormals;
            mesh.RecalculateBounds();
            var item = new GameObject(name);
            item.transform.SetParent(_riverRoot, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            var shader = Shader.Find("CityForgeV3/RiverBedSurface") ??
                         Shader.Find("Standard");
            Material CreateBandMaterial(Texture2D bandTexture, int variant)
            {
                var material = new Material(shader)
                {
                    name = name + (variant < 0
                        ? " Material"
                        : $" Variant {variant + 1} Material"),
                    color = Color.white,
                    mainTexture = bandTexture
                };
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", bandTexture);
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", bandTexture);
                bandTexture.wrapModeU = TextureWrapMode.Repeat;
                bandTexture.wrapModeV = TextureWrapMode.Clamp;
                if (material.HasProperty("_Surface"))
                    material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_SrcBlend"))
                    material.SetFloat("_SrcBlend",
                        (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend"))
                    material.SetFloat("_DstBlend",
                        (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite"))
                    material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                return material;
            }
            var renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = activeTextures != null &&
                                       activeTextures.Length > 1
                ? activeTextures.Select((candidate, variant) =>
                    CreateBandMaterial(candidate, variant)).ToArray()
                : new[] { CreateBandMaterial(texture, -1) };
            // These overlapping transparent bands must not be distance-sorted
            // differently on the near and far banks. Render dirt first and the
            // feathered grass edge second on both sides.
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            if (hideAtFarZoom)
                _riverGrassEdgeRenderers.Add(renderer);
        }

        private static int StableStringHash(string value)
        {
            unchecked
            {
                var hash = 17;
                foreach (var character in value ?? string.Empty)
                    hash = hash * 31 + character;
                return hash;
            }
        }

        public void RefreshRoadAndNeighbors(RegionCityTile district, int x, int z)
        {
            if (district == null || _roadArtworkRoot == null) return;
            foreach (var cell in new[]
                     {
                         new Vector2Int(x, z), new Vector2Int(x + 1, z),
                         new Vector2Int(x - 1, z), new Vector2Int(x, z + 1),
                         new Vector2Int(x, z - 1)
                     })
            {
                if (_roadsByCell.TryGetValue(cell, out var prior))
                {
                    _roadsByCell.Remove(cell);
                    var renderer = prior != null ? prior.GetComponent<Renderer>() : null;
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        if (Application.isPlaying) Destroy(renderer.sharedMaterial);
                        else DestroyImmediate(renderer.sharedMaterial);
                    }
                    if (prior != null)
                    {
                        if (Application.isPlaying) Destroy(prior);
                        else DestroyImmediate(prior);
                    }
                }
                var road = RoadPlacementModel.FindAt(district.Roads, cell.x, cell.y);
                if (road != null) AddRoadPiece(road);
            }
        }

        private void AddRoadPiece(PlacedRoadPiece placed)
        {
            if (placed == null) return;
            var package = RoadPiecePackageCatalog.Resolve(placed.PackageId);
            var piece = package.Piece(placed.Topology);
            if (piece?.HasArtwork != true) return;
            var suffix = package.SupportsIndependentMarkings
                ? "-no-lanes-no-center" : "-no-lines";
            var texture = Resources.Load<Texture2D>(piece.ResourcePath + suffix) ??
                          Resources.Load<Texture2D>(piece.ResourcePath);
            if (texture == null) return;
            var roadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            roadObject.name = $"District {package.DisplayName} {placed.Topology}";
            roadObject.transform.SetParent(_roadArtworkRoot, false);
            roadObject.transform.localPosition = new Vector3(
                -_widthMeters * 0.5f + (placed.GridX + 0.5f) * DistrictScale.CellSizeMeters,
                0.152f,
                -_depthMeters * 0.5f + (placed.GridZ + 0.5f) * DistrictScale.CellSizeMeters);
            roadObject.transform.localRotation = Quaternion.Euler(90f,
                placed.RotationQuarterTurns * 90f, 0f);
            roadObject.transform.localScale = new Vector3(
                package.ArtworkWidthMeters, package.ArtworkLengthMeters, 1f);
            roadObject.GetComponent<Collider>().enabled = false;
            var shader = Shader.Find("CityForgeV3/ShadowReceivingRoadOverlay") ??
                         Shader.Find("Unlit/Transparent");
            var material = new Material(shader)
            {
                name = $"District {package.DisplayName} {placed.Topology}",
                mainTexture = texture,
                renderQueue = 3002
            };
            if (package.Id != RoadPiecePackageCatalog.DirtRoadId &&
                package.Id != RoadPiecePackage.LegacyPackageId)
            {
                var roadSurface = RoadMaterialCatalog.Resolve(placed.RoadMaterialId);
                var sidewalkSurface = RoadMaterialCatalog.Resolve(
                    placed.SidewalkMaterialId, true);
                material.SetFloat("_UseMaterialZones", 1f);
                material.SetTexture("_RoadSurfaceTex", roadSurface.LoadTexture());
                material.SetTexture("_SidewalkSurfaceTex", sidewalkSurface.LoadTexture());
                material.SetFloat("_RoadMaterialTiling", roadSurface.TilesPerTenMeters);
                material.SetFloat("_SidewalkMaterialTiling", sidewalkSurface.TilesPerTenMeters);
            }
            if (material.HasProperty("_HideCurbBorders"))
                material.SetFloat("_HideCurbBorders",
                    package.Id == RoadPiecePackage.LegacyPackageId ||
                    placed.RoadMaterialId == "antique-brick" ? 1f : 0f);
            roadObject.GetComponent<Renderer>().sharedMaterial = material;
            _roadsByCell[new Vector2Int(placed.GridX, placed.GridZ)] = roadObject;
            if (material.HasProperty("_TimeTint"))
                material.SetColor("_TimeTint",
                    TimeOfDayLighting.For(TimeOfDay).NeutralArtworkTint);
        }

        public bool AddPlacedLot(RegionCityTile district,
            PlacedDistrictLot placement)
        {
            if (_content == null || district == null || placement == null ||
                string.IsNullOrWhiteSpace(placement.LotId)) return false;
            var data = LotContentCatalog.Read(placement.LotId);
            if (data == null) return false;
            var lot = AddLot(data,
                DistrictLotCenterMeters(district, placement, data),
                placement.RotationQuarterTurns, placement.InstanceId, true);
            if (lot == null) return false;
            lot.SetDistrictPresentationLevel(PresentationLevel(_zoomLevel));
            lot.SetTimeOfDay(TimeOfDay);
            return true;
        }

        public bool UpdatePlacedLotTransform(RegionCityTile district,
            PlacedDistrictLot placement)
        {
            if (district == null || placement == null ||
                !_lotsByInstance.TryGetValue(placement.InstanceId,
                    out var lot) || lot == null) return false;
            var data = LotContentCatalog.Read(placement.LotId);
            if (data == null) return false;
            var center = DistrictLotCenterMeters(district, placement, data);
            lot.transform.localPosition = new Vector3(center.x, 0.04f, center.y);
            lot.transform.localRotation = Quaternion.Euler(0f,
                placement.RotationQuarterTurns * 90f +
                HostedLotFacingOffsetDegrees, 0f);
            return true;
        }

        public void ShowLotPlacementGuide(int gridX, int gridZ,
            int spanX, int spanZ, bool placeable)
        {
            if (_content == null) return;
            spanX = Mathf.Max(1, spanX);
            spanZ = Mathf.Max(1, spanZ);
            if (_placementGuide == null)
            {
                _placementGuide = new GameObject("District Lot Placement Guide");
                _placementGuide.transform.SetParent(_content, false);
                _placementGuideMesh = new Mesh
                {
                    name = "District Lot Placement Cells"
                };
                _placementGuide.AddComponent<MeshFilter>().sharedMesh =
                    _placementGuideMesh;
                _placementGuideMaterial = new Material(
                    Shader.Find("Sprites/Default"))
                {
                    name = "District Lot Placement Guide Material"
                };
                _placementGuideMaterial.renderQueue = 3100;
                _placementGuide.AddComponent<MeshRenderer>().sharedMaterial =
                    _placementGuideMaterial;
            }

            const float inset = 0.18f;
            const float elevation = 0.22f;
            var vertices = new List<Vector3>(spanX * spanZ * 4);
            var triangles = new List<int>(spanX * spanZ * 6);
            for (var z = 0; z < spanZ; z++)
            for (var x = 0; x < spanX; x++)
            {
                var x0 = x * DistrictScale.CellSizeMeters + inset;
                var x1 = (x + 1) * DistrictScale.CellSizeMeters - inset;
                var z0 = z * DistrictScale.CellSizeMeters + inset;
                var z1 = (z + 1) * DistrictScale.CellSizeMeters - inset;
                var start = vertices.Count;
                vertices.Add(new Vector3(x0, elevation, z0));
                vertices.Add(new Vector3(x1, elevation, z0));
                vertices.Add(new Vector3(x1, elevation, z1));
                vertices.Add(new Vector3(x0, elevation, z1));
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }
            _placementGuideMesh.Clear();
            _placementGuideMesh.SetVertices(vertices);
            _placementGuideMesh.SetTriangles(triangles, 0);
            _placementGuideMesh.RecalculateBounds();
            _placementGuideMaterial.color = placeable
                ? new Color(0.35f, 1f, 0.08f, 0.48f)
                : new Color(1f, 0.08f, 0.06f, 0.52f);
            _placementGuide.transform.localPosition = new Vector3(
                -_widthMeters * 0.5f + gridX * DistrictScale.CellSizeMeters,
                0f,
                -_depthMeters * 0.5f + gridZ * DistrictScale.CellSizeMeters);
            _placementGuide.SetActive(true);
        }

        public void HideLotPlacementGuide()
        {
            if (_placementGuide != null) _placementGuide.SetActive(false);
        }

        public void ShowRoadSelectionGuide(int gridX, int gridZ)
        {
            ShowLotPlacementGuide(gridX, gridZ, 1, 1, true);
            if (_placementGuideMaterial != null)
                _placementGuideMaterial.color =
                    new Color(1f, 0.76f, 0.08f, 0.62f);
        }

        public void SetPan(Vector2 pan)
        {
            var clamped = ClampPan(pan);
            _pan = new Vector3(clamped.x, 0f, clamped.y);
            ApplyCameraPose();
        }

        public Vector2 ClampPan(Vector2 pan) => new(
            Mathf.Clamp(pan.x, -_widthMeters * 0.5f, _widthMeters * 0.5f),
            Mathf.Clamp(pan.y, -_depthMeters * 0.5f, _depthMeters * 0.5f));

        public bool TryGroundPoint(Vector2 panelPosition, out Vector2 normalized)
        {
            normalized = default;
            if (_camera == null || _widthMeters <= 0f || _depthMeters <= 0f)
                return false;
            var screenPoint = new Vector3(panelPosition.x,
                Screen.height - panelPosition.y, 0f);
            var ray = _camera.ScreenPointToRay(screenPoint);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out var distance)) return false;
            var point = ray.GetPoint(distance);
            normalized = new Vector2(point.x / _widthMeters + 0.5f,
                point.z / _depthMeters + 0.5f);
            return normalized.x >= 0f && normalized.x <= 1f &&
                   normalized.y >= 0f && normalized.y <= 1f;
        }

        public bool TryMouseGroundPoint(out Vector2 normalized)
        {
            normalized = default;
            if (_camera == null || _widthMeters <= 0f || _depthMeters <= 0f)
                return false;
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out var distance)) return false;
            var point = ray.GetPoint(distance);
            normalized = new Vector2(point.x / _widthMeters + 0.5f,
                point.z / _depthMeters + 0.5f);
            return normalized.x >= 0f && normalized.x <= 1f &&
                   normalized.y >= 0f && normalized.y <= 1f;
        }

        public string FindDistrictRiverAtPanel(RegionCityTile district,
            Vector2 panelPosition)
        {
            if (_camera == null || district?.Rivers == null) return "";
            var pointer = new Vector2(panelPosition.x,
                Screen.height - panelPosition.y);
            var closestDistance = float.PositiveInfinity;
            var closestId = "";
            foreach (var river in district.Rivers)
            {
                if (river?.Points == null || river.Points.Count < 2) continue;
                for (var index = 0; index < river.Points.Count - 1; index++)
                {
                    Vector2 Project(DistrictRiverPoint point)
                    {
                        var screen = _camera.WorldToScreenPoint(new Vector3(
                            (point.X - .5f) * _widthMeters, .2f,
                            (point.Z - .5f) * _depthMeters));
                        return new Vector2(screen.x, screen.y);
                    }
                    var from = Project(river.Points[index]);
                    var to = Project(river.Points[index + 1]);
                    var segment = to - from;
                    var denominator = segment.sqrMagnitude;
                    var t = denominator <= .001f ? 0f : Mathf.Clamp01(
                        Vector2.Dot(pointer - from, segment) / denominator);
                    var distance = Vector2.Distance(pointer,
                        from + segment * t);

                    // The visible water can become only a few pixels wide at
                    // district scale. Preserve a generous click target while
                    // still scaling it with the projected channel width.
                    var midpoint = Vector2.Lerp(from, to, .5f);
                    var worldSegment = new Vector2(
                        (river.Points[index + 1].X -
                         river.Points[index].X) * _widthMeters,
                        (river.Points[index + 1].Z -
                         river.Points[index].Z) * _depthMeters).normalized;
                    var normal = new Vector2(-worldSegment.y, worldSegment.x);
                    var centerPoint = Vector2.Lerp(
                        new Vector2(river.Points[index].X,
                            river.Points[index].Z),
                        new Vector2(river.Points[index + 1].X,
                            river.Points[index + 1].Z), .5f);
                    var offsetWorld = new Vector3(
                        (centerPoint.x - .5f) * _widthMeters +
                        normal.x * river.WidthMeters * .6f,
                        .2f,
                        (centerPoint.y - .5f) * _depthMeters +
                        normal.y * river.WidthMeters * .6f);
                    var offsetScreen3 = _camera.WorldToScreenPoint(offsetWorld);
                    var projectedRadius = Vector2.Distance(midpoint,
                        new Vector2(offsetScreen3.x, offsetScreen3.y));
                    var hitRadius = Mathf.Max(14f, projectedRadius);
                    if (distance > hitRadius || distance >= closestDistance)
                        continue;
                    closestDistance = distance;
                    closestId = river.InstanceId;
                }
            }
            return closestId;
        }

        public void ShowLotOutline(int gridX, int gridZ, int spanX,
            int spanZ, bool selected)
        {
            if (_content == null) return;
            if (_lotOutline == null)
            {
                _lotOutline = new GameObject("District Lot Hover Outline");
                _lotOutline.transform.SetParent(_content, false);
                _lotOutlineRenderer = _lotOutline.AddComponent<LineRenderer>();
                _lotOutlineRenderer.useWorldSpace = false;
                _lotOutlineRenderer.loop = true;
                _lotOutlineRenderer.positionCount = 4;
                _lotOutlineRenderer.alignment = LineAlignment.View;
                _lotOutlineRenderer.numCornerVertices = 2;
                _lotOutlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                _lotOutlineRenderer.receiveShadows = false;
                _lotOutlineRenderer.sharedMaterial = new Material(
                    Shader.Find("Sprites/Default"))
                {
                    name = "District Lot Outline Material"
                };
            }
            var width = spanX * DistrictScale.CellSizeMeters;
            var depth = spanZ * DistrictScale.CellSizeMeters;
            _lotOutline.transform.localPosition = new Vector3(
                -_widthMeters * 0.5f + gridX * DistrictScale.CellSizeMeters,
                0.34f,
                -_depthMeters * 0.5f + gridZ * DistrictScale.CellSizeMeters);
            _lotOutlineRenderer.SetPosition(0, new Vector3(0f, 0f, 0f));
            _lotOutlineRenderer.SetPosition(1, new Vector3(width, 0f, 0f));
            _lotOutlineRenderer.SetPosition(2, new Vector3(width, 0f, depth));
            _lotOutlineRenderer.SetPosition(3, new Vector3(0f, 0f, depth));
            _lotOutlineRenderer.startWidth = selected ? 0.72f : 0.46f;
            _lotOutlineRenderer.endWidth = _lotOutlineRenderer.startWidth;
            _lotOutlineRenderer.startColor = selected
                ? new Color(1f, 0.78f, 0.12f, 1f)
                : new Color(0.45f, 1f, 0.18f, 0.95f);
            _lotOutlineRenderer.endColor = _lotOutlineRenderer.startColor;
            _lotOutline.SetActive(true);
        }

        public void HideLotOutline()
        {
            if (_lotOutline != null) _lotOutline.SetActive(false);
        }

        public void SetZoom(DistrictZoomLevel level)
        {
            if (_camera == null) return;
            _zoomLevel = level;
            _camera.orthographicSize = OrthographicSize(level,
                _widthMeters, _depthMeters, _camera.aspect);
            foreach (var lot in _lots)
                if (lot != null)
                    lot.SetDistrictPresentationLevel(PresentationLevel(level));
            // Hosted lot LOD changes also reconfigure shared district lighting.
            // Reapply standalone flora projections at every zoom transition.
            UpdateDistrictFloraShadows();
            ApplyRiverGrassEdgeVisibility();
            ApplyGridVisibility();
            ApplyCameraPose();
        }

        public static bool ShowsRiverGrassEdge(DistrictZoomLevel level) =>
            level <= DistrictZoomLevel.LOD3;

        private void ApplyRiverGrassEdgeVisibility()
        {
            var visible = ShowsRiverGrassEdge(_zoomLevel);
            foreach (var renderer in _riverGrassEdgeRenderers)
                if (renderer != null)
                    renderer.enabled = visible;
        }

        public void ToggleGridVisibility()
        {
            _districtGridVisible = !_districtGridVisible;
            ApplyGridVisibility();
        }

        private void ApplyGridVisibility()
        {
            if (_grid != null)
                _grid.gameObject.SetActive(_districtGridVisible &&
                    _zoomLevel != DistrictZoomLevel.LOD5Billboard);
            if (_minorGrid != null)
                _minorGrid.SetActive(_districtGridVisible &&
                    _zoomLevel <= DistrictZoomLevel.LOD2);
        }

        public void SetTimeOfDay(TimeOfDayPreset preset)
        {
            TimeOfDay = preset;
            foreach (var renderer in _districtFloraPresentations.Values)
                if (renderer != null)
                    renderer.color = TimeOfDayLighting.For(preset)
                        .NeutralArtworkTint;
            foreach (var lot in _lots)
                if (lot != null)
                    lot.SetTimeOfDay(preset);

            var spec = TimeOfDayLighting.For(preset);
            // A hosted LotWorldController applies the complete, proven Lot
            // Editor lighting contract to this shared sun: bias, strength,
            // culling, projected shadows, flora, characters, and night lights.
            // Only use the simple fallback when the district has no lot yet.
            if (_sun != null && _lots.Count == 0)
            {
                _sun.transform.rotation = TimeOfDayLighting.SunRotation(preset);
                _sun.color = spec.SunColor;
                _sun.intensity = spec.SunIntensity;
                _sun.shadows = preset == TimeOfDayPreset.Night
                    ? LightShadows.None : LightShadows.Soft;
                _sun.shadowStrength = preset == TimeOfDayPreset.Noon
                    ? 0.92f : 0.72f;
            }
            if (_camera != null)
                _camera.backgroundColor = spec.BackgroundColor;
            ApplyDistrictGroundPresentation(preset);
            UpdateDistrictFloraShadows();
        }

        public static Vector2 DistrictLotCenterMeters(RegionCityTile district,
            float normalizedX, float normalizedY, float lotWidthMeters,
            float lotDepthMeters)
        {
            var width = DistrictScale.SizeMeters(district.Width);
            var depth = DistrictScale.SizeMeters(district.Height);
            var snappedX = DistrictScale.SnapFootprintCenter(normalizedX,
                DistrictScale.Columns(district.Width), lotWidthMeters);
            var snappedY = DistrictScale.SnapFootprintCenter(normalizedY,
                DistrictScale.Columns(district.Height), lotDepthMeters);
            return new Vector2((snappedX - 0.5f) * width,
                (snappedY - 0.5f) * depth);
        }

        public static Vector2 DistrictLotCenterMeters(RegionCityTile district,
            PlacedDistrictLot placement, LotSaveData lot)
        {
            var width = DistrictScale.SizeMeters(district.Width);
            var depth = DistrictScale.SizeMeters(district.Height);
            var spanX = DistrictScale.GridSpanForMeters(
                lot.LotWidthCells * LotMetricScale.MajorGridMeters);
            var spanZ = DistrictScale.GridSpanForMeters(
                lot.LotDepthCells * LotMetricScale.MajorGridMeters);
            return new Vector2(
                -width * 0.5f + (placement.GridX + spanX * 0.5f) *
                    LotMetricScale.MajorGridMeters,
                -depth * 0.5f + (placement.GridZ + spanZ * 0.5f) *
                    LotMetricScale.MajorGridMeters);
        }

        public static float OrthographicSize(DistrictZoomLevel level,
            float widthMeters, float depthMeters, float aspect)
        {
            if (level == DistrictZoomLevel.LOD0)
                return LotWorldController.OrthographicSizeForLot(
                    LotZoomLevel.Detail, 50);
            var fullFit = Mathf.Max(depthMeters * 0.36f,
                widthMeters * 0.5f / Mathf.Max(0.5f, aspect)) * 1.12f;
            return level switch
            {
                DistrictZoomLevel.LOD1 => 44f,
                DistrictZoomLevel.LOD2 => 132f,
                DistrictZoomLevel.LOD3 => fullFit * 0.70f,
                DistrictZoomLevel.LOD4 => fullFit,
                DistrictZoomLevel.LOD5Billboard => fullFit * 1.35f,
                _ => fullFit
            };
        }

        private LotWorldController AddLot(LotSaveData data, Vector2 center,
            int rotationQuarterTurns, string instanceId,
            bool animateConstruction = false)
        {
            var host = new GameObject($"District Lot — {data.Name}");
            host.transform.SetParent(_content, false);
            var lot = host.AddComponent<LotWorldController>();
            lot.BuildAsDistrictHosted(_camera, _sun);
            lot.ConfigureDistrictRiverSurfaceSampler(SampleRiverSurface);
            lot.LoadRuntimeLot(data);
            lot.ConfigureAsDistrictHosted(_camera, _sun,
                PresentationLevel(DistrictZoom.DefaultLevel));
            host.transform.localPosition = new Vector3(center.x, 0.04f, center.y);
            host.transform.localRotation = Quaternion.Euler(0f,
                rotationQuarterTurns * 90f + HostedLotFacingOffsetDegrees,
                0f);
            _lots.Add(lot);
            if (!string.IsNullOrWhiteSpace(instanceId))
                _lotsByInstance[instanceId] = lot;
            if (animateConstruction)
                lot.BeginAllBuildingConstruction();
            return lot;
        }

        private void BuildCamera()
        {
            var cameraObject = new GameObject("District World Camera");
            cameraObject.transform.SetParent(transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.10f, 0.14f, 0.24f);
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 10000f;
            _camera.depth = 10f;
        }

        private void BuildSun()
        {
            var sunObject = new GameObject("District Sun");
            sunObject.transform.SetParent(transform, false);
            sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            _sun = sunObject.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.intensity = 1.15f;
            _sun.shadows = LightShadows.Soft;
        }

        private void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "District Ground — 10 Meter Lot Grid Contract";
            ground.transform.SetParent(_content, false);
            ground.transform.localScale = new Vector3(
                _widthMeters / 10f, 1f, _depthMeters / 10f);
            var renderer = ground.GetComponent<MeshRenderer>();
            _groundRenderer = renderer;
            var shader = Shader.Find("CityForgeV3/ShadowReceivingLotSurface") ??
                         Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "District Grass Material",
                // Preserve the authored grass color. Directional sunlight and
                // the receiving shader provide the environmental modulation.
                color = Color.white
            };
            var texture = Resources.Load<Texture2D>(DistrictGrassResource);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                material.mainTexture = texture;
                material.mainTextureScale = new Vector2(
                    _widthMeters / DistrictGrassTextureWorldSizeMeters,
                    _depthMeters / DistrictGrassTextureWorldSizeMeters);
            }
            renderer.sharedMaterial = material;
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void ApplyDistrictGroundPresentation(TimeOfDayPreset preset)
        {
            if (_groundRenderer?.sharedMaterial == null) return;
            var material = _groundRenderer.sharedMaterial;
            var tint = LotWorldController.TextureTintForTimeOfDay(preset);
            // Preserve the authored colour just as the riverbank surface does.
            // The previous additional 0.75/0.80 calibration turned these
            // matching grass pixels into a dark olive field.
            material.color = tint;
            if (material.HasProperty("_AmbientFloor"))
                material.SetFloat("_AmbientFloor", preset switch
                {
                    TimeOfDayPreset.Morning => .68f,
                    TimeOfDayPreset.Noon => .58f,
                    TimeOfDayPreset.Afternoon => .66f,
                    _ => .52f
                });
            if (!material.HasProperty("_TerrainSunDirection")) return;
            var direction = _sun == null
                ? Vector3.up
                : -_sun.transform.forward.normalized;
            material.SetVector("_TerrainSunDirection", new Vector4(
                direction.x, direction.y, direction.z, 0f));
        }

        private void BuildGrid()
        {
            _grid = new GameObject("District Major Grid — 10 Meters").transform;
            _grid.SetParent(_content, false);
            var minorMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.48f, 0.68f, 0.66f, 0.18f)
            };
            var majorMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(1f, 0.84f, 0.38f, 0.30f)
            };
            _minorGrid = BuildGridMesh("Minor Grid — 1 Meter",
                LotMetricScale.MinorGridMeters, 0.035f, 0.13f, minorMaterial);
            _majorGrid = BuildGridMesh("Major Grid — 10 Meters",
                LotMetricScale.MajorGridMeters, 0.085f, 0.131f, majorMaterial);
        }

        private GameObject BuildGridMesh(string objectName, float spacing,
            float lineWidth, float elevation, Material material)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            void AddQuad(float x0, float z0, float x1, float z1)
            {
                var start = vertices.Count;
                vertices.Add(new Vector3(x0, elevation, z0));
                vertices.Add(new Vector3(x1, elevation, z0));
                vertices.Add(new Vector3(x1, elevation, z1));
                vertices.Add(new Vector3(x0, elevation, z1));
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
            }
            for (var x = -_widthMeters * 0.5f;
                 x <= _widthMeters * 0.5f + 0.001f; x += spacing)
                AddQuad(x - lineWidth * 0.5f, -_depthMeters * 0.5f,
                    x + lineWidth * 0.5f, _depthMeters * 0.5f);
            for (var z = -_depthMeters * 0.5f;
                 z <= _depthMeters * 0.5f + 0.001f; z += spacing)
                AddQuad(-_widthMeters * 0.5f, z - lineWidth * 0.5f,
                    _widthMeters * 0.5f, z + lineWidth * 0.5f);
            var mesh = new Mesh { name = objectName };
            mesh.indexFormat = vertices.Count > 65535
                ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            var item = new GameObject(objectName);
            item.transform.SetParent(_grid, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material;
            return item;
        }

        private void ApplyCameraPose()
        {
            if (_camera == null) return;
            var target = _pan;
            const float elevationDegrees = 20f;
            var cameraRadius = CameraRadius(_zoomLevel);
            var horizontalRadius = Mathf.Cos(
                elevationDegrees * Mathf.Deg2Rad) * cameraRadius;
            var diagonal = horizontalRadius / Mathf.Sqrt(2f);
            _camera.transform.position = target + new Vector3(
                -diagonal,
                Mathf.Sin(elevationDegrees * Mathf.Deg2Rad) * cameraRadius,
                -diagonal);
            _camera.transform.LookAt(target, Vector3.up);
        }

        public static float CameraRadius(DistrictZoomLevel level) =>
            level switch
            {
                // LOD0 is the Lot Editor's native 3D inspection view. Its
                // 60 m radius keeps the same meshes inside the same 150 m
                // directional-shadow range instead of silently dropping all
                // native building, prop, character, and flora shadows.
                DistrictZoomLevel.LOD0 => 60f,
                DistrictZoomLevel.LOD1 => 180f,
                DistrictZoomLevel.LOD2 => 480f,
                DistrictZoomLevel.LOD3 => 1600f,
                DistrictZoomLevel.LOD4 => 2400f,
                _ => 3200f
            };

        private void ClearWorld()
        {
            _groundDecals = null;
            _lots.Clear();
            _lotsByInstance.Clear();
            _roadsByCell.Clear();
            _riverSurfaces.Clear();
            _districtFloraPresentations.Clear();
            _districtSelectionRoot = null;
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private static LotZoomLevel PresentationLevel(DistrictZoomLevel level) =>
            level switch
            {
                DistrictZoomLevel.LOD0 => LotZoomLevel.Detail,
                DistrictZoomLevel.LOD1 => LotZoomLevel.Close,
                DistrictZoomLevel.LOD2 => LotZoomLevel.Lot,
                DistrictZoomLevel.LOD3 => LotZoomLevel.Wide,
                DistrictZoomLevel.LOD4 => LotZoomLevel.Far,
                _ => LotZoomLevel.Neighborhood
            };
    }
}
