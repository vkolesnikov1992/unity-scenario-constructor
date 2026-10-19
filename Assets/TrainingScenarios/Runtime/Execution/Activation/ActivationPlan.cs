using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TrainingScenarios.Runtime.Execution.Activation
{
    public sealed class ActivationPlan
    {
        public ActivationPlan(IEnumerable<ActivationStage> stages)
        {
            Stages = new ReadOnlyCollection<ActivationStage>(stages.ToList());
        }

        public IReadOnlyList<ActivationStage> Stages { get; }
    }
}
