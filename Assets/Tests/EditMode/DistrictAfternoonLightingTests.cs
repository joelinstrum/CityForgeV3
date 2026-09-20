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
  [TestCase(TimeOfDayPreset.Noon)] [TestCase(TimeOfDayPreset.Afternoon)]
  [TestCase(TimeOfDayPreset.Night)]
  public void HostedLotNeverRewritesTheDistrictSun(TimeOfDayPreset preset)
  {
   var root=new GameObject("Afternoon lighting regression");
   try {
    var world=root.AddComponent<LotWorldController>();world.Build();
    const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    var camera=(Camera)typeof(LotWorldController).GetField("_camera",flags).GetValue(world);
    var sun=(Light)typeof(LotWorldController).GetField("_sun",flags).GetValue(world);
    world.ConfigureAsDistrictHosted(camera,sun,world.ZoomLevel);
    var expectedRotation=Quaternion.Euler(17f,123f,4f);
    var expectedColor=new Color(.31f,.47f,.83f);
    sun.transform.rotation=expectedRotation;sun.color=expectedColor;
    sun.intensity=.731f;sun.shadowStrength=.619f;
    world.SetTimeOfDay(preset);
    Assert.That(Quaternion.Angle(sun.transform.rotation,expectedRotation),Is.LessThan(.001f));
    Assert.That(sun.color,Is.EqualTo(expectedColor));
    Assert.That(sun.intensity,Is.EqualTo(.731f).Within(.0001f));
    Assert.That(sun.shadowStrength,Is.EqualTo(.619f).Within(.0001f));
   } finally {Object.DestroyImmediate(root);}
  }
 }
}
