using System.Runtime.Serialization;

namespace TrainingScenarios.Runtime.Logging.Snapshots
{
    [DataContract]
    public sealed class ExamScoringSnapshot
    {
        [DataMember(Name = "maximumScore", Order = 1)]
        private int _maximumScore;

        [DataMember(Name = "failedStepPenalty", Order = 2)]
        private int _failedStepPenalty;

        [DataMember(Name = "skippedStepPenalty", Order = 3)]
        private int _skippedStepPenalty;

        public ExamScoringSnapshot(
            int maximumScore = 100,
            int failedStepPenalty = 10,
            int skippedStepPenalty = 5)
        {
            _maximumScore = maximumScore;
            _failedStepPenalty = failedStepPenalty;
            _skippedStepPenalty = skippedStepPenalty;
        }

        public int MaximumScore => _maximumScore;

        public int FailedStepPenalty => _failedStepPenalty;

        public int SkippedStepPenalty => _skippedStepPenalty;
    }
}
