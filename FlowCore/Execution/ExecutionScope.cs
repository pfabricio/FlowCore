using System.Collections.Concurrent;
using System.Diagnostics;
using FlowCore.Diagnostics;

namespace FlowCore.Execution;

internal sealed class ExecutionScope : IExecutionScope
{
    private static readonly AsyncLocal<ExecutionScope?> _current = new();

    public static ExecutionScope? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public Guid Id { get; }
    public ICorrelationContext Correlation { get; }
    public IExecutionItems Items { get; }
    public IDiagnosticsContext Diagnostics { get; }
    public IMetricsContext Metrics { get; }
    public CancellationToken CancellationToken { get; }

    public ExecutionScope(CancellationToken cancellationToken = default)
    {
        Id = Guid.NewGuid();
        Correlation = new CorrelationContext(Id);
        Items = new ExecutionItems();
        Diagnostics = new DiagnosticsContext();
        Metrics = new MetricsContext();
        CancellationToken = cancellationToken;
        Current = this;
    }

    public void Dispose()
    {
        Current = null;
        (Items as IDisposable)?.Dispose();
        (Diagnostics as IDisposable)?.Dispose();
    }

    private sealed class CorrelationContext : ICorrelationContext
    {
        public Guid CorrelationId { get; }
        public string? TraceId { get; }
        public string? SpanId { get; }
        public string? ParentSpanId { get; }

        public CorrelationContext(Guid correlationId)
        {
            CorrelationId = correlationId;
            TraceId = Activity.Current?.Id;
            SpanId = Activity.Current?.SpanId.ToString();
            ParentSpanId = Activity.Current?.ParentSpanId.ToString();
        }
    }

    private sealed class ExecutionItems : IExecutionItems, IDisposable
    {
        private readonly ConcurrentDictionary<string, object?> _items = new();

        public void Set<T>(string key, T value) => _items[key] = value;
        public bool TryGet<T>(string key, out T? value)
        {
            if (_items.TryGetValue(key, out var obj) && obj is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }
        public bool Remove(string key) => _items.TryRemove(key, out _);
        public void Dispose() => _items.Clear();
    }
}
