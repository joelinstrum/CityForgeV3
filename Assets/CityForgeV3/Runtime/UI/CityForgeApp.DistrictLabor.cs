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
            // Updated by spatial edits; do not serialize every tree three times per frame.
            var key=_districtWorldCompositionKey;
            if(_laborNavigation==null||_laborDistrict!=d||_laborComposition!=key)
            { _laborNavigation=new DistrictLaborNavigation(d,_districtWorld.IsUnderRiverWater);_laborDistrict=d;_laborComposition=key; }
            return _laborNavigation;
        }
        private int _pendingLaborCount;
        private bool _placingMarksman;
        private string _movingMarksmanId;
        private TimberScript _pendingTimberScript = new();
        private TimberScript _newTimberScript = new();
        private void CancelLaborPlacement()
        {
            _pendingLaborCount = 0;_placingMarksman=false;_movingMarksmanId=null;
            _districtWorld?.HideAxemanPlacement();
            UnityEngine.Cursor.visible = true;
        }
        private void ComposeDistrictLaborModal()
        {
            CancelLaborPlacement();
            _pendingDistrictLotId="";
            CancelDistrictSelectionPointer();
            Show(AppScreen.DistrictTerraform);
            var d=FindSelectedRegionTile();if(d==null)return;
            var state=DistrictLabor.State(d);
            var panel=CreateDocumentModal("LABOR","Choose a worker type for details and placement.");
            panel.AddToClassList("flora-modal-panel");
            var scroll=new ScrollView();scroll.AddToClassList("flora-modal-scroll");panel.Add(scroll);
            var cards=new VisualElement();cards.style.flexDirection=FlexDirection.Row;cards.style.flexWrap=Wrap.Wrap;scroll.Add(cards);
            var card=CfButton.Create("",ComposeAxemanDetails,true,"quiet");card.name="labor-axemen-card";
            card.AddToClassList("character-library-card");card.AddToClassList("road-material-card");
            var icon=new Image{image=Resources.Load<Texture2D>("CityForgeV3/UI/CharacterThumbnails/axeman-labor-v01") ?? Resources.Load<Texture2D>("CityForgeV3/Art/ResourceIconsV01/wood"),scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
            icon.AddToClassList("character-library-image");card.Add(icon);
            var title=StyledLabel("AXEMEN","character-library-name");title.pickingMode=PickingMode.Ignore;card.Add(title);
            var detail=StyledLabel("Forestry · lumber wagon included","catalog-meta");detail.pickingMode=PickingMode.Ignore;card.Add(detail);cards.Add(card);
            AddMarksmanLaborControls(scroll,d,cards);
            foreach(var crew in state.TimberCrews??new())
            {
                var row=new VisualElement();row.style.marginTop=12;
                row.Add(StyledLabel(crew.Name+" · "+(state.Workers.Any(w=>w.CrewId==crew.Id&&w.BearAlarm)?"Bear sighting — workers waiting for safety":crew.Status),"inspector-title"));
                var actions=DocumentModalActions();
                actions.Add(CfButton.Create(crew.Enabled?"PAUSE":"RUN",()=>{crew.Enabled=!crew.Enabled;SaveDistrictEdit();ComposeDistrictLaborModal();},true,"quiet"));
                actions.Add(CfButton.Create("{ } SCRIPT",()=>OpenTimberScript(JsonUtility.ToJson(crew.Script,true),crew),true,"quiet"));
                actions.Add(CfButton.Create("FIND",()=>{RemoveDocumentModal();_terraformPanOffset=crew.Camp;_districtWorld.SetPan(crew.Camp);_districtWorld.SetZoom(DistrictZoomLevel.LOD0);},true,"quiet"));
                actions.Add(CfButton.Create("COPY ID",()=>GUIUtility.systemCopyBuffer=crew.Id,true,"quiet"));
                row.Add(actions);scroll.Add(row);
            }
            if(state.PaidSlots<state.AssignedAxemen)
                scroll.Add(CfButton.Create("PAY SEASONAL WAGES",()=>{DistrictLabor.Assign(d,state.AssignedAxemen);SaveDistrictEdit();ComposeDistrictLaborModal();},true,"primary"));
            panel.Add(CfButton.Create("CLOSE",RemoveDocumentModal,true,"quiet"));
            StyleLaborPanel(panel);card.style.width=340;card.style.height=450;icon.style.height=250;
            panel.Query<Label>(className:"character-library-name").ForEach(label=>label.style.height=70);
        }
        private void ComposeAxemanDetails()
        {
            var d=FindSelectedRegionTile();if(d==null)return;
            var panel=CreateDocumentModal("AXEMEN","Place a forestry crew and its horse-drawn lumber wagon.");
            panel.AddToClassList("flora-modal-panel");
            var scroll=new ScrollView();scroll.AddToClassList("flora-modal-scroll");panel.Add(scroll);
            var picture=new Image{image=Resources.Load<Texture2D>("CityForgeV3/UI/CharacterThumbnails/horse-lumber-wagon-v01"),scaleMode=ScaleMode.ScaleToFit};
            picture.style.height=180;scroll.Add(picture);
            scroll.Add(StyledLabel("Drop the axemen in the woods. They fell nearby Cilician firs (the current harvestable tree) and collect a wagon load. The wagon delivers to the nearest lumber mill it can reach by connected roads, unloads, and returns. Wood enters your inventory only when the dock workers load lumber onto the barge.","document-modal-copy"));
            scroll.Add(StyledLabel("A nearby road becomes the wagon's parking point. Without road access it waits at the crew. Leave turning room at the camp and mill. Delivered timber supplies the mill's barge-loading behavior.","document-modal-copy"));
            var count=new IntegerField("Axemen"){value=1,name="labor-axemen-count",isDelayed=true};count.AddToClassList("document-field");scroll.Add(count);
            var cost=StyledLabel("","document-modal-copy");cost.name="labor-cost";scroll.Add(cost);
            var actions=DocumentModalActions();
            var place=CfButton.Create("PLACE",()=>{
                _pendingTimberScript=JsonUtility.FromJson<TimberScript>(JsonUtility.ToJson(_newTimberScript));
                _pendingDistrictLotId="";_pendingDistrictFloraId="";_pendingDistrictFloraMode=0;
                _districtSelection.Clear();_selectedDistrictLotInstanceId="";
                _pendingLaborCount=count.value;RemoveDocumentModal();
            },true,"primary");place.name="labor-assign";
            void Quote(){var due=DistrictLabor.AssignmentCost(d,DistrictLabor.State(d).AssignedAxemen+count.value);
                cost.text=$"$250 per axeman per season · due now ${due:N0}.\nClick Place, then click the ground. Escape cancels.";
                place.SetEnabled(count.value>=1&&count.value<=16&&due<=d.Treasury&&_districtWorld.LaborAssetReady);}
            count.RegisterValueChangedCallback(_=>Quote());Quote();
            actions.Add(place);
            actions.Add(CfButton.Create("{ } SCRIPT",()=>OpenTimberScript(JsonUtility.ToJson(_newTimberScript,true),null),true,"quiet"));
            actions.Add(CfButton.Create("BACK",ComposeDistrictLaborModal,true,"quiet"));panel.Add(actions);StyleLaborPanel(panel);
        }
        private static void StyleLaborPanel(VisualElement panel)
        {
            panel.style.width=1100;panel.style.maxWidth=Length.Percent(94);
            panel.style.height=Length.Percent(86);panel.style.maxHeight=Length.Percent(92);
            panel.Query<Label>().ForEach(label=>{label.style.fontSize=28;label.style.whiteSpace=WhiteSpace.Normal;});
            var title=panel.Q<Label>(className:"document-modal-title");if(title!=null)title.style.fontSize=42;
            panel.Query<Button>().ForEach(button=>{button.style.minHeight=70;button.style.minWidth=210;button.style.whiteSpace=WhiteSpace.NoWrap;button.style.fontSize=28;button.style.flexShrink=0;});
            panel.Query<IntegerField>().ForEach(field=>{field.style.fontSize=30;field.style.minHeight=65;});
        }
        private void OpenTimberScript(string source,DistrictTimberCrew crew)
        {
            var panel=CreateDocumentModal("{ } FORESTRY SCRIPT","Edit this crew's timber delivery recipe. Changes apply without discarding its cargo or progress.");
            panel.AddToClassList("flora-modal-panel");panel.style.width=Length.Percent(94);panel.style.maxWidth=1400;
            var error=StyledLabel("","inspector-note");panel.Add(error);
            if(crew!=null)panel.Add(StyledLabel("Crew: "+crew.Id+"\nWagon: "+crew.WagonId,"inspector-note"));
            var scroll=new ScrollView{verticalScrollerVisibility=ScrollerVisibility.AlwaysVisible};scroll.style.flexGrow=1;scroll.style.flexBasis=0;scroll.style.minHeight=0;panel.Add(scroll);
            var code=new TextField{value=source,multiline=true,name="timber-script-source"};code.style.fontSize=22;code.style.flexShrink=0;scroll.Add(code);
            void Size(){code.style.height=Mathf.Max(450,code.value.Split('\n').Length*32+40);}
            code.RegisterValueChangedCallback(_=>Size());Size();
            var actions=DocumentModalActions();
            actions.Add(CfButton.Create("APPLY",()=>{try{var script=TimberScript.Parse(code.value);if(crew==null)_newTimberScript=script;else{crew.Script=script;SaveDistrictEdit();}if(crew==null)ComposeAxemanDetails();else ComposeDistrictLaborModal();}catch(System.Exception e){error.text=e.Message;}},true,"primary"));
            actions.Add(CfButton.Create("IMPORT",()=>OpenBehaviorScriptImport(text=>OpenTimberScript(text,crew),()=>OpenTimberScript(code.value,crew),text=>TimberScript.Parse(text)),true,"quiet"));
            actions.Add(CfButton.Create("COPY",()=>GUIUtility.systemCopyBuffer=code.value,true,"quiet"));
            actions.Add(CfButton.Create("CANCEL",()=>{if(crew==null)ComposeAxemanDetails();else ComposeDistrictLaborModal();},true,"quiet"));panel.Add(actions);StyleLaborPanel(panel);
        }
        private bool LaborDropValid(RegionCityTile district,Vector2 normalized)
        {
            var point=new Vector2((normalized.x-.5f)*DistrictScale.SizeMeters(district.Width),(normalized.y-.5f)*DistrictScale.SizeMeters(district.Height));
            return LaborNavigation(district).Walkable(point);
        }
        private bool PlaceLabor(RegionCityTile district,Vector2 normalized)
        {
            if(_placingMarksman)
            {
                if(!LaborDropValid(district,normalized))return false;
                var p=new Vector2((normalized.x-.5f)*DistrictScale.SizeMeters(district.Width),(normalized.y-.5f)*DistrictScale.SizeMeters(district.Height));
                var wildlife=DistrictWildlife.State(district);var guard=wildlife.Marksmen.Find(m=>m.Id==_movingMarksmanId);
                if(guard==null){guard=DistrictWildlife.Hire(district,p);if(guard==null)return false;}guard.Position=p;
                SaveDistrictEdit();CancelLaborPlacement();Show(AppScreen.DistrictTerraform);return true;
            }
            if(_pendingLaborCount<=0||!_districtWorld.LaborAssetReady||
                Resources.Load<GameObject>(HorseWagonDefinition.For(LotWorldController.HorseLumberWagonPropId).ResourcePath)==null||
                !LaborDropValid(district,normalized))return false;
            var point=new Vector2((normalized.x-.5f)*DistrictScale.SizeMeters(district.Width),(normalized.y-.5f)*DistrictScale.SizeMeters(district.Height));
            var nav=new DistrictTimberNavigation(district,_districtWorld.IsUnderRiverWater);
            var parking=nav.ParkingNear(point);
            if(parking.HasValue&&LaborNavigation(district).Route(point,parking.Value)==null)parking=null;
            var home=parking??point;
            var crew=DistrictTimber.Place(district,point,home,_pendingLaborCount,_pendingTimberScript);
            if(crew==null)return false;
            var heading=nav.ParkingHeading(home);crew.HorseHeading=crew.BodyHeading=crew.FrontHeading=heading;
            SaveDistrictEdit();CancelLaborPlacement();Show(AppScreen.DistrictTerraform);return true;
        }
        private void OnApplicationQuit()
        {
            if(_currentScreen==AppScreen.DistrictTerraform && _openRegion!=null)PersistDistrictRegion();
        }
        private void TickDistrictLabor()
        {
            if(_root==null||_currentScreen!=AppScreen.DistrictTerraform||_districtWorld==null)return;
            var d=FindSelectedRegionTile();if(d==null)return;var s=DistrictLabor.State(d);
            bool running=!_districtSimulationPaused&&!_placingBrickworks&&!HarvestInputBusy&&_root.Q<VisualElement>("document-modal")==null&&_districtWorld.LaborAssetReady;
            bool paid=s.PaidSlots>=s.AssignedAxemen;
            if(running)
            {
                var nav=LaborNavigation(d);var change=DistrictLabor.Tick(d,Mathf.Min(Time.deltaTime,.1f),nav.Route,nav.Walkable);
                if(change.Changed.Count>0){_districtWorld.RefreshHarvestTrees(d,change.Changed);_districtWorld.PlayTreeFalls(d,change.Falling);_districtWorldCompositionKey=DistrictCompositionKey(d);}
                _laborSaveTimer+=Time.deltaTime;
                if(change.Durable){RefreshDistrictResourceBar(d);SaveDistrictEdit();_laborSaveTimer=0;}
                else if(_laborSaveTimer>=5){PersistDistrictRegion();_laborSaveTimer=0;}
            }
            if(running&&DistrictWildlife.Tick(d,Mathf.Min(Time.deltaTime,.1f),LaborNavigation(d).Walkable))SaveDistrictEdit();
            _districtWorld.PresentWildlife(d,running);
            if(running&&DistrictQuarry.Tick(d,Mathf.Min(Time.deltaTime,.1f),LaborNavigation(d).Walkable))SaveDistrictEdit();
            _districtWorld.PresentQuarries(d,running);
            _districtWorld.PresentBrickworks(d);
            if(_districtWorld.TickQuarryDeliveries(d,running,Mathf.Min(Time.deltaTime,.1f)))SaveDistrictEdit();
            if(running&&DistrictBrickworks.Tick(d,Mathf.Min(Time.deltaTime,.1f)))SaveDistrictEdit();
            paid=s.PaidSlots>=s.AssignedAxemen;
            if(_districtWorld.TickTimber(d,_lotWorld,running&&paid,Mathf.Min(Time.deltaTime,.05f)))SaveDistrictEdit();
            _districtWorld.PresentLabor(d,running&&paid);
            _laborUiTimer+=Time.unscaledDeltaTime;
            if(_laborUiTimer<.5f)return;_laborUiTimer=0;
            var money=_root.Q<Label>("district-simulation-money");if(money!=null)money.text=$"${d.Treasury:N0}";
            var season=_root.Q<Label>("district-labor-season");if(season!=null)season.text=$"Season: {DistrictLabor.SeasonName(s.SeasonIndex)} · Year {d.FoundingYear+s.SeasonIndex/4}";
            RefreshDistrictResourceBar(d);RefreshWildlifeAlert(d);RefreshBrickworksWarnings(d);
            var status=_root.Q<Label>("district-labor-status");if(status!=null)status.text=$"Axemen: {s.AssignedAxemen} · Wood: {s.Wood:N0} t"+(s.PaidSlots<s.AssignedAxemen?"\nWages due — open Labor":s.AssignedAxemen>0&&s.Workers.All(w=>w.Activity==AxemanActivity.Waiting)?"\nNo reachable trees":"");
        }
    }
}
