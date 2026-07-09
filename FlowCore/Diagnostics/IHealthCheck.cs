namespace FlowCore.Diagnostics;

public interface IHealthCheck
{
    ValueTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
