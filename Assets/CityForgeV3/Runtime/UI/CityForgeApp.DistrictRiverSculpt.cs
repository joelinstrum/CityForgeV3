using System;
using System.Collections.Generic;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        float _riverSculptRadius=80;
        int _riverSculptPointer=-1;
        readonly List<Vector2> _riverSculptStroke=new();
        PlacedDistrictRiver _riverRedrawSelection;
        float _riverLockedWidth;
        float? _riverShapeWidth;
        bool RiverSculptActive => _districtEditorMode==DistrictEditorMode.Terraform && _terraformCategory=="Water" &&
            (_terraformTool=="Shape River" || _terraformTool=="Soften River" || _terraformTool=="Erase River");
        RiverSculptMode RiverSculptModeForTool => _terraformTool=="Erase River"?RiverSculptMode.Erase:
            _terraformTool=="Soften River"?RiverSculptMode.Soften:RiverSculptMode.Shape;
        void RepairDistrictRivers()
        {
            var district=FindSelectedRegionTile();if(district==null)return;
            var draft=DistrictRiverSculpt.RepairAll(district);
            if(ReferenceEquals(draft,district.Rivers)){ShowDistrictNotice("Rivers are already smooth.");return;}
            CommitDistrictRiverSculpt(district,draft,validateBuildings:true);
        }
        void ComposeDistrictRiverModal()
        {
            var panel=CreateDocumentModal("RIVER TOOLS","Repair: rebuild smooth river paths throughout this district, preserving width, depth and endpoints. Shape: press on a river to lock its width, then trace a replacement section along it. Release to rebuild. Soften: brush rough sections. Erase: brush away part of a river. Border crossings stay anchored. Esc cancels a stroke.");
            panel.name="district-river-modal";panel.style.width=960;panel.style.maxWidth=Length.Percent(94);
            var radius=new Slider("BRUSH RADIUS (m)",20,200){value=_riverSculptRadius,showInputField=true,name="river-sculpt-radius"};
            radius.AddToClassList("environment-lighting-slider");
            radius.RegisterValueChangedCallback(e=>_riverSculptRadius=e.newValue);panel.Add(radius);
            var actions=DocumentModalActions();
            foreach(var tool in new[]{"Repair River","Shape River","Soften River","Erase River"})
            {
                var name=tool;var button=CfButton.Create(tool.ToUpperInvariant(),()=>
                {RemoveDocumentModal();if(name=="Repair River"){RepairDistrictRivers();return;}_terraformTool=name;Show(AppScreen.DistrictTerraform);},true,"primary");
                button.name="district-"+tool.ToLowerInvariant().Replace(' ','-');actions.Add(button);
            }
            actions.Add(CfButton.Create("CLOSE",RemoveDocumentModal,true,"quiet"));panel.Add(actions);
        }
        void AttachDistrictRiverSculpt(VisualElement screen,RegionCityTile district)
        {
            screen.focusable=true;
            bool WorldTarget(EventBase e)
            {
                var target=e.target as VisualElement;
                return target==screen || target?.ClassListContains("district-terraform-viewport")==true;
            }
            void Preview(Vector2 point,bool force=false)
            {
                if(_riverSculptPointer>=0)
                {
                    var size=new Vector2(DistrictScale.SizeMeters(district.Width),DistrictScale.SizeMeters(district.Height));
                    float spacing=RiverSculptModeForTool==RiverSculptMode.Shape?Mathf.Max(8,_riverLockedWidth*.2f):_riverSculptRadius/3;
                    if(_riverSculptStroke.Count==0)_riverSculptStroke.Add(point);
                    else
                    {
                        var last=_riverSculptStroke[_riverSculptStroke.Count-1];
                        float distance=Vector2.Scale(point-last,size).magnitude;
                        if(distance>=spacing || (force && distance>.1f))
                        {
                            int steps=RiverSculptModeForTool==RiverSculptMode.Shape?1:Mathf.Max(1,Mathf.CeilToInt(distance/spacing));
                            for(int i=1;i<=steps && _riverSculptStroke.Count<512;i++)_riverSculptStroke.Add(Vector2.Lerp(last,point,(float)i/steps));
                        }
                    }
                }
                // Pointer motion only records a bounded polyline and updates an untextured guide.
                // No edits, smoothing, mesh rebuilding, saving or terrain sampling here.
                _districtWorld.ShowRiverStrokePreview(_riverSculptStroke,point,
                    RiverSculptModeForTool==RiverSculptMode.Shape?(_riverSculptPointer>=0?_riverLockedWidth:(_riverShapeWidth??_riverSculptRadius*2)):_riverSculptRadius*2,
                    RiverSculptModeForTool==RiverSculptMode.Erase);
            }
            void Cancel()
            {
                int pointer=_riverSculptPointer;_riverSculptPointer=-1;
                if(pointer>=0 && screen.HasPointerCapture(pointer))screen.ReleasePointer(pointer);
                _riverSculptStroke.Clear();_riverRedrawSelection=null;_districtWorld?.HideRiverSculptPreview();
            }
            screen.RegisterCallback<PointerDownEvent>(e=>
            {
                if(!RiverSculptActive || e.button!=0 || !WorldTarget(e) || !_districtWorld.TryGroundPoint(DistrictCameraPoint(e.position),out var p))return;
                _riverRedrawSelection=RiverSculptModeForTool==RiverSculptMode.Shape?DistrictRiverEditing.FindAt(district,p):null;
                if(RiverSculptModeForTool==RiverSculptMode.Shape && _riverRedrawSelection==null){e.StopImmediatePropagation();return;}
                _riverLockedWidth=_riverShapeWidth??_riverRedrawSelection?.WidthMeters??_riverSculptRadius*2;
                var widthSlider=screen.Q<Slider>("river-shape-width");
                if(widthSlider!=null)widthSlider.SetValueWithoutNotify(_riverLockedWidth);
                var widthLabel=screen.Q<Label>("river-locked-width");
                if(widthLabel!=null)widthLabel.text=$"LOCKED WIDTH: {Mathf.RoundToInt(_riverLockedWidth)} m";
                _riverSculptPointer=e.pointerId;_riverSculptStroke.Clear();
                screen.CapturePointer(e.pointerId);screen.Focus();Preview(p,true);e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            screen.RegisterCallback<PointerMoveEvent>(e=>
            {
                if(!RiverSculptActive)return;
                if((_riverSculptPointer>=0 || WorldTarget(e)) && _districtWorld.TryGroundPoint(DistrictCameraPoint(e.position),out var p))Preview(p);
                else _districtWorld.HideRiverSculptPreview();
                if(_riverSculptPointer>=0 || WorldTarget(e))e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            screen.RegisterCallback<PointerUpEvent>(e=>
            {
                if(_riverSculptPointer!=e.pointerId || e.button!=0)return;
                if(_districtWorld.TryGroundPoint(DistrictCameraPoint(e.position),out var p))Preview(p,true);
                var draft=RiverSculptModeForTool==RiverSculptMode.Shape
                    ?DistrictRiverSculpt.Redraw(district,_riverRedrawSelection,_riverSculptStroke,_riverLockedWidth)
                    :DistrictRiverSculpt.Edit(district,_riverSculptStroke,_riverSculptRadius,RiverSculptModeForTool);
                Cancel();
                if(draft!=null && !draft.SequenceEqual(district.Rivers))CommitDistrictRiverSculpt(district,draft);
                e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            screen.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Escape && RiverSculptActive){Cancel();e.StopImmediatePropagation();}},TrickleDown.TrickleDown);
            screen.RegisterCallback<PointerCaptureOutEvent>(e=>{if(e.pointerId==_riverSculptPointer)Cancel();});
            screen.RegisterCallback<DetachFromPanelEvent>(_=>Cancel());
        }
        void CommitDistrictRiverSculpt(RegionCityTile district,List<PlacedDistrictRiver> draft,bool validateBuildings=false)
        {
            // Assess the proposed geometry before changing saved water or touching buildings.
            if(validateBuildings || RiverSculptModeForTool!=RiverSculptMode.Erase)
            {
                var paths=draft.Select(r=>new RegionRiverPath{WidthMeters=r.WidthMeters,Depth=r.Depth,
                    Points=r.Points.Select(p=>new DistrictRiverPoint(district.X+p.X*district.Width,district.Y+p.Z*district.Height)).ToList()}).ToList();
                if(FindRegionRiverBuildingConflict(_openRegion,paths)!=null)
                {var panel=CreateDocumentModal("RIVER SHAPING","That shape would cross a building. Try a smaller adjustment.");panel.Add(CfButton.Create("CLOSE",RemoveDocumentModal,true,"quiet"));return;}
            }
            var previous=district.Rivers;bool edited=district.RiversEditedLocally;
            try
            {
                district.Rivers=draft;district.RiversEditedLocally=true;
            }
            catch(Exception e)
            {
                district.Rivers=previous;district.RiversEditedLocally=edited;
                var panel=CreateDocumentModal("RIVER SHAPING","Could not apply river edit: "+e.Message);panel.Add(CfButton.Create("CLOSE",RemoveDocumentModal,true,"quiet"));return;
            }
            _districtUndo.Commit(JsonUtility.ToJson(district));
            _districtSelection.Clear();_districtWorld.ShowDistrictSelection(district,_districtSelection);
            _districtWorld.RebuildAllRiverPresentations(district,
                DistrictBulkRebuildReason.RiverGeometryReplacement,
                preservePresentations:true);
            _districtWorldCompositionKey=DistrictCompositionKey(district);_laborNavigation=null;
        }
    }
}
