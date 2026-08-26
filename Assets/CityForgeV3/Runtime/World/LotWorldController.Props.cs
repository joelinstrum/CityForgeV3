using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private const string FencePropId = "wrought-iron-fence-straight-v01";
        private const string FenceCornerPropId = "wrought-iron-fence-corner-v01";
        public const string PicketFencePropId = "white-picket-fence-v01";
        public const string DecorativeIronGardenPropId =
            "decorative-iron-garden-lamp-v01";
        public const string OrnateIronCornerPropId =
            "ornate-iron-corner-fence-v01";
        private const string ThreeLanternLamppostPropId =
            "three-lantern-lamppost-v01";
        public const string SimpleStreetLamppostPropId =
            "simple-street-lamppost-v01";
        public const string OrnateBenchPropId = "ornate-bench-v01";
        public const string VictorianGentlemanCharacterId =
            "victorian-gentleman-animated-v01";
        public const string HooliganCharacterId = "hooligan-animated-v01";
        public const string HistoricPolicemanCharacterId =
            "historic-policeman-animated-v01";
        private const string FenceResourcePath =
            "CityForgeV3/Props/WroughtIronFenceV01/CF_WroughtIronFence_Straight_LOD0_v01";
        private const string FenceCornerResourcePath =
            "CityForgeV3/Props/WroughtIronFenceV01/CF_WroughtIronFence_Corner_LShape_LOD0_v02";
        private const string PicketFenceResourcePath =
            "CityForgeV3/Props/PicketFenceV01/CF_Prop_PicketFence_Straight_v01";
        private const string DecorativeIronGardenResourcePath =
            "CityForgeV3/Props/WroughtIronVariationsV01/CF_Prop_DecorativeIronFence_v01";
        private const string OrnateIronCornerResourcePath =
            "CityForgeV3/Props/WroughtIronVariationsV01/CF_Prop_OrnateIronGate_v01";
        private const string ThreeLanternLamppostResourcePath =
            "CityForgeV3/Props/ThreeLanternLamppostV01/CF_Prop_ThreeLanternLamppost_01_game_v01";
        private const string SimpleStreetLamppostResourcePath =
            "CityForgeV3/Props/SimpleStreetLamppostV01/SimpleStreetLamppostV01";
        private const string VictorianGentlemanResourcePath =
            "CityForgeV3/Props/Characters/VictorianGentlemanV01/VictorianGentlemanAnimatedV01";
        private const string HooliganResourcePath =
            "CityForgeV3/Props/Characters/HooliganV01/HooliganAnimatedV01";
        private const string HistoricPolicemanResourcePath =
            "CityForgeV3/Props/Characters/HistoricPolicemanV01/HistoricPolicemanAnimatedV01";
        private const string OrnateBenchResourcePath =
            "CityForgeV3/Props/OrnateBenchV01/OrnateBenchV01";
        // Blender FBX imports through Unity at 0.01; 336 restores the authored
        // 1.9-unit section to a 6.38 m City Forge prop.
        private const float FenceScale = 336f;
        // The corner source was authored 2.51x larger than the straight source.
        // 134 restores its pier/fence height to the straight section's 1.5 m.
        private const float FenceCornerScale = 134f;
        private const float FenceLengthMeters = 6.4f;
        private const float FenceDepthMeters = 0.48f;
        private const float FenceCornerLengthMeters = 2.55f;
        private const float PicketFenceLengthMeters = 2.4f;
        private const float PicketFenceDepthMeters = 0.25f;
        private const float DecorativeIronGardenWidthMeters = 1.61f;
        private const float DecorativeIronGardenDepthMeters = 1.48f;
        private const float OrnateIronCornerWidthMeters = 5.30f;
        private const float OrnateIronCornerDepthMeters = 2.87f;
        private const float ThreeLanternLamppostFootprintMeters = 0.9f;
        private const float SimpleStreetLamppostFootprintMeters = 0.55f;
        private const float SimpleStreetLamppostHeightMeters = 3.6f;
        private const float OrnateBenchLengthMeters = 1.8f;
        private const float OrnateBenchDepthMeters = 0.7f;
        // Blender's FBX is authored in meters but Unity imports this exporter
        // contract at 0.01, so 100 restores the intended 4.5 m height.
        private const float ThreeLanternLamppostScale = 100f;
        internal const int PropFrontRecoveryLayer = 30;
        private Transform _propRoot;
        private Transform _propSelection;
        private Transform _propPreview;
        private Camera _propFrontRecoveryCamera;
        private readonly List<Transform> _propPresentations = new();
        private readonly List<Renderer> _propProjectedShadowRenderers = new();
        private readonly List<Material> _propProjectedShadowMaterials = new();
        private readonly Dictionary<string, float> _characterBusinessAsUsualUntil =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _characterManualOverrideUntil =
            new(StringComparer.OrdinalIgnoreCase);
        private bool _propEditorActive;
        private bool _propPlacementActive;
        private bool _propDragActive;
        private Vector2 _propDragOffset;
        private string _propPreviewId = "";
        private bool _propPreviewHasPoint;

        public int PropCount => _session.Data.Props?.Count ?? 0;
        public int SelectedPropIndex { get; private set; } = -1;
        public bool SelectedPropIsThreeDimensionalCharacter =>
            SelectedPropIndex >= 0 && SelectedPropIndex < PropCount &&
            IsThreeDimensionalCharacter(
                _session.Data.Props[SelectedPropIndex].PropId);
        public string SelectedCharacterAnimationState =>
            SelectedPropIsThreeDimensionalCharacter
                ? _session.Data.Props[SelectedPropIndex].AnimationState
                : "";
        public string SelectedCharacterBehaviorScript =>
            SelectedPropIsThreeDimensionalCharacter
                ? CharacterBehaviorScript.Normalize(
                    _session.Data.Props[SelectedPropIndex].BehaviorScript)
                : CharacterBehaviorScript.BusinessAsUsual;

        public static bool IsThreeDimensionalCharacter(string propId) =>
            string.Equals(propId, VictorianGentlemanCharacterId,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(propId, HooliganCharacterId,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(propId, HistoricPolicemanCharacterId,
                StringComparison.OrdinalIgnoreCase);

        public static bool IsHooligan(string propId) =>
            string.Equals(propId, HooliganCharacterId,
                StringComparison.OrdinalIgnoreCase);

        public static bool IsHistoricPoliceman(string propId) =>
            string.Equals(propId, HistoricPolicemanCharacterId,
                StringComparison.OrdinalIgnoreCase);

        private static string CharacterResourcePath(string propId) =>
            IsHooligan(propId) ? HooliganResourcePath :
            IsHistoricPoliceman(propId) ? HistoricPolicemanResourcePath :
            VictorianGentlemanResourcePath;

#if UNITY_EDITOR
        public bool PlacePropForQa(string propId, float positionX, float positionZ)
        {
            _session.Data.Props ??= new List<PlacedProp>();
            var prop = _session.Data.Props.Find(item =>
                string.Equals(item.PropId, propId, StringComparison.OrdinalIgnoreCase));
            if (prop == null)
            {
                prop = new PlacedProp
                {
                    InstanceId = Guid.NewGuid().ToString("N"),
                    PropId = propId,
                    RotationQuarterTurns = 0,
                    BehaviorScript = DefaultCharacterBehaviorScript(propId)
                };
                _session.Data.Props.Add(prop);
            }
            prop.PositionX = positionX;
            prop.PositionZ = positionZ;
            RebuildPropPresentations();
            NotifyStateChanged();
            return true;
        }
#endif

        private void BuildPropRoot()
        {
            _propRoot = new GameObject("Placed Props").transform;
            _propRoot.SetParent(transform, false);
            _propSelection = new GameObject("Selected Prop Highlight").transform;
            _propSelection.SetParent(transform, false);
            var material = LotSurfaceMaterial(new Color(1f, 0.72f, 0.12f, 0.9f), 2015);
            foreach (var edge in new[]
                     {
                         (new Vector3(0f, 0f, FenceDepthMeters * 0.5f),
                             new Vector3(FenceLengthMeters, 0.05f, 0.07f)),
                         (new Vector3(0f, 0f, -FenceDepthMeters * 0.5f),
                             new Vector3(FenceLengthMeters, 0.05f, 0.07f)),
                         (new Vector3(FenceLengthMeters * 0.5f, 0f, 0f),
                             new Vector3(0.07f, 0.05f, FenceDepthMeters)),
                         (new Vector3(-FenceLengthMeters * 0.5f, 0f, 0f),
                             new Vector3(0.07f, 0.05f, FenceDepthMeters))
                     })
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.transform.SetParent(_propSelection, false);
                marker.transform.localPosition = edge.Item1;
                marker.transform.localScale = edge.Item2;
                marker.GetComponent<Collider>().enabled = false;
                marker.GetComponent<Renderer>().sharedMaterial = material;
            }
            _propSelection.gameObject.SetActive(false);
        }

        public void SetPropEditorContext(bool active)
        {
            _propEditorActive = true;
            _propPlacementActive = active && !_cameraPanInteractionActive;
            ApplyPropSelection();
            if (_propPreview != null)
                _propPreview.gameObject.SetActive(_propPlacementActive && _propPreviewHasPoint &&
                    !string.IsNullOrWhiteSpace(_propPreviewId));
        }

        public void SetPropPlacementPreview(string propId)
        {
            _propPreviewId = propId ?? "";
            _propPreviewHasPoint = false;
            if (_propPreview != null)
            {
                if (Application.isPlaying) Destroy(_propPreview.gameObject);
                else DestroyImmediate(_propPreview.gameObject);
            }
            _propPreview = CreatePropPresentation(_propPreviewId, "Prop Placement Preview", 0.5f);
            if (_propPreview != null)
            {
                _propPreview.SetParent(transform, false);
                _propPreview.gameObject.SetActive(false);
            }
        }

        public bool UpdatePropPreviewFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_propPlacementActive || string.IsNullOrWhiteSpace(_propPreviewId) ||
                _propPreview == null ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            var position = ClampPropPosition(new Vector2(point.x, point.z), 0);
            _propPreview.localPosition = new Vector3(position.x, 0.055f, position.y);
            SetPropOpacity(_propPreview, _propPreviewId,
                CanPlacePropAt(position, 0, -1) ? 0.5f : 0.28f,
                CanPlacePropAt(position, 0, -1));
            _propPreviewHasPoint = true;
            _propPreview.gameObject.SetActive(true);
            return true;
        }

        public bool BeginPropDragFromPanel(string propId,
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            var placingNewProp = !string.IsNullOrWhiteSpace(propId);
            if (placingNewProp)
            {
                // An armed catalog choice means "place another", even when its
                // camera-space bounds overlap an existing prop. This keeps
                // placement continuous within a 10 m lot tile; physical
                // footprint validation below still rejects true intersections.
                SelectedPropIndex = -1;
                _propPreviewId = propId;
                var position = ClampPropPosition(new Vector2(point.x, point.z), 0);
                if (!CanPlacePropAt(position, 0, -1)) return false;
                _session.Data.Props ??= new List<PlacedProp>();
                _session.Data.Props.Add(new PlacedProp
                {
                    InstanceId = Guid.NewGuid().ToString("N"),
                    PropId = propId,
                    PositionX = position.x,
                    PositionZ = position.y,
                    BehaviorScript = DefaultCharacterBehaviorScript(propId)
                });
                SelectedPropIndex = _session.Data.Props.Count - 1;
                RebuildPropPresentations();
            }
            else
            {
                var pixel = PanelToCameraPixel(panelPosition, panelSize,
                    new Vector2(_camera.pixelWidth, _camera.pixelHeight));
                SelectedPropIndex = PropIndexAtCameraPixel(pixel);
                if (SelectedPropIndex < 0) return false;
            }
            ActiveObjectSelection = LotObjectSelectionKind.Prop;
            SelectedFloraIndex = -1;
            ApplyFloraSelection();
            _session.Select(false);
            if (_selectionFootprint != null)
                _selectionFootprint.gameObject.SetActive(false);
            var selected = _session.Data.Props[SelectedPropIndex];
            _propDragOffset = new Vector2(selected.PositionX - point.x,
                selected.PositionZ - point.z);
            _propDragActive = true;
            _propPreviewHasPoint = false;
            if (_propPreview != null) _propPreview.gameObject.SetActive(false);
            ApplyPropSelection();
            return true;
        }

        public bool NudgeSelectedPropByScreenPixels(int horizontal, int vertical)
        {
            if (SelectedPropIndex < 0 || SelectedPropIndex >= PropCount ||
                _camera == null || (horizontal == 0 && vertical == 0)) return false;
            if (!TryGroundDeltaForArrowKey(horizontal, vertical, out var delta))
                return false;
            var prop = _session.Data.Props[SelectedPropIndex];
            var target = ClampPropPosition(
                new Vector2(prop.PositionX + delta.x, prop.PositionZ + delta.z),
                prop.RotationQuarterTurns);
            if (!CanPlacePropAt(target, prop.RotationQuarterTurns, SelectedPropIndex))
                return false;
            prop.PositionX = target.x;
            prop.PositionZ = target.y;
            // Moving can cross a building-front recovery boundary. Rebuild so
            // the physical prop immediately receives (or sheds) the recovery
            // material appropriate to its new location.
            RebuildPropPresentations();
            return true;
        }

        public bool WalkSelectedCharacter(int horizontal, int vertical)
        {
            if (!SelectedPropIsThreeDimensionalCharacter ||
                (horizontal == 0 && vertical == 0))
                return false;
            var direction = CharacterDirectionForArrowInput(horizontal, vertical);
            var character = _session.Data.Props[SelectedPropIndex];
            character.MovementX = direction.x;
            character.MovementZ = direction.y;
            character.AnimationState = "walk";
            ApplyCharacterAnimation(SelectedPropIndex, "walk");
            ScheduleManualCharacterOverride(character, float.PositiveInfinity);
            return true;
        }

        public static Vector2 CharacterDirectionForArrowInput(
            int horizontal, int vertical)
        {
            if (horizontal == 0 && vertical == 0) return Vector2.zero;
            // Keep the established lot-aligned 110° east family, with a small
            // Final calibrated compass: exact cardinal and diagonal headings.
            // The remaining directions stay at exact 45° intervals.
            var heading = vertical > 0
                ? horizontal > 0 ? 45f : horizontal < 0 ? 315f : 0f
                : vertical < 0
                    ? horizontal > 0 ? 135f : horizontal < 0 ? 225f : 180f
                    : horizontal > 0 ? 90f : 270f;
            var radians = heading * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)).normalized;
        }

        public static Vector2 SnapCharacterDirectionToLotAxes(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return Vector2.zero;
            const float lotAxisHeadingDegrees = 20f;
            const float headingStepDegrees = 45f;
            var heading = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            var snappedHeading = lotAxisHeadingDegrees + Mathf.Round(
                (heading - lotAxisHeadingDegrees) / headingStepDegrees) *
                headingStepDegrees;
            var radians = snappedHeading * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)).normalized;
        }

        public bool StopSelectedCharacter()
        {
            if (!SelectedPropIsThreeDimensionalCharacter) return false;
            var character = _session.Data.Props[SelectedPropIndex];
            character.MovementX = 0f;
            character.MovementZ = 0f;
            if (TrySeatCharacterAtNearestBench(character, out var benchTurns))
            {
                character.AnimationState = "sit";
                if (SelectedPropIndex < _propPresentations.Count &&
                    _propPresentations[SelectedPropIndex] != null)
                {
                    var presentation = _propPresentations[SelectedPropIndex];
                    presentation.localPosition = new Vector3(character.PositionX,
                        0.055f, character.PositionZ);
                    presentation.localRotation = Quaternion.Euler(
                        0f, benchTurns * 90f, 0f);
                    ApplyPropSelection();
                }
                ApplyCharacterAnimation(SelectedPropIndex, "sit");
                _characterBusinessAsUsualUntil.Remove(character.InstanceId ?? "");
                return true;
            }
            character.AnimationState = "idle";
            ApplyCharacterAnimation(SelectedPropIndex, "idle");
            ScheduleManualCharacterOverride(character, 3f);
            ScheduleBusinessAsUsual(character, 3f);
            return true;
        }

        public bool SetSelectedCharacterBehaviorScript(string scriptId)
        {
            if (!SelectedPropIsThreeDimensionalCharacter) return false;
            var character = _session.Data.Props[SelectedPropIndex];
            scriptId = CharacterBehaviorScript.Normalize(scriptId);
            if (!CharacterBehaviorScript.IsAvailableFor(character.PropId, scriptId))
                return false;
            character.BehaviorScript = scriptId;
            character.MovementX = 0f;
            character.MovementZ = 0f;
            character.AnimationState = "idle";
            ApplyCharacterAnimation(SelectedPropIndex, "idle");
            _characterBusinessAsUsualUntil.Remove(character.InstanceId ?? "");
            _characterManualOverrideUntil.Remove(character.InstanceId ?? "");
            NotifyStateChanged();
            return true;
        }

        public bool SetSelectedCharacterScriptAsDefault()
        {
            if (!SelectedPropIsThreeDimensionalCharacter) return false;
            var character = _session.Data.Props[SelectedPropIndex];
            var script = CharacterBehaviorScript.Normalize(character.BehaviorScript);
            if (IsHooligan(character.PropId))
                _session.Data.DefaultHooliganBehaviorScript = script;
            else if (IsHistoricPoliceman(character.PropId))
                _session.Data.DefaultPolicemanBehaviorScript = script;
            else
                _session.Data.DefaultGentlemanBehaviorScript = script;
            NotifyStateChanged();
            return true;
        }

        private string DefaultCharacterBehaviorScript(string propId)
        {
            var script = IsHooligan(propId)
                ? _session.Data.DefaultHooliganBehaviorScript
                : IsHistoricPoliceman(propId)
                    ? _session.Data.DefaultPolicemanBehaviorScript
                    : _session.Data.DefaultGentlemanBehaviorScript;
            script = CharacterBehaviorScript.Normalize(script);
            return CharacterBehaviorScript.IsAvailableFor(propId, script)
                ? script
                : CharacterBehaviorScript.BusinessAsUsual;
        }

        private bool TrySeatCharacterAtNearestBench(PlacedProp character,
            out int benchTurns)
        {
            benchTurns = 0;
            if (_session.Data.Props == null) return false;
            PlacedProp nearest = null;
            var nearestSquaredDistance = 1.35f * 1.35f;
            foreach (var prop in _session.Data.Props)
            {
                if (!string.Equals(prop.PropId, OrnateBenchPropId,
                        StringComparison.OrdinalIgnoreCase)) continue;
                var squaredDistance = new Vector2(
                    character.PositionX - prop.PositionX,
                    character.PositionZ - prop.PositionZ).sqrMagnitude;
                if (squaredDistance > nearestSquaredDistance) continue;
                nearestSquaredDistance = squaredDistance;
                nearest = prop;
            }
            if (nearest == null) return false;
            benchTurns = nearest.RotationQuarterTurns;
            var benchRotation = Quaternion.Euler(0f, benchTurns * 90f, 0f);
            var seat = new Vector3(nearest.PositionX, 0f, nearest.PositionZ) +
                benchRotation * new Vector3(0f, 0f, -0.08f);
            character.PositionX = seat.x;
            character.PositionZ = seat.z;
            return true;
        }

        private void UpdateThreeDimensionalCharacters()
        {
            if (_session.Data.Props == null) return;
            var visible = ShowsThreeDimensionalCharacters(ZoomLevel);
            for (var index = 0; index < _session.Data.Props.Count &&
                 index < _propPresentations.Count; index++)
            {
                var character = _session.Data.Props[index];
                if (!IsThreeDimensionalCharacter(character.PropId)) continue;
                var presentation = _propPresentations[index];
                if (presentation == null) continue;
                presentation.gameObject.SetActive(visible);
                if (!visible) continue;
                presentation.GetComponent<CharacterGroundShadow>()?.SetLighting(
                    TimeOfDay, !IsRaining && TimeOfDay != TimeOfDayPreset.Night);
                UpdateCharacterBusinessAsUsual(index, character);
                if (new Vector2(character.MovementX,
                        character.MovementZ).sqrMagnitude < 0.01f) continue;
                var direction = new Vector3(character.MovementX, 0f,
                    character.MovementZ).normalized;
                var movementSpeed = string.Equals(character.AnimationState, "run",
                    StringComparison.OrdinalIgnoreCase) ? 2.6f : 1.35f;
                var proposed = new Vector2(
                    character.PositionX + direction.x * movementSpeed * Time.deltaTime,
                    character.PositionZ + direction.z * movementSpeed * Time.deltaTime);
                var target = ClampCharacterPosition(proposed);
                character.PositionX = target.x;
                character.PositionZ = target.y;
                presentation.localPosition = new Vector3(target.x, 0.055f, target.y);
                presentation.localRotation = Quaternion.LookRotation(direction, Vector3.up);
                if ((target - proposed).sqrMagnitude > 0.000001f)
                    CompleteCharacterWalkAtBoundary(index, character);
                if (index == SelectedPropIndex) ApplyPropSelection();
            }
        }

        private void UpdateCharacterBusinessAsUsual(int index,
            PlacedProp character)
        {
            if (character == null || string.Equals(character.AnimationState,
                    "sit", StringComparison.OrdinalIgnoreCase)) return;
            var key = string.IsNullOrWhiteSpace(character.InstanceId)
                ? $"character-{index}"
                : character.InstanceId;
            if (_characterManualOverrideUntil.TryGetValue(key, out var manualUntil))
            {
                if (Time.time < manualUntil) return;
                _characterManualOverrideUntil.Remove(key);
            }
            if (UpdateAuthoredCharacterScript(index, character)) return;
            if (_characterBusinessAsUsualUntil.TryGetValue(key, out var until) &&
                Time.time < until) return;

            var action = BusinessAsUsualCharacterScript.SelectForCharacter(
                character.PropId, UnityEngine.Random.value);
            var state = BusinessAsUsualCharacterScript.AnimationState(action);
            if (action == BusinessAsUsualAction.Walk)
            {
                var direction = FeasibleCharacterWalkingDirection(character,
                    UnityEngine.Random.value);
                character.MovementX = direction.x;
                character.MovementZ = direction.y;
            }
            else
            {
                character.MovementX = 0f;
                character.MovementZ = 0f;
            }
            character.AnimationState = state;
            ApplyCharacterAnimation(index, state);
            _characterBusinessAsUsualUntil[key] = action ==
                BusinessAsUsualAction.Walk
                    ? float.PositiveInfinity
                    : Time.time + BusinessAsUsualCharacterScript.Duration(action,
                        UnityEngine.Random.value);
        }

        private bool UpdateAuthoredCharacterScript(int index,
            PlacedProp character)
        {
            var script = CharacterBehaviorScript.Normalize(
                character.BehaviorScript);
            if (script == CharacterBehaviorScript.BusinessAsUsual) return false;
            if (script == CharacterBehaviorScript.EvadePolice)
            {
                var officer = NearestCharacter(character,
                    HistoricPolicemanCharacterId, out var distance);
                if (officer == null || distance > 10f) return false;
                var away = new Vector2(character.PositionX - officer.PositionX,
                    character.PositionZ - officer.PositionZ);
                if (away.sqrMagnitude < 0.01f)
                    away = BusinessAsUsualCharacterScript.WalkingDirection(
                        UnityEngine.Random.value);
                SetCharacterMotion(index, character, away.normalized, "run");
                return true;
            }
            if (script == CharacterBehaviorScript.HarassPedestrian)
            {
                var pedestrian = NearestCharacter(character,
                    VictorianGentlemanCharacterId, out var distance);
                if (pedestrian == null) return false;
                var toward = new Vector2(pedestrian.PositionX - character.PositionX,
                    pedestrian.PositionZ - character.PositionZ);
                if (distance > 1.25f)
                    SetCharacterMotion(index, character, toward.normalized, "walk");
                else
                    SetCharacterMotion(index, character, Vector2.zero, "idle");
                return true;
            }
            return false;
        }

        private PlacedProp NearestCharacter(PlacedProp source, string propId,
            out float distance)
        {
            PlacedProp nearest = null;
            var best = float.PositiveInfinity;
            foreach (var candidate in _session.Data.Props ?? new List<PlacedProp>())
            {
                if (candidate == source || !string.Equals(candidate.PropId, propId,
                        StringComparison.OrdinalIgnoreCase)) continue;
                var sqr = new Vector2(candidate.PositionX - source.PositionX,
                    candidate.PositionZ - source.PositionZ).sqrMagnitude;
                if (sqr >= best) continue;
                best = sqr;
                nearest = candidate;
            }
            distance = nearest == null ? float.PositiveInfinity : Mathf.Sqrt(best);
            return nearest;
        }

        private void SetCharacterMotion(int index, PlacedProp character,
            Vector2 direction, string state)
        {
            character.MovementX = direction.x;
            character.MovementZ = direction.y;
            if (string.Equals(character.AnimationState, state,
                    StringComparison.OrdinalIgnoreCase)) return;
            character.AnimationState = state;
            ApplyCharacterAnimation(index, state);
        }

        private Vector2 FeasibleCharacterWalkingDirection(PlacedProp character,
            float roll)
        {
            var direction = BusinessAsUsualCharacterScript.WalkingDirection(roll);
            var position = new Vector2(character.PositionX, character.PositionZ);
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var probe = ClampCharacterPosition(position + direction * 0.5f);
                if ((probe - position).sqrMagnitude > 0.04f) return direction;
                direction = new Vector2(direction.y, -direction.x);
            }
            return Vector2.zero;
        }

        private void CompleteCharacterWalkAtBoundary(int index,
            PlacedProp character)
        {
            character.MovementX = 0f;
            character.MovementZ = 0f;
            character.AnimationState = "idle";
            ApplyCharacterAnimation(index, "idle");
            var key = string.IsNullOrWhiteSpace(character.InstanceId)
                ? $"character-{index}"
                : character.InstanceId;
            _characterManualOverrideUntil.Remove(key);
            // Briefly take in the destination before choosing the next task.
            _characterBusinessAsUsualUntil[key] = Time.time +
                UnityEngine.Random.Range(1.5f, 3.5f);
        }

        private void ScheduleBusinessAsUsual(PlacedProp character,
            float delaySeconds)
        {
            if (character == null) return;
            var key = character.InstanceId ?? "";
            if (string.IsNullOrWhiteSpace(key)) return;
            _characterBusinessAsUsualUntil[key] = Time.time + delaySeconds;
        }

        private void ScheduleManualCharacterOverride(PlacedProp character,
            float delaySeconds)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.InstanceId))
                return;
            _characterManualOverrideUntil[character.InstanceId] =
                float.IsPositiveInfinity(delaySeconds)
                    ? float.PositiveInfinity
                    : Time.time + delaySeconds;
        }

        private Vector2 ClampCharacterPosition(Vector2 position) => new(
            Mathf.Clamp(position.x, -LotWidthMeters * 0.5f + 0.3f,
                LotWidthMeters * 0.5f - 0.3f),
            Mathf.Clamp(position.y, -LotDepthMeters * 0.5f + 0.3f,
                LotDepthMeters * 0.5f - 0.3f));

        private void ApplyCharacterZoomVisibility()
        {
            var visible = ShowsThreeDimensionalCharacters(ZoomLevel);
            for (var index = 0; index < _propPresentations.Count &&
                 index < (_session.Data.Props?.Count ?? 0); index++)
                if (IsThreeDimensionalCharacter(_session.Data.Props[index].PropId) &&
                    _propPresentations[index] != null)
                    _propPresentations[index].gameObject.SetActive(visible);
        }

        public bool DragPropFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!_propDragActive || SelectedPropIndex < 0 ||
                SelectedPropIndex >= (_session.Data.Props?.Count ?? 0) ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            var prop = _session.Data.Props[SelectedPropIndex];
            var target = ClampPropPosition(new Vector2(
                point.x + _propDragOffset.x, point.z + _propDragOffset.y),
                prop.RotationQuarterTurns);
            if (!CanPlacePropAt(target, prop.RotationQuarterTurns, SelectedPropIndex))
                return false;
            prop.PositionX = target.x;
            prop.PositionZ = target.y;
            if (SelectedPropIndex < _propPresentations.Count)
                _propPresentations[SelectedPropIndex].localPosition =
                    new Vector3(target.x, 0.055f, target.y);
            UpdatePropProjectedShadows();
            UpdatePropWetStreetReflections();
            ApplyPropSelection();
            return true;
        }

        public bool EndPropDrag()
        {
            if (!_propDragActive) return false;
            _propDragActive = false;
            // Dragging updates the transform continuously, but recovery shader
            // classification depends on the committed world position. Without
            // this refresh a prop retained its old material until clicked again,
            // which could leave a lamppost lantern visually inside-out.
            RebuildPropPresentations();
            NotifyStateChanged();
            return true;
        }

        public bool RotateSelectedProp(int direction)
        {
            if (SelectedPropIndex < 0 || SelectedPropIndex >= PropCount) return false;
            var prop = _session.Data.Props[SelectedPropIndex];
            var turns = ((prop.RotationQuarterTurns + direction) % 4 + 4) % 4;
            var position = ClampPropPosition(new Vector2(prop.PositionX, prop.PositionZ), turns);
            if (!CanPlacePropAt(position, turns, SelectedPropIndex)) return false;
            prop.RotationQuarterTurns = turns;
            prop.PositionX = position.x;
            prop.PositionZ = position.y;
            RebuildPropPresentations();
            NotifyStateChanged();
            return true;
        }

        public bool DeleteSelectedProp()
        {
            if (SelectedPropIndex < 0 || SelectedPropIndex >= PropCount) return false;
            _session.Data.Props.RemoveAt(SelectedPropIndex);
            SelectedPropIndex = -1;
            _propDragActive = false;
            ActiveObjectSelection = LotObjectSelectionKind.None;
            RebuildPropPresentations();
            NotifyStateChanged();
            return true;
        }

        public void ClearPropSelectionAndPreview()
        {
            SelectedPropIndex = -1;
            _propDragActive = false;
            _propPreviewId = "";
            _propPreviewHasPoint = false;
            if (_propPreview != null) _propPreview.gameObject.SetActive(false);
            ApplyPropSelection();
        }

        private void RebuildPropPresentations()
        {
            if (_propRoot == null) return;
            for (var index = _propRoot.childCount - 1; index >= 0; index--)
            {
                var child = _propRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            _propPresentations.Clear();
            _propProjectedShadowRenderers.Clear();
            _propProjectedShadowMaterials.Clear();
            foreach (var prop in _session.Data.Props ?? new List<PlacedProp>())
            {
                var presentation = CreatePropPresentation(prop.PropId,
                    $"Prop — {prop.PropId}", 1f);
                if (presentation == null) continue;
                presentation.SetParent(_propRoot, false);
                presentation.localRotation = Quaternion.Euler(
                    0f, prop.RotationQuarterTurns * 90f, 0f);
                presentation.localPosition = new Vector3(
                    prop.PositionX, 0.055f, prop.PositionZ);
                ApplyPropBuildingFrontRecovery(presentation, prop);
                _propPresentations.Add(presentation);
                // Selection changes rebuild every prop presentation. Restore a
                // character's persisted state so a seated gentleman does not
                // revert to the FBX bind/idle state (or vanish below the seat)
                // merely because the user selected something else.
                if (IsThreeDimensionalCharacter(prop.PropId))
                    ApplyCharacterAnimation(_propPresentations.Count - 1,
                        string.IsNullOrWhiteSpace(prop.AnimationState)
                            ? "idle"
                            : prop.AnimationState);
            }
            UpdatePropProjectedShadows();
            UpdatePropWetStreetReflections();
            ApplyPropSelection();
            ApplyCharacterZoomVisibility();
        }

        private void ApplyPropBuildingFrontRecovery(
            Transform presentation, PlacedProp prop)
        {
            if (presentation == null || prop == null) return;
            var hostStencilReferences = new List<int>(4);
            if (!TryResolveVisibleBuildingFrontHosts(
                    new Vector3(prop.PositionX, 0f, prop.PositionZ),
                    hostStencilReferences))
                return;

            EnsurePropFrontRecoveryCamera();

            foreach (var renderer in presentation.GetComponentsInChildren<Renderer>(true))
            {
                if (HasNamedPropRenderAncestor(renderer.transform, presentation,
                        "Depth Prepass") ||
                    HasNamedPropRenderAncestor(renderer.transform, presentation,
                        "Projected Prop") ||
                    HasNamedPropRenderAncestor(renderer.transform, presentation,
                        "Wet Street Reflection"))
                    continue;

                renderer.gameObject.layer = PropFrontRecoveryLayer;
            }
        }

        private void EnsurePropFrontRecoveryCamera()
        {
            if (_camera == null) return;
            _camera.cullingMask &= ~(1 << PropFrontRecoveryLayer);
            if (_propFrontRecoveryCamera != null) return;

            var cameraObject = new GameObject("CF Prop Front Recovery Camera");
            cameraObject.transform.SetParent(_camera.transform, false);
            _propFrontRecoveryCamera = cameraObject.AddComponent<Camera>();
            _propFrontRecoveryCamera.clearFlags = CameraClearFlags.Depth;
            _propFrontRecoveryCamera.cullingMask = 1 << PropFrontRecoveryLayer;
            _propFrontRecoveryCamera.depth = _camera.depth + 50f;
            cameraObject.AddComponent<PropFrontRecoveryCameraSync>()
                .Initialize(_camera, _propFrontRecoveryCamera);
        }

        private static bool HasNamedPropRenderAncestor(
            Transform candidate, Transform root, string fragment)
        {
            for (var current = candidate;
                 current != null && current != root;
                 current = current.parent)
                if (current.name.Contains(fragment))
                    return true;
            return false;
        }

        private void UpdatePropProjectedShadows()
        {
            var ray = TimeOfDayLighting.SunRotation(TimeOfDay) * Vector3.forward;
            if (_buildingPackage != null)
                ray = Quaternion.Euler(0f,
                    _buildingPackage.ShadowDirectionOffsetDegrees, 0f) * ray;
            var visible = !IsRaining && TimeOfDay != TimeOfDayPreset.Night &&
                ray.y < -0.01f;
            var rawDisplacement = new Vector2(ray.x, ray.z) *
                (-1.5f / ray.y) * PropShadowLengthScale(TimeOfDay);
            var displacement = Vector2.ClampMagnitude(rawDisplacement, 8.5f);
            var color = ProjectedShadowColor(TimeOfDay);
            color.a *= PropShadowOpacityMultiplier(TimeOfDay);
            for (var index = 0; index < _propProjectedShadowRenderers.Count; index++)
            {
                var renderer = _propProjectedShadowRenderers[index];
                if (renderer != null) renderer.enabled = visible;
                if (index >= _propProjectedShadowMaterials.Count) continue;
                var material = _propProjectedShadowMaterials[index];
                if (material == null) continue;
                material.SetVector("_ShadowDisplacement",
                    new Vector4(displacement.x, 0f, displacement.y, 0f));
                material.SetFloat("_GroundY", 0.062f);
                material.SetFloat("_ReferenceHeight", 1.5f);
                material.color = color;
            }
        }

        public static float PropShadowLengthScale(TimeOfDayPreset preset) =>
            preset == TimeOfDayPreset.Afternoon ? 1f : ShadowLengthScale(preset);

        public static float PropShadowOpacityMultiplier(TimeOfDayPreset preset) =>
            preset == TimeOfDayPreset.Afternoon ? 1.25f : 0.78f;

        private Transform CreatePropPresentation(string propId, string name, float alpha)
        {
            var resourcePath = string.Equals(propId, FencePropId,
                    StringComparison.OrdinalIgnoreCase)
                ? FenceResourcePath
                : string.Equals(propId, FenceCornerPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? FenceCornerResourcePath
                    : string.Equals(propId, PicketFencePropId,
                        StringComparison.OrdinalIgnoreCase)
                        ? PicketFenceResourcePath
                    : string.Equals(propId, DecorativeIronGardenPropId,
                        StringComparison.OrdinalIgnoreCase)
                        ? DecorativeIronGardenResourcePath
                    : string.Equals(propId, OrnateIronCornerPropId,
                        StringComparison.OrdinalIgnoreCase)
                        ? OrnateIronCornerResourcePath
                    : string.Equals(propId, ThreeLanternLamppostPropId,
                        StringComparison.OrdinalIgnoreCase)
                        ? ThreeLanternLamppostResourcePath
                    : string.Equals(propId, SimpleStreetLamppostPropId,
                        StringComparison.OrdinalIgnoreCase)
                        ? SimpleStreetLamppostResourcePath
                        : string.Equals(propId, OrnateBenchPropId,
                            StringComparison.OrdinalIgnoreCase)
                            ? OrnateBenchResourcePath
                        : IsThreeDimensionalCharacter(propId)
                            ? CharacterResourcePath(propId)
                            : "";
            if (string.IsNullOrWhiteSpace(resourcePath))
                return null;
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return null;
            var root = new GameObject(name).transform;
            var model = Instantiate(prefab, root, false);
            model.name = IsThreeDimensionalCharacter(propId)
                ? IsHooligan(propId)
                    ? "Hooligan Animated Model"
                    : IsHistoricPoliceman(propId)
                        ? "Historic Policeman Animated Model"
                        : "Victorian Gentleman Animated Model"
                : string.Equals(propId, OrnateBenchPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? "Ornate Bench Model"
                : string.Equals(propId, ThreeLanternLamppostPropId,
                StringComparison.OrdinalIgnoreCase)
                ? "Three-Lantern Lamppost Model"
                : string.Equals(propId, SimpleStreetLamppostPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? "Simple Street Lamppost Model"
                : string.Equals(propId, PicketFencePropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? "White Picket Fence Model"
                : string.Equals(propId, DecorativeIronGardenPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? "Decorative Iron Garden and Lamp Model"
                : string.Equals(propId, OrnateIronCornerPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? "Ornate Iron Corner Fence Model"
                : "Wrought-Iron Fence Model";
            model.transform.localScale = IsThreeDimensionalCharacter(propId)
                ? Vector3.one
                : string.Equals(propId, PicketFencePropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? Vector3.one
                : string.Equals(propId, SimpleStreetLamppostPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? Vector3.one
                : string.Equals(propId, DecorativeIronGardenPropId,
                    StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(propId, OrnateIronCornerPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? Vector3.one
                : Vector3.one *
                (string.Equals(propId, ThreeLanternLamppostPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? ThreeLanternLamppostScale
                    : string.Equals(propId, FenceCornerPropId,
                    StringComparison.OrdinalIgnoreCase)
                    ? FenceCornerScale
                    : FenceScale);
            if (IsThreeDimensionalCharacter(propId))
            {
                NormalizeCharacterToHumanScale(model.transform);
                ConfigureCharacterAnimation(root, model, propId, "idle");
                root.gameObject.AddComponent<CharacterGroundShadow>().Initialize();
            }
            else if (string.Equals(propId, OrnateBenchPropId,
                         StringComparison.OrdinalIgnoreCase))
                NormalizeStaticPropToLength(model.transform,
                    OrnateBenchLengthMeters);
            else if (string.Equals(propId, PicketFencePropId,
                         StringComparison.OrdinalIgnoreCase))
                NormalizeStaticPropToLength(model.transform,
                    PicketFenceLengthMeters);
            else if (string.Equals(propId, SimpleStreetLamppostPropId,
                         StringComparison.OrdinalIgnoreCase))
                NormalizeStaticPropToHeight(model.transform,
                    SimpleStreetLamppostHeightMeters);
            else if (string.Equals(propId, DecorativeIronGardenPropId,
                         StringComparison.OrdinalIgnoreCase))
                NormalizeStaticPropToHeight(model.transform, 1.6f);
            else if (string.Equals(propId, OrnateIronCornerPropId,
                         StringComparison.OrdinalIgnoreCase))
                NormalizeStaticPropToHeight(model.transform, 2.2f);
            SetPropOpacity(root, propId, alpha, true);
            if (alpha >= 0.999f && !IsThreeDimensionalCharacter(propId))
            {
                CreatePropDepthPrepass(prefab, root, model.transform.localScale,
                    model.transform.localPosition);
                CreateProjectedPropShadow(prefab, root, model.transform.localScale,
                    model.transform.localPosition);
                CreatePropWetStreetReflection(prefab, root, model.transform.localScale,
                    model.transform.localPosition);
                if (string.Equals(propId, ThreeLanternLamppostPropId,
                        StringComparison.OrdinalIgnoreCase))
                    CreateThreeLanternLights(root);
                else if (string.Equals(propId, SimpleStreetLamppostPropId,
                             StringComparison.OrdinalIgnoreCase))
                    CreateSimpleStreetLamppostLight(root);
                else if (string.Equals(propId, DecorativeIronGardenPropId,
                             StringComparison.OrdinalIgnoreCase))
                    CreateDecorativeIronGardenLight(root);
            }
            foreach (var collider in root.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            return root;
        }

        private static void NormalizeCharacterToHumanScale(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            if (bounds.size.y <= 0.001f) return;
            var scale = 1.78f / bounds.size.y;
            model.localScale *= scale;
        }

        private static void NormalizeStaticPropToLength(Transform model,
            float targetLengthMeters)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            var sourceLength = Mathf.Max(bounds.size.x, bounds.size.z);
            if (sourceLength <= 0.001f) return;
            model.localScale *= targetLengthMeters / sourceLength;
            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            model.position += Vector3.up * -bounds.min.y;
        }

        private static void NormalizeStaticPropToHeight(Transform model,
            float targetHeightMeters)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            if (bounds.size.y <= 0.001f) return;
            model.localScale *= targetHeightMeters / bounds.size.y;
            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            model.position += Vector3.up * -bounds.min.y;
        }

        private static void ConfigureCharacterAnimation(Transform root,
            GameObject model, string propId, string initialState)
        {
            var animator = model.GetComponentInChildren<Animator>() ??
                model.AddComponent<Animator>();
            animator.enabled = true;
            // Lot movement is authoritative; embedded take translation must
            // not make the mesh drift away from its saved prop position.
            animator.applyRootMotion = false;
            var player = root.gameObject.AddComponent<
                ThreeDimensionalCharacterAnimator>();
            player.Initialize(animator, Resources.LoadAll<AnimationClip>(
                CharacterResourcePath(propId)));
            player.Play(initialState);
        }

        private void ApplyCharacterAnimation(int index, string state)
        {
            if (index < 0 || index >= _propPresentations.Count) return;
            _propPresentations[index]
                ?.GetComponent<ThreeDimensionalCharacterAnimator>()
                ?.Play(state);
        }

        private static void CreatePropDepthPrepass(GameObject prefab,
            Transform root, Vector3 modelScale, Vector3 modelLocalPosition)
        {
            var shader = Shader.Find("CityForgeV3/BuildingDepthOccluder");
            if (shader == null) throw new MissingReferenceException(
                "CityForge V3 depth-only shader is required for committed props.");
            var depthModel = Instantiate(prefab, root, false);
            depthModel.name = "Committed Prop Depth Prepass";
            depthModel.transform.localScale = modelScale;
            depthModel.transform.localPosition = modelLocalPosition;
            foreach (var collider in depthModel.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            foreach (var renderer in depthModel.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = new Material(shader)
                {
                    name = "CF Committed Prop Depth Prepass",
                    renderQueue = 2435
                };
                renderer.sharedMaterial.SetFloat(
                    "_BuildingHostStencilWriteMask", 0f);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void CreateProjectedPropShadow(GameObject prefab, Transform root,
            Vector3 modelScale, Vector3 modelLocalPosition)
        {
            var shader = Shader.Find("CityForgeV3/ProjectedPropShadow");
            if (shader == null) throw new MissingReferenceException(
                "CityForge V3 projected prop shadow shader is required.");
            var shadowModel = Instantiate(prefab, root, false);
            shadowModel.name = "Projected Prop Silhouette";
            shadowModel.transform.localScale = modelScale;
            shadowModel.transform.localPosition = modelLocalPosition;
            foreach (var collider in shadowModel.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            foreach (var renderer in shadowModel.GetComponentsInChildren<Renderer>())
            {
                var material = new Material(shader)
                {
                    name = "CF Projected Prop Silhouette",
                    renderQueue = 2004
                };
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _propProjectedShadowRenderers.Add(renderer);
                _propProjectedShadowMaterials.Add(material);
            }
        }

        private static void CreatePropWetStreetReflection(GameObject prefab,
            Transform root, Vector3 modelScale, Vector3 modelLocalPosition)
        {
            var shader = Shader.Find("CityForgeV3/WetStreetPropReflection");
            if (shader == null) throw new MissingReferenceException(
                "City Forge V3 wet-street prop reflection shader is required.");
            var reflection = Instantiate(prefab, root, false);
            reflection.name = "Wet Street Prop Reflection";
            reflection.transform.localScale = modelScale;
            reflection.transform.localPosition = modelLocalPosition;
            foreach (var collider in reflection.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            foreach (var renderer in reflection.GetComponentsInChildren<Renderer>())
            {
                var sourceMaterials = renderer.sharedMaterials;
                var reflectionMaterials = new Material[sourceMaterials.Length];
                for (var index = 0; index < sourceMaterials.Length; index++)
                {
                    var source = sourceMaterials[index];
                    var material = new Material(shader)
                    {
                        name = "CF Wet Street Prop Reflection",
                        renderQueue = 2460
                    };
                    if (source != null && source.mainTexture != null)
                        material.mainTexture = source.mainTexture;
                    if (source != null && source.HasProperty("_Color"))
                        material.SetColor("_SourceTint", source.color);
                    material.SetFloat("_Wetness", 0f);
                    reflectionMaterials[index] = material;
                }
                renderer.sharedMaterials = reflectionMaterials;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void UpdatePropWetStreetReflections()
        {
            var direction = _camera != null
                ? Vector3.ProjectOnPlane(-_camera.transform.forward,
                    Vector3.up).normalized
                : Vector3.back;
            foreach (var presentation in _propPresentations)
            {
                if (presentation == null) continue;
                foreach (var renderer in presentation.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || renderer.sharedMaterial == null ||
                        renderer.sharedMaterial.name.IndexOf(
                            "Wet Street Prop Reflection", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null) continue;
                        material.SetFloat("_Wetness", RoadWetness);
                        material.SetFloat("_RainActive", IsRaining ? 1f : 0f);
                        material.SetVector("_ReflectionDirection",
                            new Vector4(direction.x, 0f, direction.z, 0f));
                    }
                    renderer.enabled = RoadWetness > 0.001f;
                }
            }
        }

        private void SetPropOpacity(Transform root, string propId,
            float alpha, bool valid)
        {
            if (root == null) return;
            var simpleLamppost = string.Equals(propId, SimpleStreetLamppostPropId,
                StringComparison.OrdinalIgnoreCase);
            var lamppost = simpleLamppost || string.Equals(propId,
                ThreeLanternLamppostPropId, StringComparison.OrdinalIgnoreCase);
            var character = IsThreeDimensionalCharacter(propId);
            var bench = string.Equals(propId, OrnateBenchPropId,
                StringComparison.OrdinalIgnoreCase);
            var corner = string.Equals(propId, FenceCornerPropId,
                StringComparison.OrdinalIgnoreCase);
            var picket = string.Equals(propId, PicketFencePropId,
                StringComparison.OrdinalIgnoreCase);
            var decorativeGarden = string.Equals(propId,
                DecorativeIronGardenPropId, StringComparison.OrdinalIgnoreCase);
            var ornateCorner = string.Equals(propId, OrnateIronCornerPropId,
                StringComparison.OrdinalIgnoreCase);
            var texturePrefix = corner ? "corner-" : "";
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                // Meshy FBXs refer to model.fbm aliases that are not present in
                // the supplied archive. Always bind the stable City Forge
                // texture contract instead of inheriting that white fallback.
                var material = new Material(Shader.Find("Standard"))
                {
                    name = lamppost
                        ? simpleLamppost
                            ? "CF Simple Street Lamppost Runtime Material"
                            : "CF Three-Lantern Lamppost Runtime Material"
                        : bench
                            ? "CF Ornate Bench Runtime Material"
                        : "CF Wrought-Iron Fence Runtime Material"
                };
                var baseColor = Resources.Load<Texture2D>(
                    character
                        ? IsHooligan(propId)
                            ? "CityForgeV3/Props/Characters/HooliganV01/Textures/base-color-brown"
                            : IsHistoricPoliceman(propId)
                                ? "CityForgeV3/Props/Characters/HistoricPolicemanV01/Textures/base-color"
                            : "CityForgeV3/Props/Characters/VictorianGentlemanV01/Textures/base-color-dark"
                        : picket
                        ? "CityForgeV3/Props/PicketFenceV01/base-color"
                        : bench
                        ? "CityForgeV3/Props/OrnateBenchV01/Textures/base-color"
                        : lamppost
                        ? simpleLamppost
                            ? "CityForgeV3/Props/SimpleStreetLamppostV01/Textures/base-color-dark"
                            : "CityForgeV3/Props/ThreeLanternLamppostV01/base-color"
                        : decorativeGarden
                        ? "CityForgeV3/Props/WroughtIronVariationsV01/decorative-base-color"
                        : ornateCorner
                        ? "CityForgeV3/Props/WroughtIronVariationsV01/gate-base-color"
                        : $"CityForgeV3/Props/WroughtIronFenceV01/{texturePrefix}base-color");
                if (baseColor != null) material.mainTexture = baseColor;
                var normal = Resources.Load<Texture2D>(
                    character
                        ? IsHooligan(propId)
                            ? "CityForgeV3/Props/Characters/HooliganV01/Textures/normal"
                            : IsHistoricPoliceman(propId)
                                ? "CityForgeV3/Props/Characters/HistoricPolicemanV01/Textures/normal"
                            : "CityForgeV3/Props/Characters/VictorianGentlemanV01/Textures/normal"
                        : picket
                        ? "CityForgeV3/Props/PicketFenceV01/normal"
                        : bench
                        ? "CityForgeV3/Props/OrnateBenchV01/Textures/normal"
                        : lamppost
                        ? simpleLamppost
                            ? "CityForgeV3/Props/SimpleStreetLamppostV01/Textures/normal"
                            : "CityForgeV3/Props/ThreeLanternLamppostV01/normal"
                        : decorativeGarden
                        ? "CityForgeV3/Props/WroughtIronVariationsV01/decorative-normal"
                        : ornateCorner
                        ? "CityForgeV3/Props/WroughtIronVariationsV01/gate-normal"
                        : $"CityForgeV3/Props/WroughtIronFenceV01/{texturePrefix}normal");
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.SetFloat("_BumpScale", 0.75f);
                    material.EnableKeyword("_NORMALMAP");
                }
                var metallicSmoothness = Resources.Load<Texture2D>(
                    character
                        ? IsHooligan(propId)
                            ? "CityForgeV3/Props/Characters/HooliganV01/Textures/metallic-smoothness"
                            : IsHistoricPoliceman(propId)
                                ? "CityForgeV3/Props/Characters/HistoricPolicemanV01/Textures/metallic-smoothness"
                            : "CityForgeV3/Props/Characters/VictorianGentlemanV01/Textures/metallic-smoothness"
                        : picket
                        ? "CityForgeV3/Props/PicketFenceV01/metallic-smoothness"
                        : bench
                        ? "CityForgeV3/Props/OrnateBenchV01/Textures/metallic"
                        : lamppost
                        ? simpleLamppost
                            ? "CityForgeV3/Props/SimpleStreetLamppostV01/Textures/metallic-smoothness"
                            : "CityForgeV3/Props/ThreeLanternLamppostV01/metallic-smoothness"
                        : decorativeGarden
                        ? "CityForgeV3/Props/WroughtIronVariationsV01/decorative-metallic-smoothness"
                        : ornateCorner
                        ? "CityForgeV3/Props/WroughtIronVariationsV01/gate-metallic-smoothness"
                        : $"CityForgeV3/Props/WroughtIronFenceV01/{texturePrefix}metallic-smoothness");
                if (metallicSmoothness != null)
                {
                    material.SetTexture("_MetallicGlossMap", metallicSmoothness);
                    material.EnableKeyword("_METALLICGLOSSMAP");
                }
                material.SetFloat("_Metallic", character ? 0.05f : picket ? 0f : bench ? 0.55f : 1f);
                // The supplied roughness maps are now inverted into the alpha
                // channel of the metallic/smoothness textures. Previously the
                // opaque alpha of a metallic JPG made every surface uniformly
                // glossy, lifting black cloth and iron toward ambient gray.
                material.SetFloat("_GlossMapScale", character
                    ? 0.72f
                    : simpleLamppost
                        ? 0.64f
                        : picket ? 0.18f : bench ? 0.42f : 0.72f);
                if (lamppost)
                {
                    var emission = Resources.Load<Texture2D>(
                        simpleLamppost
                            ? "CityForgeV3/Props/SimpleStreetLamppostV01/Textures/emission"
                            : "CityForgeV3/Props/ThreeLanternLamppostV01/emission");
                    if (emission != null)
                    {
                        material.SetTexture("_EmissionMap", emission);
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor",
                            IsLamppostLightingActive()
                                ? new Color(1.8f, 1.12f, 0.48f)
                                : Color.black);
                    }
                }
                // The authored base map carries the iron detail, while this
                // neutral tint keeps the simple lamp's painted metal black in
                // the comparatively bright lot-editor lighting.  Emission is
                // still driven independently by the lantern mask above.
                var color = valid
                    ? simpleLamppost
                        ? new Color(0.3f, 0.32f, 0.34f)
                        : Color.white
                    : new Color(1f, 0.2f, 0.15f);
                color.a = alpha;
                if (material.HasProperty("_Color")) material.color = color;
                if (alpha < 0.999f)
                {
                    material.SetFloat("_Mode", 3f);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                }
                else
                {
                    // Proxy depth and registered building art draw first.
                    // Committed 3D props then use their real mesh depth, so a
                    // front lamppost survives while a rear one remains hidden.
                    material.renderQueue = 2455;
                }
                renderer.sharedMaterial = material;
                // Committed fence meshes use the controlled projected shadow
                // above. Raw shadow-map projection turns thin pickets into
                // broken, extremely long streaks at low sun elevations.
                // Characters use a presentation-root-owned ground shadow.
                // Native skinned-mesh shadow bounds can retain animation/root
                // offsets and leave a detached humanoid-shaped "ghost" behind.
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = alpha >= 0.999f;
            }
        }

        private bool IsLamppostLightingActive() =>
            TimeOfDay == TimeOfDayPreset.Evening ||
            TimeOfDay == TimeOfDayPreset.Night;

        private void CreateThreeLanternLights(Transform root)
        {
            var active = IsLamppostLightingActive();
            foreach (var spec in new[]
                     {
                         (new Vector3(-0.78f, 3.55f, -0.18f), 2.25f),
                         (new Vector3(0f, 4.05f, -0.18f), 2.7f),
                         (new Vector3(0.78f, 3.55f, -0.18f), 2.25f)
                     })
            {
                var lamp = new GameObject("CF Runtime Lantern Light");
                lamp.transform.SetParent(root, false);
                lamp.transform.localPosition = spec.Item1;
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.62f, 0.28f);
                light.intensity = spec.Item2;
                light.range = 10.5f;
                light.shadows = LightShadows.Soft;
                light.enabled = active;
            }

        }

        private void CreateSimpleStreetLamppostLight(Transform root)
        {
            var active = IsLamppostLightingActive();
            var lamp = new GameObject("CF Runtime Lantern Light");
            lamp.transform.SetParent(root, false);
            lamp.transform.localPosition = new Vector3(0f, 3.15f, 0f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.28f);
            // This is a real local 3D light, not the old projected ground
            // pool.  Keep it deliberately small and shadowless so rows of
            // street lamps remain inexpensive while still lighting nearby
            // pavement, props, characters, and building surfaces.
            light.intensity = 1.75f;
            light.range = 5.5f;
            light.shadows = LightShadows.None;
            light.bounceIntensity = 0f;
            light.enabled = active;

        }

        private void CreateDecorativeIronGardenLight(Transform root)
        {
            var active = IsLamppostLightingActive();
            var lamp = new GameObject("CF Runtime Lantern Light");
            lamp.transform.SetParent(root, false);
            lamp.transform.localPosition = new Vector3(0f, 1.36f, 0f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.28f);
            light.intensity = 1.35f;
            light.range = 5.5f;
            light.shadows = LightShadows.Soft;
            light.enabled = active;
        }

        private void UpdateThreeLanternLamppostLighting()
        {
            var active = IsLamppostLightingActive();
            foreach (var presentation in _propPresentations)
            {
                if (presentation == null) continue;
                foreach (var light in presentation.GetComponentsInChildren<Light>(true))
                    if (light.name == "CF Runtime Lantern Light") light.enabled = active;
                foreach (var renderer in presentation.GetComponentsInChildren<Renderer>())
                {
                    var material = renderer.sharedMaterial;
                    if (material == null || material.name.IndexOf(
                            "Lamppost", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    material.SetColor("_EmissionColor", active
                        ? new Color(1.8f, 1.12f, 0.48f)
                        : Color.black);
                }
            }
        }

        private Vector2 ClampPropPosition(Vector2 position, int turns)
        {
            var propId = SelectedPropIndex >= 0 && SelectedPropIndex < PropCount
                ? _session.Data.Props[SelectedPropIndex].PropId
                : _propPreviewId;
            PropDimensions(propId, turns, out var width, out var depth);
            return new Vector2(
                Mathf.Clamp(position.x, -LotWidthMeters * 0.5f + width * 0.5f,
                    LotWidthMeters * 0.5f - width * 0.5f),
                Mathf.Clamp(position.y, -LotDepthMeters * 0.5f + depth * 0.5f,
                    LotDepthMeters * 0.5f - depth * 0.5f));
        }

        private bool CanPlacePropAt(Vector2 position, int turns, int ignoredIndex)
        {
            // Props are visual-authoring elements. Overlap with buildings or
            // other props is intentional and must not block fine positioning;
            // ClampPropPosition remains the sole lot-boundary constraint.
            return true;
        }

        private static void PropDimensions(string propId, int turns,
            out float width, out float depth)
        {
            if (string.Equals(propId, ThreeLanternLamppostPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                width = ThreeLanternLamppostFootprintMeters;
                depth = ThreeLanternLamppostFootprintMeters;
                return;
            }
            if (string.Equals(propId, SimpleStreetLamppostPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                width = SimpleStreetLamppostFootprintMeters;
                depth = SimpleStreetLamppostFootprintMeters;
                return;
            }
            if (IsThreeDimensionalCharacter(propId))
            {
                width = 0.65f;
                depth = 0.65f;
                return;
            }
            if (string.Equals(propId, OrnateBenchPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                var oddBench = Mathf.Abs(turns) % 2 == 1;
                width = oddBench ? OrnateBenchDepthMeters : OrnateBenchLengthMeters;
                depth = oddBench ? OrnateBenchLengthMeters : OrnateBenchDepthMeters;
                return;
            }
            if (string.Equals(propId, FenceCornerPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                width = FenceCornerLengthMeters;
                depth = FenceCornerLengthMeters;
                return;
            }
            if (string.Equals(propId, PicketFencePropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                var oddPicket = Mathf.Abs(turns) % 2 == 1;
                width = oddPicket ? PicketFenceDepthMeters : PicketFenceLengthMeters;
                depth = oddPicket ? PicketFenceLengthMeters : PicketFenceDepthMeters;
                return;
            }
            if (string.Equals(propId, DecorativeIronGardenPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                var oddGarden = Mathf.Abs(turns) % 2 == 1;
                width = oddGarden ? DecorativeIronGardenDepthMeters :
                    DecorativeIronGardenWidthMeters;
                depth = oddGarden ? DecorativeIronGardenWidthMeters :
                    DecorativeIronGardenDepthMeters;
                return;
            }
            if (string.Equals(propId, OrnateIronCornerPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                var oddCorner = Mathf.Abs(turns) % 2 == 1;
                width = oddCorner ? OrnateIronCornerDepthMeters :
                    OrnateIronCornerWidthMeters;
                depth = oddCorner ? OrnateIronCornerWidthMeters :
                    OrnateIronCornerDepthMeters;
                return;
            }
            var odd = Mathf.Abs(turns) % 2 == 1;
            width = odd ? FenceDepthMeters : FenceLengthMeters;
            depth = odd ? FenceLengthMeters : FenceDepthMeters;
        }

        private int PropIndexAtCameraPixel(Vector2 pixel)
        {
            var best = -1;
            var bestDepth = float.PositiveInfinity;
            for (var index = 0; index < _propPresentations.Count; index++)
            {
                var root = _propPresentations[index];
                if (root == null) continue;
                var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>();
                if (spriteRenderers.Length > 0)
                {
                    var opaqueSpriteHit = false;
                    foreach (var spriteRenderer in spriteRenderers)
                    {
                        if (!spriteRenderer.enabled ||
                            !SpriteRendererContainsCameraPixel(
                                spriteRenderer, _camera, pixel)) continue;
                        opaqueSpriteHit = true;
                        break;
                    }
                    if (!opaqueSpriteHit) continue;
                }
                if (!TrySelectablePropBounds(root, out var bounds)) continue;
                var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (var corner in BoundsCorners(bounds))
                {
                    var projected = (Vector2)_camera.WorldToScreenPoint(corner);
                    minimum = Vector2.Min(minimum, projected);
                    maximum = Vector2.Max(maximum, projected);
                }
                if (pixel.x < minimum.x || pixel.x > maximum.x ||
                    pixel.y < minimum.y || pixel.y > maximum.y) continue;
                var depth = _camera.WorldToScreenPoint(bounds.center).z;
                if (depth >= bestDepth) continue;
                bestDepth = depth;
                best = index;
            }
            return best;
        }

        public static bool TrySelectablePropBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            if (root == null) return false;
            var found = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer.name == "CF Runtime Lantern Light Pool" ||
                    HasAncestorNamed(renderer.transform, root,
                        "Projected Prop Silhouette") ||
                    HasAncestorNamed(renderer.transform, root,
                        "Wet Street Prop Reflection")) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static bool HasAncestorNamed(Transform candidate, Transform stop,
            string name)
        {
            for (var current = candidate; current != null && current != stop;
                 current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
        {
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
                yield return bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3(x, y, z));
        }

        private void ApplyPropSelection()
        {
            if (_propSelection == null) return;
            var visible = _propEditorActive &&
                ActiveObjectSelection == LotObjectSelectionKind.Prop &&
                SelectedPropIndex >= 0 &&
                SelectedPropIndex < PropCount;
            _propSelection.gameObject.SetActive(visible);
            if (!visible) return;
            var prop = _session.Data.Props[SelectedPropIndex];
            PropDimensions(prop.PropId, prop.RotationQuarterTurns,
                out var selectionWidth, out var selectionDepth);
            _propSelection.localPosition = new Vector3(prop.PositionX, 0.07f, prop.PositionZ);
            _propSelection.localRotation = Quaternion.Euler(
                0f, prop.RotationQuarterTurns * 90f, 0f);
            // The selection transform supplies rotation, so scale from the
            // unrotated footprint to avoid swapping dimensions twice.
            PropDimensions(prop.PropId, 0, out var baseWidth, out var baseDepth);
            _propSelection.localScale = new Vector3(
                baseWidth / FenceLengthMeters, 1f,
                baseDepth / FenceDepthMeters);
        }
    }

    internal sealed class PropFrontRecoveryCameraSync : MonoBehaviour
    {
        private Camera _source;
        private Camera _overlay;

        public void Initialize(Camera source, Camera overlay)
        {
            _source = source;
            _overlay = overlay;
            Synchronize();
        }

        private void OnPreCull() => Synchronize();

        private void Synchronize()
        {
            if (_source == null || _overlay == null) return;
            // Preserve the production camera's complete rendering contract
            // (HDR, render path, physical properties, target and projection),
            // then override only the overlay-specific depth and layer state.
            _overlay.CopyFrom(_source);
            _overlay.depth = _source.depth + 50f;
            _overlay.clearFlags = CameraClearFlags.Depth;
            _overlay.cullingMask = 1 << LotWorldController.PropFrontRecoveryLayer;
        }
    }
}
