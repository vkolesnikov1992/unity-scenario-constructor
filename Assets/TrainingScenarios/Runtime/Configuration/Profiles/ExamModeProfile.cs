using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Configuration.Flow;
using TrainingScenarios.Runtime.Execution.Modes.Contracts;
using TrainingScenarios.Runtime.Execution.Modes.Exam;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Logging.Snapshots;

namespace TrainingScenarios.Runtime.Configuration.Profiles
{
    [CreateAssetMenu(
        fileName = "ExamModeProfile",
        menuName = "Training Scenarios/Profiles/Exam")]
    public sealed class ExamModeProfile : ScenarioModeProfile
    {
        [SerializeField]
        private List<ExamGroupConfiguration> _groups =
            new List<ExamGroupConfiguration>();
        [SerializeField]
        private int _violationThreshold = 2;
        [SerializeField]
        private int _maximumScore = 100;
        [SerializeField]
        private int _failedStepPenalty = 10;
        [SerializeField]
        private int _skippedStepPenalty = 5;

        public override ScenarioMode Mode => ScenarioMode.Exam;

        public IReadOnlyList<ExamGroupConfiguration> Groups => _groups;

        public int ViolationThreshold => _violationThreshold;

        public int MaximumScore => _maximumScore;

        public int FailedStepPenalty => _failedStepPenalty;

        public int SkippedStepPenalty => _skippedStepPenalty;

        public override IScenarioModePolicy CreatePolicy()
        {
            return new ExamModePolicy(new ExamGroupPlan(
                _groups.Select(group => group.CreateRuntime()),
                _violationThreshold));
        }

        public override SessionSnapshot CreateSnapshot(
            IReadOnlyList<StepDefinition> steps)
        {
            return new SessionSnapshot(
                Mode,
                InactiveActionBehavior,
                steps.Select(step => new StepSnapshot(step.StepId, step.DisplayName)),
                _groups.Select(group => group.CreateSnapshot()),
                null,
                new ExamScoringSnapshot(
                    _maximumScore,
                    _failedStepPenalty,
                    _skippedStepPenalty),
                _violationThreshold);
        }
    }
}
