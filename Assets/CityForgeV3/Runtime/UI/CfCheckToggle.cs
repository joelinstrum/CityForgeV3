using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    // Runtime UI has no editor theme: supply a visible checkbox and check glyph.
    public static class CfCheckToggle
    {
        public static Toggle Create(string label,bool value)
        {
            var toggle=new Toggle(label){value=value};toggle.AddToClassList("cf-check-toggle");
            var box=toggle.Q<VisualElement>(className:"unity-toggle__checkmark");
            if(box!=null)
            {
                var glyph=new Label(value?"✓":""){name="check-glyph",pickingMode=PickingMode.Ignore};
                glyph.AddToClassList("cf-check-toggle-glyph");box.Add(glyph);
                toggle.RegisterValueChangedCallback(e=>glyph.text=e.newValue?"✓":"");
            }
            return toggle;
        }
    }
}
