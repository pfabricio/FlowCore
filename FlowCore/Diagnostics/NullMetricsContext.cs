using System.Collections.Immutable;

namespace FlowCore.Diagnostics;

internal sealed class NullMetricsContext : IMetricsContext
{
    public static readonly NullMetricsContext Instance = new();

    public IReadOnlyCollection<MetricEntry> Entries
        => ImmutableArray<MetricEntry>.Empty;

    public void Record(MetricEntry metric)
    {
    }
}
