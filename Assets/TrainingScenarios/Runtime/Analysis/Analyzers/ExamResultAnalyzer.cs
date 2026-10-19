using System;
using System.Collections.Generic;
using System.Linq;
using TrainingScenarios.Runtime.Analysis.Contracts;
using TrainingScenarios.Runtime.Analysis.Models;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Runtime.Analysis.Analyzers
{
    public sealed class ExamResultAnalyzer : IResultAnalyzer
    {
        public string AnalyzerId => "exam-v1";

        public ScenarioMode SupportedMode => ScenarioMode.Exam;

        public AnalysisResult Analyze(SessionJournal journal)
        {
            var warnings = new List<string>();
            if (!TryValidateCompatibility(journal, warnings))
            {
                return CreateIncompatibleResult(warnings);
            }

            var scoring = journal.Snapshot.ExamScoring;
            var knownStepIds = new HashSet<string>(
                journal.Snapshot.Steps.Select(step => step.StepId),
                StringComparer.Ordinal);
            var failedStepIds = GetTerminalStepIds(journal, ScenarioEventType.StepFailed);
            var skippedStepIds = GetTerminalStepIds(journal, ScenarioEventType.StepSkipped);

            foreach (var stepId in failedStepIds.Concat(skippedStepIds).Distinct())
            {
                if (!knownStepIds.Contains(stepId))
                {
                    warnings.Add($"Terminal event references unknown step '{stepId}'.");
                }
            }

            foreach (var stepId in failedStepIds.Intersect(skippedStepIds))
            {
                warnings.Add(
                    $"Step '{stepId}' contains both Failed and Skipped events; " +
                    "the Failed penalty was used.");
            }

            skippedStepIds.ExceptWith(failedStepIds);

            var penalty =
                failedStepIds.Count * scoring.FailedStepPenalty +
                skippedStepIds.Count * scoring.SkippedStepPenalty;
            var score = Math.Max(0, scoring.MaximumScore - penalty);

            if (journal.Snapshot.ExamGroups.Count == 0)
            {
                warnings.Add("The exam snapshot does not contain exam groups.");
            }

            if (!journal.IsCompleted)
            {
                warnings.Add("The journal does not contain SessionCompleted.");
            }

            return new AnalysisResult(
                AnalyzerId,
                true,
                score,
                scoring.MaximumScore,
                $"Exam result: {score} of {scoring.MaximumScore}; total penalty: {penalty}.",
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

            if (journal.Snapshot.ExamScoring == null)
            {
                warnings.Add("The journal snapshot does not contain exam scoring settings.");
                return false;
            }

            if (journal.Snapshot.Steps.Count == 0)
            {
                warnings.Add("The journal snapshot does not contain steps.");
                return false;
            }

            return true;
        }

        private static HashSet<string> GetTerminalStepIds(
            SessionJournal journal,
            ScenarioEventType eventType)
        {
            return new HashSet<string>(
                journal.Events
                    .Where(entry =>
                        entry != null &&
                        string.Equals(
                            entry.EventType,
                            eventType.ToString(),
                            StringComparison.Ordinal))
                    .Select(entry => entry.StepId),
                StringComparer.Ordinal);
        }

        private AnalysisResult CreateIncompatibleResult(IEnumerable<string> warnings)
        {
            return new AnalysisResult(
                AnalyzerId,
                false,
                0,
                0,
                "The exam analyzer is not compatible with this journal.",
                warnings);
        }
    }
}
