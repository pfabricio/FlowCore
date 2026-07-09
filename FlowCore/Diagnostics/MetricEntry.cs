namespace FlowCore.Diagnostics;

public sealed class MetricEntry
{
    public string Name { get; }

    public MetricType Type { get; }

    public double Value { get; }

    public string? Unit { get; }

    public DateTimeOffset Timestamp { get; }

    public IReadOnlyDictionary<string, string> Tags { get; }

    public MetricEntry(
        string name,
        MetricType type,
        double value,
        string? unit = null,
        DateTimeOffset? timestamp = null,
        IReadOnlyDictionary<string, string>? tags = null)
    {
        Name = name;
        Type = type;
        Value = value;
        Unit = unit;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
        Tags = tags ?? System.Collections.Immutable.ImmutableDictionary<string, string>.Empty;
    }
}
