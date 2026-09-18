using System.Collections.Generic;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        public void RefreshHarvestTrees(RegionCityTile district, HashSet<string> ids)
        {
            var index = DistrictHarvestIndex.For(district);
            var changed = new List<UnityEngine.SpriteRenderer>(ids.Count);
            var stationary = new List<UnityEngine.SpriteRenderer>(ids.Count);
            foreach (var id in ids)
            {
                if (_districtFloraPresentations.TryGetValue(id, out var old) && old != null)
                {
                    _floraBatches?.Remove(old);
                    old.gameObject.SetActive(false);
                    if (UnityEngine.Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
                    _districtFloraPresentations.Remove(id);
                    _forestClusters.Remove(id);
                }
                var tree = index.Find(id);
                AddDistrictFloraPresentation(tree);
                if (_districtFloraPresentations.TryGetValue(id, out var renderer))
                {
                    changed.Add(renderer);
                    if (tree.HarvestState != DistrictTreeHarvestState.Fallen) stationary.Add(renderer);
                }
            }
            // A harvested tree cannot change another tree's projected shadow.
            UpdateDistrictFloraShadowsFor(changed);
            // Falling sprites animate independently; completed stumps become static batches again.
            foreach (var renderer in stationary) _floraBatches?.Add(renderer);
        }
        public void PlayTreeFalls(RegionCityTile district, HashSet<string> ids)
        {
            var index = DistrictHarvestIndex.For(district);
            foreach (var id in ids)
            {
                var tree = index.Find(id);
                if (tree != null && _districtFloraPresentations.TryGetValue(id, out var renderer))
                    renderer.gameObject.AddComponent<DistrictTreeFallPlayer>().Begin(tree.HarvestDirection);
            }
        }
        public bool IsTreeFalling(string id) => _districtFloraPresentations.TryGetValue(id, out var renderer) &&
            renderer != null && renderer.GetComponent<DistrictTreeFallPlayer>()?.Playing == true;
    }
}
