using System;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private VisualElement ComposeDistrictIndustryBar()
        {
            var button=CfButton.Create("INDUSTRY",ComposeDistrictIndustryModal,true,"quiet");button.name="district-industry-menu";
            button.style.position=Position.Absolute;button.style.left=360;button.style.top=78;button.style.width=160;button.style.height=44;button.style.fontSize=20;
            return button;
        }
        private void FocusDistrictMine(DistrictResourceDeposit deposit)
        {
            var d=FindSelectedRegionTile();if(d==null)return;RemoveDocumentModal();_districtEdgePanDirection=Vector2Int.zero;
            _terraformPanOffset=new Vector2((deposit.NormalizedX-.5f)*DistrictScale.SizeMeters(d.Width),(deposit.NormalizedZ-.5f)*DistrictScale.SizeMeters(d.Height));
            _terraformZoomLevel=DistrictZoomLevel.LOD0;_districtWorld.SetPan(_terraformPanOffset);_districtWorld.SetZoom(_terraformZoomLevel);
        }
        private void SetDistrictMine(DistrictResourceDeposit deposit,bool built)
        {
            var d=FindSelectedRegionTile();if(d==null||!d.ResourceDeposits.Contains(deposit))return;
            EnsureDistrictUndo(d);
            if(built){if(Resources.Load<GameObject>(DistrictCoalMine.ResourcePath)==null||!DistrictCoalMine.Build(d,deposit))return;}
            else{if(!deposit.MineBuilt)return;deposit.MineBuilt=false;}
            SaveDistrictEdit();_districtWorld.RefreshCoalBuildings();
            _districtWorldCompositionKey=DistrictCompositionKey(d);
            _laborNavigation=null;Show(AppScreen.DistrictTerraform);FocusDistrictMine(deposit);
        }
        private VisualElement ComposeIndustryCard(string resource, string title, string thumbnail,
            string summary, string addLabel, Action add, bool canAdd, Action manage, bool canManage)
        {
            var card = new VisualElement { name = "industry-card-" + resource };
            card.AddToClassList("industry-resource-card");
            var image = new Image { image = Resources.Load<Texture2D>(thumbnail),
                scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.AddToClassList("industry-resource-thumbnail"); card.Add(image);
            var details = new VisualElement(); details.AddToClassList("industry-resource-details"); card.Add(details);
            details.Add(StyledLabel(title, "inspector-title"));
            details.Add(StyledLabel(summary, "document-modal-copy"));
            var actions = DocumentModalActions();
            var addButton = CfButton.Create(addLabel, add, canAdd, "primary");
            addButton.name = "industry-add-" + resource;
            if (!canAdd) addButton.tooltip = resource == "stone"
                ? "Requires an unbuilt stone deposit on clear, dry, level ground."
                : "Requires an unbuilt coal deposit on a suitable mountain slope.";
            actions.Add(addButton);
            actions.Add(CfButton.Create("MANAGE", manage, canManage, "quiet"));
            details.Add(actions);
            if (!canAdd) details.Add(StyledLabel(addButton.tooltip, "document-modal-copy"));
            return card;
        }
        private void ComposeDistrictIndustryModal()
        {
            var d=FindSelectedRegionTile();if(d==null)return;CancelBrickworksPlacement();CancelDistrictSelectionPointer();
            var panel=CreateDocumentModal("INDUSTRY","Place a saved industrial Lot or choose a district resource industry. Lot requirements are checked when you place it.");
            panel.AddToClassList("district-industry-panel");
            panel.style.width=980;panel.style.maxWidth=Length.Percent(94);
            var scroll=new ScrollView(ScrollViewMode.Vertical)
            {
                name="district-industry-scroll",
                verticalScrollerVisibility=ScrollerVisibility.AlwaysVisible,
                horizontalScrollerVisibility=ScrollerVisibility.Hidden
            };
            scroll.AddToClassList("district-industry-scroll");panel.Add(scroll);
            AddSavedIndustrialLots(scroll);
            AddStoneIndustry(scroll,d);
            AddBrickworksIndustry(scroll,d);
            var coal=d.ResourceDeposits?.Where(p=>p.Kind=="coal").ToArray()??new DistrictResourceDeposit[0];
            scroll.Add(ComposeIndustryCard("coal", "COAL MINE",
                "CityForgeV3/NaturalResources/CoalV03/coal-iso",
                $"{coal.Count(p => !p.MineBuilt)} coal deposits · {coal.Count(p => p.MineBuilt)} mines",
                "ADD COAL MINE", () => BeginIndustryPlacement("coal"),
                Resources.Load<GameObject>(DistrictCoalMine.ResourcePath) != null && coal.Any(p => DistrictCoalMine.CanBuild(d,p,out _)),
                ComposeCoalManagement, coal.Any(p => p.MineBuilt)));
            var close=DocumentModalActions();close.Add(CfButton.Create("CLOSE",RemoveDocumentModal,true,"quiet"));panel.Add(close);
        }
        private static bool IsSavedIndustrialLot(LotSaveSummary summary) =>
            summary != null && summary.LotType == LotType.Industrial;

        private void AddSavedIndustrialLots(VisualElement scroll)
        {
            // Refresh at this user-opened catalog boundary. This avoids disk
            // polling during play while making a newly saved Lot visible.
            LotContentCatalog.InvalidateCache();
            var lots=LotContentCatalog.All.Where(IsSavedIndustrialLot).ToArray();
            if(lots.Length==0)
            {
                var empty=new VisualElement { name="industry-saved-lots-empty" };
                empty.AddToClassList("industry-resource-card");
                var details=new VisualElement();details.AddToClassList("industry-resource-details");empty.Add(details);
                details.Add(StyledLabel("SAVED INDUSTRIAL LOTS","inspector-title"));
                details.Add(StyledLabel("No saved Industrial Lots yet. Create and manually save one in the Lot Editor.","document-modal-copy"));
                scroll.Add(empty);return;
            }
            foreach(var summary in lots)
            {
                var captured=summary;
                var card=new VisualElement { name="industry-saved-lot-"+captured.LotId };
                card.AddToClassList("industry-resource-card");
                var image=new Image { image=LoadSavedLotPreview(captured.LotId),
                    scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore };
                image.AddToClassList("industry-resource-thumbnail");card.Add(image);
                var details=new VisualElement();details.AddToClassList("industry-resource-details");card.Add(details);
                details.Add(StyledLabel(captured.Name.ToUpperInvariant(),"inspector-title"));
                details.Add(StyledLabel($"Saved Industrial Lot · {captured.LotWidthCells} × {captured.LotDepthCells} cells · ${captured.PlopCost:N0}","document-modal-copy"));
                var actions=DocumentModalActions();
                var place=CfButton.Create("PLACE",()=>ArmDistrictLotPlacement(captured.LotId,captured.Name),true,"primary");
                place.name="industry-place-lot-"+captured.LotId;
                place.tooltip=$"Place {captured.Name}. Era, population, education, access, cost, and resource requirements are checked on the district.";
                actions.Add(place);details.Add(actions);scroll.Add(card);
            }
        }
        private void ComposeCoalManagement()
        {
            var d=FindSelectedRegionTile();if(d==null)return;
            var panel=CreateDocumentModal("COAL MINES","Manage your existing mines.");
            panel.style.width=760;panel.style.maxWidth=Length.Percent(94);
            var scroll=new ScrollView();scroll.style.maxHeight=480;panel.Add(scroll);
            foreach(var deposit in d.ResourceDeposits.Where(p=>p.Kind=="coal" && p.MineBuilt))
            {
                var row=DocumentModalActions();
                row.Add(StyledLabel("COAL MINE", "inspector-title"));
                row.Add(CfButton.Create("VIEW SITE",()=>FocusDistrictMine(deposit),true,"quiet"));
                row.Add(CfButton.Create("REMOVE MINE",()=>SetDistrictMine(deposit,false),true,"quiet"));scroll.Add(row);
            }
            panel.Add(CfButton.Create("BACK",ComposeDistrictIndustryModal,true,"quiet"));
        }
    }
}
