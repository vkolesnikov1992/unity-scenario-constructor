using System.Collections.Generic;

namespace TrainingScenarios.Runtime.Rules.Contracts
{
    public interface ISceneBindingRequirementProvider
    {
        IEnumerable<string> GetRequiredBindingIds();
    }
}
