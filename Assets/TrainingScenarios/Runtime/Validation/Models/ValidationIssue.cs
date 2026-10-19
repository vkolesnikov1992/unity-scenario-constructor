using UnityEngine;

namespace TrainingScenarios.Runtime.Validation.Models
{
    public sealed class ValidationIssue
    {
        public ValidationIssue(
            string code,
            ValidationSeverity severity,
            string message,
            Object context)
        {
            Code = code;
            Severity = severity;
            Message = message;
            Context = context;
        }

        public string Code { get; }

        public ValidationSeverity Severity { get; }

        public string Message { get; }

        public Object Context { get; }
    }
}
