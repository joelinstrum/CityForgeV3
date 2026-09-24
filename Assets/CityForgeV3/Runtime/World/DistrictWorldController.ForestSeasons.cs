using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        readonly Dictionary<string, SpriteRenderer> _forestClusters = new();
        SeasonPreset _forestSeason = SeasonPreset.Summer;
        public SeasonPreset ForestSeason => _forestSeason;

        Dictionary<string, SpriteRenderer>.ValueCollection.Enumerator
            _pendingForestEnumerator;
        bool _forestAppearancePending;
        public bool ForestSeasonPending => _forestAppearancePending;
        public const int ForestSeasonFrameBudget = 4;
        SpriteRenderer[] _pendingTimeOfDayShadows;
        int _pendingTimeOfDayShadowIndex;
        public bool TimeOfDayPresentationPending =>
            _pendingTimeOfDayShadows != null ||
            (_floraBatches != null && _floraBatches.RebuildPending);
        public const int TimeOfDayShadowFrameBudget = 8;
        void Update()
        {
            SyncTimeOfDayPresentation(TimeOfDayShadowFrameBudget);
            SyncForestSeason(ForestSeasonFrameBudget);
        }

        void PrepareTimeOfDayPresentation()
        {
            _floraBatches?.CancelScheduledRebuild();
            _pendingTimeOfDayShadowIndex = 0;
            if (_districtFloraPresentations.Count == 0)
            {
                _pendingTimeOfDayShadows = null;
                return;
            }
            _pendingTimeOfDayShadows = new SpriteRenderer[
                _districtFloraPresentations.Count];
            _districtFloraPresentations.Values.CopyTo(
                _pendingTimeOfDayShadows, 0);
        }

        // Time changes affect the entire district, but projected tree shadows
        // are updated in bounded slices and their spatial batches are replaced
        // one at a time. Dense forests therefore never cause a single-frame
        // full flora rebuild at a clock boundary.
        public void SyncTimeOfDayPresentation(
            int budget = TimeOfDayShadowFrameBudget)
        {
            if (_pendingTimeOfDayShadows != null)
            {
                var end = Mathf.Min(_pendingTimeOfDayShadows.Length,
                    _pendingTimeOfDayShadowIndex + Mathf.Max(1, budget));
                UpdateDistrictFloraShadowsFor(new System.ArraySegment<SpriteRenderer>(
                    _pendingTimeOfDayShadows, _pendingTimeOfDayShadowIndex,
                    end - _pendingTimeOfDayShadowIndex));
                _pendingTimeOfDayShadowIndex = end;
                if (_pendingTimeOfDayShadowIndex >=
                    _pendingTimeOfDayShadows.Length)
                {
                    _pendingTimeOfDayShadows = null;
                    _pendingTimeOfDayShadowIndex = 0;
                    _floraBatches?.ScheduleRebuild();
                }
                return;
            }
            _floraBatches?.RebuildScheduled();
        }

        static void ApplyForestSeasonCutoff(SpriteRenderer renderer)
        {
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            string textureName = renderer.sprite.texture.name;
            // V03 cluster art has a nearly opaque interior and a broad,
            // chromatic antialias fringe. Clip that shared derivative family
            // at half coverage; older seasonal and individual-tree art keeps
            // its established threshold. This is one batch property per
            // texture, not a per-tree material or update.
            properties.SetFloat("_Cutoff",
                ForestClusterCatalog.UsesDepthShadedCutout(textureName) ? .5f :
                textureName.EndsWith("-winter") ? .12f : .02f);
            renderer.SetPropertyBlock(properties);
        }

        Sprite ForestSprite(string id, SeasonPreset season)
        {
            string path = ForestClusterCatalog.FarCanopyResourcePath(id,
                season) ?? ForestClusterCatalog.ResourcePath(id, season);
            if (_districtFloraSprites.TryGetValue(path, out var sprite) && sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) throw new MissingReferenceException(path);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                ForestClusterCatalog.Pivot, ForestClusterCatalog.PixelsPerUnit(id),
                0, ForestClusterCatalog.UsesQuadCanopyMesh(path)
                    ? SpriteMeshType.FullRect : SpriteMeshType.Tight);
            return _districtFloraSprites[path] = sprite;
        }

        void PrepareForestSeason(RegionCityTile district)
        {
            _forestAppearancePending = false;
            _forestSeason = ForestClusterCatalog.SeasonForIndex(district.Labor?.SeasonIndex ?? 0);
            // Warm the family sprites at the existing loading/bulk-edit boundary.
            // First seasonal use must not decode textures or build tight sprite meshes.
            if (_forestClusters.Count == 0) return;
            foreach (var season in new[] { SeasonPreset.Summer, SeasonPreset.Autumn, SeasonPreset.Winter })
                foreach (var family in FloraFamilies.Names)
                {
                    ForestSprite(ForestClusterCatalog.Id(family, false), season);
                    ForestSprite(ForestClusterCatalog.Id(family, true), season);
                }
        }

        void RegisterForestCluster(string id, SpriteRenderer renderer)
        {
            _forestClusters[id] = renderer;
            RestartForestAppearanceIfPending();
        }

        void UnregisterForestCluster(string id)
        {
            if (_forestClusters.Remove(id)) RestartForestAppearanceIfPending();
        }

        void RestartForestAppearanceIfPending()
        {
            if (_forestAppearancePending)
                _pendingForestEnumerator = _forestClusters.Values.GetEnumerator();
        }

        // Read the existing calendar without advancing it or touching labor state.
        // The common path is a scalar comparison, never a collection scan.
        public void SyncForestSeason(int budget = ForestSeasonFrameBudget)
        {
            if (_content == null || !_content.gameObject.activeInHierarchy || _terrainDistrict == null) return;
            var season = ForestClusterCatalog.SeasonForIndex(_terrainDistrict.Labor?.SeasonIndex ?? 0);
            if (season != _forestSeason)
            {
                var previous = _forestSeason;
                _forestSeason = season;
                if (!_forestAppearancePending &&
                    ((previous == SeasonPreset.Spring && season == SeasonPreset.Summer) ||
                     (previous == SeasonPreset.Summer && season == SeasonPreset.Spring))) return;
                // Season changes only create an O(1) enumerator. Dense
                // districts advance in bounded slices, not one scan.
                _pendingForestEnumerator = _forestClusters.Values.GetEnumerator();
                _forestAppearancePending = _forestClusters.Count > 0;
            }
            if (!_forestAppearancePending) return;
            _floraBatches?.BeginChanges();
            try
            {
                var changed = new List<SpriteRenderer>(Mathf.Max(1, budget));
                for (var index = 0; index < Mathf.Max(1, budget); index++)
                {
                    if (!_pendingForestEnumerator.MoveNext())
                    {
                        _forestAppearancePending = false;
                        break;
                    }
                    var renderer = _pendingForestEnumerator.Current;
                    if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                    var id = FloraTreeRepairs.Identity(renderer.sprite.texture.name);
                    var sprite = ForestSprite(id, season);
                    if (renderer.sprite == sprite) continue;
                    _floraBatches?.Remove(renderer);
                    renderer.sprite = sprite;
                    ApplyForestSeasonCutoff(renderer);
                    changed.Add(renderer);
                }
                UpdateDistrictFloraShadowsFor(changed);
                foreach (var renderer in changed) _floraBatches?.Add(renderer);
            }
            finally { _floraBatches?.EndChanges(); }
        }

    }
}
