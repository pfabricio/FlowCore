namespace FlowCore.Messaging;

public class MessageEnvelope
{
    public Guid MessageId { get; set; }
    public Guid CorrelationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public byte[] Payload { get; set; } = Array.Empty<byte>();
}