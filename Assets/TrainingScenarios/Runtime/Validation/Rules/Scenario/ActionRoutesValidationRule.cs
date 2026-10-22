using System;
using System.Collections.Generic;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Rules.Scenario
{
    public sealed class ActionRoutesValidationRule : IScenarioValidationRule
    {
        private const char RouteSeparator = '\u001f';

        public IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            if (!context.Scenario)
            {
                yield break;
            }

            var ownerByRoute = new Dictionary<string, StepDefinition>(StringComparer.Ordinal);
            var reportedConflicts = new HashSet<string>(StringComparer.Ordinal);

            foreach (var step in context.Scenario.Steps)
            {
                if (!step || !(step.RuleDefinition is IActionRequirementProvider provider))
                {
                    continue;
                }

                foreach (var requirement in provider.GetActionRequirements())
                {
                    if (requirement == null ||
                        string.IsNullOrWhiteSpace(requirement.SourceId) ||
                        string.IsNullOrWhiteSpace(requirement.TargetId))
                    {
                        continue;
                    }

                    var route = requirement.SourceId + RouteSeparator + requirement.TargetId;
                    if (!ownerByRoute.TryGetValue(route, out var existingStep))
                    {
                        ownerByRoute.Add(route, step);
                        continue;
                    }

                    if (existingStep == step || !reportedConflicts.Add(route))
                    {
                        continue;
                    }

                    yield return new ValidationIssue(
                        "ACTION_ROUTE_AMBIGUOUS",
                        ValidationSeverity.Error,
                        $"Steps '{existingStep.DisplayName}' and '{step.DisplayName}' use the " +
                        $"same action route '{requirement.SourceId}' -> '{requirement.TargetId}'. " +
                        "Each source/target route must belong to one step.",
                        step);
                }
            }
        }
    }
}
