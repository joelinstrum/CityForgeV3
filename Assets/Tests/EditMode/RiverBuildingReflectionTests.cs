using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class RiverBuildingReflectionTests
{
    [Test]
    public void SawmillReflectionIsLocalAndOnlyRendersAtCloseZoom()
    {
        Assert.That(DistrictWorldController.AllowsRiverBuildingReflection(
            DistrictZoomLevel.LOD2), Is.True);
        Assert.That(DistrictWorldController.AllowsRiverBuildingReflection(
            DistrictZoomLevel.LOD3), Is.False);
        Assert.That(UnityEditor.ShaderUtil.ShaderHasError(
            Shader.Find("CityForgeV3/RiverWaterSurface")), Is.False);

        var owner = new GameObject("Sawmill river reflection fixture");
        var path = Path.Combine(Path.GetTempPath(), "cityforge-reflection-" +
            Guid.NewGuid().ToString("N") + ".json");
        var output = new RenderTexture(960, 600, 24);
        var previousActive = RenderTexture.active;
        try
        {
            var data = new LotSaveData
            {
                LotId = "sawmill-reflection-" + Guid.NewGuid().ToString("N"),
                Name = "River sawmill",
                LotType = LotType.Industrial,
                LotWidthCells = 4,
                LotDepthCells = 4,
                LotSizeMeters = 40
            };
            data.Buildings3D.Add(new PlacedBuilding3D
            {
                AssetId = "lumber-mill-v01"
            });
            File.WriteAllText(path, JsonUtility.ToJson(data));
            _ = LotContentCatalog.All;
            typeof(LotContentCatalog).GetMethod("Add",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
                new object[]
                {
                    new LotSaveSummary { LotId = data.LotId, Name = data.Name },
                    path, "", "reflection test", false, false,
                    null, null, true
                });
            var district = new RegionCityTile { Width = 1, Height = 1 };
            district.Rivers.Add(new PlacedDistrictRiver
            {
                InstanceId = "reflection-river",
                Direction = DistrictRiverDirection.WestToEast,
                Depth = DistrictRiverDepth.Deep,
                WidthMeters = 64f,
                Points = new List<DistrictRiverPoint>
                {
                    new(0f, .5f), new(1f, .5f)
                }
            });
            var world = owner.AddComponent<DistrictWorldController>();
            world.RebuildEntireDistrict(district,
                DistrictBulkRebuildReason.TestFixture);
            var placement = new PlacedDistrictLot
            {
                InstanceId = "reflection-sawmill",
                LotId = data.LotId,
                GridX = 32,
                GridZ = 33,
                ShoreOffsetZ = -4f
            };
            district.Lots.Add(placement);
            Assert.That(world.AddPlacedLot(district, placement,
                testPlacement: true), Is.True);
            foreach (var sequence in owner.GetComponentsInChildren<
                         BuildingConstructionSequence>())
                for (var stage = 0; stage < 20 && !sequence.IsComplete;
                     stage++)
                    sequence.AdvanceOneStageForQa();
            Assert.That(world.RiverBuildingReflectionCandidateCountForQa,
                Is.EqualTo(1));
            var camera = world.WorldCamera;
            camera.targetTexture = output;
            world.SetPan(new Vector2(5f, 25f));
            world.SetZoom(DistrictZoomLevel.LOD3);
            camera.Render();
            Assert.That(world.RiverBuildingReflectionRenderCountForQa,
                Is.EqualTo(0));
            world.SetZoom(DistrictZoomLevel.LOD2);
            camera.Render();
            Assert.That(world.RiverBuildingReflectionRenderCountForQa,
                Is.EqualTo(1));
            Assert.That(world.RiverBuildingReflectionActiveForQa, Is.True);
            Assert.That(Shader.GetGlobalFloat(
                "_CF_RiverBuildingReflectionEnabled"), Is.EqualTo(1f));
            var pinnedProjection = Shader.GetGlobalMatrix(
                "_CF_RiverBuildingReflectionVP");
            var capture = Shader.GetGlobalTexture(
                "_CF_RiverBuildingReflectionTex") as RenderTexture;
            Assert.That(capture, Is.Not.Null);
            var image = new Texture2D(capture.width, capture.height,
                TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = capture;
                image.ReadPixels(new Rect(0, 0, capture.width,
                    capture.height), 0, 0);
                image.Apply();
                Assert.That(image.GetPixels32().Any(pixel => pixel.a > 0),
                    Is.True, "The capture must contain the completed mill.");
            }
            finally
            {
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(image);
            }
            camera.Render();
            Assert.That(world.RiverBuildingReflectionRenderCountForQa,
                Is.EqualTo(1), "The second frame must reuse the capture.");
            world.RiverBuildingReflectionsEnabled = false;
            camera.Render();
            Assert.That(world.RiverBuildingReflectionRenderCountForQa,
                Is.EqualTo(1));
            Assert.That(world.RiverBuildingReflectionActiveForQa, Is.False);
            world.RiverBuildingReflectionsEnabled = true;
            world.SetPan(new Vector2(12f, 27f));
            camera.Render();
            Assert.That(world.RiverBuildingReflectionRenderCountForQa,
                Is.EqualTo(2));
            var afterPanProjection = Shader.GetGlobalMatrix(
                "_CF_RiverBuildingReflectionVP");
            for (var index = 0; index < 16; index++)
                Assert.That(afterPanProjection[index],
                    Is.EqualTo(pinnedProjection[index]).Within(.0001f),
                    "Panning must not slide the mill reflection on the river.");
            world.SetZoom(DistrictZoomLevel.LOD3);
            Assert.That(world.RiverBuildingReflectionActiveForQa, Is.False);
            Assert.That(Shader.GetGlobalFloat(
                "_CF_RiverBuildingReflectionEnabled"), Is.EqualTo(0f));
            placement.ShoreOffsetZ += 100f;
            Assert.That(world.UpdatePlacedLotTransform(district, placement,
                allowPlacementConflicts: true,
                deferSurfaceRefresh: true), Is.True);
            world.SetPan(new Vector2(20f, 126f));
            world.SetZoom(DistrictZoomLevel.LOD2);
            camera.Render();
            Assert.That(world.RiverBuildingReflectionRenderCountForQa,
                Is.EqualTo(2),
                "Moving the mill away from the river must not render it.");
        }
        finally
        {
            var camera = owner.GetComponentInChildren<Camera>();
            if (camera != null) camera.targetTexture = null;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(owner);
            File.Delete(path);
            LotContentCatalog.InvalidateCache();
        }
    }

    [Test]
    public void RiverReflectionCanBeOptedOutPerLot()
    {
        var owner = new GameObject("Mill reflection option fixture");
        try
        {
            var world = owner.AddComponent<LotWorldController>();
            world.Build();
            world.ConfigureLot("Mill reflection option", LotType.Industrial,
                4, 4);
            Assert.That(world.AddExperimentalBuilding3D("lumber-mill-v01",
                0f, 0f, 0), Is.True);
            Assert.That(world.RiverReflectionEnabled, Is.True);
            var roots = new List<Transform>();
            world.CollectNativeBuildingRoots("lumber-mill-v01", roots,
                reflectionEnabledOnly: true);
            Assert.That(roots, Has.Count.EqualTo(1));
            world.SetRiverReflectionEnabled(false);
            Assert.That(world.RiverReflectionEnabled, Is.False);
            roots.Clear();
            world.CollectNativeBuildingRoots("lumber-mill-v01", roots,
                reflectionEnabledOnly: true);
            Assert.That(roots, Is.Empty);
            var saved = JsonUtility.ToJson(new LotSaveData
            {
                RiverReflectionDisabled = true
            });
            Assert.That(JsonUtility.FromJson<LotSaveData>(saved)
                .RiverReflectionDisabled, Is.True);
            Assert.That(JsonUtility.FromJson<LotSaveData>("{}")
                .RiverReflectionDisabled, Is.False,
                "Older saved lots retain their reflection by default.");
            world.SetRiverReflectionEnabled(true);
            roots.Clear();
            world.CollectNativeBuildingRoots("lumber-mill-v01", roots,
                reflectionEnabledOnly: true);
            Assert.That(roots, Has.Count.EqualTo(1));
        }
        finally { Object.DestroyImmediate(owner); }
    }
}
