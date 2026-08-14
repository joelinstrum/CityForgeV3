using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public static class CfImageButton
    {
        private static Texture2D _houseIcon;
        public static Button Create(
            string accessibleName,
            string resourcePath,
            Action action,
            bool enabled,
            string variant = "menu")
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            var button = new Button(action)
            {
                name = accessibleName,
                tooltip = enabled
                    ? accessibleName
                    : $"{accessibleName} is not available in this V3 build",
                focusable = enabled
            };

            button.AddToClassList("cf-image-button");
            button.AddToClassList($"cf-image-button--{variant}");
            if (texture != null)
            {
                button.style.backgroundImage = new StyleBackground(texture);
            }
            else
            {
                Debug.LogError($"Missing UI image resource: {resourcePath}");
            }

            button.SetEnabled(enabled);
            return button;
        }

        public static Button CreateWithTexture(
            string accessibleName,
            Texture2D texture,
            Action action,
            bool enabled,
            string variant = "menu")
        {
            var button = new Button(action)
            {
                name = accessibleName,
                tooltip = accessibleName,
                focusable = enabled
            };
            button.AddToClassList("cf-image-button");
            button.AddToClassList($"cf-image-button--{variant}");
            button.style.backgroundImage = new StyleBackground(texture);
            button.SetEnabled(enabled);
            return button;
        }

        public static Texture2D CreateHouseIcon(Color color)
        {
            if (_houseIcon != null) return _houseIcon;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CityForge House Tool Icon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            var ink = (Color32)color;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var roof = y >= 30 && y <= 50 &&
                           y <= 50 - Mathf.Abs(x - 31) * 0.72f;
                var walls = x >= 14 && x <= 49 && y >= 10 && y <= 31;
                var doorCutout = x >= 27 && x <= 36 && y >= 10 && y <= 24;
                pixels[y * size + x] = (roof || (walls && !doorCutout))
                    ? ink
                    : new Color32(0, 0, 0, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _houseIcon = texture;
            return _houseIcon;
        }
    }
}
