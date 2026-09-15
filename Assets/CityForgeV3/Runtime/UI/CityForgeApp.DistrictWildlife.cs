using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        readonly System.Collections.Generic.Dictionary<string, string> _wildlifeNoticeKeys = new();
        readonly DistrictNoticeLifetime _districtNotice = new();
        string _districtNoticeText = "";
        void ShowDistrictNotice(string message)
        {
            _districtNoticeText = message;
            _districtNotice.Show(Time.realtimeSinceStartupAsDouble);
            RefreshDistrictNotice();
        }
        void RefreshDistrictNotice()
        {
            var screen = _root?.Q<VisualElement>(className: "district-terraform-screen");
            if (screen == null) return;
            var alert = screen.Q<Label>("district-notice");
            if (!_districtNotice.Visible(Time.realtimeSinceStartupAsDouble))
            { alert?.RemoveFromHierarchy(); return; }
            if (alert == null)
            {
                alert = new Label { name = "district-notice", pickingMode = PickingMode.Ignore };
                alert.AddToClassList("district-notice");
                screen.Add(alert);
            }
            alert.text = _districtNoticeText;
        }
        void RefreshWildlifeAlert(RegionCityTile d)
        {
            var wildlife = DistrictWildlife.State(d);
            var key = wildlife.SightingCount + ":" + wildlife.Status;
            var districtKey = _openRegion.RegionId + ":" + d.TileId;
            if (_wildlifeNoticeKeys.TryGetValue(districtKey, out var previous) && previous == key) return;
            _wildlifeNoticeKeys[districtKey] = key;
            if (wildlife.Bears.Count > 0)
                ShowDistrictNotice(wildlife.Status + " · Labor → Frontier Marksman");
        }
        void ArmMarksman(string id=null)
        {CancelLaborPlacement();_placingMarksman=true;_movingMarksmanId=id;_pendingDistrictLotId="";_pendingDistrictFloraId="";RemoveDocumentModal();}
        void AddMarksmanLaborControls(VisualElement scroll,RegionCityTile d,VisualElement cards)
        {
            var card=CfButton.Create("",ComposeMarksmanDetails,true,"quiet");card.name="labor-marksman-card";card.AddToClassList("character-library-card");card.style.width=340;card.style.height=450;
            var image=new Image{image=Resources.Load<Texture2D>("CityForgeV3/UI/CharacterThumbnails/musketman-animated-v01"),scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};image.style.height=250;card.Add(image);
            card.Add(StyledLabel("FRONTIER MARKSMAN","character-library-name"));card.Add(StyledLabel("Warning shots · protects nearby workers","catalog-meta"));cards.Add(card);
            var wildlife=DistrictWildlife.State(d);scroll.Add(StyledLabel(wildlife.Status+$"\nMarksmen: ${DistrictWildlife.Wage:N0} each per season · total ${((long)wildlife.Marksmen.Count*DistrictWildlife.Wage):N0}/season","document-modal-copy"));
            var due=DistrictWildlife.WagesDue(d);
            if(due>0)scroll.Add(CfButton.Create($"PAY MARKSMEN WAGES · ${due:N0}",()=>{DistrictWildlife.PayWages(d);SaveDistrictEdit();ComposeDistrictLaborModal();},due<=d.Treasury,"primary"));
            foreach(var guard in wildlife.Marksmen)
            {
                var row=DocumentModalActions();row.Add(StyledLabel("Frontier marksman · "+guard.Shots+" warning shots"+(guard.WagesPaid?"":" · Wages due — off duty"),"document-modal-copy"));
                row.Add(CfButton.Create("FIND",()=>{RemoveDocumentModal();_terraformPanOffset=guard.Position;_districtWorld.SetPan(guard.Position);_districtWorld.SetZoom(DistrictZoomLevel.LOD0);},true,"quiet"));
                row.Add(CfButton.Create("MOVE",()=>ArmMarksman(guard.Id),true,"quiet"));
                row.Add(CfButton.Create("REMOVE",()=>{wildlife.Marksmen.Remove(guard);SaveDistrictEdit();ComposeDistrictLaborModal();},true,"quiet"));scroll.Add(row);
            }
        }
        void ComposeMarksmanDetails()
        {
            var panel=CreateDocumentModal("FRONTIER MARKSMAN","Keep mountain woodland safe for forestry crews.");
            panel.Add(StyledLabel("Bears occasionally appear among fir and mountain trees. Workers stop harvesting and retreat until the work area is clear. Place a marksman near the crew: his warning shot sends nearby bears away without harming them.","document-modal-copy"));
            panel.Add(StyledLabel("Protects a 55-meter area. After a warning shot, sightings are suppressed for five minutes. Move or remove him from the Labor menu.","document-modal-copy"));
            var d=FindSelectedRegionTile();if(d==null)return;
            panel.Add(StyledLabel($"${DistrictWildlife.Wage:N0} per marksman per season · due on placement ${DistrictWildlife.Wage:N0}. Seasonal wages renew automatically. If funds run out, he goes off duty until paid. Moving him is free; removing him stops future wages.","document-modal-copy"));
            var actions=DocumentModalActions();actions.Add(CfButton.Create("PLACE",()=>ArmMarksman(),d.Treasury>=DistrictWildlife.Wage,"primary"));actions.Add(CfButton.Create("BACK",ComposeDistrictLaborModal,true,"quiet"));panel.Add(actions);StyleLaborPanel(panel);
        }
    }
}
