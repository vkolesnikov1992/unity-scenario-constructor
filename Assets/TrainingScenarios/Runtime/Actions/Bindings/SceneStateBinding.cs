using UnityEngine;

namespace TrainingScenarios.Runtime.Actions.Bindings
{
    public sealed class SceneStateBinding : SceneBindingBehaviour, IScenarioStateProvider
    {
        [SerializeField]
        private string _initialValue;

        private string _currentValue;

        public string StateId => BindingId;

        public string CurrentValue => _currentValue;

        private void Awake()
        {
            ResetState();
        }

        public void SetValue(string value)
        {
            _currentValue = value;
        }

        public void SetTrue()
        {
            _currentValue = bool.TrueString;
        }

        public void SetFalse()
        {
            _currentValue = bool.FalseString;
        }

        public void ResetState()
        {
            _currentValue = _initialValue;
        }
    }
}
