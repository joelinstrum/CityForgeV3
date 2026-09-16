using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public static class RegionMapProjection
    {
        // Match the district camera's 20-degree elevation without stretching either ground axis.
        public const float VerticalScale = .3420201433f; // sin(20 degrees)
        public static Vector3 GroundScale(float width, float height) => new Vector3(1,-1,1);

        public static void ApplyGround(VisualElement element,float width,float height)
        {
            element.style.scale=new Scale(GroundScale(width,height));
        }

        // Inverse of the ground's scale, rotation, then vertical projection.
        // Separate nodes preserve this order even for rectangular regions.
        public static VisualElement CreateLabelAnchor(float width,float height,
            out VisualElement content)
        {
            VisualElement Node() => new VisualElement
            {
                pickingMode=PickingMode.Ignore,
                style={position=Position.Absolute,width=0,height=0,
                    transformOrigin=new TransformOrigin(0,0,0)}
            };
            var anchor=Node();
            var ground=GroundScale(width,height);
            anchor.style.scale=new Scale(new Vector3(1/ground.x,1/ground.y,1));
            var rotation=Node();rotation.style.rotate=new Rotate(new Angle(45,AngleUnit.Degree));
            content=Node();content.style.scale=new Scale(new Vector3(1,1/VerticalScale,1));
            anchor.Add(rotation);rotation.Add(content);
            return anchor;
        }
    }
}
