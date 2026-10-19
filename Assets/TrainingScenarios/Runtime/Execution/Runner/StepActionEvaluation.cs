using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Execution.Runner
{
    public sealed class StepActionEvaluation
    {
        public StepActionEvaluation(
            ScenarioAction action,
            string stepId,
            StepEvaluation evaluation,
            StepStatus stepStatus)
        {
            Action = action;
            StepId = stepId;
            Evaluation = evaluation;
            StepStatus = stepStatus;
        }

        public ScenarioAction Action { get; }

        public string StepId { get; }

        public StepEvaluation Evaluation { get; }

        public StepStatus StepStatus { get; }
    }
}
