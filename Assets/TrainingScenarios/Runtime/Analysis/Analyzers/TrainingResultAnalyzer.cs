using System;
using System.Collections.Generic;
using System.Linq;
using TrainingScenarios.Runtime.Analysis.Contracts;
using TrainingScenarios.Runtime.Analysis.Models;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Runtime.Analysis.Analyzers
{
    public sealed class TrainingResultAnalyzer : IResultAnalyzer
    {
        public string AnalyzerId => "training-v1";

        public ScenarioMode SupportedMode => ScenarioMode.Training;

        public AnalysisResult Analyze(SessionJournal journal)
        {
            var warnings = new List<string>();
            if (!TryValidateCompatibility(journal, warnings))
            {
                return CreateIncompatibleResult(warnings);
            }

            var snapshot = journal.Snapshot;
            var scoring = snapshot.TrainingScoring;
            var score = 0;

            foreach (var step in snapshot.Steps)
            {
                var completion = journal.Events.LastOrDefault(entry =>
                    entry != null &&
                    string.Equals(entry.StepId, step.StepId, StringComparison.Ordinal) &&
                    string.Equals(
                        entry.EventType,
                        ScenarioEventType.StepCompleted.ToString(),
                        StringComparison.Ordinal));

                if (completion == null)
                {
                    warnings.Add($"Step '{step.StepId}' has no completion event and received 0 points.");
                    continue;
                }

                switch (completion.AttemptNumber)
                {
                    case 1:
                        score += scoring.FirstAttemptScore;
                        break;
                    case 2:
                        score += scoring.SecondAttemptScore;
                        break;
                    default:
                        score += scoring.LaterAttemptScore;
                        break;
                }

                if (completion.AttemptNumber <= 0)
                {
                    warnings.Add(
                        $"Step '{step.StepId}' has an invalid attempt number " +
                        $"({completion.AttemptNumber}).");
                }
            }

            if (!journal.IsCompleted)
            {
                warnings.Add("The journal does not contain SessionCompleted.");
            }

            var maximumScore = scoring.MaximumPerStep * snapshot.Steps.Count;
            return new AnalysisResult(
                AnalyzerId,
                true,
                score,
                maximumScore,
                $"Training result: {score} of {maximumScore}.",
                warnings);
        }

        private bool TryValidateCompatibility(
            SessionJournal journal,
            ICollection<string> warnings)
        {
            if (journal.Snapshot == null)
            {
                warnings.Add("The journal does not contain a session snapshot.");
                return false;
            }

            if (journal.SchemaVersion != JournalSchema.CurrentVersion ||
                journal.Snapshot.SchemaVersion != JournalSchema.CurrentVersion)
            {
                warnings.Add(
                    $"Unsupported journal schema. Expected {JournalSchema.CurrentVersion}, " +
                    $"received journal {journal.SchemaVersion} and snapshot " +
                    $"{journal.Snapshot.SchemaVersion}.");
                return false;
            }

            if (!string.Equals(
                    journal.Snapshot.Mode,
                    SupportedMode.ToString(),
                    StringComparison.Ordinal))
            {
                warnings.Add(
                    $"Analyzer '{AnalyzerId}' supports {SupportedMode}, " +
                    $"but the journal mode is '{journal.Snapshot.Mode}'.");
                return false;
            }

            if (journal.Snapshot.TrainingScoring == null)
            {
                warnings.Add("The journal snapshot does not contain training scoring settings.");
                return false;
            }

            if (journal.Snapshot.Steps.Count == 0)
            {
                warnings.Add("The journal snapshot does not contain steps.");
                return false;
            }

            return true;
        }

        private AnalysisResult CreateIncompatibleResult(IEnumerable<string> warnings)
        {
            return new AnalysisResult(
                AnalyzerId,
                false,
                0,
                0,
                "The training analyzer is not compatible with this journal.",
                warnings);
        }
    }
}
