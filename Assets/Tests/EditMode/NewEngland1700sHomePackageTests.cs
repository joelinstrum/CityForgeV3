using CityForgeV3.World;
using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class NewEngland1700sHomePackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/NewEngland1700sHomeV03/building-package";

        [Test]
        public void DaytimePackageLoadsCompleteRegisteredResourceMatrix()
        {
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            var expectedIds = new[] { "front-right", "rear-right", "rear-left", "front-left" };
            var expectedAzimuths = new[] { -55.5f, 34.5f, 124.5f, -145.5f };

            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.residential.new_england_1700s_home_01"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.OccupancyWidth, Is.EqualTo(2));
            Assert.That(package.OccupancyDepth, Is.EqualTo(1));
            Assert.That(package.PlacementScale, Is.EqualTo(1f));
            Assert.That(Resources.Load<GameObject>(package.PrimitiveResourcePath), Is.Not.Null);

            for (var index = 0; index < 4; index++)
            {
                var facing = package.Facing(index);
                Assert.That(facing.Id, Is.EqualTo(expectedIds[index]));
                Assert.That(facing.CameraAzimuthDegrees,
                    Is.EqualTo(expectedAzimuths[index]).Within(0.001f));
                Assert.That(Resources.Load<Texture2D>(facing.NeutralResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.MorningShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.NoonShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.AfternoonShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.EveningShadeResourcePath), Is.Not.Null);

                var night = Resources.Load<Texture2D>(facing.NightOverlayResourcePath);
                Assert.That(night, Is.Not.Null);
                var sourcePath = AssetDatabase.GetAssetPath(night);
                var readableNight = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(ImageConversion.LoadImage(readableNight, File.ReadAllBytes(sourcePath)), Is.True);
                foreach (var pixel in readableNight.GetPixels32())
                    Assert.That(pixel.a, Is.Zero, $"{facing.Id} night overlay must remain transparent");
                Object.DestroyImmediate(readableNight);

                Assert.That(facing.SourcePivotTopOrigin.x, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(facing.SourcePivotTopOrigin.y, Is.EqualTo(0.721473f).Within(0.0001f));
            }

            Assert.That(package.Facing(4).Id, Is.EqualTo(package.Facing(0).Id));
            Assert.That(package.Facing(8).Id, Is.EqualTo(package.Facing(0).Id));
        }
    }
}
