namespace FlowCore.Diagnostics;

internal sealed class NullActivityScope : IActivityScope
{
    public static readonly NullActivityScope Instance = new();
    public void Dispose() { }
    public void SetTag(string key, object? value) { }
    public void SetException(Exception exception) { }
}

internal sealed class NullActivityFactory : IActivityFactory
{
    public static readonly NullActivityFactory Instance = new();
    public IActivityScope? Start(string name, ActivityKindType kind = ActivityKindType.Internal) => null;
    public IActivityScope? Start(string name, ActivityKindType kind, string? correlationId) => null;
}