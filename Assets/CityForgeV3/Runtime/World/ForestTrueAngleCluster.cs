using System;
using UnityEngine;

namespace CityForgeV3.World
{
    // Authored deciduous (4x3) and fir (5x3) trees stay in seasonal atlases. A
    // clump is one simulation/selection handle with several cached atlas
    // quads in the existing spatial flora batch, never child renderers.
    public sealed class ForestTrueAngleCluster : MonoBehaviour
    {
        public const float PixelsPerUnit = 20f;
        public const float FirPixelsPerUnit = 27f;
        const string Root = "CityForgeV3/Flora/ForestTrueAngleAtlasV01/true-angle-trees-";
        const string FirRoot = "CityForgeV3/Flora/ForestTrueAngleFirAtlasV01/fir-trees";

        readonly struct Tree
        {
            public readonly int Slot;
            public readonly Vector2 Position;
            public readonly float Scale;
            public Tree(int slot, float x, float z, float scale = 1f)
            { Slot = slot; Position = new Vector2(x, z); Scale = scale; }
        }

        // Back to front in each diamond. Slot numbers are row-major from
        // the top left of each of Joe's matching 4x3 seasonal sheets.
        static readonly Tree[][] Compact =
        {
            new[] { new Tree(1,-5,5), new Tree(6,5,5,.93f),
                    new Tree(0,-6,-4,.95f), new Tree(9,5,-5,1.03f) },
            new[] { new Tree(3,-4,6,.96f), new Tree(4,6,4),
                    new Tree(10,-7,-4), new Tree(7,5,-6,.92f) },
            new[] { new Tree(2,-6,4), new Tree(11,5,6,.96f),
                    new Tree(8,-4,-6,1.03f), new Tree(5,7,-3,.93f) }
        };
        static readonly Tree[][] Large =
        {
            new[] { new Tree(0,-10,8), new Tree(6,0,10), new Tree(3,10,7),
                    new Tree(4,-11,-2), new Tree(9,1,-3), new Tree(7,11,-5),
                    new Tree(2,0,-10) },
            new[] { new Tree(11,-10,8), new Tree(2,1,10), new Tree(5,11,7),
                    new Tree(8,-12,-3), new Tree(1,0,-4), new Tree(10,11,-4),
                    new Tree(6,-1,-11) },
            new[] { new Tree(7,-10,9), new Tree(0,1,10), new Tree(9,10,6),
                    new Tree(3,-11,-4), new Tree(4,0,-2), new Tree(8,11,-5),
                    new Tree(1,1,-11) }
        };

        static readonly Tree[][] FirCompact =
        {
            new[] { new Tree(1,-4,5), new Tree(7,5,4),
                    new Tree(10,-5,-4), new Tree(4,5,-5) },
            new[] { new Tree(3,-4,5), new Tree(14,5,4),
                    new Tree(5,-6,-4), new Tree(2,5,-5) },
            new[] { new Tree(8,-5,5), new Tree(0,5,4),
                    new Tree(13,-5,-4), new Tree(6,6,-5) }
        };
        static readonly Tree[][] FirLarge =
        {
            new[] { new Tree(1,-8,8), new Tree(4,0,9), new Tree(3,8,7),
                    new Tree(7,-9,-1), new Tree(10,0,-2), new Tree(12,9,-4),
                    new Tree(14,0,-9) },
            new[] { new Tree(13,-8,8), new Tree(0,0,9), new Tree(8,8,7),
                    new Tree(2,-9,-2), new Tree(6,0,-2), new Tree(11,9,-4),
                    new Tree(9,0,-9) },
            new[] { new Tree(5,-8,8), new Tree(9,0,9), new Tree(14,8,7),
                    new Tree(4,-9,-1), new Tree(1,0,-2), new Tree(7,9,-4),
                    new Tree(0,0,-9) }
        };

        static readonly Sprite[][] SeasonalSprites = new Sprite[3][];
        static readonly Sprite[][] FirSeasonalSprites = new Sprite[2][];
        static Sprite autumnFirRoot;
        Tree[] layout;
        float[] groundOffsets;
        public string FloraId { get; private set; }
        public int Variation { get; private set; }
        public SeasonPreset Season { get; private set; }
        public int PieceCount => layout?.Length ?? 0;
        public bool IsLarge => ForestClusterCatalog.IsLarge(FloraId);
        public bool IsFir => SupportsFir(FloraId);
        public float TreeWidth => IsFirIndividual(FloraId)
            ? 255f / FirPixelsPerUnit : 384f / PixelsPerUnit;
        public float TreeHeight => IsFirIndividual(FloraId)
            ? 380f / FirPixelsPerUnit : 342f / PixelsPerUnit;
        public float EnvelopeWidth => IsFirIndividual(FloraId) ? 12f :
            IsLarge ? 46f : 33f;

        static int SeasonSlot(SeasonPreset season) => season switch
        {
            SeasonPreset.Autumn => 1,
            SeasonPreset.Winter => 2,
            _ => 0
        };
        static string SeasonName(int slot) => slot == 1 ? "fall" :
            slot == 2 ? "winter" : "summer";
        public static bool SupportsFir(string id) => IsFirCluster(id) ||
            IsFirIndividual(id);
        public static bool IsFirCluster(string id) =>
            id == "forest-mountain-compact" || id == "forest-mountain-large";
        public static bool IsFirIndividual(string id) => id is "cilician-fir" or
            "medium-balsam-fir" or "medium-fraser-fir" or "medium-blue-spruce";
        public static bool Supports(string id) => id == "forest-deciduous-compact" ||
            id == "forest-deciduous-large" || SupportsFir(id);
        public static string ResourcePath(SeasonPreset season) =>
            ResourcePath("forest-deciduous-large", season);
        public static string ResourcePath(string id, SeasonPreset season) =>
            SupportsFir(id) ? FirRoot + (season == SeasonPreset.Winter ? "-winter" : "") :
            Root + SeasonName(SeasonSlot(season));

        static Sprite[] FirSprites(SeasonPreset season)
        {
            int slot = season == SeasonPreset.Winter ? 1 : 0;
            if (FirSeasonalSprites[slot] != null) return FirSeasonalSprites[slot];
            var path = ResourcePath("forest-mountain-large", season);
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) throw new MissingReferenceException(path);
            if (texture.width != 1536 || texture.height != 1024)
                throw new InvalidOperationException("Fir atlas must be 1536x1024: " + path);
            // These authoring cells are not quite uniform. The cuts follow
            // transparent valleys between trees, including the longer snowy tips.
            int[][] xs = slot == 1 ? new[]
            {
                new[] { 0, 364, 608, 980, 1205, 1536 },
                new[] { 0, 276, 627, 925, 1205, 1536 },
                new[] { 0, 349, 605, 960, 1210, 1536 }
            } : new[]
            {
                new[] { 0, 346, 585, 955, 1210, 1536 },
                new[] { 0, 270, 620, 915, 1200, 1536 },
                new[] { 0, 325, 590, 955, 1205, 1536 }
            };
            int[] centerX = { 153, 460, 768, 1075, 1382 };
            int[] top = slot == 1 ?
                new[] { 376, 361, 383, 376, 388 } :
                new[] { 368, 356, 376, 379, 371 };
            int[] middle = slot == 1 ?
                new[] { 684, 702, 686, 692, 706 } :
                new[] { 685, 699, 688, 688, 697 };
            // Transparent space below each trunk varies across the hand-cut
            // atlas. Anchor the actual foot, not an arbitrary cell percentage.
            int[] footMargins = slot == 1 ? new[]
            {
                5, 1, 1, 1, 1, 13, 1, 1, 1, 1, 24, 29, 17, 19, 17
            } : new[]
            {
                1, 1, 2, 1, 1, 1, 5, 1, 1, 1, 33, 47, 18, 21, 15
            };
            var result = new Sprite[15];
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 5; col++)
            {
                int topY = row == 0 ? 0 : row == 1 ? top[col] : middle[col];
                int bottomY = row == 0 ? top[col] : row == 1 ? middle[col] : 1024;
                int left = xs[row][col];
                int width = xs[row][col + 1] - left;
                int height = bottomY - topY;
                result[row * 5 + col] = Sprite.Create(texture,
                    new Rect(left, 1024 - bottomY, width, height),
                    new Vector2((float)(centerX[col] - left) / width,
                        (float)footMargins[row * 5 + col] / height),
                    FirPixelsPerUnit, 0,
                    SpriteMeshType.FullRect);
            }
            return FirSeasonalSprites[slot] = result;
        }

        static Sprite[] Sprites(SeasonPreset season)
        {
            int slot = SeasonSlot(season);
            if (SeasonalSprites[slot] != null) return SeasonalSprites[slot];
            var path = ResourcePath(season);
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) throw new MissingReferenceException(path);
            if (texture.width != 1536 || texture.height != 1024)
                throw new InvalidOperationException("Forest atlas must be 1536x1024: " + path);
            // Equal-height rows include the tops of trees from the next row.
            // Follow the transparent gaps instead, as the fir atlas does.
            int[][] xs =
            {
                new[] { 0, 402, 768, 1152, 1536 },
                new[] { 0, 405, 765, 1155, 1536 },
                new[] { 0, 384, 768, 1155, 1536 }
            };
            int[] top = { 352, 342, 353, 352 };
            int[] middle = { 653, 656, 661, 658 };
            // Anchor each trunk at the ground after removing the contaminated
            // lower strip; the source sheets have unequal bottom margins.
            int[] footMargins = { 19, 10, 15, 5, 6, 16, 12, 16, 55, 63, 66, 62 };
            var result = new Sprite[12];
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 4; col++)
            {
                int topY = row == 0 ? 0 : row == 1 ? top[col] : middle[col];
                int bottomY = row == 0 ? top[col] : row == 1 ? middle[col] : 1024;
                int left = xs[row][col];
                int width = xs[row][col + 1] - left;
                int height = bottomY - topY;
                result[row * 4 + col] = Sprite.Create(texture,
                    new Rect(left, 1024 - bottomY, width, height),
                    new Vector2((col * 384f + 192f - left) / width,
                        (float)footMargins[row * 4 + col] / height),
                    PixelsPerUnit, 0,
                    SpriteMeshType.FullRect);
            }
            return SeasonalSprites[slot] = result;
        }

        public static void WarmAllSeasons()
        {
            Sprites(SeasonPreset.Summer);
            Sprites(SeasonPreset.Autumn);
            Sprites(SeasonPreset.Winter);
            FirSprites(SeasonPreset.Summer);
            FirSprites(SeasonPreset.Winter);
            FirRootSprite(SeasonPreset.Autumn);
        }
        public static Sprite RootSprite(SeasonPreset season) => Sprites(season)[0];
        public static Sprite RootSprite(string id, SeasonPreset season) =>
            SupportsFir(id) ? FirRootSprite(season) : Sprites(season)[0];

        static Sprite FirRootSprite(SeasonPreset season)
        {
            if (season != SeasonPreset.Autumn) return FirSprites(season)[1];
            if (autumnFirRoot != null) return autumnFirRoot;
            // Fir artwork remains green in autumn, but its mixed clusters use
            // the autumn deciduous atlas. Give every fir a distinct root key
            // so staged summer/autumn updates cannot mix three textures in one
            // spatial batch. This shares the existing atlas and rejoins one
            // fir batch per cell when the transition finishes.
            var source = FirSprites(SeasonPreset.Summer)[1];
            var pivot = new Vector2(source.pivot.x / source.rect.width,
                source.pivot.y / source.rect.height);
            autumnFirRoot = Sprite.Create(source.texture, source.rect, pivot,
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            autumnFirRoot.name = "fir-autumn-batch-root";
            return autumnFirRoot;
        }

        static int IndividualSlot(string id, int variation)
        {
            int[] slots = id switch
            {
                "medium-fraser-fir" => new[] { 1, 3, 8, 13 },
                "medium-blue-spruce" => new[] { 1, 3, 11, 13 },
                "medium-balsam-fir" => new[] { 1, 8, 11, 13 },
                _ => new[] { 1, 11, 13, 8 }
            };
            return slots[Mathf.Abs(variation % slots.Length)];
        }

        // Both seasonal atlases place matching narrow trees in these cells.
        // Skip slot 5: its summer slice has a detached foliage island below
        // the crown. Saved identities and cluster layouts stay unchanged.
        static int NarrowFirSlot(int slot) => slot switch
        {
            0 or 1 => 1,
            2 or 3 or 4 => 3,
            5 or 6 => 11,
            7 or 8 or 9 => 8,
            10 or 11 or 12 => 11,
            _ => 13
        };
        public static Sprite IndividualSprite(string id, int variation,
            SeasonPreset season) => FirSprites(season)[IndividualSlot(id, variation)];

        public void Configure(string id, int variation, SeasonPreset season,
            Func<Vector2, float> groundDelta)
        {
            if (!Supports(id)) throw new ArgumentException(id, nameof(id));
            FloraId = id;
            Variation = variation;
            layout = IsFirIndividual(id)
                ? new[] { new Tree(IndividualSlot(id, variation), 0, 0) }
                : (IsFirCluster(id) ? IsLarge ? FirLarge : FirCompact :
                    IsLarge ? Large : Compact)[Mathf.Abs(variation % 3)];
            groundOffsets = new float[layout.Length];
            SetSeason(season);
            RefreshGround(groundDelta);
        }
        public void SetSeason(SeasonPreset season) => Season = season;
        public void RefreshGround(Func<Vector2, float> groundDelta)
        {
            for (int i = 0; i < PieceCount; i++)
                groundOffsets[i] = groundDelta?.Invoke(LocalPosition(i)) ?? 0f;
        }
        // Both temperate cluster families are mixed. The mountain identity
        // still determines placement and forestry policy, but no cluster is a
        // solid block of firs in autumn. Compact groups have one or two firs;
        // large groups have one or two, with the chosen positions varying.
        public bool IsFirPiece(int index)
        {
            if (IsFirIndividual(FloraId)) return true;
            if (layout == null || index < 0 || index >= layout.Length) return false;
            int variant = Mathf.Abs(Variation % 3);
            // Put the evergreen at an outer front position. A smaller fir
            // hidden behind broadleaf crowns does not read as a mixed stand.
            int first = IsLarge ? 3 + variant % 3 : 2 + variant % 2;
            if (index == first) return true;
            bool second = IsLarge && (IsFirCluster(FloraId) || variant == 1);
            int other = first == 3 ? 5 : first == 4 ? 6 : 3;
            return second && index == other;
        }
        public Sprite Piece(int index) => IsFirPiece(index)
            ? FirSprites(Season)[NarrowFirSlot(layout[index].Slot)]
            : Sprites(Season)[CleanDeciduousSlot(layout[index].Slot % 12)];
        // The last summer cell has an isolated 52x48 foliage island beneath
        // its crown. Slot 9 is clean and never shares a layout with slot 11.
        static int CleanDeciduousSlot(int slot) => slot == 11 ? 9 : slot;
        public float PieceScale(int index) => layout[index].Scale *
            (IsFirPiece(index) && !IsFirIndividual(FloraId) ? 1.22f : 1f);
        private Vector2 LocalPosition(int index)
        {
            var point = layout[index].Position;
            if (IsFirPiece(index) && !IsFirIndividual(FloraId))
            {
                point.x += point.x >= 0f ? 3f : -3f;
                point.y -= 2f;
            }
            return point;
        }
        public Vector3 WorldOffset(int index)
        {
            var point = LocalPosition(index);
            var local = new Vector3(point.x, groundOffsets[index], point.y);
            return transform.parent != null ? transform.parent.TransformVector(local) : local;
        }
    }
}
