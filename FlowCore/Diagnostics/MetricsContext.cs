using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace FlowCore.Diagnostics;

internal sealed class MetricsContext : IMetricsContext
{
    private readonly ConcurrentQueue<MetricEntry> _entries = new();
    private const int MaxEntries = 1000;

    public IReadOnlyCollection<MetricEntry> Entries
        => _entries.ToImmutableArray();

    public void Record(MetricEntry metric)
    {
        _entries.Enqueue(metric);
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _)) { }
    }

    public void RecordCounter(string name, long value = 1, IReadOnlyDictionary<string, string>? tags = null)
    {
        Record(new MetricEntry(name, MetricType.Counter, value, tags: tags));
    }

    public void RecordDuration(string name, TimeSpan duration, IReadOnlyDictionary<string, string>? tags = null)
    {
        Record(new MetricEntry(name, MetricType.Timer, duration.TotalMilliseconds, unit: "ms", tags: tags));
    }

    public void RecordGauge(string name, long value, IReadOnlyDictionary<string, string>? tags = null)
    {
        Record(new MetricEntry(name, MetricType.Gauge, value, tags: tags));
    }
}
