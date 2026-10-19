namespace TrainingScenarios.Runtime.Rules.Contracts
{
    public interface IScenarioContext
    {
        bool TryGetState(string stateId, out string value);
    }
}
