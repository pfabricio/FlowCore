namespace FlowCore.Core.Interfaces;

public interface ICachableQuery<TResult> : IQuery<TResult>
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
}