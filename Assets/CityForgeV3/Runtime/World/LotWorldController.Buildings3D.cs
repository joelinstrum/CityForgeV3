using System.Collections.Generic;
using CityForgeV3.Buildings3D;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        // The imported brownstones face the established CityForge world view
        // at this authored yaw; camera corrections must not modify this value.
        private const float BrownstoneDefaultFacingDegrees = 90f;
        private const float DefaultExperimentalSkyExposure = 0.95f;
        private const float DefaultExperimentalBuildingContrast = 1.42f;
        private const float DefaultExperimentalBuildingSaturation = 1.34f;
        public const string BrownstoneBuilding22kId = "brownstone-building-22k";
        public const string BrownstoneBuilding22kResource =
            "CityForgeV3/Buildings3D/BrownstoneBuilding22k/brownstone-building-22k";
        public const string BrownstoneProductionResource =
            "CityForgeV3/Buildings3D/BrownstoneProduction/Prefabs/BrownstoneProduction";
        public const string LowPolyBrownstoneV01Id = "low-poly-brownstone-v01";
        public const string LowPolyBrownstoneV01Resource =
            "CityForgeV3/Buildings3D/LowPolyBrownstoneV01/LowPolyBrownstone";
        public const string ArtMuseumProductionId = "art-museum-production-v01";
        public const string ArtMuseumProductionResource =
            "CityForgeV3/Buildings3D/ArtMuseumProduction/Prefabs/ArtMuseumProduction";
        public const string IvyTownhouseWhiteProductionId =
            "ivy-townhouse-white-production-v01";
        public const string IvyTownhouseWhiteProductionResource =
            "CityForgeV3/Buildings3D/IvyTownhouseWhiteProduction/Prefabs/IvyTownhouseWhiteProduction";
        public const string PlymouthStoreProductionId =
            "plymouth-store-v01";
        public const string PlymouthStoreProductionResource =
            "CityForgeV3/Buildings3D/PlymouthStoreProduction/Prefabs/PlymouthStoreComparisonV01";
        public const string GildedAgeMansionProductionId =
            "gilded-age-mansion-v01";
        public const string GildedAgeMansionProductionResource =
            "CityForgeV3/Buildings3D/GildedAgeMansionProduction/Prefabs/GildedAgeMansionV01";
        public const string GildedAgeMansionExperimentalId =
            "gilded-age-mansion-exp-v01";
        public const string GildedAgeMansionExperimentalResource =
            "CityForgeV3/Buildings3D/GildedAgeMansionExperimental/Prefabs/GildedAgeMansionExpV01";
        public const string HitchcockMansionProductionId =
            "hitchcock-mansion-v01";
        public const string HitchcockMansionProductionResource =
            "CityForgeV3/Buildings3D/HitchcockMansionProduction/Prefabs/HitchcockMansionV01";
        public const string KingKongEnclosureBuilding3DId =
            "king-kong-enclosure-building-v01";
        public const string KingKongEnclosureBuilding3DResource =
            "CityForgeV3/Props/Entertainment/KingKongEnclosureV01/KingKongEnclosureV01";
        public const string NyBrownstoneLightEvaluationId =
            "ny-brownstone-light-eval-v01";
        public const string NyBrownstoneBayEvaluationId =
            "ny-brownstone-bay-eval-v01";
        public const string NyFancyTownhouseEvaluationId =
            "ny-fancy-townhouse-eval-v01";
        public const string NyBrownstoneEvaluationId =
            "ny-brownstone-eval-v01";
        public const string BrooklynTownhomeRowEvaluationId =
            "brooklyn-townhome-row-eval-v01";
        public const string NorwalkClockTowerEvaluationId =
            "norwalk-clock-tower-eval-v01";
        public const string MixedUseBrickEvaluationId =
            "mixed-use-brick-eval-v01";
        public const string FortWatchtowerEvaluationId =
            "fort-watchtower-eval-v01";
        public const string FoundersCabinLargeEvaluationId =
            "founders-cabin-large-eval-v01";
        public const string FoundersCabinSmallEvaluationId =
            "founders-cabin-small-eval-v01";
        public const string NewEnglandFarmhouse1780EvaluationId =
            "new-england-farmhouse-1780-eval-v01";
        public const string NewEnglandRedBarnEvaluationId =
            "new-england-red-barn-eval-v01";
        public const string OldChurchEvaluationId =
            "old-church-eval-v01";
        public const float KingKongEnclosureBuildingSizeMeters = 30f;
        // Tripo's enclosure FBX contains outer geometry that inflates its
        // renderer bounds far beyond the structure visible to the player.
        // This calibrated envelope matches the visible palisade footprint.
        public const float KingKongEnclosureVisibleBoundsScale = 0.58f;
        private readonly List<GameObject> _experimentalBuilding3DRoots = new();
        private readonly List<GameObject> _experimentalBuilding3DVisibleRoots = new();
        private readonly List<FarZoomBuildingBillboard> _farBuildingBillboards = new();
        private readonly List<Material> _experimentalBuilding3DMaterials = new();
        private readonly Dictionary<GameObject, GameObject>
            _experimentalBuilding3DGroundShadows = new();
        private readonly Dictionary<GameObject, List<Vector2>>
            _buildingFootprintContours = new();
        private readonly Dictionary<GameObject, Bounds>
            _buildingFloraBounds = new();
        private float _shadowDistanceBeforeExperimental3D;
        private int _shadowCascadesBeforeExperimental3D;
        private bool _experimental3DShadowDistanceApplied;
        private Material _experimental3DStudioSkybox;
        private Material _skyboxBeforeExperimental3D;
        private AmbientMode _ambientModeBeforeExperimental3D;
        private float _ambientIntensityBeforeExperimental3D;
        private DefaultReflectionMode _reflectionModeBeforeExperimental3D;
        private float _reflectionIntensityBeforeExperimental3D;
        private bool _experimental3DStudioEnvironmentApplied;
        private float _environmentSunIntensityScale = 1f;
        private float _environmentSunElevationOffset;
        private float _environmentSunAzimuthOffset;
        private float _environmentAmbientIntensityScale = 1f;
        private float _environmentSkyExposure = DefaultExperimentalSkyExposure;
        private float _environmentShadowStrength = 0.86f;
        private float _environmentBuildingContrast =
            DefaultExperimentalBuildingContrast;
        private float _environmentBuildingSaturation =
            DefaultExperimentalBuildingSaturation;
        private float _environmentBuildingVibrance;
        private readonly Dictionary<TimeOfDayPreset, EnvironmentLightingState>
            _environmentLightingByTime = new();

        private struct EnvironmentLightingState
        {
            public float SunIntensity;
            public float SunElevation;
            public float SunAzimuth;
            public float Ambient;
            public float Exposure;
            public float ShadowStrength;
            public float Contrast;
            public float Saturation;
            public float Vibrance;
        }
        private int _selectedBuilding3DIndex = -1;
        private bool _building3DDragActive;
        private bool _building3DPlacementPreviewActive;
        private Vector2 _building3DDragOffset;
        public const float Building3DPlacementPreviewOpacity = 0.75f;
        public bool Building3DPlacementPreviewActive =>
            _building3DPlacementPreviewActive;
        private GameObject _building3DSelectionOutline;
        private Material _building3DSelectionOutlineMaterial;
        private Material _building3DMeshSelectionMaterial;

        public int SelectedBuilding3DIndex => _selectedBuilding3DIndex;
        public bool SelectedBuildingCanRepaint
        {
            get
            {
                var buildings = _session?.Data?.Buildings3D;
                return buildings != null && _selectedBuilding3DIndex >= 0 &&
                    _selectedBuilding3DIndex < buildings.Count &&
                    BuildingContentCatalog.Find(
                        buildings[_selectedBuilding3DIndex].AssetId)?.repaintable == true;
            }
        }
        public string SelectedBuildingPaintHex => SelectedBuildingCanRepaint
            ? _session.Data.Buildings3D[_selectedBuilding3DIndex].PaintHex ?? ""
            : "";

        public bool SetSelectedBuildingPaint(string hex)
        {
            if (!SelectedBuildingCanRepaint) return false;
            hex = (hex ?? "").Trim();
            Color color = Color.white;
            if (hex.Length > 0)
            {
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (hex.Length != 7 || !ColorUtility.TryParseHtmlString(hex,
                        out color)) return false;
                hex = "#" + ColorUtility.ToHtmlStringRGB(color);
            }
            var placed = _session.Data.Buildings3D[_selectedBuilding3DIndex];
            if (string.Equals(placed.PaintHex ?? "", hex,
                    System.StringComparison.OrdinalIgnoreCase)) return true;
            placed.PaintHex = hex;
            ApplyBuildingPaint(_selectedBuilding3DIndex);
            return true;
        }

        private void ApplyBuildingPaint(int index)
        {
            var buildings = _session?.Data?.Buildings3D;
            if (buildings == null || index < 0 || index >= buildings.Count ||
                index >= _experimentalBuilding3DVisibleRoots.Count) return;
            var root = _experimentalBuilding3DVisibleRoots[index];
            if (root == null) return;
            var placed = buildings[index];
            if (BuildingContentCatalog.Find(placed.AssetId)?.repaintable != true)
                return;
            var color = Color.white;
            var enabled = !string.IsNullOrWhiteSpace(placed.PaintHex) &&
                ColorUtility.TryParseHtmlString(placed.PaintHex, out color);
            if (!enabled) color = Color.white;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.sharedMaterial == null ||
                    renderer.sharedMaterial.shader?.name !=
                    "CityForgeV3/Experimental3DBuildingPBR") continue;
                renderer.GetPropertyBlock(block);
                block.SetColor("_PaintColor", color);
                block.SetFloat("_PaintEnabled", enabled ? 1f : 0f);
                renderer.SetPropertyBlock(block);
                block.Clear();
            }
        }
        public string SelectedBuilding3DDisplayName
        {
            get
            {
                var buildings = _session?.Data?.Buildings3D;
                if (buildings == null || _selectedBuilding3DIndex < 0 ||
                    _selectedBuilding3DIndex >= buildings.Count) return string.Empty;
                var id = buildings[_selectedBuilding3DIndex].AssetId;
                return BuildingContentCatalog.Find(id)?.displayName ?? id ?? "Building";
            }
        }

        public BuildingConstructionSequence SelectedBuildingConstruction =>
            _selectedBuilding3DIndex >= 0 &&
            _selectedBuilding3DIndex < _experimentalBuilding3DVisibleRoots.Count
                ? _experimentalBuilding3DVisibleRoots[_selectedBuilding3DIndex]
                    ?.GetComponent<BuildingConstructionSequence>()
                : null;
        public bool SelectedBuildingFrameVisible =>
            _selectedBuilding3DIndex >= 0 &&
            _selectedBuilding3DIndex < _experimentalBuilding3DVisibleRoots.Count &&
            _experimentalBuilding3DVisibleRoots[_selectedBuilding3DIndex] != null &&
            FindConstructionFrame(
                _experimentalBuilding3DVisibleRoots[_selectedBuilding3DIndex]) != null;

        public float EnvironmentSunIntensityScale => _environmentSunIntensityScale;
        public float EnvironmentSunElevationOffset => _environmentSunElevationOffset;
        public float EnvironmentSunAzimuthOffset => _environmentSunAzimuthOffset;
        public float EnvironmentAmbientIntensityScale => _environmentAmbientIntensityScale;
        public float EnvironmentSkyExposure => _environmentSkyExposure;
        public float EnvironmentShadowStrength => _environmentShadowStrength;
        public float EnvironmentBuildingContrast => _environmentBuildingContrast;
        public float EnvironmentBuildingSaturation => _environmentBuildingSaturation;
        public float EnvironmentBuildingVibrance => _environmentBuildingVibrance;

        public int ExperimentalBuilding3DCount =>
            _session?.Data?.Buildings3D?.Count ?? 0;
        public int ExperimentalBuilding3DFloraShadowCasterCount
        {
            get
            {
                var count = 0;
                foreach (var root in _experimentalBuilding3DRoots)
                {
                    if (root == null ||
                        !root.name.Contains("Flora/Prop Shadow Caster"))
                        continue;
                    count += root.GetComponentsInChildren<Renderer>(true).Length;
                }
                return count;
            }
        }

        public void CreateExperimental3DBuildingsLot()
        {
            // Keep enough receiver area around the comparison buildings for
            // the correctly directed morning/afternoon mesh shadows to remain
            // visible. The fixture is presented as an 80 m experimental lot;
            // the former 24 m plane clipped nearly every low-angle shadow.
            NewEmptyLot("3D Buildings", LotType.Mixed, 8, 8);
            _session.Data.Buildings3D = new List<PlacedBuilding3D>();
            // The experimental camera is fixed at 20 degrees. Its screen-right
            // world vector is (-sin(20), 0, cos(20)); place the comparison
            // models along that vector so they read as one horizontal row in
            // the actual Game view rather than a world-X diagonal/vertical row.
            AddExperimentalBuilding3D(BrownstoneBuilding22kId,
                4.8f, -13.2f, 0);
            AddExperimentalBuilding3D(BrownstoneBuilding22kId,
                0f, 0f, 0);
            AddExperimentalBuilding3D(LowPolyBrownstoneV01Id,
                -4.8f, 13.2f, 0);
            SetBaseTexture("grass-lush");
            SaveLot();
        }

        public void CreateArtMuseumLODTestLot()
        {
            // Neighborhood mode keeps the real road-art presentation active,
            // allowing this fixture to exercise road shadow composition as
            // well as the museum's experimental 3D ground receiver.
            NewEmptyLot("Art Museum LOD", LotType.Neighborhood, 8, 8);
            _session.Data.Buildings3D = new List<PlacedBuilding3D>();
            AddExperimentalBuilding3D(ArtMuseumProductionId, 0f, 0f, 0);
            SetBaseTexture("grass-lush");
            SaveLot();
        }

        public void CreateIvyTownhouseWhiteLODTestLot()
        {
            NewEmptyLot("Ivy Townhouse White LOD", LotType.Residential, 6, 6);
            _session.Data.Buildings3D = new List<PlacedBuilding3D>();
            AddExperimentalBuilding3D(IvyTownhouseWhiteProductionId, 0f, 0f, 0);
            SetBaseTexture("grass-lush");
            SaveLot();
        }

        public void CreatePlymouthStoreLODTestLot()
        {
            NewEmptyLot("Plymouth Store LOD", LotType.Commercial, 6, 6);
            _session.Data.Buildings3D = new List<PlacedBuilding3D>();
            AddExperimentalBuilding3D(PlymouthStoreProductionId, 0f, 0f, 0);
            SetBaseTexture("grass-lush");
            SaveLot();
        }

        public void CreatePlymouthStoreComparisonTestLot()
        {
            CreatePlymouthStoreLODTestLot();
        }

        public void CreateGildedAgeMansionLODTestLot()
        {
            NewEmptyLot("Gilded Age Mansion LOD", LotType.Residential, 6, 6);
            _session.Data.Buildings3D = new List<PlacedBuilding3D>();
            AddExperimentalBuilding3D(GildedAgeMansionExperimentalId, 0f, 0f, 0);
            SetBaseTexture("grass-lush");
            SaveLot();
        }

        public void PrepareArtMuseumSurfaceQa()
        {
            // Exercise the real experimental-ground path, not a generic empty
            // lot: a road circuit surrounds the museum while brick overlays
            // remain visible on both sides of the authored grass receiver.
            SeedRoadVerticalSlice();
            RebuildRoadArtwork();
            RebuildRoadVehicleNetwork();
            ApplyLotPlanningState();
            SetOverlayEditorContext(true);
            foreach (var cell in new[]
                     {
                         new Vector2Int(0, 0), new Vector2Int(7, 0),
                         new Vector2Int(0, 7), new Vector2Int(7, 7)
                     })
            {
                BeginOverlayPaintAtCell("brick-walkway", cell.x, cell.y);
                EndOverlayPaint();
            }
            Debug.Log($"Art Museum surface QA prepared: " +
                      $"{PlacedRoadCount} road pieces, " +
                      $"{OverlayTextureCount} overlay tiles, " +
                      $"zoom lot {LotWidthCells}x{LotDepthCells}.");
            NotifyStateChanged();
        }

        public bool AddExperimentalBuilding3D(string assetId,
            float? worldX = null, float? worldZ = null,
            int? rotationQuarterTurns = null)
        {
            // Legacy QA-only models remain accepted until their fixtures move
            // into the catalog. Player-facing content must resolve through a
            // manifest entry rather than an asset-id allow-list.
            if (BuildingContentCatalog.Find(assetId) == null &&
                assetId != BrownstoneBuilding22kId &&
                assetId != LowPolyBrownstoneV01Id &&
                assetId != GildedAgeMansionProductionId) return false;
            var preservedCamera = CaptureCameraFraming();
            _session.Data.Buildings3D ??= new List<PlacedBuilding3D>();
            var index = _session.Data.Buildings3D.Count;
            var offset = index * 2.5f;
            _session.Data.Buildings3D.Add(new PlacedBuilding3D
            {
                AssetId = assetId,
                X = worldX ?? Mathf.Clamp(-10f + offset, -15f, 15f),
                Z = worldZ ?? Mathf.Clamp(-5f + offset, -15f, 15f),
                RotationQuarterTurns = rotationQuarterTurns ?? index % 4,
                RotationEighthTurns = (rotationQuarterTurns ?? index % 4) * 2
            });
            RebuildExperimentalBuilding3DPresentations();
            // A library-card click is an add-and-select action. Selection must
            // be established after rebuilding because the visible runtime root
            // does not exist until that pass has completed.
            ActiveObjectSelection = LotObjectSelectionKind.None;
            SelectedFloraIndex = -1;
            SelectedPropIndex = -1;
            _selectedBuilding3DIndex = index;
            _building3DDragActive = false;
            RefreshBuilding3DSelectionOutline();
            ApplyTimeOfDay();
            // Adding an object must not also orbit the editor camera. The
            // previous refit forced the native-3D 45-degree azimuth here,
            // making the entire lot appear to rotate as soon as the first
            // building was placed.
            RestoreCameraFraming(preservedCamera);
            NotifyStateChanged();
            return true;
        }

        public bool BeginExperimentalBuilding3DPlacement(string assetId)
        {
            if (!AddExperimentalBuilding3D(assetId)) return false;
            _building3DPlacementPreviewActive = true;
            _building3DDragActive = true;
            _building3DDragOffset = Vector2.zero;
            ApplySelectedBuilding3DPlacementOpacity();
            return true;
        }

        private void ApplySelectedBuilding3DPlacementOpacity()
        {
            if (!_building3DPlacementPreviewActive ||
                _selectedBuilding3DIndex < 0 ||
                _selectedBuilding3DIndex >= _experimentalBuilding3DVisibleRoots.Count)
                return;
            var root = _experimentalBuilding3DVisibleRoots[_selectedBuilding3DIndex];
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (IsPackageShadowRenderer(renderer)) continue;
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_BaseColor"))
                    {
                        var color = material.GetColor("_BaseColor");
                        color.a = Building3DPlacementPreviewOpacity;
                        material.SetColor("_BaseColor", color);
                    }
                    if (material.HasProperty("_Color"))
                    {
                        var color = material.GetColor("_Color");
                        color.a = Building3DPlacementPreviewOpacity;
                        material.SetColor("_Color", color);
                    }
                    if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                    if (material.HasProperty("_SrcBlend"))
                        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    if (material.HasProperty("_DstBlend"))
                        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }
            }
        }

        private void ApplyExperimentalBuilding3DShadowDistance()
        {
            ApplyExperimentalBuilding3DGroundReceiver();

            if (ExperimentalBuilding3DCount > 0)
            {
                if (!_experimental3DShadowDistanceApplied)
                {
                    _shadowDistanceBeforeExperimental3D =
                        QualitySettings.shadowDistance;
                    _shadowCascadesBeforeExperimental3D =
                        QualitySettings.shadowCascades;
                    _experimental3DShadowDistanceApplied = true;
                }
                // The 80 m orthographic pilot is viewed from outside the
                // lower quality tiers' 15-70 m shadow range. Keep the actual
                // mesh casters in Unity's directional shadow map. The 3D
                // pilot's morning elevation is calibrated separately below.
                QualitySettings.shadowDistance = Mathf.Max(
                    _shadowDistanceBeforeExperimental3D, 150f);
                QualitySettings.shadowCascades = 4;
            }
            else if (_experimental3DShadowDistanceApplied)
            {
                QualitySettings.shadowDistance =
                    _shadowDistanceBeforeExperimental3D;
                QualitySettings.shadowCascades =
                    _shadowCascadesBeforeExperimental3D;
                _experimental3DShadowDistanceApplied = false;
            }
        }

        private Quaternion ExperimentalBuilding3DSunRotation()
        {
            if (ExperimentalBuilding3DCount <= 0)
                return TimeOfDayLighting.SunRotation(TimeOfDay);
            var spec = TimeOfDayLighting.For(TimeOfDay);
            // The cinematic 8° evening sun makes a tall 3D building cast a
            // several-lot-long shadow. Keep the evening palette and compass,
            // but use a game-readable elevation for native 3D shadows.
            // Morning is likewise calibrated separately from the 2D artwork.
            // Preserve the universal compass contract: the azimuth describes
            // where the sun is, while a Unity directional light points along
            // the rays. Morning sun is east, therefore its rays and shadows
            // travel west. Afternoon uses the exact opposite bearing.
            var canonicalRotation = TimeOfDayLighting.SunRotation(TimeOfDay);
            return Quaternion.Euler(
                (TimeOfDay switch
                {
                    TimeOfDayPreset.Morning => 48f,
                    TimeOfDayPreset.Evening => 28f,
                    _ => spec.SunElevation
                }) +
                _environmentSunElevationOffset,
                canonicalRotation.eulerAngles.y +
                _environmentSunAzimuthOffset,
                0f);
        }

        private Vector3 ProjectedObjectShadowRay()
        {
            var ray = ExperimentalBuilding3DSunRotation() * Vector3.forward;
            if (_buildingPackage != null)
                ray = Quaternion.Euler(0f,
                    _buildingPackage.ShadowDirectionOffsetDegrees, 0f) * ray;
            return ray;
        }

        private void ApplyExperimentalBuilding3DStudioEnvironment()
        {
            if (ExperimentalBuilding3DCount <= 0)
            {
                RestoreExperimentalBuilding3DStudioEnvironment();
                return;
            }

            if (!_experimental3DStudioEnvironmentApplied)
            {
                _skyboxBeforeExperimental3D = RenderSettings.skybox;
                _ambientModeBeforeExperimental3D = RenderSettings.ambientMode;
                _ambientIntensityBeforeExperimental3D =
                    RenderSettings.ambientIntensity;
                _reflectionModeBeforeExperimental3D =
                    RenderSettings.defaultReflectionMode;
                _reflectionIntensityBeforeExperimental3D =
                    RenderSettings.reflectionIntensity;
                _experimental3DStudioEnvironmentApplied = true;
            }

            if (_experimental3DStudioSkybox == null)
            {
                var panorama = Resources.Load<Texture2D>(
                    "CityForgeV3/Environment/TripoStudioV01/tripo-studio-lighting");
                var shader = Shader.Find("Skybox/Panoramic");
                if (panorama == null || shader == null) return;
                _experimental3DStudioSkybox = new Material(shader)
                {
                    name = "CF Tripo Studio IBL — Strength 0.92"
                };
                _experimental3DStudioSkybox.SetTexture("_MainTex", panorama);
                _experimental3DStudioSkybox.SetFloat("_Exposure",
                    _environmentSkyExposure);
                _experimental3DStudioSkybox.SetFloat("_Rotation", 0f);
            }

            RenderSettings.skybox = _experimental3DStudioSkybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            _experimental3DStudioSkybox.SetFloat("_Exposure",
                _environmentSkyExposure);
            RenderSettings.ambientIntensity =
                0.92f * _environmentAmbientIntensityScale;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 1f;
            DynamicGI.UpdateEnvironment();
        }

        private void ApplyExperimentalBuilding3DColorGrade()
        {
            foreach (var root in _experimentalBuilding3DRoots)
            {
                if (root == null) continue;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null ||
                        material.shader.name !=
                        "CityForgeV3/Experimental3DBuildingPBR") continue;
                    if (!material.name.StartsWith("ArtMuseum-LOD"))
                    {
                        material.SetFloat("_Contrast", _environmentBuildingContrast);
                        material.SetFloat("_Saturation", _environmentBuildingSaturation);
                        if (material.HasProperty("_Vibrance"))
                            material.SetFloat("_Vibrance",
                                _environmentBuildingVibrance);
                    }
                }
            }
        }

        public void SetEnvironmentLightingControl(string control, float value)
        {
            switch (control)
            {
                case "sun-intensity":
                    _environmentSunIntensityScale = Mathf.Clamp(value, 0f, 3f);
                    break;
                case "sun-elevation":
                    _environmentSunElevationOffset = Mathf.Clamp(value, -35f, 35f);
                    break;
                case "sun-azimuth":
                    _environmentSunAzimuthOffset = Mathf.Clamp(value, -45f, 45f);
                    break;
                case "ambient":
                    _environmentAmbientIntensityScale = Mathf.Clamp(value, 0f, 1.5f);
                    break;
                case "exposure":
                    _environmentSkyExposure = Mathf.Clamp(value, 0.25f, 1.5f);
                    break;
                case "shadow-strength":
                    _environmentShadowStrength = Mathf.Clamp01(value);
                    break;
                case "contrast":
                    _environmentBuildingContrast = Mathf.Clamp(value, 0.8f, 2f);
                    break;
                case "saturation":
                    _environmentBuildingSaturation = Mathf.Clamp(value, 0f, 4f);
                    break;
                case "vibrance":
                    _environmentBuildingVibrance = Mathf.Clamp01(value);
                    break;
                default:
                    return;
            }
            ApplyTimeOfDay();
            ApplyExperimentalBuilding3DColorGrade();
            SaveEnvironmentLightingState(TimeOfDay);
        }

        private void SaveEnvironmentLightingState(TimeOfDayPreset preset)
        {
            _environmentLightingByTime[preset] = new EnvironmentLightingState
            {
                SunIntensity = _environmentSunIntensityScale,
                SunElevation = _environmentSunElevationOffset,
                SunAzimuth = _environmentSunAzimuthOffset,
                Ambient = _environmentAmbientIntensityScale,
                Exposure = _environmentSkyExposure,
                ShadowStrength = _environmentShadowStrength,
                Contrast = _environmentBuildingContrast,
                Saturation = _environmentBuildingSaturation,
                Vibrance = _environmentBuildingVibrance
            };
        }

        private void LoadEnvironmentLightingState(TimeOfDayPreset preset)
        {
            if (!_environmentLightingByTime.TryGetValue(preset, out var state))
            {
                ResetEnvironmentLightingControls(false);
                // Afternoon is intentionally the high-contrast architectural
                // preset. Its southwest key light needs enough energy to make
                // the sun-facing facade read brightly against the shaded one.
                if (preset == TimeOfDayPreset.Afternoon)
                {
                    _environmentSunIntensityScale = 1.35f;
                    _environmentBuildingVibrance = 0.08f;
                    _environmentBuildingSaturation = 1.38f;
                }
                SaveEnvironmentLightingState(preset);
                return;
            }
            _environmentSunIntensityScale = state.SunIntensity;
            _environmentSunElevationOffset = state.SunElevation;
            _environmentSunAzimuthOffset = state.SunAzimuth;
            _environmentAmbientIntensityScale = state.Ambient;
            _environmentSkyExposure = state.Exposure;
            _environmentShadowStrength = state.ShadowStrength;
            _environmentBuildingContrast = state.Contrast;
            _environmentBuildingSaturation = state.Saturation;
            _environmentBuildingVibrance = state.Vibrance;
        }

        public void ResetEnvironmentLightingControls()
        {
            ResetEnvironmentLightingControls(true);
        }

        private void ResetEnvironmentLightingControls(bool apply)
        {
            _environmentSunIntensityScale = 1f;
            _environmentSunElevationOffset = 0f;
            _environmentSunAzimuthOffset = 0f;
            _environmentAmbientIntensityScale = 1f;
            _environmentSkyExposure = DefaultExperimentalSkyExposure;
            _environmentShadowStrength = 0.86f;
            _environmentBuildingContrast = DefaultExperimentalBuildingContrast;
            _environmentBuildingSaturation = DefaultExperimentalBuildingSaturation;
            _environmentBuildingVibrance = 0f;
            if (TimeOfDay == TimeOfDayPreset.Afternoon)
            {
                _environmentSunIntensityScale = 1.35f;
                _environmentBuildingVibrance = 0.08f;
                _environmentBuildingSaturation = 1.38f;
            }
            SaveEnvironmentLightingState(TimeOfDay);
            if (apply)
            {
                ApplyTimeOfDay();
                ApplyExperimentalBuilding3DColorGrade();
            }
        }

        private void RestoreExperimentalBuilding3DStudioEnvironment()
        {
            if (!_experimental3DStudioEnvironmentApplied) return;
            RenderSettings.skybox = _skyboxBeforeExperimental3D;
            RenderSettings.ambientMode = _ambientModeBeforeExperimental3D;
            RenderSettings.ambientIntensity = _ambientIntensityBeforeExperimental3D;
            RenderSettings.defaultReflectionMode =
                _reflectionModeBeforeExperimental3D;
            RenderSettings.reflectionIntensity =
                _reflectionIntensityBeforeExperimental3D;
            _experimental3DStudioEnvironmentApplied = false;
            DynamicGI.UpdateEnvironment();
        }

        private void ApplyExperimentalBuilding3DGroundReceiver()
        {
            if (_groundRenderer == null ||
                _groundRenderer.sharedMaterial == null) return;
            var material = _groundRenderer.sharedMaterial;
            var shaderName = ExperimentalBuilding3DCount > 0
                ? "CityForgeV3/Experimental3DGroundReceiver"
                : "CityForgeV3/ShadowReceivingLotSurface";
            var shader = Shader.Find(shaderName);
            if (shader == null) return;

            var texture = material.mainTexture;
            var textureScale = material.mainTextureScale;
            var textureOffset = material.mainTextureOffset;
            if (material.shader != shader)
            {
                material.shader = shader;
                material.mainTexture = texture;
                material.mainTextureScale = textureScale;
                material.mainTextureOffset = textureOffset;
            }
            if (_terrainShadowCasterRenderer != null)
                _terrainShadowCasterRenderer.sharedMaterial = material;

            // The display-match tint calibrates authored ground textures only.
            // A blank lot already has its time/season color in _Color; applying
            // this multiplier would darken it when the first 3D building arrives.
            if (material.HasProperty("_DisplayMatch"))
                material.SetColor("_DisplayMatch",
                    string.IsNullOrWhiteSpace(BaseTextureId)
                        ? Color.white : new Color(0.75f, 0.80f, 0.75f, 1f));

            // A shader transition can occur after the base-texture pass while a
            // saved lot is rebuilding. Do not rely on the previous shader's
            // property state: resolve and bind the authored seasonal texture to
            // the receiver explicitly every time lighting is applied.
            var option = ResolveBaseTexture(BaseTextureId);
            if (option == null) return;
            var authoredTexture = Resources.Load<Texture2D>(
                option.ResolveResourcePath(Season));
            material.SetTexture("_MainTex", authoredTexture);
            var repeatMeters = option.BaseRepeatMeters;
            material.SetTextureScale("_MainTex", new Vector2(
                Mathf.Max(1f, LotWidthMeters / repeatMeters),
                Mathf.Max(1f, LotDepthMeters / repeatMeters)));
            material.SetTextureOffset("_MainTex", Vector2.zero);
            if (material.HasProperty("_UseWorldSpaceUV"))
                material.SetFloat("_UseWorldSpaceUV",
                    option.UseWorldSpaceUv ? 1f : 0f);
            if (material.HasProperty("_TextureWorldSize"))
                material.SetFloat("_TextureWorldSize", repeatMeters);
            // The selected base texture must draw first. Projected 3D-building
            // shadows use transparent queue 3001, so they blend visibly over
            // the opaque grass without changing its color calculation.
            material.renderQueue = 2000;
        }

        private void OnDestroy()
        {
            DisposeLotBehaviors();
            RestoreExperimentalBuilding3DStudioEnvironment();
            if (_floraProjectedShadowMaterial != null)
            {
                if (Application.isPlaying) Destroy(_floraProjectedShadowMaterial);
                else DestroyImmediate(_floraProjectedShadowMaterial);
            }
            if (_experimental3DStudioSkybox != null)
            {
                if (Application.isPlaying) Destroy(_experimental3DStudioSkybox);
                else DestroyImmediate(_experimental3DStudioSkybox);
            }
            if (_experimental3DShadowDistanceApplied)
            {
                QualitySettings.shadowDistance =
                    _shadowDistanceBeforeExperimental3D;
                QualitySettings.shadowCascades =
                    _shadowCascadesBeforeExperimental3D;
            }
        }

        private void RebuildExperimentalBuilding3DPresentations()
        {
            // Editor domain reloads reset the runtime tracking collections but
            // preserve scene objects. Sweep only our own top-level pilot roots
            // so a stale morning/afternoon shadow can never survive into a
            // later preset and appear as a duplicate.
            var staleRoots = new List<GameObject>();
            foreach (Transform child in transform)
                if (child != null && child.name.StartsWith("3D Building"))
                    staleRoots.Add(child.gameObject);
            foreach (var staleRoot in staleRoots)
                DestroyForCurrentMode(staleRoot);
            foreach (var root in _experimentalBuilding3DRoots)
                if (root != null && !staleRoots.Contains(root))
                    DestroyForCurrentMode(root);
            _experimentalBuilding3DRoots.Clear();
            _experimentalBuilding3DVisibleRoots.Clear();
            _farBuildingBillboards.Clear();
            _building3DSelectionOutline = null;
            _experimentalBuilding3DGroundShadows.Clear();
            _buildingFootprintContours.Clear();
            _buildingFloraBounds.Clear();
            foreach (var material in _experimentalBuilding3DMaterials)
                if (material != null)
                {
                    if (Application.isPlaying) Destroy(material);
                    else DestroyImmediate(material);
                }
            _experimentalBuilding3DMaterials.Clear();
            if (_session?.Data?.Buildings3D == null) return;

            foreach (var placed in _session.Data.Buildings3D)
            {
                if (placed == null)
                    continue;
                if (string.IsNullOrWhiteSpace(placed.InstanceId))
                    placed.InstanceId = System.Guid.NewGuid().ToString("N");
                var content = BuildingContentCatalog.Find(placed.AssetId);
                var source = content != null
                    ? BuildingContentCatalog.LoadModel(content)
                    : Resources.Load<GameObject>(
                    placed.AssetId == LowPolyBrownstoneV01Id
                        ? LowPolyBrownstoneV01Resource
                        : placed.AssetId == GildedAgeMansionProductionId
                            ? GildedAgeMansionProductionResource
                        : BrownstoneProductionResource);
                if (source == null)
                {
                    Debug.LogError($"Missing 3D building '{placed.AssetId}' from " +
                                   $"'{content?.sourceId ?? "legacy QA"}'.");
                    continue;
                }
                var root = Instantiate(source, transform);
                root.name = $"3D Building — {content?.displayName ?? placed.AssetId}";
                root.transform.localPosition = new Vector3(placed.X, 0f, placed.Z);
                var turns = placed.RotationEighthTurns >= 0
                    ? placed.RotationEighthTurns * 45f
                    : placed.RotationQuarterTurns * 90f;
                root.transform.localRotation = content == null
                    ? ExperimentalBuildingRotation(placed)
                    : Quaternion.Euler(content.pitchDegrees,
                        content.baseYawDegrees + turns, 0f);
                ApplyBuildingContentContract(root.transform, content);
                var material = placed.AssetId == BrownstoneBuilding22kId
                    ? CreateBrownstoneBuilding22kMaterial()
                    : null;
                var packageInstance = root.GetComponent<Building3DPackageInstance>();
                var hasPackageShadowLod = packageInstance != null &&
                    HasPackageShadowRenderers(root);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (material != null)
                    {
                        var materials = renderer.sharedMaterials;
                        if (materials.Length == 0)
                            materials = new[] { material };
                        else
                            for (var index = 0; index < materials.Length; index++)
                                if (materials[index] == null ||
                                    materials[index].name.Contains("tripo_mat"))
                                    materials[index] = material;
                        renderer.sharedMaterials = materials;
                    }
                    renderer.shadowCastingMode = _districtHosted
                        ? ShadowCastingMode.Off
                        : hasPackageShadowLod
                            ? (IsPackageShadowRenderer(renderer)
                                ? ShadowCastingMode.ShadowsOnly
                                : ShadowCastingMode.Off)
                            : ShadowCastingMode.On;
                    // A separately authored lower-detail caster occupies the
                    // same volume. Letting the beauty mesh receive that
                    // mismatched topology self-shadows the entire museum to
                    // black. It still receives directional/ambient lighting;
                    // the package caster continues to shadow ground and props.
                    renderer.receiveShadows = !hasPackageShadowLod ||
                        IsPackageShadowRenderer(renderer);
                    renderer.allowOcclusionWhenDynamic = true;
                }
                root.GetComponentInChildren<BuildingDoorController>(true)?.SetOpen(placed.DoorOpen, true);
                GroundExperimentalBuilding(root);
                // Package-level unit conversion changes the representation
                // bounds after the prefab's LODGroup was authored. Recompute
                // here so Unity does not cull a meter-sized building using
                // the original centimeter-sized LOD bounds.
                packageInstance?.LodGroup?.RecalculateBounds();
                _experimentalBuilding3DRoots.Add(root);
                _experimentalBuilding3DVisibleRoots.Add(root);
                BuildExperimentalBuilding3DReceiverShadowCaster(root);
                BuildExperimentalBuilding3DProjectedGroundShadow(root);
                if (!string.IsNullOrWhiteSpace(content?.farBillboardResourceRoot))
                {
                    var far = root.AddComponent<FarZoomBuildingBillboard>();
                    if (far.Configure(content.farBillboardResourceRoot,
                            content.farBillboardPixelsPerMeter,
                            turns + content.farBillboardYawOffset, _camera))
                    {
                        _farBuildingBillboards.Add(far);
                        far.SetFar(_districtHosted && ZoomLevel >= LotZoomLevel.Far);
                    }
                    else
                    {
                        Debug.LogWarning($"Missing far views for '{content.id}'; " +
                                         "keeping its 3D model visible.");
                        if (Application.isPlaying) Destroy(far);
                        else DestroyImmediate(far);
                    }
                }
                ApplyBuildingPaint(_experimentalBuilding3DVisibleRoots.Count - 1);
            }
            RebuildEffectPresentations();
            // Lot reconstruction may run after SetTimeOfDay (the QA helpers
            // schedule it through ApplySessionState). Apply the current preset
            // again now that package instances and their night controllers
            // actually exist.
            ApplyTimeOfDay();
            // Normal saved-lot loading applies the current lighting before
            // ApplySessionState reconstructs these runtime shadow objects.
            // Populate their meshes immediately after reconstruction so the
            // loaded lot does not wait for a later time-of-day interaction.
            UpdateExperimentalBuilding3DProjectedGroundShadows();
            RefreshBuilding3DSelectionOutline();
        }

        private void BuildExperimentalBuilding3DProjectedGroundShadow(
            GameObject visibleRoot)
        {
            // Project a hidden copy of the real render meshes. This preserves
            // stairs, cornices, chimneys, and every other silhouette feature;
            // the former convex hull could only ever produce a rectangle.
            var shadow = Instantiate(visibleRoot, transform);
            shadow.name = $"3D Building Ground Shadow — {visibleRoot.name}";
            var shader = Shader.Find(
                "CityForgeV3/ProjectedBuildingMeshShadow");
            if (shader == null)
                throw new MissingReferenceException(
                    "The mesh-projected ground-shadow shader is required.");
            var material = new Material(shader)
            {
                color = new Color(0f, 0f, 0f, 0.20f),
                renderQueue = 3001
            };
            material.name = $"CF Ground Shadow — {visibleRoot.name}";
            foreach (var collider in shadow.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            var packageInstance = visibleRoot.GetComponent<Building3DPackageInstance>();
            var hasAuthoredShadowLod = packageInstance != null &&
                HasPackageShadowRenderers(visibleRoot);
            // A package clone contains every beauty LOD. For packages without
            // an authored ShadowLOD, projecting all of those renderers creates
            // stacked silhouettes and lets the shadow clone select a different
            // LOD from the visible building. Use one normalized, stable mesh.
            var fallbackShadowRoot = packageInstance != null &&
                !hasAuthoredShadowLod
                    ? shadow.transform.Find("Representations/LOD3") ??
                      shadow.transform.Find("Representations/LOD0")
                    : null;
            DisableClonedPackageLodControl(shadow, visibleRoot);
            foreach (var renderer in shadow.GetComponentsInChildren<Renderer>(true))
            {
                var useRenderer = hasAuthoredShadowLod
                    ? IsPackageShadowRenderer(renderer)
                    : fallbackShadowRoot == null ||
                      renderer.transform.IsChildOf(fallbackShadowRoot);
                if (!useRenderer)
                {
                    renderer.enabled = false;
                    continue;
                }
                var count = Mathf.Max(1, renderer.sharedMaterials.Length);
                var materials = new Material[count];
                for (var index = 0; index < count; index++)
                    materials[index] = material;
                renderer.sharedMaterials = materials;
                renderer.enabled = true;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.allowOcclusionWhenDynamic = false;
            }
            _experimentalBuilding3DMaterials.Add(material);
            _experimentalBuilding3DGroundShadows[visibleRoot] = shadow;
            _experimentalBuilding3DRoots.Add(shadow);
        }

        private void UpdateExperimentalBuilding3DProjectedGroundShadows()
        {
            if (ExperimentalBuilding3DCount <= 0 || _sun == null) return;
            // The Lot Editor's exact-color receiver uses this projected copy.
            // Hosted district buildings use the shared sun's native mesh
            // shadows instead, avoiding two competing building projections.
            var ray = ProjectedObjectShadowRay();
            // The exact-color grass receiver is intentionally unlit, so the
            // mesh projection supplies the directional silhouettes. Noon now
            // uses a high southern sun rather than a nearly vertical one, so
            // it needs the same projection path to stay visible on exact-color
            // grass. Night and rain still suppress the manual projection.
            var visible = !IsRaining &&
                TimeOfDay != TimeOfDayPreset.Night &&
                ray.y < -0.01f;
            var lengthScale = BuildingShadowLengthScale(TimeOfDay) *
                (_districtHosted && TimeOfDay == TimeOfDayPreset.Afternoon
                    ? 0.54f : 1f);
            var opacity = TimeOfDay switch
            {
                TimeOfDayPreset.Morning => 0.26f,
                TimeOfDayPreset.Noon => 0.50f,
                TimeOfDayPreset.Afternoon => _districtHosted ? 0.48f : 0.26f,
                _ => 0.22f
            };
            opacity *= Mathf.Clamp01(_environmentShadowStrength);

            foreach (var pair in _experimentalBuilding3DGroundShadows)
            {
                var source = pair.Key;
                var shadow = pair.Value;
                if (source == null || shadow == null) continue;
                shadow.SetActive(visible);
                if (!visible) continue;

                var bounds = default(Bounds);
                var hasBounds = false;
                foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (!hasBounds) continue;
                var referenceHeight = Mathf.Max(0.01f, bounds.max.y - 0.018f);
                var displacement = new Vector3(ray.x, 0f, ray.z) *
                    (referenceHeight / -ray.y) * lengthScale;
                if (_districtHosted)
                {
                    UpdateDistrictBuildingShadowHull(shadow, bounds,
                        displacement, opacity);
                    continue;
                }
                foreach (var renderer in shadow.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.enabled) continue;
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || material.shader == null ||
                            material.shader.name !=
                            "CityForgeV3/ProjectedBuildingMeshShadow")
                            continue;
                        material.SetColor("_Color",
                            new Color(0f, 0f, 0f, opacity));
                        material.SetVector("_ShadowDisplacement", displacement);
                        material.SetFloat("_GroundY", 0.018f);
                        material.SetFloat("_ReferenceHeight", referenceHeight);
                        var clipHalfWidth = _districtHosted
                            ? 100000f : LotWidthMeters * 0.5f + 2f;
                        var clipHalfDepth = _districtHosted
                            ? 100000f : LotDepthMeters * 0.5f + 2f;
                        material.SetVector("_LotHalfExtents", new Vector4(
                            clipHalfWidth, clipHalfDepth, 0f, 0f));
                    }
                }
            }
        }

        private void UpdateDistrictBuildingShadowHull(GameObject shadow,
            Bounds bounds, Vector3 displacement, float opacity)
        {
            const string hullName = "District Building Shadow Hull";
            var hullTransform = shadow.transform.Find(hullName);
            if (hullTransform == null)
            {
                var hullObject = new GameObject(hullName);
                hullObject.transform.SetParent(shadow.transform, false);
                hullObject.AddComponent<MeshFilter>();
                var renderer = hullObject.AddComponent<MeshRenderer>();
                var material = new Material(Shader.Find("Sprites/Default"))
                {
                    name = "CF District Building Shadow",
                    renderQueue = 3001
                };
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _experimentalBuilding3DMaterials.Add(material);
                hullTransform = hullObject.transform;
            }

            foreach (var renderer in shadow.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = renderer.transform == hullTransform;

            var worldPoints = new List<Vector2>(8);
            foreach (var x in new[] { bounds.min.x, bounds.max.x })
            foreach (var z in new[] { bounds.min.z, bounds.max.z })
            {
                worldPoints.Add(new Vector2(x, z));
                worldPoints.Add(new Vector2(
                    x + displacement.x, z + displacement.z));
            }
            var hull = ConvexHull(worldPoints);
            var vertices = new List<Vector3>(hull.Count);
            foreach (var point in hull)
                vertices.Add(hullTransform.InverseTransformPoint(
                    new Vector3(point.x, 0.065f, point.y)));
            var triangles = new List<int>();
            for (var index = 1; index < hull.Count - 1; index++)
            {
                triangles.Add(0);
                triangles.Add(index);
                triangles.Add(index + 1);
            }
            var mesh = hullTransform.GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh { name = hullName };
                hullTransform.GetComponent<MeshFilter>().sharedMesh = mesh;
            }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            hullTransform.GetComponent<MeshRenderer>().sharedMaterial.color =
                new Color(0f, 0f, 0f, opacity);
        }

        private static Quaternion ExperimentalBuildingRotation(
            PlacedBuilding3D placed) => Quaternion.Euler(
            -90f,
            BrownstoneDefaultFacingDegrees +
            (placed.RotationEighthTurns >= 0
                ? placed.RotationEighthTurns * 45f
                : placed.RotationQuarterTurns * 90f),
            0f);

        private void ApplyBuildingContentContract(Transform root,
            BuildingContentEntry content)
        {
            if (root == null || content == null) return;
            if (content.normalizeMeters > 0f)
            {
                if (string.Equals(content.normalizeAxis, "height",
                        System.StringComparison.OrdinalIgnoreCase))
                    NormalizeStaticPropToHeight(root, content.normalizeMeters);
                else if (string.Equals(content.normalizeAxis, "length",
                             System.StringComparison.OrdinalIgnoreCase))
                    NormalizeStaticPropToLength(root, content.normalizeMeters);
            }

            // Native building imports use the shared, source-color-preserving
            // matte contract automatically on placement and saved-lot reload.
            // Authored packages and explicitly authored materials own their look.
            if (string.Equals(content.materialMode, "embedded",
                    System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(content.runtimeProfile, "authored-materials",
                    System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(content.runtimeProfile, "opaque-prop",
                    System.StringComparison.OrdinalIgnoreCase) &&
                root.GetComponent<Building3DPackageInstance>() == null)
                ImportedBuildingMaterials.Prepare(root, _experimentalBuilding3DMaterials);

            if (string.Equals(content.materialMode, "pbr",
                    System.StringComparison.OrdinalIgnoreCase))
                ApplyManifestPbrMaterial(root, content);
            else if (string.Equals(content.runtimeProfile, "opaque-prop",
                         System.StringComparison.OrdinalIgnoreCase))
                SetPropOpacity(root, KingKongEnclosurePropId, 1f, true);
        }

        private void ApplyManifestPbrMaterial(Transform root,
            BuildingContentEntry content)
        {
            if (string.IsNullOrWhiteSpace(content.textureRoot)) return;
            var albedo = Resources.Load<Texture2D>(content.textureRoot + "_basecolor");
            var normal = Resources.Load<Texture2D>(content.textureRoot + "_normal");
            var metallic = Resources.Load<Texture2D>(content.textureRoot + "_metallic");
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR") ??
                         Shader.Find("Standard");
            if (shader == null) return;
            var tint = Color.white;
            if (!string.IsNullOrWhiteSpace(content.tintHex))
                ColorUtility.TryParseHtmlString(content.tintHex, out tint);
            foreach (var sourceRenderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var count = Mathf.Max(1, sourceRenderer.sharedMaterials.Length);
                var materials = new Material[count];
                for (var index = 0; index < count; index++)
                {
                    var material = new Material(shader)
                    {
                        name = $"{content.displayName} — Manifest PBR",
                        color = tint,
                        mainTexture = albedo
                    };
                    if (normal != null)
                    {
                        material.EnableKeyword("_NORMALMAP");
                        material.SetTexture("_BumpMap", normal);
                        material.SetFloat("_BumpScale", content.bumpScale);
                    }
                    if (metallic != null)
                    {
                        material.EnableKeyword("_METALLICGLOSSMAP");
                        material.SetTexture("_MetallicGlossMap", metallic);
                    }
                    material.SetFloat("_Metallic", content.metallic);
                    material.SetFloat("_Glossiness", content.smoothness);
                    materials[index] = material;
                    _experimentalBuilding3DMaterials.Add(material);
                }
                sourceRenderer.sharedMaterials = materials;
                sourceRenderer.shadowCastingMode = ShadowCastingMode.On;
                sourceRenderer.receiveShadows = true;
            }
        }

        private static void GroundExperimentalBuilding(GameObject root)
        {
            var bounds = default(Bounds);
            var hasBounds = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (hasBounds)
                root.transform.position += Vector3.up * -bounds.min.y;
        }

        private void BuildExperimentalBuilding3DReceiverShadowCaster(
            GameObject visibleRoot)
        {
            // District-hosted buildings use the same explicit projected ray
            // as props. Do not also create the Lot Editor's native ground
            // caster, whose shared-light compass produces a second shadow in
            // the opposite screen direction.
            if (_districtHosted)
            {
                foreach (var renderer in visibleRoot
                    .GetComponentsInChildren<Renderer>(true))
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                return;
            }
            var packageInstance =
                visibleRoot.GetComponent<Building3DPackageInstance>();
            if (packageInstance != null && HasPackageShadowRenderers(visibleRoot))
            {
                BuildPackageFloraShadowCaster(visibleRoot);
                return;
            }
            // The experimental PBR beauty shader is not a reliable native
            // caster on every graphics backend. Give the main sun one hidden,
            // mesh-identical caster and disable casting on the visible copy so
            // the ground receives exactly one detailed silhouette (never the
            // old bounds rectangle and never a doubled shadow).
            foreach (var renderer in visibleRoot.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            var groundCaster = Instantiate(visibleRoot, transform);
            groundCaster.name = "3D Building — Brownstone Native Ground Shadow Caster";
            var packageGroundCasterRoot = groundCaster
                .GetComponent<Building3DPackageInstance>() != null
                    ? groundCaster.transform.Find("Representations/LOD3") ??
                      groundCaster.transform.Find("Representations/LOD0")
                    : null;
            DisableClonedPackageLodControl(groundCaster, visibleRoot);
            foreach (var child in groundCaster.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 0;
            foreach (var collider in groundCaster.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            var casterShader = Shader.Find("Standard");
            if (casterShader == null)
                throw new MissingReferenceException(
                    "Unity Standard shader is required for native 3D building shadows.");
            foreach (var renderer in groundCaster.GetComponentsInChildren<Renderer>(true))
            {
                var useRenderer = packageGroundCasterRoot == null ||
                    renderer.transform.IsChildOf(packageGroundCasterRoot);
                renderer.enabled = useRenderer;
                if (!useRenderer)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    continue;
                }
                // The caster is never drawn, so use the engine's canonical
                // opaque ShadowCaster pass instead of the beauty material.
                renderer.sharedMaterial = new Material(casterShader)
                {
                    name = "CF Hidden Native Building Shadow Caster"
                };
                renderer.enabled = true;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderer.allowOcclusionWhenDynamic = false;
            }
            _experimentalBuilding3DRoots.Add(groundCaster);

            // Flora and transparent billboard receivers are deliberately kept
            // on an isolated layer so the main sun cannot light them twice.
            // Mirror the brownstone mesh into that layer as ShadowsOnly: the
            // visible model continues to cast onto opaque ground/3D props,
            // while this copy casts the identical time-of-day shadow across
            // alpha-cutout flora and any future billboard prop receivers.
            var caster = Instantiate(visibleRoot, transform);
            caster.name = "3D Building — Brownstone Flora/Prop Shadow Caster";
            var packageFloraCasterRoot = caster
                .GetComponent<Building3DPackageInstance>() != null
                    ? caster.transform.Find("Representations/LOD3") ??
                      caster.transform.Find("Representations/LOD0")
                    : null;
            DisableClonedPackageLodControl(caster, visibleRoot);
            foreach (var child in caster.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = FloraShadowReceiverLayer;
            foreach (var collider in caster.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (var renderer in caster.GetComponentsInChildren<Renderer>(true))
            {
                var useRenderer = packageFloraCasterRoot == null ||
                    renderer.transform.IsChildOf(packageFloraCasterRoot);
                renderer.enabled = useRenderer;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = useRenderer
                    ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off;
                renderer.allowOcclusionWhenDynamic = false;
            }
            _experimentalBuilding3DRoots.Add(caster);
        }

        private static void DisableClonedPackageLodControl(GameObject clone, GameObject visibleRoot)
        {
            foreach (var interior in clone.GetComponentsInChildren<BuildingInteriorAutomata>(true))
                interior.DisableForShadowCopy();
            // Shadow copies must not duplicate lamps and must follow the visible door.
            foreach (var lighting in clone.GetComponentsInChildren<BuildingNightLighting>(true))
            { lighting.SetNightAmount(0); lighting.enabled = false; }
            foreach (var light in clone.GetComponentsInChildren<Light>(true)) light.enabled = false;
            var sourceDoor = visibleRoot.GetComponentInChildren<BuildingDoorController>(true);
            foreach (var door in clone.GetComponentsInChildren<BuildingDoorController>(true)) door.Follow(sourceDoor);
            var sourceWheel = visibleRoot.GetComponentInChildren<BuildingWheelRotation>(true);
            foreach (var wheel in clone.GetComponentsInChildren<BuildingWheelRotation>(true)) wheel.Follow(sourceWheel);
            var lodGroup = clone.GetComponent<LODGroup>();
            if (lodGroup != null) lodGroup.enabled = false;
            var package = clone.GetComponent<Building3DPackageInstance>();
            if (package != null) package.enabled = false;
        }

        private void BuildPackageFloraShadowCaster(GameObject visibleRoot)
        {
            // The package already contains one configurable layer-0 shadow LOD.
            // Clone only that authored caster onto the isolated flora receiver
            // layer; cloning beauty renderers would double cost and opacity.
            var caster = Instantiate(visibleRoot, transform);
            caster.name = "3D Building — Package Flora/Prop Shadow Caster";
            foreach (var child in caster.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = FloraShadowReceiverLayer;
            foreach (var collider in caster.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (var renderer in caster.GetComponentsInChildren<Renderer>(true))
            {
                var isShadow = IsPackageShadowRenderer(renderer);
                renderer.enabled = isShadow;
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = isShadow
                    ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off;
                renderer.allowOcclusionWhenDynamic = false;
            }
            _experimentalBuilding3DRoots.Add(caster);
        }

        private static bool IsPackageShadowRenderer(Renderer renderer)
        {
            for (var current = renderer.transform; current != null;
                 current = current.parent)
                if (current.name == "ShadowLOD") return true;
            return false;
        }

        private static bool HasPackageShadowRenderers(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (IsPackageShadowRenderer(renderer)) return true;
            return false;
        }

        private Material CreateBrownstoneBuilding22kMaterial()
        {
            var shader = Shader.Find("CityForgeV3/Experimental3DBuildingPBR");
            if (shader == null)
                throw new MissingReferenceException(
                    "CityForge's contrast-preserving PBR shader is required for the 3D building pilot.");
            var material = new Material(shader)
            {
                name = "CF Brownstone Building 22K PBR"
            };
            material.SetFloat("_Contrast", _environmentBuildingContrast);
            material.SetFloat("_Saturation", _environmentBuildingSaturation);
            if (material.HasProperty("_Vibrance"))
                material.SetFloat("_Vibrance", _environmentBuildingVibrance);
            var root = "CityForgeV3/Buildings3D/BrownstoneBuilding22k/";
            material.mainTexture = Resources.Load<Texture2D>(root +
                "brownstone_building_3d_model_basecolor");
            var normal = Resources.Load<Texture2D>(root +
                "brownstone_building_3d_model_normal");
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            var metallicSmoothness = Resources.Load<Texture2D>(root +
                "brownstone-metallic-smoothness");
            if (metallicSmoothness != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicSmoothness);
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_GlossMapScale", 1f);
                material.EnableKeyword("_METALLICGLOSSMAP");
            }
            else
            {
                material.SetFloat("_Metallic", 0.05f);
                material.SetFloat("_Glossiness", 0.28f);
            }
            _experimentalBuilding3DMaterials.Add(material);
            return material;
        }

        public bool BeginBuilding3DDragFromPanel(Vector2 panelPosition,
            Vector2 panelSize)
        {
            if (_camera == null || _session?.Data?.Buildings3D == null) return false;
            var pixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            var ray = _camera.ScreenPointToRay(pixel);
            var bestDistance = float.PositiveInfinity;
            var bestIndex = -1;
            for (var index = 0; index < _experimentalBuilding3DVisibleRoots.Count;
                 index++)
            {
                var root = _experimentalBuilding3DVisibleRoots[index];
                if (root == null) continue;
                if (TryRaycastBuildingBeautyMesh(root, ray,
                        _camera.farClipPlane, out var hit) &&
                    hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestIndex = index;
                }
            }
            if (bestIndex < 0 || bestIndex >= _session.Data.Buildings3D.Count)
                return false;
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out var groundDistance)) return false;
            var point = ray.GetPoint(groundDistance);
            var placed = _session.Data.Buildings3D[bestIndex];
            ActiveObjectSelection = LotObjectSelectionKind.None;
            SelectedFloraIndex = -1;
            SelectedPropIndex = -1;
            _propDragActive = false;
            _floraDragActive = false;
            ApplyPropSelection();
            ApplyFloraSelection();
            ClearObjectHover();
            _selectedBuilding3DIndex = bestIndex;
            _building3DDragOffset = new Vector2(placed.X - point.x,
                placed.Z - point.z);
            _building3DDragActive = true;
            RefreshBuilding3DSelectionOutline();
            return true;
        }

        public bool DragBuilding3DFromPanel(Vector2 panelPosition,
            Vector2 panelSize)
        {
            if (!_building3DDragActive || _camera == null ||
                _selectedBuilding3DIndex < 0 ||
                _selectedBuilding3DIndex >= (_session?.Data?.Buildings3D?.Count ?? 0))
                return false;
            var pixel = PanelToCameraPixel(panelPosition, panelSize,
                new Vector2(_camera.pixelWidth, _camera.pixelHeight));
            var ray = _camera.ScreenPointToRay(pixel);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out var distance)) return false;
            var point = ray.GetPoint(distance);
            var placed = _session.Data.Buildings3D[_selectedBuilding3DIndex];
            var halfWidth = Mathf.Max(1f, LotWidthMeters * 0.5f);
            var halfDepth = Mathf.Max(1f, LotDepthMeters * 0.5f);
            placed.X = Mathf.Clamp(point.x + _building3DDragOffset.x,
                -halfWidth, halfWidth);
            placed.Z = Mathf.Clamp(point.z + _building3DDragOffset.y,
                -halfDepth, halfDepth);
            RebuildExperimentalBuilding3DPresentations();
            ApplySelectedBuilding3DPlacementOpacity();
            UpdateExperimentalBuilding3DProjectedGroundShadows();
            NotifyStateChanged();
            return true;
        }

        public void EndBuilding3DDrag()
        {
            _building3DDragActive = false;
            if (!_building3DPlacementPreviewActive) return;
            _building3DPlacementPreviewActive = false;
            // Reinstantiating restores the package's exact authored material
            // modes after the temporary transparent placement treatment.
            RebuildExperimentalBuilding3DPresentations();
        }

        public bool SelectBuilding3DForQa(int index)
        {
            if (index < 0 || index >= (_session?.Data?.Buildings3D?.Count ?? 0) ||
                index >= _experimentalBuilding3DVisibleRoots.Count)
                return false;
            _selectedBuilding3DIndex = index;
            _building3DDragActive = false;
            RefreshBuilding3DSelectionOutline();
            return true;
        }

        public void DeselectBuilding3D()
        {
            if (_selectedBuilding3DIndex < 0 &&
                _building3DSelectionOutline == null) return;
            _selectedBuilding3DIndex = -1;
            _building3DDragActive = false;
            var restorePlacementMaterials = _building3DPlacementPreviewActive;
            _building3DPlacementPreviewActive = false;
            if (restorePlacementMaterials)
                RebuildExperimentalBuilding3DPresentations();
            RefreshBuilding3DSelectionOutline();
            NotifyStateChanged();
        }

        public bool RotateSelectedBuilding3D(int direction)
        {
            if (_selectedBuilding3DIndex < 0 ||
                _selectedBuilding3DIndex >= (_session?.Data?.Buildings3D?.Count ?? 0))
                return false;
            var placed = _session.Data.Buildings3D[_selectedBuilding3DIndex];
            var eighthTurns = placed.RotationEighthTurns >= 0
                ? placed.RotationEighthTurns
                : placed.RotationQuarterTurns * 2;
            placed.RotationEighthTurns = (eighthTurns + direction % 8 + 8) % 8;
            placed.RotationQuarterTurns = placed.RotationEighthTurns / 2;
            RebuildExperimentalBuilding3DPresentations();
            ApplyTimeOfDay();
            NotifyStateChanged();
            return true;
        }

        public bool BuildSelectedBuilding3D()
        {
            if (_selectedBuilding3DIndex < 0 ||
                _selectedBuilding3DIndex >= _experimentalBuilding3DVisibleRoots.Count)
                return false;
            var root = _experimentalBuilding3DVisibleRoots[
                _selectedBuilding3DIndex];
            if (root == null ||
                root.GetComponent<BuildingConstructionSequence>() != null)
                return false;
            var bounds = CombinedRendererBounds(root, out var hasBounds);
            if (!hasBounds) return false;
            var width = bounds.size.x;
            var depth = bounds.size.z;
            var existingFrame = FindConstructionFrame(root);
            if (existingFrame != null)
                DestroyForCurrentMode(existingFrame.gameObject);
            var sequence = root.AddComponent<BuildingConstructionSequence>();
            if (_experimentalBuilding3DGroundShadows.TryGetValue(root,
                    out var shadow) && shadow != null)
                shadow.SetActive(false);
            sequence.Begin(root, width, depth, bounds.size.y, () =>
            {
                if (sequence.IsComplete && shadow != null)
                    shadow.SetActive(true);
                NotifyStateChanged();
            }, new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                Vector3.one, true);
            RefreshBuilding3DSelectionOutline();
            NotifyStateChanged();
            return true;
        }

        public int BeginAllBuildingConstruction()
        {
            var started = 0;
            LotConstructionSite site = null;
            var roots = new HashSet<GameObject>();
            foreach (var root in _experimentalBuilding3DVisibleRoots)
                if (root != null) roots.Add(root);
            if (_presentation != null) roots.Add(_presentation.gameObject);
            foreach (var presentation in _otherBuildingPresentations)
                if (presentation != null) roots.Add(presentation.gameObject);
            foreach (var root in roots)
            {
                if (!root.activeInHierarchy || root.GetComponent<BuildingConstructionSequence>() != null)
                    continue;
                var bounds = CombinedRendererBounds(root, out var hasBounds);
                if (!hasBounds) continue;
                var sequence = root.AddComponent<BuildingConstructionSequence>();
                if (_districtHosted)
                {
                    site ??= GetComponent<LotConstructionSite>() ?? gameObject.AddComponent<LotConstructionSite>();
                    site.Track(sequence, LotWidthMeters, LotDepthMeters);
                }
                _experimentalBuilding3DGroundShadows.TryGetValue(root,
                    out var shadow);
                if (shadow != null) shadow.SetActive(false);
                sequence.Begin(root, bounds.size.x, bounds.size.z,
                    bounds.size.y, () =>
                    {
                        site?.SequenceChanged(sequence);
                        if (sequence.IsComplete && shadow != null)
                            shadow.SetActive(true);
                        NotifyStateChanged();
                    }, new Vector3(bounds.center.x, bounds.min.y,
                        bounds.center.z), Vector3.one, true);
                started++;
            }
            if (started > 0) NotifyStateChanged();
            return started;
        }

        public bool ToggleSelectedBuildingConstructionFrame()
        {
            if (_selectedBuilding3DIndex < 0 ||
                _selectedBuilding3DIndex >= _experimentalBuilding3DVisibleRoots.Count)
                return false;
            var root = _experimentalBuilding3DVisibleRoots[
                _selectedBuilding3DIndex];
            if (root == null || root.GetComponent<BuildingConstructionSequence>() != null)
                return false;
            var existing = FindConstructionFrame(root);
            if (existing != null)
            {
                DestroyForCurrentMode(existing.gameObject);
                NotifyStateChanged();
                return true;
            }
            var bounds = CombinedRendererBounds(root, out var hasBounds);
            if (!hasBounds) return false;
            // The selection outline is built from these same world renderer
            // bounds. Build the diagnostic frame in that coordinate space as
            // well, so imported authoring pivots, axis conversions, and unit
            // scales cannot move or resize it a second time.
            var width = bounds.size.x;
            var depth = bounds.size.z;
            var frameObject = new GameObject(
                "3D Building Construction Frame Preview");
            frameObject.transform.SetParent(transform, true);
            frameObject.transform.SetPositionAndRotation(new Vector3(
                bounds.center.x, bounds.min.y, bounds.center.z),
                Quaternion.identity);
            frameObject.transform.localScale = Vector3.one;
            var frame = frameObject.AddComponent<BuildingConstructionFramePreview>();
            frame.SetOwner(root);
            frame.Build(width, depth, bounds.size.y);
            NotifyStateChanged();
            return true;
        }

        private BuildingConstructionFramePreview FindConstructionFrame(
            GameObject ownerRoot)
        {
            if (ownerRoot == null) return null;
            foreach (var frame in GetComponentsInChildren<
                         BuildingConstructionFramePreview>(true))
                if (frame != null && frame.OwnerRoot == ownerRoot)
                    return frame;
            return null;
        }

        private static Vector3 ConstructionLocalOrigin(Transform buildingRoot,
            Bounds visibleBounds)
        {
            // Production packages may place and scale their representation
            // beneath an authoring pivot that is nowhere near the visual
            // building centre. Anchor procedural construction to the actual
            // rendered footprint and its grounded bottom instead.
            return buildingRoot.InverseTransformPoint(new Vector3(
                visibleBounds.center.x,
                visibleBounds.min.y,
                visibleBounds.center.z));
        }

        private static Vector3 ConstructionScaleCompensation(
            Transform buildingRoot)
        {
            // Frame dimensions are already expressed in world metres. Cancel
            // the package root scale so parenting does not apply that authored
            // conversion a second time.
            var scale = buildingRoot.lossyScale;
            return new Vector3(
                1f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        }

        public bool DeleteSelectedBuilding3D()
        {
            if (_selectedBuilding3DIndex < 0 ||
                _selectedBuilding3DIndex >= (_session?.Data?.Buildings3D?.Count ?? 0))
                return false;
            var preservedCamera = CaptureCameraFraming();
            _session.Data.Buildings3D.RemoveAt(_selectedBuilding3DIndex);
            _selectedBuilding3DIndex = -1;
            _building3DDragActive = false;
            _building3DPlacementPreviewActive = false;
            ActiveObjectSelection = LotObjectSelectionKind.None;
            ClearObjectHover();
            RebuildExperimentalBuilding3DPresentations();
            ApplyTimeOfDay();
            RestoreCameraFraming(preservedCamera);
            RefreshBuilding3DSelectionOutline();
            NotifyStateChanged();
            return true;
        }

        private void RefreshBuilding3DSelectionOutline()
        {
            if (_building3DSelectionOutline != null)
                DestroyForCurrentMode(_building3DSelectionOutline);
            _building3DSelectionOutline = null;
            if (_selectedBuilding3DIndex < 0 ||
                _selectedBuilding3DIndex >= _experimentalBuilding3DVisibleRoots.Count)
                return;
            var source = _experimentalBuilding3DVisibleRoots[
                _selectedBuilding3DIndex];
            if (source == null) return;
            var stableLod = source.transform.Find("Representations/LOD0");
            var bounds = CombinedRendererBounds(
                stableLod == null ? source : stableLod.gameObject,
                out var hasBounds);
            if (!hasBounds) return;
            if (_session?.Data?.Buildings3D != null &&
                _selectedBuilding3DIndex < _session.Data.Buildings3D.Count &&
                _session.Data.Buildings3D[_selectedBuilding3DIndex]?.AssetId ==
                    KingKongEnclosureBuilding3DId)
                bounds = KingKongEnclosureVisibleBounds(bounds);
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            if (_building3DSelectionOutlineMaterial == null)
                _building3DSelectionOutlineMaterial = new Material(shader)
                {
                    name = "CF 3D Building Selection — Light Blue"
                };
            _building3DSelectionOutlineMaterial.color =
                new Color(0.3f, 0.82f, 1f, 0.95f);
            _building3DSelectionOutline = new GameObject(
                "3D Building Selection Outline");
            _building3DSelectionOutline.transform.SetParent(transform, true);
            _building3DSelectionOutline.name = "3D Building Selection Outline";
            if (BuildMeshSelectionSilhouette(
                    stableLod == null ? source : stableLod.gameObject))
            {
                _experimentalBuilding3DRoots.Add(_building3DSelectionOutline);
                return;
            }
            // A building without selectable mesh geometry has no approximate
            // selection surface. Bounds and catalog footprints are explicitly
            // not selection truth.
            DestroyForCurrentMode(_building3DSelectionOutline);
            _building3DSelectionOutline = null;
        }

        private bool BuildMeshSelectionSilhouette(GameObject geometryRoot)
        {
            var shader = Shader.Find("CityForgeV3/MeshSelectionOutline");
            if (shader == null || geometryRoot == null) return false;
            if (_building3DMeshSelectionMaterial == null)
            {
                _building3DMeshSelectionMaterial = new Material(shader)
                {
                    name = "CF 3D Building Mesh Selection Silhouette"
                };
                _building3DMeshSelectionMaterial.SetColor("_Color",
                    new Color(0.3f, 0.82f, 1f, 0.95f));
                _building3DMeshSelectionMaterial.SetFloat("_OutlineWidth", 0.035f);
            }
            var built = false;
            foreach (var sourceFilter in geometryRoot.GetComponentsInChildren<
                         MeshFilter>(true))
            {
                var sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();
                if (sourceFilter.sharedMesh == null || sourceRenderer == null ||
                    !sourceRenderer.enabled ||
                    !IsBuildingSelectionBeautyRenderer(sourceRenderer,
                        geometryRoot.transform)) continue;
                var outlinePart = new GameObject(
                    $"Mesh Edge — {sourceFilter.gameObject.name}");
                outlinePart.transform.SetParent(
                    _building3DSelectionOutline.transform, true);
                outlinePart.transform.SetPositionAndRotation(
                    sourceFilter.transform.position,
                    sourceFilter.transform.rotation);
                outlinePart.transform.localScale = sourceFilter.transform.lossyScale;
                outlinePart.AddComponent<MeshFilter>().sharedMesh =
                    sourceFilter.sharedMesh;
                var outlineRenderer = outlinePart.AddComponent<MeshRenderer>();
                outlineRenderer.sharedMaterial = _building3DMeshSelectionMaterial;
                outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
                built = true;
            }
            return built;
        }

        private static GameObject BuildingSelectionGeometryRoot(GameObject root)
        {
            if (root == null) return null;
            var lod0 = root.transform.Find("Representations/LOD0");
            return lod0 == null ? root : lod0.gameObject;
        }

        private static bool TryRaycastBuildingBeautyMesh(GameObject root,
            Ray ray, float maximumDistance, out RaycastHit closestHit)
        {
            closestHit = default;
            var geometryRoot = BuildingSelectionGeometryRoot(root);
            if (geometryRoot == null) return false;
            var found = false;
            var closestDistance = maximumDistance;
            foreach (var filter in geometryRoot.GetComponentsInChildren<
                         MeshFilter>(true))
            {
                var renderer = filter.GetComponent<Renderer>();
                if (filter.sharedMesh == null || renderer == null ||
                    !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    !IsBuildingSelectionBeautyRenderer(renderer,
                        geometryRoot.transform)) continue;
                var collider = filter.GetComponent<MeshCollider>();
                if (collider == null)
                    collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
                collider.enabled = true;
                if (!collider.Raycast(ray, out var hit, closestDistance))
                    continue;
                closestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }
            return found;
        }

        private static bool IsBuildingSelectionBeautyRenderer(
            Renderer renderer, Transform geometryRoot)
        {
            if (renderer == null || IsPackageShadowRenderer(renderer))
                return false;
            for (var current = renderer.transform; current != null;
                 current = current.parent)
            {
                var name = current.name;
                if (name.IndexOf("Collision",
                        System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Receiver",
                        System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Projected",
                        System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Selection Outline",
                        System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
                if (current == geometryRoot) break;
            }
            return true;
        }

        private void BuildFootprintSelectionOutline(IReadOnlyList<Vector2> contour,
            float minimumY, float maximumY)
        {
            for (var index = 0; index < contour.Count; index++)
            {
                var next = (index + 1) % contour.Count;
                var bottomA = new Vector3(contour[index].x, minimumY,
                    contour[index].y);
                var bottomB = new Vector3(contour[next].x, minimumY,
                    contour[next].y);
                var topA = new Vector3(contour[index].x, maximumY + 0.08f,
                    contour[index].y);
                var topB = new Vector3(contour[next].x, maximumY + 0.08f,
                    contour[next].y);
                AddBuildingSelectionLine(bottomA, bottomB);
                AddBuildingSelectionLine(topA, topB);
                AddBuildingSelectionLine(bottomA, topA);
            }
        }

        private void AddBuildingSelectionLine(Vector3 start, Vector3 end)
        {
            var lineObject = new GameObject("Footprint Edge");
            lineObject.transform.SetParent(_building3DSelectionOutline.transform,
                true);
            var line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = _building3DSelectionOutlineMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = line.endWidth = 0.08f;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private static Bounds CombinedRendererBounds(GameObject root,
            out bool hasBounds, bool includeDisabled = false)
        {
            var bounds = default(Bounds);
            hasBounds = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if ((!includeDisabled && !renderer.enabled) ||
                    IsPackageShadowRenderer(renderer)) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static Bounds KingKongEnclosureVisibleBounds(Bounds imported)
        {
            var size = imported.size;
            size.x *= KingKongEnclosureVisibleBoundsScale;
            size.z *= KingKongEnclosureVisibleBoundsScale;
            return new Bounds(imported.center, size);
        }

        private bool TryGetProjectedMeshFootprint(GameObject root,
            out List<Vector2> contour)
        {
            if (_buildingFootprintContours.TryGetValue(root, out contour))
                return contour != null && contour.Count >= 3;
            contour = BuildProjectedMeshFootprint(root, 96);
            _buildingFootprintContours[root] = contour;
            return contour != null && contour.Count >= 3;
        }

        private bool Building3DFootprintContains(Vector2 point)
        {
            var worldAnchor = transform.TransformPoint(
                new Vector3(point.x, 0f, point.y));
            var worldPoint = new Vector2(worldAnchor.x, worldAnchor.z);
            foreach (var root in _experimentalBuilding3DVisibleRoots)
            {
                if (root == null) continue;
                var geometry = BuildingSelectionGeometryRoot(root);
                if (!_buildingFloraBounds.TryGetValue(geometry, out var bounds))
                {
                    bounds = CombinedRendererBounds(geometry,
                        out var hasBounds, includeDisabled: true);
                    if (!hasBounds) continue;
                    _buildingFloraBounds[geometry] = bounds;
                }
                if (worldPoint.x < bounds.min.x ||
                    worldPoint.x > bounds.max.x ||
                    worldPoint.y < bounds.min.z ||
                    worldPoint.y > bounds.max.z) continue;
                if (!TryGetProjectedMeshFootprint(geometry, out var contour))
                    return true;
                if (PointInFootprint(worldPoint, contour)) return true;
            }
            return false;
        }

        private static bool PointInFootprint(Vector2 point,
            IReadOnlyList<Vector2> contour)
        {
            var inside = false;
            for (int i = 0, previous = contour.Count - 1;
                 i < contour.Count; previous = i++)
            {
                var a = contour[previous];
                var b = contour[i];
                if ((a.y > point.y) == (b.y > point.y)) continue;
                var crossingX = a.x + (point.y - a.y) *
                    (b.x - a.x) / (b.y - a.y);
                if (point.x < crossingX) inside = !inside;
            }
            return inside;
        }

        private static List<Vector2> BuildProjectedMeshFootprint(GameObject root,
            int resolution)
        {
            var bounds = CombinedRendererBounds(root, out var hasBounds);
            if (!hasBounds || bounds.size.x < 0.01f || bounds.size.z < 0.01f)
                return null;
            var occupied = new HashSet<int>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<Renderer>();
                var mesh = filter.sharedMesh;
                if (mesh == null || renderer == null ||
                    !mesh.isReadable || IsPackageShadowRenderer(renderer)) continue;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                for (var triangle = 0; triangle + 2 < triangles.Length;
                     triangle += 3)
                {
                    var a3 = filter.transform.TransformPoint(
                        vertices[triangles[triangle]]);
                    var b3 = filter.transform.TransformPoint(
                        vertices[triangles[triangle + 1]]);
                    var c3 = filter.transform.TransformPoint(
                        vertices[triangles[triangle + 2]]);
                    var a = FootprintGridPoint(a3, bounds, resolution);
                    var b = FootprintGridPoint(b3, bounds, resolution);
                    var c = FootprintGridPoint(c3, bounds, resolution);
                    var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,
                        Mathf.Min(b.x, c.x))), 0, resolution - 1);
                    var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,
                        Mathf.Max(b.x, c.x))), 0, resolution - 1);
                    var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,
                        Mathf.Min(b.y, c.y))), 0, resolution - 1);
                    var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,
                        Mathf.Max(b.y, c.y))), 0, resolution - 1);
                    for (var y = minY; y <= maxY; y++)
                    for (var x = minX; x <= maxX; x++)
                        if (PointInTriangle(new Vector2(x + 0.5f, y + 0.5f),
                                a, b, c))
                            occupied.Add(y * resolution + x);
                }
            }
            if (occupied.Count == 0) return null;

            var edges = new HashSet<ulong>();
            foreach (var cell in occupied)
            {
                var x = cell % resolution;
                var y = cell / resolution;
                AddOrCancelGridEdge(edges, GridPointKey(x, y, resolution),
                    GridPointKey(x + 1, y, resolution));
                AddOrCancelGridEdge(edges, GridPointKey(x + 1, y, resolution),
                    GridPointKey(x + 1, y + 1, resolution));
                AddOrCancelGridEdge(edges, GridPointKey(x + 1, y + 1, resolution),
                    GridPointKey(x, y + 1, resolution));
                AddOrCancelGridEdge(edges, GridPointKey(x, y + 1, resolution),
                    GridPointKey(x, y, resolution));
            }
            var nextByStart = new Dictionary<int, List<int>>();
            foreach (var edge in edges)
            {
                var start = (int)(edge >> 32);
                var end = (int)(edge & uint.MaxValue);
                if (!nextByStart.TryGetValue(start, out var next))
                    nextByStart[start] = next = new List<int>();
                next.Add(end);
            }
            List<int> best = null;
            var bestArea = 0f;
            foreach (var first in nextByStart.Keys)
            {
                var loop = TraceGridLoop(first, nextByStart,
                    edges.Count + 1);
                if (loop == null || loop.Count < 4) continue;
                var area = Mathf.Abs(GridLoopArea(loop, resolution));
                if (area <= bestArea) continue;
                bestArea = area;
                best = loop;
            }
            if (best == null) return null;
            var result = new List<Vector2>();
            foreach (var key in best)
            {
                var gx = key % (resolution + 1);
                var gy = key / (resolution + 1);
                result.Add(new Vector2(
                    Mathf.Lerp(bounds.min.x, bounds.max.x,
                        gx / (float)resolution),
                    Mathf.Lerp(bounds.min.z, bounds.max.z,
                        gy / (float)resolution)));
            }
            SimplifyGridContour(result);
            return result;
        }

        private static Vector2 FootprintGridPoint(Vector3 world, Bounds bounds,
            int resolution) => new(
            (world.x - bounds.min.x) / bounds.size.x * resolution,
            (world.z - bounds.min.z) / bounds.size.z * resolution);

        private static bool PointInTriangle(Vector2 point, Vector2 a,
            Vector2 b, Vector2 c)
        {
            var ab = Cross2D(b - a, point - a);
            var bc = Cross2D(c - b, point - b);
            var ca = Cross2D(a - c, point - c);
            return (ab >= 0f && bc >= 0f && ca >= 0f) ||
                (ab <= 0f && bc <= 0f && ca <= 0f);
        }

        private static float Cross2D(Vector2 a, Vector2 b) =>
            a.x * b.y - a.y * b.x;

        private static int GridPointKey(int x, int y, int resolution) =>
            y * (resolution + 1) + x;

        private static void AddOrCancelGridEdge(HashSet<ulong> edges,
            int start, int end)
        {
            var edge = ((ulong)(uint)start << 32) | (uint)end;
            var reverse = ((ulong)(uint)end << 32) | (uint)start;
            if (!edges.Remove(reverse)) edges.Add(edge);
        }

        private static List<int> TraceGridLoop(int first,
            Dictionary<int, List<int>> nextByStart, int limit)
        {
            var result = new List<int> { first };
            var current = first;
            for (var step = 0; step < limit; step++)
            {
                if (!nextByStart.TryGetValue(current, out var next) ||
                    next.Count == 0) return null;
                current = next[0];
                if (current == first) return result;
                result.Add(current);
            }
            return null;
        }

        private static float GridLoopArea(IReadOnlyList<int> loop,
            int resolution)
        {
            var area = 0f;
            for (var index = 0; index < loop.Count; index++)
            {
                var next = (index + 1) % loop.Count;
                var ax = loop[index] % (resolution + 1);
                var ay = loop[index] / (resolution + 1);
                var bx = loop[next] % (resolution + 1);
                var by = loop[next] / (resolution + 1);
                area += ax * by - bx * ay;
            }
            return area * 0.5f;
        }

        private static void SimplifyGridContour(List<Vector2> contour)
        {
            for (var index = contour.Count - 1; index >= 0 && contour.Count > 3;
                 index--)
            {
                var previous = contour[(index - 1 + contour.Count) % contour.Count];
                var current = contour[index];
                var next = contour[(index + 1) % contour.Count];
                if (Mathf.Abs(Cross2D(current - previous, next - current)) <
                    0.0001f)
                    contour.RemoveAt(index);
            }
        }

        private bool ProjectedBoundsContainsPixel(Bounds bounds, Vector2 pixel,
            out float nearestDepth)
        {
            nearestDepth = float.PositiveInfinity;
            if (_camera == null) return false;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var center = bounds.center;
            var extents = bounds.extents;
            var hasVisibleCorner = false;
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var screen = _camera.WorldToScreenPoint(center +
                    Vector3.Scale(extents, new Vector3(x, y, z)));
                if (screen.z <= 0f) continue;
                hasVisibleCorner = true;
                nearestDepth = Mathf.Min(nearestDepth, screen.z);
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }
            return hasVisibleCorner && pixel.x >= min.x && pixel.x <= max.x &&
                   pixel.y >= min.y && pixel.y <= max.y;
        }
    }
}
