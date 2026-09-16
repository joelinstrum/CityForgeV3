using System;
using System.IO;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class LotSavedViewTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void SavedViewSurvivesFreshWorldLoadAndNextPan(bool topDown)
    {
        var directory = Path.Combine(Path.GetTempPath(), "CityForge-view-" + Guid.NewGuid().ToString("N"));
        var owner = new GameObject("Saved view fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.NewEmptyLot("Saved view fixture", LotType.Residential, 4, 4);
            // First placement preserves the empty-lot camera. Reload formerly
            // recalculated this from the native building and turned the lot.
            Assert.IsTrue(world.AddExperimentalBuilding3D("new-england-farmhouse-v02", 0, 0, 0));
            if (topDown) world.ToggleTopDownView();
            world.PanCameraViewport(1, -1);
            var expected = world.CaptureCameraFraming();
            var pan = world.CameraPanWorld;
            world.SaveLot(directory);
            var id = world.Session.Data.LotId;
            var rotation = world.Session.Data.Buildings3D[0].RotationQuarterTurns;
            Object.DestroyImmediate(owner);
            owner = new GameObject("Fresh world");
            world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.SetCameraOrbitOctant(5);
            Assert.IsTrue(world.LoadLot(id, directory));
            var actual = world.CaptureCameraFraming();
            Assert.That(Quaternion.Angle(expected.Rotation, actual.Rotation), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(expected.Position, actual.Position), Is.LessThan(.001f));
            Assert.That(actual.OrthographicSize, Is.EqualTo(expected.OrthographicSize).Within(.001f));
            Assert.That(Vector3.Distance(pan, world.CameraPanWorld), Is.LessThan(.001f));
            Assert.AreEqual(topDown, world.TopDownViewEnabled);
            Assert.AreEqual(rotation, world.Session.Data.Buildings3D[0].RotationQuarterTurns);
            world.PanCameraViewport(1, 1);
            Assert.That(Quaternion.Angle(expected.Rotation, world.CaptureCameraFraming().Rotation), Is.LessThan(.001f));
        }
        finally
        {
            Object.DestroyImmediate(owner);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void LegacyLotWithoutViewLoadsWithDeterministicDefault()
    {
        var directory = Path.Combine(Path.GetTempPath(), "CityForge-view-" + Guid.NewGuid().ToString("N"));
        var owner = new GameObject("Legacy view fixture");
        try
        {
            var session = new LotEditorSession();
            session.NewLot("Legacy view", LotType.Residential, 40);
            LotSaveStore.Save(session, Array.Empty<string>(), directory);
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.SetCameraOrbitOctant(5);
            world.ToggleTopDownView();
            Assert.IsTrue(world.LoadLot(session.Data.LotId, directory));
            Assert.IsFalse(world.TopDownViewEnabled);
            Assert.AreEqual(0, world.CameraOrbitOctant);
            Assert.AreEqual(Vector3.zero, world.CameraPanWorld);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
