#if UNITY_EDITOR
namespace CityForgeV3.World
{
 public sealed partial class LotWorldController
 {
  public void SelectTreeRepairForQa(){if(FloraCount!=1)return;SelectedFloraIndex=0;ActiveObjectSelection=LotObjectSelectionKind.Flora;_floraEditorActive=true;ApplyFloraSelection();NotifyStateChanged();}
 }
}
#endif
