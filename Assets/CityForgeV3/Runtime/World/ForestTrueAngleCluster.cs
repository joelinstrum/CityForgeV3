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
        Tree[] layout;
        float[] groundOffsets;
        public string FloraId { get; private set; }
        public int Variation { get; private set; }
        public SeasonPreset Season { get; private set; }
        public int PieceCount => layout?.Length ?? 0;
        public bool IsLarge => ForestClusterCatalog.IsLarge(FloraId);
        public bool IsFir => SupportsFir(FloraId);
        public float TreeWidth => IsFir ? 307f / FirPixelsPerUnit : 384f / PixelsPerUnit;
        public float TreeHeight => IsFir ? 380f / FirPixelsPerUnit : 342f / PixelsPerUnit;
        public float EnvelopeWidth => IsFir ?
            IsLarge ? 38f : IsFirCluster(FloraId) ? 28f : 12f :
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
            var result = new Sprite[15];
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 5; col++)
            {
                int topY = row == 0 ? 0 : row == 1 ? top[col] : middle[col];
                int bottomY = row == 0 ? top[col] : row == 1 ? middle[col] : 1024;
                int left = xs[row][col];
                int width = xs[row][col + 1] - left;
                result[row * 5 + col] = Sprite.Create(texture,
                    new Rect(left, 1024 - bottomY, width, bottomY - topY),
                    new Vector2((float)(centerX[col] - left) / width, .04f),
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
            var result = new Sprite[12];
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 4; col++)
            {
                int y = row == 0 ? 683 : row == 1 ? 342 : 0;
                int height = row == 2 ? 342 : 341;
                result[row * 4 + col] = Sprite.Create(texture,
                    new Rect(col * 384, y, 384, height),
                    new Vector2(.5f, .045f), PixelsPerUnit, 0,
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
        }
        public static Sprite RootSprite(SeasonPreset season) => Sprites(season)[0];
        public static Sprite RootSprite(string id, SeasonPreset season) =>
            SupportsFir(id) ? FirSprites(season)[0] : Sprites(season)[0];

        static int IndividualSlot(string id, int variation)
        {
            int[] slots = id switch
            {
                "medium-fraser-fir" => new[] { 1, 3, 8, 13 },
                "medium-blue-spruce" => new[] { 0, 4, 7, 10 },
                "medium-balsam-fir" => new[] { 2, 6, 9, 14 },
                _ => new[] { 5, 11, 12, 14 }
            };
            return slots[Mathf.Abs(variation % slots.Length)];
        }
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
                groundOffsets[i] = groundDelta?.Invoke(layout[i].Position) ?? 0f;
        }
        public Sprite Piece(int index) =>
            (IsFir ? FirSprites(Season) : Sprites(Season))[layout[index].Slot];
        public float PieceScale(int index) => layout[index].Scale;
        public Vector3 WorldOffset(int index)
        {
            var point = layout[index].Position;
            var local = new Vector3(point.x, groundOffsets[index], point.y);
            return transform.parent != null ? transform.parent.TransformVector(local) : local;
        }
    }
}
