using System;
using System.Linq;
using UnityEngine;
using TrainingScenarios.Demo.Interactions;
using TrainingScenarios.Runtime.Composition;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Core.Sessions;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Execution.Runner;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Demo.Presentation
{
    public sealed class DemoScenarioController : MonoBehaviour
    {
        [SerializeField]
        private ScenarioSceneContext _sceneContext;
        [SerializeField]
        private ScenarioDefinition _scenario;
        [SerializeField]
        private DemoScenarioView _view;
        [SerializeField]
        private DemoJournalController _journalController;
        [SerializeField]
        private DemoPhysicsDriver _physicsDriver;
        [SerializeField]
        private DemoWorldStateController _worldState;

        private ScenarioApplication _application;
        private ScenarioRuntime _runtime;
        private string _lastMessage;

        private void OnEnable()
        {
            _sceneContext.Initialized += HandleApplicationInitialized;

            if (_sceneContext.IsInitialized)
            {
                HandleApplicationInitialized(_sceneContext.Application);
            }
            else
            {
                _view.ShowReady();
            }
        }

        private void OnDisable()
        {
            _sceneContext.Initialized -= HandleApplicationInitialized;
            DetachApplication();
        }

        public void StartTraining()
        {
            StartScenario(ScenarioMode.Training);
        }

        public void StartExam()
        {
            StartScenario(ScenarioMode.Exam);
        }

        public void RestartScenario()
        {
            if (_application == null)
            {
                return;
            }

            DetachRuntime();
            _physicsDriver.ResetProbes();
            _worldState.ResetWorld();
            var result = _application.RestartScenario();
            _view.ShowDiagnostics(result.ValidationIssues);
        }

        private void StartScenario(ScenarioMode mode)
        {
            if (_application == null)
            {
                _view.ShowReady();
                return;
            }

            DetachRuntime();
            _physicsDriver.ResetProbes();
            _worldState.ResetWorld();
            var result = _application.StartMode(mode);
            _view.ShowDiagnostics(result.ValidationIssues);
        }

        private void HandleApplicationInitialized(ScenarioApplication application)
        {
            DetachApplication();
            _application = application;
            _scenario = _sceneContext.Scenario;
            _application.RuntimeStarted += HandleRuntimeStarted;
            _view.ShowReady();

            if (_application.CurrentRuntime != null)
            {
                HandleRuntimeStarted(_application.CurrentRuntime);
            }
        }

        private void HandleRuntimeStarted(ScenarioRuntime runtime)
        {
            DetachRuntime();
            _runtime = runtime;
            _runtime.Runner.Session.StepChanged += HandleStepChanged;
            _runtime.Runner.Session.StatusChanged += HandleSessionStatusChanged;
            _runtime.Runner.StepActionProcessed += HandleStepActionProcessed;
            _runtime.Completed += HandleRuntimeCompleted;
            _worldState.Bind(_runtime, _scenario);
            _lastMessage = _runtime.Runner.Mode == ScenarioMode.Training
                ? "Training started. Follow the current objective."
                : "Exam started. Complete the active objectives.";
            RefreshView();
        }

        private void HandleStepChanged(StepRuntime _)
        {
            RefreshView();
        }

        private void HandleSessionStatusChanged(ScenarioSessionStatus _)
        {
            RefreshView();
        }

        private void HandleStepActionProcessed(StepActionResult result)
        {
            var stepName = _scenario.Steps
                .First(step => step.StepId == result.StepId)
                .DisplayName;
            _lastMessage = BuildActionMessage(stepName, result);
            RefreshView();
        }

        private void HandleRuntimeCompleted(ScenarioRuntime runtime)
        {
            _lastMessage = runtime.LastSaveResult.Succeeded
                ? $"Session completed. Journal saved to {runtime.LastSaveResult.FilePath}"
                : $"Session completed. Journal save failed: " +
                  runtime.LastSaveResult.DiagnosticMessage;
            _journalController.RefreshJournalFiles();
            RefreshView();
        }

        private void RefreshView()
        {
            if (_runtime == null)
            {
                return;
            }

            _view.Render(_runtime, _scenario, _lastMessage);
            _journalController.ShowCurrentJournal(_runtime.Journal);
            _worldState.Refresh();
        }

        private void DetachApplication()
        {
            DetachRuntime();

            if (_application == null)
            {
                return;
            }

            _application.RuntimeStarted -= HandleRuntimeStarted;
            _application = null;
        }

        private void DetachRuntime()
        {
            if (_runtime == null)
            {
                return;
            }

            _runtime.Runner.Session.StepChanged -= HandleStepChanged;
            _runtime.Runner.Session.StatusChanged -= HandleSessionStatusChanged;
            _runtime.Runner.StepActionProcessed -= HandleStepActionProcessed;
            _runtime.Completed -= HandleRuntimeCompleted;
            _runtime = null;
        }

        private static string BuildActionMessage(
            string stepName,
            StepActionResult result)
        {
            switch (result.Evaluation.Outcome)
            {
                case StepEvaluationOutcome.Success:
                    return $"Completed: {stepName}.";
                case StepEvaluationOutcome.Progress:
                    return $"Progress: {stepName}. Continue with the next substep.";
                case StepEvaluationOutcome.Violation:
                    return $"Violation in {stepName}: {result.Evaluation.Reason}";
                default:
                    return $"Action received for {stepName}.";
            }
        }
    }
}
