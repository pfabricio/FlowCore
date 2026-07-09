namespace FlowCore.Resilience;

public sealed class FallbackPolicy : IResiliencePolicy
{
    private readonly Func<CancellationToken, ValueTask> _fallback;
    private readonly Func<Exception, bool>? _when;

    public FallbackPolicy(Action fallback)
    {
        _fallback = _ =>
        {
            fallback();
            return default;
        };
    }

    public FallbackPolicy(Func<CancellationToken, ValueTask> fallback, Func<Exception, bool>? when = null)
    {
        _fallback = fallback;
        _when = when;
    }

    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (_when?.Invoke(ex) ?? true)
        {
            await _fallback(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
