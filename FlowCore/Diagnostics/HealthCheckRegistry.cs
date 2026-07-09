using System.Collections.Immutable;

namespace FlowCore.Diagnostics;

internal sealed class HealthCheckRegistry : IHealthCheckRegistry
{
    public IReadOnlyCollection<IHealthCheck> HealthChecks { get; }

    public HealthCheckRegistry(IEnumerable<IHealthCheck> healthChecks)
    {
        HealthChecks = healthChecks.ToImmutableArray();
    }
}
