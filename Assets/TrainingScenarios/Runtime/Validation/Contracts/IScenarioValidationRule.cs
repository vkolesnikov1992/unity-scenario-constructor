using System.Collections.Generic;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation.Contracts
{
    public interface IScenarioValidationRule
    {
        IEnumerable<ValidationIssue> Validate(ScenarioValidationContext context);
    }
}
