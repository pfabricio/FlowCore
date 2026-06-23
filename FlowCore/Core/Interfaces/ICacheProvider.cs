namespace FlowCore.Core.Interfaces;

/// <summary>
/// Interface para provedores de cache.
/// Implemente esta interface para habilitar cache via CachingBehavior.
/// </summary>
public interface ICacheProvider
{
    /// <summary>
    /// Obtém um item do cache.
    /// </summary>
    /// <typeparam name="T">Tipo do item.</typeparam>
    /// <param name="key">Chave do item.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O item armazenado ou null se não encontrado.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Armazena um item no cache.
    /// </summary>
    /// <typeparam name="T">Tipo do item.</typeparam>
    /// <param name="key">Chave do item.</param>
    /// <param name="value">Valor a ser armazenado.</param>
    /// <param name="expiration">Tempo de expiração. Null para expiração padrão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um item do cache.
    /// </summary>
    /// <param name="key">Chave do item.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
