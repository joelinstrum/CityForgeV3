using System;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
  public sealed partial class CityForgeApp
  {
    private static readonly string[] ResourceNames = { "Wood", "Coal", "Stone", "Iron Ore", "Gold", "Oil", "Food", "Jewels", "Cloth", "Bricks" };
    private static readonly string[] ResourceKeys = { "wood", "coal", "stone", "iron-ore", "gold", "oil", "food", "jewels", "cloth", "bricks" };
#if UNITY_EDITOR
        private string _districtResourceDebugPlacement = "";
#endif
    private VisualElement ComposeDistrictResourceBar(RegionCityTile district)
    {
      var menu = CfButton.Create("RESOURCES", ComposeDistrictResourcesModal, true, "quiet");
      menu.name = "district-resource-bar"; menu.tooltip = "District resources";
      menu.style.position = Position.Absolute; menu.style.left = 170; menu.style.top = 78;
      menu.style.width = 180; menu.style.height = 44; menu.style.fontSize = 20;
      menu.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
      menu.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
      return menu;
    }
#if UNITY_EDITOR
        private bool DistrictResourceDebugMenuOpen => _root?.Q<VisualElement>("district-resource-debug-menu") != null;

        private void OpenDistrictResourceDebugMenu()
        {
            var district=FindSelectedRegionTile();if(district==null)return;
            CancelDistrictSelectionPointer();
            RemoveDocumentModal();
            var panel=CreateDocumentModal("DEBUG RESOURCES","Editor-only placement helper for district resource testing.");
            panel.name="district-resource-debug-menu";
            panel.style.width=620;panel.style.maxWidth=Length.Percent(94);panel.style.maxHeight=Length.Percent(88);
            var intro=StyledLabel("Choose a deposit, then click the district to place it. R or Escape cancels. Build at the deposit through INDUSTRY.","document-modal-copy");
            panel.Add(intro);
            var grid=new VisualElement();grid.style.flexDirection=FlexDirection.Row;grid.style.flexWrap=Wrap.Wrap;grid.style.justifyContent=Justify.Center;grid.style.marginTop=10;grid.style.marginBottom=10;panel.Add(grid);
            foreach(var resource in new[]{"stone","coal"})
            {
                var key=resource;
                var button=CfButton.Create(resource.ToUpperInvariant(),()=>{_districtResourceDebugPlacement=key;RemoveDocumentModal();},true,"quiet");
                button.style.minWidth=180;button.style.marginTop=8;button.style.marginRight=10;button.style.marginBottom=8;button.style.marginLeft=10;grid.Add(button);
            }
            var actions=DocumentModalActions();actions.Add(CfButton.Create("CLOSE",()=>{CloseDistrictResourceDebugMenu();},true,"quiet"));panel.Add(actions);
        }
        private void CloseDistrictResourceDebugMenu()
        {
            _districtResourceDebugPlacement="";
            RemoveDocumentModal();
        }
        private void PlaceDistrictResourceDebug(RegionCityTile district,Vector2 normalized,string resource)
        {
            if(district==null || (resource!="stone" && resource!="coal"))return;
            EnsureDistrictUndo(district);
            var size=DistrictScale.SizeMeters(district.Width);
            var world= new Vector2((normalized.x-.5f)*size,(normalized.y-.5f)*DistrictScale.SizeMeters(district.Height));
            switch(resource)
            {
                case "stone":
                    district.StoneSites ??= new();
                    if(!district.StoneSites.Any(site => Vector2.Distance(DistrictQuarry.Point(district,site),world) < 24f))
                        district.StoneSites.Add(new DistrictStoneSite{Id="debug-stone-"+Guid.NewGuid().ToString("N"),NormalizedX=normalized.x,NormalizedZ=normalized.y,Kind="stone",Built=false,Enabled=true,Phase="mining"});
                    break;
                case "coal":
                    DistrictNaturalResources.Ensure(district, new DistrictElevation(district));
                    district.ResourceDeposits ??= new();
                    if(!district.ResourceDeposits.Any(deposit => deposit.Kind=="coal" && Vector2.Distance(new Vector2((deposit.NormalizedX-.5f)*size,(deposit.NormalizedZ-.5f)*DistrictScale.SizeMeters(district.Height)),world) < 24f))
                        district.ResourceDeposits.Add(new DistrictResourceDeposit{Id="debug-coal-"+Guid.NewGuid().ToString("N"),Kind="coal",ManuallyPlaced=true,NormalizedX=normalized.x,NormalizedZ=normalized.y});
                    break;
            }
            SaveDistrictEdit();
            _districtWorldCompositionKey="";
            EnsureDistrictWorld(district);
            Show(AppScreen.DistrictTerraform);
        }
#endif
    private void ComposeDistrictResourcesModal()
    {
      var district = FindSelectedRegionTile(); if (district == null) return;
      CancelBrickworksPlacement();CancelDistrictSelectionPointer();
      var panel = CreateDocumentModal("RESOURCES", "");
      panel.style.width = 900; panel.style.maxWidth = Length.Percent(94); panel.style.maxHeight = Length.Percent(94);
      var hover = new Label(" ") { name = "district-resource-tooltip", pickingMode = PickingMode.Ignore };
      hover.style.fontSize = 28; hover.style.height = 42; hover.style.unityTextAlign = TextAnchor.MiddleCenter;
      var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.style.flexShrink = 1; panel.Add(scroll);
      var grid = new VisualElement(); grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; scroll.Add(grid);
      for (int i = 0; i < ResourceKeys.Length; i++)
      {
        var index = i; var title = ResourceNames[i];
        var card = new VisualElement { name = "resource-card-" + ResourceKeys[i], tooltip = title, focusable = true };
        card.style.width = Length.Percent(33.333f); card.style.height = 190; card.style.alignItems = Align.Center;
        var icon = new Image { image = Resources.Load<Texture2D>(ResourceKeys[i]=="bricks"?"CityForgeV3/Industry/BrickworksV01/MenuThumbnail":"CityForgeV3/Art/ResourceIconsV01/" + ResourceKeys[i]), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
        icon.style.width = 135; icon.style.height = 145; card.Add(icon);
        var amount = new Label(ResourceAmount(district, index)) { name = "district-resource-" + ResourceKeys[i], pickingMode = PickingMode.Ignore };
        amount.style.fontSize = 28; amount.style.unityFontStyleAndWeight = FontStyle.Bold; card.Add(amount);
        // UI Toolkit's built-in tooltip is editor-only; show an explicit runtime hover/focus caption.
        card.RegisterCallback<PointerEnterEvent>(_ => hover.text = title);
        card.RegisterCallback<PointerLeaveEvent>(_ => hover.text = " ");
        card.RegisterCallback<FocusInEvent>(_ => hover.text = title);
        card.RegisterCallback<FocusOutEvent>(_ => hover.text = " ");
        grid.Add(card);
      }
      panel.Add(hover);
      var actions = DocumentModalActions(); actions.Add(CfButton.Create("CLOSE", RemoveDocumentModal, true, "quiet")); panel.Add(actions);
    }
    private static string ResourceAmount(RegionCityTile district, int index)
    {
      var stock = district.ResourceInventory ??= new DistrictResourceInventory();
      int amount = index switch { 0 => DistrictLabor.State(district).Wood, 1 => stock.Coal, 2 => stock.Stone, 3 => stock.IronOre, 4 => stock.Gold, 5 => stock.Oil, 6 => stock.Food, 7 => stock.Jewels, 8 => stock.Cloth, 9 => stock.Bricks, _ => 0 };
      return $"{amount:N0} t";
    }
    private void RefreshDistrictResourceBar(RegionCityTile district)
    {
      for (int i = 0; i < ResourceKeys.Length; i++)
      {
        var label = _root?.Q<Label>("district-resource-" + ResourceKeys[i]);
        if (label != null) label.text = ResourceAmount(district, i);
      }
    }
  }
}
