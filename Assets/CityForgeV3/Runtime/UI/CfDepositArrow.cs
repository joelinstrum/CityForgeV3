using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed class CfDepositArrow : VisualElement
    {
        public CfDepositArrow(bool available = true)
        {
            pickingMode = PickingMode.Ignore;
            style.width = 48;
            style.height = 58;
            generateVisualContent += context =>
            {
                var p = context.painter2D;
                p.fillColor = available ? new Color(.25f, 1f, .35f) : new Color(1f, .65f, .12f);
                p.strokeColor = new Color(.03f, .22f, .07f);
                p.lineWidth = 3;
                p.BeginPath();
                p.MoveTo(new Vector2(15, 3)); p.LineTo(new Vector2(33, 3));
                p.LineTo(new Vector2(33, 30)); p.LineTo(new Vector2(44, 30));
                p.LineTo(new Vector2(24, 53)); p.LineTo(new Vector2(4, 30));
                p.LineTo(new Vector2(15, 30)); p.ClosePath();
                p.Fill(); p.Stroke();
            };
        }
    }
}
