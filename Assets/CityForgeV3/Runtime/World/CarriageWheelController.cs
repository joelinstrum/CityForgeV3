using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    // Presentation only: the future horse-team/route controller owns translation.
    public sealed class CarriageWheelController : MonoBehaviour
    {
        sealed class Wheel { public Transform Pivot, Steering; public Vector3 Previous; public float Radius; }
        readonly List<Wheel> wheels = new();
        bool initialized;
        public int WheelCount => wheels.Count;
        public float TotalTravelMeters { get; private set; }
        void FindWheels()
        {
            wheels.Clear();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Carriage_Front_") && !t.name.StartsWith("Carriage_Rear_")) continue;
                var renderer = t.GetComponent<Renderer>();
                if (renderer == null) continue;
                wheels.Add(new Wheel { Pivot=t, Steering=t.name.StartsWith("Carriage_Front_") ? transform.Find("Forecarriage Steering") ?? transform : transform, Radius=Mathf.Max(.01f,renderer.bounds.size.y*.5f), Previous=t.position });
            }
        }
        void OnEnable() { initialized=false; FindWheels(); }
        void LateUpdate()
        {

            foreach (var w in wheels)
            {
                var forward=w.Steering.forward;var axis=Vector3.Cross(Vector3.up,forward).normalized;
                var delta=w.Pivot.position-w.Previous;
                var distance=Vector3.Dot(delta,forward);
                // Placement teleports establish a new baseline without spinning.
                if (initialized && delta.magnitude <= Mathf.Max(.1f,Time.deltaTime*6f))
                {
                    w.Pivot.Rotate(axis,distance/w.Radius*Mathf.Rad2Deg,Space.World);
                    TotalTravelMeters+=Mathf.Abs(distance)/Mathf.Max(1,wheels.Count);
                }
                w.Previous=w.Pivot.position;
            }
            initialized=true;
        }
    }
}
