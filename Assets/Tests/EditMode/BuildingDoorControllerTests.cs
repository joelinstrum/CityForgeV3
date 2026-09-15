using NUnit.Framework;
using UnityEngine;
using CityForgeV3.Buildings3D;
using CityForgeV3.World;
namespace CityForgeV3.Tests.EditMode
{
 public class BuildingDoorControllerTests
 {
  GameObject root; Transform hinge; BuildingDoorController door;
  [SetUp] public void Setup(){root=new GameObject("test");hinge=new GameObject("hinge").transform;hinge.SetParent(root.transform);hinge.localPosition=new Vector3(-.615f,.495f,2.86f);hinge.localRotation=Quaternion.Euler(0,17,0);door=root.AddComponent<BuildingDoorController>();door.Configure(hinge);}
  [TearDown] public void Teardown(){Object.DestroyImmediate(root);}
  [Test] public void OpensAndClosesWithoutMovingPivot(){var position=hinge.localPosition;var closed=hinge.localRotation;door.SetOpen(true);door.Advance(1);Assert.That(Quaternion.Angle(closed,hinge.localRotation),Is.EqualTo(90).Within(.001));Assert.That(hinge.localPosition,Is.EqualTo(position));door.SetOpen(false);door.Advance(1);Assert.That(Quaternion.Angle(closed,hinge.localRotation),Is.LessThan(.001));}
  [Test] public void ReversesSmoothlyDuringOpening(){door.SetOpen(true);door.Advance(.4f);var rotation=hinge.localRotation;door.SetOpen(false);Assert.That(Quaternion.Angle(rotation,hinge.localRotation),Is.LessThan(.001));door.Advance(.2f);Assert.That(door.OpenAmount,Is.EqualTo(.2f).Within(.001));door.Advance(1);Assert.That(door.OpenAmount,Is.Zero);}
  [Test] public void ShadowFollowerCopiesDoorAngle(){var copy=new GameObject("shadow");try{var pivot=new GameObject("hinge").transform;pivot.SetParent(copy.transform);pivot.localRotation=hinge.localRotation;var follower=copy.AddComponent<BuildingDoorController>();follower.Configure(pivot);door.SetOpen(true);door.Advance(.6f);follower.Follow(door);Assert.That(follower.OpenAmount,Is.EqualTo(.6f).Within(.001));Assert.That(Quaternion.Angle(hinge.localRotation,pivot.localRotation),Is.LessThan(.001));}finally{Object.DestroyImmediate(copy);}}
  [Test] public void DoorStateRoundTripsAndOldLotsDefaultClosed(){var data=new PlacedBuilding3D{AssetId="dry-goods-v05",DoorOpen=true};Assert.IsTrue(JsonUtility.FromJson<PlacedBuilding3D>(JsonUtility.ToJson(data)).DoorOpen);Assert.IsFalse(JsonUtility.FromJson<PlacedBuilding3D>("{\"AssetId\":\"older\"}").DoorOpen);}
 }
}
