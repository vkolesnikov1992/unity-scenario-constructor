namespace TrainingScenarios.Runtime.Logging.Models
{
    public enum ScenarioEventType
    {
        SessionStarted,
        StepActivated,
        ActionReceived,
        StepViolation,
        StepCompleted,
        StepFailed,
        StepSkipped,
        SessionCompleted
    }
}
