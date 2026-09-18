using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    [Serializable]
    public sealed class AutomataClipEntry
    {
        public string id = "";
        public string displayName = "";
        public string description = "";
        public string resourceRoot = "";
        public string thumbnailResource = "";
        public float footprintMeters = 8f;
        public int frameWidth = 224;
        public int frameHeight = 128;
        public int frameColumns = 8;
        public int frameCount = 32;
        public int facingCount = 8;
        public float framesPerSecond = 8f;
        public float pixelsPerMeter = 32f;
        public float pivotY = 0.375f;
        public float visibleBelowPivotMeters;
        public string recolorMaskRoot = "";
        public string recolorSlotOne = "";
        public string recolorSlotTwo = "";
        public int defaultTimeMask = 31;
        public int defaultSeasonMask = 15;
    }

    [Serializable]
    internal sealed class AutomataClipCollection
    {
        public AutomataClipEntry[] clips;
    }

    public static class AutomataClipCatalog
    {
        private static AutomataClipEntry[] _entries;
        private static readonly Dictionary<string, AutomataClipEntry> ById =
            new(StringComparer.Ordinal);

        public static IReadOnlyList<AutomataClipEntry> Entries
        {
            get
            {
                EnsureLoaded();
                return _entries;
            }
        }

        public static AutomataClipEntry Find(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && ById.TryGetValue(id,
                out var entry) ? entry : null;
        }

        public static bool ResourcesAvailable(AutomataClipEntry entry)
        {
            if (entry == null || entry.frameWidth <= 0 ||
                entry.frameHeight <= 0 || entry.frameColumns <= 0 ||
                entry.frameCount <= 0 || entry.facingCount <= 0 ||
                entry.framesPerSecond <= 0f || entry.pixelsPerMeter <= 0f ||
                string.IsNullOrWhiteSpace(entry.resourceRoot)) return false;
            for (var facing = 0; facing < entry.facingCount; facing++)
            {
                var texture = Resources.Load<Texture2D>(
                    entry.resourceRoot + "-facing-" + facing);
                if (texture == null ||
                    texture.width != entry.frameColumns * entry.frameWidth ||
                    texture.height !=
                    Mathf.CeilToInt((float)entry.frameCount /
                                    entry.frameColumns) * entry.frameHeight)
                    return false;
                if (!string.IsNullOrEmpty(entry.recolorMaskRoot))
                {
                    var mask = Resources.Load<Texture2D>(
                        entry.recolorMaskRoot + "-facing-" + facing);
                    if (mask == null || mask.width != texture.width ||
                        mask.height != texture.height) return false;
                }
            }
            return true;
        }

        private static void EnsureLoaded()
        {
            if (_entries != null) return;
            var asset = Resources.Load<TextAsset>(
                "CityForgeV3/Automata/automata-catalog");
            var collection = asset == null ? null :
                JsonUtility.FromJson<AutomataClipCollection>(asset.text);
            _entries = collection?.clips ?? Array.Empty<AutomataClipEntry>();
            ById.Clear();
            foreach (var entry in _entries)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.id) &&
                    !ById.ContainsKey(entry.id))
                    ById.Add(entry.id, entry);
        }
    }
}
