namespace FlowCore.Saga;

public enum SagaStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Compensating,
    Compensated
}

public class SagaState
{
    public Guid SagaId { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public SagaStatus Status { get; set; } = SagaStatus.Pending;
    public int CurrentStep { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid CorrelationId { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
    public List<string> ExecutedSteps { get; set; } = new();
    public string? FailureReason { get; set; }
}