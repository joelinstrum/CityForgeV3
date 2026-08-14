using CityForgeV3.UI;
using CityForgeV3.World;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RoadWithLampsSelectionQaMenu
{
    private const string LotId = "road-with-lamps";
    private const string PendingKey = "CityForge.RoadWithLampsSelectionQa.Pending";
    private const string ReportPath =
        "/Users/joelinstrum/dev/CityForge - V3/QA/Selection/road-with-lamps-selection-probe.json";

    static RoadWithLampsSelectionQaMenu()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("City Forge/QA/Open Road With Lamps Selection QA")]
    private static void Open()
    {
        if (!EditorApplication.isPlaying)
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
            return;
        }
        OpenRuntimeLot();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode ||
            !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        EditorApplication.delayCall += OpenRuntimeLot;
    }

    private static void OpenRuntimeLot()
    {
        var app = UnityEngine.Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenSavedLotSelectionQa(LotId))
            Debug.LogError($"Unable to open saved lot '{LotId}' for selection QA.");
    }

    [Serializable]
    private sealed class ProbeResult
    {
        public string targetKind;
        public int targetIndex;
        public string hoverKind;
        public int hoverIndex;
        public string selectedKind;
        public int selectedIndex;
        public bool dragAccepted;
        public bool targetMatched;
        public bool targetReachable;
        public int sampledPoints;
    }

    [Serializable]
    private sealed class ProbeReport
    {
        public string lotId = LotId;
        public int floraCount;
        public int propCount;
        public List<ProbeResult> results = new();
    }

    [MenuItem("City Forge/QA/Run Road With Lamps Selection Probe")]
    private static void RunSelectionProbe()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("Open Road With Lamps Selection QA before running the probe.");
            return;
        }
        var world = UnityEngine.Object.FindFirstObjectByType<LotWorldController>();
        var camera = world == null ? null : world.GetComponentInChildren<Camera>();
        if (world == null || camera == null || world.CurrentLotName != LotId)
        {
            Debug.LogError("The road-with-lamps runtime lot is not open.");
            return;
        }
        var report = new ProbeReport
        {
            floraCount = world.FloraCount,
            propCount = world.PropCount
        };
        ProbeRoot(world, camera, world.transform.Find("Placed Flora"),
            LotObjectSelectionKind.Flora, world.FloraCount, report);
        world.LoadLot(LotId);
        ProbeRoot(world, camera, world.transform.Find("Placed Props"),
            LotObjectSelectionKind.Prop, world.PropCount, report);
        world.LoadLot(LotId);
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        Debug.Log($"Road-with-lamps selection probe wrote {ReportPath}");
    }

    private static void ProbeRoot(LotWorldController world, Camera camera,
        Transform root, LotObjectSelectionKind expectedKind, int count,
        ProbeReport report)
    {
        if (root == null) return;
        var panelSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
        for (var index = 0; index < count && index < root.childCount; index++)
        {
            Bounds bounds;
            if (expectedKind == LotObjectSelectionKind.Prop)
            {
                if (!LotWorldController.TrySelectablePropBounds(
                        root.GetChild(index), out bounds)) continue;
            }
            else
            {
                var renderers = root.GetChild(index).GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                bounds = renderers[0].bounds;
                for (var rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                    if (renderers[rendererIndex].enabled)
                        bounds.Encapsulate(renderers[rendererIndex].bounds);
            }
            var centerPixel = camera.WorldToScreenPoint(bounds.center);
            var panelPoint = new Vector2(centerPixel.x, panelSize.y - centerPixel.y);
            var targetReachable = false;
            var sampledPoints = 0;
            foreach (var x in new[] { 0.2f, 0.35f, 0.5f, 0.65f, 0.8f })
            foreach (var y in new[] { 0.2f, 0.35f, 0.5f, 0.65f, 0.8f })
            {
                sampledPoints++;
                var worldSample = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, x),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, y),
                    Mathf.Lerp(bounds.min.z, bounds.max.z, 0.5f));
                var samplePixel = camera.WorldToScreenPoint(worldSample);
                var samplePanel = new Vector2(samplePixel.x, panelSize.y - samplePixel.y);
                var sampleKind = world.UpdateObjectHoverFromPanel(samplePanel, panelSize);
                if (sampleKind != expectedKind || world.HoverObjectIndex != index) continue;
                panelPoint = samplePanel;
                targetReachable = true;
                break;
            }
            var hoverKind = world.UpdateObjectHoverFromPanel(panelPoint, panelSize);
            var hoverIndex = world.HoverObjectIndex;
            var selectedKind = world.BeginExistingObjectManipulationFromPanel(
                panelPoint, panelSize);
            var selectedIndex = selectedKind == LotObjectSelectionKind.Flora
                ? world.SelectedFloraIndex : selectedKind == LotObjectSelectionKind.Prop
                    ? world.SelectedPropIndex : world.SelectedBuildingIndex;
            var dragPoint = panelPoint + new Vector2(12f, -8f);
            var dragAccepted = selectedKind switch
            {
                LotObjectSelectionKind.Flora => world.DragFloraFromPanel(dragPoint, panelSize),
                LotObjectSelectionKind.Prop => world.DragPropFromPanel(dragPoint, panelSize),
                LotObjectSelectionKind.Building => world.DragBuildingFromPanel(dragPoint, panelSize),
                _ => false
            };
            if (selectedKind == LotObjectSelectionKind.Flora) world.EndFloraDrag();
            else if (selectedKind == LotObjectSelectionKind.Prop) world.EndPropDrag();
            else if (selectedKind == LotObjectSelectionKind.Building) world.EndBuildingDrag();
            report.results.Add(new ProbeResult
            {
                targetKind = expectedKind.ToString(),
                targetIndex = index,
                hoverKind = hoverKind.ToString(),
                hoverIndex = hoverIndex,
                selectedKind = selectedKind.ToString(),
                selectedIndex = selectedIndex,
                dragAccepted = dragAccepted,
                targetReachable = targetReachable,
                sampledPoints = sampledPoints,
                targetMatched = hoverKind == expectedKind && hoverIndex == index &&
                    selectedKind == expectedKind && selectedIndex == index
            });
            world.LoadLot(LotId);
            root = world.transform.Find(expectedKind == LotObjectSelectionKind.Flora
                ? "Placed Flora" : "Placed Props");
        }
    }
}
