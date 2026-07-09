namespace FlowCore.Execution;

public interface ICorrelationContext
{
    Guid CorrelationId { get; }
    string? TraceId { get; }
    string? SpanId { get; }
    string? ParentSpanId { get; }
}
