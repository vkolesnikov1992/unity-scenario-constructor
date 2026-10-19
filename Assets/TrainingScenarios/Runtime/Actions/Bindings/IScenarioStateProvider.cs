namespace TrainingScenarios.Runtime.Actions.Bindings
{
    public interface IScenarioStateProvider
    {
        string StateId { get; }

        string CurrentValue { get; }

        void ResetState();
    }
}
