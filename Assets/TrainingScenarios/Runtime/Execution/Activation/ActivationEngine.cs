using System;
using System.Collections.Generic;
using System.Linq;
using TrainingScenarios.Runtime.Core.Steps;

namespace TrainingScenarios.Runtime.Execution.Activation
{
    public sealed class ActivationEngine
    {
        private readonly ActivationPlan _plan;
        private readonly IReadOnlyDictionary<string, StepRuntime> _steps;

        public ActivationEngine(
            ActivationPlan plan,
            IReadOnlyDictionary<string, StepRuntime> steps)
        {
            ValidateRuntimeInputs(plan, steps);
            _plan = plan;
            _steps = steps;
        }

        public event Action<ActivationStageSnapshot> StageActivated;

        public event Action<DateTimeOffset> Completed;

        public ActivationEngineStatus Status { get; private set; } = ActivationEngineStatus.Created;

        public int CurrentStageIndex { get; private set; } = -1;

        public void Start(DateTimeOffset timestamp)
        {
            if (Status != ActivationEngineStatus.Created)
            {
                throw new InvalidOperationException(
                    $"Activation engine cannot start from status '{Status}'.");
            }

            Status = ActivationEngineStatus.Running;
            ActivateNextPendingStage(timestamp);
        }

        public void NotifyStepTerminal(string stepId, DateTimeOffset timestamp)
        {
            if (Status != ActivationEngineStatus.Running)
            {
                throw new InvalidOperationException(
                    $"Activation engine is not running. Current status: '{Status}'.");
            }

            if (!_steps.TryGetValue(stepId, out var step))
            {
                throw new KeyNotFoundException($"Unknown step ID '{stepId}'.");
            }

            if (!step.IsTerminal)
            {
                throw new InvalidOperationException(
                    $"Step '{stepId}' is not in a terminal status.");
            }

            if (CurrentStageIndex >= 0 && IsCurrentStageTerminal())
            {
                ActivateNextPendingStage(timestamp);
            }
        }

        private void ActivateNextPendingStage(DateTimeOffset timestamp)
        {
            while (TryMoveToNextStage())
            {
                var stage = _plan.Stages[CurrentStageIndex];

                foreach (var stepId in stage.StepIds)
                {
                    var step = _steps[stepId];
                    if (step.Status == StepStatus.Inactive)
                    {
                        step.Activate(timestamp);
                    }
                }

                if (IsCurrentStageTerminal())
                {
                    continue;
                }

                StageActivated?.Invoke(new ActivationStageSnapshot(CurrentStageIndex, stage.StepIds));
                return;
            }

            Status = ActivationEngineStatus.Completed;
            Completed?.Invoke(timestamp);
        }

        private bool TryMoveToNextStage()
        {
            var nextStageIndex = CurrentStageIndex + 1;
            if (nextStageIndex >= _plan.Stages.Count)
            {
                return false;
            }

            CurrentStageIndex = nextStageIndex;
            return true;
        }

        private bool IsCurrentStageTerminal()
        {
            return _plan.Stages[CurrentStageIndex].StepIds.All(stepId => _steps[stepId].IsTerminal);
        }

        private static void ValidateRuntimeInputs(
            ActivationPlan plan,
            IReadOnlyDictionary<string, StepRuntime> steps)
        {
            if (steps.Count == 0)
            {
                throw new ArgumentException("Activation requires at least one step.", nameof(steps));
            }

            if (plan.Stages.Count == 0)
            {
                throw new ArgumentException("Activation plan must contain at least one stage.", nameof(plan));
            }

            var plannedStepIds = new HashSet<string>(StringComparer.Ordinal);

            for (var stageIndex = 0; stageIndex < plan.Stages.Count; stageIndex++)
            {
                var stage = plan.Stages[stageIndex];
                if (stage.StepIds.Count == 0)
                {
                    throw new ArgumentException(
                        $"Activation stage {stageIndex + 1} is empty.",
                        nameof(plan));
                }

                foreach (var stepId in stage.StepIds)
                {
                    if (!steps.ContainsKey(stepId))
                    {
                        throw new ArgumentException(
                            $"Activation plan contains unknown step '{stepId}'.",
                            nameof(plan));
                    }

                    if (!plannedStepIds.Add(stepId))
                    {
                        throw new ArgumentException(
                            $"Step '{stepId}' appears in more than one activation stage.",
                            nameof(plan));
                    }
                }
            }

            var missingStepIds = steps.Keys.Where(stepId => !plannedStepIds.Contains(stepId)).ToArray();
            if (missingStepIds.Length > 0)
            {
                throw new ArgumentException(
                    $"Activation plan does not contain steps: {string.Join(", ", missingStepIds)}.",
                    nameof(plan));
            }
        }
    }
}
