using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Runtime.Logging.Contracts
{
    public interface IEventSink
    {
        void Append(ScenarioEvent entry);
    }
}
