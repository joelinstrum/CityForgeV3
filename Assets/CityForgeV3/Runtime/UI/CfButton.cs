using System;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public static class CfButton
    {
        public static Button Create(
            string label,
            Action action,
            bool enabled = true,
            string variant = "secondary")
        {
            var button = new Button(action)
            {
                text = label,
                focusable = enabled
            };

            button.AddToClassList("cf-button");
            button.AddToClassList($"cf-button--{variant}");
            button.SetEnabled(enabled);

            if (!enabled)
            {
                button.tooltip = "Not available in this V3 foundation build";
            }

            return button;
        }

        public static Button CreateIcon(
            string accessibleName,
            string glyph,
            string tooltip,
            Action action,
            bool enabled = true,
            bool selected = false)
        {
            var button = Create(
                glyph,
                action,
                enabled,
                selected ? "icon-selected" : "icon");
            button.name = accessibleName;
            button.tooltip = tooltip;
            return button;
        }
    }
}
