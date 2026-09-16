using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class LotPlacementCameraContinuityTests
{
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void PlacementThenPanKeepsCurrentCameraBasis(bool nativeBuilding, bool handPan)
    {
        var root = new GameObject("Placement camera continuity");
        try
        {
            var world = root.AddComponent<LotWorldController>();
            world.Build();
            world.ConfigureLot("Camera continuity fixture", LotType.Mixed, 6, 6);
            var camera = root.GetComponentInChildren<Camera>();
            camera.transform.rotation = Quaternion.Euler(27f, 123f, 0f);
            var before = world.CaptureCameraFraming();
            Assert.IsTrue(nativeBuilding
                ? world.BeginExperimentalBuilding3DPlacement(LotWorldController.PlymouthStoreProductionId)
                : world.BeginBuildingPlacementAtCenter(BuildingCatalog.NewEnglandHouseId));
            var placed = world.CaptureCameraFraming();
            Assert.That(Quaternion.Angle(before.Rotation, placed.Rotation), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(before.Position, placed.Position), Is.LessThan(.001f));

            var oldPan = world.CameraPanWorld;
            if (handPan) world.PanCameraViewport(new Vector2(12f, -8f), new Vector2(1280f, 720f));
            else world.PanCameraViewport(1, 1);
            var after = world.CaptureCameraFraming();
            Assert.That(Quaternion.Angle(before.Rotation, after.Rotation), Is.LessThan(.001f),
                "Pan must not switch to the new building's default camera angle.");
            Assert.That(Vector3.Distance(after.Position - before.Position,
                world.CameraPanWorld - oldPan), Is.LessThan(.001f), "Pan should only translate by its clamped offset.");
            Assert.That(after.OrthographicSize, Is.EqualTo(before.OrthographicSize).Within(.001f));
            Assert.That(Mathf.Abs(Vector3.Dot(after.Position - before.Position,
                before.Rotation * Vector3.forward)), Is.LessThan(.001f));
        }
        finally { Object.DestroyImmediate(root); }
    }
}
