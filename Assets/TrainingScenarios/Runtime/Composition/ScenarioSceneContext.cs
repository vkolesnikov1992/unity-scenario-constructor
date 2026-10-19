using System;
using UnityEngine;
using TrainingScenarios.Runtime.Actions.Bindings;
using TrainingScenarios.Runtime.Configuration.Definitions;

namespace TrainingScenarios.Runtime.Composition
{
    [DisallowMultipleComponent]
    public sealed class ScenarioSceneContext : MonoBehaviour
    {
        [SerializeField]
        private ScenarioDefinition _scenario;
        [SerializeField]
        private SceneBindingRegistry _sceneBindings;

        public event Action<ScenarioApplication> Initialized;

        public ScenarioDefinition Scenario => _scenario;

        public SceneBindingRegistry SceneBindings => _sceneBindings;

        public ScenarioApplication Application { get; private set; }

        public bool IsInitialized => Application != null;

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            Application = new ScenarioApplication(_scenario, _sceneBindings);
            Initialized?.Invoke(Application);
        }

        private void OnDestroy()
        {
            Application?.Dispose();
            Application = null;
        }
    }
}
