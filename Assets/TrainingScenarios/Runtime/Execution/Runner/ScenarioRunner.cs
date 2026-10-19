using System;
using System.Collections.Generic;
using System.Linq;
using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Actions.Routing;
using TrainingScenarios.Runtime.Core.Sessions;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Modes.Contracts;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Rules.Contracts;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Execution.Runner
{
    public sealed class ScenarioRunner : IDisposable
    {
        private const string InactiveActionReason = "The action belongs to an inactive step.";

        private readonly ScenarioSession _session;
        private readonly ActionRouter _actionRouter;
        private readonly IScenarioContext _context;
        private readonly IScenarioModePolicy _modePolicy;
        private readonly InactiveActionBehavior _inactiveActionBehavior;

        private bool _isStarted;

        public ScenarioRunner(
            ScenarioSession session,
            ActionRouter actionRouter,
            IScenarioContext context,
            IScenarioModePolicy modePolicy,
            InactiveActionBehavior inactiveActionBehavior)
        {
            _session = session;
            _actionRouter = actionRouter;
            _context = context;
            _modePolicy = modePolicy;
            _inactiveActionBehavior = inactiveActionBehavior;
        }

        public event Action<ScenarioAction> ActionReceived;

        public event Action<StepActionEvaluation> StepEvaluated;

        public event Action<StepActionResult> StepActionProcessed;

        public ScenarioSession Session => _session;

        public ScenarioMode Mode => _modePolicy.Mode;

        public void Start()
        {
            if (_isStarted)
            {
                throw new InvalidOperationException("Scenario runner has already started.");
            }

            _isStarted = true;
            _session.StatusChanged += HandleSessionStatusChanged;
            _actionRouter.ActionReceived += ProcessAction;
            _modePolicy.Initialize(_session);
            _session.Start();
            _actionRouter.Start();
        }

        public void ProcessAction(ScenarioAction action)
        {
            if (_session.Status != ScenarioSessionStatus.Running)
            {
                throw new InvalidOperationException("Cannot process an action outside a running session.");
            }

            ActionReceived?.Invoke(action);

            var candidates = FindCandidates(action);
            foreach (var candidate in candidates)
            {
                if (candidate.Status == StepStatus.Inactive)
                {
                    ProcessInactiveAction(action, candidate.Step);
                    continue;
                }

                if (candidate.Step.Status != StepStatus.Active)
                {
                    continue;
                }

                var evaluation = candidate.Step.RuleRuntime.Evaluate(action, _context);
                StepEvaluated?.Invoke(new StepActionEvaluation(
                    action,
                    candidate.Step.DefinitionId,
                    evaluation,
                    candidate.Status));

                _modePolicy.ApplyEvaluation(_session, candidate.Step, evaluation);

                StepActionProcessed?.Invoke(new StepActionResult(
                    action,
                    candidate.Step.DefinitionId,
                    evaluation,
                    candidate.Status,
                    candidate.Step.Status));
            }
        }

        public void Stop()
        {
            if (!_isStarted)
            {
                return;
            }

            _actionRouter.Stop();
            _actionRouter.ActionReceived -= ProcessAction;
            _session.StatusChanged -= HandleSessionStatusChanged;
            _isStarted = false;
        }

        public void Dispose()
        {
            Stop();
        }

        private IReadOnlyList<StepCandidate> FindCandidates(ScenarioAction action)
        {
            return _session.Steps.Values
                .Where(step =>
                    !step.IsTerminal &&
                    step.RuleRuntime.IsRelated(action))
                .Select(step => new StepCandidate(step, step.Status))
                .ToArray();
        }

        private void ProcessInactiveAction(ScenarioAction action, StepRuntime step)
        {
            if (_inactiveActionBehavior == InactiveActionBehavior.Ignore)
            {
                return;
            }

            var evaluation = StepEvaluation.Violation(InactiveActionReason);
            StepEvaluated?.Invoke(new StepActionEvaluation(
                action,
                step.DefinitionId,
                evaluation,
                StepStatus.Inactive));

            StepActionProcessed?.Invoke(new StepActionResult(
                action,
                step.DefinitionId,
                evaluation,
                StepStatus.Inactive,
                StepStatus.Inactive));
        }

        private void HandleSessionStatusChanged(ScenarioSessionStatus status)
        {
            if (status == ScenarioSessionStatus.Completed)
            {
                Stop();
            }
        }

        private sealed class StepCandidate
        {
            public StepCandidate(StepRuntime step, StepStatus status)
            {
                Step = step;
                Status = status;
            }

            public StepRuntime Step { get; }

            public StepStatus Status { get; }
        }
    }
}
