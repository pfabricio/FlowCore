namespace FlowCore.Diagnostics;

public enum ActivityKindType
{
    Internal,
    Producer,
    Consumer,
    Server,
    Client
}

public interface IActivityFactory
{
    IActivityScope? Start(string name, ActivityKindType kind = ActivityKindType.Internal);
    IActivityScope? Start(string name, ActivityKindType kind, string? correlationId);
}