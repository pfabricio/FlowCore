using System.Threading;

namespace FlowCore.Resilience;

public sealed class RateLimiterPolicy : IResiliencePolicy
{
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _rateLock = new();

    private int _requestCount;
    private DateTime _windowStart;

    public RateLimiterPolicy(int maxRequestsPerSecond)
        : this(maxRequestsPerSecond, TimeSpan.FromSeconds(1))
    {
    }

    public RateLimiterPolicy(int maxRequests, TimeSpan window)
    {
        if (maxRequests <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRequests));

        _maxRequests = maxRequests;
        _window = window;
        _windowStart = DateTime.UtcNow;
    }

    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        await AcquireSlotAsync(cancellationToken).ConfigureAwait(false);

        return await operation(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask AcquireSlotAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var now = DateTime.UtcNow;

                lock (_rateLock)
                {
                    if (now - _windowStart >= _window)
                    {
                        _requestCount = 0;
                        _windowStart = now;
                    }

                    if (_requestCount < _maxRequests)
                    {
                        _requestCount++;
                        return;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            await Task.Delay(_window / _maxRequests, cancellationToken).ConfigureAwait(false);
        }
    }
}
