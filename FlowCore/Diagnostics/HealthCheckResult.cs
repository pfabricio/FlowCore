namespace FlowCore.Diagnostics;

public sealed class HealthCheckResult
{
    public string Name { get; }

    public HealthStatus Status { get; }

    public string? Description { get; }

    public TimeSpan Duration { get; }

    public Exception? Exception { get; }

    public IReadOnlyDictionary<string, object> Metadata { get; }

    public HealthCheckResult(
        string name,
        HealthStatus status,
        string? description = null,
        TimeSpan? duration = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        Name = name;
        Status = status;
        Description = description;
        Duration = duration ?? TimeSpan.Zero;
        Exception = exception;
        Metadata = metadata ?? System.Collections.Immutable.ImmutableDictionary<string, object>.Empty;
    }

    public static HealthCheckResult Healthy(string name, string? description = null)
        => new(name, HealthStatus.Healthy, description);

    public static HealthCheckResult Degraded(string name, string? description = null, Exception? exception = null)
        => new(name, HealthStatus.Degraded, description, exception: exception);

    public static HealthCheckResult Unhealthy(string name, string? description = null, Exception? exception = null)
        => new(name, HealthStatus.Unhealthy, description, exception: exception);
}
