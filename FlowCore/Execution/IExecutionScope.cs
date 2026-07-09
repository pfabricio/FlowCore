using FlowCore.Diagnostics;

namespace FlowCore.Execution;

public interface IExecutionScope : IDisposable
{
    Guid Id { get; }

    ICorrelationContext Correlation { get; }

    IExecutionItems Items { get; }

    IDiagnosticsContext Diagnostics { get; }

    IMetricsContext Metrics => NullMetricsContext.Instance;

    CancellationToken CancellationToken { get; }
}
