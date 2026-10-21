using System;
using System.Collections.Generic;
using System.Linq;
using TrainingScenarios.Runtime.Core.Sessions;
using TrainingScenarios.Runtime.Core.Steps;
using TrainingScenarios.Runtime.Execution.Modes.Contracts;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Rules.Models;

namespace TrainingScenarios.Runtime.Execution.Modes.Exam
{
    public sealed class ExamModePolicy : IScenarioModePolicy
    {
        private readonly ExamGroupPlan _groupPlan;
        private readonly Dictionary<string, int> _groupIndexByStepId;

        private int[] _violationCounts;
        private ExamGroupStatus[] _groupStatuses;

        public ExamModePolicy(ExamGroupPlan groupPlan)
        {
            _groupPlan = groupPlan;
            _groupIndexByStepId = CreateGroupIndex(groupPlan);
        }

        public event Action<ExamGroupProgress> GroupProgressChanged;

        public ScenarioMode Mode => ScenarioMode.Exam;

        public int CurrentGroupIndex { get; private set; } = -1;

        public string CurrentGroupId =>
            CurrentGroupIndex >= 0 && CurrentGroupIndex < _groupPlan.Groups.Count
                ? _groupPlan.Groups[CurrentGroupIndex].GroupId
                : string.Empty;

        public void Initialize(ScenarioSession session)
        {
            _violationCounts = new int[_groupPlan.Groups.Count];
            _groupStatuses = Enumerable
                .Repeat(ExamGroupStatus.Inactive, _groupPlan.Groups.Count)
                .ToArray();

            CurrentGroupIndex = 0;
            _groupStatuses[CurrentGroupIndex] = ExamGroupStatus.Active;
            PublishProgress(CurrentGroupIndex);
        }

        public void ApplyEvaluation(
            ScenarioSession session,
            StepRuntime step,
            StepEvaluation evaluation)
        {
            var stepGroupIndex = _groupIndexByStepId[step.DefinitionId];

            switch (evaluation.Outcome)
            {
                case StepEvaluationOutcome.Success:
                    session.RecordAttempt(step.DefinitionId);
                    session.CompleteStep(step.DefinitionId);
                    CompleteGroupIfTerminal(session, stepGroupIndex);
                    break;
                case StepEvaluationOutcome.Violation:
                    session.RecordAttempt(step.DefinitionId);
                    session.RecordViolation(step.DefinitionId);
                    session.FailStep(step.DefinitionId);
                    RecordGroupViolation(session, stepGroupIndex);
                    break;
            }
        }

        public void ApplyInactiveEvaluation(
            ScenarioSession session,
            StepRuntime step,
            StepEvaluation evaluation)
        {
            if (evaluation.Outcome != StepEvaluationOutcome.Violation)
            {
                return;
            }

            var stepGroupIndex = _groupIndexByStepId[step.DefinitionId];
            session.RecordInactiveViolation(step.DefinitionId);
            session.FailInactiveStep(step.DefinitionId);
            RecordGroupViolation(session, stepGroupIndex);
        }

        public ExamGroupProgress GetGroupProgress(int groupIndex)
        {
            var group = _groupPlan.Groups[groupIndex];
            return new ExamGroupProgress(
                groupIndex,
                group.GroupId,
                _violationCounts[groupIndex],
                _groupPlan.ViolationThreshold,
                _groupStatuses[groupIndex]);
        }

        private void RecordGroupViolation(ScenarioSession session, int groupIndex)
        {
            _violationCounts[groupIndex]++;

            if (_violationCounts[groupIndex] >= _groupPlan.ViolationThreshold)
            {
                _groupStatuses[groupIndex] = ExamGroupStatus.ThresholdReached;
                PublishProgress(groupIndex);
                SkipUnfinishedGroupSteps(session, groupIndex);
                if (groupIndex == CurrentGroupIndex)
                {
                    ActivateNextGroup(session);
                }

                return;
            }

            PublishProgress(groupIndex);
            if (groupIndex == CurrentGroupIndex)
            {
                CompleteGroupIfTerminal(session, groupIndex);
            }
        }

        private void CompleteGroupIfTerminal(ScenarioSession session, int groupIndex)
        {
            var group = _groupPlan.Groups[groupIndex];
            if (group.StepIds.Any(stepId => !session.GetStep(stepId).IsTerminal))
            {
                return;
            }

            _groupStatuses[groupIndex] = ExamGroupStatus.Completed;
            PublishProgress(groupIndex);
            ActivateNextGroup(session);
        }

        private void SkipUnfinishedGroupSteps(ScenarioSession session, int groupIndex)
        {
            foreach (var stepId in _groupPlan.Groups[groupIndex].StepIds)
            {
                if (!session.GetStep(stepId).IsTerminal)
                {
                    session.SkipStep(stepId);
                }
            }
        }

        private void ActivateNextGroup(ScenarioSession session)
        {
            while (++CurrentGroupIndex < _groupPlan.Groups.Count)
            {
                if (_groupStatuses[CurrentGroupIndex] == ExamGroupStatus.ThresholdReached ||
                    _groupStatuses[CurrentGroupIndex] == ExamGroupStatus.Completed)
                {
                    continue;
                }

                var group = _groupPlan.Groups[CurrentGroupIndex];
                if (group.StepIds.All(stepId => session.GetStep(stepId).IsTerminal))
                {
                    _groupStatuses[CurrentGroupIndex] = ExamGroupStatus.Completed;
                    PublishProgress(CurrentGroupIndex);
                    continue;
                }

                _groupStatuses[CurrentGroupIndex] = ExamGroupStatus.Active;
                PublishProgress(CurrentGroupIndex);
                return;
            }
        }

        private void PublishProgress(int groupIndex)
        {
            GroupProgressChanged?.Invoke(GetGroupProgress(groupIndex));
        }

        private static Dictionary<string, int> CreateGroupIndex(ExamGroupPlan groupPlan)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var groupIndex = 0; groupIndex < groupPlan.Groups.Count; groupIndex++)
            {
                foreach (var stepId in groupPlan.Groups[groupIndex].StepIds)
                {
                    result.Add(stepId, groupIndex);
                }
            }

            return result;
        }
    }
}
