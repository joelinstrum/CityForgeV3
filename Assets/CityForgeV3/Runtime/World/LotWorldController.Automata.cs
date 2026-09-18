using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private const int MaxAutomataGroupsPerLot = 8;
        private Transform _automataRoot;
        private readonly Dictionary<string, AutomataClipPlayer>
            _automataPresentations = new(StringComparer.Ordinal);
        private readonly Stack<List<PlacedAutomata>> _automataUndo = new();
        private readonly HashSet<string> _automataRetainedIds =
            new(StringComparer.Ordinal);
        private readonly List<string> _automataRemovedIds = new();
        private string _selectedAutomataId = "";
        private bool _automataDragActive;
        private bool _automataDragMoved;
        private bool _automataDragIsNew;
        private Vector2 _automataDragOffset;
        private Func<SeasonPreset> _automataSeasonProvider;

        public int AutomataCount => _session.Data.Automata?.Count ?? 0;
        public bool CanUndoAutomata => _automataUndo.Count > 0;
        public bool HasSelectedAutomata =>
            !string.IsNullOrEmpty(_selectedAutomataId) &&
            _automataPresentations.ContainsKey(_selectedAutomataId);
        public string SelectedAutomataName =>
            AutomataClipCatalog.Find(FindSelectedAutomata()?.AutomataId)
                ?.displayName ?? "None";
        public SeasonPreset AutomataSeason =>
            _automataSeasonProvider?.Invoke() ?? Season;
        public static SeasonPreset AutomataSeasonForDistrictIndex(int index)
            => (SeasonPreset)((Mathf.Max(0, index) + 1) % 4);
        public bool SelectedAutomataVisibleNow =>
            FindSelectedAutomata() is { } placement &&
            (TimeMask(placement) & (1 << (int)TimeOfDay)) != 0 &&
            (SeasonMask(placement) & (1 << (int)AutomataSeason)) != 0;

        public void BindAutomataSeasonProvider(Func<SeasonPreset> provider)
            => _automataSeasonProvider = provider;

        public bool SelectedAutomataTimeEnabled(TimeOfDayPreset preset) =>
            FindSelectedAutomata() is { } placement &&
            (TimeMask(placement) & (1 << (int)preset)) != 0;

        public bool SelectedAutomataSeasonEnabled(SeasonPreset preset) =>
            FindSelectedAutomata() is { } placement &&
            (SeasonMask(placement) & (1 << (int)preset)) != 0;

        public bool SelectNextAutomata()
        {
            var placements = _session.Data.Automata;
            if (placements == null || placements.Count == 0) return false;
            var current = -1;
            for (var index = 0; index < placements.Count; index++)
                if (placements[index]?.InstanceId == _selectedAutomataId)
                {
                    current = index;
                    break;
                }
            for (var step = 1; step <= placements.Count; step++)
            {
                var candidate = placements[(current + step) %
                    placements.Count];
                if (candidate == null ||
                    !_automataPresentations.ContainsKey(candidate.InstanceId))
                    continue;
                SelectAutomata(candidate.InstanceId);
                NotifyAutomataStateChanged();
                return true;
            }
            return false;
        }

        public bool SetSelectedAutomataTimeEnabled(TimeOfDayPreset preset,
            bool enabled) => SetSelectedAutomataScheduleBit(
                1 << (int)preset, enabled, true);

        public bool SetSelectedAutomataSeasonEnabled(SeasonPreset preset,
            bool enabled) => SetSelectedAutomataScheduleBit(
                1 << (int)preset, enabled, false);

        private bool SetSelectedAutomataScheduleBit(int bit, bool enabled,
            bool time)
        {
            var placement = FindSelectedAutomata();
            if (placement == null) return false;
            var oldMask = time ? TimeMask(placement) : SeasonMask(placement);
            var newMask = enabled ? oldMask | bit : oldMask & ~bit;
            if (newMask == oldMask) return false;
            PushAutomataUndo();
            placement.VisibleTimeMask = TimeMask(placement);
            placement.VisibleSeasonMask = SeasonMask(placement);
            placement.HasVisibilitySchedule = true;
            if (time) placement.VisibleTimeMask = newMask;
            else placement.VisibleSeasonMask = newMask;
            ApplyAutomataSchedule(placement);
            NotifyAutomataStateChanged();
            return true;
        }

        private static int TimeMask(PlacedAutomata placement) =>
            placement.HasVisibilitySchedule
                ? placement.VisibleTimeMask
                : AutomataClipCatalog.Find(placement.AutomataId)
                    ?.defaultTimeMask ?? 31;

        private static int SeasonMask(PlacedAutomata placement) =>
            placement.HasVisibilitySchedule
                ? placement.VisibleSeasonMask
                : AutomataClipCatalog.Find(placement.AutomataId)
                    ?.defaultSeasonMask ?? 15;

        private void ApplyAutomataSchedule(PlacedAutomata placement)
        {
            if (_automataPresentations.TryGetValue(placement.InstanceId,
                    out var presentation) && presentation != null)
                presentation.SetVisibilitySchedule(TimeMask(placement),
                    SeasonMask(placement));
        }

        private void BuildAutomataRoot()
        {
            _automataRoot = new GameObject("Placed Automata").transform;
            _automataRoot.SetParent(transform, false);
            _automataPresentations.Clear();
            _automataUndo.Clear();
            _selectedAutomataId = "";
            _automataDragActive = false;
        }

        public bool PlaceAutomataFromPanel(string automataId,
            Vector2 panelPosition, Vector2 panelSize)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            return PlaceAutomataAt(automataId, point.x, point.z);
        }

        public bool PlaceAutomataAt(string automataId, float x, float z)
        {
            var entry = AutomataClipCatalog.Find(automataId);
            if (entry == null ||
                !AutomataClipCatalog.ResourcesAvailable(entry) ||
                AutomataCount >= MaxAutomataGroupsPerLot ||
                !InsideAutomataBounds(x, z, entry.footprintMeters)) return false;
            PushAutomataUndo();
            var placement = new PlacedAutomata
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                AutomataId = automataId,
                PositionX = x,
                PositionZ = z,
                HasVisibilitySchedule = true,
                VisibleTimeMask = entry.defaultTimeMask,
                VisibleSeasonMask = entry.defaultSeasonMask
            };
            _session.Data.Automata ??= new List<PlacedAutomata>();
            _session.Data.Automata.Add(placement);
            CreateAutomataPresentation(placement);
            SelectAutomata(placement.InstanceId);
            NotifyAutomataStateChanged();
            return true;
        }

        public bool SelectAutomataAt(float x, float z)
        {
            var nearest = float.PositiveInfinity;
            string selected = null;
            foreach (var placement in _session.Data.Automata ??
                     new List<PlacedAutomata>())
            {
                if (placement == null) continue;
                var entry = AutomataClipCatalog.Find(placement.AutomataId);
                if (entry == null) continue;
                var half = entry.footprintMeters * 0.5f;
                if (Mathf.Abs(x - placement.PositionX) > half ||
                    Mathf.Abs(z - placement.PositionZ) > half) continue;
                var distance = (new Vector2(placement.PositionX,
                    placement.PositionZ) - new Vector2(x, z))
                    .sqrMagnitude;
                if (distance > nearest) continue;
                nearest = distance;
                selected = placement.InstanceId;
            }
            SelectAutomata(selected);
            return selected != null;
        }

        public bool BeginAutomataDragFromPanel(Vector2 panelPosition,
            Vector2 panelSize, bool newlyPlaced = false)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            if (!newlyPlaced)
            {
                var pixel = PanelToCameraPixel(panelPosition, panelSize,
                    new Vector2(_camera.pixelWidth, _camera.pixelHeight));
                var nearest = float.PositiveInfinity;
                string selected = null;
                foreach (var placement in _session.Data.Automata ??
                         new List<PlacedAutomata>())
                {
                    if (placement == null ||
                        !_automataPresentations.TryGetValue(
                            placement.InstanceId, out var presentation) ||
                        !presentation.ContainsScreenPixel(pixel)) continue;
                    var center = _camera.WorldToScreenPoint(
                        presentation.transform.position);
                    var distance = (pixel - new Vector2(center.x,
                        center.y)).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance;
                    selected = placement.InstanceId;
                }
                if (selected != null) SelectAutomata(selected);
                else if (!SelectAutomataAt(point.x, point.z)) return false;
            }
            return StartAutomataDrag(point.x, point.z, newlyPlaced);
        }

        public bool BeginAutomataDragAt(float x, float z,
            bool newlyPlaced = false)
        {
            if (!newlyPlaced && !SelectAutomataAt(x, z)) return false;
            return StartAutomataDrag(x, z, newlyPlaced);
        }

        private bool StartAutomataDrag(float x, float z,
            bool newlyPlaced)
        {
            var placement = FindSelectedAutomata();
            if (placement == null) return false;
            _automataDragOffset = newlyPlaced ? Vector2.zero :
                new Vector2(placement.PositionX - x,
                    placement.PositionZ - z);
            _automataDragActive = true;
            _automataDragMoved = false;
            _automataDragIsNew = newlyPlaced;
            return true;
        }

        public bool DragAutomataFromPanel(Vector2 panelPosition,
            Vector2 panelSize)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            return DragAutomataTo(point.x, point.z);
        }

        public bool DragAutomataTo(float x, float z)
        {
            if (!_automataDragActive) return false;
            var placement = FindSelectedAutomata();
            if (placement == null) return false;
            x += _automataDragOffset.x;
            z += _automataDragOffset.y;
            var footprint = AutomataClipCatalog.Find(placement.AutomataId)
                ?.footprintMeters ?? 8f;
            if (!InsideAutomataBounds(x, z, footprint) ||
                (Mathf.Abs(x - placement.PositionX) < 0.001f &&
                 Mathf.Abs(z - placement.PositionZ) < 0.001f)) return false;
            if (!_automataDragMoved && !_automataDragIsNew)
                PushAutomataUndo();
            _automataDragMoved = true;
            placement.PositionX = x;
            placement.PositionZ = z;
            PositionAutomataPresentation(placement);
            return true;
        }

        public bool EndAutomataDrag()
        {
            if (!_automataDragActive) return false;
            _automataDragActive = false;
            var moved = _automataDragMoved;
            _automataDragMoved = false;
            if (moved) NotifyAutomataStateChanged();
            return moved;
        }

        public bool RotateSelectedAutomata(int direction)
        {
            var placement = FindSelectedAutomata();
            if (placement == null) return false;
            PushAutomataUndo();
            placement.RotationQuarterTurns =
                (placement.RotationQuarterTurns + direction + 4) % 4;
            PositionAutomataPresentation(placement);
            NotifyAutomataStateChanged();
            return true;
        }

        public bool DeleteSelectedAutomata()
        {
            var placement = FindSelectedAutomata();
            if (placement == null) return false;
            PushAutomataUndo();
            SelectAutomata(null);
            _session.Data.Automata.Remove(placement);
            if (_automataPresentations.TryGetValue(placement.InstanceId,
                    out var presentation))
            {
                _automataPresentations.Remove(placement.InstanceId);
                if (presentation != null)
                    DestroyForCurrentMode(presentation.gameObject);
            }
            NotifyAutomataStateChanged();
            return true;
        }

        public bool UndoAutomata()
        {
            if (_automataUndo.Count == 0) return false;
            _session.Data.Automata = _automataUndo.Pop();
            SynchronizeAutomataPresentations();
            NotifyAutomataStateChanged();
            return true;
        }

        private void PushAutomataUndo()
        {
            var snapshot = new List<PlacedAutomata>();
            foreach (var item in _session.Data.Automata ??
                     new List<PlacedAutomata>())
                snapshot.Add(new PlacedAutomata
                {
                    InstanceId = item.InstanceId,
                    AutomataId = item.AutomataId,
                    PositionX = item.PositionX,
                    PositionZ = item.PositionZ,
                    RotationQuarterTurns = item.RotationQuarterTurns,
                    HasVisibilitySchedule = item.HasVisibilitySchedule,
                    VisibleTimeMask = item.VisibleTimeMask,
                    VisibleSeasonMask = item.VisibleSeasonMask
                });
            _automataUndo.Push(snapshot);
            if (_automataUndo.Count <= 20) return;
            var retained = _automataUndo.ToArray();
            _automataUndo.Clear();
            for (var index = 19; index >= 0; index--)
                _automataUndo.Push(retained[index]);
        }

        private PlacedAutomata FindSelectedAutomata()
        {
            foreach (var placement in _session.Data.Automata ??
                     new List<PlacedAutomata>())
                if (placement.InstanceId == _selectedAutomataId)
                    return placement;
            return null;
        }

        private bool InsideAutomataBounds(float x, float z,
            float footprintMeters)
        {
            var half = footprintMeters * 0.5f;
            return Mathf.Abs(x) <= LotWidthMeters * 0.5f - half &&
                Mathf.Abs(z) <= LotDepthMeters * 0.5f - half;
        }

        private void CreateAutomataPresentation(PlacedAutomata placement)
        {
            if (_automataRoot == null || placement == null ||
                _automataPresentations.ContainsKey(placement.InstanceId)) return;
            var entry = AutomataClipCatalog.Find(placement.AutomataId);
            if (entry == null) return;
            var group = new GameObject(entry.displayName);
            group.transform.SetParent(_automataRoot, false);
            var presentation = group.AddComponent<AutomataClipPlayer>();
            presentation.Initialize(this, _camera, entry,
                placement.InstanceId);
            _automataPresentations.Add(placement.InstanceId, presentation);
            ApplyAutomataSchedule(placement);
            PositionAutomataPresentation(placement);
            presentation.SetSelected(placement.InstanceId == _selectedAutomataId);
        }

        private void PositionAutomataPresentation(PlacedAutomata placement)
        {
            if (!_automataPresentations.TryGetValue(placement.InstanceId,
                    out var presentation) || presentation == null) return;
            presentation.transform.localPosition = new Vector3(
                placement.PositionX,
                SampleTerrainHeight(placement.PositionX, placement.PositionZ),
                placement.PositionZ);
            presentation.transform.localRotation = Quaternion.Euler(
                0f, placement.RotationQuarterTurns * 90f, 0f);
            presentation.RefreshPose();
        }

        private void SynchronizeAutomataPresentations()
        {
            if (_automataRoot == null) return;
            _automataRetainedIds.Clear();
            var presented = 0;
            foreach (var placement in _session.Data.Automata ??
                     new List<PlacedAutomata>())
            {
                if (placement == null ||
                    string.IsNullOrWhiteSpace(placement.InstanceId)) continue;
                if (presented >= MaxAutomataGroupsPerLot) break;
                if (!_automataRetainedIds.Add(placement.InstanceId)) continue;
                presented++;
                CreateAutomataPresentation(placement);
                ApplyAutomataSchedule(placement);
                PositionAutomataPresentation(placement);
            }
            _automataRemovedIds.Clear();
            foreach (var pair in _automataPresentations)
                if (!_automataRetainedIds.Contains(pair.Key))
                    _automataRemovedIds.Add(pair.Key);
            foreach (var id in _automataRemovedIds)
            {
                var presentation = _automataPresentations[id];
                _automataPresentations.Remove(id);
                if (presentation != null)
                    DestroyForCurrentMode(presentation.gameObject);
            }
            if (!_automataRetainedIds.Contains(_selectedAutomataId))
                SelectAutomata(null);
        }

        private void SelectAutomata(string instanceId)
        {
            if (_automataPresentations.TryGetValue(_selectedAutomataId,
                    out var previous) && previous != null)
                previous.SetSelected(false);
            _selectedAutomataId = instanceId ?? "";
            if (_automataPresentations.TryGetValue(_selectedAutomataId,
                    out var current) && current != null)
                current.SetSelected(true);
        }

        // Placed automata already receive unique IDs at creation. The generic
        // notifier re-walks every persisted lot object to repair legacy IDs;
        // this local edit has no reason to do that work.
        private void NotifyAutomataStateChanged() => StateChanged?.Invoke();
    }
}
