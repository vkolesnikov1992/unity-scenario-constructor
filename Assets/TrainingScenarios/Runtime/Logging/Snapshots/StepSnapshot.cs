using System.Runtime.Serialization;

namespace TrainingScenarios.Runtime.Logging.Snapshots
{
    [DataContract]
    public sealed class StepSnapshot
    {
        [DataMember(Name = "stepId", Order = 1)]
        private string _stepId;

        [DataMember(Name = "displayName", Order = 2)]
        private string _displayName;

        public StepSnapshot(string stepId, string displayName = null)
        {
            _stepId = stepId;
            _displayName = displayName;
        }

        public string StepId => _stepId;

        public string DisplayName => _displayName;
    }
}
