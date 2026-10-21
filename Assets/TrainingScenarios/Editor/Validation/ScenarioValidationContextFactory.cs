using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            return Create(scenario, SceneManager.GetActiveScene());
        }

        public static ScenarioValidationContext Create(
            ScenarioDefinition scenario,
            Scene scene)
        {
            var contexts = Object.FindObjectsByType<ScenarioSceneContext>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(context => context.gameObject.scene == scene)
                .ToArray();

            if (contexts.Length == 1)
            {
                return new ScenarioValidationContext(scenario, contexts[0].SceneBindings);
            }

            var matchingContexts = contexts
                .Where(context => context.Scenario == scenario)
                .ToArray();
            if (matchingContexts.Length == 1)
            {
                return new ScenarioValidationContext(
                    scenario,
                    matchingContexts[0].SceneBindings);
            }

            var registries = Object.FindObjectsByType<SceneBindingRegistry>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(registry => registry.gameObject.scene == scene)
                .ToArray();
            var registry = registries.Length == 1 ? registries[0] : null;
            return new ScenarioValidationContext(scenario, registry);
        }
    }
}
