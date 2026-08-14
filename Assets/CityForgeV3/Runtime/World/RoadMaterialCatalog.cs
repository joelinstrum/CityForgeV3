using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public enum RoadMaterialEra
    {
        Founders,
        Industrial,
        Modern
    }

    public sealed class RoadMaterialDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public RoadMaterialEra Era { get; }
        public string ResourcePath { get; }
        public bool SupportsRoad { get; }
        public bool SupportsSidewalk { get; }
        public float TilesPerTenMeters { get; }

        public RoadMaterialDefinition(string id, string displayName, RoadMaterialEra era,
            bool supportsRoad, bool supportsSidewalk, string resourcePath = null,
            float tilesPerTenMeters = 5f)
        {
            Id = id;
            DisplayName = displayName;
            Era = era;
            SupportsRoad = supportsRoad;
            SupportsSidewalk = supportsSidewalk;
            ResourcePath = string.IsNullOrWhiteSpace(resourcePath)
                ? $"CityForgeV3/Materials/RoadsV1/{id}" : resourcePath;
            TilesPerTenMeters = tilesPerTenMeters;
        }

        public Texture2D LoadTexture() => Resources.Load<Texture2D>(ResourcePath);
    }

    public static class RoadMaterialCatalog
    {
        public const string DefaultRoadId = "blacktop";
        public const string DefaultSidewalkId = "early-concrete";

        private static readonly List<RoadMaterialDefinition> Definitions = new()
        {
            new("cobblestone", "Gray Cobblestone", RoadMaterialEra.Founders, true, false,
                "CityForgeV3/Materials/RoadsChatGPTV1/cobblestone-gray", 3.333f),
            new("brick", "Realistic Brick", RoadMaterialEra.Founders, true, true,
                "CityForgeV3/Materials/RoadsChatGPTV1/brick-realistic", 3.333f),
            new("antique-brick", "Antique Brick", RoadMaterialEra.Founders, true, true,
                "CityForgeV3/Materials/RoadsChatGPTV1/brick-antique", 3.333f),
            new("dirt", "Dirt", RoadMaterialEra.Founders, true, false),
            new("early-concrete", "Early Concrete", RoadMaterialEra.Industrial, true, true),
            new("cut-stone", "Cut Stone", RoadMaterialEra.Industrial, true, true),
            new("blacktop", "Realistic Blacktop", RoadMaterialEra.Modern, true, false,
                "CityForgeV3/Materials/RoadsChatGPTV1/blacktop-realistic", 3.333f)
        };

        public static IReadOnlyList<RoadMaterialDefinition> All => Definitions;

        public static RoadMaterialDefinition Resolve(string id, bool sidewalk = false)
        {
            var fallback = sidewalk ? DefaultSidewalkId : DefaultRoadId;
            var requested = string.IsNullOrWhiteSpace(id) ? fallback : id;
            var found = Definitions.Find(item => item.Id == requested &&
                (sidewalk ? item.SupportsSidewalk : item.SupportsRoad));
            return found ?? Definitions.Find(item => item.Id == fallback);
        }

        public static List<RoadMaterialDefinition> ForEra(RoadMaterialEra era, bool sidewalk)
            => Definitions.FindAll(item => item.Era == era &&
                (sidewalk ? item.SupportsSidewalk : item.SupportsRoad));
    }
}
