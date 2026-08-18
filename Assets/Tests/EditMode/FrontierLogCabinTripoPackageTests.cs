using CityForgeV3.World;
using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class FrontierLogCabinTripoPackageTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/FrontierLogCabinTripoV03/building-package";

        [Test]
        public void DaytimeCabinLoadsCompleteRegisteredResourceMatrix()
        {
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            var expectedIds = new[] { "front-right", "rear-right", "rear-left", "front-left" };

            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.residential.frontier_log_cabin_tripo_01"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.OccupancyWidth, Is.EqualTo(1));
            Assert.That(package.OccupancyDepth, Is.EqualTo(1));
            Assert.That(package.PlacementScale, Is.EqualTo(1f));
            Assert.That(package.PrimitiveSourceVersion, Does.Contain("V05 perceptual 6.42 x 5.54m"));
            var primitive = Resources.Load<GameObject>(package.PrimitiveResourcePath);
            Assert.That(primitive, Is.Not.Null);
            CollectionAssert.Contains(package.RequiredPrimitiveObjects,
                "CF_PROXY_ROOF_GABLE");
            CollectionAssert.DoesNotContain(package.RequiredPrimitiveObjects,
                "CF_PROXY_ROOF_FLAT");
            var gable = FindDescendant(primitive.transform,
                "CF_PROXY_ROOF_GABLE");
            Assert.That(gable, Is.Not.Null);
            Assert.That(gable.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3,
                Is.EqualTo(8), "The cabin roof occluder must be a triangular gable prism, not a twelve-triangle box.");

            for (var index = 0; index < 4; index++)
            {
                var facing = package.Facing(index);
                Assert.That(facing.Id, Is.EqualTo(expectedIds[index]));
                Assert.That(Resources.Load<Texture2D>(facing.NeutralResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.MorningShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.NoonShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.AfternoonShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.EveningShadeResourcePath), Is.Not.Null);

                var night = Resources.Load<Texture2D>(facing.NightOverlayResourcePath);
                Assert.That(night, Is.Not.Null);
                var readableNight = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(ImageConversion.LoadImage(readableNight,
                    File.ReadAllBytes(AssetDatabase.GetAssetPath(night))), Is.True);
                foreach (var pixel in readableNight.GetPixels32())
                    Assert.That(pixel.a, Is.Zero,
                        $"{facing.Id} night overlay must remain transparent");
                Object.DestroyImmediate(readableNight);

                Assert.That(facing.SourcePivotTopOrigin.x, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(facing.SourcePivotTopOrigin.y, Is.EqualTo(0.705653f).Within(0.0001f));
            }

            Assert.That(package.Facing(4).Id, Is.EqualTo(package.Facing(0).Id));
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDescendant(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
