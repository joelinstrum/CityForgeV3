using System;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        void AddStoneIndustry(VisualElement scroll,RegionCityTile d)
        {
            _districtWorld.EnsureStoneDeposits(d);
            var available = d.StoneSites.Count(s => !s.Built);
            var built = d.StoneSites.Count(s => s.Built);
            var ready = d.StoneSites.Count(s => _districtWorld.QuarrySiteClear(d, s));
            scroll.Add(ComposeIndustryCard("stone", "STONE QUARRY",
                "CityForgeV3/Industry/StoneQuarryV01/MenuThumbnailV01",
                $"{d.StoneSites.Count} stone sites · {ready} ready · {available - ready} blocked · {built} quarries\n2 workers · $250 each / season · $500 total. Trees are cleared automatically.",
                "ADD STONE QUARRY", () => BeginIndustryPlacement("stone"),
                Resources.Load<GameObject>(DistrictQuarry.ResourcePath) != null &&
                    available > 0,
                () => ComposeQuarryManagement(), built > 0));
        }
        void ComposeQuarryManagement()
        {
            var d = FindSelectedRegionTile(); if (d == null) return;
            var panel = CreateDocumentModal("STONE QUARRIES", "Manage your existing quarries.");
            panel.style.width = 760; panel.style.maxWidth = Length.Percent(94);
            var scroll = new ScrollView(); scroll.style.maxHeight = 480; panel.Add(scroll);
            foreach(var site in d.StoneSites.Where(s => s.Built))
            {
                var row=new VisualElement();row.style.paddingTop=20;row.style.paddingBottom=18;
                var title=new Label("STONE QUARRY");title.style.fontSize=26;row.Add(title);
                row.Add(new Label(DistrictQuarry.WorkersPaid(d,site) ? DistrictQuarry.Status(site) : "Workers off duty — $500 wages due"));
                row.Add(new Label("2 quarry workers · $250 each per season · $500 total"));
                if(!DistrictQuarry.WorkersPaid(d,site))row.Add(CfButton.Create("PAY WORKERS · $500",()=>{DistrictQuarry.PayWorkers(d,site);SaveDistrictEdit();ComposeQuarryManagement();},d.Treasury>=DistrictQuarry.SeasonalPayroll,"primary"));
                row.Add(new Label($"Cart: {site.CartBlocks}/{site.Script.cartCapacity} blocks · Loaded: {site.BlocksLoaded:N0} blocks"));
                var actions=DocumentModalActions();actions.Add(CfButton.Create("ROTATE",()=>ComposeSelectedObject(new DistrictSelectionRef(DistrictSelectionKind.Entity, "quarry:" + site.Id)),true,"quiet"));actions.Add(CfButton.Create("VIEW SITE",()=>FocusQuarry(site),true,"quiet"));
                if(!site.Built)actions.Add(CfButton.Create("BUILD QUARRY",()=>SetQuarry(site,true),Resources.Load<GameObject>(DistrictQuarry.ResourcePath)!=null&&_districtWorld.QuarrySiteClear(d,site),"primary"));
                else
                {
                    actions.Add(CfButton.Create(site.Enabled?"PAUSE":"RUN",()=>{site.Enabled=!site.Enabled;SaveDistrictEdit();ComposeDistrictIndustryModal();},true,"quiet"));
                    actions.Add(CfButton.Create("{ } SCRIPT",()=>QuarryScriptEditor(site),true,"quiet"));
                    actions.Add(CfButton.Create("REMOVE",()=>SetQuarry(site,false),true,"quiet"));
                }
                row.Add(actions);
                row.Add(new Label($"{site.Script.miningSeconds:0.#} seconds/block · {site.Script.loadingSeconds:0.#} second crane load · {site.Script.stoneTonsPerBlock} t per block · credited and converted on delivery"));
                if(!site.Built&&!_districtWorld.QuarrySiteClear(d,site))row.Add(new Label("Move nearby roads or lots; the quarry needs dry, level ground."));
                scroll.Add(row);
            }
            panel.Add(CfButton.Create("BACK", ComposeDistrictIndustryModal, true, "quiet"));
        }
        void FocusQuarry(DistrictStoneSite site)
        {
            RemoveDocumentModal();_districtEdgePanDirection=Vector2Int.zero;
            _terraformPanOffset=DistrictQuarry.Point(FindSelectedRegionTile(),site);_terraformZoomLevel=DistrictZoomLevel.LOD0;
            _districtWorld.SetPan(_terraformPanOffset);_districtWorld.SetZoom(_terraformZoomLevel);_districtWorld.WorldCamera.orthographicSize=18;
        }
        void SetQuarry(DistrictStoneSite site,bool built)
        {
            var d=FindSelectedRegionTile();if(d==null||!d.StoneSites.Contains(site))return;
            EnsureDistrictUndo(d);
            if(built){if(!_districtWorld.BuildQuarry(d,site))return;}
            else{DistrictQuarry.Demolish(site);}
            SaveDistrictEdit();_districtWorldCompositionKey="";EnsureDistrictWorld(d);SelectDistrictCategory("Select");Show(AppScreen.DistrictTerraform);FocusQuarry(site);
        }
        void QuarryScriptEditor(DistrictStoneSite site,string source=null)
        {
            var panel=CreateDocumentModal("{ } QUARRY SCRIPT","Mine a block, load it, then credit Stone. Full wagons deliver stone to reachable Brickworks, then return.");
            panel.style.width=1100;panel.style.maxWidth=Length.Percent(94);panel.style.height=Length.Percent(86);
            panel.Add(StyledLabel("Site ID: "+site.Id,"document-modal-copy"));
            var error=StyledLabel("","document-modal-copy");panel.Add(error);
            var scroll=new ScrollView{verticalScrollerVisibility=ScrollerVisibility.AlwaysVisible};scroll.style.flexGrow=1;scroll.style.minHeight=0;panel.Add(scroll);
            var code=new TextField{value=source??JsonUtility.ToJson(site.Script,true),multiline=true,name="quarry-script-source"};code.style.fontSize=24;code.style.minHeight=500;code.style.flexShrink=0;scroll.Add(code);
            var actions=DocumentModalActions();
            actions.Add(CfButton.Create("APPLY",()=>{try{site.Script=QuarryScript.Parse(code.value);SaveDistrictEdit();ComposeDistrictIndustryModal();}catch(Exception e){error.text=e.Message;}},true,"primary"));
            actions.Add(CfButton.Create("IMPORT",()=>OpenBehaviorScriptImport(text=>QuarryScriptEditor(site,text),()=>QuarryScriptEditor(site,code.value),text=>QuarryScript.Parse(text)),true,"quiet"));
            actions.Add(CfButton.Create("COPY ID",()=>GUIUtility.systemCopyBuffer=site.Id,true,"quiet"));
            actions.Add(CfButton.Create("CANCEL",ComposeDistrictIndustryModal,true,"quiet"));panel.Add(actions);
        }
    }
}
