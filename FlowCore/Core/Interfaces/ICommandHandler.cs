namespace FlowCore.Core.Interfaces;

/// <summary>
/// Interface para handlers que processam commands.
/// </summary>
/// <typeparam name="TCommand">Tipo do command.</typeparam>
/// <typeparam name="TResult">Tipo do resultado.</typeparam>
public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Processa o command e retorna o resultado.
    /// </summary>
    /// <param name="command">Command a ser processado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado do processamento.</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
