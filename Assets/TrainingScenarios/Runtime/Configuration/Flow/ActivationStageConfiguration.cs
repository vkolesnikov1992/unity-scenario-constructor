using System;
using System.Collections.Generic;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Execution.Activation;

namespace TrainingScenarios.Runtime.Configuration.Flow
{
    [Serializable]
    public sealed class ActivationStageConfiguration
    {
        [SerializeField]
        private List<StepDefinition> _steps = new List<StepDefinition>();

        public IReadOnlyList<StepDefinition> Steps => _steps;

        public ActivationStage CreateRuntime()
        {
            var stepIds = new List<string>(_steps.Count);
            foreach (var step in _steps)
            {
                stepIds.Add(step.StepId);
            }

            return new ActivationStage(stepIds);
        }
    }
}
