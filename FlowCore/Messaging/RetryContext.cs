namespace FlowCore.Messaging;

public class RetryContext
{
    public int Attempt { get; init; }
    public Exception? Exception { get; init; }
    public string EventType { get; init; } = string.Empty;
    public Guid MessageId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string Provider { get; init; } = string.Empty;
    public Dictionary<string, object?> Metadata { get; init; } = new();
}