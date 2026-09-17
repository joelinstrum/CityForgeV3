using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        bool _placingBrickworks;float _brickworksYaw;Vector2 _brickworksHover;bool _hasBrickworksHover;
        void AddBrickworksIndustry(VisualElement scroll,RegionCityTile d)
        {
            bool unlocked=DistrictBrickworks.Unlocked(d);
            scroll.Add(ComposeIndustryCard("brickworks","BRICKWORKS","CityForgeV3/Industry/BrickworksV01/MenuThumbnail",
                unlocked?"Receives quarry wagons by road. Converts each delivered load immediately: 1 t stone becomes 1 t bricks.":"Requires a placed Stone Quarry.",
                "ADD BRICKWORKS",BeginBrickworksPlacement,unlocked&&Resources.Load<GameObject>(DistrictBrickworks.ResourcePath)!=null,
                ComposeBrickworksManagement,(d.Brickworks?.Count??0)>0));
        }
        void BeginBrickworksPlacement()
        {
            var d=FindSelectedRegionTile();if(!DistrictBrickworks.Unlocked(d)){ShowDistrictNotice("Place a Stone Quarry before building a Brickworks.");return;}
            CancelDistrictSelectionPointer();CancelLaborPlacement();CancelIndustryPlacement();RemoveDocumentModal();
            _pendingDistrictLotId="";_pendingDistrictFloraId="";
#if UNITY_EDITOR
            _districtResourceDebugPlacement="";
#endif
            _placingBrickworks=true;_brickworksYaw=0;_hasBrickworksHover=false;
            var button=CfButton.Create("CANCEL BRICKWORKS · ESC",CancelBrickworksPlacement,true,"quiet");button.name="brickworks-placement-cancel";
            button.style.position=Position.Absolute;button.style.right=24;button.style.top=78;_root.Q<VisualElement>(className:"district-terraform-screen").Add(button);
            ShowDistrictNotice("Place Brickworks on clear ground beside a road. R rotates; Esc cancels.");
        }
        void CancelBrickworksPlacement()
        {_placingBrickworks=false;_districtWorld?.HideBrickworksPlacement();_root?.Q<Button>("brickworks-placement-cancel")?.RemoveFromHierarchy();}
        void PreviewBrickworks(RegionCityTile d,Vector2 p)
        {_brickworksHover=p;_hasBrickworksHover=true;_districtWorld.ShowBrickworksPlacement(d,BrickworksCandidate(p));}
        void RotateBrickworksPreview()
        {_brickworksYaw=(_brickworksYaw+90)%360;if(_hasBrickworksHover)PreviewBrickworks(FindSelectedRegionTile(),_brickworksHover);}
        DistrictBrickworksSite BrickworksCandidate(Vector2 p)=>new(){NormalizedX=p.x,NormalizedZ=p.y,Yaw=_brickworksYaw};
        void PlaceBrickworks(RegionCityTile d,Vector2 p)
        {
            var site=BrickworksCandidate(p);var reason=_districtWorld.BrickworksPlacementReason(d,site);
            if(!string.IsNullOrEmpty(reason)){ShowDistrictNotice(reason);return;}
            EnsureDistrictUndo(d);
            if(!DistrictBrickworks.Build(d,site,LaborNavigation(d).Walkable))return;
            CancelBrickworksPlacement();SaveDistrictEdit();_districtWorldCompositionKey="";EnsureDistrictWorld(d);Show(AppScreen.DistrictTerraform);
            ShowDistrictNotice("Brickworks placed. Connect a road route from the quarry for stone deliveries.");
        }
        void ComposeBrickworksManagement()
        {
            var d=FindSelectedRegionTile();var panel=CreateDocumentModal("BRICKWORKS","Quarry wagons deliver stone here for firing into bricks.");
            var scroll=new ScrollView();scroll.style.maxHeight=480;panel.Add(scroll);
            foreach(var b in d.Brickworks??new())
            {
                var row=new VisualElement();row.style.paddingTop=18;row.style.paddingBottom=18;
                row.Add(new Label($"{(b.Enabled?"Firing enabled":"Paused")} · Stone awaiting firing: {b.StoneInput} t · Bricks produced: {b.BricksProduced} t"));
                var actions=DocumentModalActions();
                actions.Add(CfButton.Create("VIEW",()=>{RemoveDocumentModal();_terraformPanOffset=DistrictBrickworks.Point(d,b);_terraformZoomLevel=DistrictZoomLevel.LOD0;_districtWorld.SetPan(_terraformPanOffset);_districtWorld.SetZoom(_terraformZoomLevel);_districtWorld.WorldCamera.orthographicSize=20;},true,"quiet"));
                actions.Add(CfButton.Create(b.Enabled?"PAUSE":"RUN",()=>{b.Enabled=!b.Enabled;SaveDistrictEdit();ComposeBrickworksManagement();},true,"quiet"));
                actions.Add(CfButton.Create("REMOVE",()=>{EnsureDistrictUndo(d);d.Brickworks.Remove(b);SaveDistrictEdit();_districtWorldCompositionKey="";EnsureDistrictWorld(d);ComposeBrickworksManagement();},true,"quiet"));
                row.Add(actions);scroll.Add(row);
            }
            panel.Add(CfButton.Create("BACK",ComposeDistrictIndustryModal,true,"quiet"));
        }
        // Delivery warnings are read from the selected object's status callbacks.
        // Do not scan all quarry sites during routine HUD refreshes.

    }
}
