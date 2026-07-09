using System.Collections.Concurrent;

namespace FlowCore.Diagnostics;

internal sealed class DiagnosticsContext : IDiagnosticsContext, IDisposable
{
    private readonly ConcurrentQueue<DiagnosticEntry> _entries = new();

    public void Write(DiagnosticEntry entry)
    {
        _entries.Enqueue(entry);
    }

    public IReadOnlyCollection<DiagnosticEntry> Entries => _entries.ToList().AsReadOnly();

    public void Dispose()
    {
        _entries.Clear();
    }
}
