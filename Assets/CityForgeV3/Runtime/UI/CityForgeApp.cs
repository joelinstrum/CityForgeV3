using System;
using System.Collections.Generic;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public enum LotEditorCategory
    {
        Main,
        Buildings,
        BuildingProps,
        Roads,
        Paths,
        Flora,
        Props,
        BaseTextures,
        OverlayTextures,
        Environment,
        View
    }

    public sealed class CityForgeApp : MonoBehaviour
    {
        private const string StylePath = "CityForgeV3/UI/CityForgeV3";
        private const bool WarnAboutUnsavedLotChanges = false;
        private UIDocument _document;
        private VisualElement _root;
        private LotWorldController _lotWorld;
        private string _lotStatus = "";
        private AppScreen _currentScreen;
        private LotEditorCategory _lotEditorCategory = LotEditorCategory.Main;
        private bool _lotEditorCategoryExpanded;
        private bool _hasOpenLot;
        private BuildingUseCategory _buildingUseCategory = BuildingUseCategory.Residential;
        private bool _roadPointerDown;
        private bool _roadDragStarted;
        private bool _outsideConnectorDragCreated;
        private Vector2Int _lastRoadDragCell;
        private bool _buildingPointerDown;
        private bool _buildingDragStarted;
        private bool _floraPointerDown;
        private bool _floraDragStarted;
        private bool _propPointerDown;
        private bool _propDragStarted;
        private bool _buildingPropPointerDown;
        private bool _buildingPropDragStarted;
        private bool _overlayPointerDown;
        private bool _overlayDragPainted;
        private bool _roadFamilyExpanded = true;
        private bool _roadMaterialsExpanded = true;
        private bool _roadShapeExpanded = true;
        private bool _roadTrafficExpanded;
        private bool _roadEditExpanded;
        private bool _roadViewExpanded;
        private string _pendingRoadMaterialId = RoadMaterialCatalog.DefaultRoadId;
        private string _pendingSidewalkMaterialId = RoadMaterialCatalog.DefaultSidewalkId;
        private RoadMarkingStyle _pendingRoadMarkingStyle = RoadMarkingStyle.SingleDotted;
        private RoadLaneMarkingStyle _pendingRoadLaneMarkingStyle = RoadLaneMarkingStyle.Lines;
        private RoadCenterMarkingStyle _pendingRoadCenterMarkingStyle = RoadCenterMarkingStyle.DoubleLines;
        private bool _pendingApplyRoadMaterialsToAll;
        private string _placementFloraId = "maple";
        private string _placementPropId = "";
        private string _placementBuildingPropId = "";
        private string _placementOverlayTextureId = "";
        private Action _pendingDocumentAction;
        private bool _lotEditorRefreshScheduled;
        private int _pendingLotWidthCells = -1;
        private int _pendingLotDepthCells = -1;
        private VisualElement _lotContextMenu;

        public LotEditorCategory ActiveLotEditorCategory => _lotEditorCategory;
        public bool IsLotEditorCategoryExpanded => _lotEditorCategoryExpanded;

#if UNITY_EDITOR
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

            Show(AppScreen.Splash);
        }

        private void Show(AppScreen screen)
        {
            _currentScreen = screen;
            _root.Clear();
            _lotWorld?.SetVisible(screen == AppScreen.LotEditor && _hasOpenLot);

            switch (screen)
            {
                case AppScreen.Splash:
                    ComposeSplash();
                    break;
                case AppScreen.MainMenu:
                    ComposeMainMenu();
                    break;
                case AppScreen.LotEditor:
                    ComposeLotEditor();
                    break;
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_currentScreen != AppScreen.LotEditor ||
                _lotWorld == null)
            {
                return;
            }

            if (TextInputHasFocus())
            {
                return;
            }

            if (evt.keyCode == KeyCode.Escape)
            {
                DeselectAll();
                evt.StopPropagation();
            }
            else if (evt.keyCode is KeyCode.LeftArrow or KeyCode.RightArrow or
                     KeyCode.UpArrow or KeyCode.DownArrow)
            {
                var horizontal = evt.keyCode == KeyCode.LeftArrow ? -1 :
                    evt.keyCode == KeyCode.RightArrow ? 1 : 0;
                var vertical = evt.keyCode == KeyCode.UpArrow ? 1 :
                    evt.keyCode == KeyCode.DownArrow ? -1 : 0;
                MoveCategorySelectionOrPan(horizontal, vertical);
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
                _lotStatus = _lotWorld.RotateSelectedBuildingProp45Degrees()
                    ? "Building prop rotated 45°"
                    : "Select a building prop before rotating";
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

        private void ComposeSplash()
        {
            var screen = Screen("splash-screen");
            var art = new VisualElement
            {
                name = "splash-art",
                focusable = true
            };
            art.AddToClassList("splash-art");
            var texture = Resources.Load<Texture2D>("CityForgeV3/Art/city-forge-splash");
            if (texture != null)
            {
                art.style.backgroundImage = new StyleBackground(texture);
            }

            var prompt = new Label("CLICK TO CONTINUE");
            prompt.AddToClassList("splash-prompt");
            art.Add(prompt);
            art.RegisterCallback<ClickEvent>(_ => Show(AppScreen.MainMenu));
            art.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Space)
                {
                    Show(AppScreen.MainMenu);
                    evt.StopPropagation();
                }
            });
            screen.Add(art);
            _root.Add(screen);
            art.schedule.Execute(art.Focus);
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
            menu.Add(CfImageButton.Create("Open Region", "CityForgeV3/Art/MainMenu/open-region", null, false));
            menu.Add(CfImageButton.Create("New Region", "CityForgeV3/Art/MainMenu/new-region", null, false));
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
                _lotEditorCategory == LotEditorCategory.Props);
            _lotWorld.SetBuildingPropEditorContext(
                _lotEditorCategory == LotEditorCategory.BuildingProps);
            _lotWorld.SetOverlayEditorContext(
                _lotEditorCategory == LotEditorCategory.OverlayTextures);
            _lotWorld.SetCirculationEditorContext(
                _lotEditorCategory == LotEditorCategory.Paths);

            var screen = Screen("lot-editor-screen");
            screen.focusable = true;
            var timeSpec = TimeOfDayLighting.For(_lotWorld.TimeOfDay);
            var timeGrade = new VisualElement
            {
                name = "time-of-day-grade",
                pickingMode = PickingMode.Ignore
            };
            timeGrade.AddToClassList("time-of-day-grade");
            timeGrade.style.backgroundColor = timeSpec.ScreenTint;
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
            viewportInput.RegisterCallback<PointerDownEvent>(evt =>
            {
                RemoveLotContextMenu();
                _lotWorld.ClearObjectHover();
                var panelSize = new Vector2(
                    viewportInput.resolvedStyle.width,
                    viewportInput.resolvedStyle.height);
                if (evt.button == 1 &&
                    _lotEditorCategory != LotEditorCategory.Roads &&
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
                    (_lotEditorCategory == LotEditorCategory.BuildingProps &&
                     !string.IsNullOrWhiteSpace(_placementBuildingPropId));
                if (evt.button == 0 && !toolPlacementHasPriority &&
                    _lotEditorCategory != LotEditorCategory.OverlayTextures)
                {
                    var selection = _lotWorld.BeginExistingObjectManipulationFromPanel(
                        evt.position, panelSize);
                    if (selection == LotObjectSelectionKind.Building)
                    {
                        _buildingPointerDown = true;
                        _buildingDragStarted = false;
                        viewportInput.CapturePointer(evt.pointerId);
                        evt.StopPropagation();
                        return;
                    }
                    if (selection == LotObjectSelectionKind.Flora)
                    {
                        _floraPointerDown = true;
                        _floraDragStarted = false;
                        viewportInput.CapturePointer(evt.pointerId);
                        evt.StopPropagation();
                        return;
                    }
                    if (selection == LotObjectSelectionKind.Prop)
                    {
                        _propPointerDown = true;
                        _propDragStarted = false;
                        viewportInput.CapturePointer(evt.pointerId);
                        evt.StopPropagation();
                        return;
                    }
                    if (selection == LotObjectSelectionKind.BuildingProp)
                    {
                        _buildingPropPointerDown = true;
                        _buildingPropDragStarted = false;
                        viewportInput.CapturePointer(evt.pointerId);
                        evt.StopPropagation();
                        return;
                    }
                }
                if (_lotEditorCategory == LotEditorCategory.Buildings && evt.button == 0)
                {
                    if (!_lotWorld.BeginBuildingDragFromPanel(evt.position, panelSize)) return;
                    _buildingPointerDown = true;
                    _buildingDragStarted = false;
                    viewportInput.CapturePointer(evt.pointerId);
                    evt.StopPropagation();
                    return;
                }
                if (_lotEditorCategory == LotEditorCategory.BuildingProps &&
                    evt.button == 0)
                {
                    var placed = _lotWorld.PlaceBuildingPropFromPanel(
                        _placementBuildingPropId, evt.position, panelSize);
                    _lotStatus = placed
                        ? "Ale House sign attached • drag it to reposition"
                        : "Move the translucent sign over a building facade";
                    if (placed)
                    {
                        _placementBuildingPropId = "";
                        _lotWorld.SetBuildingPropPlacementPreview("");
                    }
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
                if (_lotEditorCategory == LotEditorCategory.Props && evt.button == 0)
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
                if (_lotEditorCategory != LotEditorCategory.Roads ||
                    _lotWorld.LotType != LotType.Neighborhood) return;
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
            viewportInput.RegisterCallback<PointerMoveEvent>(evt =>
            {
                var panelSize = new Vector2(
                    viewportInput.resolvedStyle.width,
                    viewportInput.resolvedStyle.height);
                var hoverSuppressed = _buildingPointerDown || _floraPointerDown ||
                    _propPointerDown || _buildingPropPointerDown ||
                    _overlayPointerDown || _roadPointerDown ||
                    ShouldPrioritizeToolPlacement(
                        _lotEditorCategory, _placementFloraId, _placementPropId) ||
                    (_lotEditorCategory == LotEditorCategory.BuildingProps &&
                     !string.IsNullOrWhiteSpace(_placementBuildingPropId)) ||
                    _lotEditorCategory == LotEditorCategory.OverlayTextures;
                _lotWorld.UpdateObjectHoverFromPanel(
                    evt.position, panelSize, hoverSuppressed);
                if (_buildingPointerDown)
                {
                    if (_lotWorld.DragBuildingFromPanel(evt.position, panelSize))
                        _buildingDragStarted = true;
                    evt.StopPropagation();
                    return;
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
                if (_lotEditorCategory == LotEditorCategory.Props)
                {
                    _lotWorld.UpdatePropPreviewFromPanel(evt.position, panelSize);
                    evt.StopPropagation();
                    return;
                }
                if (_lotEditorCategory == LotEditorCategory.BuildingProps)
                {
                    _lotWorld.UpdateBuildingPropPreviewFromPanel(
                        evt.position, panelSize);
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
                if (_lotEditorCategory != LotEditorCategory.Roads ||
                    _lotWorld.LotType != LotType.Neighborhood) return;
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
                if (_lotWorld.PaintRoadStrokeCellFromPanel(evt.position, panelSize))
                {
                    _roadDragStarted = true;
                    _lastRoadDragCell = _lotWorld.RoadCursorCell;
                }
                evt.StopPropagation();
            });
            viewportInput.RegisterCallback<PointerLeaveEvent>(_ =>
                _lotWorld.ClearObjectHover());
            viewportInput.RegisterCallback<PointerUpEvent>(evt =>
            {
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
                    _lotStatus = _buildingDragStarted
                        ? "Building moved on the construction grid"
                        : "Building selected • drag to move";
                    _buildingDragStarted = false;
                    Show(AppScreen.LotEditor);
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
                    _lotStatus = _floraDragStarted
                        ? "Tree moved and planted"
                        : "Tree selected • drag to move";
                    _floraDragStarted = false;
                    Show(AppScreen.LotEditor);
                    evt.StopPropagation();
                    return;
                }
                if (evt.button == 0 && _propPointerDown)
                {
                    _propPointerDown = false;
                    var panelSize = new Vector2(
                        viewportInput.resolvedStyle.width,
                        viewportInput.resolvedStyle.height);
                    if (_lotWorld.DragPropFromPanel(evt.position, panelSize))
                        _propDragStarted = true;
                    viewportInput.ReleasePointer(evt.pointerId);
                    _lotWorld.EndPropDrag();
                    _lotStatus = _propDragStarted
                        ? "Fence moved"
                        : "Fence selected • drag to move";
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
                !string.IsNullOrWhiteSpace(_placementFloraId))
            {
                topbar.Add(StyledLabel(
                    $"CURRENT SELECTION: {FloraDisplayName(_placementFloraId).ToUpperInvariant()}",
                    "current-selection-chip"));
            }
            if (_lotEditorCategory == LotEditorCategory.Props &&
                !string.IsNullOrWhiteSpace(_placementPropId))
                topbar.Add(StyledLabel(
                    "CURRENT SELECTION: WROUGHT-IRON FENCE",
                    "current-selection-chip"));

            var viewActions = new VisualElement();
            viewActions.AddToClassList("topbar-actions");
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
            toolRail.Add(CategoryButton(LotEditorCategory.Main, "main", "Main"));
            toolRail.Add(CategoryButton(LotEditorCategory.Buildings, "buildings", "Buildings"));
            toolRail.Add(CategoryButton(LotEditorCategory.BuildingProps,
                "props-lamppost-v91", "Building Props"));
            toolRail.Add(CategoryButton(LotEditorCategory.Roads, "roads-car-v74", "Roads"));
            toolRail.Add(CategoryButton(LotEditorCategory.Paths, "paths", "Paths"));
            toolRail.Add(CategoryButton(LotEditorCategory.Flora, "flora-tree-v91", "Flora"));
            toolRail.Add(CategoryButton(LotEditorCategory.Props, "props-lamppost-v91", "Props"));
            toolRail.Add(CategoryButton(LotEditorCategory.BaseTextures, "base-textures", "Base"));
            toolRail.Add(CategoryButton(LotEditorCategory.OverlayTextures, "overlay-textures", "Overlays"));
            toolRail.Add(CategoryButton(LotEditorCategory.Environment, "environment", "Environment"));
            toolRail.Add(CategoryButton(LotEditorCategory.View, "view", "View"));
            screen.Add(toolRail);

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

                var types = new List<string>
                {
                    "Residential", "Commercial", "Industrial", "Mixed", "Transportation"
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

            if (_lotEditorCategoryExpanded && _lotEditorCategory == LotEditorCategory.Buildings)
            {
                var catalog = new VisualElement { name = "buildings-category-panel" };
                catalog.AddToClassList("catalog");
                catalog.AddToClassList("building-catalog");
                catalog.Add(StyledLabel("BUILDINGS", "section-label"));
                catalog.Add(StyledLabel("BUILDING LIBRARY", "catalog-title"));
                var categoryTabs = new VisualElement { name = "building-use-tabs" };
                categoryTabs.AddToClassList("building-use-tabs");
                foreach (BuildingUseCategory category in Enum.GetValues(typeof(BuildingUseCategory)))
                {
                    var capturedCategory = category;
                    var categoryButton = CfButton.Create(
                        category.ToString().ToUpperInvariant(),
                        () =>
                        {
                            _buildingUseCategory = capturedCategory;
                            Show(AppScreen.LotEditor);
                        },
                        true,
                        _buildingUseCategory == category
                            ? "building-use-selected"
                            : "building-use");
                    categoryButton.name = $"building-use-{category.ToString().ToLowerInvariant()}";
                    categoryTabs.Add(categoryButton);
                }
                catalog.Add(categoryTabs);

                var visibleBuildings = BuildingCatalog.ForUseCategory(_buildingUseCategory);
                catalog.Add(StyledLabel(
                    $"{visibleBuildings.Count} {_buildingUseCategory.ToString().ToUpperInvariant()} " +
                    $"BUILDING{(visibleBuildings.Count == 1 ? string.Empty : "S")}",
                    "catalog-meta"));
                var buildingGrid = new VisualElement { name = "building-card-grid" };
                buildingGrid.AddToClassList("building-card-grid");
                foreach (var entry in visibleBuildings)
                {
                    var captured = entry;
                    var card = new Button(() => PlaceBuilding(captured))
                    {
                        name = $"building-card-{entry.Id}"
                    };
                    card.AddToClassList("building-card");

                    var thumbnail = new VisualElement();
                    thumbnail.AddToClassList("building-card-thumbnail");
                    var texture = Resources.Load<Texture2D>(entry.ThumbnailResourcePath);
                    if (texture != null)
                        thumbnail.style.backgroundImage = new StyleBackground(texture);
                    card.Add(thumbnail);

                    card.Add(StyledLabel(
                        entry.ShortName.ToUpperInvariant(),
                        "building-card-name"));
                    var compactMeta = $"{entry.OccupancyWidth}×{entry.OccupancyDepth}";
                    if (entry.ReviewStatus != "approved") compactMeta += "  REVIEW";
                    card.Add(StyledLabel(compactMeta, "building-card-meta"));
                    buildingGrid.Add(card);
                }
                catalog.Add(buildingGrid);
                if (visibleBuildings.Count == 0)
                    catalog.Add(StyledLabel(
                        $"NO {_buildingUseCategory.ToString().ToUpperInvariant()} BUILDINGS YET",
                        "catalog-empty"));
                screen.Add(catalog);
            }

            if (_lotEditorCategoryExpanded && _lotEditorCategory == LotEditorCategory.Environment)
            {
            var lightingLab = new VisualElement { name = "environment-category-panel" };
            lightingLab.AddToClassList("context-panel");
            lightingLab.Add(StyledLabel("TIME OF DAY", "section-label"));
            lightingLab.Add(StyledLabel(
                timeSpec.Label,
                "lighting-current"));
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
            inspector.Add(StyledLabel(_lotEditorCategory.ToString().ToUpperInvariant(), "section-label"));
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
                    "Foundation • Walls • Gable roof • Entrance"));

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
                trafficSection.Add(Property("TEST VEHICLES",
                    $"{_lotWorld.TestVehicleCount} ACTIVE"));
                trafficSection.Add(CfButton.Create("ADD TEST VEHICLES…",
                    OpenTestVehicleModal, _lotWorld.CanSpawnTestVehicle, "primary"));
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
                inspectionRow.Add(CfButton.Create("HYBRID", () => SetInspectionMode(BuildingInspectionMode.Hybrid), true, _lotWorld.InspectionMode == BuildingInspectionMode.Hybrid ? "mode-selected" : "quiet"));
                inspectionRow.Add(CfButton.Create("3D", () => SetInspectionMode(BuildingInspectionMode.Primitive), true, _lotWorld.InspectionMode == BuildingInspectionMode.Primitive ? "mode-selected" : "quiet"));
                inspector.Add(inspectionRow);
                var zoomRow = new VisualElement();
                zoomRow.AddToClassList("inspector-actions");
                zoomRow.Add(CfButton.Create("− ZOOM", () => StepZoom(1), _lotWorld.ZoomLevel != LotZoomLevel.Neighborhood));
                zoomRow.Add(CfButton.Create("ZOOM +", () => StepZoom(-1), _lotWorld.ZoomLevel != LotZoomLevel.Detail));
                inspector.Add(zoomRow);
                var cameraRow = new VisualElement();
                cameraRow.AddToClassList("inspector-actions");
                cameraRow.Add(CfButton.Create("↺ ROTATE", () => RotateLot(-1), true));
                cameraRow.Add(CfButton.Create("ROTATE ↻", () => RotateLot(1), true));
                inspector.Add(cameraRow);
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
            if (_lotEditorCategory is LotEditorCategory.Flora or LotEditorCategory.Props)
            {
                var flora = _lotEditorCategory == LotEditorCategory.Flora;
                inspector.Add(StyledLabel(flora ? "FLORA" : "PROPS", "inspector-title"));
                inspector.Add(Property("LIBRARY", flora
                    ? "TREES • SHRUBS • HEDGES • FLOWERS"
                    : "LIGHTING • FURNITURE • SIGNS • DETAILS"));
                if (flora)
                {
                    inspector.Add(Property("PLACED", $"{_lotWorld.FloraCount} TREES"));
                    inspector.Add(Property("ACTIVE",
                        string.IsNullOrWhiteSpace(_placementFloraId)
                            ? "NONE" : FloraDisplayName(_placementFloraId).ToUpperInvariant()));
                    inspector.Add(CfButton.Create("CHOOSE TREE…",
                        OpenFloraModal, true, "primary"));
                }
                else
                {
                    inspector.Add(Property("PLACED", $"{_lotWorld.PropCount} PROPS"));
                    inspector.Add(Property("ACTIVE",
                        string.IsNullOrWhiteSpace(_placementPropId)
                            ? "NONE"
                            : _placementPropId == "wrought-iron-fence-corner-v01"
                                ? "WROUGHT-IRON CORNER"
                                : "WROUGHT-IRON FENCE"));
                    inspector.Add(Property("FAMILY", "FENCES & GATES"));
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
                    ? "NONE" : "BRICK WALKWAY"));
                inspector.Add(Property("SELECTED", _lotWorld.SelectedOverlayTextureIndex >= 0
                    ? "BRICK WALKWAY TILE" : "NONE"));
                inspector.Add(CfButton.Create("CHOOSE OVERLAY…", OpenOverlayTextureModal, true, "primary"));
                inspector.Add(CfButton.Create("DELETE SELECTED [DELETE]",
                    DeleteSelectedOverlayTexture,
                    _lotWorld.SelectedOverlayTextureIndex >= 0, "danger"));
            }
            if (_hasOpenLot && _lotEditorCategory == LotEditorCategory.Main)
            {
                inspector.Add(StyledLabel("LOT CONTRACT", "inspector-title"));
                inspector.Add(Property("TYPE", LotTypeLabel(_lotWorld.LotType).ToUpperInvariant()));
                inspector.Add(Property("GRID",
                    $"{_lotWorld.LotWidthCells} × {_lotWorld.LotDepthCells} MAJOR CELLS"));
                inspector.Add(Property("REAL SIZE",
                    $"{_lotWorld.LotWidthMeters} × {_lotWorld.LotDepthMeters} M"));
                inspector.Add(Property("AREA",
                    $"{_lotWorld.LotWidthMeters * _lotWorld.LotDepthMeters:N0} M²"));
            }
            if (!string.IsNullOrWhiteSpace(_lotStatus))
                inspector.Add(StyledLabel(_lotStatus, "status-note"));
            screen.Add(inspector);

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
            // Catalog placement and deletion rebuild this screen after the
            // world has already framed itself. Refit once the replacement
            // panel has completed layout so the player's real Lot Editor view
            // (rather than an isolated asset preview) is authoritative.
            screen.schedule.Execute(() =>
            {
                if (_lotWorld != null && _lotWorld.gameObject.activeSelf)
                    _lotWorld.RefreshCameraFraming();
            }).ExecuteLater(1);
        }

        private void ShowLotContextMenu(VisualElement screen,
            Vector2 panelPosition, Vector2Int cell)
        {
            RemoveLotContextMenu();
            _lotContextMenu = new VisualElement { name = "lot-context-menu" };
            _lotContextMenu.AddToClassList("lot-context-menu");
            _lotContextMenu.style.position = Position.Absolute;
            _lotContextMenu.style.left = panelPosition.x;
            _lotContextMenu.style.top = panelPosition.y;
            _lotContextMenu.Add(CfButton.Create("↔  DELETE ROW", () =>
            {
                RemoveLotContextMenu();
                if (_lotWorld.DeleteMajorRow(cell.y))
                    _lotStatus = $"Deleted row {cell.y + 1} • lot is now " +
                        $"{_lotWorld.LotWidthCells} × {_lotWorld.LotDepthCells}";
                Show(AppScreen.LotEditor);
            }, _lotWorld.LotDepthCells > 1, "danger"));
            _lotContextMenu.Add(CfButton.Create("↕  DELETE COLUMN", () =>
            {
                RemoveLotContextMenu();
                if (_lotWorld.DeleteMajorColumn(cell.x))
                    _lotStatus = $"Deleted column {cell.x + 1} • lot is now " +
                        $"{_lotWorld.LotWidthCells} × {_lotWorld.LotDepthCells}";
                Show(AppScreen.LotEditor);
            }, _lotWorld.LotWidthCells > 1, "danger"));
            screen.Add(_lotContextMenu);
        }

        private void RemoveLotContextMenu()
        {
            _lotContextMenu?.RemoveFromHierarchy();
            _lotContextMenu = null;
        }

        private Button CategoryButton(
            LotEditorCategory category,
            string icon,
            string label)
        {
            var selected = _lotEditorCategoryExpanded && _lotEditorCategory == category;
            if (category == LotEditorCategory.Main)
            {
                var main = new Button(() => SetLotEditorCategory(category))
                {
                    name = "Main",
                    text = "⚙",
                    tooltip = "Main lot settings: name, dimensions, and type"
                };
                main.AddToClassList("cf-image-button");
                main.AddToClassList(selected
                    ? "cf-image-button--tool-category-selected"
                    : "cf-image-button--tool-category");
                main.AddToClassList("tool-category-main");
                return main;
            }
            if (category == LotEditorCategory.Environment)
            {
                var sun = new Button(() => SetLotEditorCategory(category))
                {
                    name = "Environment",
                    text = "☀",
                    tooltip = "Environment tools: time of day, lighting, and lot type"
                };
                sun.AddToClassList("cf-image-button");
                sun.AddToClassList(selected
                    ? "cf-image-button--tool-category-selected"
                    : "cf-image-button--tool-category");
                sun.AddToClassList("tool-category-sun");
                return sun;
            }
            if (category == LotEditorCategory.Buildings)
            {
                var house = CfImageButton.CreateWithTexture(
                    label,
                    CfImageButton.CreateHouseIcon(new Color(0.88f, 0.72f, 0.34f, 1f)),
                    () => SetLotEditorCategory(category),
                    true,
                    selected ? "tool-category-selected" : "tool-category");
                house.tooltip = "Buildings tools";
                var houseCaption = new Label(label.ToUpperInvariant())
                {
                    pickingMode = PickingMode.Ignore
                };
                houseCaption.AddToClassList("tool-category-caption");
                house.Add(houseCaption);
                return house;
            }
            var button = CfImageButton.Create(
                label,
                $"CityForgeV3/UI/LotEditorTools/{icon}",
                () => SetLotEditorCategory(category),
                true,
                selected ? "tool-category-selected" : "tool-category");
            button.tooltip = $"{label} tools";
            var caption = new Label(label.ToUpperInvariant())
            {
                pickingMode = PickingMode.Ignore
            };
            caption.AddToClassList("tool-category-caption");
            button.Add(caption);
            return button;
        }

        private void SetLotEditorCategory(LotEditorCategory category)
        {
            var nextExpanded = CategoryExpandedAfterClick(
                _lotEditorCategory, _lotEditorCategoryExpanded, category);
            if (!nextExpanded)
            {
                _lotEditorCategoryExpanded = false;
                _lotStatus = $"{category} tools collapsed";
                Show(AppScreen.LotEditor);
                return;
            }
            _lotEditorCategory = category;
            _lotEditorCategoryExpanded = true;
            _lotStatus = $"{category} tools opened";
            if (category == LotEditorCategory.Buildings)
            {
                // The primitive and hybrid modes are diagnostics. Entering
                // the authoring workspace must always show the actual art,
                // including for buildings restored from a saved lot.
                _lotWorld.SetInspectionMode(BuildingInspectionMode.Artwork);
            }
            Show(AppScreen.LotEditor);
            if (category == LotEditorCategory.Flora)
                OpenFloraModal();
            else if (category == LotEditorCategory.Props)
                OpenPropsModal();
            else if (category == LotEditorCategory.BuildingProps)
                OpenBuildingPropsModal();
            else if (category == LotEditorCategory.BaseTextures)
                OpenBaseTextureModal();
            else if (category == LotEditorCategory.OverlayTextures)
                OpenOverlayTextureModal();
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
            panel.Add(StyledLabel("URBAN SURFACES", "road-material-role"));
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

        private void OpenFloraModal()
        {
            var panel = CreateDocumentModal(
                "FLORA LIBRARY",
                "Choose a tree, then click anywhere inside the lot to plant it. These are the original City Forge tree artworks.");
            panel.AddToClassList("road-material-modal-panel");
            panel.Add(StyledLabel("DECIDUOUS TREES", "road-material-role"));
            var grid = new VisualElement();
            grid.AddToClassList("road-material-grid");
            foreach (var tree in new[]
                     {
                         (Id: "maple", Name: "Maple Tree"),
                         (Id: "ashe", Name: "Ashe Tree"),
                         (Id: "oak", Name: "Oak Tree")
                     })
            {
                var captured = tree;
                var card = new VisualElement();
                card.AddToClassList("road-material-card");
                var preview = new VisualElement();
                preview.AddToClassList("road-material-swatch");
                preview.style.backgroundImage = new StyleBackground(
                    Resources.Load<Texture2D>(
                        $"CityForgeV3/Flora/LegacyTreesV01/{tree.Id}-summer"));
                card.Add(preview);
                card.Add(CfButton.Create(tree.Name.ToUpperInvariant(), () =>
                {
                    _placementFloraId = captured.Id;
                    _lotWorld.SetFloraPlacementPreview(captured.Id);
                    _lotStatus = $"{captured.Name} selected • click the lot to plant";
                    RemoveDocumentModal();
                    ComposeLotEditor();
                }, true, _placementFloraId == tree.Id ? "mode-selected" : "quiet"));
                grid.Add(card);
            }
            panel.Add(grid);
            var actions = DocumentModalActions();
            actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
            panel.Add(actions);
        }

        private void OpenPropsModal()
        {
            var panel = CreateDocumentModal(
                "PROP LIBRARY",
                "Choose a prop, then click inside the lot to place it. Drag a selected prop to move it.");
            panel.AddToClassList("road-material-modal-panel");
            panel.Add(StyledLabel("PROP FAMILY", "road-material-role"));
            var families = new VisualElement();
            families.AddToClassList("inspector-actions");
            families.Add(CfButton.Create("FENCES & GATES", null,
                true, "mode-selected"));
            families.Add(CfButton.Create("STREET LIGHTING", null,
                true, "quiet"));
            families.Add(CfButton.Create("STORE SIGNS", null,
                false, "quiet"));
            panel.Add(families);
            panel.Add(StyledLabel("FENCES & GATES", "road-material-role"));
            var grid = new VisualElement();
            grid.AddToClassList("road-material-grid");
            void AddPropCard(VisualElement targetGrid, string label, string propId,
                string previewResource,
                string status)
            {
                var card = new VisualElement();
                card.AddToClassList("road-material-card");
                var preview = new VisualElement();
                preview.AddToClassList("road-material-swatch");
                preview.style.backgroundImage = new StyleBackground(
                    Resources.Load<Texture2D>(previewResource));
                card.Add(preview);
                card.Add(CfButton.Create(label, () =>
                {
                    _placementPropId = propId;
                    _lotWorld.SetPropPlacementPreview(_placementPropId);
                    _lotStatus = status;
                    RemoveDocumentModal();
                    ComposeLotEditor();
                }, true, _placementPropId == propId ? "mode-selected" : "quiet"));
                targetGrid.Add(card);
            }
            AddPropCard(grid, "STRAIGHT FENCE", "wrought-iron-fence-straight-v01",
                "CityForgeV3/Props/WroughtIronFenceV01/catalog-preview",
                "Straight wrought-iron fence selected • click the lot to place");
            AddPropCard(grid, "CORNER FENCE", "wrought-iron-fence-corner-v01",
                "CityForgeV3/Props/WroughtIronFenceV01/catalog-corner-preview",
                "Wrought-iron corner selected • click the lot to place");
            panel.Add(grid);
            panel.Add(StyledLabel(
                "STRAIGHT + 90° CORNER SECTIONS • OPTIMIZED 3D",
                "catalog-meta"));
            panel.Add(StyledLabel("STREET LIGHTING", "road-material-role"));
            var lightingGrid = new VisualElement();
            lightingGrid.AddToClassList("road-material-grid");
            AddPropCard(lightingGrid, "THREE-LANTERN LAMPPOST",
                "three-lantern-lamppost-v01",
                "CityForgeV3/Props/ThreeLanternLamppostV01/catalog-preview",
                "Three-lantern lamppost selected • click the lot to place • lights turn on at evening and night");
            panel.Add(lightingGrid);
            panel.Add(StyledLabel(
                "4.5 M HISTORIC COMMERCIAL LAMP • DAY OFF • EVENING + NIGHT ON",
                "catalog-meta"));
            panel.Add(StyledLabel(
                "STORE SIGNS WILL SUPPORT NIGHT EMISSION AND OPTIONAL ANIMATION STATES.",
                "catalog-meta"));
            var actions = DocumentModalActions();
            actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
            panel.Add(actions);
        }

        private void OpenBuildingPropsModal()
        {
            var panel = CreateDocumentModal(
                "BUILDING PROPS",
                "Choose an attachment, then move its translucent preview over a building facade and click to attach it. Attachments move and rotate with their host building.");
            panel.AddToClassList("building-prop-modal-panel");
            panel.Add(StyledLabel("WOODEN SIGNS", "road-material-role"));
            var grid = new VisualElement();
            grid.AddToClassList("building-prop-grid");
            foreach (var item in BuildingPropCatalog.Items)
            {
                var captured = item;
                var card = new VisualElement();
                card.AddToClassList("building-prop-card");
                var previewButton = new Button(() =>
                {
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
                    $"{captured.Revision.ToUpperInvariant()} • FRONT ELEVATION • BUILDING-OWNED",
                    "catalog-meta"));
                grid.Add(card);
            }
            panel.Add(grid);
            var actions = DocumentModalActions();
            actions.Add(CfButton.Create("DONE", RemoveDocumentModal, true, "quiet"));
            panel.Add(actions);
        }

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
            category == LotEditorCategory.OverlayTextures ||
            (category == LotEditorCategory.Flora &&
                !string.IsNullOrWhiteSpace(floraId)) ||
            (category == LotEditorCategory.Props &&
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
            _lotStatus = $"{_lotWorld.ZoomLevel} zoom";
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
                    "Hybrid view — artwork and spatial primitive",
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

        private void SetArtworkSource(BuildingArtworkSource source)
        {
            _lotWorld.SetArtworkSource(source);
            _lotStatus = source == BuildingArtworkSource.NeutralPilot
                ? "Game-lit neutral artwork selected — runtime time-of-day lighting active"
                : "Baked Blender lighting reference selected";
            Show(AppScreen.LotEditor);
        }

        private void PlaceGovernmentHouse()
        {
            _lotWorld.PlaceGovernmentHouseAtCenter();
            _lotStatus = "Government House placed at lot center";
            Show(AppScreen.LotEditor);
        }

        private void PlaceNewEnglandHouse()
        {
            _lotStatus = _lotWorld.PlaceBuildingAtCenter(BuildingCatalog.NewEnglandHouseId)
                ? "New England House 1720 added to the nearest open site"
                : "No open site remains • move or delete a building first";
            Show(AppScreen.LotEditor);
        }

        private void PlaceBuilding(BuildingCatalogEntry entry)
        {
            _lotStatus = _lotWorld.PlaceBuildingAtCenter(entry.Id)
                ? $"{entry.Name} added • {_lotWorld.BuildingCount} buildings on this lot"
                : "No open site remains • move or delete a building first";
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
            _lotWorld.PanCameraViewport(horizontal, vertical);
            _lotStatus = "Lot view panned";
            Show(AppScreen.LotEditor);
        }

        private void MoveCategorySelectionOrPan(int horizontal, int vertical)
        {
            if (_lotWorld.ActiveObjectSelection == LotObjectSelectionKind.Prop &&
                _lotWorld.SelectedPropIndex >= 0)
            {
                _lotStatus = _lotWorld.NudgeSelectedPropByScreenPixels(horizontal, vertical)
                    ? "Prop nudged one pixel"
                    : "Prop cannot move farther in that direction";
                Show(AppScreen.LotEditor);
                return;
            }
            if (_lotWorld.ActiveObjectSelection == LotObjectSelectionKind.Building &&
                _lotWorld.IsSelected)
            {
                Nudge(horizontal, vertical);
                return;
            }
            switch (_lotEditorCategory)
            {
                case LotEditorCategory.Buildings when _lotWorld.IsSelected:
                    Nudge(horizontal, vertical);
                    break;
                case LotEditorCategory.Roads when _lotWorld.RoadCursorSelected:
                    NudgeRoad(horizontal, vertical);
                    break;
                case LotEditorCategory.Paths when _lotWorld.CirculationCursorSelected:
                    NudgeCirculation(horizontal, vertical);
                    break;
                case LotEditorCategory.Props when _lotWorld.SelectedPropIndex >= 0:
                    _lotStatus = _lotWorld.NudgeSelectedPropByScreenPixels(
                        horizontal, vertical)
                        ? "Prop nudged one pixel"
                        : "Prop cannot move farther in that direction";
                    Show(AppScreen.LotEditor);
                    break;
                default:
                    PanLot(horizontal, vertical);
                    break;
            }
        }

        private void DeselectAll()
        {
            _placementFloraId = "";
            _placementPropId = "";
            _placementBuildingPropId = "";
            _placementOverlayTextureId = "";
            _lotWorld.SetBuildingPropPlacementPreview("");
            _lotWorld.DeselectAll();
            _lotStatus = "Selection cleared — arrows pan the lot";
            Show(AppScreen.LotEditor);
        }

        private bool DeleteActiveSelection()
        {
            switch (_lotWorld.ActiveObjectSelection)
            {
                case LotObjectSelectionKind.BuildingProp:
                    _lotStatus = _lotWorld.DeleteSelectedBuildingProp()
                        ? "Building prop deleted"
                        : "Select a building prop before deleting";
                    Show(AppScreen.LotEditor);
                    return true;
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
                ? "Fence rotated 90°"
                : "Fence cannot rotate here";
            Show(AppScreen.LotEditor);
        }

        private void DeleteSelectedProp()
        {
            _lotStatus = _lotWorld.DeleteSelectedProp()
                ? "Fence removed"
                : "No fence selected";
            Show(AppScreen.LotEditor);
        }

        private void DeleteSelectedOverlayTexture()
        {
            _lotStatus = _lotWorld.DeleteSelectedOverlayTexture()
                ? "Overlay tile removed"
                : "No overlay tile selected";
            Show(AppScreen.LotEditor);
        }

        private static string FloraDisplayName(string id) => id switch
        {
            "maple" => "Maple Tree",
            "ashe" => "Ashe Tree",
            "oak" => "Oak Tree",
            _ => id ?? ""
        };

        private void RotateBuilding(int direction)
        {
            _lotWorld.RotateSelected(direction);
            var orientation = _lotWorld.BuildingCardinalOrientation;
            _lotStatus = direction > 0
                ? $"Building rotated clockwise — facing {orientation}"
                : $"Building rotated counter-clockwise — facing {orientation}";
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
                new List<string> { "Residential", "Commercial", "Industrial", "Mixed", "Transportation" },
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
            if (saves.Count > 0)
            {
                var list = new ScrollView();
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

        private static Foldout RoadFoldout(string title, bool expanded,
            Action<bool> onChanged)
        {
            var foldout = new Foldout { text = title, value = expanded };
            foldout.AddToClassList("road-inspector-section");
            foldout.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            return foldout;
        }

        private VisualElement CreateDocumentModal(string title, string copy)
        {
            RemoveDocumentModal();
            var overlay = new VisualElement { name = "document-modal" };
            overlay.AddToClassList("document-modal");
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
            _ => "Transportation"
        };

        private sealed class CityForgeChoiceField : VisualElement
        {
            private readonly List<string> _choices;
            private readonly DropdownField _field;

            public int index => Mathf.Max(0, _choices.IndexOf(_field.value));
            public string value => _field.value;

            public CityForgeChoiceField(
                VisualElement overlayHost,
                string label,
                List<string> choices,
                int selectedIndex)
            {
                _choices = choices != null && choices.Count > 0
                    ? choices
                    : new List<string> { "None" };
                var index = Mathf.Clamp(selectedIndex, 0, _choices.Count - 1);
                AddToClassList("document-field");
                AddToClassList("cf-choice-field");

                var fieldLabel = StyledLabel(label, "cf-choice-label");
                Add(fieldLabel);

                _field = new DropdownField(_choices, index);
                _field.AddToClassList("cf-choice-display");
                Add(_field);
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
