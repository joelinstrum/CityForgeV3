using System;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private void AddRegionMapLayerMenu(VisualElement screen,VisualElement header)
        {
            var options=_openRegion.MapLayers??=new RegionMapLayers();
            var host=new VisualElement();host.AddToClassList("region-layers-control");header.Add(host);
            var menu=new VisualElement {name="region-map-layers-menu"};menu.AddToClassList("region-layers-menu");
            menu.style.display=DisplayStyle.None;
            Button button=null;
            void Close(){menu.style.display=DisplayStyle.None;button.text="MAP LAYERS ▾";}
            button=CfButton.Create("MAP LAYERS ▾",()=>
            {
                bool open=menu.style.display.value==DisplayStyle.None;
                menu.style.display=open?DisplayStyle.Flex:DisplayStyle.None;
                button.text=open?"MAP LAYERS ▴":"MAP LAYERS ▾";
            },true,"quiet");
            button.style.whiteSpace=WhiteSpace.NoWrap;button.style.minWidth=160;
            button.name="region-map-layers-button";button.tooltip="Choose which map layers are visible";host.Add(button);host.Add(menu);
            void Choice(string id,string label,bool value,Action<bool> set)
            {
                var toggle=CfCheckToggle.Create(label,value);
                toggle.name="region-layer-"+id;
                toggle.RegisterValueChangedCallback(e=>{set(e.newValue);ApplyRegionMapLayers(screen,options);});
                menu.Add(toggle);
            }
            Choice("towns","Towns & Cities",options.TownsAndCities,v=>options.TownsAndCities=v);
            Choice("districts","District Names & Borders",options.DistrictNamesAndBorders,v=>options.DistrictNamesAndBorders=v);
            Choice("rivers","Rivers",options.Rivers,v=>options.Rivers=v);
            Choice("topography","Topography",options.Topography,v=>options.Topography=v);
            Choice("transportation","Transportation",options.Transportation,v=>options.Transportation=v);
            var hint=StyledLabel("Highways & rail", "region-layers-hint");menu.Add(hint);
            screen.RegisterCallback<PointerDownEvent>(e=>
            {
                if(e.target is VisualElement target && !host.Contains(target))Close();
            },TrickleDown.TrickleDown);
            screen.RegisterCallback<KeyDownEvent>(e=>
            {
                if(e.keyCode!=KeyCode.Escape || menu.style.display.value==DisplayStyle.None)return;
                Close();button.Focus();e.StopImmediatePropagation();
            });
        }

        public static void ApplyRegionMapLayers(VisualElement screen,RegionMapLayers layers)
        {
            void Visible(string id,bool show)
            {var element=screen.Q<VisualElement>(id);if(element!=null)element.style.display=show?DisplayStyle.Flex:DisplayStyle.None;}
            Visible("region-place-labels",layers.TownsAndCities);
            Visible("region-district-labels",layers.DistrictNamesAndBorders);
            Visible("region-river-map-layer",layers.Rivers);
            Visible("region-topography-map-layer",layers.Topography);
            Visible("region-transportation-map-layer",layers.Transportation);
            foreach(var tile in screen.Query<Button>(className:"region-city-tile").ToList())
                tile.EnableInClassList("region-city-tile--map-hidden",!layers.DistrictNamesAndBorders);
            screen.Q<VisualElement>("region-map-plane")?.EnableInClassList("region-map--neutral",!layers.Topography);
        }
    }
}
