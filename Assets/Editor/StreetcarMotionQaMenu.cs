using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class StreetcarMotionQaMenu
{
    private const string LotId = "streetcar-test";
    private const string PendingKey = "CityForge.StreetcarMotionQa.Pending";

    static StreetcarMotionQaMenu()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("City Forge/QA/Open Streetcar Motion QA")]
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
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenSavedLotSelectionQa(LotId))
        {
            Debug.LogError($"Unable to open saved lot '{LotId}' for streetcar motion QA.");
            return;
        }

        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world != null && world.StreetcarRiderDemand == 0)
            world.AdjustStreetcarRiderDemand(40);
        if (world != null && world.StreetcarStopCount == 0)
        {
            var stopCenter = RoadPlacementModel.CellCenterMeters(
                0, 2, world.LotWidthMeters, world.LotDepthMeters);
            world.SelectRoadCellAtWorld(stopCenter.x, stopCenter.y, false);
            world.PlaceStreetcarStop();
        }
        if (world != null)
            world.SetZoomLevel(LotZoomLevel.Close);
        if (world != null)
            Debug.Log($"Streetcar motion QA riders={world.StreetcarRiderDemand} active={world.ActiveStreetcarCount} stops={world.StreetcarStopCount}");
    }
}
