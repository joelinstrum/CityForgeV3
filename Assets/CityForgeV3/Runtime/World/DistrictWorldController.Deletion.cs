using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        // Remove only selected presentations. Terrain, other trees and hosted lots stay alive.
        public void RemoveDistrictPresentations(RegionCityTile district, IEnumerable<DistrictSelectionRef> removed)
        {
            bool roads = false, rivers = false;
            foreach (var item in removed)
            {
                switch (item.Kind)
                {
                    case DistrictSelectionKind.Flora:
                        if (_districtFloraPresentations.Remove(item.Id, out var tree) && tree != null)
                            RemovePresentation(tree.gameObject);
                        break;
                    case DistrictSelectionKind.Lot:
                        if (_lotsByInstance.Remove(item.Id, out var lot) && lot != null)
                        { _lots.Remove(lot); RemovePresentation(lot.gameObject); }
                        break;
                    case DistrictSelectionKind.Road: roads = true; break;
                    case DistrictSelectionKind.River: rivers = true; break;
                }
            }
            if (roads) RefreshRoads(district);
            if (rivers) RefreshRivers(district);
            SelectDistrictFlora(""); HideLotOutline();
            ShowDistrictSelection(district, System.Array.Empty<DistrictSelectionRef>());
        }

        private static void RemovePresentation(GameObject item)
        {
            item.SetActive(false);
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
    }
}
