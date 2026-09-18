using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        readonly Dictionary<string, SpriteRenderer> _forestClusters = new();
        SeasonPreset _forestSeason = SeasonPreset.Summer;
        public SeasonPreset ForestSeason => _forestSeason;

        SpriteRenderer[] _pendingForestSeason;
        int _pendingForestIndex;
        public bool ForestSeasonPending => _pendingForestSeason != null;
        public const int ForestSeasonFrameBudget = 16;
        void Update() => SyncForestSeason(ForestSeasonFrameBudget);

        static void ApplyForestSeasonCutoff(SpriteRenderer renderer)
        {
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            // Remove faint residual canopy alpha while keeping opaque winter wood.
            properties.SetFloat("_Cutoff", renderer.sprite.texture.name.EndsWith("-winter") ? .12f : .02f);
            renderer.SetPropertyBlock(properties);
        }

        Sprite ForestSprite(string id, SeasonPreset season)
        {
            string path = ForestClusterCatalog.ResourcePath(id, season);
            if (_districtFloraSprites.TryGetValue(path, out var sprite) && sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) throw new MissingReferenceException(path);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                ForestClusterCatalog.Pivot, ForestClusterCatalog.PixelsPerUnit);
            return _districtFloraSprites[path] = sprite;
        }

        void PrepareForestSeason(RegionCityTile district)
        {
            _pendingForestSeason = null; _pendingForestIndex = 0;
            _forestSeason = ForestClusterCatalog.SeasonForIndex(district.Labor?.SeasonIndex ?? 0);
            // Warm six shared sprites at the existing loading/bulk-edit boundary.
            // First seasonal use must not decode textures or build tight sprite meshes.
            if (_forestClusters.Count == 0) return;
            foreach (var season in new[] { SeasonPreset.Summer, SeasonPreset.Autumn, SeasonPreset.Winter })
                for (int i = 0; i < 2; i++) ForestSprite(ForestClusterCatalog.Id(i), season);
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
                if (_pendingForestSeason == null &&
                    ((previous == SeasonPreset.Spring && season == SeasonPreset.Summer) ||
                     (previous == SeasonPreset.Summer && season == SeasonPreset.Spring))) return;
                // One snapshot of the cluster-only registry at the season boundary.
                // Small bounded slices below update only their affected batch cells.
                _pendingForestSeason = new SpriteRenderer[_forestClusters.Count];
                _forestClusters.Values.CopyTo(_pendingForestSeason, 0);
                _pendingForestIndex = 0;
            }
            if (_pendingForestSeason == null) return;
            _floraBatches?.BeginChanges();
            try
            {
                int end = Mathf.Min(_pendingForestSeason.Length, _pendingForestIndex + Mathf.Max(1, budget));
                var changed = new List<SpriteRenderer>(end - _pendingForestIndex);
                for (; _pendingForestIndex < end; _pendingForestIndex++)
                {
                    var renderer = _pendingForestSeason[_pendingForestIndex];
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
                if (_pendingForestIndex == _pendingForestSeason.Length) _pendingForestSeason = null;
            }
            finally { _floraBatches?.EndChanges(); }
        }

    }
}
