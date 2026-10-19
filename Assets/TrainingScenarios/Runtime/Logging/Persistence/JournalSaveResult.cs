namespace TrainingScenarios.Runtime.Logging.Persistence
{
    public sealed class JournalSaveResult
    {
        public JournalSaveResult(bool succeeded, string filePath, string diagnosticMessage)
        {
            Succeeded = succeeded;
            FilePath = filePath;
            DiagnosticMessage = diagnosticMessage;
        }

        public bool Succeeded { get; }

        public string FilePath { get; }

        public string DiagnosticMessage { get; }
    }
}
