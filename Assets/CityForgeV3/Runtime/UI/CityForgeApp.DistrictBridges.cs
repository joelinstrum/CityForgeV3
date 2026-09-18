using System.Collections.Generic;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        bool _districtBridgeModalOpen;
        string _preferredDistrictBridge="covered-wood";
        bool DistrictBridgeOccupied(RegionCityTile d,Vector2 p)
        {
            int x=Mathf.FloorToInt((p.x+DistrictScale.SizeMeters(d.Width)*.5f)/10);
            int z=Mathf.FloorToInt((p.y+DistrictScale.SizeMeters(d.Height)*.5f)/10);
            return DistrictRoadLotOccupied(d,x,z)||_districtWorld.BridgeAt(p)!=null;
        }
        bool TryOfferDistrictBridge(RegionCityTile d,Vector2Int previous,Vector2Int next)
        {
            var point=DistrictBridgePlanner.Center(d,next);
            var existingBridge=_districtWorld.BridgeAt(point);
            if(existingBridge!=null)
            {
                if(next==existingBridge.Start || next==existingBridge.End)return false;
                EndRoadStrokeForBridge();return true;
            }
            if(!_districtWorld.SampleBridgeSurface(point).Channel)return false;
            EndRoadStrokeForBridge();
            if(DistrictBridgePlanner.TryPlan(d,previous,next-previous,_districtWorld.SampleBridgeSurface,
                p=>DistrictBridgeOccupied(d,p),out var proposal,out var reason))
            {
                proposal.RoadFamily=_builderTool;proposal.StyleId=_preferredDistrictBridge;
                ComposeDistrictBridgeModal(proposal);
            }
            else
            {
                var panel=CreateDocumentModal("BRIDGE CROSSING",reason);
                _districtBridgeModalOpen=true;
                var actions=DocumentModalActions();actions.Add(CfButton.Create("CLOSE",RemoveDocumentModal,true,"quiet"));panel.Add(actions);
            }
            return true;
        }
        void EndRoadStrokeForBridge()
        {
            _districtRoadPointerDown=false;_districtRoadStrokePath.Clear();_districtRoadStrokeAdded.Clear();
            _districtWorld?.HideLotPlacementGuide();_districtWorld?.CommitSurfaceChanges();
            var d=FindSelectedRegionTile();if(d!=null)_districtWorldCompositionKey=DistrictCompositionKey(d);
            SaveDistrictEdit();
        }
        void ComposeDistrictBridgeModal(PlacedDistrictBridge proposal=null)
        {
            var d=FindSelectedRegionTile();if(d==null)return;
            float length=proposal==null?0:Vector2.Distance(DistrictBridgePlanner.Center(d,proposal.Start),DistrictBridgePlanner.Center(d,proposal.End));
            var panel=CreateDocumentModal("BRIDGES",proposal==null?
                "Choose a bridge, then drag a road from dry ground across a river. You will review the crossing before building.":
                $"{length:N0} meter crossing • Approaches included. Choose a bridge to preview it over the river.");
            _districtBridgeModalOpen=true;
            panel.RegisterCallback<DetachFromPanelEvent>(_=>_districtBridgeModalOpen=false);
            panel.style.maxWidth=650;panel.style.maxHeight=new Length(85,LengthUnit.Percent);
            var list=new ScrollView(ScrollViewMode.Vertical);list.style.flexShrink=1;
            foreach(var style in DistrictBridgeCatalog.Styles)
            {
                var chosen=style;
                var card=new Button(()=>
                {
                    if((proposal?.StyleId??_preferredDistrictBridge)==chosen.Id)return;
                    _preferredDistrictBridge=chosen.Id;
                    if(proposal!=null)proposal.StyleId=chosen.Id;
                    ComposeDistrictBridgeModal(proposal);
                });
                card.AddToClassList("district-road-family-card");
                if((proposal?.StyleId??_preferredDistrictBridge)==style.Id)card.AddToClassList("district-road-family-card--selected");
                var image=new VisualElement();image.AddToClassList("district-road-family-preview");
                image.style.backgroundImage=new StyleBackground(Resources.Load<Texture2D>(style.Resource+"/preview"));card.Add(image);
                var copy=new VisualElement();copy.style.flexShrink=1;
                copy.Add(StyledLabel(style.Name.ToUpperInvariant(),"district-road-family-name"));
                copy.Add(StyledLabel(proposal==null?$"FROM ${style.BaseCost:N0} + ${style.CostPerMeter}/M":$"${style.Price(length):N0}","district-road-family-price"));
                var detail=StyledLabel(style.Description,"document-modal-copy");detail.style.whiteSpace=WhiteSpace.Normal;copy.Add(detail);card.Add(copy);list.Add(card);
            }
            panel.Add(list);
            if(proposal!=null)
            {
                panel.style.width=480;panel.style.marginRight=18;
                panel.parent.style.alignItems=Align.FlexEnd;
                panel.parent.style.backgroundColor=new Color(0,0,0,.12f);
                _districtWorld.PreviewDistrictBridge(d,proposal);
                panel.RegisterCallback<DetachFromPanelEvent>(_=>_districtWorld?.HideDistrictBridgePreview());
            }
            else if(d.Bridges?.Count>0)
            {
                var existing=new Foldout{text=$"Built bridges ({d.Bridges.Count})",value=false};
                // Explicit catalog management is the only enumeration; normal pointer/vehicle work uses the spatial index.
                foreach(var bridge in d.Bridges)
                {
                    var captured=bridge;var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;
                    row.Add(StyledLabel(DistrictBridgeCatalog.Find(bridge.StyleId)?.Name??bridge.StyleId,"document-modal-copy"));
                    row.Add(CfButton.Create("REMOVE",()=>ConfirmRemoveDistrictBridge(d,captured),true,"quiet"));existing.Add(row);
                }
                list.Add(existing);
            }
            var actions=DocumentModalActions();
            actions.Add(CfButton.Create("CANCEL",RemoveDocumentModal,true,"quiet"));
            if(proposal==null)
                actions.Add(CfButton.Create("DRAW CROSSING",()=>
                {
                    _builderCategory="Roads";if(!IsDistrictRoadToolActive())_builderTool=DistrictRoadPlacementModel.AntiqueBrickFamily;
                    RemoveDocumentModal();Show(AppScreen.DistrictTerraform);
                }));
            else
            {
                var price=DistrictBridgeCatalog.Find(proposal.StyleId).Price(length);
                int reserve=2*DistrictRoadPlacementModel.CostPerTile(proposal.RoadFamily);
                if(d.Treasury<price+reserve)panel.Add(StyledLabel($"Requires ${price+reserve:N0} including connecting road tiles.","document-modal-copy"));
                actions.Add(CfButton.Create($"BUILD — ${price:N0}",()=>BuildDistrictBridge(d,proposal),d.Treasury>=price+reserve));
            }
            panel.Add(actions);
        }
        void BuildDistrictBridge(RegionCityTile d,PlacedDistrictBridge b)
        {
            var direction=new Vector2Int(System.Math.Sign(b.End.x-b.Start.x),System.Math.Sign(b.End.y-b.Start.y));
            // Check the captured footprint again before making any edits (simulation may have continued behind the modal).
            var a=DistrictBridgePlanner.Center(d,b.Start);var z=DistrictBridgePlanner.Center(d,b.End);
            var axis=(z-a).normalized;var side=new Vector2(-axis.y,axis.x);int steps=Mathf.CeilToInt(Vector2.Distance(a,z)/2);
            for(int i=0;i<=steps;i++)for(int lane=-2;lane<=2;lane++)
                if(DistrictBridgeOccupied(d,Vector2.Lerp(a,z,(float)i/steps)+side*(lane*DistrictBridgePlanner.HalfWidth*.5f)))
                {RemoveDocumentModal();return;}
            int price=DistrictBridgeCatalog.Find(b.StyleId).Price(Vector2.Distance(a,z));
            if(d.Treasury<price+2*DistrictRoadPlacementModel.CostPerTile(b.RoadFamily))return;
            var session=DistrictRoadSession(d);int treasury=d.Treasury-price;
            session.TryPlace(b.Start.x,b.Start.y,DistrictScale.Columns(d.Width),DistrictScale.Columns(d.Height),b.RoadFamily,ref treasury);
            session.TryPlace(b.End.x,b.End.y,DistrictScale.Columns(d.Width),DistrictScale.Columns(d.Height),b.RoadFamily,ref treasury);
            if(b.RoadFamily==DistrictRoadPlacementModel.AntiqueBrickFamily && direction.x!=0 && direction.y!=0)
            {session.TryConnectDiagonal(b.Start-direction,b.Start);session.TryConnectDiagonal(b.End,b.End+direction);}
            d.Treasury=treasury;b.Cost=price;d.Bridges??=new List<PlacedDistrictBridge>();d.Bridges.Add(b);d.BridgeRevision++;
            _districtWorld.AddDistrictBridge(d,b);
            _districtWorld.RefreshRoadCellsAndNeighbors(d,new[]{b.Start,b.End},session.At);
            _districtWorld.CommitSurfaceChanges();_districtWorldCompositionKey=DistrictCompositionKey(d);SaveDistrictEdit();
            RemoveDocumentModal();
            var money=_root?.Q<Label>("district-simulation-money");if(money!=null)money.text=$"${d.Treasury:N0}";
        }
        void ConfirmRemoveDistrictBridge(RegionCityTile d,PlacedDistrictBridge b)
        {
            var panel=CreateDocumentModal("REMOVE BRIDGE?","The crossing will be removed. Connecting road tiles remain. Construction costs are not refunded.");
            _districtBridgeModalOpen=true;
            var actions=DocumentModalActions();actions.Add(CfButton.Create("CANCEL",()=>ComposeDistrictBridgeModal(),true,"quiet"));
            actions.Add(CfButton.Create("REMOVE",()=>
            {
                _districtWorld.RemoveDistrictBridge(d,b);d.Bridges.Remove(b);d.BridgeRevision++;
                _districtWorldCompositionKey=DistrictCompositionKey(d);SaveDistrictEdit();ComposeDistrictBridgeModal();
            }));panel.Add(actions);
        }
    }
}
