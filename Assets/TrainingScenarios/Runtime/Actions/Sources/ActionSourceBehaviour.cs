using System;
using System.Collections.Generic;
using UnityEngine;
using TrainingScenarios.Runtime.Actions.Bindings;
using TrainingScenarios.Runtime.Actions.Contracts;
using TrainingScenarios.Runtime.Actions.Models;

namespace TrainingScenarios.Runtime.Actions.Sources
{
    public abstract class ActionSourceBehaviour : SceneBindingBehaviour, IActionSource
    {
        [SerializeField]
        private string _targetId;

        public string TargetId => _targetId;

        public event Action<ScenarioAction> ActionRaised;

        protected void RaiseAction(
            string actionType,
            IReadOnlyDictionary<string, string> additionalData = null)
        {
            var action = new ScenarioAction(
                BindingId,
                actionType,
                _targetId,
                DateTimeOffset.UtcNow,
                additionalData);

            ActionRaised?.Invoke(action);
        }
    }
}
