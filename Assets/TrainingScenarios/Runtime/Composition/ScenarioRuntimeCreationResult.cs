using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TrainingScenarios.Runtime.Validation.Models;

namespace TrainingScenarios.Runtime.Composition
{
    public sealed class ScenarioRuntimeCreationResult
    {
        public ScenarioRuntimeCreationResult(
            ScenarioRuntime runtime,
            IEnumerable<ValidationIssue> validationIssues)
        {
            Runtime = runtime;
            ValidationIssues = new ReadOnlyCollection<ValidationIssue>(
                validationIssues.ToList());
        }

        public ScenarioRuntime Runtime { get; }

        public IReadOnlyList<ValidationIssue> ValidationIssues { get; }

        public bool HasErrors => ValidationIssues.Any(issue =>
            issue.Severity == ValidationSeverity.Error);
    }
}
