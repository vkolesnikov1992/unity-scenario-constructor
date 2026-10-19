using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using TrainingScenarios.Runtime.Execution.Modes.Models;
using TrainingScenarios.Runtime.Logging.Models;

namespace TrainingScenarios.Runtime.Logging.Snapshots
{
    [DataContract]
    public sealed class SessionSnapshot
    {
        private static readonly IReadOnlyList<StepSnapshot> EmptySteps =
            new ReadOnlyCollection<StepSnapshot>(Array.Empty<StepSnapshot>());

        private static readonly IReadOnlyList<ExamGroupSnapshot> EmptyGroups =
            new ReadOnlyCollection<ExamGroupSnapshot>(Array.Empty<ExamGroupSnapshot>());

        [DataMember(Name = "schemaVersion", Order = 1)]
        private int _schemaVersion;

        [DataMember(Name = "mode", Order = 2)]
        private string _mode;

        [DataMember(Name = "inactiveActionBehavior", Order = 3)]
        private string _inactiveActionBehavior;

        [DataMember(Name = "steps", Order = 4)]
        private List<StepSnapshot> _steps;

        [DataMember(Name = "examGroups", Order = 5)]
        private List<ExamGroupSnapshot> _examGroups;

        [DataMember(Name = "trainingScoring", Order = 6, EmitDefaultValue = false)]
        private TrainingScoringSnapshot _trainingScoring;

        [DataMember(Name = "examScoring", Order = 7, EmitDefaultValue = false)]
        private ExamScoringSnapshot _examScoring;

        [DataMember(Name = "examViolationThreshold", Order = 8)]
        private int _examViolationThreshold;

        public SessionSnapshot(
            ScenarioMode mode,
            InactiveActionBehavior inactiveActionBehavior,
            IEnumerable<StepSnapshot> steps,
            IEnumerable<ExamGroupSnapshot> examGroups,
            TrainingScoringSnapshot trainingScoring,
            ExamScoringSnapshot examScoring,
            int examViolationThreshold = 2)
        {
            _schemaVersion = JournalSchema.CurrentVersion;
            _mode = mode.ToString();
            _inactiveActionBehavior = inactiveActionBehavior.ToString();
            _steps = steps.ToList();
            _examGroups = examGroups.ToList();
            _trainingScoring = trainingScoring;
            _examScoring = examScoring;
            _examViolationThreshold = examViolationThreshold;
        }

        public int SchemaVersion => _schemaVersion;

        public string Mode => _mode;

        public string InactiveActionBehavior => _inactiveActionBehavior;

        public IReadOnlyList<StepSnapshot> Steps => _steps ?? EmptySteps;

        public IReadOnlyList<ExamGroupSnapshot> ExamGroups => _examGroups ?? EmptyGroups;

        public TrainingScoringSnapshot TrainingScoring => _trainingScoring;

        public ExamScoringSnapshot ExamScoring => _examScoring;

        public int ExamViolationThreshold => _examViolationThreshold;
    }
}
