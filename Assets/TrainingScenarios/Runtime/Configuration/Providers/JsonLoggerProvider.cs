using UnityEngine;
using TrainingScenarios.Runtime.Logging.Contracts;
using TrainingScenarios.Runtime.Logging.Persistence;

namespace TrainingScenarios.Runtime.Configuration.Providers
{
    [CreateAssetMenu(
        fileName = "JsonLoggerProvider",
        menuName = "Training Scenarios/Providers/JSON Logger")]
    public sealed class JsonLoggerProvider : LoggerProvider
    {
        public override IJournalStore CreateStore()
        {
            return new JsonJournalStore();
        }
    }
}
