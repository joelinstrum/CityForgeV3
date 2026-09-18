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
    // Separate checkouts can run simultaneously; never consume another editor's commands.
    static string BridgePath(string suffix) => Path.Combine(Path.GetTempPath(),
        "cityforge-" + new DirectoryInfo(Application.dataPath).Parent.Name.Replace(' ', '_') + "-map-layers-" + suffix + ".txt");
    static RegionMapLayersReview(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        var path=BridgePath("command");
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
            else if(command=="plane-anchor-review")PlaneAnchorReview.Capture();
            else if(command=="plane-retirement-review")PlaneAnchorReview.VerifyRetirement();
            else if(command=="maple-review")PlaneAnchorReview.VerifyMaple();
            else if(command=="silver-maple-review")PlaneAnchorReview.VerifySilverMaple();
            else if(command=="willow-review")PlaneAnchorReview.VerifyWillow();
            else if(command=="willow-ground-review")PlaneAnchorReview.CaptureWillowGround();
            else if(command=="photo-trees-review")PlaneAnchorReview.VerifyPhotographicTrees();
            else if(command=="photo-trees-game")PhotographicTreesReview.Capture();
            else if(command=="cypress-moss-review")PhotographicTreesReview.VerifyCypressMoss();
            else if(command=="cypress-moss-game")PhotographicTreesReview.CaptureCypressMoss();
            else if(command=="cypress-moss-pair-review")PhotographicTreesReview.VerifyCypressMossPair();
            else if(command=="cypress-moss-pair-game")PhotographicTreesReview.CaptureCypressMossPair();
            else if(command=="medium-conifers-review")PhotographicTreesReview.VerifyMediumConifers();
            else if(command=="medium-conifers-game")PhotographicTreesReview.CaptureMediumConifers();
            else if(command=="photographic-palms-review")PhotographicPalmsReview.Verify();
            else if(command=="photographic-palms-game")PhotographicPalmsReview.Capture();
            else if(command=="palm-heights-review")PhotographicPalmsReview.VerifyHeightVariants();
            else if(command=="palm-heights-game")PhotographicPalmsReview.CaptureHeightVariants();
            else if(command=="play"){EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
            else if(command=="resume")EditorApplication.isPaused=false;
            else if(command=="stop")EditorApplication.isPlaying=false;
            else if(command=="tests" || command=="forest-tests")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before running EditMode tests.");
                runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Results());
                runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,groupNames=new[]{command=="forest-tests" ? ".*(RegionFloraGeneratorTests|DistrictTreeHarvestTests|DistrictFloraBatchesTests|DistrictFloraPaintTests|DistrictFloraCoverageTests).*" : ".*(DistrictCloudLayerTests|RiverBankAppearanceTests|DistrictSurfaceCacheTests|DistrictRiverSculptTests|RegionRiverDrawingTests|RegionMapOrientationTests|NationalPikePlacementTests|DistrictRoadPlacementTests|RegionPikeStrokeTests|RegionMapLayersTests|RegionRiverNetworkTests|RegionTerrainMenuTests|DistrictElevationTests).*"}}));
            }
            else UnityEngine.Object.FindFirstObjectByType<CityForgeApp>().RegionMapLayersQa(command);
            File.WriteAllText(BridgePath("result"),"OK "+command);
        }
        catch(Exception e){File.WriteAllText(BridgePath("result"),e.ToString());Debug.LogException(e);}
    }
    sealed class Results:ICallbacks
    {
        public void RunStarted(ITestAdaptor t){}public void TestStarted(ITestAdaptor t){}public void TestFinished(ITestResultAdaptor t){}
        public void RunFinished(ITestResultAdaptor result){TestRunnerApi.SaveResultToFile(result,BridgePath("tests").Replace(".txt", ".xml"));}
    }
}
#endif
