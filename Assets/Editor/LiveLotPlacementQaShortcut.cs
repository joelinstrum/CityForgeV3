using CityForgeV3.UI;
using CityForgeV3.World;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class LiveLotPlacementQaShortcut
{
    [MenuItem("City Forge/QA/Arm Tea Storefront for Live Placement")]
    private static void ArmTeaStorefrontForLivePlacement()
    {
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app != null && app.ArmBuildingPropForLiveQa(
                BuildingPropCatalog.TeaShopStorefrontId))
            Debug.Log("CF_QA_TEA_STOREFRONT_ARMED");
        else
            Debug.LogError("Open a live lot before arming the Tea storefront QA.");
    }

    [MenuItem("City Forge/QA/Open Street Lot With Storefront")]
    private static void OpenStreetLotWithStorefront()
    {
        if (!EditorApplication.isPlaying)
        {
            _requestedSavedLotId = "street-lot";
            _requestedTime = TimeOfDayPreset.Noon;
            File.WriteAllText(PendingTriggerPath,
                $"LOT:{_requestedSavedLotId}|{_requestedTime}");
            EditorApplication.isPlaying = true;
            EditorApplication.delayCall += PlaceStreetLotStorefrontWhenReady;
            return;
        }
        PlaceStreetLotStorefrontWhenReady();
    }

    private static void PlaceStreetLotStorefrontWhenReady()
    {
        if (!EditorApplication.isPlaying) return;
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenSavedLotSelectionQa("street-lot"))
        {
            EditorApplication.delayCall += PlaceStreetLotStorefrontWhenReady;
            return;
        }
        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world == null || !world.CommitBuildingProp3DForQa(
                BuildingPropCatalog.TeaShopStorefrontId, 1,
                "Front", 0f, 1.84f))
        {
            Debug.LogError("Street Lot storefront could not be attached.");
            return;
        }
        var path = world.SaveLot();
        world.SetZoomLevel(LotZoomLevel.Close);
        world.SetQaOrthographicSize(9f);
        world.SetQaCameraPan(-24.16f, 15.09f);
        Debug.Log($"CF_QA_STREET_LOT_STOREFRONT_SAVED {path}");
    }

    private const string BuildingId =
        "cityforge.base.building.commercial.art_deco_corner_building_01";
    private const string BeauxArtsBuildingId =
        "cityforge.base.building.commercial.beaux_arts_commercial_01";
    private const string MarloweHotelBuildingId =
        "cityforge.base.building_marlowe_art_deco_hotel_02";
    private const string FrontierLogCabinTripoId =
        "cityforge.v3.residential.frontier_log_cabin_tripo_01";
    private const string NewEnglandChurchTripoId =
        "cityforge.v3.civics.culture.new_england_church_tripo_01";
    private const string LawOfficeTripoId =
        "cityforge.v3.commercial.law_office_tripo_01";
    private static string _requestedBuildingId = BuildingId;
    private static string _requestedSavedLotId = "";
    private static int _startupFramesRemaining;
    private static GameObject _qaRoot;
    private static LotWorldController _qaWorld;
    private static bool _showAleHousePreviewAfterOpen;
    private const string NightTriggerPath = "/tmp/cityforge-open-art-deco-night-qa";
    private const string MorningTriggerPath = "/tmp/cityforge-open-art-deco-morning-qa";
    private const string BeauxAfternoonTriggerPath = "/tmp/cityforge-open-beaux-afternoon-qa";
    private const string BeauxNightTriggerPath = "/tmp/cityforge-open-beaux-night-qa";
    private const string MarloweAfternoonTriggerPath = "/tmp/cityforge-open-marlowe-afternoon-qa";
    private const string MarloweNightTriggerPath = "/tmp/cityforge-open-marlowe-night-qa";
    private const string AleHousePreviewTriggerPath =
        "/tmp/cityforge-show-ale-house-building-prop-preview";
    private const string BostonPropQaTriggerPath =
        "/tmp/cityforge-open-boston-building-prop-qa";
    private const string AleHouseCommitTriggerPath =
        "/tmp/cityforge-commit-ale-house-building-prop-qa";
    private const string WinterFloraTimeTriggerPath =
        "/tmp/cityforge-set-winter-flora-time-qa";
    private const string StreetCarPanUpTriggerPath =
        "/tmp/cityforge-pan-streetcar-qa-up";
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
                if (pending.Length >= 2)
                {
                    if (pending[0].StartsWith("LOT:"))
                        _requestedSavedLotId = pending[0].Substring(4);
                    else
                    {
                        _requestedSavedLotId = "";
                        _requestedBuildingId = pending[0];
                    }
                }
                _showAleHousePreviewAfterOpen = pending.Length >= 3 &&
                    pending[2] == "ALEHOUSE";
                var requestedPreset = pending.Length >= 2 ? pending[1] : requested;
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
        if (File.Exists(StreetCarPanUpTriggerPath))
        {
            File.Delete(StreetCarPanUpTriggerPath);
            var world = Object.FindFirstObjectByType<LotWorldController>();
            if (world == null)
                Debug.LogError("Could not find the active StreetCar QA world.");
            else
                world.PanCameraViewport(0, 1);
        }
        else if (File.Exists(WinterFloraTimeTriggerPath))
        {
            var requestedPreset = File.ReadAllText(
                WinterFloraTimeTriggerPath).Trim();
            File.Delete(WinterFloraTimeTriggerPath);
            var world = Object.FindFirstObjectByType<LotWorldController>();
            if (world == null)
                Debug.LogError("Could not find the active Lot Editor world.");
            else
            {
                world.SetSeason(SeasonPreset.Winter);
                world.SetTimeOfDay(requestedPreset ==
                    nameof(TimeOfDayPreset.Morning)
                        ? TimeOfDayPreset.Morning
                        : TimeOfDayPreset.Afternoon);
            }
        }
        else if (File.Exists(AleHouseCommitTriggerPath))
        {
            File.Delete(AleHouseCommitTriggerPath);
            if (_qaWorld == null || !_qaWorld.CommitBuildingPropForQa(
                    BuildingPropCatalog.AleHouseSignId, 0, 0.50f, 0.30f))
                Debug.LogError("The Ale House QA prop could not be committed.");
            else
                _qaWorld.SetBuildingPropQaCameraZoom(6f);
        }
        else if (File.Exists(BostonPropQaTriggerPath))
        {
            File.Delete(BostonPropQaTriggerPath);
            _requestedSavedLotId = "untitled-lot";
            _requestedTime = TimeOfDayPreset.Afternoon;
            if (!EditorApplication.isPlaying)
            {
                File.WriteAllText(PendingTriggerPath,
                    $"LOT:{_requestedSavedLotId}|{_requestedTime}|ALEHOUSE");
                EditorApplication.isPlaying = true;
            }
            else
            {
                _showAleHousePreviewAfterOpen = true;
                OpenPersistentQaWorld();
            }
        }
        else if (File.Exists(AleHousePreviewTriggerPath))
        {
            File.Delete(AleHousePreviewTriggerPath);
            ShowAleHouseBuildingPropPreview();
        }
        else if (File.Exists(BeauxAfternoonTriggerPath))
        {
            File.Delete(BeauxAfternoonTriggerPath);
            _requestedBuildingId = BeauxArtsBuildingId;
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

    [MenuItem("City Forge/QA/Open Frontier Log Cabin Tree Occlusion")]
    private static void OpenFrontierLogCabinTreeOcclusion()
    {
        _requestedSavedLotId = "";
        _requestedBuildingId = FrontierLogCabinTripoId;
        _requestedTime = TimeOfDayPreset.Afternoon;
        OpenArtDecoCornerLivePlacement();
    }

    [MenuItem("City Forge/QA/Open Frontier Log Cabin Primitive Overlay")]
    private static void OpenFrontierLogCabinPrimitiveOverlay()
    {
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null)
        {
            Debug.LogError("Could not find the active City Forge app.");
            return;
        }
        app.OpenBuildingInspectionQa(FrontierLogCabinTripoId);
    }

    [MenuItem("City Forge/QA/Open New England Church Inspection")]
    private static void OpenNewEnglandChurchInspection()
    {
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null)
        {
            Debug.LogError("Could not find the active City Forge app.");
            return;
        }
        app.OpenBuildingInspectionQa(NewEnglandChurchTripoId);
    }

    [MenuItem("City Forge/QA/Open Law Office Inspection")]
    private static void OpenLawOfficeInspection()
    {
        _requestedSavedLotId = "";
        _requestedBuildingId = LawOfficeTripoId;
        _requestedTime = TimeOfDayPreset.Afternoon;
        OpenArtDecoCornerLivePlacement();
    }

    [MenuItem("City Forge/QA/Set Live QA Winter Flora")]
    private static void SetLiveQaWinterFlora()
    {
        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world == null)
        {
            Debug.LogError("Could not find the active Lot Editor world.");
            return;
        }

        world.SetTimeOfDay(TimeOfDayPreset.Afternoon);
        world.SetFloraPlacementPreview("ashe");
        world.SetSeason(SeasonPreset.Winter);
        world.PlaceFloraForQa("ashe", -15f, -12f);
        world.PlaceFloraForQa("maple", 15f, 12f);
        world.StartWinterSnowfall();
    }

    [MenuItem("City Forge/QA/Start Live QA Winter Snowfall")]
    private static void StartLiveQaWinterSnowfall()
    {
        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world == null)
        {
            Debug.LogError("Could not find the active Lot Editor world.");
            return;
        }
        world.SetSeason(SeasonPreset.Winter);
        if (!world.StartWinterSnowfall())
            Debug.LogWarning("Winter snowfall is already active.");
    }

    [MenuItem("City Forge/QA/Open Winter Snowfall QA _F9")]
    private static void OpenWinterSnowfallQa()
    {
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null)
        {
            Debug.LogError("Could not find the active City Forge app.");
            return;
        }
        app.OpenBuildingInspectionQa(NewEnglandChurchTripoId);
        EditorApplication.delayCall += () =>
        {
            var world = Object.FindFirstObjectByType<LotWorldController>();
            if (world == null) return;
            world.SetSeason(SeasonPreset.Winter);
            world.StartWinterSnowfall();
        };
    }

    [MenuItem("City Forge/QA/Show Selected Building Primitive Overlay")]
    private static void ShowSelectedBuildingPrimitiveOverlay()
    {
        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world == null)
        {
            Debug.LogError("Could not find the active Lot Editor world.");
            return;
        }
        world.SetInspectionMode(BuildingInspectionMode.Hybrid);
    }

    [MenuItem("City Forge/QA/Show Selected Building Artwork")]
    private static void ShowSelectedBuildingArtwork()
    {
        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world == null)
        {
            Debug.LogError("Could not find the active Lot Editor world.");
            return;
        }
        world.SetInspectionMode(BuildingInspectionMode.Artwork);
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

    [MenuItem("City Forge/QA/Open Boston Pub Buildings Rotation QA")]
    private static void OpenBostonPubBuildingsRotationQa()
    {
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenSavedLotBuildingRotationQa("untitled-lot"))
            Debug.LogError("Could not open the saved Boston Pub Buildings rotation QA.");
    }

    [MenuItem("City Forge/QA/Open SanFranciscoLot Occlusion QA")]
    private static void OpenSanFranciscoLotOcclusionQa()
    {
        _requestedSavedLotId = "sanfranciscolot";
        _requestedTime = TimeOfDayPreset.Noon;
        OpenPersistentQaWorld();
        if (_qaWorld == null) return;
        _qaWorld.SelectBuildingAtLotPoint(new Vector2(-12f, -20f));
        _qaWorld.SetInspectionMode(BuildingInspectionMode.Artwork);
        _qaWorld.SetZoomLevel(LotZoomLevel.Detail);
        _qaWorld.SetQaOrthographicSize(10f);
        _qaWorld.SetQaCameraPan(-12f, -20f);
        EditorApplication.delayCall += DumpLivePlacement;
    }

    [MenuItem("City Forge/QA/Open StreetCar Pan Visibility QA _F10")]
    private static void OpenStreetCarPanVisibilityQa()
    {
        _requestedSavedLotId = "streetcar-test";
        _requestedTime = TimeOfDayPreset.Night;
        if (!EditorApplication.isPlaying)
        {
            File.WriteAllText(PendingTriggerPath,
                $"LOT:{_requestedSavedLotId}|{_requestedTime}");
            EditorApplication.isPlaying = true;
            return;
        }
        OpenPersistentQaWorld();
    }

    [MenuItem("City Forge/QA/Pan StreetCar QA Up _F11")]
    private static void PanStreetCarQaUp()
    {
        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world == null)
        {
            Debug.LogError("Could not find the active StreetCar QA world.");
            return;
        }
        world.PanCameraViewport(0, 1);
    }

    [MenuItem("City Forge/QA/Select SanFranciscoLot Green Occluder")]
    private static void SelectSanFranciscoLotGreenOccluder()
    {
        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world == null || !world.SelectBuildingAtLotPoint(new Vector2(-12f, -20f)))
            Debug.LogError("Could not select the rotated Green Victorian in SanFranciscoLot.");
        else
            EditorApplication.delayCall += DumpLivePlacement;
    }

    [MenuItem("City Forge/QA/Select SanFranciscoLot Red Occluder")]
    private static void SelectSanFranciscoLotRedOccluder()
    {
        var world = Object.FindFirstObjectByType<LotWorldController>();
        if (world == null || !world.SelectBuildingAtLotPoint(new Vector2(-6f, -20f)))
            Debug.LogError("Could not select the rotated Red Victorian in SanFranciscoLot.");
        else
            EditorApplication.delayCall += DumpLivePlacement;
    }

    [MenuItem("City Forge/QA/Prepare Active Lot Primitive Occlusion QA")]
    private static void PrepareActiveLotPrimitiveOcclusionQa()
    {
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null)
        {
            Debug.LogError("Could not find the active City Forge app.");
            return;
        }
        app.PrepareOcclusionQaView();
    }

    [MenuItem("City Forge/QA/Rotate Boston Pub Buildings QA Clockwise")]
    private static void RotateBostonPubBuildingsQaClockwise()
    {
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null)
        {
            Debug.LogError("Could not find the active City Forge app.");
            return;
        }
        app.RotateSelectedBuildingForQa(1);
    }

    [MenuItem("City Forge/QA/Show Ale House Building-Prop Preview")]
    private static void ShowAleHouseBuildingPropPreview()
    {
        if (_qaWorld == null)
        {
            Debug.LogError("Open the Boston Pub QA lot before showing the Ale House preview.");
            return;
        }
        _qaWorld.SetBuildingPropQaCameraZoom(6f);
        if (!_qaWorld.ShowBuildingPropPreviewForQa(
                BuildingPropCatalog.AleHouseSignId, 0, 0.50f, 0.30f))
            Debug.LogError("The Ale House preview could not be shown on the QA building.");
    }

    [MenuItem("City Forge/QA/Run Building-Prop Selection Move QA")]
    private static void RunBuildingPropSelectionMoveQa()
    {
        if (_qaWorld == null)
        {
            Debug.LogError("Open the Boston Pub QA lot before running building-prop selection QA.");
            return;
        }
        if (_qaWorld.RunBuildingPropSelectionMoveQa(36f))
            Debug.Log("Building-prop QA selected the prop ahead of its host, " +
                "moved it on the facade, and retained its selection highlight.");
        else
            Debug.LogError("Building-prop selection/move QA failed.");
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
        // Play-mode script recompiles reset these static fields without
        // destroying the already-created QA scene object. Remove every stale
        // QA root synchronously so a capture can never contain two worlds,
        // cameras, proxy sets, or flora presentations composited together.
        foreach (var existingWorld in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
        {
            if (existingWorld.transform.root.name == "Building Live Placement QA")
                Object.DestroyImmediate(existingWorld.transform.root.gameObject);
        }
        _qaRoot = null;
        _qaWorld = null;

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
            if (_requestedBuildingId == FrontierLogCabinTripoId)
            {
                world.PlaceFloraForQa("maple", -4.6f, 2.8f);
                world.PlaceFloraForQa("oak", 4.4f, 2.4f);
                world.PlaceFloraForQa("maple", 0.0f, 5.8f);
            }
        }

        world.SetBuildingEditorContext(true, false);
        world.SetInspectionMode(BuildingInspectionMode.Artwork);
        world.SetTimeOfDay(_requestedTime);
        world.SetZoomLevel(
            !string.IsNullOrWhiteSpace(_requestedSavedLotId)
                ? LotZoomLevel.Lot
                : _requestedBuildingId == FrontierLogCabinTripoId
                ? LotZoomLevel.Close
                : LotZoomLevel.Wide);

        // Keep the exact saved-lot regression fixture large enough to inspect
        // all five lamppost silhouettes and their shadow anchors in one frame.
        // This is QA-only framing; production camera behavior is unchanged.
        if (_requestedSavedLotId == "streetcar-test")
            world.SetQaOrthographicSize(8f);
        else if (!string.IsNullOrWhiteSpace(_requestedSavedLotId))
            world.SetQaOrthographicSize(22f);
        else if (_requestedBuildingId == FrontierLogCabinTripoId)
            world.SetQaOrthographicSize(7f);
        if (_showAleHousePreviewAfterOpen)
        {
            _showAleHousePreviewAfterOpen = false;
            ShowAleHouseBuildingPropPreview();
        }

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
            // Command-R is Unity's script refresh shortcut. Treat only an
            // unmodified R as the QA rotate command so recompiling cannot
            // silently rotate the fixture or call into a torn-down session.
            if (!Input.GetKeyDown(KeyCode.R) ||
                Input.GetKey(KeyCode.LeftCommand) ||
                Input.GetKey(KeyCode.RightCommand) ||
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl)) return;
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

    [MenuItem("City Forge/QA/Rotate Live QA Building Counter-clockwise")]
    private static void RotateLiveQaBuildingCounterClockwise()
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
            if (world.name == "Building Live Placement QA")
                world.RotateSelected(-1);
        EditorApplication.delayCall += DumpLivePlacement;
    }

    [MenuItem("City Forge/QA/Frame Live QA Building Props")]
    private static void FrameLiveQaBuildingProps()
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
            if (world.name == "Building Live Placement QA")
                world.SetBuildingPropQaCameraZoom(6f);
    }

    [MenuItem("City Forge/QA/Rotate Selected Building Prop 45 Degrees")]
    private static void RotateSelectedBuildingProp45Degrees()
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
            if (world.name == "Building Live Placement QA")
                world.RotateSelectedBuildingProp45Degrees();
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
