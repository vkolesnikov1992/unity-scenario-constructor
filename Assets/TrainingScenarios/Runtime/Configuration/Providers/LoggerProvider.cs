using UnityEngine;
using TrainingScenarios.Runtime.Logging.Contracts;

namespace TrainingScenarios.Runtime.Configuration.Providers
{
    public abstract class LoggerProvider : ScriptableObject
    {
        public abstract IJournalStore CreateStore();
    }
}
