#if UNITY_EDITOR
using CityForgeV3.World;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenDryGoodsQa()
        {
            EnsureLotWorld(); _lotWorld.NewEmptyLot("Dry Goods Import QA", LotType.Residential, 4, 4);
            _lotWorld.AddExperimentalBuilding3D("dry-goods-v05",0,0,0);
            _lotWorld.SetBaseTexture("grass-lush"); _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _hasOpenLot=true; _lotEditorCategory=LotEditorCategory.Buildings3D; _lotEditorCategoryExpanded=false;
            _lotStatus="Dry Goods — animated door and independent lights"; Show(AppScreen.LotEditor); _lotWorld.SetQaOrthographicSize(17f);
        }
    }
}
#endif
