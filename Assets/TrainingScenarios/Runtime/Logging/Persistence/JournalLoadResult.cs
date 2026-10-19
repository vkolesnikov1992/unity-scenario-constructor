using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Runtime.Logging.Persistence
{
    public sealed class JournalLoadResult
    {
        public JournalLoadResult(
            bool succeeded,
            SessionJournal journal,
            string diagnosticMessage)
        {
            Succeeded = succeeded;
            Journal = journal;
            DiagnosticMessage = diagnosticMessage;
        }

        public bool Succeeded { get; }

        public SessionJournal Journal { get; }

        public string DiagnosticMessage { get; }
    }
}
