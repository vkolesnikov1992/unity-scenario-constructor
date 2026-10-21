using System.Collections.Generic;

namespace TrainingScenarios.Runtime.Rules.Contracts
{
    public interface ISceneStateRequirementProvider
    {
        IEnumerable<string> GetRequiredStateIds();
    }
}
