using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TrainingScenarios.Runtime.Analysis.Models
{
    public sealed class AnalysisResult
    {
        public AnalysisResult(
            string analyzerId,
            bool isCompatible,
            int score,
            int maximumScore,
            string summary,
            IEnumerable<string> warnings)
        {
            AnalyzerId = analyzerId;
            IsCompatible = isCompatible;
            Score = score;
            MaximumScore = maximumScore;
            Summary = summary;
            Warnings = new ReadOnlyCollection<string>(warnings.ToList());
        }

        public string AnalyzerId { get; }

        public bool IsCompatible { get; }

        public int Score { get; }

        public int MaximumScore { get; }

        public string Summary { get; }

        public IReadOnlyList<string> Warnings { get; }
    }
}
