using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CityForgeV3.World;
namespace CityForgeV3.Tests.EditMode
{
 public class DistrictAfternoonLightingTests
 {
  [Test] public void NoonCastsShortNortheastShadows()
  {
   var noon=TimeOfDayLighting.SunRotation(TimeOfDayPreset.Noon)*Vector3.forward;
   var afternoon=TimeOfDayLighting.SunRotation(TimeOfDayPreset.Afternoon)*Vector3.forward;
   Assert.Greater(noon.x,0);Assert.Greater(noon.z,0);Assert.Less(noon.y,0);
   Assert.Less(new Vector2(noon.x,noon.z).magnitude/-noon.y,new Vector2(afternoon.x,afternoon.z).magnitude/-afternoon.y);
   Assert.Greater(Vector3.Dot(new Vector3(-1,0,0),-noon),0,"Storefront-facing west wall receives direct light.");
  }
  [TestCase(TimeOfDayPreset.Noon)] [TestCase(TimeOfDayPreset.Afternoon)] public void DaylightSuppressesSceneSunAndRestoresItForNightAndShutdown(TimeOfDayPreset preset)
  {
   var sceneSun=new GameObject("Unmanaged scene sun").AddComponent<Light>();sceneSun.type=LightType.Directional;
   var root=new GameObject("District light ownership");
   try {var district=root.AddComponent<DistrictWorldController>();district.SetTimeOfDay(preset);Assert.IsFalse(sceneSun.enabled);district.SetTimeOfDay(TimeOfDayPreset.Night);Assert.IsTrue(sceneSun.enabled);district.SetTimeOfDay(preset);typeof(DistrictWorldController).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(district,null);Assert.IsTrue(sceneSun.enabled);}
   finally {Object.DestroyImmediate(root);Object.DestroyImmediate(sceneSun.gameObject);}
  }
  [TestCase(TimeOfDayPreset.Noon)] [TestCase(TimeOfDayPreset.Afternoon)] public void HostedSunTravelsAlongTheProjectedShadowRay(TimeOfDayPreset preset)
  {
   var root=new GameObject("Afternoon lighting regression");
   try {
    var world=root.AddComponent<LotWorldController>();world.Build();
    const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    var camera=(Camera)typeof(LotWorldController).GetField("_camera",flags).GetValue(world);
    var sun=(Light)typeof(LotWorldController).GetField("_sun",flags).GetValue(world);
    world.ConfigureAsDistrictHosted(camera,sun,world.ZoomLevel);
    world.SetTimeOfDay(preset);
    var ray=(Vector3)typeof(LotWorldController).GetMethod("ProjectedObjectShadowRay",flags).Invoke(world,null);
    Assert.That(Vector3.Angle(sun.transform.forward,ray),Is.LessThan(.01f),"The sun must illuminate the wall opposite the direction its ground shadow travels.");
    var awayWall=new Vector3(ray.x,0,ray.z).normalized;
    Assert.That(Vector3.Dot(awayWall,-sun.transform.forward),Is.LessThan(0),"The wall facing along the shadow must receive no direct sunlight.");
   } finally {Object.DestroyImmediate(root);}
  }
 }
}
