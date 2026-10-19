using TrainingScenarios.Runtime.Rules;

namespace TrainingScenarios.Runtime.Core.Steps
{
    public interface IStepDefinition
    {
        string StepId { get; }

        IStepRuleDefinition RuleDefinition { get; }
    }
}
