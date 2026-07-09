namespace FlowCore.Diagnostics;

public interface IDiagnosticsContext
{
    void Write(DiagnosticEntry entry);
    IReadOnlyCollection<DiagnosticEntry> Entries { get; }
}
