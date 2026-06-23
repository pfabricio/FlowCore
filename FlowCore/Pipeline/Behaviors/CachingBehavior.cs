using Microsoft.Extensions.DependencyInjection;
using FlowCore.Core.Interfaces;

namespace FlowCore.Pipeline.Behaviors;

/// <summary>
/// Behavior que implementa cache automático para queries que implementam ICachableQuery.
/// </summary>
/// <typeparam name="TRequest">Tipo do request.</typeparam>
/// <typeparam name="TResult">Tipo do resultado.</typeparam>
public class CachingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : IQuery<TResult>
{
    private readonly ICacheProvider? _cacheProvider;

    public CachingBehavior(IServiceProvider serviceProvider)
    {
        _cacheProvider = serviceProvider.GetService<ICacheProvider>();
    }

    /// <inheritdoc />
    public async Task<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        if (_cacheProvider == null || request is not ICachableQuery<TResult> cachable)
            return await next();

        var cached = await _cacheProvider.GetAsync<TResult>(cachable.CacheKey, cancellationToken);

        if (cached is not null)
            return cached;

        var response = await next();

        if (response is not null)
            await _cacheProvider.SetAsync(cachable.CacheKey, response, cachable.Expiration, cancellationToken);

        return response;
    }
}