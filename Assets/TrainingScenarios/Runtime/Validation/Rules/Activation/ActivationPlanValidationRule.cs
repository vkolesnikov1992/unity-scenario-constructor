using System.Collections.Generic;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Configuration.Profiles;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Rules.Activation
{
    public sealed class ActivationPlanValidationRule : IScenarioValidationRule
    {
        public IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            var scenario = context.Scenario;
            if (scenario == null)
            {
                yield break;
            }

            foreach (var issue in ValidateProfile(scenario, scenario.TrainingProfile, "training"))
            {
                yield return issue;
            }

            foreach (var issue in ValidateProfile(scenario, scenario.ExamProfile, "exam"))
            {
                yield return issue;
            }
        }

        private static IEnumerable<ValidationIssue> ValidateProfile(
            ScenarioDefinition scenario,
            ScenarioModeProfile profile,
            string profileName)
        {
            if (profile == null)
            {
                yield break;
            }

            if (profile.ActivationPlan == null)
            {
                yield return new ValidationIssue(
                    "ACTIVATION_PLAN_MISSING",
                    ValidationSeverity.Error,
                    $"The {profileName} profile does not have an activation plan.",
                    profile);
                yield break;
            }

            if (profile.ActivationPlan.Stages.Count == 0)
            {
                yield return new ValidationIssue(
                    "ACTIVATION_STAGES_EMPTY",
                    ValidationSeverity.Error,
                    $"The {profileName} activation plan does not contain stages.",
                    profile);
                yield break;
            }

            var knownSteps = new HashSet<StepDefinition>();
            foreach (var step in scenario.Steps)
            {
                if (step != null)
                {
                    knownSteps.Add(step);
                }
            }

            var plannedSteps = new HashSet<StepDefinition>();

            for (var stageIndex = 0;
                 stageIndex < profile.ActivationPlan.Stages.Count;
                 stageIndex++)
            {
                var stage = profile.ActivationPlan.Stages[stageIndex];
                if (stage == null || stage.Steps.Count == 0)
                {
                    yield return new ValidationIssue(
                        "ACTIVATION_STAGE_EMPTY",
                        ValidationSeverity.Error,
                        $"Stage {stageIndex + 1} in the {profileName} plan is empty.",
                        profile);
                    continue;
                }

                for (var stepIndex = 0; stepIndex < stage.Steps.Count; stepIndex++)
                {
                    var step = stage.Steps[stepIndex];
                    if (step == null)
                    {
                        yield return new ValidationIssue(
                            "ACTIVATION_STEP_REFERENCE_MISSING",
                            ValidationSeverity.Error,
                            $"Stage {stageIndex + 1} in the {profileName} plan contains " +
                            "an empty step reference.",
                            profile);
                        continue;
                    }

                    if (!knownSteps.Contains(step))
                    {
                        yield return new ValidationIssue(
                            "ACTIVATION_STEP_UNKNOWN",
                            ValidationSeverity.Error,
                            $"Stage {stageIndex + 1} in the {profileName} plan contains " +
                            $"unknown step '{step.DisplayName}'.",
                            step);
                    }

                    if (!plannedSteps.Add(step))
                    {
                        yield return new ValidationIssue(
                            "ACTIVATION_STEP_DUPLICATE",
                            ValidationSeverity.Error,
                            $"Step '{step.DisplayName}' appears in more than one stage " +
                            $"of the {profileName} plan.",
                            step);
                    }
                }
            }

            foreach (var step in knownSteps)
            {
                if (!plannedSteps.Contains(step))
                {
                    yield return new ValidationIssue(
                        "ACTIVATION_STEP_NOT_PLANNED",
                        ValidationSeverity.Error,
                        $"Step '{step.DisplayName}' is not included in the " +
                        $"{profileName} activation plan.",
                        step);
                }
            }
        }
    }
}
