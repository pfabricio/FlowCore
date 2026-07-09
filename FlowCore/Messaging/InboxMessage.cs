namespace FlowCore.Messaging;

public class InboxMessage
{
    public Guid MessageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
    public string Consumer { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}