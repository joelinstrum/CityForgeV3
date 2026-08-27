using CityForgeV3.UI;
using CityForgeV3.World;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SanFranciscoBuildingFocusQaMenu
{
    private const string LotId = "sanfranciscolot";
    private const string TargetBuildingId =
        "cityforge.v3.residential.green_victorian_tripo_01";
    private const string PendingKey =
        "CityForge.SanFranciscoBuildingFocusQa.Pending";
    private const string MenuRoot =
        "City Forge/QA/SanFrancisco Building Focus/";
    private const float PositionTolerance = 0.0001f;
    private const float RotationToleranceDegrees = 0.001f;

    private static readonly string OutputDirectory = Path.GetFullPath(
        Path.Combine(Application.dataPath,
            "../QA/SanFranciscoBuildingFocus"));
    private static readonly string ReportPath = Path.Combine(
        OutputDirectory, "sanfrancisco-building-focus-report.json");

    private static bool _running;
    private static int _targetGameFrame;
    private static int _waitPollCount;
    private static Action _pendingAction;
    private static ProbeReport _report;
    private static LotWorldController _world;
    private static Camera _camera;
    private static int _targetBuildingIndex = -1;
    private static Vector3 _targetOrigin;

    static SanFranciscoBuildingFocusQaMenu()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem(MenuRoot + "Run Deterministic Probe")]
    private static void RunDeterministicProbe()
    {
        if (_running)
        {
            Debug.LogWarning("The SanFrancisco building-focus probe is already running.");
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

    [MenuItem(MenuRoot + "Open Fixture Only")]
    private static void OpenFixtureOnly()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError(
                "Enter Play Mode before opening the SanFrancisco focus fixture, " +
                "or run the deterministic probe instead.");
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
        WaitForGameFrames(2, StartProbe);
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
            focusEntryScreenshot = Path.Combine(
                OutputDirectory, "focus-entry.png"),
            focusEntryUiScreenshot = Path.Combine(
                OutputDirectory, "focus-entry-ui.png"),
            focusedMoveScreenshot = Path.Combine(
                OutputDirectory, "focused-move.png"),
            reconciledScreenshot = Path.Combine(
                OutputDirectory, "reconciled.png")
        };

        Directory.CreateDirectory(OutputDirectory);
        Guarded(() => OpenAndConfigureFixture(true));
    }

    private static void OpenAndConfigureFixture(bool continueProbe)
    {
        var app = UnityEngine.Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null)
        {
            Fail("CityForgeApp was not available in Play Mode.");
            return;
        }

        if (!app.OpenSavedLotBuildingFocusQa(LotId))
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
        _world.SetQaOrthographicSize(10f);
        _world.SetQaCameraPan(-12f, -20f);

        if (app.BuildingLibraryModalOpenForQa)
        {
            Fail("The Building Library modal remained open in the focus fixture.");
            return;
        }

        if (!continueProbe)
        {
            Debug.Log("Opened the real SanFranciscoLot building-focus fixture.");
            return;
        }

        WaitForGameFrames(3, BeginFocus);
    }

    private static void BeginFocus()
    {
        _targetBuildingIndex = FindTargetBuildingIndex(_world);
        if (_targetBuildingIndex < 0)
        {
            Fail("Could not find the rotated building centered at (-12, -20).");
            return;
        }

        var placed = _world.Session.Data.Buildings[_targetBuildingIndex];
        _report.targetMatchedGreenVictorian = string.Equals(
            placed.BuildingId, TargetBuildingId,
            StringComparison.OrdinalIgnoreCase);
        if (!_report.targetMatchedGreenVictorian)
        {
            Fail($"Expected the Green Victorian at (-12, -20), got " +
                 $"'{placed.BuildingId}'.");
            return;
        }
        _targetOrigin = new Vector3(placed.CellX, 0f, placed.CellZ);
        _report.targetBuildingIndex = _targetBuildingIndex;
        _report.targetBuildingId = placed.BuildingId;
        _report.targetRotationQuarterTurns = placed.RotationQuarterTurns;
        _report.targetOrigin = _targetOrigin;
        _report.buildingCount = _world.BuildingCount;
        _report.floraCount = _world.FloraCount;
        _report.propCount = _world.PropCount;

        var panelSize = CameraPanelSize(_camera);
        var panelPoint = WorldToPanelPoint(_camera, _targetOrigin, panelSize);
        _report.focusEntryAccepted =
            _world.BeginBuildingDragFromPanel(panelPoint, panelSize);
        _report.selectedBuildingIndexAfterEntry = _world.SelectedBuildingIndex;
        _report.focusActiveAfterEntry = _world.BuildingFocusFreezeActive;

        if (!_report.focusEntryAccepted)
        {
            Fail("The target building rejected the real panel-space drag entry.");
            return;
        }
        if (_world.SelectedBuildingIndex != _targetBuildingIndex)
        {
            Fail($"The panel hit selected building {_world.SelectedBuildingIndex}, " +
                 $"not target {_targetBuildingIndex}.");
            return;
        }
        if (!_world.BuildingFocusFreezeActive)
        {
            Fail("Building focus-freeze did not become active after selection.");
            return;
        }

        // Selection intentionally reconciles once. Wait for the old hierarchy's
        // deferred Destroy calls to finish before taking the identity baseline.
        var app = UnityEngine.Object.FindFirstObjectByType<CityForgeApp>();
        app?.RefreshBuildingFocusViewForQa();
        WaitForGameFrames(2, CaptureFocusedUiAndContinue);
    }

    private static void CaptureFocusedUiAndContinue()
    {
        var app = UnityEngine.Object.FindFirstObjectByType<CityForgeApp>();
        _report.buildingLibraryModalClosedBeforeUiCapture =
            app != null && !app.BuildingLibraryModalOpenForQa;
        _report.focusOverlayVisibleBeforeUiCapture =
            app != null && app.BuildingFocusOverlayVisibleForQa;
        _report.greenVictorianSelectedBeforeUiCapture =
            _world != null &&
            _world.SelectedBuildingIndex == _targetBuildingIndex &&
            _world.ActiveObjectSelection == LotObjectSelectionKind.Building &&
            _world.BuildingFocusFreezeActive;

        if (!_report.buildingLibraryModalClosedBeforeUiCapture)
        {
            Fail("The Building Library modal was present before UI capture.");
            return;
        }
        if (!_report.greenVictorianSelectedBeforeUiCapture)
        {
            Fail("The Green Victorian was not the focused selection before UI capture.");
            return;
        }
        if (!_report.focusOverlayVisibleBeforeUiCapture)
        {
            Fail("The building-focus spotlight was not visible before UI capture.");
            return;
        }

        ScreenCapture.CaptureScreenshot(_report.focusEntryUiScreenshot);
        WaitForGameFrames(2, RunFocusedMovementProbe);
    }

    private static void RunFocusedMovementProbe()
    {
        var baselineCamera = _world.CaptureCameraFraming();
        var baselinePan = _world.CameraPanWorld;
        var baselineContext = CaptureContext(_world, _targetBuildingIndex);
        var applyBaseline = _world.SessionStateApplyCountForQa;
        var liveMoveBaseline = _world.BuildingFocusLiveMoveCountForQa;
        var initialPresentationPosition =
            _world.BuildingPresentationPosition(_targetBuildingIndex);

        _report.contextTransformCount = baselineContext.Count;
        _report.applyCountAfterFocusEntry = applyBaseline;
        _report.liveMoveCountAfterFocusEntry = liveMoveBaseline;
        _report.selectedPresentationAtEntry = initialPresentationPosition;
        CaptureCamera(_camera, _report.focusEntryScreenshot);

        var route = new[]
        {
            _targetOrigin + new Vector3(1f, 0f, 0f),
            _targetOrigin + new Vector3(2f, 0f, 1f),
            _targetOrigin + new Vector3(3f, 0f, 2f),
            _targetOrigin + new Vector3(2f, 0f, 1f),
            _targetOrigin + new Vector3(1f, 0f, 0f),
            _targetOrigin
        };

        var panelSize = CameraPanelSize(_camera);
        for (var index = 0; index < route.Length; index++)
        {
            _report.pointerMoveAttempts++;
            var panelPoint = WorldToPanelPoint(_camera, route[index], panelSize);
            if (_world.DragBuildingFromPanel(panelPoint, panelSize))
                _report.acceptedPointerMoves++;
            else
                AddIssue($"Pointer move {index + 1} was rejected at {route[index]}.");

            var selectedPosition =
                _world.BuildingPresentationPosition(_targetBuildingIndex);
            _report.maximumSelectedDisplacement = Mathf.Max(
                _report.maximumSelectedDisplacement,
                Vector3.Distance(initialPresentationPosition, selectedPosition));
            CompareCamera(baselineCamera, baselinePan,
                $"pointer move {index + 1}");
            CompareContext(baselineContext,
                CaptureContext(_world, _targetBuildingIndex),
                $"pointer move {index + 1}");

            if (index == 2)
            {
                _report.selectedPresentationAtFarthestMove = selectedPosition;
                CaptureCamera(_camera, _report.focusedMoveScreenshot);
            }
        }

        _report.releaseAccepted = _world.EndBuildingDrag();
        if (!_report.releaseAccepted)
            AddIssue("EndBuildingDrag rejected the focused release.");

        CompareCamera(baselineCamera, baselinePan, "focused release");
        CompareContext(baselineContext,
            CaptureContext(_world, _targetBuildingIndex), "focused release");

        _report.applyCountAfterMoveAndRelease =
            _world.SessionStateApplyCountForQa;
        _report.applyCountMoveAndReleaseDelta =
            _report.applyCountAfterMoveAndRelease - applyBaseline;
        _report.liveMoveCountAfterMoveAndRelease =
            _world.BuildingFocusLiveMoveCountForQa;
        _report.liveMoveCountDelta =
            _report.liveMoveCountAfterMoveAndRelease - liveMoveBaseline;
        _report.selectedPresentationAfterRelease =
            _world.BuildingPresentationPosition(_targetBuildingIndex);
        _report.focusActiveAfterRelease = _world.BuildingFocusFreezeActive;

        _report.selectedLiveMovementObserved =
            _report.maximumSelectedDisplacement >= 1f &&
            _report.liveMoveCountDelta > 0 &&
            _report.acceptedPointerMoves > 0;
        _report.selectedReturnedToOrigin = Vector3.Distance(
            _report.selectedPresentationAfterRelease, _targetOrigin) <=
            PositionTolerance;

        if (!_report.selectedLiveMovementObserved)
            AddIssue("The selected presentation did not move live during pointer input.");
        if (!_report.selectedReturnedToOrigin)
            AddIssue("The selected presentation did not return to its saved origin.");
        if (_report.applyCountMoveAndReleaseDelta != 0)
            AddIssue("ApplySessionState ran during focused pointer moves or release: " +
                     $"delta={_report.applyCountMoveAndReleaseDelta}.");
        if (!_world.BuildingFocusFreezeActive)
            AddIssue("Focus-freeze ended on pointer release instead of remaining active.");

        var applyBeforeDeselect = _world.SessionStateApplyCountForQa;
        _world.DeselectAll();
        _report.applyCountAfterDeselect = _world.SessionStateApplyCountForQa;
        _report.applyCountDeselectDelta =
            _report.applyCountAfterDeselect - applyBeforeDeselect;
        _report.focusActiveAfterDeselect = _world.BuildingFocusFreezeActive;
        _report.activeSelectionAfterDeselect =
            _world.ActiveObjectSelection.ToString();

        CompareCamera(baselineCamera, baselinePan, "deselect reconciliation");
        CaptureCamera(_camera, _report.reconciledScreenshot);

        if (_report.applyCountDeselectDelta != 1)
            AddIssue("Deselect reconciliation must run ApplySessionState exactly once: " +
                     $"delta={_report.applyCountDeselectDelta}.");
        if (_world.BuildingFocusFreezeActive)
            AddIssue("Focus-freeze remained active after deselect.");
        if (_world.ActiveObjectSelection != LotObjectSelectionKind.None)
            AddIssue("A lot object remained selected after deselect.");

        _report.cameraFramingInvariant =
            _report.maximumCameraPositionDelta <= PositionTolerance &&
            _report.maximumCameraRotationDeltaDegrees <=
                RotationToleranceDegrees &&
            _report.maximumOrthographicSizeDelta <= PositionTolerance &&
            _report.maximumCameraPanDelta <= PositionTolerance;
        _report.nonselectedContextStable =
            _report.contextMismatchCount == 0;
        _report.passed = _report.issues.Count == 0 &&
            _report.cameraFramingInvariant &&
            _report.nonselectedContextStable;

        WriteReport();
        _running = false;
        Debug.Log(_report.passed
            ? $"SanFrancisco building-focus QA passed. Report: {ReportPath}"
            : $"SanFrancisco building-focus QA FAILED. Report: {ReportPath}");
    }

    private static int FindTargetBuildingIndex(LotWorldController world)
    {
        var buildings = world.Session.Data.Buildings;
        for (var index = 0; index < (buildings?.Count ?? 0); index++)
        {
            var building = buildings[index];
            if (Mathf.Abs(building.CellX + 12f) <= 0.01f &&
                Mathf.Abs(building.CellZ + 20f) <= 0.01f)
                return index;
        }
        return -1;
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

    private static ContextSnapshot CaptureContext(
        LotWorldController world, int selectedBuildingIndex)
    {
        var snapshot = new ContextSnapshot();
        var worldRoot = world.transform;

        for (var index = 0; index < world.BuildingCount; index++)
        {
            if (index == selectedBuildingIndex) continue;
            var prefix = $"Building {index + 1} ";
            for (var childIndex = 0; childIndex < worldRoot.childCount; childIndex++)
            {
                var child = worldRoot.GetChild(childIndex);
                if (!child.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                AddSubtree(snapshot, $"building[{index}]", child);
            }
        }

        AddNamedSubtree(snapshot, worldRoot, "Placed Flora", "flora");
        AddNamedSubtree(snapshot, worldRoot, "Placed Props", "props");
        AddNamedSubtree(snapshot, worldRoot,
            "Placed Overlay Textures", "overlay-textures");
        AddNamedSubtree(snapshot, worldRoot,
            "Road Family Artwork", "road-artwork");
        return snapshot;
    }

    private static void AddNamedSubtree(ContextSnapshot snapshot,
        Transform worldRoot, string objectName, string category)
    {
        var root = worldRoot.Find(objectName);
        if (root != null) AddSubtree(snapshot, category, root);
    }

    private static void AddSubtree(ContextSnapshot snapshot,
        string category, Transform root)
    {
        foreach (var item in root.GetComponentsInChildren<Transform>(true))
        {
            var key = category + "/" + TransformPath(root, item);
            snapshot.Items[key] = new TransformSample(item);
        }
    }

    private static string TransformPath(Transform root, Transform item)
    {
        if (item == root)
            return $"{root.name}[{root.GetSiblingIndex()}]";
        var parts = new List<string>();
        var current = item;
        while (current != null && current != root)
        {
            parts.Add($"{current.name}[{current.GetSiblingIndex()}]");
            current = current.parent;
        }
        parts.Add($"{root.name}[{root.GetSiblingIndex()}]");
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static void CompareContext(ContextSnapshot expected,
        ContextSnapshot actual, string stage)
    {
        if (expected.Count != actual.Count)
            RecordContextMismatch(stage,
                $"object count changed {expected.Count} -> {actual.Count}");

        foreach (var pair in expected.Items)
        {
            if (!actual.Items.TryGetValue(pair.Key, out var current))
            {
                RecordContextMismatch(stage, $"missing {pair.Key}");
                continue;
            }

            var baseline = pair.Value;
            if (baseline.InstanceId != current.InstanceId)
                RecordContextMismatch(stage,
                    $"identity changed for {pair.Key}: " +
                    $"{baseline.InstanceId} -> {current.InstanceId}");
            if (Vector3.Distance(baseline.LocalPosition,
                    current.LocalPosition) > PositionTolerance)
                RecordContextMismatch(stage,
                    $"local position changed for {pair.Key}");
            if (Quaternion.Angle(baseline.LocalRotation,
                    current.LocalRotation) > RotationToleranceDegrees)
                RecordContextMismatch(stage,
                    $"local rotation changed for {pair.Key}");
            if (Vector3.Distance(baseline.LocalScale,
                    current.LocalScale) > PositionTolerance)
                RecordContextMismatch(stage,
                    $"local scale changed for {pair.Key}");
            if (baseline.ActiveSelf != current.ActiveSelf)
                RecordContextMismatch(stage,
                    $"active state changed for {pair.Key}");
        }
    }

    private static void RecordContextMismatch(string stage, string detail)
    {
        _report.contextMismatchCount++;
        if (_report.contextMismatches.Count < 40)
            _report.contextMismatches.Add($"{stage}: {detail}");
        AddIssue($"Nonselected context changed during {stage}.");
    }

    private static void CompareCamera(
        LotWorldController.CameraFramingState baseline,
        Vector3 baselinePan, string stage)
    {
        var current = _world.CaptureCameraFraming();
        var positionDelta = Vector3.Distance(
            baseline.Position, current.Position);
        var rotationDelta = Quaternion.Angle(
            baseline.Rotation, current.Rotation);
        var sizeDelta = Mathf.Abs(
            baseline.OrthographicSize - current.OrthographicSize);
        var panDelta = Vector3.Distance(baselinePan, _world.CameraPanWorld);
        _report.maximumCameraPositionDelta = Mathf.Max(
            _report.maximumCameraPositionDelta, positionDelta);
        _report.maximumCameraRotationDeltaDegrees = Mathf.Max(
            _report.maximumCameraRotationDeltaDegrees, rotationDelta);
        _report.maximumOrthographicSizeDelta = Mathf.Max(
            _report.maximumOrthographicSizeDelta, sizeDelta);
        _report.maximumCameraPanDelta = Mathf.Max(
            _report.maximumCameraPanDelta, panDelta);
        if (positionDelta > PositionTolerance ||
            rotationDelta > RotationToleranceDegrees ||
            sizeDelta > PositionTolerance ||
            panDelta > PositionTolerance)
        {
            AddIssue($"Camera framing changed during {stage}.");
        }
    }

    private static void CaptureCamera(Camera camera, string path)
    {
        const int width = 1600;
        const int height = 1000;
        var target = new RenderTexture(
            width, height, 24, RenderTextureFormat.ARGB32);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var image = new Texture2D(
            width, height, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.Destroy(image);
            UnityEngine.Object.Destroy(target);
        }
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
        _report.passed = false;
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
        Debug.LogError($"SanFrancisco building-focus QA failed: {issue}");
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

    private sealed class ContextSnapshot
    {
        public readonly Dictionary<string, TransformSample> Items = new();
        public int Count => Items.Count;
    }

    private sealed class TransformSample
    {
        public readonly int InstanceId;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;
        public readonly bool ActiveSelf;

        public TransformSample(Transform transform)
        {
            InstanceId = transform.gameObject.GetInstanceID();
            LocalPosition = transform.localPosition;
            LocalRotation = transform.localRotation;
            LocalScale = transform.localScale;
            ActiveSelf = transform.gameObject.activeSelf;
        }
    }

    [Serializable]
    private sealed class ProbeReport
    {
        public string lotId = LotId;
        public string utcTimestamp;
        public string outputDirectory;
        public string reportPath;
        public string focusEntryScreenshot;
        public string focusEntryUiScreenshot;
        public string focusedMoveScreenshot;
        public string reconciledScreenshot;
        public string deselectMechanism =
            "LotWorldController.DeselectAll (the same action used by the " +
            "real empty-space focus callback)";
        public string limitation =
            "The fixture and drag math use the real CityForgeApp and " +
            "LotWorldController. Pointer positions are fed deterministically " +
            "to the controller API; this probe does not synthesize an OS/UI " +
            "Toolkit pointer event, and camera-only screenshots omit UI chrome.";
        public bool passed;
        public int buildingCount;
        public int floraCount;
        public int propCount;
        public int targetBuildingIndex;
        public string targetBuildingId;
        public int targetRotationQuarterTurns;
        public Vector3 targetOrigin;
        public bool targetMatchedGreenVictorian;
        public bool buildingLibraryModalClosedBeforeUiCapture;
        public bool greenVictorianSelectedBeforeUiCapture;
        public bool focusOverlayVisibleBeforeUiCapture;
        public bool focusEntryAccepted;
        public int selectedBuildingIndexAfterEntry;
        public bool focusActiveAfterEntry;
        public int applyCountAfterFocusEntry;
        public int liveMoveCountAfterFocusEntry;
        public int pointerMoveAttempts;
        public int acceptedPointerMoves;
        public bool releaseAccepted;
        public bool focusActiveAfterRelease;
        public int applyCountAfterMoveAndRelease;
        public int applyCountMoveAndReleaseDelta;
        public int liveMoveCountAfterMoveAndRelease;
        public int liveMoveCountDelta;
        public float maximumSelectedDisplacement;
        public bool selectedLiveMovementObserved;
        public bool selectedReturnedToOrigin;
        public Vector3 selectedPresentationAtEntry;
        public Vector3 selectedPresentationAtFarthestMove;
        public Vector3 selectedPresentationAfterRelease;
        public int contextTransformCount;
        public int contextMismatchCount;
        public bool nonselectedContextStable;
        public float maximumCameraPositionDelta;
        public float maximumCameraRotationDeltaDegrees;
        public float maximumOrthographicSizeDelta;
        public float maximumCameraPanDelta;
        public bool cameraFramingInvariant;
        public int applyCountAfterDeselect;
        public int applyCountDeselectDelta;
        public bool focusActiveAfterDeselect;
        public string activeSelectionAfterDeselect;
        public List<string> contextMismatches = new();
        public List<string> issues = new();
    }
}
