using System.Collections.Generic;
using TrainingScenarios.Runtime.Configuration.Profiles;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Rules.Providers
{
    public sealed class ProviderValidationRule : IScenarioValidationRule
    {
        public IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            var scenario = context.Scenario;
            if (!scenario)
            {
                yield break;
            }

            if (!scenario.LoggerProvider)
            {
                yield return new ValidationIssue(
                    "LOGGER_PROVIDER_MISSING",
                    ValidationSeverity.Error,
                    "The scenario does not have a logger provider.",
                    scenario);
            }

            foreach (var issue in ValidateProfile(
                         scenario.TrainingProfile,
                         ScenarioMode.Training,
                         scenario))
            {
                yield return issue;
            }

            foreach (var issue in ValidateProfile(
                         scenario.ExamProfile,
                         ScenarioMode.Exam,
                         scenario))
            {
                yield return issue;
            }
        }

        private static IEnumerable<ValidationIssue> ValidateProfile(
            ScenarioModeProfile profile,
            ScenarioMode expectedMode,
            UnityEngine.Object scenarioContext)
        {
            if (!profile)
            {
                yield return new ValidationIssue(
                    "MODE_PROFILE_MISSING",
                    ValidationSeverity.Error,
                    $"The scenario does not have a {expectedMode} profile.",
                    scenarioContext);
                yield break;
            }

            if (!profile.AnalyzerProvider)
            {
                yield return new ValidationIssue(
                    "ANALYZER_PROVIDER_MISSING",
                    ValidationSeverity.Error,
                    $"The {expectedMode} profile does not have an analyzer provider.",
                    profile);
                yield break;
            }

            if (profile.AnalyzerProvider.SupportedMode != expectedMode)
            {
                yield return new ValidationIssue(
                    "ANALYZER_PROVIDER_INCOMPATIBLE",
                    ValidationSeverity.Error,
                    $"Analyzer '{profile.AnalyzerProvider.name}' does not support " +
                    $"the {expectedMode} profile.",
                    profile.AnalyzerProvider);
            }
        }
    }
}
