using System.Collections.Generic;
using TrainingScenarios.Runtime.Configuration.Profiles;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Rules.Scoring
{
    public sealed class ScoringValidationRule : IScenarioValidationRule
    {
        public IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            var scenario = context.Scenario;
            if (!scenario)
            {
                yield break;
            }

            if (scenario.TrainingProfile)
            {
                foreach (var issue in ValidateTraining(scenario.TrainingProfile))
                {
                    yield return issue;
                }
            }

            if (scenario.ExamProfile)
            {
                foreach (var issue in ValidateExam(scenario.ExamProfile))
                {
                    yield return issue;
                }
            }
        }

        private static IEnumerable<ValidationIssue> ValidateTraining(
            TrainingModeProfile profile)
        {
            if (profile.MaximumPerStep <= 0)
            {
                yield return new ValidationIssue(
                    "TRAINING_MAXIMUM_SCORE_INVALID",
                    ValidationSeverity.Error,
                    "The training maximum score per step must be greater than zero.",
                    profile);
            }

            var scores = new[]
            {
                (Name: "first attempt", Value: profile.FirstAttemptScore),
                (Name: "second attempt", Value: profile.SecondAttemptScore),
                (Name: "third and later attempts", Value: profile.LaterAttemptScore)
            };

            foreach (var score in scores)
            {
                if (score.Value < 0)
                {
                    yield return new ValidationIssue(
                        "TRAINING_ATTEMPT_SCORE_NEGATIVE",
                        ValidationSeverity.Error,
                        $"The training score for the {score.Name} cannot be negative.",
                        profile);
                }
                else if (profile.MaximumPerStep > 0 &&
                         score.Value > profile.MaximumPerStep)
                {
                    yield return new ValidationIssue(
                        "TRAINING_ATTEMPT_SCORE_EXCEEDS_MAXIMUM",
                        ValidationSeverity.Error,
                        $"The training score for the {score.Name} cannot exceed the maximum per step.",
                        profile);
                }
            }
        }

        private static IEnumerable<ValidationIssue> ValidateExam(ExamModeProfile profile)
        {
            if (profile.MaximumScore <= 0)
            {
                yield return new ValidationIssue(
                    "EXAM_MAXIMUM_SCORE_INVALID",
                    ValidationSeverity.Error,
                    "The exam maximum score must be greater than zero.",
                    profile);
            }

            if (profile.FailedStepPenalty < 0)
            {
                yield return new ValidationIssue(
                    "EXAM_FAILED_STEP_PENALTY_NEGATIVE",
                    ValidationSeverity.Error,
                    "The exam failed-step penalty cannot be negative.",
                    profile);
            }

            if (profile.SkippedStepPenalty < 0)
            {
                yield return new ValidationIssue(
                    "EXAM_SKIPPED_STEP_PENALTY_NEGATIVE",
                    ValidationSeverity.Error,
                    "The exam skipped-step penalty cannot be negative.",
                    profile);
            }
        }
    }
}
