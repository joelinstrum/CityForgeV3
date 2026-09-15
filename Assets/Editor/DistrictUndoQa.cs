#if UNITY_EDITOR
using System.IO;
using CityForgeV3.UI;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class DistrictUndoQa
{
    [MenuItem("City Forge/QA/Undo/Load Saved District")]
    private static void Load()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.OpenSavedDistrictSelectionQa();
    }
    [MenuItem("City Forge/QA/Undo/Check Saved District Undo")]
    private static void Check()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.CheckDistrictUndoQa();
    }
    [MenuItem("City Forge/QA/Undo/Log Selection State")]
    private static void Log()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.LogDistrictSelectionQa();
    }
    private static TestRunnerApi _runner;
    [MenuItem("City Forge/QA/Undo/Run Undo Tests")]
    private static void Tests()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Run EditMode tests outside Play."); return; }
        _runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        _runner.RegisterCallbacks(new Results());
        _runner.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.EditMode,
            groupNames = new[] { "^CityForgeV3.Tests.EditMode.DistrictUndoHistoryTests" }
        }));
    }
    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor test) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("QA/DistrictUndoV01");
            TestRunnerApi.SaveResultToFile(result, "QA/DistrictUndoV01/undo.xml");
            Debug.Log($"DISTRICT UNDO TESTS passed={result.PassCount} failed={result.FailCount}");
            Object.DestroyImmediate(_runner);
        }
    }
}
#endif
