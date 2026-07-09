namespace FlowCore.Scheduling;

public enum ScheduledMessageStatus
{
    Pending,
    Scheduled,
    Publishing,
    Published,
    Failed
}

public class ScheduledMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    public DateTimeOffset ExecuteAt { get; set; }
    public ScheduledMessageStatus Status { get; set; } = ScheduledMessageStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid CorrelationId { get; set; }
    public int RetryCount { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}