using System;
using System.Collections.Generic;
using UnityEngine;
using TrainingScenarios.Runtime.Execution.Activation;

namespace TrainingScenarios.Runtime.Configuration.Flow
{
    [Serializable]
    public sealed class ActivationPlanConfiguration
    {
        [SerializeField]
        private List<ActivationStageConfiguration> _stages =
            new List<ActivationStageConfiguration>();

        public IReadOnlyList<ActivationStageConfiguration> Stages => _stages;

        public ActivationPlan CreateRuntime()
        {
            var stages = new List<ActivationStage>(_stages.Count);
            foreach (var stage in _stages)
            {
                stages.Add(stage.CreateRuntime());
            }

            return new ActivationPlan(stages);
        }
    }
}
