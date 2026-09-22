using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
  public sealed partial class CityForgeApp
  {
    private const float RegionMapUnitPixels = 72f;
    private const float DistrictCellPixels = 8f;

    public bool OpenTerraformScaleQa() =>
        OpenDistrictScaleQa(DistrictEditorMode.Terraform);

    public bool OpenBuilderScaleQa() =>
        OpenDistrictScaleQa(DistrictEditorMode.Builder);

    public bool OpenGeneratedRiverQa()
    {
      if (!OpenDistrictScaleQa(DistrictEditorMode.Terraform))
        return false;
      var district = FindSelectedRegionTile();
      if (district == null) return false;
      var result = DistrictRiverGenerator.Generate(district,
          DistrictRiverDirection.WestToEast, 0.68f,
          DistrictRiverDepth.Shallow, 1785);
      if (result?.River == null) return false;
      district.Rivers.Add(result.River);
      _terraformCategory = "Water";
      _terraformTool = "Select Water";
      _districtSelection.Clear();
      _districtSelection.Add(new DistrictSelectionRef(
          DistrictSelectionKind.River, result.River.InstanceId));
      _terraformZoomLevel = DistrictZoomLevel.LOD3;
      _districtWorldCompositionKey = "";
      Show(AppScreen.DistrictTerraform);
      return true;
    }

    public bool OpenFortressDistrictPreviewQa()
    {
      if (_root == null && GetComponent<UIDocument>() == null)
        return false;
      EnsureLotWorld();
      _lotWorld.SetVisible(true);
      if (!_lotWorld.LoadLot("fortress-lot"))
        return false;
      _hasOpenLot = true;
      const string tileId = "fortress-preview-qa";
      _openRegion = new RegionSaveData
      {
        RegionId = "transient-fortress-preview-qa",
        Name = "Fortress Preview QA",
        Width = 4,
        Height = 4,
        Tiles = new List<RegionCityTile>
                {
                    new()
                    {
                        TileId = tileId,
                        Name = "Fortress District",
                        Width = 4,
                        Height = 4,
                        Founded = true,
                        FounderBuildingId = "fortress",
                        FounderBuildingName = "Fortress",
                        LotId = "fortress-lot",
                        TimeOfDay = TimeOfDayPreset.Afternoon,
                        FounderNormalizedX = 0.5f,
                        FounderNormalizedY = 0.5f
                    }
                }
      };
      _selectedRegionTileId = tileId;
      _districtEditorMode = DistrictEditorMode.Terraform;
      _terraformPanOffset = Vector2.zero;
      _terraformZoomLevel = DistrictZoomLevel.LOD0;
      Show(AppScreen.DistrictTerraform);
      return true;
    }

    private bool OpenDistrictScaleQa(DistrictEditorMode mode)
    {
      if (_root == null && GetComponent<UIDocument>() == null)
        return false;
      const string tileId = "district-scale-qa";
      _openRegion = new RegionSaveData
      {
        RegionId = "transient-district-scale-qa",
        Name = "District Scale QA",
        Width = 4,
        Height = 4,
        Tiles = new System.Collections.Generic.List<RegionCityTile>
                {
                    new()
                    {
                        TileId = tileId,
                        Name = "Large District",
                        Width = 4,
                        Height = 4
                    }
                }
      };
      _selectedRegionTileId = tileId;
      _terraformCategory = "Terrain";
      _terraformTool = "Raise";
      _districtEditorMode = mode;
      _builderCategory = "Roads";
      _builderTool = DistrictRoadPlacementModel.DirtFamily;
      _districtSimulationPaused = false;
      _pendingFounderBuildingId = "";
      _pendingDistrictLotId = "";
      _pendingDistrictLotIsTest = false;
      _pendingDistrictLotName = "";
      _hoveredDistrictLotInstanceId = "";
      _selectedDistrictLotInstanceId = "";
      _pendingDistrictFloraId = "";
      _pendingDistrictFloraMode = 0;
      _selectedDistrictFloraInstanceId = "";
      _activeDistrictRandomFloraGroupId = "";
      _districtFloraPointerDown = false;
      Time.timeScale = 1f;
      _terraformPanOffset = Vector2.zero;
      _terraformZoomLevel = DistrictZoom.DefaultLevel;
      Show(AppScreen.DistrictTerraform);
      return true;
    }

    private void ComposeManageRegionsModal()
    {
      RemoveDocumentModal();
      var overlay = new VisualElement { name = "document-modal" };
      overlay.AddToClassList("document-modal");
      overlay.AddToClassList("region-manage-overlay");
      var panel = new VisualElement { name = "manage-regions-panel" };
      panel.AddToClassList("region-manage-panel");
      panel.style.width = 1152f;
      panel.style.height = 1024f;
      var artwork = Resources.Load<Texture2D>(
          "CityForgeV3/Art/Regions/manage-regions");
      if (artwork != null)
        panel.style.backgroundImage = new StyleBackground(artwork);
      else
        Debug.LogError("Missing UI image resource: CityForgeV3/Art/Regions/manage-regions");

      var load = CfImageButton.Create(
          "Load Region",
          "CityForgeV3/Art/Regions/load-region-button",
          ComposeLoadRegionBrowser,
          true,
          "region-manage-action");
      load.AddToClassList("region-manage-action--load");
      load.style.position = Position.Absolute;
      load.style.left = 84f;
      load.style.top = 600f;
      load.style.width = 465f;
      load.style.height = 132f;
      load.style.opacity = 0.62f;
      load.RegisterCallback<PointerEnterEvent>(_ => load.style.opacity = 1f);
      load.RegisterCallback<PointerLeaveEvent>(_ => load.style.opacity = 0.62f);
      load.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
      panel.Add(load);

      var create = CfImageButton.Create(
          "Create New Region",
          "CityForgeV3/Art/Regions/create-region-button",
          ComposeCreateRegionDialog,
          true,
          "region-manage-action");
      create.AddToClassList("region-manage-action--create");
      create.style.position = Position.Absolute;
      create.style.left = 603f;
      create.style.top = 600f;
      create.style.width = 465f;
      create.style.height = 132f;
      create.style.opacity = 0.62f;
      create.RegisterCallback<PointerEnterEvent>(_ => create.style.opacity = 1f);
      create.RegisterCallback<PointerLeaveEvent>(_ => create.style.opacity = 0.62f);
      create.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
      panel.Add(create);

      var close = new Button(RemoveDocumentModal)
      {
        name = "Close Manage Regions",
        text = "",
        tooltip = "Close Manage Regions",
        focusable = true
      };
      close.AddToClassList("region-manage-close");
      close.pickingMode = PickingMode.Ignore;
      panel.Add(close);
      panel.RegisterCallback<ClickEvent>(evt =>
      {
        var width = Mathf.Max(1f, panel.resolvedStyle.width);
        var height = Mathf.Max(1f, panel.resolvedStyle.height);
        var normalizedX = evt.localPosition.x / width;
        var normalizedY = evt.localPosition.y / height;
        if (normalizedX >= 0.88f && normalizedY <= 0.2f)
          RemoveDocumentModal();
        else if (normalizedY >= 0.5f && normalizedX < 0.5f)
          ComposeLoadRegionBrowser();
        else if (normalizedY >= 0.5f)
          ComposeCreateRegionDialog();
        evt.StopPropagation();
      });
      overlay.Add(panel);
      AttachLargeRegionHoverHelp(overlay);
      _root.Add(overlay);
      close.schedule.Execute(close.Focus);
    }

    private void ComposeCreateRegionDialog()
    {
      var panel = CreateDocumentModal(
          "CREATE REGION",
          "Name your region and choose its footprint. City boundaries are generated immediately; detailed map tools can be added later.");
      var nameField = new TextField("REGION NAME")
      {
        value = "New Region"
      };
      nameField.name = "region-name-field";
      nameField.AddToClassList("document-field");
      panel.Add(nameField);

      var selectedSize = RegionSizePreset.Medium;
      var sizeButtons = new List<Button>();
      var sizeChoices = new VisualElement { name = "region-size-choices" };
      sizeChoices.AddToClassList("region-size-choices");
      var sizeSummary = StyledLabel("", "inspector-note");
      sizeSummary.name = "region-size-summary";

      void SelectSize(RegionSizePreset size)
      {
        selectedSize = size;
        foreach (var choice in sizeButtons)
          choice.EnableInClassList("is-selected",
              choice.name == "region-size-" + size.ToString().ToLowerInvariant());
        var dimensions = RegionSizeCatalog.Dimensions(size);
        sizeSummary.text =
            $"{size.ToString().ToUpperInvariant()}  •  {dimensions.x} × {dimensions.y} MAP UNITS  •  MIXED CITY SIZES";
      }

      foreach (var size in new[]
               {
                 RegionSizePreset.Small,
                 RegionSizePreset.Medium,
                 RegionSizePreset.Large
               })
      {
        var capturedSize = size;
        var dimensions = RegionSizeCatalog.Dimensions(size);
        var choice = CfButton.Create(
            $"{size.ToString().ToUpperInvariant()}\n{dimensions.x} × {dimensions.y}",
            () => SelectSize(capturedSize), true, "secondary");
        choice.name = "region-size-" + size.ToString().ToLowerInvariant();
        choice.AddToClassList("region-size-choice");
        if (size == RegionSizePreset.Large)
          choice.AddToClassList("region-size-choice--last");
        choice.tooltip = $"Create a {size.ToString().ToLowerInvariant()} {dimensions.x} by {dimensions.y} region";
        sizeButtons.Add(choice);
        sizeChoices.Add(choice);
      }
      panel.Add(sizeChoices);
      panel.Add(sizeSummary);
      SelectSize(selectedSize);

      var actions = DocumentModalActions();
      var create = CfButton.Create("CREATE REGION", () =>
      {
        _openRegion = RegionSaveStore.Create(nameField.value, selectedSize);
        _selectedRegionTileId = "";
        _lastRegionRepeatAction = RegionRepeatAction.None;
        _regionMapScrollOffset = Vector2.zero;
        _regionMapScrollInitialized = false;
        _openRegionWasCreatedThisSession = true;
        RemoveDocumentModal();
        Show(AppScreen.RegionEditor);
      }, true, "primary");
      create.name = "create-region-confirm";
      actions.Add(create);
      actions.Add(CfButton.Create("CANCEL", RemoveDocumentModal,
          true, "quiet"));
      panel.Add(actions);
      nameField.schedule.Execute(nameField.Focus);
    }

    private void ComposeLoadRegionBrowser()
    {
      RemoveDocumentModal();
      var saves = RegionSaveStore.List();
      var overlay = new VisualElement { name = "document-modal" };
      overlay.AddToClassList("document-modal");
      overlay.AddToClassList("region-manage-overlay");
      var panel = new VisualElement { name = "open-region-panel" };
      panel.AddToClassList("open-region-panel");
      panel.Add(StyledLabel("LOAD REGION", "open-region-title"));

      if (saves.Count > 0)
      {
        var list = new ScrollView(ScrollViewMode.Vertical)
        {
          name = "region-save-list",
          verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible
        };
        list.AddToClassList("region-save-list");
        foreach (var summary in saves)
        {
          var captured = summary;
          var entry = new VisualElement();
          entry.AddToClassList("region-save-entry");

          var preview = new VisualElement();
          preview.AddToClassList("region-save-preview");
          var previewArtwork = Resources.Load<Texture2D>(
              "CityForgeV3/Art/MainMenu/background");
          if (previewArtwork != null)
            preview.style.backgroundImage = new StyleBackground(previewArtwork);
          entry.Add(preview);

          var details = new VisualElement();
          details.AddToClassList("region-save-details");
          details.Add(StyledLabel(captured.Name,
              "region-save-entry-title"));
          details.Add(StyledLabel(
              $"▰  {captured.TileCount} CITY AREAS     │     ▣  {captured.ModifiedUtc.ToLocalTime():MMM d, yyyy}",
              "region-save-entry-meta"));
          entry.Add(details);

          var load = CfButton.Create("▰  LOAD", () =>
          {
            _openRegion = RegionSaveStore.Load(captured.RegionId);
            if (_openRegion == null) return;
            _selectedRegionTileId = "";
            _lastRegionRepeatAction = RegionRepeatAction.None;
            _regionMapScrollOffset = Vector2.zero;
            _regionMapScrollInitialized = false;
            _openRegionWasCreatedThisSession = false;
            RemoveDocumentModal();
            Show(AppScreen.RegionEditor);
          }, true, "region-load");
          load.tooltip = $"Load {captured.Name}";
          entry.Add(load);
          var delete = CfButton.Create("DELETE", () =>
              ComposeDeleteRegionDialog(captured), true, "danger");
          delete.name = $"delete-region-{captured.RegionId}";
          delete.tooltip = $"Delete {captured.Name}";
          delete.style.marginLeft = 12;
          delete.style.flexShrink = 0;
          entry.Add(delete);
          list.Add(entry);
        }
        panel.Add(list);
      }
      else
      {
        var empty = StyledLabel(
            "NO SAVED REGIONS YET\n\nCreate a new region to begin building.",
            "open-region-empty");
        panel.Add(empty);
      }

      var cancel = CfImageButton.Create(
          "Cancel",
          "CityForgeV3/Art/Regions/cancel-button",
          RemoveDocumentModal,
          true,
          "region-cancel");
      panel.Add(cancel);

      var close = new Button(RemoveDocumentModal)
      {
        name = "Close Open Region",
        text = "×",
        tooltip = "Close Open Region",
        focusable = true
      };
      close.AddToClassList("open-region-close");
      panel.Add(close);
      overlay.Add(panel);
      AttachLargeRegionHoverHelp(overlay);
      _root.Add(overlay);
      close.schedule.Execute(close.Focus);
    }

    private void ComposeDeleteRegionDialog(RegionSaveSummary summary)
    {
      var panel = CreateDocumentModal("DELETE REGION?",
          $"Delete “{summary.Name}” and all of its districts? Terrain, placed buildings, and progress in this region will be removed. This cannot be undone.");
      var error = StyledLabel("", "inspector-note");
      error.name = "region-delete-error";
      panel.Add(error);
      var actions = DocumentModalActions();
      var confirm = CfButton.Create("DELETE REGION", () =>
      {
        try
        {
          RegionSaveStore.Delete(summary.RegionId);
        }
        catch (Exception exception)
        {
          error.text = "Could not delete this region. Your region is still available. " + exception.Message;
          return;
        }
        if (_openRegion?.RegionId == summary.RegionId)
        {
          _openRegion = null;
          _selectedRegionTileId = "";
          _lastRegionRepeatAction = RegionRepeatAction.None;
          _openRegionWasCreatedThisSession = false;
          _regionMapScrollOffset = Vector2.zero;
          _regionMapScrollInitialized = false;
          ClearDistrictUndo();
          Show(AppScreen.MainMenu);
        }
        ComposeLoadRegionBrowser();
      }, true, "danger");
      confirm.name = "confirm-delete-region";
      actions.Add(confirm);
      var cancel = CfButton.Create("CANCEL", ComposeLoadRegionBrowser, true, "quiet");
      cancel.name = "cancel-delete-region";
      actions.Add(cancel);
      panel.Add(actions);
      cancel.schedule.Execute(cancel.Focus);
    }

    private void ComposeRegionEditor()
    {
      if (_openRegion == null)
      {
        Show(AppScreen.MainMenu);
        return;
      }

      var screen = Screen("region-editor-screen");
      screen.AddToClassList("cf-map-screen");
      var header = ComposeRegionChrome(screen);

      var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
      {
        name = "region-map-scroll",
        horizontalScrollerVisibility = ScrollerVisibility.AlwaysVisible,
        verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible
      };
      scroll.AddToClassList("region-map-scroll");
      var map = new VisualElement { name = "region-map" };
      map.AddToClassList("region-map");
      var planeWidth = _openRegion.Width * RegionMapUnitPixels;
      var planeHeight = _openRegion.Height * RegionMapUnitPixels;
      // The plane rotates about its center before the parent compresses Y.
      // Keep all four rotated corners inside the scroll content, with room
      // around the perimeter; otherwise its northern corner is clipped at y=0.
      const float mapPadding = 160f;
      const float projectionScaleY = RegionMapProjection.VerticalScale;
      const float slabDepth = 22f;
      var rotatedExtent = (planeWidth + planeHeight) * Mathf.Sqrt(0.5f);
      var planeLeft = mapPadding + (rotatedExtent - planeWidth) * 0.5f;
      var planeTop = mapPadding / projectionScaleY +
          (rotatedExtent - planeHeight) * 0.5f;
      var mapWidth = rotatedExtent + mapPadding * 2f;
      var mapHeight = (rotatedExtent + slabDepth) * projectionScaleY + mapPadding * 2f;
      map.style.width = mapWidth;
      map.style.height = mapHeight;
      map.style.flexShrink = 0f;

      var projection = new VisualElement { name = "region-map-projection" };
      projection.AddToClassList("region-map-projection");
      projection.style.width = mapWidth;
      projection.style.height = mapHeight / projectionScaleY;

      var slab = new VisualElement { name = "region-map-slab" };
      slab.AddToClassList("region-map-plane");
      slab.AddToClassList("region-map-slab");
      slab.style.left = planeLeft;
      slab.style.top = planeTop + slabDepth;
      slab.style.width = planeWidth;
      slab.style.height = planeHeight;
      RegionMapProjection.ApplyGround(slab, planeWidth, planeHeight);
      projection.Add(slab);

      var plane = new VisualElement { name = "region-map-plane" };
      plane.AddToClassList("region-map-plane");
      plane.AddToClassList("region-map-grass");
      plane.style.left = planeLeft;
      plane.style.top = planeTop;
      plane.style.width = planeWidth;
      plane.style.height = planeHeight;
      RegionMapProjection.ApplyGround(plane, planeWidth, planeHeight);
      plane.Add(new RegionTopographyMapLayer(_openRegion, RegionMapUnitPixels));
      foreach (var tile in _openRegion.Tiles)
      {
        var captured = tile;
        var button = new Button(() => SelectRegionTile(captured.TileId))
        {
          name = $"region-tile-{tile.TileId}",
          text = "",
          tooltip = $"{(tile.Founded ? tile.Name : "Not started")} — click to enter this {tile.Width} by {tile.Height} district."
        };
        button.AddToClassList("region-city-tile");
        button.AddToClassList($"region-city-tone-{RegionTileTone(tile)}");
        if (tile.TileId == _selectedRegionTileId)
          button.AddToClassList("region-city-tile--selected");
        button.style.position = Position.Absolute;
        button.style.left = tile.X * RegionMapUnitPixels;
        button.style.top = tile.Y * RegionMapUnitPixels;
        button.style.width = tile.Width * RegionMapUnitPixels;
        button.style.height = tile.Height * RegionMapUnitPixels;

        plane.Add(button);
      }
      plane.Add(new RegionRiverMapLayer(_openRegion, RegionMapUnitPixels));
      plane.Add(new RegionTransportationMapLayer(_openRegion, RegionMapUnitPixels));
      var labels = new VisualElement { name = "region-place-labels", pickingMode = PickingMode.Ignore };
      labels.style.position = Position.Absolute; labels.style.left = 0; labels.style.top = 0;
      labels.style.width = planeWidth; labels.style.height = planeHeight;
      plane.Add(labels);
      var districtLabels = new VisualElement { name = "region-district-labels", pickingMode = PickingMode.Ignore };
      districtLabels.style.position = Position.Absolute; districtLabels.style.left = 0; districtLabels.style.top = 0;
      districtLabels.style.width = planeWidth; districtLabels.style.height = planeHeight;
      plane.Add(districtLabels);
      foreach (var tile in _openRegion.Tiles)
      {
        if (!tile.Founded) continue;
        var labelHost = new VisualElement { pickingMode = PickingMode.Ignore };
        labelHost.style.position = Position.Absolute;
        labelHost.style.left = tile.X * RegionMapUnitPixels;
        labelHost.style.top = tile.Y * RegionMapUnitPixels;
        labelHost.style.width = tile.Width * RegionMapUnitPixels;
        labelHost.style.height = tile.Height * RegionMapUnitPixels;
        AddRegionPlaceLabel(labelHost, tile, _openRegion.Width, _openRegion.Height);
        (tile.Designation == RegionPlaceDesignation.Town ? labels : districtLabels).Add(labelHost);
      }
      projection.Add(plane);
      map.Add(projection);
      scroll.Add(map);
      screen.Add(scroll);

      RefreshRegionInspector(screen);
      screen.Q("region-tool-rail")?.BringToFront();
      screen.Q("region-intent-dock")?.BringToFront();
      screen.Q("map-layer-dock")?.BringToFront();
      header.BringToFront();
      ApplyRegionMapLayers(screen, _openRegion.MapLayers);
      AttachLargeRegionHoverHelp(screen);
      _root.Add(screen);
      scroll.schedule.Execute(() =>
      {
        if (!_regionMapScrollInitialized)
        {
          var viewportWidth = scroll.contentViewport.resolvedStyle.width;
          var viewportHeight = scroll.contentViewport.resolvedStyle.height;
          var projectedCenter = new Vector2(
                    planeLeft + planeWidth * 0.5f,
                    (planeTop + planeHeight * 0.5f) * projectionScaleY);
          _regionMapScrollOffset = new Vector2(
                    Mathf.Clamp(projectedCenter.x - viewportWidth * 0.5f,
                        0f, Mathf.Max(0f, map.resolvedStyle.width - viewportWidth)),
                    Mathf.Clamp(projectedCenter.y - viewportHeight * 0.5f,
                        0f, Mathf.Max(0f, map.resolvedStyle.height - viewportHeight)));
          _regionMapScrollInitialized = true;
        }
        scroll.scrollOffset = _regionMapScrollOffset;
      });
    }

    private static void AddRegionPlaceLabel(VisualElement tileElement,
        RegionCityTile tile, int regionWidth, int regionHeight)
    {
      if (tileElement == null || tile == null || !tile.Founded ||
          string.IsNullOrWhiteSpace(tile.Name)) return;
      var marker = RegionMapProjection.CreateLabelAnchor(regionWidth, regionHeight, out var content);
      marker.name = $"region-place-marker-{tile.TileId}";
      marker.style.left = Length.Percent(50);
      marker.style.top = Length.Percent(50);
      var name = new Label(tile.Name.Trim())
      {
        name = $"region-place-name-{tile.TileId}",
        pickingMode = PickingMode.Ignore
      };
      if (tile.Designation == RegionPlaceDesignation.Town)
      {
        // Keep the outline on a separate label. At map scale TextCore's thick
        // outline can consume the small glyph face even when color is opaque.
        // Drawing the ivory face afterward guarantees a solid readable fill.
        var outline = new Label(tile.Name.Trim())
        {
          name = $"region-place-name-outline-{tile.TileId}",
          pickingMode = PickingMode.Ignore
        };
        outline.AddToClassList("region-place-name");
        outline.AddToClassList("region-town-name-outline");
        outline.AddToClassList("region-horizontal-place-name");
        outline.style.color = new StyleColor(Color.black);
        content.Add(outline);
      }
      name.AddToClassList("region-place-name");
      name.AddToClassList(tile.Designation == RegionPlaceDesignation.Town
          ? "region-town-name" : "region-district-name");
      name.AddToClassList("region-horizontal-place-name");
      if (tile.Designation == RegionPlaceDesignation.Town)
        name.style.color = new StyleColor(
            new Color32(239, 236, 215, 255));
      content.Add(name);
      if (tile.Designation == RegionPlaceDesignation.Town)
      {
        var dot = new VisualElement
        {
          name = $"region-town-dot-{tile.TileId}",
          pickingMode = PickingMode.Ignore
        };
        dot.AddToClassList("region-town-dot");
        dot.style.position = Position.Absolute;
        dot.style.left = -3.5f;
        dot.style.top = -2;
        dot.style.marginTop = 0;
        content.Add(dot);
      }
      tileElement.Add(marker);
    }

    private static void AddRegionGrassTiles(VisualElement plane,
        Texture2D grass, float width, float height)
    {
      const float patchSize = 256f;
      for (var y = 0f; y < height; y += patchSize)
        for (var x = 0f; x < width; x += patchSize)
        {
          var patch = new VisualElement();
          patch.AddToClassList("region-grass-patch");
          patch.pickingMode = PickingMode.Ignore;
          patch.style.left = x;
          patch.style.top = y;
          patch.style.width = Mathf.Min(patchSize + 1f, width - x + 1f);
          patch.style.height = Mathf.Min(patchSize + 1f, height - y + 1f);
          patch.style.backgroundImage = new StyleBackground(grass);
          plane.Add(patch);
        }
    }

    private void PollRegionArrowKeys()
    {
      if (_drawingNationalPike && Input.GetKeyDown(KeyCode.Escape)) { CancelNationalPike(); return; }
      if (_pikePointer >= 0 || _pikeNaming) return;
      if (Input.GetKeyDown(KeyCode.Escape))
      {
        if (_root.Q("document-modal") != null) RemoveDocumentModal();
        else PreviewRegionTile("");
        return;
      }
      if (MapPanningBlocked) return;
      var delta = Vector2.zero;
      if (Input.GetKeyDown(KeyCode.LeftArrow)) delta.x = -140f;
      else if (Input.GetKeyDown(KeyCode.RightArrow)) delta.x = 140f;
      if (Input.GetKeyDown(KeyCode.UpArrow)) delta.y = -100f;
      else if (Input.GetKeyDown(KeyCode.DownArrow)) delta.y = 100f;
      if (delta == Vector2.zero) return;

      var scroll = _root?.Q<ScrollView>("region-map-scroll");
      var map = _root?.Q<VisualElement>("region-map");
      if (scroll == null || map == null) return;
      var viewportWidth = scroll.contentViewport.resolvedStyle.width;
      var viewportHeight = scroll.contentViewport.resolvedStyle.height;
      var maximum = new Vector2(
          Mathf.Max(0f, map.resolvedStyle.width - viewportWidth),
          Mathf.Max(0f, map.resolvedStyle.height - viewportHeight));
      _regionMapScrollOffset = new Vector2(
          Mathf.Clamp(scroll.scrollOffset.x + delta.x, 0f, maximum.x),
          Mathf.Clamp(scroll.scrollOffset.y + delta.y, 0f, maximum.y));
      _regionMapScrollInitialized = true;
      scroll.scrollOffset = _regionMapScrollOffset;
    }

    private void SelectRegionTile(string tileId)
    {
      var scroll = _root?.Q<ScrollView>("region-map-scroll");
      if (scroll != null)
      {
        _regionMapScrollOffset = scroll.scrollOffset;
        _regionMapScrollInitialized = true;
      }
      _selectedRegionTileId = tileId ?? "";
      _terraformCategory = "Select";
      _terraformTool = "Select";
      _districtEditorMode = DistrictEditorMode.Terraform;
      _builderCategory = "Select";
      _builderTool = DistrictRoadPlacementModel.DirtFamily;
      _districtPaletteOpen = false;
      _districtPaletteCategoryOpen = false;
      _districtSimulationPaused = false;
      _pendingFounderBuildingId = "";
      _pendingDistrictLotId = "";
      _pendingDistrictLotIsTest = false;
      _pendingDistrictLotName = "";
      _hoveredDistrictLotInstanceId = "";
      _selectedDistrictLotInstanceId = "";
      _pendingDistrictFloraId = "";
      _pendingDistrictFloraMode = 0;
      _selectedDistrictFloraInstanceId = "";
      _activeDistrictRandomFloraGroupId = "";
      _districtFloraPointerDown = false;
      Time.timeScale = 1f;
      _terraformPanOffset = Vector2.zero;
      _terraformZoomLevel = DistrictZoom.DefaultLevel;
      Show(AppScreen.DistrictTerraform);
    }

    private void ComposeDistrictTerraform()
    {
      _districtEdgePanDirection = Vector2Int.zero;
      var district = FindSelectedRegionTile();
      if (_openRegion == null || district == null)
      {
        _districtSimulationPaused = false;
        Time.timeScale = 1f;
        Show(AppScreen.RegionEditor);
        return;
      }

      var screen = Screen("district-terraform-screen");
      screen.AddToClassList("cf-map-screen");
      _districtEdgePanDirection = Vector2Int.zero;
      screen.RegisterCallback<PointerMoveEvent>(evt => UpdateDistrictEdgePan(screen, evt), TrickleDown.TrickleDown);
      screen.RegisterCallback<PointerLeaveEvent>(evt =>
      {
        if (evt.target == screen) _districtEdgePanDirection = Vector2Int.zero;
      });
      var viewport = new VisualElement();
      viewport.AddToClassList("district-terraform-viewport");
      viewport.pickingMode = PickingMode.Ignore;
      screen.Add(viewport);
      _districtSelectionMarquee = new VisualElement
      {
        name = "district-selection-marquee",
        pickingMode = PickingMode.Ignore
      };
      _districtSelectionMarquee.style.position = Position.Absolute;
      _districtSelectionMarquee.style.borderLeftWidth = 1f;
      _districtSelectionMarquee.style.borderRightWidth = 1f;
      _districtSelectionMarquee.style.borderTopWidth = 1f;
      _districtSelectionMarquee.style.borderBottomWidth = 1f;
      var marqueeBlue = new Color(.35f, .82f, 1f, .95f);
      _districtSelectionMarquee.style.borderLeftColor = marqueeBlue;
      _districtSelectionMarquee.style.borderRightColor = marqueeBlue;
      _districtSelectionMarquee.style.borderTopColor = marqueeBlue;
      _districtSelectionMarquee.style.borderBottomColor = marqueeBlue;
      _districtSelectionMarquee.style.backgroundColor =
          new Color(.25f, .72f, 1f, .07f);
      _districtSelectionMarquee.style.display = DisplayStyle.None;
      screen.Add(_districtSelectionMarquee);
      var districtColumns = DistrictScale.Columns(district.Width);
      var districtRows = DistrictScale.Columns(district.Height);
      if (DistrictWorldNeedsBuild(district))
      {
        ComposeDistrictPreloader(screen, district);
        return;
      }
      EnsureDistrictWorld(district);
      EnsureDistrictUndo(district);
      RefreshSelectedDistrictLotOutline(district);
      SelectSoleDistrictRiverIfNeeded(district);
      _districtWorld.ShowDistrictSelection(district,
          _districtSelection);
      AttachDistrictRiverSculpt(screen, district);
      screen.RegisterCallback<PointerMoveEvent>(evt =>
      {
        if (_lotNudge != null) { UpdateLotNudge(DistrictCameraPoint(evt.position)); evt.StopImmediatePropagation(); return; }
        if (_districtMarqueeActive)
        {
          if (evt.pointerId == _districtSelectionPointerId)
            UpdateDistrictSelectionMarquee(evt.position);
          evt.StopImmediatePropagation();
          return;
        }
        var target = evt.target as VisualElement;
        if (target?.name == "district-resource-bar" || evt.target is Button ||
                  target?.GetFirstAncestorOfType<Button>() != null ||
                  _districtWorld == null ||
                  !_districtWorld.TryGroundPoint(
                      DistrictCameraPoint(evt.position),
                      out var normalized))
        {
          SetDistrictRoadDeleteCursor(false);
          _districtWorld?.HideAxemanPlacement(); UnityEngine.Cursor.visible = true;
          FinishDistrictFloraPaint(district);
          _districtWorld?.HideLotPlacementGuide();
          RefreshSelectedDistrictLotOutline(district);
          return;
        }

        if(_placingBrickworks)
        {PreviewBrickworks(district,normalized);evt.StopImmediatePropagation();return;}
        if (_pendingLaborCount > 0 || _placingMarksman)
        {
          _districtWorld.ShowAxemanPlacement(normalized, LaborDropValid(district, normalized), _placingMarksman);
          UnityEngine.Cursor.visible = false;
          evt.StopImmediatePropagation(); return;
        }
        if (_districtFloraPainting)
        {
          if ((evt.pressedButtons & 1) == 0) FinishDistrictFloraPaint(district);
          else ContinueDistrictFloraPaint(district, normalized);
          return;
        }

        if (!string.IsNullOrWhiteSpace(_pendingFounderBuildingId))
        {
          _districtWorld.HideLotOutline();
          if (TryFounderFootprint(district, normalized.x, normalized.y,
              out var founderX, out var founderZ, out var founderWidth, out var founderDepth))
            _districtWorld.ShowLotPlacementGuide(founderX, founderZ, founderWidth, founderDepth, true);
          else _districtWorld.HideLotPlacementGuide();
          return;
        }

        if (!string.IsNullOrWhiteSpace(_pendingDistrictLotId))
        {
          _districtWorld.HideLotOutline();
          if (!TryDistrictLotFootprint(district, normalized.x,
                        normalized.y, out var gridX, out var gridZ,
                        out var spanX, out var spanZ, out var placeable))
          {
            _districtWorld.HideLotPlacementGuide();
            return;
          }
          _districtWorld.ShowLotPlacementGuide(gridX, gridZ,
                    spanX, spanZ, placeable, _pendingDistrictLotShoreOffset.x, _pendingDistrictLotShoreOffset.y);
          return;
        }

        if (_districtSelectionDragActive)
        {
          MoveDistrictSelection(district, normalized);
          return;
        }

        if (DistrictWaterSelectToolActive())
        {
          _districtWorld.HideLotOutline();
          return;
        }

        if (_districtFloraPointerDown)
        {
          var moving = FindDistrictFlora(district,
                    _selectedDistrictFloraInstanceId);
          if (moving != null)
          {
            var candidate = new Vector2(
                      Mathf.Clamp01(normalized.x +
                          _districtFloraDragOffset.x),
                      Mathf.Clamp01(normalized.y +
                          _districtFloraDragOffset.y));
            if (DistrictFloraCanOccupyWater(moving.FloraId) ||
                      !_districtWorld.IsUnderRiverWater(candidate))
            {
              moving.NormalizedX = candidate.x;
              moving.NormalizedZ = candidate.y;
              DistrictHarvestIndex.Changed(district,moving);
              _districtWorld.MoveDistrictFlora(moving);
            }
          }
          return;
        }

        if (IsDistrictRoadToolActive())
        {
          _districtWorld.HideLotOutline();
          var roadCell = DistrictRoadCell(district,
                    normalized.x, normalized.y);
          var roadPlaceable = CanPlaceDistrictRoad(district,
                    roadCell.x, roadCell.y);
          _districtWorld.ShowLotPlacementGuide(roadCell.x, roadCell.y,
                    1, 1, roadPlaceable);
          if (_districtRoadPointerDown)
          {
            var route = _builderTool == DistrictRoadPlacementModel.AntiqueBrickFamily
                ? DistrictRoadPlacementModel.OctileRoute(
                    _lastDistrictRoadDragCell, roadCell)
                : RoadPlacementModel.BuildPlannedRoadRoute(
                    _lastDistrictRoadDragCell, roadCell);
            for (var routeIndex = 1; routeIndex < route.Count;
                       routeIndex++)
            {
              var previous = route[routeIndex - 1];
              var next = route[routeIndex];
              if (TryOfferDistrictBridge(district, previous, next)) break;
              if (previous.x != next.x && previous.y != next.y &&
                  (DistrictRoadLotOccupied(district, previous.x, next.y) ||
                   DistrictRoadLotOccupied(district, next.x, previous.y)))
                break;
              var wasEmpty = DistrictRoadSession(district).At(next.x, next.y) == null;
              if (!PlaceDistrictRoad(district, next.x, next.y)) break;
              if (wasEmpty) _districtRoadStrokeAdded.Add(next);
              _districtRoadStrokePath.Add(next);
              if (_builderTool == DistrictRoadPlacementModel.AntiqueBrickFamily &&
                  DistrictRoadSession(district).TryConnectDiagonal(previous, next))
                _districtWorld?.RefreshRoadCellsAndNeighbors(district,
                    new[] { previous, next }, DistrictRoadSession(district).At);
              if (_builderTool == DistrictRoadPlacementModel.AntiqueBrickFamily)
              {
                var session = DistrictRoadSession(district);
                var treasury = district.Treasury;
                if (session.TrySmoothAntiqueBrickStaircase(
                    _districtRoadStrokePath, _districtRoadStrokeAdded,
                    ref treasury, (from, to) =>
                        !DistrictRoadLotOccupied(district, from.x, to.y) &&
                        !DistrictRoadLotOccupied(district, to.x, from.y),
                    out var changed))
                {
                  district.Treasury = treasury;
                  _districtWorld?.RefreshRoadCellsAndNeighbors(district,
                      changed, session.At);
                  var money = _root?.Q<Label>("district-simulation-money");
                  if (money != null) money.text = $"${treasury:N0}";
                }
              }
              _lastDistrictRoadDragCell = next;
            }
          }
          return;
        }

        if (IsDistrictRoadDeleteToolActive())
        {
          _districtWorld.HideLotOutline();
          SetDistrictRoadDeleteCursor(true);
          var roadCell = DistrictRoadCell(district,
                    normalized.x, normalized.y);
          _districtWorld.ShowLotPlacementGuide(roadCell.x, roadCell.y,
                    1, 1, false);
          if (_districtRoadPointerDown)
          {
            var route = RoadPlacementModel.BuildPlannedRoadRoute(
                _lastDistrictRoadDragCell, roadCell);
            foreach (var cell in route.Skip(1))
              DeleteDistrictRoadAt(district, cell);
            _lastDistrictRoadDragCell = roadCell;
          }
          return;
        }

        SetDistrictRoadDeleteCursor(false);

        _districtWorld.HideLotPlacementGuide();
        var hovered = FindDistrictLotAt(district, normalized.x,
                  normalized.y);
        _hoveredDistrictLotInstanceId = hovered?.InstanceId ?? "";
        if (hovered != null && TryGetDistrictLotFootprint(hovered,
                      out _, out var hoverSpanX, out var hoverSpanZ))
        {
          _districtWorld.ShowLotOutline(hovered.GridX, hovered.GridZ,
                    hoverSpanX, hoverSpanZ,
                    hovered.InstanceId == _selectedDistrictLotInstanceId, hovered.ShoreOffsetX, hovered.ShoreOffsetZ);
        }
        else RefreshSelectedDistrictLotOutline(district);
      });
      screen.RegisterCallback<PointerLeaveEvent>(_ =>
      {
        SetDistrictRoadDeleteCursor(false);
        _districtWorld?.HideAxemanPlacement(); UnityEngine.Cursor.visible = true;
        // Captured rectangle drags may cross UI or viewport edges.
        // Leaving is not releasing the mouse.
        if (_districtMarqueeActive) return;
        FinishDistrictFloraPaint(district);
        _districtWorld?.HideLotPlacementGuide();
        if (_districtMarqueeActive || _districtSelectionDragActive)
          CompleteDistrictSelectionPointer(district);
        if (_districtFloraPointerDown)
        {
          _districtFloraPointerDown = false;
          _districtWorldCompositionKey = DistrictCompositionKey(district);
          SaveDistrictEdit();
        }
        if (_districtRoadPointerDown)
        {
          _districtRoadPointerDown = false;
          if (IsDistrictRoadDeleteToolActive())
            _districtWorld?.CommitRoadSurfaceChanges();
          else
            _districtWorld?.CommitLocalSurfaceChanges();
          _districtWorldCompositionKey = DistrictCompositionKey(district);
          SaveDistrictEdit();
        }
        RefreshSelectedDistrictLotOutline(district);
      });
      screen.RegisterCallback<PointerDownEvent>(evt =>
      {
        if (_districtMarqueeActive)
        {
          evt.StopImmediatePropagation();
          return;
        }
        var target = evt.target as VisualElement;
        for (var element = target; element != null && element != screen; element = element.parent)
          if (element.name == "selected-object-panel" || element.ClassListContains("cf-map-chrome")) return;
        if (evt.button != 0 || target?.name == "district-resource-bar" || evt.target is Button ||
                  target?.GetFirstAncestorOfType<Button>() != null) return;
        if (HandleArmedDistrictLotPointer(district, evt)) return;
        // Inspect existing objects before dispatching any active category's tool.
        // UI controls remain UI controls; only a click on the world surface picks.
        if (!IsDistrictRoadToolActive() &&
            !IsDistrictRoadDeleteToolActive() &&
            (target == screen || target?.ClassListContains("district-terraform-viewport") == true) &&
            TryInspectDistrictObject(DistrictCameraPoint(evt.position)))
        {
          BeginLotNudge(screen, evt.pointerId, DistrictCameraPoint(evt.position));
          evt.StopImmediatePropagation();
          return;
        }
        if (_placingBrickworks)
        {
          if(_districtWorld.TryGroundPoint(DistrictCameraPoint(evt.position),out var brickPoint))PlaceBrickworks(district,brickPoint);
          evt.StopImmediatePropagation();return;
        }
        if (IndustryPlacementActive) { evt.StopImmediatePropagation(); return; }
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(_districtResourceDebugPlacement))
        {
          if (_districtWorld != null && _districtWorld.TryGroundPoint(
              DistrictCameraPoint(evt.position), out var resourcePoint))
          {
            var resource = _districtResourceDebugPlacement;
            _districtResourceDebugPlacement = "";
            PlaceDistrictResourceDebug(district, resourcePoint, resource);
          }
          evt.StopImmediatePropagation();
          return;
        }
#endif
        if (_pendingLaborCount > 0 || _placingMarksman)
        {
          if (_districtWorld != null && _districtWorld.TryGroundPoint(DistrictCameraPoint(evt.position), out var laborPoint))
            PlaceLabor(district, laborPoint);
          evt.StopImmediatePropagation(); return;
        }
        if (_districtPaletteOpen && !_districtPaletteCategoryOpen)
        { ReturnToQuietDistrict(); evt.StopImmediatePropagation(); return; }
        if (DistrictSelectToolActive())
        {
          if (_districtWorld != null && _districtWorld.TryGroundPoint(
                  DistrictCameraPoint(evt.position), out var selectedPoint) &&
              BeginDistrictMovePointer(district, selectedPoint, evt.position))
          {
            evt.StopImmediatePropagation();
            return;
          }
          BeginDistrictSelectionPointer(district, evt.position,
                    screen, evt.pointerId);
          evt.StopImmediatePropagation();
          return;
        }
        if (DistrictWaterSelectToolActive())
        {
          SelectDistrictRiverAt(district, evt.position);
          evt.StopPropagation();
          return;
        }
        if (_districtWorld == null ||
                  !_districtWorld.TryGroundPoint(
                      DistrictCameraPoint(evt.position),
                      out var normalized)) return;

        if (DistrictFloraPlacementArmed())
        {
          BeginDistrictFloraPaint(district, normalized);
        }
        else if (DistrictMoveToolActive())
        {
          BeginDistrictMovePointer(district, normalized, evt.position);
        }
        else if (_districtEditorMode == DistrictEditorMode.Terraform &&
                       _terraformCategory == "Flora" &&
                       _terraformTool == "Trees")
        {
          if (_pendingDistrictFloraMode != 0 ||
                    !string.IsNullOrWhiteSpace(_pendingDistrictFloraId))
          {
            PlaceDistrictFlora(district, normalized);
          }
          else
          {
            var hit = _districtWorld.FindDistrictFloraAtPanel(
                      DistrictCameraPoint(evt.position));
            if (string.IsNullOrWhiteSpace(hit))
              hit = _districtWorld.FindDistrictFloraAt(normalized,
                        8f);
            if (!string.IsNullOrWhiteSpace(hit))
            {
              _selectedDistrictFloraInstanceId = hit;
              var selected = FindDistrictFlora(district, hit);
              if (selected != null)
                _districtFloraDragOffset = new Vector2(
                          selected.NormalizedX - normalized.x,
                          selected.NormalizedZ - normalized.y);
              _districtFloraPointerDown = true;
              _districtWorld.SelectDistrictFlora(hit);
            }
          }
        }
        else if (IsDistrictRoadToolActive())
        {
          _districtRoadPointerDown = true;
          _districtRoadStrokePath.Clear();
          _districtRoadStrokeAdded.Clear();
          _lastDistrictRoadDragCell = DistrictRoadCell(district,
                    normalized.x, normalized.y);
          _districtRoadStrokePath.Add(_lastDistrictRoadDragCell);
          _selectedDistrictRoadCell = _lastDistrictRoadDragCell;
          _hasSelectedDistrictRoad = true;
          var existingRoad = DistrictRoadSession(district).At(
                    _lastDistrictRoadDragCell.x,
                    _lastDistrictRoadDragCell.y);
          if (existingRoad == null)
            if (PlaceDistrictRoad(district, _lastDistrictRoadDragCell.x,
                      _lastDistrictRoadDragCell.y))
              _districtRoadStrokeAdded.Add(_lastDistrictRoadDragCell);
          _districtWorld.ShowRoadSelectionGuide(
                    _selectedDistrictRoadCell.x,
                    _selectedDistrictRoadCell.y);
        }
        else if (IsDistrictRoadDeleteToolActive())
        {
          _districtRoadPointerDown = true;
          _lastDistrictRoadDragCell = DistrictRoadCell(district,
              normalized.x, normalized.y);
          DeleteDistrictRoadAt(district, _lastDistrictRoadDragCell);
          SetDistrictRoadDeleteCursor(true);
        }
        else
        {
          var selectedLot = FindDistrictLotAt(district,
                    normalized.x, normalized.y);
          _selectedDistrictLotInstanceId = selectedLot?.InstanceId ?? "";
          Show(AppScreen.DistrictTerraform);
        }
        evt.StopPropagation();
      }, TrickleDown.TrickleDown);
      screen.RegisterCallback<PointerUpEvent>(evt =>
      {
        if (_lotNudge != null && evt.pointerId == _nudgePointer) { UpdateLotNudge(DistrictCameraPoint(evt.position)); EndLotNudge(false); evt.StopImmediatePropagation(); return; }
        if (_districtMarqueeActive)
        {
          if (evt.button == 0 && evt.pointerId == _districtSelectionPointerId)
          {
            UpdateDistrictSelectionMarquee(evt.position);
            CompleteDistrictSelectionPointer(district);
          }
          evt.StopImmediatePropagation();
          return;
        }
        if (evt.button != 0) return;
        FinishDistrictFloraPaint(district);
        if (_districtMarqueeActive || _districtSelectionDragActive)
        {
          CompleteDistrictSelectionPointer(district);
        }
        if (_districtFloraPointerDown)
        {
          _districtFloraPointerDown = false;
          _districtWorldCompositionKey = DistrictCompositionKey(district);
          SaveDistrictEdit();
        }
        if (!_districtRoadPointerDown) return;
        _districtRoadPointerDown = false;
        _districtRoadStrokePath.Clear();
        _districtRoadStrokeAdded.Clear();
        if (IsDistrictRoadDeleteToolActive())
          _districtWorld?.CommitRoadSurfaceChanges();
        else
          _districtWorld?.CommitLocalSurfaceChanges();
        _districtWorldCompositionKey = DistrictCompositionKey(district);
        SaveDistrictEdit();
      }, TrickleDown.TrickleDown);
      screen.RegisterCallback<PointerCaptureOutEvent>(evt =>
      {
        if (_lotNudge != null && evt.pointerId == _nudgePointer) EndLotNudge(true);
        if (_districtMarqueeActive && evt.pointerId == _districtSelectionPointerId)
          CancelDistrictSelectionPointer();
      });
      screen.RegisterCallback<PointerCancelEvent>(evt =>
      {
        if (_lotNudge != null && evt.pointerId == _nudgePointer) EndLotNudge(true);
        if (_districtMarqueeActive && evt.pointerId == _districtSelectionPointerId)
          CancelDistrictSelectionPointer();
      });
      screen.RegisterCallback<DetachFromPanelEvent>(_ =>
      {
        SetDistrictRoadDeleteCursor(false);
        if (_nudgeSurface == screen) EndLotNudge(true);
        if (_districtSelectionSurface == screen) CancelDistrictSelectionPointer();
      });

      ComposeDistrictChrome(screen, district);

      RefreshDistrictPalette(screen, district);
      var hud = new VisualElement();
      hud.AddToClassList("terraform-hud");
      hud.AddToClassList("cf-map-chrome");
      var miniMap = new VisualElement();
      miniMap.AddToClassList("terraform-minimap");
      var miniMapGrass = Resources.Load<Texture2D>(
          DistrictWorldController.DistrictGrassResource);
      if (miniMapGrass != null)
        miniMap.style.backgroundImage = new StyleBackground(miniMapGrass);
      hud.Add(miniMap);
      hud.Add(StyledLabel("DISTRICT INFO", "terraform-info-kicker"));
      var districtName = new TextField("NAME")
      {
        name = "terraform-district-name-field",
        isDelayed = false,
        value = district.Name ?? ""
      };
      districtName.AddToClassList("terraform-district-name-field");
      hud.Add(districtName);
      var draftDesignation = district.Designation;
      var designation = new VisualElement
      {
        name = "terraform-district-designation"
      };
      designation.AddToClassList("terraform-district-designation");
      foreach (var option in new[]
               {
                         RegionPlaceDesignation.District,
                         RegionPlaceDesignation.Town
                     })
      {
        var capturedDesignation = option;
        var designationButton = new Button(() =>
        {
          draftDesignation = capturedDesignation;
          foreach (var choice in designation.Children())
            choice.EnableInClassList("terraform-district-designation-button--selected",
                choice.name == "terraform-designation-" + capturedDesignation.ToString().ToLowerInvariant());
        })
        {
          name = "terraform-designation-" +
                   option.ToString().ToLowerInvariant(),
          text = option.ToString().ToUpperInvariant(),
          tooltip = option == RegionPlaceDesignation.Town
                ? "Show this place as a named point on the region map."
                : "Show this name faintly across its district area."
        };
        designationButton.AddToClassList(
            "terraform-district-designation-button");
        if (district.Designation == option)
          designationButton.AddToClassList(
              "terraform-district-designation-button--selected");
        designation.Add(designationButton);
      }
      hud.Add(designation);
      var districtMeta = StyledLabel(
          TerraformDistrictMeta(district, districtColumns, districtRows),
          "terraform-district-meta");
      districtMeta.name = "terraform-district-meta";
      hud.Add(districtMeta);
      var statusPanel = ComposeDistrictStatusPanel(district);
      if (district.Founded)
      {
        statusPanel.AddToClassList(
            "district-simulation-panel--embedded");
        // Founded district metrics live in the shared resource and season bars.
        if (!string.IsNullOrWhiteSpace(_pendingDistrictLotId)) { statusPanel.RemoveFromClassList("district-simulation-panel--embedded"); screen.Add(statusPanel); }
      }
      var hideInfo = new Button(() =>
      {
        if (!ApplyDistrictName(district, districtName.value)) return;
        district.Designation = draftDesignation;
        SaveDistrictEdit();
        _districtInfoVisible = false;
        SetDistrictChromeVisibility(screen);
      })
      {
        name = "district-info-close",
        text = "OK",
        tooltip = "Apply the name and designation"
      };
      hideInfo.AddToClassList("terraform-district-designation-button");
      hideInfo.SetEnabled(!string.IsNullOrWhiteSpace(districtName.value));
      districtName.RegisterValueChangedCallback(evt =>
          hideInfo.SetEnabled(!string.IsNullOrWhiteSpace(evt.newValue)));
      hud.Add(hideInfo);
      hud.style.display = _districtInterfaceVisible &&
                          _districtInfoVisible
          ? DisplayStyle.Flex : DisplayStyle.None;
      screen.Add(hud);

      var infoToggle = new Button(() =>
      {
        _districtInfoVisible = !_districtInfoVisible;
        if (_districtInfoVisible) ClearSelectedObject();
        SetDistrictChromeVisibility(screen);
      })
      {
        name = "district-info-toggle",
        text = "INFO",
        tooltip = "Show or hide district name and designation information"
      };
      infoToggle.AddToClassList("district-info-toggle");
      infoToggle.style.display = _districtInterfaceVisible &&
                                 !_districtInfoVisible
          ? DisplayStyle.Flex : DisplayStyle.None;
      screen.Add(infoToggle);

      var interfaceToggle = new Button(ToggleDistrictInterface)
      {
        name = "district-interface-toggle",
        text = _districtInterfaceVisible ? "HIDE UI" : "SHOW UI",
        tooltip = "Hide or show district editor menus and information boxes"
      };
      interfaceToggle.AddToClassList("district-interface-toggle");
      screen.Add(interfaceToggle);

      if (!district.Founded || !string.IsNullOrWhiteSpace(_pendingFounderBuildingId))
      {
        statusPanel.style.display = _districtInterfaceVisible
            ? DisplayStyle.Flex : DisplayStyle.None;
        screen.Add(statusPanel);
      }
      var lotInfo = ComposeSelectedDistrictLotPanel(district);
      if (lotInfo != null)
      {
        lotInfo.style.display = _districtInterfaceVisible
            ? DisplayStyle.Flex : DisplayStyle.None;
        screen.Add(lotInfo);
      }
      SetDistrictChromeVisibility(screen);

      EventCallback<GeometryChangedEvent> centerViewport = null;
      centerViewport = _ =>
      {
        viewport.UnregisterCallback(centerViewport);
        _districtWorld?.SetPan(_terraformPanOffset);
        _districtWorld?.SetZoom(_terraformZoomLevel);
      };
      viewport.RegisterCallback(centerViewport);
      AttachLargeRegionHoverHelp(screen);
      _root.Add(screen);
    }

    private void RefreshDistrictPalette(VisualElement screen, RegionCityTile district)
    {
      screen.Q("map-mode-switch")?.RemoveFromHierarchy();
      screen.Q("map-tool-rail")?.RemoveFromHierarchy();
      screen.Q("map-tool-options")?.RemoveFromHierarchy();
      var modeSwitch = new VisualElement();
      modeSwitch.AddToClassList("district-mode-switch");
      modeSwitch.AddToClassList("cf-quiet-dock");
      foreach (var mode in new[] { DistrictEditorMode.Builder, DistrictEditorMode.Terraform })
      {
        var capturedMode = mode;
        var modeButton = CfMapChrome.Action(mode == DistrictEditorMode.Builder ? "Build (B)" : "Terrain (T)",
            mode == DistrictEditorMode.Builder ? "Build" : "TerrainAction", () => ToggleDistrictPalette(capturedMode),
            $"district-mode-{mode.ToString().ToLowerInvariant()}");
        modeButton.EnableInClassList("district-mode-button--selected", _districtPaletteOpen && _districtEditorMode == mode);
        modeSwitch.Add(modeButton);
      }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
      modeSwitch.Add(CfMapChrome.Action("Lots (testing)", "Lots", () =>
      {
        ReturnToQuietDistrict();
        ComposeDistrictLotBrowser(null, true);
      }, "district-mode-lots"));
#endif
      modeSwitch.AddToClassList("cf-map-chrome");
      modeSwitch.name = "map-mode-switch";
      screen.Add(modeSwitch);
      modeSwitch.style.display = _districtInterfaceVisible
          ? DisplayStyle.Flex : DisplayStyle.None;

      var rail = new ScrollView(ScrollViewMode.Horizontal) { horizontalScrollerVisibility = ScrollerVisibility.Auto, verticalScrollerVisibility = ScrollerVisibility.Hidden };
      rail.AddToClassList("terraform-rail");
      if (_districtEditorMode == DistrictEditorMode.Builder)
        rail.AddToClassList("terraform-rail--builder");
      var categories = DistrictEditorCategories(_districtEditorMode);
      foreach (var category in categories)
      {
        var captured = category;
        var button = new Button(() =>
        {
          if (captured.Name == "Select") { ReturnToQuietDistrict(); return; }
          _districtPaletteCategoryOpen = true;
          SelectDistrictCategory(captured.Name);
          if (captured.Name == "Sun")
            _terraformTool = DistrictTimeToolName(district.TimeOfDay);
          RefreshDistrictPalette(screen, district);
          if (_districtEditorMode == DistrictEditorMode.Builder &&
                      captured.Name == "Roads")
            ComposeDistrictRoadFamilyModal();
        })
        {
          text = captured.Glyph,
          name = $"district-category-{captured.Name.ToLowerInvariant()}",
          tooltip = captured.Tip
        };
        button.AddToClassList("terraform-category-button");
        if (ActiveDistrictCategory == captured.Name)
          button.AddToClassList("terraform-category-button--selected");
        CfMapChrome.Illustrate(button, captured.Name);
        rail.Add(button);
      }
      if (_districtEditorMode == DistrictEditorMode.Builder)
      {
        var industry = CfMapChrome.Action("Industry", "Industry", ComposeDistrictIndustryModal, "quiet-build-industry");
        industry.AddToClassList("terraform-category-button"); rail.Add(industry);
      }
      rail.AddToClassList("cf-map-chrome");
      rail.name = "map-tool-rail";
      screen.Add(rail);
      rail.style.display = _districtInterfaceVisible
          ? DisplayStyle.Flex : DisplayStyle.None;

      var flyout = new ScrollView(ScrollViewMode.Vertical) { name = "map-tool-options", horizontalScrollerVisibility = ScrollerVisibility.Hidden };
      flyout.AddToClassList("terraform-flyout");
      flyout.AddToClassList("cf-map-chrome");
      bool waterMenu = _districtEditorMode == DistrictEditorMode.Terraform && ActiveDistrictCategory == "Water";
      if (waterMenu) flyout.AddToClassList("terraform-flyout--water");
      if (_districtEditorMode == DistrictEditorMode.Builder)
        flyout.AddToClassList("terraform-flyout--builder");
      flyout.Add(StyledLabel(ActiveDistrictCategory.ToUpperInvariant(),
          "terraform-flyout-title"));
      flyout.Add(StyledLabel(
          $"ACTIVE TOOL: {ActiveDistrictTool.ToUpperInvariant()}",
          "inspector-note"));
      if (_districtEditorMode == DistrictEditorMode.Builder &&
          ActiveDistrictCategory == "Roads")
        flyout.style.display = DisplayStyle.None;
      else foreach (var tool in ActiveDistrictTools())
      {
        var captured = tool;
        var button = new Button(() =>
        {
          if (waterMenu && captured.Name == "Repair River") { RepairDistrictRivers(); return; }
          if (_districtEditorMode == DistrictEditorMode.Terraform && ActiveDistrictCategory == "Flora" && captured.Name == "Clear Flora")
          { ClearDistrictTrees(); return; }
          SelectDistrictTool(captured.Name);
          if (_districtEditorMode == DistrictEditorMode.Terraform && ActiveDistrictCategory == "Terrain" && captured.Name == "Hills")
          { ComposeDistrictHillsModal(); return; }
          if (_districtEditorMode == DistrictEditorMode.Builder &&
              TryBuilderLotBrowserType(ActiveDistrictCategory, captured.Name,
                  out var lotType))
          {
            ComposeDistrictLotBrowser(lotType);
            return;
          }
          if (_districtEditorMode == DistrictEditorMode.Terraform &&
                      ActiveDistrictCategory == "Flora" &&
                      (captured.Name == "Trees" || captured.Name == "Forest"))
          {
            if (captured.Name == "Forest") ComposeDistrictFloraCoverageModal();
            else ComposeDistrictFloraModal();
            return;
          }
          if (ActiveDistrictCategory == "Sun" &&
                      TryDistrictTimePreset(captured.Name, out var preset))
          {
            DistrictDayCycle.Set(district, preset);
            _districtWorld?.SetTimeOfDay(preset);
            SaveDistrictEdit();
          }
          RefreshDistrictPalette(screen, district);
        })
        {
          text = captured.Name,
          name = $"district-tool-{captured.Name.ToLowerInvariant().Replace(' ', '-')}",
          tooltip = ActiveDistrictCategory == "Sun"
                ? $"Set district lighting to {captured.Name}"
                : TryBuilderLotBrowserType(ActiveDistrictCategory,
                      captured.Name, out _)
                    ? $"Browse saved Lots in {ActiveDistrictCategory} and choose one to place."
                : waterMenu && captured.Name == "Repair River"
                    ? "Repair all rivers in this district now. Smooth their general course while retaining width, depth and entry/exit points. Undo restores the edit."
                : waterMenu && captured.Name == "Shape River"
                    ? "Set a width or use the existing river width, then trace a replacement section. Release to rebuild."
                : waterMenu && captured.Name == "Soften River"
                    ? "Brush rough sections to soften bends."
                : waterMenu && captured.Name == "Erase River"
                    ? "Brush away part of a river. Undo restores the edit."
                : ActiveDistrictCategory == "Water" && captured.Name == "Select Water"
                    ? "Select a river, move it with the arrow keys, or remove it with Delete."
                : ActiveDistrictCategory == "Flora" && captured.Name == "Forest"
                    ? "Generate Light, Medium or Heavy tree coverage in this district only."
                : ActiveDistrictCategory == "Flora" && captured.Name == "Clear Flora"
                    ? "Remove all trees and forest clusters in this district. Keeps stones; supports Undo."
                : ActiveDistrictCategory == "Flora" && captured.Name == "Trees"
                    ? "Open tree and stone placement, or generate district tree coverage."
                : ActiveDistrictCategory == "Environment" && captured.Name == "Rain"
                    ? "Gather full cloud cover, rain for ten seconds, then clear. Click again to restart."
                : ActiveDistrictCategory == "Environment" && captured.Name == "Snow"
                    ? "Gather clouds, snow for 10 seconds, hold ground snow for 10, then melt over 5. Temporary test weather."
                : ActiveDistrictCategory == "Environment" && captured.Name == "Clear Skies"
                    ? "Stop the storm and restore fair weather."
                : captured.Name == "Hills" ? "Generate gentle rolling hills"
                : $"{captured.Name} — interface preview; this district tool is not enabled yet"
        };
        button.AddToClassList("terraform-tool-button");
        if (waterMenu) button.AddToClassList("terraform-tool-button--water");
        if (ActiveDistrictTool == captured.Name)
          button.AddToClassList("terraform-tool-button--selected");
        flyout.Add(button);
      }
      if (waterMenu && ActiveDistrictTool == "Shape River")
      {
        var widthLabel = StyledLabel(_riverShapeWidth.HasValue ? $"RIVER WIDTH: {Mathf.RoundToInt(_riverShapeWidth.Value)} m" : "WIDTH: FROM RIVER", "terraform-flyout-title");
        widthLabel.name = "river-locked-width"; flyout.Add(widthLabel);
        var width = new Slider(20, 200)
        { name = "river-shape-width", value = _riverShapeWidth ?? 80, showInputField = true };
        width.AddToClassList("environment-lighting-slider");
        width.style.width = 186; width.style.minWidth = 0; width.style.flexGrow = 0;
        width.style.marginBottom = 8;
        width.RegisterValueChangedCallback(e =>
        {
          _riverShapeWidth = Mathf.Clamp(e.newValue, 20, 200);
          widthLabel.text = $"RIVER WIDTH: {Mathf.RoundToInt(_riverShapeWidth.Value)} m";
        });
        flyout.Add(width);
        flyout.Add(StyledLabel("Adjust width, then trace a river and release. Width applies to the selected river; depth stays the same.", "inspector-note"));
      }
      else if (waterMenu && ActiveDistrictTool == "Repair River")
      {
        flyout.Add(StyledLabel("Click a river to repair rough bends. Undo restores the original.", "inspector-note"));
      }
      else if (waterMenu)
      {
        var radiusLabel = StyledLabel($"BRUSH RADIUS: {Mathf.RoundToInt(_riverSculptRadius)} m", "terraform-flyout-title");
        flyout.Add(radiusLabel);
        var radius = new Slider(20, 200)
        { name = "river-sculpt-radius", value = _riverSculptRadius };
        radius.AddToClassList("environment-lighting-slider");
        radius.style.width = 186; radius.style.minWidth = 0; radius.style.flexGrow = 0;
        radius.style.marginBottom = 8;
        radius.RegisterValueChangedCallback(e => { _riverSculptRadius = e.newValue; radiusLabel.text = $"BRUSH RADIUS: {Mathf.RoundToInt(e.newValue)} m"; });
        flyout.Add(radius);
      }
      AddDistrictHarvestControls(flyout);
      screen.Add(flyout);
      SetDistrictChromeVisibility(screen);
    }

    private bool MapPanningBlocked => _root?.Q("document-modal") != null ||
        _root?.Q("cf-choice-overlay") != null;

    private void UpdateDistrictEdgePan(VisualElement screen, PointerMoveEvent evt)
    {
      _districtEdgePanDirection = Vector2Int.zero;
      if (MapPanningBlocked) return;
      for (var target = evt.target as VisualElement; target != null && target != screen; target = target.parent)
        if (target is Button || target is TextField || target.ClassListContains("cf-map-chrome") ||
            target.name == "selected-object-panel") return;
      var bounds = screen.worldBound;
      if (bounds.width <= 0 || bounds.height <= 0) return;
      _districtEdgePanDirection = DistrictZoom.EdgePanWorldMotion(new Vector2(
          (evt.position.x - bounds.xMin) / bounds.width,
          (evt.position.y - bounds.yMin) / bounds.height));
    }

    private static void AttachLargeRegionHoverHelp(VisualElement screen)
    {
      if (screen == null) return;

      var helpLayer = new VisualElement { name = "region-hover-help-layer" };
      helpLayer.AddToClassList("region-hover-help-layer");
      helpLayer.pickingMode = PickingMode.Ignore;

      var helpPanel = new VisualElement { name = "region-hover-help" };
      helpPanel.AddToClassList("region-hover-help");
      helpPanel.pickingMode = PickingMode.Ignore;
      helpPanel.style.display = DisplayStyle.None;

      var helpText = new Label();
      helpText.AddToClassList("region-hover-help-text");
      helpText.pickingMode = PickingMode.Ignore;
      helpPanel.Add(helpText);
      helpLayer.Add(helpPanel);

      var helpRevision = 0;
      // Delegate hover/focus so locally replaced tool palettes keep working.
      void ShowHelp(VisualElement target)
      {
        var button = target as Button ?? target?.GetFirstAncestorOfType<Button>();
        if (button == null || string.IsNullOrWhiteSpace(button.tooltip))
        { helpPanel.style.display = DisplayStyle.None; return; }
        helpText.text = button.tooltip;
        if (screen.ClassListContains("cf-quiet-map"))
        {
          var bounds = button.worldBound;
          var point = screen.WorldToLocal(new Vector2(bounds.xMin, bounds.yMax));
          helpPanel.style.left = Mathf.Clamp(point.x, 8, Mathf.Max(8, screen.layout.width - 290));
          helpPanel.style.top = point.y > screen.layout.height - 120 ? screen.WorldToLocal(bounds.position).y - 64 : point.y + 6;
          helpPanel.style.bottom = StyleKeyword.Auto;
        }
        helpPanel.style.display = DisplayStyle.Flex;
        helpLayer.BringToFront();
        var shownRevision = ++helpRevision;
        helpPanel.schedule.Execute(() =>
        {
          if (shownRevision == helpRevision)
            helpPanel.style.display = DisplayStyle.None;
        }).ExecuteLater(3000);
      }
      screen.RegisterCallback<PointerOverEvent>(evt => ShowHelp(evt.target as VisualElement));
      screen.RegisterCallback<PointerOutEvent>(_ =>
      { helpRevision++; helpPanel.style.display = DisplayStyle.None; });
      screen.RegisterCallback<FocusInEvent>(evt => ShowHelp(evt.target as VisualElement));
      screen.RegisterCallback<FocusOutEvent>(_ =>
      { helpRevision++; helpPanel.style.display = DisplayStyle.None; });

      screen.Add(helpLayer);
      helpLayer.BringToFront();
    }

    private void EnsureDistrictWorld(RegionCityTile district)
    {
      if (district == null) return;
      _districtWorldTile = district;
      district.Climate = _openRegion?.Terrain?.Climate ?? RegionClimate.Temperate;
      var lotId = district.Founded ? district.LotId ?? "" : "";
      var compositionKey = DistrictCompositionKey(district);
      if (_districtWorld != null &&
          _districtWorld.WorldCamera != null &&
          _districtWorldTileId == district.TileId &&
          _districtWorldLotId == lotId &&
          _districtWorldCompositionKey == compositionKey)
      {
        _districtWorld.SetVisible(true);
        _districtWorld.SetPan(_terraformPanOffset);
        if (_districtWorld.ZoomLevel != _terraformZoomLevel)
          _districtWorld.SetZoom(_terraformZoomLevel);
        if (_districtWorld.TimeOfDay != district.TimeOfDay)
          _districtWorld.SetTimeOfDay(district.TimeOfDay);
        return;
      }

      if (_districtWorld == null)
      {
        var world = new GameObject("V3 District World");
        _districtWorld = world.AddComponent<DistrictWorldController>();
      }
      _districtWorld.RebuildEntireDistrict(district,
          DistrictBulkRebuildReason.LoadSwitchOrStateRestore);
      _districtWorld.SetPan(_terraformPanOffset);
      _districtWorld.SetZoom(_terraformZoomLevel);
      _districtWorldTileId = district.TileId;
      _districtWorldLotId = lotId;
      _districtWorldCompositionKey = compositionKey;
    }

    private bool DistrictWorldNeedsBuild(RegionCityTile district)
    {
      if (district == null || _lotNudge != null) return false;
      var lotId = district.Founded ? district.LotId ?? "" : "";
      return _districtWorld == null ||
             _districtWorld.WorldCamera == null ||
             _districtWorldTileId != district.TileId ||
             _districtWorldLotId != lotId ||
             _districtWorldCompositionKey != DistrictCompositionKey(district);
    }

    private void ComposeDistrictPreloader(VisualElement screen,
        RegionCityTile district)
    {
      screen.name = "district-loading-screen";
      screen.AddToClassList("district-loading-screen");
      var panel = new VisualElement();
      panel.AddToClassList("district-loading-panel");
      panel.Add(StyledLabel("CITYFORGE", "district-loading-kicker"));
      panel.Add(StyledLabel("FORGING DISTRICT…",
          "district-loading-title"));
      panel.Add(StyledLabel(district.Name?.ToUpperInvariant() ??
                            "DISTRICT", "district-loading-name"));
      var track = new VisualElement();
      track.AddToClassList("district-loading-track");
      var fill = new VisualElement();
      fill.AddToClassList("district-loading-fill");
      track.Add(fill);
      panel.Add(track);
      panel.Add(StyledLabel(
          "Loading terrain, lots, roads, flora, and waterways",
          "district-loading-copy"));
      screen.Add(panel);
      _root.Add(screen);

      screen.schedule.Execute(() =>
      {
        if (_currentScreen != AppScreen.DistrictTerraform ||
                  FindSelectedRegionTile() != district) return;
        EnsureDistrictWorld(district);
        Show(AppScreen.DistrictTerraform);
      }).ExecuteLater(80);
    }

    private static string DistrictCompositionKey(RegionCityTile district)
    {
      if (district == null) return "";
      var parts = new List<string>();
      parts.Add("bridges:"+district.BridgeRevision);
      foreach (var nudge in district.LotNudges ?? new()) parts.Add("nudge:" + JsonUtility.ToJson(nudge));
      parts.Add("hills:" + JsonUtility.ToJson(district.Hills));
      foreach(var works in district.Brickworks??new List<DistrictBrickworksSite>())parts.Add($"brickworks:{works.Id}:{works.NormalizedX}:{works.NormalizedZ}:{works.Yaw}");
      foreach (var site in district.StoneSites ?? new List<DistrictStoneSite>()) parts.Add($"stone:{site.Id}:{site.Built}:{site.NormalizedX}:{site.NormalizedZ}:{site.Yaw}");
      foreach (var deposit in district.ResourceDeposits ?? new List<DistrictResourceDeposit>())
        parts.Add("resource:" + JsonUtility.ToJson(deposit));
      foreach (var lot in district.Lots ?? new List<PlacedDistrictLot>())
        if (lot != null)
          parts.Add($"{lot.LotId}:{lot.GridX}:{lot.GridZ}:" +
                    $"{lot.RotationQuarterTurns}");
      foreach (var road in district.Roads ?? new List<PlacedRoadPiece>())
        if (road != null)
          parts.Add($"road:{road.PackageId}:{road.GridX}:{road.GridZ}:" +
                    $"{road.Topology}:{road.RotationQuarterTurns}");
      foreach (var river in district.Rivers ?? new List<PlacedDistrictRiver>())
        if (river != null)
          parts.Add($"river:{river.InstanceId}:{river.Direction}:" +
                    $"{river.Depth}:{river.Curvature}:" +
                    string.Join(",", (river.Points ??
                        new List<DistrictRiverPoint>()).Select(point =>
                        $"{point.X:0.0000}/{point.Z:0.0000}")));
      foreach (var flora in district.Flora ??
               new List<PlacedDistrictFlora>())
        if (flora != null)
          parts.Add($"flora:{flora.InstanceId}:{flora.FloraId}:" +
                    $"{flora.NormalizedX:0.0000}:" +
                    $"{flora.NormalizedZ:0.0000}:{flora.Scale:0.000}:" +
                    $"{flora.RotationEighthTurns}");
      // Harvest state is presented incrementally; it cannot invalidate district
      // geometry or walking obstacles. Undo/reload explicitly invalidate the world.
      if (parts.Count == 0) return district.LotId ?? "";
      return string.Join("|", parts);
    }

    private VisualElement ComposeDistrictStatusPanel(RegionCityTile district)
    {
      var panel = new VisualElement();
      panel.AddToClassList("district-simulation-panel");
      if (!string.IsNullOrWhiteSpace(_pendingDistrictLotId))
      {
        panel.Add(StyledLabel("PLACE SAVED LOT",
            "district-unfounded-label"));
        panel.Add(StyledLabel(_pendingDistrictLotName,
            "district-founder-placement-name"));
        var placementHint = StyledLabel("Click inside the district grid to place this complete lot.", "district-founder-hint");
        placementHint.schedule.Execute(() => placementHint.text = string.IsNullOrEmpty(_districtLotWaterHint)
            ? "Click a valid location to place this complete lot."
            : _districtLotWaterHint).Every(100);
        panel.Add(placementHint);
        var cancelLotPlacement = CfButton.Create("CANCEL PLACEMENT", () =>
        {
          _pendingDistrictLotId = "";
          _pendingDistrictLotIsTest = false;
          _pendingDistrictLotName = "";
          Show(AppScreen.DistrictTerraform);
        }, true, "quiet");
        cancelLotPlacement.tooltip = "Cancel placing this saved lot.";
        panel.Add(cancelLotPlacement);
        return panel;
      }
      if (!district.Founded || !string.IsNullOrWhiteSpace(_pendingFounderBuildingId))
      {
        if (!string.IsNullOrWhiteSpace(_pendingFounderBuildingId))
        {
          var founder = Array.Find(FounderBuildings(),
              item => item.Id == _pendingFounderBuildingId);
          var founderName = _pendingFounderBuildingId ==
              DistrictTownCenterFounderId
              ? _pendingFounderLot?.Name ?? "District Town Center"
              : founder.Name;
          panel.Add(StyledLabel("PLACE YOUR FOUNDER BUILDING",
              "district-unfounded-label"));
          panel.Add(StyledLabel(founderName,
              "district-founder-placement-name"));
          panel.Add(StyledLabel(
              "Move the building outline onto the land and click to start your town.",
              "district-founder-hint"));
          var cancelPlacement = CfButton.Create("CANCEL PLACEMENT", () =>
          {
            _pendingFounderBuildingId = "";
            Show(AppScreen.DistrictTerraform);
          }, true, "quiet");
          cancelPlacement.tooltip = "Cancel founder-building placement";
          panel.Add(cancelPlacement);
          return panel;
        }
        panel.Add(StyledLabel("NOT STARTED",
            "district-unfounded-label"));
        var found = CfButton.Create("START DISTRICT OR TOWN",
            ComposeDistrictStartModal, true, "primary");
        found.name = "found-district-button";
        found.tooltip = "Start building a district or establish a town";
        panel.Add(found);
        panel.Add(StyledLabel(
            "Start a district now, or place a Fort or District Town Center to begin a town.",
            "district-founder-hint"));
        return panel;
      }

      // Founded metrics and time controls are composed by the shared map chrome.
      return panel;
    }

    private static void InitializeDistrictStart(RegionCityTile district, int year)
    {
      if (district.Founded) return;
      district.Founded = true;
      district.FoundingYear = year;
      district.Population ??= new DistrictPopulationState();
      district.Population.FoundedSeason = DistrictLabor.State(district).SeasonIndex;
      district.Population.FoundingClockInitialized = true;
    }

    private void ComposeDistrictStartModal()
    {
      var district = FindSelectedRegionTile();
      if (district == null) return;
      var panel = CreateDocumentModal("START DISTRICT OR TOWN",
          "Choose how you want to begin here.");
      const string districtCopy = "Start a district now to begin building and linking resources. You can always start a town here later.";
      const string townCopy = "Place a Fort or District Town Center to begin your town.";
      var caption = StyledLabel(district.Founded ? townCopy : districtCopy, "document-modal-copy");
      caption.name = "district-start-caption";
      var choices = DocumentModalActions();
      var startDistrict = CfButton.Create("Start District", () => ComposeDistrictNamingModal(false),
          !district.Founded, "primary");
      startDistrict.name = "start-district-button";
      var startTown = CfButton.Create("Start Town", () => ComposeDistrictNamingModal(true),
          !(district.Founded && district.Designation == RegionPlaceDesignation.Town), "primary");
      startTown.name = "start-town-button";
      void Explain(Button button, string copy)
      {
        button.tooltip = copy;
        button.RegisterCallback<PointerEnterEvent>(_ => caption.text = copy);
        button.RegisterCallback<FocusInEvent>(_ => caption.text = copy);
      }
      Explain(startDistrict, districtCopy);
      Explain(startTown, townCopy);
      choices.Add(startDistrict);
      choices.Add(startTown);
      panel.Add(choices);
      panel.Add(caption);
      panel.Add(CfButton.Create("CANCEL", RemoveDocumentModal, true, "quiet"));
    }

    private bool ApplyDistrictName(RegionCityTile district, string name)
    {
      if (district == null || string.IsNullOrWhiteSpace(name)) return false;
      district.Name = name.Trim();
      var heading = _root?.Q("map-header")?.Q<Label>(className: "cf-map-location");
      if (heading != null) heading.text = district.Name;
      _root?.Q<TextField>("terraform-district-name-field")?.SetValueWithoutNotify(district.Name);
      return true;
    }

    private void ComposeDistrictNamingModal(bool town)
    {
      var district = FindSelectedRegionTile();
      if (district == null) return;
      var panel = CreateDocumentModal(town ? "NAME YOUR TOWN" : "NAME YOUR DISTRICT",
          town ? "Confirm the name, then choose a Fort or District Town Center." : "Confirm the name to start your district.");
      var name = new TextField("NAME")
      {
        name = "district-start-name",
        value = district.Name ?? ""
      };
      name.AddToClassList("document-field");
      panel.Add(name);
      var actions = DocumentModalActions();
      var ok = CfButton.Create("OK", () =>
      {
        if (!ApplyDistrictName(district, name.value)) return;
        if (!town)
        {
          InitializeDistrictStart(district, UnityEngine.Random.Range(1740, 1761));
          district.Designation = RegionPlaceDesignation.District;
        }
        SaveDistrictEdit();
        RemoveDocumentModal();
        if (town) ComposeFounderBuildingModal();
        else
        {
          RefreshMapMetrics(_root, district);
          _root?.Q<Button>("district-simulation-pause")?.SetEnabled(true);
          _root?.Q<Button>("district-simulation-go")?.SetEnabled(true);
          _root?.Q<Button>("found-district-button")?.parent.RemoveFromHierarchy();
        }
      }, !string.IsNullOrWhiteSpace(name.value), "primary");
      ok.name = "district-start-name-ok";
      name.RegisterValueChangedCallback(evt => ok.SetEnabled(!string.IsNullOrWhiteSpace(evt.newValue)));
      actions.Add(CfButton.Create("CANCEL", RemoveDocumentModal, true, "quiet"));
      actions.Add(ok);
      panel.Add(actions);
      name.schedule.Execute(() => { name.Focus(); name.SelectAll(); });
    }

    private void ComposeFounderBuildingModal()
    {
      RemoveDocumentModal();
      var overlay = new VisualElement { name = "document-modal" };
      overlay.AddToClassList("document-modal");
      var panel = new VisualElement();
      panel.AddToClassList("founder-modal");
      panel.Add(StyledLabel("START TOWN", "founder-modal-title"));
      panel.Add(StyledLabel(
          "Place a Fort or any District Town Center Lot to begin your town.",
          "founder-modal-intro"));
      var scroll = new ScrollView(ScrollViewMode.Vertical);
      scroll.AddToClassList("founder-building-list");
      foreach (var founder in FounderBuildings())
      {
        if (founder.Id != "fortress") continue;
        var captured = founder;
        var card = new Button(() => ArmFounderPlacement(captured.Id));
        bool available = !string.IsNullOrWhiteSpace(captured.LotId) &&
            LotContentCatalog.Read(captured.LotId) != null;
        card.SetEnabled(available);
        card.AddToClassList("founder-building-card");
        card.name = $"founder-{captured.Id}";
        card.tooltip = string.IsNullOrWhiteSpace(captured.LotId)
            ? "Coming later — this founder Lot has not been created yet."
            : $"Place {captured.Name} in this district";
        card.Add(StyledLabel(string.IsNullOrWhiteSpace(captured.LotId)
            ? "HOUSE" : "LOT", "founder-building-icon"));
        var copy = new VisualElement();
        copy.AddToClassList("founder-building-copy");
        copy.Add(StyledLabel(captured.Name, "founder-building-name"));
        copy.Add(StyledLabel(available ? captured.Description : captured.Id == "city-charter-house"
            ? "Coming later — this founder Lot has not been created yet."
            : "Not available yet — a saved building Lot is required.",
            "founder-building-description"));
        card.Add(copy);
        scroll.Add(card);
      }
      // Refresh only when this explicit, infrequent browser opens. Eligibility
      // comes from the authored Lot type; no district objects are scanned.
      LotContentCatalog.InvalidateCache();
      foreach (var summary in LotContentCatalog.All.Where(
                   IsDistrictTownCenterLot))
      {
        var captured = summary;
        var card = new Button(() => ArmDistrictTownCenterPlacement(
            captured.LotId));
        card.AddToClassList("founder-building-card");
        card.name = $"founder-lot-{captured.LotId}";
        card.tooltip = $"Start the town with {captured.Name} and its authored Lot settings";
        card.Add(StyledLabel("LOT", "founder-building-icon"));
        var copy = new VisualElement();
        copy.AddToClassList("founder-building-copy");
        copy.Add(StyledLabel(captured.Name, "founder-building-name"));
        copy.Add(StyledLabel(
            "District Town Center • uses this Lot's authored population, resources, services, jobs, and finances.",
            "founder-building-description"));
        card.Add(copy);
        scroll.Add(card);
      }
      panel.Add(scroll);
      panel.Add(CfButton.Create("CANCEL", RemoveDocumentModal,
          true, "quiet"));
      overlay.Add(panel);
      AttachLargeRegionHoverHelp(overlay);
      _root.Add(overlay);
    }

    private const string DistrictTownCenterFounderId = "district-town-center";
    private LotSaveData _pendingFounderLot;

    private static bool IsDistrictTownCenterLot(LotSaveSummary summary) =>
      summary != null && summary.LotType == LotType.DistrictTownCenter;

    private void ArmFounderPlacement(string founderId)
    {
      var founder = Array.Find(FounderBuildings(), item => item.Id == founderId);
      _pendingFounderLot = string.IsNullOrEmpty(founder.LotId) ? null : LotContentCatalog.Read(founder.LotId);
      if (_pendingFounderLot == null) return;
      ReturnToQuietDistrict();
      _pendingDistrictLotId = "";
      _pendingDistrictLotIsTest = false;
      _pendingFounderBuildingId = founderId ?? "";
      RemoveDocumentModal();
      Show(AppScreen.DistrictTerraform);
    }

    private void ArmDistrictTownCenterPlacement(string lotId)
    {
      var lot = LotContentCatalog.Read(lotId);
      if (lot == null || lot.LotType != LotType.DistrictTownCenter) return;
      _pendingFounderLot = lot;
      ReturnToQuietDistrict();
      _pendingDistrictLotId = "";
      _pendingDistrictLotIsTest = false;
      _pendingFounderBuildingId = DistrictTownCenterFounderId;
      RemoveDocumentModal();
      Show(AppScreen.DistrictTerraform);
    }

    private void ComposeDistrictLotBrowser(LotType? lotType, bool testing = false)
    {
      testing = testing && TestLotToolsAvailable;
      if (!testing && !lotType.HasValue) return;
      // Catalogs are opened explicitly and infrequently. Refresh once here so
      // a Lot manually saved since the previous browse appears immediately.
      LotContentCatalog.InvalidateCache();
      var saves = LotContentCatalog.All.Where(summary =>
          testing || summary.LotType == lotType ||
          lotType == LotType.Civics &&
          summary.LotType == LotType.DistrictTownCenter).ToList();
      var category = testing ? "Test" : lotType == LotType.CivicsParks
          ? "Park" : LotTypeLabel(lotType.Value);
      var panel = CreateDocumentModal(
          testing ? "LOTS" : $"PLACE SAVED {category.ToUpperInvariant()} LOT",
          saves.Count == 0
              ? "No saved Lots yet. Create and save one in the Lot Editor first."
              : testing ? "Place any saved Lot for testing. No construction requirements or costs apply."
              : $"Choose a saved {category.ToLowerInvariant()} Lot. Placement checks its era, population, education, treasury, stockpile, and access requirements.");
      panel.AddToClassList("load-lot-modal-panel");
      panel.AddToClassList("district-lot-browser");

      if (saves.Count > 0)
      {
        var list = new ScrollView(ScrollViewMode.Vertical)
        {
          name = "district-lot-save-list",
          verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible,
          horizontalScrollerVisibility = ScrollerVisibility.Hidden
        };
        list.AddToClassList("lot-save-list");
        list.AddToClassList("district-lot-save-list");
        foreach (var summary in saves)
        {
          var captured = summary;
          var entry = new Button(() => ArmDistrictLotPlacement(
              captured.LotId, captured.Name, testing));
          entry.name = $"district-lot-{captured.LotId}";
          entry.AddToClassList("district-lot-entry");
          entry.tooltip = $"Place {captured.Name} — a " +
              $"{LotTypeLabel(captured.LotType).ToLowerInvariant()} lot measuring " +
              $"{captured.LotWidthCells} by {captured.LotDepthCells} grid cells. " +
              (testing ? "Free test placement." : $"Plop cost: ${captured.PlopCost:N0}.");

          var thumbnail = new VisualElement();
          thumbnail.AddToClassList("lot-save-thumbnail");
          thumbnail.pickingMode = PickingMode.Ignore;
          var thumbnailTexture = LoadSavedLotPreview(summary.LotId) ??
                                 LoadLotThumbnail(summary.BuildingId);
          if (thumbnailTexture != null)
            thumbnail.style.backgroundImage =
                new StyleBackground(thumbnailTexture);
          else
            thumbnail.Add(StyledLabel("LOT", "lot-save-thumbnail-empty"));
          entry.Add(thumbnail);

          var details = new VisualElement();
          details.AddToClassList("lot-save-details");
          details.pickingMode = PickingMode.Ignore;
          details.Add(StyledLabel(summary.Name, "lot-save-entry-title"));
          details.Add(StyledLabel(
              $"{LotTypeLabel(summary.LotType).ToUpperInvariant()}  •  " +
              $"{summary.LotWidthCells} × {summary.LotDepthCells} CELLS  •  " +
              $"{summary.LotWidthCells * 10} × {summary.LotDepthCells * 10} M  •  " +
              (testing ? "FREE TEST PLACEMENT" : $"${summary.PlopCost:N0}"),
              "lot-save-entry-meta"));
          details.Add(StyledLabel("CLICK TO PLACE",
              "district-lot-place-label"));
          entry.Add(details);
          list.Add(entry);
        }
        panel.Add(list);
      }

      var actions = DocumentModalActions();
      var cancel = CfButton.Create("CANCEL", RemoveDocumentModal,
          true, "quiet");
      cancel.tooltip = "Close the saved lot catalog without placing a lot.";
      actions.Add(cancel);
      panel.Add(actions);
      var overlay = _root?.Q<VisualElement>("document-modal");
      AttachLargeRegionHoverHelp(overlay);
    }

    private void ArmDistrictLotPlacement(string lotId, string lotName, bool testing = false)
    {
      _pendingDistrictLotIsTest = false;
      if (LotContentCatalog.Read(lotId) == null) return;
      _pendingDistrictLotIsTest = testing && TestLotToolsAvailable;
      _pendingDistrictLotId = lotId ?? "";
      _pendingDistrictLotName = lotName ?? "Saved Lot";
      RemoveDocumentModal();
      Show(AppScreen.DistrictTerraform);
    }

    private Texture2D LoadSavedLotPreview(string lotId)
    {
      var bundled = LotContentCatalog.PreviewTexture(lotId);
      if (bundled != null) return bundled;
      var previewPath = LotContentCatalog.PreviewPath(lotId);
      if (!File.Exists(previewPath)) return null;
      if (_districtLotPreviewTextures.TryGetValue(previewPath,
              out var cached) && cached != null) return cached;
      try
      {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
          name = $"Saved Lot Preview — {lotId}"
        };
        if (!texture.LoadImage(File.ReadAllBytes(previewPath), false))
        {
          Destroy(texture);
          return null;
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        _districtLotPreviewTextures[previewPath] = texture;
        return texture;
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"Could not load saved lot preview '{previewPath}': " +
                         exception.Message);
        return null;
      }
    }

    private static bool RectanglesOverlap(int leftA, int bottomA,
        int widthA, int depthA, int leftB, int bottomB,
        int widthB, int depthB) =>
        leftA < leftB + widthB && leftA + widthA > leftB &&
        bottomA < bottomB + depthB && bottomA + depthA > bottomB;

    private bool TryGetDistrictLotFootprint(PlacedDistrictLot placement,
        out LotSaveData lot, out int spanX, out int spanZ)
    {
      lot = placement == null ? null : LotContentCatalog.Read(placement.LotId);
      spanX = spanZ = 1;
      if (lot == null) return false;
      spanX = DistrictScale.GridSpanForMeters(
          lot.LotWidthCells * LotMetricScale.MajorGridMeters);
      spanZ = DistrictScale.GridSpanForMeters(
          lot.LotDepthCells * LotMetricScale.MajorGridMeters);
      if ((placement.RotationQuarterTurns & 1) != 0)
        (spanX, spanZ) = (spanZ, spanX);
      return true;
    }

    private PlacedDistrictLot FindDistrictLotAt(RegionCityTile district,
        float normalizedX, float normalizedY)
    {
      if (district?.Lots == null) return null;
      var columns = DistrictScale.Columns(district.Width);
      var rows = DistrictScale.Columns(district.Height);
      var gridX = Mathf.Clamp(Mathf.FloorToInt(normalizedX * columns),
          0, columns - 1);
      var gridZ = Mathf.Clamp(Mathf.FloorToInt(normalizedY * rows),
          0, rows - 1);
      for (var index = district.Lots.Count - 1; index >= 0; index--)
      {
        var placement = district.Lots[index];
        if (!TryGetDistrictLotFootprint(placement, out _,
                out var spanX, out var spanZ)) continue;
        if (normalizedX * columns >= placement.GridX + placement.ShoreOffsetX / 10 &&
            normalizedX * columns < placement.GridX + placement.ShoreOffsetX / 10 + spanX &&
            normalizedY * rows >= placement.GridZ + placement.ShoreOffsetZ / 10 &&
            normalizedY * rows < placement.GridZ + placement.ShoreOffsetZ / 10 + spanZ) return placement;
      }
      return null;
    }

    private bool IsDistrictFootprintClear(RegionCityTile district,
        int gridX, int gridZ, int spanX, int spanZ,
        string ignoredInstanceId = "")
    {
      if (district == null) return false;
      var columns = DistrictScale.Columns(district.Width);
      var rows = DistrictScale.Columns(district.Height);
      if (gridX < 0 || gridZ < 0 || gridX + spanX > columns ||
          gridZ + spanZ > rows) return false;
      foreach (var existing in district.Lots ?? new List<PlacedDistrictLot>())
      {
        if (existing == null ||
            existing.InstanceId == ignoredInstanceId) continue;
        if (!TryGetDistrictLotFootprint(existing, out _,
                out var existingSpanX, out var existingSpanZ)) continue;
        if (RectanglesOverlap(gridX, gridZ, spanX, spanZ,
                existing.GridX, existing.GridZ,
                existingSpanX, existingSpanZ)) return false;
      }
      return true;
    }

    private void RefreshSelectedDistrictLotOutline(RegionCityTile district)
    {
      var selected = district?.Lots?.Find(lot => lot != null &&
          lot.InstanceId == _selectedDistrictLotInstanceId);
      if (selected != null && TryGetDistrictLotFootprint(selected,
              out _, out var spanX, out var spanZ))
        _districtWorld?.ShowLotOutline(selected.GridX, selected.GridZ,
            spanX, spanZ, true, selected.ShoreOffsetX, selected.ShoreOffsetZ);
      else
        _districtWorld?.HideLotOutline();
    }

    private VisualElement ComposeSelectedDistrictLotPanel(RegionCityTile district)
    {
      if (district == null) return null;
      if (_districtSelection.Count == 1)
        return BuildSelectedObjectPanel(_districtSelection[0]);
      if (!string.IsNullOrEmpty(_selectedDistrictLotInstanceId))
        return BuildSelectedObjectPanel(new DistrictSelectionRef(DistrictSelectionKind.Lot, _selectedDistrictLotInstanceId));
      return null;
    }

    private bool CanRotateDistrictLot(RegionCityTile district,
        PlacedDistrictLot placement, int direction)
    {
      if (!TryGetDistrictLotFootprint(placement, out _,
              out var spanX, out var spanZ)) return false;
      var rotatedSpanX = spanZ;
      var rotatedSpanZ = spanX;
      var columns = DistrictScale.Columns(district.Width);
      var rows = DistrictScale.Columns(district.Height);
      var rotatedGridX = placement.GridX;
      var rotatedGridZ = placement.GridZ;
      return IsDistrictFootprintClear(district, rotatedGridX,
          rotatedGridZ, rotatedSpanX, rotatedSpanZ,
          placement.InstanceId) && (_districtWorld == null || _districtWorld.ValidateLotBoatPlacement(district,
              new PlacedDistrictLot
              {
                LotId = placement.LotId,
                GridX = rotatedGridX,
                GridZ = rotatedGridZ,
                RotationQuarterTurns = (placement.RotationQuarterTurns + direction + 4) % 4,
                ShoreOffsetX = placement.ShoreOffsetX,
                ShoreOffsetZ = placement.ShoreOffsetZ
              },
              LotContentCatalog.Read(placement.LotId), out _));
    }

    private DistrictActionResult TryRotatePlacedDistrictLot(RegionCityTile district,
        PlacedDistrictLot placement, int direction)
    {
      var warning = CanRotateDistrictLot(district, placement, direction) ? "" :
        "The rotated lot may overlap another lot, extend beyond the district, or have unsuitable water access.";
      if (!TryGetDistrictLotFootprint(placement, out _, out var oldSpanX, out var oldSpanZ))
        return "The lot's saved footprint is unavailable.";
      var oldX = placement.GridX; var oldZ = placement.GridZ; var oldRotation = placement.RotationQuarterTurns;
      placement.RotationQuarterTurns = (placement.RotationQuarterTurns + direction + 4) % 4;
      if (_districtWorld != null && !_districtWorld.UpdatePlacedLotTransform(district, placement, allowPlacementConflicts: true))
      {
        placement.GridX = oldX; placement.GridZ = oldZ; placement.RotationQuarterTurns = oldRotation;
        return "The lot could not be rotated.";
      }
      InvalidateDistrictRoadLotCells();
      return DistrictActionResult.Applied(warning);
    }

    private void RotateDistrictLot(RegionCityTile district,
        PlacedDistrictLot placement, int direction)
    {
      EnsureDistrictUndo(district);
      var result = TryRotatePlacedDistrictLot(district, placement, direction);
      if (!result.Succeeded) { ShowDistrictNotice(result.Message); return; }
      SaveDistrictEdit();
      _districtWorldCompositionKey = DistrictCompositionKey(district);
      Show(AppScreen.DistrictTerraform);
    }

    private bool IsOffsetDistrictFootprintClear(RegionCityTile district, int x, int z, int sx, int sz, Vector2 offset, string ignored = null)
    {
      var left = x * 10 + offset.x; var bottom = z * 10 + offset.y;
      if (left < 0 || bottom < 0 || left + sx * 10 > DistrictScale.SizeMeters(district.Width) || bottom + sz * 10 > DistrictScale.SizeMeters(district.Height)) return false;
      foreach (var existing in district.Lots ?? new List<PlacedDistrictLot>())
      {
        if (existing.InstanceId == ignored || !TryGetDistrictLotFootprint(existing, out _, out var ex, out var ez)) continue;
        var el = existing.GridX * 10 + existing.ShoreOffsetX; var eb = existing.GridZ * 10 + existing.ShoreOffsetZ;
        if (left < el + ex * 10 && left + sx * 10 > el && bottom < eb + ez * 10 && bottom + sz * 10 > eb) return false;
      }
      return true;
    }
    private Vector2 _pendingDistrictLotShoreOffset;
    private bool _pendingDistrictLotHasBoatDockOverride;
    private Vector2 _pendingDistrictLotBoatDockLocal;
    private Vector2 _pendingDistrictLotBoatMooringLocal;
    private int _pendingDistrictLotBoatDockTurns;
    private int _pendingDistrictLotRotation;
    private string _districtLotWaterHint = "";
    private bool _pendingDistrictLotIsTest;
    private static bool TestLotToolsAvailable
    {
      get
      {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return true;
#else
        return false;
#endif
      }
    }
    private bool IsTestLotPlacement => TestLotToolsAvailable &&
        _pendingDistrictLotIsTest && !string.IsNullOrWhiteSpace(_pendingDistrictLotId);

    public static int DistrictLotRotationFromSavedView(LotSaveData lot)
    {
      var view = lot?.EditorView;
      if (view == null || !view.Valid || view.TopDown) return 0;
      var octant = ((view.OrbitOctant % 8) + 8) % 8;
      var clockwiseDiagonal = (octant + 1) / 2;
      var authoredTurns = (4 - clockwiseDiagonal) & 3;
      if (TrySavedCameraOrbitOctant(view, out var cameraOctant))
      {
        // Preserve the established octant-label mapping, then apply only the
        // signed difference carried by the real camera. This keeps consistent
        // saves unchanged while correcting stale labels in the same direction
        // the author actually turned the Lot Editor camera.
        var deltaOctants = ((cameraOctant - octant + 4) & 7) - 4;
        var correction = deltaOctants >= 0
            ? (deltaOctants + 1) / 2
            : (deltaOctants - 1) / 2;
        return (authoredTurns + correction) & 3;
      }
      // The district camera looks across the Lot from the opposite diagonal,
      // and hosted Lots already carry the matching 180-degree camera offset.
      // Saved diagonal views therefore map to quarter turns in reverse order:
      // NE=0, SE=3, SW=2, NW=1. Cardinal authoring views sit between grid-safe
      // orientations and choose the next clockwise diagonal.
      return authoredTurns;
    }

    private static bool TrySavedCameraOrbitOctant(
        LotEditorViewState view, out int octant)
    {
      octant = 0;
      if (view.OrthographicSize <= 0f) return false;
      var rotation = view.Rotation;
      var magnitude = rotation.x * rotation.x + rotation.y * rotation.y +
          rotation.z * rotation.z + rotation.w * rotation.w;
      if (magnitude < 0.5f) return false;

      // The rendered camera transform is the authoritative saved view. A few
      // real Lots carry an old OrbitOctant label that disagrees with that
      // transform, so trusting the label rotates their district placement by
      // one quarter turn. Recover the camera's horizontal position direction
      // from its look rotation and snap it to the same eight-octant contract.
      var awayFromTarget = -(rotation * Vector3.forward);
      awayFromTarget.y = 0f;
      if (awayFromTarget.sqrMagnitude < 0.01f) return false;
      var degrees = Mathf.Repeat(Mathf.Atan2(
          awayFromTarget.z, awayFromTarget.x) * Mathf.Rad2Deg, 360f);
      octant = Mathf.RoundToInt(
          Mathf.Repeat(degrees - 45f, 360f) / 45f) & 7;
      return true;
    }

    private bool QuotePendingDistrictLot(RegionCityTile district, LotSaveData lot,
        int x, int z, int width, int depth, out int cost, out long[] resources, out string reason)
    {
      cost = 0; resources = null; reason = "";
      if (district == null || lot == null) { reason = "Lot unavailable."; return false; }
      if (IsTestLotPlacement) return true;
      cost = LotEconomy.CalculatePlopCost(lot);
      return DistrictLotRequirements.Quote(district, lot, _openRegion.EraId,
          x, z, width, depth,
          (cx, cz) => _districtWorld != null && _districtWorld.HasRoadAtCell(cx, cz),
          n => _districtWorld != null && _districtWorld.IsUnderRiverWater(n),
          _pendingDistrictLotShoreOffset, out resources, out reason);
    }
    private bool TryDistrictLotFootprint(RegionCityTile district,
        float normalizedX, float normalizedY, out int gridX,
        out int gridZ, out int spanX, out int spanZ, out bool placeable)
    {
      gridX = gridZ = 0;
      spanX = spanZ = 1;
      placeable = false;
      var lot = LotContentCatalog.Read(_pendingDistrictLotId);
      if (district == null || lot == null) return false;
      var columns = DistrictScale.Columns(district.Width);
      var rows = DistrictScale.Columns(district.Height);
      var width = DistrictScale.GridSpanForMeters(lot.LotWidthCells * LotMetricScale.MajorGridMeters);
      var depth = DistrictScale.GridSpanForMeters(lot.LotDepthCells * LotMetricScale.MajorGridMeters);
      var authoredRotation = DistrictLotRotationFromSavedView(lot);
      _districtLotWaterHint = ""; _pendingDistrictLotRotation = authoredRotation; _pendingDistrictLotShoreOffset = Vector2.zero;
      _pendingDistrictLotHasBoatDockOverride = false;
      _pendingDistrictLotBoatDockLocal = Vector2.zero;
      _pendingDistrictLotBoatMooringLocal = Vector2.zero;
      _pendingDistrictLotBoatDockTurns = 0;
      var nearRiver = !lot.HasWaterOrientation ||
          DistrictRiverEditing.FindAt(district,
              new Vector2(normalizedX, normalizedY)) != null;
      var alignsCompleteLotToRiver = lot.HasWaterOrientation &&
          lot.Buildings3D?.Any(building =>
              building.AssetId == "lumber-mill-v01") == true;
      var rotationAttempts = lot.HasWaterOrientation ? 4 : 1;
      for (var attempt = 0; attempt < rotationAttempts; attempt++)
      {
        var turns = (authoredRotation + attempt) & 3;
        var sx = (turns & 1) == 0 ? width : depth; var sz = (turns & 1) == 0 ? depth : width;
        if (sx > columns || sz > rows) continue;
        var x = Mathf.Clamp(Mathf.RoundToInt(normalizedX * columns - sx * .5f), 0, columns - sx);
        var z = Mathf.Clamp(Mathf.RoundToInt(normalizedY * rows - sz * .5f), 0, rows - sz);
        if (attempt == 0) { gridX = x; gridZ = z; spanX = sx; spanZ = sz; }
        if (!IsDistrictFootprintClear(district, x, z, sx, sz)) continue;
        var offsets = alignsCompleteLotToRiver
            ? new[] { Vector2.zero }.AsEnumerable()
            : lot.HasWaterOrientation && nearRiver
            ? DistrictLotShoreOffsets(lot, turns)
            : new[] { Vector2.zero }.AsEnumerable();
        foreach (var offset in offsets)
        {
          if (!IsOffsetDistrictFootprintClear(district, x, z, sx, sz, offset)) continue;
          var candidate = new PlacedDistrictLot
          {
            LotId = lot.LotId,
            GridX = x,
            GridZ = z,
            RotationQuarterTurns = turns,
            ShoreOffsetX = offset.x,
            ShoreOffsetZ = offset.y
          };
          if (!IsTestLotPlacement && _districtWorld != null && !_districtWorld.ValidateLotBoatPlacement(district, candidate, lot, out _districtLotWaterHint)) continue;
          var alignedOffset = new Vector2(candidate.ShoreOffsetX,
              candidate.ShoreOffsetZ);
          if (!IsOffsetDistrictFootprintClear(district, x, z, sx, sz,
                  alignedOffset)) continue;
          gridX = x; gridZ = z; spanX = sx; spanZ = sz; _pendingDistrictLotRotation = turns; _pendingDistrictLotShoreOffset = alignedOffset;
          _pendingDistrictLotHasBoatDockOverride = candidate.HasBoatDockOverride;
          _pendingDistrictLotBoatDockLocal = new Vector2(candidate.BoatDockLocalX,
              candidate.BoatDockLocalZ);
          _pendingDistrictLotBoatMooringLocal = new Vector2(
              candidate.BoatMooringLocalX, candidate.BoatMooringLocalZ);
          _pendingDistrictLotBoatDockTurns = candidate.BoatDockRotationQuarterTurns;
          placeable = true;
          _districtLotWaterHint = IsTestLotPlacement ? "Free test placement — construction requirements bypassed." : lot.HasWaterOrientation ? "Shoreline fits — lot aligned with the river bank." : "";
          return true;
        }
      }
      return true;
    }

    private static IEnumerable<Vector2> DistrictLotShoreOffsets(
        LotSaveData lot, int quarterTurns)
    {
      var local = lot.WaterOrientationWater - lot.WaterOrientationLand;
      var facing = new Vector2(local.x, local.z);
      if (facing.sqrMagnitude < .0001f) facing = Vector2.right;
      var rotated = Quaternion.Euler(0, quarterTurns * 90f, 0) *
          new Vector3(facing.x, 0, facing.y);
      facing = new Vector2(rotated.x, rotated.z).normalized;
      var tangent = new Vector2(-facing.y, facing.x);
      foreach (var tangentDistance in new[] { 0f })
      {
        yield return tangent * tangentDistance;
        for (var distance = 2; distance <= 20; distance += 2)
        {
          yield return facing * distance + tangent * tangentDistance;
          yield return -facing * distance + tangent * tangentDistance;
        }
      }
    }

    private bool HandleArmedDistrictLotPointer(RegionCityTile district, PointerDownEvent evt)
    {
      bool founder = !string.IsNullOrWhiteSpace(_pendingFounderBuildingId);
      if (!founder && string.IsNullOrWhiteSpace(_pendingDistrictLotId)) return false;
      // An armed Lot owns the world click, including invalid ground positions.
      // Selection/inspection must not consume it or start a drag underneath it.
      if (_districtWorld != null && _districtWorld.TryGroundPoint(
          DistrictCameraPoint(evt.position), out var normalized))
      {
        if (founder) PlaceFounderBuilding(district, normalized.x, normalized.y);
        else PlaceDistrictLot(district, normalized.x, normalized.y);
      }
      evt.StopImmediatePropagation();
      return true;
    }

    private void PlaceDistrictLot(RegionCityTile district, float x, float y)
    {
      var lot = LotContentCatalog.Read(_pendingDistrictLotId);
      if (district == null || lot == null) return;
      var testPlacement = IsTestLotPlacement;
      if (!TryDistrictLotFootprint(district, x, y, out var gridX,
              out var gridZ, out var footprintWidth, out var footprintDepth, out var placeable) ||
          !placeable) return;
      if (!QuotePendingDistrictLot(district, lot, gridX, gridZ, footprintWidth, footprintDepth,
          out var plopCost, out var constructionResources, out var reason))
      { ShowDistrictNotice(reason); return; }
      DistrictLotSimulation.For(district);
      district.Lots ??= new List<PlacedDistrictLot>();
      var instanceId = Guid.NewGuid().ToString("N");
      var placement = new PlacedDistrictLot
      {
        InstanceId = instanceId,
        LotId = _pendingDistrictLotId,
        GridX = gridX,
        GridZ = gridZ,
        RotationQuarterTurns = _pendingDistrictLotRotation,
        ShoreOffsetX = _pendingDistrictLotShoreOffset.x,
        ShoreOffsetZ = _pendingDistrictLotShoreOffset.y,
        HasBoatDockOverride = _pendingDistrictLotHasBoatDockOverride,
        BoatDockLocalX = _pendingDistrictLotBoatDockLocal.x,
        BoatDockLocalZ = _pendingDistrictLotBoatDockLocal.y,
        BoatDockRotationQuarterTurns = _pendingDistrictLotBoatDockTurns,
        BoatMooringLocalX = _pendingDistrictLotBoatMooringLocal.x,
        BoatMooringLocalZ = _pendingDistrictLotBoatMooringLocal.y,
        BoatDockContractVersion = _pendingDistrictLotHasBoatDockOverride ? 2 : 0
      };
      district.Lots.Add(placement);
      if (_districtWorld != null &&
          !_districtWorld.AddPlacedLot(district, placement, testPlacement))
      {
        district.Lots.Remove(placement);
        return;
      }
      district.Treasury -= plopCost;
      DistrictLotRequirements.Consume(district, constructionResources);
      DistrictLotSimulation.For(district).Add(instanceId, lot,
          placement.HasPopulationOverride, placement.PopulationOverride);
      _districtWorld?.HideLotPlacementGuide();
      _pendingDistrictLotId = "";
      _pendingDistrictLotIsTest = false;
      _pendingDistrictLotName = "";
      _selectedDistrictLotInstanceId = instanceId;
      SaveDistrictEdit();
      if (_districtWorld != null)
        _districtWorldCompositionKey = DistrictCompositionKey(district);
      Show(AppScreen.DistrictTerraform);
    }

    private void PlaceFounderBuilding(RegionCityTile district, float x,
        float y)
    {
      var authoredTownCenter = _pendingFounderBuildingId ==
          DistrictTownCenterFounderId;
      var founder = Array.Find(FounderBuildings(),
          item => item.Id == _pendingFounderBuildingId);
      var lot = _pendingFounderLot;
      if (district == null || lot == null ||
          (authoredTownCenter &&
              lot.LotType != LotType.DistrictTownCenter) ||
          (!authoredTownCenter && string.IsNullOrWhiteSpace(founder.Id))) return;
      var founderId = authoredTownCenter
          ? DistrictTownCenterFounderId : founder.Id;
      var founderName = authoredTownCenter ? lot.Name : founder.Name;
      var snapped = SnapFounderPlacement(district, x, y);
      if (!TryFounderFootprint(district, x, y, out var gridX, out var gridZ,
          out var spanX, out var spanZ)) return;
      var placement = new PlacedDistrictLot
      {
        InstanceId = Guid.NewGuid().ToString("N"),
        LotId = lot.LotId,
        HasPopulationOverride = !authoredTownCenter,
        PopulationOverride = 0,
        GridX = gridX,
        GridZ = gridZ
      };
      if (!IsDistrictFootprintClear(district, placement.GridX, placement.GridZ, spanX, spanZ))
      { ShowDistrictNotice("Choose an open area for the founder Lot."); return; }
      DistrictLotSimulation.For(district);
      district.Lots ??= new List<PlacedDistrictLot>();
      district.Lots.Add(placement);
      if (_districtWorld != null && !_districtWorld.AddPlacedLot(district, placement))
      {
        district.Lots.Remove(placement);
        return;
      }
      InitializeDistrictStart(district, UnityEngine.Random.Range(1740, 1761));
      district.Designation = RegionPlaceDesignation.Town;
      district.FounderBuildingId = founderId;
      district.FounderBuildingName = founderName;
      district.LotId = lot.LotId;
      district.FounderNormalizedX = snapped.x;
      district.FounderNormalizedY = snapped.y;
      if (!authoredTownCenter)
        ApplyFounderStartingFood(district, founder.Id);
      DistrictLotSimulation.For(district).Add(placement.InstanceId, lot,
          placement.HasPopulationOverride, placement.PopulationOverride);
      _pendingFounderBuildingId = "";
      _pendingDistrictLotId = "";
      _pendingDistrictLotIsTest = false;
      _pendingDistrictLotName = "";
      _districtYear = district.FoundingYear;
      SaveDistrictEdit();
      if (_districtWorld != null)
      {
        _districtWorldLotId = district.LotId;
        _districtWorldCompositionKey = DistrictCompositionKey(district);
        _districtWorld.HideLotPlacementGuide();
      }
      RefreshMapMetrics(_root, district);
      _root?.Q<Button>("district-simulation-pause")?.SetEnabled(true);
      _root?.Q<Button>("district-simulation-go")?.SetEnabled(true);
      _root?.Q(className: "district-simulation-panel")?.RemoveFromHierarchy();
      if (!authoredTownCenter && founder.Id == "fortress")
        ShowFortNextSteps();
    }

    private static int FounderStartingFood(string founderBuildingId) =>
      founderBuildingId switch
      {
        "fortress" => 250,
        _ => 0
      };

    private static void ApplyFounderStartingFood(RegionCityTile district,
        string founderBuildingId)
    {
      if (district == null) return;
      var startingFood = FounderStartingFood(founderBuildingId);
      if (startingFood <= 0) return;
      district.ResourceInventory ??= new DistrictResourceInventory();
      district.ResourceInventory.Food = Math.Max(
          district.ResourceInventory.Food, startingFood);
    }

    private void ShowFortNextSteps()
    {
      if (_root == null) return;
      _root.Q("fort-next-steps")?.RemoveFromHierarchy();
      var panel = new VisualElement { name = "fort-next-steps" };
      panel.AddToClassList("cf-map-chrome");
      panel.AddToClassList("fort-next-steps");
      var close = CfButton.Create("X", () => panel.RemoveFromHierarchy(), true, "quiet");
      close.name = "fort-next-steps-close";
      close.tooltip = "Close next steps";
      close.AddToClassList("fort-next-steps-close");
      panel.Add(close);
      panel.Add(StyledLabel("YOUR TOWN HAS BEGUN", "fort-next-steps-kicker"));
      panel.Add(StyledLabel("Start with lumber", "fort-next-steps-title"));
      panel.Add(StyledLabel(
          "Place a few lumberjacks near trees to begin gathering wood. A steady supply of lumber will help you build your new town.",
          "fort-next-steps-copy"));
      panel.RegisterCallback<PointerEnterEvent>(_ => _districtEdgePanDirection = Vector2Int.zero);
      _root.Add(panel);
      // Wait for initial layout so only entry animates. Closing removes the panel immediately.
      EventCallback<GeometryChangedEvent> reveal = null;
      reveal = _ =>
      {
        panel.UnregisterCallback(reveal);
        panel.schedule.Execute(() => panel.AddToClassList("fort-next-steps--visible")).ExecuteLater(16);
      };
      panel.RegisterCallback(reveal);
    }

    private string _timberMillWarningDistrictId = "";
    private bool _timberMillWarningShown;

    private void UpdateTimberMillWarning(RegionCityTile district, bool waiting)
    {
      var districtId = district?.TileId ?? "";
      if (_timberMillWarningDistrictId != districtId)
      {
        _timberMillWarningDistrictId = districtId;
        _timberMillWarningShown = false;
        _root?.Q("timber-mill-warning")?.RemoveFromHierarchy();
      }
      if (!waiting)
      {
        _timberMillWarningShown = false;
        _root?.Q("timber-mill-warning")?.RemoveFromHierarchy();
        return;
      }
      if (_timberMillWarningShown) return;
      _timberMillWarningShown = true;
      ShowTimberMillWarning();
    }

    private void ShowTimberMillWarning(Action placeAction = null)
    {
      if (_root == null) return;
      _root.Q("fort-next-steps")?.RemoveFromHierarchy();
      _root.Q("timber-mill-warning")?.RemoveFromHierarchy();
      var panel = new VisualElement { name = "timber-mill-warning" };
      panel.AddToClassList("cf-map-chrome");
      panel.AddToClassList("fort-next-steps");
      panel.AddToClassList("timber-mill-warning");
      var close = CfButton.Create("X", () => panel.RemoveFromHierarchy(),
          true, "quiet");
      close.name = "timber-mill-warning-close";
      close.tooltip = "Dismiss lumber delivery warning";
      close.AddToClassList("fort-next-steps-close");
      panel.Add(close);
      panel.Add(StyledLabel("LUMBER DELIVERY BLOCKED",
          "fort-next-steps-kicker"));
      panel.Add(StyledLabel("Lumber cart has no mill to drive to",
          "fort-next-steps-title"));
      panel.Add(StyledLabel(
          "Place a Lumber Mill beside a river and connect it to the camp by road so the cart can deliver its load.",
          "fort-next-steps-copy"));
      var mill = LotContentCatalog.Read("lumber-mill-dock-operations-v01");
      var place = CfButton.Create("Place Lumber Mill", () =>
      {
        panel.RemoveFromHierarchy();
        if (placeAction != null) placeAction();
        else ArmDistrictLotPlacement("lumber-mill-dock-operations-v01",
            mill?.Name ?? "Lumber Mill Dock Operations v01");
      }, placeAction != null || mill != null, "primary");
      place.name = "timber-mill-warning-place";
      place.tooltip = mill != null || placeAction != null
          ? "Arm the Lumber Mill Lot for normal district placement."
          : "The saved Lumber Mill Lot is unavailable.";
      panel.Add(place);
      panel.RegisterCallback<PointerEnterEvent>(_ =>
          _districtEdgePanDirection = Vector2Int.zero);
      _root.Add(panel);
      EventCallback<GeometryChangedEvent> reveal = null;
      reveal = _ =>
      {
        panel.UnregisterCallback(reveal);
        panel.schedule.Execute(() =>
            panel.AddToClassList("fort-next-steps--visible")).ExecuteLater(16);
      };
      panel.RegisterCallback(reveal);
    }

    private bool TryFounderFootprint(RegionCityTile district, float x, float y,
        out int gridX, out int gridZ, out int width, out int depth)
    {
      gridX = gridZ = width = depth = 0;
      var lot = _pendingFounderLot;
      if (district == null || lot == null) return false;
      var columns = DistrictScale.Columns(district.Width);
      var rows = DistrictScale.Columns(district.Height);
      width = DistrictScale.GridSpanForMeters(lot.LotWidthCells * LotMetricScale.MajorGridMeters);
      depth = DistrictScale.GridSpanForMeters(lot.LotDepthCells * LotMetricScale.MajorGridMeters);
      if (width > columns || depth > rows) return false;
      var snapped = SnapFounderPlacement(district, x, y);
      gridX = Mathf.Clamp(Mathf.RoundToInt(snapped.x * columns - width * .5f), 0, columns - width);
      gridZ = Mathf.Clamp(Mathf.RoundToInt(snapped.y * rows - depth * .5f), 0, rows - depth);
      return true;
    }

    private Vector2 SnapFounderPlacement(RegionCityTile district,
        float normalizedX, float normalizedY)
    {
      var lot = _pendingFounderLot;
      var widthMeters = lot != null
          ? lot.LotWidthCells * 10f : DistrictScale.CellSizeMeters;
      var depthMeters = lot != null
          ? lot.LotDepthCells * 10f : DistrictScale.CellSizeMeters;
      return new Vector2(
          DistrictScale.SnapFootprintCenter(normalizedX,
              DistrictScale.Columns(district.Width), widthMeters),
          DistrictScale.SnapFootprintCenter(normalizedY,
              DistrictScale.Columns(district.Height), depthMeters));
    }

    private static (string Id, string Name, string Description, string LotId)[]
        FounderBuildings() => new[]
        {
                ("fortress", "Fort", "Establish a defended frontier town using your saved Fortress Lot.", "fortress-lot"),
                ("ranger-station", "Ranger Station", "Establish a protected landscape centered on stewardship, trails, and conservation.", ""),
                ("pioneer-farmstead", "1785 Farm", "Found an agricultural district with a New England farmhouse, red barn, crop fields, fences, and mature trees.", "1785-farm"),
                ("prospectors-office", "Prospector's Office", "Survey mineral deposits and establish a resource-focused settlement.", ""),
                ("trading-post", "Frontier Trading Post", "Create a crossroads for commerce, supplies, travelers, and regional exchange.", ""),
                ("village-hall", "Village Hall", "Found a compact small town organized around local civic life.", ""),
                ("river-landing", "River Landing", "Build around waterways, shipping, fishing, and future waterfront industry.", ""),
                ("city-charter-house", "Legacy City Center", "Compatibility identity for older town saves.", "")
        };

    private void SetDistrictSimulationPaused(bool paused)
    {
      _districtSimulationPaused = paused;
      Time.timeScale = paused ? 0f : 1f;
      RefreshDistrictTimeControls();
    }

    private void RefreshDistrictTimeControls()
    {
      var pause = _root?.Q<Button>("district-simulation-pause");
      var go = _root?.Q<Button>("district-simulation-go");
      pause?.EnableInClassList("district-time-button--selected",
          _districtSimulationPaused);
      go?.EnableInClassList("district-time-button--selected",
          !_districtSimulationPaused);
      var status = _root?.Q<Label>(className: "district-simulation-status");
      if (status != null)
        status.text = _districtSimulationPaused
            ? "SIMULATION PAUSED"
            : "SIMULATION RUNNING";
    }

    private void TickDistrictTimeOfDay()
    {
      if (_root == null || _currentScreen != AppScreen.DistrictTerraform ||
          _districtWorld == null || _districtSimulationPaused) return;
      var district = _districtWorldTile;
      if (district == null || !district.Founded) return;
      if (!DistrictDayCycle.Advance(district,
              Mathf.Min(Time.deltaTime, .1f))) return;
      _districtWorld.SetTimeOfDay(district.TimeOfDay);
      RefreshMapMetrics(_root, district);
    }

    private void LeaveDistrictEditor()
    {
      SetDistrictRoadDeleteCursor(false);
      CancelLaborPlacement();
      SaveDistrictEdit();
      SetDistrictSimulationPaused(false);
      _pendingDistrictLotId = "";
      _pendingDistrictLotIsTest = false;
      _pendingDistrictLotName = "";
      _hoveredDistrictLotInstanceId = "";
      _selectedDistrictLotInstanceId = "";
      _hasSelectedDistrictRoad = false;
      _districtWorldTile = null;
      Show(AppScreen.RegionEditor);
    }

    private void PollTerraformViewKeys()
    {
      if (MapPanningBlocked)
      {
        _districtEdgePanDirection = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.Escape)) RemoveDocumentModal();
        return;
      }
      if (!Application.isFocused)
      {
        _districtEdgePanDirection = Vector2Int.zero;
        return;
      }
      // Physical polling owns H, including when the world viewport has focus.
      // The caller already excludes text fields; leave modal dialogs visible.
      if (Input.GetKeyDown(KeyCode.H) &&
          !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) &&
          !Input.GetKey(KeyCode.LeftCommand) && !Input.GetKey(KeyCode.RightCommand))
      { ToggleDistrictInterface(); return; }
      if (_lotNudge != null) { if (Input.GetKeyDown(KeyCode.Escape)) EndLotNudge(true); return; }
      if(_placingBrickworks)
      {
        if(Input.GetKeyDown(KeyCode.Escape)){CancelBrickworksPlacement();return;}
        if(Input.GetKeyDown(KeyCode.R)){RotateBrickworksPreview();return;}
      }
      if ((_pendingLaborCount > 0 || _placingMarksman) && Input.GetKeyDown(KeyCode.Escape)) { CancelLaborPlacement(); return; }
      if (Input.GetKeyDown(KeyCode.Delete) ||
          Input.GetKeyDown(KeyCode.Backspace))
      {
        // River and marquee selections use the shared district
        // selection model. Handle them before the older, dedicated
        // builder-road selection path.
        if ((DistrictWaterSelectToolActive() ||
             DistrictSelectToolActive() || DistrictMoveToolActive() ||
             !string.IsNullOrWhiteSpace(_selectedDistrictLotInstanceId)) &&
            DeleteDistrictSelection())
          return;
        if (TryDeleteSelectedDistrictRoad()) return;
      }
      if (Input.GetKeyDown(KeyCode.Escape))
      {
        SetDistrictRoadDeleteCursor(false);
        if (_root.Q("document-modal") != null) RemoveDocumentModal();
        else ReturnToQuietDistrict();
        return;
      }
      if (_root.Q("document-modal") != null) return;
      if (Input.GetKeyDown(KeyCode.B))
      {
        ToggleDistrictPalette(DistrictEditorMode.Builder);
        return;
      }
      if (Input.GetKeyDown(KeyCode.T))
      {
        ToggleDistrictPalette(DistrictEditorMode.Terraform);
        return;
      }

      var changed = false;
      var panStep = DistrictZoom.PanStepMeters(_terraformZoomLevel) *
          (_terraformZoomLevel == DistrictZoomLevel.LOD0 ? 3f : 2f) *
          DistrictZoom.PanSpeedScale(_terraformZoomLevel);
      // Every stop has its own perceived-speed calibration. In particular,
      // player-facing Zooms 4–6 are intentionally slower than close inspection
      // for both edge hovering and arrow keys.
      var horizontalWorldMotion = 0;
      var verticalWorldMotion = 0;
      if (Input.GetKeyDown(KeyCode.LeftArrow))
      {
        horizontalWorldMotion = 1;
      }
      else if (Input.GetKeyDown(KeyCode.RightArrow))
      {
        horizontalWorldMotion = -1;
      }
      if (Input.GetKeyDown(KeyCode.UpArrow))
      {
        verticalWorldMotion = -1;
      }
      else if (Input.GetKeyDown(KeyCode.DownArrow))
      {
        verticalWorldMotion = 1;
      }
      if (horizontalWorldMotion != 0 || verticalWorldMotion != 0)
      {
        if (MoveSelectedDistrictRiver(horizontalWorldMotion,
                verticalWorldMotion))
          return;
        _terraformPanOffset += DistrictZoom.PanOffsetForWorldMotion(
            horizontalWorldMotion, verticalWorldMotion, panStep);
        changed = true;
      }

      if (_districtEdgePanDirection != Vector2Int.zero)
      {
        var continuousSpeed = _districtWorld?.WorldCamera != null
            ? DistrictZoom.EdgePanSpeedMetersPerSecond(
                _districtWorld.WorldCamera.orthographicSize)
            : panStep * 2.4f;
        var continuousStep = continuousSpeed * Time.unscaledDeltaTime;
        _terraformPanOffset += DistrictZoom.PanOffsetForWorldMotion(
            _districtEdgePanDirection.x,
            _districtEdgePanDirection.y, continuousStep);
        if (_districtWorld != null)
          _terraformPanOffset = _districtWorld.ClampPan(
              _terraformPanOffset);
        changed = true;
      }

      if (Input.GetKeyDown(KeyCode.Equals) ||
          Input.GetKeyDown(KeyCode.KeypadPlus))
      {
        var next = DistrictZoom.Step(_terraformZoomLevel, -1);
        changed |= next != _terraformZoomLevel;
        _terraformZoomLevel = next;
      }
      else if (Input.GetKeyDown(KeyCode.Minus) ||
               Input.GetKeyDown(KeyCode.KeypadMinus))
      {
        var next = DistrictZoom.Step(_terraformZoomLevel, 1);
        changed |= next != _terraformZoomLevel;
        _terraformZoomLevel = next;
      }

      if (!changed) return;
      if (_districtWorld != null)
      {
        _terraformPanOffset = _districtWorld.ClampPan(
            _terraformPanOffset);
        _districtWorld.SetPan(_terraformPanOffset);
        if (_districtWorld.ZoomLevel != _terraformZoomLevel)
          _districtWorld.SetZoom(_terraformZoomLevel);
        RefreshTerraformZoomLabel();
        return;
      }
      var viewport = _root?.Q<VisualElement>(className: "district-terraform-viewport");
      var projection = _root?.Q<VisualElement>("district-terraform-projection");
      var plane = _root?.Q<VisualElement>("district-terraform-plane");
      if (viewport == null || projection == null || plane == null) return;
      ApplyTerraformViewTransform(viewport, projection);
      RebuildTerraformGrid(plane, _terraformZoomLevel);
      ApplyTerraformGrassLevel(plane, _terraformZoomLevel);
      RefreshTerraformZoomLabel();
    }

    private void ApplyTerraformViewTransform(VisualElement viewport,
        VisualElement projection)
    {
      const float lotElevationScale = 0.34202015f;
      var zoomScale = DistrictZoom.Scale(_terraformZoomLevel);
      projection.style.scale = new Scale(new Vector2(
          zoomScale, zoomScale * lotElevationScale));
      projection.style.left = viewport.resolvedStyle.width * 0.5f +
          _terraformPanOffset.x;
      projection.style.top = viewport.resolvedStyle.height * 0.46f +
          _terraformPanOffset.y;
    }

    private string TerraformDistrictMeta(RegionCityTile district,
        int columns, int rows) =>
        $"{DistrictZoom.DisplayName(_terraformZoomLevel)}  •  " +
        $"{columns} × {rows} CELLS  •  " +
        $"{DistrictScale.SizeMeters(district.Width) / 1000f:0.0} × " +
        $"{DistrictScale.SizeMeters(district.Height) / 1000f:0.0} KM  •  " +
        $"{_districtEditorMode.ToString().ToUpperInvariant()}  •  " +
        $"{ActiveDistrictTool.ToUpperInvariant()} SELECTED";

    private void RefreshTerraformZoomLabel()
    {
      var district = FindSelectedRegionTile();
      var label = _root?.Q<Label>("terraform-district-meta");
      if (district == null || label == null) return;
      label.text = TerraformDistrictMeta(district,
          DistrictScale.Columns(district.Width),
          DistrictScale.Columns(district.Height));
    }

    private static void AddTerraformGrid(VisualElement plane,
        int columns, int rows, int interval, float lineThickness)
    {
      for (var column = 0; column <= columns; column += interval)
      {
        var line = new VisualElement();
        line.AddToClassList("terraform-grid-line");
        line.AddToClassList("terraform-grid-line--vertical");
        line.AddToClassList("terraform-grid-line--major");
        line.userData = column;
        line.style.left = column * DistrictCellPixels;
        line.style.top = 0;
        line.style.width = lineThickness;
        line.style.height = rows * DistrictCellPixels;
        line.pickingMode = PickingMode.Ignore;
        plane.Add(line);
      }

      for (var row = 0; row <= rows; row += interval)
      {
        var line = new VisualElement();
        line.AddToClassList("terraform-grid-line");
        line.AddToClassList("terraform-grid-line--horizontal");
        line.AddToClassList("terraform-grid-line--major");
        line.userData = row;
        line.style.left = 0;
        line.style.top = row * DistrictCellPixels;
        line.style.width = columns * DistrictCellPixels;
        line.style.height = lineThickness;
        line.pickingMode = PickingMode.Ignore;
        plane.Add(line);
      }
    }

    private static void RebuildTerraformGrid(VisualElement plane,
        DistrictZoomLevel level)
    {
      if (plane == null) return;
      for (var index = plane.childCount - 1; index >= 0; index--)
      {
        if (plane[index].ClassListContains("terraform-grid-line"))
          plane.RemoveAt(index);
      }

      if (plane.userData is not Vector2Int dimensions) return;
      if (!DistrictZoom.ShowsGrid(level)) return;
      var interval = DistrictZoom.GridInterval(level);
      var inverseZoom = 1f / Mathf.Max(0.01f, DistrictZoom.Scale(level));
      AddTerraformGrid(plane, dimensions.x, dimensions.y, interval,
          inverseZoom);
    }

    private static void ApplyTerraformGrassLevel(VisualElement plane,
        DistrictZoomLevel level)
    {
      if (plane == null) return;
      for (var index = 0; index <= 5; index++)
        plane.RemoveFromClassList($"district-grass-lod{index}");
      plane.AddToClassList($"district-grass-lod{(int)level}");

      // UI Toolkit flattens the isometric projection after painting the
      // background. At district-scale zooms the full-detail grass is
      // consequently undersampled into horizontal bands. Use prefiltered
      // copies of the same source texture, exactly as a material mip chain
      // would, while retaining the source texture at the lot-scale LOD0.
      if (level >= DistrictZoomLevel.LOD3)
      {
        // Below one screen pixel per metre there is no honest grass
        // detail left to display. A colour field avoids inventing a
        // false micro-grid from undersampling.
        plane.style.backgroundImage = new StyleBackground();
        return;
      }

      var texture = Resources.Load<Texture2D>(
          DistrictWorldController.DistrictGrassResource);
      if (texture != null)
        plane.style.backgroundImage = new StyleBackground(texture);
    }

    private static (string Name, string Glyph)[] TerraformTools(string category) =>
        category switch
        {
          "Select" => new[] { ("Select", "↖"), ("Move", "✥") },
          "Water" => new[] { ("Select Water", "↖"), ("Repair River", "✓"), ("Shape River", "↔"), ("Soften River", "~"), ("Erase River", "⌫") },
          "Flora" => new[] { ("Trees", "♣"), ("Forest", "♠"), ("Clear Flora", "⌫") },
          "Environment" => new[] { ("Rain", "☂"), ("Snow", "❄"), ("Clear Skies", "○") },
          "Sun" => new[] { ("Morning", "◔"), ("Noon", "☀"), ("Afternoon", "◕"), ("Early Evening", "◒"), ("Night", "●") },
          _ => new[] { ("Hills", "Hills"), ("Raise", "▲"), ("Lower", "▼"), ("Level", "▬"), ("Smooth", "~"), ("Erode", "⌁") }
        };

    private static (string Name, string Glyph, string Tip)[]
        DistrictEditorCategories(DistrictEditorMode mode) =>
        mode == DistrictEditorMode.Builder
            ? new[]
            {
                    ("Select", "↖", "Select — draw a rectangle, release to select; use Move to drag the group"),
                    ("Roads", "=", "Roads - streets, avenues, highways, and intersections"),
                    ("Zoning", "#", "Zoning - designate residential, commercial, industrial, and mixed-use land"),
                    ("Parks", "^", "Parks - plazas, playgrounds, gardens, and recreation"),
                    ("Farms", "♧", "Farms - browse and place saved agricultural lots"),
                    ("Transit", "~", "Transit - rail, stations, stops, and district connections"),
                    ("Civic", "+", "Civic - public safety, education, health, and government"),
                    ("Utilities", "*", "Utilities - power, water, waste, and district services")
            }
            : new[]
            {
                    ("Select", "↖", "Select — draw a rectangle, release to select; use Move to drag the group"),
                    ("Terrain", "▲", "Terrain — shape district land"),
                    ("Water", "≈", "Water — lakes, rivers, and shorelines"),
                    ("Flora", "♣", "Flora — trees and natural ground cover"),
                    ("Environment", "☁", "Environment — atmosphere and weather"),
                    ("Sun", "☀", "Sun — choose morning, noon, afternoon, early evening, or night lighting")
            };

    private static bool TryDistrictTimePreset(string tool,
        out TimeOfDayPreset preset)
    {
      preset = tool switch
      {
        "Morning" => TimeOfDayPreset.Morning,
        "Noon" => TimeOfDayPreset.Noon,
        "Afternoon" => TimeOfDayPreset.Afternoon,
        "Early Evening" => TimeOfDayPreset.Evening,
        "Night" => TimeOfDayPreset.Night,
        _ => TimeOfDayPreset.Noon
      };
      return tool is "Morning" or "Noon" or "Afternoon" or "Early Evening" or "Night";
    }

    private static string DistrictTimeToolName(TimeOfDayPreset preset) =>
        preset switch
        {
          TimeOfDayPreset.Morning => "Morning",
          TimeOfDayPreset.Noon => "Noon",
          TimeOfDayPreset.Afternoon => "Afternoon",
          TimeOfDayPreset.Evening => "Early Evening",
          TimeOfDayPreset.Night => "Night",
          _ => "Afternoon"
        };

    private static (string Name, string Glyph)[] BuilderTools(string category) =>
        category switch
        {
          "Select" => new[] { ("Select", "↖"), ("Move", "✥") },
          "Zoning" => new[] { ("Residential", "R"), ("Commercial", "C"), ("Industrial", "I"), ("Mixed Use", "M"), ("Dezone", "X"),
              ("Browse Residential", "▦"), ("Browse Commercial", "▦"), ("Browse Industrial", "▦"), ("Browse Mixed Use", "▦") },
          "Parks" => new[] { ("Browse Parks", "▦"), ("Pocket Park", "P"), ("Plaza", "Q"), ("Playground", "G"), ("Sports", "O") },
          "Farms" => new[] { ("Browse Farms", "▦") },
          "Transit" => new[] { ("Browse Transit Lots", "▦"), ("Rail", "R"), ("Station", "S"), ("Bus Stop", "B"), ("Connector", "C") },
          "Civic" => new[] { ("Browse Civic Lots", "▦"), ("Government", "G"), ("Education", "E"), ("Health", "+"), ("Safety", "S") },
          "Utilities" => new[] { ("Power", "P"), ("Water", "W"), ("Waste", "X"), ("Service Lines", "L") },
          _ => new[] { (DistrictRoadPlacementModel.DirtFamily, "="),
              ("Delete Road", "X") }
        };

    private static bool TryBuilderLotBrowserType(string category, string tool,
        out LotType lotType)
    {
      lotType = (category, tool) switch
      {
        ("Zoning", "Browse Residential") => LotType.Residential,
        ("Zoning", "Browse Commercial") => LotType.Commercial,
        ("Zoning", "Browse Industrial") => LotType.Industrial,
        ("Zoning", "Browse Mixed Use") => LotType.Mixed,
        ("Parks", "Browse Parks") => LotType.CivicsParks,
        ("Farms", "Browse Farms") => LotType.Agricultural,
        ("Transit", "Browse Transit Lots") => LotType.Transportation,
        ("Civic", "Browse Civic Lots") => LotType.Civics,
        _ => (LotType)(-1)
      };
      return (int)lotType >= 0;
    }

    private string ActiveDistrictCategory =>
        _districtEditorMode == DistrictEditorMode.Builder
            ? _builderCategory
            : _terraformCategory;

    private string ActiveDistrictTool =>
        _districtEditorMode == DistrictEditorMode.Builder
            ? _builderTool
            : _terraformTool;

    private (string Name, string Glyph)[] ActiveDistrictTools() =>
        _districtEditorMode == DistrictEditorMode.Builder
            ? BuilderTools(_builderCategory)
            : TerraformTools(_terraformCategory);

    private void SelectDistrictCategory(string category)
    {
      SetDistrictRoadDeleteCursor(false);
      CancelLaborPlacement();
      if (category == "Select")
      {
        CancelDistrictSelectionPointer();
        _districtFloraPointerDown = false;
        _districtRoadPointerDown = false;
        _districtSelectionDragActive = false;
        _selectedDistrictFloraInstanceId = "";
        _selectedDistrictLotInstanceId = "";
        _hoveredDistrictLotInstanceId = "";
        _hasSelectedDistrictRoad = false;
        _districtWorld?.SelectDistrictFlora("");
        _districtWorld?.HideLotOutline();
        _pendingFounderBuildingId = "";
        _pendingDistrictLotId = "";
        _pendingDistrictLotIsTest = false;
        _pendingDistrictLotName = "";
        _pendingDistrictFloraId = "";
        _pendingDistrictFloraMode = 0;
        _districtWorld?.HideLotPlacementGuide();
      }
      if (_districtEditorMode == DistrictEditorMode.Builder)
      {
        _builderCategory = category;
        _builderTool = BuilderTools(category)[0].Name;
        return;
      }
      _terraformCategory = category;
      _terraformTool = TerraformTools(category)[0].Name;
      if (category == "Water")
      {
        _districtSelection.Clear();
        _districtWorld?.ShowDistrictSelection(
            FindSelectedRegionTile(), _districtSelection);
      }
    }

    private void SelectDistrictTool(string tool)
    {
      if (_districtEditorMode == DistrictEditorMode.Builder)
        _builderTool = tool;
      else
      {
        _terraformTool = tool;
        if (_terraformCategory == "Environment")
        {
          if (tool == "Rain") _districtWorld?.StartRainStorm();
          else if (tool == "Snow") _districtWorld?.StartSnowStorm();
          else if (tool == "Clear Skies") _districtWorld?.ClearRainStorm();
        }
      }
    }

    private bool IsDistrictRoadToolActive() =>
        _districtEditorMode == DistrictEditorMode.Builder &&
        _builderCategory == "Roads" &&
        (_builderTool == DistrictRoadPlacementModel.DirtFamily ||
         _builderTool == DistrictRoadPlacementModel.AntiqueBrickFamily ||
         _builderTool == DistrictRoadPlacementModel.PikeDirtFamily);

    private bool IsDistrictRoadDeleteToolActive() =>
        _districtEditorMode == DistrictEditorMode.Builder &&
        _builderCategory == "Roads" && _builderTool == "Delete Road";

    private static readonly (string Id, string Name)[] DistrictTrees =
    {
            ("american-elm", "American Elm"),
            ("cilician-fir", "Cilician Fir"),
            ("medium-balsam-fir", "Medium Balsam Fir"),
            ("medium-fraser-fir", "Medium Fraser Fir"),
            ("medium-blue-spruce", "Medium Blue Spruce"),
            ("date-palm-tall", "Tall Date Palm"),
            ("date-palm-short", "Short Date Palm"),
            ("la-fan-palm-a", "LA Fan Palm A"),
            ("la-fan-palm-b", "LA Fan Palm B"),
            ("la-fan-palm-a-medium", "LA Fan Palm A (Medium)"),
            ("la-fan-palm-b-medium", "LA Fan Palm B (Medium)"),
            ("london-plane-a", "London Plane A"),
            ("london-plane-b", "London Plane B"),
            ("mature-oak", "Mature Oak"),
            ("shagbark-hickory", "Shagbark Hickory"),
            ("bald-cypress-moss", "Bald Cypress with Spanish Moss"),
            ("bald-cypress-moss-b", "Bald Cypress with Spanish Moss B"),
            ("vendor-red-maple", "Red Maple"),
            ("silver-maple-a", "Silver Maple A"),
            ("vendor-willow", "Willow"),
        };

    private static bool DistrictFloraCanOccupyWater(string floraId) =>
        !string.IsNullOrWhiteSpace(floraId) &&
        StoneFloraCatalog.IsSubmergible(floraId);

    private bool _districtStoneLibrary;
    private string _lastDistrictStoneId = StoneFloraCatalog.Families[0].Id;
    private string _floraTreeFamily = FloraFamilies.Deciduous;
    private string _pendingDistrictTreeFamily = FloraFamilies.Deciduous;
    private void AddTreeFamilyTabs(VisualElement panel, System.Action refresh,
        System.Action familySelected = null, System.Func<string, bool> familyAvailable = null)
    {
      var tabs = DocumentModalActions();
      foreach (var family in FloraFamilies.Names)
      {
        var captured = family;
        var button = CfButton.Create(family.ToUpperInvariant(), () =>
        {
          _floraTreeFamily = captured;
          familySelected?.Invoke();
          RemoveDocumentModal();
          refresh();
        }, familyAvailable?.Invoke(family) ?? true, family == _floraTreeFamily ? "mode-selected" : "quiet");
        button.name = "flora-family-" + family.ToLowerInvariant().Replace(" ", "-");
        tabs.Add(button);
      }
      panel.Add(tabs);
    }
    private RegionClimate CurrentRegionClimate => _openRegion?.Terrain?.Climate ?? RegionClimate.Temperate;
    private bool DistrictTreeFamilyAvailable(string family) => DistrictTrees.Any(tree =>
        FloraFamilies.ForTree(tree.Id) == family && RegionClimateRules.AllowsTree(CurrentRegionClimate, tree.Id));
    private void ComposeDistrictFloraModal()
    {
      if (!DistrictTreeFamilyAvailable(_floraTreeFamily))
        _floraTreeFamily = FloraFamilies.Names.First(DistrictTreeFamilyAvailable);
      var panel = CreateDocumentModal("DISTRICT FLORA",
          "Choose a family, then click or drag to plant. Release to finish a stroke. Tab rerolls the latest stroke within its family. Individual trees and stones can also be placed.");
      panel.AddToClassList("road-material-modal-panel");
      panel.AddToClassList("flora-modal-panel");
      AddDistrictFloraTabs(panel, _districtStoneLibrary ? "STONES" : "TREES");
      if (!_districtStoneLibrary) AddTreeFamilyTabs(panel, ComposeDistrictFloraModal,
          () => SelectDistrictFloraLibrary(false), DistrictTreeFamilyAvailable);
      var random = DocumentModalActions();
      random.Add(CfButton.Create("PAINT FAMILY GROUPS", () =>
      {
        ArmDistrictFloraPlacement("", 2);
      }, true, _pendingDistrictFloraMode == 2 ? "primary" : "quiet"));
      random.Add(CfButton.Create("PAINT SINGLE TREES", () =>
      {
        ArmDistrictFloraPlacement("", 1);
      }, true, _pendingDistrictFloraMode == 1 ? "primary" : "quiet"));
      if (!_districtStoneLibrary) panel.Add(random);

      var scroll = new ScrollView(ScrollViewMode.Vertical);
      scroll.AddToClassList("flora-modal-scroll");
      scroll.Add(StyledLabel(_districtStoneLibrary ? "STONES" : _floraTreeFamily.ToUpperInvariant() + " · A–Z", "road-material-role"));
      var grid = new VisualElement();
      grid.AddToClassList("road-material-grid");
      grid.style.flexShrink = 0f;
      if (!_districtStoneLibrary) foreach (var tree in DistrictTrees.Where(tree => FloraFamilies.ForTree(tree.Id) == _floraTreeFamily && RegionClimateRules.AllowsTree(CurrentRegionClimate, tree.Id)).OrderBy(tree => tree.Name,
                   StringComparer.OrdinalIgnoreCase))
      {
        var captured = tree;
        var card = new VisualElement();
        card.AddToClassList("road-material-card");
        var preview = new VisualElement();
        preview.AddToClassList("road-material-swatch");
        preview.style.backgroundImage = new StyleBackground(
            Resources.Load<Texture2D>(
                LotWorldController.ResolveFloraResourcePath(
                    captured.Id, SeasonPreset.Summer)));
        card.Add(preview);
        card.Add(CfButton.Create(captured.Name.ToUpperInvariant(), () =>
        {
          ArmDistrictFloraPlacement(captured.Id, 0);
        }, true, _pendingDistrictFloraMode == 0 &&
                 _pendingDistrictFloraId == captured.Id
            ? "mode-selected" : "quiet"));
        grid.Add(card);
      }
      if (_districtStoneLibrary)
        foreach (var stone in StoneFloraCatalog.Families)
          AddStoneLibraryCard(grid, stone.Id, stone.Name,
              () => ArmDistrictFloraPlacement(stone.Id, 0));
      scroll.Add(grid);
      panel.Add(scroll);
      var actions = DocumentModalActions();
      actions.Add(CfButton.Create("DONE", RemoveDocumentModal,
          true, "quiet"));
      panel.Add(actions);
    }

    private void SelectDistrictFloraLibrary(bool stones)
    {
      _districtStoneLibrary = stones;
      // Browsing a family also selects what the next ground click will place.
      ArmDistrictFloraPlacement(stones ? _lastDistrictStoneId : "",
          stones ? 0 : (_pendingDistrictFloraMode == 1 ? 1 : 2), false);
    }

    private void ArmDistrictFloraPlacement(string floraId, int mode,
        bool closeModal = true)
    {
      FinishDistrictFloraPaint(FindSelectedRegionTile());
      if (StoneFloraCatalog.IsStone(floraId)) _lastDistrictStoneId = floraId;
      // Arming must select the world tool as well as the family.
      _districtEditorMode = DistrictEditorMode.Terraform;
      _terraformCategory = "Flora";
      _terraformTool = "Trees";
      _districtFloraPainting = false;
      _pendingDistrictTreeFamily = _floraTreeFamily;
      _pendingDistrictFloraId = floraId ?? "";
      _pendingDistrictFloraMode = mode;
      _activeDistrictRandomFloraGroupId = "";
      _selectedDistrictFloraInstanceId = "";
      _districtFloraPointerDown = false;
      if (closeModal)
      {
        RemoveDocumentModal();
        Show(AppScreen.DistrictTerraform);
      }
    }

    private void PlaceDistrictFlora(RegionCityTile district,
        Vector2 normalized, bool flush = true, string strokeGroup = null)
    {
      district.Flora ??= new List<PlacedDistrictFlora>();
      var group = strokeGroup ?? Guid.NewGuid().ToString("N");
      var count = _pendingDistrictFloraMode == 2
          ? UnityEngine.Random.Range(9, 15) : 1;
      var familyRotation = UnityEngine.Random.Range(0, 8) * 45f;
      var groupHasHarvestableTree = false;
      PlacedDistrictFlora last = null;
      for (var i = 0; i < count; i++)
      {
        var id = string.IsNullOrWhiteSpace(_pendingDistrictFloraId)
            ? (_pendingDistrictFloraMode == 2
                ? RandomDistrictForestTreeId(
                    _pendingDistrictTreeFamily,
                    groupHasHarvestableTree)
                : RandomDistrictTreeId(_pendingDistrictTreeFamily, ""))
            : _pendingDistrictFloraId;
        if (!StoneFloraCatalog.IsStone(id) && !RegionClimateRules.AllowsTree(CurrentRegionClimate, id)) continue;
        var candidate = normalized;
        var foundClearGround = false;
        for (var attempt = 0; attempt < 32; attempt++)
        {
          var offsetMeters = _pendingDistrictFloraMode == 2
              ? UnityEngine.Random.insideUnitCircle * 20f
              : Vector2.zero;
          if (_pendingDistrictFloraMode == 2)
            offsetMeters = Quaternion.Euler(0f, 0f,
                familyRotation) * offsetMeters;
          candidate = new Vector2(
              Mathf.Clamp01(normalized.x + offsetMeters.x /
                  Mathf.Max(1f, DistrictScale.SizeMeters(district.Width))),
              Mathf.Clamp01(normalized.y + offsetMeters.y /
                  Mathf.Max(1f, DistrictScale.SizeMeters(district.Height))));
          if (DistrictRoadPlacementModel.IsFloraPositionClear(
                  district, candidate) &&
              (DistrictFloraCanOccupyWater(id) ||
               !_districtWorld.IsUnderRiverWater(candidate)))
          {
            foundClearGround = true;
            break;
          }
          if (_pendingDistrictFloraMode != 2) break;
        }
        if (!foundClearGround) continue;
        last = new PlacedDistrictFlora
        {
          InstanceId = Guid.NewGuid().ToString("N"),
          GroupId = group,
          FloraId = id,
          NormalizedX = candidate.x,
          NormalizedZ = candidate.y,
          Scale = _pendingDistrictFloraMode == 0 ? 1f :
                UnityEngine.Random.Range(.86f, 1.16f),
          RotationEighthTurns = UnityEngine.Random.Range(0, 8)
        };
        district.Flora.Add(last);
        if (last.FloraId == "cilician-fir")
          groupHasHarvestableTree = true;
        DistrictHarvestIndex.Changed(district,last);
        _pendingDistrictFloraPresentations.Add(last);
      }
      if (last == null)
      {
        _lotStatus =
            "This flora needs clear terrain away from roads and water";
        return;
      }
      _activeDistrictRandomFloraGroupId =
          _pendingDistrictFloraMode == 0 ? "" : group;
      _selectedDistrictFloraInstanceId = last?.InstanceId ?? "";
      if (flush) FlushDistrictFloraPaint(district, true);
    }

    private bool _districtFloraPainting;
    private Vector2 _districtFloraPaintAnchor;
    private string _districtFloraStrokeGroup;
    private void BeginDistrictFloraPaint(RegionCityTile district, Vector2 point)
    {
      if (_pendingDistrictFloraMode == 0) { PlaceDistrictFlora(district, point); return; }
      _districtFloraPainting = true;
      _districtFloraPaintAnchor = point;
      _districtFloraStrokeGroup = Guid.NewGuid().ToString("N");
      PlaceDistrictFlora(district, point, false, _districtFloraStrokeGroup);
      FlushDistrictFloraPaint(district, false);
    }
    private void ContinueDistrictFloraPaint(RegionCityTile district, Vector2 point)
    {
      var size = new Vector2(DistrictScale.SizeMeters(district.Width), DistrictScale.SizeMeters(district.Height));
      var delta = Vector2.Scale(point - _districtFloraPaintAnchor, size);
      var distance = delta.magnitude;
      var spacing = _pendingDistrictFloraMode == 2 ? 24f : 5f;
      var steps = Mathf.Min(8, Mathf.FloorToInt(distance / spacing));
      if (steps == 0) return;
      var increment = (point - _districtFloraPaintAnchor) * (spacing / distance);
      for (var i = 0; i < steps; i++)
      {
        _districtFloraPaintAnchor += increment;
        PlaceDistrictFlora(district, _districtFloraPaintAnchor, false, _districtFloraStrokeGroup);
      }
      FlushDistrictFloraPaint(district, false);
    }
    private void FinishDistrictFloraPaint(RegionCityTile district)
    {
      if (!_districtFloraPainting) return;
      _districtFloraPainting = false;
      if (district != null) FlushDistrictFloraPaint(district, true);
    }
    private void FlushDistrictFloraPaint(RegionCityTile district, bool save)
    {
      if (_pendingDistrictFloraPresentations.Count > 0)
      {
        _districtWorld.AddDistrictFloraPresentations(
            _pendingDistrictFloraPresentations,
            _selectedDistrictFloraInstanceId);
        _pendingDistrictFloraPresentations.Clear();
      }
      if (!save) return;
      _districtWorldCompositionKey = DistrictCompositionKey(district);
      SaveDistrictEdit();
    }

    private bool RerollDistrictRandomFlora()
    {
      var district = FindSelectedRegionTile();
      if (district == null || string.IsNullOrWhiteSpace(
              _activeDistrictRandomFloraGroupId)) return false;
      var changed = new List<PlacedDistrictFlora>();
      foreach (var tree in district.Flora ??
               new List<PlacedDistrictFlora>())
      {
        if (tree?.GroupId != _activeDistrictRandomFloraGroupId) continue;
        tree.FloraId = RandomDistrictTreeId(FloraFamilies.ForTree(tree.FloraId), tree.FloraId);
        DistrictHarvestIndex.Changed(district,tree);
        tree.Scale = UnityEngine.Random.Range(.86f, 1.16f);
        tree.RotationEighthTurns = UnityEngine.Random.Range(0, 8);
        changed.Add(tree);
      }
      if (changed.Count == 0) return false;
      _districtWorld?.RemoveFloraPresentations(
          changed.Select(tree => tree.InstanceId));
      _districtWorld?.AddDistrictFloraPresentations(changed,
          _selectedDistrictFloraInstanceId);
      _districtWorldCompositionKey = DistrictCompositionKey(district);
      SaveDistrictEdit();
      return true;
    }

    private string RandomDistrictTreeId(string family, string excluding)
    {
      var choices = DistrictTrees.Where(tree => FloraFamilies.ForTree(tree.Id) == family && tree.Id != excluding && RegionClimateRules.AllowsTree(CurrentRegionClimate, tree.Id)).ToArray();
      if (choices.Length == 0) return DistrictTrees.First(tree => RegionClimateRules.AllowsTree(CurrentRegionClimate, tree.Id)).Id;
      return choices[UnityEngine.Random.Range(0, choices.Length)].Id;
    }

    private string RandomDistrictForestTreeId(string family,
        bool groupHasHarvestableTree)
    {
      var harvestable = DistrictForestHarvestableTree(
          CurrentRegionClimate, family, groupHasHarvestableTree,
          UnityEngine.Random.Range(0, 5));
      if (!string.IsNullOrEmpty(harvestable)) return harvestable;
      return RandomDistrictTreeId(family, "cilician-fir");
    }

    public static string DistrictForestHarvestableTree(
        RegionClimate climate, string family,
        bool groupHasHarvestableTree, int roll) =>
      RegionClimateRules.AllowsTree(climate, "cilician-fir") &&
      ((family == FloraFamilies.Mountain && !groupHasHarvestableTree) ||
       Mathf.Abs(roll) % 5 == 0) ? "cilician-fir" : "";

    private static PlacedDistrictFlora FindDistrictFlora(
        RegionCityTile district, string instanceId) =>
        district?.Flora?.FirstOrDefault(tree => tree != null &&
            tree.InstanceId == instanceId);

    private Vector2 DistrictCameraPoint(Vector2 panelPosition)
    {
      var panelWidth = _root?.resolvedStyle.width ?? 0f;
      var panelHeight = _root?.resolvedStyle.height ?? 0f;
      if (panelWidth <= 0f || panelHeight <= 0f)
        return panelPosition;
      return new Vector2(
          panelPosition.x * UnityEngine.Screen.width / panelWidth,
          panelPosition.y * UnityEngine.Screen.height / panelHeight);
    }

    private bool DistrictSelectToolActive() =>
        ActiveDistrictCategory == "Select" &&
        ActiveDistrictTool == "Select";

    private bool DistrictMoveToolActive() =>
        ActiveDistrictCategory == "Select" && ActiveDistrictTool == "Move";

    private bool DistrictWaterSelectToolActive() =>
        _districtEditorMode == DistrictEditorMode.Terraform &&
        _terraformCategory == "Water" &&
        _terraformTool == "Select Water";

    private void SelectDistrictRiverAt(RegionCityTile district,
        Vector2 panelPosition)
    {
      var riverId = _districtWorld?.FindDistrictRiverAtPanel(
          district, DistrictCameraPoint(panelPosition)) ?? "";
      if (string.IsNullOrWhiteSpace(riverId) &&
          district?.Rivers?.Count == 1)
        riverId = district.Rivers[0]?.InstanceId ?? "";
      var river = district?.Rivers?.Find(item => item != null &&
          item.InstanceId == riverId);
      _districtSelection.Clear();
      if (river != null)
        _districtSelection.Add(new DistrictSelectionRef(
            DistrictSelectionKind.River, river.InstanceId));
      _districtWorld?.ShowDistrictSelection(district,
          _districtSelection);
    }

    private void SelectSoleDistrictRiverIfNeeded(RegionCityTile district)
    {
      if (!DistrictWaterSelectToolActive() ||
          _districtSelection.Any(item =>
              item.Kind == DistrictSelectionKind.River) ||
          district?.Rivers?.Count != 1 || district.Rivers[0] == null)
        return;
      _districtSelection.Clear();
      _districtSelection.Add(new DistrictSelectionRef(
          DistrictSelectionKind.River,
          district.Rivers[0].InstanceId));
    }

    private bool MoveSelectedDistrictRiver(int horizontalWorldMotion,
        int verticalWorldMotion)
    {
      if (!DistrictWaterSelectToolActive()) return false;
      SelectSoleDistrictRiverIfNeeded(FindSelectedRegionTile());
      var selected = _districtSelection.FirstOrDefault(item =>
          item.Kind == DistrictSelectionKind.River);
      if (selected.Kind != DistrictSelectionKind.River ||
          string.IsNullOrWhiteSpace(selected.Id)) return false;
      var district = FindSelectedRegionTile();
      var river = district?.Rivers?.Find(item => item != null &&
          item.InstanceId == selected.Id);
      if (river == null) return false;
      var delta = new Vector2(
          -horizontalWorldMotion / (float)DistrictScale.Columns(district.Width),
          -verticalWorldMotion / (float)DistrictScale.Columns(district.Height));
      if (!DistrictRiverEditing.Move(river, delta)) return true;
      SaveDistrictEdit();
      _districtWorld?.RebuildAllRiverPresentations(district,
          DistrictBulkRebuildReason.RiverGeometryReplacement);
      _districtWorldCompositionKey = DistrictCompositionKey(district);
      _districtWorld?.ShowDistrictSelection(district,
          _districtSelection);
      return true;
    }

    private bool DistrictFloraPlacementArmed() =>
        _districtEditorMode == DistrictEditorMode.Terraform &&
        _terraformCategory == "Flora" && _terraformTool == "Trees" &&
        (_pendingDistrictFloraMode != 0 ||
         !string.IsNullOrWhiteSpace(_pendingDistrictFloraId));

    private void BeginDistrictSelectionPointer(RegionCityTile district,
        Vector2 panelPosition, VisualElement surface, int pointerId)
    {
      if (_districtWorld == null || _districtMarqueeActive) return;
      _districtSelectionScreenStart = _districtSelectionScreenLast = panelPosition;
      _districtSelectionPointerId = pointerId;
      _districtSelectionSurface = surface;
      _districtSelectionDragActive = false;
      _districtFloraPointerDown = false;
      _districtRoadPointerDown = false;
      _districtEdgePanDirection = Vector2Int.zero;
      _districtMarqueeActive = true;
      _districtWorld.HideLotOutline();
      _districtWorld.HideLotPlacementGuide();
      _districtWorld.SelectDistrictFlora("");
      // Retain the prior set for Escape/capture-loss cancellation, but
      // suppress its highlighting and all selection actions during drag.
      _districtWorld.ShowDistrictSelection(district, Array.Empty<DistrictSelectionRef>());
      surface.CapturePointer(pointerId);
      UpdateDistrictSelectionMarquee(panelPosition);
    }

    private bool BeginDistrictMovePointer(RegionCityTile district,
        Vector2 normalized, Vector2 panelPosition)
    {
      if (_districtSelection.Any(item => item.Kind == DistrictSelectionKind.Entity && _districtWorld?.ResolveSelectable(item)?.DeleteBuilding == null))
      {
        ShowDistrictNotice("This object's placement is fixed. Select it to see its supported actions.");
        return false;
      }
      var pixel = DistrictCameraPoint(panelPosition);
      var hits = _districtWorld.CollectDistrictSelectionInScreenRect(district,
          new Rect(pixel.x - 4f, pixel.y - 4f, 8f, 8f));
      if (!_districtSelection.Any(selected => hits.Any(hit =>
              hit.Kind == selected.Kind && hit.Id == selected.Id))) return false;
      _districtSelectionStart = _districtSelectionLast = normalized;
      _districtSelectionGridRemainder = Vector2.zero;
      _districtSelectionMovedRiver = false;
      _districtSelectionMovedRoad = false;
      _districtSelectionMovedLot = false;
      _districtSelectionDragActive = true;
      return true;
    }

    private void UpdateDistrictSelectionMarquee(Vector2 panelPosition)
    {
      _districtSelectionScreenLast = panelPosition;
      if (_districtSelectionMarquee == null) return;
      var parent = _districtSelectionMarquee.parent;
      var start = parent.WorldToLocal(_districtSelectionScreenStart);
      var end = parent.WorldToLocal(panelPosition);
      var rect = DistrictSelectionGeometry.Rectangle(start, end);
      _districtSelectionMarquee.style.left = rect.xMin;
      _districtSelectionMarquee.style.top = rect.yMin;
      _districtSelectionMarquee.style.width = rect.width;
      _districtSelectionMarquee.style.height = rect.height;
      _districtSelectionMarquee.style.display = DisplayStyle.Flex;
    }

    private void CancelDistrictSelectionPointer()
    {
      if (!_districtMarqueeActive) return;
      var surface = _districtSelectionSurface;
      var pointerId = _districtSelectionPointerId;
      _districtMarqueeActive = false;
      _districtSelectionPointerId = -1;
      _districtSelectionSurface = null;
      if (_districtSelectionMarquee != null)
        _districtSelectionMarquee.style.display = DisplayStyle.None;
      if (surface != null && surface.HasPointerCapture(pointerId))
        surface.ReleasePointer(pointerId);
      _districtWorld?.ShowDistrictSelection(FindSelectedRegionTile(), _districtSelection);
    }

    private void CompleteDistrictSelectionPointer(RegionCityTile district)
    {
      if (_districtMarqueeActive)
      {
        var start = DistrictCameraPoint(_districtSelectionScreenStart);
        var end = DistrictCameraPoint(_districtSelectionScreenLast);
        var rect = DistrictSelectionGeometry.Rectangle(start, end);
        // A click is also committed on release, with a fixed pixel
        // tolerance instead of a district-size-dependent world radius.
        if (Vector2.Distance(_districtSelectionScreenStart,
                _districtSelectionScreenLast) < 4f)
          rect = new Rect(end.x - 4f, end.y - 4f, 8f, 8f);
        if (Vector2.Distance(_districtSelectionScreenStart, _districtSelectionScreenLast) < 4f)
        {
          var target = _districtWorld.FindSelectableAtScreenPoint(end);
          if (target != null)
          {
            _districtSelection.Clear();
            _districtSelection.Add(target.Identity);
            CancelDistrictSelectionPointer();
            if (target.ShowInspector) ComposeSelectedObject(target.Identity);
            return;
          }
        }
        var selected = _districtWorld.CollectDistrictSelectionInScreenRect(district, rect);
        _districtSelection.Clear();
        _districtSelection.AddRange(selected);
        CancelDistrictSelectionPointer();
        return;
      }
      if (_districtSelectionDragActive)
      {
        if (_districtSelectionMovedRiver)
          _districtWorld?.RebuildAllRiverPresentations(district,
              DistrictBulkRebuildReason.RiverGeometryReplacement);
        if (_districtSelectionMovedRoad)
        {
          DistrictRoadPlacementModel.Repair(district.Roads);
          _districtWorld?.RefreshRoads(district, deferSurfaceRefresh: true);
        }
        if (!_districtSelectionMovedRiver &&
            (_districtSelectionMovedRoad || _districtSelectionMovedLot))
          _districtWorld?.CommitLocalSurfaceChanges();
        _districtSelectionMovedRiver = false;
        _districtSelectionMovedRoad = false;
        _districtSelectionMovedLot = false;
        _districtWorldCompositionKey = DistrictCompositionKey(district);
        SaveDistrictEdit();
        _districtWorld?.ShowDistrictSelection(district, _districtSelection);
      }
      _districtSelectionDragActive = false;
    }

    private void MoveDistrictSelection(RegionCityTile district,
        Vector2 normalized)
    {
      var delta = normalized - _districtSelectionLast;
      if (delta.sqrMagnitude <= 0f) return;
      _districtSelectionLast = normalized;
      var columns = DistrictScale.Columns(district.Width);
      var rows = DistrictScale.Columns(district.Height);
      var movedFlora = new List<PlacedDistrictFlora>();
      foreach (var selection in _districtSelection)
      {
        if (selection.Kind == DistrictSelectionKind.Flora)
        {
          var flora = FindDistrictFlora(district, selection.Id);
          if (flora == null) continue;
          flora.NormalizedX = Mathf.Clamp01(flora.NormalizedX + delta.x);
          flora.NormalizedZ = Mathf.Clamp01(flora.NormalizedZ + delta.y);
          DistrictHarvestIndex.Changed(district,flora);
          movedFlora.Add(flora);
        }
        else if (selection.Kind == DistrictSelectionKind.River)
        {
          var river = district.Rivers?.Find(item => item != null && item.InstanceId == selection.Id);
          _districtSelectionMovedRiver |= DistrictRiverEditing.Move(river, delta);
        }
      }
      _districtSelectionGridRemainder += new Vector2(
          delta.x * columns, delta.y * rows);
      var gridDelta = new Vector2Int(
          Mathf.Abs(_districtSelectionGridRemainder.x) >= 1f
              ? (int)_districtSelectionGridRemainder.x : 0,
          Mathf.Abs(_districtSelectionGridRemainder.y) >= 1f
              ? (int)_districtSelectionGridRemainder.y : 0);
      if (gridDelta != Vector2Int.zero)
      {
        _districtSelectionGridRemainder -= gridDelta;
        foreach (var selection in _districtSelection)
        {
          if (selection.Kind == DistrictSelectionKind.Lot)
          {
            var lot = district.Lots?.Find(item => item != null &&
                item.InstanceId == selection.Id);
            if (lot == null || !TryGetDistrictLotFootprint(lot,
                    out _, out var spanX, out var spanZ)) continue;
            var targetX = Mathf.Clamp(lot.GridX + gridDelta.x, 0, columns - spanX);
            var targetZ = Mathf.Clamp(lot.GridZ + gridDelta.y, 0, rows - spanZ);
            if (_districtWorld != null && !_districtWorld.ValidateLotBoatPlacement(district,
                new PlacedDistrictLot
                {
                  LotId = lot.LotId,
                  GridX = targetX,
                  GridZ = targetZ,
                  RotationQuarterTurns = lot.RotationQuarterTurns,
                  ShoreOffsetX = lot.ShoreOffsetX,
                  ShoreOffsetZ = lot.ShoreOffsetZ
                }, LotContentCatalog.Read(lot.LotId), out _)) continue;
            lot.GridX = targetX; lot.GridZ = targetZ;
            _districtWorld?.UpdatePlacedLotTransform(district, lot, deferSurfaceRefresh: true);
            _districtSelectionMovedLot = true;
          }
          else if (selection.Kind == DistrictSelectionKind.Road)
          {
            var road = district.Roads?.Find(item => item != null &&
                item.Id == selection.Id);
            if (road == null) continue;
            road.GridX = Mathf.Clamp(road.GridX + gridDelta.x,
                0, columns - 1);
            road.GridZ = Mathf.Clamp(road.GridZ + gridDelta.y,
                0, rows - 1);
            _districtSelectionMovedRoad = true;
          }
        }
        _districtRoadEditSession = null;
        InvalidateDistrictRoadLotCells();
        if (_districtSelectionMovedRoad)
          _districtWorld?.RefreshRoads(district, deferSurfaceRefresh: true);
      }
      if (movedFlora.Count > 0)
        _districtWorld?.MoveDistrictFloraPresentations(movedFlora,
            _selectedDistrictFloraInstanceId);
      _districtWorld?.ShowDistrictSelection(district,
          _districtSelection);
    }

    private int _districtDeleteFrame = -1;
    private bool DeleteDistrictSelection()
    {
      if (_lotNudge != null) return false;
      // UI Toolkit and input polling can report the same physical key.
      if (_districtDeleteFrame == Time.frameCount) return true;
      if (_districtMarqueeActive) return false;
      var district = FindSelectedRegionTile();
      if (district == null) return false;
      // Single-click lot selection also has a dedicated inspector ID.
      // Resolve it before the water tool's implicit single-river selection.
      if (_districtSelection.Count == 0 &&
          district.Lots != null && district.Lots.Exists(item =>
              item != null && item.InstanceId == _selectedDistrictLotInstanceId))
        _districtSelection.Add(new DistrictSelectionRef(
            DistrictSelectionKind.Lot, _selectedDistrictLotInstanceId));
      SelectSoleDistrictRiverIfNeeded(district);
      if (_districtSelection.Count == 0) return false;
      if (_districtSelection.Any(selection => selection.Kind == DistrictSelectionKind.River &&
          district.Rivers.Any(river => river.InstanceId == selection.Id && !string.IsNullOrEmpty(river.RegionRiverId))))
      {
        var panel = CreateDocumentModal("REGION RIVER", "This river continues through neighboring districts. Change it from Region → Terrain → Rivers.");
        panel.Add(CfButton.Create("CLOSE", RemoveDocumentModal, true, "quiet"));
        return false;
      }
      if (_districtSelection.Any(item => item.Kind == DistrictSelectionKind.Entity && _districtWorld?.ResolveSelectable(item)?.DeleteBuilding == null))
      {
        ShowDistrictNotice("Use this object's management controls to remove it.");
        return false;
      }
      var deletedBuildings = _districtSelection.Where(item => item.Kind == DistrictSelectionKind.Lot || item.Kind == DistrictSelectionKind.Entity)
          .Select(item => _districtWorld.ResolveSelectable(item)).ToList();
      var deletedFlora = _districtSelection.Where(item => item.Kind == DistrictSelectionKind.Flora).Select(item => item.Id).ToList();
      var deletedRoadIds = new HashSet<string>(_districtSelection.Where(item => item.Kind == DistrictSelectionKind.Road).Select(item => item.Id));
      var deletedRoadCells = district.Roads.Where(road => deletedRoadIds.Contains(road.Id)).Select(road => new Vector2Int(road.GridX, road.GridZ)).ToList();
      EnsureDistrictUndo(district);
      _districtDeleteFrame = Time.frameCount;
      var removedRiver = false;
      foreach (var selection in _districtSelection)
      {
        district.LotNudges?.RemoveAll(n => n.Kind == selection.Kind && n.Id == selection.Id);
        switch (selection.Kind)
        {
          case DistrictSelectionKind.Entity:
            _districtWorld.ResolveSelectable(selection).DeleteBuilding();
            break;
          case DistrictSelectionKind.Flora:
            DistrictHarvestIndex.For(district).RemoveFlora(selection.Id);
            break;
          case DistrictSelectionKind.Lot:
            DistrictLotSimulation.For(district).Remove(selection.Id);
            DistrictTimber.RemoveLotCrews(district, selection.Id);
            district.Lots?.RemoveAll(item => item != null &&
                item.InstanceId == selection.Id);
            break;
          case DistrictSelectionKind.Road:
            district.Roads?.RemoveAll(item => item != null &&
                item.Id == selection.Id);
            break;
          case DistrictSelectionKind.River:
            district.Rivers?.RemoveAll(item => item != null &&
                item.InstanceId == selection.Id);
            removedRiver = true;
            break;
        }
      }
      if (deletedRoadCells.Count > 0) DistrictRoadPlacementModel.Repair(district.Roads);
      _districtRoadEditSession = null;
      InvalidateDistrictRoadLotCells();
      _districtSelection.Clear();
      _selectedDistrictLotInstanceId = "";
      _hoveredDistrictLotInstanceId = "";
      SaveDistrictEdit();
      foreach (var target in deletedBuildings) _districtWorld.RemoveBuildingPresentation(target);
      _districtWorld.RemoveFloraPresentations(deletedFlora);
      _districtWorld.RefreshRoadCellsAndNeighbors(district, deletedRoadCells);
      if (removedRiver) _districtWorld.RebuildAllRiverPresentations(district,
          DistrictBulkRebuildReason.RiverGeometryReplacement,
          preservePresentations: true);
      else if (deletedBuildings.Count>0) _districtWorld.CommitLocalSurfaceChanges();
      else if (deletedRoadCells.Count>0) _districtWorld.CommitRoadSurfaceChanges();
      _selectedDistrictFloraInstanceId = "";
      _hasSelectedDistrictRoad = false;
      _laborNavigation = null;
      _districtWorldCompositionKey = DistrictCompositionKey(district);
      _districtWorld.ShowDistrictSelection(district, _districtSelection);
      RefreshSelectedObjectPanel();
      return true;
    }

    private void ComposeDistrictRoadFamilyModal()
    {
      RemoveDocumentModal();
      var overlay = new VisualElement { name = "document-modal" };
      overlay.AddToClassList("document-modal");
      var panel = new VisualElement { name = "district-road-family-modal" };
      panel.AddToClassList("district-road-family-modal");
      panel.Add(StyledLabel("ROAD FAMILY", "district-road-family-title"));
      panel.Add(StyledLabel(
          "Choose a road surface. Route shapes, corners, and intersections are created automatically as you draw.",
          "district-road-family-intro"));
      var list = new ScrollView(ScrollViewMode.Vertical);
      list.AddToClassList("district-road-family-list");
      AddDistrictRoadFamilyCard(list,
          DistrictRoadPlacementModel.DirtFamily,
          "FREE PER TILE",
          "CityForgeV3/Roads/DirtRoadV1/straight");
      AddDistrictRoadFamilyCard(list,
          DistrictRoadPlacementModel.AntiqueBrickFamily,
          "$25 PER TILE",
          "CityForgeV3/Materials/RoadsChatGPTV1/brick-antique");
      AddDistrictRoadFamilyCard(list,
          DistrictRoadPlacementModel.PikeDirtFamily,
          "FREE PER TILE",
          "CityForgeV3/Roads/NationalPikeDirtV1/straight");
      var delete = CfButton.Create("DELETE ROAD", () =>
      {
        _builderTool = "Delete Road";
        RemoveDocumentModal();
        Show(AppScreen.DistrictTerraform);
      }, true, _builderTool == "Delete Road" ? "primary" : "quiet");
      delete.name = "district-road-delete";
      delete.tooltip = "Delete road tiles under the X cursor. Press Escape to return to the normal selector.";
      panel.Add(list);
      var actions = new VisualElement();
      actions.AddToClassList("district-road-family-actions");
      var bridges = CfButton.Create("BRIDGES", () =>
          ComposeDistrictBridgeModal(), true, "quiet");
      bridges.name = "district-road-bridges";
      actions.Add(bridges);
      actions.Add(delete);
      var cancel = CfButton.Create("CANCEL", RemoveDocumentModal,
          true, "quiet");
      cancel.tooltip = "Close the Road Family catalog without changing the selected road.";
      actions.Add(cancel);
      panel.Add(actions);
      overlay.Add(panel);
      _root.Add(overlay);
      panel.schedule.Execute(panel.Focus);
    }

    private void AddDistrictRoadFamilyCard(VisualElement container,
        string family, string price, string previewResource)
    {
      var card = new Button(() =>
      {
        _builderTool = family;
        RemoveDocumentModal();
        Show(AppScreen.DistrictTerraform);
      })
      {
        name = $"district-road-family-{family.ToLowerInvariant().Replace(' ', '-')}",
        tooltip = $"{family} — draw a route on the district grid. {price}. " +
                    "Corners and intersections are selected automatically."
      };
      card.AddToClassList("district-road-family-card");
      if (_builderTool == family)
        card.AddToClassList("district-road-family-card--selected");
      var preview = new VisualElement();
      preview.AddToClassList("district-road-family-preview");
      var texture = Resources.Load<Texture2D>(previewResource);
      if (texture != null)
        preview.style.backgroundImage = new StyleBackground(texture);
      var copy = new VisualElement();
      copy.AddToClassList("district-road-family-copy");
      copy.Add(StyledLabel(family.ToUpperInvariant(),
          "district-road-family-name"));
      copy.Add(StyledLabel(price, "district-road-family-price"));
      card.Add(preview);
      card.Add(copy);
      container.Add(card);
    }

    private static Vector2Int DistrictRoadCell(RegionCityTile district,
        float normalizedX, float normalizedY)
    {
      var columns = DistrictScale.Columns(district.Width);
      var rows = DistrictScale.Columns(district.Height);
      return new Vector2Int(
          Mathf.Clamp(Mathf.FloorToInt(normalizedX * columns), 0, columns - 1),
          Mathf.Clamp(Mathf.FloorToInt(normalizedY * rows), 0, rows - 1));
    }

    private bool CanPlaceDistrictRoad(RegionCityTile district, int x, int z)
    {
      if (DistrictRoadLotOccupied(district, x, z)) return false;
      var point=DistrictBridgePlanner.Center(district,new Vector2Int(x,z));
      if (_districtWorld != null)
      {
        var bridge=_districtWorld.BridgeAt(point);var cell=new Vector2Int(x,z);
        if(bridge!=null && cell!=bridge.Start && cell!=bridge.End)return false;
        if(_districtWorld.SampleBridgeSurface(point).Channel)return false;
      }
      var existing = DistrictRoadSession(district).At(x, z);
      if (existing?.PackageId == DistrictRoadPlacementModel.PackageId(
              _builderTool)) return true;
      return DistrictRoadPlacementModel.CostPerTile(_builderTool) <=
             district.Treasury;
    }

    private bool DistrictRoadLotOccupied(RegionCityTile district, int x, int z)
    {
      var lots = district.Lots;
      if (_districtRoadLotDistrict != district ||
          !ReferenceEquals(_districtRoadLotSource, lots) ||
          _districtRoadLotCount != (lots?.Count ?? 0))
      {
        _districtRoadLotDistrict = district;
        _districtRoadLotSource = lots;
        _districtRoadLotCount = lots?.Count ?? 0;
        _districtRoadLotCells.Clear();
        if (lots != null)
          foreach (var lot in lots)
          {
            if (!TryGetDistrictLotFootprint(lot, out _,
                    out var spanX, out var spanZ)) continue;
            var minX = lot.GridX + lot.ShoreOffsetX / 10f;
            var minZ = lot.GridZ + lot.ShoreOffsetZ / 10f;
            for (var cellZ = Mathf.FloorToInt(minZ);
                 cellZ < Mathf.CeilToInt(minZ + spanZ); cellZ++)
              for (var cellX = Mathf.FloorToInt(minX);
                   cellX < Mathf.CeilToInt(minX + spanX); cellX++)
                if (cellX + .5f >= minX && cellX + .5f < minX + spanX &&
                    cellZ + .5f >= minZ && cellZ + .5f < minZ + spanZ)
                  _districtRoadLotCells.Add(new Vector2Int(cellX, cellZ));
          }
      }
      return _districtRoadLotCells.Contains(new Vector2Int(x, z));
    }

    private void InvalidateDistrictRoadLotCells()
    {
      _districtRoadLotDistrict = null;
      _districtRoadLotSource = null;
      _districtRoadLotCount = -1;
      _districtRoadLotCells.Clear();
    }

    private DistrictRoadPlacementModel.EditSession DistrictRoadSession(
        RegionCityTile district)
    {
      district.Roads ??= new List<PlacedRoadPiece>();
      if (_districtRoadEditSession == null ||
          !_districtRoadEditSession.Owns(district.Roads))
        _districtRoadEditSession = new DistrictRoadPlacementModel.EditSession(
            district.Roads);
      return _districtRoadEditSession;
    }

    private bool PlaceDistrictRoad(RegionCityTile district, int x, int z)
    {
      if (district == null || !IsDistrictRoadToolActive() ||
          !CanPlaceDistrictRoad(district, x, z)) return false;
      var columns = DistrictScale.Columns(district.Width);
      var rows = DistrictScale.Columns(district.Height);
      var session = DistrictRoadSession(district);
      var treasury = district.Treasury;
      if (!session.TryPlace(x, z, columns, rows, _builderTool,
              ref treasury)) return true;
      district.Treasury = treasury;
      _districtWorld?.RefreshRoadCellsAndNeighbors(district,
          new[] { new Vector2Int(x, z) }, session.At);
      var money = _root?.Q<Label>("district-simulation-money");
      if (money != null) money.text = $"${district.Treasury:N0}";
      return true;
    }

    private bool DeleteDistrictRoadAt(RegionCityTile district,
        Vector2Int cell)
    {
      if (district?.Roads == null) return false;
      var session = DistrictRoadSession(district);
      if (!session.TryDelete(cell.x, cell.y)) return false;
      _districtWorld?.RefreshRoadCellsAndNeighbors(district,
          new[] { cell }, session.At);
      _hasSelectedDistrictRoad = false;
      return true;
    }

    private void SetDistrictRoadDeleteCursor(bool visible)
    {
      if (!visible || !IsDistrictRoadDeleteToolActive())
      {
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        return;
      }
      if (_districtRoadDeleteCursorTexture == null)
      {
        const int size = 24;
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
          for (var x = 0; x < size; x++)
          {
            var stroke = Mathf.Abs(x - y) <= 2 ||
                         Mathf.Abs(x + y - (size - 1)) <= 2;
            pixels[y * size + x] = stroke
                ? new Color32(220, 55, 45, 255)
                : new Color32(0, 0, 0, 0);
          }
        _districtRoadDeleteCursorTexture = new Texture2D(size, size,
            TextureFormat.RGBA32, false)
        { name = "City Forge Delete Road Cursor" };
        _districtRoadDeleteCursorTexture.SetPixels32(pixels);
        _districtRoadDeleteCursorTexture.Apply(false, true);
      }
      UnityEngine.Cursor.SetCursor(_districtRoadDeleteCursorTexture,
          new Vector2(12f, 12f), CursorMode.Auto);
    }

    private bool TryDeleteSelectedDistrictRoad()
    {
      if (!_hasSelectedDistrictRoad ||
          _districtEditorMode != DistrictEditorMode.Builder ||
          _builderCategory != "Roads") return false;
      var district = FindSelectedRegionTile();
      if (district?.Roads == null) return false;
      var session = DistrictRoadSession(district);
      if (!session.TryDelete(
              _selectedDistrictRoadCell.x,
              _selectedDistrictRoadCell.y))
      {
        _hasSelectedDistrictRoad = false;
        return false;
      }
      _districtWorld?.RefreshRoadCellsAndNeighbors(district,
          new[] { _selectedDistrictRoadCell }, session.At);
      _districtWorld?.HideLotPlacementGuide();
      _hasSelectedDistrictRoad = false;
      _districtWorldCompositionKey = DistrictCompositionKey(district);
      _districtWorld?.CommitRoadSurfaceChanges();
      SaveDistrictEdit();
      return true;
    }

    private RegionCityTile FindSelectedRegionTile() =>
        _openRegion?.Tiles?.Find(tile => tile.TileId == _selectedRegionTileId);

    private static int RegionTileTone(RegionCityTile tile) =>
        Mathf.Abs(tile.X * 7 + tile.Y * 11 + tile.Width * 3) % 4;
  }
}
