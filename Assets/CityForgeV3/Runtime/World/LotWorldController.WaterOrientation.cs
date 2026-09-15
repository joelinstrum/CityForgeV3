using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private Transform _waterOrientationArrow;
        private Vector3 _arrowLand, _arrowWater, _pendingWaterLand;
        private Material _waterArrowMaterial;
        private Vector3 _arrowDragOffset;
        public bool WaterArrowSelected => ActiveObjectSelection == LotObjectSelectionKind.WaterOrientation && HasWaterOrientation;
        public bool HitWaterArrow(Vector2 point, Vector2 size)
        {
            if (!HasWaterOrientation || _districtHosted || _camera == null) return false;
            var pixel = PanelToCameraPixel(point,size,new Vector2(_camera.pixelWidth,_camera.pixelHeight));
            var a = _session.Data.WaterOrientationLand; var b = _session.Data.WaterOrientationWater;
            a.y=b.y=.3f;
            var start = (Vector2)_camera.WorldToScreenPoint(transform.TransformPoint(a));
            var end = (Vector2)_camera.WorldToScreenPoint(transform.TransformPoint(b));
            var delta=end-start;
            var t=delta.sqrMagnitude>.001f ? Mathf.Clamp01(Vector2.Dot(pixel-start,delta)/delta.sqrMagnitude) : 0;
            return Vector2.Distance(pixel,start+delta*t) <= 12;
        }
        public void SelectWaterArrow(Vector2 point, Vector2 size)
        {
            DeselectBuilding3D(); ClearPropSelectionAndPreview(); ClearFloraSelectionAndPreview();
            _session.Select(false); ActiveObjectSelection=LotObjectSelectionKind.WaterOrientation;
            if (TryLotPointFromPanel(point,size,out var ground)) _arrowDragOffset=_session.Data.WaterOrientationLand-ground;
        }
        public bool PlaceWaterArrow(Vector2 point, Vector2 size)
        {
            if (!TryLotPointFromPanel(point,size,out var ground)) return false;
            var data=_session.Data; data.HasWaterOrientation=true;
            data.WaterOrientationLand=ground;
            data.WaterOrientationWater=ground+Vector3.left*6;
            MoveWaterArrow(ground); SelectWaterArrow(point,size); NotifyStateChanged(); return true;
        }
        public bool DragWaterArrow(Vector2 point,Vector2 size)
        {
            if (!WaterArrowSelected || !TryLotPointFromPanel(point,size,out var ground)) return false;
            MoveWaterArrow(ground+_arrowDragOffset); return true;
        }
        public void MoveWaterArrow(Vector3 tail)
        {
            var data=_session.Data; var direction=data.WaterOrientationWater-data.WaterOrientationLand; direction.y=0;
            tail.x=Mathf.Clamp(tail.x,-LotWidthMeters/2+.5f-Mathf.Min(0,direction.x),LotWidthMeters/2-.5f-Mathf.Max(0,direction.x));
            tail.z=Mathf.Clamp(tail.z,-LotDepthMeters/2+.5f-Mathf.Min(0,direction.z),LotDepthMeters/2-.5f-Mathf.Max(0,direction.z));
            tail.y=0; data.WaterOrientationLand=tail; data.WaterOrientationWater=tail+direction;
        }
        public int WaterArrowDirection
        {
            get
            {
                if (!HasWaterOrientation) return -1;
                var delta=_session.Data.WaterOrientationWater-_session.Data.WaterOrientationLand; delta.y=0;
                if (delta.sqrMagnitude < .001f) return -1;
                for (var i=0;i<4;i++)
                    if (Vector3.Dot(delta.normalized,Quaternion.Euler(0,(i-1)*90,0)*Vector3.forward)>.9999f) return i;
                return -1;
            }
        }
        public void SetWaterArrowDirection(int quarterTurns)
        {
            if (!HasWaterOrientation) return;
            var data=_session.Data; var length=Mathf.Clamp(Vector3.Distance(data.WaterOrientationLand,data.WaterOrientationWater),1,Mathf.Min(LotWidthMeters,LotDepthMeters)-1);
            data.WaterOrientationWater=data.WaterOrientationLand+Quaternion.Euler(0,(quarterTurns-1)*90,0)*Vector3.forward*length;
            MoveWaterArrow(data.WaterOrientationLand); NotifyStateChanged();
        }
        public void EndWaterArrowDrag() { NotifyStateChanged(); }
        public bool HasWaterOrientation => _session.Data.HasWaterOrientation;
        public bool SetWaterOrientationFromPanel(Vector2 point, Vector2 size, bool waterEnd)
        {
            if (!TryLotPointFromPanel(point, size, out var ground)) return false;
            if (!waterEnd) { _pendingWaterLand = ground; return true; }
            if (Vector3.Distance(ground, _pendingWaterLand) < 1) return false;
            _session.Data.WaterOrientationLand = _pendingWaterLand;
            _session.Data.WaterOrientationWater = ground; _session.Data.HasWaterOrientation = true;
            NotifyStateChanged(); return true;
        }
        public void ClearWaterOrientation() { _session.Data.HasWaterOrientation = false; if (WaterArrowSelected || ActiveObjectSelection == LotObjectSelectionKind.WaterOrientation) ActiveObjectSelection = LotObjectSelectionKind.None; NotifyStateChanged(); }
        private void UpdateWaterOrientationArrow()
        {
            var data = _session?.Data;
            if (_waterArrowMaterial != null) _waterArrowMaterial.color = WaterArrowSelected ? new Color(.45f,.85f,1f,1f) : new Color(.15f,.65f,1f,.95f);
            var visible = !_districtHosted && data != null && data.HasWaterOrientation;
            if (!visible) { if (_waterOrientationArrow != null) _waterOrientationArrow.gameObject.SetActive(false); return; }
            if (_waterOrientationArrow != null && _arrowLand == data.WaterOrientationLand && _arrowWater == data.WaterOrientationWater)
            { _waterOrientationArrow.gameObject.SetActive(true); return; }
            if (_waterOrientationArrow != null) Destroy(_waterOrientationArrow.gameObject);
            _arrowLand = data.WaterOrientationLand; _arrowWater = data.WaterOrientationWater;
            _waterOrientationArrow = new GameObject("Orient this part of the lot to water").transform;
            _waterOrientationArrow.SetParent(transform, false);
            if (_waterArrowMaterial == null) _waterArrowMaterial = new Material(Shader.Find("CityForgeV3/WaterOrientationGuide"));
            var material = _waterArrowMaterial;
            var start = new Vector3(_arrowLand.x, .3f, _arrowLand.z);
            var end = new Vector3(_arrowWater.x, .3f, _arrowWater.z);
            var direction = (end-start).normalized;
            var side = Vector3.Cross(Vector3.up,direction);
            void Line(Vector3 a, Vector3 b)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.GetComponent<Collider>().enabled = false; go.transform.SetParent(_waterOrientationArrow,false);
                go.transform.localPosition = (a+b)*.5f; go.transform.localRotation = Quaternion.LookRotation(b-a);
                go.transform.localScale = new Vector3(.16f,.04f,Vector3.Distance(a,b));
                go.GetComponent<Renderer>().sharedMaterial = material;
            }
            Line(start,end); Line(end,end-direction*1.2f+side*.65f); Line(end,end-direction*1.2f-side*.65f);
        }
    }
}
