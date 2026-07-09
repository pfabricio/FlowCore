namespace FlowCore.Messaging;

public class DeadLetterContext
{
    public MessageEnvelope Envelope { get; init; } = new();
    public Exception? Exception { get; init; }
    public int RetryCount { get; init; }
    public DateTimeOffset FailedAt { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = new();
}