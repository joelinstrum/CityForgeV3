using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private void ComposeWaterArrowInspector(VisualElement panel)
        {
            panel.Add(StyledLabel("WATER-FACING ARROW", "inspector-title"));
            panel.Add(StyledLabel("Drag the arrow to move it. Choose the side that should face water. The arrow sets facing; the mill stays on land and the barge stays on water.", "inspector-note"));
            var names=new[]{"NORTH ↑","EAST →","SOUTH ↓","WEST ←"};
            for(var i=0;i<4;i++)
            {
                var direction=i;
                var selected=_lotWorld.WaterArrowDirection==i;
                var button=CfButton.Create((selected ? "✓ " : "")+names[i],()=>{_lotWorld.SetWaterArrowDirection(direction);RefreshLotEditor();},true,selected ? "mode-selected" : "secondary");
                button.name="water-arrow-direction-"+i; panel.Add(button);
            }
            panel.Add(CfButton.Create("REMOVE ARROW",()=>{_lotWorld.ClearWaterOrientation();RefreshLotEditor();},true,"quiet"));
        }
    }
}
