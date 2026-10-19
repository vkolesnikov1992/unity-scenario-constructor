using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TrainingScenarios.Runtime.Validation.Contracts;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Validation
{
    public sealed class ScenarioValidator
    {
        private readonly IReadOnlyList<IScenarioValidationRule> _rules;

        public ScenarioValidator(IEnumerable<IScenarioValidationRule> rules)
        {
            _rules = new ReadOnlyCollection<IScenarioValidationRule>(rules.ToList());
        }

        public IReadOnlyList<ValidationIssue> Validate(ScenarioValidationContext context)
        {
            var issues = new List<ValidationIssue>();

            foreach (var rule in _rules)
            {
                issues.AddRange(rule.Validate(context));
            }

            return new ReadOnlyCollection<ValidationIssue>(issues);
        }
    }
}
