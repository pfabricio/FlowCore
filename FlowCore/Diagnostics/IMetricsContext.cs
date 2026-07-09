namespace FlowCore.Diagnostics;

public interface IMetricsContext
{
    void Record(MetricEntry metric);

    IReadOnlyCollection<MetricEntry> Entries { get; }
}
