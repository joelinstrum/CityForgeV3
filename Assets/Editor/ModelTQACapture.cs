using System.IO;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.EditorTools
{
    public static class ModelTQACapture
    {
        public static void Capture()
        {
            var importedAsset = Resources.Load<GameObject>(VehicleRuntimePresentation.ModelTResourcePath);
            Debug.Log($"CITYFORGE_MODEL_T_IMPORTED_ROOT_ROTATION={importedAsset.transform.localEulerAngles}");
            var stage = new GameObject("Model T Unity QA Stage");
            var vehicle = VehicleRuntimePresentation.Create(stage.transform);
            vehicle.Place(Vector2.zero, Vector2.up);
            foreach (var renderer in vehicle.VisualRoot.GetComponentsInChildren<Renderer>(true))
                Debug.Log($"CITYFORGE_MODEL_T_RENDERER={renderer.name}|BOUNDS={renderer.bounds}|MATERIALS={string.Join(",", System.Array.ConvertAll(renderer.sharedMaterials, material => material == null ? "null" : $"{material.name}:{material.shader.name}"))}");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "QA Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(1.4f, 1f, 1.4f);
            var groundShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
            var groundMaterial = new Material(groundShader)
            {
                color = new Color(0.18f, 0.23f, 0.18f)
            };
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            var lightObject = new GameObject("QA Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.93f, 0.82f);
            lightObject.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            var cameraObject = new GameObject("QA Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.11f, 0.14f);
            camera.transform.position = new Vector3(5.2f, 3.4f, -6.4f);
            camera.transform.LookAt(new Vector3(0f, 0.9f, 0f));
            camera.fieldOfView = 38f;

            var target = new RenderTexture(1200, 800, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var outputFolder = Path.Combine(projectRoot, "QA", "LotEditorV46");
            Directory.CreateDirectory(outputFolder);
            File.WriteAllBytes(Path.Combine(outputFolder, "model-t-unity-import-v46.png"), image.EncodeToPNG());

            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(lightObject);
            Object.DestroyImmediate(groundMaterial);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(stage);
            Debug.Log("CITYFORGE_MODEL_T_QA_CAPTURE_COMPLETE");
        }

        public static void CaptureLotEditor()
        {
            var root = new GameObject("Model T Lot Editor QA");
            var world = root.AddComponent<LotWorldController>();
            world.Build();
            world.SetLotType(LotType.Neighborhood);
            var vehicle = root.GetComponentInChildren<VehicleRuntimePresentation>(true);
            var route = VehicleRoute.FromNetwork(world.Session.Data.VehicleNetwork);

            var camera = root.GetComponentInChildren<Camera>(true);
            var target = new RenderTexture(1400, 900, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var outputFolder = Path.Combine(projectRoot, "QA", "LotEditorV55");
            Directory.CreateDirectory(outputFolder);
            var routeFractions = new[] { 0.04f, 0.22f, 0.47f, 0.72f };
            for (var index = 0; index < routeFractions.Length; index++)
            {
                var preset = index < 2 ? TimeOfDayPreset.Night : TimeOfDayPreset.Afternoon;
                world.SetTimeOfDay(preset);
                var distance = route.TotalLengthMeters * routeFractions[index];
                route.Sample(distance, out var point, out _);
                world.SelectRoadCellAtWorld(point.x, point.y);
                world.SetZoomLevel(LotZoomLevel.Detail);
                world.DeselectAll();
                vehicle.Place(point, route.SmoothedDirection(distance), route.SteeringDegrees(distance));
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(
                    Path.Combine(outputFolder,
                        $"{preset.ToString().ToLowerInvariant()}-vehicle-lighting-v55-{index + 1:00}.png"),
                    image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }

            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
            Debug.Log("CITYFORGE_MODEL_T_LOT_EDITOR_CAPTURE_COMPLETE");
        }

        [MenuItem("City Forge/Capture Model T Color QA")]
        public static void CaptureColorVariants()
        {
            var root = new GameObject("Model T Color Variant QA");
            var world = root.AddComponent<LotWorldController>();
            world.Build();
            world.SetLotType(LotType.Neighborhood);
            // Afternoon gives the vehicle enough lateral light to make the
            // closed proxy's complete silhouette inspectable in every detail shot.
            world.SetTimeOfDay(TimeOfDayPreset.Afternoon);
            world.SetZoomLevel(LotZoomLevel.Lot);
            world.DeselectAll();
            foreach (var vehicle in root.GetComponentsInChildren<VehicleRuntimePresentation>(true))
            {
                var body = vehicle.VisualRoot.Find("CF_ModelT_LOD0_body");
                if (body != null)
                {
                    var filter = body.GetComponent<MeshFilter>();
                    Debug.Log($"CITYFORGE_COLOR_QA_BODY_BOUNDS={filter.sharedMesh.bounds}");
                }
            }

            var camera = root.GetComponentInChildren<Camera>(true);
            var target = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var outputFolder = Path.Combine(projectRoot, "QA", "LotEditorV57");
            Directory.CreateDirectory(outputFolder);
            File.WriteAllBytes(
                Path.Combine(outputFolder, "model-t-four-color-route-v57.png"),
                image.EncodeToPNG());

            Object.DestroyImmediate(image);

            var vehicles = root.GetComponentsInChildren<VehicleRuntimePresentation>(true);
            foreach (var vehicle in vehicles)
            {
                var point = vehicle.transform.localPosition;
                world.SelectRoadCellAtWorld(point.x, point.z);
                world.SetZoomLevel(LotZoomLevel.Detail);
                world.DeselectAll();
                camera.Render();
                RenderTexture.active = target;
                var detail = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                detail.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                detail.Apply();
                File.WriteAllBytes(
                    Path.Combine(outputFolder,
                        $"model-t-{vehicle.PaintVariant.ToString().ToLowerInvariant()}-shadow-v57.png"),
                    detail.EncodeToPNG());
                Object.DestroyImmediate(detail);
            }
            camera.targetTexture = null;
            RenderTexture.active = null;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
            Debug.Log("CITYFORGE_MODEL_T_COLOR_QA_CAPTURE_COMPLETE");
        }

        [MenuItem("City Forge/Capture Traffic Lane Graph QA")]
        public static void CaptureTrafficLaneGraph()
        {
            var root = new GameObject("Traffic Lane Graph QA");
            var world = root.AddComponent<LotWorldController>();
            world.Build();
            world.SetLotType(LotType.Neighborhood);
            world.SetTimeOfDay(TimeOfDayPreset.Noon);
            world.SetZoomLevel(LotZoomLevel.Lot);
            world.ToggleCirculationDiagnostics();
            world.DeselectAll();

            var camera = root.GetComponentInChildren<Camera>(true);
            var target = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var outputFolder = Path.Combine(projectRoot, "QA", "LotEditorV61");
            Directory.CreateDirectory(outputFolder);
            File.WriteAllBytes(Path.Combine(outputFolder, "t-intersection-lane-graph-v61.png"),
                image.EncodeToPNG());

            Object.DestroyImmediate(image);
            camera.targetTexture = null;
            RenderTexture.active = null;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
            Debug.Log("CITYFORGE_TRAFFIC_LANE_GRAPH_QA_CAPTURE_COMPLETE");
        }
    }
}
