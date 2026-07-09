namespace FlowCore.Resilience;

public sealed class PolicyComposer : IResiliencePolicy
{
    private readonly IReadOnlyList<IResiliencePolicy> _policies;

    public PolicyComposer(params IResiliencePolicy[] policies)
    {
        _policies = policies;
    }

    public PolicyComposer(IEnumerable<IResiliencePolicy> policies)
    {
        _policies = policies.ToArray();
    }

    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        Func<CancellationToken, ValueTask<TResult>> pipeline = operation;

        for (var i = _policies.Count - 1; i >= 0; i--)
        {
            var policy = _policies[i];
            var next = pipeline;

            pipeline = async ct => await policy.ExecuteAsync(next, ct).ConfigureAwait(false);
        }

        return await pipeline(cancellationToken).ConfigureAwait(false);
    }
}
