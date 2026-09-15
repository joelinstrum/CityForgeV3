using UnityEngine;
namespace CityForgeV3.Buildings3D
{
    public sealed class BuildingDoorController : MonoBehaviour
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private Quaternion closedRotation = Quaternion.identity;
        [SerializeField] private float openAngle = -90f;
        [SerializeField] private float duration = 1f;
        private float amount;
        private BuildingDoorController followSource;
        public void Follow(BuildingDoorController source) { followSource = source; SyncFromSource(); }
        private void LateUpdate() { if (followSource != null) SyncFromSource(); }
        private void SyncFromSource() { if (followSource == null) return; IsOpen = followSource.IsOpen; amount = followSource.OpenAmount; Apply(); }
        public bool IsOpen { get; private set; }
        public float OpenAmount => amount;
        public void Configure(Transform value, float angle = -90f)
        { hinge = value; closedRotation = value.localRotation; openAngle = angle; SetOpen(false, true); }
        public void SetOpen(bool value, bool immediate = false)
        { IsOpen = value; if (immediate) { amount = value ? 1f : 0f; Apply(); } }
        private void Update() { if (followSource == null) Advance(Time.deltaTime); }
        public void Advance(float seconds)
        { amount = Mathf.MoveTowards(amount, IsOpen ? 1f : 0f, Mathf.Max(0,seconds) / Mathf.Max(.01f,duration)); Apply(); }
        private void Apply()
        { if (hinge != null) hinge.localRotation = closedRotation * Quaternion.AngleAxis(openAngle * Mathf.SmoothStep(0,1,amount), Vector3.up); }
    }
}
