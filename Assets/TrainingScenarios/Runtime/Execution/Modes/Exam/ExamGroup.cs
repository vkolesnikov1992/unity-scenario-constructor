using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TrainingScenarios.Runtime.Execution.Modes.Exam
{
    public sealed class ExamGroup
    {
        public ExamGroup(string groupId, IEnumerable<string> stepIds)
        {
            GroupId = groupId;
            StepIds = new ReadOnlyCollection<string>(stepIds.ToList());
        }

        public string GroupId { get; }

        public IReadOnlyList<string> StepIds { get; }
    }
}
