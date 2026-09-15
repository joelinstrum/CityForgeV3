using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    [Serializable]
    public sealed class BoatDefinition
    {
        public string id;
        public string displayName;
        public string category;
        public string prefabResourcePath;
        public string thumbnailResourcePath;
        public float widthMeters;
        public float lengthMeters;
    }

    public static class BoatCatalog
    {
        private static BoatDefinition[] _entries;
        public static IReadOnlyList<BoatDefinition> All => _entries ??= Resources
            .LoadAll<TextAsset>("CityForgeV3/Boats/Catalog")
            .Select(asset => JsonUtility.FromJson<BoatDefinition>(asset.text))
            .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.id))
            .OrderBy(entry => entry.displayName).ToArray();
        public static BoatDefinition Find(string id) => All.FirstOrDefault(entry => entry.id == id);
    }
}
