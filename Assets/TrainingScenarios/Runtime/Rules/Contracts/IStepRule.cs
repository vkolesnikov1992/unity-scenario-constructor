using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Rules.Contracts
{
    public interface IStepRule
    {
        StepEvaluation Evaluate(ScenarioAction action, IScenarioContext context);
    }
}
