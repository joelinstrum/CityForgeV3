#if UNITY_EDITOR
using System.IO;
using CityForgeV3.UI;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class DistrictSelectionQa
{
    [MenuItem("City Forge/QA/Selection/Load Saved District")]
    private static void Load()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.OpenSavedDistrictSelectionQa();
    }
    [MenuItem("City Forge/QA/Selection/Check Gesture Contract")]
    private static void Check()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.CheckDistrictSelectionGestureQa();
    }
    [MenuItem("City Forge/QA/Selection/Log Selection State")]
    private static void Log()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.LogDistrictSelectionQa();
    }
    private static TestRunnerApi _runner;
    [MenuItem("City Forge/QA/Selection/Run Geometry Tests")]
    private static void Tests()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Run EditMode tests outside Play."); return; }
        _runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        _runner.RegisterCallbacks(new Results());
        _runner.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.EditMode,
            groupNames = new[] { "^CityForgeV3.Tests.EditMode.DistrictSelectionGeometryTests" }
        }));
    }
    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor test) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("QA/DistrictSelectionV01");
            TestRunnerApi.SaveResultToFile(result, "QA/DistrictSelectionV01/geometry.xml");
            Debug.Log($"DISTRICT SELECTION GEOMETRY TESTS passed={result.PassCount} failed={result.FailCount}");
            Object.DestroyImmediate(_runner);
        }
    }
}
#endif
