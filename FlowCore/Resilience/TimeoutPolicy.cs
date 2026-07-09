namespace FlowCore.Resilience;

public sealed class TimeoutPolicy : IResiliencePolicy
{
    private readonly TimeSpan _timeout;

    public TimeoutPolicy(TimeSpan timeout)
    {
        _timeout = timeout;
    }

    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            return await operation(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {_timeout.TotalMilliseconds}ms");
        }
    }
}
