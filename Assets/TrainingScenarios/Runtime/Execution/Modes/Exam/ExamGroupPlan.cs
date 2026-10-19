using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TrainingScenarios.Runtime.Execution.Modes.Exam
{
    public sealed class ExamGroupPlan
    {
        public ExamGroupPlan(IEnumerable<ExamGroup> groups, int violationThreshold = 2)
        {
            Groups = new ReadOnlyCollection<ExamGroup>(groups.ToList());
            ViolationThreshold = violationThreshold;
        }

        public IReadOnlyList<ExamGroup> Groups { get; }

        public int ViolationThreshold { get; }
    }
}
