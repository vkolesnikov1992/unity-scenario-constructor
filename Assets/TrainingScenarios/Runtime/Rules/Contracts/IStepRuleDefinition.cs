namespace TrainingScenarios.Runtime.Rules.Contracts
{
    public interface IStepRuleDefinition
    {
        IStepRule CreateRuntime();
    }
}
