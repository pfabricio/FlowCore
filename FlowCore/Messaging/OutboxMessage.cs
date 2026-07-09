namespace FlowCore.Messaging;

public enum OutboxStatus
{
    Pending,
    Processing,
    Published,
    Failed
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; }
    public Guid CorrelationId { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}