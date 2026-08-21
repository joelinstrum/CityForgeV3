using System.IO;
using NUnit.Framework;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class WinterSnowfallFeatureTests
    {
        [Test]
        public void WinterSnowfall_HasTenSecondDurationAndPersistentAccumulation()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Snow.cs");

            StringAssert.Contains("WinterSnowfallDurationSeconds = 10f", source);
            StringAssert.Contains("EnsureSnowGroundCover", source);
            StringAssert.Contains("StartWinterSnowfall", source);
            StringAssert.Contains("_snowAccumulation = Season", source);
            StringAssert.Contains("Mathf.Lerp(0.01f, 1f, stormProgress)", source);
            StringAssert.Contains("material.SetFloat(\"_Accumulation\", opacity)", source);
            Assert.That(File.Exists(
                "Assets/CityForgeV3/Resources/CityForgeV3/Shaders/SnowAccumulation.shader"),
                Is.True);
        }

        [Test]
        public void WinterSnowfall_UsesSmallVariedDenseFlakes()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/World/LotWorldController.Snow.cs");

            StringAssert.Contains("MinMaxCurve(0.035f, 0.09f)", source);
            StringAssert.Contains("MinMaxGradient", source);
            StringAssert.Contains("0.08f", source);
            StringAssert.Contains("emission.rateOverTime = 760f", source);
            StringAssert.Contains("main.maxParticles = 8000", source);
        }

        [Test]
        public void SeasonsPanel_OnlyOffersSnowfallDuringWinter()
        {
            var source = File.ReadAllText(
                "Assets/CityForgeV3/Runtime/UI/CityForgeApp.cs");

            StringAssert.Contains("SeasonPreset.Winter", source);
            StringAssert.Contains("CanStartWinterSnowfall", source);
            StringAssert.Contains("StartWinterSnowfall", source);
            StringAssert.Contains("❄", source);
        }
    }
}
