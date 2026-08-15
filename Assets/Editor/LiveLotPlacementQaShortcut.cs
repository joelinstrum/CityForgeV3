using CityForgeV3.UI;
using CityForgeV3.World;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class LiveLotPlacementQaShortcut
{
    private const string BuildingId =
        "cityforge.base.building.commercial.art_deco_corner_building_01";
    private const string BeauxArtsBuildingId =
        "cityforge.base.building.commercial.beaux_arts_commercial_01";
    private const string MarloweHotelBuildingId =
        "cityforge.base.building_marlowe_art_deco_hotel_02";
    private const string PubQaBuildingId =
        "cityforge.qa.building.commercial.pub_qa_20deg_05";
    private static string _requestedBuildingId = BuildingId;
    private static string _requestedSavedLotId = "";
    private static int _startupFramesRemaining;
    private static GameObject _qaRoot;
    private static LotWorldController _qaWorld;
    private const string NightTriggerPath = "/tmp/cityforge-open-art-deco-night-qa";
    private const string MorningTriggerPath = "/tmp/cityforge-open-art-deco-morning-qa";
    private const string BeauxAfternoonTriggerPath = "/tmp/cityforge-open-beaux-afternoon-qa";
    private const string BeauxNightTriggerPath = "/tmp/cityforge-open-beaux-night-qa";
    private const string MarloweAfternoonTriggerPath = "/tmp/cityforge-open-marlowe-afternoon-qa";
    private const string MarloweNightTriggerPath = "/tmp/cityforge-open-marlowe-night-qa";
    private const string PubQaAfternoonTriggerPath = "/tmp/cityforge-open-pub-qa-afternoon";
    private const string PubQaMorningTriggerPath = "/tmp/cityforge-open-pub-qa-morning";
    private const string PubQaNoonTriggerPath = "/tmp/cityforge-open-pub-qa-noon";
    // Persists the requested preset across Unity's play-mode domain reload.
    private const string PendingTriggerPath = "/tmp/cityforge-art-deco-qa-pending";
    private static TimeOfDayPreset _requestedTime = TimeOfDayPreset.Afternoon;

    [InitializeOnLoadMethod]
    private static void RegisterFileTriggers()
    {
        EditorApplication.update -= PollFileTriggers;
        EditorApplication.update += PollFileTriggers;
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(PendingTriggerPath))
            {
                var requested = File.ReadAllText(PendingTriggerPath).Trim();
                var pending = requested.Split('|');
                if (pending.Length == 2)
                {
                    if (pending[0].StartsWith("LOT:"))
                        _requestedSavedLotId = pending[0].Substring(4);
                    else
                    {
                        _requestedSavedLotId = "";
                        _requestedBuildingId = pending[0];
                    }
                }
                var requestedPreset = pending.Length == 2 ? pending[1] : requested;
                _requestedTime = requestedPreset == nameof(TimeOfDayPreset.Night)
                    ? TimeOfDayPreset.Night
                    : TimeOfDayPreset.Morning;
                if (requestedPreset == nameof(TimeOfDayPreset.Afternoon))
                    _requestedTime = TimeOfDayPreset.Afternoon;
                if (EditorApplication.isPlaying)
                {
                    File.Delete(PendingTriggerPath);
                    _startupFramesRemaining = 12;
                    EditorApplication.update -= OpenWhenRuntimeIsReady;
                    EditorApplication.update += OpenWhenRuntimeIsReady;
                }
                return;
            }
            if (File.Exists(NightTriggerPath))
            {
                File.Delete(NightTriggerPath);
                _requestedTime = TimeOfDayPreset.Night;
                OpenArtDecoCornerLivePlacement();
            }
            else if (File.Exists(MorningTriggerPath))
            {
                File.Delete(MorningTriggerPath);
                _requestedTime = TimeOfDayPreset.Morning;
                OpenArtDecoCornerLivePlacement();
            }
        };
    }

    private static void PollFileTriggers()
    {
        if (File.Exists(BeauxAfternoonTriggerPath))
        {
            File.Delete(BeauxAfternoonTriggerPath);
            _requestedBuildingId = BeauxArtsBuildingId;
            _requestedTime = TimeOfDayPreset.Afternoon;
            OpenArtDecoCornerLivePlacement();
        }
        else if (File.Exists(PubQaMorningTriggerPath))
        {
            File.Delete(PubQaMorningTriggerPath);
            _requestedBuildingId = PubQaBuildingId;
            _requestedTime = TimeOfDayPreset.Morning;
            OpenArtDecoCornerLivePlacement();
        }
        else if (File.Exists(PubQaNoonTriggerPath))
        {
            File.Delete(PubQaNoonTriggerPath);
            _requestedBuildingId = PubQaBuildingId;
            _requestedTime = TimeOfDayPreset.Noon;
            OpenArtDecoCornerLivePlacement();
        }
        else if (File.Exists(PubQaAfternoonTriggerPath))
        {
            File.Delete(PubQaAfternoonTriggerPath);
            _requestedBuildingId = PubQaBuildingId;
            _requestedTime = TimeOfDayPreset.Afternoon;
            OpenArtDecoCornerLivePlacement();
        }
        else if (File.Exists(BeauxNightTriggerPath))
        {
            File.Delete(BeauxNightTriggerPath);
            _requestedBuildingId = BeauxArtsBuildingId;
            _requestedTime = TimeOfDayPreset.Night;
            OpenArtDecoCornerLivePlacement();
        }
        else if (File.Exists(MarloweAfternoonTriggerPath))
        {
            File.Delete(MarloweAfternoonTriggerPath);
            _requestedBuildingId = MarloweHotelBuildingId;
            _requestedTime = TimeOfDayPreset.Afternoon;
            OpenArtDecoCornerLivePlacement();
        }
        else if (File.Exists(MarloweNightTriggerPath))
        {
            File.Delete(MarloweNightTriggerPath);
            _requestedBuildingId = MarloweHotelBuildingId;
            _requestedTime = TimeOfDayPreset.Night;
            OpenArtDecoCornerLivePlacement();
        }
    }

    [MenuItem("City Forge/QA/Open Art Deco Corner Live Placement _F8")]
    private static void OpenArtDecoCornerLivePlacement()
    {
        if (!EditorApplication.isPlaying)
        {
            File.WriteAllText(PendingTriggerPath,
                $"{_requestedBuildingId}|{_requestedTime}");
            EditorApplication.isPlaying = true;
            return;
        }

        OpenPersistentQaWorld();
    }

    [MenuItem("City Forge/QA/Open Beaux Arts Commercial Afternoon Live Placement")]
    private static void OpenBeauxArtsCommercialAfternoonLivePlacement()
    {
        _requestedBuildingId = BeauxArtsBuildingId;
        _requestedTime = TimeOfDayPreset.Afternoon;
        OpenArtDecoCornerLivePlacement();
    }

    [MenuItem("City Forge/QA/Open Pub QA 2 Afternoon Live Placement")]
    private static void OpenPubQaAfternoonLivePlacement()
    {
        _requestedSavedLotId = "";
        _requestedBuildingId = PubQaBuildingId;
        _requestedTime = TimeOfDayPreset.Afternoon;
        OpenArtDecoCornerLivePlacement();
    }

    [MenuItem("City Forge/QA/Open Boston Pub Lot Flora Shadow QA")]
    private static void OpenBostonPubLotFloraShadowQa()
    {
        _requestedSavedLotId = "untitled-lot";
        _requestedTime = TimeOfDayPreset.Afternoon;
        if (!EditorApplication.isPlaying)
        {
            File.WriteAllText(PendingTriggerPath,
                $"LOT:{_requestedSavedLotId}|{_requestedTime}");
            EditorApplication.isPlaying = true;
            return;
        }
        OpenPersistentQaWorld();
    }

    [MenuItem("City Forge/QA/Open Beaux Arts Commercial Night Live Placement")]
    private static void OpenBeauxArtsCommercialNightLivePlacement()
    {
        _requestedBuildingId = BeauxArtsBuildingId;
        _requestedTime = TimeOfDayPreset.Night;
        OpenArtDecoCornerLivePlacement();
    }

    [MenuItem("City Forge/QA/Open Art Deco Corner Night Live Placement")]
    private static void OpenArtDecoCornerNightLivePlacement()
    {
        _requestedTime = TimeOfDayPreset.Night;
        OpenArtDecoCornerLivePlacement();
    }

    [MenuItem("City Forge/QA/Open Art Deco Corner Morning Live Placement")]
    private static void OpenArtDecoCornerMorningLivePlacement()
    {
        _requestedTime = TimeOfDayPreset.Morning;
        OpenArtDecoCornerLivePlacement();
    }

    private static void OpenWhenRuntimeIsReady()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.update -= OpenWhenRuntimeIsReady;
            return;
        }

        if (--_startupFramesRemaining > 0)
            return;

        EditorApplication.update -= OpenWhenRuntimeIsReady;
        OpenPersistentQaWorld();
    }

    private static void OpenPersistentQaWorld()
    {
        if (_qaRoot != null)
            Object.Destroy(_qaRoot);

        HybridBuildingPackageRegistry.InvalidateCache();

        // Build the same runtime LotWorldController used by the editor, but do
        // not route this visual check through the splash/UI bootstrap. That
        // keeps the test persistent in the windowed Game view and makes any
        // camera disagreement immediately visible instead of hiding it behind
        // a menu transition.
        _qaRoot = new GameObject("Building Live Placement QA");
        _qaRoot.AddComponent<QaUiSuppressor>();
        var world = _qaRoot.AddComponent<LotWorldController>();
        _qaWorld = world;
        world.Build();
        if (!string.IsNullOrWhiteSpace(_requestedSavedLotId))
        {
            if (!world.LoadLot(_requestedSavedLotId))
            {
                Debug.LogError($"Saved lot {_requestedSavedLotId} could not be loaded for flora-shadow QA.");
                return;
            }
            world.SetFloraEditorContext(true);
        }
        else
        {
            world.ConfigureLot("Building QA", LotType.Commercial, 5, 5);
            if (!world.PlaceBuildingAtCenter(_requestedBuildingId))
            {
                Debug.LogError($"Building {_requestedBuildingId} could not be placed in the live QA lot.");
                return;
            }
        }

        world.SetBuildingEditorContext(true, false);
        world.SetInspectionMode(BuildingInspectionMode.Artwork);
        world.SetTimeOfDay(_requestedTime);
        world.SetZoomLevel(
            !string.IsNullOrWhiteSpace(_requestedSavedLotId)
                ? LotZoomLevel.Lot
                : _requestedBuildingId == PubQaBuildingId
                ? LotZoomLevel.Close
                : LotZoomLevel.Wide);

        // Keep the exact saved-lot regression fixture large enough to inspect
        // all five lamppost silhouettes and their shadow anchors in one frame.
        // This is QA-only framing; production camera behavior is unchanged.
        if (!string.IsNullOrWhiteSpace(_requestedSavedLotId))
            world.SetQaOrthographicSize(22f);

        // The bootstrap splash is a full-screen UIDocument and can visually
        // cover a correctly rendered QA world. Disable only the app UI after
        // the production world has been built; the lot camera now lives under
        // _qaRoot and remains active in the normal, windowed Game view.
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app != null)
            app.gameObject.SetActive(false);

        EditorApplication.delayCall += () =>
            EditorApplication.delayCall += DumpLivePlacement;
    }

    private sealed class QaUiSuppressor : MonoBehaviour
    {
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.R)) return;
            var world = Object.FindFirstObjectByType<LotWorldController>();
            if (world != null && world.name == "Building Live Placement QA")
                world.RotateSelected(1);
        }

        private void LateUpdate()
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (document != null)
                    document.enabled = false;
            }
        }
    }

    [MenuItem("City Forge/QA/Set Live QA Morning")]
    private static void SetLiveQaMorning() => SetLiveQaTime(TimeOfDayPreset.Morning);

    [MenuItem("City Forge/QA/Set Live QA Noon")]
    private static void SetLiveQaNoon() => SetLiveQaTime(TimeOfDayPreset.Noon);

    [MenuItem("City Forge/QA/Set Live QA Afternoon")]
    private static void SetLiveQaAfternoon() => SetLiveQaTime(TimeOfDayPreset.Afternoon);

    [MenuItem("City Forge/QA/Rotate Live QA Building Clockwise")]
    private static void RotateLiveQaBuildingClockwise()
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
            if (world.name == "Building Live Placement QA")
                world.RotateSelected(1);
        EditorApplication.delayCall += DumpLivePlacement;
    }

    private static void SetLiveQaTime(TimeOfDayPreset preset)
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
            if (world.name == "Building Live Placement QA")
                world.SetTimeOfDay(preset);
        EditorApplication.delayCall += DumpLivePlacement;
    }

    [MenuItem("City Forge/QA/Boston Flora Shadow/Move Tree — Lit")]
    private static void MoveBostonTreeLit() => MoveBostonTreeForQa(11f, 11.6f);

    [MenuItem("City Forge/QA/Boston Flora Shadow/Move Tree — Partial")]
    private static void MoveBostonTreePartial() => MoveBostonTreeForQa(7.5f, 11.6f);

    [MenuItem("City Forge/QA/Boston Flora Shadow/Move Tree — Full")]
    private static void MoveBostonTreeFull() => MoveBostonTreeForQa(1.4f, 11.6f);

    [MenuItem("City Forge/QA/Boston Flora Shadow/Move Tree — Wall Overlap")]
    private static void MoveBostonTreeWallOverlap() =>
        MoveBostonTreeForQa(5.2f, 1.0f);

    [MenuItem("City Forge/QA/Boston Flora Shadow/Move Tree — Front Collision")]
    private static void MoveBostonTreeFrontCollision() =>
        MoveBostonTreeForQa(6.0f, 5.0f);

    [MenuItem("City Forge/QA/Boston Flora Shadow/Move Tree — Side Collision")]
    private static void MoveBostonTreeSideCollision() =>
        MoveBostonTreeForQa(7.0f, 1.0f);

    [MenuItem("City Forge/QA/Boston Flora Shadow/Place Front Lamppost")]
    private static void PlaceBostonFrontLamppost()
    {
        if (_qaWorld != null)
        {
            _qaWorld.PlacePropForQa("three-lantern-lamppost-v01", 9f, 0f);
            EditorApplication.delayCall += DumpLivePlacement;
            return;
        }
        LotWorldController fallback = null;
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
        {
            fallback ??= world;
            if (world.name == "Building Live Placement QA")
            {
                // Deliberately tight to the camera-facing facade so this
                // shortcut exercises the building-proxy clearance boundary.
                world.PlacePropForQa("three-lantern-lamppost-v01", 9f, 0f);
                fallback = null;
                break;
            }
        }
        fallback?.PlacePropForQa("three-lantern-lamppost-v01", 9f, 0f);
        EditorApplication.delayCall += DumpLivePlacement;
    }

    [MenuItem("City Forge/QA/Boston Flora Shadow/Place Rear Lamppost")]
    private static void PlaceBostonRearLamppost()
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
        {
            if (world.name == "Building Live Placement QA")
                world.PlacePropForQa("three-lantern-lamppost-v01", -7f, 0f);
        }
        EditorApplication.delayCall += DumpLivePlacement;
    }

    private static void MoveBostonTreeForQa(float x, float z)
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
        {
            if (world.name == "Building Live Placement QA")
                world.SetFloraPositionForQa(0, x, z);
        }
        EditorApplication.delayCall += DumpLivePlacement;
    }

    private static void DumpLivePlacement()
    {
        var report = new StringBuilder();
        Camera lotCamera = null;
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
        {
            report.AppendLine($"world {world.name} time={world.TimeOfDay} " +
                $"shadowVisible={world.ProjectedShadowVisible} " +
                $"shadowVertices={world.ProjectedShadowVertexCount} " +
                $"shadowBounds={world.ProjectedShadowBounds} " +
                $"shadowDirection={world.ProjectedShadowLocalDirection} " +
                $"shadowOffset={world.BuildingShadowDirectionOffsetDegrees:0.#} " +
                $"sourceVertices={world.ProjectedShadowSourceVertexCount}");
        }
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            report.AppendLine($"camera {camera.name} enabled={camera.enabled} tag={camera.tag} depth={camera.depth} target={camera.targetTexture?.name} pos={camera.transform.position} rot={camera.transform.eulerAngles} ortho={camera.orthographicSize} aspect={camera.aspect}");
            if (camera.name == "Lot Camera") lotCamera = camera;
        }
        foreach (var presentation in Object.FindObjectsByType<HybridBuildingPresentation>(FindObjectsSortMode.None))
        {
            report.AppendLine($"presentation {presentation.name} pos={presentation.transform.position} rot={presentation.transform.eulerAngles} scale={presentation.transform.lossyScale}");
            foreach (var renderer in presentation.GetComponentsInChildren<SpriteRenderer>(true))
            {
                report.AppendLine($"  renderer {renderer.name} enabled={renderer.enabled} color={renderer.color} pos={renderer.transform.position} rot={renderer.transform.eulerAngles} scale={renderer.transform.lossyScale} bounds={renderer.bounds} sprite={renderer.sprite?.name} rect={renderer.sprite?.rect}");
            }
        }
        foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (!renderer.name.StartsWith("Flora —")) continue;
            report.AppendLine($"flora {renderer.name} bounds={renderer.bounds} material={renderer.sharedMaterial?.name} shader={renderer.sharedMaterial?.shader?.name} receive={renderer.receiveShadows} cast={renderer.shadowCastingMode}");
        }
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!renderer.name.StartsWith("CF_PROXY_")) continue;
            report.AppendLine($"proxy {renderer.name} enabled={renderer.enabled} bounds={renderer.bounds} cast={renderer.shadowCastingMode}");
        }
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
        foreach (Transform child in world.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "Three-Lantern Lamppost Model" &&
                child.name != "Committed Prop Depth Prepass" &&
                child.name != "Projected Prop Silhouette") continue;
            report.AppendLine($"prop-part {child.parent?.name}/{child.name} " +
                $"pos={child.position} local={child.localPosition}");
            foreach (var renderer in child.GetComponentsInChildren<Renderer>())
                report.AppendLine($"  prop-renderer {renderer.name} enabled={renderer.enabled} " +
                    $"shader={renderer.sharedMaterial?.shader?.name} " +
                    $"queue={renderer.sharedMaterial?.renderQueue} bounds={renderer.bounds}");
        }
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            report.AppendLine($"light {light.name} enabled={light.enabled} type={light.type} shadows={light.shadows} strength={light.shadowStrength} rot={light.transform.eulerAngles}");
        File.WriteAllText("/tmp/cityforge-live-transform.txt", report.ToString());
        if (lotCamera != null)
            CaptureCamera(lotCamera, "/tmp/cityforge-live-placement.png");
        Debug.Log(report.ToString());
    }

    private static void CaptureCamera(Camera camera, string path)
    {
        const int width = 1600;
        const int height = 1000;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var priorTarget = camera.targetTexture;
        var priorActive = RenderTexture.active;
        var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = priorTarget;
            RenderTexture.active = priorActive;
            Object.Destroy(image);
            Object.Destroy(target);
        }
    }
}
