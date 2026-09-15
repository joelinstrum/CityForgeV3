using UnityEngine;
namespace CityForgeV3.Buildings3D
{
    /// <summary>Rigid axle rotation. Uses scaled game time; shadows can follow the visible wheel.</summary>
    public sealed class BuildingWheelRotation : MonoBehaviour
    {
        [SerializeField] Transform wheel;
        [SerializeField] Vector3 localAxis = Vector3.forward;
        [SerializeField] Quaternion restRotation = Quaternion.identity;
        [SerializeField] float revolutionsPerMinute = 4f;
        [SerializeField] bool running = true;
        double epoch;
        float phase;
        BuildingWheelRotation source;
        public Transform Wheel => wheel;
        public float RPM => revolutionsPerMinute;
        public bool Running => source != null ? source.Running : running;
        public float Angle => source != null ? source.Angle : Evaluate(Time.timeAsDouble);
        public void Configure(Transform target, Vector3 axis, float rpm = 4f)
        {
            wheel = target; localAxis = axis.sqrMagnitude > 0 ? axis.normalized : Vector3.forward;
            restRotation = target.localRotation; revolutionsPerMinute = rpm;
            epoch = Time.timeAsDouble; phase = 0; Apply();
        }
        public float Evaluate(double time) => Mathf.Repeat(phase + (running ? (float)((time-epoch)*revolutionsPerMinute*6.0) : 0),360f);
        public void SetRunning(bool value) { Rebase(); running=value; Apply(); }
        public void SetRPM(float value) { if(float.IsNaN(value)||float.IsInfinity(value))return; Rebase();revolutionsPerMinute=value;Apply(); }
        public void Follow(BuildingWheelRotation visible) { source=visible==this?null:visible; Apply(); }
        void Rebase() { phase=Angle;epoch=Time.timeAsDouble; }
        void LateUpdate() => Apply();
        void Apply() { if(wheel!=null)wheel.localRotation=restRotation*Quaternion.AngleAxis(Angle,localAxis); }
    }
}
