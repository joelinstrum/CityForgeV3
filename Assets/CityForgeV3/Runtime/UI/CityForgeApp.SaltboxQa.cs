#if UNITY_EDITOR
using CityForgeV3.World;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenSaltboxOriginalComparisonQa()
        {
            EnsureLotWorld();
            _lotWorld.NewEmptyLot("Cottage and Saltbox Comparison", LotType.Residential, 5, 5);
            _lotWorld.AddExperimentalBuilding3D("wooden-cottage-v01",2.2f,-6f,0);
            _lotWorld.AddExperimentalBuilding3D("new-england-saltbox-v05",-2.2f,6f,0);
            _lotWorld.SetBaseTexture("grass-lush");
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _hasOpenLot=true;
            _lotEditorCategory=LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded=false;
            _lotStatus="FRONT: wooden cottage • BACK: lit saltbox";
            Show(AppScreen.LotEditor);
            _lotWorld.SetQaOrthographicSize(17f);
        }
        public void OpenVictorianColorHouseQa()
        {
            EnsureLotWorld();
            _lotWorld.NewEmptyLot("Victorian Color Review", LotType.Residential, 5, 5);
            _lotWorld.AddExperimentalBuilding3D("victorian-color-house-v01",0f,0f,0);
            _lotWorld.SetBaseTexture("grass-lush");
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _hasOpenLot=true;
            _lotEditorCategory=LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded=false;
            _lotStatus="Victorian Painted House — original source colors";
            Show(AppScreen.LotEditor);
            _lotWorld.SetQaOrthographicSize(17f);
        }
        public void ShowSaltboxLibraryQa()
        {
            _lotEditorCategory=LotEditorCategory.Buildings3D;
            _lotEditorCategoryExpanded=true;
            _buildingUseCategory=BuildingUseCategory.Residential;
            Show(AppScreen.LotEditor);
        }
    }
}
#endif
