using System.Collections.Concurrent;

namespace FlowCore.Diagnostics;

internal sealed class DiagnosticsContext : IDiagnosticsContext, IDisposable
{
    private readonly ConcurrentQueue<DiagnosticEntry> _entries = new();
    private const int MaxEntries = 1000;

    public void Write(DiagnosticEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _)) { }
    }

    public IReadOnlyCollection<DiagnosticEntry> Entries => _entries.ToList().AsReadOnly();

    public void Dispose()
    {
        while (_entries.TryDequeue(out _)) { }
    }
}
