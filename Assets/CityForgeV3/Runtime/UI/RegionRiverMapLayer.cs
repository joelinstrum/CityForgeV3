using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    // Untextured cartographic overlay. Paths share the same coordinate space as tile buttons.
    public sealed class RegionRiverMapLayer : VisualElement
    {
        public RegionRiverMapLayer(RegionSaveData region, float pixelsPerUnit)
        {
            name = "region-river-map-layer";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0; style.top = 0;
            style.width = region.Width * pixelsPerUnit;
            style.height = region.Height * pixelsPerUnit;
            generateVisualContent += context =>
            {
                var painter = context.painter2D;
                painter.strokeColor = new Color(.20f,.63f,.93f,1);
                painter.lineCap = LineCap.Round;
                painter.lineJoin = LineJoin.Round;
                // Draw district sections, including manual rivers, so this map reflects
                // the actual saved playable water rather than a separate illustration.
                foreach(var tile in region.Tiles)
                foreach(var river in tile.Rivers ?? new System.Collections.Generic.List<PlacedDistrictRiver>())
                {
                    if(river?.Points == null || river.Points.Count < 2) continue;
                    painter.lineWidth = river.Depth == DistrictRiverDepth.Deep ? 7f * river.WidthMeters / 64f : 3f;
                    painter.BeginPath();
                    for(var i=0;i<river.Points.Count;i++)
                    {
                        var p=river.Points[i];
                        var point=new Vector2((tile.X+p.X*tile.Width)*pixelsPerUnit,
                            (tile.Y+p.Z*tile.Height)*pixelsPerUnit);
                        if(i==0)painter.MoveTo(point);else painter.LineTo(point);
                    }
                    painter.Stroke();
                }
            };
        }
    }
}
