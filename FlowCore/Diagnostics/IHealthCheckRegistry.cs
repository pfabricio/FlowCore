namespace FlowCore.Diagnostics;

public interface IHealthCheckRegistry
{
    IReadOnlyCollection<IHealthCheck> HealthChecks { get; }
}
