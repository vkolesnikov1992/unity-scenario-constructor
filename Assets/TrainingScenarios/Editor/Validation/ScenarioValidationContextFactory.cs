using UnityEngine;
using TrainingScenarios.Runtime.Actions.Bindings;
using TrainingScenarios.Runtime.Composition;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Editor.Validation
{
    public static class ScenarioValidationContextFactory
    {
        public static ScenarioValidationContext Create(ScenarioDefinition scenario)
        {
            var contexts = Object.FindObjectsByType<ScenarioSceneContext>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var context in contexts)
            {
                if (context.Scenario == scenario)
                {
                    return new ScenarioValidationContext(scenario, context.SceneBindings);
                }
            }

            var registries = Object.FindObjectsByType<SceneBindingRegistry>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var registry = registries.Length == 1 ? registries[0] : null;
            return new ScenarioValidationContext(scenario, registry);
        }
    }
}
