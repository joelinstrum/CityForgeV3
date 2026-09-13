using UnityEngine;
namespace CityForgeV3.World
{
    public static class StoneFloraCatalog
    {
        public readonly struct Definition
        {
            public readonly string Id;
            public readonly string Name;
            public readonly bool Submergible;

            public Definition(string id, string name, bool submergible)
            {
                Id = id;
                Name = name;
                Submergible = submergible;
            }
        }

        public static readonly Definition[] Families = {
            new("stones-mostly-large", "Mostly Large", true),
            new("stones-large-and-small", "Large and Small", true),
            new("stones-mostly-small", "Mostly Small", true),
            new("stone-clusters-with-moss", "Mossy Stone Clusters", true),
            new("stone-cluster-2", "Mixed Stone Cluster", true),
            new("stone-cluster-4", "Small Stones", true),
            new("pebbles", "Small Pebbles", true)
        };
        public static bool IsStone(string id)
        {
            foreach (var family in Families)
                if (family.Id == id) return true;
            return false;
        }
        public static string ResourcePath(string id) => IsStone(id) ?
            "CityForgeV3/Flora/StonesV01/" + id : null;
        public static bool IsSubmergible(string id)
        {
            foreach (var family in Families)
                if (family.Id == id) return family.Submergible;
            return false;
        }
        public static Sprite CreateSprite(string id)
        {
            var texture=Resources.Load<Texture2D>(ResourcePath(id));
            if(texture==null)return null;
            // Source PNGs are retained byte-for-byte; these are atlas regions.
            Rect rect = id switch
            {
                "stones-mostly-large" => new Rect(60, 594, 550, 390),
                "stones-large-and-small" => new Rect(900, 574, 630, 410),
                "stones-mostly-small" => new Rect(200, 170, 1050, 710),
                "stone-clusters-with-moss" => new Rect(15, 0, 1507, 993),
                "stone-cluster-2" => new Rect(41, 0, 1475, 1005),
                _ => new Rect(0, 0, texture.width, texture.height)
            };
            var width = id switch
            {
                "stones-mostly-small" => 2.2f,
                "stone-cluster-4" => 4f,
                "pebbles" => 4f,
                // These full-frame compositions are much taller than the
                // original cropped atlas groups. Keep them at the established
                // family footprint so their billboard height remains below a
                // deep river's water plane and emerges naturally on shallows.
                "stone-clusters-with-moss" => 4f,
                "stone-cluster-2" => 4f,
                _ => 4f
            };
            var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.025f),rect.width/width);
            return sprite;
        }
        // Sparse ground scatters need a representative close crop in the
        // small menu swatch. Placement still uses the complete original PNG.
        public static Sprite CreatePreviewSprite(string id)
        {
            if (id != "stone-cluster-4" && id != "pebbles") return CreateSprite(id);
            var texture = Resources.Load<Texture2D>(ResourcePath(id));
            if (texture == null) return null;
            var rect = id == "stone-cluster-4"
                ? new Rect(760, 360, 360, 280)
                : new Rect(40, 420, 360, 280);
            return Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100f);
        }

        public static string DisplayName(string id)
        {
            foreach (var family in Families) if (family.Id == id) return family.Name;
            return id;
        }
    }
}
