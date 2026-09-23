using System.Linq;
using UnityEngine;
namespace CityForgeV3.Behaviors
{
    public sealed class DockWorkerVisual : MonoBehaviour
    {
        private Animator _animator;
        private Transform _bundle;
        private Transform[] _upper, _fore, _hand;
        private bool _carrying;
        public void Initialize(Transform bundle, int workerIndex)
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                // Independent idle beats keep the paired dock crew from
                // appearing to perform the same step in lockstep. Simulation
                // timing and walking routes remain authoritative elsewhere.
                _animator.speed = workerIndex % 2 == 0 ? .94f : 1.06f;
                _animator.Play("Idle", 0,
                    Mathf.Repeat(workerIndex * .43f, 1f));
                _animator.Update(0f);
            }
            _bundle = bundle;
            var bones = GetComponentsInChildren<Transform>();
            _upper = new[] { bones.FirstOrDefault(x => x.name == "L_Upperarm"), bones.FirstOrDefault(x => x.name == "R_Upperarm") };
            _fore = new[] { bones.FirstOrDefault(x => x.name == "L_Forearm"), bones.FirstOrDefault(x => x.name == "R_Forearm") };
            _hand = new[] { bones.FirstOrDefault(x => x.name == "L_Hand"), bones.FirstOrDefault(x => x.name == "R_Hand") };
        }
        public void SetAction(bool walking, bool carrying)
        {
            if (_animator != null) _animator.SetBool("Walking", walking);
            _carrying = carrying;
            if (_bundle != null) _bundle.gameObject.SetActive(carrying);
        }
        private void LateUpdate()
        {
            if (!_carrying || _upper == null) return;
            for (var i = 0; i < 2; i++)
            {
                if (_upper[i] == null || _fore[i] == null || _hand[i] == null) continue;
                var shoulder = _upper[i].position;
                var target = transform.TransformPoint(new Vector3(i == 0 ? -.23f : .23f, 1.03f, .39f));
                var a = Vector3.Distance(shoulder, _fore[i].position);
                var b = Vector3.Distance(_fore[i].position, _hand[i].position);
                var axis = target - shoulder;
                var distance = Mathf.Clamp(axis.magnitude, .01f, Mathf.Max(.02f, a + b - .005f));
                axis.Normalize();
                var bend = Vector3.ProjectOnPlane(-transform.up + transform.right * (i == 0 ? -.5f : .5f), axis).normalized;
                var along = (a * a - b * b + distance * distance) / (2 * distance);
                var elbow = shoulder + axis * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
                _upper[i].rotation = Quaternion.FromToRotation(_fore[i].position - shoulder, elbow - shoulder) * _upper[i].rotation;
                _fore[i].rotation = Quaternion.FromToRotation(_hand[i].position - _fore[i].position, target - _fore[i].position) * _fore[i].rotation;
            }
        }
    }
}
