using CityForgeV3.Buildings3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class HitchcockMansion3DPackageTests
    {
        private const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/HitchcockMansionProduction";

        [Test]
        public void FullPackagePreservesSourceAndProvidesFiveMeshesAndBillboard()
        {
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                Root + "/HitchcockMansionV01.asset");
            Assert.That(package, Is.Not.Null);
            Assert.That(package.SchemaVersion,
                Is.EqualTo(Building3DPackage.CurrentSchemaVersion));
            Assert.That(package.AssetId, Is.EqualTo("hitchcock-mansion-v01"));
            Assert.That(package.Representations, Has.Count.EqualTo(6));
            Assert.That(package.CollisionPrefab, Is.Not.Null);

            var expectedTriangles = new[] { 44044, 40000, 20000, 15999, 12000 };
            for (var index = 0; index < 5; index++)
            {
                var representation = package.Representations[index];
                Assert.That(representation.Level,
                    Is.EqualTo((Building3DLevel)index));
                Assert.That(representation.VisualPrefab, Is.Not.Null);
                Assert.That(representation.OverrideMaterial, Is.Not.Null);
                Assert.That(representation.ShadowPrefab, Is.Not.Null);
                Assert.That(representation.TargetTriangleBudget,
                    Is.EqualTo(expectedTriangles[index]));
            }
            Assert.That(AssetDatabase.GetAssetPath(
                    package.Representations[0].VisualPrefab),
                Does.Contain("/HitchcockMansionProduction/Source/"));

            var billboard = package.Representations[5];
            Assert.That(billboard.Level,
                Is.EqualTo(Building3DLevel.LOD5Billboard));
            Assert.That(billboard.BillboardAngleCount, Is.EqualTo(8));
            Assert.That(billboard.VisualPrefab, Is.Not.Null);
            Assert.That(billboard.ShadowPrefab, Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(
                Root + "/Prefabs/HitchcockMansionV01.prefab"), Is.Not.Null);
        }
    }
}
