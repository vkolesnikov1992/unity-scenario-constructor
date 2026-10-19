namespace TrainingScenarios.Runtime.Rules
{
    public interface IScenarioContext
    {
        bool TryGetState(string stateId, out string value);
    }
}
