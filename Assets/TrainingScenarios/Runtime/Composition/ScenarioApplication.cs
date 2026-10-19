using System;
using System.Collections.Generic;
using TrainingScenarios.Runtime.Actions.Bindings;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Composition
{
    public sealed class ScenarioApplication : IDisposable
    {
        private readonly ScenarioDefinition _scenario;
        private readonly SceneBindingRegistry _sceneBindings;
        private readonly ScenarioRuntimeFactory _runtimeFactory;

        private ScenarioMode _currentMode = ScenarioMode.Training;

        public ScenarioApplication(
            ScenarioDefinition scenario,
            SceneBindingRegistry sceneBindings)
            : this(scenario, sceneBindings, new ScenarioRuntimeFactory())
        {
        }

        public ScenarioApplication(
            ScenarioDefinition scenario,
            SceneBindingRegistry sceneBindings,
            ScenarioRuntimeFactory runtimeFactory)
        {
            _scenario = scenario;
            _sceneBindings = sceneBindings;
            _runtimeFactory = runtimeFactory;
        }

        public event Action<ScenarioRuntime> RuntimeStarted;

        public ScenarioRuntime CurrentRuntime { get; private set; }

        public IReadOnlyList<ValidationIssue> LastValidationIssues { get; private set; } =
            Array.Empty<ValidationIssue>();

        public ScenarioRuntimeCreationResult StartTraining()
        {
            return StartMode(ScenarioMode.Training);
        }

        public ScenarioRuntimeCreationResult StartExam()
        {
            return StartMode(ScenarioMode.Exam);
        }

        public ScenarioRuntimeCreationResult RestartScenario()
        {
            return StartMode(_currentMode);
        }

        public ScenarioRuntimeCreationResult StartMode(ScenarioMode mode)
        {
            DisposeCurrentRuntime();
            _currentMode = mode;

            var creationResult = _runtimeFactory.Create(
                _scenario,
                mode,
                _sceneBindings);
            LastValidationIssues = creationResult.ValidationIssues;

            if (creationResult.HasErrors)
            {
                return creationResult;
            }

            _sceneBindings.ResetState();
            CurrentRuntime = creationResult.Runtime;
            CurrentRuntime.Start();
            RuntimeStarted?.Invoke(CurrentRuntime);
            return creationResult;
        }

        public void Dispose()
        {
            DisposeCurrentRuntime();
        }

        private void DisposeCurrentRuntime()
        {
            CurrentRuntime?.Dispose();
            CurrentRuntime = null;
        }
    }
}
