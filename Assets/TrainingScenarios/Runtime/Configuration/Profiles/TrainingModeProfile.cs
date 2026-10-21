using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Execution.Modes.Contracts;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Execution.Modes.Training;
using TrainingScenarios.Runtime.Logging.Snapshots;

namespace TrainingScenarios.Runtime.Configuration.Profiles
{
    [CreateAssetMenu(
        fileName = "TrainingModeProfile",
        menuName = "Training Scenarios/Profiles/Training")]
    public class TrainingModeProfile : ScenarioModeProfile
    {
        [SerializeField]
        private int _firstAttemptScore = 10;
        [SerializeField]
        private int _secondAttemptScore = 5;
        [SerializeField]
        private int _laterAttemptScore;
        [SerializeField]
        private int _maximumPerStep = 10;

        public override ScenarioMode Mode => ScenarioMode.Training;

        public int FirstAttemptScore => _firstAttemptScore;

        public int SecondAttemptScore => _secondAttemptScore;

        public int LaterAttemptScore => _laterAttemptScore;

        public int MaximumPerStep => _maximumPerStep;

        public override IScenarioModePolicy CreatePolicy()
        {
            return new TrainingModePolicy();
        }

        public override SessionSnapshot CreateSnapshot(
            IReadOnlyList<StepDefinition> steps)
        {
            return new SessionSnapshot(
                Mode,
                InactiveActionBehavior,
                steps.Select(step => new StepSnapshot(step.StepId, step.DisplayName)),
                Enumerable.Empty<ExamGroupSnapshot>(),
                new TrainingScoringSnapshot(
                    _firstAttemptScore,
                    _secondAttemptScore,
                    _laterAttemptScore,
                    _maximumPerStep),
                null);
        }
    }
}
