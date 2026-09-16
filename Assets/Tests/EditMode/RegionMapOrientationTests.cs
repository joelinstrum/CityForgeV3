using System.Collections;
using System.Reflection;
using CityForgeV3.World;
using CityForgeV3.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class RegionMapOrientationTests
{
    [UnityTest]
    public IEnumerator RegionAxesAndLabelsMatchActualDistrictCamera()
    {
        var window=ScriptableObject.CreateInstance<EditorWindow>();
        var worldObject=new GameObject("Orientation test world");
        try
        {
            window.position=new Rect(100,100,400,400);window.ShowUtility();
            var root=window.rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("CityForgeV3/UI/CityForgeV3"));
            var projection=new VisualElement();projection.AddToClassList("region-map-projection");
            projection.style.width=300;projection.style.height=300;root.Add(projection);
            var plane=new VisualElement();plane.AddToClassList("region-map-plane");
            plane.style.width=200;plane.style.height=100;projection.Add(plane);
            RegionMapProjection.ApplyGround(plane,200,100);
            var anchor=RegionMapProjection.CreateLabelAnchor(200,100,out var content);plane.Add(anchor);
            var label=new Label("North River");label.AddToClassList("region-road-map-label");content.Add(label);
            var world=worldObject.AddComponent<DistrictWorldController>();
            var camera=worldObject.AddComponent<Camera>();camera.enabled=false;
            typeof(DistrictWorldController).GetField("_camera",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(world,camera);
            typeof(DistrictWorldController).GetMethod("ApplyCameraPose",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(world,null);
            yield return null;yield return null;
            // A 4-by-2 district inside a rectangular region must match its physical
            // camera projection up to a single uniform zoom (including river points).
            var center=plane.LocalToWorld(new Vector2(100,50));
            var x=plane.LocalToWorld(new Vector2(101,50))-center;
            var viewX=camera.transform.InverseTransformDirection(Vector3.right);
            float zoom=x.x/viewX.x;
            foreach(var point in new[]{new Vector2(0,0),new Vector2(200,0),new Vector2(200,100),new Vector2(0,100),new Vector2(45,65),new Vector2(130,30)})
            {
                var delta=point-new Vector2(100,50);
                var view=camera.transform.InverseTransformDirection(new Vector3(delta.x,0,delta.y));
                var expected=new Vector2(view.x,-view.y)*zoom;
                Assert.Less(Vector2.Distance(plane.LocalToWorld(point)-center,expected),.01f,
                    "Region geometry is stretched relative to the district camera");
            }
            var origin=plane.LocalToWorld(new Vector2(100,100));
            foreach(var pair in new[]{(Vector2.right,Vector3.right),(Vector2.up,Vector3.forward)})
            {
                var mapDelta=plane.LocalToWorld(new Vector2(100,100)+pair.Item1*20)-origin;
                var view=camera.transform.InverseTransformDirection(pair.Item2);
                Assert.AreEqual(Mathf.Sign(view.x),Mathf.Sign(mapDelta.x),"Horizontal direction differs between views");
                Assert.AreEqual(Mathf.Sign(-view.y),Mathf.Sign(mapDelta.y),"Vertical direction differs between views");
                // Real pointer conversion must still recover the same district coordinates.
                var local=plane.WorldToLocal(origin+mapDelta);
                Assert.Less(Vector2.Distance(local,new Vector2(100,100)+pair.Item1*20),.001f);
            }
            var m=label.worldTransform;
            Assert.That(m.m00,Is.EqualTo(1).Within(.001f),"Label width is distorted");
            Assert.That(m.m11,Is.EqualTo(1).Within(.001f),"Label height is distorted");
            Assert.That(m.m01,Is.EqualTo(0).Within(.001f),"Label vertical axis is tilted");
            Assert.That(m.m10,Is.EqualTo(0).Within(.001f),"Label baseline is tilted");
        }
        finally
        {
            window.Close();Object.DestroyImmediate(worldObject);
        }
    }
}
