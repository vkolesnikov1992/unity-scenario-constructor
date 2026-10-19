using TrainingScenarios.Runtime.Analysis.Contracts;
using TrainingScenarios.Runtime.Analysis.Models;
using TrainingScenarios.Runtime.Logging.Contracts;

namespace TrainingScenarios.Runtime.Analysis.Services
{
    public sealed class JournalReanalysisService
    {
        private readonly IJournalStore _journalStore;

        public JournalReanalysisService(IJournalStore journalStore)
        {
            _journalStore = journalStore;
        }

        public AnalysisResult Analyze(string filePath, IResultAnalyzer analyzer)
        {
            var loadResult = _journalStore.Load(filePath);
            if (!loadResult.Succeeded)
            {
                return new AnalysisResult(
                    analyzer.AnalyzerId,
                    false,
                    0,
                    0,
                    "The journal could not be loaded for analysis.",
                    new[] { loadResult.DiagnosticMessage });
            }

            return analyzer.Analyze(loadResult.Journal);
        }
    }
}
