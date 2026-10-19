using System.Collections.Generic;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Configuration.Flow;
using TrainingScenarios.Runtime.Configuration.Providers;
using TrainingScenarios.Runtime.Execution.Modes.Contracts;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Logging.Snapshots;

namespace TrainingScenarios.Runtime.Configuration.Profiles
{
    public abstract class ScenarioModeProfile : ScriptableObject
    {
        [SerializeField]
        private ActivationPlanConfiguration _activationPlan =
            new ActivationPlanConfiguration();
        [SerializeField]
        private InactiveActionBehavior _inactiveActionBehavior =
            InactiveActionBehavior.RecordViolation;
        [SerializeField]
        private AnalyzerProvider _analyzerProvider;

        public abstract ScenarioMode Mode { get; }

        public ActivationPlanConfiguration ActivationPlan => _activationPlan;

        public InactiveActionBehavior InactiveActionBehavior => _inactiveActionBehavior;

        public AnalyzerProvider AnalyzerProvider => _analyzerProvider;

        public abstract IScenarioModePolicy CreatePolicy();

        public abstract SessionSnapshot CreateSnapshot(
            IReadOnlyList<StepDefinition> steps);
    }
}
