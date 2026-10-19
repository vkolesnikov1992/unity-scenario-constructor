using UnityEngine;
using TrainingScenarios.Runtime.Analysis.Analyzers;
using TrainingScenarios.Runtime.Analysis.Contracts;
using TrainingScenarios.Runtime.Execution.Modes.Models;

namespace TrainingScenarios.Runtime.Configuration.Providers
{
    [CreateAssetMenu(
        fileName = "ExamAnalyzerProvider",
        menuName = "Training Scenarios/Providers/Exam Analyzer")]
    public sealed class ExamAnalyzerProvider : AnalyzerProvider
    {
        public override ScenarioMode SupportedMode => ScenarioMode.Exam;

        public override IResultAnalyzer CreateAnalyzer()
        {
            return new ExamResultAnalyzer();
        }
    }
}
