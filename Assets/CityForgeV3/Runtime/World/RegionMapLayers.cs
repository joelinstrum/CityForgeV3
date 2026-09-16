using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class RegionMapLayers
    {
        public bool TownsAndCities = true;
        public bool DistrictNamesAndBorders = false;
        public bool Rivers = true;
        public bool Topography = true;
        public bool Transportation = true;
    }

    public enum RegionBiome { Grassland, Desert, Forest, Snow }
    public enum RegionTransportKind { Highway, Rail }

    [Serializable]
    public sealed class RegionTransportRoute
    {
        public string Id = "";
        public string Name = "";
        public RegionTransportKind Kind;
        public bool HasDistrictRoads;
        public string SurfaceId = "";
        // Region map units, shared with district X/Y and Width/Height.
        public List<Vector2> Points = new();
    }
}
