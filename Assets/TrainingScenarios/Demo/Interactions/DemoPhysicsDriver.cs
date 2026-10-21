using System.Collections;
using UnityEngine;

namespace TrainingScenarios.Demo.Interactions
{
    public sealed class DemoPhysicsDriver : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody _triggerProbe;
        [SerializeField]
        private Transform _triggerStart;
        [SerializeField]
        private Transform _triggerCenter;
        [SerializeField]
        private Transform _triggerEnd;
        [SerializeField]
        private Rigidbody _collisionProbe;
        [SerializeField]
        private Transform _collisionStart;
        [SerializeField]
        private Transform _collisionTarget;
        [SerializeField]
        private float _collisionSpeed = 8f;

        private Coroutine _triggerRoutine;
        private Coroutine _collisionRoutine;

        public void RunTriggerProbe()
        {
            if (!_triggerProbe || !_triggerStart || !_triggerCenter || !_triggerEnd)
            {
                return;
            }

            if (_triggerRoutine != null)
            {
                StopCoroutine(_triggerRoutine);
            }

            _triggerRoutine = StartCoroutine(MoveTriggerProbe());
        }

        public void LaunchCollisionProbe()
        {
            if (!_collisionProbe || !_collisionStart || !_collisionTarget)
            {
                return;
            }

            if (_collisionRoutine != null)
            {
                StopCoroutine(_collisionRoutine);
            }

            _collisionRoutine = StartCoroutine(MoveCollisionProbe());
        }

        public void ResetProbes()
        {
            ResetTriggerProbe();
            ResetCollisionProbe();
        }

        private IEnumerator MoveTriggerProbe()
        {
            ResetTriggerProbe();
            yield return new WaitForFixedUpdate();

            _triggerProbe.MovePosition(_triggerCenter.position);
            yield return new WaitForFixedUpdate();

            _triggerProbe.MovePosition(_triggerEnd.position);
            yield return new WaitForFixedUpdate();
            _triggerRoutine = null;
        }

        private IEnumerator MoveCollisionProbe()
        {
            ResetCollisionProbe();
            yield return new WaitForFixedUpdate();

            var direction = (_collisionTarget.position - _collisionStart.position).normalized;
            _collisionProbe.linearVelocity = direction * _collisionSpeed;
            _collisionRoutine = null;
        }

        private void ResetTriggerProbe()
        {
            if (!_triggerProbe || !_triggerStart)
            {
                return;
            }

            _triggerProbe.position = _triggerStart.position;
            _triggerProbe.rotation = _triggerStart.rotation;
            Physics.SyncTransforms();
        }

        private void ResetCollisionProbe()
        {
            if (!_collisionProbe || !_collisionStart)
            {
                return;
            }

            _collisionProbe.position = _collisionStart.position;
            _collisionProbe.rotation = _collisionStart.rotation;
            _collisionProbe.linearVelocity = Vector3.zero;
            _collisionProbe.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }
    }
}
