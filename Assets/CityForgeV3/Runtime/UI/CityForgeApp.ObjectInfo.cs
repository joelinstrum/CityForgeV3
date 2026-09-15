using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using CityForgeV3.World;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private void AddSelectedObjectId(VisualElement inspector)
        {
            if (!_lotWorld.TrySelectedObjectInfo(out var friendlyName, out var objectId, out var position)) return;
            var group = new VisualElement { name = "selected-object-info" };
            group.style.marginBottom = 12;
            var title = StyledLabel(friendlyName, "inspector-title"); group.Add(title);
            group.Add(StyledLabel("OBJECT ID", "section-label"));
            var field = new TextField { value = objectId, isReadOnly = true, name = "selected-object-id" };
            field.tooltip = "Stable ID for this placed object. Use it in lot scripts.";
            field.style.fontSize = 18; group.Add(field);
            Button copy = null;
            copy = CfButton.Create("COPY ID", () =>
            { GUIUtility.systemCopyBuffer = objectId; copy.text = "COPIED"; }, true, "quiet");
            copy.name = "copy-selected-object-id"; group.Add(copy);
            var coordinates = StyledLabel("", "inspector-note"); coordinates.name = "selected-object-position";
            coordinates.tooltip = "Lot coordinates in meters: X and Y are the horizontal axes (Unity X and Z).";
            group.Add(coordinates);
            var coordinateActions = new VisualElement();
            coordinateActions.AddToClassList("inspector-actions");
            void AddCopyCoordinate(string axis, bool horizontalX)
            {
                Button button = null;
                button = CfButton.Create("COPY " + axis, () =>
                {
                    if (!_lotWorld.TrySelectedObjectInfo(out _, out var selectedId, out var current) || selectedId != objectId) return;
                    GUIUtility.systemCopyBuffer = (horizontalX ? current.x : current.z).ToString("R", CultureInfo.InvariantCulture);
                    button.text = axis + " COPIED";
                    button.schedule.Execute(() => button.text = "COPY " + axis).StartingIn(1500);
                }, true, "quiet");
                button.name = "copy-selected-object-" + axis.ToLowerInvariant();
                button.tooltip = horizontalX ? "Copy current X as a number for script x" : "Copy current horizontal Y as a number for script z";
                button.style.flexGrow = 1; button.style.width = 0; button.style.minWidth = 0;
                coordinateActions.Add(button);
            }
            AddCopyCoordinate("X", true); AddCopyCoordinate("Y", false);
            group.Add(coordinateActions);
            void UpdatePosition()
            {
                if (_lotWorld.TrySelectedObjectInfo(out var currentName, out var currentId, out var point) && currentId == objectId)
                { title.text = currentName; coordinates.text = $"X: {point.x:0.00} m    Y: {point.z:0.00} m"; }
                else coordinates.text = "Object no longer present";
            }
            UpdatePosition(); group.schedule.Execute(UpdatePosition).Every(100);
            inspector.Add(group);
        }
    }
}
