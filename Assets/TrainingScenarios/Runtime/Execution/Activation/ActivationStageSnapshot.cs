using System.Collections.Generic;

namespace TrainingScenarios.Runtime.Execution.Activation
{
    public sealed class ActivationStageSnapshot
    {
        public ActivationStageSnapshot(int index, IReadOnlyList<string> stepIds)
        {
            Index = index;
            StepIds = stepIds;
        }

        public int Index { get; }

        public IReadOnlyList<string> StepIds { get; }
    }
}
