using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    // One shared atlas; UV rectangles do not create duplicate textures or materials.
    internal static class CfMapChrome
    {
        static Texture2D atlas;
        static Font titleFont;
        static Texture2D quietAtlas;
        static readonly System.Collections.Generic.Dictionary<string, Texture2D> resourceTextures = new();
        internal static Image ResourceIcon(string key)
        {
            if (key == "POPULATION" || key == "BRICK") return QuietIcon(key == "POPULATION" ? 0 : 1);
            string file = key switch { "TREASURY" => "gold", "LUMBER" => "wood", "STONE" => "stone", _ => "food" };
            if (!resourceTextures.TryGetValue(file, out var texture))
                resourceTextures[file] = texture = Resources.Load<Texture2D>("CityForgeV3/Art/ResourceIconsV01/" + file);
            var image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.AddToClassList("cf-map-icon"); return image;
        }
        static Image QuietIcon(int index)
        {
            if (quietAtlas == null) quietAtlas = Resources.Load<Texture2D>("CityForgeV3/Art/QuietHudV02/icons");
            var image = new Image { image = quietAtlas, uv = new Rect(index % 2 * .5f, (1 - index / 2) * .5f, .5f, .5f), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.AddToClassList("cf-map-icon"); return image;
        }
        internal static Label Title(string text, string style)
        {
            if (titleFont == null) titleFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "serif" }, 28);
            var label = new Label(text); label.AddToClassList(style);
            if (titleFont != null) label.style.unityFont = titleFont;
            return label;
        }
        internal static int IconIndex(string key) => key switch
        {
            "Select" => 0, "Terrain" => 1, "Water" or "Rivers" => 2,
            "Flora" => 3, "Climate" or "Environment" or "Sun" => 4,
            "Roads" => 5, "Lots" => 6, "Industry" => 7,
            "Manage" or "Save" or "Labor" or "Resources" => 8,
            "Statistics" => 9, "Menu" => 10, "Civic" or "Region" => 11,
            "Parks" => 12, "Transit" => 13, "Utilities" => 14, "Zoning" => 15,
            _ => 8
        };
        internal static Rect IconUv(string key)
        {
            int index = IconIndex(key);
            return new Rect((index % 4) * .25f, (3 - index / 4) * .25f, .25f, .25f);
        }
        internal static Image Icon(string key)
        {
            if (key == "Build" || key == "TerrainAction") return QuietIcon(key == "Build" ? 2 : 3);
            if (atlas == null) atlas = Resources.Load<Texture2D>("CityForgeV3/Art/MapChromeV01/medallions");
            var icon = new Image { image = atlas, uv = IconUv(key), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("cf-map-icon");
            return icon;
        }
        internal static void Illustrate(Button button, string key)
        {
            button.text = "";
            button.AddToClassList("cf-map-icon-button");
            button.Insert(0, Icon(key));
            var caption = new Label(key == "Water" ? "Rivers" : key == "Environment" ? "Weather" : key) { name = "map-caption", pickingMode = PickingMode.Ignore };
            caption.AddToClassList("cf-map-caption"); button.Add(caption);
        }
        internal static Button Action(string label, string icon, Action action, string name = null)
        {
            var button = new Button(action) { name = name, tooltip = label };
            button.AddToClassList("cf-map-action");
            button.Insert(0, Icon(icon));
            var caption = new Label(label) { name = "map-caption", pickingMode = PickingMode.Ignore };
            caption.AddToClassList("cf-map-caption"); button.Add(caption);
            return button;
        }
        internal static void SetCaption(Button button, string caption)
        {
            button.tooltip = caption;
            var label = button.Q<Label>("map-caption");
            if (label != null) label.text = caption;
            else button.text = caption;
        }
        internal static VisualElement Panel(string name, string style)
        {
            var element = new VisualElement { name = name };
            element.AddToClassList("cf-map-chrome"); element.AddToClassList(style);
            return element;
        }
    }
}
