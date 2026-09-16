#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using CityForgeV3.UI;
[InitializeOnLoad]
public static class RegionMapLayersReview
{
    static TestRunnerApi runner;
    static RegionMapLayersReview(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string path="/tmp/cityforge-map-layers-command.txt";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(path))return;
        var command=File.ReadAllText(path).Trim();File.Delete(path);
        try
        {
            if(command=="refresh")AssetDatabase.Refresh();
            else if(command=="windowed-game")
            {
                var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                view.maximized=false;view.Focus();view.Repaint();
            }
            else if(command=="pike-art")NationalPikeArtReview.Capture();
            else if(command=="play")EditorApplication.isPlaying=true;
            else if(command=="stop")EditorApplication.isPlaying=false;
            else if(command=="tests")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before running EditMode tests.");
                runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Results());
                runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,groupNames=new[]{".*(RiverBankAppearanceTests|DistrictSurfaceCacheTests|DistrictRiverSculptTests|RegionRiverDrawingTests|RegionMapOrientationTests|NationalPikePlacementTests|DistrictRoadPlacementTests|RegionPikeStrokeTests|RegionMapLayersTests|RegionRiverNetworkTests|RegionTerrainMenuTests|DistrictElevationTests).*"}}));
            }
            else UnityEngine.Object.FindFirstObjectByType<CityForgeApp>().RegionMapLayersQa(command);
            File.WriteAllText("/tmp/cityforge-map-layers-result.txt","OK "+command);
        }
        catch(Exception e){File.WriteAllText("/tmp/cityforge-map-layers-result.txt",e.ToString());Debug.LogException(e);}
    }
    sealed class Results:ICallbacks
    {
        public void RunStarted(ITestAdaptor t){}public void TestStarted(ITestAdaptor t){}public void TestFinished(ITestResultAdaptor t){}
        public void RunFinished(ITestResultAdaptor result){TestRunnerApi.SaveResultToFile(result,"/tmp/cityforge-map-layers-tests.xml");}
    }
}
#endif
