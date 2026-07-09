namespace FlowCore;

public enum HandlerKind
{
    Command,
    Query,
    Event
}

public class HandlerDescriptor
{
    public Type HandlerType { get; init; } = null!;
    public Type RequestType { get; init; } = null!;
    public Type? ResponseType { get; init; }
    public HandlerKind Kind { get; init; }
}
