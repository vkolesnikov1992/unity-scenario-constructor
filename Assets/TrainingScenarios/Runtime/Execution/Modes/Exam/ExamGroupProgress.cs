namespace TrainingScenarios.Runtime.Execution.Modes.Exam
{
    public sealed class ExamGroupProgress
    {
        public ExamGroupProgress(
            int groupIndex,
            string groupId,
            int violationCount,
            int violationThreshold,
            ExamGroupStatus status)
        {
            GroupIndex = groupIndex;
            GroupId = groupId;
            ViolationCount = violationCount;
            ViolationThreshold = violationThreshold;
            Status = status;
        }

        public int GroupIndex { get; }

        public string GroupId { get; }

        public int ViolationCount { get; }

        public int ViolationThreshold { get; }

        public ExamGroupStatus Status { get; }
    }
}
