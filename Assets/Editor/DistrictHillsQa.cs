#if UNITY_EDITOR
using System.IO;
using CityForgeV3.UI;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
public static class DistrictHillsQa
{
    private static CityForgeApp App=>Object.FindFirstObjectByType<CityForgeApp>();
    [MenuItem("City Forge/QA/Hills/Open Little River Bend")] private static void Open(){if(EditorApplication.isPlaying)App?.OpenLittleRiverBendHillsQa();}
    [MenuItem("City Forge/QA/Hills/Apply Hills and River")] private static void Apply(){if(EditorApplication.isPlaying)App?.ApplyLittleRiverBendHillsQa();}
    [MenuItem("City Forge/QA/Hills/Check Saved Reload")] private static void Reload(){if(EditorApplication.isPlaying)App?.CheckLittleRiverBendHillsQa();}
    [MenuItem("City Forge/QA/Hills/Open Hills Controls")] private static void Controls(){if(EditorApplication.isPlaying)App?.ShowDistrictHillsQaModal();}
    private static TestRunnerApi runner;
    [MenuItem("City Forge/QA/Hills/Run Tests")] private static void Tests()
    {
        if(EditorApplication.isPlaying)return;runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Results());
        runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,groupNames=new[]{"^CityForgeV3.Tests.EditMode.DistrictElevationTests","^CityForgeV3.Tests.EditMode.DistrictWoodResourceTests","^CityForgeV3.Tests.EditMode.DistrictLaborTests","^CityForgeV3.Tests.EditMode.DistrictTreeHarvestTests"}}));
    }
    private sealed class Results:ICallbacks
    {
        public void RunStarted(ITestAdaptor t){} public void TestStarted(ITestAdaptor t){} public void TestFinished(ITestResultAdaptor r){}
        public void RunFinished(ITestResultAdaptor r){TestRunnerApi.SaveResultToFile(r,"/private/tmp/cityforge-hills-tests.xml");Debug.Log($"HILLS TESTS {r.TestStatus} passed={r.PassCount} failed={r.FailCount}");}
    }
}
#endif
