using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Activation;

namespace TrainingScenarios.Runtime.Core.Sessions
{
    public sealed class ScenarioSession
    {
        private readonly Func<DateTimeOffset> _utcNow;
        private readonly Dictionary<string, StepRuntime> _steps;
        private readonly ActivationEngine _activationEngine;

        internal ScenarioSession(
            string sessionId,
            string scenarioId,
            Dictionary<string, StepRuntime> steps,
            ActivationPlan activationPlan,
            Func<DateTimeOffset> utcNow)
        {
            SessionId = sessionId;
            ScenarioId = scenarioId;
            _steps = steps;
            _utcNow = utcNow;

            Steps = new ReadOnlyDictionary<string, StepRuntime>(_steps);
            _activationEngine = new ActivationEngine(activationPlan, _steps);
            _activationEngine.StageActivated += HandleStageActivated;
            _activationEngine.Completed += HandleActivationCompleted;
        }

        public event Action<StepRuntime> StepChanged;

        public event Action<ScenarioSessionStatus> StatusChanged;

        public string SessionId { get; }

        public string ScenarioId { get; }

        public ScenarioSessionStatus Status { get; private set; } = ScenarioSessionStatus.Created;

        public DateTimeOffset? StartedAt { get; private set; }

        public DateTimeOffset? CompletedAt { get; private set; }

        public IReadOnlyDictionary<string, StepRuntime> Steps { get; }

        public int CurrentStageIndex => _activationEngine.CurrentStageIndex;

        public void Start()
        {
            if (Status != ScenarioSessionStatus.Created)
            {
                throw new InvalidOperationException(
                    $"Session '{SessionId}' cannot start from status '{Status}'.");
            }

            var timestamp = _utcNow();
            StartedAt = timestamp;
            SetStatus(ScenarioSessionStatus.Running);
            _activationEngine.Start(timestamp);
        }

        public StepRuntime GetStep(string stepId)
        {
            return _steps[stepId];
        }

        public void RecordAttempt(string stepId)
        {
            EnsureRunning();
            var step = GetStep(stepId);
            step.RecordAttempt();
            StepChanged?.Invoke(step);
        }

        public void RecordViolation(string stepId)
        {
            EnsureRunning();
            var step = GetStep(stepId);
            step.RecordViolation();
            StepChanged?.Invoke(step);
        }

        public void RecordInactiveViolation(string stepId)
        {
            EnsureRunning();
            var step = GetStep(stepId);
            step.RecordInactiveViolation();
            StepChanged?.Invoke(step);
        }

        public void CompleteStep(string stepId)
        {
            SetTerminalStatus(stepId, StepStatus.Completed);
        }

        public void FailStep(string stepId)
        {
            SetTerminalStatus(stepId, StepStatus.Failed);
        }

        public void FailInactiveStep(string stepId)
        {
            EnsureRunning();
            var step = GetStep(stepId);
            var timestamp = _utcNow();
            step.FailInactive(timestamp);
            StepChanged?.Invoke(step);
            _activationEngine.NotifyStepTerminal(stepId, timestamp);
        }

        public bool SkipStep(string stepId)
        {
            EnsureRunning();
            var step = GetStep(stepId);
            var timestamp = _utcNow();

            if (!step.Skip(timestamp))
            {
                return false;
            }

            StepChanged?.Invoke(step);
            _activationEngine.NotifyStepTerminal(stepId, timestamp);
            return true;
        }

        private void SetTerminalStatus(string stepId, StepStatus terminalStatus)
        {
            EnsureRunning();
            var step = GetStep(stepId);
            var timestamp = _utcNow();

            switch (terminalStatus)
            {
                case StepStatus.Completed:
                    step.Complete(timestamp);
                    break;
                case StepStatus.Failed:
                    step.Fail(timestamp);
                    break;
            }

            StepChanged?.Invoke(step);
            _activationEngine.NotifyStepTerminal(stepId, timestamp);
        }

        private void HandleStageActivated(ActivationStageSnapshot stage)
        {
            foreach (var stepId in stage.StepIds)
            {
                StepChanged?.Invoke(_steps[stepId]);
            }
        }

        private void HandleActivationCompleted(DateTimeOffset timestamp)
        {
            CompletedAt = timestamp;
            SetStatus(ScenarioSessionStatus.Completed);
        }

        private void SetStatus(ScenarioSessionStatus status)
        {
            Status = status;
            StatusChanged?.Invoke(status);
        }

        private void EnsureRunning()
        {
            if (Status != ScenarioSessionStatus.Running)
            {
                throw new InvalidOperationException(
                    $"Session '{SessionId}' is not running. Current status: '{Status}'.");
            }
        }
    }
}
