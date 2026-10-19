using System;
using System.Collections.Generic;
using UnityEngine;
using TrainingScenarios.Runtime.Actions.Contracts;
using TrainingScenarios.Runtime.Rules.Contracts;

namespace TrainingScenarios.Runtime.Actions.Bindings
{
    public sealed class SceneBindingRegistry : MonoBehaviour, IScenarioContext
    {
        [SerializeField]
        private List<SceneBindingBehaviour> _bindings = new List<SceneBindingBehaviour>();

        private readonly Dictionary<string, SceneBindingBehaviour> _bindingsById =
            new Dictionary<string, SceneBindingBehaviour>(StringComparer.Ordinal);

        private readonly List<IActionSource> _actionSources = new List<IActionSource>();

        public IReadOnlyList<SceneBindingBehaviour> Bindings => _bindings;

        public IReadOnlyList<IActionSource> ActionSources => _actionSources;

        private void Awake()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _bindingsById.Clear();
            _actionSources.Clear();

            foreach (var binding in _bindings)
            {
                if (!binding)
                {
                    continue;
                }

                if (_bindingsById.ContainsKey(binding.BindingId))
                {
                    continue;
                }

                _bindingsById.Add(binding.BindingId, binding);

                if (binding is IActionSource actionSource)
                {
                    _actionSources.Add(actionSource);
                }
            }
        }

        public bool TryGetBinding(string bindingId, out SceneBindingBehaviour binding)
        {
            return _bindingsById.TryGetValue(bindingId, out binding);
        }

        public bool TryGetState(string stateId, out string value)
        {
            if (_bindingsById.TryGetValue(stateId, out var binding) &&
                binding is IScenarioStateProvider stateProvider)
            {
                value = stateProvider.CurrentValue;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public void ResetState()
        {
            foreach (var binding in _bindings)
            {
                if (binding is IScenarioStateProvider stateProvider)
                {
                    stateProvider.ResetState();
                }
            }
        }
    }
}
