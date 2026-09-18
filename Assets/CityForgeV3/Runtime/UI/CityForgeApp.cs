using System;
using System.Collections.Generic;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
  public enum DistrictEditorMode
  {
    Terraform,
    Builder
  }

  public enum LotEditorCategory
  {
    Main,
    Buildings,
    Buildings3D,
    Roads,
    Railroad,
    Paths,
    Water,
    Terrain,
    Flora,
    Props,
    Characters,
    Entertainment,
    Effects,
    BaseTextures,
    OverlayTextures,
    Decals,
    Environment,
    View,
    Transport,
    Vehicles,
    Boats,
    Garden
  }

  public enum TerrainSculptMode
  {
    None,
    Raise,
    Lower
  }

  public sealed partial class CityForgeApp : MonoBehaviour
  {
    public const float DefaultTerrainBrushRadiusMeters = 8f;
    public const float DefaultTerrainBrushStrengthMeters = 0.2f;
    public const float MaximumTerrainBrushStrengthMeters = 1.5f;
    private const string StylePath = "CityForgeV3/UI/CityForgeV3";
    private const bool WarnAboutUnsavedLotChanges = false;
    private UIDocument _document;
    private VisualElement _root;
    private LotWorldController _lotWorld;
    private DistrictWorldController _districtWorld;
    private string _districtWorldTileId = "";
    private string _districtWorldLotId = "";
    private string _districtWorldCompositionKey = "";
    private string _lotStatus = "";
    private AppScreen _currentScreen;
    private RegionSaveData _openRegion;
    private readonly Dictionary<string, Texture2D> _districtLotPreviewTextures =
        new(StringComparer.OrdinalIgnoreCase);
    private string _selectedRegionTileId = "";
    private Vector2 _regionMapScrollOffset;
    private bool _regionMapScrollInitialized;
    private bool _openRegionWasCreatedThisSession;
    private string _terraformCategory = "Terrain";
    private string _terraformTool = "Raise";
    private DistrictEditorMode _districtEditorMode = DistrictEditorMode.Terraform;
    private string _builderCategory = "Roads";
    private string _builderTool = DistrictRoadPlacementModel.DirtFamily;
    private bool _districtSimulationPaused;
    private bool _districtRoadPointerDown;
    private Vector2Int _lastDistrictRoadDragCell;
    private bool _hasSelectedDistrictRoad;
    private Vector2Int _selectedDistrictRoadCell;
    private int _districtYear = 1788;
    private string _pendingFounderBuildingId = "";
    private string _pendingDistrictLotId = "";
    private string _pendingDistrictLotName = "";
    private string _hoveredDistrictLotInstanceId = "";
    private string _selectedDistrictLotInstanceId = "";
    private string _pendingDistrictFloraId = "";
    private int _pendingDistrictFloraMode;
    private string _selectedDistrictFloraInstanceId = "";
    private string _activeDistrictRandomFloraGroupId = "";
    private bool _districtFloraPointerDown;
    private Vector2 _districtFloraDragOffset;
    private readonly List<DistrictSelectionRef> _districtSelection = new();
    private bool _districtMarqueeActive;
    private bool _districtSelectionDragActive;
    private bool _districtSelectionMovedRiver;
    private Vector2 _districtSelectionStart;
    private Vector2 _districtSelectionLast;
    private Vector2 _districtSelectionScreenStart;
    private Vector2 _districtSelectionScreenLast;
    private int _districtSelectionPointerId = -1;
    private VisualElement _districtSelectionSurface;
    private Vector2 _districtSelectionGridRemainder;
    private VisualElement _districtSelectionMarquee;
    private Vector2 _terraformPanOffset;
    private Vector2Int _districtEdgePanDirection;
    private bool _districtInfoVisible;
    private bool _districtInterfaceVisible = true;
    private DistrictZoomLevel _terraformZoomLevel = DistrictZoom.DefaultLevel;
    private LotEditorCategory _lotEditorCategory = LotEditorCategory.Main;
    private bool _lotEditorCategoryExpanded;
    private bool _hasOpenLot;
    private BuildingUseCategory _buildingUseCategory = BuildingUseCategory.Residential;
    private string _buildingSubcategory = string.Empty;
    private bool _roadPointerDown;
    private bool _roadDragStarted;
    private bool _railroadPointerDown;
    private bool _railroadDragStarted;
    private Vector2Int _lastRailroadDragCell;
    private bool _outsideConnectorDragCreated;
    private Vector2Int _lastRoadDragCell;
    private bool _buildingPointerDown;
    private bool _building3DPointerDown;
    private bool _building3DPlacementPending;
    private bool _buildingDragStarted;
    private bool _buildingPlacementPending;
    private bool _floraPointerDown;
    private bool _floraDragStarted;
    private bool _waterPointerDown;
    private bool _waterArrowPointerDown;
    private int _waterOrientationClick;
    private bool _runtimeObjectPointerDown;
    private bool _propPointerDown;
    private bool _propDragStarted;
    private bool _buildingPropPointerDown;
    private bool _buildingPropDragStarted;
    private bool _buildingPropPlacementPointerDown;
    private bool _overlayPointerDown;
    private bool _overlayDragPainted;
    private bool _cameraPanToolActive;
    private bool _cameraPanPointerDown;
    private Vector2 _cameraPanLastPosition;
    private static Texture2D _cameraPanCursorTexture;
    private bool _roadFamilyExpanded;
    private bool _roadMaterialsExpanded;
    private bool _roadShapeExpanded;
    private bool _roadTestVehiclesExpanded = true;
    private bool _roadTrafficExpanded;
    private bool _roadEditExpanded;
    private bool _roadViewExpanded;
    private bool _railroadTransitExpanded;
    private bool _streetcarTransitExpanded = true;
    private string _pendingRoadMaterialId = RoadMaterialCatalog.DefaultRoadId;
    private string _pendingSidewalkMaterialId = RoadMaterialCatalog.DefaultSidewalkId;
    private RoadMarkingStyle _pendingRoadMarkingStyle = RoadMarkingStyle.SingleDotted;
    private RoadLaneMarkingStyle _pendingRoadLaneMarkingStyle = RoadLaneMarkingStyle.Lines;
    private RoadCenterMarkingStyle _pendingRoadCenterMarkingStyle = RoadCenterMarkingStyle.DoubleLines;
    private bool _pendingApplyRoadMaterialsToAll;
    private string _placementFloraId = "mature-oak";
    private string _placementPropId = "";
    private string _placementEffectId = "";
    private string _placementBuildingPropId = "";
    private string _placementOverlayTextureId = "";
    private string _placementDecalCategory = "";
    private TerrainSculptMode _terrainSculptMode;
    private float _terrainBrushRadiusMeters = DefaultTerrainBrushRadiusMeters;
    private float _terrainBrushStrengthMeters = DefaultTerrainBrushStrengthMeters;
    private bool _terrainPointerDown;
    private Label _decalCursor;
    private Action _pendingDocumentAction;
    private Action _refreshBuildingFocusOverlay;
    private bool _lotEditorRefreshScheduled;
    private int _pendingLotWidthCells = -1;
    private int _pendingLotDepthCells = -1;
    private VisualElement _lotContextMenu;
    private VisualElement _toolCategoryHoverInfo;
    private Vector2Int _lotContextCell;
    private int _hoveredLotStripDeleteAction;
    private Vector2Int _lastPhysicalCharacterDirection;
    private bool _lotInspectorVisible = true;
    private Vector2 _lotInspectorPosition = new(-1f, -1f);

    public LotEditorCategory ActiveLotEditorCategory => _lotEditorCategory;
    public bool IsLotEditorCategoryExpanded => _lotEditorCategoryExpanded;

#if UNITY_EDITOR
        public void OpenGildedAgeMansionLodQa()
        {
            EnsureLotWorld();
            // QA can be launched before any lot has been opened. EnsureLotWorld
            // intentionally builds an initially hidden world in that case, but
            // lot creation schedules runtime work and therefore needs an active
            // host object.
            _lotWorld.SetVisible(true);
            _lotWorld.CreateGildedAgeMansionLODTestLot();
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Night);
            _lotWorld.SetZoomLevel(LotZoomLevel.Close);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "Gilded Age Mansion textured night-light QA";
            Show(AppScreen.LotEditor);
        }

        public void OpenArtDecoCornerPlacementQa()
        {
            Show(AppScreen.LotEditor);
            _lotEditorCategory = LotEditorCategory.Buildings;
            _lotEditorCategoryExpanded = true;
            _buildingUseCategory = BuildingUseCategory.Commercial;
            _lotWorld.ConfigureLot("Art Deco placement QA", LotType.Commercial,
                4, 4, LotEraCatalog.DefaultId);
            _hasOpenLot = true;
            _lotWorld.PlaceBuildingAtCenter(
                "cityforge.base.building.commercial.art_deco_corner_building_01");
            Show(AppScreen.LotEditor);
        }

        public void OpenBuildingInspectionQa(string buildingId)
        {
            EnsureLotWorld();
            Show(AppScreen.LotEditor);
            _lotEditorCategory = LotEditorCategory.Buildings;
            _lotEditorCategoryExpanded = true;
            var catalogEntry = BuildingCatalog.Find(buildingId);
            _buildingUseCategory = BuildingCatalog.UseCategoryFor(catalogEntry);
            _buildingSubcategory = catalogEntry.Subcategory;
            _lotWorld.ConfigureLot("Building inspection QA", LotType.Residential,
                4, 4, LotEraCatalog.DefaultId);
            _hasOpenLot = true;
            _lotWorld.PlaceBuildingAtCenter(buildingId);
            _lotWorld.SetZoomLevel(LotZoomLevel.Detail);
            Show(AppScreen.LotEditor);
        }

        public bool OpenSavedLotSelectionQa(string lotId)
        {
            EnsureLotWorld();
            Show(AppScreen.LotEditor);
            if (!_lotWorld.LoadLot(lotId)) return false;
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Main;
            _lotEditorCategoryExpanded = true;
            _lotStatus = $"Selection QA • {lotId}";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenSavedLotBuildingRotationQa(string lotId)
        {
            EnsureLotWorld();
            Show(AppScreen.LotEditor);
            if (!_lotWorld.LoadLot(lotId)) return false;
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings;
            _lotEditorCategoryExpanded = true;
            _lotStatus = $"Building rotation QA • {lotId}";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenSavedLotBuildingFocusQa(string lotId)
        {
            EnsureLotWorld();
            Show(AppScreen.LotEditor);
            if (!_lotWorld.LoadLot(lotId)) return false;
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings;
            _lotEditorCategoryExpanded = false;
            _lotStatus = $"Building focus QA • {lotId}";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool BuildingLibraryModalOpenForQa =>
            _root?.Q<VisualElement>("buildings-category-panel") != null;

        public bool OpenExperimentalThreeDimensionalBuildingsQa()
        {
            EnsureLotWorld();
            _lotWorld.CreateExperimental3DBuildingsLot();
            // Exercise the same persisted reconstruction path used by LOAD;
            // otherwise this QA command can hide ordering bugs by testing only
            // freshly-created runtime objects.
            var savedLotId = _lotWorld.CurrentLotId;
            if (!_lotWorld.LoadLot(savedLotId)) return false;
            // Put transparent and opaque receivers on both sides of the pilot
            // so the same lot proves the eastward afternoon and westward
            // morning building shadows without editing the saved test lot.
            _lotWorld.PlaceFloraForQa("maple", -16f, 10f);
            _lotWorld.PlaceFloraForQa("maple", 16f, -10f);
            _lotWorld.PlacePropForQa(
                LotWorldController.OrnateBenchPropId, -15f, -8f);
            _lotWorld.PlacePropForQa(
                LotWorldController.SimpleStreetLamppostPropId, 15f, 8f);
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Morning);
            _lotWorld.SetZoomLevel(LotZoomLevel.Lot);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "3D Buildings experimental lot ready";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenSaltboxLightingQa()
        {
            EnsureLotWorld();
            _lotWorld.NewEmptyLot("Saltbox Lighting QA", LotType.Residential, 4, 4);
            _lotWorld.AddExperimentalBuilding3D("new-england-saltbox-v05", 0f, 0f, 0);
            _lotWorld.SetBaseTexture("grass-lush");
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _lotWorld.SetZoomLevel(LotZoomLevel.Close);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "New England Saltbox — independent window lighting";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenAnimatedBearQa()
        {
            EnsureLotWorld();
            _lotWorld.NewEmptyLot("Animated Bear QA", LotType.Residential,
                3, 3);
            _lotWorld.PlacePropForQa(
                LotWorldController.BearAnimalId, 0f, 0f);
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _lotWorld.SetZoomLevel(LotZoomLevel.Close);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Characters;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "Animated bear rig + walk-cycle QA";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenAnimatedHorseQa()
        {
            EnsureLotWorld();
            _lotWorld.NewEmptyLot("Animated Horse QA", LotType.Residential,
                3, 3);
            _lotWorld.PlacePropForQa(
                LotWorldController.HorseAnimalId, 0f, 0f);
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _lotWorld.SetZoomLevel(LotZoomLevel.Close);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Characters;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "Horse animation foundation • idle, walk, trot";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenArtMuseumLodQa()
        {
            EnsureLotWorld();
            _lotWorld.CreateArtMuseumLODTestLot();
            var savedLotId = _lotWorld.CurrentLotId;
            if (!_lotWorld.LoadLot(savedLotId)) return false;
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _lotWorld.SetZoomLevel(LotZoomLevel.Close);
            _lotWorld.PrepareArtMuseumSurfaceQa();
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "Art Museum LOD lot loaded through saved-lot path";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenIvyTownhouseWhiteLodQa()
        {
            EnsureLotWorld();
            _lotWorld.CreateIvyTownhouseWhiteLODTestLot();
            var savedLotId = _lotWorld.CurrentLotId;
            // An unrelated legacy hybrid-package validation failure can block
            // the saved-lot reload globally. Keep this focused 3D intake QA
            // usable by falling back to the freshly created equivalent lot.
            if (!_lotWorld.LoadLot(savedLotId))
                _lotWorld.CreateIvyTownhouseWhiteLODTestLot();
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Night);
            _lotWorld.SetZoomLevel(LotZoomLevel.Close);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "Ivy Townhouse White LOD + night-light QA";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenPlymouthStoreLodQa()
        {
            EnsureLotWorld();
            _lotWorld.CreatePlymouthStoreLODTestLot();
            var savedLotId = _lotWorld.CurrentLotId;
            if (!_lotWorld.LoadLot(savedLotId))
                _lotWorld.CreatePlymouthStoreLODTestLot();
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _lotWorld.SetZoomLevel(LotZoomLevel.Close);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "Plymouth Store four-LOD package QA";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenPlymouthStoreComparisonQa()
        {
            EnsureLotWorld();
            _lotWorld.CreatePlymouthStoreComparisonTestLot();
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _lotWorld.SetZoomLevel(LotZoomLevel.Close);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "Original and canonical-source Plymouth LOD comparison";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool OpenSurfaceLayersAndRoadShadowsQa()
        {
            EnsureLotWorld();
            _lotWorld.ApplyTrafficTestTemplate();
            _lotWorld.SetBaseTexture("grass-middle");
            _lotWorld.SetOverlayEditorContext(true);
            foreach (var cell in new[]
                     {
                         new Vector2Int(0, 0), new Vector2Int(1, 0),
                         new Vector2Int(0, 1), new Vector2Int(1, 1)
                     })
            {
                _lotWorld.BeginOverlayPaintAtCell(
                    "brick-walkway", cell.x, cell.y);
                _lotWorld.EndOverlayPaint();
            }
            _lotWorld.PlacePropForQa(
                LotWorldController.SimpleStreetLamppostPropId, -4f, 0f);
            _lotWorld.PlacePropForQa(
                LotWorldController.SimpleStreetLamppostPropId, 4f, 0f);
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Afternoon);
            _lotWorld.SetZoomLevel(LotZoomLevel.Close);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.OverlayTextures;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "Surface layers and road shadows QA ready";
            Show(AppScreen.LotEditor);
            return true;
        }

        public bool BuildingFocusOverlayVisibleForQa
        {
            get
            {
                var overlay = _root?.Q<VisualElement>(
                    "building-focus-freeze-overlay");
                return overlay != null &&
                    overlay.resolvedStyle.display != DisplayStyle.None;
            }
        }

        public void RefreshBuildingFocusViewForQa()
        {
            // The deterministic probe selects through the real world hit path,
            // then recomposes the editor once so the inspector and spotlight
            // both describe that newly selected building. This is focus entry,
            // before the no-repaint drag baseline is captured.
            _lotEditorCategory = LotEditorCategory.Buildings;
            _lotEditorCategoryExpanded = false;
            Show(AppScreen.LotEditor);
            _refreshBuildingFocusOverlay?.Invoke();
        }

        public bool OpenSavedLotOcclusionQa(string lotId)
        {
            EnsureLotWorld();
#if UNITY_EDITOR
            _lotWorld.EnsureBuiltForQa();
#endif
            if (!_lotWorld.LoadLot(lotId)) return false;
            _hasOpenLot = true;
            PrepareOcclusionQaView();
            return true;
        }

        public void PrepareOcclusionQaView()
        {
            _lotEditorCategory = LotEditorCategory.Main;
            _lotEditorCategoryExpanded = false;
            // Compose the editor before world setters publish state changes;
            // otherwise a saved-lot load can notify a stale category panel and
            // abort this QA setup before its close framing is applied.
            Show(AppScreen.LotEditor);
            _lotWorld.SetZoomLevel(LotZoomLevel.Detail);
#if UNITY_EDITOR
            _lotWorld.SetQaOrthographicSize(10f);
#endif
            _lotWorld.SetInspectionMode(BuildingInspectionMode.Primitive);
            _lotStatus = "Primitive occlusion QA";
            // QA entry must expose the viewport immediately even if a load
            // notification recomposed the previously open building category.
            _root?.Q<VisualElement>("buildings-category-panel")
                ?.RemoveFromHierarchy();
        }

        public void RotateSelectedBuildingForQa(int direction) =>
            RotateBuilding(direction);
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartApplication()
    {
      if (FindFirstObjectByType<CityForgeApp>() != null)
      {
        return;
      }

      var host = new GameObject("CityForge V3 Application");
      DontDestroyOnLoad(host);
      host.AddComponent<CityForgeApp>();
    }

    private void Awake()
    {
      var panel = Resources.Load<PanelSettings>(
          "CityForgeV3/UI/RuntimePanelSettings");
      if (panel == null)
      {
        throw new MissingReferenceException(
            "Missing reusable City Forge V3 runtime PanelSettings asset.");
      }

      _document = gameObject.AddComponent<UIDocument>();
      _document.panelSettings = panel;
      _root = _document.rootVisualElement;
      _root.name = "city-forge-v3-root";
      _root.AddToClassList("app-root");
      _root.RegisterCallback<KeyDownEvent>(
          OnKeyDown,
          TrickleDown.TrickleDown);
      _root.RegisterCallback<KeyUpEvent>(
          OnKeyUp,
          TrickleDown.TrickleDown);
      var runtimeFont = Font.CreateDynamicFontFromOSFont(
          new[] { "Avenir Next", "Helvetica Neue", "Arial" },
          16);
      if (runtimeFont != null)
      {
        _root.style.unityFont = runtimeFont;
      }

      var styles = Resources.Load<StyleSheet>(StylePath);
      if (styles != null)
      {
        _root.styleSheets.Add(styles);
      }

      Show(AppScreen.MainMenu);
    }

    private void OnApplicationFocus(bool focused)
    {
      if (!focused) CancelDistrictSelectionPointer();
    }

    private void Update()
    {
      if (!EnsureUiRoot()) return;
      TickDistrictLabor();
      TickDistrictLotBehaviors();
      RefreshDecalCursorForControl(
          Input.GetKey(KeyCode.LeftControl) ||
          Input.GetKey(KeyCode.RightControl));
      if (_currentScreen == AppScreen.LotEditor &&
          _lotWorld != null &&
          _lotWorld.ActiveObjectSelection is
              LotObjectSelectionKind.Flora or LotObjectSelectionKind.Prop &&
          !TextInputHasFocus() && Input.GetKeyDown(KeyCode.R))
        BeginSelectedObjectRepeat();
      PollPhysicalCharacterArrowKeys();
      if (_currentScreen == AppScreen.RegionEditor &&
          !TextInputHasFocus())
        PollRegionArrowKeys();
      if (_currentScreen == AppScreen.DistrictTerraform &&
          !TextInputHasFocus())
      {
        // UI Toolkit does not consistently retain keyboard focus when
        // the player clicks directly in the district Game view. Poll
        // the physical key here so G works regardless of which visual
        // element last owned focus.
        if (_districtMarqueeActive)
        {
          if (Input.GetKeyDown(KeyCode.Escape)) CancelDistrictSelectionPointer();
          return;
        }
        if(_placingBrickworks)
        {
          if(Input.GetKeyDown(KeyCode.Escape)){CancelBrickworksPlacement();return;}
          if(Input.GetKeyDown(KeyCode.R)){RotateBrickworksPreview();return;}
        }
        if (Input.GetKeyDown(KeyCode.R) && RotateSelectedDistrictObject(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1)) return;
        if (IndustryPlacementActive && Input.GetKeyDown(KeyCode.Escape))
        { CancelIndustryPlacement(); return; }
        if (Input.GetKeyDown(KeyCode.Z) &&
            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
             Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
            !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
          UndoDistrictEdit();
          return;
        }
#if UNITY_EDITOR
                if (Input.GetKeyDown(KeyCode.Escape) &&
                    (DistrictResourceDebugMenuOpen || !string.IsNullOrEmpty(_districtResourceDebugPlacement)))
                {
                    CloseDistrictResourceDebugMenu();
                    return;
                }
                if (Input.GetKeyDown(KeyCode.R))
                {
                    if (!DistrictResourceDebugMenuOpen && string.IsNullOrEmpty(_districtResourceDebugPlacement))
                        OpenDistrictResourceDebugMenu();
                    else
                        CloseDistrictResourceDebugMenu();
                    return;
                }
#endif
        if (Input.GetKeyDown(KeyCode.G))
          _districtWorld?.ToggleGridVisibility();
        PollTerraformViewKeys();
      }
      if (_currentScreen != AppScreen.LotEditor ||
          _lotContextMenu == null ||
          _hoveredLotStripDeleteAction == 0 ||
          !Input.GetMouseButtonDown(0)) return;

      var cell = _lotContextCell;
      var deleteColumn = _hoveredLotStripDeleteAction == 2;
      DeleteLotStrip(cell, deleteColumn);
    }

    private bool EnsureUiRoot()
    {
      // Script hot reload clears non-serialized field references while
      // the UIDocument survives. Rebind before composing so QA menu
      // commands cannot leave the Game view with a world but no UI.
      if (_root == null)
      {
        _document ??= GetComponent<UIDocument>();
        _root = _document?.rootVisualElement;
        if (_root == null) return false;
        _root.name = "city-forge-v3-root";
        _root.AddToClassList("app-root");
        _root.RegisterCallback<KeyDownEvent>(
            OnKeyDown, TrickleDown.TrickleDown);
        _root.RegisterCallback<KeyUpEvent>(
            OnKeyUp, TrickleDown.TrickleDown);
        var styles = Resources.Load<StyleSheet>(StylePath);
        if (styles != null)
          _root.styleSheets.Add(styles);
      }
      return true;
    }

    private void Show(AppScreen screen)
    {
      CancelNationalPike();
      CancelIndustryPlacement();CancelBrickworksPlacement();
      if (screen != AppScreen.DistrictTerraform) _districtNotice.Clear();
#if UNITY_EDITOR
      _districtResourceDebugPlacement = "";
#endif
      CancelDistrictSelectionPointer();
      if (screen != AppScreen.DistrictTerraform) ClearDistrictUndo();
      if (!EnsureUiRoot()) return;
      if (_currentScreen == AppScreen.LotEditor &&
          screen != AppScreen.LotEditor && _lotWorld != null && _hasOpenLot)
      {
        var previewPath = _lotWorld.CaptureLotPreview();
        if (!string.IsNullOrWhiteSpace(previewPath) &&
            _districtLotPreviewTextures.Remove(previewPath,
                out var stalePreview) && stalePreview != null)
          Destroy(stalePreview);
      }
      var preserveLotCamera = _currentScreen == AppScreen.LotEditor &&
          screen == AppScreen.LotEditor && _lotWorld != null;
      var preservedCamera = preserveLotCamera
          ? _lotWorld.CaptureCameraFraming()
          : default;
      _currentScreen = screen;
      _refreshBuildingFocusOverlay = null;
      _decalCursor = null;
      _root.Clear();
      _lotWorld?.SetVisible(screen == AppScreen.LotEditor && _hasOpenLot);
      _districtWorld?.SetVisible(screen == AppScreen.DistrictTerraform);

      switch (screen)
      {
        case AppScreen.MainMenu:
          ComposeMainMenu();
          break;
        case AppScreen.RegionEditor:
          ComposeRegionEditor();
          break;
        case AppScreen.DistrictTerraform:
          ComposeDistrictTerraform();
          break;
        case AppScreen.LotEditor:
          ComposeLotEditor();
          break;
      }
      if (preserveLotCamera)
        _lotWorld.RestoreCameraFraming(preservedCamera);
      if (screen == AppScreen.LotEditor && _lotWorld != null)
      {
        // Inform the world about the active tool only after ordinary
        // editor restoration. Tool/category changes deliberately do
        // not own or modify camera framing.
        _lotWorld.SetBuilding3DEditorContext(
            _lotEditorCategory == LotEditorCategory.Buildings3D);
      }
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
      if (_drawingNationalPike && evt.keyCode == KeyCode.Escape)
      { CancelNationalPike(); evt.StopImmediatePropagation(); return; }
      if(_placingBrickworks && (evt.keyCode==KeyCode.R||evt.keyCode==KeyCode.Escape))
      {evt.StopImmediatePropagation();return;}
      if (IndustryPlacementActive && evt.keyCode == KeyCode.Escape)
      { CancelIndustryPlacement(); evt.StopImmediatePropagation(); return; }
#if UNITY_EDITOR
      // Physical polling owns these keys, including events delivered in the same frame.
      if (_currentScreen == AppScreen.DistrictTerraform && !TextInputHasFocus() &&
          (evt.keyCode == KeyCode.R || (evt.keyCode == KeyCode.Escape &&
          (DistrictResourceDebugMenuOpen || !string.IsNullOrEmpty(_districtResourceDebugPlacement)))))
      { evt.StopImmediatePropagation(); return; }
#endif
      if (_pendingLaborCount > 0 && evt.keyCode == KeyCode.Escape) { CancelLaborPlacement(); evt.StopImmediatePropagation(); return; }
      if (evt.keyCode == KeyCode.Escape && !string.IsNullOrWhiteSpace(_behaviorAnchorId))
      { _behaviorAnchorId = ""; _lotStatus = "Behavior point selection cancelled"; RefreshLotEditor(); evt.StopImmediatePropagation(); return; }
      if (_root?.Q<VisualElement>("cf-choice-overlay") != null) return;
      if (_currentScreen == AppScreen.DistrictTerraform && !TextInputHasFocus() &&
          evt.keyCode == KeyCode.Z && (evt.ctrlKey || evt.commandKey))
      {
        // Physical GetKeyDown owns undo: UI events can repeat or arrive
        // in the same frame, and must never consume a second history entry.
        evt.StopImmediatePropagation();
        return;
      }
      if (_districtMarqueeActive)
      {
        if (evt.keyCode == KeyCode.Escape) CancelDistrictSelectionPointer();
        evt.StopImmediatePropagation();
        return;
      }
      if (_currentScreen == AppScreen.DistrictTerraform &&
          !TextInputHasFocus() &&
          (evt.keyCode is KeyCode.Delete or KeyCode.Backspace))
      {
        if (DeleteDistrictSelection()) evt.StopPropagation();
        return;
      }
      if (_currentScreen == AppScreen.DistrictTerraform &&
          !TextInputHasFocus() && evt.keyCode == KeyCode.Tab)
      {
        if (RerollDistrictRandomFlora()) evt.StopPropagation();
        return;
      }
      if (_currentScreen != AppScreen.LotEditor ||
          _lotWorld == null)
      {
        return;
      }

      if (evt.keyCode is KeyCode.LeftControl or KeyCode.RightControl)
        RefreshDecalCursorForControl(true);

      if (TextInputHasFocus())
      {
        return;
      }

      if (evt.keyCode == KeyCode.Z && (evt.ctrlKey || evt.commandKey))
      {
        _lotStatus = _lotWorld.UndoLastDecal()
            ? "Last decal removed"
            : "No placed decal to undo";
        Show(AppScreen.LotEditor);
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.R &&
          _lotWorld.ActiveObjectSelection is
              LotObjectSelectionKind.Flora or LotObjectSelectionKind.Prop)
      {
        BeginSelectedObjectRepeat();
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.Escape)
      {
        if (_lotWorld.WaterPlacementActive)
        {
          _lotWorld.CancelWaterPlacement();
          _lotStatus = "Swamp drawing cancelled";
          evt.StopPropagation();
          return;
        }
        if (_lotWorld.FloraRepeatLineActive)
        {
          _lotWorld.CancelFloraRepeatLine();
          _lotStatus = "Flora repeat cancelled";
          evt.StopPropagation();
          return;
        }
        if (_lotWorld.PropRepeatLineActive)
        {
          _lotWorld.CancelPropRepeatLine();
          _lotStatus = "Prop repeat cancelled";
          evt.StopPropagation();
          return;
        }
        if (_lotWorld.RoadRoutePlanningActive)
        {
          _lotWorld.CancelRoadRoutePlan();
          _lotStatus = "Road route planning cancelled";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (_lotWorld.CirculationPathDrawingActive)
        {
          _lotWorld.EndCirculationPathDrawing();
          _lotStatus = "Path finished • click to start another";
          evt.StopPropagation();
          return;
        }
        if (_cameraPanToolActive)
        {
          SetCameraPanTool(false);
          evt.StopPropagation();
          return;
        }
        DeselectAll();
        evt.StopPropagation();
      }
      else if ((evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter) &&
               _lotWorld.WaterPlacementActive)
      {
        _lotStatus = _lotWorld.FinishSwampWaterPlacement()
            ? "Swamp water created • boundary saved with the lot"
            : "Add at least three boundary points enclosing an area";
        Show(AppScreen.LotEditor);
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.Tab &&
               _lotEditorCategory == LotEditorCategory.Effects &&
               !string.IsNullOrWhiteSpace(_placementEffectId))
      {
        _lotWorld.ToggleWindowLightPlacementSize();
        _lotStatus = _lotWorld.LargeWindowLightPlacement
            ? "Large Window Light • 2 m wide × 4 m tall • Tab returns to normal"
            : "Window Light • 1 m wide × 2 m tall • Tab enlarges";
        Show(AppScreen.LotEditor);
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.S &&
               _lotEditorCategory == LotEditorCategory.Roads &&
               _lotWorld.LotType == LotType.Neighborhood)
      {
        _lotStatus = _lotWorld.BeginRoadRoutePlan()
            ? "Road start pinned • move the mouse and click the endpoint • Esc cancels"
            : "Move the road cursor onto the lot before pressing S";
        Show(AppScreen.LotEditor);
        evt.StopPropagation();
      }
      else if (evt.keyCode is KeyCode.LeftArrow or KeyCode.RightArrow or
               KeyCode.UpArrow or KeyCode.DownArrow)
      {
        if (_lotWorld.ActiveObjectSelection ==
                LotObjectSelectionKind.BuildingProp &&
            evt.keyCode is KeyCode.LeftArrow or KeyCode.RightArrow)
        {
          var clockwise = evt.keyCode == KeyCode.LeftArrow;
          _lotStatus = _lotWorld.RotateSelectedBuildingProp45Degrees(
                  clockwise ? 1 : -1)
              ? $"Building prop rotated {(clockwise ? "clockwise" : "counter-clockwise")}"
              : "Select a building prop before rotating";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.OverlayTextures &&
            _lotWorld.SelectedOverlayTextureIndex >= 0 &&
            evt.keyCode is KeyCode.LeftArrow or KeyCode.RightArrow)
        {
          var clockwise = evt.keyCode == KeyCode.LeftArrow;
          _lotStatus = _lotWorld.RotateSelectedOverlayTexture(
                  clockwise ? 1 : -1)
              ? $"Overlay rotated {(clockwise ? "clockwise" : "counter-clockwise")}"
              : "Select an overlay tile before rotating";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        // A building may remain selected while the user works in a
        // different tool. Do not let that stale selection steal the
        // pan arrows and appear to rotate the lot unexpectedly.
        if (_lotEditorCategory == LotEditorCategory.Buildings3D &&
            _lotWorld.SelectedBuilding3DIndex >= 0)
        {
          if (evt.keyCode == KeyCode.LeftArrow)
            RotateBuilding(1);
          else if (evt.keyCode == KeyCode.RightArrow)
            RotateBuilding(-1);
          evt.StopPropagation();
          return;
        }
        var horizontal = evt.keyCode == KeyCode.LeftArrow ? -1 :
            evt.keyCode == KeyCode.RightArrow ? 1 : 0;
        var vertical = evt.keyCode == KeyCode.UpArrow ? 1 :
            evt.keyCode == KeyCode.DownArrow ? -1 : 0;
        if (_lotWorld.SelectedPropIsThreeDimensionalCharacter)
        {
          var direction = PhysicalCharacterArrowDirection();
          if (direction == Vector2Int.zero)
            direction = new Vector2Int(horizontal, vertical);
          if (direction != _lastPhysicalCharacterDirection)
            MoveCategorySelectionOrPan(direction.x, direction.y);
          _lastPhysicalCharacterDirection = direction;
          evt.StopPropagation();
          return;
        }
        MoveCategorySelectionOrPan(horizontal, vertical);
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.Space &&
               _lotWorld.SelectedPropIsThreeDimensionalCharacter)
      {
        _lotStatus = _lotWorld.StopSelectedCharacter()
            ? _lotWorld.SelectedCharacterAnimationState == "sit"
                ? "Character seated on the nearby bench"
                : "Character stopped • idle"
            : "Select a 3D character first";
        _lastPhysicalCharacterDirection = Vector2Int.zero;
        evt.StopPropagation();
      }
      else if ((evt.keyCode is KeyCode.L or KeyCode.H) &&
               _lotWorld.ActiveObjectSelection ==
               LotObjectSelectionKind.Flora)
      {
        var sinking = evt.keyCode == KeyCode.L;
        var adjusted = _lotWorld.AdjustSelectedFloraSink(sinking);
        _lotStatus = adjusted
            ? $"Flora depth: {_lotWorld.SelectedFloraSinkDepth:0.00} m below ground"
            : sinking
                ? "Flora is at the maximum sink depth"
                : "Flora is already at ground level";
        evt.StopPropagation();
      }
      else if (evt.keyCode is >= KeyCode.Alpha1 and <= KeyCode.Alpha5)
      {
        SetTimeOfDay(
            (TimeOfDayPreset)((int)evt.keyCode - (int)KeyCode.Alpha1));
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.Alpha6)
      {
        SetArtworkSource(
            _lotWorld.ArtworkSource == BuildingArtworkSource.Approved
                ? BuildingArtworkSource.NeutralPilot
                : BuildingArtworkSource.Approved);
        evt.StopPropagation();
      }
#if UNITY_EDITOR
            else if (evt.keyCode == KeyCode.F9)
            {
                OpenExperimentalThreeDimensionalBuildingsQa();
                evt.StopPropagation();
            }
#endif
      else if (evt.keyCode == KeyCode.F10)
      {
        ApplyTrafficTemplate();
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.D)
      {
        ToggleRegistrationDiagnostics();
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.G)
      {
        _lotWorld.ToggleGridVisibility();
        _lotStatus = _lotWorld.GridVisible
            ? "Construction grid visible"
            : "Construction grid hidden";
        Show(AppScreen.LotEditor);
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.T)
      {
        ToggleTopDownView();
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.R &&
               _lotWorld.ActiveObjectSelection ==
               LotObjectSelectionKind.BuildingProp)
      {
        _lotStatus = _lotWorld.RotateSelectedBuildingProp45Degrees(1)
            ? "Building prop rotated 45°"
            : "Select a building prop before rotating";
        Show(AppScreen.LotEditor);
        evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.O &&
               _lotWorld.ActiveObjectSelection ==
               LotObjectSelectionKind.BuildingProp)
      {
        _lotStatus = _lotWorld.ToggleSelectedBuildingPropDoor()
            ? "Storefront door toggled"
            : "This building prop does not have an operable door";
        Show(AppScreen.LotEditor);
        evt.StopPropagation();
      }
      else if (evt.keyCode is KeyCode.Delete or KeyCode.Backspace)
      {
        if (DeleteActiveSelection()) evt.StopPropagation();
      }
      else if (evt.keyCode == KeyCode.O &&
               _lotEditorCategory == LotEditorCategory.Roads &&
               _lotWorld.SelectedRoadCanConnectOutside)
      {
        if (_lotWorld.SelectedOutsideConnector == null)
          SetOutsideConnector(RoadTrafficFlow.TwoWay);
        else
          RemoveOutsideConnector();
        evt.StopPropagation();
      }
      else if (evt.keyCode is >= KeyCode.Alpha7 and <= KeyCode.Alpha9)
      {
        SetLotType((LotType)((int)evt.keyCode - (int)KeyCode.Alpha7));
        evt.StopPropagation();
      }
      else if (evt.keyCode is KeyCode.Minus or KeyCode.KeypadMinus)
      {
        StepZoom(1);
        evt.StopPropagation();
      }
      else if (evt.keyCode is KeyCode.Equals or KeyCode.KeypadPlus)
      {
        StepZoom(-1);
        evt.StopPropagation();
      }
      else if (_lotWorld.HasBuilding && evt.keyCode == KeyCode.Q)
      {
        RotateBuilding(-1);
        evt.StopPropagation();
      }
      else if (_lotWorld.HasBuilding && evt.keyCode == KeyCode.E)
      {
        RotateBuilding(1);
        evt.StopPropagation();
      }
    }

    private void OnKeyUp(KeyUpEvent evt)
    {
      if (evt.keyCode is KeyCode.LeftControl or KeyCode.RightControl)
        RefreshDecalCursorForControl(false);
      // Repeat remains active after R is released. The next click sets
      // the endpoint; Escape cancels it.
    }

    private void RefreshDecalCursorForControl(bool controlHeld)
    {
      if (_decalCursor == null ||
          _currentScreen != AppScreen.LotEditor ||
          _lotEditorCategory != LotEditorCategory.Decals ||
          string.IsNullOrWhiteSpace(_placementDecalCategory)) return;
      _decalCursor.text = controlHeld ? "▱" : "✎";
      _decalCursor.tooltip = controlHeld
          ? "Partial decal eraser — click while holding Control"
          : "Decal paintbrush — hold Control to erase";
    }

    private void BeginSelectedObjectRepeat()
    {
      if (_lotWorld == null || _lotWorld.FloraRepeatLineActive ||
          _lotWorld.PropRepeatLineActive) return;
      if (_lotWorld.ActiveObjectSelection == LotObjectSelectionKind.Prop &&
          _lotWorld.BeginSelectedPropRepeatLine())
      {
        _lotStatus = "PROP REPEAT string active • move the mouse • click once to place joined copies";
      }
      else if (_lotWorld.BeginSelectedFloraRepeatLine())
      {
        _lotStatus = "REPEAT string active • move the mouse • click once to plant the row";
      }
      else
      {
        _lotStatus = "Select a placed flora item or prop before pressing R";
      }
    }

    private void PollPhysicalCharacterArrowKeys()
    {
      if (_currentScreen != AppScreen.LotEditor || _lotWorld == null ||
          !_lotWorld.SelectedPropIsThreeDimensionalCharacter ||
          TextInputHasFocus())
      {
        _lastPhysicalCharacterDirection = Vector2Int.zero;
        return;
      }
      var direction = PhysicalCharacterArrowDirection();
      if (direction == Vector2Int.zero)
      {
        _lastPhysicalCharacterDirection = Vector2Int.zero;
        return;
      }
      if (direction == _lastPhysicalCharacterDirection) return;
      _lastPhysicalCharacterDirection = direction;
      _lotStatus = _lotWorld.WalkSelectedCharacter(direction.x, direction.y)
          ? "Character walking • combine arrow keys for diagonals • Space stops"
          : "Character cannot walk in that direction";
    }

    private static Vector2Int PhysicalCharacterArrowDirection() =>
        EightWayCharacterDirection(
            Input.GetKey(KeyCode.LeftArrow),
            Input.GetKey(KeyCode.RightArrow),
            Input.GetKey(KeyCode.UpArrow),
            Input.GetKey(KeyCode.DownArrow));

    public static Vector2Int EightWayCharacterDirection(
        bool left, bool right, bool up, bool down) => new(
        (right ? 1 : 0) - (left ? 1 : 0),
        (up ? 1 : 0) - (down ? 1 : 0));

    private bool TextInputHasFocus()
    {
      var focused = _root?.focusController?.focusedElement as VisualElement;
      while (focused != null)
      {
        if (focused is TextField || focused.ClassListContains("unity-text-input"))
          return true;
        focused = focused.parent;
      }
      return false;
    }

    private void ComposeMainMenu()
    {
      var screen = Screen("main-menu-screen");
      var background = Resources.Load<Texture2D>("CityForgeV3/Art/MainMenu/background");
      if (background != null)
      {
        screen.style.backgroundImage = new StyleBackground(background);
      }

      var logo = new VisualElement();
      logo.AddToClassList("menu-logo");
      var logoTexture = Resources.Load<Texture2D>("CityForgeV3/Art/MainMenu/logo");
      if (logoTexture != null)
      {
        logo.style.backgroundImage = new StyleBackground(logoTexture);
      }
      screen.Add(logo);

      var menu = new VisualElement();
      menu.AddToClassList("menu-stack");
      menu.Add(CfImageButton.Create(
          "Open Region",
          "CityForgeV3/Art/MainMenu/open-region",
          ComposeLoadRegionBrowser,
          true));
      menu.Add(CfImageButton.Create(
          "New Region",
          "CityForgeV3/Art/MainMenu/new-region",
          ComposeCreateRegionDialog,
          true));
      menu.Add(CfImageButton.Create(
          "Lot Editor",
          "CityForgeV3/Art/MainMenu/lot-editor",
          () => Show(AppScreen.LotEditor),
          true));
      menu.Add(CfImageButton.Create("Water Systems", "CityForgeV3/Art/MainMenu/water-systems", null, false));
      menu.Add(CfImageButton.Create("Mods", "CityForgeV3/Art/MainMenu/mods", null, false));
      screen.Add(menu);

      var utilities = new VisualElement();
      utilities.AddToClassList("menu-utilities");
      utilities.Add(CfImageButton.Create(
          "Settings",
          "CityForgeV3/Art/MainMenu/settings",
          null,
          false,
          "utility"));
      utilities.Add(CfImageButton.Create(
          "Quit Game",
          "CityForgeV3/Art/MainMenu/stats",
          RequestQuit,
          true,
          "utility"));
      screen.Add(utilities);
      _root.Add(screen);
    }

    public static void QuitApplication()
    {
      Application.Quit();
    }

    private void ComposeLotEditor()
    {
      EnsureLotWorld();
      _lotWorld.SetVisible(_hasOpenLot);
      _lotWorld.SetBuildingEditorContext(
          _lotEditorCategory == LotEditorCategory.Buildings,
          _lotEditorCategory == LotEditorCategory.Roads);
      _lotWorld.SetFloraEditorContext(
          _lotEditorCategory == LotEditorCategory.Flora);
      _lotWorld.SetPropEditorContext(
          _lotEditorCategory is LotEditorCategory.Garden or LotEditorCategory.Props or
              LotEditorCategory.Characters or
              LotEditorCategory.Entertainment or LotEditorCategory.Boats);
      _lotWorld.SetBuildingPropEditorContext(
          _lotEditorCategory == LotEditorCategory.Props);
      _lotWorld.SetOverlayEditorContext(
          _lotEditorCategory == LotEditorCategory.OverlayTextures);
      _lotWorld.SetCirculationEditorContext(
          _lotEditorCategory == LotEditorCategory.Paths);
      _lotWorld.SetRoadEditorContext(
          _lotEditorCategory == LotEditorCategory.Roads);
      _lotWorld.SetGridEditorContext(
          _lotEditorCategory is LotEditorCategory.Buildings or
              LotEditorCategory.Buildings3D or
              LotEditorCategory.Roads or LotEditorCategory.Railroad or
              LotEditorCategory.Paths or LotEditorCategory.Water or
              LotEditorCategory.Terrain or
              LotEditorCategory.Flora or LotEditorCategory.Garden or
              LotEditorCategory.Props or LotEditorCategory.Characters or
              LotEditorCategory.Entertainment or LotEditorCategory.Boats or
              LotEditorCategory.BaseTextures or
              LotEditorCategory.OverlayTextures or
              LotEditorCategory.Decals);

      var screen = Screen("lot-editor-screen");
      screen.focusable = true;
      screen.RegisterCallback<PointerDownEvent>(evt =>
      {
        if (!_cameraPanToolActive || evt.button != 0 ||
                  !string.IsNullOrWhiteSpace(_placementBuildingPropId) ||
                  evt.position.y <= 70f || evt.position.x <= 100f ||
                  evt.position.x >= screen.resolvedStyle.width - 330f) return;
        _cameraPanPointerDown = true;
        _cameraPanLastPosition = new Vector2(
                  evt.position.x, evt.position.y);
        screen.CapturePointer(evt.pointerId);
        evt.StopImmediatePropagation();
      }, TrickleDown.TrickleDown);
      screen.RegisterCallback<PointerMoveEvent>(evt =>
      {
        if (!_cameraPanPointerDown) return;
        var pointerPosition = new Vector2(
                  evt.position.x, evt.position.y);
        var pointerDelta = pointerPosition - _cameraPanLastPosition;
        _lotWorld.PanCameraViewport(
                  pointerDelta,
                  new Vector2(screen.resolvedStyle.width,
                      screen.resolvedStyle.height));
        _cameraPanLastPosition = pointerPosition;
        evt.StopImmediatePropagation();
      }, TrickleDown.TrickleDown);
      screen.RegisterCallback<PointerUpEvent>(evt =>
      {
        if (!_cameraPanPointerDown || evt.button != 0) return;
        _cameraPanPointerDown = false;
        if (screen.HasPointerCapture(evt.pointerId))
          screen.ReleasePointer(evt.pointerId);
        _lotStatus = "Camera repositioned • hand remains active";
        evt.StopImmediatePropagation();
      }, TrickleDown.TrickleDown);
      var timeSpec = TimeOfDayLighting.For(_lotWorld.TimeOfDay);
      var timeGrade = new VisualElement
      {
        name = "time-of-day-grade",
        pickingMode = PickingMode.Ignore
      };
      timeGrade.AddToClassList("time-of-day-grade");
      void ApplyEnvironmentGrade()
      {
        var fogIntensity = _lotWorld.RainVisualIntensity;
        var environmentGrade = timeSpec.ScreenTint;
        if (fogIntensity > 0.001f)
        {
          var rainFog = new Color(0.72f, 0.73f, 0.72f, 1f);
          environmentGrade = Color.Lerp(environmentGrade, rainFog,
              0.38f * fogIntensity);
          environmentGrade.a = Mathf.Clamp01(
              timeSpec.ScreenTint.a + 0.24f * fogIntensity);
        }
        timeGrade.style.backgroundColor = environmentGrade;
      }
      ApplyEnvironmentGrade();
      timeGrade.schedule.Execute(ApplyEnvironmentGrade).Every(33);
      screen.Add(timeGrade);

      var viewportInput = new VisualElement
      {
        name = "road-grid-viewport-input",
        pickingMode = PickingMode.Position
      };
      viewportInput.style.position = Position.Absolute;
      viewportInput.style.left = 0f;
      viewportInput.style.right = 0f;
      viewportInput.style.top = 0f;
      viewportInput.style.bottom = 0f;

      // Keep placement mode visible even before the world ray has found
      // a valid facade. The world-space square supplies exact snapping;
      // this screen-space reticle makes the armed tool unmistakable.
      var effectCursor = new VisualElement
      {
        name = "window-light-placement-cursor",
        pickingMode = PickingMode.Ignore
      };
      effectCursor.style.position = Position.Absolute;
      effectCursor.style.width = 18f;
      effectCursor.style.height = 18f;
      effectCursor.style.borderTopWidth = 2f;
      effectCursor.style.borderRightWidth = 2f;
      effectCursor.style.borderBottomWidth = 2f;
      effectCursor.style.borderLeftWidth = 2f;
      effectCursor.style.borderTopColor = new Color(0.25f, 0.9f, 1f);
      effectCursor.style.borderRightColor = new Color(0.25f, 0.9f, 1f);
      effectCursor.style.borderBottomColor = new Color(0.25f, 0.9f, 1f);
      effectCursor.style.borderLeftColor = new Color(0.25f, 0.9f, 1f);
      effectCursor.style.backgroundColor =
          new Color(0.08f, 0.65f, 0.82f, 0.28f);
      effectCursor.style.display =
          _lotEditorCategory == LotEditorCategory.Effects &&
          !string.IsNullOrWhiteSpace(_placementEffectId)
              ? DisplayStyle.Flex : DisplayStyle.None;

      var focusDimOverlay = new VisualElement
      {
        name = "building-focus-freeze-overlay",
        pickingMode = PickingMode.Ignore
      };
      focusDimOverlay.style.position = Position.Absolute;
      focusDimOverlay.style.left = 0f;
      focusDimOverlay.style.right = 0f;
      focusDimOverlay.style.top = 0f;
      focusDimOverlay.style.bottom = 0f;
      focusDimOverlay.style.display = DisplayStyle.None;

      VisualElement FocusDimPane(string name)
      {
        var pane = new VisualElement
        {
          name = name,
          pickingMode = PickingMode.Ignore
        };
        pane.style.position = Position.Absolute;
        pane.style.backgroundColor =
            new Color(0.015f, 0.025f, 0.032f, 0.46f);
        focusDimOverlay.Add(pane);
        return pane;
      }

      var focusDimTop = FocusDimPane("building-focus-dim-top");
      var focusDimBottom = FocusDimPane("building-focus-dim-bottom");
      var focusDimLeft = FocusDimPane("building-focus-dim-left");
      var focusDimRight = FocusDimPane("building-focus-dim-right");

      void PositionFocusDimPane(VisualElement pane,
          float left, float top, float width, float height)
      {
        pane.style.left = Mathf.Max(0f, left);
        pane.style.top = Mathf.Max(0f, top);
        pane.style.width = Mathf.Max(0f, width);
        pane.style.height = Mathf.Max(0f, height);
      }

      void RefreshBuildingFocusOverlay()
      {
        var panelSize = new Vector2(
            viewportInput.resolvedStyle.width,
            viewportInput.resolvedStyle.height);
        if (!_lotWorld.BuildingFocusFreezeActive ||
            !_lotWorld.TryGetSelectedBuildingPanelBounds(
                panelSize, out var buildingBounds))
        {
          focusDimOverlay.style.display = DisplayStyle.None;
          return;
        }

        const float spotlightPadding = 14f;
        var left = Mathf.Clamp(
            buildingBounds.xMin - spotlightPadding, 0f, panelSize.x);
        var right = Mathf.Clamp(
            buildingBounds.xMax + spotlightPadding, 0f, panelSize.x);
        var top = Mathf.Clamp(
            buildingBounds.yMin - spotlightPadding, 0f, panelSize.y);
        var bottom = Mathf.Clamp(
            buildingBounds.yMax + spotlightPadding, 0f, panelSize.y);
        focusDimOverlay.style.display = DisplayStyle.Flex;
        PositionFocusDimPane(focusDimTop,
            0f, 0f, panelSize.x, top);
        PositionFocusDimPane(focusDimBottom,
            0f, bottom, panelSize.x, panelSize.y - bottom);
        PositionFocusDimPane(focusDimLeft,
            0f, top, left, bottom - top);
        PositionFocusDimPane(focusDimRight,
            right, top, panelSize.x - right, bottom - top);
      }

      _refreshBuildingFocusOverlay = RefreshBuildingFocusOverlay;
      viewportInput.Add(focusDimOverlay);
      viewportInput.Add(effectCursor);
      var decalCursor = new Label("✎")
      {
        name = "decal-paintbrush-cursor",
        pickingMode = PickingMode.Ignore
      };
      _decalCursor = decalCursor;
      decalCursor.style.position = Position.Absolute;
      decalCursor.style.fontSize = 26f;
      decalCursor.style.color = new Color(0.2f, 1f, 0.78f, 1f);
      decalCursor.style.display =
          _lotEditorCategory == LotEditorCategory.Decals &&
          !string.IsNullOrWhiteSpace(_placementDecalCategory)
              ? DisplayStyle.Flex : DisplayStyle.None;
      viewportInput.Add(decalCursor);
      viewportInput.RegisterCallback<GeometryChangedEvent>(_
          => RefreshBuildingFocusOverlay());
      viewportInput.RegisterCallback<PointerDownEvent>(evt =>
      {
        if (evt.button == 0 && !string.IsNullOrWhiteSpace(_behaviorAnchorId))
        {
          if (_lotWorld.SetBehaviorAnchorFromPanel(_behaviorAnchorId, _behaviorAnchorIsPickup,
                    evt.position, new Vector2(viewportInput.resolvedStyle.width, viewportInput.resolvedStyle.height)))
          {
            _behaviorAnchorId = "";
            _lotStatus = "Behavior point updated";
            RefreshLotEditor();
          }
          evt.StopImmediatePropagation(); return;
        }
        RemoveLotContextMenu();
        _lotWorld.ClearObjectHover();
        var panelSize = new Vector2(
                  viewportInput.resolvedStyle.width,
                  viewportInput.resolvedStyle.height);
        if (evt.button == 0 && _waterOrientationClick > 0)
        {
          if (_lotWorld.PlaceWaterArrow(evt.position, panelSize))
          { _waterOrientationClick = 0; _lotInspectorVisible = true; _lotStatus = "Arrow placed • drag to move or choose a direction"; RefreshLotEditor(); }
          evt.StopPropagation(); return;
        }
        if (evt.button == 0 && !_cameraPanToolActive && _lotWorld.HitWaterArrow(evt.position, panelSize))
        {
          _lotWorld.SelectWaterArrow(evt.position, panelSize); _waterArrowPointerDown = true;
          viewportInput.CapturePointer(evt.pointerId); evt.StopPropagation(); return;
        }
        if (evt.button == 0 &&
                  _lotEditorCategory == LotEditorCategory.Terrain &&
                  _terrainSculptMode != TerrainSculptMode.None)
        {
          _terrainPointerDown = _lotWorld.BeginTerrainStrokeFromPanel(
                    evt.position, panelSize, _terrainBrushRadiusMeters,
                    _terrainBrushStrengthMeters,
                    _terrainSculptMode == TerrainSculptMode.Raise);
          if (_terrainPointerDown)
            viewportInput.CapturePointer(evt.pointerId);
          _lotStatus = _terrainPointerDown
                    ? "Sculpting terrain • release to finish the stroke"
                    : "Keep the terrain brush inside the lot";
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _lotWorld.WaterPlacementActive)
        {
          var added = _lotWorld.AddSwampBoundaryPointFromPanel(
                    evt.position, panelSize);
          var finished = evt.clickCount >= 2 &&
                    _lotWorld.WaterPlacementPointCount >= 3 &&
                    _lotWorld.FinishSwampWaterPlacement();
          _lotStatus = finished
                    ? "Swamp water created • boundary saved with the lot"
                    : added
                        ? $"Swamp boundary: {_lotWorld.WaterPlacementPointCount} points • Enter or double-click to finish"
                        : "Move farther from the previous boundary point";
          if (finished) Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 &&
                  _lotEditorCategory == LotEditorCategory.Water &&
                  _lotWorld.BeginWaterAreaManipulationFromPanel(
                      evt.position, panelSize))
        {
          _waterPointerDown = true;
          viewportInput.CapturePointer(evt.pointerId);
          _lotStatus = _lotWorld.WaterVertexDragActive
                    ? "Swamp vertex selected • drag to reshape"
                    : "Swamp selected • drag a cyan vertex to reshape";
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 &&
                  _lotWorld.FloraRepeatLineActive)
        {
          var planted = _lotWorld.CommitFloraRepeatLineFromPanel(
                    evt.position, panelSize);
          _lotStatus = planted
                    ? $"{_lotWorld.LastFloraLinePlacementCount} flora items planted in a straight line"
                    : "Repeat endpoint was too close • select flora and press R to try again";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _lotWorld.PropRepeatLineActive)
        {
          var placed = _lotWorld.CommitPropRepeatLineFromPanel(
                    evt.position, panelSize);
          _lotStatus = placed
                    ? $"{_lotWorld.LastPropRepeatPlacementCount} joined prop sections added"
                    : "Repeat endpoint is shorter than one prop section";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _building3DPlacementPending)
        {
          _lotWorld.DragBuilding3DFromPanel(evt.position, panelSize);
          _lotWorld.EndBuilding3DDrag();
          _building3DPlacementPending = false;
          _lotStatus = "3D building placed • drag it again to reposition";
          evt.StopPropagation();
          return;
        }
        if ((evt.button == 0 || evt.button == 1) &&
                  _lotEditorCategory == LotEditorCategory.Decals &&
                  evt.ctrlKey)
        {
          _lotStatus = _lotWorld.EraseDecalFromPanel(
                        evt.position, panelSize)
                    ? "Decal partially erased • Ctrl+Z restores the stroke"
                    : "Move the eraser onto a placed decal";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 &&
                  _lotEditorCategory == LotEditorCategory.Decals &&
                  !string.IsNullOrWhiteSpace(_placementDecalCategory))
        {
          var placed = _lotWorld.PlaceRandomDecalFromPanel(
                    _placementDecalCategory, evt.position, panelSize);
          _lotStatus = placed
                    ? $"Random {_placementDecalCategory} decal painted • Ctrl+Z removes it"
                    : _lotWorld.LastDecalPlacementBlockedByBuilding
                        ? "Decals cannot be painted on top of buildings"
                        : _lotWorld.LastDecalPlacementRequiresStreet
                            ? "Street decals can only be painted on placed roads"
                        : "Keep the paintbrush inside the lot";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        // A placed 3D building is directly selectable in every Lot
        // Editor tool, including while the camera hand is active.
        // An armed catalog item must win first so an enclosure's mesh
        // cannot consume a click intended to drop Kong inside it.
        if (evt.button == 0 &&
                  string.IsNullOrWhiteSpace(_placementBuildingPropId) &&
                  (_lotWorld.ActiveObjectSelection ==
                       LotObjectSelectionKind.None ||
                   _lotWorld.ActiveObjectSelection ==
                       LotObjectSelectionKind.BuildingProp) &&
                  !ShouldPrioritizeToolPlacement(_lotEditorCategory,
                      _placementFloraId, _placementPropId) &&
                  !(_lotEditorCategory == LotEditorCategory.Effects &&
                    !string.IsNullOrWhiteSpace(_placementEffectId)) &&
                  !(_lotEditorCategory == LotEditorCategory.Decals &&
                    !string.IsNullOrWhiteSpace(_placementDecalCategory)) &&
                  _lotWorld.BeginExistingObjectManipulationFromPanel(
                      evt.position, panelSize) ==
                      LotObjectSelectionKind.BuildingProp)
        {
          _buildingPropPointerDown = true;
          _buildingPropDragStarted = false;
          viewportInput.CapturePointer(evt.pointerId);
          _lotStatus = "Building prop selected • drag to move • Left/Right rotates";
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Paths &&
                  evt.button == 0)
        {
          var continuing = _lotWorld.CirculationPathDrawingActive;
          if (!_lotWorld.AddCirculationStopFromPanel(
                        evt.position, panelSize)) return;
          _lotStatus = continuing &&
                    !_lotWorld.CirculationPathDrawingActive
                    ? _lotWorld.SelectedPedestrianPathKind ==
                        PedestrianPathKind.Stairs
                        ? "Stairs created • elevated end is the doorway • click to start another"
                        : "Path created • click to start another"
                    : continuing
                    ? "Path created • click to start another"
                    : "Path started • move the mouse and click the next stop • Esc finishes";
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && !_cameraPanToolActive && !_buildingPlacementPending &&
                  !ShouldPrioritizeToolPlacement(_lotEditorCategory, _placementFloraId, _placementPropId) &&
                  string.IsNullOrWhiteSpace(_placementBuildingPropId) &&
                  _lotEditorCategory is not (LotEditorCategory.Effects or LotEditorCategory.Decals or LotEditorCategory.OverlayTextures) &&
                  _lotWorld.TrySelectRuntimeObjectFromPanel(evt.position, panelSize))
        {
          _runtimeObjectPointerDown = true;
          _lotWorld.BeginRuntimeObjectDrag(evt.position, panelSize);
          viewportInput.CapturePointer(evt.pointerId);
          evt.StopPropagation(); return;
        }
        if (evt.button == 0 && !ShouldPrioritizeToolPlacement(
                      _lotEditorCategory, _placementFloraId,
                      _placementPropId) &&
                  !(_lotEditorCategory == LotEditorCategory.Props &&
                    !string.IsNullOrWhiteSpace(_placementBuildingPropId)) &&
                  !(_lotEditorCategory == LotEditorCategory.Effects &&
                    !string.IsNullOrWhiteSpace(_placementEffectId)) &&
                  _lotWorld.BeginBuilding3DDragFromPanel(
                      evt.position, panelSize))
        {
          _building3DPointerDown = true;
          viewportInput.CapturePointer(evt.pointerId);
          _lotStatus = "3D building selected • drag to move • Left/Right rotate 45°";
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 &&
                  !(_lotEditorCategory == LotEditorCategory.Props &&
                    !string.IsNullOrWhiteSpace(_placementBuildingPropId)))
          _lotWorld.DeselectBuilding3D();
        if (_cameraPanToolActive && evt.button == 0 &&
                  string.IsNullOrWhiteSpace(_placementBuildingPropId))
        {
          _cameraPanPointerDown = true;
          _cameraPanLastPosition = new Vector2(
                    evt.position.x, evt.position.y);
          viewportInput.CapturePointer(evt.pointerId);
          _lotStatus = "Camera hand active • drag to pan the lot";
          evt.StopPropagation();
          return;
        }
        if (evt.button == 1 &&
                  _lotEditorCategory is not (LotEditorCategory.Roads or
                      LotEditorCategory.Railroad) &&
                  _lotWorld.UpdateObjectHoverFromPanel(
                      evt.position, panelSize) == LotObjectSelectionKind.None &&
                  _lotWorld.TryMajorCellFromPanel(
                      evt.position, panelSize, out var contextCell))
        {
          ShowLotContextMenu(screen, evt.position, contextCell);
          evt.StopPropagation();
          return;
        }
        var toolPlacementHasPriority = ShouldPrioritizeToolPlacement(
                  _lotEditorCategory, _placementFloraId, _placementPropId) ||
                  (_lotEditorCategory == LotEditorCategory.Buildings &&
                   _buildingPlacementPending) ||
                  (_lotEditorCategory == LotEditorCategory.Props &&
                   !string.IsNullOrWhiteSpace(_placementBuildingPropId)) ||
                  (_lotEditorCategory == LotEditorCategory.Effects &&
                   !string.IsNullOrWhiteSpace(_placementEffectId)) ||
                  (_lotEditorCategory == LotEditorCategory.Decals &&
                   !string.IsNullOrWhiteSpace(_placementDecalCategory));
        if (_lotEditorCategory == LotEditorCategory.Buildings &&
                  evt.button == 0 && _buildingPlacementPending)
        {
          _lotWorld.DragBuildingFromPanel(evt.position, panelSize);
          _lotWorld.EndBuildingDrag();
          _buildingPlacementPending = false;
          RefreshBuildingFocusOverlay();
          _lotStatus = "Building placed • drag it again to reposition";
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && !toolPlacementHasPriority &&
                  _lotEditorCategory != LotEditorCategory.OverlayTextures &&
                  _lotWorld.TryAnimalDestinationFromPanel(evt.position, panelSize, out var animalStatus))
        {
          _lotStatus = animalStatus;
          if (_lotWorld.SelectedPropIsCarriageTeam) RefreshLotEditor();
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && !toolPlacementHasPriority &&
                  _lotEditorCategory != LotEditorCategory.OverlayTextures)
        {
          var selection = _lotWorld.BeginExistingObjectManipulationFromPanel(
                    evt.position, panelSize);
          if (selection == LotObjectSelectionKind.RuntimeObject)
          {
            _runtimeObjectPointerDown = true;
            _lotWorld.BeginRuntimeObjectDrag(evt.position, panelSize);
            viewportInput.CapturePointer(evt.pointerId);
            evt.StopPropagation(); return;
          }
          if (selection == LotObjectSelectionKind.Building)
          {
            _buildingPointerDown = true;
            _buildingDragStarted = false;
            RefreshBuildingFocusOverlay();
            viewportInput.CapturePointer(evt.pointerId);
            evt.StopPropagation();
            return;
          }
          if (selection == LotObjectSelectionKind.Flora)
          {
            _floraPointerDown = true;
            _floraDragStarted = false;
            RefreshBuildingFocusOverlay();
            viewportInput.CapturePointer(evt.pointerId);
            evt.StopPropagation();
            return;
          }
          if (selection == LotObjectSelectionKind.Prop)
          {
            _propPointerDown = true;
            _propDragStarted = false;
            RefreshBuildingFocusOverlay();
            viewportInput.CapturePointer(evt.pointerId);
            evt.StopPropagation();
            return;
          }
          if (selection == LotObjectSelectionKind.BuildingProp)
          {
            _buildingPropPointerDown = true;
            _buildingPropDragStarted = false;
            RefreshBuildingFocusOverlay();
            viewportInput.CapturePointer(evt.pointerId);
            evt.StopPropagation();
            return;
          }
          // Flora cards keep planting armed for repeated placement.
          // An empty click while something is selected should still
          // deselect first; the following click may plant again.
          if (_lotEditorCategory == LotEditorCategory.Flora &&
                    _lotWorld.ActiveObjectSelection !=
                        LotObjectSelectionKind.None)
          {
            _lotWorld.DeselectAll();
            RefreshBuildingFocusOverlay();
            _lotStatus = "Selection cleared • click again to plant";
            evt.StopPropagation();
            return;
          }
        }
        if (evt.button == 0 && !toolPlacementHasPriority &&
                  _lotWorld.BuildingFocusFreezeActive)
        {
          _lotStatus = "Building position applied • context restored";
          _lotWorld.DeselectAll();
          RefreshBuildingFocusOverlay();
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Buildings && evt.button == 0)
        {
          if (!_lotWorld.BeginBuildingDragFromPanel(evt.position, panelSize)) return;
          _buildingPointerDown = true;
          _buildingDragStarted = false;
          RefreshBuildingFocusOverlay();
          viewportInput.CapturePointer(evt.pointerId);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Props &&
                  evt.button == 0 &&
                  !string.IsNullOrWhiteSpace(_placementBuildingPropId))
        {
          _buildingPropPlacementPointerDown = false;
          var previewOnFacade =
                    _lotWorld.UpdateBuildingPropPreviewFromPanel(
                        evt.position, panelSize);
          var placed = previewOnFacade &&
                    _lotWorld.CommitBuildingPropPlacementPreview(
                        _placementBuildingPropId);
          _lotStatus = placed
                    ? "Building prop attached • click it again to move or rotate"
                    : "Move the translucent prop over a building facade and click to attach";
          if (placed)
          {
            _placementBuildingPropId = "";
            _lotWorld.SetBuildingPropPlacementPreview("");
            Show(AppScreen.LotEditor);
          }
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Effects &&
                  evt.button == 0 &&
                  !string.IsNullOrWhiteSpace(_placementEffectId))
        {
          var placed = _lotWorld.PlaceWindowLightFromPanel(
                    evt.position, panelSize);
          var placingCloud = _placementEffectId ==
                    LotWorldController.CloudEffectId;
          _lotStatus = placed
                    ? placingCloud
                        ? "Cloud placed • it will drift across the lot and cast a moving shadow"
                        : "Window Light placed • click again to place another • Tab changes size"
                    : placingCloud
                        ? "Aim over the lot to place the cloud"
                        : "Aim the square at a building or prop surface";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Flora && evt.button == 0)
        {
          if (!_lotWorld.BeginFloraDragFromPanel(
                        _placementFloraId, evt.position, panelSize)) return;
          _floraPointerDown = true;
          _floraDragStarted = false;
          viewportInput.CapturePointer(evt.pointerId);
          evt.StopPropagation();
          return;
        }
        if ((_lotEditorCategory is LotEditorCategory.Garden or LotEditorCategory.Props or
                      LotEditorCategory.Characters or
                      LotEditorCategory.Entertainment or LotEditorCategory.Boats) && evt.button == 0 &&
                  !string.IsNullOrWhiteSpace(_placementPropId))
        {
          if (!_lotWorld.BeginPropDragFromPanel(
                        _placementPropId, evt.position, panelSize)) return;
          _propPointerDown = true;
          _propDragStarted = false;
          viewportInput.CapturePointer(evt.pointerId);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.OverlayTextures && evt.button == 0)
        {
          if (!_lotWorld.BeginOverlayPaintFromPanel(
                        _placementOverlayTextureId, evt.position, panelSize)) return;
          _overlayPointerDown = true;
          _overlayDragPainted = false;
          viewportInput.CapturePointer(evt.pointerId);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Railroad)
        {
          if (evt.button == 1)
          {
            if (!_lotWorld.SelectRoadCellFromPanel(
                          evt.position, panelSize, false)) return;
            _lotStatus = _lotWorld.DeleteStreetcarTrack()
                      ? "Streetcar track deleted"
                      : "No streetcar track at this cell";
            Show(AppScreen.LotEditor);
          }
          else if (evt.button == 0)
          {
            _railroadDragStarted =
                      _lotWorld.PaintStreetcarTrackStrokeCellFromPanel(
                          evt.position, panelSize);
            _railroadPointerDown = true;
            _lastRailroadDragCell = _lotWorld.RoadCursorCell;
            viewportInput.CapturePointer(evt.pointerId);
          }
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory != LotEditorCategory.Roads ||
                  _lotWorld.LotType != LotType.Neighborhood) return;
        if (_lotWorld.RoadRoutePlanningActive && evt.button == 0)
        {
          _lotStatus = _lotWorld.CommitRoadRoutePlanFromPanel(
                        evt.position, panelSize)
                    ? "Planned road created • straight and angled tiles resolved"
                    : "Choose a different endpoint for the planned road";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 1)
        {
          _lotStatus = _lotWorld.EraseRoadCellFromPanel(evt.position, panelSize)
                    ? "Road tile erased • neighbors repaired"
                    : "No road tile at this cell";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button != 0) return;
        var paintedInitialCell = _lotWorld.PaintRoadStrokeCellFromPanel(
                  evt.position, panelSize);
        _roadPointerDown = true;
        _roadDragStarted = paintedInitialCell;
        _outsideConnectorDragCreated = false;
        _lastRoadDragCell = _lotWorld.RoadCursorCell;
        viewportInput.CapturePointer(evt.pointerId);
        evt.StopPropagation();
      });
      viewportInput.RegisterCallback<PointerLeaveEvent>(_ =>
      { if (!_cameraPanToolActive && !_waterArrowPointerDown) UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); });
      viewportInput.RegisterCallback<PointerMoveEvent>(evt =>
      {
        var panelSize = new Vector2(
                  viewportInput.resolvedStyle.width,
                  viewportInput.resolvedStyle.height);
        var arrowHover = _waterOrientationClick == 0 && _lotWorld.HitWaterArrow(evt.position, panelSize);
        if (!_cameraPanToolActive) UnityEngine.Cursor.SetCursor(arrowHover || _waterArrowPointerDown ? CameraPanCursorTexture() : null,
                  arrowHover || _waterArrowPointerDown ? new Vector2(8, 7) : Vector2.zero, CursorMode.Auto);
        if (_waterArrowPointerDown)
        { _lotWorld.DragWaterArrow(evt.position, panelSize); evt.StopPropagation(); return; }
        if (arrowHover) { _lotWorld.ClearObjectHover(); evt.StopPropagation(); return; }
        if (_lotEditorCategory == LotEditorCategory.Terrain &&
                  _terrainSculptMode != TerrainSculptMode.None)
        {
          _lotWorld.UpdateTerrainBrushFromPanel(
                    evt.position, panelSize, _terrainBrushRadiusMeters);
          if (_terrainPointerDown && (evt.pressedButtons & 1) != 0)
            _lotWorld.ContinueTerrainStrokeFromPanel(
                      evt.position, panelSize, _terrainBrushRadiusMeters,
                      _terrainBrushStrengthMeters,
                      _terrainSculptMode == TerrainSculptMode.Raise);
          evt.StopPropagation();
          return;
        }
        if (_lotWorld.WaterPlacementActive)
        {
          _lotWorld.UpdateSwampBoundaryPreviewFromPanel(
                    evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_waterPointerDown)
        {
          _lotWorld.DragSelectedWaterVertexFromPanel(
                    evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_lotWorld.FloraRepeatLineActive)
        {
          _lotWorld.UpdateFloraRepeatLineFromPanel(
                    evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_lotWorld.PropRepeatLineActive)
        {
          _lotWorld.UpdatePropRepeatLineFromPanel(
                    evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Paths)
        {
          _lotWorld.UpdateCirculationPathPreviewFromPanel(
                    evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Effects &&
                  !string.IsNullOrWhiteSpace(_placementEffectId))
        {
          effectCursor.style.left = evt.localPosition.x - 9f;
          effectCursor.style.top = evt.localPosition.y - 9f;
          effectCursor.style.display = DisplayStyle.Flex;
        }
        if (_lotEditorCategory == LotEditorCategory.Decals &&
                  !string.IsNullOrWhiteSpace(_placementDecalCategory))
        {
          decalCursor.text = evt.ctrlKey ? "▱" : "✎";
          decalCursor.style.left = evt.localPosition.x + 5f;
          decalCursor.style.top = evt.localPosition.y - 25f;
          decalCursor.style.display = DisplayStyle.Flex;
          _lotWorld.UpdateDecalPreviewFromPanel(evt.position, panelSize);
        }
        if (_cameraPanPointerDown)
        {
          _cameraPanLastPosition = new Vector2(
                    evt.position.x, evt.position.y);
          evt.StopPropagation();
          return;
        }
        var hoverSuppressed = _runtimeObjectPointerDown || _buildingPointerDown || _floraPointerDown ||
                  _building3DPointerDown ||
                  _building3DPlacementPending ||
                  _propPointerDown || _buildingPropPointerDown ||
                  _overlayPointerDown || _roadPointerDown ||
                  _railroadPointerDown ||
                  ShouldPrioritizeToolPlacement(
                      _lotEditorCategory, _placementFloraId, _placementPropId) ||
                  (_lotEditorCategory == LotEditorCategory.Props &&
                   !string.IsNullOrWhiteSpace(_placementBuildingPropId)) ||
                  (_lotEditorCategory == LotEditorCategory.Effects &&
                   !string.IsNullOrWhiteSpace(_placementEffectId)) ||
                  (_lotEditorCategory == LotEditorCategory.Decals &&
                   !string.IsNullOrWhiteSpace(_placementDecalCategory)) ||
                  _lotEditorCategory == LotEditorCategory.OverlayTextures;
        _lotWorld.UpdateObjectHoverFromPanel(
                  evt.position, panelSize, hoverSuppressed);
        if (_buildingPointerDown)
        {
          if (_lotWorld.DragBuildingFromPanel(evt.position, panelSize))
          {
            _buildingDragStarted = true;
            RefreshBuildingFocusOverlay();
          }
          evt.StopPropagation();
          return;
        }
        if (_building3DPointerDown)
        {
          _lotWorld.DragBuilding3DFromPanel(evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Buildings3D &&
                  _building3DPlacementPending)
        {
          _lotWorld.DragBuilding3DFromPanel(evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Buildings &&
                  _buildingPlacementPending)
        {
          _lotWorld.DragBuildingFromPanel(evt.position, panelSize);
          RefreshBuildingFocusOverlay();
          evt.StopPropagation();
          return;
        }
        if (_runtimeObjectPointerDown)
        {
          _lotWorld.DragRuntimeObjectFromPanel(evt.position, panelSize);
          evt.StopPropagation(); return;
        }
        if (_floraPointerDown)
        {
          if (_lotWorld.DragFloraFromPanel(evt.position, panelSize))
            _floraDragStarted = true;
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Flora)
        {
          _lotWorld.UpdateFloraPreviewFromPanel(
                    evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_propPointerDown)
        {
          if (_lotWorld.DragPropFromPanel(evt.position, panelSize))
            _propDragStarted = true;
          evt.StopPropagation();
          return;
        }
        if (_buildingPropPointerDown)
        {
          if (_lotWorld.DragBuildingPropFromPanel(evt.position, panelSize))
            _buildingPropDragStarted = true;
          evt.StopPropagation();
          return;
        }
        if (_buildingPropPlacementPointerDown)
        {
          _lotWorld.UpdateBuildingPropPreviewFromPanel(
                    evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if ((_lotEditorCategory is LotEditorCategory.Garden or LotEditorCategory.Props or
                  LotEditorCategory.Characters or
                  LotEditorCategory.Entertainment or LotEditorCategory.Boats) &&
                  !string.IsNullOrWhiteSpace(_placementPropId))
        {
          _lotWorld.UpdatePropPreviewFromPanel(evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Props &&
                  !string.IsNullOrWhiteSpace(_placementBuildingPropId))
        {
          _lotWorld.UpdateBuildingPropPreviewFromPanel(
                    evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Effects)
        {
          var onSurface = _lotWorld.UpdateEffectPreviewFromPanel(
                    evt.position, panelSize);
          var cursorColor = onSurface
                    ? new Color(1f, 0.72f, 0.18f)
                    : new Color(0.25f, 0.9f, 1f);
          effectCursor.style.borderTopColor = cursorColor;
          effectCursor.style.borderRightColor = cursorColor;
          effectCursor.style.borderBottomColor = cursorColor;
          effectCursor.style.borderLeftColor = cursorColor;
          evt.StopPropagation();
          return;
        }
        if (_overlayPointerDown)
        {
          if ((evt.pressedButtons & 1) != 0 &&
                    _lotWorld.PaintOverlayStrokeFromPanel(evt.position, panelSize))
            _overlayDragPainted = true;
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory == LotEditorCategory.Railroad)
        {
          if (!_railroadPointerDown || (evt.pressedButtons & 1) == 0)
            return;
          if (!_lotWorld.SelectRoadCellFromPanel(
                        evt.position, panelSize, false) ||
                    _lotWorld.RoadCursorCell == _lastRailroadDragCell)
            return;
          if (_lotWorld.PaintStreetcarTrackStrokeCellFromPanel(
                        evt.position, panelSize))
          {
            _railroadDragStarted = true;
            _lastRailroadDragCell = _lotWorld.RoadCursorCell;
          }
          evt.StopPropagation();
          return;
        }
        if (_lotEditorCategory != LotEditorCategory.Roads ||
                  _lotWorld.LotType != LotType.Neighborhood) return;
        if (_lotWorld.RoadRoutePlanningActive && !_roadPointerDown)
        {
          _lotWorld.UpdateRoadRoutePlanPreviewFromPanel(
                    evt.position, panelSize);
          evt.StopPropagation();
          return;
        }
        var insideLot = _lotWorld.SelectRoadCellFromPanel(
                  evt.position, panelSize, false);
        if (!insideLot)
        {
          if (_roadPointerDown && (evt.pressedButtons & 1) != 0 &&
                    _lotWorld.TryCreateOutsideConnectorFromPanelDrag(
                        evt.position, panelSize))
          {
            _roadDragStarted = true;
            _outsideConnectorDragCreated = true;
          }
          evt.StopPropagation();
          return;
        }
        if (!_roadPointerDown || (evt.pressedButtons & 1) == 0 ||
                  _lotWorld.RoadCursorCell == _lastRoadDragCell) return;
        var paintedRoadCell = _lotWorld.PaintRoadStrokeCellFromPanel(
                  evt.position, panelSize);
        if (paintedRoadCell)
        {
          _roadDragStarted = true;
          _lastRoadDragCell = _lotWorld.RoadCursorCell;
        }
        evt.StopPropagation();
      });
      viewportInput.RegisterCallback<PointerLeaveEvent>(_ =>
      {
        _lotWorld.ClearObjectHover();
        _lotWorld.HideTerrainBrush();
      });
      viewportInput.RegisterCallback<PointerUpEvent>(evt =>
      {
        if (evt.button == 0 && _waterArrowPointerDown)
        {
          _waterArrowPointerDown = false; viewportInput.ReleasePointer(evt.pointerId);
          _lotWorld.EndWaterArrowDrag(); _lotInspectorVisible = true; RefreshLotEditor();
          evt.StopPropagation(); return;
        }

        if (evt.button == 0 && _terrainPointerDown)
        {
          _terrainPointerDown = false;
          if (viewportInput.HasPointerCapture(evt.pointerId))
            viewportInput.ReleasePointer(evt.pointerId);
          _lotStatus = _lotWorld.EndTerrainStroke()
                    ? "Terrain sculpted • changes saved with the lot"
                    : "Terrain stroke made no change";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _waterPointerDown)
        {
          _waterPointerDown = false;
          if (viewportInput.HasPointerCapture(evt.pointerId))
            viewportInput.ReleasePointer(evt.pointerId);
          _lotWorld.EndWaterVertexDrag();
          _lotStatus = "Swamp boundary updated • changes saved with the lot";
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _cameraPanPointerDown)
        {
          _cameraPanPointerDown = false;
          if (viewportInput.HasPointerCapture(evt.pointerId))
            viewportInput.ReleasePointer(evt.pointerId);
          _lotStatus = "Camera repositioned • hand remains active";
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _buildingPointerDown)
        {
          _buildingPointerDown = false;
          var panelSize = new Vector2(
                    viewportInput.resolvedStyle.width,
                    viewportInput.resolvedStyle.height);
          if (_lotWorld.DragBuildingFromPanel(evt.position, panelSize))
            _buildingDragStarted = true;
          viewportInput.ReleasePointer(evt.pointerId);
          _lotWorld.EndBuildingDrag();
          RefreshBuildingFocusOverlay();
          _lotStatus = _buildingDragStarted
                    ? "Building moved on the construction grid"
                    : "Building selected • drag to move";
          _buildingDragStarted = false;
          OpenSelectedBuildingInspector();
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _building3DPointerDown)
        {
          _building3DPointerDown = false;
          var panelSize = new Vector2(
                    viewportInput.resolvedStyle.width,
                    viewportInput.resolvedStyle.height);
          _lotWorld.DragBuilding3DFromPanel(evt.position, panelSize);
          if (viewportInput.HasPointerCapture(evt.pointerId))
            viewportInput.ReleasePointer(evt.pointerId);
          _lotWorld.EndBuilding3DDrag();
          _lotStatus = "Building selected • drag to move • rotate in Building panel";
          OpenSelectedBuildingInspector();
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _floraPointerDown)
        {
          _floraPointerDown = false;
          var panelSize = new Vector2(
                    viewportInput.resolvedStyle.width,
                    viewportInput.resolvedStyle.height);
          if (_lotWorld.DragFloraFromPanel(evt.position, panelSize))
            _floraDragStarted = true;
          viewportInput.ReleasePointer(evt.pointerId);
          _lotWorld.EndFloraDrag();
          _lotStatus = _lotWorld.LastFloraLinePlacementCount > 1
                    ? $"{_lotWorld.LastFloraLinePlacementCount} flora items planted in a straight line"
                    : _floraDragStarted
                        ? "Flora moved and planted"
                        : "Flora selected • drag to move";
          _floraDragStarted = false;
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _runtimeObjectPointerDown)
        {
          _runtimeObjectPointerDown = false;
          viewportInput.ReleasePointer(evt.pointerId);
          _lotWorld.EndRuntimeObjectDrag();
          _lotInspectorVisible = true; RefreshLotEditor();
          evt.StopPropagation(); return;
        }
        if (evt.button == 0 && _propPointerDown)
        {
          _lotInspectorVisible = true;
          _propPointerDown = false;
          var panelSize = new Vector2(
                    viewportInput.resolvedStyle.width,
                    viewportInput.resolvedStyle.height);
          if (_lotWorld.DragPropFromPanel(evt.position, panelSize))
            _propDragStarted = true;
          viewportInput.ReleasePointer(evt.pointerId);
          _lotWorld.EndPropDrag();
          if (_lotWorld.SelectedPropCanWalk)
          {
            _placementPropId = "";
            _lotWorld.SetPropPlacementPreview("");
            _lotStatus = _lotWorld.SelectedAnimalName + " selected • click the ground to walk there";
            if (_lotWorld.SelectedPropIsCarriageTeam)
            {
              _lotInspectorVisible = true;
              _lotInspectorPosition = new Vector2(-1f, -1f);
              _lotStatus = _lotWorld.SelectedHorseWagonName + " selected • Drive follows the road loop";
            }
          }
          else if (_lotWorld.SelectedPropIsBoat)
          {
            _placementPropId = "";
            _lotWorld.SetPropPlacementPreview("");
            _lotStatus = "Boat selected • drag to move • rotate in Boats panel";
          }
          else _lotStatus = _propDragStarted
                    ? "Prop moved"
                    : "Prop selected • drag to move";
          _propDragStarted = false;
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _buildingPropPointerDown)
        {
          _buildingPropPointerDown = false;
          var panelSize = new Vector2(
                    viewportInput.resolvedStyle.width,
                    viewportInput.resolvedStyle.height);
          if (_lotWorld.DragBuildingPropFromPanel(evt.position, panelSize))
            _buildingPropDragStarted = true;
          viewportInput.ReleasePointer(evt.pointerId);
          _lotWorld.EndBuildingPropDrag();
          _lotStatus = _buildingPropDragStarted
                    ? "Building prop moved on its facade"
                    : "Building prop selected • drag to move";
          _buildingPropDragStarted = false;
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _buildingPropPlacementPointerDown)
        {
          _buildingPropPlacementPointerDown = false;
          var panelSize = new Vector2(
                    viewportInput.resolvedStyle.width,
                    viewportInput.resolvedStyle.height);
          var placed = _lotWorld.CommitBuildingPropPlacementPreview(
                    _placementBuildingPropId);
          if (viewportInput.HasPointerCapture(evt.pointerId))
            viewportInput.ReleasePointer(evt.pointerId);
          _lotStatus = placed
                    ? "Building prop attached • click it again to move or rotate"
                    : "Move the translucent prop over a building facade and release";
          if (placed)
          {
            _placementBuildingPropId = "";
            _lotWorld.SetBuildingPropPlacementPreview("");
          }
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _overlayPointerDown)
        {
          _overlayPointerDown = false;
          viewportInput.ReleasePointer(evt.pointerId);
          _lotWorld.EndOverlayPaint();
          _lotStatus = _overlayDragPainted
                    ? "Overlay path painted across tiles"
                    : "Overlay selected • drag to paint copies • Delete removes it";
          _overlayDragPainted = false;
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button == 0 && _railroadPointerDown)
        {
          _railroadPointerDown = false;
          if (viewportInput.HasPointerCapture(evt.pointerId))
            viewportInput.ReleasePointer(evt.pointerId);
          _lotStatus = _railroadDragStarted
                    ? "Streetcar track painted • curves repaired automatically"
                    : "Streetcar track cell selected";
          _railroadDragStarted = false;
          Show(AppScreen.LotEditor);
          evt.StopPropagation();
          return;
        }
        if (evt.button != 0 || !_roadPointerDown) return;
        _roadPointerDown = false;
        viewportInput.ReleasePointer(evt.pointerId);
        _lotWorld.EndRoadPaintStroke();
        _lotStatus = _roadDragStarted
                  ? _outsideConnectorDragCreated
                      ? "Outside connection created • two-way traffic enabled"
                      : "Road stroke painted • topology repaired"
                  : $"Road cell {_lotWorld.RoadCursorCell.x}, {_lotWorld.RoadCursorCell.y} selected";
        _roadDragStarted = false;
        _outsideConnectorDragCreated = false;
        Show(AppScreen.LotEditor);
        evt.StopPropagation();
      });
      viewportInput.RegisterCallback<WheelEvent>(evt =>
      {
        if (_lotWorld.BuildingFocusFreezeActive)
        {
          evt.StopPropagation();
          return;
        }
        if (Mathf.Abs(evt.delta.y) < 0.01f) return;
        StepZoom(evt.delta.y > 0f ? 1 : -1);
        evt.StopPropagation();
      });
      screen.Add(viewportInput);

      var topbar = new VisualElement();
      topbar.AddToClassList("topbar");
      topbar.Add(CfButton.Create("←  MENU",
          () => RequestDocumentAction(() => Show(AppScreen.MainMenu)), true, "quiet"));
      topbar.Add(CfButton.Create(
          _lotWorld.TopDownViewEnabled ? "EXIT TOP DOWN" : "TOP DOWN [T]",
          ToggleTopDownView,
          true,
          _lotWorld.TopDownViewEnabled ? "mode-selected" : "quiet"));

      var title = new VisualElement();
      title.AddToClassList("topbar-title");
      title.Add(StyledLabel(_hasOpenLot
          ? _lotWorld.HasUnsavedChanges
              ? $"{_lotWorld.CurrentLotName} •"
              : _lotWorld.CurrentLotName
          : "NO LOT OPEN", "topbar-heading"));
      title.Add(StyledLabel(_hasOpenLot
          ? $"{LotTypeLabel(_lotWorld.LotType).ToUpperInvariant()} • {_lotWorld.LotWidthCells} × {_lotWorld.LotDepthCells} CELLS • {_lotWorld.LotWidthMeters} × {_lotWorld.LotDepthMeters} M"
          : "CREATE A NEW LOT OR LOAD A SAVED LOT", "topbar-caption"));
      topbar.Add(title);

      if (_lotEditorCategory == LotEditorCategory.Flora &&
          (!string.IsNullOrWhiteSpace(_lotWorld.SelectedFloraId) ||
           !string.IsNullOrWhiteSpace(_placementFloraId)))
      {
        var floraId = !string.IsNullOrWhiteSpace(
            _lotWorld.SelectedFloraId)
            ? _lotWorld.SelectedFloraId
            : _placementFloraId;
        topbar.Add(StyledLabel(
            $"CURRENT SELECTION: {FloraDisplayName(floraId).ToUpperInvariant()}",
            "current-selection-chip"));
      }
      if (_lotEditorCategory == LotEditorCategory.Props &&
          !string.IsNullOrWhiteSpace(_placementPropId))
        topbar.Add(StyledLabel(
            "CURRENT SELECTION: WROUGHT-IRON FENCE",
            "current-selection-chip"));
      if (_lotEditorCategory == LotEditorCategory.Effects &&
          !string.IsNullOrWhiteSpace(_placementEffectId))
        topbar.Add(StyledLabel(
            _lotWorld.LargeWindowLightPlacement
                ? "LARGE WINDOW LIGHT 2 × 4 M • TAB = NORMAL SIZE"
                : "WINDOW LIGHT 1 × 2 M • TAB = STOREFRONT SIZE",
            "current-selection-chip"));

      var viewActions = new VisualElement();
      viewActions.AddToClassList("topbar-actions");
      var cameraPanButton = CfButton.CreateIcon(
          "camera-pan-hand",
          "✋",
          _cameraPanToolActive
              ? "Camera pan active — drag the lot to move the view"
              : "Pan camera — drag the lot when zoomed in",
          () => SetCameraPanTool(!_cameraPanToolActive),
          _hasOpenLot,
          _cameraPanToolActive);
      cameraPanButton.AddToClassList("camera-pan-button");
      viewActions.Add(cameraPanButton);
      viewActions.Add(CfButton.Create("NEW",
          () => RequestDocumentAction(ComposeNewLotDialog), true, "quiet"));
      viewActions.Add(CfButton.Create("SAVE", SaveLot, _hasOpenLot, "quiet"));
      viewActions.Add(CfButton.Create("SAVE AS", ComposeSaveAsDialog, _hasOpenLot, "quiet"));
      viewActions.Add(CfButton.Create("LOAD",
          () => RequestDocumentAction(ComposeLoadLotBrowser), true, "quiet"));
      viewActions.Add(CfButton.Create("TRAFFIC TEST",
          () => RequestDocumentAction(ApplyTrafficTemplate), _hasOpenLot, "quiet"));
      topbar.Add(viewActions);
      screen.Add(topbar);

      var toolRail = new VisualElement();
      toolRail.AddToClassList("tool-rail");
      toolRail.Add(StyledLabel("TOOLS", "section-label"));
      var toolRailScroll = new ScrollView(ScrollViewMode.Vertical);
      toolRailScroll.AddToClassList("tool-rail-scroll");
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Main, "main", "Main"));
      toolRailScroll.Add(CreateLotStatsButton());
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Buildings3D,
          "buildings", "Buildings"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Roads, "roads-car-v74", "Roads"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Transport,
          "roads-car-v74", "Transport"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Paths, "paths", "Paths"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Water,
          "water", "Water"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Terrain,
          "terrain-hill-v01", "Terrain"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Flora, "flora-tree-v91", "Flora"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Garden, "base-textures", "Garden"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Props, "props-lamppost-v91", "Props"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Characters,
          "buildings", "3D Characters"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Effects,
          "effects", "Effects"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.BaseTextures, "base-textures", "Base"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.OverlayTextures, "overlay-textures", "Overlays"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Decals, "decals", "Decals"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.Environment, "environment", "Environment"));
      toolRailScroll.Add(CategoryButton(LotEditorCategory.View, "view", "View"));
      toolRail.Add(toolRailScroll);
      screen.Add(toolRail);
      if (_lotEditorCategoryExpanded && _lotEditorCategory == LotEditorCategory.Transport)
        ComposeTransportFlyout(screen);

      if (_hasOpenLot && _lotEditorCategoryExpanded &&
          _lotEditorCategory == LotEditorCategory.Main)
      {
        var main = new VisualElement { name = "main-category-panel" };
        main.AddToClassList("context-panel");
        main.Add(StyledLabel("LOT SETTINGS", "section-label"));
        main.Add(StyledLabel("GENERAL", "catalog-title"));

        var nameField = new TextField("LOT NAME") { value = _lotWorld.CurrentLotName };
        nameField.AddToClassList("document-field");
        main.Add(nameField);
        var behaviors = CfButton.Create("LOT BEHAVIORS…", OpenLotBehaviorsModal, true, "primary");
        behaviors.name = "open-lot-behaviors";
        main.Add(behaviors);
        main.Add(CfButton.Create("WATER-FACING ARROW", () =>
        { _waterOrientationClick = 1; _lotStatus = "Click to place the arrow, then choose a direction"; RefreshLotEditor(); }, true, "primary"));
        main.Add(StyledLabel("Click to place. Drag to move. Select the arrow to choose which side faces water.", "inspector-note"));
        if (_waterOrientationClick > 0) main.Add(CfButton.Create("CANCEL ARROW", () => { _waterOrientationClick = 0; RefreshLotEditor(); }, true, "quiet"));
        if (_lotWorld.HasWaterOrientation) main.Add(CfButton.Create("REMOVE WATER ARROW", () => { _lotWorld.ClearWaterOrientation(); RefreshLotEditor(); }, true, "secondary"));

        var types = new List<string>
                {
                    "Residential", "Commercial", "Industrial", "Mixed", "Transportation", "Civics"
                };
        var typeField = new CityForgeChoiceField(
            _root, "LOT TYPE", types, Mathf.Max(0, types.IndexOf(LotTypeLabel(_lotWorld.LotType))));
        main.Add(typeField);

        var eraNames = new List<string>(LotEraCatalog.DisplayNames);
        var eraField = new CityForgeChoiceField(
            _root, "ERA", eraNames, LotEraCatalog.IndexOf(_lotWorld.CurrentEraId));
        main.Add(eraField);

        var trafficTypes = new List<string>
                {
                    "None", "Suburban Street", "Parking Lot"
                };
        var trafficField = new CityForgeChoiceField(
            _root, "TRAFFIC TYPE", trafficTypes,
            Mathf.Clamp((int)_lotWorld.TrafficType, 0, trafficTypes.Count - 1));
        main.Add(trafficField);

        if (_pendingLotWidthCells < 1)
          _pendingLotWidthCells = Mathf.Clamp(_lotWorld.LotWidthCells, 1, 8);
        if (_pendingLotDepthCells < 1)
          _pendingLotDepthCells = Mathf.Clamp(_lotWorld.LotDepthCells, 1, 8);

        var widthField = new CityForgeCellCountField(
            "WIDTH", _pendingLotWidthCells,
            cells => _pendingLotWidthCells = cells);
        main.Add(widthField);
        var depthField = new CityForgeCellCountField(
            "LENGTH", _pendingLotDepthCells,
            cells => _pendingLotDepthCells = cells);
        main.Add(depthField);
        main.Add(StyledLabel(
            "EACH MAJOR CELL IS 10 × 10 METERS • 1 METER MINOR GRID",
            "lighting-note"));
        main.Add(CfButton.Create("APPLY LOT SETTINGS", () =>
        {
          Enum.TryParse(typeField.value, out LotType lotType);
          _lotWorld.ConfigureLot(nameField.value, lotType,
                      _pendingLotWidthCells, _pendingLotDepthCells,
                      LotEraCatalog.IdForDisplayName(eraField.value));
          _lotWorld.SetTrafficType(
                      TrafficLotModel.ForDisplayName(trafficField.value));
          _lotStatus = $"Lot updated • {_pendingLotWidthCells} × {_pendingLotDepthCells} major cells";
          Show(AppScreen.LotEditor);
        }, true, "primary"));
        screen.Add(main);
      }

      if (_lotEditorCategoryExpanded &&
          _lotEditorCategory == LotEditorCategory.Buildings3D)
      {
        var modal = new VisualElement { name = "building-3d-library-modal" };
        modal.AddToClassList("document-modal");
        modal.AddToClassList("building-library-modal");
        var catalog = new VisualElement { name = "building-3d-library-panel" };
        catalog.AddToClassList("catalog");
        catalog.AddToClassList("building-catalog");
        catalog.AddToClassList("document-modal-panel");
        catalog.AddToClassList("building-library-modal-panel");
        catalog.Add(StyledLabel("BUILDINGS", "section-label"));
        catalog.Add(StyledLabel("3D BUILDING LIBRARY", "catalog-title"));
        catalog.Add(StyledLabel(
            "PRODUCTION-READY REAL-TIME BUILDINGS",
            "catalog-meta"));
        var close = CfButton.Create("CLOSE", () =>
        {
          _lotEditorCategoryExpanded = false;
          Show(AppScreen.LotEditor);
        }, true, "quiet");
        close.name = "close-building-3d-library";
        close.AddToClassList("building-library-close");
        catalog.Add(close);

        var categoryTabs = new VisualElement { name = "building-3d-use-tabs" };
        categoryTabs.AddToClassList("building-use-tabs");
        foreach (BuildingUseCategory category in Enum.GetValues(typeof(BuildingUseCategory)))
        {
          var capturedCategory = category;
          var categoryButton = CfButton.Create(
              category.ToString().ToUpperInvariant(),
              () =>
              {
                _buildingUseCategory = capturedCategory;
                _buildingSubcategory = string.Empty;
                Show(AppScreen.LotEditor);
              },
              true,
              _buildingUseCategory == category
                  ? "building-use-selected"
                  : "building-use");
          categoryButton.name =
              $"building-3d-use-{category.ToString().ToLowerInvariant()}";
          categoryTabs.Add(categoryButton);
        }
        catalog.Add(categoryTabs);

        var threeDimensionalSubcategories =
            BuildingContentCatalog.LotEditorSubcategories(
                _buildingUseCategory, _lotWorld.CurrentEraId);
        if (threeDimensionalSubcategories.Count > 0)
        {
          var subcategoryTabs = new VisualElement
          { name = "building-3d-subcategory-tabs" };
          subcategoryTabs.AddToClassList("building-use-tabs");
          subcategoryTabs.Add(CfButton.Create("ALL", () =>
          {
            _buildingSubcategory = string.Empty;
            Show(AppScreen.LotEditor);
          }, true, string.IsNullOrWhiteSpace(_buildingSubcategory)
              ? "building-use-selected" : "building-use"));
          foreach (var subcategory in threeDimensionalSubcategories)
          {
            var selected = subcategory;
            var tab = CfButton.Create(subcategory.ToUpperInvariant(), () =>
            {
              _buildingSubcategory = selected;
              Show(AppScreen.LotEditor);
            }, true, _buildingSubcategory == subcategory
                ? "building-use-selected" : "building-use");
            tab.name = $"building-3d-subcategory-{subcategory.ToLowerInvariant()}";
            subcategoryTabs.Add(tab);
          }
          catalog.Add(subcategoryTabs);
        }

        var grid = new VisualElement { name = "building-3d-card-grid" };
        grid.AddToClassList("building-card-grid");
        var visibleBuildingCount = 0;
        foreach (var entry in BuildingContentCatalog.ForLotEditor(
                     _buildingUseCategory, _lotWorld.CurrentEraId,
                     _buildingSubcategory))
        {
          var captured = entry;
          var card = new Button(() =>
          {
            _building3DPlacementPending =
                          _lotWorld.BeginExperimentalBuilding3DPlacement(
                              captured.id);
            _lotStatus = _building3DPlacementPending
                          ? $"{captured.displayName} preview • move the mouse and click to place"
                          : $"{captured.displayName} could not be added";
            _lotEditorCategoryExpanded = false;
            Show(AppScreen.LotEditor);
          })
          { name = $"building-3d-card-{captured.id}" };
          card.AddToClassList("building-card");
          var thumbnail = new VisualElement();
          thumbnail.AddToClassList("building-card-thumbnail");
          var texture = BuildingContentCatalog.LoadThumbnail(captured);
          if (texture != null)
            thumbnail.style.backgroundImage =
                new StyleBackground(texture);
          card.Add(thumbnail);
          card.Add(StyledLabel(captured.displayName.ToUpperInvariant(),
              "building-card-name"));
          card.Add(StyledLabel(
              $"{(string.IsNullOrWhiteSpace(captured.subcategory) ? captured.category : captured.subcategory).ToUpperInvariant()} • {(captured.description ?? string.Empty).ToUpperInvariant()}",
              "building-card-meta"));
          card.Add(StyledLabel("ADD TO CURRENT LOT",
              "building-card-meta"));
          grid.Add(card);
          visibleBuildingCount++;
        }
        if (string.IsNullOrWhiteSpace(_buildingSubcategory) &&
            _buildingUseCategory == BuildingUseCategory.Civics &&
            BuildingContentCatalog.Find(
                LotWorldController.ArtMuseumProductionId) == null)
        {
          var museumCard = new Button(() =>
          {
            _building3DPlacementPending =
                          _lotWorld.BeginExperimentalBuilding3DPlacement(
                              LotWorldController.ArtMuseumProductionId);
            _lotStatus = _building3DPlacementPending
                          ? "Art Museum preview • move the mouse and click to place"
                          : "Art Museum could not be added";
            _lotEditorCategoryExpanded = false;
            Show(AppScreen.LotEditor);
          })
          { name = "building-3d-card-art-museum-production-v01" };
          museumCard.AddToClassList("building-card");
          var museumThumbnail = new VisualElement();
          museumThumbnail.AddToClassList("building-card-thumbnail");
          var museumTexture = Resources.Load<Texture2D>(
              "CityForgeV3/Buildings3D/ArtMuseumProduction/Source/ArtMuseum");
          if (museumTexture != null)
            museumThumbnail.style.backgroundImage =
                new StyleBackground(museumTexture);
          museumCard.Add(museumThumbnail);
          museumCard.Add(StyledLabel("CITY FORGE ART MUSEUM",
              "building-card-name"));
          museumCard.Add(StyledLabel(
              "CIVICS • 4 AUTHORED LODS • 220K → 6.8K TRIANGLES",
              "building-card-meta"));
          museumCard.Add(StyledLabel("ADD TO CURRENT LOT", "building-card-meta"));
          grid.Add(museumCard);
          visibleBuildingCount++;
        }
        if (string.IsNullOrWhiteSpace(_buildingSubcategory) &&
            _buildingUseCategory == BuildingUseCategory.Entertainment &&
            BuildingContentCatalog.Find(
                LotWorldController.KingKongEnclosureBuilding3DId) == null)
        {
          var enclosureCard = new Button(() =>
          {
            _building3DPlacementPending =
                          _lotWorld.BeginExperimentalBuilding3DPlacement(
                              LotWorldController.KingKongEnclosureBuilding3DId);
            _lotStatus = _building3DPlacementPending
                          ? "King Kong Enclosure preview • move the mouse and click to place"
                          : "King Kong Enclosure could not be added";
            _lotEditorCategoryExpanded = false;
            Show(AppScreen.LotEditor);
          })
          { name = "building-3d-card-king-kong-enclosure-v01" };
          enclosureCard.AddToClassList("building-card");
          var enclosureThumbnail = new VisualElement();
          enclosureThumbnail.AddToClassList("building-card-thumbnail");
          var enclosureTexture = Resources.Load<Texture2D>(
              "CityForgeV3/Props/Entertainment/KingKongEnclosureV01/Textures/base-color");
          if (enclosureTexture != null)
            enclosureThumbnail.style.backgroundImage =
                new StyleBackground(enclosureTexture);
          enclosureCard.Add(enclosureThumbnail);
          enclosureCard.Add(StyledLabel("KING KONG ENCLOSURE",
              "building-card-name"));
          enclosureCard.Add(StyledLabel(
              "ENTERTAINMENT • 30 × 30 M GIANT-ANIMAL EXHIBIT",
              "building-card-meta"));
          enclosureCard.Add(StyledLabel("ADD TO CURRENT LOT",
              "building-card-meta"));
          grid.Add(enclosureCard);
          visibleBuildingCount++;
        }
        if (string.IsNullOrWhiteSpace(_buildingSubcategory) &&
            _buildingUseCategory == BuildingUseCategory.Commercial &&
            BuildingContentCatalog.Find(
                LotWorldController.PlymouthStoreProductionId) == null)
        {
          var plymouthCard = new Button(() =>
          {
            _building3DPlacementPending =
                          _lotWorld.BeginExperimentalBuilding3DPlacement(
                              LotWorldController.PlymouthStoreProductionId);
            _lotStatus = _building3DPlacementPending
                          ? "Plymouth Store preview • move the mouse and click to place"
                          : "Plymouth Store could not be added";
            _lotEditorCategoryExpanded = false;
            Show(AppScreen.LotEditor);
          })
          { name = "building-3d-card-plymouth-store-comparison-v01" };
          plymouthCard.AddToClassList("building-card");
          var plymouthThumbnail = new VisualElement();
          plymouthThumbnail.AddToClassList("building-card-thumbnail");
          var plymouthTexture = Resources.Load<Texture2D>(
              "CityForgeV3/Buildings3D/PlymouthStoreProduction/Impostor/" +
              "plymouth-store-comparison-yaw-000");
          if (plymouthTexture != null)
            plymouthThumbnail.style.backgroundImage =
                new StyleBackground(plymouthTexture);
          plymouthCard.Add(plymouthThumbnail);
          plymouthCard.Add(StyledLabel("PLYMOUTH STORE",
              "building-card-name"));
          plymouthCard.Add(StyledLabel(
              "COMMERCIAL • 4 VALIDATED LODS • 87.7K → 11K TRIANGLES",
              "building-card-meta"));
          plymouthCard.Add(StyledLabel("ADD TO CURRENT LOT", "building-card-meta"));
          grid.Add(plymouthCard);
          visibleBuildingCount++;
        }
        if (string.IsNullOrWhiteSpace(_buildingSubcategory) &&
            _buildingUseCategory == BuildingUseCategory.Residential &&
            BuildingContentCatalog.Find(
                LotWorldController.GildedAgeMansionExperimentalId) == null)
        {
          var gildedCard = new Button(() =>
          {
            _building3DPlacementPending =
                          _lotWorld.BeginExperimentalBuilding3DPlacement(
                              LotWorldController.GildedAgeMansionExperimentalId);
            _lotStatus = _building3DPlacementPending
                          ? "Exp. Gilded Age Mansion preview • move the mouse and click to place"
                          : "Exp. Gilded Age Mansion could not be added";
            _lotEditorCategoryExpanded = false;
            Show(AppScreen.LotEditor);
          })
          { name = "building-3d-card-gilded-age-mansion-v01" };
          gildedCard.AddToClassList("building-card");
          var gildedThumbnail = new VisualElement();
          gildedThumbnail.AddToClassList("building-card-thumbnail");
          var gildedTexture = Resources.Load<Texture2D>(
              "CityForgeV3/Buildings3D/GildedAgeMansionProduction/Impostor/" +
              "gilded-age-mansion-yaw-045");
          if (gildedTexture != null)
            gildedThumbnail.style.backgroundImage =
                new StyleBackground(gildedTexture);
          gildedCard.Add(gildedThumbnail);
          gildedCard.Add(StyledLabel("EXP. GILDED AGE MANSION",
              "building-card-name"));
          gildedCard.Add(StyledLabel(
              "RESIDENTIAL • 3D NIGHT-LIGHTING EXPERIMENT • 181K → 22.6K",
              "building-card-meta"));
          gildedCard.Add(StyledLabel("ADD TO CURRENT LOT", "building-card-meta"));
          grid.Add(gildedCard);
          visibleBuildingCount++;
        }
        catalog.Add(grid);
        if (visibleBuildingCount == 0)
          catalog.Add(StyledLabel(
              $"NO {_buildingUseCategory.ToString().ToUpperInvariant()} BUILDINGS YET",
              "catalog-empty"));
        modal.Add(catalog);
        screen.Add(modal);
      }

      if (_lotEditorCategoryExpanded &&
          _lotEditorCategory == LotEditorCategory.Effects)
      {
        var effectsPanel = new VisualElement
        {
          name = "effects-category-panel"
        };
        effectsPanel.AddToClassList("context-panel");
        effectsPanel.Add(StyledLabel("EFFECTS", "section-label"));
        effectsPanel.Add(StyledLabel("ATMOSPHERE & LIGHT", "catalog-title"));
        effectsPanel.Add(StyledLabel(
            $"PLACED  {_lotWorld.EffectCount}", "catalog-meta"));
        effectsPanel.Add(StyledLabel(
            string.IsNullOrWhiteSpace(_placementEffectId)
                ? "Choose an effect to begin"
                : _placementEffectId == LotWorldController.CloudEffectId
                    ? "Move over the lot and click to place a high cloud"
                    : "Move the cyan square onto a facade • gold means the surface is valid",
            "catalog-meta"));
        effectsPanel.Add(CfButton.Create("CHOOSE EFFECT…",
            OpenEffectsModal, true, "mode-selected"));
        screen.Add(effectsPanel);
      }

      if (_lotEditorCategoryExpanded &&
          _lotEditorCategory == LotEditorCategory.Water)
      {
        var waterPanel = new VisualElement
        {
          name = "water-category-panel"
        };
        waterPanel.AddToClassList("context-panel");
        waterPanel.Add(StyledLabel("WATER", "section-label"));
        waterPanel.Add(StyledLabel(
            "PONDS, LAKES, RIVERS & SWAMPS", "catalog-title"));
        waterPanel.Add(StyledLabel(
            _lotWorld.HasSelectedWaterArea
                ? "SELECTED SWAMP • drag cyan vertices to reshape"
                : "Choose a water family to browse available surfaces",
            "catalog-meta"));
        if (_lotWorld.HasSelectedWaterArea)
        {
          var textureActions = new VisualElement();
          textureActions.AddToClassList("compact-actions");
          textureActions.Add(CfButton.Create("TEXTURE −", () =>
          {
            _lotWorld.AdjustSelectedWaterTextureScale(0.8f);
            _lotStatus = "Swamp texture enlarged";
            Show(AppScreen.LotEditor);
          }, true, "quiet"));
          textureActions.Add(CfButton.Create("TEXTURE +", () =>
          {
            _lotWorld.AdjustSelectedWaterTextureScale(1.25f);
            _lotStatus = "Swamp texture repeated more densely";
            Show(AppScreen.LotEditor);
          }, true, "quiet"));
          waterPanel.Add(textureActions);
          waterPanel.Add(CfButton.Create("DELETE SWAMP", () =>
          {
            _lotWorld.DeleteSelectedWaterArea();
            _lotStatus = "Swamp water deleted";
            Show(AppScreen.LotEditor);
          }, true, "danger"));
        }
        waterPanel.Add(CfButton.Create("OPEN WATER LIBRARY…",
            OpenWaterModal, true, "mode-selected"));
        screen.Add(waterPanel);
      }
      if (_lotEditorCategoryExpanded &&
          _lotEditorCategory == LotEditorCategory.Decals)
      {
        var decalsPanel = new VisualElement { name = "decals-category-panel" };
        decalsPanel.AddToClassList("context-panel");
        decalsPanel.Add(StyledLabel("DECALS", "section-label"));
        decalsPanel.Add(StyledLabel("SURFACE DETAILS", "catalog-title"));
        decalsPanel.Add(StyledLabel(
            string.IsNullOrWhiteSpace(_placementDecalCategory)
                ? "Choose a category to arm the paintbrush"
                : $"{_placementDecalCategory.ToUpperInvariant()} armed • click repeatedly for randomized details",
            "catalog-meta"));
        decalsPanel.Add(CfButton.Create("CHOOSE DECAL CATEGORY…",
            OpenDecalsModal, true, "mode-selected"));
        decalsPanel.Add(StyledLabel(
            "HOLD CONTROL  ▱  PARTIAL ERASER",
            "catalog-meta"));
        screen.Add(decalsPanel);
      }

      if (_lotEditorCategoryExpanded &&
          _lotEditorCategory == LotEditorCategory.Terrain)
        ComposeTerrainPanel(screen);

      if (_lotEditorCategoryExpanded && _lotEditorCategory == LotEditorCategory.Environment)
      {
        var lightingLab = new VisualElement { name = "environment-category-panel" };
        lightingLab.AddToClassList("context-panel");
        lightingLab.Add(StyledLabel("TIME OF DAY", "section-label"));
        lightingLab.Add(StyledLabel(
            timeSpec.Label,
            "lighting-current"));
        lightingLab.Add(StyledLabel(
            _lotWorld.TimeOfDay switch
            {
              TimeOfDayPreset.Morning =>
                      "SUN: EAST • SHADOWS: WEST (CITYFORGE COMPASS)",
              TimeOfDayPreset.Noon =>
                      "SUN: OVERHEAD • SHADOWS: MINIMAL",
              TimeOfDayPreset.Afternoon =>
                      "SUN: WEST • SHADOWS: EAST (CITYFORGE COMPASS)",
              _ => "CITYFORGE COMPASS: N 0° • E 90° • S 180° • W 270°"
            },
            "lighting-direction-contract"));
        var timeActions = new VisualElement();
        timeActions.AddToClassList("time-actions");
        foreach (var preset in new[]
                 {
                         TimeOfDayPreset.Morning,
                         TimeOfDayPreset.Noon,
                         TimeOfDayPreset.Afternoon,
                         TimeOfDayPreset.Evening,
                         TimeOfDayPreset.Night
                     })
        {
          var captured = preset;
          timeActions.Add(CfButton.Create(
              TimeOfDayLighting.For(preset).Label,
              () => SetTimeOfDay(captured),
              true,
              _lotWorld.TimeOfDay == preset
                  ? "time-selected"
                  : "time"));
        }

        if (_lotEditorCategoryExpanded &&
            _lotEditorCategory is LotEditorCategory.Flora or LotEditorCategory.Props)
        {
          var library = new VisualElement
          {
            name = _lotEditorCategory == LotEditorCategory.Flora
                  ? "flora-category-panel" : "props-category-panel"
          };
          library.AddToClassList("catalog");
          var flora = _lotEditorCategory == LotEditorCategory.Flora;
          library.Add(StyledLabel(flora ? "FLORA" : "PROPS", "section-label"));
          library.Add(StyledLabel(flora
              ? "TREES, SHRUBS & PLANTING"
              : "LOT OBJECTS & STREET FURNITURE", "catalog-title"));
          library.Add(StyledLabel(
              flora
                  ? "FLORA LIBRARY COMING NEXT"
                  : "PROP LIBRARY COMING NEXT",
              "catalog-meta"));
          screen.Add(library);
        }
        lightingLab.Add(timeActions);
        lightingLab.Add(StyledLabel("LIGHTING CONTROLS", "source-label"));
        lightingLab.Add(StyledLabel(
            "Live preview • values are saved separately for each time-of-day preset",
            "lighting-note"));
        lightingLab.Add(EnvironmentLightingSlider(
            "SUN INTENSITY", "sun-intensity", 0f, 3f,
            _lotWorld.EnvironmentSunIntensityScale, "0.00×"));
        lightingLab.Add(EnvironmentLightingSlider(
            "SUN ELEVATION", "sun-elevation", -35f, 35f,
            _lotWorld.EnvironmentSunElevationOffset, "+0°;-0°;0°"));
        lightingLab.Add(EnvironmentLightingSlider(
            "SUN DIRECTION", "sun-azimuth", -45f, 45f,
            _lotWorld.EnvironmentSunAzimuthOffset, "+0°;-0°;0°"));
        lightingLab.Add(EnvironmentLightingSlider(
            "AMBIENT / ENVIRONMENT", "ambient", 0f, 1.5f,
            _lotWorld.EnvironmentAmbientIntensityScale, "0.00×"));
        lightingLab.Add(EnvironmentLightingSlider(
            "ENVIRONMENT EXPOSURE", "exposure", 0.25f, 1.5f,
            _lotWorld.EnvironmentSkyExposure, "0.00"));
        lightingLab.Add(EnvironmentLightingSlider(
            "SHADOW STRENGTH", "shadow-strength", 0f, 1f,
            _lotWorld.EnvironmentShadowStrength, "0%"));
        lightingLab.Add(EnvironmentLightingSlider(
            "BUILDING CONTRAST", "contrast", 0.8f, 2f,
            _lotWorld.EnvironmentBuildingContrast, "0.00"));
        lightingLab.Add(EnvironmentLightingSlider(
            "BUILDING VIBRANCE", "vibrance", 0f, 1f,
            _lotWorld.EnvironmentBuildingVibrance, "0%"));
        lightingLab.Add(EnvironmentLightingSlider(
            "BUILDING SATURATION", "saturation", 0f, 4f,
            _lotWorld.EnvironmentBuildingSaturation, "0.00"));
        lightingLab.Add(CfButton.Create("RESET LIGHTING",
            () =>
            {
              _lotWorld.ResetEnvironmentLightingControls();
              _lotStatus = "Lighting reset to the rich CityForge studio baseline";
              Show(AppScreen.LotEditor);
            }, true, "quiet"));
        var weatherActions = new VisualElement { name = "weather-actions" };
        weatherActions.AddToClassList("weather-actions");
        weatherActions.Add(CfButton.CreateIcon(
            "rain-toggle",
            "☂",
            _lotWorld.IsRaining ? "Turn rain off" : "Turn rain on",
            ToggleRain,
            true,
            _lotWorld.IsRaining));
        lightingLab.Add(weatherActions);
        lightingLab.Add(StyledLabel("SEASON", "source-label"));
        lightingLab.Add(StyledLabel(
            SeasonLighting.Label(_lotWorld.Season),
            "lighting-current"));
        var seasonActions = new VisualElement();
        seasonActions.AddToClassList("time-actions");
        foreach (var season in new[]
                 {
                         SeasonPreset.Spring,
                         SeasonPreset.Summer,
                         SeasonPreset.Autumn,
                         SeasonPreset.Winter
                     })
        {
          var capturedSeason = season;
          seasonActions.Add(CfButton.Create(
              SeasonLighting.Label(season),
              () => SetSeason(capturedSeason),
              true,
              _lotWorld.Season == season
                  ? "time-selected"
                  : "time"));
        }
        if (_lotWorld.Season == SeasonPreset.Winter)
        {
          seasonActions.Add(CfButton.CreateIcon(
              "winter-snowfall",
              "❄",
              _lotWorld.IsWinterSnowing
                  ? "Snowfall in progress"
                  : "Snowfall for 10 seconds",
              StartWinterSnowfall,
              _lotWorld.CanStartWinterSnowfall,
              _lotWorld.IsWinterSnowing));
        }
        lightingLab.Add(seasonActions);
        lightingLab.Add(StyledLabel("ARTWORK SOURCE", "source-label"));
        var sourceActions = new VisualElement();
        sourceActions.AddToClassList("source-actions");
        sourceActions.Add(CfButton.Create(
            "BAKED REFERENCE",
            () => SetArtworkSource(BuildingArtworkSource.Approved),
            true,
            _lotWorld.ArtworkSource == BuildingArtworkSource.Approved
                ? "source-selected"
                : "source"));
        sourceActions.Add(CfButton.Create(
            "GAME-LIT",
            () => SetArtworkSource(BuildingArtworkSource.NeutralPilot),
            true,
            _lotWorld.ArtworkSource == BuildingArtworkSource.NeutralPilot
                ? "source-selected"
                : "source"));
        lightingLab.Add(sourceActions);
        lightingLab.Add(StyledLabel(
            $"ZOOM: {_lotWorld.ZoomLevel.ToString().ToUpperInvariant()} • 1 M MINOR / 10 M MAJOR GRID",
            "lighting-note"));
        lightingLab.Add(StyledLabel(
            _lotWorld.NeutralPilotFallback
                ? "NEUTRAL SET UNAVAILABLE • SHOWING BAKED REFERENCE"
                : _lotWorld.NeutralPilotShowing
                    ? "PRODUCTION DEFAULT • NEUTRAL ART + RUNTIME TIME LIGHTING"
                    : "REFERENCE ONLY • BLENDER DIRECTIONAL LIGHT IS BAKED",
            "lighting-note"));
        screen.Add(lightingLab);
      }

      var inspector = new VisualElement();
      inspector.AddToClassList("inspector");
      if (_lotInspectorPosition.x >= 0f)
      {
        inspector.style.left = _lotInspectorPosition.x;
        inspector.style.top = _lotInspectorPosition.y;
        inspector.style.right = StyleKeyword.Auto;
      }
      var inspectorHeader = new VisualElement();
      inspectorHeader.AddToClassList("inspector-header");
      var inspectorDragHandle = StyledLabel("⠿  DRAG PANEL",
          "inspector-drag-handle");
      inspectorDragHandle.tooltip = "Drag to reposition this panel";
      var closeInspector = CfButton.Create("×", () =>
      {
        _lotInspectorVisible = false;
        RefreshLotEditor();
      }, true, "icon");
      closeInspector.name = "close-lot-inspector";
      closeInspector.tooltip = "Close panel";
      inspectorHeader.Add(inspectorDragHandle);
      inspectorHeader.Add(closeInspector);
      inspector.Add(inspectorHeader);
      var inspectorDragging = false;
      var inspectorDragOffset = Vector2.zero;
      inspectorDragHandle.RegisterCallback<PointerDownEvent>(evt =>
      {
        if (evt.button != 0) return;
        inspectorDragging = true;
        inspectorDragOffset = new Vector2(
                  inspector.worldBound.position.x,
                  inspector.worldBound.position.y) - new Vector2(
                  evt.position.x, evt.position.y);
        inspectorDragHandle.CapturePointer(evt.pointerId);
        evt.StopPropagation();
      });
      inspectorDragHandle.RegisterCallback<PointerMoveEvent>(evt =>
      {
        if (!inspectorDragging) return;
        var width = Mathf.Max(1f, screen.resolvedStyle.width);
        var height = Mathf.Max(1f, screen.resolvedStyle.height);
        var position = new Vector2(evt.position.x, evt.position.y) +
                  inspectorDragOffset;
        position.x = Mathf.Clamp(position.x, 0f,
                  Mathf.Max(0f, width - inspector.resolvedStyle.width));
        position.y = Mathf.Clamp(position.y, 70f,
                  Mathf.Max(70f, height - 52f));
        _lotInspectorPosition = position;
        inspector.style.left = position.x;
        inspector.style.top = position.y;
        inspector.style.right = StyleKeyword.Auto;
        evt.StopPropagation();
      });
      inspectorDragHandle.RegisterCallback<PointerUpEvent>(evt =>
      {
        if (!inspectorDragging || evt.button != 0) return;
        inspectorDragging = false;
        if (inspectorDragHandle.HasPointerCapture(evt.pointerId))
          inspectorDragHandle.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
      });
      var selectedCarriage = _lotWorld.ActiveObjectSelection == LotObjectSelectionKind.Prop &&
          _lotWorld.SelectedPropIsCarriageTeam;
      var selectedBuildingProp = _lotWorld.ActiveObjectSelection ==
          LotObjectSelectionKind.BuildingProp;
      var selectedBuilding3D = _lotWorld.SelectedBuilding3DIndex >= 0 &&
          !selectedBuildingProp;
      inspector.Add(StyledLabel(_lotWorld.ActiveObjectSelection == LotObjectSelectionKind.RuntimeObject ? "OBJECT" : selectedCarriage ? _lotWorld.SelectedHorseWagonName.ToUpperInvariant() : selectedBuildingProp
          ? "BUILDING PROP"
          : selectedBuilding3D
          ? "BUILDING"
          : _lotEditorCategory.ToString().ToUpperInvariant(),
          "section-label"));
      AddSelectedObjectId(inspector);
      if (_lotWorld.WaterArrowSelected) ComposeWaterArrowInspector(inspector);
      else if (_lotWorld.ActiveObjectSelection == LotObjectSelectionKind.RuntimeObject)
        inspector.Add(StyledLabel("Drag to reposition • moving pauses the script", "inspector-note"));
      else if (selectedCarriage)
      {
        if (!_lotWorld.SelectedCarriageIsMoving && _lotStatus == "Carriage slowing to a stop")
          _lotStatus = "Carriage stopped";
        inspector.name = "carriage-inspector";
        inspector.Add(StyledLabel(_lotWorld.SelectedHorseWagonName.ToUpperInvariant(), "inspector-title"));
        inspector.Add(Property("STATE", _lotWorld.SelectedCarriageIsStopping ? "STOPPING" : _lotWorld.SelectedCarriageIsMoving ? "DRIVING" : "STOPPED"));
        var speeds = new VisualElement();
        speeds.AddToClassList("inspector-actions");
        var slow = CfButton.Create("SLOW", () => _lotWorld.SetSelectedCarriageFast(false),
            true, !_lotWorld.SelectedCarriageFast ? "mode-selected" : "quiet");
        slow.name = "carriage-speed-slow";
        var fast = CfButton.Create("FAST", () => _lotWorld.SetSelectedCarriageFast(true),
            true, _lotWorld.SelectedCarriageFast ? "mode-selected" : "quiet");
        fast.name = "carriage-speed-fast";
        speeds.Add(slow);
        speeds.Add(fast);
        inspector.Add(speeds);
        var drive = CfButton.Create("DRIVE", () =>
        {
          _lotStatus = _lotWorld.DriveSelectedCarriage()
                      ? "Driving around the road loop • Stop to park"
                      : "No clear road loop reachable • click the ground to drive there";
          RefreshLotEditor();
        }, !_lotWorld.SelectedCarriageIsMoving || _lotWorld.SelectedCarriageIsStopping, "primary");
        drive.name = "drive-carriage";
        inspector.Add(drive);
        var stop = CfButton.Create("STOP", () =>
        {
          _lotWorld.StopSelectedCarriage();
          _lotStatus = _lotWorld.SelectedCarriageIsStopping ? "Carriage slowing to a stop" : "Carriage stopped";
          RefreshLotEditor();
        }, _lotWorld.SelectedCarriageIsMoving && !_lotWorld.SelectedCarriageIsStopping, "quiet");
        stop.name = "stop-carriage";
        inspector.Add(stop);
        inspector.Add(StyledLabel("Click the ground to drive to a particular spot.", "inspector-note"));
        inspector.Add(CfButton.Create("DELETE SELECTED", DeleteSelectedProp, true, "danger"));
      }
      else
      {
        if (_lotEditorCategory is LotEditorCategory.Environment or LotEditorCategory.View)
        {
          var panRow = new VisualElement();
          panRow.AddToClassList("inspector-actions");
          panRow.Add(CfButton.Create("←", () => PanLot(-1, 0), true, "icon"));
          panRow.Add(CfButton.Create("↑", () => PanLot(0, 1), true, "icon"));
          panRow.Add(CfButton.Create("↓", () => PanLot(0, -1), true, "icon"));
          panRow.Add(CfButton.Create("→", () => PanLot(1, 0), true, "icon"));
          inspector.Add(panRow);
        }
        // Object selection takes precedence over the currently open tool
        // category. A selected 3D building must expose its actions even
        // when MAIN (Lot Contract) was the last tool the player opened.
        if (selectedBuildingProp)
        {
          inspector.Add(StyledLabel(
              _lotWorld.SelectedBuildingPropDisplayName,
              "inspector-title"));
          inspector.Add(StyledLabel(
              "ATTACHED TO BUILDING • SIZE IS UNIFORM",
              "inspector-note"));

          var rotationRow = new VisualElement();
          rotationRow.AddToClassList("inspector-actions");
          rotationRow.Add(CfButton.Create("↻ 45°", () =>
          {
            _lotWorld.RotateSelectedBuildingProp45Degrees(1);
            _lotStatus = "Building prop rotated clockwise";
          }, true, "quiet"));
          rotationRow.Add(CfButton.Create("↺ 45°", () =>
          {
            _lotWorld.RotateSelectedBuildingProp45Degrees(-1);
            _lotStatus = "Building prop rotated counter-clockwise";
          }, true, "quiet"));
          inspector.Add(rotationRow);

          var scaleValue = StyledLabel(
              $"SIZE  {_lotWorld.SelectedBuildingPropScale:0.00}×",
              "inspector-note");
          inspector.Add(scaleValue);
          var scaleSlider = new Slider(0.25f, 3f)
          {
            value = _lotWorld.SelectedBuildingPropScale,
            showInputField = false,
            name = "building-prop-scale"
          };
          scaleSlider.AddToClassList("environment-lighting-slider");
          // The inspector is layered over the interactive viewport.
          // Keep its drag gesture from bubbling into object selection,
          // which otherwise captures the pointer before Slider can
          // update its value.
          scaleSlider.RegisterCallback<PointerDownEvent>(evt =>
              evt.StopPropagation());
          scaleSlider.RegisterCallback<PointerMoveEvent>(evt =>
              evt.StopPropagation());
          scaleSlider.RegisterCallback<PointerUpEvent>(evt =>
              evt.StopPropagation());
          scaleSlider.RegisterValueChangedCallback(evt =>
          {
            if (!_lotWorld.SetSelectedBuildingPropScale(evt.newValue))
              return;
            scaleValue.text = $"SIZE  {evt.newValue:0.00}×";
            _lotStatus = "Building prop resized uniformly";
          });
          inspector.Add(scaleSlider);

          var scaleStops = new VisualElement();
          scaleStops.AddToClassList("building-prop-scale-stops");
          foreach (var preset in new[] { 0.5f, 1f, 1.5f, 2f, 3f })
          {
            var capturedScale = preset;
            var stop = new Button(() =>
            {
              // Assign through the Slider so its thumb, label, model,
              // and persisted attachment value update together.
              scaleSlider.value = capturedScale;
            })
            {
              text = $"{capturedScale:0.#}×",
              name = $"building-prop-scale-{capturedScale:0.#}"
            };
            stop.AddToClassList("building-prop-scale-stop");
            scaleStops.Add(stop);
          }
          scaleStops.RegisterCallback<PointerDownEvent>(evt =>
              evt.StopPropagation());
          scaleStops.RegisterCallback<PointerUpEvent>(evt =>
              evt.StopPropagation());
          inspector.Add(scaleStops);

          var propActions = new VisualElement();
          propActions.AddToClassList("inspector-actions");
          propActions.Add(CfButton.Create("OPEN / CLOSE", () =>
          {
            _lotStatus = _lotWorld.ToggleSelectedBuildingPropDoor()
                      ? "Storefront door toggled"
                      : "This building prop has no operable door";
          }, true, "quiet"));
          propActions.Add(CfButton.Create("DELETE", () =>
          {
            _lotStatus = _lotWorld.DeleteSelectedBuildingProp()
                      ? "Building prop deleted"
                      : "Select a building prop before deleting";
            Show(AppScreen.LotEditor);
          }, true, "danger"));
          inspector.Add(propActions);
        }
        else if (selectedBuilding3D ||
            _lotEditorCategory == LotEditorCategory.Buildings3D)
        {
          if (_lotWorld.SelectedBuilding3DIndex >= 0)
          {
            var construction = _lotWorld.SelectedBuildingConstruction;
            inspector.name = "selected-building-inspector";
            var buildingName = StyledLabel(_lotWorld.SelectedBuilding3DDisplayName,
                "inspector-title");
            buildingName.name = "selected-building-name";
            inspector.Add(buildingName);
            var rotationRow = new VisualElement();
            rotationRow.AddToClassList("inspector-actions");
            var rotateLeft = CfButton.Create("↶ ROTATE LEFT", () =>
                RotateSelectedNativeBuilding(-1), true, "quiet");
            rotateLeft.name = "selected-building-rotate-left";
            rotateLeft.tooltip = "Rotate the selected building 45° counter-clockwise";
            var rotateRight = CfButton.Create("ROTATE RIGHT ↷", () =>
                RotateSelectedNativeBuilding(1), true, "quiet");
            rotateRight.name = "selected-building-rotate-right";
            rotateRight.tooltip = "Rotate the selected building 45° clockwise";
            rotationRow.Add(rotateLeft);
            rotationRow.Add(rotateRight);
            inspector.Add(rotationRow);
            inspector.Add(Property("MODE", construction == null
                ? "COMPLETED BUILDING"
                : construction.StageLabel));
            if (construction != null && !construction.IsComplete)
            {
              inspector.Add(Property("PROGRESS",
                  $"{construction.CompletedStories} OF " +
                  $"{construction.StoryCount} STORIES"));
              inspector.Add(StyledLabel(
                  $"A NEW LEVEL APPEARS EVERY " +
                  $"{BuildingConstructionSequence.SecondsPerStory:0} SECONDS",
                  "inspector-note"));
            }
            var buildRow = new VisualElement();
            buildRow.AddToClassList("inspector-actions");
            buildRow.Add(CfButton.Create(
                _lotWorld.SelectedBuildingFrameVisible
                    ? "HIDE FRAME"
                    : "FRAME",
                ToggleSelectedBuildingFrame,
                construction == null,
                _lotWorld.SelectedBuildingFrameVisible
                    ? "mode-selected"
                    : "quiet"));
            buildRow.Add(CfButton.Create(
                construction == null
                    ? "SIMULATE BUILD"
                    : construction.IsComplete
                        ? "BUILD COMPLETE"
                        : construction.StageLabel,
                BuildSelectedBuilding,
                construction == null,
                construction == null ? "primary" : "quiet"));
            inspector.Add(buildRow);
            if (_lotWorld.SelectedBuildingHasDoor)
              inspector.Add(CfButton.Create(
                  _lotWorld.SelectedBuildingDoorOpen ? "CLOSE DOOR" : "OPEN DOOR",
                  () => { _lotWorld.ToggleSelectedBuildingDoor(); Show(AppScreen.LotEditor); }, true, "quiet"));
          }
          else
          {
            inspector.Add(StyledLabel("Nothing Selected",
                "inspector-title"));
            inspector.Add(StyledLabel(
                "Select a placed 3D building to simulate its construction.",
                "inspector-note"));
          }
        }
        if (_lotEditorCategory == LotEditorCategory.Buildings && _lotWorld.IsSelected)
        {
          inspector.Add(StyledLabel(
              _lotWorld.BuildingPackage.DisplayName,
              "inspector-title"));
          var facing = new VisualElement();
          facing.AddToClassList("facing-indicator");
          facing.Add(StyledLabel(
              $"FACING {_lotWorld.BuildingCardinalOrientation.ToUpperInvariant()}",
              "facing-heading"));
          facing.Add(StyledLabel(
              $"{_lotWorld.BuildingCardinalQuarterTurns * 90}°  •  BUILDING ORIENTATION",
              "facing-degrees"));
          inspector.Add(facing);
          inspector.Add(Property(
              "TYPE",
              $"Hybrid rendered {_lotWorld.BuildingPackage.Category.ToLowerInvariant()} building"));
          inspector.Add(Property("LOT", _lotWorld.LotContract.DisplayName));
          inspector.Add(Property(
              "ROAD ACCESS",
              _lotWorld.LotContract.AllowsInternalRoads
                  ? _lotWorld.LotContract.AllowsThroughTraffic
                      ? "Internal roads • through traffic"
                      : "Internal access roads"
                  : "Driveway connection"));
          inspector.Add(Property(
              "FOUNDATION",
              $"{_lotWorld.BuildingPackage.WidthMeters:0.00} × {_lotWorld.BuildingPackage.DepthMeters:0.00} m"));
          inspector.Add(Property(
              "GRID ANCHOR",
              $"{_lotWorld.BuildingCell.x}, {_lotWorld.BuildingCell.y}"));
          inspector.Add(Property("CAMERA VIEW", _lotWorld.FacingLabel));
          inspector.Add(Property(
              "INSPECTION",
              _lotWorld.InspectionMode.ToString().ToUpperInvariant()));
          inspector.Add(Property(
              "ARTWORK",
              _lotWorld.NeutralPilotShowing
                  ? "GAME-LIT NEUTRAL"
                  : _lotWorld.NeutralPilotFallback
                      ? "BAKED • NEUTRAL FALLBACK"
                      : "BAKED REFERENCE"));
          inspector.Add(Property(
              "PRIMITIVE",
              $"{_lotWorld.BuildingPackage.WidthMeters:0.00} × {_lotWorld.BuildingPackage.DepthMeters:0.00} m • {_lotWorld.BuildingPackage.PrimitiveSourceVersion}"));
          inspector.Add(Property(
              "VOLUMES",
              "Foundation • Walls • Source-derived envelope • Entrance"));

          inspector.Add(CfButton.Create(
              "SHOW PRIMITIVE OVERLAY (20% ART)",
              () => SetInspectionMode(BuildingInspectionMode.Hybrid),
              true,
              _lotWorld.InspectionMode == BuildingInspectionMode.Hybrid
                  ? "mode-selected"
                  : "quiet"));

          var inspectionRow = new VisualElement();
          inspectionRow.AddToClassList("inspector-actions");
          inspectionRow.Add(CfButton.Create(
              "ART ONLY",
              () => SetInspectionMode(BuildingInspectionMode.Artwork),
              true,
              _lotWorld.InspectionMode == BuildingInspectionMode.Artwork
                  ? "mode-selected"
                  : "quiet"));
          inspectionRow.Add(CfButton.Create(
              "3D ONLY",
              () => SetInspectionMode(BuildingInspectionMode.Primitive),
              true,
              _lotWorld.InspectionMode == BuildingInspectionMode.Primitive
                  ? "mode-selected"
                  : "quiet"));
          inspector.Add(inspectionRow);
          inspector.Add(StyledLabel(
              "Overlay fades artwork to 20% so the registered primitive remains easy to inspect.",
              "inspector-note"));

          var moveRow = new VisualElement();
          moveRow.AddToClassList("inspector-actions");
          moveRow.Add(CfButton.Create("←", () => MoveCategorySelectionOrPan(-1, 0), true, "icon"));
          moveRow.Add(CfButton.Create("↑", () => MoveCategorySelectionOrPan(0, 1), true, "icon"));
          moveRow.Add(CfButton.Create("↓", () => MoveCategorySelectionOrPan(0, -1), true, "icon"));
          moveRow.Add(CfButton.Create("→", () => MoveCategorySelectionOrPan(1, 0), true, "icon"));
          inspector.Add(moveRow);

          var counterClockwiseRow = new VisualElement();
          counterClockwiseRow.AddToClassList("inspector-actions");
          var counterClockwiseButton = CfButton.Create(
              "↶  ROTATE COUNTER-CLOCKWISE",
              () => RotateBuilding(-1));
          counterClockwiseButton.name = "Rotate Building Counter-clockwise";
          counterClockwiseButton.tooltip = "Rotate counter-clockwise";
          counterClockwiseRow.Add(counterClockwiseButton);
          inspector.Add(counterClockwiseRow);

          var clockwiseRow = new VisualElement();
          clockwiseRow.AddToClassList("inspector-actions");
          var clockwiseButton = CfButton.Create(
              "↷  ROTATE CLOCKWISE",
              () => RotateBuilding(1));
          clockwiseButton.name = "Rotate Building Clockwise";
          clockwiseButton.tooltip = "Rotate clockwise";
          clockwiseRow.Add(clockwiseButton);
          inspector.Add(clockwiseRow);

          var destructiveRow = new VisualElement();
          destructiveRow.AddToClassList("inspector-actions");
          destructiveRow.Add(CfButton.Create(
              "DELETE BUILDING",
              DeleteBuilding,
              true,
              "danger"));
          inspector.Add(destructiveRow);
        }
        else if (_lotEditorCategory == LotEditorCategory.Buildings)
        {
          inspector.Add(StyledLabel("Nothing Selected", "inspector-title"));
          inspector.Add(StyledLabel(
              "Choose either validated package from BUILDINGS.",
              "inspector-note"));
          var panRow = new VisualElement();
          panRow.AddToClassList("inspector-actions");
          panRow.Add(CfButton.Create("←", () => PanLot(-1, 0), true, "icon"));
          panRow.Add(CfButton.Create("↑", () => PanLot(0, 1), true, "icon"));
          panRow.Add(CfButton.Create("↓", () => PanLot(0, -1), true, "icon"));
          panRow.Add(CfButton.Create("→", () => PanLot(1, 0), true, "icon"));
          inspector.Add(panRow);
          inspector.Add(StyledLabel("PAN LOT VIEW", "inspector-note"));
        }
        if (_lotEditorCategory == LotEditorCategory.Roads &&
            _lotWorld.LotType == LotType.Neighborhood)
        {
          inspector.Add(StyledLabel("ROADS — 10 M GRID", "section-label"));
          inspector.Add(Property("PIECE", _lotWorld.SelectedRoadTopology.ToString().ToUpperInvariant()));
          inspector.Add(Property("CELL / TURN",
              $"{_lotWorld.RoadCursorCell.x}, {_lotWorld.RoadCursorCell.y} • {_lotWorld.RoadRotationQuarterTurns * 90}°"));
          inspector.Add(StyledLabel(
              "PRESS S TO PIN A START • MOVE MOUSE • CLICK TO BUILD • ESC CANCELS",
              "inspector-note"));
          var familySection = RoadFoldout("ROAD FAMILY", _roadFamilyExpanded,
              value => _roadFamilyExpanded = value);
          var materialsSection = RoadFoldout("MATERIALS", _roadMaterialsExpanded,
              value => _roadMaterialsExpanded = value);
          var shapeSection = RoadFoldout("SHAPE & PLACEMENT", _roadShapeExpanded,
              value => _roadShapeExpanded = value);
          var trafficSection = RoadFoldout("TRAFFIC CONNECTION", _roadTrafficExpanded,
              value => _roadTrafficExpanded = value);
          var editSection = RoadFoldout("EDIT & HISTORY", _roadEditExpanded,
              value => _roadEditExpanded = value);
          var viewSection = RoadFoldout("VIEW & DIAGNOSTICS", _roadViewExpanded,
              value => _roadViewExpanded = value);
          inspector.Add(familySection);
          inspector.Add(materialsSection);
          inspector.Add(shapeSection);
          inspector.Add(trafficSection);
          inspector.Add(editSection);
          inspector.Add(viewSection);
          trafficSection.Add(Property("TRAFFIC TYPE",
              TrafficLotModel.DisplayName(_lotWorld.TrafficType).ToUpperInvariant()));
          familySection.Add(Property("SELECTED",
              _lotWorld.RoadPackage.DisplayName.ToUpperInvariant()));
          var familyRow = new VisualElement();
          familyRow.AddToClassList("inspector-actions");
          familyRow.Add(CfButton.Create("2-LANE + WALK",
              () => SelectRoadPackage(RoadPiecePackageCatalog.TwoLaneSidewalkId), true,
              _lotWorld.SelectedRoadPackageId == RoadPiecePackageCatalog.TwoLaneSidewalkId
                  ? "mode-selected" : "quiet"));
          familyRow.Add(CfButton.Create("1-WAY + WALK",
              () => SelectRoadPackage(RoadPiecePackageCatalog.OneWaySidewalkId), true,
              _lotWorld.SelectedRoadPackageId == RoadPiecePackageCatalog.OneWaySidewalkId
                  ? "mode-selected" : "quiet"));
          familySection.Add(familyRow);
          var familyRowTwo = new VisualElement();
          familyRowTwo.AddToClassList("inspector-actions");
          familyRowTwo.Add(CfButton.Create("ALLEY",
              () => SelectRoadPackage(RoadPiecePackageCatalog.AlleyId), true,
              _lotWorld.SelectedRoadPackageId == RoadPiecePackageCatalog.AlleyId
                  ? "mode-selected" : "quiet"));
          familyRowTwo.Add(CfButton.Create("PEDESTRIAN",
              () => SelectRoadPackage(RoadPiecePackageCatalog.PedestrianStreetId), true,
              _lotWorld.SelectedRoadPackageId == RoadPiecePackageCatalog.PedestrianStreetId
                  ? "mode-selected" : "quiet"));
          familySection.Add(familyRowTwo);
          var familyRowThree = new VisualElement();
          familyRowThree.AddToClassList("inspector-actions");
          familyRowThree.Add(CfButton.Create("WIDE AVENUE",
              () => SelectRoadPackage(RoadPiecePackageCatalog.WideTwoLaneAvenueId), true,
              _lotWorld.SelectedRoadPackageId == RoadPiecePackageCatalog.WideTwoLaneAvenueId
                  ? "mode-selected" : "quiet"));
          familyRowThree.Add(CfButton.Create("BOULEVARD",
              () => SelectRoadPackage(RoadPiecePackageCatalog.DividedBoulevardId), true,
              _lotWorld.SelectedRoadPackageId == RoadPiecePackageCatalog.DividedBoulevardId
                  ? "mode-selected" : "quiet"));
          familySection.Add(familyRowThree);
          var familyRowFour = new VisualElement();
          familyRowFour.AddToClassList("inspector-actions");
          familyRowFour.Add(CfButton.Create("BRICK ROAD",
              () => SelectRoadPackage(RoadPiecePackage.LegacyPackageId), true,
              _lotWorld.SelectedRoadPackageId == RoadPiecePackage.LegacyPackageId
                  ? "mode-selected" : "quiet"));
          familyRowFour.Add(CfButton.Create("DIRT ROAD",
              () => SelectRoadPackage(RoadPiecePackageCatalog.DirtRoadId), true,
              _lotWorld.SelectedRoadPackageId == RoadPiecePackageCatalog.DirtRoadId
                  ? "mode-selected" : "quiet"));
          familySection.Add(familyRowFour);
          materialsSection.Add(Property("ROAD",
              _lotWorld.SelectedRoadMaterial.DisplayName.ToUpperInvariant()));
          materialsSection.Add(Property("SIDEWALK",
              _lotWorld.SelectedSidewalkMaterial.DisplayName.ToUpperInvariant()));
          materialsSection.Add(CfButton.Create("CHOOSE MATERIALS…",
              OpenRoadMaterialModal, _lotWorld.SelectedRoadSupportsMaterials, "primary"));
          if (_lotWorld.SelectedRoadCanConnectOutside)
          {
            var connector = _lotWorld.SelectedOutsideConnector;
            trafficSection.Add(Property("OUTSIDE CONNECTOR",
                connector == null ? "NOT CONNECTED" : connector.Flow.ToString().ToUpperInvariant()));
            var connectorActions = new VisualElement();
            connectorActions.AddToClassList("inspector-actions");
            connectorActions.Add(CfButton.Create("↔ TWO-WAY",
                () => SetOutsideConnector(RoadTrafficFlow.TwoWay), true,
                connector?.Flow == RoadTrafficFlow.TwoWay ? "mode-selected" : "quiet"));
            connectorActions.Add(CfButton.Create("IN",
                () => SetOutsideConnector(RoadTrafficFlow.InboundOnly), true,
                connector?.Flow == RoadTrafficFlow.InboundOnly ? "mode-selected" : "quiet"));
            connectorActions.Add(CfButton.Create("OUT",
                () => SetOutsideConnector(RoadTrafficFlow.OutboundOnly), true,
                connector?.Flow == RoadTrafficFlow.OutboundOnly ? "mode-selected" : "quiet"));
            trafficSection.Add(connectorActions);
            if (connector != null)
              trafficSection.Add(CfButton.Create("REMOVE OUTSIDE CONNECTOR",
                  RemoveOutsideConnector, true, "danger"));
          }
          var roadPalette = new VisualElement();
          roadPalette.AddToClassList("inspector-actions");
          roadPalette.AddToClassList("road-topology-palette");
          roadPalette.Add(CfButton.CreateIcon("road-topology-straight", "│",
              "Straight road", () => SelectRoadPiece(RoadPieceTopology.Straight),
              _lotWorld.RoadPackage.Piece(RoadPieceTopology.Straight)?.HasArtwork == true,
              _lotWorld.SelectedRoadTopology == RoadPieceTopology.Straight));
          roadPalette.Add(CfButton.CreateIcon("road-topology-corner", "└",
              "Corner road", () => SelectRoadPiece(RoadPieceTopology.Corner),
              _lotWorld.RoadPackage.Piece(RoadPieceTopology.Corner)?.HasArtwork == true,
              _lotWorld.SelectedRoadTopology == RoadPieceTopology.Corner));
          roadPalette.Add(CfButton.CreateIcon("road-topology-t-junction", "┬",
              "T-junction", () => SelectRoadPiece(RoadPieceTopology.TJunction),
              _lotWorld.RoadPackage.Piece(RoadPieceTopology.TJunction)?.HasArtwork == true,
              _lotWorld.SelectedRoadTopology == RoadPieceTopology.TJunction));
          roadPalette.Add(CfButton.CreateIcon("road-topology-four-way", "┼",
              "Four-way intersection", () => SelectRoadPiece(RoadPieceTopology.FourWay),
              _lotWorld.RoadPackage.Piece(RoadPieceTopology.FourWay)?.HasArtwork == true,
              _lotWorld.SelectedRoadTopology == RoadPieceTopology.FourWay));
          roadPalette.Add(CfButton.CreateIcon("road-topology-endpoint", "●",
              "Road endpoint", () => SelectRoadPiece(RoadPieceTopology.Endpoint),
              _lotWorld.RoadPackage.Piece(RoadPieceTopology.Endpoint)?.HasArtwork == true,
              _lotWorld.SelectedRoadTopology == RoadPieceTopology.Endpoint));
          roadPalette.Add(CfButton.CreateIcon("road-topology-straight-to-diagonal", "↘",
              "Straight-to-angle transition",
              () => SelectRoadPiece(RoadPieceTopology.StraightToDiagonal),
              _lotWorld.RoadPackage.Piece(RoadPieceTopology.StraightToDiagonal)?.HasArtwork == true,
              _lotWorld.SelectedRoadTopology == RoadPieceTopology.StraightToDiagonal));
          roadPalette.Add(CfButton.CreateIcon("road-topology-diagonal", "╲",
              "Angled road", () => SelectRoadPiece(RoadPieceTopology.Diagonal),
              _lotWorld.RoadPackage.Piece(RoadPieceTopology.Diagonal)?.HasArtwork == true,
              _lotWorld.SelectedRoadTopology == RoadPieceTopology.Diagonal));
          shapeSection.Add(roadPalette);
          var completePalette = new VisualElement();
          completePalette.AddToClassList("inspector-actions");
          completePalette.Add(CfButton.Create("AUTO FIT", ApplyRoadSuggestion, _lotWorld.HasRoadSuggestion, "quiet"));
          shapeSection.Add(completePalette);
          var roadCursor = new VisualElement();
          roadCursor.AddToClassList("inspector-actions");
          roadCursor.Add(CfButton.Create("←", () => MoveCategorySelectionOrPan(-1, 0), true, "icon"));
          roadCursor.Add(CfButton.Create("↑", () => MoveCategorySelectionOrPan(0, 1), true, "icon"));
          roadCursor.Add(CfButton.Create("↓", () => MoveCategorySelectionOrPan(0, -1), true, "icon"));
          roadCursor.Add(CfButton.Create("→", () => MoveCategorySelectionOrPan(1, 0), true, "icon"));
          shapeSection.Add(roadCursor);
          var roadActions = new VisualElement();
          roadActions.AddToClassList("inspector-actions");
          roadActions.Add(CfButton.Create("ROTATE ↻", RotateRoadPiece, true));
          roadActions.Add(CfButton.Create("PLACE / REPLACE", PlaceRoadPiece, _lotWorld.SelectedRoadPieceAvailable));
          roadActions.Add(CfButton.Create("DELETE", DeleteRoadPiece, true, "danger"));
          editSection.Add(roadActions);
          var historyActions = new VisualElement();
          historyActions.AddToClassList("inspector-actions");
          historyActions.Add(CfButton.Create("↶ UNDO", UndoRoadEdit, _lotWorld.CanUndoRoadEdit, "quiet"));
          historyActions.Add(CfButton.Create("REDO ↷", RedoRoadEdit, _lotWorld.CanRedoRoadEdit, "quiet"));
          editSection.Add(historyActions);
          var roadZoomActions = new VisualElement();
          roadZoomActions.AddToClassList("inspector-actions");
          roadZoomActions.Add(CfButton.Create("− ZOOM", () => StepZoom(1),
              _lotWorld.ZoomLevel != LotZoomLevel.Neighborhood, "quiet"));
          roadZoomActions.Add(CfButton.Create("ZOOM +", () => StepZoom(-1),
              _lotWorld.ZoomLevel != LotZoomLevel.Detail, "quiet"));
          viewSection.Add(roadZoomActions);
          viewSection.Add(Property("ZOOM", _lotWorld.ZoomLevel.ToString().ToUpperInvariant()));
          viewSection.Add(Property("NETWORK",
              $"{_lotWorld.PlacedRoadCount} pieces • {_lotWorld.RoadValidationIssues.Count} issues"));
        }
        else if (_lotEditorCategory == LotEditorCategory.Roads)
        {
          inspector.Add(StyledLabel("ROADS REQUIRE A NEIGHBORHOOD LOT", "inspector-title"));
          inspector.Add(StyledLabel("Switch this lot to Neighborhood to place connected road tiles and allow through traffic.", "inspector-note"));
          inspector.Add(CfButton.Create("MAKE NEIGHBORHOOD LOT", () => SetLotType(LotType.Neighborhood), true, "primary"));
        }
        if (_lotEditorCategory == LotEditorCategory.Vehicles)
          ComposeVehicleInspector(inspector);
        if (_lotEditorCategory == LotEditorCategory.Boats)
          ComposeBoatInspector(inspector);
        if (_lotEditorCategory == LotEditorCategory.Railroad)
        {
          inspector.Add(StyledLabel("RAILROADS", "inspector-title"));
          var railroadSection = TransitSection("RAILROAD", _railroadTransitExpanded,
              value => _railroadTransitExpanded = value, out var railroadContent);
          if (_railroadTransitExpanded)
          {
            railroadContent.Add(StyledLabel(
                "Heavy rail controls will appear here as the network is added.",
                "inspector-note"));
          }
          inspector.Add(railroadSection);

          var streetcarSection = TransitSection("STREET CAR", _streetcarTransitExpanded,
              value => _streetcarTransitExpanded = value, out var streetcarContent);
          streetcarContent.Add(Property("TRACK", _lotWorld.SelectedStreetcarTrackTopology.ToString().ToUpperInvariant()));
          streetcarContent.Add(Property("PROJECTED RIDERS", _lotWorld.StreetcarRiderDemand.ToString()));
          streetcarContent.Add(Property("ACTIVE STREETCARS", _lotWorld.ActiveStreetcarCount.ToString()));
          streetcarContent.Add(Property("STOPS", _lotWorld.StreetcarStopCount.ToString()));
          streetcarContent.Add(Property("BOARDED THIS SESSION",
              _lotWorld.StreetcarBoardedPassengerCount.ToString()));
          var trackTypes = new VisualElement();
          trackTypes.AddToClassList("inspector-actions");
          trackTypes.Add(CfButton.Create("STRAIGHT",
              () => SelectStreetcarTrack(StreetcarTrackTopology.Straight), true,
              _lotWorld.SelectedStreetcarTrackTopology == StreetcarTrackTopology.Straight ? "mode-selected" : "quiet"));
          trackTypes.Add(CfButton.Create("BROAD CURVE",
              () => SelectStreetcarTrack(StreetcarTrackTopology.Curve), true,
              _lotWorld.SelectedStreetcarTrackTopology == StreetcarTrackTopology.Curve ? "mode-selected" : "quiet"));
          streetcarContent.Add(trackTypes);
          var placement = new VisualElement();
          placement.AddToClassList("inspector-actions");
          placement.Add(CfButton.Create("ROTATE ↻", () => { _lotWorld.RotateStreetcarTrack(); ComposeLotEditor(); }));
          placement.Add(CfButton.Create("PLACE / REPLACE", () => { _lotWorld.PlaceStreetcarTrack(); ComposeLotEditor(); }));
          placement.Add(CfButton.Create("DELETE", () => { _lotWorld.DeleteStreetcarTrack(); ComposeLotEditor(); }, true, "danger"));
          streetcarContent.Add(placement);
          var stops = new VisualElement();
          stops.AddToClassList("inspector-actions");
          stops.Add(CfButton.Create("PLACE / FLIP STOP", () =>
          {
            _lotStatus = _lotWorld.PlaceStreetcarStop()
                      ? "Streetcar stop placed • repeat to flip platform side"
                      : "Streetcar stops require track beneath them";
            ComposeLotEditor();
          }, true, "primary"));
          stops.Add(CfButton.Create("DELETE STOP", () =>
          {
            _lotStatus = _lotWorld.DeleteStreetcarStop()
                      ? "Streetcar stop deleted" : "No stop on selected track cell";
            ComposeLotEditor();
          }, true, "danger"));
          streetcarContent.Add(stops);
          var cursor = new VisualElement();
          cursor.AddToClassList("inspector-actions");
          cursor.Add(CfButton.Create("←", () => MoveCategorySelectionOrPan(-1, 0), true, "icon"));
          cursor.Add(CfButton.Create("↑", () => MoveCategorySelectionOrPan(0, 1), true, "icon"));
          cursor.Add(CfButton.Create("↓", () => MoveCategorySelectionOrPan(0, -1), true, "icon"));
          cursor.Add(CfButton.Create("→", () => MoveCategorySelectionOrPan(1, 0), true, "icon"));
          streetcarContent.Add(cursor);
          var demand = new VisualElement();
          demand.AddToClassList("inspector-actions");
          demand.Add(CfButton.Create("− 40 RIDERS", () => { _lotWorld.AdjustStreetcarRiderDemand(-40); ComposeLotEditor(); }, true, "quiet"));
          demand.Add(CfButton.Create("+ 40 RIDERS", () => { _lotWorld.AdjustStreetcarRiderDemand(40); ComposeLotEditor(); }, true, "quiet"));
          streetcarContent.Add(demand);
          inspector.Add(streetcarSection);
        }
        if (_lotEditorCategory == LotEditorCategory.Paths)
        {
          inspector.Add(StyledLabel("WALKWAYS & DRIVEWAYS", "inspector-title"));
          inspector.Add(Property(
              "ACTIVE NETWORK",
              _lotWorld.CirculationMode.ToString().ToUpperInvariant()));
          inspector.Add(Property(
              "PEDESTRIAN",
              $"{_lotWorld.PedestrianNodeCount} nodes • {_lotWorld.PedestrianSegmentCount} segments"));
          inspector.Add(Property(
              "VEHICLE",
              $"{_lotWorld.VehicleNodeCount} nodes • {_lotWorld.VehicleSegmentCount} segments"));
          inspector.Add(Property("TRAFFIC LANES",
              $"{_lotWorld.VehicleLaneCount} directed • {_lotWorld.VehiclePresentationCount} vehicles"));
          inspector.Add(Property("INTERSECTIONS",
              $"{_lotWorld.TrafficIntersectionCount} governed"));
          inspector.Add(Property("AVERAGE SPEED",
              $"{_lotWorld.AverageVehicleSpeedMetersPerSecond:0.0} m/s"));
          inspector.Add(Property("MINIMUM GAP",
              float.IsPositiveInfinity(_lotWorld.MinimumVehicleGapMeters)
                  ? "OPEN"
                  : $"{_lotWorld.MinimumVehicleGapMeters:0.0} m"));
          inspector.Add(Property("BRAKING", $"{_lotWorld.BrakingVehicleCount} vehicles"));
          if (_lotWorld.RoadPackage != null)
          {
            inspector.Add(Property("ROAD ART", _lotWorld.RoadPackage.DisplayName.ToUpperInvariant()));
            inspector.Add(Property("ROAD PIECES", "Straight • T • Four-way • Corner/end pending"));
          }
          inspector.Add(Property(
              "CURSOR",
              $"{_lotWorld.CirculationCursorMeters.x:0}, {_lotWorld.CirculationCursorMeters.y:0} m"));
          var networkRow = new VisualElement();
          networkRow.AddToClassList("inspector-actions");
          networkRow.Add(CfButton.Create(
              "PEDESTRIAN",
              () => SetCirculationMode(CirculationMode.Pedestrian),
              true,
              _lotWorld.CirculationMode == CirculationMode.Pedestrian ? "mode-selected" : "quiet"));
          networkRow.Add(CfButton.Create(
              "VEHICLE",
              () => SetCirculationMode(CirculationMode.Vehicle),
              true,
              _lotWorld.CirculationMode == CirculationMode.Vehicle ? "mode-selected" : "quiet"));
          inspector.Add(networkRow);
          if (_lotWorld.CirculationMode == CirculationMode.Pedestrian)
          {
            var pathTypeRow = new VisualElement();
            pathTypeRow.AddToClassList("inspector-actions");
            pathTypeRow.Add(CfButton.Create(
                "FLAT PATH",
                () => SetPedestrianPathKind(PedestrianPathKind.Flat),
                true,
                _lotWorld.SelectedPedestrianPathKind ==
                    PedestrianPathKind.Flat ? "mode-selected" : "quiet"));
            pathTypeRow.Add(CfButton.Create(
                "STAIRS ↗",
                () => SetPedestrianPathKind(PedestrianPathKind.Stairs),
                true,
                _lotWorld.SelectedPedestrianPathKind ==
                    PedestrianPathKind.Stairs ? "mode-selected" : "quiet"));
            inspector.Add(pathTypeRow);
          }
          var cursorRow = new VisualElement();
          cursorRow.AddToClassList("inspector-actions");
          cursorRow.Add(CfButton.Create("←", () => MoveCategorySelectionOrPan(-1, 0), true, "icon"));
          cursorRow.Add(CfButton.Create("↑", () => MoveCategorySelectionOrPan(0, 1), true, "icon"));
          cursorRow.Add(CfButton.Create("↓", () => MoveCategorySelectionOrPan(0, -1), true, "icon"));
          cursorRow.Add(CfButton.Create("→", () => MoveCategorySelectionOrPan(1, 0), true, "icon"));
          inspector.Add(cursorRow);
          var circulationActions = new VisualElement();
          circulationActions.AddToClassList("inspector-actions");
          circulationActions.Add(CfButton.Create("ADD + CONNECT", AddCirculationNode, true));
          circulationActions.Add(CfButton.Create("DELETE LAST", DeleteCirculationNode, true, "danger"));
          inspector.Add(circulationActions);
          inspector.Add(CfButton.Create(
              _lotWorld.CirculationDiagnosticsVisible ? "HIDE GRAPH" : "SHOW GRAPH",
              ToggleCirculationDiagnostics,
              true,
              _lotWorld.CirculationDiagnosticsVisible ? "mode-selected" : "quiet"));
        }
        if (_lotEditorCategory == LotEditorCategory.View)
        {
          inspector.Add(StyledLabel("CAMERA & DISPLAY", "inspector-title"));
          var inspectionRow = new VisualElement();
          inspectionRow.AddToClassList("inspector-actions");
          inspectionRow.Add(CfButton.Create("ART", () => SetInspectionMode(BuildingInspectionMode.Artwork), true, _lotWorld.InspectionMode == BuildingInspectionMode.Artwork ? "mode-selected" : "quiet"));
          inspectionRow.Add(CfButton.Create("OVERLAY", () => SetInspectionMode(BuildingInspectionMode.Hybrid), true, _lotWorld.InspectionMode == BuildingInspectionMode.Hybrid ? "mode-selected" : "quiet"));
          inspectionRow.Add(CfButton.Create("3D", () => SetInspectionMode(BuildingInspectionMode.Primitive), true, _lotWorld.InspectionMode == BuildingInspectionMode.Primitive ? "mode-selected" : "quiet"));
          inspector.Add(inspectionRow);
          var zoomRow = new VisualElement();
          zoomRow.AddToClassList("inspector-actions");
          zoomRow.Add(CfButton.Create("− ZOOM", () => StepZoom(1), _lotWorld.ZoomLevel != LotZoomLevel.Neighborhood));
          zoomRow.Add(CfButton.Create("ZOOM +", () => StepZoom(-1), _lotWorld.ZoomLevel != LotZoomLevel.Detail));
          inspector.Add(zoomRow);
          AddLotRotationControls(inspector);
          inspector.Add(CfButton.Create("REGISTRATION [D]", ToggleRegistrationDiagnostics, _lotWorld.HasBuilding, _lotWorld.RegistrationDiagnosticsVisible ? "mode-selected" : "quiet"));
          inspector.Add(Property("ZOOM", _lotWorld.ZoomLevel.ToString().ToUpperInvariant()));
          inspector.Add(Property("GRID", "1 M MINOR • 10 M MAJOR"));
        }
        if (_lotEditorCategory == LotEditorCategory.Environment)
        {
          inspector.Add(StyledLabel("ENVIRONMENT", "inspector-title"));
          inspector.Add(Property("TIME", timeSpec.Label));
          inspector.Add(Property("ARTWORK", _lotWorld.NeutralPilotShowing ? "GAME-LIT" : "BAKED REFERENCE"));
        }
        if (_lotEditorCategory == LotEditorCategory.Garden)
        {
          inspector.Add(StyledLabel("GARDEN", "inspector-title"));
          inspector.Add(Property("LIBRARY", "COMPLETE PLANTED BEDS"));
          inspector.Add(Property("ACTIVE",
              _placementPropId == LotWorldController.GeorgianGardenSquarePropId
                  ? "MIXED SQUARE BED" :
              _placementPropId == LotWorldController.GeorgianGardenRectanglePropId
                  ? "MIXED RECTANGLE BED" :
              _placementPropId == LotWorldController.GeorgianHedgeSquarePropId
                  ? "CLIPPED HEDGE SQUARE" :
              _placementPropId == LotWorldController.GeorgianHedgeRectanglePropId
                  ? "CLIPPED HEDGE RECTANGLE" :
              _placementPropId == LotWorldController.GeorgianGardenBorderPropId
                  ? "FLOWER & THICKET BORDER" : "NONE"));
          inspector.Add(CfButton.Create("GARDEN LIBRARY…",
              OpenGardenModal, true, "primary"));
          var gardenSelected = LotWorldController.IsGardenPropId(
              _lotWorld.SelectedPropId);
          var gardenActions = new VisualElement();
          gardenActions.AddToClassList("inspector-actions");
          gardenActions.Add(CfButton.Create("↺ ROTATE",
              () => RotateSelectedProp(-1), gardenSelected));
          gardenActions.Add(CfButton.Create("ROTATE ↻",
              () => RotateSelectedProp(1), gardenSelected));
          inspector.Add(gardenActions);
          inspector.Add(CfButton.Create("DELETE SELECTED",
              DeleteSelectedProp, gardenSelected, "danger"));
        }
        if (_lotEditorCategory is LotEditorCategory.Flora or
            LotEditorCategory.Props or LotEditorCategory.Characters or
            LotEditorCategory.Entertainment)
        {
          var flora = _lotEditorCategory == LotEditorCategory.Flora;
          var characters = _lotEditorCategory == LotEditorCategory.Characters;
          var entertainment =
              _lotEditorCategory == LotEditorCategory.Entertainment;
          inspector.Add(StyledLabel(flora ? "FLORA" :
              characters ? "3D CHARACTERS" :
              entertainment ? "ENTERTAINMENT" : "PROPS",
              "inspector-title"));
          inspector.Add(Property("LIBRARY", flora
              ? "TREES • SHRUBS • HEDGES • FLOWERS • AGRICULTURE"
              : characters
                  ? "PEOPLE • ANIMATED • INTERACTIVE"
              : entertainment
                  ? "EXHIBITS • ATTRACTIONS • ENCLOSURES"
              : "LIGHTING • FURNITURE • SIGNS • DETAILS"));
          if (flora)
          {
            inspector.Add(Property("PLACED", $"{_lotWorld.FloraCount} ITEMS"));
            inspector.Add(Property("TO PLACE",
                string.IsNullOrWhiteSpace(_placementFloraId)
                    ? "NONE" : FloraDisplayName(_placementFloraId).ToUpperInvariant()));
            inspector.Add(Property("SELECTED",
                string.IsNullOrWhiteSpace(_lotWorld.SelectedFloraId)
                    ? "NONE"
                    : FloraDisplayName(_lotWorld.SelectedFloraId)
                        .ToUpperInvariant()));
            inspector.Add(Property("HEIGHT KEYS",
                "L SINKS • H RAISES"));
            inspector.Add(Property("MOVE",
                "CLICK AND DRAG SELECTED FLORA"));
            inspector.Add(CfButton.Create("CHOOSE FLORA…",
                OpenFloraModal, true, "primary"));
          }
          else if (entertainment)
          {
            inspector.Add(Property("PLACED",
                $"{_lotWorld.PropCount} TOTAL PROPS"));
            inspector.Add(Property("ACTIVE",
                string.IsNullOrWhiteSpace(_placementPropId)
                    ? "NONE" : "KING KONG ENCLOSURE"));
            inspector.Add(Property("FOOTPRINT", "16 × 16 M"));
            inspector.Add(CfButton.Create("CHOOSE ATTRACTION…",
                OpenEntertainmentModal, true, "primary"));
            var entertainmentActions = new VisualElement();
            entertainmentActions.AddToClassList("inspector-actions");
            entertainmentActions.Add(CfButton.Create("↺ ROTATE",
                () => RotateSelectedProp(-1),
                _lotWorld.SelectedPropIndex >= 0));
            entertainmentActions.Add(CfButton.Create("ROTATE ↻",
                () => RotateSelectedProp(1),
                _lotWorld.SelectedPropIndex >= 0));
            inspector.Add(entertainmentActions);
            inspector.Add(CfButton.Create("DELETE SELECTED",
                DeleteSelectedProp,
                _lotWorld.SelectedPropIndex >= 0, "danger"));
          }
          else if (!characters)
          {
            inspector.Add(Property("PLACED", $"{_lotWorld.PropCount} PROPS"));
            inspector.Add(Property("ACTIVE",
                string.IsNullOrWhiteSpace(_placementPropId)
                    ? "NONE"
                    : _placementPropId == LotWorldController.NewEnglandBarnPropId
                        ? "NEW ENGLAND BARN"
                    : _placementPropId == LotWorldController.Hedge3DPropId
                        ? "3D HEDGE"
                    : _placementPropId == "wrought-iron-fence-corner-v01"
                        ? "WROUGHT-IRON CORNER"
                        : "WROUGHT-IRON FENCE"));
            inspector.Add(Property("FAMILY",
                _placementPropId == LotWorldController.NewEnglandBarnPropId
                    ? "AGRICULTURE"
                    : _placementPropId == LotWorldController.Hedge3DPropId
                    ? "FLORA PROPS" : "FENCES & GATES"));
            inspector.Add(Property("POSITIONING", "1 PIXEL • ARROW KEYS"));
            inspector.Add(CfButton.Create("CHOOSE PROP…",
                OpenPropsModal, true, "primary"));
            var propActions = new VisualElement();
            propActions.AddToClassList("inspector-actions");
            propActions.Add(CfButton.Create("↺ ROTATE",
                () => RotateSelectedProp(-1), _lotWorld.SelectedPropIndex >= 0));
            propActions.Add(CfButton.Create("ROTATE ↻",
                () => RotateSelectedProp(1), _lotWorld.SelectedPropIndex >= 0));
            inspector.Add(propActions);
            inspector.Add(CfButton.Create("DELETE SELECTED",
                DeleteSelectedProp, _lotWorld.SelectedPropIndex >= 0, "danger"));
          }
          else
          {
            inspector.Add(Property("PLACED", $"{_lotWorld.PropCount} TOTAL PROPS"));
            inspector.Add(Property("CONTROLS", _lotWorld.SelectedPropCanWalk
                ? "CLICK GROUND TO WALK" : "ARROWS WALK • SPACE IDLE"));
            inspector.Add(Property("VISIBLE", "ZOOM LEVELS 1–3"));
            inspector.Add(CfButton.Create("CHOOSE CHARACTER…",
                OpenCharactersModal, true, "primary"));
            if (_lotWorld.SelectedPropIsFarmer)
            {
              inspector.Add(CfButton.Create("HOE", () =>
              {
                _lotStatus = _lotWorld.HoeSelectedFarmer()
                              ? "Farmer hoeing • arrows walk • Space stops"
                              : "Move the farmer onto a field before hoeing";
              }, true, "primary"));
            }
            if (_lotWorld.SelectedPropIsThreeDimensionalCharacter && !_lotWorld.SelectedPropIsFarmer)
            {
              inspector.Add(Property("ACTIVE SCRIPT",
                  CharacterBehaviorScript.DisplayName(
                      _lotWorld.SelectedCharacterBehaviorScript)
                      .ToUpperInvariant()));
              var scriptRow = new VisualElement();
              scriptRow.AddToClassList("inspector-actions");
              scriptRow.Add(CfButton.Create("BAU", () =>
                  SelectCharacterScript(CharacterBehaviorScript.BusinessAsUsual),
                  true, _lotWorld.SelectedCharacterBehaviorScript ==
                      CharacterBehaviorScript.BusinessAsUsual
                      ? "mode-selected" : "quiet"));
              scriptRow.Add(CfButton.Create("HARASS", () =>
                  SelectCharacterScript(CharacterBehaviorScript.HarassPedestrian),
                  LotWorldController.IsHooligan(
                      _lotWorld.Session.Data.Props[
                          _lotWorld.SelectedPropIndex].PropId),
                  _lotWorld.SelectedCharacterBehaviorScript ==
                      CharacterBehaviorScript.HarassPedestrian
                      ? "mode-selected" : "quiet"));
              scriptRow.Add(CfButton.Create("EVADE POLICE", () =>
                  SelectCharacterScript(CharacterBehaviorScript.EvadePolice),
                  LotWorldController.IsHooligan(
                      _lotWorld.Session.Data.Props[
                          _lotWorld.SelectedPropIndex].PropId),
                  _lotWorld.SelectedCharacterBehaviorScript ==
                      CharacterBehaviorScript.EvadePolice
                      ? "mode-selected" : "quiet"));
              inspector.Add(scriptRow);
              inspector.Add(CfButton.Create("SET AS ARCHETYPE DEFAULT", () =>
              {
                _lotWorld.SetSelectedCharacterScriptAsDefault();
                _lotStatus = "Default character script updated for this archetype";
                Show(AppScreen.LotEditor);
              }, true, "quiet"));
              inspector.Add(Property("FIGHT HOOLIGAN",
                  "AWAITING COMBAT ANIMATIONS"));
            }
            inspector.Add(CfButton.Create("STOP • IDLE [SPACE]",
                () =>
                {
                  _lotWorld.StopSelectedCharacter();
                  _lotStatus = "Character stopped • idle";
                }, _lotWorld.SelectedPropIsThreeDimensionalCharacter));
            inspector.Add(CfButton.Create("DELETE SELECTED",
                DeleteSelectedProp, _lotWorld.SelectedPropIsThreeDimensionalCharacter,
                "danger"));
          }
          inspector.Add(StyledLabel(
              "Placement and package controls will appear here as the library is added.",
              "inspector-note"));
        }
        if (_lotEditorCategory == LotEditorCategory.BaseTextures)
        {
          inspector.Add(StyledLabel("BASE TEXTURES", "inspector-title"));
          inspector.Add(Property("ACTIVE", string.IsNullOrWhiteSpace(_lotWorld.BaseTextureId)
              ? "DEFAULT GROUND" : _lotWorld.BaseTextureId.Replace('-', ' ').ToUpperInvariant()));
          inspector.Add(Property("SCOPE", "ENTIRE LOT"));
          inspector.Add(CfButton.Create("CHOOSE GRASS…", OpenBaseTextureModal, true, "primary"));
        }
        if (_lotEditorCategory == LotEditorCategory.OverlayTextures)
        {
          inspector.Add(StyledLabel("OVERLAY TEXTURES", "inspector-title"));
          inspector.Add(Property("PLACED", $"{_lotWorld.OverlayTextureCount} TILES"));
          inspector.Add(Property("ACTIVE", string.IsNullOrWhiteSpace(_placementOverlayTextureId)
              ? "NONE"
              : LotWorldController.ResolveOverlayTexture(
                  _placementOverlayTextureId).DisplayName.ToUpperInvariant()));
          inspector.Add(Property("SELECTED", _lotWorld.SelectedOverlayTextureIndex >= 0
              ? "OVERLAY TILE" : "NONE"));
          inspector.Add(CfButton.Create("CHOOSE OVERLAY…", OpenOverlayTextureModal, true, "primary"));
          inspector.Add(CfButton.Create("DELETE SELECTED [DELETE]",
              DeleteSelectedOverlayTexture,
              _lotWorld.SelectedOverlayTextureIndex >= 0, "danger"));
          inspector.Add(Property("ROTATE",
              "LEFT = CLOCKWISE • RIGHT = COUNTER-CLOCKWISE"));
        }
        if (_lotEditorCategory == LotEditorCategory.Decals)
        {
          inspector.Add(StyledLabel("DECALS", "inspector-title"));
          inspector.Add(Property("PLACED", _lotWorld.DecalCount.ToString()));
          inspector.Add(Property("ACTIVE",
              string.IsNullOrWhiteSpace(_placementDecalCategory)
                  ? "NONE" : _placementDecalCategory.ToUpperInvariant()));
          inspector.Add(Property("VARIATION", "RANDOM TEXTURE • ROTATION"));
          inspector.Add(CfButton.Create("CHOOSE CATEGORY…",
              OpenDecalsModal, true, "primary"));
          inspector.Add(Property("ERASER", "HOLD CONTROL + CLICK"));
          inspector.Add(CfButton.Create("UNDO LAST [CTRL+Z]", () =>
          {
            _lotStatus = _lotWorld.UndoLastDecal()
                      ? "Last decal removed" : "No placed decal to undo";
            Show(AppScreen.LotEditor);
          }, _lotWorld.DecalCount > 0, "quiet"));
        }
        if (_hasOpenLot && _lotEditorCategory == LotEditorCategory.Main &&
            !selectedBuilding3D)
        {
          inspector.Add(StyledLabel("LOT CONTRACT", "inspector-title"));
          inspector.Add(Property("TYPE", LotTypeLabel(_lotWorld.LotType).ToUpperInvariant()));
          inspector.Add(Property("GRID",
              $"{_lotWorld.LotWidthCells} × {_lotWorld.LotDepthCells} MAJOR CELLS"));
          inspector.Add(Property("REAL SIZE",
              $"{_lotWorld.LotWidthMeters} × {_lotWorld.LotDepthMeters} M"));
          inspector.Add(Property("AREA",
              $"{_lotWorld.LotWidthMeters * _lotWorld.LotDepthMeters:N0} M²"));
          AddLotRotationControls(inspector);
        }
      }
      if (!string.IsNullOrWhiteSpace(_lotStatus))
        inspector.Add(StyledLabel(_lotStatus, "status-note"));
      if (_lotInspectorVisible)
        screen.Add(inspector);
      else
      {
        var showInspector = CfButton.Create("SHOW PANEL", () =>
        {
          _lotInspectorVisible = true;
          RefreshLotEditor();
        }, true, "quiet");
        showInspector.name = "show-lot-inspector";
        showInspector.AddToClassList("inspector-reopen");
        screen.Add(showInspector);
      }

      var hintText = _lotWorld.ToolMode switch
      {
        LotToolMode.Place => "CLICK THE LOT TO PLACE THE BUILDING",
        LotToolMode.Move => "CLICK THE LOT TO MOVE THE SELECTED BUILDING",
        _ when _lotEditorCategory == LotEditorCategory.Buildings =>
            "CLICK A BUILDING TO SELECT • HOLD AND DRAG TO MOVE",
        _ => "SELECT, MOVE, ROTATE, SAVE, AND RELOAD THE LOT"
      };
      var hint = StyledLabel(hintText, "viewport-hint");
      screen.Add(hint);
      _root.Add(screen);
      screen.schedule.Execute(screen.Focus);
      // Camera framing belongs to the player once the lot is open.
      // Rebuilding editor chrome must never recenter or refit the lot.
    }

    private void SelectCharacterScript(string scriptId)
    {
      if (_lotWorld.SetSelectedCharacterBehaviorScript(scriptId))
        _lotStatus = $"Character script: {CharacterBehaviorScript.DisplayName(scriptId)}";
      Show(AppScreen.LotEditor);
    }

    private void ShowLotContextMenu(VisualElement screen,
        Vector2 panelPosition, Vector2Int cell)
    {
      RemoveLotContextMenu();
      _lotContextMenu = new VisualElement { name = "lot-context-menu" };
      _lotContextCell = cell;
      _hoveredLotStripDeleteAction = 0;
      _lotContextMenu.AddToClassList("lot-context-menu");
      _lotContextMenu.style.position = Position.Absolute;
      _lotContextMenu.style.left = panelPosition.x;
      _lotContextMenu.style.top = panelPosition.y;
      var deleteRow = CfButton.Create("↔  DELETE ROW",
          () => DeleteLotStrip(cell, false),
          _lotWorld.LotWidthCells > 1);
      deleteRow.AddToClassList("lot-context-delete");
      deleteRow.RegisterCallback<PointerEnterEvent>(_ =>
      {
        _hoveredLotStripDeleteAction = 1;
        _lotWorld.ShowMajorStripDeletionPreview(cell.x, true);
      });
      deleteRow.RegisterCallback<PointerLeaveEvent>(_ =>
      {
        if (_hoveredLotStripDeleteAction == 1)
          _hoveredLotStripDeleteAction = 0;
        _lotWorld.ClearMajorStripDeletionPreview();
      });
      _lotContextMenu.Add(deleteRow);
      var deleteColumn = CfButton.Create("↕  DELETE COLUMN",
          () => DeleteLotStrip(cell, true),
          _lotWorld.LotDepthCells > 1);
      deleteColumn.AddToClassList("lot-context-delete");
      deleteColumn.RegisterCallback<PointerEnterEvent>(_ =>
      {
        _hoveredLotStripDeleteAction = 2;
        _lotWorld.ShowMajorStripDeletionPreview(cell.y, false);
      });
      deleteColumn.RegisterCallback<PointerLeaveEvent>(_ =>
      {
        if (_hoveredLotStripDeleteAction == 2)
          _hoveredLotStripDeleteAction = 0;
        _lotWorld.ClearMajorStripDeletionPreview();
      });
      _lotContextMenu.Add(deleteColumn);
      screen.Add(_lotContextMenu);
      var contextMenu = _lotContextMenu;
      screen.RegisterCallback<PointerDownEvent>(evt =>
      {
        if (evt.button != 0 || _lotContextMenu != contextMenu) return;
        if (deleteRow.enabledSelf && deleteRow.worldBound.Contains(evt.position))
        {
          evt.StopImmediatePropagation();
          DeleteLotStrip(cell, false);
        }
        else if (deleteColumn.enabledSelf &&
                       deleteColumn.worldBound.Contains(evt.position))
        {
          evt.StopImmediatePropagation();
          DeleteLotStrip(cell, true);
        }
      }, TrickleDown.TrickleDown);
      deleteRow.schedule.Execute(deleteRow.Focus);
    }

    private void DeleteLotStrip(Vector2Int cell, bool column)
    {
      if (_lotContextMenu == null) return;
      RemoveLotContextMenu();
      var deleted = column
          ? _lotWorld.DeleteMajorRow(cell.y)
          : _lotWorld.DeleteMajorColumn(cell.x);
      if (deleted)
        _lotStatus = $"Deleted {(column ? "column" : "row")} " +
            $"{(column ? cell.y : cell.x) + 1} • lot is now " +
            $"{_lotWorld.LotWidthCells} × {_lotWorld.LotDepthCells}";
      Show(AppScreen.LotEditor);
    }

    private void RemoveLotContextMenu()
    {
      _hoveredLotStripDeleteAction = 0;
      _lotWorld?.ClearMajorStripDeletionPreview();
      _lotContextMenu?.RemoveFromHierarchy();
      _lotContextMenu = null;
    }

    private Button CategoryButton(
        LotEditorCategory category,
        string icon,
        string label)
    {
      var selected = _lotEditorCategoryExpanded && (_lotEditorCategory == category ||
          category == LotEditorCategory.Transport &&
          _lotEditorCategory is LotEditorCategory.Vehicles or LotEditorCategory.Railroad or LotEditorCategory.Boats);
      if (category == LotEditorCategory.Roads)
      {
        var road = CfImageButton.CreateWithTexture(label, CfImageButton.CreateRoadIcon(),
            () => SetLotEditorCategory(category), true,
            selected ? "tool-category-selected" : "tool-category");
        var roadCaption = new Label("ROADS") { pickingMode = PickingMode.Ignore };
        roadCaption.AddToClassList("tool-category-caption");
        road.Add(roadCaption);
        AttachToolCategoryHoverInfo(road, category, label);
        return road;
      }
      if (category == LotEditorCategory.Main)
      {
        var main = new Button(() => SetLotEditorCategory(category))
        {
          name = "Main",
          text = "⚙",
          tooltip = ToolCategoryTooltip(category, label)
        };
        main.AddToClassList("cf-image-button");
        main.AddToClassList(selected
            ? "cf-image-button--tool-category-selected"
            : "cf-image-button--tool-category");
        main.AddToClassList("tool-category-main");
        AttachToolCategoryHoverInfo(main, category, label);
        return main;
      }
      if (category == LotEditorCategory.Environment)
      {
        var sun = new Button(() => SetLotEditorCategory(category))
        {
          name = "Environment",
          text = "☀",
          tooltip = ToolCategoryTooltip(category, label)
        };
        sun.AddToClassList("cf-image-button");
        sun.AddToClassList(selected
            ? "cf-image-button--tool-category-selected"
            : "cf-image-button--tool-category");
        sun.AddToClassList("tool-category-sun");
        AttachToolCategoryHoverInfo(sun, category, label);
        return sun;
      }
      if (category == LotEditorCategory.Effects)
      {
        var effects = new Button(() => SetLotEditorCategory(category))
        {
          name = "Effects",
          text = "✦",
          tooltip = ToolCategoryTooltip(category, label)
        };
        effects.AddToClassList("cf-image-button");
        effects.AddToClassList(selected
            ? "cf-image-button--tool-category-selected"
            : "cf-image-button--tool-category");
        effects.AddToClassList("tool-category-effects");
        var effectsCaption = new Label("EFFECTS")
        {
          pickingMode = PickingMode.Ignore
        };
        effectsCaption.AddToClassList("tool-category-caption");
        effects.Add(effectsCaption);
        AttachToolCategoryHoverInfo(effects, category, label);
        return effects;
      }
      if (category == LotEditorCategory.Water)
      {
        var water = new Button(() => SetLotEditorCategory(category))
        {
          name = "Water",
          text = "≋",
          tooltip = ToolCategoryTooltip(category, label)
        };
        water.AddToClassList("cf-image-button");
        water.AddToClassList(selected
            ? "cf-image-button--tool-category-selected"
            : "cf-image-button--tool-category");
        water.AddToClassList("tool-category-water");
        var waterCaption = new Label("WATER")
        {
          pickingMode = PickingMode.Ignore
        };
        waterCaption.AddToClassList("tool-category-caption");
        water.Add(waterCaption);
        AttachToolCategoryHoverInfo(water, category, label);
        return water;
      }
      if (category == LotEditorCategory.Garden)
      {
        var garden = new Button(() => SetLotEditorCategory(category))
        {
          name = "Garden",
          text = "✿",
          tooltip = ToolCategoryTooltip(category, label)
        };
        garden.AddToClassList("cf-image-button");
        garden.AddToClassList(selected
            ? "cf-image-button--tool-category-selected"
            : "cf-image-button--tool-category");
        var gardenCaption = new Label("GARDEN") { pickingMode = PickingMode.Ignore };
        gardenCaption.AddToClassList("tool-category-caption");
        garden.Add(gardenCaption);
        AttachToolCategoryHoverInfo(garden, category, label);
        return garden;
      }
      if (category == LotEditorCategory.Decals)
      {
        var decals = new Button(() => SetLotEditorCategory(category))
        {
          name = "Decals",
          text = "✎",
          tooltip = ToolCategoryTooltip(category, label)
        };
        decals.AddToClassList("cf-image-button");
        decals.AddToClassList(selected
            ? "cf-image-button--tool-category-selected"
            : "cf-image-button--tool-category");
        var decalsCaption = new Label("DECALS")
        {
          pickingMode = PickingMode.Ignore
        };
        decalsCaption.AddToClassList("tool-category-caption");
        decals.Add(decalsCaption);
        AttachToolCategoryHoverInfo(decals, category, label);
        return decals;
      }
      if (category == LotEditorCategory.Buildings ||
          category == LotEditorCategory.Buildings3D)
      {
        var house = CfImageButton.CreateWithTexture(
            label,
            CfImageButton.CreateHouseIcon(category == LotEditorCategory.Buildings3D
                ? new Color(0.35f, 0.82f, 0.88f, 1f)
                : new Color(0.88f, 0.72f, 0.34f, 1f)),
            () => SetLotEditorCategory(category),
            true,
            selected ? "tool-category-selected" : "tool-category");
        house.tooltip = ToolCategoryTooltip(category, label);
        var houseCaption = new Label(label.ToUpperInvariant())
        {
          pickingMode = PickingMode.Ignore
        };
        houseCaption.AddToClassList("tool-category-caption");
        house.Add(houseCaption);
        AttachToolCategoryHoverInfo(house, category, label);
        return house;
      }
      var button = CfImageButton.Create(
          label,
          $"CityForgeV3/UI/LotEditorTools/{icon}",
          () => SetLotEditorCategory(category),
          true,
          selected ? "tool-category-selected" : "tool-category");
      button.tooltip = ToolCategoryTooltip(category, label);
      var caption = new Label(label.ToUpperInvariant())
      {
        pickingMode = PickingMode.Ignore
      };
      caption.AddToClassList("tool-category-caption");
      button.Add(caption);
      AttachToolCategoryHoverInfo(button, category, label);
      return button;
    }

    private void AttachToolCategoryHoverInfo(Button button,
        LotEditorCategory category, string label)
    {
      var description = ToolCategoryTooltip(category, label);
      button.tooltip = description;
      button.RegisterCallback<PointerEnterEvent>(_ =>
          ShowToolCategoryHoverInfo(button, label, description));
      button.RegisterCallback<PointerLeaveEvent>(_ =>
          HideToolCategoryHoverInfo());
      button.RegisterCallback<DetachFromPanelEvent>(_ =>
          HideToolCategoryHoverInfo());
    }

    private void ShowToolCategoryHoverInfo(VisualElement anchor,
        string label, string description)
    {
      HideToolCategoryHoverInfo();
      if (_root == null) return;
      var info = new VisualElement
      {
        name = "tool-category-hover-info",
        pickingMode = PickingMode.Ignore
      };
      info.style.position = Position.Absolute;
      info.style.left = 72f;
      info.style.top = Mathf.Clamp(anchor.worldBound.yMin -
          _root.worldBound.yMin, 82f,
          Mathf.Max(82f, _root.resolvedStyle.height - 96f));
      info.style.width = 440f;
      info.style.paddingLeft = 12f;
      info.style.paddingRight = 12f;
      info.style.paddingTop = 9f;
      info.style.paddingBottom = 9f;
      info.style.backgroundColor = new Color(0.025f, 0.055f, 0.07f, 0.97f);
      info.style.borderLeftWidth = 1f;
      info.style.borderRightWidth = 1f;
      info.style.borderTopWidth = 1f;
      info.style.borderBottomWidth = 1f;
      var border = new Color(0.72f, 0.58f, 0.25f, 0.95f);
      info.style.borderLeftColor = border;
      info.style.borderRightColor = border;
      info.style.borderTopColor = border;
      info.style.borderBottomColor = border;
      var title = StyledLabel(label.ToUpperInvariant(), "section-label");
      title.pickingMode = PickingMode.Ignore;
      title.style.fontSize = 18f;
      info.Add(title);
      var details = StyledLabel(description, "catalog-meta");
      details.pickingMode = PickingMode.Ignore;
      details.style.whiteSpace = WhiteSpace.Normal;
      details.style.fontSize = 18f;
      info.Add(details);
      _root.Add(info);
      info.BringToFront();
      _toolCategoryHoverInfo = info;
    }

    private void HideToolCategoryHoverInfo()
    {
      var info = _toolCategoryHoverInfo;
      _toolCategoryHoverInfo = null;
      var parent = info?.parent;
      if (parent != null && parent.IndexOf(info) >= 0)
        parent.Remove(info);
    }

    private static string ToolCategoryTooltip(
        LotEditorCategory category, string label) => category switch
        {
          LotEditorCategory.Main => "Main — lot name, dimensions, and type",
          LotEditorCategory.Buildings3D => "Buildings — place real-time 3D models",
          LotEditorCategory.Roads => "Roads — build and edit streets",
          LotEditorCategory.Railroad => "Railroads — rail lines and streetcars",
          LotEditorCategory.Transport => "Transport — vehicles, railroads, and boats",
          LotEditorCategory.Vehicles => "Vehicles — cars and trucks",
          LotEditorCategory.Boats => "Boats — river and water transport",
          LotEditorCategory.Paths => "Paths — author pedestrian walkways and stairs",
          LotEditorCategory.Water => "Water — create ponds, lakes, rivers, and swamps",
          LotEditorCategory.Terrain => "Terrain — raise and lower the land",
          LotEditorCategory.Flora => "Flora — place trees, shrubs, and planting",
          LotEditorCategory.Garden => "Garden — hedges, flower beds, and planted arrangements",
          LotEditorCategory.Props => "Props — place lot and 3D building props",
          LotEditorCategory.Characters => "3D Characters — place people and characters",
          LotEditorCategory.Entertainment => "Entertainment — place exhibits and attractions",
          LotEditorCategory.Effects => "Effects — add atmospheric and lighting effects",
          LotEditorCategory.BaseTextures => "Base — paint base terrain textures",
          LotEditorCategory.OverlayTextures => "Overlays — paint surface overlays",
          LotEditorCategory.Decals => "Decals — paint randomized surface details",
          LotEditorCategory.Environment => "Environment — adjust time, season, and lighting",
          LotEditorCategory.View => "View — adjust the lot camera and diagnostics",
          _ => $"{label} tools"
        };

    private void SetLotEditorCategory(LotEditorCategory category)
    {
      // The image-driven catalog is retained for loading existing lots, but
      // every new building choice opens the real-time 3D library.
      if (category == LotEditorCategory.Buildings)
        category = LotEditorCategory.Buildings3D;
      if (category != LotEditorCategory.Terrain)
        _terrainSculptMode = TerrainSculptMode.None;
      if (category != LotEditorCategory.Water &&
          _lotWorld.WaterPlacementActive)
        _lotWorld.CancelWaterPlacement();
      if (category != LotEditorCategory.Effects)
      {
        _placementEffectId = "";
        _lotWorld.SetEffectPlacementPreview("");
      }
      if (category != LotEditorCategory.Decals)
      {
        _placementDecalCategory = "";
        _lotWorld.SetDecalPlacementPreview(false);
      }
      if ((category == LotEditorCategory.Garden &&
           !LotWorldController.IsGardenPropId(_placementPropId)) ||
          (category != LotEditorCategory.Garden &&
           LotWorldController.IsGardenPropId(_placementPropId)))
      {
        _placementPropId = "";
        _lotWorld.SetPropPlacementPreview("");
      }
      if (_building3DPlacementPending)
      {
        _lotWorld.EndBuilding3DDrag();
        _building3DPlacementPending = false;
      }
      var nextExpanded = CategoryExpandedAfterClick(
          _lotEditorCategory, _lotEditorCategoryExpanded, category);
      if (!nextExpanded)
      {
        if (category == LotEditorCategory.Effects)
        {
          _placementEffectId = "";
          _lotWorld.SetEffectPlacementPreview("");
        }
        if (category == LotEditorCategory.Decals)
        {
          _placementDecalCategory = "";
          _lotWorld.SetDecalPlacementPreview(false);
        }
        _lotEditorCategoryExpanded = false;
        _lotStatus = $"{category} tools collapsed";
        Show(AppScreen.LotEditor);
        return;
      }
      _lotEditorCategory = category;
      _lotEditorCategoryExpanded = true;
      if (category is LotEditorCategory.Vehicles or LotEditorCategory.Railroad or LotEditorCategory.Boats)
        _lotInspectorVisible = true;
      _lotStatus = $"{category} tools opened";
      if (category == LotEditorCategory.Buildings3D &&
          _lotWorld.LotType == LotType.Civics)
        _buildingUseCategory = BuildingUseCategory.Civics;
      if (category == LotEditorCategory.Buildings3D)
        _buildingSubcategory = string.Empty;
      if (category == LotEditorCategory.Buildings ||
          category == LotEditorCategory.Buildings3D)
      {
        // The primitive and hybrid modes are diagnostics. Entering
        // the authoring workspace must always show the actual art,
        // including for buildings restored from a saved lot.
        _lotWorld.SetInspectionMode(BuildingInspectionMode.Artwork);
      }
      Show(AppScreen.LotEditor);
      if (category == LotEditorCategory.Flora)
        OpenFloraModal();
      else if (category == LotEditorCategory.Garden)
        OpenGardenModal();
      else if (category == LotEditorCategory.Boats)
        OpenBoatsModal();
      else if (category == LotEditorCategory.Props)
        OpenPropsModal();
      else if (category == LotEditorCategory.Characters)
        OpenCharactersModal();
      else if (category == LotEditorCategory.Entertainment)
        OpenEntertainmentModal();
      else if (category == LotEditorCategory.Effects)
        OpenEffectsModal();
      else if (category == LotEditorCategory.Water)
        OpenWaterModal();
      else if (category == LotEditorCategory.Decals)
        OpenDecalsModal();
      else if (category == LotEditorCategory.BaseTextures)
        OpenBaseTextureModal();
      else if (category == LotEditorCategory.OverlayTextures)
        OpenOverlayTextureModal();
    }

    private void ComposeTerrainPanel(VisualElement screen)
    {
      var terrain = new VisualElement { name = "terrain-category-panel" };
      terrain.AddToClassList("context-panel");
      terrain.Add(StyledLabel("TERRAIN", "section-label"));
      terrain.Add(StyledLabel("RAISE & LOWER LAND", "catalog-title"));
      terrain.Add(StyledLabel(
          "Choose an instrument, then paint directly on the lot.",
          "catalog-meta"));

      var instruments = new VisualElement();
      instruments.AddToClassList("inspector-actions");
      instruments.Add(CfButton.Create("▲ RAISE",
          () => SetTerrainSculptMode(TerrainSculptMode.Raise), true,
          _terrainSculptMode == TerrainSculptMode.Raise
              ? "mode-selected" : "quiet"));
      instruments.Add(CfButton.Create("▼ LOWER",
          () => SetTerrainSculptMode(TerrainSculptMode.Lower), true,
          _terrainSculptMode == TerrainSculptMode.Lower
              ? "mode-selected" : "quiet"));
      terrain.Add(instruments);

      terrain.Add(StyledLabel(
          $"BRUSH SIZE  {_terrainBrushRadiusMeters:0.0} M", "source-label"));
      var radius = new Slider(1f, 12f)
      {
        name = "terrain-brush-radius",
        value = _terrainBrushRadiusMeters,
        tooltip = "Terrain brush radius in metres"
      };
      radius.RegisterValueChangedCallback(evt =>
      {
        _terrainBrushRadiusMeters = evt.newValue;
        RefreshLotEditor();
      });
      terrain.Add(radius);

      terrain.Add(StyledLabel(
          $"STRENGTH  {_terrainBrushStrengthMeters:0.00} M", "source-label"));
      var strength = new Slider(0.075f, MaximumTerrainBrushStrengthMeters)
      {
        name = "terrain-brush-strength",
        value = _terrainBrushStrengthMeters,
        tooltip = "Elevation change applied by each terrain stroke"
      };
      strength.RegisterValueChangedCallback(evt =>
      {
        _terrainBrushStrengthMeters = evt.newValue;
        RefreshLotEditor();
      });
      terrain.Add(strength);

      terrain.Add(CfButton.Create("CANCEL BRUSH",
          () => SetTerrainSculptMode(TerrainSculptMode.None),
          _terrainSculptMode != TerrainSculptMode.None, "quiet"));
      terrain.Add(CfButton.Create("↶ UNDO TERRAIN", () =>
      {
        _lotStatus = _lotWorld.UndoTerrainSculpt()
                  ? "Terrain stroke undone" : "Nothing to undo";
        Show(AppScreen.LotEditor);
      }, _lotWorld.CanUndoTerrainSculpt, "quiet"));
      terrain.Add(StyledLabel(
          _terrainSculptMode == TerrainSculptMode.None
              ? "NO TERRAIN INSTRUMENT SELECTED"
              : $"{_terrainSculptMode.ToString().ToUpperInvariant()} BRUSH ACTIVE",
          "catalog-meta"));
      screen.Add(terrain);
    }

    private void SetTerrainSculptMode(TerrainSculptMode mode)
    {
      _terrainSculptMode = mode;
      if (mode == TerrainSculptMode.None)
        _lotWorld?.HideTerrainBrush();
      _lotStatus = mode == TerrainSculptMode.None
          ? "Terrain brush cancelled"
          : $"{mode} terrain brush armed • drag over the lot to sculpt";
      Show(AppScreen.LotEditor);
    }

    private void OpenDecalsModal()
    {
      var panel = CreateDocumentModal(
          "DECAL LIBRARY",
          "Choose a surface category. Each click chooses a texture and quarter-turn at random; Ctrl+Z removes the most recent decal.");
      panel.AddToClassList("road-material-modal-panel");
      panel.Add(StyledLabel("SURFACE CATEGORIES", "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");

      var grass = new VisualElement();
      grass.AddToClassList("road-material-card");
      var grassSwatch = new VisualElement();
      grassSwatch.AddToClassList("road-material-swatch");
      var leaves = Resources.Load<Texture2D>("CityForgeV3/Decals/Grass/leaves-01");
      if (leaves != null)
        grassSwatch.style.backgroundImage = new StyleBackground(leaves);
      grass.Add(grassSwatch);
      grass.Add(CfButton.Create("GRASS • 3 VARIANTS", () =>
      {
        _placementDecalCategory = "grass";
        _lotWorld.SetDecalPlacementPreview(true);
        _lotStatus = "Grass decal paintbrush armed • click the lot • Ctrl+Z undoes";
        RemoveDocumentModal();
        Show(AppScreen.LotEditor);
      }, true, _placementDecalCategory == "grass"
          ? "mode-selected" : "quiet"));
      grid.Add(grass);

      var dirt = new VisualElement();
      dirt.AddToClassList("road-material-card");
      var dirtSwatch = new VisualElement();
      dirtSwatch.AddToClassList("road-material-swatch");
      var dirtTexture = Resources.Load<Texture2D>(
          "CityForgeV3/Decals/Dirt/dirt-01");
      if (dirtTexture != null)
        dirtSwatch.style.backgroundImage = new StyleBackground(dirtTexture);
      dirt.Add(dirtSwatch);
      dirt.Add(CfButton.Create("DIRT • 2 VARIANTS", () =>
      {
        _placementDecalCategory = "dirt";
        _lotWorld.SetDecalPlacementPreview(true);
        _lotStatus = "Dirt decal paintbrush armed • click the lot • Ctrl+Z undoes";
        RemoveDocumentModal();
        Show(AppScreen.LotEditor);
      }, true, _placementDecalCategory == "dirt"
          ? "mode-selected" : "quiet"));
      grid.Add(dirt);

      var street = new VisualElement();
      street.AddToClassList("road-material-card");
      var streetSwatch = new VisualElement();
      streetSwatch.AddToClassList("road-material-swatch");
      var brickStreet = Resources.Load<Texture2D>(
          "CityForgeV3/Decals/Street/brick-street-01");
      if (brickStreet != null)
        streetSwatch.style.backgroundImage = new StyleBackground(brickStreet);
      street.Add(streetSwatch);
      street.Add(CfButton.Create("STREET • 3 VARIANTS", () =>
      {
        _placementDecalCategory = "street";
        _lotWorld.SetDecalPlacementPreview(true);
        _lotStatus = "Street decal paintbrush armed • placement is restricted to roads";
        RemoveDocumentModal();
        Show(AppScreen.LotEditor);
      }, true, _placementDecalCategory == "street"
          ? "mode-selected" : "quiet"));
      grid.Add(street);
      panel.Add(grid);

      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("CANCEL", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

    private void OpenWaterModal()
    {
      var panel = CreateDocumentModal(
          "WATER LIBRARY",
          "Build natural and designed water features from four water families. Water placement and shoreline tools are the next implementation step.");
      panel.AddToClassList("road-material-modal-panel");
      panel.Add(StyledLabel("WATER FEATURES", "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      foreach (var family in new[]
               {
                         (Name: "PONDS", Detail: "SMALL, CONTAINED WATER FEATURES", Ready: false),
                         (Name: "LAKES", Detail: "LARGE STANDING-WATER SURFACES", Ready: false),
                         (Name: "RIVERS", Detail: "DIRECTED AND CURVING WATERWAYS", Ready: false),
                         (Name: "SWAMPS", Detail: "SHALLOW WETLAND WATER • SOURCE PACKAGE READY", Ready: true)
                     })
      {
        var card = new VisualElement();
        card.AddToClassList("road-material-card");
        card.Add(StyledLabel(family.Name, "road-material-name"));
        card.Add(StyledLabel(family.Detail, "road-material-meta"));
        card.Add(CfButton.Create(family.Ready
            ? "DRAW SWAMP WATER" : "COMING NEXT", () =>
            {
              if (!family.Ready) return;
              _lotWorld.BeginSwampWaterPlacement();
              _lotStatus = "Swamp drawing active • click boundary points • Enter or double-click to finish • Esc cancels";
              RemoveDocumentModal();
              ComposeLotEditor();
            }, family.Ready, family.Ready ? "mode-selected" : "quiet"));
        grid.Add(card);
      }
      panel.Add(grid);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal,
          true, "quiet"));
      panel.Add(actions);
    }

    private void OpenEffectsModal()
    {
      var panel = CreateDocumentModal(
          "EFFECTS LIBRARY",
          "Atmospheric, fire, vapor, and architectural-light effects will be placed from this library.");
      panel.AddToClassList("road-material-modal-panel");
      panel.Add(StyledLabel("VISUAL EFFECTS", "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      foreach (var effect in new[]
               {
                         (Icon: "🌫", Name: "SMOKE", Id: "",
                             Description: "CHIMNEYS • INDUSTRY • FIRES"),
                         (Icon: "🔥", Name: "FLAME", Id: "",
                             Description: "OPEN FIRE • TORCHES • DAMAGE"),
                         (Icon: "♨", Name: "STEAM", Id: "",
                             Description: "VENTS • TRAINS • MACHINERY"),
                         (Icon: "☁", Name: "CLOUDS",
                             Id: LotWorldController.CloudEffectId,
                             Description: "MOVING CUMULONIMBUS • CASTS SHADOWS"),
                         (Icon: "▣", Name: "WINDOW LIGHT",
                             Id: LotWorldController.WindowLightEffectId,
                             Description: "LIT WINDOWS • NIGHT SCENES")
                     })
      {
        var available = !string.IsNullOrWhiteSpace(effect.Id);
        var card = new VisualElement();
        card.AddToClassList("road-material-card");
        card.Add(StyledLabel(effect.Icon, "effects-card-icon"));
        card.Add(CfButton.Create(effect.Name, available ? () =>
        {
          _placementEffectId = effect.Id;
          _lotWorld.SetEffectPlacementPreview(_placementEffectId);
          _lotStatus = effect.Id == LotWorldController.CloudEffectId
                      ? "Cloud armed • click over the lot to place it"
                      : "Window Light armed • click a building or prop surface";
          RemoveDocumentModal();
          ComposeLotEditor();
        }
        : null, available,
            _placementEffectId == effect.Id && available
                ? "mode-selected" : "quiet"));
        card.Add(StyledLabel(effect.Description, "catalog-meta"));
        card.Add(StyledLabel(available
            ? "CLICK TO PLACE" : "COMING SOON", "catalog-meta"));
        grid.Add(card);
      }
      panel.Add(grid);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal,
          true, "quiet"));
      panel.Add(actions);
    }

    private void OpenBaseTextureModal()
    {
      var panel = CreateDocumentModal("BASE TEXTURES",
          "Choose a grass texture for the entire lot. The selection is applied immediately.");
      panel.AddToClassList("road-material-modal-panel");
      panel.Add(StyledLabel("GRASSES", "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      foreach (var option in LotWorldController.GrassBaseTextures)
      {
        var captured = option;
        var card = new VisualElement();
        card.AddToClassList("road-material-card");
        var preview = new VisualElement();
        preview.AddToClassList("road-material-swatch");
        preview.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>(option.ResourcePath));
        card.Add(preview);
        card.Add(CfButton.Create(option.DisplayName.ToUpperInvariant(), () =>
        {
          _lotWorld.SetBaseTexture(captured.Id);
          _lotStatus = $"{captured.DisplayName} applied to the entire lot";
          RemoveDocumentModal();
          ComposeLotEditor();
        }, true, _lotWorld.BaseTextureId == option.Id ? "mode-selected" : "quiet"));
        grid.Add(card);
      }
      panel.Add(grid);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DEFAULT GROUND", () =>
      {
        _lotWorld.SetBaseTexture("");
        RemoveDocumentModal();
        ComposeLotEditor();
      }, true, "quiet"));
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

    private void OpenOverlayTextureModal()
    {
      var panel = CreateDocumentModal("OVERLAY TEXTURES",
          "Choose an overlay, then click a 10 × 10 meter lot tile to place it above the base texture.");
      panel.AddToClassList("road-material-modal-panel");
      panel.Add(StyledLabel("SURFACE OVERLAYS", "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      foreach (var option in LotWorldController.OverlayTextures)
      {
        var captured = option;
        var card = new VisualElement();
        card.AddToClassList("road-material-card");
        var preview = new VisualElement();
        preview.AddToClassList("road-material-swatch");
        preview.style.backgroundImage = new StyleBackground(
            Resources.Load<Texture2D>(captured.ResourcePath));
        card.Add(preview);
        card.Add(CfButton.Create(captured.DisplayName.ToUpperInvariant(), () =>
        {
          _placementOverlayTextureId = captured.Id;
          _lotStatus = $"{captured.DisplayName} selected • click a lot tile to place";
          RemoveDocumentModal();
          ComposeLotEditor();
        }, true, _placementOverlayTextureId == captured.Id ? "mode-selected" : "quiet"));
        grid.Add(card);
      }
      panel.Add(grid);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

    private void OpenGardenModal()
    {
      var panel = CreateDocumentModal("GARDEN LIBRARY",
          "Choose a complete planted bed, then click the lot to place it.");
      panel.name = "garden-library-panel";
      panel.AddToClassList("road-material-modal-panel");
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      AddGardenBedCard(grid, "garden-card-georgian-square",
          "GEORGIAN MIXED SQUARE", "Square bed • 4 × 4 m • shrubs & flowers",
          LotWorldController.GeorgianGardenSquarePropId,
          "CityForgeV3/Garden/GeorgianBedsV01/square-summer");
      AddGardenBedCard(grid, "garden-card-georgian-rectangle",
          "GEORGIAN MIXED RECTANGLE",
          "Wide bed • 6 × 3 m • shrubs & flowers",
          LotWorldController.GeorgianGardenRectanglePropId,
          "CityForgeV3/Garden/GeorgianBedsV01/rectangle-summer");
      AddGardenBedCard(grid, "garden-card-hedge-square",
          "CLIPPED HEDGE SQUARE",
          "Formal hedge plot • 4 × 4 m",
          LotWorldController.GeorgianHedgeSquarePropId,
          "CityForgeV3/Garden/GeorgianClippedHedgesV01/clipped-leaves");
      AddGardenBedCard(grid, "garden-card-hedge-rectangle",
          "CLIPPED HEDGE RECTANGLE",
          "Formal hedge plot • 6 × 3 m",
          LotWorldController.GeorgianHedgeRectanglePropId,
          "CityForgeV3/Garden/GeorgianClippedHedgesV01/clipped-leaves");
      panel.Add(grid);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

    private void AddGardenBedCard(VisualElement panel, string name,
        string label, string description, string propId, string previewPath)
    {
      var card = new VisualElement { name = name };
      card.AddToClassList("road-material-card");
      var preview = new VisualElement();
      preview.AddToClassList("road-material-swatch");
      preview.style.backgroundImage = new StyleBackground(
          Resources.Load<Texture2D>(previewPath));
      card.Add(preview);
      card.Add(CfButton.Create(label, () =>
      {
        _placementPropId = propId;
        _lotWorld.SetPropPlacementPreview(_placementPropId);
        _lotStatus = label + " armed • click to place • select to rotate";
        RemoveDocumentModal();
        ComposeLotEditor();
      }, true, _placementPropId == propId
          ? "mode-selected" : "quiet"));
      card.Add(StyledLabel(description, "catalog-meta"));
      panel.Add(card);
    }

    private string _floraLibraryCategory = "Trees";
    private void OpenFloraModal()
    {
      var panel = CreateDocumentModal("FLORA LIBRARY",
          "Choose an item to plant. Select placed flora and press R to plant a row.");
      panel.AddToClassList("road-material-modal-panel");
      panel.AddToClassList("flora-modal-panel");
      var tabs = new VisualElement { name = "flora-category-tabs" };
      tabs.style.flexDirection = FlexDirection.Row;
      tabs.style.flexWrap = Wrap.Wrap;
      foreach (var category in new[] { "Trees", "Shrubs", "Stones", "Agriculture" })
      {
        var captured = category;
        var button = CfButton.Create(category.ToUpperInvariant(), () =>
        {
          _floraLibraryCategory = captured;
          RemoveDocumentModal();
          OpenFloraModal();
        }, true, category == _floraLibraryCategory ? "mode-selected" : "quiet");
        button.name = "flora-category-" + category.ToLowerInvariant();
        tabs.Add(button);
      }
      panel.Add(tabs);
      if (_floraLibraryCategory == "Trees") AddTreeFamilyTabs(panel, OpenFloraModal);
      var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "flora-library-scroll" };
      scroll.AddToClassList("flora-modal-scroll");
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      grid.style.flexShrink = 0f;
      foreach (var item in new[]
      {
                (Category: "Trees", Id: "mature-oak", Name: "Mature Oak"),
                (Category: "Trees", Id: "american-elm", Name: "American Elm"),
                (Category: "Trees", Id: "shagbark-hickory", Name: "Shagbark Hickory"),
                (Category: "Trees", Id: "bald-cypress-moss", Name: "Bald Cypress with Spanish Moss"),
                (Category: "Trees", Id: "bald-cypress-moss-b", Name: "Bald Cypress with Spanish Moss B"),
                (Category: "Trees", Id: "date-palm-tall", Name: "Tall Date Palm"),
                (Category: "Trees", Id: "date-palm-short", Name: "Short Date Palm"),
                (Category: "Trees", Id: "la-fan-palm-a", Name: "LA Fan Palm A"),
                (Category: "Trees", Id: "la-fan-palm-b", Name: "LA Fan Palm B"),
                (Category: "Trees", Id: "la-fan-palm-a-medium", Name: "LA Fan Palm A (Medium)"),
                (Category: "Trees", Id: "la-fan-palm-b-medium", Name: "LA Fan Palm B (Medium)"),
                (Category: "Trees", Id: "silver-maple-a", Name: "Silver Maple A"),
                (Category: "Trees", Id: "vendor-red-maple", Name: "Red Maple"),
                (Category: "Trees", Id: "cilician-fir", Name: "Cilician Fir"),
                (Category: "Trees", Id: "medium-balsam-fir", Name: "Medium Balsam Fir"),
                (Category: "Trees", Id: "medium-fraser-fir", Name: "Medium Fraser Fir"),
                (Category: "Trees", Id: "medium-blue-spruce", Name: "Medium Blue Spruce"),
                (Category: "Trees", Id: "london-plane-a", Name: "London Plane A"),
                (Category: "Trees", Id: "london-plane-b", Name: "London Plane B"),
                (Category: "Trees", Id: "vendor-willow", Name: "Willow"),
                (Category: "Shrubs", Id: "hart-tongue-fern", Name: "Hart's-tongue Fern"),
                (Category: "Shrubs", Id: "japanese-painted-fern", Name: "Japanese Painted Fern"),
                (Category: "Shrubs", Id: "male-fern", Name: "Male Fern"),
                (Category: "Shrubs", Id: "soft-shield-fern", Name: "Soft Shield Fern"),
                (Category: "Shrubs", Id: "small-hedge", Name: "Small Hedge"),
                (Category: "Shrubs", Id: "medium-hedge", Name: "Medium Hedge"),
                (Category: "Shrubs", Id: "long-hedge", Name: "Long Hedge"),
                (Category: "Agriculture", Id: "corn-field", Name: "Corn Field"),
            })
      {
        if (item.Category != _floraLibraryCategory) continue;
        if ((item.Category == "Trees" || item.Category == "3D") && FloraFamilies.ForTree(item.Id) != _floraTreeFamily) continue;
        var captured = item;
        var card = new VisualElement { name = "flora-card-" + item.Id };
        card.AddToClassList("road-material-card");
        var preview = new VisualElement();
        preview.AddToClassList("road-material-swatch");
        var imagePath = FloraTreeRepairs.BillboardPath(item.Id, SeasonPreset.Summer) ?? (PlaneUkFloraPresentation.IsTree(item.Id)
            ? PlaneUkFloraPresentation.BillboardPath(item.Id, SeasonPreset.Summer)
            : item.Id == "corn-field" ? "CityForgeV3/Flora/AgricultureV01/corn-canopy-summer"
            : $"CityForgeV3/Flora/LegacyTreesV01/{item.Id}-summer");
        preview.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>(imagePath));
        card.Add(preview);
        var select = CfButton.Create(item.Name.ToUpperInvariant(), () =>
        {
          _placementFloraId = captured.Id;
          _lotWorld.SetFloraPlacementPreview(captured.Id);
          _lotStatus = $"{captured.Name} armed • click to plant • select it and press R to repeat";
          RemoveDocumentModal();
          ComposeLotEditor();
        }, true, _placementFloraId == item.Id ? "mode-selected" : "quiet");
        select.name = "flora-select-" + item.Id;
        card.Add(select);
        grid.Add(card);
      }
      if (_floraLibraryCategory == "Stones")
        foreach (var stone in StoneFloraCatalog.Families)
          AddStoneLibraryCard(grid, stone.Id, stone.Name, () =>
          {
            _placementFloraId = stone.Id;
            _lotWorld.SetFloraPlacementPreview(stone.Id);
            _lotStatus = stone.Name + " armed • click to place";
            RemoveDocumentModal();
            ComposeLotEditor();
          });
      scroll.Add(grid);
      panel.Add(scroll);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

    private void AddPropLibraryCard(VisualElement grid, string title, string propId,
        string description, bool available = true)
    {
      var card = new VisualElement { name = "prop-card-" + propId };
      card.AddToClassList("road-material-card");
      card.AddToClassList("character-library-card");
      card.AddToClassList("prop-library-card");
      var select = CfButton.Create("", () =>
      {
        _placementBuildingPropId = "";
        _lotWorld.SetBuildingPropPlacementPreview("");
        _placementPropId = propId;
        _lotWorld.SetPropPlacementPreview(propId);
        _lotStatus = title + " selected • click the lot to place";
        RemoveDocumentModal();
        ComposeLotEditor();
      }, available, available && _placementPropId == propId ? "mode-selected" : "quiet");
      select.name = "prop-select-" + propId;
      select.tooltip = available ? "Place " + title : description;
      select.AddToClassList("character-library-select");
      var image = new Image
      {
        name = "prop-preview-" + propId,
        image = Resources.Load<Texture2D>("CityForgeV3/UI/PropThumbnails/" + propId),
        scaleMode = ScaleMode.ScaleToFit,
        pickingMode = PickingMode.Ignore
      };
      image.AddToClassList("character-library-image");
      select.Add(image);
      var label = StyledLabel(title, "character-library-name");
      label.pickingMode = PickingMode.Ignore;
      select.Add(label);
      card.Add(select);
      card.Add(StyledLabel(description, "catalog-meta"));
      grid.Add(card);
    }

    private void OpenPropsModal()
    {
      var panel = CreateDocumentModal("PROP LIBRARY",
          "Choose a thumbnail, then click the lot to place it. Drag a selected prop to move it.");
      panel.AddToClassList("character-library-panel");
      panel.AddToClassList("prop-library-panel");
      var links = DocumentModalActions();
      links.Add(CfButton.Create("BUILDING SIGNS & ATTACHMENTS", () =>
      {
        RemoveDocumentModal();
        OpenBuildingPropsModal();
      }, true, "quiet"));
      panel.Add(links);
      var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "prop-library-scroll" };
      scroll.AddToClassList("character-library-scroll");
      panel.Add(scroll);
      VisualElement Group(string title)
      {
        scroll.Add(StyledLabel(title, "road-material-role"));
        var grid = new VisualElement();
        grid.AddToClassList("road-material-grid");
        scroll.Add(grid);
        return grid;
      }
      var fences = Group("FENCES & GATES");
      AddPropLibraryCard(fences, "STRAIGHT IRON FENCE", "wrought-iron-fence-straight-v01", "Wrought iron • repeat to make a row");
      AddPropLibraryCard(fences, "CORNER IRON FENCE", "wrought-iron-fence-corner-v01", "Wrought-iron corner");
      AddPropLibraryCard(fences, "WHITE PICKET FENCE", LotWorldController.PicketFencePropId, "Painted wood • repeat to make a row");
      AddPropLibraryCard(fences, "OLD WOODEN FENCE", LotWorldController.OldWoodenFencePropId, "Split rail • repeat to make a row");
      AddPropLibraryCard(fences, "GARDEN FENCE & LAMP", LotWorldController.DecorativeIronGardenPropId, "Lamp lights at evening and night");
      AddPropLibraryCard(fences, "ORNATE CORNER FENCE", LotWorldController.OrnateIronCornerPropId, "Decorative iron corner");
      var lights = Group("STREET LIGHTING");
      AddPropLibraryCard(lights, "THREE-LANTERN LAMPPOST", "three-lantern-lamppost-v01", "Lights at evening and night");
      AddPropLibraryCard(lights, "SIMPLE STREET LAMP", LotWorldController.SimpleStreetLamppostPropId, "Lights at evening and night");
      var furniture = Group("STREET FURNITURE");
      AddPropLibraryCard(furniture, "ORNATE BENCH", LotWorldController.OrnateBenchPropId, "Period garden and street seating");
      var agriculture = Group("AGRICULTURE");
      AddPropLibraryCard(agriculture, "NEW ENGLAND BARN", LotWorldController.NewEnglandBarnPropId, "Traditional timber barn");
      var fortress = Group("FORTRESS");
      AddPropLibraryCard(fortress, "WOODEN PALISADE", LotWorldController.WoodenPalisadePropId, "Timber wall • repeat to make a row");
      AddPropLibraryCard(fortress, "MEDIEVAL WELL", LotWorldController.MedievalWellPropId, "Stone well");
      AddPropLibraryCard(fortress, "PALISADE GATE", LotWorldController.WoodenPalisadeGatePropId, "Timber entrance gate");
      AddPropLibraryCard(fortress, "MEDIEVAL TORCH", LotWorldController.MedievalTorchPropId, "Lights at evening and night");
      var flora = Group("FLORA PROPS");
      AddPropLibraryCard(flora, "3D HEDGE", LotWorldController.Hedge3DPropId, "Garden hedge");
      var seasonal = Group("SEASONAL PROPS");
      var available = PropSeasonCatalog.IsAvailable(LotWorldController.PumpkinJackOLanternPropId, _lotWorld.Season);
      AddPropLibraryCard(seasonal, "JACK-O'-LANTERN", LotWorldController.PumpkinJackOLanternPropId,
          available ? "Autumn • available now" : "Autumn only • change season to place", available);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

    private void OpenSeasonalPropsModal()
    {
      RemoveDocumentModal();
      var panel = CreateDocumentModal("SEASONAL PROPS",
          "Decorations remain saved year-round and appear during their assigned season.");
      panel.AddToClassList("character-library-panel");
      panel.AddToClassList("prop-library-panel");
      var scroll = new ScrollView(ScrollViewMode.Vertical);
      scroll.AddToClassList("character-library-scroll");
      panel.Add(scroll);
      scroll.Add(StyledLabel("AUTUMN", "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      scroll.Add(grid);
      var available = PropSeasonCatalog.IsAvailable(LotWorldController.PumpkinJackOLanternPropId, _lotWorld.Season);
      AddPropLibraryCard(grid, "JACK-O'-LANTERN", LotWorldController.PumpkinJackOLanternPropId,
          available ? "Autumn • available now" : "Autumn only • change season to place", available);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("BACK TO ALL PROPS", () => { RemoveDocumentModal(); OpenPropsModal(); }, true, "quiet"));
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

    private void AddCharacterLibraryCard(VisualElement grid, string title, string propId, string description)
    {
      var card = new VisualElement { name = "character-card-" + propId };
      card.AddToClassList("road-material-card");
      card.AddToClassList("character-library-card");
      var select = CfButton.Create("", () =>
      {
        _placementPropId = propId;
        _lotWorld.SetPropPlacementPreview(propId);
        _lotStatus = title + " selected • click the lot to place";
        RemoveDocumentModal();
        ComposeLotEditor();
      }, true, _placementPropId == propId ? "mode-selected" : "quiet");
      select.name = "character-select-" + propId;
      select.tooltip = "Place " + title;
      select.AddToClassList("character-library-select");
      var portrait = new Image
      {
        name = "character-preview-" + propId,
        image = Resources.Load<Texture2D>("CityForgeV3/UI/CharacterThumbnails/" + propId),
        scaleMode = ScaleMode.ScaleToFit,
        pickingMode = PickingMode.Ignore
      };
      portrait.AddToClassList("character-library-image");
      select.Add(portrait);
      select.Add(StyledLabel(title, "character-library-name"));
      card.Add(select);
      card.Add(StyledLabel(description, "catalog-meta"));
      grid.Add(card);
    }

    private void OpenCharactersModal()
    {
      var panel = CreateDocumentModal(
          "3D CHARACTER LIBRARY",
          "Choose a portrait to place it on the lot. Select an animal or rider, then click the ground to move. Horse teams also have Drive controls.");
      panel.AddToClassList("character-library-panel");
      var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "character-library-scroll" };
      scroll.AddToClassList("character-library-scroll");
      panel.Add(scroll);
      VisualElement Group(string title)
      {
        scroll.Add(StyledLabel(title, "road-material-role"));
        var grid = new VisualElement();
        grid.AddToClassList("road-material-grid");
        scroll.Add(grid);
        return grid;
      }
      var people = Group("PEOPLE");
      AddCharacterLibraryCard(people, "VICTORIAN GENTLEMAN", LotWorldController.VictorianGentlemanCharacterId,
          "IDLE • WALK • BOW • FOLD ARMS • LOOK AROUND • SIT • RUN UPSTAIRS");
      AddCharacterLibraryCard(people, "HOOLIGAN", LotWorldController.HooliganCharacterId,
          "MOSTLY IDLE • OCCASIONAL WALK");
      AddCharacterLibraryCard(people, "HISTORIC POLICEMAN", LotWorldController.HistoricPolicemanCharacterId,
          "IDLE • PATROL WALK • LOOK AROUND");
      AddCharacterLibraryCard(people, "MUSKETMAN", LotWorldController.MusketmanCharacterId,
          "PERIOD MILITIA • IDLE • WALK");
      AddCharacterLibraryCard(people, "FOUNDERS FARMER", LotWorldController.FarmerCharacterId,
          "WALK • HOE • IDLE • FARM WORKER");
      var animals = Group("ANIMALS & RIDERS");
      AddCharacterLibraryCard(animals, "KING KONG", LotWorldController.KingKongCharacterId,
          "26.6 FT GIANT • IDLE • WALK • TURN");
      AddCharacterLibraryCard(animals, "BEAR", LotWorldController.BearAnimalId,
          "ROAMS • AVOIDS MUSKETMEN AND FORT WATCHTOWERS");
      AddCharacterLibraryCard(animals, "HORSE", LotWorldController.HorseAnimalId,
          "CHESTNUT HORSE • IDLE • WALK • TROT");
      AddCharacterLibraryCard(animals, "MOUNTED CAVALRY", LotWorldController.CavalryPropId,
          "WHITE HORSE • RIDER • IDLE • WALK • TROT");
      AddCharacterLibraryCard(animals, "MOUNTED TRAPPER", LotWorldController.TrapperPropId,
          "HORSE • TRAPPER & PELTS • IDLE • WALK • TROT");
      var vehicles = Group("HORSE-DRAWN VEHICLES");
      AddCharacterLibraryCard(vehicles, "ANTIQUE CARRIAGE", LotWorldController.CarriagePropId,
          "DRIVER • ROLLING WHEELS");
      AddCharacterLibraryCard(vehicles, "HORSE & CARRIAGE", LotWorldController.HorseCarriagePropId,
          "ONE HORSE • DRIVE • SLOW / FAST");
      AddCharacterLibraryCard(vehicles, "HORSE & LUMBER WAGON", LotWorldController.HorseLumberWagonPropId,
          "WORK WAGON • DRIVER • LUMBER LOAD");
      AddCharacterLibraryCard(vehicles, "TWO HORSES & COVERED WAGON", LotWorldController.HorseCoveredWagonPropId,
          "TWO HORSES • CANVAS COVER");
      AddCharacterLibraryCard(vehicles, "HORSE & FOOD WAGON", LotWorldController.HorseFoodWagonPropId,
          "ONE HORSE • FRUIT & VEGETABLES • DRIVER");
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

    private void OpenEntertainmentModal()
    {
      var panel = CreateDocumentModal(
          "ENTERTAINMENT LIBRARY",
          "Choose an attraction or exhibit structure, then click inside the lot to place it.");
      panel.AddToClassList("road-material-modal-panel");
      panel.Add(StyledLabel("ANIMAL EXHIBITS & SPECTACLES",
          "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      var enclosureCard = new VisualElement();
      enclosureCard.AddToClassList("road-material-card");
      enclosureCard.Add(StyledLabel("🦍", "catalog-title"));
      enclosureCard.Add(CfButton.Create("KING KONG ENCLOSURE", () =>
      {
        _placementPropId =
                  LotWorldController.KingKongEnclosurePropId;
        _lotWorld.SetPropPlacementPreview(_placementPropId);
        _lotStatus =
                  "King Kong enclosure selected • click the lot to place";
        RemoveDocumentModal();
        ComposeLotEditor();
      }, true, _placementPropId ==
          LotWorldController.KingKongEnclosurePropId
              ? "mode-selected" : "quiet"));
      enclosureCard.Add(StyledLabel(
          "16 × 16 M GIANT-ANIMAL PEN • ROTATABLE 3D EXHIBIT",
          "catalog-meta"));
      grid.Add(enclosureCard);
      panel.Add(grid);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal,
          true, "quiet"));
      panel.Add(actions);
    }

    private void OpenBuildingPropsModal()
    {
      var panel = CreateDocumentModal(
          "BUILDING PROPS",
          "Choose an attachment, then move its translucent preview over a building facade and click to attach it. Attachments move and rotate with their host building.");
      panel.AddToClassList("building-prop-modal-panel");
      panel.Add(StyledLabel("SIGNS & STOREFRONTS", "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("building-prop-grid");
      foreach (var item in BuildingPropCatalog.Items)
      {
        var captured = item;
        var card = new VisualElement();
        card.AddToClassList("building-prop-card");
        var previewButton = new Button(() =>
        {
          ReleaseCameraPanForPlacement();
          _placementPropId = "";
          _lotWorld.SetPropPlacementPreview("");
          _placementBuildingPropId = captured.Id;
          _lotWorld.SetBuildingPropPlacementPreview(captured.Id);
          _lotStatus = $"{captured.DisplayName} selected • hover over a building facade and click to attach";
          RemoveDocumentModal();
          ComposeLotEditor();
        })
        {
          name = $"building-prop-{captured.Id}",
          tooltip = $"Attach {captured.DisplayName}"
        };
        previewButton.AddToClassList("building-prop-thumbnail");
        previewButton.style.backgroundImage = new StyleBackground(
            Resources.Load<Texture2D>(captured.PreviewResourcePath));
        card.Add(previewButton);
        card.Add(StyledLabel(captured.DisplayName.ToUpperInvariant(),
            "building-prop-name"));
        card.Add(StyledLabel(
            $"{captured.Revision.ToUpperInvariant()} • {captured.Family.ToUpperInvariant()} • BUILDING-OWNED",
            "catalog-meta"));
        if (!string.IsNullOrWhiteSpace(captured.DoorTransformName))
          card.Add(StyledLabel("SELECT + O TO OPEN/CLOSE",
              "catalog-meta"));
        grid.Add(card);
      }
      panel.Add(grid);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

#if UNITY_EDITOR
        public bool ArmTerrainRaiseForLiveQa()
        {
            if (_lotWorld == null || !_hasOpenLot) return false;
            ReleaseCameraPanForPlacement();
            _lotEditorCategory = LotEditorCategory.Terrain;
            _lotEditorCategoryExpanded = true;
            _terrainSculptMode = TerrainSculptMode.Raise;
            _lotStatus = "Live QA terrain Raise brush armed";
            RemoveDocumentModal();
            Show(AppScreen.LotEditor);
            return true;
        }

        // Keeps live QA on the real runtime input path while avoiding brittle
        // automation coordinates inside the dynamically composed catalog.
        public bool ArmBuildingPropForLiveQa(string componentId)
        {
            if (_lotWorld == null || !_hasOpenLot ||
                BuildingPropCatalog.Find(componentId) == null) return false;
            ReleaseCameraPanForPlacement();
            _lotEditorCategory = LotEditorCategory.Props;
            _lotEditorCategoryExpanded = true;
            _placementBuildingPropId = componentId;
            _lotWorld.SetBuildingPropPlacementPreview(componentId);
            _lotStatus = "Live QA storefront armed • drag onto a facade and release";
            RemoveDocumentModal();
            ComposeLotEditor();
            return true;
        }
#endif

    public static bool CategoryExpandedAfterClick(
        LotEditorCategory current,
        bool currentlyExpanded,
        LotEditorCategory clicked) =>
        current != clicked || !currentlyExpanded;

    private void RotateLot(int direction)
    {
      _lotWorld.Rotate(direction);
      Show(AppScreen.LotEditor);
    }

    private void AddLotRotationControls(VisualElement panel)
    {
      panel.Add(StyledLabel("LOT VIEW ANGLE", "inspector-note"));
      var actions = new VisualElement();
      actions.AddToClassList("inspector-actions");
      actions.Add(CfButton.Create("↺ ROTATE", () => RotateLot(-1), true));
      actions.Add(CfButton.Create("ROTATE ↻", () => RotateLot(1), true));
      panel.Add(actions);
      panel.Add(BuildLotOrbitDial());
    }

    private VisualElement BuildLotOrbitDial()
    {
      var dial = new VisualElement { name = "lot-orbit-dial" };
      dial.AddToClassList("lot-orbit-dial");
      var labels = new[] { "NE", "E", "SE", "S", "SW", "W", "NW", "N" };
      const float center = 90f;
      const float radius = 64f;
      const float segmentSize = 42f;
      for (var index = 0; index < 8; index++)
      {
        var captured = index;
        var radians = (-45f + index * 45f) * Mathf.Deg2Rad;
        var segment = CfButton.Create(labels[index], () =>
        {
          _lotWorld.SetCameraOrbitOctant(captured);
          _lotStatus = $"Lot view rotated to {_lotWorld.CameraAzimuthDegrees:0}°";
          Show(AppScreen.LotEditor);
        }, true, _lotWorld.CameraOrbitOctant == index
            ? "orbit-segment-selected"
            : "orbit-segment");
        segment.name = $"lot-orbit-{index}";
        segment.tooltip = $"View lot from {labels[index]} • " +
                          $"{Mathf.Repeat(45f + index * 45f, 360f):0}°";
        segment.style.position = Position.Absolute;
        segment.style.width = segmentSize;
        segment.style.height = segmentSize;
        segment.style.left = center + Mathf.Cos(radians) * radius -
                             segmentSize * 0.5f;
        segment.style.top = center + Mathf.Sin(radians) * radius -
                            segmentSize * 0.5f;
        dial.Add(segment);
      }

      var centerLabel = StyledLabel(
          $"VIEW\n{_lotWorld.CameraAzimuthDegrees:0}°", "lot-orbit-center");
      centerLabel.pickingMode = PickingMode.Ignore;
      dial.Add(centerLabel);
      return dial;
    }

    private void SetLotType(LotType lotType)
    {
      _lotWorld.SetLotType(lotType);
      _lotStatus = lotType == LotType.Neighborhood
          ? "Neighborhood lot • south road port connected • traffic active"
          : $"{lotType} lot selected";
      Show(AppScreen.LotEditor);
    }

    private void SetLotSize(int meters)
    {
      _lotWorld.SetLotSizeMeters(meters);
      _lotStatus = $"Lot resized to {meters} × {meters} meters";
      Show(AppScreen.LotEditor);
    }

    private void SelectRoadCellFromViewport(
        Vector2 panelPosition,
        Vector2 panelSize)
    {
      if (!_lotWorld.SelectRoadCellFromPanel(panelPosition, panelSize)) return;
      _lotStatus = RoadPlacementModel.FindAt(
              _lotWorld.Session.Data.RoadPieces,
              _lotWorld.RoadCursorCell.x,
              _lotWorld.RoadCursorCell.y) != null
          ? $"Road tile {_lotWorld.RoadCursorCell.x}, {_lotWorld.RoadCursorCell.y} selected"
          : $"Empty road cell {_lotWorld.RoadCursorCell.x}, {_lotWorld.RoadCursorCell.y} selected";
      Show(AppScreen.LotEditor);
    }

    private void SetCirculationMode(CirculationMode mode)
    {
      _lotWorld.SetCirculationMode(mode);
      _lotStatus = $"{mode} circulation network selected";
      Show(AppScreen.LotEditor);
    }

    private void SetPedestrianPathKind(PedestrianPathKind kind)
    {
      _lotWorld.SetPedestrianPathKind(kind);
      _lotStatus = kind == PedestrianPathKind.Stairs
          ? "Stairs selected • click the ground start, then click the doorway end"
          : "Flat pedestrian path selected • click stops to draw • Esc finishes";
      Show(AppScreen.LotEditor);
    }

    private void SelectRoadPiece(RoadPieceTopology topology)
    {
      _lotStatus = _lotWorld.PaintRoadPiece(topology)
          ? $"{topology} road placed in cell {_lotWorld.RoadCursorCell.x}, {_lotWorld.RoadCursorCell.y}"
          : $"{topology} road artwork is unavailable";
      Show(AppScreen.LotEditor);
    }

    private void SelectRoadPackage(string packageId)
    {
      _lotWorld.SelectRoadPackage(packageId);
      _lotStatus = $"{_lotWorld.RoadPackage.DisplayName} selected";
      ComposeLotEditor();
    }

    private void SelectStreetcarTrack(StreetcarTrackTopology topology)
    {
      _lotWorld.SelectStreetcarTrack(topology);
      _lotStatus = $"{topology} streetcar track selected";
      ComposeLotEditor();
    }

    private void NudgeRoad(int x, int z)
    {
      _lotWorld.NudgeRoadCursor(x, z);
      _lotStatus = "Road cursor moved one ten-meter cell";
      Show(AppScreen.LotEditor);
    }

    private void RotateRoadPiece()
    {
      _lotStatus = _lotWorld.RotateRoadPiece(1)
          ? $"Highlighted road tile rotated to {_lotWorld.RoadRotationQuarterTurns * 90}°"
          : $"Road preview rotated to {_lotWorld.RoadRotationQuarterTurns * 90}°";
      Show(AppScreen.LotEditor);
    }

    private void ApplyRoadSuggestion()
    {
      _lotStatus = _lotWorld.ApplyRoadSuggestion()
          ? $"Suggested {_lotWorld.SelectedRoadTopology} at {_lotWorld.RoadRotationQuarterTurns * 90}°"
          : "No connected neighbors suggest a road shape here";
      Show(AppScreen.LotEditor);
    }

    private void PlaceRoadPiece()
    {
      _lotStatus = _lotWorld.PlaceRoadPiece()
          ? $"Road placed • {_lotWorld.RoadValidationIssues.Count} connection issues"
          : "This road artwork is not available yet";
      Show(AppScreen.LotEditor);
    }

    public static bool ShouldPrioritizeToolPlacement(
        LotEditorCategory category, string floraId, string propId) =>
        category == LotEditorCategory.Roads ||
        category == LotEditorCategory.Railroad ||
        category == LotEditorCategory.OverlayTextures ||
        ((category is LotEditorCategory.Garden or LotEditorCategory.Props or LotEditorCategory.Characters or
              LotEditorCategory.Entertainment or LotEditorCategory.Boats) &&
            !string.IsNullOrWhiteSpace(propId));

    private void DeleteRoadPiece()
    {
      _lotStatus = _lotWorld.DeleteRoadPiece()
          ? "Road piece deleted; vehicle graph rebuilt"
          : "No road piece occupies this cell";
      Show(AppScreen.LotEditor);
    }

    private void SetOutsideConnector(RoadTrafficFlow flow)
    {
      _lotStatus = _lotWorld.SetSelectedOutsideConnector(flow)
          ? $"Outside connector set to {flow}"
          : "Select a road tile whose open lane reaches the lot boundary";
      Show(AppScreen.LotEditor);
    }

    private void RemoveOutsideConnector()
    {
      _lotStatus = _lotWorld.RemoveSelectedOutsideConnector()
          ? "Outside traffic connector removed"
          : "No outside connector selected";
      Show(AppScreen.LotEditor);
    }

    private void UndoRoadEdit()
    {
      _lotStatus = _lotWorld.UndoRoadEdit() ? "Road edit undone" : "Nothing to undo";
      Show(AppScreen.LotEditor);
    }

    private void RedoRoadEdit()
    {
      _lotStatus = _lotWorld.RedoRoadEdit() ? "Road edit restored" : "Nothing to redo";
      Show(AppScreen.LotEditor);
    }

    private void NudgeCirculation(int x, int z)
    {
      _lotWorld.NudgeCirculationCursor(x, z);
      _lotStatus = "Circulation cursor moved one meter";
      Show(AppScreen.LotEditor);
    }

    private void AddCirculationNode()
    {
      _lotWorld.AddCirculationNode();
      _lotStatus = "Node added and connected to the active route";
      Show(AppScreen.LotEditor);
    }

    private void DeleteCirculationNode()
    {
      _lotWorld.DeleteLastCirculationNode();
      _lotStatus = "Last active-network node removed";
      Show(AppScreen.LotEditor);
    }

    private void ToggleCirculationDiagnostics()
    {
      _lotWorld.ToggleCirculationDiagnostics();
      _lotStatus = _lotWorld.CirculationDiagnosticsVisible
          ? "Circulation graph inspection enabled"
          : "Road artwork view — graph diagnostics hidden";
      Show(AppScreen.LotEditor);
    }

    private void StepZoom(int direction)
    {
      _lotWorld.SetZoomLevel(LotWorldController.NextZoomLevel(
          _lotWorld.ZoomLevel, direction));
      _lotStatus = $"{_lotWorld.ZoomLevel} zoom • approximately " +
          $"{LotWorldController.ApproximateZoomAltitudeFeet(_lotWorld.ZoomLevel):N0} ft";
      Show(AppScreen.LotEditor);
    }

    private void SetInspectionMode(BuildingInspectionMode mode)
    {
      _lotWorld.SetInspectionMode(mode);
      _lotStatus = mode switch
      {
        BuildingInspectionMode.Artwork =>
            "Artwork view — directional render",
        BuildingInspectionMode.Hybrid =>
            "Overlay view — 20% artwork and spatial primitive",
        _ =>
            "Primitive view — foundation, collision, and entrance anchor"
      };
      Show(AppScreen.LotEditor);
    }

    private void SetLotTool(LotToolMode mode)
    {
      _lotWorld.SetTool(mode);
      _lotStatus = mode switch
      {
        LotToolMode.Place => "Placement tool active",
        LotToolMode.Move => "Move tool active",
        _ => "Selection tool active"
      };
      Show(AppScreen.LotEditor);
    }

    private void SetTimeOfDay(TimeOfDayPreset preset)
    {
      _lotWorld.SetTimeOfDay(preset);
      var label = TimeOfDayLighting.For(preset).Label;
      _lotStatus = _lotWorld.NeutralPilotShowing
          ? $"{label} neutral-pilot preview — runtime time tint active"
          : _lotWorld.NeutralPilotFallback
              ? $"{label} preview — approved artwork fallback is active"
              : $"{label} environment preview — approved artwork retains baked light";
      Show(AppScreen.LotEditor);
    }

    private void SetCameraPanTool(bool active)
    {
      _cameraPanToolActive = active && _hasOpenLot;
      _cameraPanPointerDown = false;
      _lotWorld?.SetCameraPanInteraction(_cameraPanToolActive);
      UnityEngine.Cursor.SetCursor(
          _cameraPanToolActive ? CameraPanCursorTexture() : null,
          _cameraPanToolActive ? new Vector2(8f, 7f) : Vector2.zero,
          CursorMode.Auto);
      _lotStatus = _cameraPanToolActive
          ? "Camera hand active • drag anywhere on the lot to pan"
          : "Camera hand released";
      Show(AppScreen.LotEditor);
    }

    private void ReleaseCameraPanForPlacement()
    {
      if (!_cameraPanToolActive) return;
      _cameraPanToolActive = false;
      _cameraPanPointerDown = false;
      _lotWorld?.SetCameraPanInteraction(false);
      UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private static Texture2D CameraPanCursorTexture()
    {
      if (_cameraPanCursorTexture != null) return _cameraPanCursorTexture;

      const int size = 24;
      var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
      {
        name = "City Forge Camera Pan Hand Cursor",
        filterMode = FilterMode.Point,
        wrapMode = TextureWrapMode.Clamp,
        hideFlags = HideFlags.HideAndDontSave
      };
      var pixels = new Color32[size * size];
      var outline = new Color32(35, 31, 24, 255);
      var fill = new Color32(239, 223, 174, 255);
      var rows = new[]
      {
                "       XX               ",
                "      XFFX              ",
                "      XFFX  XX          ",
                "      XFFX XFFX XX      ",
                "   XX XFFXXFFXXFFX      ",
                "  XFFXXFFFFFFFFFFX      ",
                "  XFFFFFFFFFFFFFFX      ",
                "   XFFFFFFFFFFFFFX      ",
                "    XFFFFFFFFFFFFX      ",
                "     XFFFFFFFFFFX       ",
                "      XFFFFFFFFX        ",
                "      XFFFFFFFFX        ",
                "       XXXXXXXX         ",
                "                        ",
                "                        ",
                "                        ",
                "                        ",
                "                        ",
                "                        ",
                "                        ",
                "                        ",
                "                        ",
                "                        ",
                "                        "
            };
      for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
          var mark = rows[y][x];
          pixels[(size - 1 - y) * size + x] = mark == 'X'
              ? outline
              : mark == 'F' ? fill : new Color32(0, 0, 0, 0);
        }
      texture.SetPixels32(pixels);
      texture.Apply(false, false);
      _cameraPanCursorTexture = texture;
      return texture;
    }

    private void SetSeason(SeasonPreset preset)
    {
      _lotWorld.SetSeason(preset);
      _lotStatus = $"{SeasonLighting.Label(preset)} seasonal preview";
      Show(AppScreen.LotEditor);
    }

    private void ToggleRain()
    {
      _lotWorld.SetRaining(!_lotWorld.IsRaining);
      _lotStatus = _lotWorld.IsRaining
          ? "Rain active • sunlight and shadows suppressed"
          : "Rain cleared • time-of-day lighting restored";
      Show(AppScreen.LotEditor);
    }

    private void StartWinterSnowfall()
    {
      if (_lotWorld.StartWinterSnowfall())
        _lotStatus = "Snowfall started • accumulation will build for 10 seconds";
      Show(AppScreen.LotEditor);
    }

    private void SetArtworkSource(BuildingArtworkSource source)
    {
      _lotWorld.SetArtworkSource(source);
      _lotStatus = source == BuildingArtworkSource.NeutralPilot
          ? "Game-lit neutral artwork selected — runtime time-of-day lighting active"
          : "Baked Blender lighting reference selected";
      Show(AppScreen.LotEditor);
    }

    private void ToggleRegistrationDiagnostics()
    {
      _lotWorld.ToggleRegistrationDiagnostics();
      _lotStatus = _lotWorld.RegistrationDiagnosticsVisible
          ? "Registration diagnostics — center/pivot, roof ridge, and entrance direction"
          : "Registration diagnostics hidden";
      Show(AppScreen.LotEditor);
    }

    private void ToggleTopDownView()
    {
      _lotWorld.ToggleTopDownView();
      _lotStatus = _lotWorld.TopDownViewEnabled
          ? "Top-down placement view — select and move objects normally"
          : "Top-down placement view closed — previous camera restored";
      Show(AppScreen.LotEditor);
    }

    private void Nudge(int x, int z)
    {
      _lotWorld.NudgeSelected(x, z);
      _lotStatus = "Building moved on the construction grid";
      Show(AppScreen.LotEditor);
    }

    private void PanLot(int horizontal, int vertical)
    {
      // Arrow controls move the visible lot in the indicated direction.
      // Camera translation is therefore the inverse of the arrow.
      _lotWorld.PanCameraViewport(-horizontal, -vertical);
      _lotStatus = "Lot view panned";
      Show(AppScreen.LotEditor);
    }

    private void MoveCategorySelectionOrPan(int horizontal, int vertical)
    {
      if (_cameraPanToolActive)
      {
        // With the hand selected, arrows always move the view. Keep
        // object selection from stealing the command, and use the
        // direction users expect: Up moves the camera/viewport up.
        PanLot(horizontal, vertical);
        return;
      }
      if (_lotWorld.ActiveObjectSelection == LotObjectSelectionKind.Prop &&
          _lotWorld.SelectedPropIndex >= 0)
      {
        if (_lotWorld.SelectedPropIsThreeDimensionalCharacter)
          _lotStatus = _lotWorld.WalkSelectedCharacter(horizontal, vertical)
              ? "Character walking • Space stops"
              : "Character cannot walk in that direction";
        else
          _lotStatus = _lotWorld.NudgeSelectedPropByScreenPixels(horizontal, vertical)
              ? "Prop nudged one pixel"
              : "Prop cannot move farther in that direction";
        return;
      }
      if (_lotWorld.ActiveObjectSelection == LotObjectSelectionKind.Building &&
          _lotWorld.IsSelected)
      {
        _lotStatus = _lotWorld.NudgeSelectedBuildingByScreenPixels(
                horizontal, vertical)
            ? "Building nudged one pixel"
            : "Building cannot move farther in that direction";
        _refreshBuildingFocusOverlay?.Invoke();
        return;
      }
      switch (_lotEditorCategory)
      {
        case LotEditorCategory.Buildings when _lotWorld.IsSelected:
          _lotStatus = _lotWorld.NudgeSelectedBuildingByScreenPixels(
                  horizontal, vertical)
              ? "Building nudged one pixel"
              : "Building cannot move farther in that direction";
          _refreshBuildingFocusOverlay?.Invoke();
          break;
        case LotEditorCategory.Roads when _lotWorld.RoadCursorSelected:
          NudgeRoad(horizontal, vertical);
          break;
        case LotEditorCategory.Railroad:
          _lotWorld.NudgeRoadCursor(horizontal, vertical);
          break;
        case LotEditorCategory.Paths when _lotWorld.CirculationCursorSelected:
          NudgeCirculation(horizontal, vertical);
          break;
        case LotEditorCategory.Garden or LotEditorCategory.Props or LotEditorCategory.Entertainment or LotEditorCategory.Boats
              when _lotWorld.SelectedPropIndex >= 0:
          _lotStatus = _lotWorld.NudgeSelectedPropByScreenPixels(
              horizontal, vertical)
              ? "Prop nudged one pixel"
              : "Prop cannot move farther in that direction";
          break;
        default:
          PanLot(horizontal, vertical);
          break;
      }
    }

    private void DeselectAll()
    {
      if (_buildingPlacementPending)
      {
        _lotWorld.EndBuildingDrag();
        _buildingPlacementPending = false;
      }
      if (_building3DPlacementPending)
      {
        _lotWorld.EndBuilding3DDrag();
        _building3DPlacementPending = false;
      }
      _placementFloraId = "";
      _placementPropId = "";
      _placementBuildingPropId = "";
      _placementOverlayTextureId = "";
      _lotWorld.EndCirculationPathDrawing();
      _lotWorld.SetBuildingPropPlacementPreview("");
      _lotWorld.DeselectAll();
      _lotStatus = "Selection cleared — arrows pan the lot";
      Show(AppScreen.LotEditor);
    }

    private bool DeleteActiveSelection()
    {
      // An attachment can be selected while its 3D host still has a
      // valid package index. The explicit active object must win or
      // Delete removes the entire host building.
      if (_lotWorld.ActiveObjectSelection ==
          LotObjectSelectionKind.BuildingProp)
      {
        _lotStatus = _lotWorld.DeleteSelectedBuildingProp()
            ? "Building prop deleted"
            : "Select a building prop before deleting";
        Show(AppScreen.LotEditor);
        return true;
      }
      // 3D packages intentionally use their own selection index rather
      // than the legacy billboard selection enum. Honor it before the
      // category switch so Delete/Backspace works for either system.
      if (_lotWorld.SelectedBuilding3DIndex >= 0)
      {
        _lotStatus = _lotWorld.DeleteSelectedBuilding3D()
            ? "3D building removed from the lot"
            : "No 3D building selected";
        _building3DPlacementPending = false;
        Show(AppScreen.LotEditor);
        return true;
      }
      switch (_lotWorld.ActiveObjectSelection)
      {
        case LotObjectSelectionKind.Prop:
          DeleteSelectedProp();
          return true;
        case LotObjectSelectionKind.Flora:
          _lotStatus = _lotWorld.DeleteSelectedFlora()
              ? "Tree deleted"
              : "Select a tree before deleting";
          Show(AppScreen.LotEditor);
          return true;
        case LotObjectSelectionKind.Building:
          DeleteBuilding();
          return true;
      }
      if (_lotEditorCategory == LotEditorCategory.Roads &&
          _lotWorld.RoadCursorSelected)
      {
        DeleteRoadPiece();
        return true;
      }
      if (_lotEditorCategory == LotEditorCategory.OverlayTextures &&
          _lotWorld.SelectedOverlayTextureIndex >= 0)
      {
        DeleteSelectedOverlayTexture();
        return true;
      }
      return false;
    }

    private void RotateSelectedProp(int direction)
    {
      _lotStatus = _lotWorld.RotateSelectedProp(direction)
          ? "Selected piece rotated 90°"
          : "Selected piece cannot rotate here";
      Show(AppScreen.LotEditor);
    }

    private void DeleteSelectedProp()
    {
      _lotStatus = _lotWorld.DeleteSelectedProp()
          ? "Selected piece removed"
          : "No piece selected";
      Show(AppScreen.LotEditor);
    }

    private void DeleteSelectedOverlayTexture()
    {
      _lotStatus = _lotWorld.DeleteSelectedOverlayTexture()
          ? "Overlay tile removed"
          : "No overlay tile selected";
      Show(AppScreen.LotEditor);
    }

    private static string FloraDisplayName(string id) => StoneFloraCatalog.IsStone(id) ? StoneFloraCatalog.DisplayName(id) : LotWorldController.CurrentTreeArtwork(id) switch
    {
      "plane-uk-3d-a" => "Plane UK A (3D)",
      "plane-uk-3d-b" => "Plane UK B (3D)",
      "maple" => "Maple Tree",
      "ashe" => "Ashe Tree",
      "oak" => "Oak Tree",
      "mature-oak" => "Mature Oak",
      "american-elm" => "American Elm",
      "shagbark-hickory" => "Shagbark Hickory",
      "medium-balsam-fir" => "Medium Balsam Fir",
      "medium-fraser-fir" => "Medium Fraser Fir",
      "medium-blue-spruce" => "Medium Blue Spruce",
      "bald-cypress-moss" => "Bald Cypress with Spanish Moss",
      "bald-cypress-moss-b" => "Bald Cypress with Spanish Moss B",
      "evergreen" => "Evergreen Pine",
      "date-palm" => "Date Palm",
      "date-palm-tall" => "Tall Date Palm",
      "date-palm-short" => "Short Date Palm",
      "la-fan-palm-a" => "LA Fan Palm A",
      "la-fan-palm-b" => "LA Fan Palm B",
      "la-fan-palm-a-medium" => "LA Fan Palm A (Medium)",
      "la-fan-palm-b-medium" => "LA Fan Palm B (Medium)",
      "narrow-street-tree" => "Street Tree",
      "street-tree-3d" => "StreetTree3D",
      "hart-tongue-fern" => "Hart's-tongue Fern",
      "japanese-painted-fern" => "Japanese Painted Fern",
      "male-fern" => "Male Fern",
      "soft-shield-fern" => "Soft Shield Fern",
      "eucalyptus-robusta-a" => "Eucalyptus Robusta A",
      "eucalyptus-robusta-b" => "Eucalyptus Robusta B",
      "silver-maple-a" => "Silver Maple A",
      "silver-maple-b" => "Silver Maple B",
      "angel-oak-spanish-moss" => "Angel Oak with Spanish Moss",
      "vendor-red-maple" => "Red Maple",
      "vendor-red-maple-young" => "Young Red Maple",
      "cilician-fir" => "Cilician Fir",
      "camphor-tree" => "Camphor Tree",
      "fraser-fir-large" => "Large Fraser Fir",
      "fraser-fir-small" => "Small Fraser Fir",
      "fraser-fir-snowy" => "Snowy Fraser Fir",
      "london-plane-a" => "London Plane A",
      "london-plane-b" => "London Plane B",
      "london-plane-c" => "London Plane B",
      "vendor-balsam-fir-classic" => "Classic Balsam Fir",
      "vendor-hickory" => "Hickory",
      "vendor-willow" => "Willow",
      "vendor-cypress-oak" => "Cypress Oak",
      "vendor-cypress-oak-wide" => "Wide Cypress Oak",
      "vendor-oregon-ash" => "Oregon Ash",
      "vendor-oregon-ash-wide" => "Wide Oregon Ash",
      "small-hedge" => "Small Hedge",
      "medium-hedge" => "Medium Hedge",
      "long-hedge" => "Long Hedge",
      "corn-field" => "Corn Field",
      _ => id ?? ""
    };

    private void RotateBuilding(int direction)
    {
      if (_lotWorld.RotateSelectedBuilding3D(direction))
      {
        _lotStatus = direction > 0
            ? "3D building rotated clockwise"
            : "3D building rotated counter-clockwise";
        Show(AppScreen.LotEditor);
        return;
      }
      _lotWorld.RotateSelected(direction);
      var orientation = _lotWorld.BuildingCardinalOrientation;
      _lotStatus = direction > 0
          ? $"Building rotated clockwise — facing {orientation}"
          : $"Building rotated counter-clockwise — facing {orientation}";
      if (_lotWorld.BuildingFocusFreezeActive)
        _refreshBuildingFocusOverlay?.Invoke();
      else
        Show(AppScreen.LotEditor);
    }

    private void BuildSelectedBuilding()
    {
      _lotStatus = _lotWorld.BuildSelectedBuilding3D()
          ? "Construction started • foundation and timber staging active"
          : "Select a completed 3D building to begin construction";
      Show(AppScreen.LotEditor);
    }

    private void ToggleSelectedBuildingFrame()
    {
      var wasVisible = _lotWorld.SelectedBuildingFrameVisible;
      _lotStatus = _lotWorld.ToggleSelectedBuildingConstructionFrame()
          ? wasVisible
              ? "Construction frame hidden"
              : "Full construction frame shown"
          : "Select a completed 3D building to preview its frame";
      Show(AppScreen.LotEditor);
    }

    private void DeleteBuilding()
    {
      _lotStatus = _lotWorld.DeleteSelectedBuilding()
          ? "Building removed from the lot"
          : "No building selected";
      Show(AppScreen.LotEditor);
    }

    private void SaveLot()
    {
      var path = _lotWorld.SaveLot();
      DistrictLotSimulation.SavedDefinitionChanged(FindSelectedRegionTile(), _lotWorld.Session.Data);
      _lotStatus = $"Saved {_lotWorld.CurrentLotName} • {System.IO.Path.GetFileName(path)}";
      Show(AppScreen.LotEditor);
    }

    private void RequestQuit()
    {
      RequestDocumentAction(QuitApplication);
    }

    private void RequestDocumentAction(Action action)
    {
      if (!WarnAboutUnsavedLotChanges || _lotWorld == null ||
          !_lotWorld.HasUnsavedChanges)
      {
        action?.Invoke();
        return;
      }

      _pendingDocumentAction = action;
      ComposeUnsavedChangesDialog();
    }

    private void ComposeUnsavedChangesDialog()
    {
      var panel = CreateDocumentModal(
          "UNSAVED LOT",
          $"{_lotWorld.CurrentLotName} has changes that have not been saved.");
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("SAVE & CONTINUE", () => ContinueDocumentAction(true), true, "primary"));
      actions.Add(CfButton.Create("DISCARD", () => ContinueDocumentAction(false), true, "secondary"));
      actions.Add(CfButton.Create("CANCEL", CancelDocumentAction, true, "quiet"));
      panel.Add(actions);
    }

    private void ContinueDocumentAction(bool save)
    {
      var action = _pendingDocumentAction;
      _pendingDocumentAction = null;
      if (save)
      {
        _lotWorld.SaveLot();
        DistrictLotSimulation.SavedDefinitionChanged(FindSelectedRegionTile(), _lotWorld.Session.Data);
      }
      RemoveDocumentModal();
      action?.Invoke();
    }

    private void CancelDocumentAction()
    {
      _pendingDocumentAction = null;
      RemoveDocumentModal();
    }

    private void ComposeNewLotDialog()
    {
      var panel = CreateDocumentModal(
          "NEW LOT",
          "Choose the lot contract and size. New lots begin empty unless a template is selected.");
      var nameField = new TextField("LOT NAME") { value = "Untitled Lot" };
      nameField.AddToClassList("document-field");
      panel.Add(nameField);

      var typeField = new CityForgeChoiceField(
          _root,
          "LOT TYPE",
          new List<string> { "Residential", "Commercial", "Industrial", "Mixed", "Transportation", "Civics" },
          0);
      panel.Add(typeField);

      var cellChoices = new List<string>();
      for (var cells = 1; cells <= 8; cells++) cellChoices.Add($"{cells} cell{(cells == 1 ? "" : "s")}");
      var widthField = new CityForgeChoiceField(_root, "WIDTH", cellChoices, 1);
      panel.Add(widthField);
      var depthField = new CityForgeChoiceField(_root, "LENGTH", cellChoices, 1);
      panel.Add(depthField);

      var templateField = new CityForgeChoiceField(
          _root,
          "STARTING CONTENT",
          new List<string> { "Empty", "Two-Way Traffic Test" },
          0);
      panel.Add(templateField);

      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("CREATE LOT", () =>
      {
        RemoveDocumentModal();
        if (templateField.index == 1)
        {
          ApplyTrafficTemplate();
          return;
        }

        Enum.TryParse(typeField.value, out LotType lotType);
        var lotName = string.IsNullOrWhiteSpace(nameField.value)
                  ? "Untitled Lot"
                  : nameField.value.Trim();
        _lotWorld.NewEmptyLot(lotName, lotType,
                  widthField.index + 1, depthField.index + 1);
        _hasOpenLot = true;
        _lotEditorCategoryExpanded = false;
        _lotStatus = $"New empty {lotType.ToString().ToLowerInvariant()} lot";
        Show(AppScreen.LotEditor);
      }, true, "primary"));
      actions.Add(CfButton.Create("CANCEL", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
      nameField.schedule.Execute(nameField.Focus);
    }

    private void ComposeSaveAsDialog()
    {
      var panel = CreateDocumentModal(
          "SAVE LOT AS",
          "Create a separate saved lot while leaving the existing document intact.");
      var nameField = new TextField("LOT NAME")
      {
        value = $"{_lotWorld.CurrentLotName} Copy"
      };
      nameField.AddToClassList("document-field");
      panel.Add(nameField);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("SAVE COPY", () =>
      {
        var path = _lotWorld.SaveLotAs(nameField.value);
        RemoveDocumentModal();
        _lotStatus = $"Saved as {_lotWorld.CurrentLotName} • {System.IO.Path.GetFileName(path)}";
        Show(AppScreen.LotEditor);
      }, true, "primary"));
      actions.Add(CfButton.Create("CANCEL", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
      nameField.schedule.Execute(nameField.Focus);
    }

    private void ComposeLoadLotBrowser()
    {
      var saves = LotSaveStore.List();
      var panel = CreateDocumentModal(
          "LOAD LOT",
          saves.Count == 0
              ? "No saved lots exist yet."
              : "Choose a saved lot. Most recently modified lots appear first.");
      panel.AddToClassList("load-lot-modal-panel");
      if (saves.Count > 0)
      {
        var list = new ScrollView(ScrollViewMode.Vertical)
        {
          name = "lot-save-list",
          verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible,
          horizontalScrollerVisibility = ScrollerVisibility.Hidden
        };
        list.style.height = 420f;
        list.style.maxHeight = 420f;
        list.style.flexGrow = 0f;
        list.style.flexShrink = 0f;
        list.contentViewport.style.overflow = Overflow.Hidden;
        list.AddToClassList("lot-save-list");
        foreach (var summary in saves)
        {
          var captured = summary;
          var missing = _lotWorld.MissingDependencies(summary.LotId);
          var entry = new VisualElement();
          entry.AddToClassList("lot-save-entry");
          var thumbnail = new VisualElement();
          thumbnail.AddToClassList("lot-save-thumbnail");
          var thumbnailTexture = LoadLotThumbnail(summary.BuildingId);
          if (thumbnailTexture != null)
            thumbnail.style.backgroundImage = new StyleBackground(thumbnailTexture);
          else
            thumbnail.Add(StyledLabel("EMPTY", "lot-save-thumbnail-empty"));
          entry.Add(thumbnail);

          var details = new VisualElement();
          details.AddToClassList("lot-save-details");
          details.Add(StyledLabel(summary.Name, "lot-save-entry-title"));
          details.Add(StyledLabel(
              $"{LotTypeLabel(summary.LotType).ToUpperInvariant()}  •  {summary.LotWidthCells} × {summary.LotDepthCells} CELLS  •  {summary.LotWidthCells * 10} × {summary.LotDepthCells * 10} M  •  {FormatSaveTime(summary.ModifiedUtc)}",
              "lot-save-entry-meta"));
          if (missing.Count > 0)
            details.Add(StyledLabel(
                $"MISSING: {string.Join(", ", missing)}",
                "lot-save-entry-warning"));
          var entryActions = new VisualElement();
          entryActions.AddToClassList("lot-save-entry-actions");
          entryActions.Add(CfButton.Create("LOAD", () =>
          {
            if (!_lotWorld.LoadLot(captured.LotId)) return;
            _hasOpenLot = true;
            _lotEditorCategoryExpanded = false;
            RemoveDocumentModal();
            _lotStatus = $"Loaded {captured.Name}";
            Show(AppScreen.LotEditor);
          }, missing.Count == 0, "primary"));
          entryActions.Add(CfButton.Create("RENAME", () => ComposeRenameLotDialog(captured), true, "secondary"));
          entryActions.Add(CfButton.Create("DUPLICATE", () =>
          {
            LotSaveStore.Duplicate(captured.LotId);
            ComposeLoadLotBrowser();
          }, true, "secondary"));
          entryActions.Add(CfButton.Create("DELETE", () => ComposeDeleteLotDialog(captured), true, "danger"));
          details.Add(entryActions);
          entry.Add(details);
          list.Add(entry);
        }
        panel.Add(list);
      }

      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("CANCEL", RemoveDocumentModal, true, "quiet"));
      panel.Add(actions);
    }

    private void ComposeRenameLotDialog(LotSaveSummary summary)
    {
      var panel = CreateDocumentModal("RENAME LOT", "Change the display name without changing its stable lot ID.");
      var nameField = new TextField("LOT NAME") { value = summary.Name };
      nameField.AddToClassList("document-field");
      panel.Add(nameField);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("RENAME", () =>
      {
        if (_lotWorld.CurrentLotId == summary.LotId)
        {
          _lotWorld.RenameAndSaveLot(nameField.value);
        }
        else
        {
          var session = new LotEditorSession();
          if (LotSaveStore.Load(session, summary.LotId))
          {
            session.Rename(nameField.value);
            LotSaveStore.Save(session, session.Data.RequiredPackageIds);
          }
        }
        ComposeLoadLotBrowser();
      }, true, "primary"));
      actions.Add(CfButton.Create("CANCEL", ComposeLoadLotBrowser, true, "quiet"));
      panel.Add(actions);
      nameField.schedule.Execute(nameField.Focus);
    }

    private void ComposeDeleteLotDialog(LotSaveSummary summary)
    {
      var panel = CreateDocumentModal(
          "DELETE SAVED LOT?",
          $"Delete {summary.Name}? This removes its JSON save file and cannot be undone.");
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DELETE", () =>
      {
        LotSaveStore.Delete(summary.LotId);
        ComposeLoadLotBrowser();
      }, true, "danger"));
      actions.Add(CfButton.Create("CANCEL", ComposeLoadLotBrowser, true, "quiet"));
      panel.Add(actions);
    }

    private static Texture2D LoadLotThumbnail(string buildingId)
    {
      if (string.IsNullOrWhiteSpace(buildingId)) return null;
      foreach (var package in HybridBuildingPackageRegistry.All)
      {
        if (package.Id != buildingId) continue;
        var facing = package.Facing(package.FrontFacingQuarterTurns);
        var path = string.IsNullOrWhiteSpace(facing.NeutralResourcePath)
            ? facing.ApprovedResourcePath
            : facing.NeutralResourcePath;
        return Resources.Load<Texture2D>(path);
      }
      return null;
    }

    private void OpenRoadMaterialModal()
    {
      _pendingRoadMaterialId = _lotWorld.SelectedRoadMaterialId;
      _pendingSidewalkMaterialId = _lotWorld.SelectedSidewalkMaterialId;
      _pendingRoadMarkingStyle = _lotWorld.SelectedRoadMarkingStyle;
      _pendingRoadLaneMarkingStyle = _lotWorld.SelectedRoadLaneMarkingStyle;
      _pendingRoadCenterMarkingStyle = _lotWorld.SelectedRoadCenterMarkingStyle;
      _pendingApplyRoadMaterialsToAll = _lotWorld.ApplyRoadMaterialsToAll;
      ComposeRoadMaterialModal();
    }

    private void OpenTestVehicleModal()
    {
      var panel = CreateDocumentModal(
          "TEST VEHICLE LIBRARY",
          _lotWorld.CanSpawnTestVehicle
              ? "Choose a vehicle to add to the current road circuit. Each click adds another independently moving test vehicle."
              : "Build a connected road circuit before adding test vehicles.");
      panel.AddToClassList("road-material-modal-panel");
      panel.Add(StyledLabel("FORD MODEL T", "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      foreach (VehiclePaintVariant variant in
               System.Enum.GetValues(typeof(VehiclePaintVariant)))
      {
        var captured = variant;
        var card = new VisualElement();
        card.AddToClassList("road-material-card");
        var swatch = new VisualElement();
        swatch.AddToClassList("road-material-swatch");
        swatch.style.backgroundColor = VehicleRuntimePresentation.PaintColor(variant);
        card.Add(swatch);
        var button = CfButton.Create($"ADD {variant.ToString().ToUpperInvariant()}",
            () => AddTestVehicleAndKeepOpen(captured),
            _lotWorld.CanSpawnTestVehicle, "quiet");
        button.name = $"test-vehicle-{variant}";
        card.Add(button);
        grid.Add(card);
      }
      panel.Add(grid);
      panel.Add(StyledLabel("1926 ROLLS-ROYCE", "road-material-role"));
      var rollsRoyceRow = new VisualElement();
      rollsRoyceRow.AddToClassList("inspector-actions");
      rollsRoyceRow.Add(CfButton.Create("ADD 1926 ROLLS-ROYCE",
          AddRollsRoyceTestVehicleAndKeepOpen,
          _lotWorld.CanSpawnTestVehicle, "quiet"));
      panel.Add(rollsRoyceRow);
      var count = StyledLabel(
          $"{_lotWorld.TestVehicleCount} TEST VEHICLES ACTIVE",
          "inspector-note");
      count.name = "test-vehicle-count";
      panel.Add(count);
      var actions = DocumentModalActions();
      var removeAll = CfButton.Create("REMOVE ALL",
          RemoveAllTestVehiclesAndKeepOpen,
          _lotWorld.TestVehicleCount > 0, "danger");
      removeAll.name = "test-vehicle-remove-all";
      actions.Add(removeAll);
      actions.Add(CfButton.Create("DONE", () =>
      {
        RemoveDocumentModal();
        ComposeLotEditor();
      }, true, "primary"));
      panel.Add(actions);
    }

    private void AddTestVehicleAndKeepOpen(VehiclePaintVariant variant)
    {
      _lotStatus = _lotWorld.SpawnTestVehicle(variant)
          ? $"Added {variant.ToString().ToLowerInvariant()} Ford Model T test vehicle"
          : "A connected road circuit is required for test vehicles";
      RefreshTestVehicleModalState();
    }

    private void RemoveAllTestVehiclesAndKeepOpen()
    {
      _lotWorld.RemoveTestVehicles();
      _lotStatus = "Removed all test vehicles";
      RefreshTestVehicleModalState();
    }

    private void AddRollsRoyceTestVehicleAndKeepOpen()
    {
      _lotStatus = _lotWorld.SpawnTestVehicle(
              TestVehicleModel.RollsRoyce1926)
          ? "Added 1926 Rolls-Royce test vehicle"
          : "A connected road circuit is required for test vehicles";
      RefreshTestVehicleModalState();
    }

    private void RefreshTestVehicleModalState()
    {
      var count = _root.Q<Label>("test-vehicle-count");
      if (count != null)
        count.text = $"{_lotWorld.TestVehicleCount} TEST VEHICLES ACTIVE";
      var removeAll = _root.Q<Button>("test-vehicle-remove-all");
      removeAll?.SetEnabled(_lotWorld.TestVehicleCount > 0);
    }

    private void ComposeRoadMaterialModal()
    {
      var panel = CreateDocumentModal(
          "ROAD & SIDEWALK MATERIALS",
          _lotWorld.HasSelectedRoadPiece
              ? "Choose independent finishes for the selected road tile. Choices are also retained for newly placed tiles."
              : "Choose the finishes that newly placed road tiles will use.");
      panel.AddToClassList("road-material-modal-panel");
      panel.Add(StyledLabel("APPLY SCOPE", "road-material-role"));
      var applyAllButton = CfButton.Create(
          _pendingApplyRoadMaterialsToAll ? "✓ APPLY TO ALL ROADS" : "APPLY TO ALL ROADS",
          () =>
          {
            _pendingApplyRoadMaterialsToAll = !_pendingApplyRoadMaterialsToAll;
            ApplyPendingRoadMaterialsAndKeepOpen();
          }, true, _pendingApplyRoadMaterialsToAll ? "mode-selected" : "quiet");
      applyAllButton.name = "road-material-apply-all";
      panel.Add(applyAllButton);
      panel.Add(StyledLabel(
          "When enabled, existing compatible roads update together and new roads keep these choices.",
          "inspector-note"));
      if (_lotWorld.SelectedRoadSupportsIndependentMarkings)
      {
        panel.Add(StyledLabel("LANE DIVIDERS", "road-material-role"));
        var laneRow = new VisualElement();
        laneRow.AddToClassList("road-material-marking-row");
        AddRoadLaneMarkingChoice(laneRow, "LINES", RoadLaneMarkingStyle.Lines);
        AddRoadLaneMarkingChoice(laneRow, "NONE", RoadLaneMarkingStyle.NoLines);
        panel.Add(laneRow);
        panel.Add(StyledLabel("CENTER DIVISION", "road-material-role"));
        var centerRow = new VisualElement();
        centerRow.AddToClassList("road-material-marking-row");
        AddRoadCenterMarkingChoice(centerRow, "DOUBLE", RoadCenterMarkingStyle.DoubleLines);
        AddRoadCenterMarkingChoice(centerRow, "NONE", RoadCenterMarkingStyle.NoLines);
        panel.Add(centerRow);
      }
      else
      {
        panel.Add(StyledLabel("CENTER LINES", "road-material-role"));
        var markingRow = new VisualElement();
        markingRow.AddToClassList("road-material-marking-row");
        AddRoadMarkingChoice(markingRow, "NO LINES", RoadMarkingStyle.NoLines);
        AddRoadMarkingChoice(markingRow, "DOUBLE LINES", RoadMarkingStyle.DoubleLines);
        AddRoadMarkingChoice(markingRow, "SINGLE DOTTED", RoadMarkingStyle.SingleDotted);
        panel.Add(markingRow);
      }
      var list = new ScrollView(ScrollViewMode.Vertical);
      list.AddToClassList("road-material-list");
      list.Add(StyledLabel("ROAD SURFACE", "road-material-role"));
      foreach (RoadMaterialEra era in System.Enum.GetValues(typeof(RoadMaterialEra)))
        AddRoadMaterialEra(list, era, false);
      list.Add(StyledLabel("SIDEWALK / EDGE", "road-material-role"));
      foreach (RoadMaterialEra era in System.Enum.GetValues(typeof(RoadMaterialEra)))
        AddRoadMaterialEra(list, era, true);
      panel.Add(list);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", () =>
      {
        RemoveDocumentModal();
        ComposeLotEditor();
      }, true, "primary"));
      panel.Add(actions);
    }

    private void ApplyPendingRoadMaterialsAndKeepOpen()
    {
      var changedTile = _lotWorld.ApplyRoadMaterials(
          _pendingRoadMaterialId, _pendingSidewalkMaterialId,
          _pendingRoadMarkingStyle, _pendingApplyRoadMaterialsToAll,
          _pendingRoadLaneMarkingStyle, _pendingRoadCenterMarkingStyle);
      _lotStatus = changedTile
          ? (_pendingApplyRoadMaterialsToAll
              ? "Materials and markings applied to all compatible roads"
              : "Road materials and markings applied to the selected tile")
          : "Road materials and markings set for newly placed tiles";
      RefreshRoadMaterialPaletteState();
    }

    private void AddRoadMarkingChoice(VisualElement row, string label,
        RoadMarkingStyle style)
    {
      var button = CfButton.Create(label, () =>
      {
        _pendingRoadMarkingStyle = style;
        ApplyPendingRoadMaterialsAndKeepOpen();
      }, true, _pendingRoadMarkingStyle == style ? "mode-selected" : "quiet");
      button.name = $"road-marking-{style}";
      row.Add(button);
    }

    private void AddRoadLaneMarkingChoice(VisualElement row, string label,
        RoadLaneMarkingStyle style)
    {
      var button = CfButton.Create(label, () =>
      {
        _pendingRoadLaneMarkingStyle = style;
        ApplyPendingRoadMaterialsAndKeepOpen();
      }, true, _pendingRoadLaneMarkingStyle == style ? "mode-selected" : "quiet");
      button.name = $"road-lane-marking-{style}";
      row.Add(button);
    }

    private void AddRoadCenterMarkingChoice(VisualElement row, string label,
        RoadCenterMarkingStyle style)
    {
      var button = CfButton.Create(label, () =>
      {
        _pendingRoadCenterMarkingStyle = style;
        ApplyPendingRoadMaterialsAndKeepOpen();
      }, true, _pendingRoadCenterMarkingStyle == style ? "mode-selected" : "quiet");
      button.name = $"road-center-marking-{style}";
      row.Add(button);
    }

    private void AddRoadMaterialEra(VisualElement parent, RoadMaterialEra era,
        bool sidewalk)
    {
      var materials = RoadMaterialCatalog.ForEra(era, sidewalk);
      if (materials.Count == 0) return;
      parent.Add(StyledLabel(era.ToString().ToUpperInvariant(), "road-material-era"));
      var row = new VisualElement();
      row.AddToClassList("road-material-grid");
      foreach (var definition in materials)
      {
        var captured = definition;
        var selected = sidewalk
            ? _pendingSidewalkMaterialId == definition.Id
            : _pendingRoadMaterialId == definition.Id;
        var card = new VisualElement();
        card.AddToClassList("road-material-card");
        var swatch = new VisualElement();
        swatch.AddToClassList("road-material-swatch");
        swatch.style.backgroundImage = new StyleBackground(definition.LoadTexture());
        card.Add(swatch);
        var button = CfButton.Create(definition.DisplayName.ToUpperInvariant(), () =>
        {
          if (sidewalk) _pendingSidewalkMaterialId = captured.Id;
          else _pendingRoadMaterialId = captured.Id;
          ApplyPendingRoadMaterialsAndKeepOpen();
        }, true, selected ? "mode-selected" : "quiet");
        button.name = $"road-material-{(sidewalk ? "sidewalk" : "road")}-{definition.Id}";
        card.Add(button);
        row.Add(card);
      }
      parent.Add(row);
    }

    private void RefreshRoadMaterialPaletteState()
    {
      var applyAll = _root.Q<Button>("road-material-apply-all");
      if (applyAll != null)
      {
        applyAll.text = _pendingApplyRoadMaterialsToAll
            ? "✓ APPLY TO ALL ROADS" : "APPLY TO ALL ROADS";
        SetRoadPaletteButtonSelected(applyAll, _pendingApplyRoadMaterialsToAll);
      }
      foreach (var definition in RoadMaterialCatalog.All)
      {
        SetRoadPaletteButtonSelected(
            _root.Q<Button>($"road-material-road-{definition.Id}"),
            _pendingRoadMaterialId == definition.Id);
        SetRoadPaletteButtonSelected(
            _root.Q<Button>($"road-material-sidewalk-{definition.Id}"),
            _pendingSidewalkMaterialId == definition.Id);
      }
      foreach (RoadMarkingStyle style in System.Enum.GetValues(typeof(RoadMarkingStyle)))
        SetRoadPaletteButtonSelected(_root.Q<Button>($"road-marking-{style}"),
            _pendingRoadMarkingStyle == style);
      foreach (RoadLaneMarkingStyle style in System.Enum.GetValues(typeof(RoadLaneMarkingStyle)))
        SetRoadPaletteButtonSelected(_root.Q<Button>($"road-lane-marking-{style}"),
            _pendingRoadLaneMarkingStyle == style);
      foreach (RoadCenterMarkingStyle style in System.Enum.GetValues(typeof(RoadCenterMarkingStyle)))
        SetRoadPaletteButtonSelected(_root.Q<Button>($"road-center-marking-{style}"),
            _pendingRoadCenterMarkingStyle == style);
    }

    private static void SetRoadPaletteButtonSelected(Button button, bool selected)
    {
      if (button == null) return;
      button.EnableInClassList("cf-button--mode-selected", selected);
      button.EnableInClassList("cf-button--quiet", !selected);
    }

    private VisualElement EnvironmentLightingSlider(string label,
        string control, float minimum, float maximum, float value,
        string format)
    {
      var row = new VisualElement();
      row.AddToClassList("environment-lighting-control");
      var valueLabel = StyledLabel(
          $"{label}  {value.ToString(format)}", "lighting-note");
      row.Add(valueLabel);
      var slider = new Slider(minimum, maximum)
      {
        name = $"environment-{control}",
        value = value,
        showInputField = false
      };
      slider.AddToClassList("environment-lighting-slider");
      slider.RegisterValueChangedCallback(evt =>
      {
        _lotWorld?.SetEnvironmentLightingControl(control, evt.newValue);
        valueLabel.text = $"{label}  {evt.newValue.ToString(format)}";
      });
      var adjustment = new VisualElement();
      adjustment.AddToClassList("environment-lighting-adjustment");
      adjustment.Add(CfButton.Create("−", () =>
      {
        var step = (maximum - minimum) / 40f;
        slider.value = Mathf.Clamp(slider.value - step, minimum, maximum);
      }, true, "icon"));
      adjustment.Add(slider);
      adjustment.Add(CfButton.Create("+", () =>
      {
        var step = (maximum - minimum) / 40f;
        slider.value = Mathf.Clamp(slider.value + step, minimum, maximum);
      }, true, "icon"));
      row.Add(adjustment);
      return row;
    }

    private static Foldout RoadFoldout(string title, bool expanded,
        Action<bool> onChanged)
    {
      var foldout = new Foldout { text = title, value = expanded };
      foldout.AddToClassList("road-inspector-section");
      foldout.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
      return foldout;
    }

    private VisualElement TransitSection(string title, bool expanded,
        Action<bool> onChanged, out VisualElement content)
    {
      var section = new VisualElement();
      section.AddToClassList("transit-inspector-section");
      var toggle = CfButton.Create($"{(expanded ? "−" : "+")}  {title}", () =>
      {
        onChanged?.Invoke(!expanded);
        ComposeLotEditor();
      }, true, "quiet");
      toggle.AddToClassList("transit-inspector-toggle");
      section.Add(toggle);
      content = new VisualElement();
      content.AddToClassList("transit-inspector-content");
      content.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
      section.Add(content);
      return section;
    }

    private VisualElement CreateDocumentModal(string title, string copy)
    {
      CancelIndustryPlacement();
      RemoveDocumentModal();
      var overlay = new VisualElement { name = "document-modal" };
      overlay.AddToClassList("document-modal");
      if (_currentScreen == AppScreen.RegionEditor || _currentScreen == AppScreen.DistrictTerraform)
        overlay.AddToClassList("cf-map-modal");
      overlay.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
      overlay.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
      overlay.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
      var panel = new VisualElement();
      panel.AddToClassList("document-modal-panel");
      panel.Add(StyledLabel(title, "document-modal-title"));
      panel.Add(StyledLabel(copy, "document-modal-copy"));
      overlay.Add(panel);
      _root.Add(overlay);
      return panel;
    }

    private static VisualElement DocumentModalActions()
    {
      var actions = new VisualElement();
      actions.AddToClassList("document-modal-actions");
      return actions;
    }

    private void RemoveDocumentModal()
    {
      _root?.Q<VisualElement>("document-modal")?.RemoveFromHierarchy();
    }

    private static string FormatSaveTime(string utc)
    {
      return DateTime.TryParse(utc, out var value)
          ? value.ToLocalTime().ToString("MMM d, yyyy  h:mm tt")
          : "Saved lot";
    }

    private static string LotTypeLabel(LotType lotType) => lotType switch
    {
      LotType.Residential => "Residential",
      LotType.Commercial => "Commercial",
      LotType.Industrial => "Industrial",
      LotType.Mixed => "Mixed",
      LotType.Agricultural => "Agricultural",
      LotType.Civics => "Civics",
      _ => "Transportation"
    };

    private sealed class CityForgeChoiceField : VisualElement
    {
      private static CityForgeChoiceField _openChoice;
      private readonly List<string> _choices;
      private readonly VisualElement _overlayHost;
      private readonly Button _field;
      private VisualElement _popup;

      public int index { get; private set; }
      public string value => _choices[index];

      public CityForgeChoiceField(
          VisualElement overlayHost,
          string label,
          List<string> choices,
          int selectedIndex)
      {
        _overlayHost = overlayHost;
        _choices = choices != null && choices.Count > 0
            ? choices : new List<string> { "None" };
        index = Mathf.Clamp(selectedIndex, 0, _choices.Count - 1);
        AddToClassList("document-field");
        AddToClassList("cf-choice-field");
        Add(StyledLabel(label, "cf-choice-label"));
        _field = new Button(OpenMenu) { text = $"{value}   ▼", tooltip = label };
        _field.AddToClassList("cf-choice-display");
        Add(_field);
        RegisterCallback<DetachFromPanelEvent>(_ => CloseMenu(false));
      }

      private void CloseMenu(bool restoreFocus)
      {
        _popup?.RemoveFromHierarchy();
        _popup = null;
        if (_openChoice == this) _openChoice = null;
        RemoveFromClassList("cf-choice-open");
        if (restoreFocus && _field.panel != null) _field.Focus();
      }

      private void OpenMenu()
      {
        if (_popup != null) { CloseMenu(true); return; }
        _openChoice?.CloseMenu(false);
        _openChoice = this;
        AddToClassList("cf-choice-open");
        var popup = new VisualElement { name = "cf-choice-overlay" };
        popup.style.position = Position.Absolute;
        popup.style.left = popup.style.right = 0;
        popup.style.top = popup.style.bottom = 0;
        _popup = popup;
        // A last root sibling paints above dialogs and inspectors and
        // escapes their clipping. UI Toolkit does not use CSS z-index.
        _overlayHost.Add(popup);
        popup.BringToFront();
        var menu = new ScrollView(ScrollViewMode.Vertical) { name = "cf-choice-menu" };
        menu.AddToClassList("cf-choice-menu");
        menu.style.position = Position.Absolute;
        var anchor = _overlayHost.WorldToLocal(_field.worldBound.position);
        var hostWidth = _overlayHost.resolvedStyle.width;
        var hostHeight = _overlayHost.resolvedStyle.height;
        var width = Mathf.Min(_field.worldBound.width, Mathf.Max(1, hostWidth - 16));
        var below = Mathf.Max(0, hostHeight - anchor.y - _field.worldBound.height - 10);
        var above = Mathf.Max(0, anchor.y - 10);
        var wantedHeight = Mathf.Min(300, _choices.Count * 36 + 12);
        var opensBelow = below >= wantedHeight || below >= above;
        var height = Mathf.Min(wantedHeight, opensBelow ? below : above);
        menu.style.left = Mathf.Clamp(anchor.x, 8, Mathf.Max(8, hostWidth - width - 8));
        menu.style.top = opensBelow ? anchor.y + _field.worldBound.height + 2
            : anchor.y - height - 2;
        menu.style.width = width;
        menu.style.height = height;
        popup.Add(menu);
        var options = new List<Button>();
        void FocusOption(int optionIndex)
        {
          var option = options[(optionIndex + options.Count) % options.Count];
          option.Focus();
          menu.ScrollTo(option);
        }
        for (var i = 0; i < _choices.Count; i++)
        {
          var choiceIndex = i;
          var option = new Button(() =>
          {
            index = choiceIndex;
            _field.text = $"{value}   ▼";
            CloseMenu(true);
          })
          { text = (i == index ? "✓  " : "    ") + _choices[i] };
          option.AddToClassList("cf-choice-option");
          option.EnableInClassList("is-selected", i == index);
          option.RegisterCallback<KeyDownEvent>(evt =>
          {
            if (evt.keyCode != KeyCode.DownArrow && evt.keyCode != KeyCode.UpArrow) return;
            FocusOption(choiceIndex + (evt.keyCode == KeyCode.DownArrow ? 1 : -1));
            evt.StopPropagation();
          });
          options.Add(option);
          menu.Add(option);
        }
        popup.RegisterCallback<PointerDownEvent>(evt =>
        {
          if (evt.target == popup) CloseMenu(true);
          evt.StopPropagation();
        });
        popup.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
        popup.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        popup.RegisterCallback<KeyDownEvent>(evt =>
        {
          if (evt.keyCode == KeyCode.Escape) { CloseMenu(true); evt.StopPropagation(); }
        }, TrickleDown.TrickleDown);
        popup.schedule.Execute(() => FocusOption(index));
      }
    }

    private sealed class CityForgeCellCountField : VisualElement
    {
      private readonly List<Button> _buttons = new();

      public int value { get; private set; }

      public CityForgeCellCountField(
          string label, int selectedCells, Action<int> selectionChanged)
      {
        value = Mathf.Clamp(selectedCells, 1, 8);
        AddToClassList("cf-cell-count-field");
        Add(StyledLabel(label, "cf-choice-label"));

        var choices = new VisualElement();
        choices.AddToClassList("cf-cell-count-choices");
        for (var cells = 1; cells <= 8; cells++)
        {
          var capturedCells = cells;
          var button = new Button(() =>
          {
            Select(capturedCells);
            selectionChanged?.Invoke(value);
          })
          {
            text = cells.ToString()
          };
          button.AddToClassList("cf-cell-count-option");
          choices.Add(button);
          _buttons.Add(button);
        }

        Add(choices);
        RefreshSelection();
      }

      private void Select(int cells)
      {
        value = Mathf.Clamp(cells, 1, 8);
        RefreshSelection();
      }

      private void RefreshSelection()
      {
        for (var index = 0; index < _buttons.Count; index++)
          _buttons[index].EnableInClassList("is-selected", index + 1 == value);
      }
    }

    private void ApplyTrafficTemplate()
    {
      _lotWorld.ApplyTrafficTestTemplate();
      _hasOpenLot = true;
      _lotWorld.SetTimeOfDay(TimeOfDayPreset.Afternoon);
      _lotWorld.SetZoomLevel(LotZoomLevel.Close);
      _lotWorld.SpawnTestVehicle(VehiclePaintVariant.Red);
      _lotEditorCategory = LotEditorCategory.Roads;
      _lotEditorCategoryExpanded = true;
      _lotStatus = "Afternoon vehicle-shadow visual test ready";
      Show(AppScreen.LotEditor);
    }

    private void EnsureLotWorld()
    {
      if (_lotWorld != null)
      {
        _lotWorld.SetVisible(_hasOpenLot);
        return;
      }

      var world = new GameObject("V3 Lot World");
      _lotWorld = world.AddComponent<LotWorldController>();
      _lotWorld.StateChanged += RefreshLotEditor;
      _lotWorld.Build();
      _lotWorld.SetVisible(false);
    }

    private void OpenSelectedBuildingInspector()
    {
      _lotInspectorVisible = true;
      _lotEditorCategoryExpanded = false;
      _lotEditorCategory = _lotWorld.SelectedBuilding3DIndex >= 0
          ? LotEditorCategory.Buildings3D : LotEditorCategory.Buildings;
      RefreshLotEditor();
    }

    private void RotateSelectedNativeBuilding(int direction)
    {
      _lotStatus = _lotWorld.RotateSelectedBuilding3D(direction)
          ? "Building rotated 45°" : "Select a building to rotate";
      RefreshLotEditor();
    }

    private void RefreshLotEditor()
    {
      if (_root == null || _lotWorld == null ||
          !_lotWorld.gameObject.activeSelf || _lotEditorRefreshScheduled)
        return;

      // World notifications can originate inside a UI Toolkit pointer
      // callback. Rebuilding synchronously removes the element that still
      // owns the pointer and can leave the lot editor without its panels.
      _lotEditorRefreshScheduled = true;
      _root.schedule.Execute(() =>
      {
        _lotEditorRefreshScheduled = false;
        if (_root == null || _lotWorld == null ||
                  !_lotWorld.gameObject.activeSelf ||
                  _root.Q<VisualElement>("document-modal") != null)
          return;
        // Keep the captured viewport alive through building drags.
        // Pointer-up opens the selected inspector and refreshes it.
        if (_buildingPointerDown || _building3DPointerDown) return;
        Show(AppScreen.LotEditor);
      });
    }

    private static VisualElement Screen(string className)
    {
      var screen = new VisualElement();
      screen.AddToClassList("screen");
      screen.AddToClassList(className);
      return screen;
    }

    private static Label StyledLabel(string text, string className)
    {
      var label = new Label(text);
      label.AddToClassList(className);
      return label;
    }

    private static VisualElement Property(string key, string value)
    {
      var row = new VisualElement();
      row.AddToClassList("property-row");
      row.Add(StyledLabel(key, "property-key"));
      row.Add(StyledLabel(value, "property-value"));
      return row;
    }
  }
}
