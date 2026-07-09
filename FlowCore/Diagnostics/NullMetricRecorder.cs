namespace FlowCore.Diagnostics;

internal sealed class NullMetricRecorder : IMetricRecorder
{
    public static readonly NullMetricRecorder Instance = new();
    public void RecordCounter(string name, long value = 1) { }
    public void RecordDuration(string name, TimeSpan duration) { }
    public void RecordGauge(string name, long value) { }
}