using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;

namespace CityForgeV3.Tests
{
    public sealed class OldChurchTripoPackageTests
    {
        private const string ChurchId =
            "cityforge.v3.civics.culture.old_church_tripo_01";

        [Test]
        public void OldChurchAppearsInTheCivicsBuildingLibrary()
        {
            HybridBuildingPackageRegistry.InvalidateCache();

            var church = BuildingCatalog.ForUseCategory(BuildingUseCategory.Civics)
                .Single(entry => entry.Id == ChurchId);

            Assert.That(church.Name, Is.EqualTo("Old Church"));
            Assert.That(church.Subcategory, Is.EqualTo("Culture"));
        }
    }
}
