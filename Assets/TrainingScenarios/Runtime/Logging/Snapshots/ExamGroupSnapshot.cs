using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace TrainingScenarios.Runtime.Logging.Snapshots
{
    [DataContract]
    public sealed class ExamGroupSnapshot
    {
        private static readonly IReadOnlyList<string> EmptyStepIds =
            new ReadOnlyCollection<string>(Array.Empty<string>());

        [DataMember(Name = "groupId", Order = 1)]
        private string _groupId;

        [DataMember(Name = "stepIds", Order = 2)]
        private List<string> _stepIds;

        public ExamGroupSnapshot(string groupId, IEnumerable<string> stepIds)
        {
            _groupId = groupId;
            _stepIds = stepIds.ToList();
        }

        public string GroupId => _groupId;

        public IReadOnlyList<string> StepIds => _stepIds ?? EmptyStepIds;
    }
}
