using System.Collections.Generic;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Rules.Scenario
{
    public sealed class ScenarioIdentityValidationRule : IScenarioValidationRule
    {
        public IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            if (!context.Scenario)
            {
                yield return new ValidationIssue(
                    "SCENARIO_MISSING",
                    ValidationSeverity.Error,
                    "A scenario definition is not assigned.",
                    null);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(context.Scenario.ScenarioId))
            {
                yield return new ValidationIssue(
                    "SCENARIO_ID_MISSING",
                    ValidationSeverity.Error,
                    "The scenario does not have a stable ID.",
                    context.Scenario);
            }
        }
    }
}
