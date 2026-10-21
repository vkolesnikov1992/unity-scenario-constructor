using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TrainingScenarios.Demo.Interactions
{
    public sealed class WorldInteractable : MonoBehaviour
    {
        [SerializeField]
        private string _prompt = "Interact";
        [SerializeField]
        private WorldActionSource _actionSource;
        [SerializeField]
        private Button _buttonAction;
        [SerializeField]
        private UnityEvent _onInteract = new UnityEvent();
        [SerializeField]
        private bool _singleUse;
        [SerializeField]
        private Transform _visualPivot;
        [SerializeField]
        private Vector3 _usedLocalEulerAngles;

        private Quaternion _initialLocalRotation;
        private bool _used;

        public string Prompt => _prompt;

        public bool CanInteract => !_singleUse || !_used;

        public Vector3 InteractionPosition => transform.position;

        private void Awake()
        {
            if (_visualPivot)
            {
                _initialLocalRotation = _visualPivot.localRotation;
            }
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            _used = true;
            _onInteract.Invoke();
            _buttonAction?.onClick.Invoke();
            _actionSource?.Raise();

            if (_visualPivot)
            {
                _visualPivot.localRotation = Quaternion.Euler(_usedLocalEulerAngles);
            }
        }

        public void ResetInteraction()
        {
            _used = false;
            if (_visualPivot)
            {
                _visualPivot.localRotation = _initialLocalRotation;
            }
        }
    }
}
