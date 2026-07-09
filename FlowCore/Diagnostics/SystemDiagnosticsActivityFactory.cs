using System.Diagnostics;

namespace FlowCore.Diagnostics;

internal sealed class SystemDiagnosticsActivityScope : IActivityScope
{
    private readonly Activity? _activity;

    public SystemDiagnosticsActivityScope(Activity? activity)
    {
        _activity = activity;
    }

    public void SetTag(string key, object? value)
    {
        _activity?.SetTag(key, value);
    }

    public void SetException(Exception exception)
    {
        _activity?.SetTag("exception.message", exception.Message);
        _activity?.SetTag("exception.stacktrace", exception.ToString());
        _activity?.SetStatus(ActivityStatusCode.Error);
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }
}

internal sealed class SystemDiagnosticsActivityFactory : IActivityFactory
{
    private static readonly ActivitySource Source = new("FlowCore", "2.0.0");

    public IActivityScope? Start(string name, ActivityKindType kind = ActivityKindType.Internal)
    {
        var sysKind = kind switch
        {
            ActivityKindType.Producer => System.Diagnostics.ActivityKind.Producer,
            ActivityKindType.Consumer => System.Diagnostics.ActivityKind.Consumer,
            ActivityKindType.Server => System.Diagnostics.ActivityKind.Server,
            ActivityKindType.Client => System.Diagnostics.ActivityKind.Client,
            _ => System.Diagnostics.ActivityKind.Internal
        };
        var activity = Source.StartActivity(name, sysKind);
        return activity is not null ? new SystemDiagnosticsActivityScope(activity) : null;
    }

    public IActivityScope? Start(string name, ActivityKindType kind, string? correlationId)
    {
        var scope = Start(name, kind);
        if (scope is SystemDiagnosticsActivityScope s && correlationId is not null)
            s.SetTag("correlation.id", correlationId);
        return scope;
    }
}