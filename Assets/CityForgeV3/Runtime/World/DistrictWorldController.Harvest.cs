using System.Collections.Generic;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        public void RefreshHarvestTrees(RegionCityTile district, HashSet<string> ids)
        {
            foreach (var id in ids)
            {
                if (_districtFloraPresentations.TryGetValue(id, out var old) && old != null)
                {
                    old.gameObject.SetActive(false);
                    if (UnityEngine.Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
                    _districtFloraPresentations.Remove(id);
                }
                AddDistrictFloraPresentation(district.Flora.Find(t => t.InstanceId == id));
            }
            UpdateDistrictFloraShadows();
        }
        public void PlayTreeFalls(RegionCityTile district, HashSet<string> ids)
        {
            foreach (var tree in district.Flora)
                if (ids.Contains(tree.InstanceId) && _districtFloraPresentations.TryGetValue(tree.InstanceId, out var renderer))
                    renderer.gameObject.AddComponent<DistrictTreeFallPlayer>().Begin(tree.HarvestDirection);
        }
        public bool IsTreeFalling(string id) => _districtFloraPresentations.TryGetValue(id, out var renderer) &&
            renderer != null && renderer.GetComponent<DistrictTreeFallPlayer>()?.Playing == true;
    }
}
