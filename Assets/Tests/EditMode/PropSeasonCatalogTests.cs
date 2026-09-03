using CityForgeV3.World;
using NUnit.Framework;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class PropSeasonCatalogTests
    {
        [Test]
        public void Pumpkin_IsAvailableOnlyInAutumn()
        {
            var id = LotWorldController.PumpkinJackOLanternPropId;

            Assert.That(PropSeasonCatalog.SeasonFor(id), Is.EqualTo(PropSeason.Autumn));
            Assert.That(PropSeasonCatalog.IsAvailable(id, SeasonPreset.Autumn), Is.True);
            Assert.That(PropSeasonCatalog.IsAvailable(id, SeasonPreset.Spring), Is.False);
            Assert.That(PropSeasonCatalog.IsAvailable(id, SeasonPreset.Summer), Is.False);
            Assert.That(PropSeasonCatalog.IsAvailable(id, SeasonPreset.Winter), Is.False);
        }

        [Test]
        public void UnregisteredLegacyProp_RemainsAvailableAllYear()
        {
            const string legacyProp = "existing-unregistered-prop";

            Assert.That(PropSeasonCatalog.SeasonFor(legacyProp), Is.EqualTo(PropSeason.All));
            Assert.That(PropSeasonCatalog.IsAvailable(legacyProp, SeasonPreset.Spring), Is.True);
            Assert.That(PropSeasonCatalog.IsAvailable(legacyProp, SeasonPreset.Summer), Is.True);
            Assert.That(PropSeasonCatalog.IsAvailable(legacyProp, SeasonPreset.Autumn), Is.True);
            Assert.That(PropSeasonCatalog.IsAvailable(legacyProp, SeasonPreset.Winter), Is.True);
        }
    }
}
