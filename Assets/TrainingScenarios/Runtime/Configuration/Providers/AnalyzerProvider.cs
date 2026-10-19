using UnityEngine;
using TrainingScenarios.Runtime.Analysis.Contracts;
using TrainingScenarios.Runtime.Execution.Modes.Models;

namespace TrainingScenarios.Runtime.Configuration.Providers
{
    public abstract class AnalyzerProvider : ScriptableObject
    {
        public abstract ScenarioMode SupportedMode { get; }

        public abstract IResultAnalyzer CreateAnalyzer();
    }
}
