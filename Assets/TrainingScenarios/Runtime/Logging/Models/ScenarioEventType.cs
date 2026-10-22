namespace TrainingScenarios.Runtime.Logging.Models
{
    public enum ScenarioEventType
    {
        SessionStarted,
        StepActivated,
        ActionReceived,
        StepEvaluated,
        StepViolation,
        StepCompleted,
        StepFailed,
        StepSkipped,
        SessionCompleted
    }
}
