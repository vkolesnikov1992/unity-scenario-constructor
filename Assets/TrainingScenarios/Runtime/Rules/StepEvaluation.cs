namespace TrainingScenarios.Runtime.Rules
{
    public sealed class StepEvaluation
    {
        private StepEvaluation(StepEvaluationOutcome outcome, string reason)
        {
            Outcome = outcome;
            Reason = reason ?? string.Empty;
        }

        public StepEvaluationOutcome Outcome { get; }

        public string Reason { get; }

        public static StepEvaluation Success(string reason = null)
        {
            return new StepEvaluation(StepEvaluationOutcome.Success, reason);
        }

        public static StepEvaluation Violation(string reason)
        {
            return new StepEvaluation(StepEvaluationOutcome.Violation, reason);
        }

        public static StepEvaluation NotRelevant(string reason = null)
        {
            return new StepEvaluation(StepEvaluationOutcome.NotRelevant, reason);
        }
    }
}
