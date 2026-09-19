using System.Linq;
using System.Reflection;
using CityForgeV3.World;
using CityForgeV3.UI;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class LotConnectorTests
    {
        [Test]
        public void DirtEntryIsQuarterTileOutsideAndSupportsCampTraffic()
        {
            var definition = LotWorldController.ResolveConnector(
                LotWorldController.DirtEntryConnectorId);
            Assert.That(definition.WidthMeters, Is.EqualTo(5f));
            Assert.That(definition.LengthMeters, Is.EqualTo(5f));
            Assert.That(LotWorldController.ConnectorOutsideExtensionMeters,
                Is.EqualTo(2.5f));
            Assert.That(definition.AllowsPedestrians, Is.True);
            Assert.That(definition.AllowsVehicles, Is.True);
            Assert.That(CityForgeApp.ShouldPrioritizeToolPlacement(
                LotEditorCategory.Connectors, "", ""), Is.True,
                "Connector clicks must not fall through to object selection.");
        }

        [TestCase(LotConnectorEdge.North, 20f, 0f, 12.5f, 0f, 17.5f)]
        [TestCase(LotConnectorEdge.East, 15f, 17.5f, 0f, 22.5f, 0f)]
        [TestCase(LotConnectorEdge.South, 20f, 0f, -12.5f, 0f, -17.5f)]
        [TestCase(LotConnectorEdge.West, 15f, -17.5f, 0f, -22.5f, 0f)]
        public void AccessContractCrossesSelectedLotEdge(
            LotConnectorEdge edge, float offset, float insideX, float insideZ,
            float outsideX, float outsideZ)
        {
            var connector = new PlacedLotConnector
            {
                Edge = edge,
                OffsetMeters = offset,
                AllowsPedestrians = true,
                AllowsVehicles = true
            };
            var access = LotWorldController.ConnectorAccess(connector, 40, 30);
            Assert.That(access.Inside.x, Is.EqualTo(insideX).Within(.001f));
            Assert.That(access.Inside.y, Is.EqualTo(insideZ).Within(.001f));
            Assert.That(access.Outside.x, Is.EqualTo(outsideX).Within(.001f));
            Assert.That(access.Outside.y, Is.EqualTo(outsideZ).Within(.001f));
            Assert.That(access.AllowsPedestrians, Is.True);
            Assert.That(access.AllowsVehicles, Is.True);
        }

        [Test]
        public void ConnectorSurvivesLotJsonRoundTrip()
        {
            var source = new LotSaveData { LotWidthCells = 4, LotDepthCells = 3 };
            source.Connectors.Add(new PlacedLotConnector
            {
                InstanceId = "camp-entry",
                ConnectorId = LotWorldController.DirtEntryConnectorId,
                Edge = LotConnectorEdge.South,
                OffsetMeters = 12.5f,
                AllowsPedestrians = true,
                AllowsVehicles = true
            });
            var restored = JsonUtility.FromJson<LotSaveData>(
                JsonUtility.ToJson(source));
            Assert.That(restored.Connectors.Count, Is.EqualTo(1));
            Assert.That(restored.Connectors[0].InstanceId,
                Is.EqualTo("camp-entry"));
            Assert.That(restored.Connectors[0].ConnectorId,
                Is.EqualTo(LotWorldController.DirtEntryConnectorId));
            Assert.That(restored.Connectors[0].Edge,
                Is.EqualTo(LotConnectorEdge.South));
            Assert.That(restored.Connectors[0].OffsetMeters,
                Is.EqualTo(12.5f));
            var reference = LotObjectRegistry.Read(restored)
                .Single(item => item.Id == "camp-entry");
            Assert.That(reference.Name,
                Is.EqualTo(LotWorldController.DirtEntryConnectorId));
            Assert.That(reference.HasPosition, Is.True);
            Assert.That(reference.Position.z, Is.EqualTo(-12.5f).Within(.001f));
        }

        [TestCase(0f, 0f, false)]
        [TestCase(0f, 10f, true)]
        [TestCase(0f, 12.5f, true)]
        [TestCase(0f, 13f, false)]
        [TestCase(20f, 0f, true)]
        public void PlacementRequiresTheLotEdgeOrQuarterTileApron(
            float x, float z, bool expected)
        {
            Assert.That(LotWorldController.PointNearConnectorEdge(
                new Vector3(x, 0f, z), 40f, 20f), Is.EqualTo(expected));
        }

        [Test]
        public void RuntimePresentationExtendsBeyondLotWithoutExpandingGround()
        {
            var host = new GameObject("Isolated connector presentation");
            host.SetActive(false);
            try
            {
                var world = host.AddComponent<LotWorldController>();
                var lot = new LotSaveData
                {
                    LotWidthCells = 4,
                    LotDepthCells = 3
                };
                lot.Connectors.Add(new PlacedLotConnector
                {
                    InstanceId = "north-entry",
                    ConnectorId = LotWorldController.DirtEntryConnectorId,
                    Edge = LotConnectorEdge.North,
                    OffsetMeters = 20f
                });
                world.Session.Restore(JsonUtility.ToJson(lot));
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(LotWorldController).GetMethod("BuildConnectorRoot", flags)
                    .Invoke(world, null);
                typeof(LotWorldController).GetMethod(
                        "RebuildConnectorPresentations", flags)
                    .Invoke(world, null);
                var connector = host.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(item => item.name == "Connector — Dirt Entry");
                Assert.That(connector, Is.Not.Null);
                Assert.That(connector.localPosition.z, Is.EqualTo(15f).Within(.001f));
                Assert.That(connector.localScale.x, Is.EqualTo(5f).Within(.001f));
                Assert.That(connector.localScale.y, Is.EqualTo(5f).Within(.001f));
                Assert.That(world.LotDepthMeters, Is.EqualTo(30f),
                    "The connector must not enlarge the authored lot footprint.");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
