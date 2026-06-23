using FlowCore.Core;

namespace FlowCore.Core.Interfaces;

/// <summary>
/// Interface principal do mediator para operações CQRS.
/// </summary>
public interface IFlowMediator
{
    /// <summary>
    /// Executa um command e retorna o resultado.
    /// </summary>
    /// <typeparam name="TResult">Tipo do resultado.</typeparam>
    /// <param name="command">Command a ser executado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da execução.</returns>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa um command que não retorna valor.
    /// </summary>
    /// <param name="command">Command a ser executado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task SendAsync(ICommand<Unit> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa uma query e retorna o resultado.
    /// </summary>
    /// <typeparam name="TResult">Tipo do resultado.</typeparam>
    /// <param name="query">Query a ser executada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da query.</returns>
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publica um evento para todos os handlers registrados.
    /// </summary>
    /// <param name="event">Evento a ser publicado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}