namespace FlowCore.Core.Interfaces;

/// <summary>
/// Interface para handlers que processam eventos.
/// </summary>
/// <typeparam name="TEvent">Tipo do evento.</typeparam>
public interface IEventHandler<TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Processa o evento.
    /// </summary>
    /// <param name="event">Evento a ser processado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
