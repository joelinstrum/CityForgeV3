using CityForgeV3.Buildings3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class Building3DPackageTests
    {
        private const string PackagePath =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/BrownstoneProduction/BrownstoneProduction.asset";
        private const string PrefabPath =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/BrownstoneProduction/Prefabs/BrownstoneProduction.prefab";

        [Test]
        public void SixLevelSchemaPreservesOldValuesAndAddsMeshAndBillboardLevels()
        {
            Assert.That((int)Building3DLevel.LOD0, Is.Zero);
            Assert.That((int)Building3DLevel.LOD3, Is.EqualTo(3));
            Assert.That((int)Building3DLevel.LOD4, Is.EqualTo(4));
            Assert.That((int)Building3DLevel.LOD5Billboard, Is.EqualTo(5));
            Assert.That(Building3DPackage.CurrentSchemaVersion, Is.EqualTo(2));
        }

        [TestCase(0f, 0)]
        [TestCase(44f, 1)]
        [TestCase(91f, 2)]
        [TestCase(179f, 4)]
        [TestCase(-91f, 6)]
        [TestCase(-44f, 7)]
        public void EightAngleBillboardSelectsNearestWrappedView(
            float degrees, int expected)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Sin(radians), 0f,
                Mathf.Cos(radians));
            Assert.That(EightAngleBuildingBillboard.CalculateAngleIndex(
                direction), Is.EqualTo(expected));
        }

        [Test]
        public void DownloadsEvaluationBuildingsAreGroundRegisteredLod0Packages()
        {
            foreach (var folder in new[]
                     {
                         "NYBrownstoneLight", "NYBrownstoneBay",
                         "NYFancyTownhouse", "NYBrownstone",
                         "BrooklynTownhomeRow"
                     })
            {
                var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                    $"Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/" +
                    $"Evaluation/{folder}/{folder}Evaluation.asset");
                Assert.That(package, Is.Not.Null, folder);
                Assert.That(package.SchemaVersion,
                    Is.EqualTo(Building3DPackage.CurrentSchemaVersion), folder);
                Assert.That(package.Representations, Has.Count.EqualTo(1), folder);
                Assert.That(package.Representations[0].Level,
                    Is.EqualTo(Building3DLevel.LOD0), folder);
                Assert.That(package.Representations[0].VisualPrefab,
                    Is.Not.Null, folder);
                Assert.That(package.AuthoredScale.x, Is.GreaterThan(0f), folder);
                Assert.That(package.FootprintMeters.x, Is.GreaterThan(0f), folder);
                Assert.That(package.FootprintMeters.y, Is.GreaterThan(0f), folder);
                if (folder.StartsWith("NY"))
                {
                    var material = package.Representations[0].OverrideMaterial;
                    Assert.That(material, Is.Not.Null, folder);
                    Assert.That(material.shader.name,
                        Is.EqualTo("CityForgeV3/Experimental3DBuildingPBR"),
                        folder);
                    Assert.That(material.GetFloat("_Saturation"),
                        Is.EqualTo(1.72f).Within(0.001f), folder);
                }
            }
        }

        [Test]
        public void MixedUseBrickEvaluationPreservesLod0AndSuppliesFullLodChain()
        {
            const string packagePath =
                "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/Evaluation/MixedUseBrick/MixedUseBrickEvaluation.asset";
            const string prefabPath =
                "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/Evaluation/MixedUseBrick/Prefabs/MixedUseBrickEvaluation.prefab";
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                packagePath);

            Assert.That(package, Is.Not.Null);
            Assert.That(package.AssetId, Is.EqualTo("mixed-use-brick-eval-v01"));
            Assert.That(package.Representations, Has.Count.EqualTo(6));
            Assert.That(package.AuthoredScale.y, Is.GreaterThan(0f));
            Assert.That(package.FootprintMeters.x, Is.GreaterThan(0f));
            Assert.That(package.FootprintMeters.y, Is.GreaterThan(0f));

            var previousTriangles = int.MaxValue;
            for (var index = 0; index < 5; index++)
            {
                var representation = package.Representations[index];
                Assert.That(representation.Level,
                    Is.EqualTo((Building3DLevel)index));
                Assert.That(representation.VisualPrefab, Is.Not.Null);
                Assert.That(representation.OverrideMaterial, Is.Not.Null);
                Assert.That(representation.TargetTriangleBudget,
                    Is.LessThan(previousTriangles));
                previousTriangles = representation.TargetTriangleBudget;
            }

            Assert.That(AssetDatabase.GetAssetPath(
                    package.Representations[0].VisualPrefab),
                Does.Contain("/MixedUseBrick/Source/"),
                "LOD0 must remain the supplied source FBX, not a decimated derivative.");
            Assert.That(package.Representations[0].TargetTriangleBudget,
                Is.EqualTo(30588));

            var billboard = package.Representations[5];
            Assert.That(billboard.Level,
                Is.EqualTo(Building3DLevel.LOD5Billboard));
            Assert.That(billboard.VisualPrefab, Is.Not.Null);
            Assert.That(billboard.BillboardAngleCount, Is.EqualTo(8));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),
                Is.Not.Null);
        }

        [Test]
        public void BrownstonePilotPreservesSourceAndRegistersItOnlyAsLod2()
        {
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(
                PackagePath);
            Assert.That(package, Is.Not.Null);
            Assert.That(package.AssetId, Is.EqualTo("brownstone-production-v01"));
            Assert.That(package.Representations, Has.Count.EqualTo(1));
            Assert.That(package.Representations[0].Level,
                Is.EqualTo(Building3DLevel.LOD2));
            Assert.That(package.Representations[0].VisualPrefab, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(
                    package.Representations[0].VisualPrefab),
                Does.EndWith("BrownstoneBuilding22k/brownstone-building-22k.fbx"));
            Assert.That(package.Representations[0].ShadowPrefab, Is.Null,
                "A separate authored shadow LOD must not be faked by duplicating the beauty mesh in package metadata.");
        }

        [Test]
        public void PilotPrefabUsesCrossFadingLodGroupAndStableTransform()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            try
            {
                var packageInstance = instance.GetComponent<Building3DPackageInstance>();
                Assert.That(packageInstance, Is.Not.Null);
                Assert.That(packageInstance.LodGroup, Is.Not.Null);
                Assert.That(packageInstance.LodGroup.fadeMode,
                    Is.EqualTo(LODFadeMode.CrossFade));
                Assert.That(packageInstance.LodGroup.GetLODs(), Has.Length.EqualTo(1));
                Assert.That(instance.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void ArtMuseumUsesFourAuthoredVisualLodsAndCheaperShadowLods()
        {
            const string path =
                "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/ArtMuseumProduction/ArtMuseumProduction.asset";
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(path);
            Assert.That(package, Is.Not.Null);
            Assert.That(package.AssetId, Is.EqualTo("art-museum-production-v01"));
            Assert.That(package.Representations, Has.Count.EqualTo(4));
            Assert.That(package.AuthoredScale, Is.EqualTo(Vector3.one * 40f));
            for (var index = 0; index < 4; index++)
            {
                Assert.That(package.Representations[index].Level,
                    Is.EqualTo((Building3DLevel)index));
                Assert.That(package.Representations[index].VisualPrefab, Is.Not.Null);
                Assert.That(package.Representations[index].OverrideMaterial,
                    Is.Not.Null);
                Assert.That(package.Representations[index].OverrideMaterial
                    .GetTexture("_MainTex"), Is.Not.Null,
                    $"LOD{index} must explicitly bind its supplied base-color texture.");
                Assert.That(package.Representations[index].OverrideMaterial
                    .GetFloat("_Metallic"), Is.Zero,
                    "Stone must not inherit the FBX's incorrectly packed metallic interpretation.");
                Assert.That(package.Representations[index].ShadowPrefab, Is.Not.Null);
            }
            Assert.That(package.Representations[0].ShadowPrefab,
                Is.SameAs(package.Representations[2].VisualPrefab));
            Assert.That(package.Representations[3].ShadowPrefab,
                Is.SameAs(package.Representations[3].VisualPrefab));
        }

        [Test]
        public void PlymouthStoreUsesFourTexturedAuthoredLods()
        {
            const string path =
                "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/PlymouthStoreProduction/PlymouthStoreComparisonV01.asset";
            var package = AssetDatabase.LoadAssetAtPath<Building3DPackage>(path);
            Assert.That(package, Is.Not.Null);
            Assert.That(package.AssetId,
                Is.EqualTo("plymouth-store-v01"));
            Assert.That(package.Representations, Has.Count.EqualTo(4));
            for (var index = 0; index < 4; index++)
            {
                var representation = package.Representations[index];
                Assert.That(representation.Level,
                    Is.EqualTo((Building3DLevel)index));
                Assert.That(representation.VisualPrefab, Is.Not.Null);
                Assert.That(representation.OverrideMaterial, Is.Not.Null);
                Assert.That(representation.OverrideMaterial.GetTexture("_MainTex"),
                    Is.Not.Null);
                Assert.That(representation.OverrideMaterial.GetTexture("_BumpMap"),
                    Is.Not.Null);
                Assert.That(representation.LocalPosition, Is.EqualTo(Vector3.zero));
                Assert.That(representation.LocalEulerAngles, Is.EqualTo(Vector3.zero));
                Assert.That(representation.LocalScale, Is.EqualTo(Vector3.one));
            }
            Assert.That(package.Representations[0].LocalScale.x,
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(package.Representations[1].LocalScale.x,
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(package.UseCrossFade, Is.False,
                "Plymouth's independently exported FBXs require discrete LOD switches.");
        }

    }
}
