namespace FlowCore.Core.Interfaces;

/// <summary>
/// Delegate para o próximo handler no pipeline de behaviors.
/// </summary>
/// <typeparam name="TResult">Tipo do resultado.</typeparam>
/// <returns>Resultado da execução do próximo handler.</returns>
public delegate Task<TResult> RequestHandlerDelegate<TResult>();

/// <summary>
/// Interface para behaviors que interceptam a execução do handler.
/// Behaviors são executados em cadeia antes do handler final.
/// </summary>
/// <typeparam name="TRequest">Tipo do request.</typeparam>
/// <typeparam name="TResult">Tipo do resultado.</typeparam>
public interface IPipelineBehavior<TRequest, TResult>
{
    /// <summary>
    /// Processa o request e chama o próximo behavior ou handler.
    /// </summary>
    /// <param name="request">Request sendo processado.</param>
    /// <param name="next">Delegate para o próximo behavior/handler.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado do processamento.</returns>
    Task<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken);
}
