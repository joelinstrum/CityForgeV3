using System;
using System.Collections.Generic;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        bool _drawingNationalPike,_pikeNaming;
        bool _drawingRiver;
        RegionRiverSize _riverSize;
        int _pikePointer=-1;
        RegionPikeStroke _pikeStroke;
        List<Vector2> _riverPreview=new();
        VisualElement _pikeOverlay,_pikeToolbar;
        Label _pikePencil,_pikeHint;

        void BeginNationalPike() => BeginRegionStroke(false,RegionRiverSize.Small);
        void BeginRegionRiver(RegionRiverSize size) => BeginRegionStroke(true,size);

        void BeginRegionStroke(bool river, RegionRiverSize size)
        {
            if(_openRegion==null||_currentScreen!=AppScreen.RegionEditor)return;
            CancelNationalPike();RemoveDocumentModal();
            _drawingRiver=river;_riverSize=size;
            var screen=_root.Q<VisualElement>(className:"region-editor-screen");
            var plane=screen?.Q<VisualElement>("region-map-plane");if(plane==null)return;
            _openRegion.MapLayers??=new();
            if(river)_openRegion.MapLayers.Rivers=true;else _openRegion.MapLayers.Transportation=true;
            var toggle=screen.Q<Toggle>(river?"region-layer-rivers":"region-layer-transportation");
            toggle?.SetValueWithoutNotify(true);
            var glyph=toggle?.Q<Label>("check-glyph");if(glyph!=null)glyph.text="✓";
            string drawingHint=river?(size.ToString().ToUpperInvariant()+" RIVER")+" · Click and drag. Release to apply. Keep drawing to add or connect rivers. Esc to finish.":"NATIONAL PIKE · Click and drag to draw. Release to name your road.";
            ApplyRegionMapLayers(screen,_openRegion.MapLayers);
            _drawingNationalPike=true;_pikeStroke=new RegionPikeStroke(_openRegion.Width,_openRegion.Height);
            _pikeOverlay=new VisualElement{name="region-pike-drawing",focusable=true};
            _pikeOverlay.style.position=Position.Absolute;_pikeOverlay.style.left=0;_pikeOverlay.style.top=0;
            _pikeOverlay.style.width=_openRegion.Width*RegionMapUnitPixels;_pikeOverlay.style.height=_openRegion.Height*RegionMapUnitPixels;
            _pikeOverlay.generateVisualContent+=context=>
            {
                if(_pikeStroke==null||_pikeStroke.Points.Count<2)return;
                var points=river?_riverPreview:_pikeStroke.Points;
                if(points.Count<2)return;
                var painter=context.painter2D;painter.strokeColor=river?new Color(.20f,.63f,.93f):new Color(1,.78f,.32f);painter.lineWidth=river?(size==RegionRiverSize.Major?14:size==RegionRiverSize.Large?7:3):5;painter.lineCap=LineCap.Round;painter.lineJoin=LineJoin.Round;
                painter.BeginPath();painter.MoveTo(points[0]*RegionMapUnitPixels);
                for(int i=1;i<points.Count;i++)painter.LineTo(points[i]*RegionMapUnitPixels);
                painter.Stroke();
            };
            plane.Add(_pikeOverlay);
            _pikePencil=new Label("✎"){name="region-pike-pencil",pickingMode=PickingMode.Ignore};_pikePencil.AddToClassList("region-pike-pencil");_pikePencil.style.display=DisplayStyle.None;screen.Add(_pikePencil);
            if(river)_pikePencil.style.color=new Color(.20f,.63f,.93f);
            _pikeToolbar=new VisualElement{name="region-pike-toolbar"};_pikeToolbar.AddToClassList("region-pike-toolbar");
            _pikeHint=StyledLabel(drawingHint,"region-pike-hint");_pikeToolbar.Add(_pikeHint);
            _pikeToolbar.Add(CfButton.Create("CANCEL · ESC",CancelNationalPike,true,"quiet"));screen.Add(_pikeToolbar);
            _pikeOverlay.RegisterCallback<PointerDownEvent>(e=>
            {
                if(e.button!=0||_pikeNaming||_pikePointer>=0)return;
                _pikeStroke.Points.Clear();_pikeHint.text=drawingHint;_pikePointer=e.pointerId;AddPikePoint(e.position);
                _pikeOverlay.Focus();_pikeOverlay.CapturePointer(e.pointerId);e.StopImmediatePropagation();
            });
            _pikeOverlay.RegisterCallback<PointerMoveEvent>(e=>
            {
                if(_pikeNaming)return;
                var point=screen.WorldToLocal(e.position);_pikePencil.style.left=point.x+3;_pikePencil.style.top=point.y-30;_pikePencil.style.display=DisplayStyle.Flex;
                if(_pikePointer==e.pointerId)AddPikePoint(e.position);
                e.StopPropagation();
            });
            _pikeOverlay.RegisterCallback<PointerLeaveEvent>(_=>{if(_pikePencil!=null)_pikePencil.style.display=DisplayStyle.None;});
            _pikeOverlay.RegisterCallback<PointerUpEvent>(e=>
            {
                if(e.button!=0||_pikePointer!=e.pointerId)return;
                AddPikePoint(e.position,true);int pointer=_pikePointer;_pikePointer=-1;_pikeOverlay.ReleasePointer(pointer);
                if(_pikeStroke.IsValid){if(river)CommitDrawnRegionRiver();else NameNationalPike();}
                else{_pikeStroke.Points.Clear();_pikeHint.text="Draw a longer "+(river?"river":"road")+": click, hold, and move across the region.";_pikeOverlay.MarkDirtyRepaint();}
                e.StopImmediatePropagation();
            });
            _pikeOverlay.RegisterCallback<PointerCaptureOutEvent>(e=>
            {if(_pikePointer==e.pointerId){_pikePointer=-1;_pikeStroke.Points.Clear();_pikeOverlay.MarkDirtyRepaint();}});
            _pikeOverlay.RegisterCallback<WheelEvent>(e=>{if(_pikePointer>=0)e.StopImmediatePropagation();},TrickleDown.TrickleDown);
            _pikeOverlay.RegisterCallback<ClickEvent>(e=>e.StopImmediatePropagation());
            _pikeOverlay.RegisterCallback<DetachFromPanelEvent>(_=>CancelNationalPike());
            _pikeOverlay.Focus();
        }

        void AddPikePoint(Vector2 panelPoint,bool final=false)
        {
            var point=_pikeOverlay.WorldToLocal(panelPoint)/RegionMapUnitPixels;
            if(_drawingRiver)
            {
                RegionRiverDrawing.AddPoint(_pikeStroke,_riverSize,point,final);
                _riverPreview=RegionRiverDrawing.Preview(_pikeStroke,_riverSize,_openRegion);
            }
            else _pikeStroke.Add(point,final);
            _pikeOverlay.MarkDirtyRepaint();
        }

        void NameNationalPike()
        {
            _pikeNaming=true;_pikePencil.style.display=DisplayStyle.None;_pikeToolbar.style.display=DisplayStyle.None;
            var panel=CreateDocumentModal("NAME YOUR NATIONAL PIKE","Give this road a name.");panel.name="region-pike-name-modal";
            var field=new TextField("Road name"){name="region-pike-name",maxLength=80};field.AddToClassList("region-pike-name");panel.Add(field);
            var error=StyledLabel("","inspector-note");error.style.display=DisplayStyle.None;panel.Add(error);
            var actions=DocumentModalActions();
            Button save=null;
            void Commit()
            {
                var route=_pikeStroke?.CreateRoute(field.value);if(route==null)return;
                var previous = new Dictionary<RegionCityTile,List<PlacedRoadPiece>>();
                _openRegion.TransportRoutes??=new();
                try
                {
                    var edits=NationalPikePlacement.Build(_openRegion,route);
                    foreach(var edit in edits){previous[edit.Key]=edit.Key.Roads;edit.Key.Roads=edit.Value;}
                    _openRegion.TransportRoutes.Add(route);
                }
                catch(Exception ex){foreach(var item in previous)item.Key.Roads=item.Value;_openRegion.TransportRoutes.Remove(route);error.text="Could not add this road: "+ex.Message;error.style.display=DisplayStyle.Flex;return;}
                var layer=_root.Q<RegionTransportationMapLayer>("region-transportation-map-layer");layer?.RefreshRouteLabels();layer?.MarkDirtyRepaint();
                CancelNationalPike();
            }
            save=CfButton.Create("ADD ROAD",Commit,true,"primary");save.name="region-pike-save";save.SetEnabled(false);
            field.RegisterValueChangedCallback(e=>save.SetEnabled(!string.IsNullOrWhiteSpace(e.newValue)));
            field.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter){Commit();e.StopImmediatePropagation();}});
            actions.Add(save);actions.Add(CfButton.Create("DRAW AGAIN",()=>{CancelNationalPike();BeginNationalPike();},true,"quiet"));
            actions.Add(CfButton.Create("CANCEL",CancelNationalPike,true,"quiet"));panel.Add(actions);
            field.schedule.Execute(()=>field.Focus()).ExecuteLater(30);
        }

        void CancelNationalPike()
        {
            if(!_drawingNationalPike)return;
            _drawingNationalPike=false;_drawingRiver=false;bool naming=_pikeNaming;_pikeNaming=false;
            var overlay=_pikeOverlay;_pikeOverlay=null;int pointer=_pikePointer;_pikePointer=-1;
            if(pointer>=0&&overlay!=null&&overlay.HasPointerCapture(pointer))overlay.ReleasePointer(pointer);
            overlay?.RemoveFromHierarchy();_pikeToolbar?.RemoveFromHierarchy();_pikePencil?.RemoveFromHierarchy();
            _pikeStroke=null;_pikeToolbar=null;_pikePencil=null;
            if(naming)RemoveDocumentModal();
        }
    }
}
