namespace FlowCore.Diagnostics;

public enum DiagnosticSeverity
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

public class DiagnosticEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public DiagnosticSeverity Severity { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Event { get; init; } = string.Empty;
    public string? Message { get; init; }
    public Exception? Exception { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}
