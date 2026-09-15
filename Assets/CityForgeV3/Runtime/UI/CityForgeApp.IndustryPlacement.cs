using System;
using System.Collections.Generic;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        VisualElement _industryPlacementOverlay;
        readonly List<(Button button, Vector2 point)> _industryMarkers = new();
        bool IndustryPlacementActive => _industryPlacementOverlay != null;

        void CancelIndustryPlacement()
        {
            _industryPlacementOverlay?.RemoveFromHierarchy();
            _industryPlacementOverlay = null;
            _industryMarkers.Clear();
        }

        void BeginIndustryPlacement(string resource)
        {
            var d = FindSelectedRegionTile(); if (d == null || _districtWorld == null) return;
            CancelDistrictSelectionPointer(); CancelLaborPlacement(); RemoveDocumentModal();
            CancelIndustryPlacement();
#if UNITY_EDITOR
            _districtResourceDebugPlacement = "";
#endif
            _pendingDistrictLotId = ""; _pendingDistrictFloraId = "";
            _districtEdgePanDirection = Vector2Int.zero;
            var screen = _root.Q<VisualElement>(className: "district-terraform-screen");
            if (screen == null) return;
            _industryPlacementOverlay = new VisualElement
                { name = "industry-placement", pickingMode = PickingMode.Ignore };
            _industryPlacementOverlay.style.position = Position.Absolute;
            _industryPlacementOverlay.StretchToParentSize(); screen.Add(_industryPlacementOverlay);
            if (resource == "stone")
                foreach (var site in d.StoneSites)
                {
                    if (site.Built || site.Kind != "stone") continue;
                    var reason = _districtWorld.QuarrySiteBlockReason(d, site);
                    AddIndustryMarker(site.Id, DistrictQuarry.Point(d, site),
                        string.IsNullOrEmpty(reason) ? "Build stone quarry here" : reason, () =>
                    {
                        var currentReason = _districtWorld.QuarrySiteBlockReason(d, site);
                        if (!string.IsNullOrEmpty(currentReason))
                        { ShowDistrictNotice(currentReason); return; }
                        SetQuarry(site, true);
                    }, string.IsNullOrEmpty(reason));
                }
            else if (resource == "coal")
                foreach (var deposit in d.ResourceDeposits)
                {
                    if (!DistrictCoalMine.CanBuild(d, deposit, out _)) continue;
                    var point = new Vector2((deposit.NormalizedX-.5f)*DistrictScale.SizeMeters(d.Width),
                        (deposit.NormalizedZ-.5f)*DistrictScale.SizeMeters(d.Height));
                    AddIndustryMarker(deposit.Id, point, "Build coal mine here", () =>
                    {
                        if (!DistrictCoalMine.CanBuild(d, deposit, out _))
                        { ShowDistrictNotice("This coal deposit needs a suitable mountain slope."); return; }
                        SetDistrictMine(deposit, true);
                    });
                }
            if (_industryMarkers.Count == 0)
            { CancelIndustryPlacement(); ShowDistrictNotice("No buildable deposits available."); return; }
            var cancel = CfButton.Create("CANCEL PLACEMENT · ESC", CancelIndustryPlacement, true, "quiet");
            cancel.name = "industry-placement-cancel";
            cancel.style.position = Position.Absolute; cancel.style.right = 24; cancel.style.top = 78;
            _industryPlacementOverlay.Add(cancel);
            _terraformPanOffset = Vector2.zero; _terraformZoomLevel = DistrictZoomLevel.LOD4;
            _districtWorld.SetPan(_terraformPanOffset); _districtWorld.SetZoom(_terraformZoomLevel);
            ShowDistrictNotice(resource == "stone"
                ? "Green arrows: build a quarry. Amber arrows: click to see what prevents building."
                : "Click a green arrow to place a coal mine.");
            RefreshIndustryMarkers();
        }

        void AddIndustryMarker(string id, Vector2 point, string label, Action build, bool available = true)
        {
            var button = new Button(build) { name = "industry-site-" + id, tooltip = label };
            button.AddToClassList("industry-deposit-arrow");
            button.Add(new CfDepositArrow(available));
            button.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            button.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
            _industryPlacementOverlay.Add(button); _industryMarkers.Add((button, point));
        }

        void LateUpdate()
        {
            RefreshDistrictNotice();
            RefreshIndustryMarkers();
        }

        void RefreshIndustryMarkers()
        {
            if (!IndustryPlacementActive || _districtWorld == null) return;
            var camera = _districtWorld.WorldCamera; if (camera == null) return;
            var width = _industryPlacementOverlay.resolvedStyle.width;
            var height = _industryPlacementOverlay.resolvedStyle.height;
            if (float.IsNaN(width) || float.IsNaN(height)) return;
            var bounce = 8f + 10f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * 5f));
            foreach (var marker in _industryMarkers)
            {
                var p = camera.WorldToViewportPoint(_districtWorld.ResourceWorldPoint(marker.point));
                marker.button.style.display = p.z > 0 && p.x > 0 && p.x < 1 && p.y > 0 && p.y < 1
                    ? DisplayStyle.Flex : DisplayStyle.None;
                marker.button.style.left = p.x * width - 28;
                marker.button.style.top = (1-p.y) * height - 60 - bounce;
            }
        }
    }
}
