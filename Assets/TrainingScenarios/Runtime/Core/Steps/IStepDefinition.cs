using TrainingScenarios.Runtime.Rules.Contracts;

namespace TrainingScenarios.Runtime.Core.Steps
{
    public interface IStepDefinition
    {
        string StepId { get; }

        IStepRuleDefinition RuleDefinition { get; }
    }
}
