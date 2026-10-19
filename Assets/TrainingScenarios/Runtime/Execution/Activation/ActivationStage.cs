using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TrainingScenarios.Runtime.Execution.Activation
{
    public sealed class ActivationStage
    {
        public ActivationStage(IEnumerable<string> stepIds)
        {
            StepIds = new ReadOnlyCollection<string>(stepIds.ToList());
        }

        public IReadOnlyList<string> StepIds { get; }
    }
}
