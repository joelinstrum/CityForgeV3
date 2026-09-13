using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private DistrictLaborNavigation _laborNavigation;
        private RegionCityTile _laborDistrict;
        private string _laborComposition;
        private float _laborSaveTimer,_laborUiTimer;
        private DistrictLaborNavigation LaborNavigation(RegionCityTile d)
        {
            var key=DistrictCompositionKey(d);
            if(_laborNavigation==null||_laborDistrict!=d||_laborComposition!=key)
            { _laborNavigation=new DistrictLaborNavigation(d,_districtWorld.IsUnderRiverWater);_laborDistrict=d;_laborComposition=key; }
            return _laborNavigation;
        }
        private void ComposeDistrictLaborModal()
        {
            var d=FindSelectedRegionTile();if(d==null)return;
            CancelDistrictSelectionPointer();var s=DistrictLabor.State(d);
            var panel=CreateDocumentModal("LABOR","Assign axemen to automatically gather wood in this district.");
            panel.style.width=1100;panel.style.maxWidth=Length.Percent(94);panel.style.maxHeight=Length.Percent(94);
            var count=new IntegerField("Axemen") {value=s.AssignedAxemen,name="labor-axemen-count",isDelayed=true};count.style.fontSize=36;count.style.height=72;panel.Add(count);
            var quote=StyledLabel("","document-modal-copy");quote.name="labor-cost";panel.Add(quote);
            panel.Add(StyledLabel("$250 per axeman per season · 10 minutes of running simulation.\nWages are paid up front; reductions do not refund this season's wages.","document-modal-copy"));
            panel.Add(StyledLabel("Workers start at a temporary lumber camp until the lumber mill is available. They gather Cilician Firs and bring wood back to camp.","document-modal-copy"));
            panel.Add(StyledLabel($"Wood stored: {s.Wood:N0} · Treasury: ${d.Treasury:N0}","document-modal-copy"));
            var error=StyledLabel("","document-modal-copy");panel.Add(error);
            var actions=DocumentModalActions();
            var apply=CfButton.Create("ASSIGN AXEMEN",()=>
            {
                if(count.value<0){error.text="Enter zero or more axemen.";return;}
                if(count.value>0&&!_districtWorld.LaborAssetReady){error.text="The axeman model is unavailable. No wages were charged.";return;}
                if(count.value>0&&!s.CampPlaced)
                {
                    var tree=d.Flora.FirstOrDefault(DistrictTreeHarvest.CanFell);
                    var preferred=tree!=null?DistrictLabor.TreePoint(d,tree)+new Vector2(0,-8):new Vector2(12,12);
                    var camp=LaborNavigation(d).FindCamp(preferred);
                    if(!camp.HasValue){error.text="No dry, open camp location is available.";return;}
                    // Validate funds before committing the new dispatch location.
                    if(DistrictLabor.AssignmentCost(d,count.value)>d.Treasury){error.text="Not enough money for this assignment.";return;}
                    s.Camp=camp.Value;s.CampPlaced=true;
                }
                if(!DistrictLabor.Assign(d,count.value)){error.text="Not enough money for this assignment.";return;}
                SaveDistrictEdit();RemoveDocumentModal();Show(AppScreen.DistrictTerraform);
                _districtWorld.PresentLabor(d,!_districtSimulationPaused);
                Debug.Log($"LABOR assigned={s.AssignedAxemen} treasury={d.Treasury} season={s.SeasonIndex}");
            },true,"primary");apply.name="labor-assign";actions.Add(apply);
            if(s.CampPlaced)actions.Add(CfButton.Create("FIND CAMP",()=>{RemoveDocumentModal();_terraformPanOffset=s.Camp;_terraformZoomLevel=DistrictZoomLevel.LOD0;_districtWorld.FocusLaborCamp(d);},true,"quiet"));
            actions.Add(CfButton.Create("CANCEL",RemoveDocumentModal,true,"quiet"));panel.Add(actions);
            void Quote(){long cost=(long)Mathf.Max(0,count.value)*DistrictLabor.Wage;long due=DistrictLabor.AssignmentCost(d,count.value);quote.text=$"Seasonal wages: ${cost:N0}\nDue now: ${due:N0}";apply.SetEnabled(count.value>=0&&due<=d.Treasury);}
            count.RegisterValueChangedCallback(_=>Quote());Quote();
            panel.Query<Label>().ForEach(label=>{label.style.fontSize=30;label.style.whiteSpace=WhiteSpace.Normal;});
            panel.Q<Label>(className:"document-modal-title").style.fontSize=44;
            panel.Query<Button>().ForEach(button=>{button.style.height=76;button.style.fontSize=30;});
            var input=count.Q<VisualElement>("unity-text-input");input.style.fontSize=36;input.style.backgroundColor=new Color(.08f,.13f,.16f);input.style.borderBottomWidth=2;input.style.borderBottomColor=new Color(.5f,.6f,.5f);
            count.schedule.Execute(()=>count.Focus()).StartingIn(100);
        }
        private void OnApplicationQuit()
        {
            if(_currentScreen==AppScreen.DistrictTerraform && _openRegion!=null)PersistDistrictRegion();
        }
        private void TickDistrictLabor()
        {
            if(_currentScreen!=AppScreen.DistrictTerraform||_districtWorld==null)return;
            var d=FindSelectedRegionTile();if(d==null)return;var s=DistrictLabor.State(d);
            bool running=!_districtSimulationPaused&&!HarvestInputBusy&&_root.Q<VisualElement>("document-modal")==null&&_districtWorld.LaborAssetReady;
            bool paid=s.PaidSlots>=s.AssignedAxemen;
            if(running&&s.CampPlaced)
            {
                var nav=LaborNavigation(d);var change=DistrictLabor.Tick(d,Mathf.Min(Time.deltaTime,.1f),nav.Route,nav.Walkable);
                if(change.Changed.Count>0){_districtWorld.RefreshHarvestTrees(d,change.Changed);_districtWorld.PlayTreeFalls(d,change.Falling);_districtWorldCompositionKey=DistrictCompositionKey(d);}
                _laborSaveTimer+=Time.deltaTime;
                if(change.Durable){SaveDistrictEdit();_laborSaveTimer=0;}
                else if(_laborSaveTimer>=5){PersistDistrictRegion();_laborSaveTimer=0;}
            }
            _districtWorld.PresentLabor(d,running&&paid);
            _laborUiTimer+=Time.unscaledDeltaTime;
            if(_laborUiTimer<.5f)return;_laborUiTimer=0;
            var money=_root.Q<Label>("district-simulation-money");if(money!=null)money.text=$"${d.Treasury:N0}";
            var season=_root.Q<Label>("district-labor-season");if(season!=null)season.text=$"Season: {DistrictLabor.SeasonName(s.SeasonIndex)} · Year {d.FoundingYear+s.SeasonIndex/4}";
            var status=_root.Q<Label>("district-labor-status");if(status!=null)status.text=$"Axemen: {s.AssignedAxemen} · Wood: {s.Wood:N0}"+(s.PaidSlots<s.AssignedAxemen?"\nWages due — open Labor":s.AssignedAxemen>0&&s.Workers.All(w=>w.Activity==AxemanActivity.Waiting)?"\nNo reachable trees":"");
        }
    }
}
