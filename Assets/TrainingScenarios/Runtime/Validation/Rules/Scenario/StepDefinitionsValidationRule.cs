using System;
using System.Collections.Generic;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Rules.Scenario
{
    public sealed class StepDefinitionsValidationRule : IScenarioValidationRule
    {
        public IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            var scenario = context.Scenario;
            if (scenario == null)
            {
                yield break;
            }

            if (scenario.Steps.Count == 0)
            {
                yield return new ValidationIssue(
                    "SCENARIO_STEPS_EMPTY",
                    ValidationSeverity.Error,
                    "The scenario does not contain any steps.",
                    scenario);
                yield break;
            }

            var stepIds = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < scenario.Steps.Count; index++)
            {
                var step = scenario.Steps[index];
                if (step == null)
                {
                    yield return new ValidationIssue(
                        "STEP_REFERENCE_MISSING",
                        ValidationSeverity.Error,
                        $"Scenario step position {index + 1} does not reference a step asset.",
                        scenario);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(step.StepId))
                {
                    yield return new ValidationIssue(
                        "STEP_ID_MISSING",
                        ValidationSeverity.Error,
                        $"Step '{step.DisplayName}' does not have a stable ID.",
                        step);
                }
                else if (!stepIds.Add(step.StepId))
                {
                    yield return new ValidationIssue(
                        "STEP_ID_DUPLICATE",
                        ValidationSeverity.Error,
                        $"Step ID '{step.StepId}' is used by more than one step.",
                        step);
                }

                if (step.RuleDefinition == null)
                {
                    yield return new ValidationIssue(
                        "STEP_RULE_MISSING",
                        ValidationSeverity.Error,
                        $"Step '{step.DisplayName}' does not have a completion rule.",
                        step);
                }
            }
        }
    }
}
