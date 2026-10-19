using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Execution.Runner
{
    public sealed class StepActionResult
    {
        public StepActionResult(
            ScenarioAction action,
            string stepId,
            StepEvaluation evaluation,
            StepStatus previousStatus,
            StepStatus currentStatus)
        {
            Action = action;
            StepId = stepId;
            Evaluation = evaluation;
            PreviousStatus = previousStatus;
            CurrentStatus = currentStatus;
        }

        public ScenarioAction Action { get; }

        public string StepId { get; }

        public StepEvaluation Evaluation { get; }

        public StepStatus PreviousStatus { get; }

        public StepStatus CurrentStatus { get; }
    }
}
