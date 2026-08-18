using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class NewEnglandHouseAngleCandidateTests
    {
        private const string PackagePath =
            "CityForgeV3/Buildings/NewEnglandHouse1720V33Candidate/building-package";

        [Test]
        public void CandidateLoadsEveryRegisteredLayerAtBostonRelativeAngles()
        {
            var package = HybridBuildingPackageRegistry.Load(PackagePath);
            var expectedIds = new[] { "front-right", "rear-right", "rear-left", "front-left" };
            var expectedAzimuths = new[] { -70f, 20f, 110f, -160f };

            Assert.That(package.Id, Is.EqualTo(
                "cityforge.v3.residential.new_england_house_1720_angle_candidate_v33"));
            Assert.That(package.FacingCount, Is.EqualTo(4));
            Assert.That(package.FrontFacingQuarterTurns, Is.EqualTo(2));
            Assert.That(package.PlacementScale, Is.EqualTo(1f));
            Assert.That(Resources.Load<GameObject>(package.PrimitiveResourcePath), Is.Not.Null);

            for (var index = 0; index < 4; index++)
            {
                var facing = package.Facing(index);
                Assert.That(facing.Id, Is.EqualTo(expectedIds[index]));
                Assert.That(facing.CameraAzimuthDegrees,
                    Is.EqualTo(expectedAzimuths[index]).Within(0.001f));
                Assert.That(Resources.Load<Texture2D>(facing.NeutralResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.NightOverlayResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.WinterResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.MorningShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.NoonShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.AfternoonShadeResourcePath), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>(facing.EveningShadeResourcePath), Is.Not.Null);
                Assert.That(facing.SourcePivotTopOrigin.x, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(facing.SourcePivotTopOrigin.y, Is.EqualTo(0.7116016f).Within(0.0001f));
            }

            Assert.That(package.Facing(4).Id, Is.EqualTo(package.Facing(0).Id));
        }
    }
}
