using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class AgricultureFloraTests
    {
        [TestCase(SeasonPreset.Spring)]
        [TestCase(SeasonPreset.Summer)]
        [TestCase(SeasonPreset.Autumn)]
        [TestCase(SeasonPreset.Winter)]
        public void CornField_UsesItsSeasonalAgricultureArtwork(
            SeasonPreset season)
        {
            var expected = "CityForgeV3/Flora/AgricultureV01/corn-field-" +
                season.ToString().ToLowerInvariant();

            Assert.That(LotWorldController.ResolveFloraResourcePath(
                "corn-field", season), Is.EqualTo(expected));
            Assert.That(Resources.Load<Texture2D>(expected), Is.Not.Null);
        }

        [Test]
        public void CornField_RepeatSpacingMatchesItsTwelveMeterFootprint()
        {
            Assert.That(LotWorldController.FloraLineSpacingMeters("corn-field"),
                Is.EqualTo(12f));
        }

        [TestCase("spring")]
        [TestCase("summer")]
        [TestCase("autumn")]
        public void CornField_SeasonalCanopyArtworkIsAvailable(string season)
        {
            Assert.That(Resources.Load<Texture2D>(
                $"CityForgeV3/Flora/AgricultureV01/corn-canopy-{season}"),
                Is.Not.Null);
        }

        [TestCase("corn-field-summer")]
        [TestCase("corn-canopy-summer")]
        public void CornField_LayersUseFullAlphaShapePicking(string textureName)
        {
            Assert.That(LotWorldController.UsesFoliageShapePicking(textureName),
                Is.True);
        }

        [Test]
        public void CornCanopy_IsDetailOnlyAndNeverShownInWinter()
        {
            Assert.That(LotWorldController.ShouldShowAgricultureCanopy(
                LotZoomLevel.Lot, SeasonPreset.Summer), Is.False);
            Assert.That(LotWorldController.ShouldShowAgricultureCanopy(
                LotZoomLevel.Detail, SeasonPreset.Summer), Is.True);
            Assert.That(LotWorldController.ShouldShowAgricultureCanopy(
                LotZoomLevel.Detail, SeasonPreset.Winter), Is.False);
        }
    }
}
