using TrainingScenarios.Runtime.Logging.Contracts;
using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Runtime.Logging.Sinks
{
    public sealed class MemoryEventSink : IEventSink
    {
        private readonly SessionJournal _journal;

        public MemoryEventSink(SessionJournal journal)
        {
            _journal = journal;
        }

        public void Append(ScenarioEvent entry)
        {
            _journal.Append(entry);
        }
    }
}
