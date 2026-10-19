using UnityEngine;
using TrainingScenarios.Runtime.Analysis.Analyzers;
using TrainingScenarios.Runtime.Analysis.Contracts;
using TrainingScenarios.Runtime.Execution.Modes.Models;

namespace TrainingScenarios.Runtime.Configuration.Providers
{
    [CreateAssetMenu(
        fileName = "TrainingAnalyzerProvider",
        menuName = "Training Scenarios/Providers/Training Analyzer")]
    public sealed class TrainingAnalyzerProvider : AnalyzerProvider
    {
        public override ScenarioMode SupportedMode => ScenarioMode.Training;

        public override IResultAnalyzer CreateAnalyzer()
        {
            return new TrainingResultAnalyzer();
        }
    }
}
