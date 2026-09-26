using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private void ComposeDistrictHillsModal()
        {
            var district=FindSelectedRegionTile();if(district==null)return;CancelDistrictSelectionPointer();
            var old=district.Hills??new DistrictHillSettings();
            var panel=CreateDocumentModal("RELIEF","Choose gentle hills or steep mountains. Coal appears only at suitable mountain faces.");
            panel.style.width=1000;panel.style.maxWidth=Length.Percent(94);
            var mountain=new Toggle("Steep mountains"){value=old.Mountains};panel.Add(mountain);
            var height=new IntegerField("Relief Height (0–240 m)"){value=Mathf.RoundToInt(old.HeightMeters>0?old.HeightMeters:35)};
            mountain.RegisterValueChangedCallback(e=>height.value=e.newValue?180:35);
            var coverage=new IntegerField("Coverage (10–100%)"){value=Mathf.RoundToInt(old.Coverage*100)};
            var seed=new IntegerField("Landscape seed"){value=old.Seed};
            var vertical=new FloatField("Vertical Relief Scale (0.25–4x)")
                {value=old.VerticalReliefScale>0?old.VerticalReliefScale:1f};
            foreach(var field in new[]{height,coverage,seed})
            {
                field.style.fontSize=30;field.style.height=68;field.style.flexDirection=FlexDirection.Row;
                field.labelElement.style.width=440;field.labelElement.style.minWidth=440;
                var input=field.Q<VisualElement>("unity-text-input");
                if(input!=null){input.style.backgroundColor=new Color(.23f,.29f,.31f);input.style.color=Color.white;input.style.flexGrow=1;input.style.paddingLeft=14;input.style.paddingTop=8;}
                panel.Add(field);
            }
            vertical.style.fontSize=30;vertical.style.height=68;vertical.style.flexDirection=FlexDirection.Row;
            vertical.labelElement.style.width=440;vertical.labelElement.style.minWidth=440;
            var verticalInput=vertical.Q<VisualElement>("unity-text-input");
            if(verticalInput!=null){verticalInput.style.backgroundColor=new Color(.23f,.29f,.31f);verticalInput.style.color=Color.white;verticalInput.style.flexGrow=1;verticalInput.style.paddingLeft=14;verticalInput.style.paddingTop=8;}
            vertical.SetEnabled(!old.Mountains);mountain.RegisterValueChangedCallback(e=>vertical.SetEnabled(!e.newValue));
            panel.Add(vertical);
            panel.Q<Label>(className:"document-modal-title").style.fontSize=42;
            foreach(var label in panel.Query<Label>(className:"document-modal-copy").ToList())label.style.fontSize=26;
            var actions=DocumentModalActions();
            actions.Add(CfButton.Create("APPLY RELIEF",()=>ApplyDistrictHills(new DistrictHillSettings{Version=2,Mountains=mountain.value,Seed=seed.value,HeightMeters=Mathf.Clamp(height.value,0,240),Coverage=Mathf.Clamp(coverage.value,10,100)/100f,VerticalReliefScale=mountain.value?1f:Mathf.Clamp(vertical.value,.25f,4f)}),true,"primary"));
            actions.Add(CfButton.Create("FLAT",()=>ApplyDistrictHills(new DistrictHillSettings{Seed=seed.value,HeightMeters=0,Coverage=Mathf.Clamp(coverage.value,10,100)/100f,VerticalReliefScale=vertical.value}),true,"quiet"));
            actions.Add(CfButton.Create("CANCEL",RemoveDocumentModal,true,"quiet"));foreach(var button in actions.Query<Button>().ToList()){button.style.fontSize=28;button.style.height=64;}
            panel.Add(actions);
            height.schedule.Execute(()=>height.Focus()).StartingIn(100);
        }
        private void ApplyDistrictHills(DistrictHillSettings hills)
        {
            var district=FindSelectedRegionTile();if(district==null)return;
            district.Hills=hills;SaveDistrictEdit();RemoveDocumentModal();_districtWorldCompositionKey="";Show(AppScreen.DistrictTerraform);
        }
    }
}
