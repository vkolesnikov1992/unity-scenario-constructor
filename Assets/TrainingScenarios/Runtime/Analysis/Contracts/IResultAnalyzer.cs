using TrainingScenarios.Runtime.Analysis.Models;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Runtime.Analysis.Contracts
{
    public interface IResultAnalyzer
    {
        string AnalyzerId { get; }

        ScenarioMode SupportedMode { get; }

        AnalysisResult Analyze(SessionJournal journal);
    }
}
