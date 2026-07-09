namespace FlowCore.Diagnostics;

public interface IMetricRecorder
{
    void RecordCounter(string name, long value = 1);
    void RecordDuration(string name, TimeSpan duration);
    void RecordGauge(string name, long value);
}