#if UNITY_EDITOR
using CityForgeV3.World;
namespace CityForgeV3.UI { public sealed partial class CityForgeApp {
 public void OpenLumberMillQa(){EnsureLotWorld();_lotWorld.NewEmptyLot("Linstrum Lumber Mill",LotType.Residential,4,4);_lotWorld.AddExperimentalBuilding3D("lumber-mill-v01",0,0,0);_lotWorld.SetBaseTexture("grass-lush");_lotWorld.SetTimeOfDay(TimeOfDayPreset.Noon);_hasOpenLot=true;_lotEditorCategory=LotEditorCategory.Buildings3D;_lotEditorCategoryExpanded=false;_lotStatus="Linstrum Lumber Mill";Show(AppScreen.LotEditor);}
} }
#endif
