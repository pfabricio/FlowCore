namespace FlowCore.Core.Interfaces;

/// <summary>
/// Interface para queries que suportam cache automático.
/// Implemente esta interface para habilitar cache via CachingBehavior.
/// </summary>
/// <typeparam name="TResult">Tipo do resultado da query.</typeparam>
public interface ICachableQuery<TResult> : IQuery<TResult>
{
    /// <summary>
    /// Chave única para identificar o item no cache.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Tempo de expiração do cache. Null para expiração padrão.
    /// </summary>
    TimeSpan? Expiration { get; }
}
