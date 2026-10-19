using TrainingScenarios.Runtime.Actions;

namespace TrainingScenarios.Runtime.Rules
{
    public interface IStepRule
    {
        StepEvaluation Evaluate(ScenarioAction action, IScenarioContext context);
    }
}
