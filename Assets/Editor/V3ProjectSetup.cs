using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.Editor
{
    public static class V3ProjectSetup
    {
        private const string SceneDirectory = "Assets/CityForgeV3/Scenes";
        private const string BootstrapScene = SceneDirectory + "/Bootstrap.unity";
        private const string UiResourceDirectory =
            "Assets/CityForgeV3/Resources/CityForgeV3/UI";
        private const string ThemeAsset =
            UiResourceDirectory + "/RuntimeTheme.tss";
        private const string PanelAsset =
            UiResourceDirectory + "/RuntimePanelSettings.asset";
        private const string MacOSBuildOutput = "foe.app";

        [InitializeOnLoadMethod]
        private static void ConfigureDefaultBuildLocation()
        {
            EditorApplication.delayCall += () =>
                EditorUserBuildSettings.SetBuildLocation(
                    BuildTarget.StandaloneOSX,
                    Path.GetFullPath(MacOSBuildOutput));
        }

        public static void Configure()
        {
            Directory.CreateDirectory(SceneDirectory);
            Directory.CreateDirectory(UiResourceDirectory);
            ConfigureRuntimePanel();

            if (!File.Exists(BootstrapScene))
            {
                var scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene, NewSceneMode.Single);
                _ = new GameObject("City Forge V3 Bootstrap");
                EditorSceneManager.SaveScene(scene, BootstrapScene);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScene, true)
            };

            PlayerSettings.companyName = "City Forge";
            PlayerSettings.productName = "City Forge V3";
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Standalone,
                "com.cityforge.v3");

            AssetDatabase.SaveAssets();
            Debug.Log("City Forge V3 project configuration complete.");
        }

        private static void ConfigureRuntimePanel()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemeAsset);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
                theme.name = "City Forge V3 Runtime Theme";
                AssetDatabase.CreateAsset(theme, ThemeAsset);
            }

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelAsset);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.name = "City Forge V3 Runtime Panel";
                AssetDatabase.CreateAsset(panel, PanelAsset);
            }

            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.sortingOrder = 100;
            EditorUtility.SetDirty(panel);
        }

        [MenuItem("City Forge/Build macOS Player")]
        public static void BuildMacOS()
        {
            BuildMacOS(BuildOptions.None);
        }

        [MenuItem("City Forge/Build and Run macOS Player")]
        public static void BuildAndRunMacOS()
        {
            BuildMacOS(BuildOptions.AutoRunPlayer);
        }

        private static void BuildMacOS(BuildOptions options)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "Exit Play Mode before building the City Forge V3 macOS player.");
                return;
            }

            Configure();
            var outputPath = Path.GetFullPath(MacOSBuildOutput);
            EditorUserBuildSettings.SetBuildLocation(
                BuildTarget.StandaloneOSX, outputPath);
            var report = BuildPipeline.BuildPlayer(
                EditorBuildSettings.scenes,
                outputPath,
                BuildTarget.StandaloneOSX,
                options);

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException(
                    $"City Forge V3 macOS build failed: {report.summary.result}");
            }

            Debug.Log($"City Forge V3 macOS build succeeded: {report.summary.totalSize} bytes.");
        }

        [MenuItem("City Forge/Build macOS Player", true)]
        private static bool CanBuildMacOS()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("City Forge/Build and Run macOS Player", true)]
        private static bool CanBuildAndRunMacOS()
        {
            return CanBuildMacOS();
        }
    }

}
