using System;
using System.Collections.Generic;
using System.Linq;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Rules.Bindings
{
    public sealed class SceneBindingsValidationRule : IScenarioValidationRule
    {
        public IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            var scenario = context.Scenario;
            if (scenario == null)
            {
                yield break;
            }

            if (context.SceneBindings == null)
            {
                yield return new ValidationIssue(
                    "SCENE_BINDING_REGISTRY_MISSING",
                    ValidationSeverity.Error,
                    "A scene binding registry is required to run this scenario.",
                    scenario);
                yield break;
            }

            var bindingIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in context.SceneBindings.Bindings)
            {
                if (binding == null)
                {
                    yield return new ValidationIssue(
                        "SCENE_BINDING_REFERENCE_MISSING",
                        ValidationSeverity.Error,
                        "The scene binding registry contains an empty reference.",
                        context.SceneBindings);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.BindingId))
                {
                    yield return new ValidationIssue(
                        "SCENE_BINDING_ID_MISSING",
                        ValidationSeverity.Error,
                        $"Scene object '{binding.name}' does not have a binding ID.",
                        binding);
                    continue;
                }

                if (!bindingIds.Add(binding.BindingId))
                {
                    yield return new ValidationIssue(
                        "SCENE_BINDING_ID_DUPLICATE",
                        ValidationSeverity.Error,
                        $"Binding ID '{binding.BindingId}' is used by more than one scene object.",
                        binding);
                }
            }

            foreach (var step in scenario.Steps)
            {
                if (step == null ||
                    !(step.RuleDefinition is ISceneBindingRequirementProvider requirements))
                {
                    continue;
                }

                foreach (var bindingId in requirements
                             .GetRequiredBindingIds()
                             .Distinct(StringComparer.Ordinal))
                {
                    if (string.IsNullOrWhiteSpace(bindingId))
                    {
                        yield return new ValidationIssue(
                            "STEP_BINDING_ID_MISSING",
                            ValidationSeverity.Error,
                            $"Step '{step.DisplayName}' contains an empty scene binding ID.",
                            step);
                    }
                    else if (!bindingIds.Contains(bindingId))
                    {
                        yield return new ValidationIssue(
                            "STEP_BINDING_NOT_FOUND",
                            ValidationSeverity.Error,
                            $"Step '{step.DisplayName}' requires scene binding '{bindingId}', " +
                            "but it is not registered in the scene.",
                            step);
                    }
                }
            }
        }
    }
}
