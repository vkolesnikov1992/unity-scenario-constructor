using System.Runtime.Serialization;

namespace TrainingScenarios.Runtime.Logging.Snapshots
{
    [DataContract]
    public sealed class TrainingScoringSnapshot
    {
        [DataMember(Name = "firstAttemptScore", Order = 1)]
        private int _firstAttemptScore;

        [DataMember(Name = "secondAttemptScore", Order = 2)]
        private int _secondAttemptScore;

        [DataMember(Name = "laterAttemptScore", Order = 3)]
        private int _laterAttemptScore;

        [DataMember(Name = "maximumPerStep", Order = 4)]
        private int _maximumPerStep;

        public TrainingScoringSnapshot(
            int firstAttemptScore = 10,
            int secondAttemptScore = 5,
            int laterAttemptScore = 0,
            int maximumPerStep = 10)
        {
            _firstAttemptScore = firstAttemptScore;
            _secondAttemptScore = secondAttemptScore;
            _laterAttemptScore = laterAttemptScore;
            _maximumPerStep = maximumPerStep;
        }

        public int FirstAttemptScore => _firstAttemptScore;

        public int SecondAttemptScore => _secondAttemptScore;

        public int LaterAttemptScore => _laterAttemptScore;

        public int MaximumPerStep => _maximumPerStep;
    }
}
