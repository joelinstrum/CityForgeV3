using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum LotObjectSelectionKind
    {
        None,
        Building,
        Flora,
        Prop,
        BuildingProp
    }

    public sealed partial class LotWorldController : MonoBehaviour
    {
        public readonly struct CameraFramingState
        {
            public readonly bool Valid;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly float OrthographicSize;

            public CameraFramingState(Camera camera)
            {
                Valid = camera != null;
                Position = camera != null ? camera.transform.position : Vector3.zero;
                Rotation = camera != null ? camera.transform.rotation : Quaternion.identity;
                OrthographicSize = camera != null ? camera.orthographicSize : 0f;
            }
        }

        private static readonly Color GroundColor = new(0.27f, 0.34f, 0.27f);
        private const int FloraShadowReceiverLayer = 31;
        private const float BuildingCameraFrontToleranceMeters = 0.01f;
        private const float BuildingCameraFrontInteriorRecoveryMeters = 1.5f;
        private const int BuildingOcclusionStencilShift = 2;
        private const int BuildingOcclusionStencilRecoverableSlots = 62;
        private const int BuildingOcclusionStencilOverflowReference = 252;
        // Semantic massing remains active for occlusion and native shadows,
        // but normal artwork mode hides it after the registration experiment.
        private const bool ShowBuildingPrimitivesExperiment = false;
        private Camera _camera;
        private HybridBuildingPackage _buildingPackage;
        private Transform _cameraPivot;
        private Transform _minorGrid;
        private Transform _majorGrid;
        private Transform _majorStripDeletionPreview;
        private bool _gridVisible = true;
        private Transform _neighborhoodRoad;
        private Transform _roadArtworkRoot;
        private Transform _outsideConnectorRoot;
        private Transform _floraRoot;
        private Transform _floraSelection;
        private SpriteRenderer _floraPreview;
        private string _floraPreviewId = "";
        private bool _floraPreviewHasPoint;
        private readonly List<SpriteRenderer> _floraPresentations = new();
        private readonly List<SpriteRenderer> _floraCastShadows = new();
        private Material _floraLitShadowReceiverMaterial;
        private readonly Dictionary<int, Material>
            _floraHostFrontRecoveryMaterials = new();
        private readonly List<(float distance, float lateral, int stencil)>
            _floraFrontHostCandidates = new();
        private readonly List<int> _floraFrontHostStencilReferences = new(4);
        private Light _floraShadowSun;
        private bool _floraEditorActive;
        private bool _floraPlacementActive;
        private bool _floraDragActive;
        private Vector2 _floraDragOffset;
        private Transform _roadCursor;
        private Renderer _roadCursorFill;
        private Transform _trafficVehicle;
        private Transform _circulationRoot;
        private Transform _circulationTravelerRoot;
        private bool _circulationEditorActive;
        private Transform _circulationCursor;
        private Transform _pedestrianTraveler;
        private Transform _vehicleTraveler;
        private VehicleRuntimePresentation _vehiclePresentation;
        private readonly List<VehicleRuntimePresentation> _vehiclePresentations = new();
        private readonly List<TestVehicleTraveler> _testVehicles = new();
        private Transform _proxy;
        private Transform _buildingDepthOccluder;
        private HybridBuildingPresentation _presentation;
        private readonly List<HybridBuildingPresentation> _otherBuildingPresentations = new();
        private readonly List<int> _otherBuildingIndices = new();
        private readonly List<Transform> _otherBuildingDepthOccluders = new();
        private readonly List<Transform> _otherBuildingProjectedShadows = new();
        private readonly List<HybridBuildingPackage> _otherBuildingShadowPackages = new();
        private readonly List<MeshRenderer> _buildingGapShadowRenderers = new();
        private Transform _selectionFootprint;
        private Transform _selectionFrontMarker;
        private Transform _objectHoverHighlight;
        private int _hoverObjectIndex = -1;
        private Transform _registrationDiagnostics;
        private Renderer[] _proxyRenderers;
        private Renderer[] _buildingDepthOccluderRenderers;
        private MeshFilter[] _proxyMeshFilters;
        private readonly List<Vector3> _proxyLocalVertices = new();
        private Transform _projectedShadow;
        private Light _sun;
        private Transform _ground;
        private Renderer _groundRenderer;
        private int _facing;
        private string _placementBuildingId =
            BuildingCatalog.ColonialGovernmentHouseId;
        private readonly LotEditorSession _session = new();
        private readonly Stack<string> _roadUndo = new();
        private readonly Stack<string> _roadRedo = new();
        private string _roadStrokeStart;
        private bool _buildingDragActive;
        private bool _buildingFocusFreezeActive;
        private int _sessionStateApplyCountForQa;
        private int _buildingFocusLiveMoveCountForQa;
        private bool _buildingsSelectable = true;
        private bool _cameraPanInteractionActive;
        private bool _buildingAuthoringActive;
        private float _buildingContextOpacity = 1f;
        private Vector2 _buildingDragOffset;
        private float _trafficProgress;
        private float _pedestrianProgress;
        private RoadTrafficGraph _trafficGraph;
        private VehicleTypePackage _vehicleType;
        private readonly List<LaneVehicleState> _laneVehicleStates = new();
        private List<Vector2> _suburbanTrip = new();
        private float _suburbanTripDistance;
        private float _suburbanSpawnTimer = TrafficLotModel.SuburbanMinimumSpawnSeconds;
        private int _suburbanSpawnSequence;
        private bool _suburbanVehicleActive;
        private Vector3 _cameraPanWorld;
        private string _activeNodeId = "";
        private RoadPiecePackage _roadPackage;
        private string _selectedRoadMaterialId = RoadMaterialCatalog.DefaultRoadId;
        private string _selectedSidewalkMaterialId = RoadMaterialCatalog.DefaultSidewalkId;
        private RoadMarkingStyle _selectedRoadMarkingStyle = RoadMarkingStyle.SingleDotted;
        private RoadLaneMarkingStyle _selectedRoadLaneMarkingStyle = RoadLaneMarkingStyle.Lines;
        private RoadCenterMarkingStyle _selectedRoadCenterMarkingStyle = RoadCenterMarkingStyle.DoubleLines;
        private bool _applyRoadMaterialsToAll;
        public IReadOnlyList<RoadPiecePackage> RoadPackages =>
            RoadPiecePackageCatalog.Packages;
        public string SelectedRoadPackageId => _roadPackage?.Id ?? "";
        public bool BuildingFocusFreezeActive => _buildingFocusFreezeActive;
        public int SessionStateApplyCountForQa => _sessionStateApplyCountForQa;
        public int BuildingFocusLiveMoveCountForQa =>
            _buildingFocusLiveMoveCountForQa;
        public string SelectedRoadMaterialId => _selectedRoadMaterialId;
        public string SelectedSidewalkMaterialId => _selectedSidewalkMaterialId;
        public RoadMarkingStyle SelectedRoadMarkingStyle => _selectedRoadMarkingStyle;
        public RoadLaneMarkingStyle SelectedRoadLaneMarkingStyle => _selectedRoadLaneMarkingStyle;
        public RoadCenterMarkingStyle SelectedRoadCenterMarkingStyle => _selectedRoadCenterMarkingStyle;
        public bool SelectedRoadSupportsIndependentMarkings =>
            _roadPackage?.SupportsIndependentMarkings == true;
        public bool ApplyRoadMaterialsToAll => _applyRoadMaterialsToAll;
        public RoadMaterialDefinition SelectedRoadMaterial =>
            RoadMaterialCatalog.Resolve(_selectedRoadMaterialId);
        public RoadMaterialDefinition SelectedSidewalkMaterial =>
            RoadMaterialCatalog.Resolve(_selectedSidewalkMaterialId, true);
        public bool SelectedRoadSupportsMaterials => _roadPackage != null &&
            _roadPackage.Id != RoadPiecePackage.LegacyPackageId;
        public RoadPieceTopology SelectedRoadTopology { get; private set; } = RoadPieceTopology.Straight;
        public Vector2Int RoadCursorCell { get; private set; } = new(-1, -1);
        public int RoadRotationQuarterTurns { get; private set; }
        public int PlacedRoadCount => _session.Data.RoadPieces.Count;
        public IReadOnlyList<string> RoadValidationIssues =>
            RoadPlacementModel.Validate(_session.Data.RoadPieces, _roadPackage,
                LotWidthMeters, LotDepthMeters);
        public bool SelectedRoadPieceAvailable =>
            _roadPackage?.Piece(SelectedRoadTopology)?.HasArtwork == true;
        public bool HasRoadSuggestion => RoadPlacementModel.TrySuggest(_session.Data.RoadPieces,
            RoadCursorCell.x, RoadCursorCell.y, _roadPackage, out _, out _);
        public bool CanUndoRoadEdit => _roadUndo.Count > 0;
        public bool CanRedoRoadEdit => _roadRedo.Count > 0;
        public TrafficLotType TrafficType => _session.Data.TrafficType;
        public int OutsideConnectorCount => _session.Data.OutsideRoadConnectors?.Count ?? 0;
        public int OutsideConnectorMarkerCount => _outsideConnectorRoot?.childCount ?? 0;
        public bool SuburbanVehicleActive => _suburbanVehicleActive;
        public float SuburbanTripLengthMeters => TrafficLotModel.TripLength(_suburbanTrip);

        public string FacingLabel => _buildingPackage.Facing(_facing).Id;
        public BuildingInspectionMode InspectionMode { get; private set; } =
            BuildingInspectionMode.Artwork;
        public bool TopDownViewEnabled { get; private set; }
        public float CameraPitchDegrees => _camera == null
            ? 0f : _camera.transform.eulerAngles.x;
        private BuildingInspectionMode _inspectionModeBeforeTopDown =
            BuildingInspectionMode.Artwork;
        private Vector2 _topDownWorldZScreenDirection = Vector2.up;
        public bool ProxyVisible =>
            BuildingInspectionPolicy.ShowsPrimitive(InspectionMode);
        public bool RegistrationDiagnosticsVisible { get; private set; }
        public LotEditorSession Session => _session;
        public bool HasBuilding => _session.Data.HasBuilding;
        public int BuildingCount => _session.Data.Buildings?.Count ?? 0;
        public int SelectedBuildingIndex => _session.SelectedBuildingIndex;
        public bool BuildingsSelectable => _buildingsSelectable;
        public bool CameraPanInteractionActive => _cameraPanInteractionActive;
        public float BuildingContextOpacity => _buildingContextOpacity;
        public float SelectedBuildingOpacity => _presentation?.Opacity ?? 0f;
        public string CurrentLotName => _session.Data.Name;
        public string CurrentEraId => _session.Data.EraId;
        public string CurrentLotId => _session.Data.LotId;
        public bool HasUnsavedChanges => _session.IsDirty;
        public bool IsSelected => _session.IsSelected;
        public LotObjectSelectionKind ActiveObjectSelection { get; private set; }
        public LotObjectSelectionKind HoverObjectKind { get; private set; }
        public int HoverObjectIndex => _hoverObjectIndex;
        public bool ObjectHoverVisible => _objectHoverHighlight != null &&
            _objectHoverHighlight.gameObject.activeSelf;
        public bool RoadCursorSelected { get; private set; }
        public bool CirculationCursorSelected { get; private set; }
        public int VehiclePresentationCount => _vehiclePresentations.Count;
        public int TestVehicleCount => _testVehicles.Count;
        public int FloraCount => _session.Data.Flora?.Count ?? 0;
        public int SelectedFloraIndex { get; private set; } = -1;

#if UNITY_EDITOR
        public bool PlaceFloraForQa(string floraId, float positionX,
            float positionZ)
        {
            if (!AddFlora(floraId, new Vector3(positionX, 0f, positionZ)))
                return false;
            RebuildFloraPresentations();
            NotifyStateChanged();
            return true;
        }

        public bool SetFloraPositionForQa(int index, float positionX, float positionZ)
        {
            if (index < 0 || index >= (_session.Data.Flora?.Count ?? 0))
                return false;
            _session.Data.Flora[index].PositionX = positionX;
            _session.Data.Flora[index].PositionZ = positionZ;
            RebuildFloraPresentations();
            NotifyStateChanged();
            return true;
        }
#endif
        public bool CanSpawnTestVehicle =>
            (_trafficGraph != null && _trafficGraph.LaneCount > 0) ||
            (_session.Data.VehicleNetwork?.Segments.Count ?? 0) > 0;
        public LotToolMode ToolMode => _session.ToolMode;
        public Vector2Int BuildingCell =>
            new(Mathf.RoundToInt(_session.Data.CellX),
                Mathf.RoundToInt(_session.Data.CellZ));
        public Vector2 BuildingPosition =>
            new(_session.Data.CellX, _session.Data.CellZ);
        public int BuildingRotationQuarterTurns =>
            _session.Data.RotationQuarterTurns;
        public int BuildingCardinalQuarterTurns =>
            _buildingPackage.WrapFacing(
                _buildingPackage.FrontFacingQuarterTurns +
                BuildingRotationQuarterTurns);
        public string BuildingCardinalOrientation =>
            LotEditorSession.CardinalOrientation(BuildingCardinalQuarterTurns);
        public bool SelectedBuildingFrontMarkerVisible =>
            _selectionFrontMarker != null &&
            _selectionFrontMarker.gameObject.activeInHierarchy;
        public Vector3 SelectedBuildingFrontDirection =>
            _selectionFrontMarker != null
                ? _selectionFrontMarker.forward
                : Vector3.zero;
        public string BuildingId => _session.Data.BuildingId;
        public HybridBuildingPackage BuildingPackage => _buildingPackage;
        public TimeOfDayPreset TimeOfDay { get; private set; } =
            TimeOfDayPreset.Noon;
        public SeasonPreset Season { get; private set; } = SeasonPreset.Summer;
        public bool IsRaining { get; private set; }
        public BuildingArtworkSource ArtworkSource { get; private set; } =
            BuildingArtworkSource.NeutralPilot;
        public bool NeutralPilotShowing =>
            _presentation != null && _presentation.NeutralPilotShowing;
        public bool NeutralPilotFallback =>
            _presentation != null && _presentation.PilotRequestedButUnavailable;
        public bool ProjectedShadowVisible =>
            _projectedShadow != null && _projectedShadow.gameObject.activeSelf;
        public Bounds ProjectedShadowBounds =>
            _projectedShadow == null ||
            _projectedShadow.GetComponent<MeshFilter>() == null ||
            _projectedShadow.GetComponent<MeshFilter>().sharedMesh == null
                ? new Bounds()
                : _projectedShadow.GetComponent<MeshFilter>().sharedMesh.bounds;
        public int ProjectedShadowVertexCount =>
            _projectedShadow == null ||
            _projectedShadow.GetComponent<MeshFilter>() == null ||
            _projectedShadow.GetComponent<MeshFilter>().sharedMesh == null
                ? 0
                : _projectedShadow.GetComponent<MeshFilter>().sharedMesh.vertexCount;
        public Vector2 ProjectedShadowLocalDirection { get; private set; }
        public float BuildingShadowDirectionOffsetDegrees =>
            _buildingPackage?.ShadowDirectionOffsetDegrees ?? 0f;
        public int ProjectedShadowSourceVertexCount { get; private set; }
        public event Action StateChanged;
        public LotZoomLevel ZoomLevel { get; private set; } = LotZoomLevel.Lot;
        public LotType LotType => _session.Data.LotType;
        public int LotSizeMeters => _session.Data.LotSizeMeters;
        public int LotWidthCells => _session.Data.LotWidthCells;
        public int LotDepthCells => _session.Data.LotDepthCells;
        public int LotWidthMeters => LotWidthCells * (int)LotMetricScale.MajorGridMeters;
        public int LotDepthMeters => LotDepthCells * (int)LotMetricScale.MajorGridMeters;
        public int LotMajorCellCount => Mathf.Max(LotWidthCells, LotDepthCells);
        public int LotMajorCellArea => LotWidthCells * LotDepthCells;
        public Vector3 CameraPanWorld => _cameraPanWorld;
        public CameraFramingState CaptureCameraFraming() => new(_camera);

        public void RestoreCameraFraming(CameraFramingState framing)
        {
            if (!framing.Valid || _camera == null) return;
            _camera.transform.SetPositionAndRotation(
                framing.Position, framing.Rotation);
            _camera.orthographicSize = framing.OrthographicSize;
            AlignFloraToCamera();
            UpdatePresentationDepthOrdering();
        }
        public LotTypeContract LotContract => LotTypeCatalog.For(LotType);
        public bool MinorGridVisible => _minorGrid != null && _minorGrid.gameObject.activeSelf;
        public bool MajorGridVisible => _majorGrid != null && _majorGrid.gameObject.activeSelf;
        public bool GridVisible => _gridVisible;
        public bool NeighborhoodRoadVisible => _neighborhoodRoad != null && _neighborhoodRoad.gameObject.activeSelf;
        public bool RoadArtworkVisible => _roadArtworkRoot != null && _roadArtworkRoot.gameObject.activeSelf;
        public RoadPiecePackage RoadPackage => _roadPackage;
        public bool RoadCursorHighlightVisible => _roadCursorFill != null &&
            _roadCursorFill.enabled && _roadCursor.gameObject.activeSelf;
        public bool HasSelectedRoadPiece => RoadCursorSelected &&
            RoadPlacementModel.FindAt(_session.Data.RoadPieces,
                RoadCursorCell.x, RoadCursorCell.y) != null;
        public bool SelectedRoadCanConnectOutside =>
            _roadPackage?.AllowsVehicles == true &&
            TrafficLotModel.TryGetExteriorPort(
                RoadPlacementModel.FindAt(_session.Data.RoadPieces,
                    RoadCursorCell.x, RoadCursorCell.y),
                _roadPackage, LotWidthMeters, LotDepthMeters, out _);
        public OutsideRoadConnector SelectedOutsideConnector =>
            _session.Data.OutsideRoadConnectors?.Find(connector => connector != null &&
                connector.GridX == RoadCursorCell.x && connector.GridZ == RoadCursorCell.y);
        public RoadConnectionPort PrimaryRoadPort => LotContract.RoadPorts.Count > 0 ? LotContract.RoadPorts[0] : null;
        public CirculationMode CirculationMode { get; private set; } = CirculationMode.Pedestrian;
        public Vector2 CirculationCursorMeters { get; private set; }
        public CirculationNetwork ActiveCirculationNetwork => CirculationMode == CirculationMode.Pedestrian
            ? _session.Data.PedestrianNetwork
            : _session.Data.VehicleNetwork;
        public int PedestrianNodeCount => _session.Data.PedestrianNetwork.Nodes.Count;
        public int PedestrianSegmentCount => _session.Data.PedestrianNetwork.Segments.Count;
        public int VehicleNodeCount => _session.Data.VehicleNetwork.Nodes.Count;
        public int VehicleSegmentCount => _session.Data.VehicleNetwork.Segments.Count;
        public int VehicleRoutePointCount =>
            _trafficGraph?.Routes.Count > 0 ? _trafficGraph.Routes[0].Points.Count : 0;
        public float VehicleRouteLengthMeters =>
            _trafficGraph?.Routes.Count > 0 ? _trafficGraph.Routes[0].TotalLengthMeters : 0f;
        public int VehicleLaneCount => _trafficGraph?.LaneCount ?? 0;
        public int VehicleDirectedSegmentCount => _trafficGraph?.DirectedSegmentCount ?? 0;
        public int TrafficIntersectionCount => _trafficGraph?.IntersectionCount ?? 0;
        public float AverageVehicleSpeedMetersPerSecond
        {
            get
            {
                if (_laneVehicleStates.Count == 0) return 0f;
                var total = 0f;
                foreach (var state in _laneVehicleStates) total += state.SpeedMetersPerSecond;
                return total / _laneVehicleStates.Count;
            }
        }
        public float MinimumVehicleGapMeters
        {
            get
            {
                var minimum = float.PositiveInfinity;
                foreach (var state in _laneVehicleStates)
                    minimum = Mathf.Min(minimum, state.GapAheadMeters);
                return minimum;
            }
        }
        public int BrakingVehicleCount =>
            _laneVehicleStates.FindAll(state => state.Braking).Count;
        public bool CirculationDiagnosticsVisible { get; private set; }

        public void Build()
        {
            _buildingPackage =
                HybridBuildingPackageRegistry.GovernmentHouse;
            BuildLighting();
            BuildGround();
            BuildLotTextureRoot();
            BuildGrid();
            BuildNeighborhoodRoadSlice();
            BuildRoadArtworkSlice();
            BuildFloraRoot();
            BuildPropRoot();
            BuildBuildingPropRoot();
            BuildCirculationEditor();
            BuildCamera();
            EnsurePropFrontRecoveryCamera();
            BuildBuildingPropOverlayPass();
            BuildProxyBuilding();
            BuildProjectedShadow();
            BuildHybridPresentation();
            BuildSelectionFootprint();
            BuildObjectHoverHighlight();
            BuildRegistrationDiagnostics();
            ApplySessionState();
        }

        private void BuildFloraRoot()
        {
            _floraRoot = new GameObject("Placed Flora").transform;
            _floraRoot.SetParent(transform, false);
            _floraSelection = new GameObject("Selected Flora Highlight").transform;
            _floraSelection.SetParent(transform, false);
            var material = LotSurfaceMaterial(new Color(1f, 0.72f, 0.12f, 0.9f), 2015);
            foreach (var edge in new[]
                     {
                         (new Vector3(0f, 0f, 0.8f), new Vector3(1.6f, 0.05f, 0.08f)),
                         (new Vector3(0f, 0f, -0.8f), new Vector3(1.6f, 0.05f, 0.08f)),
                         (new Vector3(0.8f, 0f, 0f), new Vector3(0.08f, 0.05f, 1.6f)),
                         (new Vector3(-0.8f, 0f, 0f), new Vector3(0.08f, 0.05f, 1.6f))
                     })
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.transform.SetParent(_floraSelection, false);
                marker.transform.localPosition = edge.Item1;
                marker.transform.localScale = edge.Item2;
                marker.GetComponent<Collider>().enabled = false;
                marker.GetComponent<Renderer>().sharedMaterial = material;
            }
            _floraSelection.gameObject.SetActive(false);
            var preview = new GameObject("Flora Placement Preview");
            preview.transform.SetParent(transform, false);
            _floraPreview = preview.AddComponent<SpriteRenderer>();
            _floraPreview.sortingOrder = 18;
            preview.SetActive(false);
        }

        public void SetFloraEditorContext(bool active)
        {
            _floraEditorActive = true;
            _floraPlacementActive = active && !_cameraPanInteractionActive;
            ApplyFloraSelection();
            if (_floraPreview != null)
                _floraPreview.gameObject.SetActive(
                    _floraPlacementActive && _floraPreviewHasPoint &&
                    !string.IsNullOrWhiteSpace(_floraPreviewId));
        }

        public void SetFloraPlacementPreview(string floraId)
        {
            _floraPreviewId = floraId ?? "";
            _floraPreviewHasPoint = false;
            if (_floraPreview == null) return;
            _floraPreview.sprite = LoadFloraSprite(_floraPreviewId);
            _floraPreview.color = FloraColorForTime(1f);
            _floraPreview.gameObject.SetActive(false);
        }

        public bool UpdateFloraPreviewFromPanel(
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_floraPlacementActive || string.IsNullOrWhiteSpace(_floraPreviewId) ||
                _floraPreview == null ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            var position = new Vector2(
                Mathf.Clamp(point.x, -LotWidthMeters * 0.5f, LotWidthMeters * 0.5f),
                Mathf.Clamp(point.z, -LotDepthMeters * 0.5f, LotDepthMeters * 0.5f));
            _floraPreview.transform.localPosition =
                new Vector3(position.x, 0.025f, position.y);
            _floraPreview.transform.rotation = _camera.transform.rotation;
            _floraPreview.color = CanPlaceFloraAt(position)
                ? FloraColorForTime(1f)
                : InvalidFloraPreviewColorForTime();
            _floraPreviewHasPoint = true;
            _floraPreview.gameObject.SetActive(true);
            _floraPreview.sortingOrder = DepthSortingOrder(
                new Vector3(position.x, 0f, position.y));
            return true;
        }

        public void ClearFloraSelectionAndPreview()
        {
            SelectedFloraIndex = -1;
            _floraDragActive = false;
            _floraPreviewId = "";
            _floraPreviewHasPoint = false;
            if (_floraPreview != null) _floraPreview.gameObject.SetActive(false);
            ApplyFloraSelection();
        }

        public bool BeginFloraDragFromPanel(string floraId,
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            var pixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            SelectedFloraIndex = FloraIndexAtCameraPixel(pixel);
            if (SelectedFloraIndex < 0)
            {
                if (string.IsNullOrWhiteSpace(floraId)) return false;
                if (!AddFlora(floraId,
                        new Vector3(point.x, point.y, point.z))) return false;
                SelectedFloraIndex = _session.Data.Flora.Count - 1;
                RebuildFloraPresentations();
            }
            SelectedPropIndex = -1;
            ApplyPropSelection();
            _session.Select(false);
            ActiveObjectSelection = LotObjectSelectionKind.Flora;
            var selected = _session.Data.Flora[SelectedFloraIndex];
            _floraDragOffset = new Vector2(
                selected.PositionX - point.x,
                selected.PositionZ - point.z);
            _floraDragActive = true;
            _floraPreviewHasPoint = false;
            if (_floraPreview != null) _floraPreview.gameObject.SetActive(false);
            ApplyFloraSelection();
            return true;
        }

        private bool AddFlora(string floraId, Vector3 point)
        {
            var halfWidth = LotWidthMeters * 0.5f;
            var halfDepth = LotDepthMeters * 0.5f;
            _session.Data.Flora ??= new List<PlacedFlora>();
            var position = new Vector2(
                Mathf.Clamp(point.x, -halfWidth, halfWidth),
                Mathf.Clamp(point.z, -halfDepth, halfDepth));
            if (!CanPlaceFloraAt(position)) return false;
            _session.Data.Flora.Add(new PlacedFlora
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                FloraId = floraId,
                PositionX = position.x,
                PositionZ = position.y
            });
            return true;
        }

        public bool DragFloraFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_floraDragActive || SelectedFloraIndex < 0 ||
                SelectedFloraIndex >= (_session.Data.Flora?.Count ?? 0) ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            var placed = _session.Data.Flora[SelectedFloraIndex];
            var target = new Vector2(
                Mathf.Clamp(point.x + _floraDragOffset.x,
                -LotWidthMeters * 0.5f, LotWidthMeters * 0.5f),
                Mathf.Clamp(point.z + _floraDragOffset.y,
                -LotDepthMeters * 0.5f, LotDepthMeters * 0.5f));
            if (!CanPlaceFloraAt(target)) return false;
            placed.PositionX = target.x;
            placed.PositionZ = target.y;
            if (SelectedFloraIndex < _floraPresentations.Count &&
                _floraPresentations[SelectedFloraIndex] != null)
                _floraPresentations[SelectedFloraIndex].transform.localPosition =
                    new Vector3(placed.PositionX, 0.02f, placed.PositionZ);
            ApplyFloraSelection();
            UpdatePresentationDepthOrdering();
            return true;
        }

        public bool CanPlaceFloraAt(Vector2 position)
        {
            // Placement and visibility are independent. An in-bounds ground
            // anchor may overlap a building footprint or be completely hidden;
            // depth rendering decides what is visible after the drop.
            return true;
        }

        public bool EndFloraDrag()
        {
            if (!_floraDragActive) return false;
            _floraDragActive = false;
            NotifyStateChanged();
            return true;
        }

        public bool DeleteSelectedFlora()
        {
            if (SelectedFloraIndex < 0 ||
                SelectedFloraIndex >= (_session.Data.Flora?.Count ?? 0))
                return false;
            _session.Data.Flora.RemoveAt(SelectedFloraIndex);
            SelectedFloraIndex = -1;
            _floraDragActive = false;
            ActiveObjectSelection = LotObjectSelectionKind.None;
            RebuildFloraPresentations();
            ApplyFloraSelection();
            NotifyStateChanged();
            return true;
        }

        private int FloraIndexAtCameraPixel(Vector2 pixel)
        {
            var best = -1;
            var bestDepth = float.PositiveInfinity;
            for (var index = 0; index < _floraPresentations.Count; index++)
            {
                var renderer = _floraPresentations[index];
                if (renderer == null || !renderer.enabled) continue;
                if (!SpriteRendererContainsCameraPixel(
                        renderer, _camera, pixel)) continue;
                var depth = _camera.WorldToScreenPoint(
                    renderer.transform.position).z;
                if (depth >= bestDepth) continue;
                bestDepth = depth;
                best = index;
            }
            return best;
        }

        public static bool SpriteRendererContainsCameraPixel(
            SpriteRenderer renderer, Camera camera, Vector2 pixel,
            float alphaThreshold = 0.02f)
        {
            if (renderer == null || renderer.sprite == null || camera == null)
                return false;
            var ray = camera.ScreenPointToRay(pixel);
            var plane = new Plane(renderer.transform.forward,
                renderer.transform.position);
            if (!plane.Raycast(ray, out var distance)) return false;
            var local = renderer.transform.InverseTransformPoint(
                ray.GetPoint(distance));
            var bounds = renderer.sprite.bounds;
            if (local.x < bounds.min.x || local.x > bounds.max.x ||
                local.y < bounds.min.y || local.y > bounds.max.y)
                return false;
            var normalized = new Vector2(
                Mathf.InverseLerp(bounds.min.x, bounds.max.x, local.x),
                Mathf.InverseLerp(bounds.min.y, bounds.max.y, local.y));
            if (renderer.flipX) normalized.x = 1f - normalized.x;
            if (renderer.flipY) normalized.y = 1f - normalized.y;
            var sprite = renderer.sprite;
            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var u = (textureRect.x + normalized.x * textureRect.width) /
                texture.width;
            var v = (textureRect.y + normalized.y * textureRect.height) /
                texture.height;
            return texture.GetPixelBilinear(u, v).a > alphaThreshold;
        }

        private void ApplyFloraSelection()
        {
            if (_floraSelection == null) return;
            var visible = _floraEditorActive &&
                ActiveObjectSelection == LotObjectSelectionKind.Flora &&
                SelectedFloraIndex >= 0 &&
                SelectedFloraIndex < (_session.Data.Flora?.Count ?? 0);
            _floraSelection.gameObject.SetActive(visible);
            if (!visible) return;
            var placed = _session.Data.Flora[SelectedFloraIndex];
            _floraSelection.localPosition =
                new Vector3(placed.PositionX, 0.06f, placed.PositionZ);
        }

        private void RebuildFloraPresentations()
        {
            if (_floraRoot == null) return;
            for (var index = _floraRoot.childCount - 1; index >= 0; index--)
            {
                var child = _floraRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            _floraPresentations.Clear();
            _floraCastShadows.Clear();
            foreach (var placed in _session.Data.Flora ?? new List<PlacedFlora>())
            {
                var variation = FloraVariationProfile(placed);
                var presentationId = ResolveFloraPresentationId(
                    placed.FloraId, variation, Season);
                var sprite = LoadFloraSprite(presentationId);
                if (sprite == null) continue;
                var root = new GameObject(
                    $"Flora — {placed.FloraId} — Variant {variation + 1}");
                root.layer = FloraShadowReceiverLayer;
                root.transform.SetParent(_floraRoot, false);
                root.transform.localPosition = new Vector3(
                    placed.PositionX, 0.02f, placed.PositionZ);
                var scale = FloraVariationScale(placed.FloraId, variation);
                root.transform.localScale = new Vector3(scale, scale, scale);
                if (_camera != null) root.transform.rotation = _camera.transform.rotation;
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.flipX = IsMirroredFloraVariation(
                    placed.FloraId, variation);
                renderer.color = FloraColorForTime(1f);
                renderer.sharedMaterial = FloraLitShadowReceiverMaterial();
                renderer.receiveShadows = true;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                _floraPresentations.Add(renderer);
                BuildFloraShadows(root.transform, sprite, renderer.flipX);
            }
            UpdateFloraShadows();
            ApplyFloraSelection();
            UpdatePresentationDepthOrdering();
        }

        private void BuildFloraShadows(Transform root, Sprite treeSprite,
            bool flipX)
        {
            var cast = new GameObject("Flora Shadow — Canopy");
            cast.transform.SetParent(root, false);
            var castRenderer = cast.AddComponent<SpriteRenderer>();
            castRenderer.sprite = treeSprite;
            castRenderer.flipX = flipX;
            _floraCastShadows.Add(castRenderer);
        }

        private static float FloraShadowOpacityMultiplier(Sprite sprite,
            SeasonPreset season, TimeOfDayPreset timeOfDay)
        {
            if (season != SeasonPreset.Winter || sprite?.texture == null)
                return 1f;

            // Leafless seasonal sprites contain substantially less opaque area
            // than the approved leafy canopy art. Strengthen only those exact
            // winter assets so their projected branch shadows remain legible;
            // summer artwork and seasonal fallbacks retain the native profile.
            if (!UsesLeaflessWinterFloraArt(sprite, season))
                return 1f;

            // Bare twigs need only restrained compensation. The former boost
            // pushed winter oaks close to opaque black, especially in morning
            // and afternoon, so keep them lighter than evergreen silhouettes.
            return 0.72f;
        }

        private static bool UsesRegisteredWinterFloraArt(Sprite sprite,
            SeasonPreset season) =>
            season == SeasonPreset.Winter && sprite?.texture != null &&
            sprite.texture.name.EndsWith("-winter",
                StringComparison.OrdinalIgnoreCase);

        private static bool UsesLeaflessWinterFloraArt(Sprite sprite,
            SeasonPreset season) =>
            UsesRegisteredWinterFloraArt(sprite, season) &&
            !sprite.texture.name.StartsWith("evergreen",
                StringComparison.OrdinalIgnoreCase);

        private static bool UsesSunRegisteredFloraShadow(Sprite sprite)
        {
            if (sprite?.texture == null) return false;
            var name = sprite.texture.name;
            // Maple and ashe retain their historical hand-authored shadow
            // contract. Every current and future exported tree defaults to the
            // shared world-sun projection, preventing species-specific angles.
            return !name.StartsWith("maple", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("ashe", StringComparison.OrdinalIgnoreCase);
        }

        private static Vector2 FloraCastOffset(Sprite sprite,
            SeasonPreset season, FloraShadowProfile profile)
        {
            // Seasonal leafless sprites are authored with a bottom-centre
            // pivot at the foot of the trunk. Keep that registered point at
            // the flora root so the projected branches begin at the trunk,
            // instead of inheriting the old leafy-canopy screen offset.
            return UsesSunRegisteredFloraShadow(sprite)
                ? Vector2.zero
                : profile.CastOffset;
        }

        private float FloraCastRotation(Sprite sprite,
            SeasonPreset season, TimeOfDayPreset timeOfDay,
            FloraShadowProfile profile)
        {
            if (!UsesSunRegisteredFloraShadow(sprite) || _camera == null ||
                timeOfDay is not (TimeOfDayPreset.Morning or
                    TimeOfDayPreset.Afternoon))
                return profile.CastRotation;

            // Derive the billboard-plane angle from the same world-space sun
            // ray used by building projected shadows. This avoids guessing a
            // sprite-local compass angle: package compass registration, camera
            // facing, morning, and afternoon all resolve through one contract.
            var directionOffset = _buildingPackage != null
                ? _buildingPackage.ShadowDirectionOffsetDegrees
                : 0f;
            var ray = Quaternion.Euler(0f, directionOffset, 0f) *
                (TimeOfDayLighting.SunRotation(timeOfDay) * Vector3.forward);
            var horizontal = new Vector3(ray.x, 0f, ray.z).normalized;
            var screenDirection = new Vector2(
                Vector3.Dot(horizontal, _camera.transform.right),
                Vector3.Dot(horizontal, _camera.transform.up));
            if (screenDirection.sqrMagnitude <= 0.000001f)
                return profile.CastRotation;

            // A tree sprite grows along local +Y from its bottom-centre pivot.
            // Rotate that axis onto the projected world shadow direction.
            return Mathf.Atan2(-screenDirection.x, screenDirection.y) *
                Mathf.Rad2Deg;
        }

        private void UpdateFloraShadows()
        {
            var profile = FloraShadowProfileForTime(TimeOfDay);
            for (var index = 0; index < _floraPresentations.Count; index++)
            {
                if (index < _floraCastShadows.Count && _floraCastShadows[index] != null)
                {
                    var shadow = _floraCastShadows[index];
                    shadow.enabled = !IsRaining;
                    var castOffset = FloraCastOffset(shadow.sprite, Season,
                        profile);
                    shadow.transform.localPosition = new Vector3(
                        castOffset.x, castOffset.y, 0.01f);
                    shadow.transform.localRotation =
                        Quaternion.Euler(0f, 0f, FloraCastRotation(
                            shadow.sprite, Season, TimeOfDay, profile));
                    // Legacy profile scales were authored as final screen-space
                    // dimensions. A SpriteRenderer child interprets them as
                    // multipliers, so normalize by the tree sprite's world size.
                    var bounds = shadow.sprite.bounds.size;
                    var registeredShadow = UsesSunRegisteredFloraShadow(
                        shadow.sprite);
                    // The registered winter sprite grows from the trunk along
                    // local Y, so Y is shadow length and X is canopy width.
                    // Legacy leafy art retains its approved scale convention.
                    var castWidth = registeredShadow
                        ? profile.CastScale.y
                        : profile.CastScale.x;
                    var castLength = registeredShadow
                        ? profile.CastScale.x
                        : profile.CastScale.y;
                    shadow.transform.localScale = new Vector3(
                        castWidth / Mathf.Max(0.01f, bounds.x),
                        castLength / Mathf.Max(0.01f, bounds.y), 1f);
                    shadow.color = new Color(0f, 0f, 0f, Mathf.Clamp01(
                        profile.CastOpacity *
                        FloraShadowOpacityMultiplier(
                            shadow.sprite, Season, TimeOfDay)));
                }
            }
        }

        private readonly struct FloraShadowProfile
        {
            public readonly Vector2 BaseOffset, BaseScale, CastOffset, CastScale;
            public readonly float BaseOpacity, CastRotation, CastOpacity;
            public FloraShadowProfile(Vector2 baseOffset, Vector2 baseScale,
                float baseOpacity, Vector2 castOffset, Vector2 castScale,
                float castRotation, float castOpacity)
            {
                BaseOffset = baseOffset; BaseScale = baseScale;
                BaseOpacity = baseOpacity; CastOffset = castOffset;
                CastScale = castScale; CastRotation = castRotation;
                CastOpacity = castOpacity;
            }
        }

        private static FloraShadowProfile FloraShadowProfileForTime(
            TimeOfDayPreset preset) => preset switch
        {
            TimeOfDayPreset.Morning => new(new(-0.06f, -0.12f), new(1.15f, 0.36f),
                0.056f, new(-1.45f, -0.34f), new(5.375f, 1.675f), 22f, 0.14f),
            TimeOfDayPreset.Noon => new(new(0f, -0.10f), new(1f, 0.30f),
                0.05f, new(0.08f, -0.22f), new(2f, 0.82f), 0f, 0.18f),
            TimeOfDayPreset.Afternoon => new(new(0f, -0.12f), new(1.55f, 0.46f),
                0.077f, new(0.12f, -0.70f), new(8.1f, 2.43f), -26f, 0.315f),
            TimeOfDayPreset.Evening => new(new(0.04f, -0.16f), new(1.8f, 0.56f),
                0.14f, new(0.82f, -0.92f), new(7.8f, 2.05f), -33f, 0.54f),
            _ => new(new(0f, -0.08f), new(0.8f, 0.22f),
                0.01f, new(0.10f, -0.14f), new(0.95f, 0.42f), -4f, 0.02f)
        };

        public static string ResolveFloraResourcePath(string floraId,
            SeasonPreset season)
        {
            if (string.IsNullOrWhiteSpace(floraId)) return null;
            var root = $"CityForgeV3/Flora/LegacyTreesV01/{floraId}";
            var seasonalPath = $"{root}-{season.ToString().ToLowerInvariant()}";
            return Resources.Load<Texture2D>(seasonalPath) != null
                ? seasonalPath
                : $"{root}-summer";
        }

        public static int StableFloraVariationProfile(string seed)
        {
            // FNV-1a is explicit and stable across runtimes. String.GetHashCode
            // is not a persistence contract and could reshuffle saved lots.
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in seed ?? "")
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
                return (int)(hash & 3u);
            }
        }

        public static string ResolveFloraPresentationId(string floraId,
            int variation, SeasonPreset season = SeasonPreset.Summer)
        {
            if (string.Equals(floraId, "evergreen",
                    StringComparison.OrdinalIgnoreCase))
            {
                var alternate = (variation & 1) != 0;
                // Winter evergreens always carry visible snow. Variation still
                // selects a different 3D viewing angle, so groves do not repeat
                // one identical silhouette.
                var snowy = season == SeasonPreset.Winter;
                if (snowy) return alternate
                    ? "evergreen-b-snow"
                    : "evergreen-snow";
                return alternate ? "evergreen-b" : "evergreen";
            }
            if (!string.Equals(floraId, "oak",
                    StringComparison.OrdinalIgnoreCase))
                return floraId;
            return (variation & 2) == 0 ? "oak" : "oak-b";
        }

        private static int FloraVariationProfile(PlacedFlora placed)
        {
            if (placed == null) return 0;
            var seed = placed.InstanceId;
            if (string.IsNullOrWhiteSpace(seed))
                seed = $"{placed.FloraId}:{Mathf.RoundToInt(placed.PositionX * 100f)}:" +
                       Mathf.RoundToInt(placed.PositionZ * 100f);
            return StableFloraVariationProfile(seed);
        }

        private static bool IsMirroredFloraVariation(string floraId,
            int variation) =>
            (string.Equals(floraId, "oak", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(floraId, "evergreen", StringComparison.OrdinalIgnoreCase)) &&
            !string.Equals(floraId, "evergreen", StringComparison.OrdinalIgnoreCase) &&
            (variation & 1) != 0;

        private static float FloraVariationScale(string floraId, int variation)
        {
            var supportsVariation = string.Equals(floraId, "oak",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(floraId, "evergreen",
                    StringComparison.OrdinalIgnoreCase);
            if (!supportsVariation) return 1f;
            return variation switch
            {
                0 => 1.02f,
                1 => 0.96f,
                2 => 1.00f,
                _ => 0.94f
            };
        }

        private Sprite LoadFloraSprite(string floraId)
        {
            if (string.IsNullOrWhiteSpace(floraId)) return null;
            var texture = Resources.Load<Texture2D>(
                ResolveFloraResourcePath(floraId, Season));
            return texture == null ? null : Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0f), 32f);
        }

        private Material FloraLitShadowReceiverMaterial()
        {
            if (_floraLitShadowReceiverMaterial != null)
                return _floraLitShadowReceiverMaterial;
            var shader = Shader.Find("CityForgeV3/LitShadowReceivingSprite");
            if (shader == null)
                throw new MissingReferenceException(
                    "CityForge V3 lit flora shadow receiver shader is required.");
            _floraLitShadowReceiverMaterial = new Material(shader)
            {
                name = "CF Native Lit Flora Shadow Receiver",
                // Draw after the separate projected canopy shadow. This keeps
                // that ground shadow from darkening the billboard itself.
                renderQueue = 3001
            };
            _floraLitShadowReceiverMaterial.SetFloat("_Cutoff", 0.02f);
            _floraLitShadowReceiverMaterial.SetFloat("_ShadowFloor", 0.38f);
            _floraLitShadowReceiverMaterial.SetFloat("_ZTest",
                (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            return _floraLitShadowReceiverMaterial;
        }

        private Material FloraHostFrontRecoveryMaterial(
            IReadOnlyList<int> hostStencilReferences)
        {
            var count = hostStencilReferences?.Count ?? 0;
            var reference1 = count > 0 ? hostStencilReferences[0] : 0;
            var reference2 = count > 1 ? hostStencilReferences[1] : 0;
            var reference3 = count > 2 ? hostStencilReferences[2] : 0;
            var reference4 = count > 3 ? hostStencilReferences[3] : 0;
            var materialKey = reference1 |
                reference2 << 8 |
                reference3 << 16 |
                reference4 << 24;
            if (_floraHostFrontRecoveryMaterials.TryGetValue(
                    materialKey, out var material))
                return material;
            var shader = Shader.Find(
                "CityForgeV3/FrontFacadeLitShadowReceivingSprite");
            if (shader == null)
                throw new MissingReferenceException(
                    "CityForge V3 host-facade flora shader is required.");
            material = new Material(shader)
            {
                name = $"CF Native Lit Flora Hosts {reference1}-{reference2}-" +
                    $"{reference3}-{reference4} Recovery",
                renderQueue = 3001
            };
            material.SetFloat("_Cutoff", 0.02f);
            material.SetFloat("_ShadowFloor", 0.38f);
            material.SetFloat("_ZTest",
                (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            material.SetFloat("_BuildingHostStencilRef", reference1);
            material.SetFloat("_BuildingHostStencilRef2", reference2);
            material.SetFloat("_BuildingHostStencilRef3", reference3);
            material.SetFloat("_BuildingHostStencilRef4", reference4);
            _floraHostFrontRecoveryMaterials.Add(materialKey, material);
            return material;
        }

        public static bool TryBuildingOcclusionStencilReference(
            int buildingIndex, out int stencilReference)
        {
            if (buildingIndex < 0 ||
                buildingIndex >= BuildingOcclusionStencilRecoverableSlots)
            {
                stencilReference = 0;
                return false;
            }
            stencilReference = (buildingIndex + 1) <<
                BuildingOcclusionStencilShift;
            return true;
        }

        public static int BuildingDepthOcclusionStencilReference(
            int buildingIndex)
        {
            if (buildingIndex < 0) return 0;
            return TryBuildingOcclusionStencilReference(
                buildingIndex, out var reference)
                ? reference
                : BuildingOcclusionStencilOverflowReference;
        }

        private static void ConfigureBuildingDepthStencil(
            Material material, int buildingIndex)
        {
            if (material == null) return;
            material.SetFloat("_BuildingHostStencilRef",
                BuildingDepthOcclusionStencilReference(buildingIndex));
            material.SetFloat("_BuildingHostStencilWriteMask", 252f);
        }

        private static void ConfigureBuildingDepthStencil(
            IEnumerable<Renderer> renderers, int buildingIndex)
        {
            if (renderers == null) return;
            foreach (var renderer in renderers)
                ConfigureBuildingDepthStencil(
                    renderer != null ? renderer.sharedMaterial : null,
                    buildingIndex);
        }

        public static Vector3 AuthoredBuildingFrontDirection(
            HybridBuildingPackage package,
            int placedRotationQuarterTurns)
        {
            if (package == null) return Vector3.forward;
            var radians = package.EntranceFacingDegrees * Mathf.Deg2Rad;
            var packageFront = new Vector3(
                Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            return Quaternion.Euler(
                0f, placedRotationQuarterTurns * 90f, 0f) * packageFront;
        }

        private static bool TryGetVisibleBuildingFrontApronMetrics(
            Vector3 localPosition,
            Vector3 buildingPosition,
            HybridBuildingPackage package,
            int placedRotationQuarterTurns,
            Vector3 localTowardCameraDirection,
            out float absoluteFrontPlaneDistance,
            out float absoluteLateralDistance)
        {
            absoluteFrontPlaneDistance = float.PositiveInfinity;
            absoluteLateralDistance = float.PositiveInfinity;
            if (package == null) return false;
            var rotation = Quaternion.Euler(
                0f, placedRotationQuarterTurns * 90f, 0f);
            var relative = Quaternion.Inverse(rotation) *
                (localPosition - buildingPosition);
            var cameraDirection = localTowardCameraDirection;
            cameraDirection.y = 0f;
            if (cameraDirection.sqrMagnitude <= 0.0001f) return false;
            cameraDirection.Normalize();
            var front = Quaternion.Inverse(rotation) * cameraDirection;
            front.y = 0f;
            front.Normalize();
            var lateral = new Vector3(front.z, 0f, -front.x);
            var halfWidth = package.WidthMeters * 0.5f;
            var halfDepth = package.DepthMeters * 0.5f;
            var lateralOffset = Mathf.Abs(Vector3.Dot(relative, lateral));

            // A shallow interior band absorbs small registration differences
            // between the visible facade/sidewalk seam and its semantic
            // footprint. Deeper anchors remain physically hidden. For an
            // exterior anchor, classify the closest point on the footprint.
            // If the vector from that surface point to the anchor points
            // toward the camera, this building is behind the anchor. This
            // handles billboard width beside a facade: the trunk's infinitely
            // thin ground ray can miss the footprint even while the canopy is
            // visibly covered by that building. The host stencil still limits
            // recovery to pixels actually occluded by this one building.
            if (Mathf.Abs(relative.x) <= halfWidth &&
                Mathf.Abs(relative.z) <= halfDepth)
            {
                var distanceToCameraFacingSurface = float.PositiveInfinity;
                if (front.x > BuildingCameraFrontToleranceMeters)
                    distanceToCameraFacingSurface = Mathf.Min(
                        distanceToCameraFacingSurface,
                        (halfWidth - relative.x) / front.x);
                else if (front.x < -BuildingCameraFrontToleranceMeters)
                    distanceToCameraFacingSurface = Mathf.Min(
                        distanceToCameraFacingSurface,
                        (-halfWidth - relative.x) / front.x);
                if (front.z > BuildingCameraFrontToleranceMeters)
                    distanceToCameraFacingSurface = Mathf.Min(
                        distanceToCameraFacingSurface,
                        (halfDepth - relative.z) / front.z);
                else if (front.z < -BuildingCameraFrontToleranceMeters)
                    distanceToCameraFacingSurface = Mathf.Min(
                        distanceToCameraFacingSurface,
                        (-halfDepth - relative.z) / front.z);

                if (distanceToCameraFacingSurface >
                    BuildingCameraFrontInteriorRecoveryMeters)
                    return false;
                absoluteFrontPlaneDistance = distanceToCameraFacingSurface;
                absoluteLateralDistance = lateralOffset;
                return true;
            }

            var closestSurfacePoint = new Vector2(
                Mathf.Clamp(relative.x, -halfWidth, halfWidth),
                Mathf.Clamp(relative.z, -halfDepth, halfDepth));
            var surfaceToAnchor =
                new Vector2(relative.x, relative.z) - closestSurfacePoint;
            var cameraSideDistance = Vector2.Dot(
                surfaceToAnchor, new Vector2(front.x, front.z));
            if (cameraSideDistance <= BuildingCameraFrontToleranceMeters)
                return false;

            absoluteFrontPlaneDistance = surfaceToAnchor.magnitude;
            absoluteLateralDistance = lateralOffset;
            return true;
        }

        public static bool IsInVisibleBuildingFrontApron(
            Vector3 localPosition,
            Vector3 buildingPosition,
            HybridBuildingPackage package,
            int placedRotationQuarterTurns,
            Vector3 localTowardCameraDirection) =>
            TryGetVisibleBuildingFrontApronMetrics(
                localPosition, buildingPosition, package,
                placedRotationQuarterTurns, localTowardCameraDirection,
                out _, out _);

        private bool TryResolveVisibleBuildingFrontHosts(
            Vector3 localPosition, List<int> hostStencilReferences)
        {
            hostStencilReferences.Clear();
            _floraFrontHostCandidates.Clear();
            if (_camera == null) return false;
            // Orthographic projection uses one parallel view ray everywhere.
            // Deriving a direction from each building to the camera position
            // incorrectly changes facade eligibility across a wide lot.
            var localTowardCameraDirection =
                transform.InverseTransformDirection(-_camera.transform.forward);
            if (localTowardCameraDirection.sqrMagnitude <= 0.0001f)
                return false;
            localTowardCameraDirection.Normalize();
            var buildings = _session.Data.Buildings ?? new List<PlacedBuilding>();
            for (var index = 0; index < buildings.Count; index++)
            {
                if (!TryBuildingOcclusionStencilReference(
                        index, out var stencilReference))
                    continue;
                var building = buildings[index];
                var catalogEntry = BuildingCatalog.Find(building.BuildingId);
                var package = HybridBuildingPackageRegistry.Load(
                    catalogEntry.PackageResourcePath);
                var buildingPosition = new Vector3(
                    building.CellX, 0f, building.CellZ);
                if (!TryGetVisibleBuildingFrontApronMetrics(
                        localPosition, buildingPosition, package,
                        building.RotationQuarterTurns,
                        localTowardCameraDirection,
                        out var frontDistance,
                        out var lateralDistance))
                    continue;
                _floraFrontHostCandidates.Add(
                    (frontDistance, lateralDistance, stencilReference));
            }
            _floraFrontHostCandidates.Sort((first, second) =>
            {
                var distance = first.distance.CompareTo(second.distance);
                if (distance != 0) return distance;
                var lateral = first.lateral.CompareTo(second.lateral);
                if (lateral != 0) return lateral;
                return first.stencil.CompareTo(second.stencil);
            });
            for (var index = 0;
                 index < Mathf.Min(4, _floraFrontHostCandidates.Count);
                 index++)
                hostStencilReferences.Add(
                    _floraFrontHostCandidates[index].stencil);
            return hostStencilReferences.Count > 0;
        }

        private Color FloraColorForTime(float alpha)
        {
            var tint = TimeOfDayLighting.For(TimeOfDay).NeutralArtworkTint;
            tint = SeasonLighting.Multiply(
                tint, SeasonLighting.FloraTint(Season));
            return new Color(tint.r, tint.g, tint.b, alpha);
        }

        private Color InvalidFloraPreviewColorForTime()
        {
            var tint = TimeOfDayLighting.For(TimeOfDay).NeutralArtworkTint;
            return new Color(tint.r, tint.g * 0.28f, tint.b * 0.20f, 0.42f);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Rotate(int direction)
        {
            _facing = _buildingPackage.WrapFacing(_facing + direction);
            ApplyCameraFacing(true);
            NotifyStateChanged();
        }

        public void SetInspectionMode(BuildingInspectionMode mode)
        {
            if (TopDownViewEnabled)
            {
                _inspectionModeBeforeTopDown = mode;
                InspectionMode = BuildingInspectionMode.Primitive;
            }
            else
            {
                InspectionMode = mode;
            }
            ApplySessionState();
            NotifyStateChanged();
        }

        public void ToggleTopDownView()
        {
            if (!TopDownViewEnabled && _camera != null)
            {
                var projectedWorldZ = new Vector2(
                    Vector3.Dot(Vector3.forward, _camera.transform.right),
                    Vector3.Dot(Vector3.forward, _camera.transform.up));
                if (projectedWorldZ.sqrMagnitude > 0.0001f)
                    _topDownWorldZScreenDirection = projectedWorldZ.normalized;
            }
            TopDownViewEnabled = !TopDownViewEnabled;
            if (TopDownViewEnabled)
            {
                _inspectionModeBeforeTopDown = InspectionMode;
                InspectionMode = BuildingInspectionMode.Primitive;
            }
            else
            {
                InspectionMode = _inspectionModeBeforeTopDown;
            }
            ApplySessionState();
            ApplyCameraFacing(false);
            NotifyStateChanged();
        }

        public void ToggleRegistrationDiagnostics()
        {
            RegistrationDiagnosticsVisible = !RegistrationDiagnosticsVisible;
            ApplySessionState();
            NotifyStateChanged();
        }

        public void SetTimeOfDay(TimeOfDayPreset preset)
        {
            var timeChanged = TimeOfDay != preset;
            TimeOfDay = preset;
            if (timeChanged)
                ClearRoadWetness();
            ApplyTimeOfDay();
            NotifyStateChanged();
        }

        public void SetSeason(SeasonPreset preset)
        {
            if (preset != SeasonPreset.Winter)
                ClearWinterSnow();
            Season = preset;
            if (_groundRenderer != null)
                ApplyBaseTexturePresentation();
            else
                ApplyTimeOfDay();
            RebuildFloraPresentations();
            if (_floraPreview != null &&
                !string.IsNullOrWhiteSpace(_floraPreviewId))
                _floraPreview.sprite = LoadFloraSprite(_floraPreviewId);
            NotifyStateChanged();
        }

        public void SetArtworkSource(BuildingArtworkSource source)
        {
            ArtworkSource = source;
            ApplyPresentationAppearance();
            NotifyStateChanged();
        }

        public void SetTool(LotToolMode mode)
        {
            _session.SetTool(mode);
            ApplySessionState();
            NotifyStateChanged();
        }

        public void SetLotType(LotType lotType)
        {
            var promoteCompactDemo = lotType == LotType.Neighborhood &&
                IsCompactSeedRoadLoop();
            _session.SetLotType(lotType);
            if (promoteCompactDemo)
            {
                _session.Data.RoadPieces.Clear();
                SetLotSizeMeters(80);
                SeedRoadVerticalSlice();
                RebuildRoadArtwork();
                RebuildRoadVehicleNetwork();
                ApplyRoadCursor();
            }
            ApplyLotPlanningState();
            NotifyStateChanged();
        }

        public void SetTrafficType(TrafficLotType trafficType)
        {
            _session.SetTrafficType(trafficType);
            ResetSuburbanTraffic();
            NotifyStateChanged();
        }

        public bool SetSelectedOutsideConnector(RoadTrafficFlow flow)
        {
            var piece = RoadPlacementModel.FindAt(_session.Data.RoadPieces,
                RoadCursorCell.x, RoadCursorCell.y);
            if (!TrafficLotModel.TryGetExteriorPort(piece, _roadPackage,
                    LotWidthMeters, LotDepthMeters, out var edge)) return false;
            _session.Data.OutsideRoadConnectors ??= new List<OutsideRoadConnector>();
            var connector = SelectedOutsideConnector;
            if (connector == null)
            {
                connector = new OutsideRoadConnector
                {
                    Id = Guid.NewGuid().ToString("N"),
                    GridX = RoadCursorCell.x,
                    GridZ = RoadCursorCell.y,
                    Edge = edge
                };
                _session.Data.OutsideRoadConnectors.Add(connector);
            }
            connector.Edge = edge;
            connector.Flow = flow;
            RebuildOutsideConnectorMarkers();
            RebuildRoadVehicleNetwork();
            NotifyStateChanged();
            return true;
        }

        public bool RemoveSelectedOutsideConnector()
        {
            if (_session.Data.OutsideRoadConnectors == null) return false;
            var removed = _session.Data.OutsideRoadConnectors.RemoveAll(connector =>
                connector != null && connector.GridX == RoadCursorCell.x &&
                connector.GridZ == RoadCursorCell.y) > 0;
            if (!removed) return false;
            RebuildOutsideConnectorMarkers();
            RebuildRoadVehicleNetwork();
            NotifyStateChanged();
            return true;
        }

        private bool IsCompactSeedRoadLoop()
        {
            if (LotSizeMeters != 20 || _session.Data.RoadPieces.Count != 4) return false;
            foreach (var piece in _session.Data.RoadPieces)
                if (piece == null || piece.Topology != RoadPieceTopology.Corner ||
                    piece.GridX < -1 || piece.GridX > 0 ||
                    piece.GridZ < -1 || piece.GridZ > 0) return false;
            return true;
        }

        public void SetLotSizeMeters(int meters)
        {
            var cells = Mathf.Clamp(Mathf.RoundToInt(meters / 10f), 1, 8);
            SetLotDimensions(cells, cells);
        }

        public void ConfigureLot(string name, LotType lotType,
            int widthCells, int depthCells, string eraId = LotEraCatalog.DefaultId)
        {
            _session.Rename(name);
            _session.SetLotType(lotType);
            _session.SetEra(eraId);
            SetLotDimensions(widthCells, depthCells);
        }

        public void SetLotDimensions(int widthCells, int depthCells)
        {
            _session.SetLotDimensions(widthCells, depthCells);
            if (HasBuilding) MoveBuildingTo(
                Mathf.RoundToInt(_session.Data.CellX),
                Mathf.RoundToInt(_session.Data.CellZ));
            ResizeGround();
            ApplyBaseTexturePresentation();
            BuildGrid();
            ApplyCameraFacing();
            ClampRoadCursorToLot();
            _session.Data.RoadPieces.RemoveAll(piece => piece == null ||
                piece.GridX < RoadPlacementModel.MinimumCellForLot(LotWidthMeters) ||
                piece.GridX > RoadPlacementModel.MaximumCellForLot(LotWidthMeters) ||
                piece.GridZ < RoadPlacementModel.MinimumCellForLot(LotDepthMeters) ||
                piece.GridZ > RoadPlacementModel.MaximumCellForLot(LotDepthMeters));
            RebuildRoadArtwork();
            RebuildRoadVehicleNetwork();
            ApplyRoadCursor();
            NotifyStateChanged();
        }

        public bool TryMajorCellFromPanel(Vector2 panelPosition,
            Vector2 panelSize, out Vector2Int cell)
        {
            cell = default;
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point) ||
                point.x < -LotWidthMeters * 0.5f ||
                point.x >= LotWidthMeters * 0.5f ||
                point.z < -LotDepthMeters * 0.5f ||
                point.z >= LotDepthMeters * 0.5f) return false;
            cell = new Vector2Int(
                Mathf.FloorToInt((point.x + LotWidthMeters * 0.5f) / 10f),
                Mathf.FloorToInt((point.z + LotDepthMeters * 0.5f) / 10f));
            return true;
        }

        public bool DeleteMajorColumn(int columnIndex) =>
            DeleteMajorStrip(columnIndex, true);

        public bool DeleteMajorRow(int rowIndex) =>
            DeleteMajorStrip(rowIndex, false);

        public bool ShowMajorStripDeletionPreview(int stripIndex, bool column)
        {
            var cellCount = column ? LotWidthCells : LotDepthCells;
            if (stripIndex < 0 || stripIndex >= cellCount) return false;
            if (_majorStripDeletionPreview == null)
            {
                var preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                preview.name = "Major Row Column Deletion Preview";
                preview.transform.SetParent(transform, false);
                var collider = preview.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying) Destroy(collider);
                    else DestroyImmediate(collider);
                }
                preview.GetComponent<Renderer>().sharedMaterial =
                    LotSurfaceMaterial(new Color(0.9f, 0.06f, 0.04f, 0.15f), 5001);
                _majorStripDeletionPreview = preview.transform;
            }
            var center = -cellCount * 5f + stripIndex * 10f + 5f;
            _majorStripDeletionPreview.localPosition = column
                ? new Vector3(center, 0.08f, 0f)
                : new Vector3(0f, 0.08f, center);
            _majorStripDeletionPreview.localScale = column
                ? new Vector3(10f, 0.04f, LotDepthMeters)
                : new Vector3(LotWidthMeters, 0.04f, 10f);
            _majorStripDeletionPreview.gameObject.SetActive(true);
            return true;
        }

        public void ClearMajorStripDeletionPreview()
        {
            if (_majorStripDeletionPreview != null)
                _majorStripDeletionPreview.gameObject.SetActive(false);
        }

        private bool DeleteMajorStrip(int stripIndex, bool alongX)
        {
            ClearMajorStripDeletionPreview();
            var selectedBuildingInstanceId =
                _session.SelectedBuildingIndex >= 0 &&
                _session.SelectedBuildingIndex < (_session.Data.Buildings?.Count ?? 0)
                    ? _session.Data.Buildings[_session.SelectedBuildingIndex]?.InstanceId
                    : null;
            var oldCellCount = alongX ? LotWidthCells : LotDepthCells;
            if (oldCellCount <= 1 || stripIndex < 0 || stripIndex >= oldCellCount)
                return false;
            var oldMeters = oldCellCount * 10f;
            var stripStart = -oldMeters * 0.5f + stripIndex * 10f;
            var stripEnd = stripStart + 10f;

            bool InStrip(float value) => value >= stripStart && value < stripEnd;
            float Shift(float value) => value < stripStart ? value + 5f : value - 5f;

            _session.Data.Buildings.RemoveAll(building =>
            {
                if (building == null) return true;
                var entry = BuildingCatalog.Find(building.BuildingId);
                var package = string.IsNullOrWhiteSpace(entry.PackageResourcePath)
                    ? null
                    : HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
                var width = package?.WidthMeters ?? 1f;
                var depth = package?.DepthMeters ?? 1f;
                if (Mathf.Abs(building.RotationQuarterTurns) % 2 == 1)
                    (width, depth) = (depth, width);
                var center = alongX ? building.CellX : building.CellZ;
                var extent = (alongX ? width : depth) * 0.5f;
                if (center + extent > stripStart && center - extent < stripEnd)
                    return true;
                if (alongX) building.CellX = Mathf.RoundToInt(Shift(building.CellX));
                else building.CellZ = Mathf.RoundToInt(Shift(building.CellZ));
                return false;
            });

            ShiftPointPlacements(_session.Data.Flora, alongX, InStrip, Shift);
            ShiftPointPlacements(_session.Data.Props, alongX, InStrip, Shift);
            ShiftCirculationNetwork(_session.Data.PedestrianNetwork, alongX,
                InStrip, Shift);
            ShiftCirculationNetwork(_session.Data.VehicleNetwork, alongX,
                InStrip, Shift);

            _session.Data.RoadPieces ??= new List<PlacedRoadPiece>();
            _session.Data.OutsideRoadConnectors ??=
                new List<OutsideRoadConnector>();
            _session.Data.OverlayTextures ??= new List<PlacedOverlayTexture>();
            var oldMinimum = RoadPlacementModel.MinimumCellForLot(
                Mathf.RoundToInt(oldMeters));
            var newMeters = Mathf.RoundToInt(oldMeters - 10f);
            var newMinimum = RoadPlacementModel.MinimumCellForLot(newMeters);
            int RemapCell(int value)
            {
                var zeroBased = value - oldMinimum;
                if (zeroBased > stripIndex) zeroBased--;
                return newMinimum + zeroBased;
            }
            _session.Data.RoadPieces.RemoveAll(piece => piece == null ||
                (alongX ? piece.GridX : piece.GridZ) - oldMinimum == stripIndex);
            foreach (var piece in _session.Data.RoadPieces)
            {
                if (alongX) piece.GridX = RemapCell(piece.GridX);
                else piece.GridZ = RemapCell(piece.GridZ);
            }
            _session.Data.OutsideRoadConnectors.RemoveAll(connector => connector == null ||
                (alongX ? connector.GridX : connector.GridZ) - oldMinimum == stripIndex);
            foreach (var connector in _session.Data.OutsideRoadConnectors)
            {
                if (alongX) connector.GridX = RemapCell(connector.GridX);
                else connector.GridZ = RemapCell(connector.GridZ);
            }
            _session.Data.OverlayTextures.RemoveAll(overlay => overlay == null ||
                (alongX ? overlay.CellX : overlay.CellZ) == stripIndex);
            foreach (var overlay in _session.Data.OverlayTextures)
            {
                if (alongX && overlay.CellX > stripIndex) overlay.CellX--;
                if (!alongX && overlay.CellZ > stripIndex) overlay.CellZ--;
            }

            var survivingBuildingIndex = -1;
            if (_session.Data.Buildings != null &&
                _session.Data.Buildings.Count > 0)
            {
                if (!string.IsNullOrEmpty(selectedBuildingInstanceId))
                {
                    survivingBuildingIndex = _session.Data.Buildings.FindIndex(
                        building => building != null &&
                            building.InstanceId == selectedBuildingInstanceId);
                }
                if (survivingBuildingIndex < 0)
                    survivingBuildingIndex = 0;
            }
            _session.SelectBuilding(survivingBuildingIndex);
            _session.Select(false);
            ActiveObjectSelection = LotObjectSelectionKind.None;
            ClearObjectHover();
            SelectedFloraIndex = -1;
            SelectedPropIndex = -1;
            _selectedBuildingPropPresentationIndex = -1;
            _selectedBuildingPropBuildingIndex = -1;
            _selectedBuildingPropAttachmentIndex = -1;
            _session.SetLotDimensions(
                alongX ? oldCellCount - 1 : LotWidthCells,
                alongX ? LotDepthCells : oldCellCount - 1);
            ResizeGround();
            ApplyBaseTexturePresentation();
            BuildGrid();
            ClampRoadCursorToLot();
            ApplySessionState();
            ApplyCameraFacing();
            NotifyStateChanged();
            return true;
        }

        private static void ShiftPointPlacements(List<PlacedFlora> placements,
            bool alongX, Func<float, bool> inStrip, Func<float, float> shift)
        {
            placements?.RemoveAll(item => item == null ||
                inStrip(alongX ? item.PositionX : item.PositionZ));
            foreach (var item in placements ?? new List<PlacedFlora>())
            {
                if (alongX) item.PositionX = shift(item.PositionX);
                else item.PositionZ = shift(item.PositionZ);
            }
        }

        private static void ShiftPointPlacements(List<PlacedProp> placements,
            bool alongX, Func<float, bool> inStrip, Func<float, float> shift)
        {
            placements?.RemoveAll(item => item == null ||
                inStrip(alongX ? item.PositionX : item.PositionZ));
            foreach (var item in placements ?? new List<PlacedProp>())
            {
                if (alongX) item.PositionX = shift(item.PositionX);
                else item.PositionZ = shift(item.PositionZ);
            }
        }

        private static void ShiftCirculationNetwork(CirculationNetwork network,
            bool alongX, Func<float, bool> inStrip, Func<float, float> shift)
        {
            if (network == null) return;
            var removed = new HashSet<string>();
            foreach (var node in network.Nodes)
                if (node == null || inStrip(alongX
                        ? node.PositionMeters.x : node.PositionMeters.y))
                    removed.Add(node?.Id ?? "");
            network.Nodes.RemoveAll(node => node == null || removed.Contains(node.Id));
            network.Segments.RemoveAll(segment => segment == null ||
                removed.Contains(segment.StartNodeId) ||
                removed.Contains(segment.EndNodeId));
            foreach (var node in network.Nodes)
            {
                var position = node.PositionMeters;
                if (alongX) position.x = shift(position.x);
                else position.y = shift(position.y);
                node.PositionMeters = position;
            }
        }

        public void SetZoomLevel(LotZoomLevel level)
        {
            ZoomLevel = level;
            // Zoom is a lens-only operation. Never recompute the camera target,
            // rotation, translation, or projected bounds here.
            ApplyZoomLevel();
            AlignFloraToCamera();
            UpdatePresentationDepthOrdering();
            ApplyCharacterZoomVisibility();
            NotifyStateChanged();
        }

#if UNITY_EDITOR
        public void EnsureBuiltForQa()
        {
            if (_session == null)
                Build();
        }

        public void SetQaOrthographicSize(float orthographicSize)
        {
            if (_camera != null)
                _camera.orthographicSize = orthographicSize;
        }

        public void SetQaCameraPan(float x, float z)
        {
            _cameraPanWorld = new Vector3(x, 0f, z);
            ApplyCameraFacing(true);
        }
#endif

        public void PanCameraViewport(int horizontal, int vertical)
        {
            if (_camera == null || (horizontal == 0 && vertical == 0)) return;
            const float stepMeters = 5f;
            PanCameraInScreenPlane(horizontal * stepMeters, vertical * stepMeters);
            ApplyCameraFacing(true);
        }

        public void PanCameraViewport(Vector2 screenDelta, Vector2 viewportSize)
        {
            if (_camera == null || screenDelta.sqrMagnitude < 0.01f ||
                viewportSize.y <= 1f) return;

            var metersPerPixel =
                (2f * _camera.orthographicSize) / viewportSize.y;

            // UI Toolkit Y grows downward. Move the camera opposite the drag
            // so the lot follows the hand in both screen axes.
            PanCameraInScreenPlane(
                screenDelta.x * metersPerPixel,
                -screenDelta.y * metersPerPixel);
            ApplyCameraFacing(true);
        }

        private void PanCameraInScreenPlane(float screenRight, float screenUp)
        {
            // Orthographic panning must remain parallel to the image plane.
            // Projecting camera-up onto the ground adds a camera-forward
            // component; repeated vertical pans then carry stationary billboard
            // buildings through the near plane one at a time.
            var cameraRight = _camera.transform.right;
            var cameraUp = _camera.transform.up;
            var next = _cameraPanWorld -
                       cameraRight * screenRight - cameraUp * screenUp;
            var rightOffset = Mathf.Clamp(Vector3.Dot(next, cameraRight),
                -LotWidthMeters * 0.5f, LotWidthMeters * 0.5f);
            var upOffset = Mathf.Clamp(Vector3.Dot(next, cameraUp),
                -LotDepthMeters * 0.5f, LotDepthMeters * 0.5f);
            _cameraPanWorld = cameraRight * rightOffset + cameraUp * upOffset;
        }

        public void SetCameraPanInteraction(bool active)
        {
            _cameraPanInteractionActive = active;
            ClearObjectHover();
            if (!active) return;

            ActiveObjectSelection = LotObjectSelectionKind.None;
            _session.Select(false);
            RoadCursorSelected = false;
            CirculationCursorSelected = false;
            SelectedFloraIndex = -1;
            SelectedPropIndex = -1;
            _buildingDragActive = false;
            _buildingFocusFreezeActive = false;
            _floraDragActive = false;
            _propDragActive = false;
            if (_roadCursor != null) _roadCursor.gameObject.SetActive(false);
            if (_circulationCursor != null)
                _circulationCursor.gameObject.SetActive(false);
            if (_floraPreview != null) _floraPreview.gameObject.SetActive(false);
            ApplyFloraSelection();
            ApplyPropSelection();
            ApplySessionState();
        }

        public void DeselectAll()
        {
            ClearObjectHover();
            _buildingDragActive = false;
            _buildingFocusFreezeActive = false;
            ActiveObjectSelection = LotObjectSelectionKind.None;
            _session.Select(false);
            RoadCursorSelected = false;
            CirculationCursorSelected = false;
            if (_roadCursor != null) _roadCursor.gameObject.SetActive(false);
            if (_circulationCursor != null) _circulationCursor.gameObject.SetActive(false);
            ClearFloraSelectionAndPreview();
            ClearPropSelectionAndPreview();
            ClearOverlayTextureSelection();
            ApplySessionState();
            NotifyStateChanged();
        }

        private void ReconcileBuildingFocusBeforeObjectSwitch()
        {
            if (!_buildingFocusFreezeActive) return;
            _buildingDragActive = false;
            _buildingFocusFreezeActive = false;
            _session.Select(false);
            ActiveObjectSelection = LotObjectSelectionKind.None;
            ApplySessionState();
        }

        public void SetBuildingEditorContext(bool selectable, bool faded)
        {
            var enteringBuildingEditor = selectable && !_buildingAuthoringActive;
            var leavingFocusedBuildingEditor = !selectable &&
                _buildingAuthoringActive && _buildingFocusFreezeActive;
            _buildingsSelectable = true;
            _buildingAuthoringActive = selectable;
            if (enteringBuildingEditor && !TopDownViewEnabled)
            {
                // A saved lot can restore directly into the Buildings
                // workspace without invoking its category button. Artwork is
                // the authoring view; primitive/hybrid remain explicit View
                // diagnostics only.
                InspectionMode = BuildingInspectionMode.Artwork;
            }
            _buildingContextOpacity = faded ? 0.32f : 1f;
            var artworkOpacity = BuildingInspectionPolicy.ArtworkOpacity(
                InspectionMode, _buildingContextOpacity);
            _presentation?.SetOpacity(artworkOpacity);
            foreach (var presentation in _otherBuildingPresentations)
                presentation?.SetOpacity(artworkOpacity);
            if (leavingFocusedBuildingEditor)
            {
                _buildingDragActive = false;
                _buildingFocusFreezeActive = false;
                ApplySessionState();
            }
            else if (enteringBuildingEditor)
                ApplySessionState();
            if (_selectionFootprint != null)
                UpdateSelectedBuildingSelectionVisuals(HasBuilding);
        }

        public void SetCirculationEditorContext(bool active)
        {
            _circulationEditorActive = active;
            if (_circulationCursor != null)
                _circulationCursor.gameObject.SetActive(active && CirculationCursorSelected);
        }

        public void PlaceGovernmentHouseAtCenter()
        {
            PlaceBuildingAtCenter(BuildingCatalog.ColonialGovernmentHouseId);
        }

        public bool PlaceBuildingAtCenter(string buildingId)
        {
            SelectCatalogBuilding(buildingId);
            if (!TryFindBuildingPlacement(_buildingPackage, out var cell)) return false;
            _session.AddBuilding(buildingId, cell.x, cell.y);
            ApplySessionState();
            // Adding content must not steal the player's framing. The camera
            // is fit when a lot is opened, then remains entirely user-owned.
            NotifyStateChanged();
            return true;
        }

        public bool BeginBuildingPlacementAtCenter(string buildingId)
        {
            SelectCatalogBuilding(buildingId);
            if (!TryFindBuildingPlacement(_buildingPackage, out var cell)) return false;
            _session.AddBuilding(buildingId, cell.x, cell.y);
            ActiveObjectSelection = LotObjectSelectionKind.Building;
            SelectedFloraIndex = -1;
            SelectedPropIndex = -1;
            _buildingDragActive = true;
            _buildingFocusFreezeActive = true;
            _buildingDragOffset = Vector2.zero;
            ApplySessionState();
            NotifyStateChanged();
            return true;
        }

        public void SelectCatalogBuilding(string buildingId)
        {
            // Catalog placement is an art-authoring action. Never inherit a
            // diagnostic proxy view from a previously inspected building.
            if (!TopDownViewEnabled)
                InspectionMode = BuildingInspectionMode.Artwork;
            _placementBuildingId = buildingId;
            EnsureBuildingPackage(buildingId);
        }

        public void NudgeSelected(int deltaX, int deltaZ)
        {
            MoveBuildingTo(
                Mathf.RoundToInt(_session.Data.CellX) + deltaX,
                Mathf.RoundToInt(_session.Data.CellZ) + deltaZ);
            ApplySessionState();
            NotifyStateChanged();
        }

        public bool NudgeSelectedBuildingByScreenPixels(int horizontal, int vertical)
        {
            if (!HasBuilding || _buildingPackage == null || _camera == null ||
                (horizontal == 0 && vertical == 0)) return false;
            if (!TryGroundDeltaForArrowKey(horizontal, vertical, out var delta))
                return false;
            var odd = Mathf.Abs(_session.Data.RotationQuarterTurns) % 2 == 1;
            var width = odd ? _buildingPackage.DepthMeters : _buildingPackage.WidthMeters;
            var depth = odd ? _buildingPackage.WidthMeters : _buildingPackage.DepthMeters;
            var minimumX = -LotWidthMeters * 0.5f + width * 0.5f;
            var maximumX = LotWidthMeters * 0.5f - width * 0.5f;
            var minimumZ = -LotDepthMeters * 0.5f + depth * 0.5f;
            var maximumZ = LotDepthMeters * 0.5f - depth * 0.5f;
            if (minimumX > maximumX) minimumX = maximumX = 0f;
            if (minimumZ > maximumZ) minimumZ = maximumZ = 0f;
            var targetX = Mathf.Clamp(_session.Data.CellX + delta.x,
                minimumX, maximumX);
            var targetZ = Mathf.Clamp(_session.Data.CellZ + delta.z,
                minimumZ, maximumZ);
            if (Mathf.Approximately(targetX, _session.Data.CellX) &&
                Mathf.Approximately(targetZ, _session.Data.CellZ)) return false;
            _session.Move(targetX, targetZ);
            if (_buildingFocusFreezeActive)
                ApplySelectedBuildingPositionOnly();
            else
                ApplySessionState();
            return true;
        }

        private void ApplySelectedBuildingPositionOnly()
        {
            var position = new Vector3(
                _session.Data.CellX, 0f, _session.Data.CellZ);
            var previousPosition = _presentation != null
                ? _presentation.transform.position
                : _proxy != null
                    ? _proxy.position
                    : position;
            var worldDelta = position - previousPosition;
            if (_presentation != null) _presentation.transform.position = position;
            if (_proxy != null) _proxy.position = position;
            if (_buildingDepthOccluder != null)
                _buildingDepthOccluder.position = position;
            if (_projectedShadow != null) _projectedShadow.position = position;
            if (_selectionFootprint != null)
                _selectionFootprint.position = position +
                    new Vector3(0f, 0.025f, 0f);
            if (_registrationDiagnostics != null)
                _registrationDiagnostics.position = position;
            TranslateBuildingPropPresentationsForBuilding(
                _session.SelectedBuildingIndex, worldDelta);
            _buildingFocusLiveMoveCountForQa++;
        }

        private void ApplySelectedBuildingRotationOnly()
        {
            ApplySelectedBuildingPositionOnly();
            var rotation = BuildingRotation();
            var primitiveRotation = PrimitiveRotation(
                _buildingPackage, _session.Data.RotationQuarterTurns);
            if (_proxy != null) _proxy.rotation = primitiveRotation;
            if (_buildingDepthOccluder != null)
                _buildingDepthOccluder.rotation = primitiveRotation;
            if (_registrationDiagnostics != null)
                _registrationDiagnostics.rotation = primitiveRotation;
            ApplyPresentationFacing();
            if (_presentation != null && _buildingPackage != null)
                _presentation.RegisterToProxy(
                    _proxyLocalVertices, primitiveRotation);
            if (_selectionFootprint != null)
                _selectionFootprint.rotation = rotation;
            RepositionBuildingPropPresentationsForBuilding(
                _session.SelectedBuildingIndex);
            UpdateProjectedShadow();
        }

        private bool TryGroundDeltaForArrowKey(int horizontal, int vertical,
            out Vector3 delta)
        {
            delta = Vector3.zero;
            if (_camera == null) return false;
            var center = new Vector2(
                Mathf.Max(1, _camera.pixelWidth) * 0.5f,
                Mathf.Max(1, _camera.pixelHeight) * 0.5f);
            var ground = new Plane(Vector3.up, Vector3.zero);
            var fromRay = _camera.ScreenPointToRay(center);
            // One arrow press maps to one camera pixel. The previous 0.2
            // subpixel calibration could quantize away at common Game View
            // resolutions and made selected buildings appear immovable.
            const float displayedPixelScale = 1f;
            var toRay = _camera.ScreenPointToRay(center + new Vector2(
                horizontal * displayedPixelScale,
                vertical * displayedPixelScale));
            if (!ground.Raycast(fromRay, out var fromDistance) ||
                !ground.Raycast(toRay, out var toDistance)) return false;
            delta = toRay.GetPoint(toDistance) - fromRay.GetPoint(fromDistance);
            return true;
        }

        public void ToggleGridVisibility()
        {
            _gridVisible = !_gridVisible;
            ApplyGridVisibility();
            NotifyStateChanged();
        }

        public void RotateSelected(int direction)
        {
            var targetRotation = FiveBayHybridContract.WrapFacing(
                _session.Data.RotationQuarterTurns + direction);
            var odd = targetRotation % 2 == 1;
            var width = odd ? _buildingPackage.DepthMeters : _buildingPackage.WidthMeters;
            var depth = odd ? _buildingPackage.WidthMeters : _buildingPackage.DepthMeters;
            var targetX = Mathf.RoundToInt(Mathf.Clamp(_session.Data.CellX,
                -LotWidthMeters * 0.5f + width * 0.5f,
                LotWidthMeters * 0.5f - width * 0.5f));
            var targetZ = Mathf.RoundToInt(Mathf.Clamp(_session.Data.CellZ,
                -LotDepthMeters * 0.5f + depth * 0.5f,
                LotDepthMeters * 0.5f - depth * 0.5f));
            if (CanOccupyBuilding(_buildingPackage,
                    targetX, targetZ, targetRotation,
                    _session.SelectedBuildingIndex))
            {
                _session.Rotate(direction);
                _session.Move(targetX, targetZ);
                if (_session.SelectedBuildingIndex >= 0 &&
                    _session.SelectedBuildingIndex < _session.Data.Buildings.Count)
                    BuildingPropCatalog.RotateWithBuilding(
                        _session.Data.Buildings[_session.SelectedBuildingIndex],
                        direction);
            }
            var focused = _buildingFocusFreezeActive;
            if (focused)
                ApplySelectedBuildingRotationOnly();
            else
                ApplySessionState();
            if (!focused) NotifyStateChanged();
        }

        public bool DeleteSelectedBuilding()
        {
            if (!_session.IsSelected || _session.SelectedBuildingIndex < 0 ||
                _session.SelectedBuildingIndex >= BuildingCount)
                return false;
            _session.Delete();
            _session.Select(false);
            ClearObjectHover();
            _buildingDragActive = false;
            _buildingFocusFreezeActive = false;
            ActiveObjectSelection = LotObjectSelectionKind.None;
            ApplySessionState();
            NotifyStateChanged();
            return true;
        }

        public void DeleteSelected() => DeleteSelectedBuilding();

        public void RefreshCameraFraming()
        {
            ApplyCameraFacing(false);
        }

        public void NewEmptyLot(string name = "Untitled Lot",
            LotType lotType = LotType.Residential, int sizeMeters = 20)
            => NewEmptyLot(name, lotType,
                Mathf.Clamp(Mathf.RoundToInt(sizeMeters / 10f), 1, 8),
                Mathf.Clamp(Mathf.RoundToInt(sizeMeters / 10f), 1, 8));

        public void NewEmptyLot(string name, LotType lotType,
            int widthCells, int depthCells)
        {
            _buildingDragActive = false;
            _buildingFocusFreezeActive = false;
            _session.NewLot(name, lotType, Mathf.Max(widthCells, depthCells) * 10);
            _session.SetLotDimensions(widthCells, depthCells);
            _session.MarkClean();
            _roadUndo.Clear();
            _roadRedo.Clear();
            _activeNodeId = "";
            RebuildRoadArtwork();
            RebuildRoadVehicleNetwork();
            ApplySessionState();
            NotifyStateChanged();
        }

        public void ApplyTrafficTestTemplate()
        {
            // Keep the visual QA loop centered and close enough to inspect in
            // the windowed Editor. The larger production-demo circuit placed
            // its vehicle around the far edge of an 8 x 8 lot.
            NewEmptyLot("Two-Way Traffic Test", LotType.Neighborhood, 40);
            SeedRoadVerticalSlice();
            CirculationDefaults.SeedVerticalSlice(_session.Data);
            RebuildRoadArtwork();
            RebuildRoadVehicleNetwork();
            ApplyLotPlanningState();
            // Keep one real road tile selected during the hands-on shadow QA.
            // This makes the highlighted receiver and the three ordinary road
            // receivers directly comparable in the same Editor frame.
            var comparisonCell = RoadPlacementModel.CellCenterMeters(
                -1, -1, LotWidthMeters, LotDepthMeters);
            var comparisonWorld = transform.TransformPoint(
                new Vector3(comparisonCell.x, 0f, comparisonCell.y));
            SelectRoadCellAtWorld(comparisonWorld.x, comparisonWorld.z, false);
            NotifyStateChanged();
        }

        public string SaveLot()
        {
            var requirements = new List<string> { _vehicleType.Id };
            foreach (var road in _session.Data.RoadPieces ?? new List<PlacedRoadPiece>())
            {
                var packageId = string.IsNullOrWhiteSpace(road?.PackageId)
                    ? RoadPiecePackage.LegacyPackageId : road.PackageId;
                if (!requirements.Contains(packageId)) requirements.Add(packageId);
            }
            foreach (var building in _session.Data.Buildings ?? new List<PlacedBuilding>())
                if (!string.IsNullOrWhiteSpace(building.BuildingId) &&
                    !requirements.Contains(building.BuildingId))
                    requirements.Add(building.BuildingId);
            var path = LotSaveStore.Save(_session, requirements);
            NotifyStateChanged();
            return path;
        }

        public string SaveLotAs(string name)
        {
            var safeName = string.IsNullOrWhiteSpace(name) ? "Untitled Lot" : name.Trim();
            _session.ForkAs(safeName, LotSaveStore.UniqueId(safeName));
            return SaveLot();
        }

        public string RenameAndSaveLot(string name)
        {
            _session.Rename(name);
            return SaveLot();
        }

        public List<string> MissingDependencies(string lotId, string root = null)
        {
            _vehicleType ??= VehicleTypePackage.LoadModelT();
            var available = new HashSet<string> { _vehicleType.Id };
            foreach (var package in RoadPiecePackageCatalog.Packages)
                available.Add(package.Id);
            foreach (var package in HybridBuildingPackageRegistry.All)
                available.Add(package.Id);
            return LotSaveStore.MissingDependencies(lotId, available, root);
        }

        public bool LoadLot(string lotId = "", string root = null)
        {
            if (string.IsNullOrWhiteSpace(lotId))
            {
                var saves = LotSaveStore.List();
                if (saves.Count == 0) return false;
                lotId = saves[0].LotId;
            }
            if (MissingDependencies(lotId, root).Count > 0) return false;
            _buildingDragActive = false;
            _buildingFocusFreezeActive = false;
            var loaded = LotSaveStore.Load(_session, lotId, root);
            if (loaded && _session.Data.HasBuilding)
            {
                EnsureBuildingPackage(_session.Data.BuildingId);
            }
            if (loaded)
            {
                _buildingDragActive = false;
                _buildingFocusFreezeActive = false;
                // Restore changes the data dimensions, but the existing world
                // was built at the previous lot size. Rebuild every piece of
                // size-dependent presentation without mutating the restored
                // save or requiring Joe to re-apply its dimensions manually.
                ResizeGround();
                BuildGrid();
                ClampRoadCursorToLot();
                RebuildRoadArtwork();
                RebuildRoadVehicleNetwork();
                ApplyRoadCursor();
                ApplyZoomLevel();
                ApplyCameraFacing();
            }
            ApplySessionState();
            NotifyStateChanged();
            return loaded;
        }

        public void SetCirculationMode(CirculationMode mode)
        {
            CirculationMode = mode;
            CirculationCursorSelected = true;
            _activeNodeId = "";
            ApplyCirculationCursor();
            NotifyStateChanged();
        }

        public void NudgeCirculationCursor(int deltaX, int deltaZ)
        {
            CirculationCursorSelected = true;
            CirculationCursorMeters = new Vector2(
                Mathf.Clamp(CirculationCursorMeters.x + deltaX, -10f, 10f),
                Mathf.Clamp(CirculationCursorMeters.y + deltaZ, -10f, 10f));
            ApplyCirculationCursor();
            NotifyStateChanged();
        }

        public void AddCirculationNode()
        {
            CirculationCursorSelected = true;
            var node = ActiveCirculationNetwork.AddNode(CirculationCursorMeters);
            if (!string.IsNullOrWhiteSpace(_activeNodeId))
                ActiveCirculationNetwork.Connect(_activeNodeId, node.Id);
            _activeNodeId = node.Id;
            RebuildCirculationVisualization();
            NotifyStateChanged();
        }

        public void DeleteLastCirculationNode()
        {
            var network = ActiveCirculationNetwork;
            if (network.Nodes.Count == 0) return;
            network.DeleteNode(network.Nodes[network.Nodes.Count - 1].Id);
            _activeNodeId = network.Nodes.Count == 0 ? "" : network.Nodes[network.Nodes.Count - 1].Id;
            RebuildCirculationVisualization();
            NotifyStateChanged();
        }

        public void SeedCirculationVerticalSlice()
        {
            CirculationDefaults.SeedVerticalSlice(_session.Data);
            RebuildCirculationVisualization();
            NotifyStateChanged();
        }

        public void ToggleCirculationDiagnostics()
        {
            CirculationDiagnosticsVisible = !CirculationDiagnosticsVisible;
            if (_circulationRoot != null)
                _circulationRoot.gameObject.SetActive(CirculationDiagnosticsVisible);
            NotifyStateChanged();
        }

        public void SelectRoadPiece(RoadPieceTopology topology)
        {
            RoadCursorSelected = true;
            SelectedRoadTopology = topology;
            NotifyStateChanged();
        }

        public void SelectRoadPackage(string packageId)
        {
            _roadPackage = RoadPiecePackageCatalog.Resolve(packageId);
            NotifyStateChanged();
        }

        public bool ApplyRoadMaterials(string roadMaterialId, string sidewalkMaterialId,
            RoadMarkingStyle markingStyle = RoadMarkingStyle.SingleDotted,
            bool applyToAll = false,
            RoadLaneMarkingStyle laneMarkingStyle = RoadLaneMarkingStyle.Lines,
            RoadCenterMarkingStyle centerMarkingStyle = RoadCenterMarkingStyle.DoubleLines)
        {
            _selectedRoadMaterialId = RoadMaterialCatalog.Resolve(roadMaterialId).Id;
            _selectedSidewalkMaterialId = RoadMaterialCatalog.Resolve(
                sidewalkMaterialId, true).Id;
            _selectedRoadMarkingStyle = markingStyle;
            _selectedRoadLaneMarkingStyle = laneMarkingStyle;
            _selectedRoadCenterMarkingStyle = centerMarkingStyle;
            _applyRoadMaterialsToAll = applyToAll;
            if (applyToAll)
            {
                PushRoadUndo(_session.Serialize());
                var changed = false;
                foreach (var road in _session.Data.RoadPieces)
                {
                    if (RoadPiecePackageCatalog.Resolve(road.PackageId).Id ==
                        RoadPiecePackage.LegacyPackageId) continue;
                    road.RoadMaterialId = _selectedRoadMaterialId;
                    road.SidewalkMaterialId = _selectedSidewalkMaterialId;
                    road.MarkingStyle = _selectedRoadMarkingStyle;
                    road.LaneMarkingStyle = _selectedRoadLaneMarkingStyle;
                    road.CenterMarkingStyle = _selectedRoadCenterMarkingStyle;
                    changed = true;
                }
                if (changed) RebuildRoadArtwork();
                NotifyStateChanged();
                return changed;
            }
            var placed = RoadPlacementModel.FindAt(_session.Data.RoadPieces,
                RoadCursorCell.x, RoadCursorCell.y);
            if (placed == null || RoadPiecePackageCatalog.Resolve(placed.PackageId).Id ==
                RoadPiecePackage.LegacyPackageId)
            {
                NotifyStateChanged();
                return false;
            }
            PushRoadUndo(_session.Serialize());
            placed.RoadMaterialId = _selectedRoadMaterialId;
            placed.SidewalkMaterialId = _selectedSidewalkMaterialId;
            placed.MarkingStyle = _selectedRoadMarkingStyle;
            placed.LaneMarkingStyle = _selectedRoadLaneMarkingStyle;
            placed.CenterMarkingStyle = _selectedRoadCenterMarkingStyle;
            RebuildRoadArtwork();
            NotifyStateChanged();
            return true;
        }

        public void NudgeRoadCursor(int deltaX, int deltaZ)
        {
            RoadCursorSelected = true;
            RoadCursorCell = new Vector2Int(
                Mathf.Clamp(RoadCursorCell.x + deltaX,
                    RoadPlacementModel.MinimumCellForLot(LotWidthMeters),
                    RoadPlacementModel.MaximumCellForLot(LotWidthMeters)),
                Mathf.Clamp(RoadCursorCell.y + deltaZ,
                    RoadPlacementModel.MinimumCellForLot(LotDepthMeters),
                    RoadPlacementModel.MaximumCellForLot(LotDepthMeters)));
            ApplyRoadCursor();
            NotifyStateChanged();
        }

        public bool SelectRoadCellAtWorld(float worldX, float worldZ, bool notify = true)
        {
            var local = transform.InverseTransformPoint(new Vector3(worldX, 0f, worldZ));
            var halfSize = LotSizeMeters * 0.5f;
            if (local.x < -halfSize || local.x >= halfSize ||
                local.z < -halfSize || local.z >= halfSize) return false;
            RoadCursorCell = RoadPlacementModel.WorldToCell(
                local.x, local.z, LotWidthMeters, LotDepthMeters);
            RoadCursorSelected = true;
            var selected = RoadPlacementModel.FindAt(
                _session.Data.RoadPieces, RoadCursorCell.x, RoadCursorCell.y);
            if (selected != null)
            {
                SelectedRoadTopology = selected.Topology;
                RoadRotationQuarterTurns = selected.RotationQuarterTurns;
                _roadPackage = RoadPiecePackageCatalog.Resolve(selected.PackageId);
                _selectedRoadMaterialId = RoadMaterialCatalog.Resolve(
                    selected.RoadMaterialId).Id;
                _selectedSidewalkMaterialId = RoadMaterialCatalog.Resolve(
                    selected.SidewalkMaterialId, true).Id;
                _selectedRoadMarkingStyle = selected.MarkingStyle;
                _selectedRoadLaneMarkingStyle = selected.LaneMarkingStyle;
                _selectedRoadCenterMarkingStyle = selected.CenterMarkingStyle;
            }
            ApplyRoadCursor();
            if (notify) NotifyStateChanged();
            return true;
        }

        public bool SelectRoadCellFromScreen(Vector2 screenPosition)
        {
            if (_camera == null) return false;
            var ray = _camera.ScreenPointToRay(screenPosition);
            var plane = new Plane(transform.up, transform.position);
            return plane.Raycast(ray, out var distance) &&
                SelectRoadCellAtWorld(ray.GetPoint(distance).x, ray.GetPoint(distance).z);
        }

        public bool SelectRoadCellFromPanel(
            Vector2 panelPosition,
            Vector2 panelSize,
            bool notify = true)
        {
            if (_cameraPanInteractionActive || _camera == null) return false;
            var pixel = PanelToCameraPixel(
                panelPosition,
                panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            var ray = _camera.ScreenPointToRay(pixel);
            var plane = new Plane(transform.up, transform.position);
            return plane.Raycast(ray, out var distance) && SelectRoadCellAtWorld(
                ray.GetPoint(distance).x, ray.GetPoint(distance).z, notify);
        }

        public bool BeginBuildingDragFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_buildingsSelectable || !HasBuilding ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var lotPoint))
                return false;
            var pixel = PanelToCameraPixel(
                panelPosition,
                panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            var hitIndex = FindBuildingHitIndex(pixel,
                new Vector2(lotPoint.x, lotPoint.z));
            if (hitIndex < 0) return false;
            if (_buildingAuthoringActive && !TopDownViewEnabled)
                InspectionMode = BuildingInspectionMode.Artwork;
            ActiveObjectSelection = LotObjectSelectionKind.Building;
            SelectedFloraIndex = -1;
            SelectedPropIndex = -1;
            ApplyFloraSelection();
            ApplyPropSelection();
            var selectionChanged = hitIndex != _session.SelectedBuildingIndex ||
                !_session.IsSelected || !_buildingFocusFreezeActive;
            if (hitIndex != _session.SelectedBuildingIndex)
            {
                _session.SelectBuilding(hitIndex);
                EnsureBuildingPackage(_session.Data.BuildingId);
            }

            _buildingDragActive = true;
            _buildingFocusFreezeActive = true;
            _buildingDragOffset = new Vector2(
                _session.Data.CellX - lotPoint.x,
                _session.Data.CellZ - lotPoint.z);
            _session.Select(true);
            if (selectionChanged)
                ApplySessionState();
            return true;
        }

        public LotObjectSelectionKind BeginExistingObjectManipulationFromPanel(
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (_cameraPanInteractionActive)
                return LotObjectSelectionKind.None;
            ClearObjectHover();
            var pixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            var buildingPropIndex = BuildingPropPresentationIndexAtCameraPixel(pixel);
            if (_buildingFocusFreezeActive && buildingPropIndex >= 0)
            {
                ReconcileBuildingFocusBeforeObjectSwitch();
                buildingPropIndex = BuildingPropPresentationIndexAtCameraPixel(pixel);
            }
            if (buildingPropIndex >= 0 && BeginBuildingPropDragFromPanel(
                    buildingPropIndex, panelPosition, panelSize))
                return LotObjectSelectionKind.BuildingProp;
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var lotPoint))
                return LotObjectSelectionKind.None;
            var buildingIndex = FindBuildingHitIndex(pixel,
                new Vector2(lotPoint.x, lotPoint.z));
            var floraIndex = FloraIndexAtCameraPixel(pixel);
            var propIndex = PropIndexAtCameraPixel(pixel);
            var kind = LotObjectSelectionKind.None;
            var nearestDepth = float.PositiveInfinity;

            void Consider(LotObjectSelectionKind candidate, int index, Vector3 position)
            {
                if (index < 0) return;
                var depth = _camera.WorldToScreenPoint(position).z;
                if (depth >= nearestDepth) return;
                nearestDepth = depth;
                kind = candidate;
            }

            // Flora and ground props use their own visible presentation hit tests.
            // Those explicit hits must beat the much broader building-billboard
            // fallback, especially for objects northwest/behind a tall building.
            // Otherwise a visibly exposed tree canopy can only select the building.
            if (buildingIndex >= 0 && floraIndex < 0 && propIndex < 0)
            {
                var building = _session.Data.Buildings[buildingIndex];
                Consider(LotObjectSelectionKind.Building, buildingIndex,
                    new Vector3(building.CellX, 0f, building.CellZ));
            }
            if (floraIndex >= 0)
            {
                var flora = _session.Data.Flora[floraIndex];
                Consider(LotObjectSelectionKind.Flora, floraIndex,
                    new Vector3(flora.PositionX, 0f, flora.PositionZ));
            }
            if (propIndex >= 0)
            {
                var prop = _session.Data.Props[propIndex];
                Consider(LotObjectSelectionKind.Prop, propIndex,
                    new Vector3(prop.PositionX, 0f, prop.PositionZ));
            }

            if (_buildingFocusFreezeActive &&
                kind is LotObjectSelectionKind.Flora or LotObjectSelectionKind.Prop)
                ReconcileBuildingFocusBeforeObjectSwitch();

            return kind switch
            {
                LotObjectSelectionKind.Building when
                    BeginBuildingDragFromPanel(panelPosition, panelSize) => kind,
                LotObjectSelectionKind.Flora when
                    BeginFloraDragFromPanel("", panelPosition, panelSize) => kind,
                LotObjectSelectionKind.Prop when
                    BeginPropDragFromPanel("", panelPosition, panelSize) => kind,
                _ => LotObjectSelectionKind.None
            };
        }

        public LotObjectSelectionKind UpdateObjectHoverFromPanel(
            Vector2 panelPosition, Vector2 panelSize, bool suppress = false)
        {
            if ((_buildingFocusFreezeActive && !_buildingDragActive) ||
                _cameraPanInteractionActive || suppress || _buildingDragActive ||
                _floraDragActive || _propDragActive ||
                BuildingPropDragActive)
            {
                ClearObjectHover();
                return LotObjectSelectionKind.None;
            }
            var pixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            var buildingPropIndex = BuildingPropPresentationIndexAtCameraPixel(pixel);
            if (buildingPropIndex >= 0)
            {
                ApplyBuildingPropHover(buildingPropIndex);
                return LotObjectSelectionKind.BuildingProp;
            }
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var lotPoint))
            {
                ClearObjectHover();
                return LotObjectSelectionKind.None;
            }
            var buildingIndex = FindBuildingHitIndex(pixel,
                new Vector2(lotPoint.x, lotPoint.z));
            var floraIndex = FloraIndexAtCameraPixel(pixel);
            var propIndex = PropIndexAtCameraPixel(pixel);
            var kind = LotObjectSelectionKind.None;
            var index = -1;
            var nearestDepth = float.PositiveInfinity;

            void Consider(LotObjectSelectionKind candidate, int candidateIndex,
                Vector3 position)
            {
                if (candidateIndex < 0) return;
                var depth = _camera.WorldToScreenPoint(position).z;
                if (depth >= nearestDepth) return;
                nearestDepth = depth;
                kind = candidate;
                index = candidateIndex;
            }

            if (buildingIndex >= 0 && floraIndex < 0 && propIndex < 0)
            {
                var item = _session.Data.Buildings[buildingIndex];
                Consider(LotObjectSelectionKind.Building, buildingIndex,
                    new Vector3(item.CellX, 0f, item.CellZ));
            }
            if (floraIndex >= 0)
            {
                var item = _session.Data.Flora[floraIndex];
                Consider(LotObjectSelectionKind.Flora, floraIndex,
                    new Vector3(item.PositionX, 0f, item.PositionZ));
            }
            if (propIndex >= 0)
            {
                var item = _session.Data.Props[propIndex];
                Consider(LotObjectSelectionKind.Prop, propIndex,
                    new Vector3(item.PositionX, 0f, item.PositionZ));
            }
            ApplyObjectHover(kind, index);
            return kind;
        }

        public void ClearObjectHover()
        {
            ClearBuildingPropHover();
            HoverObjectKind = LotObjectSelectionKind.None;
            _hoverObjectIndex = -1;
            if (_objectHoverHighlight != null)
                _objectHoverHighlight.gameObject.SetActive(false);
        }

        private int FindBuildingHitIndex(Vector2 pixel, Vector2 lotPoint)
        {
            var bestIndex = -1;
            var bestScore = float.PositiveInfinity;
            for (var index = 0; index < (_session.Data.Buildings?.Count ?? 0); index++)
            {
                var placed = _session.Data.Buildings[index];
                var package = HybridBuildingPackageRegistry.Load(
                    BuildingCatalog.Find(placed.BuildingId).PackageResourcePath);
                if (BuildingFootprintContains(lotPoint,
                        new Vector2(placed.CellX, placed.CellZ),
                        package.WidthMeters, package.DepthMeters,
                        placed.RotationQuarterTurns))
                    return index;
                var presentation = PresentationForBuildingIndex(index);
                if (!BuildingVisualContainsCameraPixel(presentation, pixel)) continue;
                var score = BuildingVisualHitScore(presentation, pixel);
                if (score >= bestScore) continue;
                bestScore = score;
                bestIndex = index;
            }
            return bestIndex;
        }

        public bool SelectBuildingAtLotPoint(Vector2 lotPoint)
        {
            if (!_buildingsSelectable) return false;
            for (var index = 0; index < (_session.Data.Buildings?.Count ?? 0); index++)
            {
                var placed = _session.Data.Buildings[index];
                var package = HybridBuildingPackageRegistry.Load(
                    BuildingCatalog.Find(placed.BuildingId).PackageResourcePath);
                if (!BuildingFootprintContains(lotPoint,
                        new Vector2(placed.CellX, placed.CellZ),
                        package.WidthMeters, package.DepthMeters,
                        placed.RotationQuarterTurns)) continue;
                if (index != _session.SelectedBuildingIndex)
                {
                    _session.SelectBuilding(index);
                    EnsureBuildingPackage(_session.Data.BuildingId);
                    ApplySessionState();
                }
                return true;
            }
            return false;
        }

        public Vector3 BuildingPresentationPosition(int index)
        {
            var presentation = PresentationForBuildingIndex(index);
            return presentation != null
                ? presentation.transform.position
                : new Vector3(float.NaN, float.NaN, float.NaN);
        }

        public float BuildingPresentationOpacity(int index) =>
            PresentationForBuildingIndex(index)?.Opacity ?? 0f;

        private HybridBuildingPresentation PresentationForBuildingIndex(int index)
        {
            if (index == _session.SelectedBuildingIndex) return _presentation;
            var viewIndex = _otherBuildingIndices.IndexOf(index);
            return viewIndex >= 0 && viewIndex < _otherBuildingPresentations.Count
                ? _otherBuildingPresentations[viewIndex] : null;
        }

        private float BuildingVisualHitScore(
            HybridBuildingPresentation presentation, Vector2 pixel)
        {
            if (_camera == null || presentation == null) return float.PositiveInfinity;
            var renderers = presentation.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds();
            var found = false;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) return float.PositiveInfinity;
            var center = _camera.WorldToScreenPoint(bounds.center);
            return ((Vector2)center - pixel).sqrMagnitude;
        }

        private bool BuildingVisualContainsCameraPixel(
            HybridBuildingPresentation presentation,
            Vector2 pixel)
        {
            // A billboard's renderer bounds include its entire transparent
            // rectangle. Hit the tight alpha-derived sprite mesh instead so
            // empty sky/ground around the artwork cannot select the building.
            return presentation != null &&
                presentation.ContainsVisibleArtworkPixel(_camera, pixel);
        }

        public bool DragBuildingFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_buildingDragActive ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var lotPoint)) return false;
            if (_buildingAuthoringActive && !TopDownViewEnabled)
                InspectionMode = BuildingInspectionMode.Artwork;
            var cellX = Mathf.RoundToInt(lotPoint.x + _buildingDragOffset.x);
            var cellZ = Mathf.RoundToInt(lotPoint.z + _buildingDragOffset.y);
            if (cellX == _session.Data.CellX && cellZ == _session.Data.CellZ) return false;
            if (!MoveBuildingTo(cellX, cellZ)) return false;
            ApplySelectedBuildingPositionOnly();
            return true;
        }

        private void RestoreCameraFraming(
            Vector3 position, Quaternion rotation, float orthographicSize)
        {
            if (_camera == null) return;
            _camera.transform.SetPositionAndRotation(position, rotation);
            _camera.orthographicSize = orthographicSize;
            AlignFloraToCamera();
            UpdatePresentationDepthOrdering();
        }

        private bool MoveBuildingTo(int cellX, int cellZ)
        {
            if (!HasBuilding || _buildingPackage == null) return false;
            var oddTurn = Mathf.Abs(_session.Data.RotationQuarterTurns) % 2 == 1;
            var width = oddTurn ? _buildingPackage.DepthMeters : _buildingPackage.WidthMeters;
            var depth = oddTurn ? _buildingPackage.WidthMeters : _buildingPackage.DepthMeters;
            var minimumX = Mathf.CeilToInt(-LotWidthMeters * 0.5f + width * 0.5f);
            var maximumX = Mathf.FloorToInt(LotWidthMeters * 0.5f - width * 0.5f);
            var minimumZ = Mathf.CeilToInt(-LotDepthMeters * 0.5f + depth * 0.5f);
            var maximumZ = Mathf.FloorToInt(LotDepthMeters * 0.5f - depth * 0.5f);
            if (minimumX > maximumX) minimumX = maximumX = 0;
            if (minimumZ > maximumZ) minimumZ = maximumZ = 0;
            var targetX = Mathf.Clamp(cellX, minimumX, maximumX);
            var targetZ = Mathf.Clamp(cellZ, minimumZ, maximumZ);
            if (!CanOccupyBuilding(_buildingPackage, targetX, targetZ,
                    _session.Data.RotationQuarterTurns,
                    _session.SelectedBuildingIndex)) return false;
            _session.Move(targetX, targetZ);
            return true;
        }

        private bool TryFindBuildingPlacement(
            HybridBuildingPackage package,
            out Vector2Int cell)
        {
            cell = default;
            var minX = Mathf.CeilToInt(-LotWidthMeters * 0.5f + package.WidthMeters * 0.5f);
            var maxX = Mathf.FloorToInt(LotWidthMeters * 0.5f - package.WidthMeters * 0.5f);
            var minZ = Mathf.CeilToInt(-LotDepthMeters * 0.5f + package.DepthMeters * 0.5f);
            var maxZ = Mathf.FloorToInt(LotDepthMeters * 0.5f - package.DepthMeters * 0.5f);
            if (BuildingCount == 0 && CanOccupyBuilding(package, 0, 0, 0, -1))
            {
                cell = Vector2Int.zero;
                return true;
            }
            for (var x = minX; x <= maxX; x++)
            for (var z = minZ; z <= maxZ; z++)
            {
                if (!CanOccupyBuilding(package, x, z, 0, -1)) continue;
                cell = new Vector2Int(x, z);
                return true;
            }
            return false;
        }

        private bool CanOccupyBuilding(
            HybridBuildingPackage package,
            int cellX,
            int cellZ,
            int rotationQuarterTurns,
            int ignoredIndex)
        {
            // Placement inside the lot is intentionally permissive. Buildings,
            // trees, props, and sidewalks are authoring layers rather than hard
            // physics obstacles; overlap is a visual choice for the lot maker.
            return true;
        }

        public bool EndBuildingDrag()
        {
            if (!_buildingDragActive) return false;
            _buildingDragActive = false;
            if (_buildingAuthoringActive && !TopDownViewEnabled)
                InspectionMode = BuildingInspectionMode.Artwork;
            return true;
        }

        private bool TryLotPointFromPanel(
            Vector2 panelPosition,
            Vector2 panelSize,
            out Vector3 lotPoint)
        {
            lotPoint = default;
            if (_camera == null) return false;
            var pixel = PanelToCameraPixel(
                panelPosition,
                panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            var ray = _camera.ScreenPointToRay(pixel);
            var plane = new Plane(transform.up, transform.position);
            if (!plane.Raycast(ray, out var distance)) return false;
            lotPoint = transform.InverseTransformPoint(ray.GetPoint(distance));
            return true;
        }

        public static bool BuildingFootprintContains(
            Vector2 point,
            Vector2 center,
            float widthMeters,
            float depthMeters,
            int rotationQuarterTurns)
        {
            var oddTurn = Mathf.Abs(rotationQuarterTurns) % 2 == 1;
            var width = oddTurn ? depthMeters : widthMeters;
            var depth = oddTurn ? widthMeters : depthMeters;
            return Mathf.Abs(point.x - center.x) <= width * 0.5f &&
                   Mathf.Abs(point.y - center.y) <= depth * 0.5f;
        }

        public static Vector2 PanelToCameraPixel(
            Vector2 panelPosition,
            Vector2 panelSize,
            Vector2 cameraPixelSize)
        {
            if (panelSize.x <= 0f || panelSize.y <= 0f ||
                cameraPixelSize.x <= 0f || cameraPixelSize.y <= 0f)
                return Vector2.zero;
            var normalizedX = Mathf.Clamp01(panelPosition.x / panelSize.x);
            var normalizedY = Mathf.Clamp01(panelPosition.y / panelSize.y);
            return new Vector2(
                normalizedX * cameraPixelSize.x,
                (1f - normalizedY) * cameraPixelSize.y);
        }

        public bool RotateRoadPiece(int direction)
        {
            var placed = RoadPlacementModel.FindAt(
                _session.Data.RoadPieces, RoadCursorCell.x, RoadCursorCell.y);
            if (placed == null)
            {
                RoadRotationQuarterTurns = FiveBayHybridContract.WrapFacing(
                    RoadRotationQuarterTurns + direction);
                ApplyRoadCursor();
                NotifyStateChanged();
                return false;
            }
            PushRoadUndo(_session.Serialize());
            placed.RotationQuarterTurns = FiveBayHybridContract.WrapFacing(
                placed.RotationQuarterTurns + direction);
            RoadRotationQuarterTurns = placed.RotationQuarterTurns;
            SelectedRoadTopology = placed.Topology;
            _roadPackage = RoadPiecePackageCatalog.Resolve(placed.PackageId);
            RebuildRoadArtwork();
            RebuildRoadVehicleNetwork();
            ApplyRoadCursor();
            NotifyStateChanged();
            return true;
        }

        public bool PlaceRoadPiece()
        {
            if (!SelectedRoadPieceAvailable) return false;
            PushRoadUndo(_session.Serialize());
            PlaceRoadPieceInternal();
            NotifyStateChanged();
            return true;
        }

        private void PlaceRoadPieceInternal()
        {
            RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, SelectedRoadTopology,
                RoadCursorCell.x, RoadCursorCell.y, RoadRotationQuarterTurns,
                LotWidthMeters, LotDepthMeters, _roadPackage.Id);
            RoadPlacementModel.RepairConnectedTopologies(
                _session.Data.RoadPieces, _roadPackage, LotWidthMeters, LotDepthMeters);
            var repaired = RoadPlacementModel.FindAt(
                _session.Data.RoadPieces, RoadCursorCell.x, RoadCursorCell.y);
            if (repaired != null)
            {
                repaired.RoadMaterialId = _selectedRoadMaterialId;
                repaired.SidewalkMaterialId = _selectedSidewalkMaterialId;
                repaired.MarkingStyle = _selectedRoadMarkingStyle;
                repaired.LaneMarkingStyle = _selectedRoadLaneMarkingStyle;
                repaired.CenterMarkingStyle = _selectedRoadCenterMarkingStyle;
                SelectedRoadTopology = repaired.Topology;
                RoadRotationQuarterTurns = repaired.RotationQuarterTurns;
            }
            RebuildRoadArtwork();
            RebuildRoadVehicleNetwork();
        }

        public bool PaintRoadPiece(RoadPieceTopology topology)
        {
            SelectedRoadTopology = topology;
            return PlaceRoadPiece();
        }

        public bool DeleteRoadPiece()
        {
            var before = _session.Serialize();
            if (!RoadPlacementModel.DeleteAt(_session.Data.RoadPieces,
                RoadCursorCell.x, RoadCursorCell.y)) return false;
            PushRoadUndo(before);
            RoadPlacementModel.RepairConnectedTopologies(
                _session.Data.RoadPieces, _roadPackage, LotWidthMeters, LotDepthMeters);
            RebuildRoadArtwork();
            RebuildRoadVehicleNetwork();
            NotifyStateChanged();
            return true;
        }

        public bool EraseRoadCellFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!SelectRoadCellFromPanel(panelPosition, panelSize, false)) return false;
            var before = _session.Serialize();
            if (!RoadPlacementModel.DeleteAt(_session.Data.RoadPieces,
                RoadCursorCell.x, RoadCursorCell.y)) return false;
            PushRoadUndo(before);
            RoadPlacementModel.RepairConnectedTopologies(
                _session.Data.RoadPieces, _roadPackage, LotWidthMeters, LotDepthMeters);
            RebuildRoadArtwork();
            RebuildRoadVehicleNetwork();
            NotifyStateChanged();
            return true;
        }

        public bool PaintRoadStrokeCellFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!SelectedRoadPieceAvailable ||
                !SelectRoadCellFromPanel(panelPosition, panelSize, false)) return false;
            _roadStrokeStart ??= _session.Serialize();
            PlaceRoadPieceInternal();
            return true;
        }

        public bool TryCreateOutsideConnectorFromPanelDrag(
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            return TryCreateOutsideConnectorFromDrag(point.x, point.z);
        }

        public bool TryCreateOutsideConnectorFromDrag(float localX, float localZ)
        {
            var piece = RoadPlacementModel.FindAt(_session.Data.RoadPieces,
                RoadCursorCell.x, RoadCursorCell.y);
            if (piece == null || piece.Topology != RoadPieceTopology.Straight) return false;
            var package = RoadPiecePackageCatalog.Resolve(piece.PackageId);
            if (!package.AllowsVehicles || !TrafficLotModel.TryGetExteriorPort(piece,
                    package, LotWidthMeters, LotDepthMeters, out var edge)) return false;
            var crossed = edge switch
            {
                RoadPiecePort.North => localZ >= LotDepthMeters * 0.5f,
                RoadPiecePort.East => localX >= LotWidthMeters * 0.5f,
                RoadPiecePort.South => localZ < -LotDepthMeters * 0.5f,
                _ => localX < -LotWidthMeters * 0.5f
            };
            if (!crossed) return false;
            _session.Data.OutsideRoadConnectors ??= new List<OutsideRoadConnector>();
            var existing = _session.Data.OutsideRoadConnectors.Find(connector =>
                connector != null && connector.GridX == piece.GridX &&
                connector.GridZ == piece.GridZ && connector.Edge == edge);
            if (existing != null) return true;
            _roadStrokeStart ??= _session.Serialize();
            _session.Data.OutsideRoadConnectors.Add(new OutsideRoadConnector
            {
                Id = Guid.NewGuid().ToString("N"),
                GridX = piece.GridX,
                GridZ = piece.GridZ,
                Edge = edge,
                Flow = RoadTrafficFlow.TwoWay
            });
            RebuildOutsideConnectorMarkers();
            RebuildRoadVehicleNetwork();
            return true;
        }

        public void EndRoadPaintStroke()
        {
            if (_roadStrokeStart != null)
            {
                PushRoadUndo(_roadStrokeStart);
                _roadStrokeStart = null;
            }
            NotifyStateChanged();
        }

        public bool UndoRoadEdit()
        {
            if (_roadUndo.Count == 0) return false;
            _roadRedo.Push(_session.Serialize());
            RestoreRoadSnapshot(_roadUndo.Pop());
            return true;
        }

        public bool RedoRoadEdit()
        {
            if (_roadRedo.Count == 0) return false;
            _roadUndo.Push(_session.Serialize());
            RestoreRoadSnapshot(_roadRedo.Pop());
            return true;
        }

        private void PushRoadUndo(string snapshot)
        {
            _roadUndo.Push(snapshot);
            while (_roadUndo.Count > 40)
            {
                var retained = _roadUndo.ToArray();
                _roadUndo.Clear();
                for (var index = retained.Length - 2; index >= 0; index--)
                    _roadUndo.Push(retained[index]);
            }
            _roadRedo.Clear();
        }

        private void RestoreRoadSnapshot(string snapshot)
        {
            _session.Restore(snapshot);
            ClampRoadCursorToLot();
            RebuildRoadArtwork();
            RebuildRoadVehicleNetwork();
            ApplyRoadCursor();
            NotifyStateChanged();
        }

        public bool ApplyRoadSuggestion()
        {
            if (!RoadPlacementModel.TrySuggest(_session.Data.RoadPieces, RoadCursorCell.x,
                RoadCursorCell.y, _roadPackage, out var topology, out var turns)) return false;
            SelectedRoadTopology = topology;
            RoadRotationQuarterTurns = turns;
            ApplyRoadCursor();
            NotifyStateChanged();
            return true;
        }

        private void BuildLighting()
        {
            _sun = new GameObject("Time of Day Sun").AddComponent<Light>();
            _sun.transform.SetParent(transform);
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.72f;
            _sun.shadowBias = 0.035f;
            _sun.shadowNormalBias = 0.28f;
            _sun.cullingMask &= ~(1 << FloraShadowReceiverLayer);

            // Pub QA's approved projected-shadow compass can be rotated from
            // the beauty-light compass by package data. A dedicated light on
            // an isolated layer lets transparent flora receive the matching
            // native shadow map without changing the approved building art,
            // ground grade, or directional facade overlay.
            _floraShadowSun = new GameObject("Flora Shadow Alignment Sun")
                .AddComponent<Light>();
            _floraShadowSun.transform.SetParent(transform);
            _floraShadowSun.type = LightType.Directional;
            _floraShadowSun.color = Color.white;
            _floraShadowSun.intensity = 1f;
            _floraShadowSun.shadows = LightShadows.Soft;
            _floraShadowSun.shadowStrength = 0.72f;
            // Transparent flora exposes shadow-map texels more readily than the
            // opaque lot surface. Keep this isolated light crisp as props move
            // through a building edge without changing the approved world sun.
            _floraShadowSun.shadowCustomResolution = 4096;
            _floraShadowSun.shadowBias = 0.035f;
            _floraShadowSun.shadowNormalBias = 0.28f;
            _floraShadowSun.cullingMask = 1 << FloraShadowReceiverLayer;
            ApplyTimeOfDay();
        }

        private void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Lot Surface";
            ground.transform.SetParent(transform);
            _ground = ground.transform;
            ResizeGround();
            _groundRenderer = ground.GetComponent<Renderer>();
            _groundRenderer.material =
                ShadowReceivingLotMaterial(GroundColor);
            _groundRenderer.receiveShadows = true;
            ApplyTimeOfDay();
        }

        private void ResizeGround()
        {
            if (_ground == null) return;
            _ground.localScale = new Vector3(
                (LotWidthMeters + 4f) / 10f, 1f, (LotDepthMeters + 4f) / 10f);
            ResizeSnowGroundCover();
        }

        private void ClampRoadCursorToLot()
        {
            RoadCursorCell = new Vector2Int(
                Mathf.Clamp(RoadCursorCell.x,
                    RoadPlacementModel.MinimumCellForLot(LotWidthMeters),
                    RoadPlacementModel.MaximumCellForLot(LotWidthMeters)),
                Mathf.Clamp(RoadCursorCell.y,
                    RoadPlacementModel.MinimumCellForLot(LotDepthMeters),
                    RoadPlacementModel.MaximumCellForLot(LotDepthMeters)));
        }

        private static void ClearChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private void BuildGrid()
        {
            if (_minorGrid == null)
            {
                _minorGrid = new GameObject("Minor Grid — 1 Meter").transform;
                _minorGrid.SetParent(transform);
            }
            else ClearChildren(_minorGrid);
            if (_majorGrid == null)
            {
                _majorGrid = new GameObject("Major Grid — 10 Meters").transform;
                _majorGrid.SetParent(transform);
            }
            else ClearChildren(_majorGrid);
            var minorMaterial =
                LotSurfaceMaterial(new Color(0.72f, 0.76f, 0.65f, 0.42f), 1999);
            var majorMaterial =
                LotSurfaceMaterial(new Color(0.93f, 0.77f, 0.34f, 0.72f), 2000);

            for (var index = -LotWidthMeters / 2; index <= LotWidthMeters / 2; index++)
            {
                var major = IsMajorGridLine(index, LotWidthMeters);
                var root = major ? _majorGrid : _minorGrid;
                var material = major ? majorMaterial : minorMaterial;
                var width = major ? 0.055f : 0.018f;
                AddLine(root, new Vector3(index, 0.012f, -LotDepthMeters / 2f),
                    new Vector3(index, 0.012f, LotDepthMeters / 2f), material, width);
            }
            for (var index = -LotDepthMeters / 2; index <= LotDepthMeters / 2; index++)
            {
                var major = IsMajorGridLine(index, LotDepthMeters);
                var root = major ? _majorGrid : _minorGrid;
                var material = major ? majorMaterial : minorMaterial;
                var width = major ? 0.055f : 0.018f;
                AddLine(root, new Vector3(-LotWidthMeters / 2f, 0.012f, index),
                    new Vector3(LotWidthMeters / 2f, 0.012f, index), material, width);
            }
            ApplyZoomLevel();
        }

        public static bool IsMajorGridLine(int positionMeters, int lotSizeMeters)
        {
            var halfSize = Mathf.Clamp(lotSizeMeters, 10, 80) / 2;
            return (positionMeters + halfSize) % (int)LotMetricScale.MajorGridMeters == 0;
        }

        private void BuildNeighborhoodRoadSlice()
        {
            _neighborhoodRoad = new GameObject("Neighborhood Road Vertical Slice").transform;
            _neighborhoodRoad.SetParent(transform);
            var road = Cube("Internal Two-Lane Road", _neighborhoodRoad,
                new Vector3(0f, 0.025f, -2.5f), new Vector3(6f, 0.05f, 15f),
                new Color(0.12f, 0.13f, 0.14f));
            road.GetComponent<Collider>().enabled = false;
            road.GetComponent<Renderer>().enabled = false;
            _trafficVehicle = null;
            ApplyLotPlanningState();
        }

        private void BuildRoadArtworkSlice()
        {
            _roadPackage = RoadPiecePackageCatalog.Default;
            _roadArtworkRoot = new GameObject("Road Family Artwork").transform;
            _roadArtworkRoot.SetParent(transform);
            _outsideConnectorRoot = new GameObject("Outside Traffic Connectors").transform;
            _outsideConnectorRoot.SetParent(transform);
            _roadCursor = new GameObject("Road Placement Cursor").transform;
            _roadCursor.SetParent(transform);
            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "Selected Road Cell Highlight";
            fill.transform.SetParent(_roadCursor);
            fill.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            fill.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            fill.transform.localScale = new Vector3(9.65f, 9.65f, 1f);
            fill.GetComponent<Collider>().enabled = false;
            _roadCursorFill = fill.GetComponent<Renderer>();
            _roadCursorFill.sharedMaterial = LotSurfaceMaterial(
                new Color(1f, 0.67f, 0.08f, 0.12f), 2004);
            SeedRoadVerticalSlice();
            RebuildRoadArtwork();
            BuildStreetcarTrackLayer();
            RebuildOutsideConnectorMarkers();
            ApplyRoadCursor();
            ApplyLotPlanningState();
        }

        private void SeedRoadVerticalSlice()
        {
            if (_session.Data.RoadPieces.Count > 0) return;
            if (LotSizeMeters >= 80)
            {
                // A 6 x 6-cell circuit within the 8 x 8 neighborhood leaves a
                // one-cell editing apron and four straight cells per side.
                RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Corner, -3, -3, 2, LotSizeMeters);
                RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Corner, 2, -3, 1, LotSizeMeters);
                RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Corner, -3, 2, 3, LotSizeMeters);
                RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Corner, 2, 2, 0, LotSizeMeters);
                for (var cell = -2; cell <= 1; cell++)
                {
                    RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Straight, cell, -3, 1, LotSizeMeters);
                    RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Straight, cell, 2, 1, LotSizeMeters);
                    RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Straight, -3, cell, 0, LotSizeMeters);
                    RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Straight, 2, cell, 0, LotSizeMeters);
                }
                // West boundary access enters the main circuit at a governed
                // T-intersection. The outer straight publishes a lot port.
                RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces,
                    RoadPieceTopology.TJunction, -3, 0, 1, LotSizeMeters);
                RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces,
                    RoadPieceTopology.Straight, -4, 0, 1, LotSizeMeters);
                return;
            }
            RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Corner, -1, -1, 2);
            RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Corner, 0, -1, 1);
            RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Corner, -1, 0, 3);
            RoadPlacementModel.PlaceOrReplace(_session.Data.RoadPieces, RoadPieceTopology.Corner, 0, 0, 0);
        }

        private void RebuildRoadArtwork()
        {
            if (_roadArtworkRoot == null) return;
            for (var index = _roadArtworkRoot.childCount - 1; index >= 0; index--)
            {
                var child = _roadArtworkRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            foreach (var piece in _session.Data.RoadPieces)
            {
                var package = RoadPiecePackageCatalog.Resolve(piece.PackageId);
                AddRoadPiece(piece,
                    RoadArtworkCenter(piece, package),
                    piece.RotationQuarterTurns);
                AddRoadExperimentHighlight(piece);
            }
            RebuildStreetcarTracks();
        }

        private Vector2 RoadArtworkCenter(PlacedRoadPiece piece, RoadPiecePackage package)
        {
            var center = RoadPlacementModel.CellCenterMeters(
                piece.GridX, piece.GridZ, LotWidthMeters, LotDepthMeters);
            if (package == null || package.OccupancyCrossCells <= 1) return center;
            var offset = (package.OccupancyCrossCells - 1) *
                RoadPlacementModel.TileSizeMeters * 0.5f;
            return piece.RotationQuarterTurns % 2 == 0
                ? center + Vector2.right * offset
                : center + Vector2.up * offset;
        }

        private void AddRoadExperimentHighlight(PlacedRoadPiece piece)
        {
            if (_roadArtworkRoot == null) return;
            var center = RoadPlacementModel.CellCenterMeters(
                piece.GridX, piece.GridZ, LotWidthMeters, LotDepthMeters);
            var package = RoadPiecePackageCatalog.Resolve(piece.PackageId);
            center = RoadArtworkCenter(piece, package);
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Quad);
            receiver.name = $"Invisible Road Receiver {piece.GridX}, {piece.GridZ}";
            receiver.transform.SetParent(_roadArtworkRoot);
            receiver.transform.localPosition = new Vector3(center.x, 0.085f, center.y);
            receiver.transform.localRotation = Quaternion.Euler(
                90f, piece.RotationQuarterTurns * 90f, 0f);
            receiver.transform.localScale = new Vector3(
                package.ArtworkWidthMeters * 0.965f,
                package.ArtworkLengthMeters * 0.965f, 1f);
            receiver.GetComponent<Collider>().enabled = false;
            receiver.GetComponent<Renderer>().sharedMaterial = LotSurfaceMaterial(
                new Color(1f, 0.67f, 0.08f, 0f), 2003);
        }

        private void RebuildRoadVehicleNetwork()
        {
            RemoveTestVehicles();
            PruneOutsideConnectors();
            _session.Data.VehicleNetwork = RoadPlacementModel.BuildVehicleNetwork(
                _session.Data.RoadPieces,
                piece => RoadPiecePackageCatalog.Resolve(piece.PackageId),
                LotWidthMeters, LotDepthMeters);
            var trafficPackage = ResolveTrafficPackage();
            _trafficGraph = RoadTrafficGraph.FromRoadNetwork(
                _session.Data.VehicleNetwork, trafficPackage);
            _vehicleType ??= VehicleTypePackage.LoadModelT();
            _laneVehicleStates.Clear();
            ResetSuburbanTraffic();
            RebuildCirculationVisualization();
        }

        private RoadPiecePackage ResolveTrafficPackage()
        {
            RoadPiecePackage firstVehiclePackage = null;
            foreach (var piece in _session.Data.RoadPieces ?? new List<PlacedRoadPiece>())
            {
                if (piece == null) continue;
                var package = RoadPiecePackageCatalog.Resolve(piece.PackageId);
                if (!package.AllowsVehicles) continue;
                firstVehiclePackage ??= package;
                if (package.LaneCount >= 2) return package;
            }
            return firstVehiclePackage ?? _roadPackage;
        }

        private void PruneOutsideConnectors()
        {
            _session.Data.OutsideRoadConnectors ??= new List<OutsideRoadConnector>();
            _session.Data.OutsideRoadConnectors.RemoveAll(connector =>
                !ConnectorUsesVehicleRoad(connector) ||
                !TrafficLotModel.IsValidConnector(connector,
                    _session.Data.RoadPieces, _roadPackage,
                    LotWidthMeters, LotDepthMeters));
            RebuildOutsideConnectorMarkers();
        }

        private bool ConnectorUsesVehicleRoad(OutsideRoadConnector connector)
        {
            if (connector == null) return false;
            var piece = RoadPlacementModel.FindAt(_session.Data.RoadPieces,
                connector.GridX, connector.GridZ);
            return piece != null && RoadPiecePackageCatalog.Resolve(piece.PackageId).AllowsVehicles;
        }

        private void RebuildOutsideConnectorMarkers()
        {
            if (_outsideConnectorRoot == null) return;
            for (var index = _outsideConnectorRoot.childCount - 1; index >= 0; index--)
            {
                var child = _outsideConnectorRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            foreach (var connector in _session.Data.OutsideRoadConnectors ??
                     new List<OutsideRoadConnector>())
                AddOutsideConnectorMarker(connector);
        }

        private void AddOutsideConnectorMarker(OutsideRoadConnector connector)
        {
            var cell = RoadPlacementModel.CellCenterMeters(
                connector.GridX, connector.GridZ, LotWidthMeters, LotDepthMeters);
            var outward = connector.Edge switch
            {
                RoadPiecePort.North => Vector2.up,
                RoadPiecePort.East => Vector2.right,
                RoadPiecePort.South => Vector2.down,
                _ => Vector2.left
            };
            var color = connector.Flow switch
            {
                RoadTrafficFlow.InboundOnly => new Color(0.20f, 0.88f, 1f),
                RoadTrafficFlow.OutboundOnly => new Color(1f, 0.42f, 0.22f),
                _ => new Color(0.18f, 0.95f, 0.42f)
            };
            var root = new GameObject($"Outside Connector {connector.Flow}").transform;
            root.SetParent(_outsideConnectorRoot, false);
            root.localPosition = new Vector3(cell.x + outward.x * 4.2f, 0.16f,
                cell.y + outward.y * 4.2f);
            AddConnectorArrow(root, connector.Flow != RoadTrafficFlow.InboundOnly
                ? outward : -outward, color, "Outbound");
            if (connector.Flow == RoadTrafficFlow.TwoWay)
            {
                var inbound = new GameObject("Inbound").transform;
                inbound.SetParent(root, false);
                inbound.localPosition = new Vector3(-outward.y * 1.1f, 0f, outward.x * 1.1f);
                AddConnectorArrow(inbound, -outward, color, "Inbound");
            }
        }

        private void AddConnectorArrow(Transform parent, Vector2 direction, Color color, string name)
        {
            var yaw = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            var arrow = new GameObject(name).transform;
            arrow.SetParent(parent, false);
            arrow.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var stem = Cube("Stem", arrow, new Vector3(0f, 0f, 0f),
                new Vector3(0.32f, 0.16f, 2.8f), color);
            stem.GetComponent<Collider>().enabled = false;
            foreach (var side in new[] { -1f, 1f })
            {
                var head = Cube("Arrow Head", arrow,
                    new Vector3(side * 0.48f, 0f, 1.25f),
                    new Vector3(0.24f, 0.16f, 1.25f), color);
                head.transform.localRotation = Quaternion.Euler(0f, side * 42f, 0f);
                head.GetComponent<Collider>().enabled = false;
            }
        }

        private void ApplyRoadCursor()
        {
            if (_roadCursor == null) return;
            _roadCursor.gameObject.SetActive(RoadCursorSelected);
            var center = RoadPlacementModel.CellCenterMeters(
                RoadCursorCell.x, RoadCursorCell.y, LotWidthMeters, LotDepthMeters);
            _roadCursor.localPosition = new Vector3(center.x, 0.045f, center.y);
            _roadCursor.localRotation = Quaternion.Euler(0f, RoadRotationQuarterTurns * 90f, 0f);
        }

        private void AddRoadPiece(PlacedRoadPiece placed, Vector2 positionMeters, int quarterTurns)
        {
            var package = RoadPiecePackageCatalog.Resolve(placed.PackageId);
            var topology = placed.Topology;
            var piece = package.Piece(topology);
            if (piece == null || !piece.HasArtwork) return;
            var markingSuffix = package.SupportsIndependentMarkings
                ? $"-{(placed.LaneMarkingStyle == RoadLaneMarkingStyle.Lines ? "lanes" : "no-lanes")}-{(placed.CenterMarkingStyle == RoadCenterMarkingStyle.DoubleLines ? "double-center" : "no-center")}"
                : placed.MarkingStyle switch
            {
                RoadMarkingStyle.NoLines => "-no-lines",
                RoadMarkingStyle.DoubleLines => "-double-lines",
                _ => "-single-dotted"
            };
            var texturePath = package.Id == RoadPiecePackage.LegacyPackageId
                ? piece.ResourcePath : piece.ResourcePath + markingSuffix;
            var texture = Resources.Load<Texture2D>(texturePath);
            if (texture == null) texture = Resources.Load<Texture2D>(piece.ResourcePath);
            if (texture == null) return;
            var roadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            roadObject.name = $"{package.DisplayName} {topology}";
            roadObject.transform.SetParent(_roadArtworkRoot);
            roadObject.transform.localPosition = new Vector3(positionMeters.x, 0.052f, positionMeters.y);
            roadObject.transform.localRotation = Quaternion.Euler(90f, quarterTurns * 90f, 0f);
            roadObject.transform.localScale = new Vector3(
                package.ArtworkWidthMeters, package.ArtworkLengthMeters, 1f);
            roadObject.GetComponent<Collider>().enabled = false;
            var shader = Shader.Find("CityForgeV3/ShadowReceivingRoadOverlay");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            var material = new Material(shader)
            {
                name = $"{package.DisplayName} {topology} Overlay",
                mainTexture = texture,
                // Roads are ground color. They must draw before invisible
                // prop/building depth prepasses (2435/2440), otherwise those
                // prepasses punch rectangular holes in exposed road pixels.
                renderQueue = 2430
            };
            if (package.Id != RoadPiecePackage.LegacyPackageId)
            {
                material.SetFloat("_UseMaterialZones", 1f);
                material.SetTexture("_RoadSurfaceTex",
                    RoadMaterialCatalog.Resolve(placed.RoadMaterialId).LoadTexture());
                material.SetTexture("_SidewalkSurfaceTex",
                    RoadMaterialCatalog.Resolve(placed.SidewalkMaterialId, true).LoadTexture());
                material.SetFloat("_MaterialTiling",
                    RoadMaterialCatalog.Resolve(placed.RoadMaterialId).TilesPerTenMeters);
                material.SetFloat("_RoadMaterialTiling",
                    RoadMaterialCatalog.Resolve(placed.RoadMaterialId).TilesPerTenMeters);
                material.SetFloat("_SidewalkMaterialTiling",
                    RoadMaterialCatalog.Resolve(placed.SidewalkMaterialId, true).TilesPerTenMeters);
            }
            roadObject.GetComponent<Renderer>().sharedMaterial = material;
            if (material.HasProperty("_TimeTint"))
                material.SetColor("_TimeTint",
                    TimeOfDayLighting.For(TimeOfDay).NeutralArtworkTint);
        }

        private void BuildCirculationEditor()
        {
            _circulationRoot = new GameObject("Circulation Networks").transform;
            _circulationRoot.SetParent(transform);
            _circulationTravelerRoot = new GameObject("Circulation Travelers").transform;
            _circulationTravelerRoot.SetParent(transform);
            _circulationCursor = Cube("Circulation Cursor", transform,
                new Vector3(0f, 0.16f, 0f), new Vector3(0.65f, 0.18f, 0.65f),
                new Color(1f, 0.78f, 0.18f)).transform;
            _circulationCursor.GetComponent<Collider>().enabled = false;
            CirculationDefaults.SeedVerticalSlice(_session.Data);
            RebuildRoadVehicleNetwork();
            RebuildCirculationVisualization();
            ApplyCirculationCursor();
        }

        private void RebuildCirculationVisualization()
        {
            if (_circulationRoot == null) return;
            for (var index = _circulationRoot.childCount - 1; index >= 0; index--)
            {
                var child = _circulationRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            if (_circulationTravelerRoot != null)
            {
                for (var index = _circulationTravelerRoot.childCount - 1; index >= 0; index--)
                {
                    var child = _circulationTravelerRoot.GetChild(index).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }
            _vehiclePresentations.Clear();
            var pedestrianMaterial = LotSurfaceMaterial(new Color(0.96f, 0.30f, 0.66f, 0.92f), 2010);
            var vehicleMaterial = LotSurfaceMaterial(new Color(0.18f, 0.92f, 0.42f, 0.92f), 2011);
            BuildNetworkVisualization(_session.Data.PedestrianNetwork, pedestrianMaterial);
            BuildTrafficLaneVisualization(vehicleMaterial);
            _pedestrianTraveler = BuildTraveler("Pedestrian Traveler", new Color(1f, 0.42f, 0.76f), 0.36f);
            foreach (var variant in new[]
            {
                VehiclePaintVariant.Green,
                VehiclePaintVariant.Blue,
                VehiclePaintVariant.Red,
                VehiclePaintVariant.Yellow
            })
            {
                var presentation = VehicleRuntimePresentation.Create(
                    _circulationTravelerRoot, variant);
                presentation.gameObject.name = $"Vehicle Traveler — {variant}";
                presentation.SetTimeOfDay(TimeOfDay);
                _vehiclePresentations.Add(presentation);
            }
            _vehiclePresentation = _vehiclePresentations[0];
            _vehicleTraveler = _vehiclePresentation.transform;
            _laneVehicleStates.AddRange(LaneTrafficModel.Seed(
                _vehiclePresentations.Count, _trafficGraph, _vehicleType));
            PlaceVehicleAtRouteDistance();
            _circulationRoot.gameObject.SetActive(CirculationDiagnosticsVisible);
        }

        private void BuildNetworkVisualization(CirculationNetwork network, Material material)
        {
            foreach (var segment in network.Segments)
            {
                var start = network.FindNode(segment.StartNodeId);
                var end = network.FindNode(segment.EndNodeId);
                if (start == null || end == null) continue;
                AddLine(_circulationRoot,
                    new Vector3(start.PositionMeters.x, 0.09f, start.PositionMeters.y),
                    new Vector3(end.PositionMeters.x, 0.09f, end.PositionMeters.y),
                    material, network.Mode == CirculationMode.Vehicle ? 0.16f : 0.10f);
            }
            foreach (var node in network.Nodes)
            {
                var marker = Cube($"{network.Mode} Node — {node.Id}", _circulationRoot,
                    new Vector3(node.PositionMeters.x, 0.14f, node.PositionMeters.y),
                    new Vector3(0.36f, 0.24f, 0.36f), material.color);
                marker.GetComponent<Collider>().enabled = false;
            }
        }

        private void BuildTrafficLaneVisualization(Material material)
        {
            if (_trafficGraph == null) return;
            var colors = new[]
            {
                new Color(0.18f, 0.92f, 0.42f, 0.92f),
                new Color(0.22f, 0.68f, 1f, 0.92f)
            };
            for (var laneIndex = 0; laneIndex < _trafficGraph.Routes.Count; laneIndex++)
            {
                var route = _trafficGraph.Routes[laneIndex];
                var laneMaterial = new Material(material) { color = colors[laneIndex % colors.Length] };
                for (var index = 0; index < route.Points.Count; index++)
                {
                    var start = route.Points[index];
                    var end = route.Points[(index + 1) % route.Points.Count];
                    AddLine(_circulationRoot,
                        new Vector3(start.x, 0.10f, start.y),
                        new Vector3(end.x, 0.10f, end.y), laneMaterial, 0.12f);
                    if (index % 4 != 0) continue;
                    var direction = (end - start).normalized;
                    var marker = Cube($"Lane {laneIndex + 1} Direction", _circulationRoot,
                        new Vector3(start.x, 0.14f, start.y),
                        new Vector3(0.24f, 0.12f, 0.62f), colors[laneIndex % colors.Length]);
                    marker.transform.localRotation = Quaternion.LookRotation(
                        new Vector3(direction.x, 0f, direction.y), Vector3.up);
                    marker.GetComponent<Collider>().enabled = false;
                }
            }
            foreach (var intersection in _trafficGraph.Intersections)
            {
                var marker = Cube($"{intersection.MinorApproachControl} T-intersection",
                    _circulationRoot,
                    new Vector3(intersection.PositionMeters.x, 0.18f,
                        intersection.PositionMeters.y),
                    new Vector3(0.72f, 0.20f, 0.72f),
                    new Color(1f, 0.58f, 0.10f));
                marker.GetComponent<Collider>().enabled = false;
            }
        }

        private Transform BuildTraveler(string name, Color color, float size)
        {
            var traveler = Cube(name, _circulationTravelerRoot, Vector3.up * 0.42f,
                new Vector3(size, size, size), color, true);
            traveler.GetComponent<Collider>().enabled = false;
            return traveler.transform;
        }

        private void ApplyCirculationCursor()
        {
            if (_circulationCursor == null) return;
            _circulationCursor.gameObject.SetActive(
                _circulationEditorActive && CirculationCursorSelected);
            _circulationCursor.position = new Vector3(
                CirculationCursorMeters.x, 0.16f, CirculationCursorMeters.y);
            _circulationCursor.GetComponent<Renderer>().sharedMaterial.color =
                CirculationMode == CirculationMode.Pedestrian
                    ? new Color(1f, 0.34f, 0.74f)
                    : new Color(0.24f, 1f, 0.46f);
        }

        private void BuildProxyBuilding()
        {
            var proxyAsset = Resources.Load<GameObject>(
                _buildingPackage.PrimitiveResourcePath);
            if (proxyAsset == null)
            {
                throw new MissingReferenceException(
                    $"Missing building proxy resource '{_buildingPackage.PrimitiveResourcePath}'.");
            }

            var instance = Instantiate(proxyAsset, transform);
            instance.name =
                $"Hybrid Building Spatial Proxy — {_buildingPackage.PrimitiveSourceVersion}";
            _proxy = instance.transform;
            _proxy.localPosition = Vector3.zero;
            _proxy.localRotation = Quaternion.identity;

            // Primitive geometry is a package contract. Never assume that a
            // new building shares the Colonial proxy's gable roof (the Empire
            // tower, for example, declares CF_PROXY_ROOF_FLAT).
            foreach (var objectName in _buildingPackage.RequiredPrimitiveObjects)
            {
                var isAnchor = objectName.Contains("ANCHOR");
                var color = objectName.Contains("FOUNDATION")
                    ? new Color(0.19f, 0.72f, 0.83f, 0.48f)
                    : objectName.Contains("WALL")
                        ? new Color(0.15f, 0.55f, 0.74f, 0.16f)
                        : objectName.Contains("ROOF")
                            ? new Color(0.36f, 0.42f, 0.78f, 0.26f)
                            : new Color(1f, 0.72f, 0.12f, 0.92f);
                var renderQueue = isAnchor ? 3104 : 2101;
                ApplyProxyMaterial(objectName, color, renderQueue);
            }

            _proxyRenderers = _proxy.GetComponentsInChildren<Renderer>();
            _proxyMeshFilters = _proxy.GetComponentsInChildren<MeshFilter>();
            CaptureProxyLocalVertices();
            foreach (var renderer in _proxyRenderers)
            {
                renderer.gameObject.layer = FloraShadowReceiverLayer;
                renderer.enabled = false;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode =
                    renderer.gameObject.name == "CF_ANCHOR_ENTRANCE"
                        ? UnityEngine.Rendering.ShadowCastingMode.Off
                        : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            var depthShader = Shader.Find("CityForgeV3/BuildingDepthOccluder");
            if (depthShader == null)
                throw new MissingReferenceException(
                    "CityForge V3 building depth occluder shader is required.");
            var depthMaterial = new Material(depthShader)
            {
                name = "CF Invisible Building Depth Occluder"
            };
            ConfigureBuildingDepthStencil(
                depthMaterial, _session.SelectedBuildingIndex);
            var depthInstance = Instantiate(proxyAsset, transform);
            depthInstance.name = "Hybrid Building Depth Occluder";
            _buildingDepthOccluder = depthInstance.transform;
            _buildingDepthOccluder.localPosition = Vector3.zero;
            _buildingDepthOccluder.localRotation = Quaternion.identity;
            foreach (var collider in depthInstance.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            _buildingDepthOccluderRenderers =
                depthInstance.GetComponentsInChildren<Renderer>();
            foreach (var renderer in _buildingDepthOccluderRenderers)
            {
                renderer.sharedMaterial = depthMaterial;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.enabled = IsPerceptualBuildingOccluder(renderer);
            }
        }

        private static bool IsPerceptualBuildingOccluder(Renderer renderer)
        {
            if (renderer == null) return false;
            var objectName = renderer.gameObject.name;
            // Foundations own placement, selection, occupancy, and grounding.
            // They are deliberately excluded from the visual depth mask: an
            // oblique camera ray can encounter the broad ground slab before a
            // billboard's canopy and incorrectly hide the entire tree. Only
            // authored above-ground massing may occlude presentation pixels.
            return objectName.Contains("WALL") || objectName.Contains("ROOF");
        }

        private void CaptureProxyLocalVertices()
        {
            _proxyLocalVertices.Clear();
            if (_proxy == null || _proxyMeshFilters == null) return;

            foreach (var filter in _proxyMeshFilters)
            {
                if (filter == null || filter.sharedMesh == null ||
                    filter.gameObject.name == "CF_ANCHOR_ENTRANCE")
                    continue;
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    var world = filter.transform.TransformPoint(vertex);
                    _proxyLocalVertices.Add(_proxy.InverseTransformPoint(world));
                }
            }
        }

        private void BuildProjectedShadow()
        {
            _projectedShadow = CreateProjectedShadow(
                "Primitive Projected Shadow", _buildingPackage);
            UpdateProjectedShadow();
        }

        private Transform CreateProjectedShadow(
            string name, HybridBuildingPackage package)
        {
            if (package.UsesMeshProjectedShadow)
            {
                var proxyAsset = Resources.Load<GameObject>(
                    package.PrimitiveResourcePath);
                var shader = Shader.Find(
                    "CityForgeV3/ProjectedBuildingMeshShadow");
                if (proxyAsset == null || shader == null)
                    throw new MissingReferenceException(
                        "Mesh-projected building shadow requires its proxy and projected shadow shader.");
                var instance = Instantiate(proxyAsset, transform, false);
                instance.name = name;
                foreach (var collider in instance.GetComponentsInChildren<Collider>())
                    collider.enabled = false;
                foreach (var meshRenderer in instance.GetComponentsInChildren<Renderer>())
                {
                    var isAnchor = meshRenderer.gameObject.name.Contains("ANCHOR");
                    meshRenderer.enabled = !isAnchor;
                    meshRenderer.sharedMaterial = new Material(shader)
                    {
                        name = $"{name} Material"
                    };
                    meshRenderer.receiveShadows = false;
                    meshRenderer.shadowCastingMode =
                        UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                return instance.transform;
            }

            var shadowObject = new GameObject(name);
            shadowObject.transform.SetParent(transform, false);
            shadowObject.AddComponent<MeshFilter>();
            var projectedRenderer = shadowObject.AddComponent<MeshRenderer>();
            projectedRenderer.material = LotSurfaceMaterial(
                new Color(0.035f, 0.042f, 0.050f, 0.24f), 2001);
            projectedRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            projectedRenderer.receiveShadows = false;
            return shadowObject.transform;
        }

        private void UpdateProjectedShadow()
        {
            if (_projectedShadow == null || _proxy == null ||
                _buildingPackage == null)
            {
                return;
            }

            UpdateProjectedShadow(
                _projectedShadow,
                _buildingPackage,
                _proxy.position,
                _proxy.rotation,
                HasBuilding,
                true,
                _proxyLocalVertices);
        }

        private void UpdateProjectedShadow(
            Transform shadow,
            HybridBuildingPackage package,
            Vector3 buildingPosition,
            Quaternion buildingRotation,
            bool hasBuilding,
            bool publishDiagnostics,
            IReadOnlyList<Vector3> proxyVertices = null)
        {
            if (shadow == null || package == null)
                return;

            var rayDirection =
                TimeOfDayLighting.SunRotation(TimeOfDay) * Vector3.forward;
            // The lot surface is deliberately unlit, so it needs this
            // receiver-compatible projection even when a ShadowsOnly proxy is
            // also active for lit receivers. Its geometry comes exclusively
            // from the current package's spatial contract below.
            var visible = hasBuilding && !IsRaining && rayDirection.y < -0.01f &&
                TimeOfDay != TimeOfDayPreset.Night;
            shadow.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            shadow.position = buildingPosition;
            shadow.rotation = buildingRotation;

            var shadowColor = ProjectedShadowColor(TimeOfDay);
            shadowColor.a *= BuildingShadowOpacityMultiplier(TimeOfDay) *
                package.ShadowOpacityMultiplier;
            if (package.UsesMeshProjectedShadow)
            {
                UpdateMeshProjectedShadow(
                    shadow, package, buildingPosition, buildingRotation,
                    rayDirection, shadowColor);
                if (publishDiagnostics)
                    ProjectedShadowSourceVertexCount =
                        proxyVertices?.Count ?? 0;
                return;
            }
            shadow.GetComponent<MeshRenderer>().sharedMaterial.color =
                shadowColor;

            var localRay = Quaternion.Inverse(buildingRotation) * rayDirection;
            // Some authored lot presentations use a rotated visual compass.
            // Keep that registration in package data instead of asset-ID code.
            localRay = Quaternion.Euler(
                0f, package.ShadowDirectionOffsetDegrees, 0f) * localRay;
            var localHorizontal = new Vector2(localRay.x, localRay.z) *
                package.ShadowLengthScale(TimeOfDay);
            var maximumProjection =
                package.MaximumShadowProjectionMeters;
            var primitivePoints = proxyVertices != null && proxyVertices.Count > 0
                ? new List<Vector3>(proxyVertices)
                : SemanticPrimitiveVertices(package);
            var casterHeight = package.HeightMeters;
            if (package.ShadowSemanticVertices.Count >= 12 &&
                primitivePoints.Count > 0)
            {
                casterHeight = 0f;
                foreach (var point in primitivePoints)
                    casterHeight = Mathf.Max(casterHeight, point.y);
            }
            if (maximumProjection > 0f && casterHeight > 0f)
            {
                var maximumHorizontalMagnitude = Mathf.Abs(localRay.y) *
                    maximumProjection / casterHeight;
                localHorizontal = Vector2.ClampMagnitude(
                    localHorizontal, maximumHorizontalMagnitude);
            }
            localRay = new Vector3(
                localHorizontal.x,
                localRay.y,
                localHorizontal.y);
            if (publishDiagnostics)
                ProjectedShadowLocalDirection =
                    localHorizontal.sqrMagnitude > 0.000001f
                        ? localHorizontal.normalized
                        : Vector2.zero;

            var projectedPoints = new List<Vector2>();
            // Project a stable semantic building volume.  The imported proxy
            // meshes are authored in several nested coordinate spaces; using
            // their raw vertices here produced a mathematically non-empty hull
            // that could wind up outside the visible lot.  The package spatial
            // contract is the authoritative runtime footprint.
            if (publishDiagnostics)
                ProjectedShadowSourceVertexCount = primitivePoints.Count;
            foreach (var primitivePoint in primitivePoints)
            {
                if (primitivePoint.y <= 0.001f)
                {
                    projectedPoints.Add(
                        new Vector2(primitivePoint.x, primitivePoint.z));
                    continue;
                }
                var distance = -primitivePoint.y / localRay.y;
                var projected = primitivePoint + localRay * distance;
                projectedPoints.Add(new Vector2(projected.x, projected.z));
            }

            var hull = ConvexHull(projectedPoints);
            var vertices = new List<Vector3>(hull.Count);
            foreach (var point in hull)
                vertices.Add(new Vector3(point.x, 0.018f, point.y));
            var triangles = new List<int>();
            for (var index = 1; index < hull.Count - 1; index++)
            {
                triangles.Add(0);
                triangles.Add(index);
                triangles.Add(index + 1);
            }

            var shadowMesh = new Mesh { name = "Projected Proxy Shadow Mesh" };
            shadowMesh.SetVertices(vertices);
            shadowMesh.SetTriangles(triangles, 0);
            shadowMesh.RecalculateBounds();
            shadow.GetComponent<MeshFilter>().sharedMesh = shadowMesh;
        }

        private void UpdateMeshProjectedShadow(
            Transform shadow,
            HybridBuildingPackage package,
            Vector3 buildingPosition,
            Quaternion buildingRotation,
            Vector3 rayDirection,
            Color shadowColor)
        {
            var localRay = Quaternion.Inverse(buildingRotation) * rayDirection;
            localRay = Quaternion.Euler(
                0f, package.ShadowDirectionOffsetDegrees, 0f) * localRay;
            var localHorizontal = new Vector2(localRay.x, localRay.z) *
                package.ShadowLengthScale(TimeOfDay);
            if (package.MaximumShadowProjectionMeters > 0f &&
                package.HeightMeters > 0f)
            {
                var maximumHorizontalMagnitude = Mathf.Abs(localRay.y) *
                    package.MaximumShadowProjectionMeters /
                    package.HeightMeters;
                localHorizontal = Vector2.ClampMagnitude(
                    localHorizontal, maximumHorizontalMagnitude);
            }
            localRay = new Vector3(
                localHorizontal.x, localRay.y, localHorizontal.y);
            ProjectedShadowLocalDirection =
                localHorizontal.sqrMagnitude > 0.000001f
                    ? localHorizontal.normalized
                    : Vector2.zero;
            var worldRay = buildingRotation * localRay;
            var displacement = Mathf.Abs(localRay.y) > 0.0001f
                ? new Vector3(worldRay.x, 0f, worldRay.z) *
                  (package.HeightMeters / -localRay.y)
                : Vector3.zero;
            foreach (var renderer in shadow.GetComponentsInChildren<Renderer>())
            {
                if (renderer.gameObject.name.Contains("ANCHOR")) continue;
                var material = renderer.sharedMaterial;
                material.SetColor("_Color", shadowColor);
                material.SetVector("_ShadowDisplacement", displacement);
                material.SetFloat("_GroundY", buildingPosition.y + 0.018f);
                material.SetFloat("_ReferenceHeight", package.HeightMeters);
            }
        }

        private List<Vector3> SemanticPrimitiveVertices(
            HybridBuildingPackage package)
        {
            var points = new List<Vector3>();
            var authored = package.ShadowSemanticVertices;
            if (authored != null && authored.Count >= 12 && authored.Count % 3 == 0)
            {
                for (var index = 0; index < authored.Count; index += 3)
                    points.Add(new Vector3(
                        authored[index], authored[index + 1], authored[index + 2]));
                return points;
            }
            SemanticShadowFootprint(package,
                out var minX, out var maxX, out var minZ, out var maxZ);
            var flatRoof = SemanticPrimitiveHasFlatRoof(package);
            var eaveHeight = flatRoof
                ? package.HeightMeters
                : package.HeightMeters * 0.74f;
            foreach (var x in new[] { minX, maxX })
            foreach (var z in new[] { minZ, maxZ })
            {
                points.Add(new Vector3(x, 0f, z));
                points.Add(new Vector3(x, eaveHeight, z));
            }

            // A flat-roof semantic proxy is a rectangular prism. Do not add
            // the legacy ridge vertices merely because older manifests still
            // carry a roofRidgeAxis value for schema compatibility.
            if (flatRoof)
                return points;

            var ridgeAlongX = package.RoofRidgeAxis == "x";
            points.Add(ridgeAlongX
                ? new Vector3(minX, package.HeightMeters, 0f)
                : new Vector3(0f, package.HeightMeters, minZ));
            points.Add(ridgeAlongX
                ? new Vector3(maxX, package.HeightMeters, 0f)
                : new Vector3(0f, package.HeightMeters, maxZ));
            return points;
        }

        private static bool SemanticPrimitiveHasFlatRoof(
            HybridBuildingPackage package)
        {
            foreach (var objectName in package.RequiredPrimitiveObjects)
            {
                if (!string.IsNullOrWhiteSpace(objectName) &&
                    objectName.IndexOf(
                        "ROOF_FLAT",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private void SemanticShadowFootprint(
            HybridBuildingPackage package,
            out float minX,
            out float maxX,
            out float minZ,
            out float maxZ)
        {
            // The package dimensions describe the structural footprint, while
            // the presentation artwork also includes façade projections such
            // as pilasters, cornices, and awnings. Give the semantic shadow a
            // small, symmetric allowance so it visually covers that artwork.
            const float artworkCoverage = 1.15f;
            var halfWidth = package.WidthMeters * 0.5f *
                package.ShadowFootprintScale * artworkCoverage;
            var halfDepth = package.DepthMeters * 0.5f *
                package.ShadowFootprintScale * artworkCoverage;
            minX = -halfWidth;
            maxX = halfWidth;
            minZ = -halfDepth;
            maxZ = halfDepth;
        }

        private static Color ProjectedShadowColor(TimeOfDayPreset preset) =>
            preset switch
            {
                TimeOfDayPreset.Noon =>
                    new Color(0.025f, 0.032f, 0.038f, 0.20f),
                TimeOfDayPreset.Evening =>
                    new Color(0.035f, 0.042f, 0.050f, 0.28f),
                TimeOfDayPreset.Morning or TimeOfDayPreset.Afternoon =>
                    new Color(0.028f, 0.034f, 0.040f, 0.175f),
                _ => new Color(0.028f, 0.034f, 0.040f, 0.25f)
            };

        public static float ShadowLengthScale(TimeOfDayPreset preset) =>
            preset switch
            {
                TimeOfDayPreset.Morning => 0.35f,
                TimeOfDayPreset.Noon => 0.45f,
                TimeOfDayPreset.Afternoon => 0.50f,
                TimeOfDayPreset.Evening => 0.32f,
                _ => 0.45f
            };

        public static float BuildingShadowLengthScale(TimeOfDayPreset preset) =>
            preset switch
            {
                TimeOfDayPreset.Morning => 0.90f,
                TimeOfDayPreset.Noon => 0.45f,
                TimeOfDayPreset.Afternoon => 1.15f,
                TimeOfDayPreset.Evening => 0.65f,
                _ => 0.45f
            };

        public static float BuildingShadowOpacityMultiplier(
            TimeOfDayPreset preset) =>
            PropShadowOpacityMultiplier(preset) * (preset switch
            {
                // Joe review: building shadows need more weight against the
                // substantially darker native tree shadows. Keep this scoped
                // to buildings so prop and tree appearance remains unchanged.
                TimeOfDayPreset.Morning => 1.8125f,   // +25% from 1.45
                TimeOfDayPreset.Afternoon => 3.0f,
                _ => 1.45f
            });

        private static List<Vector2> ConvexHull(List<Vector2> points)
        {
            points.Sort((a, b) =>
            {
                var x = a.x.CompareTo(b.x);
                return x != 0 ? x : a.y.CompareTo(b.y);
            });
            if (points.Count <= 1) return points;
            var hull = new List<Vector2>();
            foreach (var point in points)
            {
                while (hull.Count >= 2 && Cross(
                    hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            var lowerCount = hull.Count;
            for (var index = points.Count - 2; index >= 0; index--)
            {
                var point = points[index];
                while (hull.Count > lowerCount && Cross(
                    hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static float Cross(Vector2 origin, Vector2 a, Vector2 b) =>
            (a.x - origin.x) * (b.y - origin.y) -
            (a.y - origin.y) * (b.x - origin.x);

        private void ApplyProxyMaterial(
            string objectName,
            Color color,
            int renderQueue)
        {
            var child = FindDescendant(_proxy, objectName);
            if (child == null)
            {
                throw new MissingReferenceException(
                    $"Building proxy '{_buildingPackage.PrimitiveSourceVersion}' " +
                    $"is missing required object '{objectName}'.");
            }

            var renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = LotSurfaceMaterial(color, renderQueue);
            }

            // Semantic anchors are transforms, not geometry. Only spatial
            // volumes need a mesh collider; an empty entrance anchor is a
            // valid Blender export and must not abort artwork construction.
            var meshFilter = child.GetComponent<MeshFilter>();
            if (child.GetComponent<Collider>() == null &&
                meshFilter != null && meshFilter.sharedMesh != null)
            {
                child.gameObject.AddComponent<MeshCollider>();
            }
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private void BuildHybridPresentation()
        {
            var presentation = new GameObject("Five-Bay Directional Presentation");
            presentation.transform.SetParent(transform);
            _presentation = presentation.AddComponent<HybridBuildingPresentation>();
            _presentation.Build(_camera, _buildingPackage);
            ApplyPresentationAppearance();
        }

        private void BuildSelectionFootprint()
        {
            var selection = new GameObject("Selected Building Footprint Outline");
            selection.transform.SetParent(transform);
            var material = LotSurfaceMaterial(
                new Color(0.25f, 0.82f, 0.90f, 0.78f), 2002);
            // Mesh-traced buildings can include eaves, stairs, towers, and other
            // silhouette details beyond the nominal foundation dimensions. Keep
            // their selection outline clear of the artwork instead of letting the
            // cyan line peek through along the foundation edge.
            var selectionPadding = _buildingPackage.UsesMeshProjectedShadow
                ? 0.9f
                : 0.3f;
            var halfWidth = (_buildingPackage.WidthMeters + selectionPadding) * 0.5f;
            var halfDepth = (_buildingPackage.DepthMeters + selectionPadding) * 0.5f;
            const float height = 0.035f;
            AddDiagnosticLine(selection.transform, "Selection South",
                new Vector3(-halfWidth, height, -halfDepth),
                new Vector3(halfWidth, height, -halfDepth), material);
            AddDiagnosticLine(selection.transform, "Selection East",
                new Vector3(halfWidth, height, -halfDepth),
                new Vector3(halfWidth, height, halfDepth), material);
            AddDiagnosticLine(selection.transform, "Selection North",
                new Vector3(halfWidth, height, halfDepth),
                new Vector3(-halfWidth, height, halfDepth), material);
            AddDiagnosticLine(selection.transform, "Selection West",
                new Vector3(-halfWidth, height, halfDepth),
                new Vector3(-halfWidth, height, -halfDepth), material);
            _selectionFootprint = selection.transform;
            BuildSelectedBuildingFrontMarker(selection.transform, material);
            _selectionFootprint.gameObject.SetActive(false);
        }

        private void BuildSelectedBuildingFrontMarker(
            Transform selection,
            Material material)
        {
            var marker = new GameObject(
                "Selected Building Authored Front Direction").transform;
            marker.SetParent(selection, false);
            marker.localRotation = Quaternion.Euler(
                0f, _buildingPackage.EntranceFacingDegrees, 0f);
            var packageDirection = AuthoredBuildingFrontDirection(
                _buildingPackage, 0);
            var edgeDistance = Mathf.Abs(packageDirection.x) *
                _buildingPackage.WidthMeters * 0.5f +
                Mathf.Abs(packageDirection.z) *
                _buildingPackage.DepthMeters * 0.5f;
            const float markerHeight = 0.22f;
            var tail = new Vector3(0f, markerHeight, edgeDistance + 0.25f);
            var tip = new Vector3(0f, markerHeight, edgeDistance + 2.1f);
            AddDiagnosticLine(marker, "Front Arrow Shaft", tail, tip, material);
            AddDiagnosticLine(marker, "Front Arrow Head Left", tip,
                tip + new Vector3(-0.48f, 0f, -0.62f), material);
            AddDiagnosticLine(marker, "Front Arrow Head Right", tip,
                tip + new Vector3(0.48f, 0f, -0.62f), material);
            _selectionFrontMarker = marker;
            marker.gameObject.SetActive(false);
        }

        private void UpdateSelectedBuildingSelectionVisuals(bool visible)
        {
            if (_selectionFootprint == null) return;
            var selected = visible && IsSelected && _buildingsSelectable &&
                ActiveObjectSelection == LotObjectSelectionKind.Building;
            _selectionFootprint.gameObject.SetActive(selected);
            if (_selectionFrontMarker != null)
                _selectionFrontMarker.gameObject.SetActive(
                    selected && TopDownViewEnabled);
        }

        private void BuildObjectHoverHighlight()
        {
            var root = new GameObject("Selectable Object Hover Outline").transform;
            root.SetParent(transform, false);
            var material = LotSurfaceMaterial(
                new Color(0.24f, 0.95f, 0.48f, 0.72f), 2012);
            AddDiagnosticLine(root, "Hover South",
                new Vector3(-0.5f, 0.08f, -0.5f),
                new Vector3(0.5f, 0.08f, -0.5f), material);
            AddDiagnosticLine(root, "Hover East",
                new Vector3(0.5f, 0.08f, -0.5f),
                new Vector3(0.5f, 0.08f, 0.5f), material);
            AddDiagnosticLine(root, "Hover North",
                new Vector3(0.5f, 0.08f, 0.5f),
                new Vector3(-0.5f, 0.08f, 0.5f), material);
            AddDiagnosticLine(root, "Hover West",
                new Vector3(-0.5f, 0.08f, 0.5f),
                new Vector3(-0.5f, 0.08f, -0.5f), material);
            _objectHoverHighlight = root;
            root.gameObject.SetActive(false);
        }

        private void ApplyObjectHover(LotObjectSelectionKind kind, int index)
        {
            if (_objectHoverHighlight == null || kind == LotObjectSelectionKind.None ||
                index < 0)
            {
                ClearObjectHover();
                return;
            }
            var position = Vector3.zero;
            var rotation = 0;
            var width = 1.8f;
            var depth = 1.8f;
            switch (kind)
            {
                case LotObjectSelectionKind.Building:
                    if (index >= (_session.Data.Buildings?.Count ?? 0))
                    { ClearObjectHover(); return; }
                    var building = _session.Data.Buildings[index];
                    var package = HybridBuildingPackageRegistry.Load(
                        BuildingCatalog.Find(building.BuildingId).PackageResourcePath);
                    position = new Vector3(building.CellX, 0f, building.CellZ);
                    rotation = building.RotationQuarterTurns;
                    width = package.WidthMeters + 0.35f;
                    depth = package.DepthMeters + 0.35f;
                    break;
                case LotObjectSelectionKind.Flora:
                    if (index >= (_session.Data.Flora?.Count ?? 0))
                    { ClearObjectHover(); return; }
                    var flora = _session.Data.Flora[index];
                    position = new Vector3(flora.PositionX, 0f, flora.PositionZ);
                    break;
                case LotObjectSelectionKind.Prop:
                    if (index >= PropCount) { ClearObjectHover(); return; }
                    var prop = _session.Data.Props[index];
                    position = new Vector3(prop.PositionX, 0f, prop.PositionZ);
                    rotation = prop.RotationQuarterTurns;
                    PropDimensions(prop.PropId, 0, out width, out depth);
                    width += 0.25f;
                    depth += 0.25f;
                    break;
            }
            HoverObjectKind = kind;
            _hoverObjectIndex = index;
            _objectHoverHighlight.localPosition = position;
            _objectHoverHighlight.localRotation = Quaternion.Euler(0f, rotation * 90f, 0f);
            _objectHoverHighlight.localScale = new Vector3(width, 1f, depth);
            _objectHoverHighlight.gameObject.SetActive(true);
        }

        private void BuildRegistrationDiagnostics()
        {
            var root = new GameObject("Registration Diagnostics").transform;
            root.SetParent(transform);
            _registrationDiagnostics = root;
            var center = LotSurfaceMaterial(new Color(1f, 0.76f, 0.16f, 0.95f), 3200);
            var ridge = LotSurfaceMaterial(new Color(0.7f, 0.45f, 1f, 0.95f), 3201);
            var front = LotSurfaceMaterial(new Color(0.2f, 1f, 0.72f, 0.95f), 3202);
            AddDiagnosticLine(root, "FOUNDATION CENTER X", new Vector3(-0.45f, 0.18f, 0), new Vector3(0.45f, 0.18f, 0), center);
            AddDiagnosticLine(root, "FOUNDATION CENTER Z", new Vector3(0, 0.18f, -0.45f), new Vector3(0, 0.18f, 0.45f), center);
            var alongX = _buildingPackage.RoofRidgeAxis == "x";
            var half = (alongX ? _buildingPackage.WidthMeters : _buildingPackage.DepthMeters) * 0.47f;
            AddDiagnosticLine(root, "ROOF RIDGE AXIS",
                alongX ? new Vector3(-half, _buildingPackage.HeightMeters + 0.08f, 0) : new Vector3(0, _buildingPackage.HeightMeters + 0.08f, -half),
                alongX ? new Vector3(half, _buildingPackage.HeightMeters + 0.08f, 0) : new Vector3(0, _buildingPackage.HeightMeters + 0.08f, half), ridge);
            var radians = _buildingPackage.EntranceFacingDegrees * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
            var edgeDistance = Mathf.Abs(direction.x) > 0.5f
                ? _buildingPackage.WidthMeters * 0.5f
                : _buildingPackage.DepthMeters * 0.5f;
            var edge = direction * edgeDistance;
            AddDiagnosticLine(root, "ENTRANCE FRONT DIRECTION", edge + Vector3.up * 0.24f,
                edge + direction * 1.25f + Vector3.up * 0.24f, front);
            AddDiagnosticLine(root, "ARTWORK PIVOT", new Vector3(0, 0.2f, 0), new Vector3(0, 1.35f, 0), center);
            root.gameObject.SetActive(false);
        }

        private static void AddDiagnosticLine(Transform parent, string name, Vector3 start, Vector3 end, Material material)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.09f;
            line.endWidth = 0.09f;
            line.material = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void BuildCamera()
        {
            _cameraPivot = new GameObject("Camera Pivot").transform;
            _cameraPivot.SetParent(transform);

            // Bootstrap already supplies a Main Camera. Reuse that camera as the
            // lot camera so the hybrid artwork and the Game view can never bind
            // to different cameras. Creating a second camera made the result
            // dependent on render order and produced the rolled, oversized view.
            _camera = Camera.main;
            if (_camera == null || _camera.targetTexture != null)
            {
                var cameraObject = new GameObject("Lot Camera");
                cameraObject.tag = "MainCamera";
                _camera = cameraObject.AddComponent<Camera>();
            }

            _camera.name = "Lot Camera";
            _camera.transform.SetParent(_cameraPivot, false);
            _camera.enabled = true;
            _camera.depth = 100f;
            _camera.orthographic = true;
            // Camera-facing building sprites can otherwise retain a stale
            // hardware-occlusion result after the camera pans. In the bounded
            // Lot Editor scene deterministic building visibility is preferable.
            _camera.useOcclusionCulling = false;
            _camera.orthographicSize = OrthographicSizeForLot(ZoomLevel, LotSizeMeters);
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = FarClipPlaneForLot(LotSizeMeters);
            _camera.backgroundColor = new Color(0.075f, 0.105f, 0.13f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            ApplyCameraFacing(false);
            ApplyTimeOfDay();

            RetireCompetingScreenCameras();
        }

        private void ApplyTimeOfDay()
        {
            ApplyExperimentalBuilding3DShadowDistance();
            var spec = TimeOfDayLighting.For(TimeOfDay);
            var ambientLight = IsRaining
                ? Color.Lerp(spec.AmbientColor, new Color(0.18f, 0.22f, 0.26f), 0.55f)
                : spec.AmbientColor;
            if (ExperimentalBuilding3DCount > 0)
                ApplyExperimentalBuilding3DStudioEnvironment();
            else
            {
                RestoreExperimentalBuilding3DStudioEnvironment();
                RenderSettings.ambientMode =
                    UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = ambientLight *
                    _environmentAmbientIntensityScale;
            }

            if (_sun != null)
            {
                var sunIntensity = spec.SunIntensity;
                var sunColor = spec.SunColor;
                if (ExperimentalBuilding3DCount > 0)
                {
                    sunIntensity = TimeOfDay switch
                    {
                        TimeOfDayPreset.Morning => 0.62f,
                        TimeOfDayPreset.Noon => 0.55f,
                        TimeOfDayPreset.Afternoon => 0.50f,
                        TimeOfDayPreset.Evening => 0.14f,
                        _ => 0.035f
                    };
                    if (TimeOfDay == TimeOfDayPreset.Morning)
                    {
                        // Make warmth a subtle directional cue rather than an
                        // orange exposure wash across every PBR surface.
                        sunColor = new Color(1f, 0.985f, 0.96f);
                    }
                }
                _sun.intensity = sunIntensity * _environmentSunIntensityScale *
                    (IsRaining ? 0.32f : 1f);
                _sun.color = sunColor;
                _sun.shadows = IsRaining ? LightShadows.None : LightShadows.Soft;
                _sun.shadowStrength = ExperimentalBuilding3DCount > 0
                    ? _environmentShadowStrength
                    : 0.72f;
                _sun.shadowCustomResolution = ExperimentalBuilding3DCount > 0
                    ? 4096
                    : 0;
                _sun.shadowBias = ExperimentalBuilding3DCount > 0
                    ? 0.055f
                    : 0.035f;
                _sun.shadowNormalBias = ExperimentalBuilding3DCount > 0
                    ? 0.36f
                    : 0.28f;
                // The layer-31 duplicate is a private ShadowsOnly caster for
                // flora/billboard receivers. If the main sun also sees it,
                // every real 3D building casts twice onto ordinary ground.
                _sun.cullingMask = ExperimentalBuilding3DCount > 0
                    ? ~(1 << FloraShadowReceiverLayer)
                    : ~0;
                // Native 3D objects live in the Lot Editor's world compass.
                // A hybrid building package may register its pre-rendered
                // artwork with a visual offset, but that must never rotate a
                // walking character's physical shadow.
                _sun.transform.rotation =
                    ExperimentalBuilding3DSunRotation();
                if (ExperimentalBuilding3DCount <= 0 &&
                    (_environmentSunElevationOffset != 0f ||
                     _environmentSunAzimuthOffset != 0f))
                    _sun.transform.rotation *= Quaternion.Euler(
                        _environmentSunElevationOffset,
                        _environmentSunAzimuthOffset, 0f);
            }

            UpdateFloraShadowSun();
            UpdateExperimentalBuilding3DProjectedGroundShadows();
            ApplyExperimentalBuilding3DColorGrade();

            if (_groundRenderer != null)
            {
                var groundBaseline = string.IsNullOrWhiteSpace(
                    _session.Data.BaseTextureId)
                    ? spec.GroundColor
                    : LotTextureTint(TimeOfDay);
                if (ExperimentalBuilding3DCount > 0 &&
                    BaseTextureHasExactSeasonResource())
                {
                    // Seasonal grass is already fully color-authored. A warm
                    // time-of-day multiplier turns its deep green into olive;
                    // sunlight supplies the time cue without recoloring it.
                    groundBaseline = Color.white;
                }
                if (string.IsNullOrWhiteSpace(_session.Data.BaseTextureId))
                    groundBaseline = ExperimentalBuilding3DGroundColor(
                        groundBaseline);
                _groundRenderer.sharedMaterial.color =
                    BaseTextureHasExactSeasonResource()
                        ? groundBaseline
                        : SeasonLighting.GroundColor(Season, groundBaseline);
                if (_groundRenderer.sharedMaterial.HasProperty("_AmbientFloor"))
                {
                    _groundRenderer.sharedMaterial.SetFloat("_AmbientFloor", TimeOfDay switch
                    {
                        TimeOfDayPreset.Morning => 0.68f,
                        TimeOfDayPreset.Noon => 0.58f,
                        TimeOfDayPreset.Afternoon => 0.66f,
                        _ => 0.52f
                    });
                }
            }

            if (_camera != null)
            {
                _camera.backgroundColor = IsRaining
                    ? Color.Lerp(spec.BackgroundColor,
                        new Color(0.20f, 0.25f, 0.30f), 0.62f)
                    : spec.BackgroundColor;
            }

            _presentation?.SetTimeOfDay(TimeOfDay);
            _presentation?.SetSeason(Season);
            _presentation?.SetRain(IsRaining);
            foreach (var presentation in _otherBuildingPresentations)
            {
                presentation?.SetTimeOfDay(TimeOfDay);
                presentation?.SetSeason(Season);
                presentation?.SetRain(IsRaining);
            }
            foreach (var vehicle in _vehiclePresentations)
                vehicle.SetTimeOfDay(TimeOfDay);
            foreach (var flora in _floraPresentations)
                if (flora != null) flora.color = FloraColorForTime(1f);
            UpdateFloraShadows();
            UpdatePropProjectedShadows();
            UpdateThreeLanternLamppostLighting();
            UpdateLotTextureLighting();
            if (_floraPreview != null)
                _floraPreview.color = FloraColorForTime(1f);
            if (_roadArtworkRoot != null)
            {
                var roadTint = spec.NeutralArtworkTint;
                foreach (var renderer in _roadArtworkRoot.GetComponentsInChildren<Renderer>())
                    if (renderer.sharedMaterial != null &&
                        renderer.sharedMaterial.HasProperty("_TimeTint"))
                        renderer.sharedMaterial.SetColor("_TimeTint", roadTint);
            }
            UpdateProjectedShadow();
            UpdateOtherBuildingProjectedShadows();
            UpdateBuildingGapShadowAppearance();
        }

        private void ApplyCameraFacing(bool preserveProjectionFit = true)
        {
            if (_camera == null)
            {
                return;
            }

            _camera.farClipPlane = FarClipPlaneForLot(LotSizeMeters);

            if (TopDownViewEnabled)
            {
                ApplyTopDownCamera(preserveProjectionFit);
                return;
            }

            var experimental3D = ExperimentalBuilding3DCount > 0;
            var angle = experimental3D
                ? 20f
                : _buildingPackage.Facing(_facing).CameraAzimuthDegrees;
            var azimuthRadians = angle * Mathf.Deg2Rad;
            // Tall native 3D façades read too top-down at the 28–30 degree
            // elevation used to register pre-rendered billboard artwork.
            // The experimental mesh lot uses the shallower CityForge view.
            var cameraElevationDegrees = experimental3D
                ? 20f
                : _buildingPackage.CameraElevationDegrees;
            var elevationRadians = cameraElevationDegrees * Mathf.Deg2Rad;
            var cameraRadius = CameraRadiusForLot(
                experimental3D ? 60f : _buildingPackage.CameraRadiusMeters,
                LotSizeMeters);
            var horizontalRadius =
                cameraRadius * Mathf.Cos(elevationRadians);
            var target = new Vector3(
                0f,
                HasBuilding && !experimental3D
                    ? _buildingPackage.CameraTargetHeightMeters
                    : 0f,
                0f);
            if (ZoomLevel == LotZoomLevel.Detail && LotType == LotType.Neighborhood)
            {
                var focus = RoadPlacementModel.CellCenterMeters(
                    RoadCursorCell.x, RoadCursorCell.y, LotWidthMeters, LotDepthMeters);
                target.x = focus.x;
                target.z = focus.y;
            }
            target += _cameraPanWorld;
            _camera.transform.position = target + new Vector3(
                Mathf.Cos(azimuthRadians) * horizontalRadius,
                cameraRadius * Mathf.Sin(elevationRadians),
                Mathf.Sin(azimuthRadians) * horizontalRadius);
            _camera.transform.LookAt(target);
            var compositionShift = ZoomLevel == LotZoomLevel.Detail
                ? _camera.transform.right * DetailInspectorClearanceMeters
                : -_camera.transform.right * CameraFramingOffsetMeters(LotSizeMeters);
            _camera.transform.position += compositionShift;
            target += compositionShift;
            _camera.transform.LookAt(target);
            AlignBuildingPresentationsToCamera();
            AlignFloraToCamera();
            UpdatePresentationDepthOrdering();
            if (!preserveProjectionFit)
                ApplyProjectedLotFit();
        }

        private void ApplyTopDownCamera(bool preserveProjectionFit = false)
        {
            var target = new Vector3(0f, 0f, 0f) + _cameraPanWorld;
            var cameraHeight = Mathf.Max(
                CameraRadiusForLot(_buildingPackage.CameraRadiusMeters,
                    LotSizeMeters),
                _buildingPackage.HeightMeters + 10f);
            _camera.transform.position = target + Vector3.up * cameraHeight;
            _camera.transform.rotation = ResolveTopDownRotation(
                _topDownWorldZScreenDirection);
            if (!preserveProjectionFit)
                _camera.orthographicSize = OrthographicSizeForLot(
                    ZoomLevel, LotSizeMeters);
            ApplyPresentationFacing();
            AlignBuildingPresentationsToCamera();
            AlignFloraToCamera();
            UpdatePresentationDepthOrdering();
            if (!preserveProjectionFit)
                ApplyProjectedLotFit();
        }

        private void AlignBuildingPresentationsToCamera()
        {
            // Camera panning happens from UI events, before the normal
            // LateUpdate billboard pass. Realign immediately so Unity never
            // evaluates a building's previous-frame renderer bounds against
            // the newly moved camera and incorrectly culls the artwork.
            _presentation?.AlignToCamera();
            foreach (var presentation in _otherBuildingPresentations)
                presentation?.AlignToCamera();
        }

        public static Quaternion ResolveTopDownRotation(
            Vector2 worldZScreenDirection)
        {
            worldZScreenDirection = SnapTopDownScreenDirection(
                worldZScreenDirection);
            var groundUp = new Vector3(
                -worldZScreenDirection.x, 0f, worldZScreenDirection.y);
            return Quaternion.LookRotation(Vector3.down, groundUp);
        }

        public static Vector2 SnapTopDownScreenDirection(
            Vector2 worldScreenDirection)
        {
            if (worldScreenDirection.sqrMagnitude <= 0.0001f)
                return Vector2.up;
            return Mathf.Abs(worldScreenDirection.x) >
                   Mathf.Abs(worldScreenDirection.y)
                ? new Vector2(Mathf.Sign(worldScreenDirection.x), 0f)
                : new Vector2(0f, Mathf.Sign(worldScreenDirection.y));
        }

        private void AlignFloraToCamera()
        {
            if (_floraRoot == null || _camera == null) return;
            foreach (Transform flora in _floraRoot)
                flora.rotation = _camera.transform.rotation;
        }

        private int DepthSortingOrder(Vector3 worldPosition)
        {
            if (_camera == null) return 1000;
            var cameraDepth = _camera.transform.InverseTransformPoint(worldPosition).z;
            return 10000 - Mathf.RoundToInt(cameraDepth * 100f);
        }

        private void UpdatePresentationDepthOrdering()
        {
            if (_camera == null) return;
            for (var index = 0; index < (_session.Data.Buildings?.Count ?? 0); index++)
            {
                var presentation = PresentationForBuildingIndex(index);
                var building = _session.Data.Buildings[index];
                if (presentation != null)
                {
                    presentation.SetSortingOrder(DepthSortingOrder(
                        new Vector3(building.CellX, 0f, building.CellZ)));
                }
            }
            for (var index = 0; index < _floraPresentations.Count &&
                 index < (_session.Data.Flora?.Count ?? 0); index++)
            {
                var flora = _session.Data.Flora[index];
                if (_floraPresentations[index] != null)
                {
                    var renderer = _floraPresentations[index];
                    var order = DepthSortingOrder(
                        new Vector3(flora.PositionX, 0f, flora.PositionZ));
                    renderer.sortingOrder = order;
                    var inFrontApron = TryResolveVisibleBuildingFrontHosts(
                        new Vector3(flora.PositionX, 0f, flora.PositionZ),
                        _floraFrontHostStencilReferences);
                    renderer.sharedMaterial = inFrontApron
                        ? FloraHostFrontRecoveryMaterial(
                            _floraFrontHostStencilReferences)
                        : FloraLitShadowReceiverMaterial();
                    if (index < _floraCastShadows.Count &&
                        _floraCastShadows[index] != null)
                        _floraCastShadows[index].sortingOrder = order - 1;
                }
            }
        }

        private void ApplyProjectedLotFit()
        {
            if (_camera == null || ZoomLevel == LotZoomLevel.Detail) return;

            var halfWidth = LotWidthMeters * 0.5f;
            var halfDepth = LotDepthMeters * 0.5f;
            var corners = new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, halfDepth)
            };
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            foreach (var corner in corners)
            {
                var local = _camera.transform.InverseTransformPoint(corner);
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }

            // Hybrid buildings are camera-facing sprite planes. Their authored
            // canvases can be much taller than the lot (especially towers), so
            // fitting only the ground makes the actual catalog placement look
            // massively zoomed even though an isolated asset preview is fine.
            // Include every visible building renderer in the same projected
            // bounds calculation used for the lot.
            void IncludePresentation(HybridBuildingPresentation presentation)
            {
                if (presentation == null) return;
                foreach (var renderer in presentation.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled) continue;
                    var bounds = renderer.bounds;
                    var center = bounds.center;
                    var extents = bounds.extents;
                    for (var x = -1; x <= 1; x += 2)
                    for (var y = -1; y <= 1; y += 2)
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var world = center + Vector3.Scale(
                            extents, new Vector3(x, y, z));
                        var local = _camera.transform.InverseTransformPoint(world);
                        minX = Mathf.Min(minX, local.x);
                        maxX = Mathf.Max(maxX, local.x);
                        minY = Mathf.Min(minY, local.y);
                        maxY = Mathf.Max(maxY, local.y);
                    }
                }
            }

            IncludePresentation(_presentation);
            foreach (var presentation in _otherBuildingPresentations)
                IncludePresentation(presentation);

            // Size alone is insufficient for tall hybrid artwork because its
            // pivot sits at ground level. Center the orthographic camera on
            // the combined lot + artwork projection before applying the fit.
            // This also restores the empty lot to the viewport after deleting
            // the last building.
            var projectedCenterX = (minX + maxX) * 0.5f;
            var projectedCenterY = (minY + maxY) * 0.5f;
            // Automatic framing owns the initial centered view. Once the user
            // has panned, retain that camera translation: applying this
            // projected-center correction after every drag would immediately
            // pull the lot back toward center and make panning look like
            // viewport clipping instead of moving the view.
            if (_cameraPanWorld.sqrMagnitude <= 0.0001f)
            {
                _camera.transform.position +=
                    _camera.transform.right * projectedCenterX +
                    _camera.transform.up * projectedCenterY;
            }

            var halfProjectedWidth = (maxX - minX) * 0.5f;
            var halfProjectedHeight = (maxY - minY) * 0.5f;
            var aspect = Mathf.Max(0.5f, _camera.aspect);
            var fullFit = Mathf.Max(halfProjectedHeight, halfProjectedWidth / aspect) * 1.18f;
            var required = ZoomLevel switch
            {
                LotZoomLevel.Inspection => fullFit * 0.42f,
                LotZoomLevel.CloseUp => fullFit * 0.56f,
                LotZoomLevel.Close => fullFit * 0.72f,
                LotZoomLevel.Near => fullFit * 0.86f,
                LotZoomLevel.Lot => fullFit,
                LotZoomLevel.Wide => fullFit * 1.15f,
                LotZoomLevel.Far => fullFit * 1.35f,
                _ => fullFit * 1.6f
            };
            _camera.orthographicSize = Mathf.Max(
                OrthographicSizeForLot(ZoomLevel, LotSizeMeters), required);
        }

        private void Update()
        {
            HandleWorldClick();
            UpdateThreeDimensionalCharacters();
            UpdateNeighborhoodTraffic();
            UpdateCirculationTravelers();
            UpdateStreetcars();
        }

        private void LateUpdate()
        {
            if (_camera == null)
                return;

            RebuildBuildingPropOverlayPass();
            RetireCompetingScreenCameras();
        }

        private void RetireCompetingScreenCameras()
        {
            // The bootstrap scene contains CF_GAME_Camera_v02, a top-down
            // helper camera. Merely disabling it is insufficient because its
            // owner can re-enable it later in the frame. When that happens it
            // renders the billboard from a different basis, making upright
            // artwork appear rolled and enormously scaled. The Lot Editor
            // owns the only display camera while this world is active, so
            // remove competing screen-camera components altogether. Cameras
            // rendering to textures remain untouched.
            foreach (var otherCamera in FindObjectsByType<Camera>(
                         FindObjectsSortMode.None))
            {
                if (otherCamera == null || otherCamera == _camera ||
                    otherCamera == _propFrontRecoveryCamera ||
                    otherCamera.targetTexture != null)
                    continue;

                otherCamera.enabled = false;
                Destroy(otherCamera);
            }
        }

        private void UpdateCirculationTravelers()
        {
            _pedestrianProgress = Mathf.PingPong(_pedestrianProgress + Time.deltaTime * 0.18f, 1f);
            PositionTraveler(_pedestrianTraveler, _session.Data.PedestrianNetwork, _pedestrianProgress);
            if (TrafficType == TrafficLotType.SuburbanStreet)
            {
                UpdateSuburbanTraffic(Time.deltaTime);
            }
            else if (TrafficType == TrafficLotType.None &&
                     _trafficGraph != null && _trafficGraph.LaneCount > 0)
            {
                LaneTrafficModel.Step(_laneVehicleStates, _trafficGraph, _vehicleType, Time.deltaTime);
                PlaceVehicleAtRouteDistance();
            }
            UpdateTestVehicles(Time.deltaTime);
        }

        public bool SpawnTestVehicle(VehiclePaintVariant variant)
            => SpawnTestVehicle(TestVehicleModel.FordModelT, variant);

        public bool SpawnTestVehicle(TestVehicleModel vehicleModel,
            VehiclePaintVariant variant = VehiclePaintVariant.Green)
        {
            if (!CanSpawnTestVehicle || _circulationTravelerRoot == null)
                return false;
            VehicleRoute route = null;
            TestVehiclePath openPath = null;
            if (_trafficGraph != null && _trafficGraph.LaneCount > 0)
                route = _trafficGraph.Routes[_testVehicles.Count % _trafficGraph.LaneCount];
            else
                openPath = TestVehiclePath.FromNetwork(_session.Data.VehicleNetwork);
            var pathLength = route?.TotalLengthMeters ?? openPath?.TotalLengthMeters ?? 0f;
            if (pathLength <= 0.001f)
                return false;
            var presentation = VehicleRuntimePresentation.Create(
                _circulationTravelerRoot, vehicleModel, variant);
            var displayName = vehicleModel == TestVehicleModel.RollsRoyce1926
                ? "1926 Rolls-Royce"
                : $"Ford Model T — {variant}";
            presentation.gameObject.name = $"Test Vehicle — {displayName}";
            presentation.SetTimeOfDay(TimeOfDay);
            var traveler = new TestVehicleTraveler
            {
                Presentation = presentation,
                Route = route,
                OpenPath = openPath,
                DistanceMeters = (_testVehicles.Count * 5.5f) % pathLength
            };
            _testVehicles.Add(traveler);
            PlaceTestVehicle(traveler);
            return true;
        }

        public void RemoveTestVehicles()
        {
            foreach (var traveler in _testVehicles)
                if (traveler.Presentation != null)
                {
                    if (Application.isPlaying) Destroy(traveler.Presentation.gameObject);
                    else DestroyImmediate(traveler.Presentation.gameObject);
                }
            _testVehicles.Clear();
        }

        private void UpdateTestVehicles(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            // A hand-authored open vehicle path is also a valid test route and
            // does not necessarily build a lane graph. Keep those vehicles
            // moving with a conservative city speed instead of freezing them.
            var speed = _trafficGraph != null
                ? Mathf.Max(2f, _trafficGraph.SpeedMetersPerSecond * 0.72f)
                : 5f;
            foreach (var traveler in _testVehicles)
            {
                traveler.DistanceMeters += speed * deltaTime;
                PlaceTestVehicle(traveler);
            }
        }

        private static void PlaceTestVehicle(TestVehicleTraveler traveler)
        {
            if (traveler?.Presentation == null) return;
            Vector2 point;
            Vector2 direction;
            float steering;
            if (traveler.Route != null)
            {
                traveler.Route.Sample(traveler.DistanceMeters, out point, out _);
                direction = traveler.Route.SmoothedDirection(traveler.DistanceMeters);
                steering = traveler.Route.SteeringDegrees(traveler.DistanceMeters);
            }
            else if (traveler.OpenPath != null)
            {
                traveler.OpenPath.SamplePatrol(traveler.DistanceMeters,
                    out point, out direction);
                steering = 0f;
            }
            else return;
            traveler.Presentation.Place(point, direction, steering);
            traveler.Presentation.gameObject.SetActive(true);
        }

        private sealed class TestVehicleTraveler
        {
            public VehicleRuntimePresentation Presentation;
            public VehicleRoute Route;
            public TestVehiclePath OpenPath;
            public float DistanceMeters;
        }

        private sealed class TestVehiclePath
        {
            private readonly List<Vector2> _points;
            private readonly List<float> _segmentStarts = new();
            public float TotalLengthMeters { get; }

            private TestVehiclePath(List<Vector2> points)
            {
                _points = points;
                var length = 0f;
                for (var index = 0; index < points.Count - 1; index++)
                {
                    _segmentStarts.Add(length);
                    length += Vector2.Distance(points[index], points[index + 1]);
                }
                TotalLengthMeters = length;
            }

            public static TestVehiclePath FromNetwork(CirculationNetwork network)
            {
                if (network == null || network.Segments.Count == 0) return null;
                var neighbors = new Dictionary<string, List<string>>();
                foreach (var node in network.Nodes)
                    if (node != null) neighbors[node.Id] = new List<string>();
                foreach (var segment in network.Segments)
                {
                    if (segment == null || !neighbors.ContainsKey(segment.StartNodeId) ||
                        !neighbors.ContainsKey(segment.EndNodeId)) continue;
                    neighbors[segment.StartNodeId].Add(segment.EndNodeId);
                    neighbors[segment.EndNodeId].Add(segment.StartNodeId);
                }
                var start = network.Nodes.Find(node => node != null &&
                    neighbors.TryGetValue(node.Id, out var adjacent) && adjacent.Count == 1) ??
                    network.Nodes.Find(node => node != null && neighbors.ContainsKey(node.Id));
                if (start == null) return null;
                var ordered = new List<Vector2>();
                var visitedEdges = new HashSet<string>();
                var current = start.Id;
                while (!string.IsNullOrEmpty(current))
                {
                    var node = network.FindNode(current);
                    if (node == null) break;
                    ordered.Add(node.PositionMeters);
                    string next = null;
                    foreach (var candidate in neighbors[current])
                    {
                        var edge = string.CompareOrdinal(current, candidate) < 0
                            ? current + "|" + candidate : candidate + "|" + current;
                        if (visitedEdges.Contains(edge)) continue;
                        visitedEdges.Add(edge);
                        next = candidate;
                        break;
                    }
                    current = next;
                }
                return ordered.Count >= 2 ? new TestVehiclePath(ordered) : null;
            }

            public void SamplePatrol(float distanceMeters,
                out Vector2 point, out Vector2 direction)
            {
                var cycle = Mathf.Repeat(distanceMeters, TotalLengthMeters * 2f);
                var returning = cycle > TotalLengthMeters;
                var distance = returning ? TotalLengthMeters * 2f - cycle : cycle;
                var segmentIndex = Mathf.Max(0, _points.Count - 2);
                for (var index = 0; index < _points.Count - 1; index++)
                {
                    var end = index == _points.Count - 2
                        ? TotalLengthMeters : _segmentStarts[index + 1];
                    if (distance <= end) { segmentIndex = index; break; }
                }
                var start = _points[segmentIndex];
                var endPoint = _points[segmentIndex + 1];
                var length = Vector2.Distance(start, endPoint);
                var progress = length <= 0.001f ? 0f :
                    (distance - _segmentStarts[segmentIndex]) / length;
                point = Vector2.Lerp(start, endPoint, Mathf.Clamp01(progress));
                direction = (endPoint - start).normalized * (returning ? -1f : 1f);
            }
        }

        private static void PositionTraveler(Transform traveler, CirculationNetwork network, float progress)
        {
            if (traveler == null) return;
            traveler.gameObject.SetActive(network.Segments.Count > 0);
            var point = network.SampleFirstSegment(progress);
            traveler.localPosition = new Vector3(point.x, 0.42f, point.y);
        }

        private void PlaceVehicleAtRouteDistance()
        {
            if (_vehiclePresentations.Count == 0) return;
            var active = TrafficType == TrafficLotType.None && CirculationDiagnosticsVisible &&
                _trafficGraph != null && _trafficGraph.LaneCount == 2 &&
                _laneVehicleStates.Count == _vehiclePresentations.Count;
            for (var index = 0; index < _vehiclePresentations.Count; index++)
            {
                var vehicle = _vehiclePresentations[index];
                vehicle.gameObject.SetActive(active);
                if (!active) continue;
                var state = _laneVehicleStates[index];
                var route = _trafficGraph.Routes[state.LaneIndex];
                var distance = state.DistanceMeters;
                route.Sample(distance, out var point, out _);
                vehicle.Place(
                    point,
                    route.SmoothedDirection(distance),
                    route.SteeringDegrees(distance));
            }
        }

        public bool TryBeginSuburbanTrip()
        {
            if (TrafficType != TrafficLotType.SuburbanStreet ||
                _suburbanVehicleActive || _vehiclePresentations.Count == 0) return false;
            _suburbanTrip = TrafficLotModel.FindTrip(
                _session.Data.VehicleNetwork, _session.Data.OutsideRoadConnectors);
            if (_suburbanTrip.Count < 2) return false;
            _suburbanTripDistance = 0f;
            _suburbanVehicleActive = true;
            var vehicle = _vehiclePresentations[_suburbanSpawnSequence %
                _vehiclePresentations.Count];
            foreach (var presentation in _vehiclePresentations)
                presentation.gameObject.SetActive(presentation == vehicle);
            TrafficLotModel.SampleTrip(_suburbanTrip, 0f, out var point, out var direction);
            vehicle.Place(point, direction);
            _suburbanSpawnSequence++;
            return true;
        }

        private void UpdateSuburbanTraffic(float deltaTime)
        {
            if (!_suburbanVehicleActive)
            {
                _suburbanSpawnTimer -= deltaTime;
                if (_suburbanSpawnTimer <= 0f && !TryBeginSuburbanTrip())
                    _suburbanSpawnTimer = TrafficLotModel.SuburbanMinimumSpawnSeconds;
                return;
            }
            var tripLength = TrafficLotModel.TripLength(_suburbanTrip);
            _suburbanTripDistance += Mathf.Max(1f,
                _trafficGraph?.SpeedMetersPerSecond ?? 4f) * 0.72f * deltaTime;
            if (_suburbanTripDistance >= tripLength)
            {
                _suburbanVehicleActive = false;
                foreach (var presentation in _vehiclePresentations)
                    presentation.gameObject.SetActive(false);
                var phase = (_suburbanSpawnSequence % 4) / 3f;
                _suburbanSpawnTimer = Mathf.Lerp(
                    TrafficLotModel.SuburbanMinimumSpawnSeconds,
                    TrafficLotModel.SuburbanMaximumSpawnSeconds, phase);
                return;
            }
            TrafficLotModel.SampleTrip(_suburbanTrip, _suburbanTripDistance,
                out var point, out var direction);
            var activeIndex = (_suburbanSpawnSequence - 1 + _vehiclePresentations.Count) %
                _vehiclePresentations.Count;
            _vehiclePresentations[activeIndex].Place(point, direction);
        }

        private void ResetSuburbanTraffic()
        {
            _suburbanTrip.Clear();
            _suburbanTripDistance = 0f;
            _suburbanVehicleActive = false;
            _suburbanSpawnTimer = TrafficLotModel.SuburbanMinimumSpawnSeconds;
            foreach (var presentation in _vehiclePresentations)
                presentation.gameObject.SetActive(false);
        }

        private void UpdateNeighborhoodTraffic()
        {
            if (_trafficVehicle == null || !NeighborhoodRoadVisible) return;
            _trafficProgress = Mathf.Repeat(_trafficProgress + Time.deltaTime * 0.055f, 1f);
            var local = _trafficVehicle.localPosition;
            local.z = Mathf.Lerp(-9f, 4f, _trafficProgress);
            _trafficVehicle.localPosition = local;
        }

        private void ApplyZoomLevel()
        {
            if (_camera != null)
                _camera.orthographicSize = OrthographicSizeForLot(ZoomLevel, LotSizeMeters);
            ApplyGridVisibility();
        }

        private void ApplyGridVisibility()
        {
            if (_minorGrid != null)
                _minorGrid.gameObject.SetActive(
                    _gridVisible && LotMetricScale.ShowsMinorGrid(ZoomLevel));
            if (_majorGrid != null) _majorGrid.gameObject.SetActive(_gridVisible);
        }

        public static float OrthographicSizeForLot(LotZoomLevel level, int lotSizeMeters)
        {
            // These are absolute editorial camera bands, not lot-relative fits.
            // Keeping them independent of lot dimensions makes a given zoom
            // level read consistently as an approximate altitude.
            return level switch
            {
                LotZoomLevel.Detail => 11.4375f,
                LotZoomLevel.Close => 22f,       // approximately 200 ft
                LotZoomLevel.Lot => 44f,         // approximately 400 ft
                LotZoomLevel.Wide => 88f,        // approximately 800 ft
                LotZoomLevel.Far => 132f,        // approximately 1,200 ft
                LotZoomLevel.Neighborhood => 220f, // approximately 2,000 ft
                // Retained for old serialized/editor references; interactive
                // zooming skips these superseded intermediate bands.
                LotZoomLevel.Inspection => 13f,
                LotZoomLevel.CloseUp => 17.5f,
                LotZoomLevel.Near => 33f,
                _ => 44f
            };
        }

        public static int ApproximateZoomAltitudeFeet(LotZoomLevel level) => level switch
        {
            LotZoomLevel.Detail => 105,
            LotZoomLevel.Close => 200,
            LotZoomLevel.Lot => 400,
            LotZoomLevel.Wide => 800,
            LotZoomLevel.Far => 1200,
            LotZoomLevel.Neighborhood => 2000,
            LotZoomLevel.Inspection => 110,
            LotZoomLevel.CloseUp => 150,
            LotZoomLevel.Near => 300,
            _ => 400
        };

        public static bool ShowsThreeDimensionalCharacters(LotZoomLevel level) =>
            level is LotZoomLevel.Detail or LotZoomLevel.Close or LotZoomLevel.Lot;

        public static float CameraFramingOffsetMeters(int lotSizeMeters) =>
            Mathf.Clamp(lotSizeMeters, 20, 80) * 0.12f;

        public const float DetailInspectorClearanceMeters = 3f;

        public static float FarClipPlaneForLot(int lotSizeMeters) =>
            Mathf.Max(100f, Mathf.Clamp(lotSizeMeters, 20, 80) * 3.5f);

        public static float CameraRadiusForLot(float authoredRadiusMeters, int lotSizeMeters) =>
            lotSizeMeters <= 40
                ? authoredRadiusMeters
                : Mathf.Max(authoredRadiusMeters, lotSizeMeters * 1.5f);

        public static LotZoomLevel NextZoomLevel(LotZoomLevel current, int direction)
        {
            var levels = new[]
            {
                LotZoomLevel.Detail,
                LotZoomLevel.Close,
                LotZoomLevel.Lot,
                LotZoomLevel.Wide,
                LotZoomLevel.Far,
                LotZoomLevel.Neighborhood
            };
            var currentIndex = System.Array.IndexOf(levels, current);
            if (currentIndex < 0)
            {
                var altitude = ApproximateZoomAltitudeFeet(current);
                currentIndex = 0;
                while (currentIndex < levels.Length - 1 &&
                       ApproximateZoomAltitudeFeet(levels[currentIndex]) < altitude)
                    currentIndex++;
            }
            return levels[Mathf.Clamp(currentIndex + direction, 0, levels.Length - 1)];
        }

        private void ApplyLotPlanningState()
        {
            if (_neighborhoodRoad != null)
                _neighborhoodRoad.gameObject.SetActive(LotType == LotType.Neighborhood);
            if (_roadArtworkRoot != null)
                _roadArtworkRoot.gameObject.SetActive(LotType == LotType.Neighborhood);
            if (_roadCursor != null)
                _roadCursor.gameObject.SetActive(
                    LotType == LotType.Neighborhood && RoadCursorSelected);
            if (_circulationRoot != null)
                _circulationRoot.gameObject.SetActive(CirculationDiagnosticsVisible);
        }

        private void HandleWorldClick()
        {
            if (_cameraPanInteractionActive || !_buildingsSelectable ||
                _buildingDragActive || _camera == null ||
                !Input.GetMouseButtonDown(0))
            {
                return;
            }

            var mouse = Input.mousePosition;
            if (mouse.y > Screen.height - 76f || mouse.x < 105f ||
                mouse.x > Screen.width - 340f)
            {
                return;
            }

            if (!Physics.Raycast(_camera.ScreenPointToRay(mouse), out var hit, 200f) ||
                hit.collider.gameObject.name != "Lot Surface")
            {
                return;
            }

            var cellX = Mathf.RoundToInt(hit.point.x);
            var cellZ = Mathf.RoundToInt(hit.point.z);
            switch (_session.ToolMode)
            {
                case LotToolMode.Place:
                    EnsureBuildingPackage(_placementBuildingId);
                    if (CanOccupyBuilding(_buildingPackage, cellX, cellZ, 0, -1))
                        _session.AddBuilding(_placementBuildingId, cellX, cellZ);
                    break;
                case LotToolMode.Move:
                    MoveBuildingTo(cellX, cellZ);
                    break;
                default:
                    var deltaX = Mathf.Abs(cellX - _session.Data.CellX);
                    var deltaZ = Mathf.Abs(cellZ - _session.Data.CellZ);
                    _session.Select(
                        HasBuilding &&
                        deltaX <= Mathf.CeilToInt(_buildingPackage.WidthMeters * 0.5f) &&
                        deltaZ <= Mathf.CeilToInt(_buildingPackage.DepthMeters * 0.5f));
                    break;
            }

            ApplySessionState();
            NotifyStateChanged();
        }

        private void ApplySessionState()
        {
            _sessionStateApplyCountForQa++;
            var preservedCamera = CaptureCameraFraming();
            ApplyLotPlanningState();
            ApplyBaseTexturePresentation();
            if (HasBuilding)
            {
                EnsureBuildingPackage(_session.Data.BuildingId);
            }
            var visible = HasBuilding;
            var position = new Vector3(
                _session.Data.CellX,
                0f,
                _session.Data.CellZ);
            if (_presentation != null && _buildingPackage != null)
            {
                _presentation.SetHostBuildingStencilReference(
                    BuildingDepthOcclusionStencilReference(
                        _session.SelectedBuildingIndex));
                _presentation.transform.position = position;
                _presentation.SetVisible(
                    visible &&
                    BuildingInspectionPolicy.ShowsArtwork(InspectionMode));
                _presentation.SetOpacity(BuildingInspectionPolicy.ArtworkOpacity(
                    InspectionMode, _buildingContextOpacity));
                ApplyPresentationFacing();
                _presentation.RegisterToProxy(
                    _proxyLocalVertices,
                    PrimitiveRotation(
                        _buildingPackage,
                        _session.Data.RotationQuarterTurns));
            }

            if (_proxy != null && _buildingPackage != null)
            {
                _proxy.position = position;
                _proxy.rotation = PrimitiveRotation(
                    _buildingPackage,
                    _session.Data.RotationQuarterTurns);
                foreach (var renderer in _proxyRenderers)
                {
                    var entranceDiagnostic =
                        renderer.gameObject.name == "CF_ANCHOR_ENTRANCE";
                    var foundationDiagnostic =
                        renderer.gameObject.name == "CF_PROXY_FOUNDATION";
                    var showsPrimitive =
                        BuildingInspectionPolicy.ShowsPrimitive(InspectionMode);
                    var showsExperimentalPrimitive =
                        ShowBuildingPrimitivesExperiment && !entranceDiagnostic;
                    var showsDiagnostic = (showsPrimitive ||
                        showsExperimentalPrimitive) &&
                        (!foundationDiagnostic ||
                         showsExperimentalPrimitive ||
                         BuildingInspectionPolicy.ShowsFoundationFill(InspectionMode));
                    // Keep semantic geometry active in artwork mode so it can
                    // cast the real, light-driven building shadow while
                    // remaining visually invisible via ShadowsOnly.
                    renderer.enabled = visible &&
                        (showsDiagnostic ||
                         (!entranceDiagnostic &&
                          !_buildingPackage.UsesMeshProjectedShadow));
                    renderer.shadowCastingMode = entranceDiagnostic ||
                        _buildingPackage.UsesMeshProjectedShadow
                        ? UnityEngine.Rendering.ShadowCastingMode.Off
                        : showsPrimitive || showsExperimentalPrimitive
                            ? UnityEngine.Rendering.ShadowCastingMode.On
                            : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }

            if (_buildingDepthOccluder != null)
            {
                _buildingDepthOccluder.position = position;
                _buildingDepthOccluder.rotation = PrimitiveRotation(
                    _buildingPackage,
                    _session.Data.RotationQuarterTurns);
                ConfigureBuildingDepthStencil(
                    _buildingDepthOccluderRenderers,
                    _session.SelectedBuildingIndex);
                foreach (var renderer in _buildingDepthOccluderRenderers)
                    // Perceptual occlusion is authored by above-ground semantic
                    // massing for every building. Foundations remain placement
                    // data only and may never become invisible visual walls.
                    renderer.enabled = visible &&
                        IsPerceptualBuildingOccluder(renderer);
            }

            UpdateProjectedShadow();
            RefreshBuildingPropPresentations();

            if (_selectionFootprint != null)
            {
                _selectionFootprint.position =
                    position + new Vector3(0f, 0.025f, 0f);
                _selectionFootprint.rotation = BuildingRotation();
                UpdateSelectedBuildingSelectionVisuals(visible);
            }
            if (_registrationDiagnostics != null)
            {
                _registrationDiagnostics.position = position;
                _registrationDiagnostics.rotation = PrimitiveRotation(
                    _buildingPackage,
                    _session.Data.RotationQuarterTurns);
                _registrationDiagnostics.gameObject.SetActive(
                    visible && RegistrationDiagnosticsVisible);
            }
            RebuildOtherBuildingPresentations();
            RebuildExperimentalBuilding3DPresentations();
            RebuildFloraPresentations();
            RebuildPropPresentations();
            RebuildOverlayTexturePresentations();
            UpdatePresentationDepthOrdering();
            // Selection swaps the primary presentation and reconstructs the
            // remaining building views. Reapply persistent street wetness only
            // after every new presentation exists, otherwise the building that
            // just became primary can miss the earlier rain transition.
            UpdateWetStreetReflections();
            ScheduleWetStreetReflectionRefresh();

            // Selection and ordinary lot-state changes must never reposition
            // or re-zoom the camera. Camera framing is owned only by initial
            // load/setup and explicit camera controls.
            RestoreCameraFraming(preservedCamera);
        }

        private void RebuildOtherBuildingPresentations()
        {
            foreach (var other in _otherBuildingPresentations)
                if (other != null)
                {
                    if (Application.isPlaying) Destroy(other.gameObject);
                    else DestroyImmediate(other.gameObject);
                }
            _otherBuildingPresentations.Clear();
            _otherBuildingIndices.Clear();
            foreach (var occluder in _otherBuildingDepthOccluders)
                if (occluder != null)
                    DestroyForCurrentMode(occluder.gameObject);
            _otherBuildingDepthOccluders.Clear();
            foreach (var shadow in _otherBuildingProjectedShadows)
                if (shadow != null)
                    DestroyForCurrentMode(shadow.gameObject);
            _otherBuildingProjectedShadows.Clear();
            _otherBuildingShadowPackages.Clear();
            foreach (var renderer in _buildingGapShadowRenderers)
                if (renderer != null)
                    DestroyForCurrentMode(renderer.gameObject);
            _buildingGapShadowRenderers.Clear();
            if (_camera == null || _session.Data.Buildings == null) return;
            for (var index = 0; index < _session.Data.Buildings.Count; index++)
            {
                if (index == _session.SelectedBuildingIndex) continue;
                var placed = _session.Data.Buildings[index];
                var package = HybridBuildingPackageRegistry.Load(
                    BuildingCatalog.Find(placed.BuildingId).PackageResourcePath);
                var root = new GameObject($"Building {index + 1} — {package.DisplayName}");
                root.transform.SetParent(transform);
                var presentation = root.AddComponent<HybridBuildingPresentation>();
                presentation.Build(_camera, package);
                presentation.SetHostBuildingStencilReference(
                    BuildingDepthOcclusionStencilReference(index));
                root.transform.position = new Vector3(placed.CellX, 0f, placed.CellZ);
                presentation.ApplyFacing(package.PresentationFacing(
                    _facing, placed.RotationQuarterTurns));
                presentation.SetBuildingRotation(placed.RotationQuarterTurns);
                presentation.SetArtworkSource(ArtworkSource);
                presentation.SetTimeOfDay(TimeOfDay);
                presentation.SetRain(IsRaining);
                presentation.SetWetReflection(RoadWetness,
                    WetReflectionDirectionFor(new Vector3(
                        placed.CellX, 0f, placed.CellZ)));
                presentation.SetVisible(BuildingInspectionPolicy.ShowsArtwork(InspectionMode));
                presentation.SetOpacity(BuildingInspectionPolicy.ArtworkOpacity(
                    InspectionMode, _buildingContextOpacity));
                _otherBuildingPresentations.Add(presentation);
                _otherBuildingIndices.Add(index);
                _otherBuildingDepthOccluders.Add(
                    CreateOtherBuildingDepthOccluder(placed, package, index));
                _otherBuildingProjectedShadows.Add(CreateProjectedShadow(
                    $"Building {index + 1} Projected Shadow", package));
                _otherBuildingShadowPackages.Add(package);
            }
            RebuildBuildingGapShadows();
            UpdateOtherBuildingProjectedShadows();
        }

        private readonly struct BuildingGroundFootprint
        {
            public readonly float MinX;
            public readonly float MaxX;
            public readonly float MinZ;
            public readonly float MaxZ;

            public BuildingGroundFootprint(PlacedBuilding placed,
                HybridBuildingPackage package)
            {
                var swapsAxes = (placed.RotationQuarterTurns & 1) != 0;
                var width = swapsAxes ? package.DepthMeters : package.WidthMeters;
                var depth = swapsAxes ? package.WidthMeters : package.DepthMeters;
                MinX = placed.CellX - width * 0.5f;
                MaxX = placed.CellX + width * 0.5f;
                MinZ = placed.CellZ - depth * 0.5f;
                MaxZ = placed.CellZ + depth * 0.5f;
            }
        }

        private void RebuildBuildingGapShadows()
        {
            if (_session?.Data?.Buildings == null) return;
            var footprints = new List<BuildingGroundFootprint>();
            foreach (var placed in _session.Data.Buildings)
            {
                var catalogEntry = BuildingCatalog.Find(placed.BuildingId);
                if (string.IsNullOrWhiteSpace(catalogEntry.PackageResourcePath))
                    continue;
                var package = HybridBuildingPackageRegistry.Load(
                    catalogEntry.PackageResourcePath);
                if (package != null)
                    footprints.Add(new BuildingGroundFootprint(placed, package));
            }

            for (var first = 0; first < footprints.Count; first++)
            for (var second = first + 1; second < footprints.Count; second++)
            {
                var a = footprints[first];
                var b = footprints[second];
                var overlapZ = Mathf.Min(a.MaxZ, b.MaxZ) -
                    Mathf.Max(a.MinZ, b.MinZ);
                if (overlapZ > 0.35f && TryOrderedGap(
                        a.MinX, a.MaxX, b.MinX, b.MaxX,
                        out var gapMinX, out var gapMaxX))
                {
                    CreateBuildingGapShadow(
                        new Vector2(gapMinX, Mathf.Max(a.MinZ, b.MinZ)),
                        new Vector2(gapMaxX, Mathf.Min(a.MaxZ, b.MaxZ)));
                }

                var overlapX = Mathf.Min(a.MaxX, b.MaxX) -
                    Mathf.Max(a.MinX, b.MinX);
                if (overlapX > 0.35f && TryOrderedGap(
                        a.MinZ, a.MaxZ, b.MinZ, b.MaxZ,
                        out var gapMinZ, out var gapMaxZ))
                {
                    CreateBuildingGapShadow(
                        new Vector2(Mathf.Max(a.MinX, b.MinX), gapMinZ),
                        new Vector2(Mathf.Min(a.MaxX, b.MaxX), gapMaxZ));
                }
            }
            UpdateBuildingGapShadowAppearance();
        }

        private static bool TryOrderedGap(float aMin, float aMax,
            float bMin, float bMax, out float gapMin, out float gapMax)
        {
            gapMin = gapMax = 0f;
            if (aMax <= bMin)
            {
                gapMin = aMax;
                gapMax = bMin;
            }
            else if (bMax <= aMin)
            {
                gapMin = bMax;
                gapMax = aMin;
            }
            else return false;

            return gapMax - gapMin <= 3.5f;
        }

        private void CreateBuildingGapShadow(Vector2 minimum, Vector2 maximum)
        {
            const float edgeBleed = 0.12f;
            minimum -= Vector2.one * edgeBleed;
            maximum += Vector2.one * edgeBleed;
            if (maximum.x - minimum.x < 0.05f ||
                maximum.y - minimum.y < 0.05f) return;

            var shadowObject = new GameObject("Primitive Neighbor Gap Shadow");
            shadowObject.transform.SetParent(transform, false);
            var filter = shadowObject.AddComponent<MeshFilter>();
            var renderer = shadowObject.AddComponent<MeshRenderer>();
            var mesh = new Mesh { name = "CF Building Gap Contact Shadow" };
            mesh.vertices = new[]
            {
                new Vector3(minimum.x, 0.066f, minimum.y),
                new Vector3(maximum.x, 0.066f, minimum.y),
                new Vector3(maximum.x, 0.066f, maximum.y),
                new Vector3(minimum.x, 0.066f, maximum.y)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = LotSurfaceMaterial(
                BuildingGapShadowColor(TimeOfDay), 2002);
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _buildingGapShadowRenderers.Add(renderer);
        }

        private void UpdateBuildingGapShadowAppearance()
        {
            var color = BuildingGapShadowColor(TimeOfDay);
            foreach (var renderer in _buildingGapShadowRenderers)
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    renderer.enabled = TimeOfDay != TimeOfDayPreset.Night;
                    renderer.sharedMaterial.color = color;
                }
        }

        public static Color BuildingGapShadowColor(TimeOfDayPreset preset) =>
            new Color(0.025f, 0.03f, 0.038f, preset switch
            {
                TimeOfDayPreset.Morning => 0.34f,
                TimeOfDayPreset.Noon => 0.20f,
                TimeOfDayPreset.Afternoon => 0.30f,
                TimeOfDayPreset.Evening => 0.24f,
                _ => 0f
            });

        private Transform CreateOtherBuildingDepthOccluder(
            PlacedBuilding placed, HybridBuildingPackage package, int index)
        {
            var proxyAsset = Resources.Load<GameObject>(
                package.PrimitiveResourcePath);
            var shader = Shader.Find("CityForgeV3/BuildingDepthOccluder");
            if (proxyAsset == null || shader == null) return null;

            var instance = Instantiate(proxyAsset, transform);
            instance.name = $"Building {index + 1} Depth Occluder";
            instance.transform.position = new Vector3(
                placed.CellX, 0f, placed.CellZ);
            instance.transform.rotation = PrimitiveRotation(
                package, placed.RotationQuarterTurns);
            foreach (var collider in instance.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            var material = new Material(shader)
            {
                name = $"CF Building {index + 1} Invisible Depth Occluder"
            };
            ConfigureBuildingDepthStencil(material, index);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.enabled = IsPerceptualBuildingOccluder(renderer);
            }
            return instance.transform;
        }

        private void UpdateOtherBuildingProjectedShadows()
        {
            if (_session?.Data?.Buildings == null)
                return;

            var count = Mathf.Min(
                _otherBuildingIndices.Count,
                Mathf.Min(
                    _otherBuildingProjectedShadows.Count,
                    _otherBuildingShadowPackages.Count));
            for (var listIndex = 0; listIndex < count; listIndex++)
            {
                var buildingIndex = _otherBuildingIndices[listIndex];
                if (buildingIndex < 0 ||
                    buildingIndex >= _session.Data.Buildings.Count)
                    continue;
                var placed = _session.Data.Buildings[buildingIndex];
                UpdateProjectedShadow(
                    _otherBuildingProjectedShadows[listIndex],
                    _otherBuildingShadowPackages[listIndex],
                    new Vector3(placed.CellX, 0f, placed.CellZ),
                    PrimitiveRotation(
                        _otherBuildingShadowPackages[listIndex],
                        placed.RotationQuarterTurns),
                    true,
                    false);
            }
        }

        private Quaternion BuildingRotation() =>
            Quaternion.Euler(
                0f,
                _session.Data.RotationQuarterTurns * 90f,
                0f);

        public static Quaternion PrimitiveRotation(
            HybridBuildingPackage package, int placedRotationQuarterTurns) =>
            Quaternion.Euler(
                0f,
                placedRotationQuarterTurns * 90f +
                (package?.PrimitiveRotationOffsetDegrees ?? 0f),
                0f);

        private void ApplyPresentationFacing()
        {
            if (_presentation == null || _buildingPackage == null ||
                _session?.Data == null)
            {
                return;
            }

            var presentationFacing = _buildingPackage.PresentationFacing(
                _facing, _session.Data.RotationQuarterTurns);
            _presentation.ApplyFacing(presentationFacing);
            _presentation.SetBuildingRotation(
                _session.Data.RotationQuarterTurns);
            ApplyPresentationAppearance();
        }

        private void ApplyPresentationAppearance()
        {
            if (_presentation == null)
            {
                return;
            }

            _presentation.SetArtworkSource(ArtworkSource);
            _presentation.SetTimeOfDay(TimeOfDay);
            _presentation.SetRain(IsRaining);
            _presentation.SetWetReflection(RoadWetness,
                WetReflectionDirectionFor(new Vector3(
                    _session.Data.CellX, 0f, _session.Data.CellZ)));
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private void EnsureBuildingPackage(string buildingId)
        {
            var entry = BuildingCatalog.Find(buildingId);
            var package =
                HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
            if (_buildingPackage != null && _buildingPackage.Id == package.Id)
            {
                return;
            }

            if (_proxy != null) DestroyForCurrentMode(_proxy.gameObject);
            if (_buildingDepthOccluder != null)
                DestroyForCurrentMode(_buildingDepthOccluder.gameObject);
            if (_projectedShadow != null)
                DestroyForCurrentMode(_projectedShadow.gameObject);
            if (_presentation != null) DestroyForCurrentMode(_presentation.gameObject);
            if (_selectionFootprint != null)
                DestroyForCurrentMode(_selectionFootprint.gameObject);
            _selectionFrontMarker = null;
            if (_registrationDiagnostics != null)
                DestroyForCurrentMode(_registrationDiagnostics.gameObject);

            _buildingPackage = package;
            if (_camera != null)
            {
                BuildProxyBuilding();
                BuildProjectedShadow();
                BuildHybridPresentation();
                BuildSelectionFootprint();
                BuildRegistrationDiagnostics();
                // A package swap happens during building selection. Keep the
                // user's exact camera transform and only orient the newly
                // constructed billboard to that existing view.
                ApplyPresentationFacing();
                _presentation?.AlignToCamera();
                UpdateFloraShadowSun();
            }
        }

        private void UpdateFloraShadowSun()
        {
            if (_floraShadowSun == null) return;
            var directionOffset = _buildingPackage != null
                ? _buildingPackage.ShadowDirectionOffsetDegrees
                : 0f;
            _floraShadowSun.enabled = !IsRaining &&
                TimeOfDay != TimeOfDayPreset.Night;
            _floraShadowSun.transform.rotation =
                Quaternion.Euler(0f, directionOffset, 0f) *
                ExperimentalBuilding3DSunRotation();
        }

        private static void DestroyForCurrentMode(GameObject target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        private static GameObject Cube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool castShadows = false)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            var renderer = cube.GetComponent<Renderer>();
            renderer.material = Material(color, 0.55f);
            renderer.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;
            return cube;
        }

        private static void AddLine(Transform parent, Vector3 start, Vector3 end, Material material, float width = 0.018f)
        {
            var lineObject = new GameObject("Grid Line");
            lineObject.transform.SetParent(parent);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = width;
            line.endWidth = width;
            line.material = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static Material Material(Color color, float roughness)
        {
            var shader = Shader.Find("CityForgeV3/WorldColor");
            if (shader == null)
            {
                throw new MissingReferenceException(
                    "Required Resources shader CityForgeV3/WorldColor was not included.");
            }

            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 1f - roughness);
            }

            return material;
        }

        private static Material LotSurfaceMaterial(Color color, int renderQueue)
        {
            var shader = Shader.Find("CityForgeV3/LotSurfaceColor");
            if (shader == null)
            {
                throw new MissingReferenceException(
                    "Required Resources shader CityForgeV3/LotSurfaceColor was not included.");
            }

            return new Material(shader)
            {
                color = color,
                renderQueue = renderQueue
            };
        }

        private static Material ShadowReceivingLotMaterial(Color color)
        {
            var shader = Shader.Find("CityForgeV3/ShadowReceivingLotSurface");
            if (shader == null)
            {
                throw new MissingReferenceException(
                    "CityForge V3 shadow-receiving lot shader is required.");
            }

            var material = new Material(shader)
            {
                color = color,
                renderQueue = 1998
            };
            return material;
        }
    }
}
