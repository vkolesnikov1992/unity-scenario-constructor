using TrainingScenarios.Runtime.Actions.Bindings;
using TrainingScenarios.Runtime.Configuration.Definitions;

namespace TrainingScenarios.Runtime.Validation.Models
{
    public sealed class ScenarioValidationContext
    {
        public ScenarioValidationContext(
            ScenarioDefinition scenario,
            SceneBindingRegistry sceneBindings = null)
        {
            Scenario = scenario;
            SceneBindings = sceneBindings;
        }

        public ScenarioDefinition Scenario { get; }

        public SceneBindingRegistry SceneBindings { get; }
    }
}
