using System;
using TrainingScenarios.Runtime.Analysis.Contracts;
using TrainingScenarios.Runtime.Analysis.Models;
using TrainingScenarios.Runtime.Execution.Runner;
using TrainingScenarios.Runtime.Logging.Contracts;
using TrainingScenarios.Runtime.Logging.Models;
using TrainingScenarios.Runtime.Logging.Persistence;
using TrainingScenarios.Runtime.Logging.Recording;

namespace TrainingScenarios.Runtime.Composition
{
    public sealed class ScenarioRuntime : IDisposable
    {
        private readonly ScenarioRunner _runner;
        private readonly ScenarioJournalRecorder _recorder;
        private readonly IJournalStore _journalStore;
        private readonly IResultAnalyzer _analyzer;

        public ScenarioRuntime(
            ScenarioRunner runner,
            ScenarioJournalRecorder recorder,
            SessionJournal journal,
            IJournalStore journalStore,
            IResultAnalyzer analyzer)
        {
            _runner = runner;
            _recorder = recorder;
            _journalStore = journalStore;
            _analyzer = analyzer;
            Journal = journal;
            _recorder.JournalCompleted += HandleJournalCompleted;
        }

        public event Action<ScenarioRuntime> Completed;

        public ScenarioRunner Runner => _runner;

        public SessionJournal Journal { get; }

        public IJournalStore JournalStore => _journalStore;

        public IResultAnalyzer Analyzer => _analyzer;

        public JournalSaveResult LastSaveResult { get; private set; }

        public AnalysisResult LastAnalysisResult { get; private set; }

        public void Start()
        {
            _recorder.StartRecording();
            _runner.Start();
        }

        public void Dispose()
        {
            _recorder.JournalCompleted -= HandleJournalCompleted;
            _recorder.Dispose();
            _runner.Dispose();
        }

        private void HandleJournalCompleted(SessionJournal journal)
        {
            LastSaveResult = _journalStore.Save(journal);
            LastAnalysisResult = _analyzer.Analyze(journal);
            Completed?.Invoke(this);
        }
    }
}
