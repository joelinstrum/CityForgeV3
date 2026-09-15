#if UNITY_EDITOR
using System.IO;
using CityForgeV3.UI;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class DistrictLaborQa
{
    [MenuItem("City Forge/QA/Labor/Load Saved District")]
    private static void Load()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.OpenDistrictLaborQa();
    }
    [MenuItem("City Forge/QA/Labor/Reload Saved Labor")]
    private static void Check()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.ReloadDistrictLaborQa();
    }
    [MenuItem("City Forge/QA/Labor/Log Labor")]
    private static void Log()
    {
        if (EditorApplication.isPlaying)
            Object.FindFirstObjectByType<CityForgeApp>()?.LogDistrictLaborQa();
    }
    [MenuItem("City Forge/QA/Labor/Open Labor Modal")]
    private static void Clear() { if(EditorApplication.isPlaying) Object.FindFirstObjectByType<CityForgeApp>()?.ShowDistrictLaborQaModal(); }
    [MenuItem("City Forge/QA/Labor/Log UI")]
    private static void Isolate() { if(EditorApplication.isPlaying) Object.FindFirstObjectByType<CityForgeApp>()?.LogDistrictLaborQa(); }
    [MenuItem("City Forge/QA/Labor/Assign Two Through Modal")]
    private static void AssignTwo(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.AssignDistrictLaborQa();}
    [MenuItem("City Forge/QA/Labor/Inspect First Worker")]
    private static void InspectWorker(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.InspectDistrictWorkerQa();}
    [MenuItem("City Forge/QA/Labor/Check Wood HUD and Reload")]
    private static void CheckWood(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.CheckDistrictWoodHudQa();}
    private static TestRunnerApi _runner;
    [MenuItem("City Forge/QA/Labor/Run Labor Tests")]
    private static void Tests()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Run EditMode tests outside Play."); return; }
        _runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        _runner.RegisterCallbacks(new Results());
        _runner.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.EditMode,
            groupNames = new[] { "^CityForgeV3.Tests.EditMode.DistrictLaborTests", "^CityForgeV3.Tests.EditMode.DistrictTreeHarvestTests", "^CityForgeV3.Tests.EditMode.DistrictWoodResourceTests" }
        }));
    }
    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor test) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("QA/DistrictLaborV01");
            TestRunnerApi.SaveResultToFile(result, "QA/DistrictLaborV01/labor.xml");
            Debug.Log($"DISTRICT LABOR TESTS passed={result.PassCount} failed={result.FailCount}");
            Object.DestroyImmediate(_runner);
        }
    }
}
#endif
