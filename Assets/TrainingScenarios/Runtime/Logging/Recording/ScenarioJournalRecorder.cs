using System;
using System.Collections.Generic;
using System.Linq;
using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Core.Sessions;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Runner;
using TrainingScenarios.Runtime.Logging.Contracts;
using TrainingScenarios.Runtime.Logging.Models;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Logging.Recording
{
    public sealed class ScenarioJournalRecorder : IDisposable
    {
        private readonly ScenarioRunner _runner;
        private readonly SessionJournal _journal;
        private readonly IEventSink _eventSink;
        private readonly Dictionary<string, StepStatus> _stepStatuses;
        private readonly Dictionary<string, string> _groupIdByStepId;

        private bool _isRecording;

        public ScenarioJournalRecorder(
            ScenarioRunner runner,
            SessionJournal journal,
            IEventSink eventSink)
        {
            _runner = runner;
            _journal = journal;
            _eventSink = eventSink;
            _stepStatuses = runner.Session.Steps.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Status,
                StringComparer.Ordinal);
            _groupIdByStepId = CreateGroupIndex(journal);
        }

        public event Action<SessionJournal> JournalCompleted;

        public SessionJournal Journal => _journal;

        public void StartRecording()
        {
            if (_isRecording)
            {
                return;
            }

            _runner.ActionReceived += HandleActionReceived;
            _runner.StepEvaluated += HandleStepEvaluated;
            _runner.Session.StepChanged += HandleStepChanged;
            _runner.Session.StatusChanged += HandleSessionStatusChanged;
            _isRecording = true;
        }

        public void StopRecording()
        {
            if (!_isRecording)
            {
                return;
            }

            _runner.ActionReceived -= HandleActionReceived;
            _runner.StepEvaluated -= HandleStepEvaluated;
            _runner.Session.StepChanged -= HandleStepChanged;
            _runner.Session.StatusChanged -= HandleSessionStatusChanged;
            _isRecording = false;
        }

        public void Dispose()
        {
            StopRecording();
        }

        private void HandleActionReceived(ScenarioAction action)
        {
            var data = new List<JournalDataEntry>
            {
                new JournalDataEntry("sourceId", action.SourceId),
                new JournalDataEntry("actionType", action.ActionType),
                new JournalDataEntry("targetId", action.TargetId)
            };

            data.AddRange(action.AdditionalData
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new JournalDataEntry($"actionData.{pair.Key}", pair.Value)));

            AppendEvent(
                action.Timestamp,
                ScenarioEventType.ActionReceived,
                string.Empty,
                "Received",
                0,
                data);
        }

        private void HandleStepEvaluated(StepActionEvaluation stepEvaluation)
        {
            if (stepEvaluation.Evaluation.Outcome != StepEvaluationOutcome.Violation)
            {
                return;
            }

            var step = _runner.Session.GetStep(stepEvaluation.StepId);
            var attemptNumber = stepEvaluation.StepStatus == StepStatus.Active
                ? step.AttemptCount + 1
                : 0;

            AppendEvent(
                stepEvaluation.Action.Timestamp,
                ScenarioEventType.StepViolation,
                stepEvaluation.StepId,
                stepEvaluation.Evaluation.Outcome.ToString(),
                attemptNumber,
                new[]
                {
                    new JournalDataEntry("reason", stepEvaluation.Evaluation.Reason)
                });
        }

        private void HandleStepChanged(StepRuntime step)
        {
            var previousStatus = _stepStatuses[step.DefinitionId];
            if (previousStatus == step.Status)
            {
                return;
            }

            _stepStatuses[step.DefinitionId] = step.Status;

            switch (step.Status)
            {
                case StepStatus.Active:
                    AppendStepStatusEvent(
                        step,
                        step.ActivatedAt.Value,
                        ScenarioEventType.StepActivated);
                    break;
                case StepStatus.Completed:
                    AppendStepStatusEvent(
                        step,
                        step.TerminalAt.Value,
                        ScenarioEventType.StepCompleted);
                    break;
                case StepStatus.Failed:
                    AppendStepStatusEvent(
                        step,
                        step.TerminalAt.Value,
                        ScenarioEventType.StepFailed);
                    break;
                case StepStatus.Skipped:
                    AppendStepStatusEvent(
                        step,
                        step.TerminalAt.Value,
                        ScenarioEventType.StepSkipped);
                    break;
            }
        }

        private void HandleSessionStatusChanged(ScenarioSessionStatus status)
        {
            switch (status)
            {
                case ScenarioSessionStatus.Running:
                    AppendEvent(
                        _runner.Session.StartedAt.Value,
                        ScenarioEventType.SessionStarted,
                        string.Empty,
                        _journal.Snapshot.Mode,
                        0,
                        new[]
                        {
                            new JournalDataEntry(
                                "inactiveActionBehavior",
                                _journal.Snapshot.InactiveActionBehavior)
                        });
                    break;
                case ScenarioSessionStatus.Completed:
                    AppendEvent(
                        _runner.Session.CompletedAt.Value,
                        ScenarioEventType.SessionCompleted,
                        string.Empty,
                        ScenarioSessionStatus.Completed.ToString(),
                        0,
                        Array.Empty<JournalDataEntry>());
                    JournalCompleted?.Invoke(_journal);
                    StopRecording();
                    break;
            }
        }

        private void AppendStepStatusEvent(
            StepRuntime step,
            DateTimeOffset timestamp,
            ScenarioEventType eventType)
        {
            AppendEvent(
                timestamp,
                eventType,
                step.DefinitionId,
                step.Status.ToString(),
                step.AttemptCount,
                Array.Empty<JournalDataEntry>());
        }

        private void AppendEvent(
            DateTimeOffset timestamp,
            ScenarioEventType eventType,
            string stepId,
            string result,
            int attemptNumber,
            IEnumerable<JournalDataEntry> additionalData)
        {
            var groupId = _groupIdByStepId.TryGetValue(stepId, out var foundGroupId)
                ? foundGroupId
                : string.Empty;

            _eventSink.Append(new ScenarioEvent(
                _journal.SessionId,
                _journal.ScenarioId,
                timestamp,
                eventType,
                stepId,
                result,
                attemptNumber,
                groupId,
                additionalData));
        }

        private static Dictionary<string, string> CreateGroupIndex(SessionJournal journal)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var group in journal.Snapshot.ExamGroups)
            {
                foreach (var stepId in group.StepIds)
                {
                    result[stepId] = group.GroupId;
                }
            }

            return result;
        }
    }
}
