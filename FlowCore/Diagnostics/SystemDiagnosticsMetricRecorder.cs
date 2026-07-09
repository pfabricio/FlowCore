using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace FlowCore.Diagnostics;

internal sealed class SystemDiagnosticsMetricRecorder : IMetricRecorder
{
    private static readonly Meter Meter = new("FlowCore", "2.0.0");
    private readonly ConcurrentDictionary<string, Counter<long>> _counters = new();
    private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new();

    public void RecordCounter(string name, long value = 1)
    {
        var counter = _counters.GetOrAdd(name, n => Meter.CreateCounter<long>(n));
        counter.Add(value);
    }

    public void RecordDuration(string name, TimeSpan duration)
    {
        var histogram = _histograms.GetOrAdd(name, n => Meter.CreateHistogram<double>(n, "ms"));
        histogram.Record(duration.TotalMilliseconds);
    }

    public void RecordGauge(string name, long value)
    {
        Meter.CreateObservableGauge(name, () => value);
    }
}