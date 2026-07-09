namespace FlowCore.Messaging;

public static class EnvelopeValidator
{
    public static bool IsValid(MessageEnvelope envelope)
    {
        if (envelope is null)
            return false;

        if (envelope.MessageId == Guid.Empty)
            return false;

        if (string.IsNullOrWhiteSpace(envelope.EventType))
            return false;

        if (envelope.Payload is null || envelope.Payload.Length == 0)
            return false;

        return true;
    }
}