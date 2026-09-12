#if UNITY_EDITOR
using CityForgeV3.World;
using UnityEngine;
using System.Reflection;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenFarmerQa()
        {
            // Never replace an active user lot for a preview.
            if (_hasOpenLot) { Debug.LogWarning("Farmer preview requires a fresh session without an open lot.");return; }
            EnsureLotWorld();
            _lotWorld.NewEmptyLot("Farmer animation preview",LotType.Residential,3,3);
            _lotWorld.SetBaseTexture("grass-lush");
            _lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);
            _lotWorld.PlacePropForQa(LotWorldController.FarmerCharacterId,0f,0f);
            const BindingFlags f=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            typeof(LotWorldController).GetProperty("SelectedPropIndex",f).SetValue(_lotWorld,0);
            typeof(LotWorldController).GetProperty("ActiveObjectSelection",f).SetValue(_lotWorld,LotObjectSelectionKind.Prop);
            _hasOpenLot=true;_lotEditorCategory=LotEditorCategory.Characters;_lotEditorCategoryExpanded=false;
            _lotStatus="Farmer • Hoe button works • arrows walk • Space stops";
            Show(AppScreen.LotEditor);
            _lotWorld.SetQaOrthographicSize(3.5f);
            _lotWorld.HoeSelectedFarmer();
        }
    }
}
#endif
