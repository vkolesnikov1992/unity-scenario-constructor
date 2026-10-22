using System.Collections.Generic;
using TrainingScenarios.Runtime.Actions.Models;

namespace TrainingScenarios.Runtime.Rules.Contracts
{
    public interface IActionRequirementProvider
    {
        IEnumerable<ActionRequirement> GetActionRequirements();
    }
}
