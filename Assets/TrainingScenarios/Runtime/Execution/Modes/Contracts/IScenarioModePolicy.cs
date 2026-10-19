using TrainingScenarios.Runtime.Core.Sessions;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Execution.Modes.Contracts
{
    public interface IScenarioModePolicy
    {
        ScenarioMode Mode { get; }

        void Initialize(ScenarioSession session);

        void ApplyEvaluation(
            ScenarioSession session,
            StepRuntime step,
            StepEvaluation evaluation);
    }
}
