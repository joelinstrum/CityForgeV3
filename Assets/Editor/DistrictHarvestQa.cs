#if UNITY_EDITOR
using System.IO;
using CityForgeV3.UI;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class DistrictHarvestQa
{
    [MenuItem("City Forge/QA/Harvest/Load Saved District")]
    private static void Load()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.OpenDistrictHarvestQa();
    }
    [MenuItem("City Forge/QA/Harvest/Check Harvest Save and Undo")]
    private static void Check()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.CheckDistrictHarvestQa();
    }
    [MenuItem("City Forge/QA/Harvest/Play Fall")]
    private static void Log()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.FallDistrictHarvestQa();
    }
    [MenuItem("City Forge/QA/Harvest/Clear Wood")]
    private static void Clear() { if(EditorApplication.isPlaying) Object.FindFirstObjectByType<CityForgeApp>()?.ClearDistrictHarvestQa(); }
    [MenuItem("City Forge/QA/Harvest/Isolate Saved Tree")]
    private static void Isolate() { if(EditorApplication.isPlaying) Object.FindFirstObjectByType<CityForgeApp>()?.IsolateDistrictHarvestQa(); }
    private static TestRunnerApi _runner;
    [MenuItem("City Forge/QA/Harvest/Run Harvest Tests")]
    private static void Tests()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Run EditMode tests outside Play."); return; }
        _runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        _runner.RegisterCallbacks(new Results());
        _runner.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.EditMode,
            groupNames = new[] { "^CityForgeV3.Tests.EditMode.DistrictTreeHarvestTests" }
        }));
    }
    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor test) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("QA/DistrictHarvestV01");
            TestRunnerApi.SaveResultToFile(result, "QA/DistrictHarvestV01/harvest.xml");
            Debug.Log($"DISTRICT HARVEST TESTS passed={result.PassCount} failed={result.FailCount}");
            Object.DestroyImmediate(_runner);
        }
    }
}
#endif
