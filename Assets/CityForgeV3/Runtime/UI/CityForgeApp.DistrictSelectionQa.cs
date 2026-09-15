#if UNITY_EDITOR
using System;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenSavedDistrictSelectionQa()
        {
            if (_currentScreen != AppScreen.Splash || _hasOpenLot || _openRegion != null)
            {
                Debug.LogWarning("Selection QA requires fresh splash; live work preserved.");
                return;
            }
            OpenSavedDistrictDecalQa();
            if (_districtWorld == null) return;
            SelectDistrictCategory("Select");
            Show(AppScreen.DistrictTerraform);
        }

        public void CheckDistrictSelectionGestureQa()
        {
            if (_currentScreen != AppScreen.DistrictTerraform || _districtWorld == null || _districtMarqueeActive)
                return;
            var district = FindSelectedRegionTile();
            var snapshot = JsonUtility.ToJson(_openRegion);
            var surface = _districtSelectionMarquee.parent;
            var bounds = surface.worldBound;
            var start = new Vector2(bounds.xMin + bounds.width * .28f, bounds.yMin + bounds.height * .25f);
            var end = new Vector2(bounds.xMin + bounds.width * .85f, bounds.yMin + bounds.height * .85f);
            string[] Selected() => _districtSelection.Select(s => s.Kind + ":" + s.Id).OrderBy(s => s).ToArray();
            var original = Selected();
            BeginDistrictSelectionPointer(district, start, surface, PointerId.mousePointerId);
            UpdateDistrictSelectionMarquee(end);
            if (!_districtMarqueeActive || _districtSelectionDragActive || !Selected().SequenceEqual(original))
                throw new Exception("Selection changed before release or became a move");
            if (DeleteDistrictSelection()) throw new Exception("Delete ran while drawing rectangle");
            // Crossing UI/viewport edges must retain the gesture until release.
            UpdateDistrictSelectionMarquee(bounds.max + new Vector2(40, 40));
            if (!_districtMarqueeActive) throw new Exception("Viewport exit committed early");
            UpdateDistrictSelectionMarquee(end);
            CompleteDistrictSelectionPointer(district);
            var forward = Selected();
            if (forward.Length == 0 || _districtMarqueeActive || surface.HasPointerCapture(PointerId.mousePointerId))
                throw new Exception("Release failed to commit/release capture");
            BeginDistrictSelectionPointer(district, end, surface, PointerId.mousePointerId);
            UpdateDistrictSelectionMarquee(start);
            CompleteDistrictSelectionPointer(district);
            if (!Selected().SequenceEqual(forward)) throw new Exception("Reverse drag changed membership");
            BeginDistrictSelectionPointer(district, start, surface, PointerId.mousePointerId);
            UpdateDistrictSelectionMarquee(end);
            CancelDistrictSelectionPointer();
            if (!Selected().SequenceEqual(forward)) throw new Exception("Cancel changed previous selection");
            if (JsonUtility.ToJson(_openRegion) != snapshot) throw new Exception("Selecting changed region data");
            Debug.Log("DISTRICT SELECTION GESTURE PASS count=" + forward.Length +
                " deferred=true reverse=true cancel=true noDataChanges=true " +
                string.Join(" ", _districtSelection.GroupBy(s => s.Kind).Select(g => g.Key + "=" + g.Count())));
            // Keep the tested selection visible for physical mouse review.
        }

        public void LogDistrictSelectionQa()
        {
            Debug.Log("DISTRICT SELECTION STATE active=" + _districtMarqueeActive +
                " category=" + ActiveDistrictCategory + " tool=" + ActiveDistrictTool + " move=" + _districtSelectionDragActive + " count=" + _districtSelection.Count + " " +
                string.Join(" ", _districtSelection.GroupBy(s => s.Kind).Select(g => g.Key + "=" + g.Count())));
        }
    }
}
#endif
