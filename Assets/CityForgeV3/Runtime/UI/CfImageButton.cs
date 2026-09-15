using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public static class CfImageButton
    {
        private static Texture2D _houseIcon;
        private static Texture2D _roadIcon;
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

        public static Texture2D CreateRoadIcon()
        {
            if (_roadIcon != null) return _roadIcon;
            const int size = 64;
            var pixels = new Color32[size * size];
            for (var y = 8; y < 58; y++)
            for (var x = 0; x < size; x++)
            {
                var halfWidth = Mathf.Lerp(23, 10, (y - 8) / 50f);
                var distance = Mathf.Abs(x - 31.5f);
                if (distance > halfWidth) continue;
                var edge = distance > halfWidth - 2;
                var centerLine = distance < 1.5f && ((y - 8) / 8) % 2 == 0;
                pixels[y * size + x] = edge ? new Color32(232, 222, 184, 255)
                    : centerLine ? new Color32(225, 179, 69, 255)
                    : new Color32(70, 80, 84, 255);
            }
            _roadIcon = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CityForge Road Tool Icon", filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            _roadIcon.SetPixels32(pixels);
            _roadIcon.Apply(false, true);
            return _roadIcon;
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
