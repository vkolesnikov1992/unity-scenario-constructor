using System;
using TrainingScenarios.Runtime.Rules.Contracts;

namespace TrainingScenarios.Runtime.Core.Steps
{
    public sealed class StepRuntime
    {
        public StepRuntime(string definitionId, IStepRule ruleRuntime)
        {
            DefinitionId = definitionId;
            RuleRuntime = ruleRuntime;
            Status = StepStatus.Inactive;
        }

        public string DefinitionId { get; }

        public StepStatus Status { get; private set; }

        public int AttemptCount { get; private set; }

        public int ViolationCount { get; private set; }

        public IStepRule RuleRuntime { get; }

        public DateTimeOffset? ActivatedAt { get; private set; }

        public DateTimeOffset? TerminalAt { get; private set; }

        public bool IsTerminal =>
            Status == StepStatus.Completed ||
            Status == StepStatus.Failed ||
            Status == StepStatus.Skipped;

        internal void Activate(DateTimeOffset timestamp)
        {
            if (Status != StepStatus.Inactive)
            {
                throw new InvalidOperationException(
                    $"Step '{DefinitionId}' cannot be activated from status '{Status}'.");
            }

            Status = StepStatus.Active;
            ActivatedAt = timestamp;
        }

        internal void RecordAttempt()
        {
            EnsureActive("record an attempt for");
            AttemptCount = checked(AttemptCount + 1);
        }

        internal void RecordViolation()
        {
            EnsureActive("record a violation for");
            ViolationCount = checked(ViolationCount + 1);
        }

        internal void Complete(DateTimeOffset timestamp)
        {
            EnsureActive("complete");
            SetTerminalStatus(StepStatus.Completed, timestamp);
        }

        internal void Fail(DateTimeOffset timestamp)
        {
            EnsureActive("fail");
            SetTerminalStatus(StepStatus.Failed, timestamp);
        }

        internal bool Skip(DateTimeOffset timestamp)
        {
            if (IsTerminal)
            {
                return false;
            }

            SetTerminalStatus(StepStatus.Skipped, timestamp);
            return true;
        }

        private void SetTerminalStatus(StepStatus status, DateTimeOffset timestamp)
        {
            Status = status;
            TerminalAt = timestamp;
        }

        private void EnsureActive(string operation)
        {
            if (Status != StepStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Cannot {operation} step '{DefinitionId}' while its status is '{Status}'.");
            }
        }
    }
}
