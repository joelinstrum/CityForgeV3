#if UNITY_EDITOR
using CityForgeV3.World;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenFarmCabinQa(bool texturedGround = true)
        {
            EnsureLotWorld();
            _lotWorld.NewEmptyLot("Founders Farmhouse and Cabin", LotType.Residential, 5, 5);
            _lotWorld.AddExperimentalBuilding3D("founders-farm-cabin-v01", 0f, 0f, 0);
            if (texturedGround) _lotWorld.SetBaseTexture("grass-lush");
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _hasOpenLot = true;
            _lotEditorCategory = LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded = false;
            _lotStatus = "Founders Farmhouse & Cabin • $2,500 to build";
            Show(AppScreen.LotEditor);
            _lotWorld.SetQaOrthographicSize(14f);
        }
    }
}
#endif
