using TrainingScenarios.Runtime.Core.Sessions;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Modes.Contracts;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Execution.Modes.Training
{
    public sealed class TrainingModePolicy : IScenarioModePolicy
    {
        public ScenarioMode Mode => ScenarioMode.Training;

        public void Initialize(ScenarioSession session)
        {
        }

        public void ApplyEvaluation(
            ScenarioSession session,
            StepRuntime step,
            StepEvaluation evaluation)
        {
            switch (evaluation.Outcome)
            {
                case StepEvaluationOutcome.Success:
                    session.RecordAttempt(step.DefinitionId);
                    session.CompleteStep(step.DefinitionId);
                    break;
                case StepEvaluationOutcome.Violation:
                    session.RecordAttempt(step.DefinitionId);
                    session.RecordViolation(step.DefinitionId);
                    break;
            }
        }
    }
}
