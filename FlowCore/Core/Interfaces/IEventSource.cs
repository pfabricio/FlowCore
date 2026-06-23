namespace FlowCore.Core.Interfaces;

/// <summary>
/// Interface para requests que geram eventos automaticamente após o processamento.
/// O EventDispatcherBehavior extrai e despacha esses eventos.
/// </summary>
public interface IEventSource
{
    /// <summary>
    /// Coleção de eventos gerados pelo request.
    /// </summary>
    IEnumerable<IEvent> Events { get; }
}
