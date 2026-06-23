namespace FlowCore.Core.Interfaces;

/// <summary>
/// Interface para handlers que processam queries.
/// </summary>
/// <typeparam name="TQuery">Tipo da query.</typeparam>
/// <typeparam name="TResult">Tipo do resultado.</typeparam>
public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Processa a query e retorna o resultado.
    /// </summary>
    /// <param name="query">Query a ser processada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da query.</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
