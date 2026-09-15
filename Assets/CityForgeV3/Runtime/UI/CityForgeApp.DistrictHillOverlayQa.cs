#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using CityForgeV3.World;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenHillOverlayQa()
        {
            OpenLittleRiverBendHillsQa();
            _root.schedule.Execute(()=>{FocusHillOverlayQa(false);CheckHillOverlayQa();}).StartingIn(500);
        }
        public void FocusHillOverlayQa(bool close)
        {
            var overlay=_districtWorld?.GetComponentInChildren<DistrictHillGroundOverlay>();if(overlay==null)throw new Exception("Hill overlay not built");
            _districtEdgePanDirection=Vector2Int.zero;
            _terraformPanOffset=overlay.ReviewPoint;_terraformZoomLevel=close?DistrictZoomLevel.LOD0:DistrictZoomLevel.LOD2;
            _districtWorld.SetPan(_terraformPanOffset);_districtWorld.SetZoom(_terraformZoomLevel);
            if(close)_districtWorld.WorldCamera.orthographicSize=20;
        }
        public void ToggleHillOverlayQa(bool visible)
        {
            var overlay=_districtWorld?.GetComponentInChildren<DistrictHillGroundOverlay>();if(overlay!=null){overlay.PresentationEnabled=visible;FocusHillOverlayQa(_terraformZoomLevel==DistrictZoomLevel.LOD0);}
        }
        public void CheckHillOverlayQa()
        {
            var before=_districtWorld?.GetComponentInChildren<DistrictHillGroundOverlay>();if(before==null||before.PatchCount<1)throw new Exception("Missing hill detail");
            int patches=before.PatchCount;float signature=before.GeometrySignature;
            CheckLittleRiverBendHillsQa();
            var after=_districtWorld.GetComponentInChildren<DistrictHillGroundOverlay>();
            if(after==null||after.PatchCount!=patches||after.GeometrySignature!=signature)throw new Exception("Overlay changed after saved reload");
            foreach(var filter in after.GetComponentsInChildren<MeshFilter>())foreach(var p in filter.sharedMesh.vertices)
                if(Mathf.Abs(p.y-_districtWorld.TerrainElevation(p.x,p.z)-.04f)>.001f)throw new Exception("Overlay lifted away from hill");
            var material=after.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            if(material.mainTexture==null||material.shader.name!="CityForgeV3/HillGroundOverlay")throw new Exception("Missing overlay texture or shader");
            FocusHillOverlayQa(false);
            Debug.Log($"HILL OVERLAY QA PASS patches={patches} signature={signature} savedReload=true grounded=true texture={material.mainTexture.name} review={after.ReviewPoint}");
        }
    }
}
#endif
