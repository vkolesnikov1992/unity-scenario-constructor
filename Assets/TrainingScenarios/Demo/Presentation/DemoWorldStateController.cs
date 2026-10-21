using System.Collections.Generic;
using UnityEngine;
using TrainingScenarios.Demo.Interactions;
using TrainingScenarios.Runtime.Actions.Bindings;
using TrainingScenarios.Runtime.Composition;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Core.Steps;

namespace TrainingScenarios.Demo.Presentation
{
    public sealed class DemoWorldStateController : MonoBehaviour
    {
        [SerializeField]
        private TopDownPlayerController _player;
        [SerializeField]
        private Transform _playerStart;

        private ScenarioRuntime _runtime;
        private ScenarioDefinition _scenario;
        private WorldStepMarker[] _markers;
        private WorldInteractable[] _interactables;
        private SceneStateBinding[] _stateBindings;

        private void Awake()
        {
            CacheSceneObjects();
            ApplyInactiveState();
        }

        public void Bind(ScenarioRuntime runtime, ScenarioDefinition scenario)
        {
            _runtime = runtime;
            _scenario = scenario;
            ApplyStepOrder();
            Refresh();
        }

        public void Refresh()
        {
            if (_runtime == null)
            {
                ApplyInactiveState();
                return;
            }

            foreach (var marker in _markers)
            {
                if (_runtime.Runner.Session.Steps.TryGetValue(marker.StepId, out var step))
                {
                    marker.Apply(step.Status);
                }
            }
        }

        public void ResetWorld()
        {
            CacheSceneObjects();
            _runtime = null;
            _scenario = null;

            foreach (var interactable in _interactables)
            {
                interactable.ResetInteraction();
            }

            foreach (var stateBinding in _stateBindings)
            {
                stateBinding.ResetState();
            }

            if (_player && _playerStart)
            {
                _player.Teleport(_playerStart.position, _playerStart.rotation);
            }

            ApplyInactiveState();
        }

        private void CacheSceneObjects()
        {
            _markers = FindObjectsByType<WorldStepMarker>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            _interactables = FindObjectsByType<WorldInteractable>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            _stateBindings = FindObjectsByType<SceneStateBinding>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        private void ApplyInactiveState()
        {
            if (_markers == null)
            {
                return;
            }

            foreach (var marker in _markers)
            {
                marker.Apply(StepStatus.Inactive);
            }
        }

        private void ApplyStepOrder()
        {
            if (_runtime == null || !_scenario || _markers == null)
            {
                return;
            }

            var profile = _scenario.GetProfile(_runtime.Runner.Mode);
            if (!profile)
            {
                return;
            }

            var orderByStepId = new Dictionary<string, int>();
            var order = 1;
            foreach (var stage in profile.ActivationPlan.Stages)
            {
                if (stage?.Steps == null)
                {
                    continue;
                }

                foreach (var step in stage.Steps)
                {
                    if (!step || orderByStepId.ContainsKey(step.StepId))
                    {
                        continue;
                    }

                    orderByStepId.Add(step.StepId, order);
                    order++;
                }
            }

            foreach (var marker in _markers)
            {
                if (orderByStepId.TryGetValue(marker.StepId, out var stepOrder))
                {
                    marker.ApplyOrder(stepOrder);
                }
            }
        }
    }
}
