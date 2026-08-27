using CityForgeV3.UI;
using CityForgeV3.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class NyResidentialBuildingFrontQaMenu
{
    private const string LotId = "ny-residential";
    private const string PendingKey =
        "CityForge.NyResidentialBuildingFrontQa.Pending";
    private const string MenuRoot =
        "City Forge/QA/NY Residential Building Front/";
    private const string BrownstoneId =
        "cityforge.v3.residential.ny_brownstone_tripo_01";
    private const string BayWindowsId =
        "cityforge.v3.residential.ny_brownstone_bay_windows_tripo_01";
    private const string FancyTownhouseId =
        "cityforge.v3.residential.ny_fancy_townhouse_tripo_01";
    private const float PositionTolerance = 0.0001f;
    private const float RotationToleranceDegrees = 0.001f;

    private static readonly string OutputDirectory = Path.GetFullPath(
        Path.Combine(Application.dataPath,
            "../QA/BuildingFrontMarkerOcclusionTolerance"));
    private static readonly string ReportPath = Path.Combine(
        OutputDirectory, "ny-residential-building-front-report.json");

    private static bool _running;
    private static int _targetGameFrame;
    private static int _waitPollCount;
    private static Action _pendingAction;
    private static ProbeReport _report;
    private static CityForgeApp _app;
    private static LotWorldController _world;
    private static Camera _camera;
    private static int _brownstoneIndex = -1;
    private static LotWorldController.CameraFramingState _focusedCamera;
    private static Vector3 _focusedPan;
    private static int _focusedApplyCount;
    private static SpriteRenderer _hostSpecificTreeRenderer;
    private static Material _hostSpecificProductionMaterial;
    private static Material _hostSpecificForcedAlwaysMaterial;

    static NyResidentialBuildingFrontQaMenu()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem(MenuRoot + "Run Deterministic Windowed Probe")]
    public static void RunDeterministicWindowedProbe()
    {
        if (_running)
        {
            Debug.LogWarning("The NY Residential building-front probe is already running.");
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
            return;
        }

        StartProbe();
    }

    [MenuItem(MenuRoot + "Open Exact Fixture Only")]
    public static void OpenExactFixtureOnly()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("Enter Play Mode before opening the exact NY Residential fixture.");
            return;
        }

        OpenAndConfigureFixture(false);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            StopWaiting();
            _running = false;
            return;
        }

        if (state != PlayModeStateChange.EnteredPlayMode ||
            !SessionState.GetBool(PendingKey, false)) return;

        SessionState.SetBool(PendingKey, false);
        WaitForGameFrames(3, StartProbe);
    }

    private static void StartProbe()
    {
        if (_running) return;
        _running = true;
        _report = new ProbeReport
        {
            utcTimestamp = DateTime.UtcNow.ToString("O"),
            outputDirectory = OutputDirectory,
            reportPath = ReportPath,
            artworkScreenshot = Path.Combine(
                OutputDirectory, "01-artwork-all-three-street-trees.png"),
            topDownFrontArrowScreenshot = Path.Combine(
                OutputDirectory, "02-top-down-brownstone-front-arrow.png"),
            sideBackControlScreenshot = Path.Combine(
                OutputDirectory, "03-side-back-ordinary-depth-control.png"),
            hostSpecificNoTreeScreenshot = Path.Combine(
                OutputDirectory, "04a-host-specific-no-tree-baseline.png"),
            hostSpecificProductionScreenshot = Path.Combine(
                OutputDirectory, "04b-host-specific-production.png"),
            hostSpecificForcedAlwaysScreenshot = Path.Combine(
                OutputDirectory, "04c-host-specific-forced-always-diagnostic.png"),
            unityWindowed = !Application.isBatchMode,
            gameViewCaptureContract =
                "ScreenCapture.CaptureScreenshot from a normal windowed Unity " +
                "Editor Play Mode Game View; no batchmode, nographics, standalone, " +
                "fullscreen, target texture, or synthetic scene."
        };

        Directory.CreateDirectory(OutputDirectory);
        DeleteCaptureIfPresent(_report.artworkScreenshot);
        DeleteCaptureIfPresent(_report.topDownFrontArrowScreenshot);
        DeleteCaptureIfPresent(_report.sideBackControlScreenshot);
        DeleteCaptureIfPresent(_report.hostSpecificNoTreeScreenshot);
        DeleteCaptureIfPresent(_report.hostSpecificProductionScreenshot);
        DeleteCaptureIfPresent(_report.hostSpecificForcedAlwaysScreenshot);
        if (Application.isBatchMode)
        {
            Fail("This visual probe must run in a normal windowed Unity Editor.");
            return;
        }

        Guarded(() => OpenAndConfigureFixture(true));
    }

    private static void OpenAndConfigureFixture(bool continueProbe)
    {
        _app = UnityEngine.Object.FindFirstObjectByType<CityForgeApp>();
        if (_app == null)
        {
            Fail("CityForgeApp was not available in Play Mode.");
            return;
        }

        if (!_app.OpenSavedLotBuildingFocusQa(LotId))
        {
            Fail($"CityForgeApp could not load the saved lot '{LotId}'.");
            return;
        }

        _world = UnityEngine.Object.FindFirstObjectByType<LotWorldController>();
        _camera = _world == null
            ? null
            : _world.GetComponentInChildren<Camera>(true);
        if (_world == null || _camera == null)
        {
            Fail("The real LotWorldController or its camera was not available.");
            return;
        }

        if (!string.Equals(_world.CurrentLotId, LotId,
                StringComparison.OrdinalIgnoreCase))
        {
            Fail($"Expected lot id '{LotId}', got '{_world.CurrentLotId}'.");
            return;
        }

        _world.SetInspectionMode(BuildingInspectionMode.Artwork);
        _world.SetZoomLevel(LotZoomLevel.Detail);
        _world.SetQaCameraPan(-13f, 1f);
        _world.SetQaOrthographicSize(17f);

        if (!continueProbe)
        {
            Debug.Log("Opened the exact saved NY Residential front-occlusion fixture.");
            return;
        }

        WaitForGameFrames(5, ValidateFixtureAndCaptureArtwork);
    }

    private static void ValidateFixtureAndCaptureArtwork()
    {
        var data = _world.Session.Data;
        _report.lotId = data.LotId;
        _report.lotName = data.Name;
        _report.lotWidthCells = data.LotWidthCells;
        _report.lotDepthCells = data.LotDepthCells;
        _report.buildingCount = _world.BuildingCount;
        _report.floraCount = _world.FloraCount;
        _report.propCount = _world.PropCount;

        ValidateExactFixture(data);
        RecordArtworkCamera();
        RecordFloraRenderers("saved-front", 0, 3, _report.savedFrontTrees);
        if (_report.savedFrontTrees.Count != 3)
            AddIssue($"Expected three saved street-tree renderers, got " +
                     $"{_report.savedFrontTrees.Count}.");
        foreach (var tree in _report.savedFrontTrees)
        {
            var expectedHostIndex = tree.index;
            LotWorldController.TryBuildingOcclusionStencilReference(
                expectedHostIndex, out var expectedStencilReference);
            tree.expectedHostBuildingIndex = expectedHostIndex;
            tree.expectedHostStencilReference = expectedStencilReference;
            if (!tree.hostRecoveryMaterial)
                AddIssue($"Saved tree {tree.index} did not receive the " +
                         "host-specific front-facade recovery material.");
            if (!Mathf.Approximately(tree.zTest,
                    (float)CompareFunction.LessEqual))
                AddIssue($"Saved tree {tree.index} host-recovery base pass " +
                         $"used ZTest {tree.zTest}, not LEqual.");
            if (!Mathf.Approximately(tree.buildingHostStencilReference,
                    expectedStencilReference))
                AddIssue($"Saved tree {tree.index} used host stencil " +
                         $"{tree.buildingHostStencilReference}, expected " +
                         $"{expectedStencilReference} for building " +
                         $"{expectedHostIndex}.");
            if (tree.materialHasViewDepthBiasProperty ||
                Mathf.Abs(tree.propertyBlockViewDepthBiasMeters) > 0.0001f)
                AddIssue($"Saved tree {tree.index} retained the removed scalar " +
                         "view-depth-bias path.");
        }

        _report.artworkInspectionMode = _world.InspectionMode.ToString();
        _report.artworkTopDown = _world.TopDownViewEnabled;
        _report.artworkSelected = _world.ActiveObjectSelection.ToString();
        CaptureGameView(_report.artworkScreenshot);
        WaitForGameFrames(3, SelectBrownstoneAndProbeFocusedDrag);
    }

    private static void RecordArtworkCamera()
    {
        _report.cameraPositionWorld = _camera.transform.position;
        _report.cameraPositionLocal = _world.transform.InverseTransformPoint(
            _camera.transform.position);
        _report.cameraRotationWorldEuler = _camera.transform.eulerAngles;
        _report.cameraForwardWorld = _camera.transform.forward;
        _report.cameraForwardLocal = _world.transform.InverseTransformDirection(
            _camera.transform.forward);
        _report.cameraOrthographic = _camera.orthographic;
        _report.cameraOrthographicSize = _camera.orthographicSize;
        _report.cameraPixelSize = new Vector2Int(
            _camera.pixelWidth, _camera.pixelHeight);
    }

    private static void ValidateExactFixture(LotSaveData data)
    {
        var buildings = data.Buildings ?? new List<PlacedBuilding>();
        var flora = data.Flora ?? new List<PlacedFlora>();
        _brownstoneIndex = FindBuilding(buildings, BrownstoneId, -20f, 5f);
        var bayIndex = FindBuilding(buildings, BayWindowsId, -13f, 1f);
        var fancyIndex = FindBuilding(buildings, FancyTownhouseId, -6f, 1f);
        _report.exactBuildingFixture = buildings.Count == 3 &&
            _brownstoneIndex == 0 && bayIndex == 1 && fancyIndex == 2;
        _report.exactFloraFixture = flora.Count == 3 &&
            MatchesFlora(flora[0], "ee210a7745cb4b30a7fad59bd9489fe9",
                -20.435188f, -6.456524f) &&
            MatchesFlora(flora[1], "1714dc9aa5374834be4b2a1f501c7cdd",
                -12.664310f, -7.164764f) &&
            MatchesFlora(flora[2], "b71c8edd1f52450ead4fb232053836b4",
                -8.542774f, -4.711983f);
        if (!_report.exactBuildingFixture)
            AddIssue("The three-building NY Residential fixture identity changed.");
        if (!_report.exactFloraFixture)
            AddIssue("The three-tree NY Residential fixture identity changed.");
    }

    private static int FindBuilding(IReadOnlyList<PlacedBuilding> buildings,
        string buildingId, float x, float z)
    {
        for (var index = 0; index < buildings.Count; index++)
        {
            var building = buildings[index];
            if (string.Equals(building.BuildingId, buildingId,
                    StringComparison.OrdinalIgnoreCase) &&
                Mathf.Abs(building.CellX - x) <= 0.001f &&
                Mathf.Abs(building.CellZ - z) <= 0.001f &&
                building.RotationQuarterTurns == 0)
                return index;
        }
        return -1;
    }

    private static bool MatchesFlora(PlacedFlora flora, string instanceId,
        float x, float z) => flora != null &&
        string.Equals(flora.InstanceId, instanceId,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(flora.FloraId, "narrow-street-tree",
            StringComparison.OrdinalIgnoreCase) &&
        Mathf.Abs(flora.PositionX - x) <= 0.001f &&
        Mathf.Abs(flora.PositionZ - z) <= 0.001f;

    private static void SelectBrownstoneAndProbeFocusedDrag()
    {
        if (_brownstoneIndex < 0)
        {
            Fail("The exact Brownstone target was unavailable.");
            return;
        }

        var data = _world.Session.Data.Buildings[_brownstoneIndex];
        var origin = new Vector3(data.CellX, 0f, data.CellZ);
        var panelSize = CameraPanelSize(_camera);
        var originPanel = WorldToPanelPoint(_camera, origin, panelSize);
        _report.selectionAccepted =
            _world.BeginBuildingDragFromPanel(originPanel, panelSize);
        _report.selectedBuildingIndex = _world.SelectedBuildingIndex;
        if (!_report.selectionAccepted ||
            _world.SelectedBuildingIndex != _brownstoneIndex)
        {
            Fail("The real panel-space building selection did not select the Brownstone.");
            return;
        }

        _app.RefreshBuildingFocusViewForQa();
        WaitForGameFrames(3, ExerciseFocusedDragWithoutRebuild);
    }

    private static void ExerciseFocusedDragWithoutRebuild()
    {
        var data = _world.Session.Data.Buildings[_brownstoneIndex];
        var origin = new Vector3(data.CellX, 0f, data.CellZ);
        var panelSize = CameraPanelSize(_camera);
        _focusedCamera = _world.CaptureCameraFraming();
        _focusedPan = _world.CameraPanWorld;
        _focusedApplyCount = _world.SessionStateApplyCountForQa;

        var outward = origin + new Vector3(0.6f, 0f, -0.4f);
        _report.focusedMoveOutAccepted = _world.DragBuildingFromPanel(
            WorldToPanelPoint(_camera, outward, panelSize), panelSize);
        _report.focusedMoveBackAccepted = _world.DragBuildingFromPanel(
            WorldToPanelPoint(_camera, origin, panelSize), panelSize);
        _report.focusedReleaseAccepted = _world.EndBuildingDrag();
        _report.applyCountAfterFocusedDrag =
            _world.SessionStateApplyCountForQa;
        _report.focusedDragApplyDelta =
            _report.applyCountAfterFocusedDrag - _focusedApplyCount;
        _report.focusedCameraInvariant = CameraMatches(
            _focusedCamera, _focusedPan);
        var returned = _world.BuildingPresentationPosition(_brownstoneIndex);
        _report.focusedBuildingReturnedToOrigin =
            Vector3.Distance(returned, origin) <= PositionTolerance;

        if (!_report.focusedMoveOutAccepted || !_report.focusedMoveBackAccepted ||
            !_report.focusedReleaseAccepted)
            AddIssue("The selected Brownstone did not accept the focused move/release route.");
        if (_report.focusedDragApplyDelta != 0)
            AddIssue($"ApplySessionState ran during focused drag/release: " +
                     $"delta={_report.focusedDragApplyDelta}.");
        if (!_report.focusedCameraInvariant)
            AddIssue("Camera framing changed during focused drag/release.");
        if (!_report.focusedBuildingReturnedToOrigin)
            AddIssue("The Brownstone did not return to its exact saved origin.");

        _world.ToggleTopDownView();
        _world.SetQaCameraPan(-20f, 4f);
        _world.SetQaOrthographicSize(12f);
        WaitForGameFrames(5, CaptureTopDownFrontArrow);
    }

    private static void CaptureTopDownFrontArrow()
    {
        _report.topDownEnabled = _world.TopDownViewEnabled;
        _report.frontMarkerVisibleWhileSelected =
            _world.SelectedBuildingFrontMarkerVisible;
        _report.markerDirection = _world.SelectedBuildingFrontDirection;
        _report.expectedMarkerDirection = Vector3.back;
        _report.markerDirectionDot = Vector3.Dot(
            _report.markerDirection.normalized,
            _report.expectedMarkerDirection);
        _report.markerPointsTowardSavedStreetTrees =
            _report.markerDirectionDot >= 0.999f;
        _report.applyCountAtSelectedTopDown =
            _world.SessionStateApplyCountForQa;

        if (!_report.topDownEnabled)
            AddIssue("Top-down mode was not enabled for the marker capture.");
        if (!_report.frontMarkerVisibleWhileSelected)
            AddIssue("The selected Brownstone front marker was not visible in top-down mode.");
        if (!_report.markerPointsTowardSavedStreetTrees)
            AddIssue($"The Brownstone marker pointed {_report.markerDirection}, " +
                     "not toward the -Z street/tree row.");

        CaptureGameView(_report.topDownFrontArrowScreenshot);
        WaitForGameFrames(4, ProbeDeselectThenControls);
    }

    private static void ProbeDeselectThenControls()
    {
        var cameraBeforeDeselect = _world.CaptureCameraFraming();
        var panBeforeDeselect = _world.CameraPanWorld;
        var applyBeforeDeselect = _world.SessionStateApplyCountForQa;
        _world.DeselectAll();
        _report.frontMarkerVisibleAfterDeselect =
            _world.SelectedBuildingFrontMarkerVisible;
        _report.applyCountAfterDeselect =
            _world.SessionStateApplyCountForQa;
        _report.deselectApplyDelta =
            _report.applyCountAfterDeselect - applyBeforeDeselect;
        _report.deselectCameraInvariant = CameraMatches(
            cameraBeforeDeselect, panBeforeDeselect);
        if (_report.frontMarkerVisibleAfterDeselect)
            AddIssue("The authored-front marker remained visible after deselect.");
        if (_report.deselectApplyDelta != 1)
            AddIssue($"Deselect must reconcile exactly once: " +
                     $"delta={_report.deselectApplyDelta}.");
        if (!_report.deselectCameraInvariant)
            AddIssue("Camera framing changed during deselect reconciliation.");

        _world.ToggleTopDownView();
        if (!_world.LoadLot(LotId))
        {
            Fail("Could not reload the exact saved fixture for side/back controls.");
            return;
        }
        _world.SetInspectionMode(BuildingInspectionMode.Artwork);
        _world.SetZoomLevel(LotZoomLevel.Detail);
        // The normal artwork camera's ground-plane center is offset from the
        // pan anchor by its authored composition.  This anchor centers the
        // Brownstone plus the side/back control probes in the real Game View.
        _world.SetQaCameraPan(-6f, 3f);
        _world.SetQaOrthographicSize(17f);
        if (!_world.PlaceFloraForQa("narrow-street-tree", -24.6f, 5f))
            AddIssue("Could not place the transient Brownstone side control tree.");
        if (!_world.PlaceFloraForQa("narrow-street-tree", -20f, 16f))
            AddIssue("Could not place the transient Brownstone back control tree.");
        WaitForGameFrames(5, CaptureSideBackControls);
    }

    private static void CaptureSideBackControls()
    {
        RecordFloraRenderers("side-back-control", 3, 2,
            _report.sideBackControlTrees);
        if (_report.sideBackControlTrees.Count != 2)
            AddIssue($"Expected two transient side/back control renderers, got " +
                     $"{_report.sideBackControlTrees.Count}.");
        foreach (var tree in _report.sideBackControlTrees)
        {
            if (tree.hostRecoveryMaterial)
                AddIssue($"Side/back control tree {tree.index} incorrectly " +
                         "received a host-recovery material.");
            if (!Mathf.Approximately(tree.zTest,
                    (float)CompareFunction.LessEqual))
                AddIssue($"Side/back control tree {tree.index} used ZTest " +
                         $"{tree.zTest}, not LEqual.");
            if (tree.buildingHostStencilReference >= 0f)
                AddIssue($"Side/back control tree {tree.index} unexpectedly " +
                         $"declared host stencil " +
                         $"{tree.buildingHostStencilReference}.");
            if (Mathf.Abs(tree.propertyBlockViewDepthBiasMeters) > 0.0001f)
                AddIssue($"Side/back control tree {tree.index} received nonzero " +
                         $"view-depth bias {tree.propertyBlockViewDepthBiasMeters}.");
        }

        CaptureGameView(_report.sideBackControlScreenshot);
        WaitForGameFrames(4, SetupHostSpecificOcclusionRegression);
    }

    private static void SetupHostSpecificOcclusionRegression()
    {
        if (!_world.LoadLot(LotId))
        {
            Fail("Could not reload the exact saved fixture for the host-specific regression.");
            return;
        }

        var data = _world.Session.Data;
        ValidateExactFixture(data);
        if (_brownstoneIndex != 0 || data.Buildings == null ||
            data.Buildings.Count != 3)
        {
            Fail("The exact three-building fixture was unavailable for the host-specific regression.");
            return;
        }

        // QA-only transient arrangement.  The first saved tree remains in the
        // Brownstone's authored front apron.  The bay-window building is moved
        // out of frame, while the unrelated Fancy Townhouse is placed closer
        // to the orthographic camera so its side/facade overlaps only part of
        // that tree's canopy.  This state is never saved back to the lot file.
        var bay = data.Buildings[1];
        var unrelated = data.Buildings[2];
        _report.hostSpecificHostBuildingIndex = 0;
        _report.hostSpecificUnrelatedBuildingIndex = 2;
        _report.hostSpecificTreeIndex = 0;
        _report.hostSpecificHostBuildingPosition = new Vector2(
            data.Buildings[0].CellX, data.Buildings[0].CellZ);
        _report.hostSpecificUnrelatedOriginalPosition = new Vector2(
            unrelated.CellX, unrelated.CellZ);
        _report.hostSpecificUnrelatedTransientPosition =
            new Vector2(-9.5f, -11f);
        _report.hostSpecificUnrelatedTransientRotationQuarterTurns = 0;
        bay.CellX = 40f;
        bay.CellZ = 40f;
        unrelated.CellX = _report.hostSpecificUnrelatedTransientPosition.x;
        unrelated.CellZ = _report.hostSpecificUnrelatedTransientPosition.y;
        unrelated.RotationQuarterTurns =
            _report.hostSpecificUnrelatedTransientRotationQuarterTurns;

        var applyBefore = _world.SessionStateApplyCountForQa;
        var applyMethod = typeof(LotWorldController).GetMethod(
            "ApplySessionState",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (applyMethod == null)
        {
            Fail("Could not invoke the private ApplySessionState method for the transient QA arrangement.");
            return;
        }
        applyMethod.Invoke(_world, null);
        _report.hostSpecificTransientApplyDelta =
            _world.SessionStateApplyCountForQa - applyBefore;
        if (_report.hostSpecificTransientApplyDelta != 1)
            AddIssue($"Transient host-specific arrangement rebuilt " +
                     $"{_report.hostSpecificTransientApplyDelta} times, not once.");

        // The preceding selected-building probe deliberately leaves a focused
        // highlight behind.  Remove it before the pixel-controlled captures so
        // 04a/04b/04c differ only by the deterministic tree renderer/material,
        // never by a settling selection outline.
        _world.DeselectAll();

        _world.SetInspectionMode(BuildingInspectionMode.Artwork);
        _world.SetZoomLevel(LotZoomLevel.Detail);
        _world.SetQaCameraPan(-2f, -9f);
        _world.SetQaOrthographicSize(14f);
        _report.hostSpecificCameraPan = _world.CameraPanWorld;
        _report.hostSpecificOrthographicSize = _camera.orthographicSize;

        var floraRoot = _world.transform.Find("Placed Flora");
        if (floraRoot == null || floraRoot.childCount < 3)
        {
            Fail("The saved three-tree hierarchy was unavailable for the host-specific regression.");
            return;
        }
        for (var index = 1; index < 3; index++)
            floraRoot.GetChild(index).gameObject.SetActive(false);
        _hostSpecificTreeRenderer = floraRoot.GetChild(0)
            .GetComponentInChildren<SpriteRenderer>(true);
        if (_hostSpecificTreeRenderer == null)
        {
            Fail("The deterministic host-qualified tree had no SpriteRenderer.");
            return;
        }

        _hostSpecificProductionMaterial =
            _hostSpecificTreeRenderer.sharedMaterial;
        _report.hostSpecificProductionMaterialName =
            _hostSpecificProductionMaterial == null
                ? ""
                : _hostSpecificProductionMaterial.name;
        _report.hostSpecificProductionShaderName =
            _hostSpecificProductionMaterial?.shader == null
                ? ""
                : _hostSpecificProductionMaterial.shader.name;
        _report.hostSpecificProductionZTest =
            _hostSpecificProductionMaterial != null &&
            _hostSpecificProductionMaterial.HasProperty("_ZTest")
                ? _hostSpecificProductionMaterial.GetFloat("_ZTest")
                : -1f;
        _report.hostSpecificProductionStencilReference =
            _hostSpecificProductionMaterial != null &&
            _hostSpecificProductionMaterial.HasProperty(
                "_BuildingHostStencilRef")
                ? _hostSpecificProductionMaterial.GetFloat(
                    "_BuildingHostStencilRef")
                : -1f;

        RecordFloraRenderers("host-specific-overlap", 0, 1,
            _report.hostSpecificTree);
        if (_report.hostSpecificTree.Count != 1)
        {
            Fail("The deterministic host-specific tree probe was not recorded.");
            return;
        }
        var tree = _report.hostSpecificTree[0];
        LotWorldController.TryBuildingOcclusionStencilReference(
            _report.hostSpecificHostBuildingIndex,
            out var expectedHostStencilReference);
        tree.expectedHostBuildingIndex =
            _report.hostSpecificHostBuildingIndex;
        tree.expectedHostStencilReference = expectedHostStencilReference;
        if (!tree.hostRecoveryMaterial)
            AddIssue("The deterministic host-qualified tree did not use the host-recovery material.");
        if (!Mathf.Approximately(tree.zTest,
                (float)CompareFunction.LessEqual))
            AddIssue($"The deterministic host-qualified tree base pass used " +
                     $"ZTest {tree.zTest}, not LEqual.");
        if (!Mathf.Approximately(tree.buildingHostStencilReference,
                expectedHostStencilReference))
            AddIssue($"The deterministic host-qualified tree used stencil " +
                     $"{tree.buildingHostStencilReference}, expected " +
                     $"{expectedHostStencilReference} for Brownstone host 0.");
        _report.hostSpecificHostEligible =
            tree.buildingCandidates.Count > 0 &&
            tree.buildingCandidates[0].eligibleVisibleFrontApron;
        _report.hostSpecificUnrelatedEligible =
            tree.buildingCandidates.Count > 2 &&
            tree.buildingCandidates[2].eligibleVisibleFrontApron;
        _report.hostSpecificEligibleCandidateCount =
            tree.eligibleCandidateCount;
        if (!_report.hostSpecificHostEligible ||
            _report.hostSpecificEligibleCandidateCount != 1)
            AddIssue("The deterministic tree was not qualified exclusively by its Brownstone host facade.");
        if (_report.hostSpecificUnrelatedEligible)
            AddIssue("The nearer unrelated Fancy Townhouse incorrectly qualified as the tree's host facade.");

        var hostWorld = new Vector3(
            data.Buildings[0].CellX, 0f, data.Buildings[0].CellZ);
        var treeWorld = new Vector3(
            data.Flora[0].PositionX, 0f, data.Flora[0].PositionZ);
        var unrelatedWorld = new Vector3(
            unrelated.CellX, 0f, unrelated.CellZ);
        _report.hostSpecificHostCameraDepth =
            _camera.WorldToViewportPoint(hostWorld).z;
        _report.hostSpecificTreeCameraDepth =
            _camera.WorldToViewportPoint(treeWorld).z;
        _report.hostSpecificUnrelatedCameraDepth =
            _camera.WorldToViewportPoint(unrelatedWorld).z;
        _report.hostSpecificUnrelatedIsNearerThanTree =
            _report.hostSpecificUnrelatedCameraDepth <
            _report.hostSpecificTreeCameraDepth;
        if (!_report.hostSpecificUnrelatedIsNearerThanTree)
            AddIssue("The transient unrelated building was not geometrically nearer to the camera than the tree.");

        WaitForGameFrames(5, CaptureHostSpecificNoTreeBaseline);
    }

    private static void CaptureHostSpecificNoTreeBaseline()
    {
        var floraRoot = _world.transform.Find("Placed Flora");
        if (floraRoot == null || floraRoot.childCount < 3)
        {
            Fail("The settled saved three-tree hierarchy was unavailable for the host-specific capture.");
            return;
        }
        for (var index = 1; index < 3; index++)
            floraRoot.GetChild(index).gameObject.SetActive(false);
        _hostSpecificTreeRenderer = floraRoot.GetChild(0)
            .GetComponentInChildren<SpriteRenderer>(true);
        if (_hostSpecificTreeRenderer == null)
        {
            Fail("The settled deterministic host-qualified tree had no SpriteRenderer.");
            return;
        }
        _hostSpecificProductionMaterial =
            _hostSpecificTreeRenderer.sharedMaterial;
        _report.hostSpecificProductionMaterialName =
            _hostSpecificProductionMaterial == null
                ? ""
                : _hostSpecificProductionMaterial.name;
        _report.hostSpecificProductionShaderName =
            _hostSpecificProductionMaterial?.shader == null
                ? ""
                : _hostSpecificProductionMaterial.shader.name;
        _report.hostSpecificProductionZTest =
            _hostSpecificProductionMaterial != null &&
            _hostSpecificProductionMaterial.HasProperty("_ZTest")
                ? _hostSpecificProductionMaterial.GetFloat("_ZTest")
                : -1f;
        _report.hostSpecificProductionStencilReference =
            _hostSpecificProductionMaterial != null &&
            _hostSpecificProductionMaterial.HasProperty(
                "_BuildingHostStencilRef")
                ? _hostSpecificProductionMaterial.GetFloat(
                    "_BuildingHostStencilRef")
                : -1f;
        RecordHostSpecificCameraAndScreenRect();
        _hostSpecificTreeRenderer.enabled = false;
        CaptureGameView(_report.hostSpecificNoTreeScreenshot);
        WaitForGameFrames(4, CaptureHostSpecificProduction);
    }

    private static void CaptureHostSpecificProduction()
    {
        _hostSpecificTreeRenderer.enabled = true;
        _hostSpecificTreeRenderer.sharedMaterial =
            _hostSpecificProductionMaterial;
        CaptureGameView(_report.hostSpecificProductionScreenshot);
        WaitForGameFrames(4, CaptureHostSpecificForcedAlways);
    }

    private static void CaptureHostSpecificForcedAlways()
    {
        if (_hostSpecificProductionMaterial == null ||
            !_hostSpecificProductionMaterial.HasProperty("_ZTest"))
        {
            Fail("The production host-specific tree material has no _ZTest diagnostic control.");
            return;
        }
        _hostSpecificForcedAlwaysMaterial = new Material(
            _hostSpecificProductionMaterial)
        {
            name = "QA Host-Specific Forced Always Diagnostic"
        };
        _hostSpecificForcedAlwaysMaterial.SetFloat("_ZTest",
            (float)CompareFunction.Always);
        _hostSpecificTreeRenderer.sharedMaterial =
            _hostSpecificForcedAlwaysMaterial;
        CaptureGameView(_report.hostSpecificForcedAlwaysScreenshot);
        WaitForGameFrames(5, CompareHostSpecificCaptures);
    }

    private static void CompareHostSpecificCaptures()
    {
        _hostSpecificTreeRenderer.sharedMaterial =
            _hostSpecificProductionMaterial;
        if (_hostSpecificForcedAlwaysMaterial != null)
            UnityEngine.Object.Destroy(_hostSpecificForcedAlwaysMaterial);
        _hostSpecificForcedAlwaysMaterial = null;

        _report.hostSpecificProductionVisibleChangedPixels =
            CountChangedPixels(
                _report.hostSpecificNoTreeScreenshot,
                _report.hostSpecificProductionScreenshot,
                10, out var baselineCompared);
        _report.hostSpecificProductionVsForcedAlwaysChangedPixels =
            CountChangedPixels(
                _report.hostSpecificProductionScreenshot,
                _report.hostSpecificForcedAlwaysScreenshot,
                10, out var forcedCompared);
        _report.hostSpecificComparedPixels =
            Mathf.Min(baselineCompared, forcedCompared);
        _report.hostSpecificTreeVisibleOverHost =
            _report.hostSpecificProductionVisibleChangedPixels >= 500;
        _report.hostSpecificUnrelatedOcclusionRetained =
            _report.hostSpecificProductionVsForcedAlwaysChangedPixels >= 500;

        if (!_report.hostSpecificTreeVisibleOverHost)
            AddIssue("The host-qualified tree was not visibly restored over its Brownstone host facade.");
        if (!_report.hostSpecificUnrelatedOcclusionRetained)
            AddIssue("HOST-SPECIFIC REGRESSION: production matched the global forced-Always diagnostic; the nearer unrelated building did not retain occlusion over the host tree.");

        WaitForGameFrames(2, FinalizeProbe);
    }

    private static void RecordHostSpecificCameraAndScreenRect()
    {
        _report.hostSpecificCameraPositionWorld =
            _camera.transform.position;
        _report.hostSpecificCameraRotationWorldEuler =
            _camera.transform.eulerAngles;
        _report.hostSpecificCameraForwardWorld =
            _camera.transform.forward;
        var bounds = _hostSpecificTreeRenderer.bounds;
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (var x = -1; x <= 1; x += 2)
        for (var y = -1; y <= 1; y += 2)
        for (var z = -1; z <= 1; z += 2)
        {
            var world = bounds.center + Vector3.Scale(
                bounds.extents, new Vector3(x, y, z));
            var screen = _camera.WorldToScreenPoint(world);
            min = Vector2.Min(min, screen);
            max = Vector2.Max(max, screen);
        }
        _report.hostSpecificTreeProjectedScreenRect =
            Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static int CountChangedPixels(string firstPath,
        string secondPath, int channelThreshold, out int comparedPixels)
    {
        comparedPixels = 0;
        if (!File.Exists(firstPath) || !File.Exists(secondPath))
        {
            AddIssue($"Could not compare missing captures '{firstPath}' and '{secondPath}'.");
            return 0;
        }

        var first = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var second = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(first, File.ReadAllBytes(firstPath),
                    false) ||
                !ImageConversion.LoadImage(second, File.ReadAllBytes(secondPath),
                    false) ||
                first.width != second.width || first.height != second.height)
            {
                AddIssue("Host-specific capture dimensions could not be compared.");
                return 0;
            }
            var firstPixels = first.GetPixels32();
            var secondPixels = second.GetPixels32();
            comparedPixels = firstPixels.Length;
            var changed = 0;
            for (var index = 0; index < firstPixels.Length; index++)
            {
                var a = firstPixels[index];
                var b = secondPixels[index];
                if (Mathf.Abs(a.r - b.r) >= channelThreshold ||
                    Mathf.Abs(a.g - b.g) >= channelThreshold ||
                    Mathf.Abs(a.b - b.b) >= channelThreshold ||
                    Mathf.Abs(a.a - b.a) >= channelThreshold)
                    changed++;
            }
            return changed;
        }
        finally
        {
            UnityEngine.Object.Destroy(first);
            UnityEngine.Object.Destroy(second);
        }
    }

    private static void RecordFloraRenderers(string stage, int firstIndex,
        int count, List<FloraRendererProbe> destination)
    {
        var floraRoot = _world.transform.Find("Placed Flora");
        if (floraRoot == null)
        {
            AddIssue($"Placed Flora hierarchy was missing during {stage}.");
            return;
        }

        var data = _world.Session.Data.Flora ?? new List<PlacedFlora>();
        for (var offset = 0; offset < count; offset++)
        {
            var index = firstIndex + offset;
            if (index >= data.Count || index >= floraRoot.childCount)
            {
                AddIssue($"Flora renderer {index} was missing during {stage}.");
                continue;
            }

            var renderer = floraRoot.GetChild(index)
                .GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null)
            {
                AddIssue($"Flora {index} had no SpriteRenderer during {stage}.");
                continue;
            }

            var material = renderer.sharedMaterial;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var materialBias = material != null &&
                material.HasProperty("_ViewDepthBiasMeters")
                    ? material.GetFloat("_ViewDepthBiasMeters")
                    : 0f;
            var propertyBias = block.GetFloat("_ViewDepthBiasMeters");
            var probe = new FloraRendererProbe
            {
                stage = stage,
                index = index,
                instanceId = data[index].InstanceId,
                floraId = data[index].FloraId,
                position = new Vector2(
                    data[index].PositionX, data[index].PositionZ),
                rendererName = renderer.name,
                spriteName = renderer.sprite == null
                    ? ""
                    : renderer.sprite.name,
                materialName = material == null ? "" : material.name,
                shaderName = material?.shader == null
                    ? ""
                    : material.shader.name,
                renderQueue = material == null ? -1 : material.renderQueue,
                zTest = material != null && material.HasProperty("_ZTest")
                    ? material.GetFloat("_ZTest")
                    : -1f,
                materialViewDepthBiasMeters = materialBias,
                propertyBlockViewDepthBiasMeters = propertyBias,
                materialHasViewDepthBiasProperty = material != null &&
                    material.HasProperty("_ViewDepthBiasMeters"),
                rendererColor = renderer.color,
                rendererEnabled = renderer.enabled,
                rendererActiveInHierarchy = renderer.gameObject.activeInHierarchy,
                rendererSortingLayer = renderer.sortingLayerName,
                rendererSortingOrder = renderer.sortingOrder,
                rendererBoundsCenter = renderer.bounds.center,
                rendererBoundsExtents = renderer.bounds.extents,
                rendererLossyScale = renderer.transform.lossyScale,
                materialColor = material != null && material.HasProperty("_Color")
                    ? material.GetColor("_Color")
                    : Color.clear,
                materialCutoff = material != null && material.HasProperty("_Cutoff")
                    ? material.GetFloat("_Cutoff")
                    : -1f,
                materialShadowFloor = material != null &&
                    material.HasProperty("_ShadowFloor")
                        ? material.GetFloat("_ShadowFloor")
                        : -1f,
                hasPropertyBlock = renderer.HasPropertyBlock(),
                propertyBlockColor = block.GetColor("_Color"),
                hostRecoveryMaterial = material != null &&
                    material.name.IndexOf("Host ",
                        StringComparison.OrdinalIgnoreCase) >= 0 &&
                    material.name.IndexOf(" Recovery",
                        StringComparison.OrdinalIgnoreCase) >= 0 &&
                    string.Equals(material.shader?.name,
                        "CityForgeV3/FrontFacadeLitShadowReceivingSprite",
                        StringComparison.Ordinal),
                buildingHostStencilReference = material != null &&
                    material.HasProperty("_BuildingHostStencilRef")
                        ? material.GetFloat("_BuildingHostStencilRef")
                        : -1f
            };
            if (renderer.sprite != null)
            {
                probe.spriteRect = renderer.sprite.rect;
                probe.spritePivot = renderer.sprite.pivot;
                probe.spritePixelsPerUnit = renderer.sprite.pixelsPerUnit;
            }
            RecordBuildingCandidates(probe, data[index]);
            destination.Add(probe);
        }
    }

    private static void RecordBuildingCandidates(FloraRendererProbe probe,
        PlacedFlora flora)
    {
        var buildings = _world.Session.Data.Buildings ??
            new List<PlacedBuilding>();
        var localTowardCameraDirection =
            _world.transform.InverseTransformDirection(
                -_camera.transform.forward);
        probe.localTowardCameraDirection = localTowardCameraDirection;
        probe.nearestEligibleCandidateDistanceSquared =
            float.PositiveInfinity;
        for (var buildingIndex = 0;
             buildingIndex < buildings.Count; buildingIndex++)
        {
            var building = buildings[buildingIndex];
            var catalog = BuildingCatalog.Find(building.BuildingId);
            var package = HybridBuildingPackageRegistry.Load(
                catalog.PackageResourcePath);
            var floraPosition = new Vector3(
                flora.PositionX, 0f, flora.PositionZ);
            var buildingPosition = new Vector3(
                building.CellX, 0f, building.CellZ);
            var eligible = LotWorldController.IsInVisibleBuildingFrontApron(
                floraPosition, buildingPosition, package,
                building.RotationQuarterTurns,
                localTowardCameraDirection);
            var rotation = Quaternion.Euler(
                0f, building.RotationQuarterTurns * 90f, 0f);
            var relative = Quaternion.Inverse(rotation) *
                (floraPosition - buildingPosition);
            var halfWidth = package.WidthMeters * 0.5f;
            var halfDepth = package.DepthMeters * 0.5f;
            var outsideX = Mathf.Max(Mathf.Abs(relative.x) - halfWidth, 0f);
            var outsideZ = Mathf.Max(Mathf.Abs(relative.z) - halfDepth, 0f);
            var distanceSquared = outsideX * outsideX + outsideZ * outsideZ;
            var candidate = new BuildingBiasCandidateProbe
            {
                buildingIndex = buildingIndex,
                buildingId = building.BuildingId,
                buildingPosition = buildingPosition,
                rotationQuarterTurns = building.RotationQuarterTurns,
                packageWidthMeters = package.WidthMeters,
                packageDepthMeters = package.DepthMeters,
                eligibleVisibleFrontApron = eligible,
                footprintOutsideDistanceSquared = distanceSquared
            };
            probe.buildingCandidates.Add(candidate);
            if (!eligible) continue;
            probe.eligibleCandidateCount++;
            if (distanceSquared < probe.nearestEligibleCandidateDistanceSquared)
            {
                probe.nearestEligibleCandidateDistanceSquared = distanceSquared;
                probe.nearestEligibleCandidateBuildingId = building.BuildingId;
            }
        }
        if (float.IsPositiveInfinity(
                probe.nearestEligibleCandidateDistanceSquared))
            probe.nearestEligibleCandidateDistanceSquared = -1f;
    }

    private static bool CameraMatches(
        LotWorldController.CameraFramingState baseline,
        Vector3 baselinePan)
    {
        var current = _world.CaptureCameraFraming();
        var positionDelta = Vector3.Distance(
            baseline.Position, current.Position);
        var rotationDelta = Quaternion.Angle(
            baseline.Rotation, current.Rotation);
        var sizeDelta = Mathf.Abs(
            baseline.OrthographicSize - current.OrthographicSize);
        var panDelta = Vector3.Distance(
            baselinePan, _world.CameraPanWorld);
        _report.maximumCameraPositionDelta = Mathf.Max(
            _report.maximumCameraPositionDelta, positionDelta);
        _report.maximumCameraRotationDeltaDegrees = Mathf.Max(
            _report.maximumCameraRotationDeltaDegrees, rotationDelta);
        _report.maximumOrthographicSizeDelta = Mathf.Max(
            _report.maximumOrthographicSizeDelta, sizeDelta);
        _report.maximumCameraPanDelta = Mathf.Max(
            _report.maximumCameraPanDelta, panDelta);
        return positionDelta <= PositionTolerance &&
            rotationDelta <= RotationToleranceDegrees &&
            sizeDelta <= PositionTolerance &&
            panDelta <= PositionTolerance;
    }

    private static Vector2 CameraPanelSize(Camera camera) => new(
        Mathf.Max(1, camera.pixelWidth),
        Mathf.Max(1, camera.pixelHeight));

    private static Vector2 WorldToPanelPoint(
        Camera camera, Vector3 worldPoint, Vector2 panelSize)
    {
        var pixel = camera.WorldToScreenPoint(worldPoint);
        return new Vector2(pixel.x, panelSize.y - pixel.y);
    }

    private static void CaptureGameView(string path)
    {
        ScreenCapture.CaptureScreenshot(path, 1);
    }

    private static void DeleteCaptureIfPresent(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            File.Delete(path);
    }

    private static void FinalizeProbe()
    {
        _report.screenshotsExist =
            File.Exists(_report.artworkScreenshot) &&
            File.Exists(_report.topDownFrontArrowScreenshot) &&
            File.Exists(_report.sideBackControlScreenshot) &&
            File.Exists(_report.hostSpecificNoTreeScreenshot) &&
            File.Exists(_report.hostSpecificProductionScreenshot) &&
            File.Exists(_report.hostSpecificForcedAlwaysScreenshot);
        if (!_report.screenshotsExist)
            AddIssue("One or more normal-windowed Game View screenshots were not written.");

        _report.automatedPassed = _report.issues.Count == 0 &&
            _report.unityWindowed &&
            _report.exactBuildingFixture &&
            _report.exactFloraFixture &&
            _report.savedFrontTrees.Count == 3 &&
            _report.sideBackControlTrees.Count == 2 &&
            _report.markerPointsTowardSavedStreetTrees &&
            _report.hostSpecificTreeVisibleOverHost &&
            _report.hostSpecificUnrelatedOcclusionRetained &&
            _report.screenshotsExist;
        _report.visualInspectionStatus =
            "PENDING_AGENT_INSPECTION_OF_ACTUAL_GAME_VIEW_PNGS";
        WriteReport();
        _running = false;
        Debug.Log(_report.automatedPassed
            ? $"NY Residential building-front automated QA passed; " +
              $"visual inspection still required. Report: {ReportPath}"
            : $"NY Residential building-front automated QA FAILED. " +
              $"Report: {ReportPath}");
    }

    private static void AddIssue(string issue)
    {
        if (_report == null || string.IsNullOrWhiteSpace(issue)) return;
        if (!_report.issues.Contains(issue))
            _report.issues.Add(issue);
    }

    private static void Fail(string issue)
    {
        if (_report == null)
        {
            _report = new ProbeReport
            {
                utcTimestamp = DateTime.UtcNow.ToString("O"),
                outputDirectory = OutputDirectory,
                reportPath = ReportPath
            };
        }
        AddIssue(issue);
        _report.automatedPassed = false;
        try
        {
            Directory.CreateDirectory(OutputDirectory);
            WriteReport();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        StopWaiting();
        _running = false;
        Debug.LogError($"NY Residential building-front QA failed: {issue}");
    }

    private static void WriteReport()
    {
        File.WriteAllText(ReportPath, JsonUtility.ToJson(_report, true));
    }

    private static void Guarded(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Fail(exception.ToString());
        }
    }

    private static void WaitForGameFrames(int frameCount, Action action)
    {
        StopWaiting();
        _targetGameFrame = Time.frameCount + Mathf.Max(1, frameCount);
        _waitPollCount = 0;
        _pendingAction = action;
        EditorApplication.update += PollForGameFrame;
    }

    private static void PollForGameFrame()
    {
        _waitPollCount++;
        if (!EditorApplication.isPlaying)
        {
            StopWaiting();
            return;
        }
        if (Time.frameCount < _targetGameFrame && _waitPollCount < 1200)
            return;

        var action = _pendingAction;
        StopWaiting();
        Guarded(action);
    }

    private static void StopWaiting()
    {
        EditorApplication.update -= PollForGameFrame;
        _pendingAction = null;
        _waitPollCount = 0;
    }

    [Serializable]
    private sealed class FloraRendererProbe
    {
        public string stage;
        public int index;
        public string instanceId;
        public string floraId;
        public Vector2 position;
        public string rendererName;
        public string spriteName;
        public string materialName;
        public string shaderName;
        public int renderQueue;
        public float zTest;
        public float materialViewDepthBiasMeters;
        public float propertyBlockViewDepthBiasMeters;
        public bool materialHasViewDepthBiasProperty;
        public Color rendererColor;
        public bool rendererEnabled;
        public bool rendererActiveInHierarchy;
        public string rendererSortingLayer;
        public int rendererSortingOrder;
        public Vector3 rendererBoundsCenter;
        public Vector3 rendererBoundsExtents;
        public Vector3 rendererLossyScale;
        public Color materialColor;
        public float materialCutoff;
        public float materialShadowFloor;
        public bool hasPropertyBlock;
        public Color propertyBlockColor;
        public Rect spriteRect;
        public Vector2 spritePivot;
        public float spritePixelsPerUnit;
        public bool hostRecoveryMaterial;
        public float buildingHostStencilReference;
        public int expectedHostBuildingIndex = -1;
        public int expectedHostStencilReference;
        public Vector3 localTowardCameraDirection;
        public int eligibleCandidateCount;
        public float nearestEligibleCandidateDistanceSquared;
        public float nearestEligibleCandidateBiasMeters;
        public string nearestEligibleCandidateBuildingId;
        public float maximumEligibleCandidateBiasMeters;
        public string maximumEligibleCandidateBuildingId;
        public float maximumEligibleBiasAppliedMeters;
        public float maximumOrthographicCandidateBiasMeters;
        public string maximumOrthographicCandidateBuildingId;
        public float orthographicBiasAppliedMeters;
        public List<BuildingBiasCandidateProbe> buildingCandidates = new();
    }

    [Serializable]
    private sealed class BuildingBiasCandidateProbe
    {
        public int buildingIndex;
        public string buildingId;
        public Vector3 buildingPosition;
        public int rotationQuarterTurns;
        public float packageWidthMeters;
        public float packageDepthMeters;
        public bool eligibleVisibleFrontApron;
        public float requiredViewDepthBiasMeters;
        public float orthographicViewRequiredBiasMeters;
        public float footprintOutsideDistanceSquared;
    }

    [Serializable]
    private sealed class ProbeReport
    {
        public string lotId;
        public string lotName;
        public string utcTimestamp;
        public string outputDirectory;
        public string reportPath;
        public string artworkScreenshot;
        public string topDownFrontArrowScreenshot;
        public string sideBackControlScreenshot;
        public string hostSpecificNoTreeScreenshot;
        public string hostSpecificProductionScreenshot;
        public string hostSpecificForcedAlwaysScreenshot;
        public string gameViewCaptureContract;
        public string visualInspectionStatus;
        public bool unityWindowed;
        public bool automatedPassed;
        public bool screenshotsExist;
        public int lotWidthCells;
        public int lotDepthCells;
        public int buildingCount;
        public int floraCount;
        public int propCount;
        public bool exactBuildingFixture;
        public bool exactFloraFixture;
        public Vector3 cameraPositionWorld;
        public Vector3 cameraPositionLocal;
        public Vector3 cameraRotationWorldEuler;
        public Vector3 cameraForwardWorld;
        public Vector3 cameraForwardLocal;
        public bool cameraOrthographic;
        public float cameraOrthographicSize;
        public Vector2Int cameraPixelSize;
        public string artworkInspectionMode;
        public bool artworkTopDown;
        public string artworkSelected;
        public bool selectionAccepted;
        public int selectedBuildingIndex;
        public bool focusedMoveOutAccepted;
        public bool focusedMoveBackAccepted;
        public bool focusedReleaseAccepted;
        public int applyCountAfterFocusedDrag;
        public int focusedDragApplyDelta;
        public bool focusedCameraInvariant;
        public bool focusedBuildingReturnedToOrigin;
        public bool topDownEnabled;
        public bool frontMarkerVisibleWhileSelected;
        public Vector3 markerDirection;
        public Vector3 expectedMarkerDirection;
        public float markerDirectionDot;
        public bool markerPointsTowardSavedStreetTrees;
        public int applyCountAtSelectedTopDown;
        public bool frontMarkerVisibleAfterDeselect;
        public int applyCountAfterDeselect;
        public int deselectApplyDelta;
        public bool deselectCameraInvariant;
        public float maximumCameraPositionDelta;
        public float maximumCameraRotationDeltaDegrees;
        public float maximumOrthographicSizeDelta;
        public float maximumCameraPanDelta;
        public int hostSpecificHostBuildingIndex;
        public int hostSpecificUnrelatedBuildingIndex;
        public int hostSpecificTreeIndex;
        public Vector2 hostSpecificHostBuildingPosition;
        public Vector2 hostSpecificUnrelatedOriginalPosition;
        public Vector2 hostSpecificUnrelatedTransientPosition;
        public int hostSpecificUnrelatedTransientRotationQuarterTurns;
        public int hostSpecificTransientApplyDelta;
        public Vector3 hostSpecificCameraPan;
        public float hostSpecificOrthographicSize;
        public Vector3 hostSpecificCameraPositionWorld;
        public Vector3 hostSpecificCameraRotationWorldEuler;
        public Vector3 hostSpecificCameraForwardWorld;
        public Rect hostSpecificTreeProjectedScreenRect;
        public string hostSpecificProductionMaterialName;
        public string hostSpecificProductionShaderName;
        public float hostSpecificProductionZTest;
        public float hostSpecificProductionStencilReference;
        public bool hostSpecificHostEligible;
        public bool hostSpecificUnrelatedEligible;
        public int hostSpecificEligibleCandidateCount;
        public float hostSpecificHostCameraDepth;
        public float hostSpecificTreeCameraDepth;
        public float hostSpecificUnrelatedCameraDepth;
        public bool hostSpecificUnrelatedIsNearerThanTree;
        public int hostSpecificProductionVisibleChangedPixels;
        public int hostSpecificProductionVsForcedAlwaysChangedPixels;
        public int hostSpecificComparedPixels;
        public bool hostSpecificTreeVisibleOverHost;
        public bool hostSpecificUnrelatedOcclusionRetained;
        public List<FloraRendererProbe> savedFrontTrees = new();
        public List<FloraRendererProbe> sideBackControlTrees = new();
        public List<FloraRendererProbe> hostSpecificTree = new();
        public List<string> issues = new();
    }
}
