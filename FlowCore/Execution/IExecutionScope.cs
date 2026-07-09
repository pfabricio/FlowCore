using FlowCore.Diagnostics;

namespace FlowCore.Execution;

public interface IExecutionScope : IDisposable
{
    Guid Id { get; }
    ICorrelationContext Correlation { get; }
    IExecutionItems Items { get; }
    IDiagnosticsContext Diagnostics { get; }
    CancellationToken CancellationToken { get; }
}
