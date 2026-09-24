using System;
using UnityEngine;

namespace CityForgeV3.World
{
    // The twelve authored trees stay in their original seasonal atlases. A
    // clump is one simulation/selection handle with several cached atlas
    // quads in the existing spatial flora batch, never child renderers.
    public sealed class ForestTrueAngleCluster : MonoBehaviour
    {
        public const float PixelsPerUnit = 20f;
        const string Root = "CityForgeV3/Flora/ForestTrueAngleAtlasV01/true-angle-trees-";

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

        static readonly Sprite[][] SeasonalSprites = new Sprite[3][];
        Tree[] layout;
        float[] groundOffsets;
        public string FloraId { get; private set; }
        public SeasonPreset Season { get; private set; }
        public int PieceCount => layout?.Length ?? 0;
        public bool IsLarge => ForestClusterCatalog.IsLarge(FloraId);
        public float TreeWidth => 384f / PixelsPerUnit;
        public float TreeHeight => 342f / PixelsPerUnit;
        public float EnvelopeWidth => IsLarge ? 46f : 33f;

        static int SeasonSlot(SeasonPreset season) => season switch
        {
            SeasonPreset.Autumn => 1,
            SeasonPreset.Winter => 2,
            _ => 0
        };
        static string SeasonName(int slot) => slot == 1 ? "fall" :
            slot == 2 ? "winter" : "summer";
        public static bool Supports(string id) => id == "forest-deciduous-compact" ||
            id == "forest-deciduous-large";
        public static string ResourcePath(SeasonPreset season) =>
            Root + SeasonName(SeasonSlot(season));

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
        }
        public static Sprite RootSprite(SeasonPreset season) => Sprites(season)[0];

        public void Configure(string id, int variation, SeasonPreset season,
            Func<Vector2, float> groundDelta)
        {
            if (!Supports(id)) throw new ArgumentException(id, nameof(id));
            FloraId = id;
            layout = (IsLarge ? Large : Compact)[Mathf.Abs(variation % 3)];
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
        public Sprite Piece(int index) => Sprites(Season)[layout[index].Slot];
        public float PieceScale(int index) => layout[index].Scale;
        public Vector3 WorldOffset(int index)
        {
            var point = layout[index].Position;
            var local = new Vector3(point.x, groundOffsets[index], point.y);
            return transform.parent != null ? transform.parent.TransformVector(local) : local;
        }
    }
}
