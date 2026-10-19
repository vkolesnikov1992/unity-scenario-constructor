using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Rules.Contracts
{
    public interface IStepRule
    {
        bool IsRelated(ScenarioAction action);

        StepEvaluation Evaluate(ScenarioAction action, IScenarioContext context);
    }
}
