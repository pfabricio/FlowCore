using FlowCore.Core.Interfaces;

namespace FlowCore.Messaging;

internal sealed class ImmediateRetryPolicy : IRetryPolicy
{
    private readonly int _maxAttempts;

    public ImmediateRetryPolicy(int maxAttempts = 3)
    {
        _maxAttempts = maxAttempts;
    }

    public ValueTask<RetryDecision> EvaluateAsync(RetryContext context, CancellationToken cancellationToken = default)
    {
        if (context.Attempt >= _maxAttempts)
        {
            return ValueTask.FromResult(new RetryDecision { Action = RetryAction.Stop });
        }

        return ValueTask.FromResult(new RetryDecision { Action = RetryAction.Retry });
    }
}